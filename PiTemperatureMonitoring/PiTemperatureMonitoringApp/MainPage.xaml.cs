using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Windows.System.Threading;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;

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
        private const int TemperatureMonitorIntervalInSeconds = 60;
        private const string ConnectionString = "HostName=L-TemperatureIoTHub.azure-devices.net;DeviceId=TemperatureDevice01;SharedAccessKey=IT4Lt/EUYmn35uHiHQxmgVnbOcvxUeGf7KrDNs0EAXE=";

        private ThreadPoolTimer _threadPoolTimer;
        private double _temperatureMax = double.MinValue;
        private double _temperatureMin = double.MaxValue;
        private DeviceClient _deviceClient;
        private TemperatureSensor _sensor;

        public MainPage()
        {
            InitializeComponent();
            InitializeBackgroundMonitor();
            InitializeSensor();
        }

        private void InitializeSensor()
        {
            _sensor = new TemperatureSensor();
        }

        private void InitializeBackgroundMonitor()
        {
            _deviceClient = DeviceClient.CreateFromConnectionString(ConnectionString, TransportType.Http1);

            _threadPoolTimer = ThreadPoolTimer.CreatePeriodicTimer(async source =>
            {
                var reading = await _sensor.ReadAsync();

                if (reading.IsValid)
                {
                    _temperatureMax = Math.Max(_temperatureMax, reading.Temperature);
                    _temperatureMin = Math.Min(_temperatureMin, reading.Temperature);

                    await SendDeviceToCloudMessageAsync(reading.Temperature);

                    await Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
                    {
                        TemperatureNow.Text = $"Now: {reading.Temperature:#.##}° C";
                        TemperatureMax.Text = $"High: {_temperatureMax:#.##}°";
                        TemperatureMin.Text = $"Low: {_temperatureMin:#.##}°";
                    });
                }
                else
                {
                    await LogWarnMessage("Temperature reading invalid");
                }

            }, TimeSpan.FromSeconds(TemperatureMonitorIntervalInSeconds));
        }

        private async Task SendDeviceToCloudMessageAsync(double temperatureNow)
        {
            try
            {
                var telemetryDataPoint = new
                {
                    temperature = temperatureNow
                };
                var messageString = JsonConvert.SerializeObject(telemetryDataPoint);
                var message = new Message(Encoding.ASCII.GetBytes(messageString));

                await _deviceClient.SendEventAsync(message);
                await LogInfoMessage($"Logging Temperature: {temperatureNow:#.##}");
            }
            catch (Exception e)
            {
                await LogExceptionMessage("Failed to log from device to cloud.", e);
            }
        }

        private async Task LogMessage(string message)
        {
            Trace.WriteLine($"{message}");

            await Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
            {
                if (TemperatureListView.Items?.Count > 30)
                {
                    TemperatureListView.Items?.RemoveAt(0);
                }
                TemperatureListView.Items?.Add(new { Message = message });
            });
        }

        private async Task LogInfoMessage(string message)
        {
            message = $"INFO {message}";
            await LogMessage(message);
        }

        private async Task LogWarnMessage(string message)
        {
            message = $"WARN {message}";
            await LogMessage(message);
        }

        private async Task LogExceptionMessage(string message, Exception exception)
        {
            message = $"EXCEPTION {message}. {exception}";
            await LogMessage(message);
        }

        private void Page_Unloaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            try
            {
                _deviceClient?.CloseAsync().Wait();
                _threadPoolTimer?.Cancel();
            }
            catch (Exception exception)
            {
                Trace.WriteLine($"{exception}\n{exception.StackTrace}");
            }
        }
    }
}
