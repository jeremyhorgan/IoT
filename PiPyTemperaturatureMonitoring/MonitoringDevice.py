import random
import time
import sys
import argparse
import Adafruit_DHT

# Using the Python Device SDK for IoT Hub:
#   https://github.com/Azure/azure-iot-sdk-python
# The sample connects to a device-specific MQTT endpoint on your IoT Hub.
import iothub_client

# pylint: disable=E0611
from iothub_client import IoTHubClient, IoTHubClientError, IoTHubTransportProvider, IoTHubClientResult
from iothub_client import IoTHubMessage, IoTHubMessageDispositionResult, IoTHubError, DeviceMethodReturnValue

# The device connection string to authenticate the device with your IoT hub.
CONNECTION_STRING = "HostName=TestIoTTempHub.azure-devices.net;DeviceId=temp1;SharedAccessKey=PacKH0LnQy0202U6b7kmX9LkrIES1SorIy1lHKSCQ20="

# Using the MQTT protocol.
PROTOCOL = IoTHubTransportProvider.MQTT
MESSAGE_TIMEOUT = 10000

# Define the JSON message to send to IoT Hub.
TEMPERATURE = 20.0
HUMIDITY = 60
MESSAGE_FORMAT = "{\"temperature\": %.2f,\"humidity\": %.2f}"

def send_confirmation_callback(message, result, user_context):
    print ( "IoT Hub responded to message with status: %s" % (result) )

def iothub_client_init():
    # Create an IoT Hub client
    client = IoTHubClient(CONNECTION_STRING, PROTOCOL)
    return client

def measure_temperature():
  temperature = TEMPERATURE + (random.random() * 15)
  humidity = HUMIDITY + (random.random() * 20)
  return temperature, humidity

def iothub_temperature_measurement_client(interval):

    try:
        client = iothub_client_init()
        print("IoT Hub device sending periodic messages, press Ctrl-C to exit")

        while True:
            temperature, humidity = measure_temperature()
            
            message_text_formatted = MESSAGE_FORMAT % (temperature, humidity)
            message = IoTHubMessage(message_text_formatted)

            # Send the message.
            print("Sending message: %s" % message.get_string())
            client.send_event_async(message, send_confirmation_callback, None)
            time.sleep(interval)

    except IoTHubError as iothub_error:
        print("Unexpected error %s from IoTHub" % iothub_error)
        return
    except KeyboardInterrupt:
        print("IoTHubClient sample stopped")

if __name__ == '__main__':
    print("IoT Hub Temperature Measurement - PI device")
    print("Press Ctrl-C to exit")

    parser = argparse.ArgumentParser()
    parser.add_argument("--interval", help="specifies the time interval between temperature samples, defaults to 5 seconds")
    args = parser.parse_args()

    interval = 5
    if args.interval:
      interval = int(args.interval)
    
    print("time interval between temperature is %d seconds" % interval)

    iothub_temperature_measurement_client(interval)