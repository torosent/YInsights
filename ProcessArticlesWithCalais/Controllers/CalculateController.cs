using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SharedCode.Providers;
using Microsoft.Azure.Documents.Client;
using SharedCode.Poco;
using System.Net.Http;
using Newtonsoft.Json.Linq;

// For more information on enabling Web API for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace ProcessArticlesWithCalais.Controllers
{
    [Route("api/[controller]")]
    public class CalculateController : Controller
    {
        private const string URL = "https://api.thomsonreuters.com/permid/calais";

        private string apitoken;

       static DocumentDBProvider documentDBProvider;

        public CalculateController(DocumentDBProvider _documentDBProvider,token token)
        {
            documentDBProvider = _documentDBProvider;
            apitoken = token.key;
        }
        // GET: api/values
        [HttpGet]
        public void Get()
        {
            GetPreProcessedArticles();
        }

        private  void GetPreProcessedArticles()
        {

            var articleExistQuery = documentDBProvider.Client.CreateDocumentQuery<Article>(
                UriFactory.CreateDocumentCollectionUri("articles", "article")).Where(f => f.processed == false).Take(5);
        
            
            foreach (var item in articleExistQuery.ToList())
            {
                Console.WriteLine($"Fetched article {item.Id}");
                ProcessArticles(item);
            }


        }

        private async void ProcessArticles(Article article)
        {
            try
            { 
                var result = await ProcessArticle(article);
                if (result)
                {
                    article.processed = true;
                    var upsertResult = await documentDBProvider.Client.UpsertDocumentAsync(UriFactory.CreateDocumentCollectionUri("articles", "article"), article);
                    Console.WriteLine($"Processed article {article.Id}");

                    //  ServiceEventSource.Current.ServiceMessage(this, $"Updated article {article.Value.Id}");
                    //    ApplicationInsightsClient.LogEvent($"Updated article", article.Value.Id);

                }
                else
                {
                  //  var count = await articlesDictionary.GetCountAsync(tx);
                 //   ServiceEventSource.Current.ServiceMessage(this, $"Result for {article.Value.Id} is false. List contains {count}");
                  //  ApplicationInsightsClient.LogEvent("Analyze Failed", article.Value.Id);
                }
            }

            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
               // ServiceEventSource.Current.ServiceMessage(this, ex.Message, ex);
               // ApplicationInsightsClient.LogException(ex);

            }


        }



        private async Task<bool> ProcessArticle(Article article)
        {
            try
            {
                Console.WriteLine($"Start calais for {article.Id}");
                HttpClient client = new HttpClient();

                client.DefaultRequestHeaders.TryAddWithoutValidation("ContentType", "text/raw");
                client.DefaultRequestHeaders.TryAddWithoutValidation("X-AG-Access-Token", apitoken);
                client.DefaultRequestHeaders.TryAddWithoutValidation("outputformat", "application/json");

                var response =  client.PostAsync(URL, new StringContent(article.title)).Result;
               
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    Console.WriteLine($"Finished calais for {article.Id}");

                    string responseBody = await response.Content.ReadAsStringAsync();
                    ParseTopics(article, responseBody);

                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

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
