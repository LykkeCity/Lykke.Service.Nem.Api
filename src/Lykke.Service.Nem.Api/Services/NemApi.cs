using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Common;
using io.nem1.sdk.Infrastructure.HttpRepositories;
using io.nem1.sdk.Infrastructure.Imported.Client;
using io.nem1.sdk.Model.Accounts;
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
        const char AddressSeparator = '$';
        const string XEM = "nem:xem";

        readonly static DateTime _nemesis = 
            new DateTime(2015, 03, 29, 0, 6, 25, 0).ToUniversalTime();

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
                return Address.CreateFromEncoded(address.Split(AddressSeparator)[0]) != null;
            }
            catch
            {
                return false;
            }
        }

        public async Task<string> BroadcastTransactionAsync(string signedTransaction)
        {
            var result = await new TransactionHttp(_nemUrl).Announce(SignedTransaction.FromJson(signedTransaction));

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

            return null;
        }

        public async Task<(string transactionContext, decimal fee, long expiration)> BuildTransactionAsync(Guid operationId, 
            IAsset asset, IReadOnlyList<IOperationAction> actions, bool includeFee)
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
            var message = !string.IsNullOrEmpty(memo)
                ? PlainMessage.Create(memo) as IMessage
                : EmptyMessage.Create();
            var mosaic = Mosaic.CreateFromIdentifier(asset.AssetId, (ulong)asset.ToBaseUnit(action.Amount));
            var fee = await TransferTransaction.CalculateFee(networkType, message, new [] { mosaic }, new NamespaceMosaicHttp(_nemUrl));

            if (includeFee)
            {
                try
                {
                    checked
                    {
                        if (mosaic.NamespaceName == Xem.NamespaceName &&
                            mosaic.MosaicName == Xem.MosaicName)
                        {
                            mosaic.Amount -= fee.fee;
                        }

                        // only single transfers are supported,
                        // so there must be single levy

                        var levy = fee.levies.SingleOrDefault();

                        if (levy != null &&
                            mosaic.NamespaceName == levy.NamespaceName &&
                            mosaic.MosaicName == levy.MosaicName)
                        {
                            mosaic.Amount -= levy.Amount;
                        }
                    }
                }
                catch(OverflowException)
                {
                    throw new BlockchainException(BlockchainErrorCode.AmountIsTooSmall, "Amount is less than fee");
                }
            }

            // check balances of FromAddress for all required assets

            var fromAddress = action.From.Split(AddressSeparator)[0];
            var owned = await new AccountHttp(_nemUrl).MosaicsOwned(Address.CreateFromEncoded(fromAddress));
            var required = fee.levies
                .Append(Xem.CreateAbsolute(fee.fee))
                .Append(mosaic)
                .GroupBy(m => new { m.NamespaceName, m.MosaicName })
                .Select(g => new Mosaic(g.Key.NamespaceName, g.Key.MosaicName, g.Aggregate(0UL, (v, m) => v += m.Amount)))
                .ToList();

            foreach (var req in required)
            {
                var own = owned.FirstOrDefault(m => m.NamespaceName == req.NamespaceName && m.MosaicName == req.MosaicName)?.Amount ?? 0UL;
                if (own < req.Amount)
                {
                    throw new BlockchainException(BlockchainErrorCode.NotEnoughBalance, 
                        $"Not enough {req.NamespaceName}:{req.MosaicName}");
                }
            }

            var tx = TransferTransaction.Create(networkType, Deadline.CreateMinutes(_expiresInMinutes), fee.fee,
                Address.CreateFromEncoded(toAddress), new List<Mosaic> { mosaic }, message);

            return (
                tx.ToJson(),
                Convert.ToDecimal(fee.fee * 1e-6),
                tx.Deadline.GetInstant()
            );
        }

        public bool CanGetBalances => false;

        public Task DeleteAddressObservationAsync(string address) => Task.CompletedTask;

        public Task<BlockchainBalance[]> GetBalancesAsync(string[] addresses, Func<string, Task<IAsset>> getAsset) =>
            Task.FromException<BlockchainBalance[]>(new NotImplementedException());

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

        public ConstantsResponse GetConstants()
        {
            return new ConstantsResponse
            {
                PublicAddressExtension = new PublicAddressExtensionConstantsContract 
                { 
                    BaseDisplayName = "Address",
                    DisplayName = "Message",
                    Separator = AddressSeparator
                }
            };
        }

        public string[] GetExplorerUrl(string address)
        {
            return !string.IsNullOrEmpty(_explorerUrl)
                ? new string[] { string.Format(_explorerUrl, address) }
                : new string[] { };
        }
 
        public async Task<long> GetLastConfirmedBlockNumberAsync() =>
            (long)((await new BlockchainHttp(_nemUrl).GetBlockchainHeight()) - (ulong)_requiredConfirmations);

        public async Task<BlockchainTransaction> GetTransactionAsync(string transactionHash, long expiration, IAsset asset)
        {
            Transaction tx = null;

            try
            {
                tx = await new TransactionHttp(_nemUrl).GetByHash(transactionHash);
            }
            catch (ApiException ex) when (ex.ErrorCode == 400 && ex.Message.Contains("Hash was not found in cache"))
            {
                // transaction not found
            }

            if (tx == null || tx.TransactionInfo.Height == 0)
            {
                var http = new BlockchainHttp(_nemUrl);
                var currentBlockchainHeight = await http.GetBlockchainHeight();
                var lastConfirmedBlock = await http.GetBlockByHeight(currentBlockchainHeight - (ulong)_requiredConfirmations);

                return lastConfirmedBlock.TimeStamp > expiration
                    ? BlockchainTransaction.Failed("Transaction expired", BlockchainErrorCode.BuildingShouldBeRepeated)
                    : BlockchainTransaction.InProgress();
            }
            else
            {
                var transfer = tx as TransferTransaction ?? 
                    throw new ArgumentException("Transaction must be of Transfer type");

                var blockNumber = (long)transfer.TransactionInfo.Height;
                var blockTime = _nemesis.AddSeconds(transfer.TransactionInfo.TimeStamp);
                var memo = (transfer.Message as PlainMessage)?.GetStringPayload();
                var to = string.IsNullOrEmpty(memo)
                    ? transfer.Address.Plain
                    : transfer.Address.Plain + AddressSeparator + memo;
                var actions = new List<BlockchainAction>();

                foreach (var mos in transfer.Mosaics)
                {
                    if (asset.AssetId != $"{mos.NamespaceName}:{mos.MosaicName}")
                        continue;

                    var actionId = $"{asset.AssetId}:{mos.Amount}".CalculateHexHash32();
                    var amount = asset.FromBaseUnit((long)mos.Amount);

                    actions.Add(new BlockchainAction(actionId, blockNumber, blockTime, transactionHash, transfer.Signer.Address.Plain, asset.AssetId, (-1) * amount));
                    actions.Add(new BlockchainAction(actionId, blockNumber, blockTime, transactionHash, to, asset.AssetId, amount));
                }

                return BlockchainTransaction.Completed(blockNumber, blockTime, actions.ToArray());
            }
        }

        public Task ObserveAddressAsync(string address) => Task.CompletedTask;
    }
}