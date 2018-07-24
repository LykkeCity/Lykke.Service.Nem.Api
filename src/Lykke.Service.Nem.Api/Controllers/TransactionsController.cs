using System.Reactive.Linq;
using System.Threading.Tasks;
using Common;
using io.nem1.sdk.Infrastructure.HttpRepositories;
using io.nem1.sdk.Model.Transactions;
using Lykke.Service.BlockchainApi.Contract.Transactions;
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

        public TransactionsController(BlockchainSettings bcnSettings)
        {
            _bcnSettings = bcnSettings;
        }

        [HttpPost("single")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(BuildTransactionResponse))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> BuildSingle(BuildSingleTransactionRequest request)
        {
            if (!ModelState.IsValid ||
                !ModelState.IsValidRequest(request, out ulong amount))
            {
                return BadRequest(ModelState);
            }

            // TODO: check balance of FromAddress
            // TODO: fee?

            var context = new SignTransactionContext
            {
                ExpiresInMinutes = _bcnSettings.ExpiresInMinutes,
                To = request.ToAddress,
                AssetId = request.AssetId,
                Amount = amount
            };

            return Ok(new BuildTransactionResponse
            {
                TransactionContext = JsonConvert.SerializeObject(context).ToBase64()
            });
        }

        [HttpPost("broadcast")]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Broadcast(BroadcastTransactionRequest request)
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

        public class SignTransactionContext
        {
            public int ExpiresInMinutes { get; set; }
            public string To { get; set; }
            public string AssetId { get; set; }
            public ulong Amount { get; set; }
        }
    }
}
