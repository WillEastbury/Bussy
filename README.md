# Bussy McBusFace
## Smart Taps (Campervan Water System)

If you've ever used the pressure-switched type of water system in a campervan / RV, you'll know how much of a pain they are to calibrate and use. So I snapped one day and designed my own Smart Water System for my Campervan. 

---------------------------------

This repo includes the designs for a Raspberry Pi 4 based .net WebServer that can update an Grove LCD RGB based display(I2C) and read 2x Grove ultrasonic ranger sensors to sense the water level in the clean tank and grey waste tank, if mains water is not connected to the vehicle (Sensed via a gpio switch).

The Pi .net 6-based server receives data from 2x RaspberryPi Pico W devices that have attached buttons for hot and cold water (Connected to digital LED buttons on a Grove Pi Pico Shield).

These Pico W devices connect to the server .net minimal API to request hot or cold water on a button press.

There is a third connected PicoPi W device, attached into a Relay Box with 8 attached DC relays which respond to data polled from the buttons to energise the relays to enable the following 12V supply connectors :-

There is an LPG gas boiler connected through the Hot Water Boiler Enabled Solenoid to provide on-demand hot water. 

---------------------------------
- Water Tank Pump
- Hot Water Boiler Enabled Solenoid
- Hot Water Kitchen Tap Solenoid
- Hot Water Bathroom Tap Solenoid
- Cold Water Kitchen Tap Solenoid
- Cold Water Bathroom Tap Solenoid
---------------------------------

In the repo, I provide Arduino Sketches for the picos, .net project source code for 2x .net Daemons (1x for the Webserver control panel, one for the Display Output on the Pi). 

To rig the entire system you will need the following parts.

### Plumbing
- 5x 15mm DN15 12V Water Solenoids with check valve - https://thepihut.com/products/12v-solenoid-valve-1-2
- 1x 12v Submersible Water Pump (I used a Whale pump) - https://www.eurocarparts.com/p/whale-standard-12v-submersible-electric-pump-white-10-litres-gp1002-560773980
- 2x Fiamma 23L Tanks (1 blue (fresh) and 1 grey (waste)) - https://www.halfords.com/camping/water-and-waste/fiamma-roll-tank-23l-fresh-483158.html
- 2x 15mm Pushfit elbows (JG Speedfit)
- 5x 15mm Pushfit Equal Tees (JG Speedfit)
- 1x 10mm to 15mm Pushfit reducing coupler (JG Speedfit) 
- 2x 3m Some 15mm pex barrier pipe (JG Speedfit)
- 1x 3m 15mm Chrome Pipe 
- 4x 15mm Compression fit Chrome elbows
- 2x 15mm Compression fit Shower check valve 
- 4x 15mm to 15mm flexible hose (Push fit)

### External Water Plumbing
- Lengths of drainage pipe (25 / 35 mm and elbows + 1x equal tee) for drains
- 1x Hose Pipe to tap fitting
- 1x 12mm Hose to 15mm push fit adapter
- 1x 12mm Jubilee Clip

### Power
- 1x Campervan 12v supply or external 12v battery
- 1x Kohree 12 way 12V Fuse Box - https://www.amazon.co.uk/dp/B082KLBGGZ?psc=1&ref=ppx_yo2ov_dt_b_product_details
- 3x JZK 12V to Dual USB PSU - https://www.amazon.co.uk/dp/B06XSCCLCD?psc=1&ref=ppx_yo2ov_dt_b_product_details
- 1x box 18AWG hook up wire 
  
## Gas [Warning - get a GasSafe registered installer if you intend on a permanent install]
- 1x A gas boiler (10L CO-Z is the one I used), professionally installed and connected to an LPG tank
  - https://www.amazon.co.uk/CO-Z-Stainless-Liquefied-Petroleum-Certified/dp/B08YR3SXWY?th=1
- Or a simple gas bottle (Calor or CampingGaz) if you intend to put the boiler outside with a regulator and hose

### Compute
- 1x Raspberry Pi 4B (with Raspbian Linux installed, Wireless connected, and I2C enabled) - https://www.raspberrypi.com/products/raspberry-pi-4-model-b/
- 3x Raspberry Pi Pico WH (And the Arduino IDE installed and configured for Pi Pico W on your PC) - https://www.adafruit.com/product/5544
  
### Sensors and Actuators
- 1x GrovePi+ Shield (Optional, if you don't have one of these then you can wire up the I2C direct to the pins) - https://www.unmannedtechshop.co.uk/product/grovepi/
- 3x Grove Pico Shield - https://thepihut.com/products/grove-shield-for-raspberry-pi-pico-v1-0?variant=41952985448643&currency=GBP&utm_medium=product_sync&utm_source=bing&utm_content=sag_organic&utm_campaign=sag_organic&msclkid=915231ca1f341bec0c0b549d808df1c0&utm_term=4585375811816292
- 2x Grove Ultrasonic Ranger Sensor - https://thepihut.com/products/dexter-grovepi-and-gopigo-ultrasonic-sensor
- 2x Grove LED Button - Red - https://uk.rs-online.com/web/p/hmi-development-tools/1887114?cm_mmc=UK-PLA-DS3A-_-bing-_-PLA_UK_EN_Catch+All-_-Electronic+Components,+Power+%26+Connectors-_-1887114&matchtype=e&pla-4574724306713135&cq_src=google_ads&cq_cmp=554644865&cq_term=&cq_plac=&cq_net=o&cq_plt=gp&gclid=482ae6ac33451e2dde7c83fb93661a1b&gclsrc=3p.ds&msclkid=482ae6ac33451e2dde7c83fb93661a1b
- 2x Grove 10 Segment LED Display 
- 2x Grove LED Button - Blue
- 2x Grove 0.96" OLED Yellow and Blue Display (SSD1315) - https://thepihut.com/products/grove-0-96-oled-yellow-blue-display-ssd1315
- 1x Grove LCD Backlight Display - https://uk.farnell.com/seeed-studio/104030001/grove-lcd-rgb-backlight-display/dp/3932101?gross_price=true&msclkid=710062e1fa2e1b49f7d207b65a1be06d&CMP=KNC-MUK-GEN-SHOPPING-ALL-PRODUCTS-TEST1611
- 1x Grove Switch (P) - https://wiki.seeedstudio.com/Grove-Switch-P/
- 1x Waveshare Pico Relay B - https://www.waveshare.com/wiki/Pico-Relay-B
