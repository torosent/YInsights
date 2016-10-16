using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Microsoft.ServiceBus.Messaging;
using Microsoft.Azure;
using ServiceFabric.ServiceBus.Services.CommunicationListeners;
using ServiceFabric.ServiceBus.Services;
using System.Net.Http;
using System.Data.SqlClient;
using System.Data;
using Microsoft.Azure.Documents.Client;
using YInsights.Shared;
using YInsights.Shared.Poco;
using YInsights.Shared.AI;
using YInsights.Shared.Providers;

namespace GetArticle
{
    /// <summary>
    /// An instance of this class is created for each service instance by the Service Fabric runtime.
    /// </summary>
    internal sealed class GetArticle : StatelessService
    {

        internal IDocumentDBProvider documentDB;
        public GetArticle(StatelessServiceContext context,  IDocumentDBProvider documentDB)
            : base(context)
        {   
            this.documentDB = documentDB;
        }

        /// <summary>
        /// Optional override to create listeners (e.g., TCP, HTTP) for this service replica to handle client or user requests.
        /// </summary>
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            string serviceBusQueueName = null;

            yield return new ServiceInstanceListener(context => new ServiceBusQueueCommunicationListener(
             new Handler(this)
             , context
             , serviceBusQueueName
             , requireSessions: false)
            { 
                AutoRenewTimeout = TimeSpan.FromSeconds(70),  //auto renew up until 70s, so processing can take no longer than 60s (default lock duration).
                MessagePrefetchCount = 1
            }, "StatelessService-ServiceBusQueueListener");


        }

        

    }
    internal sealed class Handler : AutoCompleteServiceBusMessageReceiver
    {

        GetArticle _service;
        public Handler(GetArticle service)
        {
            _service = service;

        }
        protected override Task ReceiveMessageImplAsync(BrokeredMessage message, MessageSession messageSession, CancellationToken cancellationToken)
        {
            ProccessStory(message.MessageId);
          //  ServiceEventSource.Current.ServiceMessage(_service.Context, $" Handling subscription message {message.MessageId}");
            return Task.FromResult(true);

        }

        private async void ProccessStory(string storyId)
        {
            try
            {
              
               

                var queryOptions = new FeedOptions { MaxItemCount = -1 };

              

                IQueryable<Article> articleExistQuery = _service.documentDB.Client.CreateDocumentQuery<Article>(
                    UriFactory.CreateDocumentCollectionUri("articles", "article"), queryOptions)
                    .Where(f => f.Id == storyId);

                var list = articleExistQuery.ToList();
                ServiceEventSource.Current.ServiceMessage(_service.Context, $"Processing {storyId}");
                if (!list.Any())
                {
                    using (var client = new HttpClient())
                    {
                        string url = $"https://hacker-news.firebaseio.com/v0/item/{storyId}.json";
                        HttpResponseMessage response = await client.GetAsync(url);
                        // response.EnsureSuccessStatusCode();
                        if (response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            string responseBody = await response.Content.ReadAsStringAsync();
                            dynamic story = (dynamic)Newtonsoft.Json.Linq.JObject.Parse(responseBody);
                            if (!string.IsNullOrEmpty(story.url.ToString()))
                            {
                                InsertStory(story);
                            }
                        }
                        else
                        {
                            string responseBody = await response.Content.ReadAsStringAsync();
                            ServiceEventSource.Current.ServiceMessage(_service.Context, $"{storyId} {responseBody}");
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                ApplicationInsightsClient.LogException(ex);
                ServiceEventSource.Current.ServiceMessage(_service.Context, $"{ex.Message}");
            }
        }
        private async void InsertStory(dynamic story)
        {

          
            var article = new Article()
            {
                Id = story.id.ToString(),
                score = Convert.ToInt32(story.score),
                time = Convert.ToInt32(story.time),
                title = story.title.ToString(),
                url = story.url.ToString()
            };
            await _service.documentDB.Client.CreateDocumentAsync(UriFactory.CreateDocumentCollectionUri("articles", "article"), article);
            ServiceEventSource.Current.ServiceMessage(_service.Context, $"Inserted {article.Id}");     
        }
    }
}
