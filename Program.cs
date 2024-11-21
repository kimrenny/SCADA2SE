using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using Program.cs;

class SensorEmulator
{
    private static readonly HttpClient _httpClient = new HttpClient();
    private const string BaseUrl = "http://localhost:5039/api/Sensor";
    private static readonly Dictionary<Guid, double> SensorValues = new Dictionary<Guid, double>();

    static async Task Main(string[] args)
    {
        Console.WriteLine("Sensor emulation has started...");

        while (true)
        {
            var sensors = await GetSensors();

            foreach (var sensor in sensors)
            {
                Console.WriteLine($"Sensor ID: {sensor.Id}, Name: {sensor.Name}, Value: {sensor.Value}");
                if (string.IsNullOrWhiteSpace(sensor.Value) || !double.TryParse(sensor.Value, out var parsedValue))
                {
                    parsedValue = 0.0;
                }

                var currentValue = GetCurrentValue(sensor.Id, parsedValue);
                await UpdateSensorValue(sensor.Id.ToString(), currentValue.ToString("F2"));
                Console.WriteLine($"Sensor {sensor.Name} updated. New Value: {currentValue}");
            }

            await Task.Delay(1000);
        }
    }

    private static async Task<SensorResponse[]> GetSensors()
    {
        var response = await _httpClient.GetAsync($"{BaseUrl}");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"Raw JSON Response: {json}");
        return JsonSerializer.Deserialize<SensorResponse[]>(json);
    }

    private static async Task UpdateSensorValue(string sensorId, string newValue)
    {
        var model = new
        {
            Id = sensorId,
            Value = newValue
        };

        var content = new StringContent(JsonSerializer.Serialize(model), Encoding.UTF8, "application/json");
        var response = await _httpClient.PutAsync($"{BaseUrl}", content);
        if (response.IsSuccessStatusCode)
        {
            Console.WriteLine($"Sensor {sensorId} updated successfully with value {newValue}");
        }
        else
        {
            Console.WriteLine($"Failed to update sensor {sensorId}. Status: {response.StatusCode}, Reason: {response.ReasonPhrase}");
        }
    }

    private static double GetCurrentValue(Guid sensorId, double currentValue)
    {
        if (!SensorValues.ContainsKey(sensorId))
        {
            SensorValues[sensorId] = currentValue;
        }

        var random = new Random();
        var change = random.NextDouble() * 2 - 1;
        var newValue = SensorValues[sensorId] + change;

        newValue = Math.Max(-100, Math.Min(100, newValue));

        SensorValues[sensorId] = newValue;

        return newValue;
    }
}
