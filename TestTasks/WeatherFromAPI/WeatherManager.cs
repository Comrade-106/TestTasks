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

        private readonly IWeatherApiClient _weatherApiClient;

        public WeatherManager() : this(new HttpClient())
        {
        }

        public WeatherManager(HttpClient httpClient)
        {
            _weatherApiClient = new WeatherApiClient(httpClient, apiKey);
        }

        // One call api isn`t available for free tier, so I decided to use forecast api
        // This api is very similar to one call api, it also has 5 days forecast with data about temperature and rain
        public async Task<WeatherComparisonResult> CompareWeather(string cityA, string cityB, int dayCount)
        {
            ValidateParameters(cityA, cityB, dayCount);

            GeoResult cityAGeo = await _weatherApiClient.GetCityGeoByName(cityA);
            GeoResult cityBGeo = await _weatherApiClient.GetCityGeoByName(cityB);

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

        private async Task<DailyWeatherData[]> GetWeatherDataForDays(GeoResult geoResult, int dayCount)
        {
            var data = await _weatherApiClient.GetWeatherForecast(geoResult);

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
    }
}
