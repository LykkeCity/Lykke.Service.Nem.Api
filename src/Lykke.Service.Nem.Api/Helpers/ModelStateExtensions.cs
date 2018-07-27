using System;
using Common;
using io.nem1.sdk.Model.Accounts;
using io.nem1.sdk.Model.Mosaics;
using Lykke.Service.BlockchainApi.Contract.Transactions;
using Lykke.Service.Nem.Api.Domain.Assets;
using Lykke.Service.Nem.Api.Models.Transactions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;

namespace Lykke.Service.Nem.Api.Helpers
{
    public static class ModelStateExtensions
    {
        public static bool IsValidOperationId(this ModelStateDictionary modelState, Guid operationId, string field = null)
        {
            if (operationId == default)
            {
                modelState.AddModelError(field ?? "operationId",
                    "Invalid operation identifier, must be non-empty UUID");
                return false;
            }

            return true;
        }

        public static bool IsValidAddress(this ModelStateDictionary modelState, string address, string field = null)
        {
            try
            {
                Address.CreateFromEncoded(address);
            }
            catch
            {
                modelState.AddModelError(field ?? "Address",
                    "Invalid address, must be valid NEM address");
                return false;
            }

            return true;
        }

        public static bool IsValidNemMosaicId(this ModelStateDictionary modelState, string assetId, string field = null)
        {
            try
            {
                MosaicId.CreateFromMosaicIdentifier(assetId);
            }
            catch
            {
                modelState.AddModelError(field ?? "AssetId",
                    "Invalid asset identifier, must be valid NEM mosaic identifier");
                return false;
            }

            return true;
        }

        public static bool IsValidRequest(this ModelStateDictionary modelState, BuildSingleTransactionRequest request, out ulong amount)
        {
            amount = 0;

            // if model in invalid then we can't check request

            if (modelState.IsValid)
            {
                modelState.IsValidOperationId(request.OperationId, nameof(BuildSingleTransactionRequest.OperationId));
                modelState.IsValidAddress(request.FromAddress, nameof(BuildSingleTransactionRequest.FromAddress));
                modelState.IsValidAddress(request.ToAddress, nameof(BuildSingleTransactionRequest.ToAddress));
                modelState.IsValidNemMosaicId(request.AssetId);

                if (!ulong.TryParse(request.Amount, out amount) || amount == 0)
                {
                    modelState.AddModelError(nameof(BuildSingleTransactionRequest.Amount),
                        "Invalid amount, must be positive integer string");
                }
            }

            return modelState.IsValid;
        }

        public static bool IsValidRequest(this ModelStateDictionary modelState, BroadcastTransactionRequest request, out SignedTransactionContext context)
        {
            context = null;

            // if model in invalid then we can't check request

            if (modelState.IsValid)
            {
                modelState.IsValidOperationId(request.OperationId, nameof(BuildSingleTransactionRequest.OperationId));

                try
                {
                    context = JsonConvert.DeserializeObject<SignedTransactionContext>(request.SignedTransaction.Base64ToString());
                }
                catch
                {
                    modelState.AddModelError(nameof(SignTransactionRequest.TransactionContext),
                        "Invalid transaction context");
                }
            }

            return modelState.IsValid;
        }

        public static bool IsValidAzureContinuation(this ModelStateDictionary modelState, string continuation)
        {
            // kinda specific knowledge but there is no 
            // another way to ensure continuation token
            if (!string.IsNullOrEmpty(continuation))
            {
                try
                {
                    JsonConvert.DeserializeObject<TableContinuationToken>(Utils.HexToString(continuation));
                }
                catch
                {
                    modelState.AddModelError(nameof(continuation), "Invalid continuation token");
                    return false;
                }
            }

            return true;
        }
    }
}
