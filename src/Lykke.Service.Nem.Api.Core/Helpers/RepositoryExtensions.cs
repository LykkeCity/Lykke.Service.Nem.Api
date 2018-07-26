using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lykke.Service.Nem.Api.Domain.Helpers
{
    public static class RepositoryExtensions
    {
        public static async Task<E[]> All<E>(this IRepository<E> repo)
        {
            string continuation = null;
            var items = new List<E>();

            do
            {
                var chunk = await repo.Get(1000, continuation);
                continuation = chunk.continuation;
                items.AddRange(chunk.items);
            } while (continuation != null);

            return items.ToArray();
        }
    }
}
