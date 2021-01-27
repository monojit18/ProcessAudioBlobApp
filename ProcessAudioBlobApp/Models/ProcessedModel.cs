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

        [JsonProperty("timestamp")]
        public string Timestamp { get; set; }

        [JsonProperty("durationInTicks")]
        public double DurationInTicks { get; set; }

        [JsonProperty("duration")]
        public string Duration { get; set; }

        [JsonProperty("combinedRecognizedPhrases")]
        public List<CombinedRecognizedPhrasesModel> CombinedRecognizedPhrases { get; set; }

        [JsonProperty("recognizedPhrases")]
        public List<RecognozedPhraseModel> RecognizedPhrases { get; set; }
    }

    public class CombinedRecognizedPhrasesModel
    {

        [JsonProperty("channel")]
        public int Channel { get; set; }

        [JsonProperty("lexical")]
        public string Lexical { get; set; }

        [JsonProperty("itn")]
        public string Itn { get; set; }

        [JsonProperty("maskedITN")]
        public string MaskedITN { get; set; }

        [JsonProperty("display")]
        public string Display { get; set; }

    }

    public class RecognozedPhraseModel
    {       

        [JsonProperty("recognitionStatus")]
        public string RecognitionStatus { get; set; }

        [JsonProperty("channel")]
        public int Channel { get; set; }

        [JsonProperty("offset")]
        public string Offset { get; set; }

        [JsonProperty("duration")]
        public string Duration { get; set; }

        [JsonProperty("offsetInTicks")]
        public double OffsetInTicks { get; set; }

        [JsonProperty("durationInTicks")]
        public double DurationInTicks { get; set; }

        [JsonProperty("nBest")]
        public List<BestApproximationModel> BestApproximations { get; set; }

    }

    public class BestApproximationModel
    {

        [JsonProperty("confidence")]
        public double Confidence { get; set; }

        [JsonProperty("lexical")]
        public string Lexical { get; set; }

        [JsonProperty("itn")]
        public string Itn { get; set; }

        [JsonProperty("maskedITN")]
        public string MaskedITN { get; set; }

        [JsonProperty("display")]
        public string Display { get; set; }

    }
}
