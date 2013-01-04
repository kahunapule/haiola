// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003-2013, SIL International, EBT, and Youth With A Mission
// <copyright from='2003' to='2013' company='SIL International, EBT, and Youth With A Mission'>
//    
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
// File: Logit.cs
// Responsibility: (Kahunapule) Michael P. Johnson
// Last reviewed: 
// 
// <remarks>
// UI agnostic logging/status reporting mechanism
// </remarks>
// --------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
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
    public delegate void StringDelegate(string s);

    /// <summary>
    /// This class provides a way to display results from the command console,
    /// a scrolling list box, or a log file (or any combination of those.
    /// </summary>
    public class Logit
    {
        public static StringDelegate GUIWriteString;
        public static StringDelegate UpdateStatus;
        public static bool useConsole;
        //		public static System.Windows.Forms.ListBox lstBox;
        protected static System.IO.StreamWriter sw;
        public static bool loggedError = false;
        public static string logFileName = String.Empty;

        public static void ShowStatus(string s)
        {
            if (UpdateStatus != null)
                UpdateStatus(s);
        }

        public static void WriteError(string s)
        {
            WriteLine(s);
            loggedError = true;
        }

        public static void WriteLine(string s)
        {
            if (useConsole)
                Console.WriteLine(s);
            if (GUIWriteString != null)
                GUIWriteString(s);
            if ((!useConsole) && (GUIWriteString == null) && (sw == null))
                System.Windows.Forms.MessageBox.Show(s);
            if (sw != null)
                sw.WriteLine(s);
        }

        public static void OpenFile(string fName)
        {
            loggedError = false;
            try
            {
                CloseFile();
                sw = new StreamWriter(fName, false);
                if (useConsole)
                    Console.WriteLine("Log file opened: " + fName);
                logFileName = fName;
            }
            catch
            {
                WriteLine("Unable to open log file " + fName);
            }
        }

        public static void CloseFile()
        {
            try
            {
                if (sw != null)
                    sw.Close();
                sw = null;
            }
            catch
            {
                sw = null;
            }
        }
    }
}
