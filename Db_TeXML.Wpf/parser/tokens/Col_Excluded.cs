using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Data;

namespace Db_TeXML.Wpf.parser.tokens
{
    public class Col_Excluded : Col
    {
        public Col_Excluded()
        {

        }

        public override bool CanParse()
        {
            return this.isExcluded;
        }
    }
}
