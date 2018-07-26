using io.nem1.sdk.Model.Mosaics;
using Lykke.Service.BlockchainApi.Contract.Balances;

namespace Lykke.Service.Nem.Api.Helpers
{
    public static class MosaicExtensions
    {
        public static string GetId(this Mosaic mosaic)
        {
            return $"{mosaic.NamespaceName}:{mosaic.MosaicName}";
        }

        public static WalletBalanceContract ToContract(this Mosaic mosaic, string address, long block)
        {
            if (mosaic != null)
            {
                return new WalletBalanceContract
                {
                    Address = address,
                    AssetId = mosaic.GetId(),
                    Balance = mosaic.Amount.ToString("D"),
                    Block = block
                };
            }

            return null;
        }
    }
}
