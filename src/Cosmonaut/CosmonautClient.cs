﻿using Cosmonaut.Extensions;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Cosmonaut.Diagnostics;
using Cosmonaut.Response;
using Newtonsoft.Json;

namespace Cosmonaut
{
    public class CosmonautClient : ICosmonautClient
    {
        public CosmonautClient(IDocumentClient documentClient)
        {
            DocumentClient = documentClient;
        }

        public CosmonautClient(Func<IDocumentClient> documentClientFunc)
        {
            DocumentClient = documentClientFunc();
        }

        public CosmonautClient(
            Uri endpoint, 
            string authKeyOrResourceToken, 
            ConnectionPolicy connectionPolicy = null,
            ConsistencyLevel? desiredConsistencyLevel = null)
        {
            DocumentClient = DocumentClientFactory.CreateDocumentClient(endpoint, authKeyOrResourceToken, connectionPolicy, desiredConsistencyLevel);
        }

        public CosmonautClient(
            Uri endpoint,
            string authKeyOrResourceToken,
            JsonSerializerSettings jsonSerializerSettings,
            ConnectionPolicy connectionPolicy = null,
            ConsistencyLevel? desiredConsistencyLevel = null)
        {
            DocumentClient = DocumentClientFactory.CreateDocumentClient(endpoint, authKeyOrResourceToken, jsonSerializerSettings, connectionPolicy, desiredConsistencyLevel);
        }

        public CosmonautClient(
            string endpoint,
            string authKeyOrResourceToken,
            ConnectionPolicy connectionPolicy = null,
            ConsistencyLevel? desiredConsistencyLevel = null) : this(new Uri(endpoint), authKeyOrResourceToken, connectionPolicy, desiredConsistencyLevel)
        {
        }

        public async Task<Database> GetDatabaseAsync(string databaseId, RequestOptions requestOptions = null)
        {
            var databaseUri = UriFactory.CreateDatabaseUri(databaseId);
            return await this.InvokeCosmosOperationAsync(() => DocumentClient.ReadDatabaseAsync(databaseUri, requestOptions), databaseId)
                .ExecuteCosmosQuery();
        }

        public async Task<IEnumerable<Database>> QueryDatabasesAsync(Expression<Func<Database, bool>> predicate = null, 
            FeedOptions feedOptions = null, CancellationToken cancellationToken = default)
        {
            if (predicate == null) predicate = x => true;
            return await DocumentClient.CreateDatabaseQuery(feedOptions).Where(predicate).ToListAsync(cancellationToken);
        }
        
        public async Task<Document> GetDocumentAsync(string databaseId, string collectionId, string documentId, RequestOptions requestOptions = null)
        {
            var documentUri = UriFactory.CreateDocumentUri(databaseId, collectionId, documentId);
            return await this.InvokeCosmosOperationAsync(() => DocumentClient.ReadDocumentAsync(documentUri, requestOptions), documentId)
                .ExecuteCosmosQuery();
        }
        
