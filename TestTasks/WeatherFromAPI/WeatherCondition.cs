using System.Text.Json.Serialization;

namespace TestTasks.WeatherFromAPI
{
    public class WeatherCondition
    {
        [JsonPropertyName("main")]
        public string Main { get; set; }

        public bool IsRain()
        {
            if(string.IsNullOrEmpty(Main))
                return false;

            return Main == "Rain";
        }
    }
}
