using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Collections;
using Db_TeXML.Wpf.parser.tokens;
using System.Text.RegularExpressions;
using System.Data;
using Db_TeXML.Wpf.parser.tokens._base;
using System.Runtime.Remoting;
using System.Threading;
using System.IO;
using NUnit.Framework;
using System.Windows;
using _CommonLib;

namespace Db_TeXML.Wpf.parser
{
    [TestFixture]
    public class ParseEngine
    {
        #region Static Members (Token Reflection Information Stored Here)
        /// <summary>
        /// Token class data initialized at creation via reflection.
        /// It is used to get a token's class name for use in reflection during script output generation.
        /// 
        /// Usage Format:
        ///   Hashtable[string tokenName, string tokenClassName]
        ///   
        /// Sample data:
        ///   Hashtable["table", "Table"]
        ///   Hashtable["col", "Col"]
        ///   Hashtable["col_priamry", "Col_Primary"]
        /// </summary>
        private static Hashtable tokenNameToTokenClassNameReflectionHash = new Hashtable();
        public static Hashtable TokenNameToTokenClassNameReflectionHash
        {
            get
            {
                return tokenNameToTokenClassNameReflectionHash;
            }
            set
            {
                tokenNameToTokenClassNameReflectionHash = value;
            }
        }

        /// <summary>
        /// Token type data initialized at creation via reflection.
        /// 
        /// Usage Format:
        ///   Hashtable[type tokenType, Hashtable[string replaceValue, string reflectionCallBack]]
        ///   
        /// Sample data:
        ///   Hashtable[typeof(Table), Hashtable["table.name.sql", "get_Name|get_Sql"]]
        /// </summary>
        private static Hashtable tokenTypeToReflectionCallbackHash = new Hashtable();
        public static Hashtable TokenTypeToReflectionCallbackHash
        {
            get
            {
                return tokenTypeToReflectionCallbackHash;
            }
            set
            {
                tokenTypeToReflectionCallbackHash = value;
            }
        }

        /// <summary>
        /// Namespace containing all tokens for parsing.
        /// </summary>
        public static string Db_TeXML_TokenNamespace = "Db_TeXML.Wpf.parser.tokens";
        #endregion

        #region Public / Private Members (Script Parse Information Stored Here)
        /// <summary>
        /// Token data after parsing is completed stored in the form of:
        /// 
        /// Usage Format:
        ///   Hashtable[string parseTokenClassName, Hashtable[string randomKey, Pair(string tokenData, ArrayList args)]]
        ///   
        /// Sample data:
        ///   Hashtable["Table", 
        ///     Hashtable["%PNGQWHRXTZUQBOAJ%", 
        ///       new Pair("drop table table.name.sql;create table table.name.sql\n{\n%GVBFRINWUCZEBGVJ%%KKOAWFUJXOYLTLIA%\n};",
        ///       new ArrayList(){ "%KKOAWFUJ%", "", "", "CreateScripts\table.name.sql.sql" })
        ///     ]
        ///   ]
        /// </summary>
        private Hashtable tokenDataHash = new Hashtable();
        public Hashtable TokenDataHash
        {
            get { return tokenDataHash; }
        }

        /// <summary>
        /// Use unique token place holders are stored in this hash. 
        /// It is used to ensure the same token is not accidentally created twice.
        /// 
        /// Usage Format:
        ///   Hashtable[string tokenKey, bool isUsed]
        ///   
        /// Sample data:
        ///   Hashtable["%PNGQWHRXTZUQBOAJ%", true]
        /// </summary>
        private Hashtable usedTokenKeysHash = new Hashtable();
        public Hashtable UsedTokenKeysHash
        {
            get { return usedTokenKeysHash; }
        }

        /// <summary>
        /// Output filename as defined in the scripts header section with:
        /// # file = savefilename.ext
        /// 
        /// After parsing a script this value is used to determine the save file name.
        /// </summary>
        public string OutFileName = "";

        /// <summary>
        /// Exclude columns as defined in the scripts header section with:
        /// # excludes = cola, colb, colc
        /// 
        /// After parsing out all the token data, this HashSet is used to determine if a column should generate data or not.
        /// </summary>
        public HashSet<string> ExcludeCols = new HashSet<string>();

        /// <summary>
        /// This hashtable hold all table file output information after token parsings have been applied.
        /// 
        /// Usage Format:
        ///   Hashtable[string saveFilePath, string tokenData]
        ///   
        /// Sample data:
        ///   Hashtable["CreateScripts\students.sql", "drop table students;\ncreate table students\n{\n\tstudent_id integer not null ,%KKOAWFUJ%	student_name varchar(64)  ,%KKOAWFUJ%	create_date timestamp without time zone  %KKOAWFUJ%	,primary key (student_id)\n};"]
        /// </summary>
        private Hashtable fileOutputData = new Hashtable();

        /// <summary>
        /// A special replace key for new line characters embedded within the script.
        /// Used to ensure the script is not split on new lines escaped within the script.
        /// </summary>
        private string newLineReplaceKey = "";
        #endregion

        #region Public Members (Database Table and Column Hashes)
        /// <summary>
        /// Database table information for the user specified database.
        /// 
        /// Value should be set in the constructor and never modified.
        /// 
        /// A new database connection should require a new instance of the ParseEngine.
        /// </summary>
        private DataTable tableDt = null;
        public DataTable TableDt
        {
            get { return this.tableDt; }
        }

        /// <summary>
        /// Database column information for the user specified database.
        /// 
        /// Value should be set in the constructor and never modified.
        /// 
        /// A new database connection should require a new instance of the ParseEngine.
        /// </summary>
        private DataTable columnDt = null;
        public DataTable ColumnDt
        {
            get { return this.columnDt; }
        }
        #endregion

        #region Private Members (Other)
        private string[] invalidSubstrings = new string[] { @"..\", "/", ":", "*", "?", "\"", "<", ">", "|" };

        #endregion

        #region Constructor(s)
        /// <summary>
        /// This constructor will initialize the static hashtables holding token reflection information.
        /// </summary>
        static ParseEngine()
        {
            InitializeParseEngineTokens();
        }

        /// <summary>
        /// This constructor sets the database information that will be applied to the script.
        /// </summary>
        /// <param name="tableDt">database table information</param>
        /// <param name="columnDt">database column information</param>
        public ParseEngine(DataTable tableDt, DataTable columnDt)
        {
            this.tableDt = tableDt;
            this.columnDt = columnDt;
        }

