using System;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Threading.Tasks;
using System.Text.Json;

namespace TestTasks.WeatherFromAPI
{
    public class WeatherApiClient : IWeatherApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public WeatherApiClient(HttpClient httpClient, string apiKey)
        {
            _httpClient = httpClient;
            _apiKey = apiKey;
        }

        public async Task<GeoResult> GetCityGeoByName(string cityName)
        {
            string url = $"http://api.openweathermap.org/geo/1.0/direct?q={cityName}&limit=1&appid={_apiKey}";

            using var response = await _httpClient.GetAsync(url);
            await HandleErrors(response);

            var json = await response.Content.ReadAsStringAsync();
            var results = JsonSerializer.Deserialize<GeoResult[]>(json);

            if (results == null || results.Length == 0)
                throw new ArgumentException($"City '{cityName}' not found.", nameof(cityName));

            return results[0];
        }

        public async Task<WeatherData> GetWeatherForecast(GeoResult geoResult)
        {
            string url = $"https://api.openweathermap.org/data/2.5/forecast?lat={geoResult.Lat}&lon={geoResult.Lon}&appid={_apiKey}&units=metric";

            using var response = await _httpClient.GetAsync(url);
            await HandleErrors(response);

            var json = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<WeatherData>(json);

            if (data == null || data.List == null || data.List.Count == 0)
                throw new ArgumentException($"Data for '{geoResult.Name}' not found.", nameof(geoResult));

            return data;
        }

        private static async Task HandleErrors(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                    throw new HttpRequestException("Too many requests");

                string errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Error response: {response.StatusCode}, body: \n{errorBody}");
            }
        }
    }
}
