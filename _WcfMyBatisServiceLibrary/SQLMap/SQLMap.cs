#region References
using System;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Configuration;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Xml;
using IBatisNet.Common.Utilities;
using IBatisNet.DataMapper;
using IBatisNet.DataMapper.Configuration;
using log4net;
using log4net.Config;
using System.Text.RegularExpressions;
using System.Collections.Specialized;
using System.Threading;
using System.Text;
using System.Web.Configuration;
#endregion

namespace _WcfMyBatisServiceLibrary
{
    public class SQLMap
    {
        public enum DatabaseType { POSTGRES, MYSQL, SQLSERVER, ORACLE };

        #region Private Variables
        private const int TIME_OUT = -2;
        private bool transaction = false;
        private ISqlMapper sqlMap = null;
        private static readonly ILog log = LogManager.GetLogger("IBatisNet");

        // Log Messages:
        private static readonly string openConnection;
        private static readonly string closeConnection;
        private static readonly string transactionStartMessage;
        private static readonly string transactionNoMessage;
        private static readonly string commitMessage;
        private static readonly string rollbackMessage;
        private static readonly string callMessage;
        private static readonly string changedRecordMessage;
        #endregion

        #region Connection Details
        private static DatabaseType DbType = DatabaseType.POSTGRES;
        private static string Server = "";
        private static string Port = "";
        private static string Database = "";
        private static string UserId = "";
        private static string Password = "";

        public static void SetConnectionDetails(string server, string port, string database, string userId, string password, DatabaseType dbType)
        {
            SQLMap.DbType = dbType;
            SQLMap.Server = server;
            SQLMap.Port = port;
            SQLMap.Database = database;
            SQLMap.UserId = userId;
            SQLMap.Password = password;
        }
        #endregion

        #region Constructor(s)
        static SQLMap()
        {
            Type type = typeof(SQLMap);

            openConnection = "[opening connection]";
            closeConnection = "[closing connection]";
            transactionStartMessage = "[transaction start]";
            transactionNoMessage = "[error : attempting to commit a transaction that has not started]";
            commitMessage = "[transaction commit]";
            rollbackMessage = "[transaction rollback]";
            callMessage = "[sql call]";
            changedRecordMessage = "[sql update complete] updated rows: ";

            //IBatisNet.DataMapper.
        }

        public SQLMap()
        {
            SQLMapEmptyConstructor(false);
        }

        public SQLMap(bool useDefaults)
        {
            SQLMapEmptyConstructor(useDefaults);
        }

