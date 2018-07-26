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
        private static readonly string _xemId = $"{Xem.NamespaceName}:{Xem.MosaicName}";

        public TransactionsController(BlockchainSettings bcnSettings)
        {
            _bcnSettings = bcnSettings;
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
                    amount -= tx.Fee;
                }

                requiredNative = requiredNative + amount;
                requiredMosaic = requiredNative;
            }
            else
            {
                // TODO: support mosaics with non-zero levy
            }

            if (amount <= 0)
            {
                return BadRequest(BlockchainErrorResponse.FromKnownError(BlockchainErrorCode.AmountIsTooSmall));
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

        [HttpPost("broadcast")]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Broadcast([FromBody] BroadcastTransactionRequest request)
        {
            if (!ModelState.IsValid ||
                !ModelState.IsValidRequest(request, out var context))
            {
                return BadRequest(ModelState);
            }

            var signedTransaction = SignedTransaction.Create(
                context.Payload.GetHexStringToBytes(),
                context.Signature.GetHexStringToBytes(),
                context.Hash.GetHexStringToBytes(),
                context.Signer.GetHexStringToBytes(),
                context.TransactionType);

            var http = new TransactionHttp(_bcnSettings.Host);

            var data = await http.Announce(signedTransaction);

            return Ok(data);
        }
    }
}
