using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.Extensions.Logging;
using ProcessAudioBlobApp.Models;
using Newtonsoft.Json;

namespace ProcessAudioBlobApp
{

    public static class CreateAudioTranscript
    {

        private static CloudStorageAccount kCloudStorageAccount =
            CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("AzureWebJobsStorage"));
        private static CloudBlobClient kCloudBlobClient = kCloudStorageAccount
                                                          .CreateCloudBlobClient();
        private static HttpClient kHttpClient = new HttpClient();

        private static string GetAudioSaSToken(AudioModel audioModel)
        {

            if (audioModel == null)
                return null;

            var clockSkew = TimeSpan.FromMinutes(15d);
            var accessDuration = TimeSpan.FromMinutes(15d);

            var audioSaS = new SharedAccessBlobPolicy()
            {

                Permissions = SharedAccessBlobPermissions.Read,
                SharedAccessStartTime = DateTime.UtcNow.Subtract(clockSkew),
                SharedAccessExpiryTime = DateTime.UtcNow.Add(accessDuration) + clockSkew

            };

            
            var container = kCloudBlobClient.GetContainerReference(audioModel.ContainerName);
            var blob = container.GetBlockBlobReference(audioModel.AudioName);

            var sasTokenString = blob.GetSharedAccessSignature(audioSaS);
            sasTokenString = string.Concat(audioModel.AudioUri, sasTokenString);
            return sasTokenString;

        }

        private static string GetTranscriptCode(TranscriptModel transcriptModel)
        {

            if (transcriptModel == null)
                return string.Empty;

            var transcriptURLString = transcriptModel.Self;
            var tokenStringsArray = transcriptURLString.Split("/");
            if (tokenStringsArray.Length == 0)
                return string.Empty;

            var transcriptCodeString = tokenStringsArray[tokenStringsArray.Length - 1];
            return transcriptCodeString;

        }

        private static RetryOptions GetRetryOptions()
        {

            int.TryParse(Environment.GetEnvironmentVariable("First_Retry_Interval"),
                                                            out int firstRetryInterval);

            int.TryParse(Environment.GetEnvironmentVariable("Retry_TimeOut"),
                                                            out int retryTimeout);

            int.TryParse(Environment.GetEnvironmentVariable("Max_Number_Of_Attempts"),
                                                            out int maxNumberOfAttempts);

            double.TryParse(Environment.GetEnvironmentVariable("Back_Off_Attempts"),
                                                               out double backOffCoefficient);
            
            var retryOptions = new RetryOptions(TimeSpan.FromSeconds(firstRetryInterval),
                                                maxNumberOfAttempts)
            {
                BackoffCoefficient = backOffCoefficient,
                RetryTimeout = TimeSpan.FromMinutes(retryTimeout)               

            };

            return retryOptions;

        }

        public static async Task<List<TranscriptModel>>
                            ReteieveTranscriptFilesAsync(string transcriptCodeString)
        {

            if (transcriptCodeString.Equals(string.Empty) == true)
                return null;

            var getTranscriptURLString = Environment.GetEnvironmentVariable("GET_TRANSCRIPT_URL");
            getTranscriptURLString = string.Format(getTranscriptURLString, transcriptCodeString);

            var apiKeyString = Environment.GetEnvironmentVariable("Ocp-Apim-Subscription-Key");
            kHttpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", apiKeyString);

            var getTranscriptFilesResponse = await kHttpClient.GetAsync(getTranscriptURLString);
            var transcriptFileModelsString = await getTranscriptFilesResponse.Content.ReadAsStringAsync();
            var transcriptFileModels = JsonConvert.DeserializeObject<TranscriptModels>
                                                   (transcriptFileModelsString);
            var transcriptFilesList = transcriptFileModels.Transcripts;
            return transcriptFilesList;

        }

        [FunctionName("ProcessTranscriptFiles")]
        public static async Task ProcessTranscriptFilesAsync(
                                 [ActivityTrigger] TranscriptModels transcriptModels)
        {

            if (transcriptModels == null)
                return;

            var transcriptFilesList = transcriptModels.Transcripts;
            if (transcriptFilesList.Count <= 1)
                return;

            var processedFilesList = transcriptFilesList.Where((TranscriptModel transcriptModel) =>
            {

                return (transcriptModel.Kind.Equals("Transcription") == true);

            }).ToList();

            var processedModels = new TranscriptModels()
            {

                InstanceId = transcriptModels.InstanceId,
                Transcripts = processedFilesList

            };

            var processedModelsString = JsonConvert.SerializeObject(processedModels);
            var content = new StringContent(processedModelsString);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var processTranscriptURL = Environment.GetEnvironmentVariable("PROCESS_TRANSCRIPT_URL");
            await kHttpClient.PostAsync(processTranscriptURL, content);            

        }

