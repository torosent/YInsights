using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Microsoft.Azure.Documents.Client;
using System.Net.Http;
using YInsights.Shared;
using Newtonsoft.Json.Linq;
using Microsoft.Azure;
using YInsights.Shared.Poco;
using YInsights.Shared.AI;
using YInsights.Shared.Providers;

namespace CalaisTopicAndTags
{
    /// <summary>
    /// An instance of this class is created for each service replica by the Service Fabric runtime.
    /// </summary>
    internal sealed class CalaisTopicAndTags : StatefulService
    {
        private const string URL = "https://api.thomsonreuters.com/permid/calais";

        string apitoken = CloudConfigurationManager.GetSetting("ApiToken");
        IDocumentDBProvider documentDB;
       
        public CalaisTopicAndTags(StatefulServiceContext context, IDocumentDBProvider documentDB)
            : base(context)
        {
            this.documentDB = documentDB;
           
        }

        /// <summary>
        /// Optional override to create listeners (e.g., HTTP, Service Remoting, WCF, etc.) for this service replica to handle client or user requests.
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return new ServiceReplicaListener[0];
        }

        /// <summary>
        /// This is the main entry point for your service replica.
        /// This method executes when this replica of your service becomes primary and has write status.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service replica.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Task.Run(() =>
           {
               ProcessArticles();
           }, cancellationToken);

            try
            {
                while (true)
                {

                    ServiceEventSource.Current.ServiceMessage(this, "GetPreProcessedArticles 20 sec Cycle");

                    GetPreProcessedArticles();
                    await Task.Delay(TimeSpan.FromSeconds(15), cancellationToken);
                }
            }
            catch (Exception ex)
            {
                ApplicationInsightsClient.LogException(ex);
                ServiceEventSource.Current.ServiceMessage(this, ex.Message);
            }
        }


        private async void GetPreProcessedArticles()
        {

            var articleExistQuery = documentDB.Client.CreateDocumentQuery<Article>(
                UriFactory.CreateDocumentCollectionUri("articles", "article")).Where(f => f.processed == false).Take(5);


            var articlesDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, Article>>("articlesDictionary");
            var articlesQueue = await this.StateManager.GetOrAddAsync<IReliableQueue<string>>("articlesQueue");

            using (var tx = this.StateManager.CreateTransaction())
            {
                foreach (var item in articleExistQuery)
                {
                    var contains = await articlesDictionary.ContainsKeyAsync(tx, item.Id);
                    if (!contains)
                    {
                        await articlesDictionary.TryAddAsync(tx, item.Id, item);
                        await articlesQueue.EnqueueAsync(tx, item.Id);
                        ServiceEventSource.Current.ServiceMessage(this, item.Id);
                        ApplicationInsightsClient.LogEvent($"Preprocess", item.Id);

                    }
                }
                await tx.CommitAsync();
            }
        }


        private async void ProcessArticles()
        {


            var articlesDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, Article>>("articlesDictionary");
            var articlesQueue = await this.StateManager.GetOrAddAsync<IReliableQueue<string>>("articlesQueue");

            while (true)
            {
                try
                {
                    using (var tx = this.StateManager.CreateTransaction())
                    {
                        var id = await articlesQueue.TryDequeueAsync(tx);
                        if (id.HasValue)
                        {
                            var article = await articlesDictionary.TryGetValueAsync(tx, id.Value);
                            if (article.HasValue)
                            {
                                var result = ProcessArticle(article.Value).Result;
                                if (result)
                                {
                                    article.Value.processed = true;
                                    var upsertResult = await documentDB.Client.UpsertDocumentAsync(UriFactory.CreateDocumentCollectionUri("articles", "article"), article.Value);
                                   
                                    ServiceEventSource.Current.ServiceMessage(this, $"Updated article {article.Value.Id}");
                                    ApplicationInsightsClient.LogEvent($"Updated article", article.Value.Id);

                                }
                                else
                                {
                                    var count = await articlesDictionary.GetCountAsync(tx);
                                    ServiceEventSource.Current.ServiceMessage(this, $"Result for {article.Value.Id} is false. List contains {count}");
                                    ApplicationInsightsClient.LogEvent("Analyze Failed", article.Value.Id);
                                }
                                await articlesDictionary.TryRemoveAsync(tx, article.Value.Id);
                            }
                            await tx.CommitAsync();
                        }

                    }
                }

                catch (Exception ex)
                {
                    ServiceEventSource.Current.ServiceMessage(this, ex.Message, ex);
                    ApplicationInsightsClient.LogException(ex);

                }
            }

        }



        private async Task<bool> ProcessArticle(Article article)
        {
            try
            {
                HttpClient client = new HttpClient();

                client.DefaultRequestHeaders.TryAddWithoutValidation("ContentType", "text/raw");
                client.DefaultRequestHeaders.TryAddWithoutValidation("X-AG-Access-Token", apitoken);
                client.DefaultRequestHeaders.TryAddWithoutValidation("outputformat", "application/json");

                var response = client.PostAsync(URL, new StringContent(article.title)).Result;
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    ParseTopics(article, responseBody);
                    ServiceEventSource.Current.ServiceMessage(this, $"Parsed article {article.Id}");
                    return true;
                }
                ApplicationInsightsClient.LogEvent($"Process fail", article.Id, response.StatusCode.ToString(), response.Content.ReadAsStringAsync().Result);
                ServiceEventSource.Current.ServiceMessage(this, $"Error with status code {article.Id} {response.StatusCode.ToString()} -  {response.Content.ReadAsStringAsync().Result}");
                return false;
            }
            catch (Exception ex)
            {
                ServiceEventSource.Current.ServiceMessage(this, ex.Message, ex);
                ApplicationInsightsClient.LogException(ex);
                return false;
            }

        }

        private static void ParseTopics(Article article, string responseBody)
        {
            JObject dynamicResult = (JObject)Newtonsoft.Json.Linq.JObject.Parse(responseBody);

            var topics = new List<string>();
            var tags = new List<string>();
            foreach (JToken token in dynamicResult.Children())
            {
                if (token is JProperty)
                {
                    var prop = token as JProperty;
                    if (prop != null)
                    {
                        var values = prop.Children()["_typeGroup"].Values();
                        if (values.Count() > 0)
                        {
                            var value = values.FirstOrDefault();
                            if (value != null && value.Value<string>() == "topics")
                            {
                                var name = prop.Children()["name"].Values().FirstOrDefault().Value<string>();
                                topics.Add(name);
                            }
                            if (value != null && value.Value<string>() == "socialTag")
                            {
                                var name = prop.Children()["name"].Values().FirstOrDefault().Value<string>();
                                tags.Add(name);
                            }
                        }

                    }
                }
            }
            article.tags = tags;
            article.topics = topics;
            if (article.lctags == null)
                article.lctags = new List<string>();
            article.lctags.AddRange(tags.Select(x => x.ToLower()));
            article.lctags.AddRange(topics.Select(x => x.ToLower()));
        }

    }
}
