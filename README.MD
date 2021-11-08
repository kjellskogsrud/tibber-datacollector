# Tibber Data collector
This application reads Realtime measurements from your Tibber pulse via the [Tibber API](https://developer.tibber.com/explorer), and writes datapoints to influxdb.

Us together with something like Grafana to make dashboards:
![tibber_dashboard](https://user-images.githubusercontent.com/5667385/140745787-6ebfbdbf-1218-4862-b743-38f6bc25b277.PNG)

Download and build your own, or just rund it from docker with this docker-compose file:

``` yaml
---
version: "3.8"
services:
  tibber-grutle3:
    container_name: tibber-pulse-collector
    image: tibberdatacollector
    build:
        context: .
        dockerfile: ./Tibber Data Collector/Dockerfile
    environment:
      - influxHost=your_host
      - influxPort=8086
      - influxUser=your_user
      - influxPassword=your_password
      - influxDatabase=tibber
      - tibberAPIKey=your_token_here
      - tibberHomeId=your_tibber_homeid
```

Please note that this does not make the influx database for you, so you will have to create that first:

``` shell-session
> CREATE DATABASE tibber;
```
