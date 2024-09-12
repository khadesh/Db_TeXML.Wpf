using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Db_TeXML.Wpf.parser
{
    public class ParseException : Exception
    {
        public ParseException(string message) : base(message)
        {

        }
    }
}
