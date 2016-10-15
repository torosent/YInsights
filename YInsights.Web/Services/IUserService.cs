using YInsights.Web.Model;

namespace YInsights.Web.Services
{
    public interface IUserService
    {
        User FindUserByUsername(string username);
        void InsertUser(User user);
        void UpdateUser(User user);
    }
}