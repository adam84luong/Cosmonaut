﻿using System;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Newtonsoft.Json;

namespace Cosmonaut.Response
{
    public class CosmosResponse<TEntity> where TEntity : class
    {
        public bool IsSuccess => (ResourceResponse?.StatusCode != null &&
            (int)ResourceResponse.StatusCode >= 200 && 
            (int)ResourceResponse.StatusCode <= 299 && 
            CosmosOperationStatus == CosmosOperationStatus.Success) || 
            (ResourceResponse == null && CosmosOperationStatus == CosmosOperationStatus.Success);

        public CosmosOperationStatus CosmosOperationStatus { get; } = CosmosOperationStatus.Success;

        public ResourceResponse<Document> ResourceResponse { get; }

        public TEntity Entity { get; set; }

        public Exception Exception { get; }

        public CosmosResponse(ResourceResponse<Document> resourceResponse)
        {
            ResourceResponse = resourceResponse;
        }

        public CosmosResponse(CosmosOperationStatus statusType)
        {
            CosmosOperationStatus = statusType;
        }

        public CosmosResponse(TEntity entity, Exception exception)
        {
            if (exception == null)
                return;

            Entity = entity;
            Exception = exception;
            CosmosOperationStatus = CosmosOperationStatus.Exception;
        }

        public CosmosResponse(TEntity entity, ResourceResponse<Document> resourceResponse)
        {
            ResourceResponse = resourceResponse;
            Entity = entity;
        }

        public CosmosResponse(TEntity entity, CosmosOperationStatus statusType)
        {
            CosmosOperationStatus = statusType;
            Entity = entity;
        }

        public static implicit operator TEntity(CosmosResponse<TEntity> response)
        {
            if (response?.Entity != null)
                return response.Entity;

            if (!string.IsNullOrEmpty(response?.ResourceResponse?.Resource?.ToString()))
                return JsonConvert.DeserializeObject<TEntity>(response.ResourceResponse.Resource.ToString());

            return null;
        }
    }
}