        private void SQLMapEmptyConstructor(bool useDefaults)
        {
            try
            {
                if (useDefaults)
                {
                    // Load from the config file:
                    sqlMap = Mapper.Instance();
                }
                else
                {
                    // Load the config file (embedded resource in assembly).
                    System.Xml.XmlDocument xmlDoc = IBatisNet.Common.Utilities.Resources.GetConfigAsXmlDocument("SQLMap.config");

                    // Overwrite the connectionString in the XmlDocument, hence changing database.
                    // NB if your connection string needs extra parameters,
                    // such as `Integrated Security=SSPI;` for user authentication,
                    // then append that to InnerText too.
                    switch (SQLMap.DbType)
                    {
                        case DatabaseType.POSTGRES:
                            xmlDoc["sqlMapConfig"]["database"]["provider"]
                                .Attributes["name"].InnerText = "PostgreSql2.2.3.0";
                            xmlDoc["sqlMapConfig"]["database"]["dataSource"]
                                .Attributes["connectionString"]
                                .InnerText =
                                    "Server=" + SQLMap.Server +
                                    ";Port=" + SQLMap.Port +
                                    ";Database=" + SQLMap.Database +
                                    ";User Id=" + SQLMap.UserId +
                                    ";Password=" + SQLMap.Password +
                                    ";Encoding=UTF8;";
                            break;
                        case DatabaseType.MYSQL:
                            xmlDoc["sqlMapConfig"]["database"]["provider"]
                                .Attributes["name"].InnerText = "MySql";
                            xmlDoc["sqlMapConfig"]["database"]["dataSource"]
                                .Attributes["connectionString"]
                                .InnerText =
                                    "Server=" + SQLMap.Server +
                                    ";Port=" + SQLMap.Port +
                                    ";Database=" + SQLMap.Database +
                                    ";Uid=" + SQLMap.UserId +
                                    ";Pwd=" + SQLMap.Password +
                                    ";";
                            break;
                        case DatabaseType.SQLSERVER:
                            xmlDoc["sqlMapConfig"]["database"]["provider"]
                                .Attributes["name"].InnerText = "sqlServer2.0";
                            if (SQLMap.Port == null || SQLMap.Port.Equals(""))
                            {

                                xmlDoc["sqlMapConfig"]["database"]["dataSource"]
                                    .Attributes["connectionString"]
                                    .InnerText =
                                        "Server=" + SQLMap.Server +
                                        ";Database=" + SQLMap.Database +
                                        ";User Id=" + SQLMap.UserId +
                                        ";Password=" + SQLMap.Password +
                                        ";";
                            }
                            else
                            {
                                xmlDoc["sqlMapConfig"]["database"]["dataSource"]
                                    .Attributes["connectionString"]
                                    .InnerText =
                                        "Data Source=" + SQLMap.Server + "," + SQLMap.Port + ";Network Library=DBMSSOCN;" +
                                        ";Initial Catalog=" + SQLMap.Database +
                                        ";User Id=" + SQLMap.UserId +
                                        ";Password=" + SQLMap.Password +
                                        ";";
                            }
                            break;
                        case DatabaseType.ORACLE:
                            xmlDoc["sqlMapConfig"]["database"]["provider"]
                                .Attributes["name"].InnerText = "oracle2.121.2.0";
                            xmlDoc["sqlMapConfig"]["database"]["dataSource"]
                                .Attributes["connectionString"]
                                .InnerText =
                                //JETRO=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(COMMUNITY=TCP)(PROTOCOL=TCP)(HOST=192.168.2.244)(PORT=1521)))(CONNECT_DATA=(SID=XE)))
                                    //"Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(COMMUNITY=TCP)(PROTOCOL=TCP)(HOST=" + SQLMap.Server + ")(PORT=" + SQLMap.Port + ")))(CONNECT_DATA=(SID=" + SQLMap.Database + ")))" +
                                    "Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=" + SQLMap.Server + ")(PORT=" + SQLMap.Port + "))(CONNECT_DATA=(SERVICE_NAME=" + SQLMap.Database + ")))" +
                                    ";User Id=" + SQLMap.UserId +
                                    ";Password=" + SQLMap.Password +
                                    ";";
                            //SERVER=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=MyHost)(PORT=MyPort))(CONNECT_DATA=(SERVICE_NAME=MyOracleSID)));
                            //uid=myUsername;pwd=myPassword;
                            break;
                    }
                    // Instantiate Ibatis mapper using the XmlDocument via a Builder,
                    // instead of Ibatis using the config file.
                    IBatisNet.DataMapper.Configuration.DomSqlMapBuilder builder = new IBatisNet.DataMapper.Configuration.DomSqlMapBuilder();
                    sqlMap = builder.Configure(xmlDoc);
                }
            }
            catch (SqlException err)
            {

                if (err.Number == TIME_OUT)
                {
                    outputWern(err);
                    throw new SQLTimeOutException(err);
                }
                else
                {
                    outputErr(err);
                }

                throw err;
            }
            catch (Exception err)
            {
                outputErr(err);
                throw err;
            }
        }
        #endregion

        #region Open / Close Connection
        public void OpenConnection()
        {
            log.Debug(openConnection);
            transaction = true;

            try
            {
                sqlMap.OpenConnection();
            }
            catch (SqlException err)
            {

                if (err.Number == TIME_OUT)
                {
                    outputWern(err);
                    throw new SQLTimeOutException(err);
                }
                else
                {
                    outputErr(err);
                }

                throw err;
            }
            catch (Exception err)
            {
                outputErr(err);
                throw err;
            }
        }

