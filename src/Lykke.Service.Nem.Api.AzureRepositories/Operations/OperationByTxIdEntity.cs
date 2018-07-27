using System;
using Lykke.AzureStorage.Tables;
using Microsoft.WindowsAzure.Storage.Table;

namespace Lykke.Service.Nem.Api.AzureRepositories.Operations
{
    public class OperationByTxIdEntity : TableEntity
    {
        public Guid OperationId { get; set; }
    }
}