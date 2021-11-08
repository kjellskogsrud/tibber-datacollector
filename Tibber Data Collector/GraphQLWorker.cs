// <copyright file="GraphQLWorker.cs" company="Kjell Skogsrud">
// Copyright (c) Kjell Skogsrud. BSD 3-Clause License
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
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
    public class GraphQLWorker : BackgroundService
    {
        private readonly ILogger<GraphQLWorker> logger;
        private IConfiguration appsettings;

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphQLWorker"/> class.
        /// </summary>
        /// <param name="logger">Pass in the logger.</param>
        /// <param name="configuration">The app configuraiton.</param>
        public GraphQLWorker(ILogger<GraphQLWorker> logger, IConfiguration configuration)
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

            while (!stoppingToken.IsCancellationRequested)
            {
                this.logger.LogInformation("GraphQLWorker running at: {time}", DateTimeOffset.Now);

                var client = new TibberApiClient(this.appsettings["tibberAPIKey"]);

                var basicData = await client.Query(@"{
                viewer {
                    homes {
                        currentSubscription{
                            priceInfo{
                                current{
                                    total
                                    energy
                                    tax
                                    startsAt
                                }
                                today {
                                    total
                                    energy
                                    tax
                                    startsAt
                                }
                                tomorrow {
                                    total
                                    energy
                                    tax
                                    startsAt
                                }
                            }
                        }
                    }
                }
            }
            ");

                List<Point> pricePoints = new List<Point>();

                // Read todays Price points
                foreach (Price p in basicData.Data.Viewer.Homes.First().CurrentSubscription.PriceInfo.Today)
                {
                    Point pricePoint = new Point();
                    pricePoint.Name = "energyPrice";
                    pricePoint.Fields = new Dictionary<string, object>();
                    DateTimeOffset startsAt = DateTimeOffset.Parse(p.StartsAt);
                    pricePoint.Timestamp = startsAt.DateTime;
                    pricePoint.Fields.Add("total", p.Total);
                    pricePoint.Fields.Add("tax", p.Tax);
                    pricePoint.Fields.Add("energy", p.Energy);

                    pricePoints.Add(pricePoint);
                }

                // Read tomorrows Price points
                foreach (Price p in basicData.Data.Viewer.Homes.First().CurrentSubscription.PriceInfo.Tomorrow)
                {
                    Point pricePoint = new Point();
                    pricePoint.Name = "energyPrice";
                    pricePoint.Fields = new Dictionary<string, object>();
                    DateTimeOffset startsAt = DateTimeOffset.Parse(p.StartsAt);
                    pricePoint.Timestamp = startsAt.DateTime;
                    pricePoint.Fields.Add("total", p.Total);
                    pricePoint.Fields.Add("tax", p.Tax);
                    pricePoint.Fields.Add("energy", p.Energy);

                    pricePoints.Add(pricePoint);
                }

                var result = influxClient.Client.WriteAsync(pricePoints, this.appsettings["influxDatabase"]).GetAwaiter().GetResult();

                if (!result.Success)
                {
                    this.logger.LogError("Failed to write to InfluxDB");
                }

                // Wait 5 minutes
                await Task.Delay(5 * 60000, stoppingToken);
            }
        }
    }
}
