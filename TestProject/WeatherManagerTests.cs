using System;
using System.Net.Http;
using System.Net;
using System.Threading.Tasks;
using Xunit;
using TestTasks.WeatherFromAPI;
using RichardSzalay.MockHttp;
using System.Text.Json;

namespace TestProject
{
    public class WeatherManagerTests
    {
        private const string GeoBaseUrl = "http://api.openweathermap.org/geo/1.0/direct";
        private const string ForecastBaseUrl = "https://api.openweathermap.org/data/2.5/forecast";

        [Theory]
        [InlineData(null, "London,gb", 1)]
        [InlineData("", "London,gb", 1)]
        [InlineData("Kyiv,ua", null, 2)]
        [InlineData("Kyiv,ua", "", 2)]
        public async Task CompareWeather_Throws_Argument_Exeption_For_Empty_City_Names(string cityA, string cityB, int dayCount)
        {
            // Arrange
            var manager = new WeatherManager();

            // Act + Assert
            await Assert.ThrowsAsync<ArgumentException>(() => manager.CompareWeather(cityA, cityB, dayCount));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(6)]
        public async Task CompareWeather_Throws_Argument_Exeption_For_Invalid_Day_Count(int dayCount)
        {
            // Arrange
            var manager = new WeatherManager();

            // Act + Assert
            await Assert.ThrowsAsync<ArgumentException>(() => manager.CompareWeather("Kyiv,ua", "London,gb", dayCount));
        }

        [Fact]
        public async Task CompareWeather_Throws_Argument_Exception_If_Geo_Empty()
        {
            // Arrange
            var mockHttp = new MockHttpMessageHandler();

            string geoUrlA = $"{GeoBaseUrl}?q=Kyiv,ua&limit=1&appid=da30e988baeb89d1d38b46cffee1c4a4";
            mockHttp.When(geoUrlA)
                    .Respond("application/json", "[]");

            var httpClient = mockHttp.ToHttpClient();

            var manager = new WeatherManager(httpClient);

            // Act + Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                manager.CompareWeather("Kyiv,ua", "London,gb", 1));
        }

        [Fact]
        public async Task CompareWeather_Throws_Argument_Exception_If_Forecast_Empty()
        {
            // Arrange
            var mockHttp = new MockHttpMessageHandler();

            string geoUrlA = $"{GeoBaseUrl}?q=Kyiv,ua&limit=1&appid=da30e988baeb89d1d38b46cffee1c4a4";
            var geoKyivJson = "[ {\"name\":\"Kyiv\",\"lat\":50.45,\"lon\":30.52,\"country\":\"UA\"} ]";
            mockHttp.When(geoUrlA).Respond("application/json", geoKyivJson);

            string geoUrlB = $"{GeoBaseUrl}?q=London,gb&limit=1&appid=da30e988baeb89d1d38b46cffee1c4a4";
            var geoLondonJson = "[ {\"name\":\"London\",\"lat\":51.5074,\"lon\":-0.1278,\"country\":\"GB\"} ]";
            mockHttp.When(geoUrlB).Respond("application/json", geoLondonJson);

            string forecastKyivUrl = $"{ForecastBaseUrl}?lat=50,45&lon=30,52&appid=da30e988baeb89d1d38b46cffee1c4a4&units=metric";
            var emptyForecastJson = "{\"list\": []}";
            mockHttp.When(forecastKyivUrl).Respond("application/json", emptyForecastJson);

            string forecastLondonUrl = $"{ForecastBaseUrl}?lat=51,5074&lon=-0,1278&appid=da30e988baeb89d1d38b46cffee1c4a4&units=metric";
            var forecastLondonJson = "{\"list\":[{\"main\":{\"temp\":15},\"weather\":[],\"dt_txt\":\"2023-08-02 12:00:00\"}]}";
            mockHttp.When(forecastLondonUrl).Respond("application/json", forecastLondonJson);

            var httpClient = mockHttp.ToHttpClient();
            var manager = new WeatherManager(httpClient);

            // Act + Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                manager.CompareWeather("Kyiv,ua", "London,gb", 1));
        }

