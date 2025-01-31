using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TestTasks.WeatherFromAPI
{
    public class WeatherData
    {
        [JsonPropertyName("list")]
        public List<WeatherEntry> List { get; set; }
    }
}
