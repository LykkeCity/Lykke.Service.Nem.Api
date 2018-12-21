using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;
using io.nem1.sdk.Infrastructure.HttpRepositories;
using io.nem1.sdk.Model.Accounts;
using io.nem1.sdk.Model.Blockchain;
using io.nem1.sdk.Model.Mosaics;
using io.nem1.sdk.Model.Transactions;
using io.nem1.sdk.Model.Transactions.Messages;
using Lykke.Service.BlockchainApi.Contract;
using Lykke.Service.BlockchainApi.Contract.Common;
using Lykke.Service.BlockchainApi.Sdk;
using Newtonsoft.Json;

namespace Lykke.Service.Nem.Api.Services
{
    public class NemApi : IBlockchainApi
    {
        const string AddressSeparator = "$";
        const string XEM = "nem:xem";
        
        readonly string _nemUrl;
        readonly string _explorerUrl;
        readonly int _requiredConfirmations;
        readonly int _expiresInMinutes;

        public NemApi(string nemUrl, string explorerUrl, int requiredConfirmations, int expiresInMinutes)
        {
            _nemUrl = nemUrl;
            _explorerUrl = explorerUrl;
            _requiredConfirmations = requiredConfirmations;
            _expiresInMinutes = expiresInMinutes;
        }

        public Task<bool> AddressIsExistAsync(string address) => Task.FromResult(AddressIsValid(address));

        public bool AddressIsValid(string address)
        {
            try
            {
                return Address.CreateFromEncoded(address) != null;
            }
            catch
            {
                return false;
            }
        }

        public async Task<string> BroadcastTransactionAsync(string signedTransaction)
        {
            var announceRequest = JsonConvert.DeserializeObject<NemRequestAnnounce>(signedTransaction);
            var result = await _client.AnnounceTransaction(announceRequest);

            // see https://nemproject.github.io/#nemRequestResult 
            // for meaning of the code 

            switch (result.Code)
            {
                case 0:
                case 1:
                    // success
                    break;
                case 3:
                case 9:
                    throw new BlockchainException(BlockchainErrorCode.BuildingShouldBeRepeated, "Transaction expired");
                case 7:
                    throw new BlockchainException(BlockchainErrorCode.BuildingShouldBeRepeated, "Transaction hash duplicated");
                case 5:
                    throw new BlockchainException(BlockchainErrorCode.NotEnoughBalance, "Not enough balance");
                default:
                    throw new ArgumentException(JsonConvert.SerializeObject(result));
            }

            return "";
        }

        public async Task<(string transactionContext, decimal fee, long expiration)> BuildTransactionAsync(Guid operationId, IAsset asset, IReadOnlyList<IOperationAction> actions, bool includeFee)
        {
            // from one side NEM supports single sender and single receiver per transaction,
            // from the other side we support single asset per transaction,
            // finally only single transfers are allowed

            if (actions.Count != 1)
            {
                throw new ArgumentException("Transaction must contain a single transfer only");
            }

            var networkType = await new NodeHttp(_nemUrl).GetNetworkType();
            var action = actions[0];
            var toAddressParts = action.To.Split(AddressSeparator);
            var toAddress = toAddressParts[0];
            var memo = toAddressParts.Length > 1
                ? toAddressParts[1]
                : "";
            var deadline = Deadline.CreateMinutes(_expiresInMinutes);
            var tx = TransferTransaction.Create(
                networkType,
                deadline,
                Address.CreateFromEncoded(toAddress),
                new List<Mosaic> { Mosaic.CreateFromIdentifier(asset.AssetId, (ulong)asset.ToBaseUnit(action.Amount)) },
                !string.IsNullOrEmpty(memo)
                    ? PlainMessage.Create(memo) as IMessage
                    : EmptyMessage.Create()
            );
            var fee = await tx.CalculateFee(new NamespaceMosaicHttp(_nemUrl));
            var required = new List<Mosaic>
            {
                Xem.CreateAbsolute(fee.fee)
            };

            if (includeFee)
            {
                try
                {
                    checked
                    {
                        if (tx.Mosaics[0].NamespaceName == Xem.NamespaceName &&
                            tx.Mosaics[0].MosaicName == Xem.MosaicName)
                        {
                            tx.Mosaics[0].Amount -= fee.fee;
                        }

                        // only single transfers are supported,
                        // so there must be single levy

                        var levy = fee.levies.SingleOrDefault();
                        if (levy != null &&
                            tx.Mosaics[0].NamespaceName == levy.NamespaceName &&
                            tx.Mosaics[0].MosaicName == levy.MosaicName)
                        {
                            tx.Mosaics[0].Amount -= levy.Amount;
                        }
                    }
                }
                catch(OverflowException)
                {
                    throw new BlockchainException(BlockchainErrorCode.AmountIsTooSmall, "Amount is less than fee");
                }
            }

            required = required
                .Concat(tx.Mosaics)
                .Concat(fee.levies)
                .GroupBy(m => new { m.NamespaceName, m.MosaicName })
                .Select(g => new Mosaic(g.Key.NamespaceName, g.Key.MosaicName, g.Aggregate(0UL, (v, m) => v += m.Amount)))
                .ToList();

            // check balance of FromAddress for all required assets

            var fromAddressParts = action.From.Split(AddressSeparator);
            var fromAddress = fromAddressParts[0];
            var mosaicOwned = await new AccountHttp(_nemUrl).MosaicsOwned(Address.CreateFromEncoded(fromAddress));

            foreach (var mosaic in required)
            {
                var owned = mosaicOwned.FirstOrDefault(m => m.NamespaceName == mosaic.NamespaceName && m.MosaicName == mosaic.MosaicName)?.Amount ?? 0UL;
                if (owned < mosaic.Amount)
                {
                    throw new BlockchainException(BlockchainErrorCode.NotEnoughBalance,
                        $"Not enough {mosaic.NamespaceName}:{mosaic.MosaicName}");
                }
            }

            return (
                tx.ToJson().ToBase64(),
                Convert.ToDecimal(fee.fee * 1e-6),
                deadline.GetInstant()
            );
        }

