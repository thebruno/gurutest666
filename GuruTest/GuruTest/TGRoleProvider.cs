using System.Web.Security;
using System.Configuration.Provider;
using System.Collections.Specialized;
using System;
using System.Data;
using System.Data.Odbc;
using System.Configuration;
using System.Diagnostics;
using System.Web;
using System.Globalization;
using GuruTest;
using System.Linq;
using System.Collections.Generic;

namespace GuruTest
{
    public sealed class TGRoleProvider : RoleProvider
    {
        private ConnectionStringSettings pConnectionStringSettings;
        private string connectionString;
        private bool pWriteExceptionsToEventLog = false;
        private string eventSource = "TGSqlMembershipProvider";
        private string eventLog = "Application";
        private string exceptionMessage = "An exception occurred. Please check the Event Log.";
        private const int maxPermissionNameLength = 50;

        public bool WriteExceptionsToEventLog
        {
            get { return pWriteExceptionsToEventLog; }
            set { pWriteExceptionsToEventLog = value; }
        }

        public override void Initialize(string name, NameValueCollection config)
        {
            if (config == null)
                throw new ArgumentNullException("config");

            if (name == null || name.Length == 0)
                name = "TGSqlMembershipProvider";

            if (String.IsNullOrEmpty(config["description"]))
            {
                config.Remove("description");
                config.Add("description", "Our TestGuru role provider");
            }

            // Initialize the abstract base class.
            base.Initialize(name, config);

            pApplicationName = TGMembershipProvider.GetConfigValue(config["applicationName"], System.Web.Hosting.HostingEnvironment.ApplicationVirtualPath);

            pWriteExceptionsToEventLog = Convert.ToBoolean(TGMembershipProvider.GetConfigValue(config["writeExceptionsToEventLog"], "true"));

            pConnectionStringSettings = ConfigurationManager.ConnectionStrings[config["connectionStringName"]];

            if (pConnectionStringSettings == null || pConnectionStringSettings.ConnectionString.Trim() == "")
                throw new ProviderException("Connection string cannot be blank.");

            connectionString = pConnectionStringSettings.ConnectionString;
        }

        private string pApplicationName;

        public override string ApplicationName
        {
            get { return pApplicationName; }
            set { pApplicationName = value; }
        }

        public override void AddUsersToRoles(string[] usernames, string[] rolenames)
        {
            throw new ProviderException("Use TestGuru management tools to add users to roles.");
        }

        /// <summary>
        /// Creates new single permission in database.
        /// </summary>
        /// <param name="rolename">Permission name</param>
        public override void CreateRole(string rolename)
        {
            if (rolename == null)
                throw new ArgumentNullException("Role name cannot be null.");

            if (rolename == "" || rolename.Contains(",") || rolename.Length > maxPermissionNameLength)
                throw new ArgumentException(String.Format("Role name cannot contain commas, be empty nor exceed {0} characters length.", maxPermissionNameLength));

            if (RoleExists(rolename))
                throw new ProviderException("Role name already exists.");

            try
            {
                TestGuruDBDataContext db = new TestGuruDBDataContext(connectionString);
                // insert new record
                Permission Entry = new Permission() { Name = rolename };
                db.Permissions.InsertOnSubmit(Entry);
                db.SubmitChanges();
            }
            catch (Exception e)
            {
                if (WriteExceptionsToEventLog)
                {
                    WriteToEventLog(e, "Unhandled exception in CreateRole method");
                    throw new ProviderException(exceptionMessage);
                }
                else
                    throw e;
            }
        }

        public override bool DeleteRole(string rolename, bool throwOnPopulatedRole)
        {
            if (rolename == null)
                throw new ArgumentNullException("Role name cannot be null.");

            if (rolename == "")
                throw new ArgumentException("Role name cannot be empty.");

            try
            {
                TestGuruDBDataContext db = new TestGuruDBDataContext(connectionString);

                // select record
                var result = from i in db.Permissions
                             where i.Name == rolename
                             select i;

                if (result.Count() > 0)
                {
                    Permission toDelete = result.First();
                    if (toDelete.PermissionRoles.Count > 0 && throwOnPopulatedRole)
                        throw new ProviderException("Role being deleted is populated.");

                    //delete record
                    db.Permissions.DeleteOnSubmit(toDelete);
                    db.SubmitChanges();

                    return true;
                }
                else
                    throw new ArgumentException("Role does not exist.");
            }
            catch (Exception e)
            {
                if (WriteExceptionsToEventLog)
                {
                    WriteToEventLog(e, "Unhandled exception in DeleteRole method");
                    throw new ProviderException(exceptionMessage);
                }
                else
                    throw e;
            }
        }

