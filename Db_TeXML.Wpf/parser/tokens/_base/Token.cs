using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Reflection;
using System.Data;

namespace Db_TeXML.Wpf.parser.tokens._base
{
    public class Token
    {
        private string results = "";
        private string fileName = "";

        public void AddFileDataToHash(Hashtable fileData)
        {
            if (!fileName.Equals(""))
            {
                fileData[fileName] = results;
            }
        }

        protected ParseEngine engine;
        protected string text = "";
        protected ArrayList args = new ArrayList();
        protected DataRow dataRow;
        protected Name name;

        public Name Name
        {
            get { return name; }
            set { this.name = value; }
        }

        public virtual void Initialize(ParseEngine engine, DataRow dataRow, string text, ArrayList args)
        {
            this.engine = engine;
            this.dataRow = dataRow;
            this.text = text;
            this.args = args;
        }

        public virtual bool CanParse()
        {
            return true;
        }

        public virtual string ApplyArgs(string productText)
        {
            if (this.args.Count > 0)
            {
                productText = productText + this.args[0].ToString();
            }
            if (this.args.Count > 3)
            {
                string preparsedFileName = this.args[3].ToString().Trim();
                fileName = this.ParseFileName(preparsedFileName);
                if (fileName.Equals(preparsedFileName))
                {
                    // Invalid filename, must reference a table token
                    fileName = "";
                }
            }

            return productText;
        }

        public virtual string ParseFileName(string fileName)
        {
            if (fileName.Trim().Length > 0)
            {
                string productText = fileName;
                Hashtable parseHash = ((Hashtable)ParseEngine.TokenTypeToReflectionCallbackHash[GetType()]);
                foreach (string parseToken in parseHash.Keys)
                {
                    string value = this.GetTokenData(parseToken, parseHash[parseToken].ToString());
                    productText = productText.Replace(parseToken, value);
                }
                return productText;
            }
            return "";
        }

        public virtual string ParseToken(bool applyArgs)
        {
            string productText = this.text;
            Hashtable parseHash = ((Hashtable)ParseEngine.TokenTypeToReflectionCallbackHash[GetType()]);
            foreach (string parseToken in parseHash.Keys)
            {
                string value = this.GetTokenData(parseToken, parseHash[parseToken].ToString());
                productText = productText.Replace(parseToken, value);
            }

            if (applyArgs)
            {
                productText = this.ApplyArgs(productText);
            }

            return productText;
        }

        public string GetTokenData(string token, string methodsJoined)
        {
            string[] methods = methodsJoined.Split('|');

            if (methods.Length == 2)
            {
                MethodInfo methodInfo1 = this.GetType().GetMethod(methods[0]);
                object member = methodInfo1.Invoke(this, null);
  
                MethodInfo methodInfo2 = member.GetType().GetMethod(methods[1]);
                object result = methodInfo2.Invoke(member, null);

                return result.ToString();
            }

            return token;
        }

        public virtual void SetTokenResults(string results, Hashtable fileData)
        {
            this.results = results;
            this.AddFileDataToHash(fileData);
        }
    }
}
