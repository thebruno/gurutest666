using System;
using System.Collections.Specialized;
using System.Configuration;
using System.Configuration.Provider;
using System.Data;
using System.Data.Common;

using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web.Configuration;
using System.Web.Security;

namespace GuruTest
{
    public sealed class TGMembershipProvider : MembershipProvider
    {
        private int newPasswordLength = 8;
        private string eventSource = "TGSqlMembershipProvider";
        private string eventLog = "Application";
        private string exceptionMessage = "An exception occurred. Please check the Event Log.";
        private string connectionString;
        private string factoryString = "System.Data.SqlClient";
        private MachineKeySection machineKey;
        private bool pWriteExceptionsToEventLog;

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
                config.Add("description", "Our TestGuru Membership provider");
            }

            // Initialize the abstract base class.
            base.Initialize(name, config);

            pApplicationName = GetConfigValue(config["applicationName"],
                                            System.Web.Hosting.HostingEnvironment.ApplicationVirtualPath);
            pMaxInvalidPasswordAttempts = Convert.ToInt32(GetConfigValue(config["maxInvalidPasswordAttempts"], "5"));
            pPasswordAttemptWindow = Convert.ToInt32(GetConfigValue(config["passwordAttemptWindow"], "10"));
            pMinRequiredNonAlphanumericCharacters = Convert.ToInt32(GetConfigValue(config["minRequiredNonAlphanumericCharacters"], "1"));
            pMinRequiredPasswordLength = Convert.ToInt32(GetConfigValue(config["minRequiredPasswordLength"], "7"));
            pPasswordStrengthRegularExpression = Convert.ToString(GetConfigValue(config["passwordStrengthRegularExpression"], ""));
            pEnablePasswordReset = Convert.ToBoolean(GetConfigValue(config["enablePasswordReset"], "true"));
            pEnablePasswordRetrieval = false; // Convert.ToBoolean(GetConfigValue(config["enablePasswordRetrieval"], "true"));
            pRequiresQuestionAndAnswer = Convert.ToBoolean(GetConfigValue(config["requiresQuestionAndAnswer"], "false"));
            pRequiresUniqueEmail = Convert.ToBoolean(GetConfigValue(config["requiresUniqueEmail"], "true"));
            pWriteExceptionsToEventLog = Convert.ToBoolean(GetConfigValue(config["writeExceptionsToEventLog"], "true"));
            factoryString = Convert.ToString(GetConfigValue(config["factoryString"], "System.Data.SqlClient"));

            pPasswordFormat = MembershipPasswordFormat.Hashed;

            ConnectionStringSettings ConnectionStringSettings =
              ConfigurationManager.ConnectionStrings[config["connectionStringName"]];

            if (ConnectionStringSettings == null || ConnectionStringSettings.ConnectionString.Trim() == "")
            {
                throw new ProviderException("Connection string cannot be blank.");
            }

            connectionString = ConnectionStringSettings.ConnectionString;

