using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SharedCode.Providers;
using Microsoft.Azure.Documents.Client;
using SharedCode.Poco;
using System.Net.Http;

// For more information on enabling Web API for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace HNArticles.Controllers
{
    [Route("api/[controller]")]
    public class CalculateController : Controller
    {
        DocumentDBProvider documentDBProvider;
        public CalculateController(DocumentDBProvider _documentDBProvider)
        {
            documentDBProvider = _documentDBProvider;
        }
        // GET: api/values
        [HttpGet]
        public void Get()
        {
            GetArticlesFromHN();
        }

        private async void GetArticlesFromHN()
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
                ProccessStory(storyId);

            }
        }

        private async void ProccessStory(string storyId)
        {
            try
            {
                var queryOptions = new FeedOptions { MaxItemCount = -1 };
                IQueryable<Article> articleExistQuery = documentDBProvider.Client.CreateDocumentQuery<Article>(
                    UriFactory.CreateDocumentCollectionUri("articles", "article"), queryOptions)
                    .Where(f => f.Id == storyId);

                var list = articleExistQuery.ToList();
                
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
                          
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                
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
            await documentDBProvider.Client.CreateDocumentAsync(UriFactory.CreateDocumentCollectionUri("articles", "article"), article);
           
        }

    }
}