        [FunctionName("GetTranscriptFiles")]
        public static async Task<List<TranscriptModel>> GetTranscriptFilesAsync(
                                                        [ActivityTrigger] string transcriptCodeString)
        {

            if (transcriptCodeString.Equals(string.Empty) == true)
                return null;

            List<TranscriptModel> transcriptFilesList = null;
            do
            {

                transcriptFilesList = await ReteieveTranscriptFilesAsync(transcriptCodeString);

            } while (transcriptFilesList.Count <= 1);
            
            return transcriptFilesList;

        }

        [FunctionName("CreateTranscript")]
        public static async Task<TranscriptModel> CreateTranscriptAsync([ActivityTrigger]
                                                                         AudioModel audioModel)
        {

            var audioSaStokenString = GetAudioSaSToken(audioModel);
            var transcriptReqestModel = new TranscriptRequestModel()
            {

                ContentUrls = new List<string>()
                {

                    audioSaStokenString

                },

                Properties = new TranscriptProperties()
                {

                    DiarizationEnabled = false,
                    WordLevelTimestampsEnabled = false,
                    PunctuationMode = "DictatedAndAutomatic",
                    ProfanityFilterMode = "Masked"

                },

                Locale = "en-US",
                DisplayName = "Transcription using default model for en-U"


            };

            var createTranscriptURL = Environment.GetEnvironmentVariable("CREATE_TRANSCRIPT_URL");
            var apiKeyString = Environment.GetEnvironmentVariable("Ocp-Apim-Subscription-Key");
            kHttpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", apiKeyString);

            var transcriptContentString = JsonConvert.SerializeObject(transcriptReqestModel);
            var content = new StringContent(transcriptContentString);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var transcriptResponse = await kHttpClient.PostAsync(createTranscriptURL, content);
            var createdTranscript = await transcriptResponse.Content.ReadAsStringAsync();
            var transcriptModel = JsonConvert.DeserializeObject<TranscriptModel>(createdTranscript);
            return transcriptModel;

        }

        [FunctionName("ProcessTranscript")]
        public static async Task RunOrchestrator([OrchestrationTrigger]
                                                 IDurableOrchestrationContext context,
                                                 ILogger logger)
        {

            var audioModel = context.GetInput<AudioModel>();
            var transcriptModel = await context.CallActivityAsync<TranscriptModel>
                                                ("CreateTranscript", audioModel);
           

            var transcriptCodeString = GetTranscriptCode(transcriptModel);
            var retryOptions = GetRetryOptions();
            var transcriptsList = await context.CallActivityWithRetryAsync<List<TranscriptModel>>
                                                ("GetTranscriptFiles", retryOptions,
                                                transcriptCodeString);

            var transcriptModels = new TranscriptModels()
            {

                InstanceId = context.InstanceId,
                Transcripts = transcriptsList

            };

            await context.CallActivityWithRetryAsync<string>
                          ("ProcessTranscriptFiles", retryOptions, transcriptModels);

            using (var cts = new CancellationTokenSource())
            {

                var dueTime = context.CurrentUtcDateTime.AddMinutes(3);
                var timerTask = context.CreateTimer(dueTime, cts.Token);
                var processedTask = context.WaitForExternalEvent<bool>("Processed");
                var completedTask = await Task.WhenAny(processedTask, timerTask);                
                var isProcessed = processedTask.Result;

                if (isProcessed == true)
                    logger.LogInformation("Processsed");
                else
                    logger.LogInformation("Not yet");

            }            
        }

        [FunctionName("CreateAudioTranscriptStart")]
        public static async Task CreateAudioTranscriptStart([BlobTrigger("audioblob/{name}")]
                                                            CloudBlockBlob cloudBlockBlob,
                                                            [Blob("audioblob/{name}",
                                                            FileAccess.ReadWrite)]
                                                            byte[] blobContents,
                                                            [DurableClient]
                                                            IDurableOrchestrationClient starter,
                                                            ILogger logger)
        {

            var audioModel = new AudioModel()
            {

                AudioUri = cloudBlockBlob.Uri.AbsoluteUri,
                AudioName = cloudBlockBlob.Name,
                ContainerName = cloudBlockBlob.Container.Name

            };          

            string instanceId = await starter.StartNewAsync("ProcessTranscript", audioModel);
            logger.LogInformation($"Started orchestration with ID = '{instanceId}'.");
        }
    }
}