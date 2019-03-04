using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Windows.System.Threading;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
#if USE_PI
using Windows.Devices.Gpio;
using Sensors.Dht;
#endif


// ReSharper disable RedundantExtendsListEntry
namespace PiTemperatureMonitoringApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    /// <remarks>
    /// Ref: https://www.hackster.io/porrey/go-native-c-with-the-dht22-a8e8eb
    /// </remarks>
    public sealed partial class MainPage : Page
    {
        private const int TemperatureMonitorIntervalInSeconds = 3;
        private const string ConnectionString = "HostName=TestIoTTempHub.azure-devices.net;DeviceId=temp1;SharedAccessKey=PacKH0LnQy0202U6b7kmX9LkrIES1SorIy1lHKSCQ20=";

        private ThreadPoolTimer _threadPoolTimer;
        private double _temperatureMax = double.MinValue;
        private double _temperatureMin = double.MaxValue;
        private double _temperatureNow;
        private DeviceClient _deviceClient;
#if USE_PI
        private Dht22 _sensor;
#endif
        public MainPage()
        {
            InitializeComponent();
            InitializeBackgroundMonitor();
            InitializeSensor();
        }

        private void InitializeSensor()
        {
#if USE_PI
            var dataPin = GpioController.GetDefault().OpenPin(4, GpioSharingMode.Exclusive);
            dataPin.SetDriveMode(GpioPinDriveMode.Input);

            _sensor = new Dht22(dataPin, GpioPinDriveMode.Input);
#endif
        }

        private void InitializeBackgroundMonitor()
        {
            _deviceClient = DeviceClient.CreateFromConnectionString(ConnectionString, TransportType.Mqtt);
            _threadPoolTimer = ThreadPoolTimer.CreatePeriodicTimer(async source =>
            {
                bool isValid;
#if USE_PI
                var temperatureReading = await _sensor.GetReadingAsync();
                isValid = temperatureReading.IsValid;
                _temperatureNow = temperatureReading.Temperature;
#else
                _temperatureNow = new Random().NextDouble() * (30 - 18) + 18;
                isValid = true;
#endif
                _temperatureMax = Math.Max(_temperatureMax, _temperatureNow);
                _temperatureMin = Math.Min(_temperatureMin, _temperatureNow);

                if (isValid)
                {
                    SendDeviceToCloudMessagesAsync(_temperatureNow).Wait();

                    await Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
                    {
                        TemperatureNow.Text = $"Now: {_temperatureNow:#.##}° C";
                        TemperatureMax.Text = $"High: {_temperatureMax:#.##}°";
                        TemperatureMin.Text = $"Low: {_temperatureMin:#.##}°";
                    });
                }
                else
                {
                    Debug.WriteLine("Temperature reading invalid");
                }

            }, TimeSpan.FromSeconds(TemperatureMonitorIntervalInSeconds));
        }

        private async Task SendDeviceToCloudMessagesAsync(double temperatureNow)
        {
            var telemetryDataPoint = new
            {
                temperature = temperatureNow
            };
            var messageString = JsonConvert.SerializeObject(telemetryDataPoint);
            var message = new Message(Encoding.ASCII.GetBytes(messageString));

            Debug.WriteLine($"Temperature: {_temperatureNow}");
            await _deviceClient.SendEventAsync(message);
        }

        private void Page_Unloaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            _deviceClient?.CloseAsync().Wait();
            _threadPoolTimer?.Cancel();
        }
    }
}
