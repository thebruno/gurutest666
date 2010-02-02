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
            if (rolename == null)
                throw new ArgumentNullException("Role name cannot be null.");

            if (rolename == "")
                throw new ArgumentException("Role name cannot be empty.");

            // role names to return
            List<string> toReturn = new List<string>();

            try
            {
                TestGuruDBDataContext db = new TestGuruDBDataContext(connectionString);
                var result = from acc in db.Accounts
                             where (
                                from accRl in db.AccountRoles
                                where accRl.Account == acc.ID && (
                                    from rl in db.Roles
                                    where rl.ID == accRl.Account && rl.Administrator == true && (
                                        from per in db.Permissions
                                        where per.Name == rolename
                                        select per
                                    ).Count() > 0
                                    select rl).Count() > 0
                                select accRl
                             ).Count() > 0 || (
                                from accRl in db.AccountRoles
                                where accRl.Account == acc.ID && (
                                    from rl in db.Roles
                                    where rl.ID == accRl.Role && (
                                        from rolePerm in db.PermissionRoles
                                        where rolePerm.Role == rl.ID &&
                                            rolePerm.Permission == rolename
                                        select rolePerm
                                    ).Count() > 0
                                    select rl
                                ).Count() > 0
                                select accRl
                             ).Count() > 0
                             select acc;
                foreach (Account a in result)
                    toReturn.Add(a.Login);
            }
            catch (Exception e)
            {
                if (WriteExceptionsToEventLog)
                {
                    WriteToEventLog(e, "Unhandled exception in GetUsersInRole method");
                    throw new ProviderException(exceptionMessage);
                }
                else
                    throw e;               
            }

            return toReturn.ToArray();
        }

        public override bool IsUserInRole(string username, string rolename)
        {
            if (username == null || rolename == null)
                throw new ArgumentNullException("User name and role name cannot be null.");

            if (username == "" || rolename == "")
                throw new ArgumentException("User name and role name cannot be empty.");

            try
            {
                TestGuruDBDataContext db = new TestGuruDBDataContext(connectionString);
                bool toReturn = (from acc in db.Accounts
                                 where acc.Login == username && (
                                 (
                                    from accRl in db.AccountRoles
                                    where accRl.Account == acc.ID && (
                                        from rl in db.Roles
                                        where rl.ID == accRl.Account && rl.Administrator == true && (
                                            from per in db.Permissions
                                            where per.Name == rolename
                                            select per
                                        ).Count() > 0
                                        select rl).Count() > 0
                                    select accRl
                                 ).Count() > 0 || (
                                    from accRl in db.AccountRoles
                                    where accRl.Account == acc.ID && (
                                        from rl in db.Roles
                                        where rl.ID == accRl.Role && (
                                            from rolePerm in db.PermissionRoles
                                            where rolePerm.Role == rl.ID &&
                                                rolePerm.Permission == rolename
                                            select rolePerm
                                        ).Count() > 0
                                        select rl
                                    ).Count() > 0
                                    select accRl
                                 ).Count() > 0
                                 )
                                 select acc).Count() > 0;
                return toReturn;
            }
            catch (Exception e)
            {
               if (WriteExceptionsToEventLog)
                {
                    WriteToEventLog(e, "Unhandled exception in IsUserInRole method");
                    throw new ProviderException(exceptionMessage);
                }
                else
                    throw e;
            }
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
            if (rolename == null || usernameToMatch == null)
                throw new ArgumentNullException("Role name and user name to match cannot be null.");

            if (rolename == "" || usernameToMatch == "")
                throw new ArgumentException("Role name and user name to match cannot be empty.");

            // role names to return
            List<string> toReturn = new List<string>();

            try
            {
                TestGuruDBDataContext db = new TestGuruDBDataContext(connectionString);
                var result = from acc in db.Accounts
                             where System.Data.Linq.SqlClient.SqlMethods.Like(acc.Login, usernameToMatch) && (
                             (
                                from accRl in db.AccountRoles
                                where accRl.Account == acc.ID && (
                                    from rl in db.Roles
                                    where rl.ID == accRl.Account && rl.Administrator == true && (
                                        from per in db.Permissions
                                        where per.Name == rolename
                                        select per
                                    ).Count() > 0
                                    select rl).Count() > 0
                                select accRl
                             ).Count() > 0 || (
                                from accRl in db.AccountRoles
                                where accRl.Account == acc.ID && (
                                    from rl in db.Roles
                                    where rl.ID == accRl.Role && (
                                        from rolePerm in db.PermissionRoles
                                        where rolePerm.Role == rl.ID &&
                                            rolePerm.Permission == rolename
                                        select rolePerm
                                    ).Count() > 0
                                    select rl
                                ).Count() > 0
                                select accRl
                             ).Count() > 0)
                             select acc;
                foreach (Account a in result)
                    toReturn.Add(a.Login);
            }
            catch (Exception e)
            {
                if (WriteExceptionsToEventLog)
                {
                    WriteToEventLog(e, "Unhandled exception in FindUsersInRole method");
                    throw new ProviderException(exceptionMessage);
                }
                else
                    throw e;
            }

            return toReturn.ToArray();
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
