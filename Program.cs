using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace CosmosDBDemo;

    class Program
    {
    static async Task Main(string[] args)
    {
        var configuration = LoadConfiguration();
        var cosmosDBSettings = configuration.GetSection("CosmosDBSettings");

        if (args.Length > 0 && args[0] == "q")
        {
            await QueryCosmosDB(cosmosDBSettings);
        }
        else if (args.Length > 0 && args[0] == "i")
        {
            if (args.Length > 1 && int.TryParse(args[1], out int numberOfCalls))
            {
                for (int i = 0; i < numberOfCalls; i++)
                {
                    await PatchIncrementPropertyInDocument(cosmosDBSettings);
                }
                Console.WriteLine($"Patch called {numberOfCalls} times.");
               // await QueryCosmosDB(cosmosDBSettings);
                 await DisplayAttributes(cosmosDBSettings);

                 // Retrieve the document
                var documentId = DocumentConstants.DocumentId;
                var (areAttributesEqual, comparisonResult) = CompareAttributes(cosmosDBSettings, documentId);

                if (areAttributesEqual)
                {
                    Console.WriteLine("Attributes match: " + comparisonResult);
                }
                else
                {
                    if (comparisonResult.Contains("not found"))
                    {
                        Console.WriteLine("Error: Document not found.");
                    }
                    else
                    {
                        Console.WriteLine("Attributes DO NOT match: " + comparisonResult);
                    }
                }                

            }
            else
            {
                Console.WriteLine("Invalid number of calls. Usage: dotnet run increment <number>");
            }
        }
        else
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("To run the query: dotnet run q");
            Console.WriteLine("To increment 'lifr_dfm': dotnet run increment <number>");
        }
    }


        static IConfiguration LoadConfiguration()
        {
            return new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();
        }

        static async Task QueryCosmosDB(IConfigurationSection cosmosDBSettings)
        {
            var connectionString = cosmosDBSettings["ConnectionString"];
            var databaseId = cosmosDBSettings["DatabaseId"];
            var containerId = cosmosDBSettings["ContainerId"];

            using (var client = new CosmosClient(connectionString))
            {
                var database = client.GetDatabase(databaseId);
                var container = database.GetContainer(containerId);

                var response = await container.ReadItemAsync<dynamic>(DocumentConstants.DocumentId, new PartitionKey(DocumentConstants.DocumentId));

                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var document = response.Resource;
                    Console.WriteLine(document);
                }
                else
                {
                    Console.WriteLine($"Document not found with ID: {DocumentConstants.DocumentId}");
                }
            }
        }

        static async Task PatchIncrementPropertyInDocument(IConfigurationSection cosmosDBSettings)
        {
            var connectionString = cosmosDBSettings["ConnectionString"];
            var databaseId = cosmosDBSettings["DatabaseId"];
            var containerId = cosmosDBSettings["ContainerId"];

            using (var client = new CosmosClient(connectionString))
            {
                var database = client.GetDatabase(databaseId);
                var container = database.GetContainer(containerId);

                // Define the patch operation
                var patchOperations = new List<PatchOperation>
                {
                   // PatchOperation.Increment("/trackingSummary/lifr_dfm", 1)
                    PatchOperation.Increment("/dispatch/success", 1)
                };

                // Execute the patch operation with the correct partition key path
                var patchRequestOptions = new PatchItemRequestOptions { EnableContentResponseOnWrite = false };
                var partitionKeyValue = DocumentConstants.DocumentId; // Use the document ID as the partition key value
                var response = await container.PatchItemAsync<JObject>(DocumentConstants.DocumentId, new PartitionKey(partitionKeyValue), patchOperations, patchRequestOptions);

                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                  //  Console.WriteLine("'lifr_dfm' incremented successfully.");
                }
                else
                {
                    Console.WriteLine("Failed to increment 'lifr_dfm'.");
                }
            }
        }

        // Add this new method to display attributes
        static async Task DisplayAttributes(IConfigurationSection cosmosDBSettings)
        {
            var connectionString = cosmosDBSettings["ConnectionString"];
            var databaseId = cosmosDBSettings["DatabaseId"];
            var containerId = cosmosDBSettings["ContainerId"];

            using (var client = new CosmosClient(connectionString))
            {
                var database = client.GetDatabase(databaseId);
                var container = database.GetContainer(containerId);

                var response = await container.ReadItemAsync<JObject>(DocumentConstants.DocumentId, new PartitionKey(DocumentConstants.DocumentId));

                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var document = response.Resource;
                    var documentFlushItemCount = document["documentFlushItemCount"];
                    var dispatchSuccess = document["dispatch"]["success"];

                    Console.WriteLine($"documentFlushItemCount: {documentFlushItemCount}");
                    Console.WriteLine($"dispatch/success: {dispatchSuccess}");
                }
                else
                {
                    Console.WriteLine($"Document not found with ID: {DocumentConstants.DocumentId}");
                }
            }
        }

        static (bool, string) CompareAttributes(IConfigurationSection cosmosDBSettings, string documentId)
        {
            var connectionString = cosmosDBSettings["ConnectionString"];
            var databaseId = cosmosDBSettings["DatabaseId"];
            var containerId = cosmosDBSettings["ContainerId"];

            using (var client = new CosmosClient(connectionString))
            {
                var database = client.GetDatabase(databaseId);
                var container = database.GetContainer(containerId);

                var response = container.ReadItemAsync<JObject>(documentId, new PartitionKey(documentId)).Result;

                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var document = response.Resource;
                    var documentFlushItemCount = document["documentFlushItemCount"]?.Value<int>() ?? 0;
                    var dispatchSuccess = document["dispatch"]["success"]?.Value<int>() ?? 0;

                    bool areAttributesEqual = documentFlushItemCount == dispatchSuccess;
                    
                    return (areAttributesEqual, "Attributes compared successfully.");
                }
                else
                {
                    return (false, $"Error: Document not found with ID: {documentId}");
                }
            }
        }

    }
    