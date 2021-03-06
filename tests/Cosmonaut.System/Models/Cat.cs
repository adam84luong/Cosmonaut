﻿using Cosmonaut.Attributes;
using Newtonsoft.Json;

namespace Cosmonaut.System.Models
{
    public class Cat : Animal
    {
        [JsonProperty("id")]
        [CosmosPartitionKey]
        public string CatId { get; set; }
    }
}