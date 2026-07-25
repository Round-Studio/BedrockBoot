using System.Text.Json;
using System.Text.Json.Serialization;

namespace BedrockBoot.GravityCone.Entry;

public class CliResponse
{
    [JsonPropertyName("id")] public int Id { get; set; }

    [JsonPropertyName("status")] public string Status { get; set; } = string.Empty;

    [JsonPropertyName("data")] public JsonElement Data { get; set; }

    [JsonPropertyName("error")] public CliError? Error { get; set; }

    public bool IsSuccess => Status == "success";
    public bool IsProgress => Status == "progress";
    public bool IsError => Status == "error";
}

public class CliError
{
    [JsonPropertyName("code")] public string Code { get; set; } = string.Empty;

    [JsonPropertyName("message")] public string Message { get; set; } = string.Empty;
}
