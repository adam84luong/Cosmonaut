﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Cosmonaut.Extensions;
using Microsoft.Azure.Documents.Client;
using Microsoft.Extensions.DependencyInjection;

namespace Cosmonaut.Console
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var connectionPolicy = new ConnectionPolicy
            {
                ConnectionProtocol = Protocol.Tcp,
                ConnectionMode = ConnectionMode.Direct
            };

            var cosmosSettings = new CosmosStoreSettings("localtest", 
                "https://localhost:8081", 
                "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw=="
                , connectionPolicy
                , defaultCollectionThroughput: 5000);

            var cosmonautClient = new CosmonautClient("https://localhost:8081",
                "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==");

            var serviceCollection = new ServiceCollection();

            serviceCollection.AddCosmosStore<Book>("localtest", "https://localhost:8081",
                "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==",
                settings =>
            {
                settings.ConnectionPolicy = connectionPolicy;
                settings.DefaultCollectionThroughput = 5000;
            }, "pewpew");

            serviceCollection.AddCosmosStore<Car>(cosmosSettings);

            var provider = serviceCollection.BuildServiceProvider();

            var booksStore = provider.GetService<ICosmosStore<Book>>();
            var carStore = provider.GetService<ICosmosStore<Car>>();


            System.Console.WriteLine($"Started");

            var database = await cosmonautClient.GetDatabaseAsync("localtest");
            System.Console.WriteLine($"Retrieved database with id {database.Id}");

            var collection = await cosmonautClient.GetCollectionAsync("localtest", "shared");
            System.Console.WriteLine($"Retrieved collection with id {collection.Id}");

            var offer = await cosmonautClient.GetOfferForCollectionAsync("localtest", "shared");
            System.Console.WriteLine($"Retrieved offer with id {offer.Id}");

            var offerV2 = await cosmonautClient.GetOfferV2ForCollectionAsync("localtest", "shared");
            System.Console.WriteLine($"Retrieved offerV2 with id {offerV2.Id}");

            var databases = await cosmonautClient.QueryDatabasesAsync();
            System.Console.WriteLine($"Retrieved all {databases.Count()} databased in the account.");

            var booksRemoved = await booksStore.RemoveAsync(x => true);
            System.Console.WriteLine($"Removed {booksRemoved.SuccessfulEntities.Count} books from the database.");

            var carsRemoved = await carStore.RemoveAsync(x => true);
            System.Console.WriteLine($"Removed {carsRemoved.SuccessfulEntities.Count} cars from the database.");


            var books = new List<Book>();
            for (int i = 0; i < 50; i++)
            {
                books.Add(new Book
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Test " + i,
                    AnotherRandomProp = "Random " + i
                });
            }

            var cars = new List<Car>();
            for (int i = 0; i < 50; i++)
            {
                cars.Add(new Car
                {
                    Id = Guid.NewGuid().ToString(),
                    ModelName = "Car " + i,
                });
            }

            var watch = new Stopwatch();
            watch.Start();
            var addedCars = await carStore.AddRangeAsync(cars);

            var addedBooks = await booksStore.AddRangeAsync(books);
            
            System.Console.WriteLine($"Added {addedCars.SuccessfulEntities.Count + addedBooks.SuccessfulEntities.Count} documents in {watch.ElapsedMilliseconds}ms");
            watch.Restart();
            //await Task.Delay(3000);

            var aCarId = addedCars.SuccessfulEntities.First().Entity.Id;
            var firstAddedCar = await carStore.QueryMultipleAsync("select * from c where c.id = @id", new { id= aCarId });
            var allTheCars = await carStore.QueryMultipleAsync<Car>("select * from c");

            var addedRetrieved = await booksStore.Query().ToListAsync();

            System.Console.WriteLine($"Retrieved {addedRetrieved.Count} documents in {watch.ElapsedMilliseconds}ms");
            watch.Restart();
            foreach (var addedre in addedRetrieved)
            {
                addedre.AnotherRandomProp += " Nick";
            }

            var updated = await booksStore.UpsertRangeAsync(addedRetrieved);
            System.Console.WriteLine($"Updated {updated.SuccessfulEntities.Count} documents in {watch.ElapsedMilliseconds}ms");
            watch.Restart();

            var removed = await booksStore.RemoveRangeAsync(addedRetrieved);
            System.Console.WriteLine($"Removed {removed.SuccessfulEntities.Count} documents in {watch.ElapsedMilliseconds}ms");
            watch.Reset();
            watch.Stop();

            System.Console.ReadKey();
        }
    }
}
