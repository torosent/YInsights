using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using System.Net.Http;
using Microsoft.ServiceBus.Messaging;
using ServiceFabric.ServiceBus.Clients;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Communication.Client;
using Microsoft.Azure;
using YInsights.Shared.AI;

namespace GetArticles
{
    /// <summary>
    /// An instance of this class is created for each service instance by the Service Fabric runtime.
    /// </summary>
    internal sealed class GetArticles : StatelessService
    {
        public GetArticles(StatelessServiceContext context)
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
           
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                GetArticlesFromHN();

                await Task.Delay(TimeSpan.FromMinutes(15), cancellationToken);
            }
        }

        private async void GetArticlesFromHN()
        {
            try
            {
                var responseBody = string.Empty;
                using (var client = new HttpClient())
                {
                    HttpResponseMessage response = await client.GetAsync("https://hacker-news.firebaseio.com/v0/newstories.json");
                    response.EnsureSuccessStatusCode();
                    responseBody = await response.Content.ReadAsStringAsync();
                }

                var stories = (string[])Newtonsoft.Json.JsonConvert.DeserializeObject(responseBody, typeof(string[]));
                foreach (var storyId in stories)
                {
                    SendToServiceBus(storyId);

                }
            }
            catch (Exception ex)
            {
                ApplicationInsightsClient.LogException(ex);

                ServiceEventSource.Current.ServiceMessage(this.Context, $"{ex.Message}");
            }


        }

        private void SendToServiceBus(string storyId)
        {
            var uri = new Uri("fabric:/YInsights/GetArticle");
            var resolver = ServicePartitionResolver.GetDefault();
            string serviceBusQueueName = null;
            var factory = new ServiceBusQueueCommunicationClientFactory(resolver, serviceBusQueueName);
            var servicePartitionClient = new ServicePartitionClient<ServiceBusQueueCommunicationClient>(factory, uri);
            servicePartitionClient.InvokeWithRetry(c => c.SendMessage(CreateMessage(storyId, storyId)));
            ServiceEventSource.Current.ServiceMessage(this.Context, $" Sending {storyId} to be processed");
            ApplicationInsightsClient.LogEvent("Send to process",storyId);

        }

        private BrokeredMessage CreateMessage(string messageId, string messageBody)
        {
           
            var message = new BrokeredMessage(messageBody);
            message.MessageId = messageId;
            return message;
        }

    }
}
