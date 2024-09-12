using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Db_TeXML.Wpf.parser
{
    public class Attribute
    {
        public Attribute()
        {

        }

        private bool checkCharacterTokenValidity(char next, bool firstChar)
        {
            string checkRegex = (firstChar ? "[a-zA-Z]" : "[a-zA-Z0-9]");
            if (Regex.IsMatch(next.ToString(), "[a-zA-Z0-9]"))
            {
                return true;
            }
            return false;
        }

        protected string getClassName(string text)
        {
            string returnText = "";
            bool nameBreak = true;
            bool firstChar = true;

            foreach (char next in text)
            {
                if (checkCharacterTokenValidity(next, firstChar))
                {
                    if (nameBreak)
                    {
                        returnText += next.ToString().ToUpper();
                        nameBreak = false;
                    }
                    else
                    {
                        returnText += next.ToString().ToLower();
                    }
                    firstChar = false;
                }
                else
                {
                    nameBreak = true;
                }
            }

            return returnText;
        }

        protected string getMethodName(string text)
        {
            string returnText = this.getClassName(text);
            if (returnText.Length > 0)
            {
                returnText = returnText[0].ToString().ToLower() + returnText.Substring(1, returnText.Length - 1);
            }
            return returnText;
        }
    }
}
