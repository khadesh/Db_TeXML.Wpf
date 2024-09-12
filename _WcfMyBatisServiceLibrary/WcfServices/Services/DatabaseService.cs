#region References
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using IBatisNet.Common.Logging;
using System.Collections;
using System.Text.RegularExpressions;
using System.Reflection;
#endregion

namespace _WcfMyBatisServiceLibrary
{
    /// <summary>
    /// 
    /// </summary>
    public class DatabaseService : BaseService, IDatabaseService
    {
        #region Properties
        private static readonly ILog databaseLogger = LogManager.GetLogger("IBatisNet");
        #endregion

        #region Web Service Requests [OperationContracts]
        public DatabaseSelectResult SelectRequest(string callInfo, string xmlNamespace, string request, Hashtable parameters)
        {
            DatabaseSelectResult result = new DatabaseSelectResult();
            List<List<string>> returnLists = new List<List<string>>();
            SQLMap sqlMap = new SQLMap();
            databaseLogger.Debug("[DatabaseService]SelectRequest Go");
            databaseLogger.Debug("[Origin(Class.Method)=" + callInfo + "]");

            try
            {
                sqlMap.StartTransaction();
                parameters = ConvertDateTimesToProperFormat(parameters);
                returnLists = this.DataRequest(xmlNamespace, request, sqlMap, returnLists, parameters);
                result.Rows = returnLists;
                sqlMap.CommitTransaction();
            }
            catch (Exception ex)
            {
                databaseLogger.Debug("[DatabaseService]SelectRequest ERROR", ex);
                result.SetError(ex);
                sqlMap.RollbackTransaction();
            }

            databaseLogger.Debug("[DatabaseService]SelectRequest Finish");
            return result;
        }

        public DatabaseRequestResult InsertRequest(string callInfo, string xmlNamespace, string request, Hashtable parameters)
        {
            DatabaseRequestResult requestResult = new DatabaseRequestResult();
            SQLMap sqlMap = new SQLMap();
            databaseLogger.Debug("[DatabaseService]InsertRequest Go");
            databaseLogger.Debug("[Origin(Class.Method)=" + callInfo + "]");

            try
            {
                sqlMap.StartTransaction();
                parameters = ConvertDateTimesToProperFormat(parameters);
                requestResult.ReturnCode = sqlMap.InsertRecord(xmlNamespace + "." + request, parameters);
                sqlMap.CommitTransaction();
            }
            catch (Exception ex)
            {
                databaseLogger.Debug("[DatabaseService]InsertRequest ERROR", ex);
                requestResult.SetError(ex);
                sqlMap.RollbackTransaction();
            }

            databaseLogger.Debug("[DatabaseService]InsertRequest Finish");
            return requestResult;
        }

        public DatabaseRequestResult UpdateRequest(string callInfo, string xmlNamespace, string request, Hashtable parameters)
        {
            DatabaseRequestResult requestResult = new DatabaseRequestResult();
            SQLMap sqlMap = new SQLMap();
            databaseLogger.Debug("[DatabaseService]UpdateRequest Go");
            databaseLogger.Debug("[Origin(Class.Method)=" + callInfo + "]");

            try
            {
                sqlMap.StartTransaction();
                parameters = ConvertDateTimesToProperFormat(parameters);
                requestResult.ReturnCode = sqlMap.UpdateRecord(xmlNamespace + "." + request, parameters);
                sqlMap.CommitTransaction();
            }
            catch (Exception ex)
            {
                databaseLogger.Debug("[DatabaseService]UpdateRequest ERROR", ex);
                requestResult.SetError(ex);
                sqlMap.RollbackTransaction();
            }

            databaseLogger.Debug("[DatabaseService]UpdateRequest Finish");
            return requestResult;
        }

