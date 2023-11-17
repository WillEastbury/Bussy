#!/bin/bash
### Publish the app 
cd /home/will/web/DisplayChannel
sudo mkdir /srv/TankSensorsClean
sudo chown will /srv/TankSensorsClean
dotnet publish -c Release -o /srv/TankSensorsClean
sudo cp DisplayChannel.service /etc/systemd/system/TankSensorsClean.service

### Restart the server
sudo systemctl daemon-reload
sudo systemctl start TankSensorsClean