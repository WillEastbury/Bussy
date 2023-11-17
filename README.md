# Bussy
## BussyMcBusFace's Designs for Campervan Water System
This repo includes the designs for a Raspberry Pi 4 based .net WebServer that can update an Grove LCD RGB based display(I2C) and read 2x Grove ultrasonic ranger sensors to sense the water level in the clean tank and grey waste tank, if mains water is not connected to the vehicle (Sensed via a gpio switch).

The Pi .net 6-based server receives data from 2x RaspberryPi Pico W devices that have attached buttons for hot and cold water (Connected to digital LED buttons on a Grove Pi Pico Shield).

These Pico W devices connect to the server .net minimal API to request hot or cold water on a button press.

There is a third connected PicoPi W device, attached into a Relay Box with 8 attached DC relays which respond to data polled from the buttons to energise the relays to enable the following connectors :-

There is an LPG gas boiler connected through the Hot Water Boiler Enabled Solenoid to provide on-demand hot water. 
---------------------------------
- Water Tank Pump
- Hot Water Boiler Enabled Solenoid
- Hot Water Kitchen Tap Solenoid
- Hot Water Bathroom Tap Solenoid
- Cold Water Kitchen Tap Solenoid
- Cold Water Bathroom Tap Solenoid

In the repo I provide Arduino Sketches for the picos, 2x .net Daemons (1x for the Webserver control panel, one for the Display Output on the Pi). 

