namespace io.nem1.sdk.Model.Mosaics
{
    public static class MosaicExtensions
    {
        public static string GetId(this Mosaic mosaic)
        {
            return $"{mosaic.NamespaceName}:{mosaic.MosaicName}";
        }
    }
}
