using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.Security;

namespace GuruTest
{
    public partial class _Default : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            //
            //string[] a = System.Web.Security.Roles.Provider.GetAllRoles();
            //string[] b = System.Web.Security.Roles.Provider.GetRolesForUser("f");
            //int i = 0;

            //System.Web.Security.Membership.Provider.UnlockUser("f");
            //int a;
            //System.Web.Security.Membership.Provider.GetAllUsers(0, 100, out a);

            //System.Web.Security.MembershipCreateStatus t;
            //System.Web.Security.Membership.Provider.CreateUser("f", "test!!!!", "f@f.f", "f?", "f!", false, null, out t);

            string [] s = Roles.GetUsersInRole("CanLogOn");
            s = Roles.GetUsersInRole("CanAddArticle");
            s = Roles.GetUsersInRole("CanDoSomething");
            s = Roles.GetUsersInRole("CanLogOut");

            bool result = Roles.IsUserInRole("test", "CanLogOn");
            result = Roles.IsUserInRole("test", "CanAddArticle");
            result = Roles.IsUserInRole("test", "CanDoSomething");
            result = Roles.IsUserInRole("test", "CanLogOut");

            result = Roles.IsUserInRole("f", "CanLogOn");
            result = Roles.IsUserInRole("f", "CanAddArticle");
            result = Roles.IsUserInRole("f", "CanDoSomething");
            result = Roles.IsUserInRole("f", "CanLogOut");

            result = Roles.IsUserInRole("a", "CanLogOn");
            result = Roles.IsUserInRole("f", "a");
            result = Roles.IsUserInRole("y", "x");

            s = Roles.FindUsersInRole("CanLogOn", "test");
            s = Roles.FindUsersInRole("CanLogOn", "te%");
            s = Roles.FindUsersInRole("CanLogOn", "%");

            s = Roles.FindUsersInRole("CanDoSomething", "test");
            s = Roles.FindUsersInRole("CanDoSomething", "te%");
            s = Roles.FindUsersInRole("CanDoSomething", "%");
            s = Roles.FindUsersInRole("a", "test");
        }

        protected void Button1_Click(object sender, EventArgs e)
        {
            System.Web.Security.FormsAuthentication.SignOut();
            Response.Redirect("~/default.aspx");
        }
    }
}
