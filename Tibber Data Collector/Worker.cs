// <copyright file="Worker.cs" company="Kjell Skogsrud">
// Copyright (c) Kjell Skogsrud. BSD 3-Clause License
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using InfluxData.Net.Common.Enums;
using InfluxData.Net.InfluxDb;
using InfluxData.Net.InfluxDb.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tibber.Sdk;

namespace Tibber_Data_Collector
{
    /// <inheritdoc/>
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> logger;
        private IConfiguration appsettings;

        /// <summary>
        /// Initializes a new instance of the <see cref="Worker"/> class.
        /// </summary>
        /// <param name="logger">Pass in the logger.</param>
        /// <param name="configuration">The app configuraiton.</param>
        public Worker(ILogger<Worker> logger, IConfiguration configuration)
        {
            this.logger = logger;
            this.appsettings = configuration;
        }

        /// <inheritdoc/>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Setup InfluxDBClient
            // This is a lazy client and does not connect untill you ask it to actually do something.
            InfluxDbClient influxClient = new InfluxDbClient(
                "http://" + this.appsettings["influxHost"] + ":" + this.appsettings["influxPort"].ToString() + "/",
                this.appsettings["influxUser"],
                this.appsettings["influxPassword"],
                InfluxDbVersion.Latest);

            // this.logger.LogInformation("Lazy Influx Client ready.\n{0}\n{1}\n{2}\n{3}", this.appsettings["influxHost"], this.appsettings["influxPort"], this.appsettings["influxUser"], this.appsettings["influxPassword"]);
            var client = new TibberApiClient(this.appsettings["tibberAPIKey"]);

            var homeId = Guid.Parse(this.appsettings["tibberHomeId"]);

            var listener = await client.StartRealTimeMeasurementListener(homeId);

            listener.Subscribe(new RealTimeMeasurementObserver(this.logger, influxClient, this.appsettings));

            while (!stoppingToken.IsCancellationRequested)
            {
                // this.logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

                await Task.Delay(10000, stoppingToken);
            }

            this.logger.LogWarning("ExecuteAsync Ended, this is a problem");
        }
    }
}
