#!/bin/bash
### Publish the app 
cd /home/will/web/DisplayChannel
dotnet publish -c Release -o /srv/TankSensorsClean
sudo cp DisplayChannel.service /etc/systemd/system/TankSensorsClean.service

### Restart the server
sudo systemctl daemon-reload
sudo systemctl stop TankSensorsClean
sudo systemctl start TankSensorsClean