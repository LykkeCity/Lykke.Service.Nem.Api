using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Common;
using io.nem1.sdk.Infrastructure.HttpRepositories;
using io.nem1.sdk.Model.Accounts;
using io.nem1.sdk.Model.Blockchain;
using io.nem1.sdk.Model.Mosaics;
using io.nem1.sdk.Model.Transactions;
using io.nem1.sdk.Model.Transactions.Messages;
using Lykke.Service.BlockchainApi.Contract;
using Lykke.Service.BlockchainApi.Contract.Transactions;
using Lykke.Service.Nem.Api.Domain.Assets;
using Lykke.Service.Nem.Api.Domain.Helpers;
using Lykke.Service.Nem.Api.Domain.Operations;
using Lykke.Service.Nem.Api.Helpers;
using Lykke.Service.Nem.Api.Models.Transactions;
using Lykke.Service.Nem.Api.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Lykke.Service.Nem.Api.Controllers
{
    [Route("/api/transactions")]
    public class TransactionsController : Controller
    {
        private readonly BlockchainSettings _bcnSettings;
        private readonly IOperationRepository _operationRepository;
        private readonly IAssetRepository _assetRepository;
        private static readonly string _xemId = $"{Xem.NamespaceName}:{Xem.MosaicName}";

        public TransactionsController(BlockchainSettings bcnSettings, IOperationRepository operationRepository, IAssetRepository assetRepository)
        {
            _bcnSettings = bcnSettings;
            _operationRepository = operationRepository;
            _assetRepository = assetRepository;
        }

        [HttpPost("single")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(BuildTransactionResponse))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> BuildSingle([FromBody] BuildSingleTransactionRequest request)
        {
            if (!ModelState.IsValid ||
                !ModelState.IsValidRequest(request, out ulong amount))
            {
                return BadRequest(ModelState);
            }

            var asset = await _assetRepository.Get(request.AssetId);
            if (asset == null)
            {
                return BadRequest(BlockchainErrorResponse.Create("Unknown assetId"));
            }

            var operation = await _operationRepository.Get(request.OperationId);
            if (operation != null && operation.IsSent())
            {
                return StatusCode(StatusCodes.Status409Conflict, BlockchainErrorResponse.Create("Operation is already sent"));
            }

            // calculate amounts and fees

            var tx = TransferTransaction.Create(
                NetworkType.GetNetwork(_bcnSettings.Network),
                Deadline.CreateMinutes(_bcnSettings.ExpiresInMinutes),
                Address.CreateFromEncoded(request.ToAddress),
                new List<Mosaic> { Mosaic.CreateFromIdentifier(request.AssetId, amount) },
                EmptyMessage.Create()
            );

            var requiredNative = tx.Fee;
            var requiredMosaic = amount;

            if (request.AssetId == _xemId) // is native XEM transfer
            {
                if (request.IncludeFee)
                {
                    if (amount <= tx.Fee)
                        return BadRequest(BlockchainErrorResponse.FromKnownError(BlockchainErrorCode.AmountIsTooSmall));
                    else
                        amount -= tx.Fee;
                }

                requiredNative = requiredNative + amount;
                requiredMosaic = requiredNative;
            }
            else
            {
                // TODO: support mosaics with non-zero levy
            }

            // check balance of FromAddress

            var http = new AccountHttp(_bcnSettings.Host);
            var data = await http.MosaicsOwned(Address.CreateFromEncoded(request.FromAddress));

            var ownedNative = data.FirstOrDefault(m => m.GetId() == _xemId);
            if (ownedNative == null || ownedNative.Amount < requiredNative)
            {
                return BadRequest(BlockchainErrorResponse.FromKnownError(BlockchainErrorCode.NotEnoughBalance));
            }

            var ownedMosaic = data.FirstOrDefault(m => m.GetId() == request.AssetId);
            if (ownedMosaic == null || ownedMosaic.Amount < requiredMosaic)
            {
                return BadRequest(BlockchainErrorResponse.FromKnownError(BlockchainErrorCode.NotEnoughBalance));
            }

            // store operation

            await _operationRepository.Upsert(request.OperationId, request.FromAddress, request.ToAddress, request.IncludeFee,
                amount, asset.ToDecimal(amount), tx.Fee, asset.ToDecimal(tx.Fee), request.AssetId);

            // build context for signing

            var context = new TransactionContext
            {
                ExpiresInMinutes = _bcnSettings.ExpiresInMinutes,
                To = request.ToAddress,
                AssetId = request.AssetId,
                Amount = amount,
                Fee = tx.Fee
            };

            return Ok(new BuildTransactionResponse
            {
                TransactionContext = JsonConvert.SerializeObject(context).ToBase64()
            });
        }

        [HttpPost("single/receive")]
        [HttpPost("many-inputs")]
        [HttpPost("many-outputs")]
        [HttpPut()]
        [ProducesResponseType(StatusCodes.Status501NotImplemented)]
        public IActionResult NotImplemented()
        {
            return StatusCode(StatusCodes.Status501NotImplemented);
        }

        [HttpPost("broadcast")]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Broadcast([FromBody] BroadcastTransactionRequest request)
        {
            if (!ModelState.IsValid ||
                !ModelState.IsValidRequest(request, out var context))
            {
                return BadRequest(ModelState);
            }

            var operation = await _operationRepository.Get(request.OperationId);
            if (operation != null && operation.IsSent())
            {
                return StatusCode(StatusCodes.Status409Conflict,
                    BlockchainErrorResponse.Create($"Operation [{request.OperationId}] already broadcasted"));
            }

            // For now transaction can be safely broadcasted multiple times.
            // Any subsequent try, after the first successful, is accepted by node but does nothing within blockchain.
            // So we can simply repeat failed requests until all data stored successfully.
            
            var signedTransaction = SignedTransaction.Create(
                context.Payload.GetHexStringToBytes(),
                context.Signature.GetHexStringToBytes(),
                context.Hash.GetHexStringToBytes(),
                context.Signer.GetHexStringToBytes(),
                context.TransactionType);

            var http = new TransactionHttp(_bcnSettings.Host);

            var data = await http.Announce(signedTransaction);

            switch (data.Code)
            {
                case 0:
                case 1:
                    // success
                    break;
                case 3:
                    return this.BadRequest(BlockchainErrorResponse.Create("Transaction expired"));
                case 5:
                    return this.BadRequest(BlockchainErrorResponse.FromKnownError(BlockchainErrorCode.NotEnoughBalance));
                default:
                    return this.BadRequest(data);
            }

            var expiryTime = DateTime.UtcNow.AddMinutes(_bcnSettings.ExpiresInMinutes);

            await _operationRepository.UpsertOperationIdByTxId(request.OperationId, data.Hash);
            await _operationRepository.UpsertOperationIdByExpiryTime(request.OperationId, expiryTime);
            await _operationRepository.Update(request.OperationId, 
                sendTime: DateTime.UtcNow, expiryTime: expiryTime, txId: data.Hash);

            return Ok(data);
        }
    }
}
