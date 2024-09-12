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
using System.DirectoryServices;
using System.Configuration;
#endregion

namespace _WcfMyBatisServiceLibrary
{
    /// <summary>
    /// 
    /// </summary>
    public class GeneralService : BaseService, IGeneralService
    {
        private static readonly ILog logger = LogManager.GetLogger("DEBUG");

        public LdapLoginResult LDAP_LoginUser(string username, string password)
        {
            LdapLoginResult returnVals = new LdapLoginResult();
            string domain = ConfigurationManager.AppSettings["Domain"];
            string path = "LDAP://" + domain;
            string domainAndUser = domain + @"\" + username;

            try
            {
                DirectoryEntry entry = new DirectoryEntry(path, domainAndUser, password);

                object obj = entry.NativeObject;

                DirectorySearcher search = new DirectorySearcher(entry);
                search.Filter = "(SAMAccountName=" + username + ")";
                search.PropertiesToLoad.Add("cn");
                SearchResult result = search.FindOne();

                if (null == result)
                {
                    logger.Debug("Login failure for user: " + username);
                    returnVals.LoginSuccess = false;
                    return returnVals;
                }

                returnVals.UserName = result.Properties["CN"][0].ToString();
            }
            catch (Exception ex)
            {
                logger.Debug("Login failure for user, with exception: " + username + ", " + ex.Message + ", " + ex.Source + ", " + ex.StackTrace);
                returnVals.LoginSuccess = false;
                returnVals.SetError(ex);
                return returnVals;
            }

            logger.Debug("Login success for user: " + username);
            returnVals.LoginSuccess = true;
            return returnVals;
        }
    }
}