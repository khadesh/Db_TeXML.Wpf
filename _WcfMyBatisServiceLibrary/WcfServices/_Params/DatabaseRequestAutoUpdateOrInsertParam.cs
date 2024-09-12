using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace _WcfMyBatisServiceLibrary
{
    public class DatabaseRequestAutoUpdateOrInsertParam
    {
        private string xmlNamespace = null;
        public string XmlNamespace
        {
            get { return xmlNamespace; }
            set { xmlNamespace = value; }
        }

        private string requestSelect = null;
        public string RequestSelect
        {
            get { return requestSelect; }
            set { requestSelect = value; }
        }

        private string requestInsert = null;
        public string RequestInsert
        {
            get { return requestInsert; }
            set { requestInsert = value; }
        }

        private string requestUpdate = null;
        public string RequestUpdate
        {
            get { return requestUpdate; }
            set { requestUpdate = value; }
        }

        private Hashtable parameters = null;
        public Hashtable Parameters
        {
            get { return parameters; }
            set { parameters = value; }
        }
    }
}
