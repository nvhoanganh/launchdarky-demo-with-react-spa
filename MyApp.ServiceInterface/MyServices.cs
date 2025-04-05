using ServiceStack;
using MyApp.ServiceModel;
using ServiceStack.OrmLite;

using LaunchDarkly.Sdk;
using LaunchDarkly.Sdk.Server;

namespace MyApp.ServiceInterface;

public class MyServices : Service
{
    private readonly LdClient ldClient;
    public MyServices(LdClient ldClient)
    {
        this.ldClient = ldClient;
    }
    public object Any(Hello request)
    {
        return new HelloResponse { Result = $"Hello, {request.Name}!" };
    }
    
    static readonly string[] summaries = [
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    ];

    private string GetSummaryForTemperature(int temperatureC)
    {
        return temperatureC switch
        {
            <= -10 => "Freezing 🥶",
            <= 0 => "Bracing 🌬️",
            <= 10 => "Chilly ❄️",
            <= 15 => "Cool 🍃",
            <= 20 => "Mild 🌤️",
            <= 25 => "Warm 🌞",
            <= 30 => "Balmy 🌴",
            <= 35 => "Hot 🔥",
            <= 40 => "Sweltering 🌡️",
            _ => "Scorching 🌋",
        };
    }

    public async Task<object> Any(GetWeatherForecast request)
    {
        var userSession = await GetSessionAsync();
        var roles = userSession?.Roles != null ? string.Join(",", userSession.Roles) : "guest";
        var context = !string.IsNullOrEmpty(userSession?.Email)
            ? Context.Builder(userSession.Email)
                .Name(userSession.DisplayName)
                .Set("roles", roles)
                .Build()
            : Context.Builder("ananymous")
                .Anonymous(true)
                .Build();

        var useV2Summary = ldClient.BoolVariation("get-weather-v-2", context, false);

        var rng = new Random();
        return await Task.WhenAll(Enumerable.Range(1, 5).Select(async index =>
        {
            var temperatureC = rng.Next(-20, 55);

            return new Forecast(
            Date: (request.Date ?? DateOnly.FromDateTime(DateTime.Now)).AddDays(index),
                TemperatureC: temperatureC,
                Summary: useV2Summary ? GetSummaryForTemperature(temperatureC) : summaries[new Random().Next(summaries.Length)]
                );
        }));
    }

    public async Task<object> Any(AdminData request)
    {
        var tables = new (string Label, Type Type)[] 
        {
            ("Bookings", typeof(Booking)),
            ("Coupons",  typeof(Coupon)),
        };
        var dialect = Db.GetDialectProvider();
        var totalSql = tables.Map(x => $"SELECT '{x.Label}', COUNT(*) FROM {dialect.GetQuotedTableName(x.Type.GetModelMetadata())}")
            .Join(" UNION ");
        var results = await Db.DictionaryAsync<string,int>(totalSql);
        
        return new AdminDataResponse {
            PageStats = tables.Map(x => new PageStats {
                Label = x.Label, 
                Total = results[x.Label],
            })
        };
    }
}
