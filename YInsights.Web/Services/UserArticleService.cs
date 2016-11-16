using Microsoft.Azure.Documents.Client;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using YInsights.Web.Extentions;
using YInsights.Web.Model;
using YInsights.Web.Providers;


namespace YInsights.Web.Services
{
    public class UserArticleService : IUserArticleService
    {
        private readonly YInsightsContext db;
        private readonly DocumentDBProvider docClient;
        public UserArticleService(YInsightsContext _db, RedisProvider _redisdb, DocumentDBProvider _docClient)
        {
            db = _db;
            docClient = _docClient;
        }


        public Tuple<IEnumerable<UserArticles>, int> GetUserUnviewedArticles(string username, string title = null, string tags = null, int pageIndex = -1, int pageSize = -1, bool star = false)
        {
            var articlesList = new List<UserArticles>();
            var query = db.UserArticles.Where(x => x.username.Contains(username) && x.isviewed != true).OrderByDescending(x => x.articleid);
            if (star)
            {
                query = query.Where(x => x.star == true).OrderByDescending(x => x.articleid);
            }
            var count = db.UserArticles.Count(x => x.username == username && x.isviewed != true);

            if (pageIndex > -1 && string.IsNullOrEmpty(title) && string.IsNullOrEmpty(tags))
            {
                query = query.Skip(pageIndex).Take(pageSize).OrderByDescending(x => x.articleid); ;
            }
            if (pageIndex == -1 && pageSize > -1)
            {
                query = query.Take(pageSize).OrderByDescending(x => x.articleid); ;
            }

            var articles = docClient.Client.CreateDocumentQuery<UserArticles>(
            UriFactory.CreateDocumentCollectionUri("articles", "article")).AsQueryable();

            var userArticles = query.ToList();

            var ids = userArticles.Select(x => x.articleid.ToString()).ToList();

            articles = articles.Where(x => ids.Contains(x.id.ToString()));
            articlesList.AddRange(articles);
            foreach (var userArticle in userArticles)
            {
                var val = articlesList.FirstOrDefault(x => x.id == userArticle.articleid.ToString());
                val.articleid = userArticle.articleid;
                val.star = userArticle.star;
            }



            if (!string.IsNullOrEmpty(title))
            {
                articlesList.RemoveAll(x => !x.title.Contains(title, StringComparison.OrdinalIgnoreCase));
            }
            if (!string.IsNullOrEmpty(tags))
            {
                var tempList = new List<UserArticles>();
                foreach (var item in articlesList)
                {
                    if (item.tags.Contains(tags, StringComparison.OrdinalIgnoreCase))
                    {
                        tempList.Add(item);
                    }

                }

                articlesList.Clear();
                articlesList.AddRange(tempList);
            }
            if (pageIndex > -1 && (!string.IsNullOrEmpty(title) || !string.IsNullOrEmpty(tags)))
            {
                count = articlesList.Count;
                articlesList = articlesList.Skip(pageIndex).Take(pageSize).OrderByDescending(x => x.articleid).ToList();
            }
            return new Tuple<IEnumerable<UserArticles>, int>(articlesList.OrderByDescending(x => x.time), count);
        }


        public void DeleteUserArticle(string username, int id)
        {
            var article = db.UserArticles.FirstOrDefault(x => x.username == username && x.articleid == id);
            article.isviewed = true;
            db.SaveChanges();
        }
        public void StarUserArticle(string username, int id, bool star)
        {
            var article = db.UserArticles.FirstOrDefault(x => x.username == username && x.articleid == id);
            article.star = star;
            db.SaveChanges();
        }
    }
}