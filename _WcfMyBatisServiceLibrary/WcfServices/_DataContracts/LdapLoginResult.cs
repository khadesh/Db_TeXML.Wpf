using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace _WcfMyBatisServiceLibrary
{
    [DataContract]
    public class LdapLoginResult : BaseResult
    {
        bool loginSuccess = false;
        [DataMember]
        public bool LoginSuccess
        {
            get { return loginSuccess; }
            set { loginSuccess = value; }
        }

        string userName = "";
        [DataMember]
        public string UserName
        {
            get { return userName; }
            set { userName = value; }
        }
    }
}