        public CapabilitiesResponse GetCapabilities()
        {
            return new CapabilitiesResponse()
            {
                AreManyInputsSupported = false,
                AreManyOutputsSupported = false,
                IsTransactionsRebuildingSupported = false,
                IsTestingTransfersSupported = false,
                IsPublicAddressExtensionRequired = false,
                IsReceiveTransactionRequired = false,
                CanReturnExplorerUrl = !string.IsNullOrEmpty(_explorerUrl),
                IsAddressMappingRequired = false,
                IsExclusiveWithdrawalsRequired = false
            };
        }

        public ConstantsResponse GetConstants() => new ConstantsResponse();

        public string[] GetExplorerUrl(string address)
        {
            return !string.IsNullOrEmpty(_explorerUrl)
                ? new string[] { string.Format(_explorerUrl, address) }
                : new string[] { };
        }

        public async Task<long> GetLastConfirmedBlockNumberAsync() =>
            (await _client.GetBlockchainHeight()).Height - _requiredConfirmations;

        public async Task<BlockchainTransaction> GetTransactionAsync(string transactionHash, long expiration, IAsset asset)
        {
            var tx = await _client.GetTransaction(transactionHash);

            if (tx == null || tx.Meta.Height == null)
            {
                var lastConfirmedBlock = await _client.GetBlock(
                    await GetLastConfirmedBlockNumberAsync());

                return lastConfirmedBlock.TimeStamp > expiration
                    ? BlockchainTransaction.Failed("Transaction expired", BlockchainErrorCode.BuildingShouldBeRepeated)
                    : BlockchainTransaction.InProgress();
            }
            else
            {
                var blockNumber = tx.Meta.Height.Value;
                var blockTime = _nemesisTimestamp.AddSeconds(tx.Transaction.TimeStamp);
                var from = NemUtils.PublicKeyToAddress(tx.Transaction.Signer, _networkType);
                var transfers = tx.Transaction.Mosaics == null || tx.Transaction.Mosaics.Length == 0
                    ? new[] { new { actionId = transactionHash.CalculateHexHash32(), amount = Convert.ToDecimal(tx.Transaction.Amount * 1e-6) } }
                    : tx.Transaction.Mosaics.Select(m => new { actionId = m.MosaicId.ToString().CalculateHexHash32(), amount = asset.FromBaseUnit(m.Quantity) });
                var actions = transfers.SelectMany(x => new[]
                    {
                        new BlockchainAction(x.actionId, blockNumber, blockTime, transactionHash, from, asset.AssetId, (-1) * x.amount),
                        new BlockchainAction(x.actionId, blockNumber, blockTime, transactionHash, tx.Transaction.Recipient, asset.AssetId, x.amount),
                    });

                return BlockchainTransaction.Completed(blockNumber, blockTime, actions.ToArray());
            }
        }

        public Task ObserveAddressAsync(string address) => Task.CompletedTask;

        public Task DeleteAddressObservationAsync(string address) => Task.CompletedTask;

        public bool CanGetBalances => false;

        public Task<BlockchainBalance[]> GetBalancesAsync(string[] addresses, Func<string, Task<IAsset>> getAsset) =>
            Task.FromException<BlockchainBalance[]>(new NotImplementedException());
    }
}