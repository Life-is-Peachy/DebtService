using Newtonsoft.Json;

namespace Rosreestr.Sessions.Classes
{
    public class Solution
    {
        [JsonProperty("text")]
        public object Text { get; set; }
    }

    public class JSonResult
    {
        [JsonProperty("errorId")]
        public int ErrorId { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("solution")]
        public Solution Solution { get; set; }
    }

    public class JTask
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("body")]
        public string Body { get; set; }

        [JsonProperty("Case")]
        public bool Case { get; set; }

        [JsonProperty("numeric")]
        public int Numeric { get; set; }

        [JsonProperty("math")]
        public bool Math { get; set; }
    }

    public class JSonTask
    {
        [JsonProperty("clientKey")]
        public string ClientKey { get; set; }

        [JsonProperty("task")]
        public JTask Task { get; set; }
    }

    public class JSonAnswer
    {
        [JsonProperty("errorId")]
        public int ErrorId { get; set; }

        [JsonProperty("errorCode")]
        public string ErrorCode { get; set; }

        [JsonProperty("taskId")]
        public int TaskId { get; set; }
    }

    public class JSonRequester
    {
        [JsonProperty("clientKey")]
        public string clientKey { get; set; }

        [JsonProperty("taskId")]
        public int TaskId { get; set; }
    }
}
