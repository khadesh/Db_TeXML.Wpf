#region References
using System;
using System.Runtime.Serialization;
#endregion

namespace _WcfMyBatisServiceLibrary
{
    public class SQLTimeOutException : System.ApplicationException
    {
        #region Class Contents
        /// <summary>
        /// メッセージ
        /// </summary>
        private const string message = "UECS.WebService.SQLTimeOutException";

        /// <summary>
        /// コンストラクターです。

        /// </summary>
        public SQLTimeOutException()
            : base(SQLTimeOutException.message)
        {
        }

        /// <summary>
        /// コンストラクターです。

        /// </summary>
        protected SQLTimeOutException(
                SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        /// <summary>
        /// コンストラクターです。

        /// </summary>
        public SQLTimeOutException(Exception innerException)
            : base(SQLTimeOutException.message, innerException)
        {
        }
        #endregion
    }
}