        public DatabaseRequestResult DeleteRequest(string callInfo, string xmlNamespace, string request, Hashtable parameters)
        {
            DatabaseRequestResult requestResult = new DatabaseRequestResult();
            SQLMap sqlMap = new SQLMap();
            databaseLogger.Debug("[DatabaseService]DeleteRequest Go");
            databaseLogger.Debug("[Origin(Class.Method)=" + callInfo + "]");

            try
            {
                sqlMap.StartTransaction();
                parameters = ConvertDateTimesToProperFormat(parameters);
                requestResult.ReturnCode = sqlMap.DeleteRecord(xmlNamespace + "." + request, parameters);
                sqlMap.CommitTransaction();
            }
            catch (Exception ex)
            {
                databaseLogger.Debug("[DatabaseService]DeleteRequest ERROR", ex);
                requestResult.SetError(ex);
                sqlMap.RollbackTransaction();
            }

            databaseLogger.Debug("[DatabaseService]DeleteRequest Finish");
            return requestResult;
        }

        public DatabaseRequestResult[] MultiRequest(string callInfo, List<DatabaseRequestParam> databaseRequestParams)
        {
            ArrayList results = new ArrayList();
            SQLMap sqlmap = new SQLMap();
            databaseLogger.Debug("[DatabaseService]MultiRequest Go");
            databaseLogger.Debug("[Origin(Class.Method)=" + callInfo + "]");

            // 引数チェック
            if (databaseRequestParams == null)
            {
                throw new ArgumentNullException();
            }

            if (databaseRequestParams.Count == 0)
            {
                throw new ArgumentException();
            }

            foreach (DatabaseRequestParam databaseRequestParam in databaseRequestParams)
            {
                if (databaseRequestParam.XmlNamespace == null ||
                    databaseRequestParam.Request == null ||
                    databaseRequestParam.Parameters == null ||
                    databaseRequestParam.Type == null)
                {
                    throw new ArgumentNullException();
                }
            }

            bool isAbort = false;
            sqlmap.StartTransactionSerializable();
            for (int i = 0; i < databaseRequestParams.Count; i++)
            {
                DatabaseRequestResult requestResult = new DatabaseRequestResult();
                try
                {
                    switch (databaseRequestParams[i].Type)
                    {
                        case DatabaseServiceRequestType.INSERT:
                            requestResult.ReturnCode = sqlmap.InsertRecord(
                                databaseRequestParams[i].XmlNamespace + "." + databaseRequestParams[i].Request, 
                                ConvertDateTimesToProperFormat(databaseRequestParams[i].Parameters));
                            break;
                        case DatabaseServiceRequestType.UPDATE:
                            requestResult.ReturnCode = sqlmap.UpdateRecord(
                                databaseRequestParams[i].XmlNamespace + "." + databaseRequestParams[i].Request,
                                ConvertDateTimesToProperFormat(databaseRequestParams[i].Parameters));
                            break;
                        case DatabaseServiceRequestType.DELETE:
                            requestResult.ReturnCode = sqlmap.DeleteRecord(
                                databaseRequestParams[i].XmlNamespace + "." + databaseRequestParams[i].Request,
                                ConvertDateTimesToProperFormat(databaseRequestParams[i].Parameters));
                            break;

                    }
                }
                catch (Exception ex)
                {
                    databaseLogger.Debug("[DatabaseService]MultiRequest ERROR", ex);
                    isAbort = true;
                    requestResult.SetError(ex);
                }
                results.Add(requestResult);
            }

            if (isAbort)
            {
                sqlmap.RollbackTransaction();
            }
            else
            {
                sqlmap.CommitTransaction();
            }

            databaseLogger.Debug("[DatabaseService]MultiRequest Finish");
            return (DatabaseRequestResult[])results.ToArray(typeof(DatabaseRequestResult));
        }


