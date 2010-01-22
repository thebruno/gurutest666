using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Runtime.Serialization;

namespace GuruTest
{
    [Serializable]
    public class TGMembershipUser : MembershipUser, ISerializable
    {
        public bool Active { get; set; }
        public bool Deleted { get; set; }
        public Guid ActivationGUID { get; set; }
        public Guid TerminationGUID { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string City { get; set; }
        public DateTime Created { get; set; }
        public TGMembershipUser(string providername,
                                  string username,
                                  object providerUserKey,
                                  string email,
                                  string passwordQuestion,
                                  string comment,
                                  bool IsLockedOut,
                                  DateTime lastLockoutDate) :
            base(providername,
                                  username,
                                  providerUserKey,
                                  email,
                                  passwordQuestion,
                                  comment,
                                  true,
                                  IsLockedOut,
                                  DateTime.Now,
                                  DateTime.Now,
                                  DateTime.Now,
                                  DateTime.Now,
                                  lastLockoutDate)
        {
        }

        public TGMembershipUser(string providername,
                                  Account account) :
            base(providername,
                                  account.Login,
                                  account.ID,
                                  account.Email,
                                  account.PasswordQuestion,
                                  "",
                                  true,
                                  account.Locked,
                                  DateTime.Now,
                                  DateTime.Now,
                                  DateTime.Now,
                                  DateTime.Now,
                                  account.LockDateTime == null ? DateTime.Now : account.LockDateTime.Value)
        {
            Active = account.Active;
            Deleted = account.Deleted;
            ActivationGUID = account.ActivationGUID;
            TerminationGUID = account.TerminationGUID;
            FirstName = account.FirstName;
            LastName = account.LastName;
            City = account.City;
            Created = account.Created;
        }
        public TGMembershipUser(SerializationInfo info, StreamingContext context)
        {
            // get the set of serializable members for our class and base classes
            Type thisType = this.GetType();
            string exps = "";

            System.Reflection.MemberInfo[] mi = thisType.GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

            // DeSerialize all of the allowed fields and handle any of the removed fields.
            foreach (System.Reflection.MemberInfo member in mi)
            {
                try
                {
                    ((System.Reflection.FieldInfo)member).SetValue(this, info.GetValue(member.Name,
          ((System.Reflection.FieldInfo)member).FieldType));
                }
                catch (Exception e)
                {
                    // resolve upgrade issues here
                    switch (member.Name)
                    {
                        case "AddEvent":
                            // Handle the Event properties
                            continue;
                    }

                    exps += String.Format("\nError during deserialization setting Member name: {0} : {1}", member.Name, e.Message);
                }
            }

            // this.InnerList = info.GetString("k");
            System.Diagnostics.Debug.WriteLine(exps);
        }

        #region ISerializable Members

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {

            /*if (context.State == StreamingContextStates.CrossAppDomain)
            {
                info.SetType(typeof(TGMembershipUser));
                info.AddValue("m_name", "UnsereMembershipUser");
                //info.AddValue("IsApproved", true);
                info.AddValue("m_type", "TGMembershipUser");
                return;
            }*/
            // get the set of serializable members for our class and base classes

            Type thisType = this.GetType();
            System.Reflection.MemberInfo[] mi = thisType.GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

            foreach (System.Reflection.MemberInfo member in mi)
            {
                info.AddValue(member.Name, ((System.Reflection.FieldInfo)member).GetValue(this));
            }
        }

        #endregion
    }
}
