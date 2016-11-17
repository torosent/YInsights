using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YInsights.Web.Model;
using YInsights.Web.Providers;
using YInsights.Web.Extentions;
using Microsoft.Azure.Documents.Client;

namespace YInsights.Web.Services
{
    public class TopicService
    {
        private readonly YInsightsContext db;    
        private readonly DocumentDBProvider docdb;

        public TopicService(YInsightsContext _db, DocumentDBProvider _docdb)
        {
            db = _db;
            docdb = _docdb;
        }
        public  IEnumerable<Topics> GetTopics(int limit)
        {
            var sql = "SELECT * FROM c where c.id = " + '"' + "WordCloudTopics" + '"';
            var articleExistQuery = docdb.Client.CreateDocumentQuery(
              UriFactory.CreateDocumentCollectionUri("articles", "article"), sql).AsEnumerable();


            var strTopics = articleExistQuery.FirstOrDefault().value.ToString();
            if (!string.IsNullOrEmpty(strTopics))
            {
                var topics = (List<Topics>)Newtonsoft.Json.JsonConvert.DeserializeObject<List<Topics>>(strTopics);
                return topics.Take(limit);
            }
            return new List<Topics>();
        }
        public IEnumerable<Topics> GetLastTopics()
        {
            var sql = "SELECT * FROM c where c.id = " + '"' + "LastTopics" + '"';
            var articleExistQuery = docdb.Client.CreateDocumentQuery(
              UriFactory.CreateDocumentCollectionUri("articles", "article"), sql).AsEnumerable();


            var strTopics = articleExistQuery.FirstOrDefault().value.ToString();
          
            if (!string.IsNullOrEmpty(strTopics))
            {
                var topics = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Topics>>(strTopics);
                return topics;
            }
            return new List<Topics>();
        }
        public IEnumerable<Topics> GetTrendingTopics()
        {
            var sql = "SELECT * FROM c where c.id = " + '"' + "TrendingTopics" + '"';
            var articleExistQuery = docdb.Client.CreateDocumentQuery(
              UriFactory.CreateDocumentCollectionUri("articles", "article"), sql).AsEnumerable();


            var strTopics = articleExistQuery.FirstOrDefault().value.ToString();
         
            if (!string.IsNullOrEmpty(strTopics))
            {
                var topics = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Topics>>(strTopics);
                return topics;
            }
            return new List<Topics>();
        }
        public IEnumerable<string> SearchTopics(string text, int limit)
        {
            var sql = "SELECT * FROM c where c.id = " + '"' + "Topics" + '"';
            var articleExistQuery = docdb.Client.CreateDocumentQuery(
              UriFactory.CreateDocumentCollectionUri("articles", "article"), sql).AsEnumerable();


            string strTopics = articleExistQuery.FirstOrDefault().value.ToString();
            if (!string.IsNullOrEmpty(strTopics))
            {
                var topics = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Topics>>(strTopics);
                var stringTopics = topics.Where(x => x.topic.Contains(text, StringComparison.OrdinalIgnoreCase)).OrderByDescending(x => x.count).Take(limit).Select(x => x.topic);
                  return stringTopics;
            }
            return new List<string>();

        }
    }
}
