using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;

namespace FuncSearch
{
    public static class Function1
    {
        // ADD THIS PART TO YOUR CODE

        // The Azure Cosmos DB endpoint for running this sample.
        private static readonly string EndpointUri = "https://rt-demosql.documents.azure.com:443/";
        // The primary key for the Azure Cosmos account.
        private static readonly string PrimaryKey = "C7o7rWydLY03MGmKRK60cMdoYZn4DrjOwZH2LcVfo7oTa7HzpT3MbcVTJbZmuVMT3c5xiKGJVzLe6cTksf3waQ==";

        // The Cosmos client instance
        private static CosmosClient cosmosClient;

        // The database we will create
        private static Database database;

        // The container we will create.
        private static Container container;

        // The name of the database and container we will create
        private static string databaseId = "rt-demoavnet";
        private static string containerId = "avnetfranchisee";

        [FunctionName("MfrCodeSearch")]
        public static async Task<String> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequestMessage req,
            ILogger log)
        {
            string json;

          

            try
            {
                cosmosClient = new CosmosClient(EndpointUri, PrimaryKey);
                container = cosmosClient.GetContainer(databaseId, containerId);

                

                    // Get request body
                    PostData r = await req.Content.ReadAsAsync<PostData>();

                var serviceClient = new SearchServiceClient(System.Environment.GetEnvironmentVariable("SearchServiceName"), new SearchCredentials(System.Environment.GetEnvironmentVariable("SearchServiceAdminApiKey")));
                var parameters = new SearchParameters()
                {
                    Select = new[] { "rid","avnetmfscode","custname" }
                };
                string indexName = System.Environment.GetEnvironmentVariable("SearchIndexName");
                ISearchIndexClient isc = serviceClient.Indexes.GetClient(indexName);
                if (r.name.Length > 0)
                {
                    ReturnData returnData = new ReturnData();
                    returnData.results = new List<MfrCode>();

                   foreach(var custname in r.name)
                    {
                        //string custname = r.name[0];
                        var azureSearchResponse = isc.Documents.Search<MfrCode>(custname, parameters);
                        if (azureSearchResponse.Results.Count > 0)
                        {

                            foreach (var result in azureSearchResponse.Results)
                          {

                              MfrCode mfrCode = new MfrCode();
                              mfrCode.rid = result.Document.rid;
                              mfrCode.avnetmfscode = result.Document.avnetmfscode;
                              mfrCode.custname = result.Document.custname;
                              mfrCode.avnetmfscodeNotFoundFlag = "N";
                              mfrCode.score = result.Score.ToString();
                              var sqlQueryText = "SELECT * FROM c WHERE c.avnetmfcode = '" + mfrCode.avnetmfscode + "'";

                              Console.WriteLine("Running query: {0}\n", sqlQueryText);

                              QueryDefinition queryDefinition = new QueryDefinition(sqlQueryText);
                              FeedIterator<MfrCodeResults> queryResultSetIterator = container.GetItemQueryIterator<MfrCodeResults>(queryDefinition);
                              bool hasresults = queryResultSetIterator.HasMoreResults;

                              if (hasresults)
                              {
                                  foreach (var item in await queryResultSetIterator.ReadNextAsync())
                                  {
                                      {
                                          mfrCode.exclusionFlag = item.exclusionflag;
                                      }
                                  }
                                    //returnData.results.Add(result.Document);
                                    returnData.results.Add(mfrCode);
                              }

                          };
                           
                        }
                        else
                        {
                            MfrCode mfrCode = new MfrCode();
                            mfrCode.custname = custname;
                            mfrCode.avnetmfscodeNotFoundFlag = "Y";
                            returnData.results.Add(mfrCode);
                        }
                    };
                    json = JsonConvert.SerializeObject(returnData.results); ;

                    return await Task.FromResult(json).ConfigureAwait(false);
                }
  
            }
            catch (Exception ex)
            {
                log.LogInformation(ex.Message);
            }
            json = string.Format("NO Search results returned");
            return await Task.FromResult(json).ConfigureAwait(false);
        }
    }
}
