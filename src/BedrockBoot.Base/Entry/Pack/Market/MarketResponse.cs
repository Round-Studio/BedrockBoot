using System.Text.Json.Serialization;

namespace BedrockBoot.Base.Entry.Pack.Market;

public class MarketResponse
{
    [JsonPropertyName("plugins")]
    public List<PluginInfo> Plugins { get; set; } = new();

    public class PluginInfo
    {
        [JsonPropertyName("username")]
        public string Username { get; set; } = string.Empty;

        [JsonPropertyName("path")]
        public string Path { get; set; } = string.Empty;

        [JsonPropertyName("iconUrl")]
        public string IconUrl { get; set; } = string.Empty;

        [JsonPropertyName("repositoryUrl")]
        public string RepositoryUrl { get; set; } = string.Empty;

        [JsonPropertyName("repositoryOwner")]
        public string RepositoryOwner { get; set; } = string.Empty;

        [JsonPropertyName("repositoryName")]
        public string RepositoryName { get; set; } = string.Empty;

        [JsonPropertyName("pluginName")]
        public string PluginName { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("icon")]
        public string Icon { get; set; } = string.Empty;

        [JsonPropertyName("labels")]
        public List<string> Labels { get; set; }
    }
}