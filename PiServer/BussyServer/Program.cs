using System.Collections.Concurrent;
using System.Text;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Any;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Net;
using System.Net.NetworkInformation;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

const string AuthKey = "<Insert_Shared_Key_Here>";
const string AppUrl = "http://*:5000";
const string SecondAppUrl = "http://*:5001";
const int SensorExtensionSeconds = 2;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options => {
    options.SwaggerDoc("v1", new OpenApiInfo {
        Version = "v1",
        Title = "BussyServer (Bussy McBus Face's Server)",
        Description = "A water control and temperature / boiler control system for a campervan conversion",
        Contact = new OpenApiContact {Name = "William Eastbury"},
        License = new OpenApiLicense {Name = "MIT License"}
    });
});

WebApplication app = builder.Build();
// Task backgrounder = Task.Run(() => BackgroundProcess);
app.MapGet("/status", () => {
    
    return Results.File(Path.Combine(Directory.GetCurrentDirectory(), "index.html"), "text/html");
});

app.UseSwagger();
app.UseSwaggerUI(c => {
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "BussyServer v1");
    c.RoutePrefix = "swagger";
});

ConcurrentDictionary<string, DateTime> Connections = new();
ConcurrentDictionary<string, bool> Sensors = new();
ConcurrentDictionary<string, DateTime> Timers = new();
ConcurrentDictionary<string, int> Analogs = new();

// Calculations
bool OnMainsWater() => Sensors["MainsConnectedWater"];
bool WasteTankFull() => OnMainsWater() ? false : Analogs["WasteWaterTank"] > 90;
bool CleanTankFull() => OnMainsWater() ? false : Analogs["CleanWaterTank"] > 90;
bool CleanTankEmpty() => OnMainsWater() ? false : Analogs["CleanWaterTank"] < 10; 

string WaterTankPercent() => Analogs["CleanWaterTank"].ToString(); 
string WasteTankPercent() => Analogs["WasteWaterTank"].ToString();

bool IsColdWaterRequired() => IsColdTapInKitchenOn() || IsColdTapInBathroomOn() || IsHotWaterRequired() ;
bool IsHotWaterRequired() => IsHotTapInKitchenOn() || IsHotTapInBathroomOn() ;

bool WaterShutOff() => OnMainsWater() ? false : CleanTankEmpty() ? true : WasteTankFull()  ? true : false;

bool WaterPumpEnabled() => OnMainsWater() ? false : ! WaterShutOff() && (IsColdWaterRequired() ||  IsHotWaterRequired());
bool BoilerSolenoidEnabled() => ! WaterShutOff() && IsHotWaterRequired();
bool WaterSystemActiveLightOn() => IsColdWaterRequired() ||  IsHotWaterRequired();
bool IsColdTapInKitchenOn() => !WaterShutOff() && (Timers["C_Kitchen"] - DateTime.Now).TotalSeconds > 0;
bool IsHotTapInKitchenOn() => !WaterShutOff() && (Timers["H_Kitchen"] - DateTime.Now).TotalSeconds > 0;
bool IsColdTapInBathroomOn() => !WaterShutOff() && (Timers["C_Bathroom"] - DateTime.Now).TotalSeconds > 0;
bool IsHotTapInBathroomOn() => !WaterShutOff() && (Timers["H_Bathroom"] - DateTime.Now).TotalSeconds > 0;

Dictionary<string, Func<bool>> AllDataBool = new Dictionary<string, Func<bool>>
{
    { "IsColdTapInKitchenOn", IsColdTapInKitchenOn },
    { "IsHotTapInKitchenOn", IsHotTapInKitchenOn },
    { "IsColdTapInBathroomOn", IsColdTapInBathroomOn },
    { "IsHotTapInBathroomOn", IsHotTapInBathroomOn },
    { "WaterSystemActiveLightOn", WaterSystemActiveLightOn},
    { "IsHotWaterRequired", IsHotWaterRequired },
    { "IsColdWaterRequired", IsColdWaterRequired },
    { "WasteTankFull", WasteTankFull },
    { "CleanTankFull", CleanTankFull },
    { "CleanTankEmpty", CleanTankEmpty },
    { "WaterShutOff", WaterShutOff },
    { "OnMainsWater", OnMainsWater },
    { "WaterPumpEnabled", WaterPumpEnabled },
    { "BoilerSolenoidEnabled", BoilerSolenoidEnabled }
    
};