        [Fact]
        public async Task CompareWeather_Returns_Result_Valid_Scenario()
        {
            var mockHttp = new MockHttpMessageHandler();

            string geoKyivUrl = $"{GeoBaseUrl}?q=Kyiv,ua&limit=1&appid=da30e988baeb89d1d38b46cffee1c4a4";
            string geoKyivJson = "[ {\"name\":\"Kyiv\",\"lat\":50.45,\"lon\":30.52,\"country\":\"UA\"} ]";
            mockHttp.When(geoKyivUrl).Respond("application/json", geoKyivJson);

            string geoLondonUrl = $"{GeoBaseUrl}?q=London,gb&limit=1&appid=da30e988baeb89d1d38b46cffee1c4a4";
            string geoLondonJson = "[ {\"name\":\"London\",\"lat\":51.5074,\"lon\":-0.1278,\"country\":\"GB\"} ]";
            mockHttp.When(geoLondonUrl).Respond("application/json", geoLondonJson);

            // day 1 => temp avg = 2,625, rain = 0
            // day 2 => temp avg = 1, rain = 3
            var forecastKyivUrl = $"{ForecastBaseUrl}?lat=50,45&lon=30,52&appid=da30e988baeb89d1d38b46cffee1c4a4&units=metric";
            string kyivForecastJson = GetKyivForecastJson();
            mockHttp.When(forecastKyivUrl).Respond("application/json", kyivForecastJson);

            // day1 => temp avg = -6,825, rain = 4
            // day2 => temp avg = -7,75, rain = 5
            var forecastLondonUrl = $"{ForecastBaseUrl}?lat=51,5074&lon=-0,1278&appid=da30e988baeb89d1d38b46cffee1c4a4&units=metric";
            string londonForecastJson = GetLondonForecastJson();
            mockHttp.When(forecastLondonUrl).Respond("application/json", londonForecastJson);

            var httpClient = mockHttp.ToHttpClient();
            var manager = new WeatherManager(httpClient);

            // Act
            var result = await manager.CompareWeather("Kyiv,ua", "London,gb", 2);

            // Assert
            Assert.Equal("Kyiv, UA", result.CityA);
            Assert.Equal("London, GB", result.CityB);
            Assert.Equal(2, result.WarmerDaysCount);
            Assert.Equal(0, result.RainierDaysCount);
        }

        private static string GetLondonForecastJson()
        {
            var londonForecastJson = JsonSerializer.Serialize(new
            {
                list = new object[] {
                    new {
                        main = new { temp = -9.0 },
                        weather = new[]{new { main = "Rain" }},
                        dt_txt = "2025-02-01 00:00:00"
                    },
                    new {
                        main = new { temp = -11.0 },
                        weather = new[]{new { main = "Clouds" }},
                        dt_txt = "2025-02-01 03:00:00"
                    },
                    new {
                        main = new { temp = -12.0 },
                        weather = new[]{new { main = "Rain" }},
                        dt_txt = "2025-02-01 06:00:00"
                    },
                    new {
                        main = new { temp = -8.0 },
                        weather = new[]{new { main = "Clouds" }},
                        dt_txt = "2025-02-01 09:00:00"
                    },
                    new {
                        main = new { temp = -5.0 },
                        weather = new[]{new { main = "Rain" }},
                        dt_txt = "2025-02-01 12:00:00"
                    },
                    new {
                        main = new { temp = -2.0 },
                        weather = new[]{new { main = "Clouds" }},
                        dt_txt = "2025-02-01 15:00:00"
                    },
                    new {
                        main = new { temp = -3.0 },
                        weather = new[]{new { main = "Rain" }},
                        dt_txt = "2025-02-01 18:00:00"
                    },
                    new {
                        main = new { temp = -5.0 },
                        weather = new[]{new { main = "Clouds" }},
                        dt_txt = "2025-02-01 21:00:00"
                    },
                    // ---
                    new {
                        main = new { temp = -8.0 },
                        weather = new[]{new { main = "Rain" }},
                        dt_txt = "2025-02-02 00:00:00"
                    },
                    new {
                        main = new { temp = -12.0 },
                        weather = new[]{new { main = "Rain" }},
                        dt_txt = "2025-02-02 03:00:00"
                    },
                    new {
                        main = new { temp = -13.0 },
                        weather = new[]{new { main = "Clouds" }},
                        dt_txt = "2025-02-02 06:00:00"
                    },
                    new {
                        main = new { temp = -7.0 },
                        weather = new[]{new { main = "Clouds" }},
                        dt_txt = "2025-02-02 09:00:00"
                    },
                    new {
                        main = new { temp = -5.0 },
                        weather = new[]{new { main = "Rain" }},
                        dt_txt = "2025-02-02 12:00:00"
                    },
                    new {
                        main = new { temp = -3.0 },
                        weather = new[]{new { main = "Rain" }},
                        dt_txt = "2025-02-02 15:00:00"
                    },
                    new {
                        main = new { temp = -6.0 },
                        weather = new[]{new { main = "Rain" }},
                        dt_txt = "2025-02-02 18:00:00"
                    },
                    new {
                        main = new { temp = -8.0 },
                        weather = new[]{new { main = "Clouds" }},
                        dt_txt = "2025-02-02 21:00:00"
                    }
                }
            });
            return londonForecastJson;
        }

