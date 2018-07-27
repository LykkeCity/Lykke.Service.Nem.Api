using Lykke.Service.Nem.Api.Domain.Operations;

namespace Lykke.Service.Nem.Api.Domain.Helpers
{
    public static class OperationExtensions
    {
        public static bool IsSent(this IOperation operation)
        {
            return operation.SendTime != null;
        }
    }
}