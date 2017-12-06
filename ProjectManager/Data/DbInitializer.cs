using ProjectManager.Models;
using System.Linq;
using System.Threading.Tasks;

namespace ProjectManager.Data
{
    public static class DbInitializer
    {
        public static void Initialize(ProjectDBContext context)
        {
            context.Database.EnsureCreated();

            if (context.User.Any())
            {
                return;
            }

            var user = new User
            {
                Login = "admin",
                Password = "ISMvKXpXpadDiUoOSoAfww=="
            };
            context.User.Add(user);
            context.SaveChanges();
        }



    }
}
