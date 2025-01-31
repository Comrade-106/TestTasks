
using System.Threading.Tasks;

namespace TestTasks.WeatherFromAPI
{
    public interface IWeatherApiClient
    {
        Task<GeoResult> GetCityGeoByName(string cityName);
        Task<WeatherData> GetWeatherForecast(GeoResult geoResult);
    }
}
