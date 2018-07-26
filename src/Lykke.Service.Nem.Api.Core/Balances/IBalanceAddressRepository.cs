using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Lykke.Service.Nem.Api.Domain.Balances
{
    public interface IBalanceAddressRepository : IRepository<string>
    {
        Task<bool> Exists(string address);
        Task Upsert(string address);
        Task Delete(string address);
    }
}
