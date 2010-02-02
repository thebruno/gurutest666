using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace GuruTest
{
    public partial class _Default : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            //System.Web.Security.MembershipCreateStatus t;
            string[] a = System.Web.Security.Roles.Provider.GetAllRoles();
            string[] b = System.Web.Security.Roles.Provider.GetRolesForUser("f");
            int i = 0;

            //System.Web.Security.Membership.Provider.UnlockUser("f");
            //int a;
            //System.Web.Security.Membership.Provider.GetAllUsers(0, 100, out a);
            //System.Web.Security.Membership.Provider.CreateUser("f", "test!!!!", "f@f.f", "f?", "f!", false, null, out t);
        }

        protected void Button1_Click(object sender, EventArgs e)
        {
            System.Web.Security.FormsAuthentication.SignOut();
            Response.Redirect("~/default.aspx");
        }
    }
}
