using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Data;
using System.IO;
using System.Collections;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Schema;
using System.Diagnostics;

namespace WordSend
{
    /// <summary>
    /// High level class to handle processing USFM files.
    /// </summary>
    public class SFConverter
    {
        static public Scriptures scripture;
        static public XMLini appIni;
        static public XMLini jobIni;
        static private string appHomeDir;

        /// <summary>
        /// Returns the full path to a file name in the application directory.
        /// </summary>
        /// <param name="fName">file name without path</param>
        /// <returns>Path to appliction directory combined with the given file name</returns>
        static public string AppDir(string fName)
        {
            if (appHomeDir == null)
            {
                appHomeDir = Path.GetDirectoryName(System.IO.Path.GetFullPath(Environment.GetCommandLineArgs()[0]));
            }
            return (Path.Combine(appHomeDir, fName));
        }

        /// <summary>
        /// Find an auxilliary file associated with this program, searching:
        /// 1. The application directory (where this .exe resides)
        /// 2. The Resources directory in the directory above the application directory (standard on Mac OS X).
        /// 3. 2 directories above the executable directory (great for development work)
        /// </summary>
        /// <param name="fName">Name of resource file to find</param>
        /// <returns>Full path to the given resource file</returns>
        static public string FindAuxFile(string fName)
        {
            string result = AppDir(fName);
            if (File.Exists(result))
                return result;
            result = Path.Combine(Path.Combine(Path.Combine(appHomeDir, ".."), "Resources"), fName);
            if (File.Exists(result))
                return result;
            result = Path.Combine(Path.Combine(Path.Combine(appHomeDir, @".."), @".."), fName);
            if (File.Exists(result))
                return result;
            fileHelper.DebugWrite("Couldn't find " + fName);
            return fName;
        }

        /// <summary>
        /// Read a USFM file with automatic text encoding detection (within small limits)
        /// </summary>
        /// <param name="fileSpec">USFM file or wildcard file specification to read</param>
        static public void ProcessFilespec(string fileSpec)
        {
            ProcessFilespec(fileSpec, null);
        }

        /// <summary>
        /// Expand file wildcards and process found files with the USFM reader using
        /// the specified text encoding (null = automatic text encoding attempt).
        /// </summary>
        /// <param name="fileSpec">File specification possibly including wild card(s)</param>
        /// <param name="textEncoding">Text encoding (or null to attempt to figure it out automatically)</param>
        static public void ProcessFilespec(string fileSpec, Encoding textEncoding)
        {
            string dirPath = Path.GetDirectoryName(fileSpec);
            if ((dirPath == null) || (dirPath == ""))
                dirPath = ".";
            string fileName = Path.GetFileName(fileSpec);
            if ((fileName == null) || (fileName == ""))
                fileName = "*.sfm";
            DirectoryInfo dir = new DirectoryInfo(dirPath);
            foreach (FileInfo f in dir.GetFiles(fileName))
            {
                SFConverter.scripture.ReadUSFM(f.FullName, textEncoding);
            }
        }

       
        /// <summary>
        /// Get one command line option from a switch. The option may or may not
        /// be separated from the switch by white space.
        /// </summary>
        /// <param name="i">index into the array of command line arguments</param>
        /// <param name="args">command line arguments</param>
        /// <returns>string representing the indexed command line argument</returns>
        public static string GetOption(ref int i, string[] args)
        {
            string result = "";

            if (args[i].Length > 2)
            {
                result = args[i].Remove(0, 2);
            }
            else
            {
                i++;
                if (args.Length > i)
                    result = args[i];
            }
            return result;
        }
    }
}
