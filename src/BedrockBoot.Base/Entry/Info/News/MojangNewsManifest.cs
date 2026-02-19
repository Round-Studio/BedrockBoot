using System.Text.Json.Serialization;

namespace BedrockBoot.Base.Entry.Info.News;

public class MojangNewsManifest
{
    [JsonPropertyName("version")]
    public int Version { get; set; }

    [JsonPropertyName("entries")]
    public List<PatchNoteEntry> Entries { get; set; }

    public class PatchNoteEntry
    {
        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("version")]
        public string Version { get; set; }

        [JsonPropertyName("patchNoteType")]
        public string PatchNoteType { get; set; }

        [JsonPropertyName("date")]
        public string Date { get; set; }

        [JsonPropertyName("image")]
        public PatchNoteImage Image { get; set; }

        [JsonPropertyName("contentPath")]
        public string ContentPath { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("shortText")]
        public string ShortText { get; set; }
    }

    public class PatchNoteImage
    {
        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }
    }
}
