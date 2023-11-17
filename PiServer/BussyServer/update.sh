#!/bin/bash
## Stop the server
sudo systemctl stop Bussy
### Publish the app 
cd /home/will/web/BussyServer 
dotnet restore
dotnet build -c Release
dotnet publish -c Release -o /srv/Bussy
sudo cp index.html /srv/Bussy
### Restart the server
sudo systemctl start Bussy