        public DatabaseRequestResult[] MultiRequestAutoDetectInsertOrUpdate(string callInfo, List<DatabaseRequestAutoUpdateOrInsertParam> databaseRequestParams, List<DatabaseRequestParam> requestPreExecuteParams)
        {
            ArrayList results = new ArrayList();
            SQLMap sqlmap = new SQLMap();
            databaseLogger.Debug("[DatabaseService]MultiRequestAutoDetectInsertOrUpdate Go");
            databaseLogger.Debug("[Origin(Class.Method)=" + callInfo + "]");

            // 引数チェック
            if (databaseRequestParams == null)
            {
                throw new ArgumentNullException();
            }

            if (databaseRequestParams.Count == 0)
            {
                throw new ArgumentException();
            }

            foreach (DatabaseRequestAutoUpdateOrInsertParam databaseRequestParam in databaseRequestParams)
            {
                if (databaseRequestParam.XmlNamespace == null ||
                    databaseRequestParam.RequestSelect == null ||
                    databaseRequestParam.RequestInsert == null ||
                    databaseRequestParam.RequestUpdate == null ||
                    databaseRequestParam.Parameters == null)
                {
                    throw new ArgumentNullException();
                }
            }

            #region Execute Pre Update Delete Requests
            // First, execute the clear data script:
            bool isAbort = false;
            try
            {
                sqlmap.StartTransactionSerializable();
                for (int i = 0; i < requestPreExecuteParams.Count; i++)
                {
                    DatabaseRequestResult requestResult = new DatabaseRequestResult();
                    try
                    {
                        switch (requestPreExecuteParams[i].Type)
                        {
                            case DatabaseServiceRequestType.INSERT:
                                requestResult.ReturnCode = sqlmap.UpdateRecord(
                                    requestPreExecuteParams[i].XmlNamespace + "." + requestPreExecuteParams[i].Request,
                                    ConvertDateTimesToProperFormat(requestPreExecuteParams[i].Parameters));
                                break;
                            case DatabaseServiceRequestType.UPDATE:
                                requestResult.ReturnCode = sqlmap.UpdateRecord(
                                    requestPreExecuteParams[i].XmlNamespace + "." + requestPreExecuteParams[i].Request,
                                    ConvertDateTimesToProperFormat(requestPreExecuteParams[i].Parameters));
                                break;
                            case DatabaseServiceRequestType.DELETE:
                                requestResult.ReturnCode = sqlmap.DeleteRecord(
                                    requestPreExecuteParams[i].XmlNamespace + "." + requestPreExecuteParams[i].Request,
                                    ConvertDateTimesToProperFormat(requestPreExecuteParams[i].Parameters));
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        databaseLogger.Debug("[DatabaseService]MultiRequestAutoDetectInsertOrUpdate ERROR", ex);
                        isAbort = true;
                        requestResult.SetError(ex);
                    }
                    if (!requestResult.IsSuccess)
                    {
                        isAbort = true;
                    }
                    results.Add(requestResult);
                }
            }
            catch (Exception ex)
            {
                databaseLogger.Debug("[DatabaseService]MultiRequestAutoDetectInsertOrUpdate ERROR", ex);
            }
            #endregion

            #region Execute Multi Update / Insert Requests
            if (!isAbort)
            {
                for (int i = 0; i < databaseRequestParams.Count; i++)
                {
                    DatabaseRequestResult requestResult = new DatabaseRequestResult();
                    try
                    {
                        Hashtable parameters = ConvertDateTimesToProperFormat(databaseRequestParams[i].Parameters);

                        List<List<string>> returnLists = this.DataRequest(
                            databaseRequestParams[i].XmlNamespace,
                            databaseRequestParams[i].RequestSelect,
                            sqlmap,
                            new List<List<string>>(),
                            parameters
                        );

                        DatabaseServiceRequestType requestType = DatabaseServiceRequestType.NONE;

                        if (returnLists == null)
                        {
                            requestType = DatabaseServiceRequestType.INSERT;
                        }
                        else if (returnLists.Count == 2)
                        {
                            requestType = DatabaseServiceRequestType.UPDATE;
                        }
                        else
                        {
                            throw new Exception("MultiRequestAutoDetectInsertOrUpdate found more than one row for update.");
                        }

                        switch (requestType)
                        {
                            case DatabaseServiceRequestType.INSERT:
                                requestResult.ReturnCode = sqlmap.InsertRecord(
                                    databaseRequestParams[i].XmlNamespace + "." + databaseRequestParams[i].RequestInsert,
                                    parameters);
                                break;
                            case DatabaseServiceRequestType.UPDATE:
                                requestResult.ReturnCode = sqlmap.UpdateRecord(
                                    databaseRequestParams[i].XmlNamespace + "." + databaseRequestParams[i].RequestUpdate,
                                    parameters);
                                break;

                        }
                    }
                    catch (Exception ex)
                    {
                        databaseLogger.Debug("[DatabaseService]MultiRequestAutoDetectInsertOrUpdate ERROR", ex);
                        isAbort = true;
                        requestResult.SetError(ex);
                    }
                    results.Add(requestResult);
                }
            }
            #endregion

            // Determine Rollback or Commit:
            if (isAbort)
            {
                sqlmap.RollbackTransaction();
            }
            else
            {
                sqlmap.CommitTransaction();
            }

            databaseLogger.Debug("[DatabaseService]MultiRequest Finish");
            return (DatabaseRequestResult[])results.ToArray(typeof(DatabaseRequestResult));
        }
        #endregion

        #region Data Request Service Call
        public List<List<string>> DataRequest(string xmlNamespace, string request, SQLMap sqlmap, List<List<string>> returnLists, Hashtable parameters)
        {
            databaseLogger.Debug("DatabaseService.DataRequest Start: " + request);
            returnLists = sqlmap.FindRecords(xmlNamespace + "." + request, parameters);
            databaseLogger.Debug("DatabaseService.DataRequest Fin - Success");
            return returnLists;
        }
        #endregion

        #region Convert Dictionary To Hashtable
        private Hashtable ConvertDictionaryToHashtable(Dictionary<string, string> dictionary)
        {
            Hashtable returnHash = new Hashtable();
            if (dictionary != null)
            {
                foreach (string key in dictionary.Keys)
                {
                    if (dictionary[key] != null)
                    {
                        returnHash.Add(key, DateTimeConverstion(dictionary[key].ToString()));
                    }
                    else
                    {
                        returnHash.Add(key, dictionary[key]);
                    }
                }
            }
            return returnHash;
        }
        #endregion

        #region Convert DateTimes To Proper Format
        private Hashtable ConvertDateTimesToProperFormat(Hashtable hash)
        {
            Hashtable returnHash = new Hashtable();
            if (hash != null)
            {
                foreach (string key in hash.Keys)
                {
                    if (hash[key] != null)
                    {
                        returnHash.Add(key, DateTimeConverstion(hash[key].ToString()));
                    }
                    else
                    {
                        returnHash.Add(key, hash[key]);
                    }
                }
            }
            return returnHash;
        }
        #endregion

        #region DateTime Conversion
        private string DateTimeConverstion(string value)
        {
            if (Regex.IsMatch(value, @"^0001\/01\/01 0:00:00$"))
            {
                return null;
            }
            else if (
                Regex.IsMatch(value, @"^[\d][\d][\d][\d].[\d][\d].[\d][\d].[\d]:[\d][\d]:[\d][\d]$") ||
                Regex.IsMatch(value, @"^[\d][\d][\d][\d].[\d][\d].[\d][\d].[\d][\d]:[\d][\d]:[\d][\d]$") ||
                Regex.IsMatch(value, @"^[\d][\d][\d][\d].[\d][\d].[\d][\d]$")
                )
            {
                return value.Replace('/', '-').Replace("-", "");
            }
            return value;
        }
        #endregion

        #region Base64 Conversions
        private string EncodeTo64(byte[] toEncodeAsBytes)
        {
            return System.Convert.ToBase64String(toEncodeAsBytes);
        }

        private byte[] DecodeFrom64(string toDecodeString)
        {
            return System.Convert.FromBase64String(toDecodeString);
        }
        #endregion
    }
}