            // Get encryption and decryption key information from the configuration.
            Configuration cfg =
              WebConfigurationManager.OpenWebConfiguration(System.Web.Hosting.HostingEnvironment.ApplicationVirtualPath);
            machineKey = (MachineKeySection)cfg.GetSection("system.web/machineKey");

        }

        private string GetConfigValue(string configValue, string defaultValue)
        {
            if (String.IsNullOrEmpty(configValue))
                return defaultValue;

            return configValue;
        }

        private string pApplicationName;
        private bool pEnablePasswordReset;
        private bool pEnablePasswordRetrieval;
        private bool pRequiresQuestionAndAnswer;
        private bool pRequiresUniqueEmail;
        private int pMaxInvalidPasswordAttempts;
        private int pPasswordAttemptWindow;
        private MembershipPasswordFormat pPasswordFormat;

        public override string ApplicationName
        {
            get { return pApplicationName; }
            set { pApplicationName = value; }
        }

        public override bool EnablePasswordReset
        {
            get { return pEnablePasswordReset; }
        }


        public override bool EnablePasswordRetrieval
        {
            get { return pEnablePasswordRetrieval; }
        }


        public override bool RequiresQuestionAndAnswer
        {
            get { return pRequiresQuestionAndAnswer; }
        }


        public override bool RequiresUniqueEmail
        {
            get { return pRequiresUniqueEmail; }
        }


        public override int MaxInvalidPasswordAttempts
        {
            get { return pMaxInvalidPasswordAttempts; }
        }


        public override int PasswordAttemptWindow
        {
            get { return pPasswordAttemptWindow; }
        }


        public override MembershipPasswordFormat PasswordFormat
        {
            get { return pPasswordFormat; }
        }

        private int pMinRequiredNonAlphanumericCharacters;

        public override int MinRequiredNonAlphanumericCharacters
        {
            get { return pMinRequiredNonAlphanumericCharacters; }
        }

        private int pMinRequiredPasswordLength;

        public override int MinRequiredPasswordLength
        {
            get { return pMinRequiredPasswordLength; }
        }

        private string pPasswordStrengthRegularExpression;

        public override string PasswordStrengthRegularExpression
        {
            get { return pPasswordStrengthRegularExpression; }
        }

        public override bool ChangePassword(string username, string oldPwd, string newPwd)
        {
            if (!ValidateUser(username, oldPwd))
                return false;

            ValidatePasswordEventArgs args = new ValidatePasswordEventArgs(username, newPwd, true);
            OnValidatingPassword(args);

            if (args.Cancel)
                if (args.FailureInformation != null)
                    throw args.FailureInformation;
                else
                    throw new MembershipPasswordException("Change password canceled due to new password validation failure.");

            try
            {
                TestGuruDBDataContext db = new TestGuruDBDataContext(connectionString);
                Account account = GetAccount(username);
                if (account != null)
                {
                    account.Password = MD5.Create().ComputeHash(Encoding.Default.GetBytes(newPwd));
                    db.SubmitChanges();
                    return true;
                }
            }
            catch (Exception e)
            {
                if (WriteExceptionsToEventLog)
                {
                    WriteToEventLog(e, "Unhandled exception in ChangePassword method");
                    throw new ProviderException(exceptionMessage);
                }
                else
                    throw e;
            }
            return false;
        }

        public override bool ChangePasswordQuestionAndAnswer(string username,
                      string password,
                      string newPwdQuestion,
                      string newPwdAnswer)
        {
            if (!ValidateUser(username, password))
                return false;

            try
            {
                TestGuruDBDataContext db = new TestGuruDBDataContext(connectionString);
                Account account = GetAccount(username);
                if (account != null)
                {
                    account.PasswordQuestion = newPwdQuestion;
                    account.PasswordAnswer = newPwdAnswer;
                    db.SubmitChanges();
                    return true;
                }
            }
            catch (Exception e)
            {
                if (WriteExceptionsToEventLog)
                {
                    WriteToEventLog(e, "Unhandled exception in ChangePasswordQuestionAndAnswer method");
                    throw new ProviderException(exceptionMessage);
                }
                else
                    throw e;
            }
            return false;
        }

        public override MembershipUser CreateUser(string username,
                 string password,
                 string email,
                 string passwordQuestion,
                 string passwordAnswer,
                 bool isApproved,
                 object providerUserKey,
                 out MembershipCreateStatus status)
        {

            // call the validate password event handler
            ValidatePasswordEventArgs args = new ValidatePasswordEventArgs(username, password, true);
            OnValidatingPassword(args);
            if (args.Cancel)
            {
                status = MembershipCreateStatus.InvalidPassword;
                return null;
            }

            if (RequiresUniqueEmail && GetUserNameByEmail(email) != "")
            {
                status = MembershipCreateStatus.DuplicateEmail;
                return null;
            }
            // check if login is in use
            MembershipUser u = GetUser(username, false);
            if (u == null)
            {
                System.Data.Common.DbCommand command = null;
                System.Data.Common.DbConnection connection = null;
                try
                {
                    DbProviderFactory factory = DbProviderFactories.GetFactory(factoryString);
                    connection = factory.CreateConnection();
                    connection.ConnectionString = connectionString;
                    connection.Open();
                    command = factory.CreateCommand();
                    command.Connection = connection;
                    command.Transaction = connection.BeginTransaction(IsolationLevel.RepeatableRead);
                    command.CommandText = "INSERT INTO Auth.Account ([Login], [Password], [Email], [ActivationGUID], [TerminationGUID], [PasswordQuestion], [PasswordAnswer]) VALUES (@Login, @Password, @Email, @AGUID, @TGUID, @PasswordQuestion, @PasswordAnswer)";
                    string[] paramNames = { "@Login",
								    "@Password",								    
								    "@Email",
								    "@AGUID",
								    "@TGUID",
                                    "@PasswordQuestion",
                                    "@PasswordAnswer"
							      };
                    DbType[] types = {  DbType.StringFixedLength,
							    DbType.Binary,
							    DbType.StringFixedLength,
							    DbType.Guid,
							    DbType.Guid,
                                DbType.StringFixedLength,
                                DbType.StringFixedLength
						     };
                    object[] values = { username.ToLower(),
							    MD5.Create().ComputeHash(Encoding.Default.GetBytes(password)),
							    email,
							    Guid.NewGuid(),
							    Guid.NewGuid(),
                                passwordQuestion,
                                passwordAnswer
						      };
                    for (int i = 0; i < paramNames.Length; i++)
                        command.Parameters.Add(CreateParameter(paramNames[i], types[i], values[i]));
                    command.ExecuteNonQuery();

                    TestGuruDBDataContext db = new TestGuruDBDataContext(connection);
                    db.Transaction = command.Transaction;
                    MembershipUser toReturn = GetUser(username, null, null, false, db);

                    command.Transaction.Commit();
                    connection.Close();

                    if (toReturn != null)
                    {

                        try
                        {
                            //MailManagement.SendActivationEmail(toReturn as TGMembershipUser);
                        }
                        catch (Exception ex)
                        {
                            if (WriteExceptionsToEventLog)
                            {
                                WriteToEventLog(ex, "Mail activation sending failure");
                                throw new ProviderException(exceptionMessage);
                            }
                            else
                                throw new AttemptToUseRestrictedEmailException(ex.Message, ex);
                        }
                        status = MembershipCreateStatus.Success;
                    }
                    else
                        status = MembershipCreateStatus.UserRejected;

                    return toReturn;
                }

                catch (System.Data.SqlClient.SqlException ex)
                {
                    //Próba wycofania transakcji
                    try
                    {
                        command.Transaction.Rollback();
                    }
                    //Próba zamknięcia połączenia
                    catch (Exception) { }
                    try
                    {
                        connection.Close();
                    }
                    catch (Exception) { }
                    //Wyrzucenie odpowiedniego lub domyślnego wyjątku
                    status = MembershipCreateStatus.ProviderError;
                    if (ex.State == 3)
                    {
                        if (WriteExceptionsToEventLog)
                        {
                            WriteToEventLog(ex, "Attempt to use restricted email exception");
                            throw new ProviderException(exceptionMessage);
                        }
                        else
                            throw new AttemptToUseRestrictedEmailException(ex.Message, ex);
                    }
                    else if (ex.State == 4)
                    {
                        status = MembershipCreateStatus.DuplicateUserName;
                        return null;
                    }
                    if (WriteExceptionsToEventLog)
                    {
                        WriteToEventLog(ex, "Unrecognized provider error");
                        throw new ProviderException(exceptionMessage);
                    }
                    else
                        throw;
                }
            }
            status = MembershipCreateStatus.DuplicateUserName;
            return null;
        }

        private MembershipUser GetUser(string username, string email, int? id, bool userIsOnline, TestGuruDBDataContext db)
        {
            bool ownConnection = (db == null);
            DbConnection connection = null;

            try
            {
                if (ownConnection)
                {
                    DbProviderFactory factory = DbProviderFactories.GetFactory(factoryString);
                    connection = factory.CreateConnection();
                    connection.ConnectionString = connectionString;
                    connection.Open();
                    db = new TestGuruDBDataContext(connection);
                    db.Transaction = connection.BeginTransaction(IsolationLevel.RepeatableRead);
                }

                var result = from i in db.Accounts
                             where (username != null ? i.Login == username : true)
                             && (email != null ? i.Email == email : true)
                             && (id != null ? i.ID == id : true)
                             && i.Deleted == false
                             select i;

                if (result.Count() > 0)
                {
                    Account account = result.First();
                    if (userIsOnline)
                    {
                        account.LastActivityDate = DateTime.Now;
                        db.SubmitChanges();
                    }
                    if (ownConnection)
                        db.Transaction.Commit();

                    return new TGMembershipUser("TGSqlMembershipProvider", account);
                }

                if (ownConnection)
                    db.Transaction.Commit();
            }
            catch (Exception e)
            {
                if (ownConnection)
                    db.Transaction.Rollback();
                if (WriteExceptionsToEventLog)
                {
                    WriteToEventLog(e, "Unhandled exception in GetUser method");
                    throw new ProviderException(exceptionMessage);
                }
                else
                    throw e;
            }
            finally
            {
                try
                {
                    if (ownConnection)
                        connection.Close();
                }
                catch (Exception) { }
            }
            return null;
        }

        private SqlParameter CreateParameter(string name, DbType type, object value)
        {
            var result = new SqlParameter(name, value);
            result.DbType = type;
            return result;
        }

        public override bool DeleteUser(string username, bool deleteAllRelatedData)
        {
            try
            {
                TestGuruDBDataContext db = new TestGuruDBDataContext(connectionString);
                var result = from i in db.Accounts
                             where i.Login == username
                             select i;
                if (result.Count() == 0)
                    return false;
                db.Accounts.DeleteOnSubmit(result.First());
                db.SubmitChanges();
                return true;
            }
            catch (Exception e)
            {
                if (WriteExceptionsToEventLog)
                {
                    WriteToEventLog(e, "Unhandled exception in DeleteUser method");
                    throw new ProviderException(exceptionMessage);
                }
                else
                    throw e;
            }
        }

        public override MembershipUserCollection GetAllUsers(int pageIndex, int pageSize, out int totalRecords)
        {
            MembershipUserCollection users = new MembershipUserCollection();
            try
            {
                TestGuruDBDataContext db = new TestGuruDBDataContext(connectionString);
                var allUsers = from i in db.Accounts
                               where i.Active == true && i.Deleted == false
                               orderby i.Login ascending
                               select i;

                int counter = 0;
                int startIndex = pageSize * pageIndex;
                int endIndex = startIndex + pageSize - 1;

                totalRecords = allUsers.Count();
                foreach (Account account in allUsers)
                {
                    if (counter >= startIndex)
                        users.Add(new TGMembershipUser("TGSqlMembershipProvider", account));

                    if (counter >= endIndex)
                        break;
                    ++counter;
                }
            }
            catch (Exception e)
            {
                if (WriteExceptionsToEventLog)
                {
                    WriteToEventLog(e, "Unhandled exception in GetAllUsers method");
                    throw new ProviderException(exceptionMessage);
                }
                else
                    throw e;
            }
            return users;
        }

        public override int GetNumberOfUsersOnline()
        {
            TimeSpan onlineSpan = new TimeSpan(0, System.Web.Security.Membership.UserIsOnlineTimeWindow, 0);
            DateTime compareTime = DateTime.Now.Subtract(onlineSpan);

            try
            {
                TestGuruDBDataContext db = new TestGuruDBDataContext(connectionString);
                var allUsers = from i in db.Accounts
                               where i.Active == true && i.Deleted == false && i.LastActivityDate > compareTime
                               select i;

                return allUsers.Count();
            }
            catch (Exception e)
            {
                if (WriteExceptionsToEventLog)
                {
                    WriteToEventLog(e, "Unhandled exception in GetNumberOfUsersOnline method");
                    throw new ProviderException(exceptionMessage);
                }
                else
                    throw e;
            }
        }

        public override string GetPassword(string username, string answer)
        {
            throw new ProviderException("Cannot retrieve Hashed passwords.");
        }

        public override MembershipUser GetUser(string username, bool userIsOnline)
        {
            return GetUser(username, null, null, userIsOnline, null);
        }

        public override MembershipUser GetUser(object providerUserKey, bool userIsOnline)
        {
            return GetUser(null, null, providerUserKey as int?, userIsOnline, null);
        }

        public override bool UnlockUser(string username)
        {
            return false;
        }

        public override string GetUserNameByEmail(string email)
        {
            TGMembershipUser user = (TGMembershipUser)GetUser(null, email, null, false, null);
            if (user != null)
                return user.UserName;
            return "";
        }

        public override string ResetPassword(string username, string answer)
        {
            return "NOT IMPLEMENTED";
            /*if (!EnablePasswordReset)
                throw new NotSupportedException("Password reset is not enabled.");

            if (answer == null && RequiresQuestionAndAnswer)
                throw new ProviderException("Password answer required for password reset.");

            string newPassword = System.Web.Security.Membership.GeneratePassword(newPasswordLength, MinRequiredNonAlphanumericCharacters);

            ValidatePasswordEventArgs args = new ValidatePasswordEventArgs(username, newPassword, true);
            OnValidatingPassword(args);

            if (args.Cancel)
                if (args.FailureInformation != null)
                    throw args.FailureInformation;
                else
                    throw new MembershipPasswordException("Reset password canceled due to password validation failure.");


            OdbcConnection conn = new OdbcConnection(connectionString);
            OdbcCommand cmd = new OdbcCommand("SELECT PasswordAnswer, IsLockedOut FROM Users " +
                  " WHERE Username = ? AND ApplicationName = ?", conn);

            cmd.Parameters.Add("@Username", OdbcType.VarChar, 255).Value = username;
            cmd.Parameters.Add("@ApplicationName", OdbcType.VarChar, 255).Value = pApplicationName;

            int rowsAffected = 0;
            string passwordAnswer = "";
            OdbcDataReader reader = null;

            try
            {
                conn.Open();

                reader = cmd.ExecuteReader(CommandBehavior.SingleRow);

                if (reader.HasRows)
                {
                    reader.Read();

                    if (reader.GetBoolean(1))
                        throw new MembershipPasswordException("The supplied user is locked out.");

                    passwordAnswer = reader.GetString(0);
                }
                else
                {
                    throw new MembershipPasswordException("The supplied user name is not found.");
                }

                if (RequiresQuestionAndAnswer && !CheckPassword(answer, passwordAnswer))
                {
                    UpdateFailureCount(username, "passwordAnswer");

                    throw new MembershipPasswordException("Incorrect password answer.");
                }

                OdbcCommand updateCmd = new OdbcCommand("UPDATE Users " +
                    " SET Password = ?, LastPasswordChangedDate = ?" +
                    " WHERE Username = ? AND ApplicationName = ? AND IsLockedOut = False", conn);

                updateCmd.Parameters.Add("@Password", OdbcType.VarChar, 255).Value = EncodePassword(newPassword);
                updateCmd.Parameters.Add("@LastPasswordChangedDate", OdbcType.DateTime).Value = DateTime.Now;
                updateCmd.Parameters.Add("@Username", OdbcType.VarChar, 255).Value = username;
                updateCmd.Parameters.Add("@ApplicationName", OdbcType.VarChar, 255).Value = pApplicationName;

                rowsAffected = updateCmd.ExecuteNonQuery();
            }
            catch (OdbcException e)
            {
                if (WriteExceptionsToEventLog)
                {
                    WriteToEventLog(e, "ResetPassword");

                    throw new ProviderException(exceptionMessage);
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

            if (rowsAffected > 0)
            {
                return newPassword;
            }
            else
            {
                throw new MembershipPasswordException("User not found, or user is locked out. Password not Reset.");
            }*/
        }

        public override void UpdateUser(MembershipUser user)
        {
            TGMembershipUser tgUser = user as TGMembershipUser;

            try
            {
                TestGuruDBDataContext db = new TestGuruDBDataContext(connectionString);
                // get user by login
                Account account = null;

                var result = from i in db.Accounts
                             where i.Login == user.UserName.ToLower() && i.Deleted == false
                             select i;

                if (result.Count() > 0)
                {
                    account = result.First();
                    // update user data
                    account.Email = user.Email;
                    if (tgUser != null)
                    {
                        account.FirstName = tgUser.FirstName;
                        account.LastName = tgUser.LastName;
                        account.City = tgUser.City;
                    }
                    db.SubmitChanges();
                }
            }
            catch (Exception e)
            {
                if (WriteExceptionsToEventLog)
                {
                    WriteToEventLog(e, "Unhandled exception in UpdateUser method");
                    throw new ProviderException(exceptionMessage);
                }
                else
                    throw e;
            }
        }

        private Account GetAccount(string login)
        {
            Account toReturn = null;
            try
            {
                TestGuruDBDataContext db = new TestGuruDBDataContext(connectionString);
                var result = from i in db.Accounts
                             where i.Login == login.ToLower() && i.Deleted == false
                             select i;

                if (result.Count() > 0)
                    toReturn = result.First();
            }
            catch (Exception e)
            {
                if (WriteExceptionsToEventLog)
                {
                    WriteToEventLog(e, "Unhandled exception in GetAccount method");
                    throw new ProviderException(exceptionMessage);
                }
                else
                    throw e;
            }
            return toReturn;
        }

        public override bool ValidateUser(string username, string password)
        {
            Account account = GetAccount(username);
            return (account != null && account.Active && account.Password == MD5.Create().ComputeHash(Encoding.Default.GetBytes(password)));
        }

        private void UpdateFailureCount(string username, string failureType)
        {
            /*OdbcConnection conn = new OdbcConnection(connectionString);
            OdbcCommand cmd = new OdbcCommand("SELECT FailedPasswordAttemptCount, " +
                                              "  FailedPasswordAttemptWindowStart, " +
                                              "  FailedPasswordAnswerAttemptCount, " +
                                              "  FailedPasswordAnswerAttemptWindowStart " +
                                              "  FROM Users " +
                                              "  WHERE Username = ? AND ApplicationName = ?", conn);

            cmd.Parameters.Add("@Username", OdbcType.VarChar, 255).Value = username;
            cmd.Parameters.Add("@ApplicationName", OdbcType.VarChar, 255).Value = pApplicationName;

            OdbcDataReader reader = null;
            DateTime windowStart = new DateTime();
            int failureCount = 0;

            try
            {
                conn.Open();

                reader = cmd.ExecuteReader(CommandBehavior.SingleRow);

                if (reader.HasRows)
                {
                    reader.Read();

                    if (failureType == "password")
                    {
                        failureCount = reader.GetInt32(0);
                        windowStart = reader.GetDateTime(1);
                    }

                    if (failureType == "passwordAnswer")
                    {
                        failureCount = reader.GetInt32(2);
                        windowStart = reader.GetDateTime(3);
                    }
                }

                reader.Close();

                DateTime windowEnd = windowStart.AddMinutes(PasswordAttemptWindow);

                if (failureCount == 0 || DateTime.Now > windowEnd)
                {
                    // First password failure or outside of PasswordAttemptWindow. 
                    // Start a new password failure count from 1 and a new window starting now.

                    if (failureType == "password")
                        cmd.CommandText = "UPDATE Users " +
                                          "  SET FailedPasswordAttemptCount = ?, " +
                                          "      FailedPasswordAttemptWindowStart = ? " +
                                          "  WHERE Username = ? AND ApplicationName = ?";

                    if (failureType == "passwordAnswer")
                        cmd.CommandText = "UPDATE Users " +
                                          "  SET FailedPasswordAnswerAttemptCount = ?, " +
                                          "      FailedPasswordAnswerAttemptWindowStart = ? " +
                                          "  WHERE Username = ? AND ApplicationName = ?";

                    cmd.Parameters.Clear();

                    cmd.Parameters.Add("@Count", OdbcType.Int).Value = 1;
                    cmd.Parameters.Add("@WindowStart", OdbcType.DateTime).Value = DateTime.Now;
                    cmd.Parameters.Add("@Username", OdbcType.VarChar, 255).Value = username;
                    cmd.Parameters.Add("@ApplicationName", OdbcType.VarChar, 255).Value = pApplicationName;

                    if (cmd.ExecuteNonQuery() < 0)
                        throw new ProviderException("Unable to update failure count and window start.");
                }
                else
                {
                    if (failureCount++ >= MaxInvalidPasswordAttempts)
                    {
                        // Password attempts have exceeded the failure threshold. Lock out
                        // the user.

                        cmd.CommandText = "UPDATE Users " +
                                          "  SET IsLockedOut = ?, LastLockedOutDate = ? " +
                                          "  WHERE Username = ? AND ApplicationName = ?";

                        cmd.Parameters.Clear();

                        cmd.Parameters.Add("@IsLockedOut", OdbcType.Bit).Value = true;
                        cmd.Parameters.Add("@LastLockedOutDate", OdbcType.DateTime).Value = DateTime.Now;
                        cmd.Parameters.Add("@Username", OdbcType.VarChar, 255).Value = username;
                        cmd.Parameters.Add("@ApplicationName", OdbcType.VarChar, 255).Value = pApplicationName;

                        if (cmd.ExecuteNonQuery() < 0)
                            throw new ProviderException("Unable to lock out user.");
                    }
                    else
                    {
                        // Password attempts have not exceeded the failure threshold. Update
                        // the failure counts. Leave the window the same.

                        if (failureType == "password")
                            cmd.CommandText = "UPDATE Users " +
                                              "  SET FailedPasswordAttemptCount = ?" +
                                              "  WHERE Username = ? AND ApplicationName = ?";

                        if (failureType == "passwordAnswer")
                            cmd.CommandText = "UPDATE Users " +
                                              "  SET FailedPasswordAnswerAttemptCount = ?" +
                                              "  WHERE Username = ? AND ApplicationName = ?";

                        cmd.Parameters.Clear();

                        cmd.Parameters.Add("@Count", OdbcType.Int).Value = failureCount;
                        cmd.Parameters.Add("@Username", OdbcType.VarChar, 255).Value = username;
                        cmd.Parameters.Add("@ApplicationName", OdbcType.VarChar, 255).Value = pApplicationName;

                        if (cmd.ExecuteNonQuery() < 0)
                            throw new ProviderException("Unable to update failure count.");
                    }
                }
            }
            catch (OdbcException e)
            {
                if (WriteExceptionsToEventLog)
                {
                    WriteToEventLog(e, "UpdateFailureCount");

                    throw new ProviderException(exceptionMessage);
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
            }*/
        }

        public override MembershipUserCollection FindUsersByName(string usernameToMatch, int pageIndex, int pageSize, out int totalRecords)
        {            
            MembershipUserCollection users = new MembershipUserCollection();
            try
            {
                TestGuruDBDataContext db = new TestGuruDBDataContext(connectionString);
                var allUsers = from i in db.Accounts
                               where i.Active == true && i.Deleted == false 
                               //&& SqlMethods.Like(i.Login, usernameToMatch)
                               orderby i.Login ascending
                               select i;

                int counter = 0;
                int startIndex = pageSize * pageIndex;
                int endIndex = startIndex + pageSize - 1;

                totalRecords = allUsers.Count();
                foreach (Account account in allUsers)
                {
                    if (counter >= startIndex)
                        users.Add(new TGMembershipUser("TGSqlMembershipProvider", account));

                    if (counter >= endIndex)
                        break;
                    ++counter;
                }
            }
            catch (Exception e)
            {
                if (WriteExceptionsToEventLog)
                {
                    WriteToEventLog(e, "Unhandled exception in FindUsersByName method");
                    throw new ProviderException(exceptionMessage);
                }
                else
                    throw e;
            }
            return users;           
        }

        public override MembershipUserCollection FindUsersByEmail(string emailToMatch, int pageIndex, int pageSize, out int totalRecords)
        {
            MembershipUserCollection users = new MembershipUserCollection();
            try
            {
                TestGuruDBDataContext db = new TestGuruDBDataContext(connectionString);
                var allUsers = from i in db.Accounts
                               where i.Active == true && i.Deleted == false 
                               //&& SqlMethods.Like(i.Login, emailToMatch)
                               orderby i.Login ascending
                               select i;

                int counter = 0;
                int startIndex = pageSize * pageIndex;
                int endIndex = startIndex + pageSize - 1;

                totalRecords = allUsers.Count();
                foreach (Account account in allUsers)
                {
                    if (counter >= startIndex)
                        users.Add(new TGMembershipUser("TGSqlMembershipProvider", account));

                    if (counter >= endIndex)
                        break;
                    ++counter;
                }
            }
            catch (Exception e)
            {
                if (WriteExceptionsToEventLog)
                {
                    WriteToEventLog(e, "Unhandled exception in FindUsersByEmail method");
                    throw new ProviderException(exceptionMessage);
                }
                else
                    throw e;
            }
            return users;   
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
