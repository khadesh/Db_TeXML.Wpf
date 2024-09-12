using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace _WcfMyBatisServiceLibrary
{
    [DataContract]
    public class DatabaseSelectFileResult : BaseResult
    {
        byte[] fileBytes;
        [DataMember]
        public byte[] FileBytes
        {
            get { return fileBytes; }
            set { fileBytes = value; }
        }

        string fileName;
        [DataMember]
        public string FileName
        {
            get { return fileName; }
            set { fileName = value; }
        }

        bool isPathed;
        [DataMember]
        public bool IsPathed
        {
            get { return isPathed; }
            set { isPathed = value; }
        }

        string filePath;
        [DataMember]
        public string Path
        {
            get { return filePath; }
            set { filePath = value; }
        }
    }
}
