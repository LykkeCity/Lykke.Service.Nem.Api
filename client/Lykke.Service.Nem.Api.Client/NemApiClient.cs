using Lykke.HttpClientGenerator;

namespace Lykke.Service.Nem.Api.Client
{
    public class NemApiClient : INemApiClient
    {
        //public IControllerApi Controller { get; }
        
        public NemApiClient(IHttpClientGenerator httpClientGenerator)
        {
            //Controller = httpClientGenerator.Generate<IControllerApi>();
        }
        
    }
}
