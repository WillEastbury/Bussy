# Bussy
## BussyMcBusFace's Designs for Campervan Water System
This repo includes the designs for a Raspberry Pi 4 based .net WebServer that can update an Grove LCD RGB based display(I2C) and read a grove water level sensor over the I2C Bus to sense the water level in the clean tank if mains water is not connected to the vehicle.
There is also an optional Pi 3b based Water Sensor device that senses the I2C values of another sensor attached to the Waste Tank (if fitted and connected).

The Pi .net 6-based server receives data from 2x RaspberryPi Pico W devices that have attached buttons for hot and cold water (connected to digital pins on a Grove Pi Pico Shield).

These Pico W devices connect to the server .net minimal API to request hot or cold water on a button press.

There is a third connected PicoPi W device, attached into a Relay Box with 8 attached DC relays which respond to data polled from the buttons to energise the relays to enable the following connectors :-

---------------------------------
- Water Tank Pump
- Hot Water Boiler Solenoid
- Hot Water Kitchen Tap Solenoid
- Hot Water Bathroom Tap Solenoid
- Cold Water Kitchen Tap Solenoid
- Cold Water Bathroom Tap Solenoid

