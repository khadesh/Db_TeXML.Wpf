using System;
using System.Collections.Generic;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Folding;
using System.Text.RegularExpressions;
using Db_TeXML.Wpf.parser;
using System.Collections;

namespace Db_TeXML.Wpf
{
    public class DbTeXMLFoldingStrategy
    {
        public char OpeningBrace { get; set; }
        public char ClosingBrace { get; set; }

        public DbTeXMLFoldingStrategy()
        {
            this.OpeningBrace = '<';
            this.ClosingBrace = '>';
		}
		
		public IEnumerable<NewFolding> CreateNewFoldings(TextDocument document, out int firstErrorOffset)
		{
			firstErrorOffset = -1;
			return CreateNewFoldings(document);
		}

        private string getStartTokensRegex()
        {
            string regex = "";
            foreach (var key in ParseEngine.TokenTypeToReflectionCallbackHash.Keys)
            {
                string subkey = ((System.Type)key).Name.ToString().ToLower();
                regex += "<" + subkey + "[(:]|";
            }
            regex = regex.Substring(0, regex.Length - 1);
            return "(" + regex + ")";
        }

        private string getEndTokensRegex()
        {
            string regex = "";
            foreach (var key in ParseEngine.TokenTypeToReflectionCallbackHash.Keys)
            {
                string subkey = ((System.Type)key).Name.ToString().ToLower();
                regex += "/" + subkey + ">|";
            }
            regex = regex.Substring(0, regex.Length - 1);
            return "(" + regex + ")";
        }

        public bool CheckStartTag(string checkText)
        {
            return Regex.IsMatch(checkText, this.getStartTokensRegex());
        }

        public bool CheckEndTag(string checkText)
        {
            return Regex.IsMatch(checkText, this.getEndTokensRegex());
        }

		public IEnumerable<NewFolding> CreateNewFoldings(ITextSource document)
		{            
			List<NewFolding> newFoldings = new List<NewFolding>();
			Stack<int> startOffsets = new Stack<int>();
			int lastNewLineOffset = 0;
			char openingBrace = this.OpeningBrace;
			char closingBrace = this.ClosingBrace;
			for (int i = 0; i < document.TextLength; i++) {
				char c = document.GetCharAt(i);
				if (c == openingBrace) 
                {
                    int offset = Math.Min((document.TextLength - i), 15);
                    string checkText = document.GetText(i, offset);

                    if (CheckStartTag(checkText))
                    {
                        startOffsets.Push(i);
                    }
				}
                else if (c == closingBrace && startOffsets.Count > 0)
                {
                    int startIndex = Math.Max(i - 14, 0);
                    int offset = Math.Min(15, document.TextLength);
                    string checkText = document.GetText(startIndex, offset);

                    if (CheckEndTag(checkText))
                    {
                        int startOffset = startOffsets.Pop();
                        // don't fold if opening and closing brace are on the same line
                        if (startOffset < lastNewLineOffset)
                        {
                            newFoldings.Add(new NewFolding(startOffset, i + 1));
                        }
                    }
				}
                else if (c == '\n' || c == '\r')
                {
					lastNewLineOffset = i + 1;
				}
			}

            newFoldings.Sort((a, b) => a.StartOffset.CompareTo(b.StartOffset));
			return newFoldings;
		}

        public void UpdateFoldings(FoldingManager manager, TextDocument document)
        {
			int firstErrorOffset = -1;
            IEnumerable<NewFolding> newFoldings = this.CreateNewFoldings(document, out firstErrorOffset);
            manager.UpdateFoldings(newFoldings, firstErrorOffset);
        }
	}
}
