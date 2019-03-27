using System;
using System.Threading.Tasks;
#if USE_PI
using Windows.Devices.Gpio;
using Sensors.Dht;
#endif

namespace PiTemperatureMonitoringApp
{
    internal class TemperatureSensor
    {
#if USE_PI
        private Dht22 _sensor;
#endif

        public TemperatureSensor()
        {
#if USE_PI
            var dataPin = GpioController.GetDefault().OpenPin(4, GpioSharingMode.Exclusive);
            dataPin.SetDriveMode(GpioPinDriveMode.Input);

            _sensor = new Dht22(dataPin, GpioPinDriveMode.Input);
#endif
        }

        internal async Task<(bool IsValid, double Temperature)> ReadAsync()
        {
#if USE_PI
            var temperatureReading = await _sensor.GetReadingAsync();
            var isValid = temperatureReading.IsValid;
            var temperatureNow = temperatureReading.Temperature;

            return (isValid, temperatureNow);
#else
            var temperatureNow = new Random().NextDouble() * (30 - 18) + 18;
            return await Task.FromResult((true, temperatureNow));
#endif
        }
    }
}
