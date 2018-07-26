using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Common;
using io.nem1.sdk.Infrastructure.HttpRepositories;
using io.nem1.sdk.Model.Accounts;
using Lykke.Common.Api.Contract.Responses;
using Lykke.Service.BlockchainApi.Contract;
using Lykke.Service.BlockchainApi.Contract.Balances;
using Lykke.Service.Nem.Api.Domain.Balances;
using Lykke.Service.Nem.Api.Domain.Helpers;
using Lykke.Service.Nem.Api.Helpers;
using Lykke.Service.Nem.Api.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;

namespace Lykke.Service.Nem.Api.Controllers
{
    [Route("/api/balances")]
    public class BalancesController : Controller
    {
        private readonly BlockchainSettings _bcnSettings;
        private readonly IBalanceAddressRepository _balanceAddressRepository;

        public BalancesController(BlockchainSettings bcnSettings, IBalanceAddressRepository balanceAddressRepository)
        {
            _bcnSettings = bcnSettings;
            _balanceAddressRepository = balanceAddressRepository;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PaginationResponse<WalletBalanceContract>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Get(
            [FromQuery, Range(1, int.MaxValue)] int take,
            [FromQuery] string continuation)
        {
            if (!ModelState.IsValid ||
                !ModelState.IsValidAzureContinuation(continuation))
            {
                return BadRequest(ModelState);
            }

            var accountHttp = new AccountHttp(_bcnSettings.Host);
            var blockchainHttp = new BlockchainHttp(_bcnSettings.Host);
            var block = Convert.ToInt64(await blockchainHttp.GetBlockchainHeight()) - _bcnSettings.ConfirmationsRequired;
            var items = new List<WalletBalanceContract>();

            do
            {
                var chunk = await _balanceAddressRepository.Get(take, continuation);

                continuation = chunk.continuation;

                foreach (var address in chunk.items)
                {
                    var owned = await accountHttp.MosaicsOwned(Address.CreateFromEncoded(address));
                    var nonZero = owned.Where(m => m.Amount > 0);

                    if (nonZero.Any())
                    {
                        items.AddRange(nonZero.Select(m => m.ToContract(address, block)));
                        take--;
                    }
                }
            }
            while (take > 0 && !string.IsNullOrEmpty(continuation));

            return Ok(PaginationResponse.From(continuation, items));
        }

        [HttpPost("{address}/observation")]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Create([FromRoute]string address)
        {
            if (!ModelState.IsValid ||
                !ModelState.IsValidAddress(address))
            {
                return BadRequest(ModelState);
            }

            if (await _balanceAddressRepository.Exists(address))
            {
                return StatusCode(StatusCodes.Status409Conflict);
            }
            else
            {
                await _balanceAddressRepository.Upsert(address);
                return Ok();
            }
        }

        [HttpDelete("{address}/observation")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Delete([FromRoute]string address)
        {
            if (!ModelState.IsValid ||
                !ModelState.IsValidAddress(address))
            {
                return BadRequest(ModelState);
            }

            if (await _balanceAddressRepository.Exists(address))
            {
                await _balanceAddressRepository.Delete(address);
                return Ok();
            }
            else
            {
                return NoContent();
            }
        }
    }
}