        public void CloseConnection()
        {
            log.Debug(closeConnection);
            transaction = true;

            try
            {
                sqlMap.CloseConnection();
            }
            catch (SqlException err)
            {
                if (err.Number == TIME_OUT)
                {
                    outputWern(err);
                    throw new SQLTimeOutException(err);
                }
                else
                {
                    outputErr(err);
                }

                throw err;
            }
            catch (Exception err)
            {
                outputErr(err);
                throw err;
            }
        }

        #endregion

        #region Transaction Start / Commit / Rollback
        public bool IsTransaction()
        {
            return transaction;
        }

        public void StartTransaction()
        {
            log.Debug(transactionStartMessage);

            transaction = true;

            try
            {
                sqlMap.BeginTransaction(IsolationLevel.ReadCommitted);
            }
            catch (SqlException err)
            {
                outputErr(err);

                if (err.Number == TIME_OUT)
                {
                    throw new SQLTimeOutException(err);
                }

                throw err;
            }
            catch (Exception err)
            {
                outputErr(err);
                throw err;
            }
        }

        public void StartTransactionSerializable()
        {
            log.Debug(transactionStartMessage);

            transaction = true;

            try
            {
                sqlMap.BeginTransaction(IsolationLevel.ReadCommitted);
            }
            catch (SqlException err)
            {
                outputErr(err);

                if (err.Number == TIME_OUT)
                {
                    throw new SQLTimeOutException(err);
                }

                throw err;
            }
            catch (Exception err)
            {
                outputErr(err);
                throw err;
            }
        }

        public void CommitTransaction()
        {
            log.Debug(commitMessage);

            if (!transaction)
            {
                throw new ApplicationException(transactionNoMessage);
            }

            try
            {
                sqlMap.CommitTransaction();
            }
            catch (SqlException err)
            {
                outputErr(err);

                if (err.Number == TIME_OUT)
                {
                    throw new SQLTimeOutException(err);
                }

                throw err;
            }
            catch (Exception err)
            {
                outputErr(err);
                throw err;
            }

            transaction = false;
        }

        public void RollbackTransaction()
        {
            log.Debug(rollbackMessage);

            if (!transaction)
            {
                throw new ApplicationException(transactionNoMessage);
            }

            try
            {
                sqlMap.RollBackTransaction();
            }
            catch (SqlException err)
            {
                outputErr(err);

                if (err.Number == TIME_OUT)
                {
                    throw new SQLTimeOutException(err);
                }

                throw err;
            }
            catch (Exception err)
            {
                outputErr(err);
                throw err;
            }

            transaction = false;
        }

        #endregion

        #region Select / Insert / Update / Delete Methods (Manual Transaction Control)

        #region Selects
        public Hashtable FindRecord(string id, Hashtable parameter)
        {
            log.Debug(string.Format(callMessage, id));
            parameter = ConvertBadBytes(parameter);

            Hashtable row = null;

            try
            {
                row = (Hashtable)sqlMap.QueryForObject(id, parameter);
            }
            catch (SqlException err)
            {

                if (err.Number == TIME_OUT)
                {
                    outputWern(err);
                    throw new SQLTimeOutException(err);
                }
                else
                {
                    outputErr(err);
                }

                throw err;
            }
            catch (Exception err)
            {
                outputErr(err);
                throw err;
            }

            if (row == null)
            {
                row = new Hashtable();
            }

            return row;
        }

        public List<List<string>> FindRecords(string id, Hashtable parameter)
        {
            log.Debug(string.Format(callMessage, id));
            parameter = ConvertBadBytes(parameter);

            IList list = null;

            try
            {
                HybridDictionary x = sqlMap.MappedStatements;
                list = sqlMap.QueryForList(id, parameter);
            }
            catch (SqlException err)
            {

                if (err.Number == TIME_OUT)
                {
                    outputWern(err);
                    throw new SQLTimeOutException(err);
                }
                else
                {
                    outputErr(err);
                }

                throw err;
            }
            catch (Exception err)
            {
                outputErr(err);
                throw err;
            }

            if (list == null)
            {
                list = new ArrayList();
            }

            return this.toListOfLists(list);
        }

