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
    public interface IDatabaseService
    {
        [OperationContract]
        DatabaseSelectResult SelectRequest(string callInfo, string xmlNamespace, string request, Hashtable parameters);

        [OperationContract]
        DatabaseRequestResult InsertRequest(string callInfo, string xmlNamespace, string request, Hashtable parameters);

        [OperationContract]
        DatabaseRequestResult UpdateRequest(string callInfo, string xmlNamespace, string request, Hashtable parameters);
        
        [OperationContract]
        DatabaseRequestResult DeleteRequest(string callInfo, string xmlNamespace, string request, Hashtable parameters);

        [OperationContract]
        DatabaseRequestResult[] MultiRequest(string callInfo, List<DatabaseRequestParam> databaseRequestParams);

        [OperationContract]
        DatabaseRequestResult[] MultiRequestAutoDetectInsertOrUpdate(string callInfo, List<DatabaseRequestAutoUpdateOrInsertParam> databaseRequestParams, List<DatabaseRequestParam> requestPreExecuteParams);
    }
}