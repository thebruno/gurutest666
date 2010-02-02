using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace GuruTest
{
    public partial class adduser : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {

        }

        protected void CreateUserWizard1_CreatedUser(object sender, EventArgs e)
        {
           
        }

        protected void cuWizard_FinishButtonClick(object sender, WizardNavigationEventArgs e)
        {
            TestGuruDBDataContext dc = new TestGuruDBDataContext(@"Data Source=BrunoPC\SQLEXPRESS2008EX;Initial Catalog=TestGuruDB;Integrated Security=True");
            var result = from i in dc.Accounts
                         where i.Login == cuWizard.UserName
                         select i;
            if (result.Count() > 0)
            {
                Account a = result.First();
                a.City = txtCity.Text;
                dc.SubmitChanges();
            }
        }
    }
}