        public async Task<IEnumerable<DocumentCollection>> QueryCollectionsAsync(string databaseId, 
            Expression<Func<DocumentCollection, bool>> predicate = null, FeedOptions feedOptions = null, CancellationToken cancellationToken = default)
        {
            if (predicate == null) predicate = x => true;
            var databaseUri = UriFactory.CreateDatabaseUri(databaseId);
            return await DocumentClient.CreateDocumentCollectionQuery(databaseUri, feedOptions).Where(predicate).ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<T>> QueryDocumentsAsync<T>(string databaseId, string collectionId, Expression<Func<T, bool>> predicate = null, 
            FeedOptions feedOptions = null, CancellationToken cancellationToken = default)
        {
            if (predicate == null) predicate = x => true;
            var collectionUri = UriFactory.CreateDocumentCollectionUri(databaseId, collectionId);
            return await DocumentClient.CreateDocumentQuery<T>(collectionUri, feedOptions).Where(predicate).ToGenericListAsync(cancellationToken);
        }

        public async Task<IEnumerable<T>> QueryDocumentsAsync<T>(string databaseId, string collectionId, string sql, object parameters = null,
            FeedOptions feedOptions = null, CancellationToken cancellationToken = default)
        {
            var collectionUri = UriFactory.CreateDocumentCollectionUri(databaseId, collectionId);
            var sqlParameters = parameters.ConvertToSqlParameterCollection();
            var sqlQuerySpec = sqlParameters != null && sqlParameters.Any() ? new SqlQuerySpec(sql, sqlParameters) : new SqlQuerySpec(sql);
            return await DocumentClient.CreateDocumentQuery<T>(collectionUri, sqlQuerySpec, feedOptions).ToGenericListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Document>> QueryDocumentsAsync(string databaseId, string collectionId, 
            Expression<Func<Document, bool>> predicate = null, FeedOptions feedOptions = null, CancellationToken cancellationToken = default)
        {
            if (predicate == null) predicate = x => true;
            var collectionUri = UriFactory.CreateDocumentCollectionUri(databaseId, collectionId);
            return await DocumentClient.CreateDocumentQuery(collectionUri, feedOptions).Where(predicate).ToGenericListAsync(cancellationToken);
        }

        public async Task<DocumentCollection> GetCollectionAsync(string databaseId, string collectionId, RequestOptions requestOptions = null)
        {
            var collectionUri = UriFactory.CreateDocumentCollectionUri(databaseId, collectionId);
            return await this.InvokeCosmosOperationAsync(() => DocumentClient.ReadDocumentCollectionAsync(collectionUri, requestOptions), collectionId)
                .ExecuteCosmosQuery();
        }
        
        public async Task<Offer> GetOfferForCollectionAsync(string databaseId, string collectionId, FeedOptions feedOptions = null, 
            CancellationToken cancellationToken = default)
        {
            var collection = await GetCollectionAsync(databaseId, collectionId);
            return await DocumentClient.CreateOfferQuery(feedOptions).SingleOrDefaultAsync(x=> x.ResourceLink == collection.SelfLink, cancellationToken);
        }

        public async Task<OfferV2> GetOfferV2ForCollectionAsync(string databaseId, string collectionId, FeedOptions feedOptions = null, 
            CancellationToken cancellationToken = default)
        {
            return (OfferV2) await GetOfferForCollectionAsync(databaseId, collectionId, feedOptions, cancellationToken);
        }

        public async Task<IEnumerable<Offer>> QueryOffersAsync(Expression<Func<Offer, bool>> predicate = null, FeedOptions feedOptions = null, CancellationToken cancellationToken = default)
        {
            if (predicate == null) predicate = x => true;
            return await DocumentClient.CreateOfferQuery(feedOptions).Where(predicate).ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<OfferV2>> QueryOffersV2Async(Expression<Func<Offer, bool>> predicate = null, FeedOptions feedOptions = null, CancellationToken cancellationToken = default)
        {
            if (predicate == null) predicate = x => true;
            var offers = await DocumentClient.CreateOfferQuery(feedOptions).Where(predicate).ToListAsync(cancellationToken);
            return offers.Cast<OfferV2>();
        }

        public async Task<IEnumerable<StoredProcedure>> QueryStoredProceduresAsync(string databaseId, string collectionId, Expression<Func<StoredProcedure, bool>> predicate = null, 
            FeedOptions feedOptions = null, CancellationToken cancellationToken = default)
        {
            if (predicate == null) predicate = x => true;
            var collectionUri = UriFactory.CreateDocumentCollectionUri(databaseId, collectionId);
            return await DocumentClient.CreateStoredProcedureQuery(collectionUri, feedOptions).Where(predicate).ToListAsync(cancellationToken);
        }

        public async Task<StoredProcedure> GetStoredProcedureAsync(string databaseId, string collectionId, string storedProcedureId, RequestOptions requestOptions = null)
        {
            var storedProcedureUri = UriFactory.CreateStoredProcedureUri(databaseId, collectionId, storedProcedureId);
            return await this.InvokeCosmosOperationAsync(() => DocumentClient.ReadStoredProcedureAsync(storedProcedureUri, requestOptions), storedProcedureId)
                .ExecuteCosmosQuery();
        }

        public IQueryable<T> Query<T>(string databaseId, string collectionId, FeedOptions feedOptions = null)
        {
            var collectionUri = UriFactory.CreateDocumentCollectionUri(databaseId, collectionId);
            var queryable = DocumentClient.CreateDocumentQuery<T>(collectionUri, feedOptions);
            return queryable;
        }

        public IQueryable<T> Query<T>(string databaseId, string collectionId, string sql, object parameters = null, FeedOptions feedOptions = null)
        {
            var collectionUri = UriFactory.CreateDocumentCollectionUri(databaseId, collectionId);
            var sqlParameters = parameters.ConvertToSqlParameterCollection();
            return GetSqlBasedQueryableForType<T>(collectionUri, sql, sqlParameters, feedOptions);
        }

        public async Task<ResourceResponse<DocumentCollection>> CreateCollectionAsync(string databaseId, DocumentCollection collection,
            RequestOptions requestOptions = null)
        {
            var databaseUri = UriFactory.CreateDatabaseUri(databaseId);
            return await this.InvokeCosmosOperationAsync(() => DocumentClient.CreateDocumentCollectionAsync(databaseUri, collection, requestOptions), collection.ToString())
                .ExecuteCosmosCommand();
        }

        public async Task<ResourceResponse<Database>> CreateDatabaseAsync(Database database, RequestOptions requestOptions = null)
        {
            return await this.InvokeCosmosOperationAsync(() => DocumentClient.CreateDatabaseAsync(database, requestOptions), database.ToString())
                .ExecuteCosmosCommand();
        }

        public async Task<ResourceResponse<Document>> CreateDocumentAsync(string databaseId,
            string collectionId, Document obj,
            RequestOptions requestOptions = null)
        {
            var collectionUri = UriFactory.CreateDocumentCollectionUri(databaseId, collectionId);
            return await this.InvokeCosmosOperationAsync(() =>
                    DocumentClient.CreateDocumentAsync(collectionUri, obj, requestOptions), obj.GetDocumentId())
                .ExecuteCosmosCommand();
        }

        public async Task<CosmosResponse<T>> CreateDocumentAsync<T>(string databaseId, string collectionId, T obj, RequestOptions requestOptions = null) where T : class
        {
            var safeDocument = obj.ConvertObjectToDocument();
            var collectionUri = UriFactory.CreateDocumentCollectionUri(databaseId, collectionId);
            return await this.InvokeCosmosOperationAsync(() =>
                    DocumentClient.CreateDocumentAsync(collectionUri, safeDocument, requestOptions), obj.GetDocumentId())
                .ExecuteCosmosCommand(obj);
        }

        public async Task<ResourceResponse<Document>> DeleteDocumentAsync(string databaseId, string collectionId, string documentId,
            RequestOptions requestOptions = null)
        {
            var documentUri = UriFactory.CreateDocumentUri(databaseId, collectionId, documentId);
            return await this.InvokeCosmosOperationAsync(() => DocumentClient.DeleteDocumentAsync(documentUri, requestOptions), documentId)
                .ExecuteCosmosCommand();
        }

        public async Task<ResourceResponse<Document>> UpdateDocumentAsync(string databaseId, string collectionId, Document document,
            RequestOptions requestOptions = null)
        {
            var documentUri = UriFactory.CreateDocumentUri(databaseId, collectionId, document.Id);
            return await this.InvokeCosmosOperationAsync(() =>
                    DocumentClient.ReplaceDocumentAsync(documentUri, document, requestOptions), document.GetDocumentId())
                .ExecuteCosmosCommand();
        }

        public async Task<CosmosResponse<T>> UpdateDocumentAsync<T>(string databaseId, string collectionId, T document, RequestOptions requestOptions = null) where T : class
        {
            var safeDocument = document.ConvertObjectToDocument();
            var documentUri = UriFactory.CreateDocumentUri(databaseId, collectionId, safeDocument.Id);
            return await this.InvokeCosmosOperationAsync(() =>
                    DocumentClient.ReplaceDocumentAsync(documentUri, safeDocument, requestOptions), document.GetDocumentId())
                .ExecuteCosmosCommand(document);
        }

        public async Task<ResourceResponse<Document>> UpsertDocumentAsync(string databaseId, string collectionId, Document document,
            RequestOptions requestOptions = null)
        {
            var collectionUri = UriFactory.CreateDocumentCollectionUri(databaseId, collectionId);
            return await this.InvokeCosmosOperationAsync(() =>
                    DocumentClient.UpsertDocumentAsync(collectionUri, document, requestOptions), document.Id)
                .ExecuteCosmosCommand();
        }

        public async Task<CosmosResponse<T>> UpsertDocumentAsync<T>(string databaseId, string collectionId, T document, RequestOptions requestOptions = null) where T : class
        {
            var safeDocument = document.ConvertObjectToDocument();
            var collectionUri = UriFactory.CreateDocumentCollectionUri(databaseId, collectionId);
            return await this.InvokeCosmosOperationAsync(() =>
                    DocumentClient.UpsertDocumentAsync(collectionUri, safeDocument, requestOptions), document.GetDocumentId())
                .ExecuteCosmosCommand(document);
        }

        public async Task<ResourceResponse<Database>> DeleteDatabaseAsync(string databasedId, RequestOptions requestOptions = null)
        {
            var databaseUri = UriFactory.CreateDatabaseUri(databasedId);
            return await this.InvokeCosmosOperationAsync(() => DocumentClient.DeleteDatabaseAsync(databaseUri, requestOptions), databasedId)
                .ExecuteCosmosCommand();
        }

        public async Task<ResourceResponse<DocumentCollection>> DeleteCollectionAsync(string databasedId, string collectionId, RequestOptions requestOptions = null)
        {
            var collectionUri = UriFactory.CreateDocumentCollectionUri(databasedId, collectionId);
            return await this.InvokeCosmosOperationAsync(() => DocumentClient.DeleteDocumentCollectionAsync(collectionUri, requestOptions), collectionId)
                .ExecuteCosmosCommand();
        }

        public async Task<ResourceResponse<DocumentCollection>> UpdateCollectionAsync(string databasedId, string collectionId, DocumentCollection documentCollection,
            RequestOptions requestOptions = null)
        {
            var collectionUri = UriFactory.CreateDocumentCollectionUri(databasedId, collectionId);
            return await this.InvokeCosmosOperationAsync(() => DocumentClient.ReplaceDocumentCollectionAsync(collectionUri, documentCollection, requestOptions), collectionId)
                .ExecuteCosmosCommand();
        }

        public async Task<StoredProcedureResponse<TValue>> ExecuteStoredProcedureAsync<TValue>(string databasedId, string collectionId, string storedProcedureId,
            params object[] procedureParams)
        {
            var storedProcedureUri = UriFactory.CreateStoredProcedureUri(databasedId, collectionId, storedProcedureId);
            return await this.InvokeCosmosOperationAsync(
                () => DocumentClient.ExecuteStoredProcedureAsync<TValue>(storedProcedureUri, procedureParams), storedProcedureId);
        }

        public async Task<StoredProcedureResponse<TValue>> ExecuteStoredProcedureAsync<TValue>(string databasedId, string collectionId, string storedProcedureId,
            RequestOptions requestOptions, params object[] procedureParams)
        {
            var storedProcedureUri = UriFactory.CreateStoredProcedureUri(databasedId, collectionId, storedProcedureId);
            return await this.InvokeCosmosOperationAsync(
                () => DocumentClient.ExecuteStoredProcedureAsync<TValue>(storedProcedureUri, requestOptions, procedureParams), storedProcedureId);
        }

        private IQueryable<T> GetSqlBasedQueryableForType<T>(Uri collectionUri, string sql, 
            SqlParameterCollection parameters, FeedOptions feedOptions)
        {
            var sqlQuerySpec = parameters != null && parameters.Any() ? new SqlQuerySpec(sql, parameters) : new SqlQuerySpec(sql);
            var queryable = DocumentClient.CreateDocumentQuery<T>(collectionUri, sqlQuerySpec, feedOptions);
            return queryable;
        }

        public IDocumentClient DocumentClient { get; }
    }
}