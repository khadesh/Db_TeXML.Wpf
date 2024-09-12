using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Db_TeXML.Wpf.parser
{
    public class Name : Attribute
    {
        private string rawName = "";
        public string Sql
        {
            get { return this.rawName; }
        }
        public string Class
        {
            get { return this.getClassName(this.rawName); }
        }
        public string Method
        {
            get { return this.getMethodName(this.rawName); }
        }
        public string Var
        {
            get { return this.Method; }
        }
        public string Upper
        {
            get { return this.rawName.ToUpper(); }
        }
        public string Lower
        {
            get { return this.rawName.ToLower(); }
        }

        public Name(string name)
        {
            this.rawName = name;
        }
    }
}
