#region References

using System.Collections.Generic;
using System.ServiceModel;
using System.Collections;
using System.Data;
using System.Web;
using System.Diagnostics;
using System.Xml;
using System.Web.SessionState;
using System.ServiceModel.Description;
using _WcfMyBatisServiceLibrary;

#endregion

namespace _WcfMyBatisUtilLibrary
{
    #region Summary + Examples
    /*******************************************************************************
     * 使い方の例：

     *******************************************************************************
        private void setCategories()
        {
            try
            {
                DatabaseServiceRequestor requestor = new DatabaseServiceRequestor();
                requestor.request(
                    "GET_ALL_CATEGORIES",
                    null,
                    null,
                    new EventHandler<WebServiceRequestCompletedEventArgs>(this.requestHandler)
                );
            }
            catch (Exception ex)
            {
                // ignore
            }
        }

        private void requestHandler(Object sender, WebServiceRequestCompletedEventArgs args)
        {
            try
            {
                this.Categories = DatabaseServiceRequestor.convertToListOfDictionary(args.Result);
            }
            catch (Exception ex)
            {
                // ignore
            }
        }
    *******************************************************************************/

    /// <summary>
    /// このクラスで、ウェブサービスを呼び出すことが簡単にできるし、
    /// 呼び出されたサービスのリザルトを使えるフォーマットにすることもできます。
    /// また、新しいサービスを作れば、こういうみたいなクラスをもう一つ作ったほうが良いと思います。
    /// 上に、使い方の例があります。参考にしてください。
    /// </summary>
    #endregion

    public class DatabaseServiceProxy : BaseServiceRequestor
    {
        #region WCF Service Communication Functions
        /// <summary>
        /// Directly calls an iBATIS Database Web Service and returns the results.
        /// </summary>

        public static DataTable SelectRequest(string xmlNamespace, string serviceName, Hashtable parameters)
        {
            string callInfo = DatabaseServiceProxy.GetParentCallInfo();
            return convertToDataTable(getClient().SelectRequest(callInfo, xmlNamespace, serviceName, parameters).Rows, "unnamed_table", false);
        }

        public static DatabaseRequestResult InsertRequest(string xmlNamespace, string serviceName, Hashtable parameters, HttpSessionState session)
        {
            string callInfo = DatabaseServiceProxy.GetParentCallInfo();
            return getClient().InsertRequest(callInfo, xmlNamespace, serviceName, AddGlobalProperties(parameters, session));
        }

        public static DatabaseRequestResult UpdateRequest(string xmlNamespace, string serviceName, Hashtable parameters, HttpSessionState session)
        {
            string callInfo = DatabaseServiceProxy.GetParentCallInfo();
            return getClient().UpdateRequest(callInfo, xmlNamespace, serviceName, AddGlobalProperties(parameters, session));
        }

        public static DatabaseRequestResult DeleteRequest(string xmlNamespace, string serviceName, Hashtable parameters, HttpSessionState session)
        {
            string callInfo = DatabaseServiceProxy.GetParentCallInfo();
            return getClient().DeleteRequest(callInfo, xmlNamespace, serviceName, AddGlobalProperties(parameters, session));
        }

        public static DatabaseRequestResult[] MultiRequest(DatabaseRequestParam[] databaseRequestParams, HttpSessionState session)
        {
            string callInfo = DatabaseServiceProxy.GetParentCallInfo();
            return getClient().MultiRequest(callInfo, convert(AddGlobalProperties(databaseRequestParams, session)));
        }

        public static DatabaseRequestResult[] MultiRequestAutoDetectInsertOrUpdate(DatabaseRequestAutoUpdateOrInsertParam[] databaseRequestParams, DatabaseRequestParam[] requestPreExecuteParams, HttpSessionState session)
        {
            string callInfo = DatabaseServiceProxy.GetParentCallInfo();
            return getClient().MultiRequestAutoDetectInsertOrUpdate(callInfo, convert(AddGlobalProperties(databaseRequestParams, session)), convert(AddGlobalProperties(requestPreExecuteParams, session)));
        }

        public static string GetParentCallInfo()
        {
            try
            {
                StackFrame frame = new StackFrame(2);
                var method = frame.GetMethod();
                var type = method.DeclaringType;
                var name = method.Name;

                return "{" + type + "." + name + "}";
            }
            catch { }
            return "{unknown}";
        }

        private static DataTable removeHtmlTags(DataTable dt)
        {
            if (dt == null)
            {
                return null;
            }

            foreach (DataRow row in dt.Rows)
            {
                foreach (DataColumn col in dt.Columns)
                {
                    row[col] = HttpUtility.HtmlEncode(row[col].ToString());
                }
            }
            return dt;
        }
        #endregion

        #region [Get Client]
        public static DatabaseServiceClient getClient()
        {
            // For connecting to a hosted WCF web service for data retrieval:
            /*
            var basicHttpBinding = new BasicHttpBinding();
            var endpointAddress = new EndpointAddress(Configurations.GetServiceUrl(new StackFrame(0).GetMethod().DeclaringType));
            basicHttpBinding.MaxReceivedMessageSize = 2147483647;
            basicHttpBinding.MaxBufferSize = 2147483647;
            basicHttpBinding.MaxBufferPoolSize = 2147483647;

            XmlDictionaryReaderQuotas readerQuotas = XmlDictionaryReaderQuotas.Max;
            basicHttpBinding.ReaderQuotas = readerQuotas;

            DatabaseServiceClient client = new DatabaseServiceClient(basicHttpBinding, endpointAddress);
            foreach (var operation in client.Endpoint.Contract.Operations)
            {
                var dataContractBehavior = operation.Behaviors.Find<DataContractSerializerOperationBehavior>();
                if (dataContractBehavior != null)
                {
                    dataContractBehavior.MaxItemsInObjectGraph = 2147483647;
                }
            }

            return client;
            */

            // For a stand alone application with no web service:
            return new DatabaseServiceClient();
        }
        #endregion

        #region Private Class Def: DatabaseServiceClient
        public class DatabaseServiceClient
        {
            private DatabaseService databaseService = new DatabaseService();

            public DatabaseSelectResult SelectRequest(string callInfo, string xmlNamespace, string request, Hashtable parameters)
            {
                return databaseService.SelectRequest(callInfo, xmlNamespace, request, parameters);
            }

            public DatabaseRequestResult InsertRequest(string callInfo, string xmlNamespace, string request, Hashtable parameters)
            {
                return databaseService.InsertRequest(callInfo, xmlNamespace, request, parameters);
            }

            public DatabaseRequestResult UpdateRequest(string callInfo, string xmlNamespace, string request, Hashtable parameters)
            {
                return databaseService.UpdateRequest(callInfo, xmlNamespace, request, parameters);
            }

            public DatabaseRequestResult DeleteRequest(string callInfo, string xmlNamespace, string request, Hashtable parameters)
            {
                return databaseService.DeleteRequest(callInfo, xmlNamespace, request, parameters);
            }

            public DatabaseRequestResult[] MultiRequest(string callInfo, List<DatabaseRequestParam> databaseRequestParams)
            {
                return databaseService.MultiRequest(callInfo, databaseRequestParams);
            }

            public DatabaseRequestResult[] MultiRequestAutoDetectInsertOrUpdate(string callInfo, List<DatabaseRequestAutoUpdateOrInsertParam> databaseRequestParams, List<DatabaseRequestParam> requestPreExecuteParams)
            {
                return databaseService.MultiRequestAutoDetectInsertOrUpdate(callInfo, databaseRequestParams, requestPreExecuteParams);
            }
        }
        #endregion
    }
}