        public IList FindRecordsIList(string id, Hashtable parameter)
        {
            log.Debug(string.Format(callMessage, id));
            parameter = ConvertBadBytes(parameter);

            IList list = null;

            try
            {
                list = sqlMap.QueryForList(id, parameter);
            }
            catch (SqlException err)
            {

                if (err.Number == TIME_OUT)
                {
                    outputWern(err);
                    throw new SQLTimeOutException(err);
                }
                else
                {
                    outputErr(err);
                }

                throw err;
            }
            catch (Exception err)
            {
                outputErr(err);
                throw err;
            }

            if (list == null)
            {
                list = new ArrayList();
            }

            return list;
        }

        public ArrayList FindRecordsAsArrayList(string id, Hashtable parameter)
        {
            log.Debug(string.Format(callMessage, id));
            parameter = ConvertBadBytes(parameter);

            IList list = null;

            try
            {
                HybridDictionary x = sqlMap.MappedStatements;
                list = sqlMap.QueryForList(id, parameter);
            }
            catch (SqlException err)
            {

                if (err.Number == TIME_OUT)
                {
                    outputWern(err);
                    throw new SQLTimeOutException(err);
                }
                else
                {
                    outputErr(err);
                }

                throw err;
            }
            catch (Exception err)
            {
                outputErr(err);
                throw err;
            }

            if (list == null)
            {
                list = new ArrayList();
            }

            return (ArrayList)list;
        }
        #endregion

        #region Inserts
        public int InsertRecord(string id, Hashtable parameter)
        {
            return this.InsertRecord(id, parameter, true);
        }

        public int InsertRecord(string id, Hashtable parameter, bool checkTransaction)
        {
            log.Debug(string.Format(callMessage, id));
            parameter = ConvertBadBytes(parameter);

            if (!transaction && checkTransaction)
            {
                throw new ApplicationException(transactionNoMessage);
            }

            int key = -1;

            try
            {
                object result = sqlMap.Insert(id, parameter);
                if (result != null)
                {
                    key = (int)result;
                }
            }
            catch (SqlException err)
            {
                if (err.Number == TIME_OUT)
                {
                    outputWern(err);
                    throw new SQLTimeOutException(err);
                }
                else
                {
                    outputErr(err);
                }

                throw err;
            }
            catch (Exception err)
            {
                outputErr(err);
                throw err;
            }

            return key;
        }
        #endregion

        #region Updates
        public int UpdateRecord(string id, Hashtable parameter)
        {
            return this.UpdateRecord(id, parameter, true);
        }

        public int UpdateRecord(string id, Hashtable parameter, bool checkTransaction)
        {
            log.Debug(string.Format(callMessage, id));
            parameter = ConvertBadBytes(parameter);

            if (!transaction && checkTransaction)
            {
                throw new ApplicationException(transactionNoMessage);
            }

            int dataCount = 0;

            try
            {
                dataCount = sqlMap.Update(id, parameter);
            }
            catch (SqlException err)
            {
                if (err.Number == TIME_OUT)
                {
                    outputWern(err);
                    throw new SQLTimeOutException(err);
                }
                else
                {
                    outputErr(err);
                }
                throw err;
            }
            catch (Exception err)
            {
                outputErr(err);
                throw err;
            }

            log.Debug(string.Format(changedRecordMessage, dataCount));

            return dataCount;
        }
        #endregion

        #region Deletes
        public int DeleteRecord(string id, Hashtable parameter)
        {
            return this.DeleteRecord(id, parameter, true);
        }

        public int DeleteRecord(string id, Hashtable parameter, bool checkTransaction)
        {
            log.Debug(string.Format(callMessage, id));
            parameter = ConvertBadBytes(parameter);

            if (!transaction && checkTransaction)
            {
                throw new ApplicationException(transactionNoMessage);
            }

            int dataCount = 0;

            try
            {
                dataCount = sqlMap.Delete(id, parameter);
            }
            catch (SqlException err)
            {
                if (err.Number == TIME_OUT)
                {
                    outputWern(err);
                    throw new SQLTimeOutException(err);
                }
                else
                {
                    outputErr(err);
                }
                throw err;
            }
            catch (Exception err)
            {
                outputErr(err);
                throw err;
            }

            log.Debug(string.Format(changedRecordMessage, dataCount));

            return dataCount;
        }
        #endregion

