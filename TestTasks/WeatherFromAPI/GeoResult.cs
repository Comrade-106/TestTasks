using System.Text.Json.Serialization;

namespace TestTasks.WeatherFromAPI
{
    public class GeoResult
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("lat")]
        public double Lat { get; set; }

        [JsonPropertyName("lon")]
        public double Lon { get; set; }

        [JsonPropertyName("country")]
        public string Country { get; set; }

        public string GetFullCountryName() => $"{Name}, {Country}";
    }
}
