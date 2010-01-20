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
                                  string comment) :
            base(providername,
                                       username,
                                       providerUserKey,
                                       email,
                                       passwordQuestion,
                                       comment,
                                       true,
                                       false,
                                       DateTime.Now,
                                       DateTime.Now,
                                       DateTime.Now,
                                       DateTime.Now,
                                       DateTime.Now)
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
                                       false,
                                       DateTime.Now,
                                       DateTime.Now,
                                       DateTime.Now,
                                       DateTime.Now,
                                       DateTime.Now)
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

        #region ISerializable Members

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {

            if (context.State == StreamingContextStates.CrossAppDomain)
            {
                info.SetType(typeof(TGMembershipUser));
                info.AddValue("m_name", "UnsereMembershipUser");
                //info.AddValue("IsApproved", true);
                info.AddValue("m_type", "TGMembershipUser");
                return;
            }
        }

        #endregion
    }
}
