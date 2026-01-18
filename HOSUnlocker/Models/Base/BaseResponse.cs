using System.Text.Json.Serialization;

namespace HOSUnlock.Models.Base;

public sealed class BaseResponse<T> where T : class
{
    [JsonPropertyName("code")]
    public int Code { get; init; }

    [JsonPropertyName("data")]
    public T? Data { get; init; }

    [JsonPropertyName("message")]
    public string? Message { get; init; }
}
