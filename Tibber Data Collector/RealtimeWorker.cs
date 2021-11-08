// <copyright file="RealtimeWorker.cs" company="Kjell Skogsrud">
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
    public class RealtimeWorker : BackgroundService
    {
        private readonly ILogger<RealtimeWorker> logger;
        private IConfiguration appsettings;

        /// <summary>
        /// Initializes a new instance of the <see cref="RealtimeWorker"/> class.
        /// </summary>
        /// <param name="logger">Pass in the logger.</param>
        /// <param name="configuration">The app configuraiton.</param>
        public RealtimeWorker(ILogger<RealtimeWorker> logger, IConfiguration configuration)
        {
            this.logger = logger;
            this.appsettings = configuration;
        }

        /// <inheritdoc/>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            this.logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

            // Setup InfluxDBClient
            // This is a lazy client and does not connect untill you ask it to actually do something.
            InfluxDbClient influxClient = new InfluxDbClient(
                "http://" + this.appsettings["influxHost"] + ":" + this.appsettings["influxPort"].ToString() + "/",
                this.appsettings["influxUser"],
                this.appsettings["influxPassword"],
                InfluxDbVersion.Latest);

            TibberApiClient client = new TibberApiClient(this.appsettings["tibberAPIKey"]);
            Guid homeId = Guid.Parse(this.appsettings["tibberHomeId"]);
            var listener = await client.StartRealTimeMeasurementListener(homeId);
            RealTimeMeasurementObserver rtmo = new RealTimeMeasurementObserver(this.logger, influxClient, this.appsettings);
            listener.Subscribe(rtmo);

            while (!stoppingToken.IsCancellationRequested)
            {
                if (rtmo.HasCompleted || rtmo.HasHadError)
                {
                    rtmo = new RealTimeMeasurementObserver(this.logger, influxClient, this.appsettings);
                    listener.Subscribe(rtmo);
                }

                await Task.Delay(10000, stoppingToken);
            }

            this.logger.LogWarning("ExecuteAsync Ended. This should not happen unless it was stopped.");
        }
    }
}
