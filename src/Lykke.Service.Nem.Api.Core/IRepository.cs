using System.Threading.Tasks;

namespace Lykke.Service.Nem.Api.Domain
{
    public interface IRepository<E>
    {
        Task<(string continuation, E[] items)> Get(int take = 100, string continuation = null);
    }
}
