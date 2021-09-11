using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Weather.Domain;

namespace Console.Formatter.Host.Controllers
{
   [ApiController]
   [Route("[controller]")]
   public class WeatherForecastController : ControllerBase
   {
      private readonly WeatherService _weatherService;
      private readonly ILogger<WeatherForecastController> _logger;

      public WeatherForecastController(
         WeatherService weatherService, 
         ILogger<WeatherForecastController> logger)
      {
         _weatherService = weatherService;
         _logger = logger;
      }

      [HttpGet]
      public IEnumerable<WeatherForecast> Get()
      {
         _logger.LogInformation("Invoking WeatherService");

         return _weatherService.GetForecasts(daysAhead: 5).ToArray();
      }
   }
}