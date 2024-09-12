using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace _WcfMyBatisServiceLibrary
{
    [DataContract]
    public class DatabaseSelectResult : BaseResult
    {
        List<List<string>> rows;
        [DataMember]
        public List<List<string>> Rows
        {
            get { return rows; }
            set { rows = value; }
        }
    }
}
