using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Microsoft.Azure;
using YInsights.Shared.Poco;
using YInsights.Shared.AI;
using YInsights.Shared.Common;
using YInsights.Shared.Providers;
using Microsoft.Azure.Documents.Client;

namespace CalculateTopicsAndTags
{
    /// <summary>
    /// An instance of this class is created for each service instance by the Service Fabric runtime.
    /// </summary>
    internal sealed class CalculateTopicsAndTags : StatelessService
    {

        ICacheProvider cache;
        IDocumentDBProvider documentDB;
        public CalculateTopicsAndTags(StatelessServiceContext context, ICacheProvider cache, IDocumentDBProvider documentDB)
            : base(context)
        {
            this.cache = cache;
            this.documentDB = documentDB;
        }

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
            try
            {
                while (true)
                {

                    cancellationToken.ThrowIfCancellationRequested();
                    CalculateTopics();
                    var minutes = 15;
                    await Task.Delay(TimeSpan.FromMinutes(minutes), cancellationToken);
                }
            }
            catch (Exception ex)
            {
                ApplicationInsightsClient.LogException(ex);

                ServiceEventSource.Current.ServiceMessage(this, ex.Message);

            }
        }

        private void CalculateTopics()
        {


            var articleExistQuery = documentDB.Client.CreateDocumentQuery<Article>(
                UriFactory.CreateDocumentCollectionUri("articles", "article")).Where(f => f.processed == true);


            var tags = new Dictionary<string, int>();
            foreach (var article in articleExistQuery)
            {
                Tags.CalculateTags(tags, article);
            }


            var listTags = tags.OrderByDescending(x => x.Value);

            var list = new List<dynamic>();
            foreach (var tag in listTags)
            {
                list.Add(new { topic = tag.Key, count = tag.Value });
            }

            cache.SetValue("Topics", Newtonsoft.Json.JsonConvert.SerializeObject(list));
            cache.SetValue("WordCloudTopics", Newtonsoft.Json.JsonConvert.SerializeObject(list.Take(1000)));

            ServiceEventSource.Current.ServiceMessage(this, $"Commited {tags.Count} Topics");
            ApplicationInsightsClient.LogEvent($"Commited Topics", tags.Count.ToString());


        }

    }
}
