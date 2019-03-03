using System;
using System.Diagnostics;
using Windows.System.Threading;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
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

        private ThreadPoolTimer _threadPoolTimer;
        private double _temperatureMax = double.MinValue;
        private double _temperatureMin = double.MaxValue;
        private double _temperatureNow;
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

                Debug.WriteLine($"Temperature: {_temperatureNow} isValid: {isValid}");

                if (isValid)
                {
                    await Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
                    {
                        TemperatureNow.Text = $"Now: {_temperatureNow:#.##}° C";
                        TemperatureMax.Text = $"High: {_temperatureMax:#.##}°";
                        TemperatureMin.Text = $"Low: {_temperatureMin:#.##}°";
                    });
                }

            }, TimeSpan.FromSeconds(TemperatureMonitorIntervalInSeconds));
        }

        private void Page_Unloaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            _threadPoolTimer?.Cancel();
        }
    }
}