Dictionary<string, Func<string>> AllDataString = new Dictionary<string, Func<string>>
{
      { "ActualWaterPercentageInColdTank", WaterTankPercent },
      { "ActualWaterPercentageInWasteTank", WasteTankPercent }
};

SetInitialValuesForSensorData();
ConfigureWebHosting();

// Start the server on the first available port
if (!PortInUse(5000))
{
    Console.WriteLine("Bussy McServer Starting: Web -> " + AppUrl);
    app.Run(AppUrl);
}
else 
{
    Console.WriteLine("Port 5000 already in use!");
    Console.WriteLine("Bussy McServer Starting: Web -> " + SecondAppUrl);
    app.Run(SecondAppUrl);
}

bool PortInUse(int port)
{
    var ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
    var activeListeners = ipGlobalProperties.GetActiveTcpListeners();
    return activeListeners.Any(e => e.Port == port);
}

void ProcessRemoteSensor(string sensor, bool value)
{
    // Set the internal value of the sensor
    Sensors[sensor] = value;
    if (value)
    {
        // We caught a button press ... or a hold ...
        // is there a matching timer ? 
        if (Timers.ContainsKey(sensor))
        {
            if(Timers[sensor] < DateTime.Now) {
                // if the offswitch date is in the past, move it to the future and , extend it by the offset time in SensorExtensionSeconds
                Timers[sensor] = DateTime.Now.AddSeconds(SensorExtensionSeconds +1 );
                //Console.WriteLine($"Timer {sensor} set to {SensorExtensionSeconds + 1 } s from now");
            }
            else {

                // if the offswitch date is in the future, extend it by the offset time in SensorExtensionSeconds
                Timers[sensor] = Timers[sensor].AddSeconds(SensorExtensionSeconds);
                TimeSpan diff = Timers[sensor] - DateTime.Now;
                //Console.WriteLine($"Timer {sensor} extended by {SensorExtensionSeconds} s to {diff.TotalSeconds} s");
            }
        }
    }
}

void ProcessAnalogSensor(string sensor, int value)
{
    // Set the internal value of the sensor
    Analogs[sensor] = value;
}

void SetInitialValuesForSensorData()
{
    Sensors.TryAdd("MainsConnectedWater", false);
    Sensors.TryAdd("C_Kitchen", false);
    Sensors.TryAdd("H_Kitchen", false);
    Sensors.TryAdd("C_Bathroom", false);
    Sensors.TryAdd("H_Bathroom", false);

    // Populate the initial user controlled relay state to all off 
    Timers.TryAdd("C_Kitchen", DateTime.MinValue);
    Timers.TryAdd("H_Kitchen", DateTime.MinValue);
    Timers.TryAdd("C_Bathroom", DateTime.MinValue);
    Timers.TryAdd("H_Bathroom", DateTime.MinValue);

    // Setup the Sensor Data 
    Analogs.TryAdd("CleanWaterTank", 0);        // 3 - What is the current percentage of water in the clean water tank?
    Analogs.TryAdd("WasteWaterTank", 100); 
}

