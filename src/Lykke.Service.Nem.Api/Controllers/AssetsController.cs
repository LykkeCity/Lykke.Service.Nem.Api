using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Lykke.Service.BlockchainApi.Contract;
using Lykke.Service.BlockchainApi.Contract.Assets;
using Lykke.Service.Nem.Api.Domain.Assets;
using Lykke.Service.Nem.Api.Helpers;
using Lykke.Service.Nem.Api.Models.Assets;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Lykke.Service.Nem.Api.Controllers
{
    [Route("/api/assets")]
    public class AssetsController : Controller
    {
        private readonly IAssetRepository _assetRepository;

        public AssetsController(IAssetRepository assetRepository)
        {
            _assetRepository = assetRepository;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PaginationResponse<AssetContract>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAssetList(
            [FromQuery, Range(1, int.MaxValue)] int take,
            [FromQuery] string continuation)
        {
            if (!ModelState.IsValid ||
                !ModelState.IsValidAzureContinuation(continuation))
            {
                return BadRequest(ModelState);
            }

            var chunk = await _assetRepository.Get(take, continuation);

            return Ok(PaginationResponse.From(
                chunk.continuation,
                chunk.items.Select(e => e.ToContract()).ToArray()));
        }

        [HttpGet("{assetId}")]
        public async Task<AssetResponse> GetAsset(string assetId)
        {
            return (await _assetRepository.Get(assetId)).ToResponse();
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateAssetRequest asset)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            else
            {
                await _assetRepository.Upsert(asset);
                return Ok();
            }
        }
    }
}
