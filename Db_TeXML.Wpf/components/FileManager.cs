using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Input;

namespace Db_TeXML.Wpf
{
    public class FileManager
    {
        /// <summary>
        /// Save a file and create it's directory if it doesn't exist.
        /// Truncate an existing file before saving.
        /// </summary>
        /// <param name="saveFilepath">path to save to</param>
        /// <param name="saveText">the text to save</param>
        public static void SaveFile(string saveFilepath, string saveText)
        {
            // Get the save dir from the filepath:
            string saveDirectory = Regex.Replace(saveFilepath, @"\\[^\\]*$", "");

            // Ensure malicious code isn't trying to create directories in strange places:
            if (!saveDirectory.Contains("..\\"))
            {
                // Create non existing directories:
                if (!Directory.Exists(saveDirectory))
                {
                    Directory.CreateDirectory(saveDirectory);
                }

                // Truncate the file if it already exists, otherwise open normally:
                FileStream saveStream = (File.Exists(saveFilepath) ? File.Open(saveFilepath, FileMode.Truncate) : File.OpenWrite(saveFilepath));

                // Write and close:
                saveStream.Write(Encoding.ASCII.GetBytes(saveText), 0, saveText.Length);
                saveStream.Close();
            }
        }
    }
}
