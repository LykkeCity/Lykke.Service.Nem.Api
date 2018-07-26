using Lykke.Service.BlockchainApi.Contract.Addresses;
using Lykke.Service.Nem.Api.Helpers;
using Lykke.Service.Nem.Api.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Lykke.Service.Nem.Api.Controllers
{
    [Route("/api/addresses")]
    public class AddressesController : Controller
    {
        private readonly BlockchainSettings _bcnSettings;

        public AddressesController(BlockchainSettings bcnSettings)
        {
            _bcnSettings = bcnSettings;
        }

        [HttpGet("{address}/validity")]
        public AddressValidationResponse IsValid([FromRoute]string address)
        {
            return new AddressValidationResponse()
            {
                IsValid = ModelState.IsValidAddress(address)
            };
        }

        [HttpGet("{address}/explorer-url")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string[]))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult ExplorerUrl([FromRoute]string address)
        {
            if (!ModelState.IsValid ||
                !ModelState.IsValidAddress(address))
            {
                return BadRequest(ModelState);
            }

            var urls = !string.IsNullOrEmpty(_bcnSettings.ExplorerUrl) ?
                new string[] { string.Format(_bcnSettings.ExplorerUrl, address) } :
                new string[] { };

            return Ok(urls);
        }
    }
}
