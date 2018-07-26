using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Lykke.Service.Nem.Api.Controllers
{
    [Route("/api/testing")]
    public class TestingController : Controller
    {
        [HttpPost("transfers")]
        [ProducesResponseType(StatusCodes.Status501NotImplemented)]
        public IActionResult Transfer()
        {
            return StatusCode(StatusCodes.Status501NotImplemented);
        }
    }
}
