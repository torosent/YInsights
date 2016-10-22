using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using YInsights.Web.Services;
using YInsights.Web.Providers;
using System.Text.Encodings.Web;

namespace YInsights.Web.Controllers
{
    public class HomeController : Controller
    {



        private UserService userService;
        private TopicService topicService;
        private AIService aiService;
        public HomeController(UserService _userService, TopicService _topicService, AIService _aiService)
        {
        
            userService = _userService;
            topicService = _topicService;
            aiService = _aiService;
        }
        [Authorize]
        public async Task<IActionResult> Index()
        {
            try
            {
                string username = User.Claims.FirstOrDefault(y => y.Type == "name").Value;
                var user = userService.FindUserByUsername(username);
                aiService.TrackUser("Homepage", username);
                if (user == null || string.IsNullOrEmpty(user.topics))
                {
                    ViewBag.user = string.Empty;
                }
                else
                {
                    ViewBag.user = user.Id;

                }

                ViewBag.title = "Home";
                ViewBag.LastTopics = await topicService.GetLastTopics();
                ViewBag.TrendingTopics = await topicService.GetTrendingTopics();

            }
            catch (Exception ex)
            {
                aiService.TrackException(ex);
            }
            return View();
        }

        [Authorize]
        public IActionResult RSS()
        {
            string username = User.Claims.FirstOrDefault(y => y.Type == "name").Value;
            var link = string.Concat("https://", HttpContext.Request.Host.ToUriComponent(), "/api/feed?id=", username);
            ViewBag.link = link;
            return View();
        }


        public IActionResult Error()
        {
            return View();
        }


    }
}
