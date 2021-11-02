// <copyright file="RealTimeMeasurementObserver.cs" company="Kjell Skogsrud">
// Copyright (c) Kjell Skogsrud. BSD 3-Clause License
// </copyright>

using System;
using System.Collections.Generic;
using InfluxData.Net.InfluxDb;
using InfluxData.Net.InfluxDb.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Tibber.Sdk;

namespace Tibber_Data_Collector
{
    /// <summary>
    /// The realtime measurement observer.
    /// </summary>
    public class RealTimeMeasurementObserver : IObserver<RealTimeMeasurement>
    {
        private readonly ILogger<Worker> logger;
        private InfluxDbClient influxDbClient;
        private IConfiguration appsettings;
        private int signalStrenght = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="RealTimeMeasurementObserver"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="influxDbClient">The influxDB client.</param>
        /// <param name="appsettings">A referance to the Appsettings object.</param>
        public RealTimeMeasurementObserver(ILogger<Worker> logger, InfluxDbClient influxDbClient, IConfiguration appsettings)
        {
            this.logger = logger;
            this.influxDbClient = influxDbClient;
            this.logger.LogInformation("RealTimeMeasurementObserver was made");
            this.appsettings = appsettings;
        }

        /// <inheritdoc/>
        public void OnCompleted() => this.logger.LogInformation("Real time measurement stream has been terminated. ");

        /// <inheritdoc/>
        public void OnError(Exception error) => this.logger.LogInformation($"An error occured: {error}");

        /// <inheritdoc/>
        public void OnNext(RealTimeMeasurement value)
        {
            Point point = new Point();
            point.Name = "pulse";
            point.Tags = new Dictionary<string, object>()
            {
                { "home", this.appsettings["tibberHomeId"] },
            };
            point.Fields = new Dictionary<string, object>()
            {
                { "time", value.Timestamp.ToUnixTimeMilliseconds() },
                { "power", value.Power },
                { "accumulatedConsumption", value.AccumulatedConsumption },
                { "accumulatedCost", value.AccumulatedCost },
                { "minPower", value.MinPower },
                { "averagePower", value.AveragePower },
                { "maxPower", value.MaxPower },
                { "currentPhase1", value.CurrentPhase1 },
                { "currentPhase2", value.CurrentPhase2 },
                { "currentPhase3", value.CurrentPhase3 },
                { "voltagePhase1", value.VoltagePhase1 },
                { "voltagePhase2", value.VoltagePhase2 },
                { "voltagePhase3", value.VoltagePhase3 },
                { "powerFactor", value.PowerFactor },
            };

            if (value.SignalStrength != null)
            {
                point.Fields.Add("signalStrength", value.SignalStrength);
                this.logger.LogInformation("signalStrength was {0}", value.SignalStrength);
                this.signalStrenght = (int)value.SignalStrength;
            }
            else
            {
                point.Fields.Add("signalStrength", this.signalStrenght);
            }

            // this.logger.LogInformation($"{value.Timestamp} - power: {value.Power:N0} W (average: {value.AveragePower:N0} W); consumption since last midnight: {value.AccumulatedConsumption:N3} kWh; cost since last midnight: {value.AccumulatedCost:N2} {value.Currency}");

            var result = this.influxDbClient.Client.WriteAsync(point, this.appsettings["influxDatabase"]).GetAwaiter().GetResult();

            if (!result.Success)
            {
                this.logger.LogError("Failed to write to InfluxDB");
            }
        }
    }
}
