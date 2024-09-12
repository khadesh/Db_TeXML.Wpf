#region References
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Collections;
#endregion

namespace _WcfMyBatisServiceLibrary
{
    [ServiceContract]
    public interface IGeneralService
    {
        [OperationContract]
        LdapLoginResult LDAP_LoginUser(string username, string password);
    }
}