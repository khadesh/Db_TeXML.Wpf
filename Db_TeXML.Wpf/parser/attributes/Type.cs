using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Db_TeXML.Wpf.parser
{
    public class Type : Attribute
    {
        private string rawType = "";
        private string rawTypeUdt = "";
        private string rawLength = "";
        private string rawNullable = "";
        private string rawDefault = "";
        public string Sql
        {
            get 
            {
                return (this.rawLength != null && !rawLength.Equals("") ? rawType + "(" + rawLength + ")" : rawType); 
            }
        }
        public string CS
        {
            get { return this.getCSharpType(this.rawTypeUdt); }
        }
        public string Upper
        {
            get { return this.Sql.ToUpper(); }
        }
        public string Lower
        {
            get { return this.Sql.ToLower(); }
        }
        public string Length
        {
            get
            {
                return (this.rawTypeUdt.StartsWith("varchar") ? rawLength : "");
            }
        }
        public string Nullable
        {
            get { return (this.rawNullable.ToLower().Equals("not nullable") ? "not null" : ""); }
        }
        public string Default
        {
            get { return (this.rawDefault.Equals("") ? "" : "default " + this.rawDefault); }
        }

        public Type(string rawType, string rawTypeUdt, string rawLength, string rawNullable, string rawDefault)
        {
            this.rawType = rawType;
            this.rawTypeUdt = rawTypeUdt;
            this.rawLength = rawLength;
            this.rawNullable = rawNullable;
            this.rawDefault = rawDefault;

            // nextval('alltypes_type02_seq'::regclass)
            if (Regex.IsMatch(this.rawDefault, @"nextval\(.*::regclass\)") && this.rawNullable.Equals("not nullable"))
            {
                if (this.rawType.Equals("bigint"))
                {
                    this.rawType = "bigserial";
                    this.rawDefault = "";
                    this.rawNullable = "";
                }
                else if (this.rawType.Equals("smallint"))
                {
                    this.rawType = "smallserial";
                    this.rawDefault = "";
                    this.rawNullable = "";
                }
                else if (this.rawType.Equals("integer"))
                {
                    this.rawType = "serial";
                    this.rawDefault = "";
                    this.rawNullable = "";
                }
            }
        }

        /* 
        List of all postgres types
        Url: http://www.postgresql.org/docs/9.4/static/datatype.html
        Name                                         Aliases                     Description
        ---------------------------------------------------------------------------------------------------------------------
        bigint										int8						signed eight-byte integer
        bigserial									serial8						autoincrementing eight-byte integer
        bit [ (n) ]																fixed-length bit string
        bit varying [ (n) ]							varbit						variable-length bit string
        boolean										bool						logical Boolean (true/false)
        box																		rectangular box on a plane
        bytea																	binary data ("byte array")
        character [ (n) ]							char [ (n) ]				fixed-length character string
        character varying [ (n) ]					varchar [ (n) ]				variable-length character string
        cidr												    					IPv4 or IPv6 network address
        circle																	circle on a plane
        date											    						calendar date (year, month, day)
        double precision							    float8						double precision floating-point number (8 bytes)
        inet																    	IPv4 or IPv6 host address
        integer										int, int4					signed four-byte integer
        interval [ fields ] [ (p) ]												time span
        json																    	textual JSON data
        jsonb																	binary JSON data, decomposed
        line																	    infinite line on a plane
        lseg																	    line segment on a plane
        macaddr																	MAC (Media Access Control) address
        money																	currency amount
        numeric [ (p, s) ]							decimal [ (p, s) ]			exact numeric of selectable precision
        path																	    geometric path on a plane
        pg_lsn																	PostgreSQL Log Sequence Number
        point																	geometric point on a plane
        polygon																	closed geometric path on a plane
        real										    float4						single precision floating-point number (4 bytes)
        smallint									    int2						signed two-byte integer
        smallserial									serial2						autoincrementing two-byte integer
        serial										serial4						autoincrementing four-byte integer
        text																    	variable-length character string
        time [ (p) ] [ without time zone ]										time of day (no time zone)
        time [ (p) ] with time zone					timetz						time of day, including time zone
        timestamp [ (p) ] [ without time zone ]									date and time (no time zone)
        timestamp [ (p) ] with time zone			    timestamptz					date and time, including time zone
        tsquery																	text search query
        tsvector																    text search document
        txid_snapshot															user-level transaction ID snapshot
        uuid																	    universally unique identifier
        xml																		XML data
        ---------------------------------------------------------------------------------------------------------------------
        */

        /*
        -------------------------------------------------
        -- MySQL Data Types -----------------------------
        -------------------------------------------------
        Numeric Data Types:

        MySQL uses all the standard ANSI SQL numeric data types, so if you're coming to MySQL from a different database system, these definitions will look familiar to you. The following list shows the common numeric data types and their descriptions:

        INT  - A normal-sized integer that can be signed or unsigned. If signed, the allowable range is from -2147483648 to 2147483647. If unsigned, the allowable range is from 0 to 4294967295. You can specify a width of up to 11 digits.

        TINYINT  - A very small integer that can be signed or unsigned. If signed, the allowable range is from -128 to 127. If unsigned, the allowable range is from 0 to 255. You can specify a width of up to 4 digits.

        SMALLINT  - A small integer that can be signed or unsigned. If signed, the allowable range is from -32768 to 32767. If unsigned, the allowable range is from 0 to 65535. You can specify a width of up to 5 digits.

        MEDIUMINT  - A medium-sized integer that can be signed or unsigned. If signed, the allowable range is from -8388608 to 8388607. If unsigned, the allowable range is from 0 to 16777215. You can specify a width of up to 9 digits.

        BIGINT  - A large integer that can be signed or unsigned. If signed, the allowable range is from -9223372036854775808 to 9223372036854775807. If unsigned, the allowable range is from 0 to 18446744073709551615. You can specify a width of up to 20 digits.

        FLOAT(M,D)  - A floating-point number that cannot be unsigned. You can define the display length (M) and the number of decimals (D). This is not required and will default to 10,2, where 2 is the number of decimals and 10 is the total number of digits (including decimals). Decimal precision can go to 24 places for a FLOAT.

        DOUBLE(M,D)  - A double precision floating-point number that cannot be unsigned. You can define the display length (M) and the number of decimals (D). This is not required and will default to 16,4, where 4 is the number of decimals. Decimal precision can go to 53 places for a DOUBLE. REAL is a synonym for DOUBLE.

        DECIMAL(M,D)  - An unpacked floating-point number that cannot be unsigned. In unpacked decimals, each decimal corresponds to one byte. Defining the display length (M) and the number of decimals (D) is required. NUMERIC is a synonym for DECIMAL.

        -------------------------------------------------
        Date and Time Types:

        The MySQL date and time datatypes are:

        DATE  - A date in YYYY-MM-DD format, between 1000-01-01 and 9999-12-31. For example, December 30th, 1973 would be stored as 1973-12-30.

        DATETIME  - A date and time combination in YYYY-MM-DD HH:MM:SS format, between 1000-01-01 00:00:00 and 9999-12-31 23:59:59. For example, 3:30 in the afternoon on December 30th, 1973 would be stored as 1973-12-30 15:30:00.

        TIMESTAMP  - A timestamp between midnight, January 1, 1970 and sometime in 2037. This looks like the previous DATETIME format, only without the hyphens between numbers; 3:30 in the afternoon on December 30th, 1973 would be stored as 19731230153000 ( YYYYMMDDHHMMSS ).

        TIME  - Stores the time in HH:MM:SS format.

        YEAR(M)  - Stores a year in 2-digit or 4-digit format. If the length is specified as 2 (for example YEAR(2)), YEAR can be 1970 to 2069 (70 to 69). If the length is specified as 4, YEAR can be 1901 to 2155. The default length is 4.

        -------------------------------------------------
        String Types:

        Although numeric and date types are fun, most data you'll store will be in string format. This list describes the common string datatypes in MySQL.

        CHAR(M) - A fixed-length string between 1 and 255 characters in length (for example CHAR(5)), right-padded with spaces to the specified length when stored. Defining a length is not required, but the default is 1.

        VARCHAR(M)  - A variable-length string between 1 and 255 characters in length; for example VARCHAR(25). You must define a length when creating a VARCHAR field.

        BLOB or TEXT  - A field with a maximum length of 65535 characters. BLOBs are "Binary Large Objects" and are used to store large amounts of binary data, such as images or other types of files. Fields defined as TEXT also hold large amounts of data; the difference between the two is that sorts and comparisons on stored data are case sensitive on BLOBs and are not case sensitive in TEXT fields. You do not specify a length with BLOB or TEXT.

        TINYBLOB or TINYTEXT  - A BLOB or TEXT column with a maximum length of 255 characters. You do not specify a length with TINYBLOB or TINYTEXT.

        MEDIUMBLOB or MEDIUMTEXT  - A BLOB or TEXT column with a maximum length of 16777215 characters. You do not specify a length with MEDIUMBLOB or MEDIUMTEXT.

        LONGBLOB or LONGTEXT  - A BLOB or TEXT column with a maximum length of 4294967295 characters. You do not specify a length with LONGBLOB or LONGTEXT.

        ENUM  - An enumeration, which is a fancy term for list. When defining an ENUM, you are creating a list of items from which the value must be selected (or it can be NULL). For example, if you wanted your field to contain "A" or "B" or "C", you would define your ENUM as ENUM ('A', 'B', 'C') and only those values (or NULL) could ever populate that field.
        -------------------------------------------------
         */

        private string getCSharpType(string rawTypeUdt)
        {
            if (rawTypeUdt.StartsWith("varchar"))
            {
                return "string";
            }
            else if (rawTypeUdt.StartsWith("bpchar"))
            {
                return "string";
            }
            else if (rawTypeUdt.StartsWith("text"))
            {
                return "string";
            }
            else if (rawTypeUdt.StartsWith("bool"))
            {
                return "bool";
            }
            else if (rawTypeUdt.StartsWith("int"))
            {
                return "int";
            }
            else if (rawTypeUdt.StartsWith("money"))
            {
                return "decimal";
            }
            else if (rawTypeUdt.StartsWith("float"))
            {
                return "decimal";
            }
            else if (rawTypeUdt.StartsWith("numeric"))
            {
                return "decimal";
            }
            else if (rawTypeUdt.StartsWith("date"))
            {
                return "DateTime";
            }
            else if (rawTypeUdt.StartsWith("time"))
            {
                return "DateTime";
            }
            else if (rawTypeUdt.StartsWith("interval"))
            {
                return "TimeSpan";
            }
            return "string";
        }
    }
}
