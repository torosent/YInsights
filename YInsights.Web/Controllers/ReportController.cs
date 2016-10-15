using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using YInsights.Web.Model;

// For more information on enabling MVC for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace YInsights.Web.Controllers
{
    public class ReportController : Controller
    {
        private static YInsightsContext db;
        public ReportController(YInsightsContext context)
        {
            db = context;

        }
        [Authorize]
        public IActionResult Index()
        {
            var users = db.User.ToList();
            var dynamicList = new List<dynamic>();
            foreach (var user in users)
            {
                var count = db.UserArticles.Count(x => x.username == user.Id);
                dynamicList.Add(new { user = user.Id, topics = user.topics, articles = count });
            }
            return Json(dynamicList);
        }
    }
}
