#!/bin/bash
### Setup Firewall
sudo iptables -A INPUT -p tcp --dport 5000 -j ACCEPT
sudo apt-get update
sudo apt-get install iptables-persistent
sudo service netfilter-persistent start

### Publish the app 
sudo systemctl stop Bussy
cd /home/will/web/BussyServer 
sudo mkdir /srv/Bussy
sudo chown will /srv/Bussy
dotnet publish -c Release -o /srv/Bussy
sudo cp Bussy.service /etc/systemd/system/Bussy.service

### Restart the server
sudo systemctl daemon-reload
sudo systemctl start Bussy