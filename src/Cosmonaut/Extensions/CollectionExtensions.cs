﻿using System;
using System.Linq;
using System.Reflection;
using Cosmonaut.Attributes;
using Cosmonaut.Exceptions;
using Humanizer;

namespace Cosmonaut.Extensions
{
    internal static class CollectionExtensions
    {
        internal static string GetCollectionName(this Type entityType)
        {
            var collectionNameAttribute = entityType.GetCustomAttribute<CosmosCollectionAttribute>();

            var collectionName = collectionNameAttribute?.Name;

            return !string.IsNullOrEmpty(collectionName) ? collectionName : entityType.Name.ToLower().Pluralize();
        }

        internal static string GetSharedCollectionEntityName(this Type entityType)
        {
            var collectionNameAttribute = entityType.GetCustomAttribute<SharedCosmosCollectionAttribute>();

            var collectionName = collectionNameAttribute?.EntityName;

            return !string.IsNullOrEmpty(collectionName) ? collectionName : entityType.Name.ToLower().Pluralize();
        }

        internal static string GetSharedCollectionName(this Type entityType)
        {
            var collectionNameAttribute = entityType.GetCustomAttribute<SharedCosmosCollectionAttribute>();

            var collectionName = collectionNameAttribute?.SharedCollectionName;

            if (string.IsNullOrEmpty(collectionName))
                throw new SharedCollectionNameMissingException(entityType);

            return collectionName;
        }

        internal static bool UsesSharedCollection(this Type entityType)
        {
            var hasSharedCosmosCollectionAttribute = entityType.GetCustomAttribute<SharedCosmosCollectionAttribute>() != null;
            var implementsSharedCosmosEntity = entityType.GetInterfaces().Contains(typeof(ISharedCosmosEntity));

            if (hasSharedCosmosCollectionAttribute && !implementsSharedCosmosEntity)
                throw new SharedEntityDoesNotImplementExcepction(entityType);

            if (!hasSharedCosmosCollectionAttribute && implementsSharedCosmosEntity)
                throw new SharedEntityDoesNotHaveAttribute(entityType);

            return hasSharedCosmosCollectionAttribute;
        }

        internal static int GetCollectionThroughputForEntity(this Type entityType,
            int collectionThroughput)
        {
            if (collectionThroughput < CosmosConstants.MinimumCosmosThroughput) throw new IllegalCosmosThroughputException();
            return collectionThroughput;
        }
    }
}