using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Authentication;
using Microsoft.AspNetCore.Authorization;

using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using YInsights.Web.Services;
using YInsights.Web.Model;


namespace YInsights.Web.Controllers
{
    public class AccountController : Controller
    {
     
        private UserArticleService userArticleService;
        private UserService userService;
        private TopicService topicService;
        private AIService aiService;


        public AccountController(UserService _userService, UserArticleService _userArticleService,TopicService _topicService,AIService _aiService)
        {
            userService = _userService;
            userArticleService = _userArticleService;
            topicService = _topicService;
            aiService = _aiService;
        }
        public IActionResult Login(string returnUrl = "/")
        {
          
            return new ChallengeResult("Auth0", new AuthenticationProperties() { RedirectUri = returnUrl });

        }

        [Authorize]
        public IActionResult Logout()
        {
            string username = User.Claims.FirstOrDefault(y => y.Type == "name").Value;
            aiService.TrackUser("Logout", username);
            HttpContext.Authentication.SignOutAsync("Auth0");
            HttpContext.Authentication.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToAction("Index", "Home");
        }

     
        

        [Authorize]
        public async Task<IActionResult> Profile()
        {
            ViewBag.title = "Profile";


            ViewBag.LastTopics = await topicService.GetLastTopics();
            ViewBag.TrendingTopics = await topicService.GetTrendingTopics();


            string username = User.Claims.FirstOrDefault(y => y.Type == "name").Value;
            aiService.TrackUser("ViewProfile", username);

            var user = userService.FindUserByUsername(username);
            if (user == null)
            {
                user = new User();
                user.Id = username;

                return View(user);
            }
            else
            {
                if (!string.IsNullOrEmpty(user.topics))
                {
                    user.tags = Newtonsoft.Json.JsonConvert.DeserializeObject<List<string>>(user.topics);
                }
                return View(user);
            }

        }


        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(User inputuser)
        {

            ViewBag.title = "Profile";
            ViewBag.LastTopics = await topicService.GetLastTopics();
            ViewBag.TrendingTopics = await topicService.GetTrendingTopics();

            string username = User.Claims.FirstOrDefault(y => y.Type == "name").Value;

            aiService.TrackUser("ChangeProfile", username);
            var user = userService.FindUserByUsername(username);
            if (inputuser.tags != null)
            {
                inputuser.topics = Newtonsoft.Json.JsonConvert.SerializeObject(inputuser.tags);
            }
            if (user == null)
            {
                inputuser.Id = username;
                userService.InsertUser(inputuser);
                ViewBag.success = "true";
                return View(inputuser);
            }
            else
            {
                user.hn = inputuser.hn;
                user.topics = inputuser.topics;
                user.tags = inputuser.tags;
                userService.UpdateUser(user);
                ViewBag.success = "true";
                return View(user);
            }

        }

       
        [Authorize]
        public IActionResult MyList()
        {
            ViewBag.title = "My List";         
            return View();
        }


        [Authorize]
        [HttpDelete]
        public void DeleteArticle(int id)
        {
            string username = User.Claims.FirstOrDefault(y => y.Type == "name").Value;
            aiService.TrackUser("DeleteArticle", username,id.ToString());

            userArticleService.DeleteUserArticle(username, id);


        }
        [Authorize]
        [HttpPut]
        public void StarArticle(int id,bool star)
        {
            string username = User.Claims.FirstOrDefault(y => y.Type == "name").Value;
            aiService.TrackUser("StarArticle", username, id.ToString(),star.ToString());

            userArticleService.StarUserArticle(username, id,star);


        }
        [Authorize]
        public async Task<IActionResult> SearchTopics(string text)
        {

            var topicsList =await topicService.SearchTopics(text, 20);
            return Json(topicsList);
        }

        [Authorize]
        public async Task<IActionResult> GenerateMyList(string title,string tags, int pageIndex = 0, int pageSize = 15,bool star=false)
        {
            try
            {
                string username = User.Claims.FirstOrDefault(y => y.Type == "name").Value;
                aiService.TrackUser("GenerateMyList", username);

                var tuple = await userArticleService.GetUserUnviewedArticles(username, title,tags,pageIndex,pageSize,star);
               
                return Json(new { data = tuple.Item1, itemsCount = tuple.Item2});
            }
            catch(Exception ex)
            {
                aiService.TrackException(ex);
               
                return Json(new List<UserArticles>());
            }
        }



    }
}
