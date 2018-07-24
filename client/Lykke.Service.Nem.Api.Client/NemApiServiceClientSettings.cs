using Lykke.SettingsReader.Attributes;

namespace Lykke.Service.Nem.Api.Client 
{
    /// <summary>
    /// Nem.Api client settings.
    /// </summary>
    public class NemApiServiceClientSettings 
    {
        /// <summary>Service url.</summary>
        [HttpCheck("api/isalive")]
        public string ServiceUrl {get; set;}
    }
}
