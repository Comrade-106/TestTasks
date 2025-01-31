using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using TestTasks.WeatherFromAPI.Models;

namespace TestTasks.WeatherFromAPI
{
    public class WeatherManager
    {
        private const string apiKey = "da30e988baeb89d1d38b46cffee1c4a4";

        private readonly HttpClient _httpClient;

        public WeatherManager()
        {
            _httpClient = new HttpClient();
        }

        public WeatherManager(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        // One call api isn`t available for free tier, so I decided to use forecast api
        // This api is very similar to one call api, it also has 5 days forecast with data about temperature and rain
        public async Task<WeatherComparisonResult> CompareWeather(string cityA, string cityB, int dayCount)
        {
            ValidateParameters(cityA, cityB, dayCount);

            GeoResult cityAGeo = await GetCityGeoByName(cityA);
            GeoResult cityBGeo = await GetCityGeoByName(cityB);

            DailyWeatherData[] cityAData = await GetWeatherDataForDays(cityAGeo, dayCount);
            DailyWeatherData[] cityBData = await GetWeatherDataForDays(cityBGeo, dayCount);

            int warmerDays = 0;
            int rainierDays = 0;
            for (int i = 0; i < dayCount; i++)
            {
                warmerDays += cityAData[i].DailyAverageTemperature > cityBData[i].DailyAverageTemperature ? 1 : 0;

                rainierDays += cityAData[i].DailyRainVolume > cityBData[i].DailyRainVolume ? 1 : 0;
            }

            return new WeatherComparisonResult(cityAGeo.GetFullCountryName(), 
                                                cityBGeo.GetFullCountryName(), 
                                                warmerDays, 
                                                rainierDays);
        }

        private static void ValidateParameters(string cityA, string cityB, int dayCount)
        {
            if (string.IsNullOrWhiteSpace(cityA))
                throw new ArgumentException("City A cannot be empty.", nameof(cityA));

            if (string.IsNullOrWhiteSpace(cityB))
                throw new ArgumentException("City B cannot be empty.", nameof(cityB));

            if (dayCount < 1 || dayCount > 5)
                throw new ArgumentException("Day count should be between 1 and 5.", nameof(dayCount));
        }

        private async Task<GeoResult> GetCityGeoByName(string cityName)
        {
            string url = $"http://api.openweathermap.org/geo/1.0/direct?q={cityName}&limit=1&appid={apiKey}";

            using var response = await _httpClient.GetAsync(url);
            await HandleErrors(response);

            var json = await response.Content.ReadAsStringAsync();

            var results = JsonSerializer.Deserialize<GeoResult[]>(json);

            if (results == null || results.Length == 0)
                throw new ArgumentException($"City '{cityName}' not found.", nameof(cityName));

            return results[0];
        }

        private async Task<DailyWeatherData[]> GetWeatherDataForDays(GeoResult geoResult, int dayCount)
        {
            var data = await GetForecastRequest(geoResult);

            var results = new DailyWeatherData[dayCount];

            var groupedByDay = data.List
                .GroupBy(entry => DateTime.Parse(entry.DateTimeText).Date)
                .OrderBy(g => g.Key)
                .Take(dayCount) 
                .ToList();

            int index = 0;
            foreach (var dayGroup in groupedByDay)
            {
                double tempSum = 0;
                int count = dayGroup.Count();
                int rainCount = 0;

                foreach (var entry in dayGroup)
                {
                    tempSum += entry.Main.Temperature;
                    rainCount += entry.Weather.Any(w => w.IsRain()) ? 1 : 0;
                }

                double avgTemp = tempSum / dayGroup.Count();

                results[index++] = new DailyWeatherData
                {
                    DailyAverageTemperature = avgTemp,
                    DailyRainVolume = rainCount
                };
            }

            return results;
        }

        private async Task<WeatherData> GetForecastRequest(GeoResult geoResult)
        {
            string url = $"https://api.openweathermap.org/data/2.5/forecast?lat={geoResult.Lat}&lon={geoResult.Lon}&appid={apiKey}&units=metric";

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
                {
                    throw new HttpRequestException("Too many requests");
                }

                string errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Error response: {response.StatusCode}, body: \n{errorBody}");
            }
        }
    }
}
