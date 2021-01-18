using System;
using Microsoft.WindowsAzure.Storage.Blob;

namespace ProcessAudioBlobApp.Models
{
    public class AudioModel
    {
        public string AudioUri              { get; set; }
        public string AudioName             { get; set; }
        public string ContainerName         { get; set; }
    }
}
