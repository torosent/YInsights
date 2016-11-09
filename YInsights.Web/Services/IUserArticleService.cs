using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YInsights.Web.Model;

namespace YInsights.Web.Services
{
    public interface IUserArticleService
    {
        Tuple<IEnumerable<UserArticles>, int> GetUserUnviewedArticles(string username, string title,string tags, int pageIndex, int pageSize,bool star);
        void DeleteUserArticle(string username, int id);
        void StarUserArticle(string username, int id, bool star);
    }
}