void ConfigureWebHosting()
{
    app.MapGet("api/sensors",      () => Results.Json(Sensors.OrderBy(e => e.Key).ToDictionary(e => e.Key, e => e.Value))).WithName("GetSensors");
    app.MapGet("api/analogs",      () => Results.Json(Analogs.OrderBy(e => e.Key).ToDictionary(e => e.Key, e => e.Value))).WithName("GetAnalogs");
    app.MapGet("api/connections",  () => Results.Json(Connections.OrderBy(e => e.Key).ToDictionary(e => e.Key, e => e.Value))).WithName("GetConnections");
    app.MapGet("api/timers",       () => Results.Json(Timers.OrderBy(e => e.Key).ToDictionary(e => e.Key, e => e.Value))).WithName("GetTimers");
    app.MapGet("api/calculations", (HttpContext context) => {
        bool authed = CheckAndRegisterDeviceConnection(context);
        return Results.Json(AllDataBool.ToDictionary(item => item.Key, item => item.Value()));
    }).WithName("GetCalculations");
    app.MapGet("api/information", () => Results.Json(AllDataString.ToDictionary(item => item.Key, item => item.Value()))).WithName("GetInformation");
    app.MapGet("api/sensors/{SensorName}/{numSensorValue}", ([FromHeader(Name = "ServiceKey")] [Required] string ServiceKeyHeader, [FromRoute] string SensorName, [FromRoute] int numSensorValue, HttpContext context) =>
    {
        bool SensorValue = !(numSensorValue == 0);
        if(CheckAndRegisterDeviceConnection(context)) 
        {
            //Console.WriteLine($"Host {context.Items["Host"]} Requested Sensor: {SensorName} be set to: {SensorValue}");
            ProcessRemoteSensor(SensorName, SensorValue);
            return Results.Ok();
        }
        else
        {
            return Results.Unauthorized();
        }
    })        
    .WithName("WriteSensor");
    
    app.MapGet("api/analogs/{SensorName}/{numSensorValue}", ([FromHeader(Name = "ServiceKey")] [Required] string ServiceKeyHeader, [FromRoute] string SensorName, [FromRoute] int numSensorValue, HttpContext context) =>
    {

        if(CheckAndRegisterDeviceConnection(context)) 
        {
            //Console.WriteLine($"Host {context.Items["Host"]} Requested A Sensor: {SensorName} be set to: {numSensorValue}");
            ProcessAnalogSensor(SensorName, numSensorValue);
            return Results.Ok();
        }
        else
        {
            return Results.Unauthorized();
        }
    })        
    .WithName("WriteAnalogSensor");

    // FromHeader here is a hack to force SwaggerUI to allow us to add the header
    app.MapGet("api/all", ([FromHeader(Name = "ServiceKey")] string? ServiceKeyHeader, HttpContext context) =>
    {
        bool authed = CheckAndRegisterDeviceConnection(context);

        Dictionary<string, object> combinedData = new Dictionary<string, object>();
        foreach (string key in AllDataBool.Keys) combinedData.Add(key, AllDataBool[key]());
        foreach (string key in AllDataString.Keys) combinedData.Add(key, AllDataString[key]());
        foreach (string key in Analogs.Keys) combinedData.Add(key, Analogs[key]);
        combinedData.Add("SystemVersion", "0.03");
        return Results.Json(combinedData);

    })
    .WithName("GetAllData");
    
    bool CheckAndRegisterDeviceConnection(HttpContext context)
    {

        string ServiceKey = context.Request.Headers["ServiceKey"];

        if((ServiceKey ?? "None") != "None")
        {
            string DecodedAuthHeader = ServiceKey;
            context.Items["Host"] = DecodedAuthHeader.Substring(0, 16).Trim();
            context.Items["authKey"] = DecodedAuthHeader.Substring(16,32);
            string Host = context.Items["Host"].ToString();
            string authKey = context.Items["authKey"].ToString();

            bool matchKey = AuthKey == authKey ;
            if(matchKey)
            {
                context.Items["IsAuthenticated"] = true; 
                //Console.WriteLine($"Success - Device {context.Items["Host"]} authenticated");
                // Update the last known connection time for the device if authenticated.
                Connections.AddOrUpdate(context.Items["Host"].ToString(), DateTime.Now, (key, oldValue) => DateTime.Now);
            }
            else
            {
                Console.WriteLine($"Fail - Device {context.Items["Host"]} failed authentication with an invalid key");
                Console.WriteLine($"Client Key: |{context.Items["authKey"]}|");
                context.Items["IsAuthenticated"] = false;
            }
        }
        else
        {
            context.Items["Host"] = context.Connection.RemoteIpAddress.ToString();
            context.Items["IsAuthenticated"] = false;
        }
        return (bool)context.Items["IsAuthenticated"];
    }
}