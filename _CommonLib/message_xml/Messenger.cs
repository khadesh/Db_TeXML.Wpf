#region References
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Xml;
using System.IO;
#endregion

namespace _CommonLib
{
    public class Messenger
    {
        #region Message Access Methods and Operator Overloads
        private static Hashtable messages = new Hashtable();

        /// <summary>
        /// Keys are not case sensitive.
        /// </summary>
        /// <param name="key">Index</param>
        /// <returns>The message indexed by key.</returns>
        public string this[string key]
        {
            get { return GetMessage(key); }
        }

        /// <summary>
        /// Keys are not case sensitive.
        /// </summary>
        /// <param name="key">Index</param>
        /// <param name="replacements">replacement values for %i% marked indicies within the message.</param>
        /// <returns>The message indexed by key.</returns>
        public string this[string key, string[] replacements]
        {
            get { return GetMessage(key, replacements); }
        }

        /// <summary>
        /// Keys are not case sensitive.
        /// </summary>
        /// <param name="key">Index</param>
        /// <returns>The message indexed by key.</returns>
        public static string GetMessage(string key)
        {
            if (!messages.ContainsKey(key.ToLower()))
            {
                return "";
            }
            return messages[key.ToLower()].ToString();
        }

        #region Standard Errors
        public static string GetErrorMessage_NoDataError() { return GetMessage("General.NoDataError"); }
        public static string GetErrorMessage_CreationError() { return GetMessage("General.CreationError"); }
        public static string GetErrorMessage_DeletionError() { return GetMessage("General.DeletionError"); }
        public static string GetErrorMessage_InputRequired() { return GetMessage("General.InputRequired"); }
        public static string GetErrorMessage_DeleteConfirmation() { return GetMessage("General.DeleteConfirmation"); }
        public static string GetErrorMessage_DeleteConfirmationTitle() { return GetMessage("General.DeleteConfirmationTitle"); }
        #endregion

        /// <summary>
        /// Keys are not case sensitive.
        /// </summary>
        /// <param name="key">Index</param>
        /// <param name="replacements">replacement values for %i% marked indicies within the message.</param>
        /// <returns>The message indexed by key.</returns>
        public static string GetMessage(string key, string[] replacements)
        {
            if (!messages.ContainsKey(key.ToLower()))
            {
                return "";
            }
            string value = messages[key.ToLower()].ToString();
            for (int i = 0; i < replacements.Length; i++)
            {
                value = value.Replace("%" + i + "%", replacements[i]);
                value = value.Replace("{" + i + "}", replacements[i]);
            }
            return value;
        }
        #endregion

        #region Static Constructor
        /// <summary>
        /// The static constructor is meant to read in and store all messages defined in the Messages.xml file.
        /// </summary>
        static Messenger()
        {
            try
            {
                StringBuilder output = new StringBuilder();
                String xmlString = "";
                {
                    xmlString = File.ReadAllText("message_xml\\Messages.xml");
                }
                using (XmlReader reader = XmlReader.Create(new StringReader(xmlString)))
                {
                    XmlWriterSettings ws = new XmlWriterSettings();
                    ws.Indent = true;
                    using (XmlWriter writer = XmlWriter.Create(output, ws))
                    {
                        while (reader.Read())
                        {
                            switch (reader.NodeType)
                            {
                                case XmlNodeType.Element:
                                    if (reader.Name.Equals("message"))
                                    {
                                        try
                                        {
                                            if (reader.GetAttribute("key") != null &&
                                                !reader.GetAttribute("key").Equals(""))
                                            {
                                                if (!messages.ContainsKey(reader.GetAttribute("key").ToLower()))
                                                {
                                                    messages.Add(
                                                        reader.GetAttribute("key").ToLower(),
                                                        reader.GetAttribute("value").Replace("\\n", "\n")
                                                    );
                                                }
                                            }
                                        }
                                        catch { }
                                    }
                                    break;
                            }
                        }
                    }
                }
                string result = output.ToString();
            }
            catch { }
        }
        #endregion
    }
}
