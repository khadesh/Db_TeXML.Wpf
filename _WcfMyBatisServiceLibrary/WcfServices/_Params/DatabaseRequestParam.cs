using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace _WcfMyBatisServiceLibrary
{
    public class DatabaseRequestParam
    {
        private string xmlNamespace = null;
        public string XmlNamespace
        {
            get { return xmlNamespace; }
            set { xmlNamespace = value; }
        }

        private string request = null;
        public string Request
        {
            get { return request; }
            set { request = value; }
        }

        private Hashtable parameters = null;
        public Hashtable Parameters
        {
            get { return parameters; }
            set { parameters = value; }
        }

        private DatabaseServiceRequestType type = DatabaseServiceRequestType.NONE;
        public DatabaseServiceRequestType Type
        {
            get { return type; }
            set { type = value; }
        }
    }
}
