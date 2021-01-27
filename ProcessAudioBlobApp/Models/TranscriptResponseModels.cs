using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace ProcessAudioBlobApp.Models
{
    public class TranscriptResponseModels
    {

        public string InstanceId { get; set; }

        [JsonProperty("values")]
        public List<TranscriptModel> Transcripts { get; set; }        

    }

    public class TranscriptModel
    {

        [JsonProperty("self")]
        public string Self { get; set; }

        [JsonProperty("kind")]
        public string Kind { get; set; }

        [JsonProperty("links")]
        public TranscriptLinkModel Links { get; set; }

    }

    public class TranscriptLinkModel
    {

        [JsonProperty("contentUrl")]
        public string ContentUrl { get; set; }       

    }

    public class TranscriptCodeModel
    {

        public string InstanceId { get; set; }
        public string TranscriptCode { get; set; }

    }
}