        public ParseEngine()
        {
            this.tableDt = this.generateDataTableFromCsv(Db_TeXML.Wpf.Resources.practice_select_information_schema_tables, "tables");
            this.columnDt = this.generateDataTableFromCsv(Db_TeXML.Wpf.Resources.practice_select_information_schema_columns, "columns");
        }
        #endregion

        #region Parse Tokens
        /// <summary>
        /// This is the "main" method of this class.
        /// It will parse a given text and save the output as defined in table token arguments within the scripts.
        /// 
        /// It will not save the final script output, this must be done in the parent class.
        /// </summary>
        /// <param name="text">script text to be parsed</param>
        /// <param name="outputName">default output directory</param>
        /// <param name="databaseName">current connected database</param>
        /// <returns></returns>
        public string ParseTokens(string text, string outputName, string databaseName)
        {
            this.ClearData();

            text = text.Trim().Replace("\r\n","\n");

            // Create a list of all table tokens, replace contents with mock values:
            text = parseTokensAndGenerateTokenHashes(text, text, "table", "Table");

            // Apply database data to the hash:
            text = applyTokenData(text, "Table");
            
            // Apply new lines to file output and save files:
            this.saveTableFiles(outputName, databaseName);

            return text;
        }

        /// <summary>
        /// Clear all variables used for parsing scripts, so another parse may be executed:
        /// </summary>
        private void ClearData()
        {
            this.newLineReplaceKey = "";
            this.TokenDataHash.Clear();
            this.UsedTokenKeysHash.Clear();
            this.fileOutputData.Clear();
            this.OutFileName = "";
            this.ExcludeCols = new HashSet<string>();
        }

