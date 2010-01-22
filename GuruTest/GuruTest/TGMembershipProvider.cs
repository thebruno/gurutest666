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
        private int pAutoUnlockTime = 60;
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
            pAutoUnlockTime = Convert.ToInt32(GetConfigValue(config["autoUnlockTime"], "10"));
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
                throw new ProviderException("Connection string cannot be blank.");

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

        public int AutoUnlockTime
        {
            get { return pAutoUnlockTime; }
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

        /// <summary>
        /// Helper function to update user lock status and return if user is validated considering lock status
        /// </summary>
        /// <param name="userID">ID which belongs to user being validated, optional if userName passed</param>
        /// <param name="userName">Login which belongs to user begin validate, optional if ID passed</param>
        /// <param name="dataCorrect">True if user entered correct password/answer</param>
        /// <param name="AnswerNotPassword">True if this function is called due to answer (not password) validation</param>
        /// <param name="db">Data context to use, null if called function is to create and use it's own database connection</param>
        /// <returns>True if user gave correct validation data and was not locked out</returns>
        private bool CheckLock(int? userID, string userName, bool dataCorrect, bool AnswerNotPassword, TestGuruDBDataContext db)
        {
            DbConnection connection = null;
            bool useOwnConnection = (db == null);
            Account userOfConcern = null;
            // set to true causes function to commit changes before return
            bool performCommit = false;

            try
            {
                // create own connection if necessary
                if (useOwnConnection)
                {
                    DbProviderFactory factory = DbProviderFactories.GetFactory(factoryString);
                    connection = factory.CreateConnection();
                    connection.ConnectionString = connectionString;
                    connection.Open();
                    db = new TestGuruDBDataContext(connection);
                    db.Transaction = connection.BeginTransaction(IsolationLevel.RepeatableRead);
                }

                // check if user is locked out
                var result = from i in db.Accounts
                             where (userID != null ? i.ID == userID : true)
                             && (userName != null ? i.Login == userName : true)
                             && i.Deleted == false
                             && i.Active == true
                             select i;

                if (result.Count() <= 0)
                    // miraculously no such user found
                    return false;

                userOfConcern = result.First();

                if (dataCorrect)
                {
                    // data provided by user was correct
                    if (userOfConcern.Locked)
                    {
                        // check if lockout window hasn't expired
                        if (userOfConcern.LockDateTime.Value.AddMinutes(AutoUnlockTime) < DateTime.Now)
                        {
                            // clear lock
                            userOfConcern.Locked = false;
                            userOfConcern.LockDateTime = null;
                            userOfConcern.BadAnswerAttempts = 0;
                            userOfConcern.BadPasswordAttempts = 0;
                            // update database
                            db.SubmitChanges();
                            performCommit = true;
                            // user was locked, but provided correct data and window expired, so lock has been removed
                            return true;
                        }
                        else
                            // user provided correct data, but is locked out and window still has not expired
                            return false;
                    }
                    else
                    {
                        // clear bad attempt counter
                        userOfConcern.BadAnswerAttempts = 0;
                        userOfConcern.BadPasswordAttempts = 0;
                        // update database
                        db.SubmitChanges();
                        performCommit = true;
                        // user provided correct data and is not locked out
                        return true;
                    }
                }
                else
                {
                    // data provided by user was incorrect
                    if (userOfConcern.Locked)
                        // update lock window datetime
                        userOfConcern.LockDateTime = DateTime.Now;
                    else
                    {
                        if (AnswerNotPassword)
                        {
                            // clear bad attempt counter if it's window expired
                            if (userOfConcern.BadAnswerWindowStart.AddMinutes(pPasswordAttemptWindow) < DateTime.Now)
                            {
                                userOfConcern.BadAnswerWindowStart = DateTime.Now;
                                userOfConcern.BadAnswerAttempts = 0;
                            }

                            // update window start if bad data is given for the first time
                            if (AnswerNotPassword && userOfConcern.BadAnswerAttempts == 0)
                                userOfConcern.BadAnswerWindowStart = DateTime.Now;

                            // increment bad attempt counter
                            ++userOfConcern.BadAnswerAttempts;

                            // check if counter hasn't exceeded it's max value
                            if (userOfConcern.BadAnswerAttempts > pMaxInvalidPasswordAttempts)
                            {
                                // lock user
                                userOfConcern.Locked = true;
                                userOfConcern.LockDateTime = DateTime.Now;
                            }
                        }
                        else
                        {
                            // clear bad attempt counter if it's window expired
                            if (userOfConcern.BadPasswordWindowStart.AddMinutes(pPasswordAttemptWindow) < DateTime.Now)
                            {
                                userOfConcern.BadPasswordWindowStart = DateTime.Now;
                                userOfConcern.BadPasswordAttempts = 0;
                            }

                            // update window start if bad data is given for the first time
                            if (userOfConcern.BadPasswordAttempts == 0)
                                userOfConcern.BadPasswordWindowStart = DateTime.Now;

                            // increment bad attempt counter
                            ++userOfConcern.BadPasswordAttempts;

                            // check if counter hasn't exceeded it's max value
                            if (userOfConcern.BadPasswordAttempts > pMaxInvalidPasswordAttempts)
                            {
                                // lock user
                                userOfConcern.Locked = true;
                                userOfConcern.LockDateTime = DateTime.Now;
                            }
                        }
                    }
                    // update database
                    db.SubmitChanges();
                    performCommit = true;

                    return false;
                }
            }
            catch (Exception e)
            {
                if (WriteExceptionsToEventLog)
                {
                    WriteToEventLog(e, "Unhandled exception in UpdateLock method");
                    throw new ProviderException(exceptionMessage);
                }
                else
                    throw e;
            }
            finally
            {
                if (useOwnConnection)
                {
                    try
                    {
                        // end transaction
                        if (performCommit)
                            db.Transaction.Commit();
                        else
                            db.Transaction.Rollback();
                        // close database connection
                        connection.Close();
                    }
                    catch (Exception) { }
                }
            }
        }

        public override bool ChangePassword(string username, string oldPwd, string newPwd)
        {
            if (!CheckLock(null, username, ValidateUser(username, oldPwd), false, null))
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
                Account account = GetAccount(username, db);
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
            if (!CheckLock(null, username, ValidateUser(username, password), false, null))
                return false;

            try
            {
                TestGuruDBDataContext db = new TestGuruDBDataContext(connectionString);
                Account account = GetAccount(username, db);
                if (account != null)
                {
                    account.PasswordQuestion = newPwdQuestion;
                    account.PasswordAnswer = MD5.Create().ComputeHash(Encoding.Default.GetBytes(newPwdAnswer));
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
                  DbType.Binary
          };
                    object[] values = { username.ToLower(),
							    MD5.Create().ComputeHash(Encoding.Default.GetBytes(password)),
							    email,
							    Guid.NewGuid(),
							    Guid.NewGuid(),
                  passwordQuestion,
                  MD5.Create().ComputeHash(Encoding.Default.GetBytes(passwordAnswer))
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
                            // TODO: uncomment following line
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
            try
            {
                // create connection
                TestGuruDBDataContext db = new TestGuruDBDataContext(connectionString);

                // check if user is locked out
                var result = from i in db.Accounts
                             where username == i.Login && i.Deleted == false && i.Active == true
                             select i;

                if (result.Count() <= 0)
                    // no such user found
                    return false;

                Account userOfConcern = result.First();
                // clear lock
                userOfConcern.Locked = false;
                userOfConcern.LockDateTime = null;
                userOfConcern.BadAnswerAttempts = 0;
                userOfConcern.BadPasswordAttempts = 0;
                // update database
                db.SubmitChanges();

                return true;
            }
            catch (Exception e)
            {
                if (WriteExceptionsToEventLog)
                {
                    WriteToEventLog(e, "Unhandled exception in UnlockUser method");
                    throw new ProviderException(exceptionMessage);
                }
                else
                    throw e;
            }
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
            if (!EnablePasswordReset)
                throw new NotSupportedException("Password reset is not enabled.");

            if (answer == null && RequiresQuestionAndAnswer)
                throw new ProviderException("Password answer required for password reset.");

            // get new password
            string newPassword = System.Web.Security.Membership.GeneratePassword(newPasswordLength, MinRequiredNonAlphanumericCharacters);

            ValidatePasswordEventArgs args = new ValidatePasswordEventArgs(username, newPassword, true);
            OnValidatingPassword(args);
            if (args.Cancel)
                if (args.FailureInformation != null)
                    throw args.FailureInformation;
                else
                    throw new MembershipPasswordException("Reset password canceled due to password validation failure.");

            bool performCommit = false;
            TestGuruDBDataContext db = null;
            DbConnection connection = null;
            Account userOfConcern = null;

            try
            {
                // initialize database connection
                DbProviderFactory factory = DbProviderFactories.GetFactory(factoryString);
                connection = factory.CreateConnection();
                connection.ConnectionString = connectionString;
                connection.Open();
                db = new TestGuruDBDataContext(connection);
                db.Transaction = connection.BeginTransaction(IsolationLevel.RepeatableRead);

                // check if given answer is correct
                var result = from i in db.Accounts
                             where i.Login == username && i.Active && !i.Deleted
                             select i;

                if (result.Count() <= 0)
                    throw new MembershipPasswordException("The supplied user name is not found.");

                userOfConcern = result.First();

                if (!RequiresQuestionAndAnswer || CheckLock(null, username, userOfConcern.PasswordAnswer == MD5.Create().ComputeHash(Encoding.Default.GetBytes(answer)), true, db))
                {
                    // TODO: uncomment following line
                    //MailManagement.SendNewPasswordEmail(userOfConcern, newPassword);
                    // update password in database
                    userOfConcern.Password = MD5.Create().ComputeHash(Encoding.Default.GetBytes(newPassword));
                    db.SubmitChanges();
                    performCommit = true;

                    return newPassword;
                }
                else
                    throw new MembershipPasswordException("Answer is incorrect or user is locked out.");
            }
            catch (Exception e)
            {
                if (WriteExceptionsToEventLog)
                {
                    WriteToEventLog(e, "Unhandled exception in ResetPassword method");
                    throw new ProviderException(exceptionMessage);
                }
                else
                    throw e;
            }
            finally
            {
                try
                {
                    // end transaction
                    if (performCommit)
                        db.Transaction.Commit();
                    else
                        db.Transaction.Rollback();
                    // close database connection
                    connection.Close();
                }
                catch (Exception) { }
            }
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

        private Account GetAccount(string login, TestGuruDBDataContext db)
        {
            Account toReturn = null;
            try
            {
                if (db == null)
                    db = new TestGuruDBDataContext(connectionString);
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
            Account account = GetAccount(username, null);
            return (account != null && account.Active && CheckLock(account.ID, null, account.Password == MD5.Create().ComputeHash(Encoding.Default.GetBytes(password)), false, null));
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
