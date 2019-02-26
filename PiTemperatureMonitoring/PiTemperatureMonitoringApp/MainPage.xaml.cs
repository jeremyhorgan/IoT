using System;
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
        private const int TemperatureMonitorIntervalInSeconds = 1;

        private ThreadPoolTimer _threadPoolTimer;
        private double _temperatureMax = double.MinValue;
        private double _temperatureMin = double.MaxValue;
        private double _temperatureNow;
#if USE_PI
        private Dht11 _dht11;
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
            var gpioPin4 = GpioController.GetDefault().OpenPin(4, GpioSharingMode.Exclusive);
            _dht11 = new Dht11(gpioPin4, GpioPinDriveMode.Input);
#endif
        }

        private void InitializeBackgroundMonitor()
        {
            _threadPoolTimer = ThreadPoolTimer.CreatePeriodicTimer(async source =>
            {
#if USE_PI
                var temperatureReading = await _dht11.GetReadingAsync();
                _temperatureNow = temperatureReading.Temperature;
#else
                _temperatureNow = new Random().NextDouble() * (30 - 18) + 18;
#endif
                _temperatureMax = Math.Max(_temperatureMax, _temperatureNow);
                _temperatureMin = Math.Min(_temperatureMin, _temperatureNow);

                await Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
                {
                    TemperatureNow.Text = $"Now: {_temperatureNow:#.##}° C";
                    TemperatureMax.Text = $"High: {_temperatureMax:#.##}°";
                    TemperatureMin.Text = $"Low: {_temperatureMin:#.##}°";
                });

            }, TimeSpan.FromSeconds(TemperatureMonitorIntervalInSeconds));
        }

        private void Page_Unloaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            _threadPoolTimer?.Cancel();
        }
    }
}
