using System.Text.Json.Serialization;

namespace HOSUnlock.Models.Base
{
    public class BaseResponse<T> where T : class
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("data")]
        public T? Data { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        public BaseResponse()
        {
        }

        public BaseResponse(int code, T? data, string? message = null)
        {
            Code = code;
            Data = data;
            Message = message;
        }
    }
}