        public override string[] GetAllRoles()
        {
            // role names to return
            List<string> toReturn = new List<string>();

            try
            {
                TestGuruDBDataContext db = new TestGuruDBDataContext(connectionString);

                // select all roles
                var result = from i in db.Permissions
                             select i;

                foreach (Permission p in result)
                    toReturn.Add(p.Name);
            }
            catch (Exception e)
            {
                if (WriteExceptionsToEventLog)
                {
                    WriteToEventLog(e, "Unhandled exception in GetAllRoles method");
                    throw new ProviderException(exceptionMessage);
                }
                else
                    throw e;
            }

            return toReturn.ToArray();
        }

        public override string[] GetRolesForUser(string username)
        {
            if (username == null)
                throw new ArgumentNullException("User name cannot be null.");

            if (username == "")
                throw new ArgumentException("User name cannot be empty.");

            // role names to return
            List<string> toReturn = new List<string>();

            try
            {
                TestGuruDBDataContext db = new TestGuruDBDataContext(connectionString);

                // select all roles
                var result = from per in db.Permissions
                             where (
                                from rl in db.Roles
                                where rl.Administrator == true && (
                                    from accRl in db.AccountRoles
                                    where rl.ID == accRl.Role && (
                                        from acc in db.Accounts
                                        where accRl.Account == acc.ID && acc.Login == username
                                        select acc).Count() > 0
                                    select accRl
                                ).Count() > 0
                                select rl
                             ).Count() > 0 || (
                                from perRl in db.PermissionRoles
                                where per.Name == perRl.Permission && (
                                    from rl in db.Roles
                                    where perRl.Role == rl.ID && (
                                        from accRl in db.AccountRoles
                                        where rl.ID == accRl.Role && (
                                            from acc in db.Accounts
                                            where accRl.Account == acc.ID && acc.Login == username
                                            select acc).Count() > 0
                                        select accRl
                                    ).Count() > 0
                                    select rl
                                ).Count() > 0
                                select perRl
                             ).Count() > 0
                             select per;

                foreach (Permission p in result)
                    toReturn.Add(p.Name);

                return toReturn.ToArray();
            }
            catch (Exception e)
            {
                if (WriteExceptionsToEventLog)
                {
                    WriteToEventLog(e, "Unhandled exception in GetRolesForUser method");
                    throw new ProviderException(exceptionMessage);
                }
                else
                    throw e;
            }
        }

        public override string[] GetUsersInRole(string rolename)
        {
            return null;

            // TODO: zaimplementować
            /*string tmpUserNames = "";

            OdbcConnection conn = new OdbcConnection(connectionString);
            OdbcCommand cmd = new OdbcCommand("SELECT Username FROM UsersInRoles " +
                      " WHERE Rolename = ? AND ApplicationName = ?", conn);

            cmd.Parameters.Add("@Rolename", OdbcType.VarChar, 255).Value = rolename;
            cmd.Parameters.Add("@ApplicationName", OdbcType.VarChar, 255).Value = ApplicationName;

            OdbcDataReader reader = null;

            try
            {
                conn.Open();

                reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    tmpUserNames += reader.GetString(0) + ",";
                }
            }
            catch (OdbcException e)
            {
                if (WriteExceptionsToEventLog)
                {
                    WriteToEventLog(e, "GetUsersInRole");
                }
                else
                {
                    throw e;
                }
            }
            finally
            {
                if (reader != null) { reader.Close(); }
                conn.Close();
            }

            if (tmpUserNames.Length > 0)
            {
                // Remove trailing comma.
                tmpUserNames = tmpUserNames.Substring(0, tmpUserNames.Length - 1);
                return tmpUserNames.Split(',');
            }

            return new string[0];*/
        }

