using Lykke.Service.BlockchainApi.Contract.Common;
using Microsoft.AspNetCore.Mvc;

namespace Lykke.Service.Nem.Api.Controllers
{
    [Route("/api/capabilities")]
    public class CapabilitiesController : Controller
    {
        [HttpGet]
        public CapabilitiesResponse Get()
        {
            return new CapabilitiesResponse()
            {
                AreManyInputsSupported = false,
                AreManyOutputsSupported = false,
                IsTransactionsRebuildingSupported = false,
                IsTestingTransfersSupported = false,
                IsPublicAddressExtensionRequired = false,
                IsReceiveTransactionRequired = false,
                CanReturnExplorerUrl = true,
                IsAddressMappingRequired = false,
                IsExclusiveWithdrawalsRequired = false
            };
        }
    }
}