        /// <summary>
        /// Completes the saveText, and sets the save path for the file and directory.
        /// 
        /// May throw exceptions if an invalid filename is produced.
        /// </summary>
        /// <param name="fileNameKey">save file name</param>
        /// <param name="databaseName">current selected database</param>
        /// <param name="outputName">standard output directory</param>
        /// <returns>the path to save the file to</returns>
        private string setSaveFileInformation(string fileNameKey, string databaseName, string outputName)
        {
            foreach (string sub in invalidSubstrings)
            {
                if (fileNameKey.Contains(sub))
                {
                    throw new Exception(@"File names must not include '" + sub + "', please correct the script and run the parser again.");
                }
            }

            if (fileNameKey.EndsWith(@"\"))
            {
                throw new Exception(@"File names must not end with '\', please define a valid filename for output.");
            }

            string savePath = EditorWindow.ProjectsDirectory + databaseName + "\\Output\\" + outputName + "\\" + fileNameKey;

            // If a user has defined a directory and a filename, then don't use the standard output directory:
            if (fileNameKey.Contains("\\"))
            {
                savePath = EditorWindow.ProjectsDirectory + databaseName + "\\Output\\" + fileNameKey;
            }

            System.IO.FileInfo fileInfo = null;

            // Check for valid file name:
            // (throws excpetions if an invalid filename is passed in)
            fileInfo = new System.IO.FileInfo(savePath);

            return savePath;
        }

        /// <summary>
        /// This function is save table token data to unique files for each table, if defined properly in the script.
        /// 
        /// This function may display a MessageBox dialog if an error occurs during the file saves.
        /// </summary>
        /// <param name="outputName">Subfolder to save the output to, must not include "..\"</param>
        /// <param name="databaseName">The database name</param>
        private void saveTableFiles(string outputName, string databaseName)
        {
            bool result = true;
            string errorInfo = "";
            try
            {
                outputName = outputName.Replace("Script", "Output");

                foreach (string fileNameKey in this.fileOutputData.Keys)
                {
                    string saveText = this.fileOutputData[fileNameKey].ToString();
                    string saveFilepath = this.setSaveFileInformation(fileNameKey, databaseName, outputName);
                    FileManager.SaveFile(saveFilepath, saveText);
                }
            }
            catch (Exception ex)
            {
                result = false;
                errorInfo = ex.Message;
            }
            if (!result)
            {
                MessageBox.Show("Error in " + outputName + Messenger.GetMessage("TableTokenDataSaveError") + errorInfo, Messenger.GetMessage("TableTokenDataSaveErrorTitle"), MessageBoxButton.OK);
            }
        }

        /// <summary>
        /// This function is to ensure escape characters are properly replaced.
        /// </summary>
        /// <param name="text">text to apply escape characters to</param>
        /// <returns></returns>
        public string applyNewLineCharacters(string text)
        {
            return text.Replace(newLineReplaceKey, "\n").Replace("\\/\\/", "//").Replace("\\\\", "\\");
        }

        /// <summary>
        /// Checks to see if a token has arguments or not.
        /// </summary>
        /// <param name="substring">string to check</param>
        /// <param name="tokenName">token to check for</param>
        /// <returns>true if it has arguments defined</returns>
        private bool checkTokenHasArgs(string substring, string tokenName)
        {
            if (substring.StartsWith("<" + tokenName + "("))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Checks to ensure a start token has been found and closed properly.
        /// </summary>
        /// <param name="substring">string to check</param>
        /// <param name="tokenName">token to check for</param>
        /// <returns>true if a start token is found</returns>
        private bool checkSubstringForToken(string substring, string tokenName)
        {
            if (substring.StartsWith("<" + tokenName + "(") ||
                substring.StartsWith("<" + tokenName + ":"))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Checks to ensure an end token has been found and closed properly.
        /// </summary>
        /// <param name="substring">string to check</param>
        /// <param name="tokenName">token to check for</param>
        /// <returns>true if an end token is found</returns>
        private bool checkSubstringForTokenEnd(string substring, string tokenName)
        {
            if (substring.StartsWith("/" + tokenName + ">"))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// This function is a helper used by the extractArgs function.
        /// 
        /// It is used to determine if defined arguments are valid or not.
        /// </summary>
        /// <param name="nextChar">the next argument character to check</param>
        /// <returns>true if a valid character is found, false otherwise</returns>
        private bool checkArgChar(char nextChar)
        {
            if (nextChar == ')' ||
                nextChar == ' ' ||
                nextChar == '\'' ||
                nextChar == ',')
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Extracts token argument data and returns it in the format:
        ///   Pair(string remainingLineData, ArrayList arguments)
        ///   
        /// Rules:
        /// 1. \\ and \' are the only escape characters \ followed by anything else will simply become a \
        /// 2. Args are contained within two single quotes, and seperated by a single comma and any number of spaces.
        /// 3. Spaces are also allowed before the first arg and after the last.
        /// 4. Excluding arg content, only four characters are allowed: 
        ///    ')' - bracket to close the args
        ///    ' ' - a space for visual preferences
        ///    ''' - single quotes to start and end args
        ///    ',' - commas to separate args
        /// 5. Exceptions will be thrown if these rules are broken.
        /// 
        /// </summary>
        /// <param name="tokenData">the token data to be checked for arguments</param>
        /// <param name="tokenName">the name of the token being checked</param>
        /// <returns>Pair(string remainingLineData, ArrayList arguments)</returns>
        private Pair extractArgs(string tokenData, string tokenName, int lineNo)
        {
            ArrayList args = new ArrayList();
            if (checkTokenHasArgs(tokenData, tokenName))
            {
                string lineError = "\n[line " + (lineNo + 1) +"]";

                int tokenStartLength = ("<" + tokenName + "(").Length;
                tokenData = tokenData.Substring(tokenStartLength, tokenData.Length - tokenStartLength);

                bool readingArg = false;
                bool commaFound = true;
                bool clearLast = false;
                bool skipChar = false;
                bool closingBracketFound = false;
                string arg = "";
                int endIndex = 0;
                char last = ' ';

                foreach (char next in tokenData)
                {
                    // Check args for validity:
                    if (!checkArgChar(next) && !readingArg)
                    {
                        throw new ParseException("Invalid character in arg definitions found." + lineError);
                    }

                    if (next.Equals(',') && !readingArg)
                    {
                        commaFound = !commaFound;
                        if (!commaFound)
                        {
                            throw new ParseException("Token arguments contain too many commas or commas are misplaced." + lineError);
                        }
                    }

                    // End of args found, short circuit the foreach loop:
                    else if (next.Equals(')') && !readingArg)
                    {
                        closingBracketFound = true;
                        if (commaFound)
                        {
                            throw new ParseException("There is a misplaced comma before the token argument closing bracket." + lineError);
                        }
                        endIndex++;
                        break;
                    }                        
                    else if (readingArg && last.Equals('\\'))
                    {
                        if (next.Equals('\\'))
                        {
                            clearLast = true;
                        }
                        else if (next.Equals('\''))
                        {
                            // remove the preceding escape character from the final value:
                            arg = arg.Substring(0, arg.Length - 1);
                        }
                    }
                    // The begining or end of an arg has been found:
                    else if (next.Equals('\''))
                    {
                        // toggle read on or off
                        readingArg = !readingArg;
                        if (!readingArg)
                        {
                            // if an arg has been read entirely, then add it to the return list and clear the next arg:
                            if (!commaFound)
                            {
                                throw new ParseException("Token arguments must be separated by commas." + lineError);
                            }

                            // make escaped character replacements:
                            arg = arg.Replace(@"\\", @"\");

                            // add the arg to the array list and reset for the next arg:
                            args.Add(arg);
                            arg = "";
                            commaFound = false;
                        }
                        else
                        {
                            // single quote start char should not be added to the final arg:
                            skipChar = true;
                        }
                    }

                    if (readingArg)
                    {
                        if (!skipChar) 
                        {
                            // add more data to the arg:
                            arg += next;
                        }
                        else
                        {
                            // reset the skip char:
                            skipChar = false;
                        }

                    }

                    // ensure that if a \ was escaped, it is not then thought to be an escape character itself.
                    if (clearLast)
                    {
                        clearLast = false;
                        last = ' ';
                    }
                    else
                    {
                        last = next;
                    }
                    endIndex++;
                }

                if (!closingBracketFound)
                {
                    throw new ParseException("Arguement closing bracket not found." + lineError);
                }
                return new Pair(Regex.Replace(tokenData.Substring(endIndex, tokenData.Length - endIndex).Trim(), @"^\:", ""), args);
            }
            else
            {
                int tokenStartLength = ("<" + tokenName + "(").Length;
                tokenData = tokenData.Substring(tokenStartLength, tokenData.Length - tokenStartLength);
            }
            return new Pair(tokenData, args);
        }

        /// <summary>
        /// This function will initialize the newLineReplaceKey if currently unset.
        /// </summary>
        private void initializeNewLineReplaceKey(string text)
        {
            if (newLineReplaceKey.Equals(""))
            {
                newLineReplaceKey = this.RandomString(8, text);
            }
        }

        /// <summary>
        /// Splits the text lines while preserving escaped new line characters.
        /// </summary>
        /// <param name="text">the text to be split safely</param>
        /// <returns>a string array of text lines</returns>
        private string[] splitLinesSafe(string text)
        {
            this.initializeNewLineReplaceKey(text);
            text = text.Replace("\\n", newLineReplaceKey);
            return text.Split('\n');
        }

        /// <summary>
        /// Strips all comments from a line.
        /// 
        /// Anything from a // and beyond is considered a comment.
        /// 
        /// // can be escaped with \/\/ and \/\/ can be escaped with \\/\\/, etc.
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        private string clearLineComments(string line)
        {
            // Remove comments:
            while (line.Contains("//"))
            {
                line = Regex.Replace(line, "//.*$", "");
            }
            return line;
        }

        /// <summary>
        /// Parse out the header data of a script.
        /// # file = filename.ext
        /// # excludes = cola, colb, colc
        /// 
        /// Whitespace contained within the filename will be removed.
        /// 
        /// The offset returned might be an index outside the range of lines[]
        /// 
        /// Made static to allow for access from other classes.
        /// </summary>
        /// <param name="lines">lines of the script</param>
        /// <returns>line offset where the actual script begins</returns>
        public static int parseHeaderData(string[] lines, ref string outFileName, ref HashSet<string> excludeCols)
        {
            return parseHeaderData(lines, ref outFileName, ref excludeCols, HeaderParseArgs.PARSE_ALL);
        }

        public static string parseHeaderDataFilename(string[] lines)
        {
            string outFileName = "";
            HashSet<string> excludeCols = null;
            parseHeaderData(lines, ref outFileName, ref excludeCols, HeaderParseArgs.PARSE_FILENAME);
            return outFileName;
        }

        public static int parseHeaderData(string[] lines, ref string outFileName, ref HashSet<string> excludeCols, HeaderParseArgs headerParseArgs)
        {
            bool isInitData = true;
            int offset = 0;
            int i = 0;
            for (i = 0; i < lines.Length && isInitData; i++)
            {
                if (lines[i].StartsWith("#"))
                {
                    //# file = savefilename.ext
                    if (Regex.IsMatch(lines[i], @"^#[\s]*file[\s]*=[\s]*"))
                    {
                        if (headerParseArgs == HeaderParseArgs.PARSE_FILENAME || headerParseArgs == HeaderParseArgs.PARSE_ALL)
                        {
                            outFileName = Regex.Replace(Regex.Replace(lines[i], @"^#[\s]*file[\s]*=[\s]*", "").Trim(), @"\s", "");
                        }
                    }
                    //# excludes = cola, colb, colc
                    else if (Regex.IsMatch(lines[i], @"^#[\s]*excludes[\s]*=[\s]*"))
                    {
                        if (headerParseArgs == HeaderParseArgs.PARSE_EXCLUDES || headerParseArgs == HeaderParseArgs.PARSE_ALL)
                        {
                            if (excludeCols == null)
                            {
                                excludeCols = new HashSet<string>();
                            }
                            string[] excludes = Regex.Replace(lines[i], @"^#[\s]*excludes[\s]*=[\s]*", "").Trim().Replace(" ", "").Split(',');
                            foreach (string exclude in excludes)
                            {
                                if (!excludeCols.Contains(exclude))
                                {
                                    excludeCols.Add(exclude);
                                }
                            }
                        }
                    }
                }
                else
                {
                    isInitData = false;
                    offset = i;
                }
            }

            // Correctly set offset if only a header is found:
            if (isInitData)
            {
                offset = i;
            }

            return offset;
        }

        /// <summary>
        /// Searches for a start token in the given line.
        /// 
        /// Will save all text up to the token start in lineBeforeTokenStart, 
        /// and will save all data after the token start in lineAfterTokenStart.
        /// 
        /// If the token begins and ends on the same line, this is fine because
        /// searchForTokenEnd will be called on the remaining line data: lineAfterTokenStart
        /// </summary>
        /// <param name="lineAfterTokenStart">line to be searched</param>
        /// <param name="state">current state of search</param>
        /// <param name="lineBeforeTokenStart">extracted text from the token</param>
        /// <param name="nextTokenArgs">token arguments, will be extracted in this method</param>
        /// <param name="tokenName"></param>
        private void searchForTokenStart(ref string lineAfterTokenStart, ref TokenSearchState state, ref string lineBeforeTokenStart, ref ArrayList nextTokenArgs, string tokenName, int lineNo)
        {
            int indexFound = 0;
            for (int i = 0; i < lineAfterTokenStart.Length && state == TokenSearchState.SEARCHING; i++)
            {
                if (lineAfterTokenStart[i] == '<' && checkSubstringForToken(lineAfterTokenStart.Substring(i, lineAfterTokenStart.Length - i), tokenName))
                {
                    state = TokenSearchState.FOUND;
                    indexFound = i;
                }
            }

            // Generate args and remove token from the line if found
            if (state == TokenSearchState.FOUND)
            {
                // Add the skipped data back to parsed text:
                string skippedData = lineAfterTokenStart.Substring(0, indexFound);
                lineBeforeTokenStart += skippedData;

                // Extract args, and set line to the subtring after the token:
                string tokenData = lineAfterTokenStart.Substring(indexFound, lineAfterTokenStart.Length - indexFound);

                // Parses out the arguments and any remaining portion of the line:
                Pair results = extractArgs(tokenData, tokenName, lineNo);

                // The pair result will contain the remaining portion of the line:
                lineAfterTokenStart = (string)results.Value1;

                // As well as an ArrayList of token arguments:
                nextTokenArgs = (ArrayList)results.Value2;
            }
        }

        /// <summary>
        /// Searches for an end token in a given line.
        /// </summary>
        /// <param name="lineAfterTokenEnd">line to be searched</param>
        /// <param name="state">current state of search</param>
        /// <param name="nextTokenArgs">token arguments</param>
        /// <param name="lineBeforeTokenEnd">will be cleared if an end token is found</param>
        /// <param name="tokenClassName">token's class name</param>
        /// <param name="textFull">the scripts complete text, used to ensure a random unique token key is generated as a place holder</param>
        /// <param name="tokenName">the token's name</param>
        /// <returns>the random string key for whichever token was closed</returns>
        private string searchForTokenEnd(ref string lineAfterTokenEnd, ref TokenSearchState state, ref ArrayList nextTokenArgs, ref string lineBeforeTokenEnd, string tokenClassName, string textFull, string tokenName)
        {
            string tokenKey = "";

            // Search for the end of the current token:
            int indexFound = 0;
            for (int i = 0; i < lineAfterTokenEnd.Length && state == TokenSearchState.FOUND; i++)
            {
                if (lineAfterTokenEnd[i] == '/' && checkSubstringForTokenEnd(lineAfterTokenEnd.Substring(i, lineAfterTokenEnd.Length - i), tokenName))
                {
                    state = TokenSearchState.CLOSING;
                    indexFound = i;
                }
            }

            // If the CLOSING state has been set, this indicates the remaining token and line data need to parsed out and set appropriately:
            if (state == TokenSearchState.CLOSING)
            {
                // Add the skipped data back to the token data:
                string skippedData = lineAfterTokenEnd.Substring(0, indexFound);
                lineBeforeTokenEnd += skippedData;

                // For table tokens only, recusively parse out column token data:
                if (tokenName.Equals("table"))
                {
                    foreach (string token in TokenNameToTokenClassNameReflectionHash.Keys)
                    {
                        if (!token.Equals("table"))
                        {
                            lineBeforeTokenEnd = parseTokensAndGenerateTokenHashes(lineBeforeTokenEnd, textFull, token, TokenNameToTokenClassNameReflectionHash[token].ToString());
                        }
                    }
                }

                // Remove begining and trailing spaces:
                lineBeforeTokenEnd = lineBeforeTokenEnd.Trim(new char[] { ' ' });

                // Add the token data to the hash, and use a randomly generated string as a place holder within the script:
                tokenKey = this.AddTokenToHash(tokenClassName, lineBeforeTokenEnd, nextTokenArgs, textFull);
                // tokenKey will a unique random string not found in the script like: "Ahdkhaksjd13JADShasd"

                // Fix the remaining line data by removing the token data and replacing it with the tokenKey:
                string replaceString = "/" + tokenName + ">";
                lineAfterTokenEnd = lineAfterTokenEnd.Substring(indexFound, lineAfterTokenEnd.Length - indexFound);
                lineAfterTokenEnd = tokenKey + lineAfterTokenEnd.Substring(replaceString.Length, lineAfterTokenEnd.Length - replaceString.Length);

                // Reset the search state and continue searching for the next token:
                lineBeforeTokenEnd = "";
                nextTokenArgs = new ArrayList();
                state = TokenSearchState.SEARCHING;
            }

            return tokenKey;
        }

        /// <summary>
        /// This function will parse out all token information and save that data into appropriate hashes.
        /// It will also replace any tokens found in the text with a unique randomly generated place holder data can be easily sewn back together later on.
        /// </summary>
        /// <param name="text">the text to search for tokens, this value will be split on line feeds and iterated over</param>
        /// <param name="textFull">the full text of the script, used to ensure unique random place holder keys are generated</param>
        /// <param name="tokenName">the search token's name</param>
        /// <param name="tokenClassName">the search token's class name</param>
        /// <returns>the text with token data parsed out and replaced with unique place holder keys</returns>
        private string parseTokensAndGenerateTokenHashes(string text, string textFull, string tokenName, string tokenClassName)
        {
            string parsedText = "";
            string nextTokenData = "";
            TokenSearchState state = TokenSearchState.SEARCHING;
            string[] lines = this.splitLinesSafe(text);
            ArrayList nextTokenArgs = new ArrayList();

            // Begin parsing, start with header:
            int offset = parseHeaderData(lines, ref this.OutFileName, ref this.ExcludeCols);

            for (int k = 0 + offset; k < lines.Length; k++) 
            {
                bool searchInLoop = true;
                string line = clearLineComments(lines[k]);

                while (searchInLoop)
                {
                    searchInLoop = false;

                    // Look for an instance of the token:
                    if (state == TokenSearchState.SEARCHING)
                    {
                        this.searchForTokenStart(ref line, ref state, ref parsedText, ref nextTokenArgs, tokenName, k);
                    }

                    // If found, begin looking for the end of the token:
                    if (state == TokenSearchState.FOUND)
                    {
                        this.searchForTokenEnd(ref line, ref state, ref nextTokenArgs, ref nextTokenData, tokenClassName, textFull, tokenName);
                        if (state == TokenSearchState.SEARCHING)
                        {
                            // If a token has been closed on this line, there might be another token in the line so we must search for it:
                            searchInLoop = true;
                        }
                    }
                }

                // Decide where to add line info, the script output or the token data:
                if (state == TokenSearchState.SEARCHING)
                {
                    // If a token has not been found, continue adding line data to the parsed text output.
                    parsedText += line + "\n";
                }
                else
                {
                    // If a token has been found continue adding parsed content to the token data:
                    nextTokenData += line + "\n";
                }
            }

            if (parsedText.EndsWith("\n"))
            {
                parsedText = parsedText.Substring(0, parsedText.Length - 1);
            }

            return parsedText;
        }

        /// <summary>
        /// Add the token data to a hash, accessible by class name.
        /// </summary>
        /// <param name="tokenClassName"></param>
        /// <param name="tokenData"></param>
        /// <param name="nextTokenArgs"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        private string AddTokenToHash(string tokenClassName, string tokenData, ArrayList nextTokenArgs, string text)
        {
            if (!this.TokenDataHash.ContainsKey(tokenClassName))
            {
                this.TokenDataHash.Add(tokenClassName, new Hashtable());
            }
            string randomKey = RandomString(16, text);
            ((Hashtable)this.TokenDataHash[tokenClassName]).Add(randomKey, new Pair(tokenData, nextTokenArgs));
            return randomKey;
        }

        /// <summary>
        /// Generate a random string of specified size (+2 enclosing % characters).
        /// </summary>
        /// <param name="size">length of the string to generate</param>
        /// <param name="checkText">text to check for duplicate keys</param>
        /// <returns>a random string of length size, enclosed in two % characters</returns>
        private string RandomString(int size, string checkText)
        {
            StringBuilder builder = new StringBuilder();
            string returnKey = "";
            while (returnKey.Equals("") || checkText.Contains(returnKey) || this.UsedTokenKeysHash.ContainsKey(returnKey))
            {
                if (!returnKey.Equals(""))
                {
                    // Sleep to generate a different random number:
                    Thread.Sleep(1);
                }

                Random random = new Random();
                char ch;
                for (int i = 0; i < size; i++)
                {
                    ch = Convert.ToChar(Convert.ToInt32(Math.Floor(26 * random.NextDouble() + 65)));
                    builder.Append(ch);
                }

                returnKey = "%" + builder.ToString() + "%";
            }

            this.UsedTokenKeysHash.Add(returnKey, true);
            return returnKey;
        }

        /// <summary>
        /// Replaces all place holder keys with corresponding text based on the database connected to by the user.
        /// </summary>
        /// <param name="text">text containing place holder keys to be replaced</param>
        /// <param name="tokenText">token text that needs to be applied to tables or columns</param>
        /// <param name="tableName">the table name column data belongs to</param>
        /// <param name="tokenClassName">class name of the token being processed</param>
        /// <param name="tokenArgs">arguments for the token being processed</param>
        /// <returns>text with place holder keys replaced by actual data</returns>
        private string applyTokenDataReplacements(string placeHolderKey, string text, string tokenText, string tableName, string tokenClassName, ArrayList tokenArgs)
        {
            string tokenDataResults = "";
            string lastSeperator = "";
            
            DataRow[] rows = (tokenClassName.Equals("Table") ? this.TableDt.Select() : this.ColumnDt.Select("table_name = '" + tableName + "'"));

            for (int i = 0; i < rows.Length; i++)
            {
                DataRow row = rows[i];

                ObjectHandle tokenInstance = Activator.CreateInstance("Db_TeXML.Wpf", Db_TeXML_TokenNamespace + "." + tokenClassName);
                ((Token)tokenInstance.Unwrap()).Initialize(this, row, tokenText, tokenArgs);

                // Tables may contain sub tokens, must check for all of them:
                if (tokenClassName.Equals("Table"))
                {
                    string tableTokenResults = ((Token)tokenInstance.Unwrap()).ParseToken(true);
                    foreach (string token in TokenNameToTokenClassNameReflectionHash.Keys)
                    {
                        // table tokens cannot be embedded within table tokens:
                        if (!token.Equals("table"))
                        {
                            tableTokenResults = this.applyTokenData(tableTokenResults, TokenNameToTokenClassNameReflectionHash[token].ToString(), row["table_name"].ToString());
                        }
                    }

                    tokenDataResults += tableTokenResults;
                    ((Token)tokenInstance.Unwrap()).SetTokenResults(this.applyNewLineCharacters(tableTokenResults), fileOutputData);
                }
                // Col tokens process normally:
                else
                {
                    tokenDataResults += ((Token)tokenInstance.Unwrap()).ParseToken(true);
                }

                lastSeperator = (tokenArgs.Count > 0 ? tokenArgs[0].ToString() : "");
            }
            tokenDataResults = applyArgsToProductText(tokenDataResults, lastSeperator, tokenArgs);
            text = text.Replace(placeHolderKey, tokenDataResults);

            return this.applyNewLineCharacters(text);
        }

        /// <summary>
        /// Apply database information to token data.
        /// </summary>
        /// <param name="text">text with token place holder keys to be populated with data</param>
        /// <param name="tokenClassName">name of current token being parsed</param>
        /// <returns>text with place holder keys populated with actual data</returns>
        private string applyTokenData(string text, string tokenClassName)
        {
            return applyTokenData(text, tokenClassName, null);
        }

        /// <summary>
        /// Apply database information to token data.
        /// </summary>
        /// <param name="text">text with token place holder keys to be populated with data</param>
        /// <param name="tokenClassName">name of current token being parsed</param>
        /// <param name="tableName">used for applying subtoken data to the same table</param>
        /// <returns>text with place holder keys populated with actual data</returns>
        private string applyTokenData(string text, string tokenClassName, string tableName)
        {
            if (this.TokenDataHash.ContainsKey(tokenClassName))
            {
                Hashtable tokenDataHash = (Hashtable)this.TokenDataHash[tokenClassName];

                foreach (string key in tokenDataHash.Keys)
                {
                    Pair pairData = (Pair)tokenDataHash[key];
                    string tokenText = pairData.Value1.ToString();
                    ArrayList tokenArgs = (ArrayList)pairData.Value2;
                    text = applyTokenDataReplacements(key, text, tokenText, tableName, tokenClassName, tokenArgs);
                }
            }
            return text;
        }

        /// <summary>
        /// Applies arg data to token data results.
        /// The first token arg (arg[0]) is the seperator arg, and if it exists, the
        /// last seperator needs to be removed from the results.
        /// </summary>
        /// <param name="tokenDataResults">generated token results</param>
        /// <param name="lastSeperator">value of the seperator arg last applied</param>
        /// <param name="tokenArgs">token args</param>
        /// <returns></returns>
        private string applyArgsToProductText(string tokenDataResults, string lastSeperator, ArrayList tokenArgs)
        {
            // Remove the last seperator from the end of the token results:
            // So we get: "cola, colb, colc" instead of "cola, colb, colc,"
            if (tokenDataResults.Length >= lastSeperator.Length)
            {
                tokenDataResults = this.applyNewLineCharacters(tokenDataResults);
                tokenDataResults = tokenDataResults.Substring(0, tokenDataResults.Length - this.applyNewLineCharacters(lastSeperator).Length);
            }

            // Apply further args if they exist:
            if (tokenArgs.Count > 1)
            {
                // Only apply args to non empty results:
                if (!tokenDataResults.Trim().Equals(""))
                {
                    // Arg[1] is the prefix arg:
                    tokenDataResults = tokenArgs[1].ToString() + tokenDataResults;
                    
                    if (tokenArgs.Count > 2)
                    {
                        // Arg[2] is the suffix arg:
                        tokenDataResults = tokenDataResults + tokenArgs[2].ToString();
                    }
                }
            }
            return tokenDataResults;
        }
        #endregion
        
        #region Initialize Parse Engine Tokens
        /// <summary>
        /// Iterates over the entire "Db_TeXML.Wpf.parser.tokens" namespace to find public get members.
        /// Generates a list of possible tokens user input will accept and replace with values when the engine parses their script.
        /// This method should only be called once in the static constructor.
        /// 
        /// The following static hashtables will be set in this method:
        /// 1. TokenNameToTokenClassNameReflectionHash
        /// 2. TokenTypeToReflectionCallbackHash
        /// </summary>
        private static void InitializeParseEngineTokens()
        {
            if (TokenNameToTokenClassNameReflectionHash.Count == 0 && TokenTypeToReflectionCallbackHash.Count == 0)
            {
                // Get a list of all assemblies:
                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                ArrayList assemblyList = new ArrayList();
                foreach (Assembly assembly in assemblies)
                {
                    if (assembly.FullName.StartsWith("Db_TeXML.Wpf"))
                    {
                        assemblyList.Add(assembly);
                    }
                }

                // Get a list of all types in the Db_TeXML.Wpf.parser.tokens namespace:
                var parseTokens = ((Assembly[])assemblyList.ToArray(typeof(Assembly)))
                           .SelectMany(t => t.GetTypes())
                           .Where(t => t.IsClass && t.Namespace == Db_TeXML_TokenNamespace);

                // Iterate over each type and set the static reflection hashtables with appropriate information:
                foreach (System.Type type in parseTokens)
                {
                    string token = type.Name.ToLower();
                    MemberInfo[] fields = type.GetMembers();

                    // Map the token names to their type names:
                    if (!TokenNameToTokenClassNameReflectionHash.ContainsKey(token))
                    {
                        TokenNameToTokenClassNameReflectionHash.Add(token, type.Name);
                    }

                    foreach (MemberInfo field in fields)
                    {
                        if (field.Name.StartsWith("get_"))
                        {
                            // Map each token class type to all public methods contained in the token:
                            // (these public methods can later be called with reflection to help parse scripts dynamically)
                            if (!TokenTypeToReflectionCallbackHash.ContainsKey(type))
                            {
                                // Initialize the hashtable to contain the callbacks:
                                TokenTypeToReflectionCallbackHash.Add(type, new Hashtable());
                            }

                            string subtoken = field.Name.Replace("get_", "");
                            MethodInfo subtokenMethodInfo = field.DeclaringType.GetMethod(field.Name);
                            MemberInfo[] subFields = subtokenMethodInfo.ReturnType.GetMembers();

                            foreach (MemberInfo subField in subFields)
                            {
                                if (subField.Name.StartsWith("get_"))
                                {
                                    string subsubtoken = subField.Name.Replace("get_", "");
                                    string addToken = (token + "." + subtoken + "." + subsubtoken).ToLower();
                                    if (!TokenTypeToReflectionCallbackHash.ContainsKey(addToken))
                                    {
                                        // Map the script variables to their callback methods:
                                        ((Hashtable)TokenTypeToReflectionCallbackHash[type]).Add(addToken, field.Name + "|" + subField.Name);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        #endregion

        #region Generate Data Table From Csv (Practice Mode)
        /// <summary>
        /// Converts a csv file to a DataTable object using \r for row delimeters and \t for cell delimeters.
        /// </summary>
        /// <param name="csv">csv file text</param>
        /// <param name="tableName">table name to save results as</param>
        /// <returns>DataTable converted from the csv text</returns>
        private DataTable generateDataTableFromCsv(string csv, string tableName)
        {
            DataTable dt = new DataTable(tableName);

            string[] lines = csv.Split('\n');
            foreach (string header in lines[0].Replace("\r", "").Split('\t'))
            {
                dt.Columns.Add(new DataColumn(header));
            }

            for (int i = 1; i < lines.Length; i++)
            {
                string[] parts = lines[i].Split('\t');
                DataRow nextRow = dt.NewRow();
                for (int k = 0; k < parts.Length; k++)
                {
                    string next = parts[k].Trim();

                    // Replace "(NULL)" values with empty strings:
                    if (next.Contains("(NULL)"))
                    {
                        nextRow[k] = "";
                    }
                    else
                    {
                        nextRow[k] = next;
                    }
                }
                dt.Rows.Add(nextRow);
            }

            return dt;
        }
        #endregion

        #region Private Class Pair
        /// <summary>
        /// A simple class to hold a pair of values.
        /// </summary>
        private class Pair
        {
            public object Value1;
            public object Value2;

            public Pair(object value1, object value2)
            {
                this.Value1 = value1;
                this.Value2 = value2;
            }
        }
        #endregion

        #region ************Unit Tests************
        [Test]
        public void test_setSaveFileInformation()
        {
            string value = this.setSaveFileInformation(@"aaa", "bbb", "ccc");
            Assert.AreEqual(value.Contains("aaa") && value.Contains("bbb") && value.Contains("ccc"), true);

            value = this.setSaveFileInformation(@"zzz\aaa", "bbb", "ccc");
            Assert.AreEqual(value.Contains("aaa") && value.Contains("bbb") && !value.Contains("ccc"), true);

            this.testHelper_setSaveFileInformationExceptionCheck(@"..\zzz\aaa", "bbb", "ccc");
            this.testHelper_setSaveFileInformationExceptionCheck(@"zzz\aaa\", "bbb", "ccc");

            // Check invalid file characters:
            // \ / : * ? " < > |
            this.testHelper_setSaveFileInformationExceptionCheck(@"zzz\a/aa", "bbb", "ccc");
            this.testHelper_setSaveFileInformationExceptionCheck(@"zzz\a:aa", "bbb", "ccc");
            this.testHelper_setSaveFileInformationExceptionCheck(@"zzz\a*aa", "bbb", "ccc");
            this.testHelper_setSaveFileInformationExceptionCheck(@"zzz\a?aa", "bbb", "ccc");
            this.testHelper_setSaveFileInformationExceptionCheck("zzz\\a\"aa", "bbb", "ccc");
            this.testHelper_setSaveFileInformationExceptionCheck(@"zzz\a<aa", "bbb", "ccc");
            this.testHelper_setSaveFileInformationExceptionCheck(@"zzz\a>aa", "bbb", "ccc");
            this.testHelper_setSaveFileInformationExceptionCheck(@"zzz\a|aa", "bbb", "ccc");
        }

        private void testHelper_setSaveFileInformationExceptionCheck(string fileNameKey, string databaseName, string outputName)
        {
            bool exceptionThrown = false;
            try
            {
                this.setSaveFileInformation(fileNameKey, databaseName, outputName);
            }
            catch
            {
                exceptionThrown = true;
            }
            Assert.AreEqual(exceptionThrown, true);
        }

        [Test]
        public void test_extractArgs()
        {
            Pair output = this.extractArgs(@"<table('aaa','bbb','ccc','ddd')", "table", 0);
            ArrayList value2 = (ArrayList)output.Value2;
            Assert.AreEqual(4, value2.Count);
            Assert.AreEqual(@"aaa", value2[0]);
            Assert.AreEqual(@"bbb", value2[1]);
            Assert.AreEqual(@"ccc", value2[2]);
            Assert.AreEqual(@"ddd", value2[3]);

            output = this.extractArgs(@"<table(    'aaa\\\\abc\'',   'b\b\b'    ,  '\\\ccc'  ,'ddd\mypath.table.name.sql.ext'     )", "table", 0);
            value2 = (ArrayList)output.Value2;
            Assert.AreEqual(4, value2.Count);
            Assert.AreEqual(@"aaa\\abc'", value2[0]);
            Assert.AreEqual(@"b\b\b", value2[1]);
            Assert.AreEqual(@"\\ccc", value2[2]);
            Assert.AreEqual(@"ddd\mypath.table.name.sql.ext", value2[3]);

            testHelper_extractArgsExceptionCheck(@"<table('aaa\\\\abc\'',,'b\b\b','\\\ccc','ddd\mypath.table.name.sql.ext')", "table");
            testHelper_extractArgsExceptionCheck(@"<table('aaa\\\\abc\'\','b\b\b','\\\ccc','ddd\mypath.table.name.sql.ext')", "table");
            testHelper_extractArgsExceptionCheck(@"<table(\'aaa\\\\abc\'','b\b\b','\\\ccc','ddd\mypath.table.name.sql.ext')", "table");
            testHelper_extractArgsExceptionCheck(@"<table('aaa\\\\abc\'','b\b\b','\\\ccc','ddd\mypath.table.name.sql.ext',)", "table");
            testHelper_extractArgsExceptionCheck(@"<table('aaa\\\\abc\'','b\b\b','\\\ccc','ddd\mypath.table.name.sql.ext'',)", "table");
            testHelper_extractArgsExceptionCheck(@"<table(\\'aaa\\\\abc\'','b\b\b','\\\ccc''ddd\mypath.table.name.sql.ext')", "table");
            testHelper_extractArgsExceptionCheck(@"<table('aaa\\\\abc\'','b\b\b','\\\ccc',,,'ddd\mypath.table.name.sql.ext')", "table");
        }

        private void testHelper_extractArgsExceptionCheck(string tokenData, string tokenName)
        {
            bool exceptionThrown = false;
            try
            {
                this.extractArgs(tokenData, tokenName, 0);
            }
            catch
            {
                exceptionThrown = true;
            }
            Assert.AreEqual(exceptionThrown, true);
        }

        [Test]
        public void test_parseHeaderData()
        {
            this.testHelper_parseHeaderDataCheckData(new string[] { "# hello", "# test" }, "", new HashSet<string>(), 2);
            this.testHelper_parseHeaderDataCheckData(new string[] { "#     file =   myfilename .txt", "# test" }, "myfilename.txt", new HashSet<string>(), 2);
            this.testHelper_parseHeaderDataCheckData(new string[] { "#file=myfilename.txt    ", "# test" }, "myfilename.txt", new HashSet<string>(), 2);
            this.testHelper_parseHeaderDataCheckData(new string[] { "#file=myf  ilename.txt    ", "# excludes=" }, "myfilename.txt", new HashSet<string>(), 2);
            this.testHelper_parseHeaderDataCheckData(new string[] { "#file= my   file  name.txt    ", "#excludes    =cola,    colb,       colc        " }, "myfilename.txt", new HashSet<string>() { "cola", "colb", "colc" }, 2);
        }

        public void testHelper_parseHeaderDataCheckData(string[] lines, string expectedOutFileName, HashSet<string> expectedExcludeCols, int expectedOffset)
        {
            string outFileName = "";
            HashSet<string> excludeCols = new HashSet<string> { };
            int offset = parseHeaderData(lines, ref outFileName, ref excludeCols);

            Assert.AreEqual(offset, expectedOffset);
            Assert.AreEqual(outFileName, expectedOutFileName);
            if (expectedExcludeCols != null && excludeCols != null)
            {
                foreach (string key in expectedExcludeCols)
                {
                    Assert.AreEqual(excludeCols.Contains(key), true);
                }
            }
            else
            {
                Assert.AreEqual(expectedExcludeCols, excludeCols);
            }
        }

        [Test]
        public void test_searchForTokenStart()
        {
            this.testHelper_searchForTokenStartCheckData(
                "aaa<table: zzz /table>bbb", TokenSearchState.SEARCHING, "", new ArrayList(), "table", 0,
                "aaa", TokenSearchState.FOUND, " zzz /table>bbb", new ArrayList()
            );

            this.testHelper_searchForTokenStartCheckData(
                "aaa        tokenstart  <table('','',''   ,''   ):      zzz <col(''): col.name.sql /col>/table>bbb", TokenSearchState.SEARCHING, "", new ArrayList(), "table", 0,
                "aaa        tokenstart  ", TokenSearchState.FOUND, "      zzz <col(''): col.name.sql /col>/table>bbb", new ArrayList()
            );

            this.testHelper_searchForTokenStartCheckData(
                "aaa        tokenstart  <table ('','',''   ,''   )<table:      zzz <col(''): col.name.sql /col>/table>bbb", TokenSearchState.SEARCHING, "", new ArrayList(), "table", 0,
                "aaa        tokenstart  <table ('','',''   ,''   )", TokenSearchState.FOUND, "      zzz <col(''): col.name.sql /col>/table>bbb", new ArrayList()
            );

            this.testHelper_searchForTokenStartCheckData(
                "<table:x/table><table:y/table>", TokenSearchState.SEARCHING, "", new ArrayList(), "table", 0,
                "", TokenSearchState.FOUND, "x/table><table:y/table>", new ArrayList()
            );
        }

        private void testHelper_searchForTokenStartCheckData(
            string lineAfterTokenStart, TokenSearchState state, string lineBeforeTokenStart, ArrayList nextTokenArgs, string tokenName, int lineNo,
            string expectedLineAfterTokenStart, TokenSearchState expectedState, string expectedLineUpToTokenStart, ArrayList expectedNextTokenArgs)
        {
            this.searchForTokenStart(ref lineAfterTokenStart, ref state, ref lineBeforeTokenStart, ref nextTokenArgs, tokenName, lineNo);

            Assert.AreEqual(lineAfterTokenStart, expectedLineUpToTokenStart);
            Assert.AreEqual(lineBeforeTokenStart, expectedLineAfterTokenStart);
            Assert.AreEqual(state, expectedState);
        }

        [Test]
        public void test_searchForTokenEnd()
        {
            this.testHelper_searchForTokenEndCheckData(
                " zzz /table>bbb", TokenSearchState.FOUND, "", new ArrayList(), "table", "Table", "",
                "bbb", TokenSearchState.SEARCHING, "", new ArrayList()
            );

            this.testHelper_searchForTokenEndCheckData(
                "      zzz <col(''): col.name.sql /col>/table>bbb", TokenSearchState.FOUND, "", new ArrayList(), "table", "Table", "",
                "bbb", TokenSearchState.SEARCHING, "", new ArrayList()
            );


            this.testHelper_searchForTokenEndCheckData(
                "      zzz <col(''): col.name.sql /col>/table>bbb", TokenSearchState.FOUND, "", new ArrayList(), "table", "Table", "",
                "bbb", TokenSearchState.SEARCHING, "", new ArrayList()
            );
        }

        private void testHelper_searchForTokenEndCheckData(
            string lineAfterTokenEnd, TokenSearchState state, string lineBeforeTokenEnd, ArrayList nextTokenArgs, string tokenName, string tokenClassName, string textFull,
            string expectedLineAfterTokenEnd, TokenSearchState expectedState, string expectedLineBeforeTokenEnd, ArrayList expectedNextTokenArgs)
        {
            string key = this.searchForTokenEnd(ref lineAfterTokenEnd, ref state, ref nextTokenArgs, ref lineBeforeTokenEnd, tokenClassName, textFull, tokenName);

            Assert.AreEqual(lineAfterTokenEnd, key + expectedLineAfterTokenEnd);
            Assert.AreEqual(lineBeforeTokenEnd, expectedLineBeforeTokenEnd);
            Assert.AreEqual(state, expectedState);
        }
        #endregion
    }
}
