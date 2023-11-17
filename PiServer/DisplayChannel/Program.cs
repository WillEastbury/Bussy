using System;
using System.Device.I2c;
using System.Text;
using System.Drawing;
using System.Collections;
using Iot.Device.CharacterLcd;
using Iot.Device.GrovePiDevice;
using Iot.Device.GrovePiDevice.Models;
using Iot.Device.GrovePiDevice.Sensors;
using System.Text.Json;

internal class Program
{
    static string ServerAddress = "192.168.200.36";
    static string AuthKey = "Insert_Shared_Key_Here";
    static string HostName = "TankSensorsClean";
    static I2cDevice i2cLcdDevice;
    static I2cDevice i2cRgbDevice;
    static I2cDevice lowWaterDevice;
    static I2cDevice highWaterDevice;
    static DigitalInput GreySensor;
    static LcdRgb display;
    static HttpClient client = new HttpClient();
    static GrovePi grovePi;
    static int threshold = 120; // Adjust the threshold as needed
    public static async Task<int> Main(string[] args)
    {
        I2cConnectionSettings i2CConnectionSettings = new I2cConnectionSettings(1, 0x04);
        grovePi = new GrovePi(I2cDevice.Create(i2CConnectionSettings));
        string header = $"{HostName}{AuthKey}";
        client.DefaultRequestHeaders.Add("ServiceKey", header);

        // I2C LCD Display
        i2cLcdDevice = I2cDevice.Create(new I2cConnectionSettings(busId: 1, deviceAddress: 0x3E));
        i2cRgbDevice = I2cDevice.Create(new I2cConnectionSettings(busId: 1, deviceAddress: 0x62));

        // I2C Water Sensors for Clean Water Tank
        lowWaterDevice = I2cDevice.Create(new I2cConnectionSettings(busId: 1, deviceAddress: 0x78));
        highWaterDevice = I2cDevice.Create(new I2cConnectionSettings(busId: 1, deviceAddress: 0x77));

        display = new LcdRgb(new System.Drawing.Size(16, 2), i2cLcdDevice, i2cRgbDevice);
        MovingAverage movingAverage = new MovingAverage(5);

        int lvl = 0;
        while(true)
        {
            try
            {
                display.Clear();
                display.Write($"Connecting to Server...");
                string jsonData = await ReadFromServer();

                if (!string.IsNullOrEmpty(jsonData))
                {
                    // Deserialize the JSON response into an object
                    DataDTO data = JsonSerializer.Deserialize<DataDTO>(jsonData);
                    string hot = data.IsHotWaterRequired ? "ON" : "Off";
                    string cold = data.IsColdWaterRequired ? "ON" : "Off";
                    string off = data.WaterShutOff ? "Shutdown" : "OK";

                    // Display the relevant information on the LCD display
                    
                    if (data.WaterShutOff)
                    {
                        display.SetBacklightColor(Color.Red);
                    }
                    else
                    {
                        display.SetBacklightColor(Color.Green);
                    }

                    display.Clear();
                    display.Write($"Hot:{hot} LVL: {lvl}%");
                    display.SetCursorPosition(0, 1);
                    display.Write($"Cold:{cold} !: {off}");
                }

                lvl = (int) movingAverage.AddDataPoint(GetTankSensorData());
                await Task.Delay(250);
                lvl = (int) movingAverage.AddDataPoint(GetTankSensorData());
                await SendAnalogSensorData("CleanWaterTank", lvl);
                await Task.Delay(250);

                Console.Write(lvl);
                Console.WriteLine("%");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        static int GetTankSensorData()
        {
            // Pull the data from the ATTINY85 on the I2C bus (2 devices) - low and high water sensor data from I2C devices
            byte[] combinedData = new byte[20];                                             

            Array.Copy(ReadI2CData(lowWaterDevice, 8), 0, combinedData, 0, 8);              // Copy lowData to the beginning of combinedData
            Array.Copy(ReadI2CData(highWaterDevice, 12), 0, combinedData, 8, 12);           // Copy highData after lowData

            int totalPads = 21;                                                             // Total number of pads
            int padsCoveredByWater = combinedData.Count(b => (b >= 200) && (b <= 255));  

            double waterHeightPercentage = (double)padsCoveredByWater / totalPads * 100;    

            return (int) waterHeightPercentage;
        }
    
        static async Task SendAnalogSensorData(string SensorName, int level)
        {
            // Read water level and send to the service 
            string apiUrl = $"http://" + ServerAddress + ":5000/api/analogs/{SensorName}/{level.ToString()}";

            HttpResponseMessage response = await client.GetAsync(apiUrl);
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("Transmission Successful! (analogs)" + SensorName + " " + level.ToString());
            }
            else
            {
                Console.WriteLine("HTTP send request failed with status code: " + response.StatusCode);
            }
        }

        static async Task SendDigitalSensorData(string SensorName, bool level)
        {
            int levelout = level ? 1 : 0;
            // Read water level and send to the service 
            string apiUrl = $"http://" + ServerAddress + ":5000/api/sensors/{SensorName}/{levelout}";
            HttpResponseMessage response = await client.GetAsync(apiUrl);
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("Transmission Successful! (Digital Sensors):" + SensorName + " " + level.ToString() + " " + levelout.ToString());
            }
            else
            {
                Console.WriteLine("HTTP send failed with status code: " + response.StatusCode);
            }
        }

        static int ReadLevel(bool[] data)
        {
            int level = 0;

            for (int i = 0; i < data.Length; i++)
            {
                if (data[i])
                {
                    level = level + 5;
                }
            }
            return level ;
        }
        

        static void RenderHorizontalBar(bool[] thresholdReached)
        {
            // Define the character to represent the bar
            char barCharacter = '*';

            for (int i = 0; i < thresholdReached.Length; i++)
            {
                if (thresholdReached[i])
                {
                    Console.Write(barCharacter);
                }
                else
                {
                   // Console.Write(' '); // Print a space for non-threshold bits
                }
            }
        }

        static byte[] ReadI2CData(I2cDevice device, int dataSize)
        {
            byte[] data = new byte[dataSize];
            device.Read(data);
            return data;
        }
    }