        public override bool IsUserInRole(string username, string rolename)
        {
            return false;

            // TODO: zaimplementować
            /*bool userIsInRole = false;

            OdbcConnection conn = new OdbcConnection(connectionString);
            OdbcCommand cmd = new OdbcCommand("SELECT COUNT(*) FROM UsersInRoles " +
                    " WHERE Username = ? AND Rolename = ? AND ApplicationName = ?", conn);

            cmd.Parameters.Add("@Username", OdbcType.VarChar, 255).Value = username;
            cmd.Parameters.Add("@Rolename", OdbcType.VarChar, 255).Value = rolename;
            cmd.Parameters.Add("@ApplicationName", OdbcType.VarChar, 255).Value = ApplicationName;

            try
            {
                conn.Open();

                int numRecs = (int)cmd.ExecuteScalar();

                if (numRecs > 0)
                {
                    userIsInRole = true;
                }
            }
            catch (OdbcException e)
            {
                if (WriteExceptionsToEventLog)
                {
                    WriteToEventLog(e, "IsUserInRole");
                }
                else
                {
                    throw e;
                }
            }
            finally
            {
                conn.Close();
            }

            return userIsInRole;*/
        }

        public override void RemoveUsersFromRoles(string[] usernames, string[] rolenames)
        {
            throw new ProviderException("Use TestGuru management tools to remove users from roles.");
        }

        public override bool RoleExists(string rolename)
        {
            if (rolename == null)
                throw new ArgumentNullException("Role name cannot be null.");

            if (rolename == "")
                throw new ArgumentException("Role name cannot be empty.");

            try
            {
                TestGuruDBDataContext db = new TestGuruDBDataContext(connectionString);

                // select all roles
                var result = from i in db.Permissions
                             where i.Name == rolename
                             select i;

                return (result.Count() > 0);
            }
            catch (Exception e)
            {
                if (WriteExceptionsToEventLog)
                {
                    WriteToEventLog(e, "Unhandled exception in RoleExists method");
                    throw new ProviderException(exceptionMessage);
                }
                else
                    throw e;
            }
        }

        public override string[] FindUsersInRole(string rolename, string usernameToMatch)
        {
            return null;

            // TODO: zaimplementować
            /*OdbcConnection conn = new OdbcConnection(connectionString);
            OdbcCommand cmd = new OdbcCommand("SELECT Username FROM UsersInRoles  " +
                      "WHERE Username LIKE ? AND RoleName = ? AND ApplicationName = ?", conn);
            cmd.Parameters.Add("@UsernameSearch", OdbcType.VarChar, 255).Value = usernameToMatch;
            cmd.Parameters.Add("@RoleName", OdbcType.VarChar, 255).Value = rolename;
            cmd.Parameters.Add("@ApplicationName", OdbcType.VarChar, 255).Value = pApplicationName;

            string tmpUserNames = "";
            OdbcDataReader reader = null;

            try
            {
                conn.Open();

                reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    tmpUserNames += reader.GetString(0) + ",";
                }
            }
            catch (OdbcException e)
            {
                if (WriteExceptionsToEventLog)
                {
                    WriteToEventLog(e, "FindUsersInRole");
                }
                else
                {
                    throw e;
                }
            }
            finally
            {
                if (reader != null) { reader.Close(); }

                conn.Close();
            }

            if (tmpUserNames.Length > 0)
            {
                // Remove trailing comma.
                tmpUserNames = tmpUserNames.Substring(0, tmpUserNames.Length - 1);
                return tmpUserNames.Split(',');
            }

            return new string[0];*/
        }

        private void WriteToEventLog(Exception e, string action)
        {
            EventLog log = new EventLog();
            log.Source = eventSource;
            log.Log = eventLog;

            string message = "An exception occurred communicating with TestGuru database.\n\n";
            message += "Action: " + action + "\n\n";
            message += "Exception: " + e.ToString();

            log.WriteEntry(message);
        }
    }
}