        private static string GetKyivForecastJson()
        {
            return JsonSerializer.Serialize(new
            {
                list = new object[] {
                    new {
                        main = new { temp = -1.0 },
                        weather = new[]{new { main = "Clouds" }},
                        dt_txt = "2025-02-01 00:00:00"
                    },
                    new {
                        main = new { temp = -2.0 },
                        weather = new[]{new { main = "Clouds" }},
                        dt_txt = "2025-02-01 03:00:00"
                    },
                    new {
                        main = new { temp = -1.0 },
                        weather = new[]{new { main = "Clouds" }},
                        dt_txt = "2025-02-01 06:00:00"
                    },
                    new {
                        main = new { temp = 2.0 },
                        weather = new[]{new { main = "Clouds" }},
                        dt_txt = "2023-02-01 09:00:00"
                    },
                    new {
                        main = new { temp = 4.0 },
                        weather = new[]{new { main = "Clouds" }},
                        dt_txt = "2025-02-01 12:00:00"
                    },
                    new {
                        main = new { temp = 8.0 },
                        weather = new[]{new { main = "Clouds" }},
                        dt_txt = "2025-02-01 15:00:00"
                    },
                    new {
                        main = new { temp = 6.0 },
                        weather = new[]{new { main = "Clouds" }},
                        dt_txt = "2025-02-01 18:00:00"
                    },
                    new {
                        main = new { temp = 5.0 },
                        weather = new[]{new { main = "Clouds" }},
                        dt_txt = "2025-02-01 21:00:00"
                    },
                    // -----
                    new {
                        main = new { temp = -2.0 },
                        weather = new[]{new { main = "Rain" }},
                        dt_txt = "2025-02-02 00:00:00"
                    },
                    new {
                        main = new { temp = -3.0 },
                        weather = new[]{new { main = "Clouds" }},
                        dt_txt = "2025-02-02 03:00:00"
                    },
                    new {
                        main = new { temp = -2.0 },
                        weather = new[]{new { main = "Cloud" }},
                        dt_txt = "2025-02-02 06:00:00"
                    },
                    new {
                        main = new { temp = 0.0 },
                        weather = new[]{new { main = "Clouds" }},
                        dt_txt = "2025-02-02 09:00:00"
                    },
                    new {
                        main = new { temp = 3.0 },
                        weather = new[]{new { main = "Rain" }},
                        dt_txt = "2025-02-02 12:00:00"
                    },
                    new {
                        main = new { temp = 5.0 },
                        weather = new[]{new { main = "Clouds" }},
                        dt_txt = "2025-02-02 15:00:00"
                    },
                    new {
                        main = new { temp = 5.0 },
                        weather = new[]{new { main = "Rain" }},
                        dt_txt = "2025-02-02 18:00:00"
                    },
                    new {
                        main = new { temp = 2.0 },
                        weather = new[]{new { main = "Clouds" }},
                        dt_txt = "2025-02-02 21:00:00"
                    }
                }
            });
        }
    }
}