    private static async Task<string> ReadFromServer()
    {
        string jsonData = "";
        // Make an HTTP GET request to retrieve the data
        string apiUrl = "http://" + ServerAddress + ":5000/api/all";

        HttpResponseMessage response = await client.GetAsync(apiUrl);

        if (response.IsSuccessStatusCode)
        {
            jsonData = await response.Content.ReadAsStringAsync();
        }
        else
        {
            Console.WriteLine("HTTP get request failed with status code: " + response.StatusCode);
        }

        return jsonData;
    }

    public class MovingAverage
    {
        private Queue<int> dataQueue;
        private int period;
        private int sum;

        public MovingAverage(int period)
        {
            this.period = period;
            this.dataQueue = new Queue<int>(period);
            this.sum = 0;
        }

        public double AddDataPoint(int dataPoint)
        {
            dataQueue.Enqueue(dataPoint);
            sum += dataPoint;

            if (dataQueue.Count > period)
            {
                int removedData = dataQueue.Dequeue();
                sum -= removedData;
            }

            return (double)sum / dataQueue.Count;
        }
    }
    public class DataDTO
    {
        public bool IsHotWaterRequired { get; set; }
        public bool IsColdWaterRequired { get; set; }
        public bool WaterShutOff { get; set; }
        public bool WaterPumpEnabled { get; set; }
        public bool BoilerSolenoidEnabled { get; set; }
        public bool IsColdTapInKitchenOn { get; set; }
        public bool IsHotTapInKitchenOn { get; set; }
        public bool IsColdTapInBathroomOn { get; set; }
        public bool IsHotTapInBathroomOn { get; set; }
        public string WaterShutOffReason { get; set; }
        public string ActualWaterPercentageInColdTank { get; set; }
        public string ActualWaterPercentageInWasteTank { get; set; }
    }
}