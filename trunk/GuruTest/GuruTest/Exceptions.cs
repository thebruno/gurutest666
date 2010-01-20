using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GuruTest
{
    //Kody błędów:
    //1 - Próba usunięcia ostatniego administratora
    //2 - Próba edycji rekordu w tabeli AccountRole
    //3 - Próba wstawienia usera z zastrzeżonym e-mailem
    //4 - Próba wstawienia usera z powtarzającym się loginem

    /// <summary>
    /// Occures when last account with administrator privileges is being removed.
    /// </summary>
    public class AttemptToRemoveLastAdminException : Exception
    {
        public AttemptToRemoveLastAdminException(string message, Exception innerException)
            : base(message, innerException)
        { }
    }

    /// <summary>
    /// Occurs when role is being edited - only insertion and deletion is possible
    /// </summary>
    public class AttemptToEditAccountRoleTableException : Exception
    {
        public AttemptToEditAccountRoleTableException(string message, Exception innerException)
            : base(message, innerException)
        { }
    }

    /// <summary>
    /// Occurs when somebody wants to use email which exists on restricted list
    /// </summary>
    public class AttemptToUseRestrictedEmailException : Exception
    {
        public AttemptToUseRestrictedEmailException(string message, Exception innerException)
            : base(message, innerException)
        { }
    }

    /// <summary>
    /// Occures when somebody wants to insert account with the same login
    /// </summary>
    public class AttemptToDuplicateLoginException : Exception
    {
        public AttemptToDuplicateLoginException(string message, Exception innerException)
            : base(message, innerException)
        { }
    }
}
