using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Microsoft.Azure;
using YInsights.Shared.AI;
using StackExchange.Redis;
using Microsoft.Azure.Documents.Client;
using YInsights.Shared.Poco;
using YInsights.Shared.Extentions;
using YInsights.Shared.Common;

namespace TrendingTopics
{
    /// <summary>
    /// An instance of this class is created for each service instance by the Service Fabric runtime.
    /// </summary>
    internal sealed class TrendingTopics : StatelessService
    {
        string EndpointUri = CloudConfigurationManager.GetSetting("DocumentDBUri");
        string PrimaryKey = CloudConfigurationManager.GetSetting("DocumentDBKey");
        string RedisConnection = CloudConfigurationManager.GetSetting("RedisConnection");
        IDatabase Database;

        public TrendingTopics(StatelessServiceContext context)
            : base(context)
        { }

        /// <summary>
        /// Optional override to create listeners (e.g., TCP, HTTP) for this service replica to handle client or user requests.
        /// </summary>
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            return new ServiceInstanceListener[0];
        }

        /// <summary>
        /// This is the main entry point for your service instance.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service instance.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            ConnectionMultiplexer redis = ConnectionMultiplexer.Connect(RedisConnection);
            Database = redis.GetDatabase();

            while (true)
            {

                cancellationToken.ThrowIfCancellationRequested();
                CalculateTrendingTopics();
                var minutes = 15;
                await Task.Delay(TimeSpan.FromMinutes(minutes), cancellationToken);

            }
        }

        private async void CalculateTrendingTopics()
        {
            try
            {
                var docclient = new DocumentClient(new Uri(EndpointUri), PrimaryKey, new ConnectionPolicy
                {
                    ConnectionMode = ConnectionMode.Direct,
                    ConnectionProtocol = Protocol.Tcp
                });


                var currentDate = DateTime.Now.AddDays(-2).DateTimeToUnixTimestamp();

                await docclient.OpenAsync();
                var articleExistQuery = docclient.CreateDocumentQuery<Article>(
                    UriFactory.CreateDocumentCollectionUri("articles", "article")).Where(f => f.processed == true && f.time > currentDate);


                var tags = new Dictionary<string, int>();
                foreach (var article in articleExistQuery)
                {
                    Tags.CalculateTags(tags, article, false);
                }


                var listTags = tags.OrderByDescending(x => x.Value);

                var list = new List<dynamic>();
                foreach (var tag in listTags)
                {
                    list.Add(new { topic = tag.Key, count = tag.Value });
                }

                await Database.StringSetAsync("TrendingTopics", Newtonsoft.Json.JsonConvert.SerializeObject(list.Take(5)));

                ServiceEventSource.Current.ServiceMessage(this, $"Commited Trending {tags.Count} Topics");
                ApplicationInsightsClient.LogEvent($"Commited Trending Topics", tags.Count.ToString());
            }
            catch (Exception ex)
            {
                ApplicationInsightsClient.LogException(ex);

                ServiceEventSource.Current.ServiceMessage(this, ex.Message);

            }

        }
    }


}


