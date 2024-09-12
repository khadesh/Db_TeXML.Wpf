using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Runtime.Serialization;

namespace _WcfMyBatisServiceLibrary
{
    [DataContract]
    public class BaseResult
    {
        bool isSuccess = true;
        [DataMember]
        public bool IsSuccess
        {
            get { return isSuccess; }
            set { isSuccess = value; }
        }

        int returnCode = -2;
        [DataMember]
        public int ReturnCode
        {
            get { return returnCode; }
            set { returnCode = value; }
        }

        string message = "";
        [DataMember]
        public string Message
        {
            get { return message; }
            set { message = value; }
        }

        string source = "";
        [DataMember]
        public string Source
        {
            get { return source; }
            set { source = value; }
        }

        string stackTrace = "";
        [DataMember]
        public string StackTrace
        {
            get { return stackTrace; }
            set { stackTrace = value; }
        }

        public void SetError(Exception ex)
        {
            Type type = this.GetType();
            PropertyInfo propIsSuccess = this.GetType().GetProperty("IsSuccess", BindingFlags.Public | BindingFlags.Instance);
            if (null != propIsSuccess && propIsSuccess.CanWrite)
            {
                propIsSuccess.SetValue(this, false, null);
            }

            PropertyInfo propReturnCode = this.GetType().GetProperty("ReturnCode", BindingFlags.Public | BindingFlags.Instance);
            if (null != propReturnCode && propReturnCode.CanWrite)
            {
                propReturnCode.SetValue(this, -1, null);
            }

            PropertyInfo propMessage = this.GetType().GetProperty("Message", BindingFlags.Public | BindingFlags.Instance);
            if (null != propMessage && propMessage.CanWrite)
            {
                propMessage.SetValue(this, ex.Message, null);
            }

            PropertyInfo propSource = this.GetType().GetProperty("Source", BindingFlags.Public | BindingFlags.Instance);
            if (null != propSource && propSource.CanWrite)
            {
                propSource.SetValue(this, ex.Source, null);
            }

            PropertyInfo propStackTrace = this.GetType().GetProperty("StackTrace", BindingFlags.Public | BindingFlags.Instance);
            if (null != propStackTrace && propStackTrace.CanWrite)
            {
                propStackTrace.SetValue(this, ex.StackTrace, null);
            }
        }
    }
}
