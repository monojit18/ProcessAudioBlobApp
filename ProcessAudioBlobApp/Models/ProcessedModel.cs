using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace ProcessAudioBlobApp.Models
{
    public class ProcessedModel
    {

        [JsonProperty("id")]
        public string SourceId { get; set; }

        [JsonProperty("source")]
        public string Source { get; set; }
    }
}
