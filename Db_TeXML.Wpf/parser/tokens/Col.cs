using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using Db_TeXML.Wpf.parser.tokens._base;
using System.Collections;

namespace Db_TeXML.Wpf.parser.tokens
{
    public class Col : Token
    {
        protected Type type;
        public Type Type
        {
            get { return type; }
            set { this.type = value; }
        }

        protected bool isExcluded = false;
        protected bool isPrimary = false;

        public Col()
        {

        }

        public override void Initialize(ParseEngine engine, DataRow dataRow, string text, ArrayList args)
        {
            base.Initialize(engine, dataRow, text, args);
            this.Name = new Name(dataRow["column_name"].ToString());
            this.Type =
                new Type(
                    dataRow["data_type"].ToString(),
                    dataRow["data_type_short"].ToString(),
                    dataRow["character_maximum_length"].ToString(),
                    dataRow["column_nullable"].ToString(),
                    dataRow["column_default"].ToString()
                );

            this.isPrimary = dataRow["is_primary_key"].Equals("1");

            if (engine.ExcludeCols != null && engine.ExcludeCols.Contains(dataRow["column_name"].ToString()))
            {
                this.isExcluded = true;
            }
        }

        public override bool CanParse()
        {
            return !this.isExcluded;
        }

        public override string ParseToken(bool applyArgs)
        {
            if (this.CanParse())
            {
                return base.ParseToken(applyArgs);
            }
            return "";
        }
    }
}
