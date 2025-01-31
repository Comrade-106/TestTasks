using System.Text.Json.Serialization;

namespace TestTasks.WeatherFromAPI
{
    public class MainData
    {
        [JsonPropertyName("temp")]
        public double Temperature { get; set; }
    }
}
