#region References
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ServiceModel;
using System.Collections;
using System.Data;
using System.Web.SessionState;
using _WcfMyBatisServiceLibrary;
//using _WcfMyBatisUtilLibrary.DatabaseServiceRef;
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

    public class BaseServiceRequestor
    {
        #region Set Global Properties
        public static Hashtable AddGlobalPropertiesStandAloneApp(Hashtable parameters)
        {
            if (parameters == null)
            {
                parameters = new Hashtable();
            }
            if (parameters != null)
            {
                if (!parameters.ContainsKey("an_create"))
                {
                    parameters.Add("an_create", "system");
                }
                if (!parameters.ContainsKey("an_update"))
                {
                    parameters.Add("an_update", "system");
                }
            }
            return parameters;
        }

        public static Hashtable AddGlobalProperties(Hashtable parameters, HttpSessionState session)
        {
            if (parameters == null)
            {
                parameters = new Hashtable();
            }
            if (parameters != null)
            {
                if (!parameters.ContainsKey("an_create"))
                {
                    parameters.Add("an_create", session["loginUserId"] == null ? "" : session["loginUserId"].ToString());
                }
                if (!parameters.ContainsKey("an_update"))
                {
                    parameters.Add("an_update", session["loginUserId"] == null ? "" : session["loginUserId"].ToString());
                }
                if (!parameters.ContainsKey("login_user_cd_secretariat"))
                {
                    parameters.Add("login_user_cd_secretariat", session["loginUserCdSecretariat"] == null ? "" : session["loginUserCdSecretariat"].ToString());
                }
            }
            return parameters;
        }

        public static DatabaseRequestAutoUpdateOrInsertParam[] AddGlobalProperties(DatabaseRequestAutoUpdateOrInsertParam[] parameters, HttpSessionState session)
        {
            foreach (DatabaseRequestAutoUpdateOrInsertParam parameter in parameters)
            {
                if (parameter != null)
                {
                    if (!parameter.Parameters.ContainsKey("an_create"))
                    {
                        parameter.Parameters.Add("an_create", session["loginUserId"] == null ? "" : session["loginUserId"].ToString());
                    }
                    if (!parameter.Parameters.ContainsKey("an_update"))
                    {
                        parameter.Parameters.Add("an_update", session["loginUserId"] == null ? "" : session["loginUserId"].ToString());
                    }
                    if (!parameter.Parameters.ContainsKey("login_user_cd_secretariat"))
                    {
                        parameter.Parameters.Add("login_user_cd_secretariat", session["loginUserCdSecretariat"] == null ? "" : session["loginUserCdSecretariat"].ToString());
                    }
                }
            }
            return parameters;
        }

        public static DatabaseRequestParam[] AddGlobalProperties(DatabaseRequestParam[] parameters, HttpSessionState session)
        {
            foreach (DatabaseRequestParam parameter in parameters)
            {
                if (parameter != null)
                {
                    if (!parameter.Parameters.ContainsKey("an_create"))
                    {
                        parameter.Parameters.Add("an_create", session["loginUserId"] == null ? "" : session["loginUserId"].ToString());
                    }
                    if (!parameter.Parameters.ContainsKey("an_update"))
                    {
                        parameter.Parameters.Add("an_update", session["loginUserId"] == null ? "" : session["loginUserId"].ToString());
                    }
                    if (!parameter.Parameters.ContainsKey("login_user_cd_secretariat"))
                    {
                        parameter.Parameters.Add("login_user_cd_secretariat", session["loginUserCdSecretariat"] == null ? "" : session["loginUserCdSecretariat"].ToString());
                    }
                }
            }
            return parameters;
        }


        public static Dictionary<string, string> AddGlobalPropertiesDictionary(Dictionary<string, string> parameters, HttpSessionState session)
        {
            if (parameters == null)
            {
                parameters = new Dictionary<string, string>();
            }
            if (parameters != null)
            {
                if (!parameters.ContainsKey("an_create"))
                {
                    parameters.Add("an_create", session["loginUserId"] == null ? "" : session["loginUserId"].ToString());
                }
                if (!parameters.ContainsKey("an_update"))
                {
                    parameters.Add("an_update", session["loginUserId"] == null ? "" : session["loginUserId"].ToString());
                }
                if (!parameters.ContainsKey("login_user_cd_secretariat"))
                {
                    parameters.Add("login_user_cd_secretariat", session["loginUserCdSecretariat"] == null ? "" : session["loginUserCdSecretariat"].ToString());
                }
            }
            return parameters;
        }

        public static Dictionary<object, object> AddGlobalPropertiesDictionary2(Dictionary<object, object> parameters, HttpSessionState session)
        {
            if (parameters == null)
            {
                parameters = new Dictionary<object, object>();
            }
            if (parameters != null)
            {
                if (!parameters.ContainsKey("an_create"))
                {
                    parameters.Add("an_create", session["loginUserId"] == null ? "" : session["loginUserId"].ToString());
                }
                if (!parameters.ContainsKey("an_update"))
                {
                    parameters.Add("an_update", session["loginUserId"] == null ? "" : session["loginUserId"].ToString());
                }
                if (!parameters.ContainsKey("login_user_cd_secretariat"))
                {
                    parameters.Add("login_user_cd_secretariat", session["loginUserCdSecretariat"] == null ? "" : session["loginUserCdSecretariat"].ToString());
                }
            }
            return parameters;
        }
        #endregion

        #region Conversion Helper Functions
        public static List<Dictionary<string, string>> convertToListOfDictionary(string[][] collection)
        {
            List<Dictionary<string, string>> returnList = new List<Dictionary<string, string>>();

            // the collection must not be null and contain at least two or more rows:
            if (collection != null && collection.Length > 1)
            {
                List<string> keys = new List<string>();
                for (int i = 0; i < collection.Length; i++)
                {
                    // the first row always contains the keys:
                    if (i == 0)
                    {
                        for (int x = 0; x < collection[i].Length; x++)
                        {
                            keys.Add(collection[i][x]);
                        }
                    }
                    else
                    {
                        if (collection[i].Length == keys.Count)
                        {
                            Dictionary<string, string> newRow = new Dictionary<string, string>();
                            for (int j = 0; j < collection[i].Length; j++)
                            {
                                newRow.Add(keys[j], collection[i][j]);
                            }
                            returnList.Add(newRow);
                        }
                    }
                }
            }

            return returnList;
        }

        public static ArrayList convertToArrayListOfHashtable(string[][] collection)
        {
            ArrayList returnList = new ArrayList();

            // the collection must not be null and contain at least two or more rows:
            if (collection != null && collection.Length > 1)
            {
                List<string> keys = new List<string>();
                for (int i = 0; i < collection.Length; i++)
                {
                    // the first row always contains the keys:
                    if (i == 0)
                    {
                        for (int x = 0; x < collection[i].Length; x++)
                        {
                            keys.Add(collection[i][x]);
                        }
                    }
                    else
                    {
                        if (collection[i].Length == keys.Count)
                        {
                            Hashtable newRow = new Hashtable();
                            for (int j = 0; j < collection[i].Length; j++)
                            {
                                newRow.Add(keys[j], collection[i][j]);
                            }
                            returnList.Add(newRow);
                        }
                    }
                }
            }

            return returnList;
        }

        public static string[][] convertToStringArrayArray(List<List<string>> collection)
        {
            List<string[]> returnLists = new List<string[]>();
            if (collection != null)
            {
                foreach (List<string> row in collection)
                {
                    returnLists.Add((string[])(row.ToArray()));
                }
            }
            return returnLists.ToArray();
        }

        public static DataTable convertToDataTable(List<List<string>> collectionLL, string returnTableName, bool useUpperCaseCols)
        {
            return convertToDataTable(convertToStringArrayArray(collectionLL), returnTableName, useUpperCaseCols);
        }

        public static DataTable convertToDataTable(string[][] collection, string returnTableName, bool useUpperCaseCols)
        {
            DataTable returnTable = new DataTable(returnTableName);

            if (collection != null && collection.Length > 1)
            {
                List<string> keys = new List<string>();
                for (int i = 0; i < collection.Length; i++)
                {
                    if (i == 0)
                    {
                        for (int x = 0; x < collection[i].Length; x++)
                        {
                            if (useUpperCaseCols)
                            {
                                keys.Add(collection[i][x].ToString().ToUpper());
                                returnTable.Columns.Add(collection[i][x].ToString().ToUpper(), typeof(string));
                            }
                            else
                            {
                                keys.Add(collection[i][x]);
                                returnTable.Columns.Add(collection[i][x].ToString(), typeof(string));
                            }
                        }
                    }
                    else
                    {
                        if (collection[i].Length == keys.Count)
                        {
                            DataRow newRow = returnTable.NewRow();
                            for (int j = 0; j < collection[i].Length; j++)
                            {
                                newRow[keys[j]] = collection[i][j];
                            }
                            returnTable.Rows.Add(newRow);
                        }
                    }
                }
            }
            return returnTable;
        }

        public static Dictionary<object,object> convertToDictionary(Hashtable collection)
        {
            Dictionary<object, object> returnDictionary = new Dictionary<object, object>();

            if (collection != null)
            {
                foreach (string key in collection.Keys)
                {
                    returnDictionary.Add(key, collection[key]);
                }
            }

            return returnDictionary;
        }

        public static Dictionary<string, string>[] convertToDictionaryArray(Hashtable[] collection, HttpSessionState session)
        {
            ArrayList returnDictionaries = new ArrayList();

            foreach (Hashtable hash in collection)
            {
                Dictionary<string, string> returnDictionary = new Dictionary<string, string>();
                if (hash != null)
                {
                    foreach (string key in hash.Keys)
                    {
                        if (hash[key] != null)
                        {
                            returnDictionary.Add(key, hash[key].ToString());
                        }
                        else
                        {
                            returnDictionary.Add(key, null);
                        }
                    }
                }
                returnDictionaries.Add(AddGlobalPropertiesDictionary(returnDictionary, session));
            }

            return (Dictionary<string, string>[])returnDictionaries.ToArray(typeof(Dictionary<string, string>));
        }

        public static DataSet mergeDataSets(ArrayList dataSetsList) 
        {
            DataSet ds = new DataSet();

            foreach (DataSet arg in dataSetsList)
            {
                foreach (DataTable dt in arg.Tables) 
                {
                    if (!ds.Tables.Contains(dt.TableName))
                    {
                        ds.Tables.Add(dt);
                    }
                }
            }

            return ds;
        }

        public static List<DatabaseRequestParam> convert(DatabaseRequestParam[] databaseRequestParams)
        {
            List<DatabaseRequestParam> returnList = new List<DatabaseRequestParam>();
            foreach (DatabaseRequestParam paramSet in databaseRequestParams)
            {
                returnList.Add(paramSet);
            }
            return returnList;
        }

        public static List<DatabaseRequestAutoUpdateOrInsertParam> convert(DatabaseRequestAutoUpdateOrInsertParam[] databaseRequestParams)
        {
            List<DatabaseRequestAutoUpdateOrInsertParam> returnList = new List<DatabaseRequestAutoUpdateOrInsertParam>();
            foreach (DatabaseRequestAutoUpdateOrInsertParam paramSet in databaseRequestParams)
            {
                returnList.Add(paramSet);
            }
            return returnList;
        }
        #endregion
    }
}