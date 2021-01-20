using System;
using System.Linq;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using ProcessAudioBlobApp.Models;

namespace ProcessAudioBlobApp.Functions
{
    public static class SaveAudioTranscript
    {
        private static HttpClient kHttpClient = new HttpClient();
        private static CosmosClient kCosmosClient = new CosmosClient(
                                                    Environment.GetEnvironmentVariable
                                                    ("AUDIO_BLOB_DB_CONN_STRING"));

        private static async Task<HttpStatusCode> SaveTranscriptAsync(ProcessedModel processedModel)
        {

            if (processedModel == null)
                return HttpStatusCode.BadRequest;

            var databaseId = Environment.GetEnvironmentVariable("AUDIO_BLOB_DB_DATABASE_ID");
            var containerId = Environment.GetEnvironmentVariable("AUDIO_BLOB_DB_CONTAINER_ID");

            var container = kCosmosClient.GetContainer(databaseId, containerId);
            var insertResponse = await container.CreateItemAsync(processedModel,
                                                                 new PartitionKey
                                                                 (processedModel.SourceId));
            return (insertResponse.StatusCode);

        }

        [FunctionName("SaveAudioTranscript")]
        public static async Task SaveAudioTranscriptAsync(
                                 [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)]
                                 HttpRequestMessage requestMessage,
                                 [DurableClient] IDurableOrchestrationClient client, ILogger logger)
        {

            var body = await requestMessage.Content.ReadAsStringAsync();
            if (body == null)
                new BadRequestObjectResult("Bad Request") { StatusCode = 400 };

            var transcriptModels = JsonConvert.DeserializeObject<TranscriptModels>(body);
            if (transcriptModels == null)
                new BadRequestObjectResult("Bad Request") { StatusCode = 400 };

            var transcriptFilesList = transcriptModels.Transcripts;
            var taskList = transcriptFilesList.Select(async (TranscriptModel transcriptModel) =>
            {

                var uri = transcriptModel.Links.ContentUrl;
                var fileContentString = await kHttpClient.GetStringAsync(uri);

                var processedModel = JsonConvert.DeserializeObject<ProcessedModel>(fileContentString);
                processedModel.SourceId = Guid.NewGuid().ToString();

                var responseCode = await SaveTranscriptAsync(processedModel);
                logger.LogInformation(responseCode.ToString());

            }).ToList();

            await Task.WhenAll(taskList);
            await client.RaiseEventAsync(transcriptModels.InstanceId, "Processed", true);
            
        }
    }
}