        #endregion

        #region Error Output
        private void outputErr(Exception err)
        {
            log.Error(err.Message);

            if (err is SqlException)
            {
                // SQL Server
                SqlException sqlErr = (SqlException)err;

                log.Debug("Source=" + sqlErr.Source + "," +
                        "Number=" + sqlErr.Number + "," +
                        "State=" + sqlErr.State + "," +
                        "Class=" + sqlErr.Class + "," +
                        "Server=" + sqlErr.Server + "," +
                        "Procedure=" + sqlErr.Procedure + "," +
                        "LineNumber=" + sqlErr.LineNumber
                        );
            }

            log.Debug(err.GetType() + "\n" + err.StackTrace);
        }
        #endregion

        #region Warning Output
        private void outputWern(Exception err)
        {
            log.Warn(err.Message);

            if (err is SqlException)
            {
                // SQL Server
                SqlException sqlErr = (SqlException)err;

                log.Debug("Source=" + sqlErr.Source + "," +
                    "Number=" + sqlErr.Number + "," +
                    "State=" + sqlErr.State + "," +
                    "Class=" + sqlErr.Class + "," +
                    "Server=" + sqlErr.Server + "," +
                    "Procedure=" + sqlErr.Procedure + "," +
                    "LineNumber=" + sqlErr.LineNumber
                    );
            }

            log.Debug(err.GetType() + "\n" + err.StackTrace);
        }

        #endregion

        #region Conversion Utils
        protected List<List<string>> toListOfLists(IList rows)
        {
            List<List<string>> returnList = null;

            if (rows != null && rows.Count > 0)
            {
                returnList = new List<List<string>>();
                List<string> keys = new List<string>();

                for (int i = 0; i < rows.Count; i++)
                {
                    Hashtable row = (Hashtable)rows[i];
                    List<string> rowOfValues = new List<string>();

                    if (i == 0)
                    {
                        foreach (string key in row.Keys)
                        {
                            keys.Add(key.ToLower());
                        }
                        returnList.Add(keys);
                    }

                    foreach (Object value in row.Values)
                    {
                        if (value == null)
                        {
                            rowOfValues.Add("");
                        }
                        else
                        {
                            rowOfValues.Add(value.ToString());
                        }
                    }
                    returnList.Add(rowOfValues);
                }
            }

            return returnList;
        }

        /// <summary>
        /// Converts a pair of List<string> keys, List<string> values to a Hashtable.
        /// 
        /// If the keys and values are of different lengths or if there are duplicate keys then
        /// this function will return a null value.
        /// </summary>
        /// <param name="keys">The keys</param>
        /// <param name="values">The values</param>
        /// <returns></returns>
        public static Hashtable convertToHash(List<string> keys, List<string> values)
        {
            Hashtable returnHashtable = null;
            if (keys != null && values != null && keys.Count == values.Count && keys.Count != 0)
            {
                returnHashtable = new Hashtable();

                for (int i = 0; i < keys.Count; i++)
                {
                    if (returnHashtable.ContainsKey(keys[i]))
                    {
                        return null; // return a null set of parameters if there are duplicate keys:
                    }
                    returnHashtable.Add(keys[i], values[i]);
                }

            }
            return returnHashtable;
        }
        #endregion

        #region Convert Bad Bytes
        private static Hashtable ConvertBadBytes(Hashtable parameters) 
        {
            /*
            if (parameters != null)
            {
                ArrayList keysList = new ArrayList();
                foreach (string key in parameters.Keys)
                {
                    keysList.Add(key);
                }
                foreach (string key in keysList)
                {
                    if (parameters[key] != null)
                    {
                        if (parameters[key].ToString().Contains("'"))
                        {
                            parameters[key] = parameters[key].ToString().Replace("'", "' + char(39) + '");
                        }
                        if (parameters[key].ToString().Contains("?"))
                        {
                            parameters[key] = parameters[key].ToString().Replace("?", "' + char(63) + '");
                        }
                    }
                }
            }
            */
            return parameters;
        }
        #endregion
    }
}