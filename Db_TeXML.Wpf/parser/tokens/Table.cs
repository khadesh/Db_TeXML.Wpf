using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using Db_TeXML.Wpf.parser.tokens._base;
using System.Collections;

namespace Db_TeXML.Wpf.parser.tokens
{
    public class Table : Token
    {
        public Table()
        {

        }

        public override string ApplyArgs(string productText)
        {
            // Save file arg[3]:

            return base.ApplyArgs(productText);
        }

        public override void Initialize(ParseEngine engine, DataRow dataRow, string text, ArrayList args)
        {
            base.Initialize(engine, dataRow, text, args);
            this.Name = new Name(dataRow["table_name"].ToString());
        }
    }
}
