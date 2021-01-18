using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace ProcessAudioBlobApp.Models
{
    public class TranscriptRequestModel
    {

        [JsonProperty("contentUrls")]
        public List<string> ContentUrls { get; set; }

        [JsonProperty("properties")]
        public TranscriptProperties Properties { get; set; }

        [JsonProperty("locale")]
        public string Locale { get; set; }

        [JsonProperty("displayName")]
        public string DisplayName { get; set; }

    }

    public class TranscriptProperties
    {

        [JsonProperty("diarizationEnabled")]
        public bool DiarizationEnabled { get; set; }

        [JsonProperty("wordLevelTimestampsEnabled")]
        public bool WordLevelTimestampsEnabled { get; set; }

        [JsonProperty("punctuationMode")]
        public string PunctuationMode { get; set; }

        [JsonProperty("profanityFilterMode")]
        public string ProfanityFilterMode { get; set; }


    }
}
