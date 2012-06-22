// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003-2011, SIL International, EBT, and Youth With A Mission
// <copyright from='2003' to='2011' company='SIL International, EBT, and Youth With A Mission'>
//		Copyright © 2004, SIL International. All Rights Reserved.   
//    
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
// File: BibleFileLib.cs
// Responsibility: (Kahunapule) Michael P. Johnson
// Last reviewed: 
// 
// <remarks>
// Most of the inner workings of the WordSend Bible format conversion
// project are in this file. The objects in this object library are
// called by both the command line and the Windows UI versions of the
// USFM to WordML converter. They may also be used by other conversion
// processes.
// </remarks>
// --------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Collections;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Diagnostics;


namespace WordSend
{

	/// <summary>
	/// The purpose of this class is to provide very simple persistence of variables
	/// in the manner of old .ini files, but without sections. Files are persisted in
	/// XML files based on ini.dtd. All variables are keyed based on a case-sensitive
	/// string. If this class gets heavily used, it should be refactored as a database
	/// rather than a simple XML file.
	/// </summary>
	public class XMLini
	{
		private string fileName;
		private Hashtable hashTbl;

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="XMLini"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public XMLini(string iniName)
		{
			XmlTextReader xml = null;
			hashTbl = new Hashtable();

			string k = null;
			string v = null;
			string elementName = null;
			
			string dirName = Path.GetDirectoryName(iniName);
            if (!Directory.Exists(dirName))
                Directory.CreateDirectory(dirName);

            /****
			string dtdname = Path.Combine(Path.GetDirectoryName(iniName),"ini.dtd");
			if (!File.Exists(dtdname))
			{
				try
				{
					StreamWriter sw = new StreamWriter(dtdname, false, System.Text.Encoding.UTF8);
					sw.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n"+
						"<!--.ini file replacement-->\n"+
						"<!ELEMENT ini (entry*)>\n"+
						"<!ELEMENT entry (key, value)>\n"+
						"<!ELEMENT key (#PCDATA)>\n"+
						"<!ELEMENT value (#PCDATA)>");
					sw.Flush();
					sw.Close();
				}
				catch
				{
					// Probably a path to removed or read-only media
				}
			}
            ****/

			// Open the named XML file and read in all entries into keys and values
			// arrays.
			fileName = iniName;
			// debugLog.WriteLine(fileName);
			// debugLog.Close();
			
			if (File.Exists(fileName))
			{
				try
				{
					xml = new XmlTextReader(fileName);
					xml.WhitespaceHandling = WhitespaceHandling.None;
					xml.MoveToContent();
                    try
                    {
                        while (xml.Read())
                        {
                            switch (xml.NodeType)
                            {
                                case XmlNodeType.Element:
                                    elementName = xml.Name;
                                    if (elementName == "entry")
                                    {
                                        k = v = "";
                                    }
                                    break;
                                case XmlNodeType.Text:
                                    if (elementName == "key")
                                        k += xml.Value;
                                    else if (elementName == "value")
                                        v += xml.Value;
                                    break;
                                case XmlNodeType.EntityReference:
                                    if (elementName == "key")
                                        k += xml.Value;
                                    else if (elementName == "value")
                                        v += xml.Value;
                                    break;
                                case XmlNodeType.EndElement:
                                    if (xml.Name == "entry")
                                        hashTbl[k] = v;
                                    break;
                            }
                        }
                    }
                    catch
                    {
                        Logit.WriteError("Bad input format in file " + fileName + "; using defaults.");
                    }
				}
				finally
				{
					if (xml != null)
						xml.Close();
				}
			}
		}

        /// <summary>
        /// Using ~ as an escape character, ensure the output string has no <, >, or & characters
        /// in a reversible way. Expands the string when any of those 4 characters are in it.
        /// </summary>
        /// <param name="s">unencoded string</param>
        /// <returns>encoded string</returns>
        public static string encodeStringForXml(string s)
        {
            if ((s == null) || (s == String.Empty))
                return s;
            string result = s.Replace("~", "~~");
            result = result.Replace("<", "~l");
            result = result.Replace(">", "~g");
            result = result.Replace("&", "~a");
            return result;
        }

        /// <summary>
        /// Undoes what encodeStringForXml does.
        /// </summary>
        /// <param name="s">encoded string</param>
        /// <returns>unencoded (plain, original) string</returns>
        public static string unencodeStringForXml(string s)
        {
            if ((s == null) || (s == String.Empty))
                return s;
            string result = s.Replace("~a", "&");
            result = result.Replace("~g", ">");
            result = result.Replace("~l", "<");
            result = result.Replace("~~", "~");
            return result;
        }

		public void WriteString(string key, string val)
		{
			hashTbl[key] = encodeStringForXml(val);
		}

		public void WriteInt(string key, int i)
		{
			hashTbl[key] = i.ToString();
		}

		public void WriteBool(string key, bool b)
		{
			hashTbl[key] = b.ToString();
		}

		public string ReadString(string key, string deflt)
		{
			string result = unencodeStringForXml((string) hashTbl[key]);
			if (result == null)
			{
				result = deflt;
			}
			return result;
		}

		public int ReadInt(string key, int deflt)
		{
			int result = deflt;
			try
			{
				string s = (string) hashTbl[key];
				if ((s == null) || (s == ""))
				{
					return deflt;
				}
				result = int.Parse(s);
			}
			catch
			{
			}
			return result;
		}

		public bool ReadBool(string key, bool deflt)
		{
			bool result = deflt;
			try
			{
				string s = (string) hashTbl[key];
				if ((s == null) || (s == ""))
					return deflt;
				result = bool.Parse(s);
			}
			catch
			{
			}
			return result;
		}

        public void WriteDateTime(string key, DateTime dt)
        {
            hashTbl[key] = dt.ToString("o");
        }

        public DateTime ReadDateTime(string key, DateTime deflt)
        {
            DateTime result = deflt;
            try
            {
                string s = (string)hashTbl[key];
                if ((s == null) || (s == ""))
                    return deflt;
                result = DateTime.Parse(s, null, System.Globalization.DateTimeStyles.RoundtripKind);
            }
            catch
            {
            }
            return result;
        }

		// Flush the hash table to an XML file ("save").
		public bool Write()
		{
			XmlTextWriter xml = null;
			bool result = false;
			try
			{
				try
				{
					xml = new XmlTextWriter(fileName, System.Text.Encoding.UTF8);
					xml.WriteStartDocument();
					xml.Formatting = Formatting.Indented;
					xml.WriteDocType("ini", null, null/* "ini.dtd" */, null);
					xml.WriteStartElement("ini");

					IDictionaryEnumerator enu = hashTbl.GetEnumerator();
					while (enu.MoveNext())
					{
						xml.WriteStartElement("entry");
						xml.WriteElementString("key", (string) enu.Key);
						xml.WriteElementString("value", (string) enu.Value);
						xml.WriteEndElement();	// entry
					}
					xml.WriteEndElement();	// ini
					xml.WriteEndDocument();
					result = true;
				}
				finally
				{
					if (xml != null)
						xml.Close();
				}
			}
			catch
			{
				Logit.WriteError("Can't write to "+fileName);
			}
			return result;
		}

		// Flush the hash table to fName as an XML file ("save as").
		public bool Write(string fName)
		{
			fileName = fName;
			return Write();
		}

		public static string ExecutableName()
		{
			return (System.IO.Path.GetFullPath(Environment.GetCommandLineArgs()[0]));
		}

        public static string MyDocsDir()
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        }

        public static string AppDataDir()
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        }
	}

	/// <summary>
	/// This class extends XmlTextReader to keep track of some the current node
	/// path as the file is read. It also adds a copynode method to make XML
	/// copy with modification operations easier.
	/// 
	/// Only the constructor that takes a file name as a parameter is supported.
	/// (But you could easily extend it if you need to read from something other
	/// than a named text file.)
	/// </summary>
	public class XmlFileReader: XmlTextReader
	{
		protected ArrayList nodePathList;
		protected string nodePathCache;
		protected bool atEmptyElement;
		public string currentElement;

		public XmlFileReader(string fileName): base(fileName)
		{
			nodePathList = new ArrayList(64);
			currentElement = "";
		}

		public override bool Read()
		{
			bool result = base.Read();
			nodePathCache = null;
			if (result)
			{
				if (NodeType == XmlNodeType.Element)
				{
					currentElement = Name;
					atEmptyElement = IsEmptyElement;
					if (!IsEmptyElement)
						nodePathList.Add(Name);
				}
				else if (NodeType == XmlNodeType.EndElement)
				{
					if (nodePathList.Count > 0)
                        nodePathList.RemoveAt(nodePathList.Count-1);
					atEmptyElement = false;
				}

			}
			return result;
		}

		public string NodePath()
		{
			if (nodePathCache != null)
				return nodePathCache;
			int i;
			StringBuilder sb = new StringBuilder(128);

			for (i = 0; i < nodePathList.Count; i++)
			{
				sb.Append("/");
				sb.Append((string) nodePathList[i]);
			}
			if (atEmptyElement)
			{
				sb.Append("/");
				sb.Append(currentElement);
			}
			sb.Append("/");
			nodePathCache = sb.ToString();
			return nodePathCache;
		}

		public bool NodePathContains(string s)
		{
			string np = NodePath();
			return np.IndexOf(s) != -1;
		}

		public void CopyNode(XmlTextWriter xw)
		{
			switch (NodeType)
			{
				case XmlNodeType.Element:
					xw.WriteStartElement(Name);
					xw.WriteAttributes(this, true);
					if (IsEmptyElement)
						xw.WriteEndElement();
					break;
				case XmlNodeType.EndElement:
					xw.WriteEndElement();
					break;
				case XmlNodeType.Text:
					xw.WriteString(Value);
					break;
				case XmlNodeType.SignificantWhitespace:
					xw.WriteWhitespace(Value);
					break;
				case XmlNodeType.Whitespace:
					// You could insert xw.WriteWhitespace(Value); to preserve
					// insignificant white space, but it either adds bloat or
					// messes up formatting.
					break;
				case XmlNodeType.Attribute:
					xw.WriteAttributeString(Name, Value);
					break;
				case XmlNodeType.ProcessingInstruction:
					xw.WriteProcessingInstruction(Name, Value);
					break;
				case XmlNodeType.XmlDeclaration:
					xw.WriteStartDocument(true);
					break;
				default:
					Logit.WriteLine("Doing NOTHING with type="+NodeType.ToString()+" Name="+Name+" Value="+Value); // DEBUG
					break;
			}
		}

	}

	public delegate void StringDelegate(string s);

	/// <summary>
	/// This class provides a way to display results from the command console,
	/// a scrolling list box, or a log file (or any combination of those.
	/// </summary>
	public class Logit
	{
		public static StringDelegate GUIWriteString;
		public static bool useConsole;
//		public static System.Windows.Forms.ListBox lstBox;
		protected static System.IO.StreamWriter sw;
        public static bool loggedError = false;

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
			try
			{
				CloseFile();
				sw = new StreamWriter(fName, false);
				if (useConsole)
					Console.WriteLine("Log file opened: "+fName);
			}
			catch
			{
				WriteLine("Unable to open log file "+fName);
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

    /// <summary>
    /// File-related helper functions.
    /// </summary>
    public class fileHelper
    {
        protected static readonly int CHUNK_SIZE = 1048576;

        /// <summary>
        /// Returns the Unicode file encoding if there are proper byte order marks;
        /// otherwise guesses if this is a UTF-8 (no BOM) file or ANSI file.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static Encoding IdentifyFileCharset(string fileName)
        {
            int i;
            int oddNullCount = 0;   // Bigger on Big Endian UTF-16
            int evenNullCount = 0;  // Bigger on Little Endian UTF-16
            int e2Count = 0;
            bool odd = true;
            Encoding result = Encoding.UTF8;    // Assume innocent until proven guilty.
            try
            {
                FileStream fs;
                fs = new FileStream(fileName, FileMode.Open, FileAccess.Read);
                // use one byte per character encoding for reading dump data
                BinaryReader br = new BinaryReader(fs, new ASCIIEncoding());
                byte[] chunk;

                // read one chunk
                // chunk.Length will be 0 if end of file is reached.
                chunk = br.ReadBytes(CHUNK_SIZE);
                if (chunk.Length > 4)
                {
                    if ((chunk[0] == 0xFF) && (chunk[1] == 0xFE))
                    {
                        result = Encoding.Unicode;
                        if ((chunk[2] == 0) && (chunk[3] == 0))
                            result = Encoding.UTF32;
                    }
                    else if ((chunk[0] == 0xFE) && (chunk[1] == 0xFF))
                    {
                        result = Encoding.BigEndianUnicode;
                    }
                    else if ((chunk[0] == 0xEF) && (chunk[1] == 0xBB) && (chunk[2] == 0xBF))
                    {
                        result = Encoding.UTF8;
                    }
                    else
                    {
                        for (i = 0; i < (chunk.Length - 1); i++)
                        {
                            if (chunk[i] == 0)
                            {
                                if (odd)
                                    oddNullCount++;
                                else
                                    evenNullCount++;
                            }
                            // UTF-8 encoding of any Unicode code point greater than 127 fit the
                            // following pattern for the first 2 of 2, 3, 4, 5, or 6 bytes.
                            // If this pattern isn't followed, it isn't UTF-8. If it is found,
                            // it probably is UTF-8.

                            if ((chunk[i] & 0xE0) == 0xC0)
                            {
                                if ((chunk[i+1] & 0xC0) == 0x80)
                                {
                                    e2Count++;
                                }
                                else
                                {
                                    e2Count = -1000;
                                    i = chunk.Length;
                                }
                            }
                            else if ((i < chunk.Length - 2) && ((chunk[i] & 0xF0) == 0xE0))
                                if (((chunk[i+1] & 0xC0) == 0x80) && ((chunk[i+2] & 0xC0) == 0x80))
                                {
                                    e2Count++;
                                }
                                else
                                {
                                    e2Count = -1000;
                                    i = chunk.Length;
                                }
                            else if ((i < chunk.Length - 3) && ((chunk[i] & 0xF8) == 0xF0))
                                if (((chunk[i + 1] & 0xC0) == 0x80) && ((chunk[i + 2] & 0xC0) == 0x80) &&
                                    ((chunk[i + 3] & 0xC0) == 0x80))
                                {
                                    e2Count++;
                                }
                                else
                                {
                                    e2Count = -1000;
                                    i = chunk.Length;
                                }
                            else if ((i < chunk.Length - 4) && ((chunk[i] & 0xFC) == 0xF8))
                                if (((chunk[i + 1] & 0xC0) == 0x80) && ((chunk[i + 2] & 0xC0) == 0x80) &&
                                    ((chunk[i + 3] & 0xC0) == 0x80) && ((chunk[i + 4] & 0xC0) == 0x80))
                                {
                                    e2Count++;
                                }
                                else
                                {
                                    e2Count = -1000;
                                    i = chunk.Length;
                                }
                            else if ((i < chunk.Length - 5) && ((chunk[i] & 0xFE) == 0xFC))
                                if (((chunk[i + 1] & 0xC0) == 0x80) && ((chunk[i + 2] & 0xC0) == 0x80) &&
                                    ((chunk[i + 3] & 0xC0) == 0x80) && ((chunk[i + 4] & 0xC0) == 0x80) &&
                                    ((chunk[i + 5] & 0xC0) == 0x80))
                                {
                                    e2Count++;
                                }
                                else
                                {
                                    e2Count = -1000;
                                    i = chunk.Length;
                                }
                            odd = !odd;
                        }
                        if (e2Count < 0)
                            result = Encoding.GetEncoding(1252);
                        else if (e2Count > oddNullCount + evenNullCount + 1)
                            result = Encoding.UTF8;
                        else if (oddNullCount > (evenNullCount * 2))
                            result = Encoding.BigEndianUnicode;
                        else if (evenNullCount > (oddNullCount * 2))
                            result = Encoding.Unicode;
                    }
                }
                fs.Close();
            }
            catch (Exception err)
            {
                result = Encoding.UTF8;
                Logit.WriteError("Cannot read " + fileName + "\r\n" + err.Message +
                    "\r\n" + err.StackTrace);
            }
            // Logit.WriteLine("Identified " + fileName + " as " + result.ToString());
            return result;
        }

        public static bool IsNormalWhiteSpace(string s, int index)
        {
            return IsNormalWhiteSpace(s[index]);
        }

        public static bool IsNormalWhiteSpace(char ch)
        {
            return (ch == ' ') || (ch == '\r') || (ch == '\n') || (ch == '\t');
        }

        /// <summary>
        /// Escapes \ ' " CR and LF as \\ \' \" \r and \n, respectively.
        /// </summary>
        /// <param name="s">String to be escaped.</param>
        /// <returns>String with escape substitutions made.</returns>
        public static string sqlString(string s)
        {
            s = s.Replace("\\", "\\\\");
            s = s.Replace("'", "\\'");
            s = s.Replace("\"", "\\\"");
            s = s.Replace("\n", "\\n");
            s = s.Replace("\r", "\\r");
            return s;
        }
        
        /// <summary>
        /// Replaces only the first instance of oldstring in string s with newstring.
        /// (String.Replace replaces ALL instances of the replacement string. This
        /// just does the first.)
        /// </summary>
        /// <param name="s">String on which the replacement is to be made.</param>
        /// <param name="oldstring">String to find and overwrite with newstring</param>
        /// <param name="newstring">New string to replace oldstring</param>
        /// <returns>s, but with the first instance of oldstring replaced with newstring</returns>
        public static string ReplaceFirst(string s, string oldstring, string newstring)
        {
            if ((oldstring == null) || (s == null) || (s == String.Empty))
                return s;
            if (newstring == null)
                newstring = String.Empty;
            if (newstring == null)
                newstring = string.Empty;
            int pos = s.IndexOf(oldstring);
            if (pos < 0)
                return s;
            return s.Substring(0, pos) + newstring + s.Substring(pos + oldstring.Length);
        }

        public static string KhmerDigits(string s)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char ch in s)
            {
                switch (ch)
                {
                    case '0':
                        sb.Append('០');
                        break;
                    case '1':
                        sb.Append('១');
                        break;
                    case '2':
                        sb.Append('២');
                        break;
                    case '3':
                        sb.Append('៣');
                        break;
                    case '4':
                        sb.Append('៤');
                        break;
                    case '5':
                        sb.Append('៥');
                        break;
                    case '6':
                        sb.Append('៦');
                        break;
                    case '7':
                        sb.Append('៧');
                        break;
                    case '8':
                        sb.Append('៨');
                        break;
                    case '9':
                        sb.Append('៩');
                        break;
                    default:
                        sb.Append(ch);
                        break;
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Property that returns the full path to the running executable program.
        /// </summary>
        public static string ExePath
        {
            get
            {
                return Path.GetDirectoryName(System.IO.Path.GetFullPath(Environment.GetCommandLineArgs()[0]));
            }
        }

        /// <summary>
        /// Recursively copies the contents of the source directory to the destination directory.
        /// The immediate parent of the destination directory must exist.
        /// </summary>
        /// <param name="Src">Path to source directory</param>
        /// <param name="Dst">Path to destination directory</param>
        public static void CopyDirectory(string Src, string Dst)
        {
            String[] Files;

            if (Dst[Dst.Length - 1] != Path.DirectorySeparatorChar)
                Dst += Path.DirectorySeparatorChar;
            if (!Directory.Exists(Dst))
                Directory.CreateDirectory(Dst);
            Files = Directory.GetFileSystemEntries(Src);
            foreach (string Element in Files)
            {
                // Sub directories
                if (Directory.Exists(Element))
                    CopyDirectory(Element, Dst + Path.GetFileName(Element));
                else // Files in directory
                    File.Copy(Element, Dst + Path.GetFileName(Element), true);
            }
        }

        /// Create the directory if it does not exist already. Return true if a problem occurs.
        /// </summary>
        /// <param name="destinationPath"></param>
        /// <returns></returns>
        public static bool EnsureDirectory(string destinationPath)
        {
            if (!File.Exists(destinationPath))
            {
                try
                {
                    Directory.CreateDirectory(destinationPath);
                }
                catch (Exception ex)
                {
                    Logit.WriteError(String.Format("Unable to create directory {0}. Details: {1}", destinationPath, ex.Message));
                    return true;
                }
            }
            return false;
        }


        public static void revisePua(string fName)
        {
            // int u;
            int v, i;
            int badChar = 0;
            try
            {
                StreamReader sr = new StreamReader(fName);
                string s;
                s = sr.ReadToEnd();
                sr.Close();
                StringBuilder sb = new StringBuilder(s.Length);
                for (i = 0; i < s.Length; i++)
                {
                    v = (int)s[i];
                    switch (v)
                    {
                        case 0xF134:
                            v = 0x230A;
                            break;
                        case 0xF135:
                            v = 0x230B;
                            break;
                        case 0xF170:
                            v = 0x1DC2;
                            break;
                        case 0xF171:
                            v = 0x1DC4;
                            break;
                        case 0xF172:
                            v = 0x1DC5;
                            break;
                        case 0xF173:
                            v = 0x1DC6;
                            break;
                        case 0xF174:
                            v = 0x1DC7;
                            break;
                        case 0xF175:
                            v = 0x1DC8;
                            break;
                        case 0xF176:
                            v = 0x035C;
                            break;
                        case 0xF177:
                            v = 0x035E;
                            break;
                        case 0xF178:
                            v = 0x1DCA;
                            break;
                        case 0xF179:
                            v = 0x1DC9;
                            break;
                        case 0xF17A:
                            v = 0x0308;
                            break;
                        case 0xF17B:
                            v = 0x1DFD;
                            break;
                        case 0xF180:
                            v = 0x1D50;
                            break;
                        case 0xF181:
                            v = 0x1DAE;
                            break;
                        case 0xF182:
                            v = 0x1D51;
                            break;
                        case 0xF183:
                            v = 0x1D43;
                            break;
                        case 0xF184:
                            v = 0x1D44;
                            break;
                        case 0xF185:
                            v = 0x1D45;
                            break;
                        case 0xF186:
                            v = 0x1D47;
                            break;
                        case 0xF187:
                            v = 0x1D48;
                            break;
                        case 0xF188:
                            v = 0x1D49;
                            break;
                        case 0xF189:
                            v = 0x1D4A;
                            break;
                        case 0xF18A:
                            v = 0x1D4B;
                            break;
                        case 0xF18B:
                            v = 0x1D9F;
                            break;
                        case 0xF18C:
                            v = 0x1D4D;
                            break;
                        case 0xF18D:
                            v = 0x1D4F;
                            break;
                        case 0xF18E:
                            v = 0x1D52;
                            break;
                        case 0xF18F:
                            v = 0x1D53;
                            break;
                        case 0xF190:
                            v = 0x1D56;
                            break;
                        case 0xF191:
                            v = 0x1D57;
                            break;
                        case 0xF192:
                            v = 0x1D58;
                            break;
                        case 0xF193:
                            v = 0x1D5A;
                            break;
                        case 0xF194:
                            v = 0x1D5B;
                            break;
                        case 0xF195:
                            v = 0x02CB;
                            break;
                        case 0xF196:
                            v = 0x02C8;
                            break;
                        case 0xF197:
                            v = 0x02CA;
                            break;
                        case 0xF198:
                            v = 0xA717;
                            break;
                        case 0xF199:
                            v = 0xA718;
                            break;
                        case 0xF19A:
                            v = 0xA719;
                            break;
                        case 0xF19B:
                            v = 0xA71A;
                            break;
                        case 0xF19C:
                            v = 0xA71B;
                            break;
                        case 0xF19D:
                            v = 0xA71C;
                            break;
                        case 0xF19E:
                            v = 0xA71D;
                            break;
                        case 0xF19F:
                            v = 0xA71E;
                            break;
                        case 0xF1A0:
                            v = 0x1D9B;
                            break;
                        case 0xF1A2:
                            v = 0x1D9D;
                            break;
                        case 0xF1A5:
                            v = 0x1DA0;
                            break;
                        case 0xF1A6:
                            v = 0x1DA2;
                            break;
                        case 0xF1A7:
                            v = 0x1DA4;
                            break;
                        case 0xF1A8:
                            v = 0x1DA6;
                            break;
                        case 0xF1A9:
                            v = 0x1DA1;
                            break;
                        case 0xF1AA:
                            v = 0x1DA9;
                            break;
                        case 0xF1AC:
                            v = 0x1DB1;
                            break;
                        case 0xF1AF:
                            v = 0x1DB4;
                            break;
                        case 0xF1B0:
                            v = 0x1DB6;
                            break;
                        case 0xF1B1:
                            v = 0x1DB7;
                            break;
                        case 0xF1B2:
                            v = 0x1DAD;
                            break;
                        case 0xF1B3:
                            v = 0x1DBA;
                            break;
                        case 0xF1B6:
                            v = 0x1DBB;
                            break;
                        case 0xF1B7:
                            v = 0x1DBD;
                            break;
                        case 0xF1B8:
                            v = 0x1DBE;
                            break;
                        case 0xF1B9:
                            v = 0x1D9C;
                            break;
                        case 0xF1BA:
                            v = 0x1D9E;
                            break;
                        case 0xF1BB:
                            v = 0x1DA3;
                            break;
                        case 0xF1BD:
                            v = 0x1DA8;
                            break;
                        case 0xF1BE:
                            v = 0x1DAA;
                            break;
                        case 0xF1BF:
                            v = 0x1DAB;
                            break;
                        case 0xF1C0:
                            v = 0x1DAC;
                            break;
                        case 0xF1C1:
                            v = 0x1DAF;
                            break;
                        case 0xF1C2:
                            v = 0x1DB0;
                            break;
                        case 0xF1C3:
                            v = 0x1DB2;
                            break;
                        case 0xF1C4:
                            v = 0x1DB3;
                            break;
                        case 0xF1C5:
                            v = 0x1DB5;
                            break;
                        case 0xF1C6:
                            v = 0x1DB9;
                            break;
                        case 0xF1C7:
                            v = 0x1DBC;
                            break;
                        case 0xF1C8:
                            v = 0x02C0;
                            break;
                        case 0xF1C9:
                            v = 0x1DBF;
                            break;
                        case 0xF1CA:
                            v = 0x1DA5;
                            break;
                        case 0xF1CB:
                            v = 0x1DA7;
                            break;
                        case 0xF1CC:
                            v = 0x1DB8;
                            break;
                        case 0xF1D0:
                            v = 0xA712;
                            break;
                        case 0xF1D1:
                            v = 0xA713;
                            break;
                        case 0xF1D2:
                            v = 0xA714;
                            break;
                        case 0xF1D3:
                            v = 0xA715;
                            break;
                        case 0xF1D4:
                            v = 0xA716;
                            break;
                        case 0xF1D5:
                            v = 0xA708;
                            break;
                        case 0xF1D6:
                            v = 0xA709;
                            break;
                        case 0xF1D7:
                            v = 0xA70A;
                            break;
                        case 0xF1D8:
                            v = 0xA70B;
                            break;
                        case 0xF1D9:
                            v = 0xA70C;
                            break;
                        case 0xF1DA:
                            v = 0xA70D;
                            break;
                        case 0xF1DB:
                            v = 0xA70E;
                            break;
                        case 0xF1DC:
                            v = 0xA70F;
                            break;
                        case 0xF1DD:
                            v = 0xA710;
                            break;
                        case 0xF1DE:
                            v = 0xA711;
                            break;
                        case 0xF1DF:
                            v = 0xA700;
                            break;
                        case 0xF1E0:
                            v = 0xA702;
                            break;
                        case 0xF1E1:
                            v = 0xA704;
                            break;
                        case 0xF1E2:
                            v = 0xA706;
                            break;
                        case 0xF1E3:
                            v = 0xA701;
                            break;
                        case 0xF1E4:
                            v = 0xA703;
                            break;
                        case 0xF1E5:
                            v = 0xA705;
                            break;
                        case 0xF1E6:
                            v = 0xA707;
                            break;
                        case 0xF1E7:
                            v = 0xA788;
                            break;
                        case 0xF1E8:
                            v = 0x02EC;
                            break;
                        case 0xF1E9:
                            v = 0xA789;
                            break;
                        case 0xF1EA:
                            v = 0xA78A;
                            break;
                        case 0xF200:
                            v = 0x1D00;
                            break;
                        case 0xF201:
                            v = 0x0221;
                            break;
                        case 0xF202:
                            v = 0x1D07;
                            break;
                        case 0xF203:
                            v = 0x0234;
                            break;
                        case 0xF204:
                            v = 0x0235;
                            break;
                        case 0xF205:
                            v = 0x0236;
                            break;
                        case 0xF206:
                            v = 0x02AE;
                            break;
                        case 0xF207:
                            v = 0x02AF;
                            break;
                        case 0xF208:
                            v = 0x2C6D;
                            break;
                        case 0xF209:
                            v = 0x2C70;
                            break;
                        case 0xF20A:
                            v = 0x0243;
                            break;
                        case 0xF20B:
                            v = 0x023C;
                            break;
                        case 0xF20C:
                            v = 0x1D91;
                            break;
                        case 0xF20E:
                            v = 0x2C61;
                            break;
                        case 0xF20F:
                            v = 0x2C60;
                            break;
                        case 0xF210:
                            v = 0x1D7D;
                            break;
                        case 0xF211:
                            v = 0x024B;
                            break;
                        case 0xF212:
                            v = 0x024A;
                            break;
                        case 0xF213:
                            v = 0x024D;
                            break;
                        case 0xF214:
                            v = 0x024C;
                            break;
                        case 0xF215:
                            v = 0x2C64;
                            break;
                        case 0xF216:
                            v = 0x1D98;
                            break;
                        case 0xF217:
                            v = 0x01B7;
                            break;
                        case 0xF218:
                            v = 0x0244;
                            break;
                        case 0xF219:
                            v = 0x0245;
                            break;
                        case 0xF21A:
                            v = 0x2C73;
                            break;
                        case 0xF21B:
                            v = 0x2C72;
                            break;
                        case 0xF21C:
                            v = 0x1D9A;
                            break;
                        case 0xF21D:
                            v = 0xA78C;
                            break;
                        case 0xF21E:
                            v = 0x0242;
                            break;
                        case 0xF21F:
                            v = 0x023D;
                            break;
                        case 0xF220:
                            v = 0x0247;
                            break;
                        case 0xF221:
                            v = 0x0246;
                            break;
                        case 0xF222:
                            v = 0x2C68;
                            break;
                        case 0xF223:
                            v = 0x2C67;
                            break;
                        case 0xF224:
                            v = 0x1D80;
                            break;
                        case 0xF226:
                            v = 0x1D81;
                            break;
                        case 0xF227:
                            v = 0x1D82;
                            break;
                        case 0xF228:
                            v = 0x1D83;
                            break;
                        case 0xF229:
                            v = 0x1D84;
                            break;
                        case 0xF22A:
                            v = 0x1D85;
                            break;
                        case 0xF22B:
                            v = 0x1D86;
                            break;
                        case 0xF22C:
                            v = 0x1D87;
                            break;
                        case 0xF22D:
                            v = 0x1D88;
                            break;
                        case 0xF22E:
                            v = 0x1D89;
                            break;
                        case 0xF22F:
                            v = 0x1D8A;
                            break;
                        case 0xF230:
                            v = 0x1D8B;
                            break;
                        case 0xF231:
                            v = 0x1D8C;
                            break;
                        case 0xF232:
                            v = 0x1D8D;
                            break;
                        case 0xF233:
                            v = 0x1D8E;
                            break;
                        case 0xF236:
                            v = 0x1D8F;
                            break;
                        case 0xF237:
                            v = 0x1D90;
                            break;
                        case 0xF238:
                            v = 0x1D92;
                            break;
                        case 0xF239:
                            v = 0x1D93;
                            break;
                        case 0xF23A:
                            v = 0x1D94;
                            break;
                        case 0xF23B:
                            v = 0x1D95;
                            break;
                        case 0xF23C:
                            v = 0x1D96;
                            break;
                        case 0xF23D:
                            v = 0x1D97;
                            break;
                        case 0xF23E:
                            v = 0x1D99;
                            break;
                        case 0xF23F:
                            v = 0x1D7E;
                            break;
                        case 0xF240:
                            v = 0x0238;
                            break;
                        case 0xF241:
                            v = 0x0239;
                            break;
                        case 0xF242:
                            v = 0x2C62;
                            break;
                        case 0xF243:
                            v = 0x024F;
                            break;
                        case 0xF244:
                            v = 0x024E;
                            break;
                        case 0xF245:
                            v = 0xA72B;
                            break;
                        case 0xF246:
                            v = 0xA72D;
                            break;
                        case 0xF249:
                            v = 0x1D6C;
                            break;
                        case 0xF24A:
                            v = 0x1D6D;
                            break;
                        case 0xF24B:
                            v = 0x1D6E;
                            break;
                        case 0xF24C:
                            v = 0x1D6F;
                            break;
                        case 0xF24D:
                            v = 0x1D70;
                            break;
                        case 0xF24E:
                            v = 0x1D71;
                            break;
                        case 0xF24F:
                            v = 0x1D72;
                            break;
                        case 0xF250:
                            v = 0x1D73;
                            break;
                        case 0xF251:
                            v = 0x1D74;
                            break;
                        case 0xF252:
                            v = 0x1D75;
                            break;
                        case 0xF253:
                            v = 0x1D76;
                            break;
                        case 0xF254:
                            v = 0x1D7B;
                            break;
                        case 0xF255:
                            v = 0x1D7F;
                            break;
                        case 0xF256:
                            v = 0x023F;
                            break;
                        case 0xF257:
                            v = 0x0240;
                            break;
                        case 0xF25A:
                            v = 0xA727;
                            break;
                        case 0xF25B:
                            v = 0x2C6E;
                            break;
                        case 0xF25C:
                            v = 0x2C63;
                            break;
                        case 0xF25D:
                            v = 0x1D7C;
                            break;
                        case 0xF25E:
                            v = 0x2C74;
                            break;
                        case 0xF25F:
                            v = 0x2C71;
                            break;
                        case 0xF260:
                            v = 0x0249;
                            break;
                        case 0xF261:
                            v = 0x0248;
                            break;
                        case 0xF262:
                            v = 0x2C6A;
                            break;
                        case 0xF263:
                            v = 0x2C69;
                            break;
                        case 0xF264:
                            v = 0x2C6C;
                            break;
                        case 0xF265:
                            v = 0x2C6B;
                            break;
                        case 0xF266:
                            v = 0xA78E;
                            break;
                        case 0xF26A:
                            v = 0xA78B;
                            break;
                        case 0xF26B:
                            v = 0xA78D;
                            break;
                        case 0xF320:
                            v = 0x04F6;
                            break;
                        case 0xF321:
                            v = 0x04F7;
                            break;
                        case 0xF322:
                            v = 0x0512;
                            break;
                        case 0xF323:
                            v = 0x0513;
                            break;
                        case 0xF324:
                            v = 0x04FC;
                            break;
                        case 0xF325:
                            v = 0x04FD;
                            break;
                        case 0xF328:
                            v = 0x04FE;
                            break;
                        case 0xF329:
                            v = 0x04FF;
                            break;
                        case 0xF32A:
                            v = 0x0510;
                            break;
                        case 0xF32B:
                            v = 0x0511;
                            break;
                        case 0xF32C:
                            v = 0x0526;
                            break;
                        case 0xF32D:
                            v = 0x0527;
                            break;
                    }
                    sb.Append((char)v);
                    if ((v >= 0xE000) && (v <= 0xF8FF) || (v >= 0xF0000))
                        badChar = v;
                    // if (u != v)
                    //    Logit.WriteLine(fName + ": " + u.ToString("X") + "->" + v.ToString("X"));
                }
                if (badChar > 0)
                    Logit.WriteError(fName + " contains strange character U+" + badChar.ToString("X"));
                StreamWriter sw = new StreamWriter(fName);
                sw.Write(sb.ToString());
                sw.Close();
            }
            catch(Exception ex)
            {
                Logit.WriteError("Unable to correct PUA encoding in "+fName+"\n"+ex.Message);
            }
        }
    }


	/// <summary>
	/// This class provides a consistent way to write USFM files.
	/// </summary>
	class SFWriter
	{
		protected StreamWriter OutputFile;
		protected bool VirtualSpace;
		protected int lineLength;
		protected bool fileIsOpen;
		const int WORDWRAPLENGTH = 79;

		public void Open(string fileName)
		{
			if (fileIsOpen)
				OutputFile.Close();
			OutputFile = new StreamWriter(fileName, false); // Write in UTF-8 encoding without BOM.
			lineLength = 0;
			VirtualSpace = false;
			fileIsOpen = true;
		}

		public SFWriter()
		{
			lineLength = 0;
			VirtualSpace = false;
		}

		public SFWriter(string fileName)
		{
			Open(fileName);
		}

		public void Close()
		{
			OutputFile.Close();
			fileIsOpen = false;
		}

		public bool Opened()
		{
			return fileIsOpen;
		}

		protected void WriteEOL()
		{
			OutputFile.WriteLine();
			lineLength = 0;
			VirtualSpace = false;
		}

		public void WriteSFM(string marker, string level, string content, bool firstcol)
		{
            if (level == "1")
                level = "";
			if (!fileIsOpen)
			{
				Logit.WriteError("Error: attempt to write to closed file.");
				Logit.WriteLine("\\"+marker+level+" "+content);
				return;
			}
			if ((firstcol && (lineLength > 0)) ||
				((lineLength + marker.Length + content.Length) > (WORDWRAPLENGTH - 9)))
			{
				OutputFile.WriteLine("");
				lineLength = 0;
			}
			else if (VirtualSpace)
			{
				OutputFile.Write(" ");
				lineLength += 1;
			}
			OutputFile.Write("\\{0}", marker);
			lineLength += marker.Length + 1;
			if (level != "")
			{
				OutputFile.Write("{0}", level);
				lineLength += level.Length;
			}
			if (content != "")
			{
				OutputFile.Write(" {0}", content);
				lineLength += 1 + content.Length;
			}
			if (!marker.EndsWith("*"))
			{
				VirtualSpace = true;
			}
		}

		public void WriteSFM(string marker, string content, bool firstcol)
		{
			WriteSFM(marker, "", content, firstcol);
		}

		public void WriteSFM(string marker, string content)
		{
			WriteSFM(marker, "", content, true);
		}


		public void WriteSFM(string marker)
		{
			WriteSFM(marker, "", "", true);
		}

		public void WriteString(string s)
		{	// Writes a string out with fixed but loose word wrap that should
			// usually result in lines less than 80 characters in length, in
			// memory of the 80-column Hollerith punch card and the standard
			// DOS terminal width. As this length is rather arbitrary, the
			// constant is hard-coded, but could be made a variable if you
			// like.
			int i, j;
			char[] spaceLf = " \n".ToCharArray();

			if (!fileIsOpen)
			{
				Logit.WriteError("Error: attempt to write to closed file.");
				Logit.WriteLine(s);
				return;
			}

			if (VirtualSpace)
			{
				OutputFile.Write(" ");
				VirtualSpace = false;
				lineLength += 1;
			}
			for (i = 0; i < s.Length; i++)
			{
				if (((s[i] == '\n') || (s[i] == ' ')) && (lineLength >= (WORDWRAPLENGTH / 2)))
				{
					if (s.Length > i)
                        j = s.IndexOfAny(spaceLf, i+1);
					else
						j = -1;
					if ((j > 0) && (lineLength - i + j < WORDWRAPLENGTH))
					{
						OutputFile.Write(" ");
						lineLength++;
					}
					else
					{
						OutputFile.WriteLine();
						lineLength = 0;
					}
				}
				else if (s[i] == '\n')
				{
					OutputFile.Write(' ');
					lineLength++;
				}
				else if (s[i] == '\r')
				{
					// Ignore CR, react to LF
				}
				else
				{
					OutputFile.Write(s[i]);
					lineLength++;
				}
			}
		}
	}


	public struct stringPair
	{
		public string old;
		public string niu;
	}

	public class SubstituteStrings
	{
		protected int numsubs;
		protected ArrayList substList;

		public static bool IsHexCharacter(string s, int index)
		{
            if ((index >= s.Length) || (index < 0))
                return false;
			if (Char.IsNumber(s, index))
				return true;
			int c = (int)s[index];
			if ((c >= (int)'a') && (c <= (int)'f'))
				return true;
			if ((c >= (int)'A') && (c <= (int)'F'))
				return true;
			else
				return false;
		}

		public static string UNameSubst(string s)
		{
			string result = s;
			string hexnum;
			string s2;
			StringBuilder sb;
			int i, j;
			Int64 u = new Int64();
			
			i = result.IndexOf("U+");
			while (i >= 0)
			{
				sb = new StringBuilder(8);
				for (j = i+2; (j < result.Length) && (IsHexCharacter(result, j)) && (sb.Length < 5); j++)
				{
					sb.Append(result[j]);
				}
				hexnum = sb.ToString();
				u = Int64.Parse(hexnum, System.Globalization.NumberStyles.HexNumber);
				s2 = new string( (char)u, 1);
				result = result.Replace("U+" + hexnum, s2);
				i = result.IndexOf("U+");
			}

			return result;
		}

		public SubstituteStrings(string fname)
		{
			stringPair sp = new stringPair();
			substList = new ArrayList(47);
			XmlTextReader xr = null;
			try
			{
				xr = new XmlTextReader(fname);
				xr.WhitespaceHandling = WhitespaceHandling.Significant;
				xr.MoveToContent();
				while (xr.Read())
				{
					if (xr.NodeType == XmlNodeType.Element)
					{
						switch (xr.Name)
						{
							case "changelist":
								break;
							case "comment":
								break;
							case "replace":
								sp = new stringPair();
								break;
							case "old":
								xr.Read();
								sp.old = UNameSubst(xr.Value);
								break;
							case "new":
								xr.Read();
								sp.niu = UNameSubst(xr.Value);
								substList.Add(sp);
								break;
							default:
								xr.Read();
								break;
						}
					}
				}
			}
			finally
			{
				if (xr != null)
					xr.Close();
			}
		}

		public string Substitute(string s)
		{
			stringPair sp;
			string result = s;
			foreach (object o in substList)
			{
				sp = (stringPair)o;
				result = result.Replace(sp.old, sp.niu);
			}
			return result;
		}
	}

	public class CrossReference
	{
		protected int numbks;
		protected Hashtable xref;

		public CrossReference()
		{
			numbks = 0;
			xref = new Hashtable(7);
		}

		public CrossReference(string fname)
		{
			int i;
			string[] src = new string[90];
			string[] tgt = new string[90];
			numbks = 0;
			string at = "";
			string note;
			string s;
			xref = new Hashtable(397);
			XmlTextReader xr = null;
			try
			{
				xr = new XmlTextReader(fname);
				xr.WhitespaceHandling = WhitespaceHandling.Significant;
				xr.MoveToContent();
				while (xr.Read())
				{
					if (xr.NodeType == XmlNodeType.Element)
					{
						switch (xr.Name)
						{
							case "xlat":
								break;
							case "source":
								xr.Read();
								src[numbks] = xr.Value;
								break;
							case "target":
								xr.Read();
								tgt[numbks] = xr.Value;
								numbks++;
								break;
							case "xref":
								break;
							case "at":
								xr.Read();
								at = xr.Value;
								break;
							case "note":
								xr.Read();
								note = xr.Value;
								try
								{
									if (note != null)
									{
										for (i = 0; i < numbks; i++)
											note = note.Replace(src[i], tgt[i]);
										s = (string)xref[at];
										if (s == null)
                                 xref.Add(at, note);
										else
											xref[at] = s+"; "+note;
									}
								}
								catch (System.Exception ex)
								{
									Logit.WriteError(ex.ToString());
									Logit.WriteLine("Error found in line "+xr.LineNumber.ToString()+" of "+fname);
								}
								break;
							default:
								xr.Read();
								break;
						}
					}
				}
			}
			finally
			{
				if (xr != null)
					xr.Close();
			}
			Logit.WriteLine(numbks.ToString()+" book name translations and "+xref.Count.ToString()+" crossreference notes read.");
		}

		public string find(string place)
		{
			return (string)xref[place];
		}
	}
	
	public class FootNoteCaller
	{
		protected int index;
		protected ArrayList markers;

		public FootNoteCaller()
		{
			index = 0;
			markers = new ArrayList();
			markers.Add((object)"*");
			markers.Add((object)"†");
			markers.Add((object)"‡");
		}

		// marks = space separated list of markers, like "* † ‡" 
		public FootNoteCaller(string marks)
		{
			index = 0;
			markers = new ArrayList();
			if ((marks == null) || (marks == ""))
			{
				markers.Add((object)"\u200b");	// zero-width space
			}
			else
			{
				StringBuilder sb = new StringBuilder();
				int i;
				for (i = 0; i < marks.Length; i++)
				{
                    if (fileHelper.IsNormalWhiteSpace(marks, i))
					{
						if (sb.Length > 0)
						{
							markers.Add(sb.ToString());
							sb.Length = 0;
						}
					}
					else
					{
						sb.Append(marks[i]);
					}
				}
				if (sb.Length > 0)
				{
					markers.Add(sb.ToString());
					sb.Length = 0;
				}
			}
		}

		public string Marker()
		{
			string Result = (string)markers[index];
			index++;
			if (index >= markers.Count)
				index = 0;
			return Result;
		}

		public void reset()
		{
			index = 0;
		}
	}

	public class TagRecord
	{
		public string tag;
		public string endTag;
		public string kind;
		public string paragraphStyle;
		public string characterStyle;
		public string parameterName;
		public string contentName;
		public bool nestingAllowed;
		public bool levelExpected;
		public string usfx;
		public string usfxAttribute;
		public string TeXcode;
		public string comment;

		public TagRecord()
		{
			tag = endTag = paragraphStyle = characterStyle = parameterName =
				contentName = TeXcode = comment = "";
		}

		public bool hasEndTag()
		{
			return (endTag.IndexOf('*') >= 0);
		}

		public string noSpaces(string s)
		{
			int i = s.IndexOf(' ');
			if ((i < 0) || (s.Length < 1))
				return s;
			return (noSpaces(s.Remove(i, 1)));
		}

		public string paragraphStyleID()
		{
			return noSpaces(paragraphStyle);
		}

		public string paragraphStyleID(int level)
		{
			if (levelExpected)
				return noSpaces(paragraphStyle+level.ToString());
			return noSpaces(paragraphStyle);
		}
		
		public string characterStyleID()
		{
			return noSpaces(characterStyle);
		}
	}

	public class TagInfo
	{
		public Hashtable tags;
		public bool inCanon;

		public TagRecord info(string id)
		{
			TagRecord result = (TagRecord)tags[id];
			if (result == null)
				result = new TagRecord();
			return result;
		}

		public void ReadTagInfo(string fileName)
		{
			TagRecord tagRec = null;
			int i;
			tags = new Hashtable(251);
			XmlTextReader xr = new XmlTextReader(fileName);
			xr.WhitespaceHandling = WhitespaceHandling.Significant;
			while (xr.Read())
			{
				if (xr.NodeType == XmlNodeType.Element)
				{
					switch (xr.Name)
					{
						case "sfm":
							tagRec = new TagRecord();
							for (i = 0; i < xr.AttributeCount; i++)
							{
								xr.MoveToAttribute(i);
								if (xr.Name == "kind")
								{
									tagRec.kind = xr.Value;
								}
							}
							xr.MoveToElement();
							break;
						case "tag":
							xr.Read();
							tagRec.tag = xr.Value;
							break;
						case "endTag":
							xr.Read();
							tagRec.endTag = xr.Value;
							break;
						case "paragraphStyle":
							xr.Read();
							tagRec.paragraphStyle = xr.Value;
							break;
						case "characterStyle":
							xr.Read();
							tagRec.characterStyle = xr.Value;
							break;
						case "parameterName":
							xr.Read();
							tagRec.parameterName = xr.Value;
							break;
						case "contentName":
							xr.Read();
							tagRec.contentName = xr.Value;
							break;
						case "nestingAllowed":
							xr.Read();
							tagRec.nestingAllowed = Convert.ToBoolean(xr.Value);
							break;
						case "levelExpected":
							xr.Read();
							tagRec.levelExpected = Convert.ToBoolean(xr.Value);
							break;
						case "usfx":
							xr.Read();
							tagRec.usfx = xr.Value;
							break;
						case "usfxAttribute":
							xr.Read();
							tagRec.usfxAttribute = xr.Value;
							break;
						case "TeXcode":
							xr.Read();
							tagRec.TeXcode = xr.Value;
							break;
						case "comment":
							xr.Read();
							tagRec.comment = xr.Value;
							break;
					}
				}
				else if (xr.NodeType == XmlNodeType.EndElement)
				{
					if (xr.Name == "sfm")
					{
						tags[tagRec.tag] = tagRec;
						if ((tagRec.endTag != "") && (tagRec.endTag.StartsWith(tagRec.tag)))
						{
							tags[tagRec.tag + "*"] = tagRec;
						}
					}
				}
			}
			xr.Close();
		}

		public TagInfo(string fileName)
		{
			ReadTagInfo(fileName);
		}

		public TagInfo()
		{
			ReadTagInfo(SFConverter.FindAuxFile("SFMInfo.xml"));
		}
	}

	public class BibleBookRecord
	{
		public int sortOrder;
		public int numChapters;
		public int[] verseCount;
        public string tla;
		public string osisName;
		public string name;
		public string shortName;
		public string vernacularHeader;
		public string vernacularName;
        public string vernacularAbbreviation;
		public string testament;
        public StringBuilder toc;
		public BibleBookRecord()
		{
			tla = osisName = name = shortName = testament = vernacularAbbreviation = vernacularHeader = vernacularName = "";
            toc = new StringBuilder();
		}
	}

	public class BibleBookInfo
	{
		public const int MAXNUMBOOKS=101;	// Includes Apocrypha + extrabiblical helps, front & back matter, etc.
		public Hashtable books;
		public BibleBookRecord[] bookArray = new BibleBookRecord[MAXNUMBOOKS];
		protected bool apocryphaFound;

        public bool isApocrypha(string abbrev)
        {
            BibleBookRecord br = (BibleBookRecord)books[abbrev];
            if (br == null)
                return false;
            return br.testament == "a";
        }

        public bool isPeripheral(string abbrev)
        {
            BibleBookRecord br = (BibleBookRecord)books[abbrev];
            if (br == null)
                return false;
            return br.testament == "x";
        }

        public int Order(string abbrev)
		{
			BibleBookRecord br = (BibleBookRecord)books[abbrev];
			if (br == null)
				return 0;
			return br.sortOrder;
		}

		public string FilePrefix(string abbrev)
		{
			BibleBookRecord br = (BibleBookRecord)books[abbrev];
			if (br == null)
				return "00-"+abbrev;
			int num = br.sortOrder;
			if (num < 40)
			{
				apocryphaFound = false;
				return num.ToString("d2")+"-"+abbrev;
			}
			if (num < 64)
			{
				apocryphaFound = true;
			}
			else
			{
				if (!apocryphaFound)
					num -= 24;
			}
			return num.ToString("d2")+"-"+abbrev;
		}

		public string OsisID(string abbrev)
		{
			BibleBookRecord br = (BibleBookRecord)books[abbrev];
			if (br == null)
				return "";
			return br.osisName;
		}

		public BibleBookRecord BkRec(string abbrev)
		{
			return (BibleBookRecord)books[abbrev];
		}

		public void ReadBookInfoFile(string fileName)
		{
			int i;
			books = new Hashtable(197);
			BibleBookRecord bkRecord = null; 
			XmlTextReader xr = new XmlTextReader(fileName);
			xr.WhitespaceHandling = WhitespaceHandling.Significant;
			while (xr.Read())
			{
				if (xr.NodeType == XmlNodeType.Element)
				{
					switch (xr.Name)
					{
						case "Book":
							bkRecord = new BibleBookRecord();
							for (i = 0; i < xr.AttributeCount; i++)
							{
								xr.MoveToAttribute(i);
								if (xr.Name == "testament")
								{
									bkRecord.testament = xr.Value;
								}
							}
							xr.MoveToElement();
							break;
						case "sfmTla":
							xr.Read();
							bkRecord.tla = xr.Value;
							break;
						case "osisName":
							xr.Read();
							bkRecord.osisName = xr.Value;
							break;
						case "name":
							xr.Read();
							bkRecord.name = xr.Value;
							break;
						case "shortName":
							xr.Read();
							bkRecord.shortName = xr.Value;
							break;
						case "sortOrder":
							xr.Read();
							bkRecord.sortOrder = Convert.ToInt32(xr.Value);
							break;
						case "numChapters":
							xr.Read();
							bkRecord.numChapters = Convert.ToInt32(xr.Value);
							bkRecord.verseCount = new int[bkRecord.numChapters + 1];
                            if ((bkRecord.sortOrder < 0) || (bkRecord.sortOrder >= BibleBookInfo.MAXNUMBOOKS))
							{
								Logit.WriteError("ERROR: bad sort order number:"+bkRecord.sortOrder.ToString());
								bkRecord.sortOrder = 0;
							}
							break;
					}
				}
				else if (xr.NodeType == XmlNodeType.EndElement)
				{
					if (xr.Name == "Book")
					{
						books[bkRecord.tla] = bkRecord;
						bookArray[bkRecord.sortOrder] = bkRecord;
					}
				}
			}
			xr.Close();
		}
		
		public BibleBookInfo(string fileName)
		{
			ReadBookInfoFile(fileName);
		}

		public BibleBookInfo()
		{
			ReadBookInfoFile(SFConverter.FindAuxFile("BibleBookInfo.xml"));
		}
	}

	public enum sfmType
	{
		textonly,	// just text
		character,	// styled characters (i. e. \nd Yahweh\nd*)
		paragraph,	// styled paragraph (i. e. \p, \q1, \q2, \mt)
		reference,	// chapter and verse markers
		note,		// footnote, endnote, crossreference
		book,		// book of the Bible
		peripherals,	// Preface, helps, etc.
		meta		// metadata (\id, \ide, \h
	}

	public class SfmObject
	{
		public string tag;	// Opening sfm tag without the \.
		public int level;	// Value of number attached to tag without space (i. e. 2 in \q2)
		public string attribute; // i. e. chapter or verse number, footnote marker
		public string text;	// Text up to the next \.
		public bool isEndTag;	// True for explicit end tags (tag ends in *)
		public TagRecord info;  // Information about this particular tag

        // The following state variables are used for input context validation on read.
        private static string prevTag = String.Empty;
        private static string expectedEndTag = String.Empty;
        private static string tagToTerminate = String.Empty;
        private static string currentBook = String.Empty;
        private static string currentChapter = String.Empty;
        private static string currentVerse = String.Empty;

		public SfmObject()
		{
			tag = attribute = text = String.Empty;
            tagToTerminate = String.Empty;
            // currentBook = currentChapter = currentVerse = String.Empty;
		}

        private void validate()
        {
            bool endFound = false;

            if (tag.Length < 1)
                return;  // Nothing to do.

            // Track where we are.
            if ((info.kind != null) && (info.kind.CompareTo("meta") == 0))
            {
                if (tag.CompareTo("id") == 0)
                {
                    currentBook = attribute;
                    currentChapter = "0";
                    currentVerse = "0";
                }
                else if (tag.CompareTo("c") == 0)
                {
                    currentChapter = attribute;
                    currentVerse = "0";
                }
                else if (tag.CompareTo("v") == 0)
                    currentVerse = attribute;
            }

            // Check to see if we have properly ended a tag range.
            if ((expectedEndTag.Length > 0) && (tagToTerminate.Length > 0))
            {
                if ((isEndTag && (tag.CompareTo(tagToTerminate) == 0)) || (expectedEndTag.Contains(tag)))
                {
                    tagToTerminate = expectedEndTag = String.Empty;
                    endFound = true;
                    // Logit.WriteLine(tagToTerminate + " properly terminated with " + tag + "*");
                }
                else if ((info.kind != null) && (info.kind.CompareTo("meta") == 0) && (expectedEndTag.Length > 0) && (info.tag.Length > 0))
                {
                    Logit.WriteError("UNTERMINATED TAG: " + tagToTerminate + " before " +
                        currentBook + " " + currentChapter + ":" + currentVerse);
                }
            }


            // Set up for next check
            prevTag = tag;
            if ((!endFound) && (!isEndTag) && (info.endTag.Length > 0) && (info.kind.CompareTo("note") == 0))
            {
                tagToTerminate = tag;
                expectedEndTag = info.endTag;
            }
        }

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Read one SFM & its immediate associated data.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public bool Read(StreamReader sr, string fileName)
		{
			StringBuilder sb = new StringBuilder();
			int ch;
			int lookAhead;
            // int barCount = 0;
			bool isEndTag = false;
			// Skip to \, parse parts up to but not including next \ or EOF.
			do
			{
				ch = sr.Read();
			} while (((char)ch != '\\') && (ch != -1));
			if (ch == -1)	// eof
				return false;	// Failed to find \.
			// Found \. Copy characters to tag until white space or anything after * or anything before \ or digit or end of file.
			do
			{
				ch = sr.Read();
                lookAhead = sr.Peek();
                if ((ch != -1) && (!fileHelper.IsNormalWhiteSpace((char)ch)) && (!Char.IsDigit((char)ch)))
				{
					sb.Append((char)ch);
				}
				if ('*' == (char)ch)
				{
					isEndTag = true;
				}
			} while ((ch != -1) && (!fileHelper.IsNormalWhiteSpace((char)ch)) &&
				(!isEndTag) && !Char.IsDigit((char)ch) && (char)lookAhead != '\\');
			tag = sb.ToString();
			info = SFConverter.scripture.tags.info(tag);
			// Special logic to disambiguate \k # in helps vs \k...\k* in canon.
			if ((tag == "k") && !SFConverter.scripture.tags.inCanon)
				info = SFConverter.scripture.tags.info("keyword");
			if ((info == null) || (info.tag == ""))
			{
                Logit.WriteError("ERROR! Unrecognized marker in " + fileName + 
                    ": [\\" + tag + "] after " + currentBook + " " + currentChapter +
                    ":" + currentVerse);
			}
			sb.Length = 0;
			level = 0;
			if (isEndTag)
			{
				lookAhead = sr.Peek();
                if ((lookAhead == -1) || ((char)lookAhead == '\\'))
                {
                    text = attribute = "";
                    return true;
                }
                else
                {
                    ch = sr.Read();
                    lookAhead = sr.Peek();
                }
			}
			else // Not an end tag (not terminated in *)
			{
                level = 1;
				if (Char.IsDigit((char)ch))
				{
					while (Char.IsDigit((char)ch))
					{
						sb.Append((char)ch);
                        if ((char)lookAhead != '\\')
                        {
                            ch = sr.Read();
                            lookAhead = sr.Peek();
                        }
                        else
                        {
                            // Early termination of tag by starting another tag; pretend it was properly terminated by a space.
                            ch = (int)' ';
                        }
					}
					level = Convert.ToInt32(sb.ToString());
				}

                // At this point, ch should be the white space or * after the tag and level.

				sb.Length = 0;
				if (info.parameterName != "")
				{	// Tag requires an attribute (Chapter, Verse, footnote marker, book code)
                    if ((char)lookAhead != '\\')
                    {
                        ch = sr.Read();	// Read whatever followed the single white space.
                        lookAhead = sr.Peek();
                    }
					// Allow multiple white spaces before the attribute (like line end between \f and +)
					while (fileHelper.IsNormalWhiteSpace((char)ch) && (ch != -1) && (lookAhead != '\\'))
					{
						ch = sr.Read();
						lookAhead = sr.Peek();
					}
                    if ((ch == -1) || (fileHelper.IsNormalWhiteSpace((char)ch)))
                    {
                        Logit.WriteError("Error in USFM syntax: missing attribute after \\" + tag +
                            " in " + fileName +
                            " after " + currentBook + " " + currentChapter +
                            ":" + currentVerse);
                    }
                    // The first non-whitespace after the tag starts the parameter, like the verse number or footnote marker.
					while ((ch != -1) && (!fileHelper.IsNormalWhiteSpace((char)ch)) && ((char)lookAhead != '\\'))
					{
						sb.Append((char)ch);
						ch = sr.Read();
						lookAhead = sr.Peek();
					}
					attribute = sb.ToString();
					sb.Length = 0;
				}
                // Eat the space that serves only to terminate a tag. This is very important with some nonroman scripts.
                if ((!isEndTag) && (fileHelper.IsNormalWhiteSpace((char)ch)) && ((char)lookAhead != '\\'))
                {
                    ch = sr.Read();
                    lookAhead = sr.Peek();
                }
				// Collapse all contiguous white space characters after a tag to one space (i.e. space CR LF to space)
				while ((ch != -1) && fileHelper.IsNormalWhiteSpace((char)ch) && fileHelper.IsNormalWhiteSpace((char)lookAhead))
				{
					ch = sr.Read();
					lookAhead = sr.Peek();
                    // No need to bail out here due to lookahead check to make sure we don't kill the only significant space
				}
			}   // End of non-end tag case.
			// Read text up to next \, collapsing all contiguous white space to a single space.
			bool inSpace = false;
			lookAhead = sr.Peek();
            // Special case to skip over \ characters embedded in filename a \fig ...\fig* sequence
            // \ may occur in a filename between the 1st and 2nd | of 6 in a figure specification.
            // However, markup of actual texts often get the | syntax wrong, and we often just strip out
            // fig tags, anyway, so we are omitting this check for now. This means that we aren't supporting
            // the \ character in file names. The forward slash, /, however, is OK. Best practice: just put in
            // the file name and no path in the figure specification, or better yet, use an external figure list
            // that could be used with multiple sources.
            /*
            if (tag == "fig")
            {
                if ((sb.ToString().EndsWith("\fi") && (ch == 'g') && (lookAhead == '*')))
                {
                    barCount += 7;
                    Logit.WriteError("Bad figure markup | count " + sb.ToString() + "g*" + " near " +
                        currentBook + " " + currentChapter + ":" + currentVerse);
                }
                while ((ch != -1) && ((ch != '\\') || (barCount < 2)))
                {
                    if (fileHelper.IsNormalWhiteSpace((char)ch))
                    {
                        if (!inSpace)
                        {
                            inSpace = true;
                            sb.Append(' ');
                        }
                    }
                    else
                    {
                        sb.Append((char)ch);
                        inSpace = false;
                        if (ch == '|')
                            barCount++;
                    }
                    ch = sr.Read();
                }
            }
             */

            if (fileHelper.IsNormalWhiteSpace((char)ch))
            {   // Any run of contiguous normal white space (tab, space, CR, LF) is replaced with
                // just one space. If spaces must be preserved, use nonbreaking spaces.
                if (!inSpace)
                {
                    inSpace = true;
                    sb.Append(' ');
                }
            }
            else if (ch != -1)
            {   // Nonspaces are appended to the text run as is, up to but not including the next \.
                sb.Append((char)ch);
                inSpace = false;
            }
            while ((ch != -1) && (lookAhead != -1) && (lookAhead != '\\'))
            {
                ch = sr.Read();
                lookAhead = sr.Peek();
                if (fileHelper.IsNormalWhiteSpace((char)ch))
				{   // Any run of contiguous normal white space (tab, space, CR, LF) is replaced with
                    // just one space. If spaces must be preserved, use nonbreaking spaces.
					if (!inSpace)
					{
						inSpace = true;
						sb.Append(' ');
					}
				}
				else
				{   // Nonspaces are appended to the text run as is, up to but not including the next \.
					sb.Append((char)ch);
					inSpace = false;
				}
            } 

            // USFM officially has two markup items that break the \tag and \tag ...\tag* pattern:
            // ~ (non-breaking space) and // (discretional line break). We convert the former here.
            // The later is left for the publication or conversion process to deal with.
            // Unofficially, << < > >> convert to “ ‘ ’ ”. That is left to the publication process
            // as well.
			text = sb.ToString().Replace('~', (char)(0x00A0) /*non-breaking space*/);
			if (tag == "id")
			{
				if ((attribute.StartsWith("BAK")) || (attribute.StartsWith("OTH")) || (attribute.StartsWith("FRT")) ||
					(attribute.StartsWith("bak")) || (attribute.StartsWith("oth")) || (attribute.StartsWith("frt")))
				{
					SFConverter.scripture.tags.inCanon = false;
				}
				else
				{
					SFConverter.scripture.tags.inCanon = true;
				}
			}
            validate();
			return true;
		}
	}

	public class Footnote: SfmObject
	{
		public string marker;	// + for auto or supplied in parameter
		public string referencedCV;	// Chapter & verse in cc.vv format
		public string keywords;	// Keyword(s) footnote is for
		// Note: \fdc ...\fdc* is counted as styled text.
		public Footnote(): base()
		{
			marker = referencedCV = keywords = "";
		}
	}

	public class BibleBook
	{
		public string bookCode;			// Three letter abbreviation for book (English)
		public int sortKey;				// Number in canonical order
		public ArrayList chapterList;	// List of ArrayLists containing SfmObjects in chapter. Objects before \c go in chapter 0.
		public string vernacularHeader;
		public string vernacularName;
		protected int sfmIndex;
		protected int chapterIndex;

		public BibleBook(): base()
		{
			bookCode = "";
			sortKey = 0;
			chapterList = new ArrayList();
			chapterList.Add(new ArrayList());
			vernacularHeader = "";
			vernacularName = "";
			sfmIndex = chapterIndex = 0;
		}

		public void AddSfm(SfmObject sfm)
		{
			if (sfm.tag == "c")
			{
				try
				{
					chapterIndex = Int32.Parse(sfm.attribute);
				}
				catch
				{
					chapterIndex = 0;
				}
			}
            else if (sfm.tag == "id")
            {
                chapterIndex = 0;
            }
            while (chapterList.Count <= chapterIndex)
            {
                chapterList.Add(new ArrayList());
            }
            ArrayList sfms = (ArrayList)chapterList[chapterIndex];
			sfms.Add(sfm);
		}

		public SfmObject NextSfm()
		{
			ArrayList sfmList;
			if (chapterIndex >= chapterList.Count)
				return null;
			sfmList = (ArrayList)chapterList[chapterIndex];
			if (sfmList == null)
				return null;
			if (sfmIndex < sfmList.Count)
			{
				return (SfmObject)sfmList[sfmIndex++];
			}
			else
			{
				sfmIndex = 0;
				chapterIndex++;
				return NextSfm();
			}
		}

		public SfmObject FirstSfm()
		{
			sfmIndex = chapterIndex = 0;
			return NextSfm();
		}
	}

	public class LengthString
	{
		protected string rawText;
		protected double v, toTwips;
		protected int twipsCount;
		protected StringBuilder sb;
		protected int i;
		public bool Valid;

		public int Twips
		{
			get
			{
				return twipsCount;
			}
			set
			{
				twipsCount = value;
				rawText = twipsCount.ToString()+" twips";
			}
		}

		public int HalfPoints
		{
			get
			{
				return twipsCount/10;
			}
			set
			{
				twipsCount = value * 10;
				v = twipsCount / 20.0;
				rawText = v.ToString("f1") + " pt";
			}
		}

		public double Points
		{
			get
			{
				return twipsCount/20.0;
			}
			set
			{
				twipsCount = (int)Math.Round(value * 20.0);
				rawText = value.ToString("f1")+" pt";
			}
		}

		public string PointString
		{
			get
			{
				double pts = twipsCount/20.0;
				return pts.ToString("f2")+"pt";
			}
			set
			{
				Set(value, 0.0, 'p');
			}
		}

		public double Millimeters
		{
			get
			{
				return twipsCount / 56.6929134;
			}
			set
			{
				twipsCount = (int)Math.Round(56.6929134 * value);
				rawText = value.ToString("f2") + " mm";
			}
		}

		public double Inches
		{
			get
			{
				return twipsCount * 1440.0;
			}
			set
			{
				twipsCount = (int)Math.Round(1440.0 * value);
				rawText = value.ToString("f3") + " in";
			}
		}

		public LengthString(string text, double defaultValue, char defaultUnits)
		{
			Set(text, defaultValue, defaultUnits);
		}

		public LengthString(string text)
		{
			Set(text, 0.0, 't');
		}

		public LengthString(int numTwips)
		{
			Twips = numTwips;
		}

		public LengthString()
		{
			Set("0 t", 0.0, 't');
		}

		public void Set(string text, double defaultValue, char defaultUnits)
		{
			toTwips = 1.0;
			switch (defaultUnits)
			{
				case 'i':	// Inches
					toTwips = 1440.0;
					break;
				case 'm':	// mm
					toTwips = 56.6929134;
					break;
				case 'p':	// points
					toTwips = 20.0;
					break;
				case 't': // twips
					toTwips = 1.0;
					break;
				case 'h':	// half-points
					toTwips = 10.0;
					break;
			}
			try
			{
				rawText = text.Trim();
				sb = new StringBuilder();
				for (i = 0; (i < rawText.Length) && (Char.IsNumber(rawText, i) || (rawText[i] == '-')); i++)
					sb.Append(rawText[i]);
				v = Double.Parse(sb.ToString());
				while ((i < rawText.Length) && !Char.IsLetter(rawText, i))
					i++;
				if (i < rawText.Length)
				{
					switch (Char.ToLower(rawText[i]))
					{
						case 'i':	// Inches
							toTwips = 1440.0;
							break;
						case 'm':	// mm
							toTwips = 56.6929134;
							break;
						case 'p':	// points
							toTwips = 20.0;
							break;
						case 't': // twips
							toTwips = 1.0;
							break;
						case 'h':	// half-points
							toTwips = 10.0;
							break;
					}
				}
				twipsCount = (int)Math.Round(v * toTwips);
			}
			catch
			{
				Set(defaultValue.ToString("f3")+defaultUnits, defaultValue, defaultUnits);
			}
			Valid = true;
		}

		public string Text
		{
			get
			{
				return rawText;
			}
			set
			{
				Set(value, 0.0, 't');
			}
		}
	}

	public delegate void ThunkDelegate();

	// This class models the state of Scripture text attributes that apply to the
	// current text. It is designed to be maximally generous with its
	// interpretation of inbound text, but designed to be used to produce
	// unambiguous output in various formats. It handles overlaps like
	// \it italic \bd bold italic\it* bold\bd*, although markup like that
	// is not best practice, and we do not generate non-nested markup for
	// the benefit of more simplistic conversions to XML.
	public class TextAttributeTracker
	{
		const int STACKSIZE = 40;
		// The following two variables are very weakly encapsulated for
		// convenience. Methods outside of this class should not alter
		// these, but only refer to them.
		public string[] styleNameArray;
		public int stackPointer;
		public bool forbidStackedStyles;
		public StringDelegate startScriptureTextAttribute;
		public StringDelegate stopScriptureTextAttribute;
		// fixNesting is called to allow action to nest styles XML-style
		// when the styles are not properly nested on the input.
		public ThunkDelegate fixNesting;

		public TextAttributeTracker()
		{
			styleNameArray = new string[STACKSIZE];
			stackPointer = -1;	// Empty list
			forbidStackedStyles = false;
		}

		public bool RemoveStyle(string s)
		{	// Returns true if s was removed.
			int i, j;
			bool result = false;
			if (stackPointer < 0)
				return false;
			if (styleNameArray[stackPointer] == s)
			{
				stackPointer--;
				return true;
			}
			for (i = 0; i <= stackPointer; i++)
			{
				if (styleNameArray[i] == s)
				{
					for (j = i + 1; j <= stackPointer; j++)
						styleNameArray[j-1] = styleNameArray[j];
					stackPointer--;
					result = true;
					if (fixNesting != null)
						fixNesting();
				}
			}
			return result;
		}

		public bool IsActive(string s)
		{
			int i;
			for (i = 0; i <= stackPointer; i++)
			{
				if (styleNameArray[i] == s)
					return true;
			}
			return false;
		}

		public bool ReplaceStyle(string s)
		{
			bool result = RemoveStyle(s);
			if (stackPointer < 0)
				stackPointer = 0;
			styleNameArray[stackPointer] = s;
			return result;
		}

		public void CancelStyles()
		{
			stackPointer = -1;
		}

		public bool PushStyle(string s)
		{
			bool result = RemoveStyle(s);
			if (forbidStackedStyles)
			{
				ReplaceStyle(s);
			}
			else
			{
				stackPointer++;
				if (stackPointer < STACKSIZE)
					styleNameArray[stackPointer] = s;
				else
				{
					stackPointer = STACKSIZE - 1;
					Logit.WriteError("WARNING: Too many stacked styles! Reverting to flat mode!");
					forbidStackedStyles = true;
					result = true;
				}
			}
			return result;
		}

		public string PeekStyle()
		{
			if (stackPointer < 0)
				return "";
			else
				return styleNameArray[stackPointer];
		}

		public string PopStyle()
		{
			string result = "";
			if (stackPointer >= 0)
			{
				result = styleNameArray[stackPointer];
				stackPointer--;
			}
			return result;
		}
	}

    public class Figure
    {
        // \fig Fox over hole|BK00051b.tif|col|MAT 8|Horace Knowles; The British & Foreign Bible Society|Aromong be inoon wu kaye̱e̱r ribong|8.20\fig*
        // \fig description|catalog or file name|size col|MAT 8|Horace Knowles; The British & Foreign Bible Society|Aromong be inoon wu kaye̱e̱r ribong|8.20\fig*
        public string description;
        public string catalog;
        public string size;
        public string location;
        public string copyright;
        public string caption;
        public string reference;

        protected int fieldStart;
        protected int fieldEnd;

        protected string firstField(string s)
        {
            fieldStart = 0;
            fieldEnd = s.IndexOf('|');
            if (fieldEnd == -1)
                fieldEnd = s.Length;
            int len = fieldEnd - fieldStart;
            if ((len < 1) || (fieldStart + len > s.Length))
                return "";
            return s.Substring(fieldStart, len).Trim();
        }

        protected string nextField(string s)
        {
            fieldStart = fieldEnd + 1;
            if (fieldStart >= s.Length)
                fieldStart = s.Length - 1;
            fieldEnd = s.IndexOf('|', fieldStart);
            if (fieldEnd == -1)
                fieldEnd = s.Length;
            int len = fieldEnd - fieldStart;
            if ((len < 1) || (fieldStart + len > s.Length))
                return "";
            return s.Substring(fieldStart, len).Trim();
        }

        public void ParseFields(string s)
        {
            description = catalog = size = location = caption = reference = "";
            s = s.Trim();
            description = firstField(s);
            catalog = nextField(s);
            size = nextField(s);
            location = nextField(s);
            copyright = nextField(s);
            caption = nextField(s);
            reference = nextField(s);
        }

        public string figSpec
        {
            get
            {
                return description + "|" + catalog + "|" + size + "|" + location +
                    "|" + copyright + "|" + caption + "|" + reference;
            }
            set
            {
                ParseFields(value);
            }
        }
        public Figure(string figSpec)
        {
            ParseFields(figSpec);
        }

        public Figure()
        {
            description = catalog = size = location = caption = reference = "";
        }
    }

	public class Scriptures
	{
		protected string chapterMark;	// Chapter number or letter
		protected string verseMark;	// Verse number or range, i. e. 6-7
		protected string currentParagraphStyle;
		protected string headerName;
		protected string activeRunStyle;
		protected TextAttributeTracker cStyles;
		protected int usfxStyleCount;
		protected int currentChapter;
		protected BibleBook[] books;
		protected BibleBook book;
		protected BibleBookRecord bkRec;
		protected TagRecord tagRec;
		protected Hashtable dynVars;
		public BibleBookInfo bkInfo;
		public TagInfo tags;
		public bool markFirstVerse;
		public bool markFirstChapter;
		public bool markPsalmV1;
		public bool markPsalm1;
		protected bool inPsalms;
		public bool dropCap;
		public bool dropCapPsalm;
		public bool autoCalcDropCap;
		public string versePrefix;
		public string verseSuffix;
		public string chapterPrefix;
		public string chapterSuffix;
		public string psalmPrefix;
		public string psalmSuffix;
		protected bool inPara;
		protected bool inRun;
		protected bool inFootnote;
		protected bool inXref;
		protected bool inEndnote;
		protected bool inFootnoteCharStyle;
		protected bool chapterStart;
		public StreamWriter sw;
		protected bool addRefToFootnote;
		protected bool addRefToXrefNote;
		protected bool customFootnoteMark;
		protected bool customXrefMark;
		protected FootNoteCaller footnoteMark;
		protected FootNoteCaller xrefMark;
		protected bool embedUsfx;
		public bool suppressIndentWithDropCap;
		public int dropCapLines;
		public LengthString horizFromText;
		public LengthString dropCapSpacing;
		public LengthString dropCapSize;
		public LengthString dropCapPosition;
		public LengthString dropCapBefore;
		public bool mergeXref;
		public bool includeCropMarks;
		public LengthString croppedPageWidth;
		public LengthString croppedPageLength;
		public LengthString seedPaperWidth;
		public LengthString seedPaperHeight;
		public string xrefName;
		protected bool isNTPP;
		public bool enableSubstitutions;
		public string substitutionName;
		protected SubstituteStrings substitutions;
		protected string ns;
		protected bool inUSFXNote;
		protected bool inUSFXNoteStyle;
		protected string currentFootnoteCaller;
        public bool sfmErrorsFound;

		protected string cvMark
		{
			get
			{
				if (verseMark == "")
					return chapterMark;
				if (bkRec.numChapters == 1)
					return verseMark;
			   return chapterMark + ":" + verseMark;
			}
		}

		public static string ConvertToRealNBSpace(string s)
		{
			string result = s.Replace("nbsp", "\u00A0");
			result = result.Replace("nbhs", "\u202F");
			return result;
		}

		public Scriptures()
		{
            if (SFConverter.jobIni == null)
            {
                string dataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "haiola");
                if (!Directory.Exists(dataDir))
                    Directory.CreateDirectory(dataDir);
                dataDir = Path.Combine(dataDir, "WordSend");
                if (!Directory.Exists(dataDir))
                    Directory.CreateDirectory(dataDir);
                SFConverter.jobIni = new XMLini(Path.Combine(dataDir, "joboptions.xml"));
            }
			UsfxStyleSuspended = false;
			isNTPP = true;
			Logit.WriteLine("--- "+DateTime.Now.ToLongDateString()+" "+DateTime.Now.ToLongTimeString()+" ---");
			books = new BibleBook[BibleBookInfo.MAXNUMBOOKS];
			bkInfo = new BibleBookInfo();
			tags = new TagInfo();
			currentParagraphStyle = "";
			dynVars = new Hashtable();
			inPara = false;
			inRun = false;
			inFootnote = false;
			inXref = false;
			inUSFXNote = false;
			inUSFXNoteStyle = false;
			inEndnote = false;
			chapterStart = false;
			inFootnoteCharStyle = false;
			cStyles = new TextAttributeTracker();
			usfxStyleCount = 0;
			currentCharacterStyle = null;
			headerName = "";
			activeRunStyle = null;
			addRefToFootnote = SFConverter.jobIni.ReadBool("insertCallingVerseRef", true);
			addRefToXrefNote = SFConverter.jobIni.ReadBool("insertXrefVerse", true);
			customFootnoteMark = SFConverter.jobIni.ReadBool("useCustomFootnoteCaller", false);
			customXrefMark = SFConverter.jobIni.ReadBool("useCustomXrefCaller", true);
			footnoteMark = new FootNoteCaller(SFConverter.jobIni.ReadString("customFootnoteCaller", "* † ‡"));
			xrefMark = new FootNoteCaller(SFConverter.jobIni.ReadString("customXrefCaller", "#"));
			markFirstVerse = SFConverter.jobIni.ReadBool("verse1", true);
			markFirstChapter = SFConverter.jobIni.ReadBool("chapter1", true);
			markPsalmV1 = SFConverter.jobIni.ReadBool("labelPsalmV1", true);
			markPsalm1  = SFConverter.jobIni.ReadBool("labelPsalm1", true);
			dropCap = SFConverter.jobIni.ReadBool("dropCap", true);
			dropCapPsalm = SFConverter.jobIni.ReadBool("dropCapPsalm", false);
			ns = SFConverter.jobIni.ReadString("nameSpace", "").Trim();
			if ((ns.Length > 0) && (ns[ns.Length-1] != ':'))
				ns = ns + ":";
			versePrefix = ConvertToRealNBSpace(SFConverter.jobIni.ReadString("versePrefix", ""));
			verseSuffix = ConvertToRealNBSpace(SFConverter.jobIni.ReadString("verseSuffix", "nbhs"));
			chapterPrefix = ConvertToRealNBSpace(SFConverter.jobIni.ReadString("chapterName", "Chapter"));
			chapterSuffix = ConvertToRealNBSpace(SFConverter.jobIni.ReadString("chapterSuffix", ""));
			psalmPrefix = ConvertToRealNBSpace(SFConverter.jobIni.ReadString("vernacularPsalm", "Psalm"));
			psalmSuffix = ConvertToRealNBSpace(SFConverter.jobIni.ReadString("vernacularPsalmSuffix", ""));
			embedUsfx =  SFConverter.jobIni.ReadBool("embedUsfx", true);
			suppressIndentWithDropCap = SFConverter.jobIni.ReadBool("suppressIndentWithDropCap", false);
			autoCalcDropCap = SFConverter.jobIni.ReadBool("autoCalcDropCap", true);
			horizFromText = new LengthString(SFConverter.jobIni.ReadString("horizFromText", "72 twips"), 72, 't');
			dropCapSpacing = new LengthString(SFConverter.jobIni.ReadString("dropCapSpacing", "459 twips"), 459.0, 't');
			dropCapSize = new LengthString(SFConverter.jobIni.ReadString("dropCapSize", "26.5 pt"), 53, 'h');
			dropCapPosition = new LengthString(SFConverter.jobIni.ReadString("dropCapPosition", "-3 pt"), -6.0, 'h');
			dropCapBefore = new LengthString(SFConverter.jobIni.ReadString("dropCapBefore", "0 pt"), 0, 'p');
			dropCapLines = SFConverter.jobIni.ReadInt("dropCapLines", 2);
			includeCropMarks = SFConverter.jobIni.ReadBool("includeCropMarks", false);
			croppedPageWidth = new LengthString(SFConverter.jobIni.ReadString("pageWidth", "150 mm"), 150.0, 'm');
			croppedPageLength = new LengthString(SFConverter.jobIni.ReadString("pageLength", "216 mm"), 216.0, 'm');

			xrefName = SFConverter.jobIni.ReadString("xrefName",
				Path.Combine(Path.GetDirectoryName(XMLini.ExecutableName()), "crossreference.xml"));
			substitutionName = SFConverter.jobIni.ReadString("substitutionName",
				Path.Combine(Path.GetDirectoryName(XMLini.ExecutableName()), "fixquotemarks.xml"));
			enableSubstitutions = SFConverter.jobIni.ReadBool("enableSubstitutions", false);
			if (enableSubstitutions)
				substitutions = new SubstituteStrings(substitutionName);
			mergeXref = SFConverter.jobIni.ReadBool("mergeXref", false);
            sfmErrorsFound = false;
		}

		protected int zindex;

		protected void DrawLine(LengthString fromX, LengthString fromY, LengthString toX, LengthString toY)
		{
			// Logit.WriteLine("Drawing line from ("+fromX.Millimeters.ToString()+
			// ", "+fromY.Millimeters.ToString()+") to ("+toX.Millimeters.ToString()+", "+
			// toY.Millimeters.ToString()+") mm"); // DEBUG
			zindex++;
			EndWordMLTextRun();
			xw.WriteStartElement("w:r");
			xw.WriteStartElement("w:rPr");
			xw.WriteElementString("w:noProof", "");
			xw.WriteEndElement();	// w:rPr
			xw.WriteStartElement("w:pict");
			xw.WriteStartElement("v:line");
			xw.WriteAttributeString("id", "unique"+zindex.ToString());
			xw.WriteAttributeString("style", "position:absolute;left:0;text-align:left;z-index:"+zindex+";mso-position-horizontal-relative:page;mso-position-vertical-relative:page");
			xw.WriteAttributeString("from", fromX.PointString+","+fromY.PointString);
			xw.WriteAttributeString("to", toX.PointString+","+toY.PointString);
			xw.WriteAttributeString("o:allowincell", "f");
			xw.WriteStartElement("w10:wrap");
			xw.WriteAttributeString("anchorx", "page");
			xw.WriteAttributeString("anchory", "page");
			xw.WriteEndElement();	// w10:wrap
			xw.WriteElementString("w10:anchorlock", "");
			xw.WriteEndElement();	// v:line
			xw.WriteEndElement();	// w:pict
			xw.WriteEndElement();	// w:r
		}

		protected void DrawCropMarks(LengthString PaperWidth, LengthString PaperHeight, LengthString PageWidth, LengthString PageHeight)
		{
			// Computations are done in twips (1/1440 inches) in this function.
			int sideMargin, topMargin;
			LengthString fromX = new LengthString();
			LengthString fromY = new LengthString();
			LengthString toX = new LengthString();
			LengthString toY = new LengthString();

			sideMargin = (PaperWidth.Twips - PageWidth.Twips) / 2;
			topMargin = (PaperHeight.Twips - PageHeight.Twips) / 2;

			// Logit.WriteLine("Paper size = "+PaperWidth.Millimeters.ToString()+"×"+PaperHeight.Millimeters.ToString()+" mm");
			// Logit.WriteLine("Page size = "+PageWidth.Millimeters.ToString()+"×"+PageHeight.Millimeters.ToString()+" mm");
			// Logit.WriteLine("Margins = "+sideMargin.ToString()+"×"+topMargin.ToString()+" twips");

			// Top left horizontal
			fromX.Twips = sideMargin - 680;
			fromY.Twips = toY.Twips = topMargin;
			toX.Twips = fromX.Twips + 567;
			DrawLine(fromX, fromY, toX, toY);

			// Top left vertical
			fromX.Twips = toX.Twips = sideMargin;
			fromY.Twips = topMargin - 680;
			toY.Twips = topMargin - 113;
			DrawLine(fromX, fromY, toX, toY);

			// Top right vertical
			fromX.Twips = toX.Twips = sideMargin + PageWidth.Twips;
			// fromY.Twips = topMargin - 680;
			// toY.Twips = topMargin - 113;
			DrawLine(fromX, fromY, toX, toY);

			// Top right horizontal
			fromX.Twips = sideMargin + PageWidth.Twips + 113;
			toX.Twips = fromX.Twips + 567;
			fromY.Twips = toY.Twips = topMargin;
			DrawLine(fromX, fromY, toX, toY);

			// Bottom right horizontal
			// fromX.Twips = sideMargin + PageWidth.Twips + 113;
			// toX.Twips = fromX.Twips + 567;
			fromY.Twips = toY.Twips = topMargin + PageHeight.Twips;
			DrawLine(fromX, fromY, toX, toY);

			// Bottom right vertical
			fromX.Twips = toX.Twips = sideMargin + PageWidth.Twips;
			fromY.Twips = topMargin + PageHeight.Twips + 113;
			toY.Twips = fromY.Twips + 567;
			DrawLine(fromX, fromY, toX, toY);

			// Bottom left vertical
			fromX.Twips = toX.Twips = sideMargin;
			// fromY.Twips = topMargin + PageHeight.Twips + 113;
			// toY.Twips = fromY.Twips + 567;
			DrawLine(fromX, fromY, toX, toY);

			// Bottom left horizontal
			fromX.Twips = sideMargin - 680;
			toX.Twips = fromX.Twips + 567;
			fromY.Twips = toY.Twips = topMargin + PageHeight.Twips;
			DrawLine(fromX, fromY, toX, toY);
		}

		protected void InsertCropMarkedFooter(string whichFooter)
		{	// whichFooter should be "even", "odd", or "first" (in that order)
			xw.WriteStartElement("w:ftr");
			xw.WriteAttributeString("w:type", whichFooter);
			xw.WriteStartElement("w:p");
			xw.WriteStartElement("w:pPr");
			xw.WriteStartElement("w:pStyle");
			xw.WriteAttributeString("w:val", "Footer");
			xw.WriteEndElement();	//	w:pStyle
			xw.WriteEndElement();	// w:pPr
			DrawCropMarks(seedPaperWidth, seedPaperHeight, croppedPageWidth, croppedPageLength);
			xw.WriteEndElement();	// w:P
			xw.WriteEndElement();	// w:ftr

		}

		protected void PushCharStyle(string style)
		{
			cStyles.PushStyle(style);
		}

		protected void EndFootnoteCharStyle()
		{
			if (inFootnoteCharStyle)
			{
				cStyles.PopStyle();
				inFootnoteCharStyle = false;
			}
            EndUSFXNoteStyle();
        }

		protected void StartFootnoteCharStyle(string style)
		{
			EndFootnoteCharStyle();
			cStyles.PushStyle(style);
			inFootnoteCharStyle = true;
		}

		protected string PopCharStyle()
		{
			return cStyles.PopStyle();
		}

		public string currentCharacterStyle
		{
			get
			{
				return cStyles.PeekStyle();
			}
			set
			{
				cStyles.PushStyle(value);
			}
		}

		public SortedList holyBible;	// List of BibleBooks

		protected StreamReader sr;
		protected XmlTextWriter xw;

        public void ReadUSFM(string fileName)
        {
            ReadUSFM(fileName, null);
        }

		public void ReadUSFM(string fileName, Encoding textEncoding)
		{
			bool foundAnSfm;
			SfmObject sfm;
			int bookIndex = 0;
            currentChapter = 0;
			int  vs = 0;

            if (textEncoding == null)
                textEncoding = fileHelper.IdentifyFileCharset(fileName);
			sr = new StreamReader(fileName, textEncoding);
			book = new BibleBook();
			// Read in the book, one SFM record at a time. We'll sort out the
			// symantics and hierarchy once the book is in memory.
			do
			{
				sfm = new SfmObject();
				foundAnSfm = sfm.Read(sr, fileName);
				if (foundAnSfm)
				{
					if (enableSubstitutions && (sfm.text != null))
					{
						sfm.text = substitutions.Substitute(sfm.text);
					}
					book.AddSfm(sfm);
					if (sfm.tag == "h")
						book.vernacularHeader = sfm.text;
					else if (sfm.tag == "mt")
					{
						if (book.vernacularName.Length > 1)
							book.vernacularName += " ";
						book.vernacularName += sfm.text;
                        if (book.vernacularHeader.Length < 1)
                            book.vernacularHeader = book.vernacularName;
					}
					if (sfm.info.parameterName != null)
					{
						dynVars[sfm.info.parameterName] = sfm.attribute;
					}
				}
			} while (foundAnSfm);
			sr.Close();

			// Search the book for metadata.
			sfm = book.FirstSfm();
			while (sfm != null)
			{

				try
				{
					if (sfm.tag == "id")
					{
                        sfm.attribute = sfm.attribute.ToUpper();
                        bookIndex = 0;
                        book.bookCode = sfm.attribute;
                        if (book.bookCode.Length > 3)
                        {
                            book.bookCode = book.bookCode.Remove(book.bookCode.IndexOfAny(".;,:-".ToCharArray()));
                            if (book.bookCode.Length > 3)
                                book.bookCode = book.bookCode.Substring(0, 3);
                            Logit.WriteError("Warning: shortened book code from '" + sfm.attribute + "' to '" + book.bookCode + "'");
                            sfm.attribute = book.bookCode;
                        }
						bookIndex = ((BibleBookRecord)bkInfo.books[book.bookCode]).sortOrder;
					}
					else if (sfm.tag == "c")
					{
						//Logit.WriteLine("book #"+bookIndex.ToString()+" chapter "+currentChapter.ToString()+" has "+bkInfo.bookArray[bookIndex].verseCount[currentChapter].ToString()+" verses.");
						currentChapter = Int32.Parse(sfm.attribute);
						vs = 0;
					}
					else if (sfm.tag == "v")
					{

                        vs = Math.Max(vs, Int32.Parse(sfm.attribute));
						if ((currentChapter > 0) && (currentChapter <= bkInfo.bookArray[bookIndex].numChapters))
						{
							bkInfo.bookArray[bookIndex].verseCount[currentChapter] = vs;
						}
					}
				}
				catch
				{
                    if (sfm.tag == "id")
                    {
                        Logit.WriteError("ERROR in \\id line, invalid book code: '" + sfm.attribute + "'");
                    }
					// never mind: ignore non-numeric chapter and verse numbers. May be verse bridge.
				}
				sfm = book.NextSfm();
			}
            if (book.bookCode == "")
            {
                Logit.WriteError("ERROR: No \\id tag found in " + fileName);
                return;
            }

			// Insert this file's data into the data structure.
			if (books[bookIndex] == null)
			{	// First instance of this book.
				books[bookIndex] = book;
			}
			else
			{	// Adding a chapter...
				sfm = book.FirstSfm();
				while (sfm != null)
				{
					books[bookIndex].AddSfm(sfm);
					sfm = book.NextSfm();
				}
			}

			if ((bookIndex < 64) && (bookIndex != 19) && (bookIndex != 20))
			{
				isNTPP = false;
			}

		}

		public string StripLeadingWhiteSpace(string s)
		{
			while ((s.Length > 0) && (fileHelper.IsNormalWhiteSpace(s[0])))
				s = s.Remove(0, 1);
			return s;
		}

		protected bool inUsfxParagraph;

		protected void WriteWordMLParagraph(string paraStyle, string contents, string charStyle, string sfm, int level, string paragraphStyle)
		{
			EndWordMLParagraph();
			currentParagraphStyle = paraStyle;

			if (embedUsfx)
			{
				StartUSFXParagraph(sfm, level, paraStyle, null);
			}
			xw.WriteStartElement("w:p");
			inPara = true;
			if (paraStyle != "Normal")
			{
				xw.WriteStartElement("w:pPr");
				xw.WriteStartElement("w:pStyle");
				xw.WriteAttributeString("w:val", paraStyle);
				xw.WriteEndElement();	// w:pStyle
				xw.WriteEndElement();	// w:pPr
			}
			WriteWordMLTextRun(StripLeadingWhiteSpace(contents), charStyle);
		}

		protected void WriteWordMLParagraph(string paraStyle, string contents, string charStyle)
		{
			WriteWordMLParagraph(paraStyle, contents, charStyle, null, 0, null);
		}

		protected void WriteWordMLParagraph(string paraStyle, string contents)
		{
			WriteWordMLParagraph(paraStyle, contents, null);
		}

		protected void StartWordMLParagraph(string paraStyle, string contents, string charStyle, string sfm, int level, string styleName)
		{
			EndWordMLParagraph();
			currentParagraphStyle = paraStyle;
			StartUSFXParagraph(sfm, level, styleName, null);
	
			xw.WriteStartElement("w:p");
			inPara = true;
			if (paraStyle == "Normal")
			{
				if (suppressIndent)
				{
					xw.WriteStartElement("w:pPr");
					xw.WriteStartElement("w:ind");
					xw.WriteAttributeString("w:first-line", "0");
					xw.WriteEndElement();	// w:ind
					xw.WriteEndElement();	// w:pPr
					suppressIndent = false;
				}
			}
			else
			{
				xw.WriteStartElement("w:pPr");
				xw.WriteStartElement("w:pStyle");
				xw.WriteAttributeString("w:val", paraStyle);
				xw.WriteEndElement();	// w:pStyle
				if (suppressIndent)
				{
					xw.WriteStartElement("w:ind");
					xw.WriteAttributeString("w:first-line", "0");
					xw.WriteEndElement();	// w:ind
					suppressIndent = false;
				}
				xw.WriteEndElement();	// w:pPr
			}
			ResumeUSFXStyle();
			xw.WriteStartElement("w:r");
			inRun = true;
			WriteWordMLTextRun(StripLeadingWhiteSpace(contents), charStyle);
		}

		protected void StartUSFXParagraph(string sfm, int level, string styleName, string contents)
		{
			try
			{
				EndWordMLParagraph();
				EndUSFXParagraph();
				if (embedUsfx)
				{
					if (sfm != null)
					{
						inUsfxParagraph = true;
						if ((sfm == "p") || (sfm == "b") || (sfm == "q") || (sfm == "d") || (sfm == "s") || (sfm == "generated"))
							xw.WriteStartElement(ns+sfm);
						else
						{
							xw.WriteStartElement(ns+"p");
							xw.WriteAttributeString("sfm", sfm);
							if ((styleName != null) && (styleName != ""))
								xw.WriteAttributeString("style", styleName);
						}
						if (level > 1)
							xw.WriteAttributeString("level", level.ToString());
					}
				}
				if (contents != null)
					WriteUSFXText(StripLeadingWhiteSpace(contents));
			}
			catch (System.Exception ex)
			{
				Logit.WriteError(ex.ToString());
			}
		}

		protected void StartWordMLParagraph(string paraStyle, string contents)
		{
			StartWordMLParagraph(paraStyle, contents, null, null, 0, null);
		}

		protected void ResumeWordMLParagraph(string contents, string styleName)
		{
			EndWordMLParagraph();
			inPara = true;
			xw.WriteStartElement("w:p");
			xw.WriteStartElement("w:pPr");
			if (currentParagraphStyle != "Normal")
			{
				xw.WriteStartElement("w:pStyle");
				xw.WriteAttributeString("w:val", currentParagraphStyle);
				xw.WriteEndElement();	// w:pStyle
			}
			xw.WriteStartElement("w:ind");
			xw.WriteAttributeString("w:first-line", "0");
			xw.WriteEndElement();	// w:ind
			xw.WriteEndElement();	// w:pPr
			ResumeUSFXStyle();
			xw.WriteStartElement("w:r");
			inRun = true;
			WriteWordMLTextRun(contents, styleName);
		}

		protected void WriteWordMLTextRun(string contents, string styleName)
		{
			if ((contents != null) && (contents != ""))
			{
				if (inPara)
				{
					if ((styleName != activeRunStyle) || (!inRun))
					{
						if (inRun)
							xw.WriteEndElement();
						xw.WriteStartElement("w:r");
						inRun = true;
						if ((styleName != null) && (styleName != ""))
						{
							xw.WriteStartElement("w:rPr");
							xw.WriteStartElement("w:rStyle");
							xw.WriteAttributeString("w:val", styleName);
							xw.WriteEndElement();	// w:rStyle
							xw.WriteEndElement();	// w:rPr
						}
						activeRunStyle = styleName;
					}
					xw.WriteElementString("w:t", contents);
				}
				else
				{
					ResumeWordMLParagraph(contents, styleName);
				}
			}
		}

		protected void EndWordMLTextRun()
		{
			if (inRun)
			{
				xw.WriteEndElement();
				inRun = false;
			}
		}

		protected void WriteUSFXText(string contents)
		{
            int lineBreakPlace;
			if ((contents != null) && (contents != ""))
			{
                lineBreakPlace = contents.IndexOf("//");
                if (lineBreakPlace >= 0)
                {
                    if (lineBreakPlace > 0)
                    {
                        xw.WriteString(contents.Substring(0, lineBreakPlace));
                    }
                    xw.WriteElementString("optionalLineBreak", "");
                    if (contents.Length > (lineBreakPlace + 2))
                    {   // Recursive call in case there is more than one // in contents.
                        WriteUSFXText(contents.Substring(lineBreakPlace + 2));
                    }
                }
                else
                {
                    if (contents.EndsWith(" "))
                    {
                        xw.WriteString(contents.TrimEnd() + "\r\n");
                    }
                    else
                    {
                        xw.WriteString(contents);
                    }
                    
                }
			}
		}

		protected void WriteWordMLTextRun(string contents)
		{
			WriteWordMLTextRun(contents, currentCharacterStyle);
		}

		protected void WriteField(string contents)
		{
			if (contents != null)
			{
                EndWordMLTextRun();
				// Write field begin indicator
				xw.WriteStartElement("w:r");
				xw.WriteStartElement("w:rPr");
				xw.WriteElementString("w:i", "");
				xw.WriteElementString("w:i-cs", "");
				xw.WriteEndElement();	// w:rPr
				xw.WriteStartElement("w:fldChar");
				xw.WriteAttributeString("w:fldCharType", "begin");
				xw.WriteEndElement();	// w:fldChar
				xw.WriteEndElement();	// w:r

				// Write text run containing field instruction
				xw.WriteStartElement("w:r");
				xw.WriteStartElement("w:rPr");
				xw.WriteElementString("w:i", "");
				xw.WriteElementString("w:i-cs", "");
				xw.WriteEndElement();	// w:rPr
				xw.WriteElementString("w:instrText", contents);
				xw.WriteEndElement();	// w:r

				// Write field end indicator
				xw.WriteStartElement("w:r");
				xw.WriteStartElement("w:rPr");
				xw.WriteElementString("w:i", "");
				xw.WriteElementString("w:i-cs", "");
				xw.WriteEndElement();	// w:rPr
				xw.WriteStartElement("w:fldChar");
				xw.WriteAttributeString("w:fldCharType", "end");
				xw.WriteEndElement();	// w:fldChar
				xw.WriteEndElement();	// w:r
			}
		}

		protected int usfxNestLevel;

		protected void EndWordMLParagraph()
		{
			if (inPara)
			{
				if (inRun)
				{
					xw.WriteEndElement();	// w:r
					inRun = false;
					activeRunStyle = null;
				}
				xw.WriteEndElement();	// w:p
				inPara = false;
			}
//			if (embedUsfx)
//				EndUSFXParagraph();
		}

		protected void EndUSFXParagraph()
		{
			if (inUsfxParagraph)
			{
				while  (usfxStyleCount > 0)
				{
					// Logit.WriteLine("Ending style level "+usfxStyleCount.ToString());
					usfxStyleCount--;
					xw.WriteEndElement();
				}

				while (usfxNestLevel > 0)
				{
					// Logit.WriteLine("Ending USFX element level "+usfxNestLevel.ToString());
					xw.WriteEndElement();
					usfxNestLevel--;
				}

				// Logit.WriteLine("Ending paragraph");
				xw.WriteEndElement();	// ns+p
				inUsfxParagraph = false;
			}
		}


		protected void StartEmbedUSFXElement(string elementName, string attributeName, string attribute, string contents, string styleName)
		{
			if ((contents != "") && (!inPara))
				StartWordMLParagraph(currentParagraphStyle, null, currentCharacterStyle, null, 0, currentParagraphStyle);
			if (inRun)
			{
				xw.WriteEndElement();
				inRun = false;
			}

			if (embedUsfx)
			{
				StartUSFXElement(elementName, attributeName, attribute, null);
			}
			if (styleName == null)
				styleName = currentParagraphStyle;
			WriteWordMLTextRun(contents, styleName);
		}

		protected void WriteUSFXMilestone(string elementname, string sfm, int level, string attribute, string content)
		{
            if (embedUsfx)
            {
                if (inRun)
                {
                    xw.WriteEndElement();
                    inRun = false;
                }
                xw.WriteStartElement(ns + elementname);
                xw.WriteAttributeString("sfm", sfm);
                if (level > 1)
                    xw.WriteAttributeString("level", level.ToString());
                if (attribute != null)
                    xw.WriteAttributeString("attribute", attribute);
                xw.WriteEndElement();
            }
            if (content != null)
                WriteUSFXText(content);
		}

		protected void StartUSFXElement(string elementName, string attributeName, string attribute, string contents)
		{
			if (embedUsfx)
			{
				EndWordMLTextRun();	// Observer proper mixed WordML & custom XML mixing rules.
				// Logit.WriteLine("Starting element "+elementName);
				xw.WriteStartElement(ns+elementName);
				usfxNestLevel++;
				if ((attributeName != null) && (attribute != null))
				{
					xw.WriteAttributeString(attributeName, attribute);
				}
			}
			WriteUSFXText(contents);
		}

		protected void StartUSFXElement(string elementName)
		{
			StartUSFXElement(elementName, null, null, null);
		}

		protected void EndEmbedUSFXElement()
		{
			if (embedUsfx)
			{
				if (inRun)
				{
					xw.WriteEndElement();	// w:r
					inRun = false;
				}
				EndUSFXElement();
			}
		}

		protected void EndUSFXElement()
		{
			if (usfxNestLevel > 0)
			{
				// Logit.WriteLine(" Ending element level "+usfxNestLevel.ToString());
				xw.WriteEndElement();
				usfxNestLevel--;
			}
		}

		protected void WriteParaStyle(string styleName)
		{
			xw.WriteStartElement("w:pPr");
			xw.WriteStartElement("w:pStyle");
			xw.WriteAttributeString("w:val", styleName);
			xw.WriteEndElement();	// w:pStyle
			xw.WriteEndElement();	// w:pPr
		}

		protected void WriteFieldChar(string type, string style)
		{
			xw.WriteStartElement("w:r");
			if (style != null)
			{
				xw.WriteStartElement("w:rPr");
				xw.WriteStartElement("w:rStyle");
				xw.WriteAttributeString("w:val", style);
				xw.WriteEndElement();	// w:rStyle
				xw.WriteEndElement();	// w:rPr
			}
			xw.WriteStartElement("w:fldChar");
			xw.WriteAttributeString("w:fldCharType", type);
			xw.WriteEndElement();	// w:fldChar
			xw.WriteEndElement();	// w:r
		}

		protected void WriteFieldChar(string type)
		{
			WriteFieldChar(type, null);
		}

		protected bool suppressIndent;

		protected bool DropCapChapter(string chap)
		{
			bool result = false;
			if (chapterStart &&
				((inPsalms && ((chap != "1") || (markPsalm1))) ||
				(!inPsalms && ((chap != "1") || (markFirstChapter)))))
			{
				result = true;
				SuspendUSFXStyle();
				EndWordMLParagraph();

				xw.WriteStartElement("wx:pBdrGroup");
				   xw.WriteStartElement("wx:apo");
				      xw.WriteStartElement("wx:jc");
				         xw.WriteAttributeString("wx:val", "left");
				      xw.WriteEndElement();	// wx:jc
				      xw.WriteStartElement("wx:horizFromText");
				         xw.WriteAttributeString("wx:val", horizFromText.Twips.ToString());
				      xw.WriteEndElement();	// wx:horizFromText
				   xw.WriteEndElement();	// wx:apo
				   xw.WriteStartElement("w:p");
				      xw.WriteStartElement("w:pPr");
				         xw.WriteStartElement("w:keepNext");
				         xw.WriteEndElement();	// w:keepNext
				         xw.WriteStartElement("w:framePr");
				            xw.WriteAttributeString("w:drop-cap", "drop");
				            xw.WriteAttributeString("w:lines", dropCapLines.ToString());
				            xw.WriteAttributeString("w:hspace", horizFromText.Twips.ToString());
				            xw.WriteAttributeString("w:wrap", "around");
				            xw.WriteAttributeString("w:vanchor", "text");
				            xw.WriteAttributeString("w:hanchor", "text");
				         xw.WriteEndElement();	// w:framePr
				         xw.WriteStartElement("w:spacing");
				            if (dropCapBefore.Twips > 0)
									xw.WriteAttributeString("w:before", dropCapBefore.Twips.ToString());
				            xw.WriteAttributeString("w:line", dropCapSpacing.Twips.ToString());
				            xw.WriteAttributeString("w:line-rule", "exact");
				         xw.WriteEndElement();	// w:spacing
				         xw.WriteStartElement("w:ind");
				             xw.WriteAttributeString("w:first-line", "0");
				         xw.WriteEndElement();	// w:ind
				         xw.WriteStartElement("w:textAlignment");
				            xw.WriteAttributeString("w:val", "baseline");
				         xw.WriteEndElement();	// w:textAlignment
				         xw.WriteStartElement("w:rPr");
				            xw.WriteStartElement("w:position");
				               xw.WriteAttributeString("w:val", dropCapPosition.HalfPoints.ToString());
				            xw.WriteEndElement();
				            xw.WriteStartElement("w:sz");
				               xw.WriteAttributeString("w:val", dropCapSize.HalfPoints.ToString());
				            xw.WriteEndElement();	// w:sz
				            xw.WriteStartElement("w:sz-cs");
				               xw.WriteAttributeString("w:val", dropCapSize.HalfPoints.ToString());
				            xw.WriteEndElement();	// w:sz-cs
				         xw.WriteEndElement();	// w:rPr
				     xw.WriteEndElement();	// w:pPr
					if (embedUsfx)
						xw.WriteStartElement(ns+"generated");
				    xw.WriteStartElement("w:r");
				       xw.WriteStartElement("w:rPr");
				          xw.WriteStartElement("w:position");
				             xw.WriteAttributeString("w:val", dropCapPosition.HalfPoints.ToString());
				          xw.WriteEndElement();	// w:position
				          xw.WriteStartElement("w:sz");
				             xw.WriteAttributeString("w:val", dropCapSize.HalfPoints.ToString());
				          xw.WriteEndElement();	// w:sz
				          xw.WriteStartElement("w:sz-cs");
				             xw.WriteAttributeString("w:val", dropCapSize.HalfPoints.ToString());
				          xw.WriteEndElement();	// w:sz-cs
				       xw.WriteEndElement();	// w:rPr
				       xw.WriteElementString("w:t", chap);
				    xw.WriteEndElement();	// w:r
					if (embedUsfx)
						xw.WriteEndElement();	// generated
			    xw.WriteEndElement();	// w:p
	       xw.WriteEndElement();	// wx:pBdrGroup
   		 suppressIndent = suppressIndentWithDropCap;
			}
			chapterStart = false;

			return result;
		}

		protected void StartUSFXNote(string sfm, string caller, string content)
		{
			if (embedUsfx)
			{
				EndUSFXNote();
				EndWordMLTextRun();	// Observer proper mixed WordML & custom XML mixing rules.
				// Logit.WriteLine(" Starting note "+sfm);
				if (sfm == "fe")
				{
					xw.WriteStartElement(ns+"f");
					xw.WriteAttributeString("sfm", sfm);
				}
				else
				{
					xw.WriteStartElement(ns+sfm);
				}
				xw.WriteAttributeString("caller", caller);
				inUSFXNote = true;
			}
			WriteUSFXText(content);
		}

		protected void EndUSFXNote()
		{
            EndUSFXNoteStyle();
			if (inUSFXNote)
			{
				// Logit.WriteLine(" Ending note.");
				xw.WriteEndElement();	// f, x
				inUSFXNote = false;
			}
		}

		protected void EndUSFXNoteStyle()
		{
			if (inUSFXNoteStyle)
			{
				// Logit.WriteLine("  Ending note style.");
				xw.WriteEndElement();
				inUSFXNoteStyle = false;
			}
		}

		protected void StartUSFXNoteStyle(string sfm, string content)
		{
			EndUSFXNoteStyle();
			if (embedUsfx)
			{
				inUSFXNoteStyle = true;
				// Logit.WriteLine("  Starting note style "+sfm);
				xw.WriteStartElement(ns+sfm);
			}
			WriteUSFXText(content);
		}

		protected static string charStyleTagList = " add bd bdit bk em it k nd no ord pn pro qac qs qt sc sig sls tl wr wj fk fm fq fr ft fv xk xo xq xt";

		protected string suspendedUsfxStyle;
		protected bool UsfxStyleSuspended;

		protected void StartUSFXStyle(string sfm, string content)
		{
			EndWordMLTextRun();
			if (embedUsfx)
			{
				usfxStyleCount++;
				// Logit.WriteLine("Starting style "+sfm+" level "+usfxStyleCount.ToString());
				if (charStyleTagList.IndexOf(sfm) > 0)
					xw.WriteStartElement(ns+sfm);
				else
				{
					xw.WriteStartElement(ns+"cs");
					xw.WriteAttributeString("sfm", sfm);
				}
				suspendedUsfxStyle = sfm;
			}
			WriteUSFXText(content);
		}

		protected void EndUSFXStyle()
		{
			if (usfxStyleCount > 0)
			{
				// Logit.WriteLine(" Ending style level "+usfxStyleCount.ToString());
				usfxStyleCount--;
				xw.WriteEndElement();
			}
		}

		protected void SuspendUSFXStyle()
		{
			if (embedUsfx && (usfxStyleCount > 0))
			{
				UsfxStyleSuspended = true;
				EndUSFXStyle();
			}
		}

		protected void ResumeUSFXStyle()
		{
			if (embedUsfx)
			{
				if (UsfxStyleSuspended && (suspendedUsfxStyle != null))
				{
					UsfxStyleSuspended = false;
					StartUSFXStyle(suspendedUsfxStyle, null);
				}
			}
		}

		protected void StartFootnote(string caller, string charStyle)
		{
			if (inFootnote)
				EndFootnote();
			if (inXref)
				EndXref();
			currentFootnoteCaller = caller;
			// Insert footnote caller
			if (inRun)
			{
				xw.WriteEndElement();	// w:r
				activeRunStyle = null;
				inRun = false;
			}
			xw.WriteStartElement("w:r");	// 1
			xw.WriteStartElement("w:rPr");	// 2
			xw.WriteStartElement("w:rStyle");	// 3
			xw.WriteAttributeString("w:val", "FootnoteReference");
			xw.WriteEndElement();	// w:rStyle 2
			xw.WriteEndElement();	// w:rPr 1
			if ((caller == "+") && (!customFootnoteMark))
			{
				xw.WriteStartElement("w:footnote"); // 2
			}
			else
			{
				xw.WriteStartElement("w:footnote");	// 2
				xw.WriteAttributeString("w:suppressRef", "on");
			}
			xw.WriteStartElement("w:p");	// 3
			xw.WriteStartElement("w:pPr");	// 4
			xw.WriteStartElement("w:pStyle");	// 5
			xw.WriteAttributeString("w:val", "FootnoteText");
			xw.WriteEndElement();	// w:pStyle 4
			xw.WriteEndElement();	// w:pPr 3
			xw.WriteStartElement("w:r");	// 4
			xw.WriteStartElement("w:rPr");	// 5
			xw.WriteStartElement("w:rStyle");	// 6
			xw.WriteAttributeString("w:val", "FootnoteReference");
			xw.WriteEndElement();	// w:rStyle 5
			xw.WriteEndElement();	// w:rPr 4
			if ((caller == "+") && (!customFootnoteMark))
			{
				xw.WriteStartElement("w:footnoteRef");	// 5
				xw.WriteEndElement();	// w:footnoteRef 4
			}
			else if (caller == "-")
			{
				xw.WriteElementString("w:t", "\u200b");	// Zero width space for marker
			}
			else
			{
				xw.WriteElementString("w:t", caller);
			}
			xw.WriteEndElement();	// w:r 3
			inFootnote = true;
			PushCharStyle(charStyle);
		}

		protected void EndFootnote()
		{
			if (inFootnote)
			{
				if (inRun)
				{
					xw.WriteEndElement();	// w:r
					activeRunStyle = null;
					inRun = false;
				}
				EndFootnoteCharStyle();
				inFootnote = false;
				xw.WriteEndElement();	// w:p 2
				xw.WriteEndElement();	// w:footnote 1
				if (!((currentFootnoteCaller == "+") && (!customFootnoteMark)))
					xw.WriteElementString("w:t", currentFootnoteCaller);
				xw.WriteEndElement();	// r 0
				PopCharStyle();
			}
		}

		protected string currentXrefCaller;

		protected void StartXref(string caller, string charStyle)
		{
			if (inFootnote)
				EndFootnote();
			if (inXref)
				EndXref();
            if (inEndnote)
                EndEndnote();
			currentXrefCaller = caller;

			if (inRun)
			{
				xw.WriteEndElement();	// w:r
				activeRunStyle = null;
				inRun = false;
			}
			xw.WriteStartElement("w:r");	// 1
			xw.WriteStartElement("w:rPr");	// 2
			xw.WriteStartElement("w:rStyle");	// 3
			xw.WriteAttributeString("w:val", "FootnoteReference");
			xw.WriteEndElement();	// w:rStyle 2
			xw.WriteEndElement();	// w:rPr 1
			if ((caller == "+") && (!customXrefMark))
			{
				xw.WriteStartElement("w:footnote"); // 2
			}
			else
			{
				xw.WriteStartElement("w:footnote");	// 2
				xw.WriteAttributeString("w:suppressRef", "on");
			}
			xw.WriteStartElement("w:p");	// 3
			xw.WriteStartElement("w:pPr");	// 4
			xw.WriteStartElement("w:pStyle");	// 5
			xw.WriteAttributeString("w:val", "FootnoteText");
			xw.WriteEndElement();	// w:pStyle 4
			xw.WriteEndElement();	// w:pPr 3
			xw.WriteStartElement("w:r");	// 4
			xw.WriteStartElement("w:rPr");	// 5
			xw.WriteStartElement("w:rStyle");	// 6
			xw.WriteAttributeString("w:val", "FootnoteReference");
			xw.WriteEndElement();	// w:rStyle 5
			xw.WriteEndElement();	// w:rPr 4
			if ((caller == "+") && (!customXrefMark))
			{
				xw.WriteStartElement("w:footnoteRef");	// 5
				xw.WriteEndElement();	// w:footnoteRef 4
			}
			else if (caller == "-")
			{
				xw.WriteElementString("w:t", "\u200b"); // Zero width space for marker
			}
			else
			{
				xw.WriteElementString("w:t", caller);
			}
			xw.WriteEndElement();	// w:r 3
			inXref = true;
			PushCharStyle(charStyle);
		}

		protected void EndXref()
		{
			if (inXref)
			{
				if (inRun)
				{
					xw.WriteEndElement();	// w:r
					activeRunStyle = null;
					inRun = false;
				}
				EndFootnoteCharStyle();
				inXref = false;
				xw.WriteEndElement();	// w:p 2
				xw.WriteEndElement();	// w:footnote 1
				if (!((currentFootnoteCaller == "+") && (!customXrefMark)))
					xw.WriteElementString("w:t", currentXrefCaller);
				xw.WriteEndElement();	// r 0
				PopCharStyle();
			}
		}


		protected void StartEndnote(string caller)
		{
			if (inEndnote)
				EndEndnote();
			// Insert footnote caller
			if (caller != "+")
			{
				WriteWordMLTextRun(caller, null);
			}
			if (inRun)
			{
				xw.WriteEndElement();	// w:r
				activeRunStyle = null;
				inRun = false;
			}
			xw.WriteStartElement("w:r");	// 1
			xw.WriteStartElement("w:rPr");	// 2
			xw.WriteStartElement("w:rStyle");	// 3
			xw.WriteAttributeString("w:val", "EndnoteReference");
			xw.WriteEndElement();	// w:rStyle 2
			xw.WriteEndElement();	// w:rPr 1
			if (caller == "+")
			{
				xw.WriteStartElement("w:endnote"); // 2
			}
			else
			{
				xw.WriteStartElement("w:endnote");	// 2
				xw.WriteAttributeString("w:suppressRef", "on");
			}
			xw.WriteStartElement("w:p");	// 3
			xw.WriteStartElement("w:pPr");	// 4
			xw.WriteStartElement("w:pStyle");	// 5
			xw.WriteAttributeString("w:val", "Endnotenormal");
			xw.WriteEndElement();	// w:pStyle 4
			xw.WriteEndElement();	// w:pPr 3
			xw.WriteStartElement("w:r");	// 4
			xw.WriteStartElement("w:rPr");	// 5
			xw.WriteStartElement("w:rStyle");	// 6
			xw.WriteAttributeString("w:val", "EndnoteReference");
			xw.WriteEndElement();	// w:rStyle 5
			xw.WriteEndElement();	// w:rPr 4
			if (caller == "+")
			{
				xw.WriteStartElement("w:footnoteRef");	// 5
				xw.WriteEndElement();	// w:footnoteRef 4
			}
			else
			{
				xw.WriteElementString("w:t", caller);
			}
			xw.WriteEndElement();	// w:r 3
			inEndnote = true;
			PushCharStyle("Endnote");
		}

		protected void EndEndnote()
		{
			if (inEndnote)
			{
				if (inRun)
				{
					xw.WriteEndElement();	// w:r
					activeRunStyle = null;
					inRun = false;
				}
				EndFootnoteCharStyle();
				inEndnote = false;
				xw.WriteEndElement();	// w:p 2
				xw.WriteEndElement();	// w:endnote 1
				xw.WriteEndElement();	// r 0
				PopCharStyle();
			}
		}

		protected CrossReference xref;
		protected SfmObject sf;
		protected bool inserted;
		protected bool bookStarted;
		protected XmlFileReader xr;
		protected string caller;
		protected string chapterLabel;
        public bool fatalError;
		string templateName;

		protected void WriteWordMLBook(int bknum)
		{
			string s;
			string badsf;
			string chapterModifier;
			book = books[bknum];
			bookStarted = false;
            // Figure fig;
			if ((bkInfo.bookArray[bknum] != null) && (book != null))
			{
				inPsalms = (book.bookCode == "PSA");
				bkRec = (BibleBookRecord)bkInfo.bookArray[bknum];
				if (bkRec == null)
				{
					Logit.WriteError("ERROR: BibleBookInfo array item "+bknum.ToString()+" missing.");
					xw.WriteAttributeString("sortOrder", bknum.ToString());
				}
				sf = book.FirstSfm();
				if (embedUsfx)
				{
					xw.WriteStartElement(ns+"book");
					xw.WriteAttributeString("id", book.bookCode);
				}								
				while (sf != null)
				{
					if (sf.info.contentName != "")
					{
						dynVars[sf.info.contentName] = sf.text;
					}
					if (sf.info.parameterName != "")
					{
						dynVars[sf.info.parameterName] = sf.attribute;
					}

					switch (sf.info.kind)
					{
						case "paragraph":
                            if (inFootnote)
                            {
                                Logit.WriteError("ERROR: paragraph tag " + sf.tag + " is not allowed in a footnote. " + book.bookCode + " " + chapterMark + ":" + verseMark);
                                EndFootnote();
                                fatalError = true;
                            }
                            if (inEndnote)
                            {
                                Logit.WriteError("ERROR: paragraph tag " + sf.tag + " is not allowed in an endnote." + book.bookCode + " " + chapterMark + ":" + verseMark);
                                EndEndnote();
                                fatalError = true;
                            }
                            if (inXref)
                            {
                                Logit.WriteError("ERROR: paragraph tag " + sf.tag + " is not allowed in a crossreference." + book.bookCode + " " + chapterMark + ":" + verseMark);
                                EndXref();
                                fatalError = true;
                            }
                            chapterModifier = "";
							if ((sf.tag == "mt") && !bookStarted)
							{
								headerName = sf.text;
								StartWordMLParagraph("Bookbreak"," ", null, "generated", 0, "Bookbreak");
								WriteWordMLTextRun(book.vernacularHeader+" ", "Bookmarker");
								EndWordMLParagraph();
								bookStarted = true;
							}
							if (chapterStart && (sf.text != "") && (sf.tag.StartsWith("p") || sf.tag.StartsWith("q") || (sf.tag == "d")))
							{
								if (DropCapChapter(chapterMark) && (sf.tag.StartsWith("p") || (sf.tag.StartsWith("q"))))
								{
									chapterModifier = "-ch";
								}
							}
							currentParagraphStyle = sf.info.paragraphStyleID(sf.level);
							StartWordMLParagraph(currentParagraphStyle+chapterModifier, sf.text, currentCharacterStyle, sf.tag, sf.level, sf.info.paragraphStyle);
							break;
						case "meta":
                            if (inFootnote)
                            {
                                Logit.WriteError("ERROR: tag " + sf.tag + " is not allowed in a footnote." + book.bookCode + " " + chapterMark + ":" + verseMark);
                                EndFootnote();
                                fatalError = true;
                            }
                            if (inEndnote)
                            {
                                Logit.WriteError("ERROR: tag " + sf.tag + " is not allowed in an endnote." + book.bookCode + " " + chapterMark + ":" + verseMark);
                                fatalError = true;
                                EndEndnote();
                            }
                            if (inXref)
                            {
                                Logit.WriteError("ERROR: tag " + sf.tag + " is not allowed in a crossreference." + book.bookCode + " " + chapterMark + ":" + verseMark);
                                EndXref();
                                fatalError = true;
                            }
                            switch (sf.tag)
						{
							case "c":
								s = currentParagraphStyle;
								chapterMark = sf.attribute;
								try
								{
									currentChapter = Int32.Parse(chapterMark);
								}
								catch
								{
									currentChapter = 0;
								}
								verseMark = "";
								if (embedUsfx)
								{
									EndWordMLTextRun();	// Observer proper mixed WordML & custom XML mixing rules.
									// Logit.WriteLine("Starting element "+elementName);
									xw.WriteStartElement(ns+"c");
									xw.WriteAttributeString("id", sf.attribute);
									xw.WriteEndElement();	// ns+c
								}
								if (inPsalms)
								{
									string cl = (string)dynVars["chapterLabel"];
									if (cl == null)
										cl = psalmPrefix;
									if (psalmPrefix.Length > 0)
										chapterLabel = cl + " " + sf.attribute;
									else
										chapterLabel = sf.attribute;
									if (psalmSuffix.Length > 0)
										chapterLabel = chapterLabel + " " + psalmSuffix;
									if (markPsalm1 || (chapterMark != "1"))
									{
										chapterStart = true;
										if (!dropCapPsalm)
										{
											WriteWordMLParagraph("PsalmLabel", chapterLabel, "", "generated", 0, null);
											//StartWordMLParagraph("Normal", "");
											chapterStart = false;
										}
									}
								}
								else
								{
									string cl = (string)dynVars["chapterLabel"];
									if (cl == null)
										cl = chapterPrefix;
									if (cl.Length > 0)
										chapterLabel = cl + " " + sf.attribute;
									else
										chapterLabel = sf.attribute;
									if (chapterSuffix.Length > 0)
										chapterLabel = chapterLabel + " " + chapterSuffix;
									if (markFirstChapter || (chapterMark != "1"))
									{
										chapterStart = true;
										if (!dropCap)
										{
											WriteWordMLParagraph("ChapterLabel", chapterLabel, "", "generated", 0, null);
											StartWordMLParagraph("Normal", "");
											chapterStart = false;
										}
									}
								}
								currentParagraphStyle = s;
								WriteWordMLTextRun(sf.text.TrimStart());
								break;
							case "v":
								if (chapterStart)
								{
									if ((inPsalms && dropCapPsalm) ||  (dropCap && !inPsalms))
									{
										if (DropCapChapter(chapterMark) && ((currentParagraphStyle == "Normal") ||
											(currentParagraphStyle == "Poetry line 1")))
											currentParagraphStyle += "-ch";
									}
								}
								if (embedUsfx)
								{
									if (!inPara)
										ResumeWordMLParagraph(null, null);
									StartUSFXElement("v", "id", sf.attribute, null);
								}
								verseMark = sf.attribute.Replace('-', '\u2011');	// Replace hyphens with non-breaking hyphens in verse markers.
								if (bkRec.numChapters == 1)
								{
									WriteWordMLTextRun(" ", "cvmarker");
								}
								else
								{
									WriteWordMLTextRun(chapterMark, "cvmarker");
								}
								if ((verseMark != "1") ||
									(inPsalms && markPsalmV1) ||
									(markFirstVerse && !inPsalms ))
								{
									WriteWordMLTextRun(versePrefix+verseMark+verseSuffix, "Versemarker");
								}
								if (embedUsfx)
								{
									EndUSFXElement();	// ns+v
								}
								WriteWordMLTextRun(sf.text, currentCharacterStyle);
								if (mergeXref)
								{
									string xrefNote = xref.find(book.bookCode+" "+cvMark);
									if ((xrefNote != null) && (xrefNote != ""))
									{
										StartUSFXElement("generated");
										if (customXrefMark)
											caller = xrefMark.Marker();
										else
											caller = "\ufeff";	// zero-width non-breaking space
										StartXref(caller, "Crossreference");
										if (addRefToXrefNote)
										{
											WriteWordMLTextRun(cvMark+" ", "Footnotesource");
										}
										StartFootnoteCharStyle("Footnote");
										WriteWordMLTextRun(xrefNote, "Crossreference");
										EndXref();
										EndUSFXElement();	// generated
									}
								}
								try
								{
									if ((bkRec.numChapters != 1) &&
										(Int32.Parse(sf.attribute) >=  bkInfo.bookArray[bknum].verseCount[currentChapter]))
									{
										StartUSFXElement("generated");
										WriteWordMLTextRun(chapterMark, "cvmarker");
										WriteWordMLTextRun(" ", "Hidden");
										EndUSFXElement();	//generated
									}
								}
								catch
								{
									// Ignore error; probably due to non-numeric verse number
								}
								break;
							case "nb":
								if (embedUsfx)
									StartUSFXParagraph(sf.tag, sf.level, sf.info.paragraphStyle, null);
								if (inPara)
								{
									WriteWordMLTextRun(sf.text);
								}
								else
									StartWordMLParagraph(currentParagraphStyle, sf.text, currentCharacterStyle, sf.tag, sf.level, sf.info.paragraphStyle);
								break;
							case "id":
								if (embedUsfx)
								{
									EndWordMLParagraph();
									StartUSFXElement(sf.tag, "id", sf.attribute, null);
									WriteWordMLParagraph(sf.info.paragraphStyleID(), sf.text.Trim());
									EndWordMLParagraph();
									EndUSFXElement();
								}
								break;
							case "ide":
								if (embedUsfx)
								{
									EndWordMLParagraph();
									StartUSFXElement(sf.tag, "charset", sf.attribute, null);
									s = sf.text.Trim();
									if (s != "")
									{
										WriteWordMLParagraph(sf.info.paragraphStyleID(), s);
										EndWordMLParagraph();
									}
									EndUSFXElement();
								}
								break;
							case "rem":
                            case "periph":
                                if (embedUsfx)
                                {
                                    StartUSFXElement(sf.tag);
                                    WriteWordMLParagraph(sf.info.paragraphStyleID(), sf.text.Trim());
                                    EndUSFXElement();
                                }
                                break;
							case "h":
							case "e":
								if (embedUsfx)
								{
									EndWordMLParagraph();
									EndUSFXParagraph();
									StartUSFXElement(sf.tag);
									WriteWordMLParagraph(sf.info.paragraphStyleID(), sf.text.Trim());
									EndWordMLParagraph();
									EndUSFXElement();
								}
								break;
							case "cl":
								if (embedUsfx)
								{
									EndWordMLParagraph();
									StartUSFXElement(sf.tag, null, null, sf.text);
									EndUSFXElement();
								}
								break;
                            case "toc":
                                // TODO: store metadata and do something useful with it.
                                break;
							default:
                                badsf = "\\" + sf.tag;
                                if (sf.level > 0)
                                    badsf += sf.level.ToString();
                                if (sf.attribute != "")
                                    badsf += " " + sf.attribute;
                                Logit.WriteError("ERROR: UNRECOGNIZED TAG " + badsf + " IN " + book.bookCode + " " + chapterMark + ":" + verseMark);
                                fatalError = true;
                                WriteUSFXMilestone("milestone", sf.tag, sf.level, sf.attribute, null);
                                WriteWordMLTextRun(sf.text);
                                sfmErrorsFound = true;
								break;
						}
							break;
						case "note":
						switch (sf.tag)
						{
							case "f":	// Regular footnote
								StartUSFXNote(sf.tag, sf.attribute, null);
								if (customFootnoteMark && (sf.attribute == "+"))
									caller = footnoteMark.Marker();
								else if (sf.attribute == "-")
									caller = "";
								else
									caller = sf.attribute;
								StartFootnote(caller, "Footnote");
								if (addRefToFootnote)
								{
									StartUSFXElement("generated");
									WriteWordMLTextRun(cvMark+" ", "Footnotesource");
									EndUSFXElement();	// generated
								}
								StartFootnoteCharStyle("Footnote");
								WriteWordMLTextRun(sf.text, "Footnote");
								break;
							case "x":	// Cross reference footnote
								StartUSFXNote(sf.tag, sf.attribute, null);
								if (customXrefMark && (sf.attribute == "+"))
									caller = xrefMark.Marker();
								else if (sf.attribute == "-")
									caller = "";
								else
									caller = sf.attribute;
								StartXref(caller, "Crossreference");
								if (addRefToXrefNote)
								{
									StartUSFXElement("generated");
									WriteWordMLTextRun(cvMark+" ", "Footnotesource");
									EndUSFXElement();	// generated
								}
								StartFootnoteCharStyle("Footnote");
								WriteWordMLTextRun(sf.text, "Crossreference");
								break;
							case "f*":
								EndFootnote();
								EndUSFXNote();
								WriteWordMLTextRun(sf.text);
								break;
							case "x*":
								EndXref();
								EndUSFXNote();
								WriteWordMLTextRun(sf.text);
								break;
							case "fe":	// End note
								if (inFootnote)
								{	// This test is for an error where someone used old PNGSFM syntax for ending a footnote.
									// This should never happen with real USFM, as end notes are not allowed in footnotes.
									// End notes are not recommended for use in minority language Bibles. I don't even like
									// them in majority language Bibles.
									EndUSFXNote();
									EndFootnote();
								}
								else
								{
									StartUSFXNote(sf.tag, sf.attribute, null);
									StartEndnote(sf.attribute);
								}
								WriteWordMLTextRun(sf.text);
								break;
							case "fe*":
								EndEndnote();
								EndUSFXNote();
								WriteWordMLTextRun(sf.text);
								break;
						}
							break;
						case "character":
							EndWordMLTextRun();
							if (sf.tag.EndsWith("*"))
							{
								if (inFootnote || inEndnote)
									EndFootnoteCharStyle();
								else
									PopCharStyle();
								if (sf.tag == "it*")
								{
									WriteField(@" ADVANCE \r 2 ");
								}
								if (inUSFXNote)
								{
									EndUSFXNoteStyle();
								}
								else
								{
									EndUSFXStyle();
								}
								WriteWordMLTextRun(sf.text);
							}
							else
							{
								if (!sf.info.nestingAllowed)
								{
									EndFootnoteCharStyle();
									if (inUSFXNote)
									{
										EndUSFXNoteStyle();
									}
								}
								if (inUSFXNote)
								{
									StartUSFXNoteStyle(sf.tag, null);
								}
								else
								{
									StartUSFXStyle(sf.tag, null);
								}
								if (inFootnote || inEndnote)
								{
									StartFootnoteCharStyle(sf.info.characterStyleID());
								}
								else if (sf.info.nestingAllowed)
								{
									PushCharStyle(sf.info.characterStyleID());
								}
								else
								{
									PopCharStyle();
									PushCharStyle(sf.info.characterStyleID());
								}
								WriteWordMLTextRun(sf.text);
							}
							break;
                        case "figure":
                            /* Disable doing anything with figures for now.
                            if (sf.tag == "fig")
                            {
                                fig = new Figure(sf.text);
                                PushCharStyle(sf.info.characterStyleID());
                                StartEmbedUSFXElement("fig", null, null, null, sf.info.characterStyleID());
                                StartEmbedUSFXElement("description", null, null, " " + fig.description + " ", "FigureDescription");
                                EndEmbedUSFXElement();  // description
                                StartEmbedUSFXElement("catalog", null, null, fig.catalog + "  ", "FigureFilename");
                                EndEmbedUSFXElement();  // catalog
                                StartEmbedUSFXElement("size", null, null, fig.size + "  ", "FigureSize");
                                EndEmbedUSFXElement();  // size
                                StartEmbedUSFXElement("location", null, null, fig.location + "  ", "FigureLocation");
                                EndEmbedUSFXElement();  // location
                                StartEmbedUSFXElement("copyright", null, null, fig.copyright + "  ", "FigureCopyright");
                                EndEmbedUSFXElement();  // copyright
                                StartEmbedUSFXElement("caption", null, null, fig.caption + "  ", "FigureCaption");
                                EndEmbedUSFXElement();  // caption
                                StartEmbedUSFXElement("reference", null, null, fig.reference + "  ", "FigureReference");
                                EndEmbedUSFXElement();  // reference
                                EndEmbedUSFXElement();  // fig
                                PopCharStyle();
                            }
                            else if (sf.tag == "fig*")
                            {
                                WriteWordMLTextRun(sf.text);
                            }
                            */
                            break;
                        /*							
                                                                    case "sidebar":
                                                                        break;
                                                                    case "dc":
                                                                        break;
                                                                    case "layout":
                                                                        break;
                                                                    case "index":
                                                                        break;
                                                                    case "table":
                                                                        break;
                                        */
						case "ignore":
							break;
						default:
                            badsf = "\\" + sf.tag;
                            if (sf.level > 0)
                                badsf += sf.level.ToString();
                            if (sf.attribute != "")
                                badsf += " " + sf.attribute;
                            Logit.WriteError("ERROR: UNRECOGNIZED TAG " + badsf + " IN " + book.bookCode + " " + chapterMark + ":" + verseMark);
                            fatalError = true;
							WriteUSFXMilestone("milestone", sf.tag, sf.level, sf.attribute, null);
                            WriteWordMLTextRun(sf.text);
                            sfmErrorsFound = true;
							break;
					}
					sf = book.NextSfm();
				}
				EndWordMLParagraph();
				if (embedUsfx)
				{
					EndUSFXParagraph();
					xw.WriteEndElement();	// ns+book
					// Logit.WriteLine("END OF BOOK "+book.bookCode);
				}
			}
		}

		protected void WriteUSFXBook(int bknum)
		{
            Figure fig = new Figure();
			book = books[bknum];
			if ((bkInfo.bookArray[bknum] != null) && (book != null))
			{
				inPsalms = (book.bookCode == "PSA");
				bkRec = (BibleBookRecord)bkInfo.bookArray[bknum];
				if (bkRec == null)
				{
					Logit.WriteError("ERROR: BibleBookInfo array item "+bknum.ToString()+" missing.");
                    fatalError = true;
					xw.WriteAttributeString("sortOrder", bknum.ToString());
				}
				sf = book.FirstSfm();
				xw.WriteStartElement(ns+"book");
				xw.WriteAttributeString("id", book.bookCode);
				// Logit.WriteLine("Starting book: "+book.vernacularName+" ("+book.bookCode+").");
				while (sf != null)
				{
					switch (sf.info.kind)
					{
						case "paragraph":
							StartUSFXParagraph(sf.tag, sf.level, sf.info.paragraphStyle, sf.text);
							break;
						case "meta":
						switch (sf.tag)
						{
							case "c":
								chapterMark = sf.attribute;
								verseMark = "";
								StartUSFXElement("c", "id", sf.attribute, null);
								EndUSFXElement();	// ns+c
								WriteUSFXText(sf.text);
								break;
                            case "ca":
                                StartUSFXElement("ca");
                                WriteUSFXText(sf.text);
                                break;
                            case "ca*":
                                EndUSFXElement();   // ca
                                break;
							case "v":
								StartUSFXElement(sf.tag, "id", sf.attribute, null);
								EndUSFXElement();	// ns+v
								WriteUSFXText(sf.text);
								break;
                            case "va":
                            case "vp":
                                StartUSFXElement(sf.tag, null, null, sf.text);
                                break;
                            case "va*":
                            case "vp*":
                                EndUSFXElement();
                                break;
							case "nb":
								StartUSFXParagraph(sf.tag, sf.level, sf.info.paragraphStyle, sf.text);
								break;
							case "id":
								StartUSFXElement(sf.tag, "id", sf.attribute, sf.text);
								EndUSFXElement();
								break;
                            case "toc":
                                StartUSFXElement(sf.tag, "level", sf.attribute, sf.text);
                                EndUSFXElement();
                                break;
                            case "ide":
								StartUSFXElement(sf.tag, "charset", sf.attribute, sf.text.Trim());
								EndUSFXElement();
								break;
							case "rem":
                            case "periph":
                                StartUSFXElement(sf.tag, null, null, sf.text);
                                EndUSFXElement();
                                break;
                            case "sts":
							case "h":
							case "e":
                            case "cp":  // Published chapter designator (not necessarily Arabic numerals)
                            case "cl":  // Vernacular name for "Chapter"
                                    // EndUSFXParagraph();
									StartUSFXElement(sf.tag, null, null, sf.text);
									EndUSFXElement();
								break;
							default:
                                // in WriteUSFXBook
								WriteUSFXMilestone("milestone", sf.tag, sf.level, sf.attribute, sf.text);
                                if (inFootnote)
                                {
                                    Logit.WriteError("Error: unclosed footnote at " + book.bookCode + " " + chapterMark +
                                        ":" + verseMark);
                                    fatalError = true;
                                }
								break;

						}
							break;
						case "note":
						switch (sf.tag)
						{
							case "f":	// Regular footnote
							case "x":	// Cross reference footnote
								StartUSFXNote(sf.tag, sf.attribute, sf.text);
								break;
							case "fe":	// End note
								if (inUSFXNote)
								{	// This test is for an error where someone used old PNGSFM syntax for ending a footnote.
									// This should never happen with real USFM, as end notes are not allowed in footnotes.
									// End notes are not recommended for use in minority language Bibles. I don't even like
									// them in majority language Bibles.
									EndUSFXNote();
									WriteUSFXText(sf.text);
								}
								else
								{	// A real end note
									StartUSFXNote(sf.tag, sf.attribute, sf.text);
								}
								break;
							case "f*":
							case "fe*":
							case "x*":
								EndUSFXNote();
								WriteUSFXText(sf.text);
								break;
							default:
                               	WriteUSFXMilestone("milestone", sf.tag, sf.level, sf.attribute, sf.text);
								break;
						}
							break;
						case "character":
							if (sf.tag.EndsWith("*"))
							{
								if (inUSFXNote)
								{
									EndUSFXNoteStyle();
								}
								else
								{
									EndUSFXStyle();
								}
								WriteUSFXText(sf.text);
							}
							else
							{
								if (inUSFXNote)
								{
									StartUSFXNoteStyle(sf.tag, sf.text);
								}
								else
								{
									StartUSFXStyle(sf.tag, sf.text);
								}
							}
							break;
                        case "figure":
                            if (sf.tag == "fig")
                            {
                                fig.figSpec = sf.text;
                                xw.WriteStartElement("fig");
                                xw.WriteElementString("description", fig.description);
                                xw.WriteElementString("catalog", fig.catalog);
                                xw.WriteElementString("size", fig.size);
                                xw.WriteElementString("location", fig.location);
                                xw.WriteElementString("copyright", fig.copyright);
                                xw.WriteElementString("caption", fig.caption);
                                xw.WriteElementString("reference", fig.reference);
                                xw.WriteEndElement();   // fig
                            }
                            else if (sf.tag == "fig*")
                            {
                                if (sf.text.Length > 0)
                                    xw.WriteString(sf.text);
                            }
                            else
                            {
                                Logit.WriteError("ERROR: INVALID FIGURE TAG \\" + sf.tag);
                                fatalError = true;
                                xw.WriteString("\\" + sf.tag + " " + sf.text);
                            }
                            break;
                        /*							
                                                                    case "sidebar":
                                                                        break;
                                                                    case "dc":
                                                                        break;
                                                                    case "layout":
                                                                        break;
                                                                    case "index":
                                                                        break;
                                                                    case "table":
                                                                        break;
                                        */
						case "ignore":
							break;
						default:
							WriteUSFXMilestone("milestone", sf.tag, sf.level, sf.attribute, sf.text);
							break;
					}
					sf = book.NextSfm();
				}
				EndUSFXParagraph();
				xw.WriteEndElement();	// ns+book
				// Logit.WriteLine("END OF BOOK "+book.bookCode);
			}
		}

		public int ParseInt(string s, int defaultValue)
		{
			try
			{
				return int.Parse(s);
			}
			catch
			{
				return defaultValue;
			}
		}

/* The following test proceedure is the simple framework upon which the 
 * second pass of WriteToWordML is built.
 * It is just an example of how to reliably read from one XML document and
 * write to another, pretty-printing it in the process. (You could remove
 * the indentation formatting from this sample method for more space
 * efficiency at the expense of human readability.)
 * 
		public void CloneXmlFile()
		{
			string fileName = @"c:\sil\test\testout.xml";

			try
			{
				xr = new XmlTextReader(SFConverter.jobIni.ReadString("templateName",
					Path.Combine(Path.GetDirectoryName(XMLini.ExecutableName()), "Scripture.xml")));
				xw = new XmlTextWriter(fileName, System.Text.Encoding.UTF8);
				xw.Formatting = System.Xml.Formatting.Indented;
				xw.Indentation = 1;
				xw.IndentChar = ' ';
				xr.Read();
				while (!xr.EOF)
				{
					Logit.WriteLine("Seed node type="+xr.NodeType.ToString()+" Name="+xr.Name+" Value="+xr.Value); // DEBUG
					switch (xr.NodeType)
					{
						case XmlNodeType.Element:
							xw.WriteStartElement(xr.Name);
							xw.WriteAttributes(xr, true);
							if (xr.IsEmptyElement)
								xw.WriteEndElement();
							break;
						case XmlNodeType.EndElement:
							xw.WriteEndElement();
							break;
						case XmlNodeType.Text:
							xw.WriteString(xr.Value);
							break;
						case XmlNodeType.SignificantWhitespace:
							xw.WriteWhitespace(xr.Value);
							break;
						case XmlNodeType.Whitespace:
							// You could insert xw.WriteWhitespace(xr.Value); to preserve
							// notsingnificant whites space, but why?
							break;
						case XmlNodeType.Attribute:
							xw.WriteAttributeString(xr.Name, xr.Value);
							break;
						case XmlNodeType.ProcessingInstruction:
							xw.WriteProcessingInstruction(xr.Name, xr.Value);
							break;
						case XmlNodeType.XmlDeclaration:
							xw.WriteStartDocument(true);
							break;
						default:
							Logit.WriteLine("Doing NOTHING with type="+xr.NodeType.ToString()+" Name="+xr.Name+" Value="+xr.Value); // DEBUG
							break;
					}
					if (!xr.EOF)
						xr.Read();
				}
				xw.Close();
				xr.Close();
				Logit.WriteLine(fileName+" written.");
			}
			catch (System.Exception ex)
			{
				Logit.WriteLine(ex.ToString());
				Logit.WriteLine("Failed to parse seed file "+templateName);
				return;
			}
		}
*/

		public void WriteToWordML(string fileName)
		{
			inserted = false;
            fatalError = false;
			chapterLabel = "";
			templateName = SFConverter.jobIni.ReadString("templateName",
				Path.Combine(Path.GetDirectoryName(XMLini.ExecutableName()), "Scripture.xml"));
			Logit.WriteLine("Reading template "+templateName);
			currentParagraphStyle = "Normal";
			ns = "ns0:";

			// Read in crossreference list, if the user wants to use it.

			if (mergeXref)
			{
				try
				{
					Logit.WriteLine("Reading "+xrefName);
					xref = new CrossReference(xrefName);
				}
				catch (System.Exception ex)
				{
					Logit.WriteError(ex.ToString());
					Logit.WriteLine("Failed to read crossreferences from "+xrefName);
					return;
				}
			}

			bool stillLooking = true;
			int lineSpacing = 0;
			int sz = 0;
			int before = 0;
			int chSpacing = 0;
			int chsz = 0;
			int chBefore = 0;
			double lineHeight = 12.0;
			Logit.WriteLine("Reading parameters from "+templateName);
			try
			{
				xr = new XmlFileReader(templateName);
				xr.Read();
				while (!xr.EOF)
				{
					xr.Read();
					if (xr.NodeType == XmlNodeType.Element)
					{
						if ((xr.Name == "w:style") && (xr.HasAttributes))
						{
							xr.MoveToAttribute("w:styleId");
							if (xr.Value == "Normal")
							{
								xr.MoveToElement();
								xr.Read();
								while (stillLooking && !xr.EOF)
								{
									xr.Read();
									if ((xr.NodeType == XmlNodeType.Element) && (xr.HasAttributes))
									{
										switch (xr.Name)
										{
											case "w:spacing":
												if (xr.MoveToAttribute("w:line"))
													lineSpacing = ParseInt(xr.Value, 0);
												if (xr.MoveToAttribute("w:before"))
													before = ParseInt(xr.Value, 0);
												// Logit.WriteLine("Normal lineSpacing="+lineSpacing.ToString()+" before="+before.ToString());
												break;
											case "w:sz":
												if (xr.MoveToAttribute("w:val"))
													sz = ParseInt(xr.Value, 0);
												stillLooking = false;
												break;
										}
									}
									else if ((xr.NodeType == XmlNodeType.EndElement) && (xr.Name == "w:style"))
										stillLooking = false;
								}
								if (chSpacing + chBefore == 0)
									stillLooking = true;
							}
							else if (xr.Value == "Normal-ch")
							{
								xr.MoveToElement();
								xr.Read();
								while (stillLooking && !xr.EOF)
								{
									xr.Read();
									if ((xr.NodeType == XmlNodeType.Element) && (xr.HasAttributes))
									{
										switch (xr.Name)
										{
											case "w:spacing":
												if (xr.MoveToAttribute("w:line"))
													chSpacing = ParseInt(xr.Value, 0);
												if (xr.MoveToAttribute("w:before"))
													chBefore = ParseInt(xr.Value, 0);
												// Logit.WriteLine("Normal-ch chSpacing="+chSpacing.ToString()+" chBefore="+chBefore.ToString());
												break;
											case "w:sz":
												if (xr.MoveToAttribute("w:val"))
													chsz = ParseInt(xr.Value, 0);
												// Logit.WriteLine("Normal-ch chsz="+chsz.ToString());
												stillLooking = false;
												break;
										}
									}
									else if ((xr.NodeType == XmlNodeType.EndElement) && (xr.Name == "w:style"))
										stillLooking = false;
								}
								if (sz == 0)
									stillLooking = true;
							}
						}
						else if ((xr.Name == "w:pgSz") && (xr.HasAttributes))
						{
							xr.MoveToAttribute("w:w");
							seedPaperWidth = new LengthString(ParseInt(xr.Value, 12242));
							xr.MoveToAttribute("w:h");
							seedPaperHeight = new LengthString(ParseInt(xr.Value, 15842));
						}
					}
				}
				xr.Close();
			}

			catch (System.Exception ex)
			{
				Logit.WriteError(ex.ToString());
				Logit.WriteLine("Failed to parse seed file "+templateName);
				return;
			}
			if (sz > 0)
			{
				lineHeight = 0.55 * sz;
				// Logit.WriteLine("Normal sz lineHeight="+lineHeight.ToString("f1"));
			}
			if (chsz > 0)
			{
				lineHeight = 0.55 * chsz;
				// Logit.WriteLine("Normal-ch sz lineHeight="+lineHeight.ToString("f1"));
			}
			if (lineSpacing > 0)
			{
				lineHeight = lineSpacing / 20.0;
				// Logit.WriteLine("Normal lineSpacing lineHeight="+lineHeight.ToString("f1"));
			}
			if (chSpacing > 0)
			{
				lineHeight = chSpacing / 20.0;
				// Logit.WriteLine("Normal-ch lineSpacing lineHeight="+lineHeight.ToString("f1"));
			}
			if (before > 0)
			{
				dropCapBefore.Twips = before;
				// Logit.WriteLine("Normal before="+dropCapBefore.Points.ToString("f1"));
			}
			if (chBefore > 0)
			{
				dropCapBefore.Twips = chBefore;
				// Logit.WriteLine("Normal-ch before="+dropCapBefore.Points.ToString("f1"));
			}
			if (autoCalcDropCap)
			{
				horizFromText.Points = 0.3 * lineHeight;
				dropCapSpacing.Points = 2.0 * lineHeight;
				dropCapSize.Points = 2.2 * lineHeight;
				dropCapPosition.Points = -0.25-(0.1 * dropCapSize.Points);
				Logit.WriteLine("Normal line spacing is "+lineHeight.ToString("f1")+
					" points; space before is "+dropCapBefore.Points.ToString("f1")+
					" points.");
			}

			try
			{
				xr = new XmlFileReader(templateName);
			}
			catch (System.Exception ex)
			{
				Logit.WriteError(ex.ToString());
				Logit.WriteLine("Failed to open seed file "+templateName);
				return;
			}

			try
			{
				bool needsFooter = true;
				bool oneshot = true;
				xw = new XmlTextWriter(fileName, System.Text.Encoding.UTF8);
				xw.Formatting = System.Xml.Formatting.None;
				xw.Indentation = 1;
				xw.IndentChar = ' ';
				xr.Read();
				while (!xr.EOF)
				{
					if (xr.NodeType == XmlNodeType.Element)
					{
						if (xr.Name == "w:wordDocument")
						{
							xw.WriteStartElement(xr.Name);
							for (int i = 0; i < xr.AttributeCount; i++)
							{
								xr.MoveToAttribute(i);
								if (xr.Name != "xmlns:ns0")
									xw.WriteAttributeString(xr.Name, xr.Value);
							}
							xw.WriteAttributeString("xmlns:ns0", "http://ebible.org/usfx/usfx-2009-07-12.xsd");
							xr.MoveToElement();
						}
						else if (xr.Name == "w:ftr")
						{
							oneshot = true;
							xw.WriteStartElement(xr.Name);
							xw.WriteAttributes(xr, true);
						}
						else if (xr.Name == "w:p")
						{
							xw.WriteStartElement(xr.Name);
							xw.WriteAttributes(xr, true);
						}
						else if (xr.Name == "w:sectPr")
						{	// Insert converted text at the end of the
							// first section of the seed document.
							if (!inserted)
							{
								//	Logit.WriteLine("Inserting Scripture text here. -------------------"); // DEBUG
								int j;
								inserted = true;
								usfxNestLevel = 0;
								inPara = false;
								inRun = false;
								WriteWordMLParagraph("Blank", " ", "", null, 0, null);
								EndWordMLParagraph();
								if (embedUsfx)
								{
									xw.WriteStartElement(ns+"usfx");
								}
								if (isNTPP)
								{
									for (j = 64; j < BibleBookInfo.MAXNUMBOOKS; j++)
										WriteWordMLBook(j);
									WriteWordMLBook(19);
									WriteWordMLBook(20);
								}
								else
								{
									for (j = 0; j < BibleBookInfo.MAXNUMBOOKS; j++)
									{
										WriteWordMLBook(j);
									}
								}
							}
							xw.WriteStartElement(xr.Name);
							xw.WriteAttributes(xr, true);
							needsFooter = true;
						}
						else if (includeCropMarks && oneshot)
						{
							if (xr.NodePathContains("/w:sectPr/w:ftr/w:p/") && !xr.NodePathContains("/w:pPr/"))
							{
								Logit.WriteLine("Drawing crop marks in existing footer.");
								DrawCropMarks(seedPaperWidth, seedPaperHeight, croppedPageWidth, croppedPageLength);
								oneshot = false;
								needsFooter = false;
							}
							else if (needsFooter && xr.NodePathContains("/w:sectPr/") &&
								!xr.NodePathContains("/w:hdr/") && !xr.NodePathContains("/w:ftr/"))
							{
								Logit.WriteLine("Drawing crop marks in new footer.");
								InsertCropMarkedFooter("even");
								InsertCropMarkedFooter("odd");
								InsertCropMarkedFooter("first");
								needsFooter = false;
							}
							xw.WriteStartElement(xr.Name);
							xw.WriteAttributes(xr, false);
						}
						else
						{
							xw.WriteStartElement(xr.Name);
							xw.WriteAttributes(xr, false);
						}
						if (xr.IsEmptyElement)
							xw.WriteEndElement();
					}
					else if (xr.NodeType == XmlNodeType.EndElement)
					{
						if (includeCropMarks && needsFooter)
						{
							if (xr.Name == "w.sectPr")
							{	// This code is unlikely to be executed.
								Logit.WriteLine("Drawing crop marks into new footer."); // DEBUG
								InsertCropMarkedFooter("even");
								InsertCropMarkedFooter("odd");
								InsertCropMarkedFooter("first");
								needsFooter = false;
							}
						}
						xw.WriteEndElement();
					}
					else
					{
						xr.CopyNode(xw);
					}
					if (!xr.EOF)
						xr.Read();
				}
				xw.Close();
				xr.Close();
				Logit.WriteLine(fileName+" written.");
			}
			catch (System.Exception ex)
			{
				Logit.WriteError(ex.ToString());
				Logit.WriteLine("Failed to write to output file "+fileName);
				xr.Close();
				try
				{
					xw.Close();
				}
				catch
				{
					// Do nothing.
				}
				return;
			}
		}

        public string languageCode = "";

		public void WriteUSFX(string fileName)
		{
			int j;

			ns = SFConverter.jobIni.ReadString("nameSpace", "");
			ns.Trim();
			if ((ns.Length > 0) && (ns[ns.Length-1] != ':'))
				ns = ns + ":";
			xw = null;
			try
			{
				xw = new XmlTextWriter(fileName, System.Text.Encoding.UTF8);
				xw.Namespaces = true;
                xw.Formatting = Formatting.None;

				xw.WriteStartDocument();
				usfxNestLevel = 0;
				inUsfxParagraph = false;
				usfxStyleCount = 0;
				xw.WriteStartElement(ns+"usfx");
				xw.WriteAttributeString("xmlns:ns0", @"http://eBible.org/usfx/usfx-2009-07-12.xsd");
				xw.WriteAttributeString("xmlns:xsi", @"http://www.w3.org/2001/XMLSchema-instance");
				xw.WriteAttributeString("xsi:noNamespaceSchemaLocation", @"usfx-2009-07-12.xsd");
                if (languageCode.Length == 3)
                    xw.WriteElementString("languageCode", languageCode);
				for (j = 0; j < BibleBookInfo.MAXNUMBOOKS; j++)
				{
					WriteUSFXBook(j);
				}
				xw.WriteEndElement();	// ns+usfx
				xw.WriteEndDocument();
				xw.Close();
//				Logit.WriteLine(fileName+" written.");
			}
			catch (System.Exception ex)
			{
				Logit.WriteError(ex.ToString());
				Logit.WriteLine("Failed to write USFX to output file "+fileName);
			}
		}

		public static string StripNS(string name)
		{
			int i = name.IndexOf(":")+1;
			if (i > 0)
			{
				return name.Substring(i, name.Length - i);
			}
			else
				return name;
		}		

		public void ExtractUSFX(string inFileName, string outFileName)
		{
			ns = "ns";
			bool includeThis = false;
			bool inTextRun = false;
			bool inBody = false;
			bool inSection = false;
			try
			{
				XmlFileReader xr = new XmlFileReader(inFileName);
				Logit.WriteLine("Reading "+inFileName);
				XmlTextWriter xw = new XmlTextWriter(outFileName, System.Text.Encoding.UTF8);
				Logit.WriteLine("Writing "+outFileName);
				xw.Namespaces = true;
				xw.Formatting = Formatting.None;

				xw.WriteStartDocument();
				Logit.WriteLine("Writing USFX data only to "+inFileName);
				usfxNestLevel = 0;
				inUsfxParagraph = false;
				usfxStyleCount = 0;

				xr.Read();
				xr.MoveToContent();
				while (!xr.EOF)
				{
					xr.Read();
					if (xr.NodeType == XmlNodeType.Element)
					{
						if (xr.Name.StartsWith(ns) && xr.Name.EndsWith(":usfx"))
						{
							xw.WriteStartElement("usfx");
							xw.WriteAttributeString("xmlns:xsi", @"http://www.w3.org/2001/XMLSchema-instance");
							xw.WriteAttributeString("xsi:noNamespaceSchemaLocation", @"usfx-2009-07-12.xsd");
							inBody = true;
						}
						else if (xr.Name.StartsWith(ns))
						{
							if (xr.Name.EndsWith(":generated"))
							{
								includeThis = false;
							}
							else if ((xr.Name.EndsWith(":v")) || (xr.Name.EndsWith(":c")))
							{
								includeThis = false;
								xw.WriteStartElement(StripNS(xr.Name));
								xw.WriteAttributes(xr, true);
								if (xr.IsEmptyElement)
									xw.WriteEndElement();
							}
							else
							{
								includeThis = true;
								xw.WriteStartElement(StripNS(xr.Name));
								xw.WriteAttributes(xr, true);
								if (xr.IsEmptyElement)
									xw.WriteEndElement();
							}
						}
						else if (xr.Name == "wx:sect")
						{
							inSection = true;
						}
						else if (xr.Name == "w:sectPr")
						{
							inSection = false;
						}
						else if ((xr.Name == "w:t") || (xr.Name.StartsWith(ns) && xr.Name.EndsWith(":cl")))
							inTextRun = true;
					}
					else if (xr.NodeType == XmlNodeType.EndElement)
					{
						if (xr.Name.StartsWith(ns))
						{
							if (xr.Name.StartsWith(ns) && xr.Name.EndsWith(":generated"))
							{
								includeThis = true;
							}
							else if (xr.Name.StartsWith(ns) &&
								(xr.Name.EndsWith(":v") || xr.Name.EndsWith(":c")))
							{
								includeThis = true;
								xw.WriteEndElement();
							}
							else
							{
								xw.WriteEndElement();
								if (xr.Name.EndsWith(":usfx"))
								{
									inBody = false;
								}

							}
						}
						else if (xr.Name == "wx:sect")
						{
							inSection = false;
						}
						else if ((xr.Name == "w:t") || (xr.Name == ns+"cl"))
							inTextRun = false;
					}
					else if (inBody && inTextRun && includeThis && inSection)
					{
						if (xr.NodeType == XmlNodeType.Text)
                            xw.WriteString(xr.Value);
						else if (xr.NodeType == XmlNodeType.SignificantWhitespace)
							xw.WriteWhitespace(xr.Value);
					}
				}
				xr.Close();
				xw.WriteEndDocument();
				xw.Close();
			}
			catch (System.Exception ex)
			{
				Logit.WriteLine(ex.ToString());
			}
		}

        public void USFXtoUSFM(string inFileName, string outDir, string outFileName)
		{
			int i;
			stringPair tagPair;
			int quoteLevel = 0;
			string bookId = "000-aaa";
			string chapter = "0";
			string verse = "0";
			string id = "";
			string sfm = "";
			string style = "";
			string level = "";
			string who = "";
			string s = "";
			Stack sfmPairStack = new Stack();
			bool firstCol = true;
			bool ignore = false;
			bool afterBlankLine = false;
			SFWriter usfmFile;
            Figure fig = new Figure();

			Logit.WriteLine("inFileName="+inFileName+"  outDir="+outDir+"  outFileName="+outFileName);

			try
			{
				XmlFileReader usfxFile = new XmlFileReader(inFileName);
                if (!((usfxFile.MoveToContent() == XmlNodeType.Element) && (usfxFile.Name == "usfx")))
                {
                    // Not a USFX file. Skip it.
                    Logit.WriteError(inFileName + " is not a USFX file.");
                    usfxFile.Close();
                    return;
                }
                if (!Directory.Exists(outDir))
                {
                    Directory.CreateDirectory(outDir);
                }
				usfmFile = new SFWriter();
				while (!usfxFile.EOF)
				{
					usfxFile.Read();
					if (usfxFile.NodeType == XmlNodeType.Element)
					{
						// The SFM name is the element name UNLESS sfm attribute overrides it.
						sfm = usfxFile.Name;
						id = level = style = who = "";
						if (usfxFile.HasAttributes)
						{
							for (i = 0; i < usfxFile.AttributeCount; i++)
							{
								usfxFile.MoveToAttribute(i);
								switch (usfxFile.Name)
								{
									case "id":
										id = usfxFile.Value;
										break;
									case "charset":
										id = usfxFile.Value;
										break;
									case "level":
										level = usfxFile.Value;
										break;
									case "sfm":
										sfm = usfxFile.Value;
										break;
									case "style":
										style = usfxFile.Value;
										break;
									case "attribute":
										id = usfxFile.Value;
										break;
									case "caller":
										// footnote caller, sfm attribute
										id = usfxFile.Value;
										break;
									case "who":
										// makes sense with <quoteStart>
										who = usfxFile.Value;
										break;
									case "xmlns:ns0":
									case "xmlns:xsi":
									case "xsi:noNamespaceSchemaLocation":
										// ignore these
										break;
									default:
										Logit.WriteError("Unrecognized attribute: "+usfxFile.Name+"="+usfxFile.Value);
										break;
								}
							}
							usfxFile.MoveToElement();
						}
						firstCol = !tags.info(sfm).hasEndTag();
						if (!firstCol)
						{
							tagPair = new stringPair();
							tagPair.old = usfxFile.Name;
							tagPair.niu = sfm+"*";
							sfmPairStack.Push(tagPair);
						}

						switch (usfxFile.Name)
						{
							case "book":
								// Open USFM file if we have an ID
								if (id != "")
								{
									bookId = id;
									usfmFile.Open(Path.Combine(outDir, bkInfo.FilePrefix(bookId)+outFileName));
								}
								break;
							case "id":
								// Open USFM file if it is not open already
								if (((id != "") && (id != bookId)) || !usfmFile.Opened())
								{
									bookId = id;
									usfmFile.Open(Path.Combine(outDir, bkInfo.FilePrefix(bookId)+outFileName));
								}
								usfmFile.WriteSFM("id", "", id, true);
								break;
							case "c":
								chapter = id;
								usfmFile.WriteSFM(sfm, "", id, true);
								if (!usfxFile.IsEmptyElement)
								{
									ignore = true;
								}
								break;
							case "v":
								verse = id;
								usfmFile.WriteSFM(sfm, "", id, true);
								if (!usfxFile.IsEmptyElement)
								{
									ignore = true;
								}
								break;
							case "fig":
								if (!usfxFile.IsEmptyElement)
								{
									ignore = true;
								}
								usfmFile.WriteSFM(sfm, "", "", false);
								break;
							case "generated":
								if (!usfxFile.IsEmptyElement)
								{
									ignore = true;
								}
								break;
							case "table":
								// Do nothing... the table row and column elements are enough.
								break;
							case "quoteStart":
								quoteLevel++;
								/* Do nothing. Read the quotation mark from the element contents.
								if (!usfxFile.IsEmptyElement)
								{
									ignore = true;
								}
								// The following is an ethnocentric simplification of
								// English rules of punctuation.
								// Warning: results may be wrong if you mix
								// markers and actual quotation marks.
								if ((quoteLevel % 2) == 1)
                                    usfmFile.WriteString("“");
								else
									usfmFile.WriteString("‘");
								*/
								break;
							case "quoteRemind":
								/* Do nothing. Read the quotation mark from the element contents.
								if (!usfxFile.IsEmptyElement)
								{
									ignore = true;
								}
								*/
								break;
							case "quoteEnd":
								/* Do nothing. Read the quotation mark from the element contents.
								if (!usfxFile.IsEmptyElement)
								{
									ignore = true;
								}
								// The following is an ethnocentric simplification of
								// English rules of punctuation.
								if ((quoteLevel % 2) == 1)
									usfmFile.WriteString("”");
								else
									usfmFile.WriteString("’");
								*/
								quoteLevel--;
								if (quoteLevel < 0)
								{
									Logit.WriteLine("Warning: unmatched quoteEnd at "+
										bookId+" "+chapter+":"+verse);
									quoteLevel = 0;
								}
								break;
							case "p":
								if (quoteLevel > 0)
								{
									for (i = quoteLevel; i > 0; i--)
									{
										if ((i % 2) == 1)
											usfmFile.WriteString("“");
										else
											usfmFile.WriteString("‘");
									}
								}
								usfmFile.WriteSFM(sfm, level, id, !tags.info(sfm).hasEndTag());
								afterBlankLine = true;
								break;
							case "b":
								afterBlankLine = true;
								usfmFile.WriteSFM(sfm, level, id, !tags.info(sfm).hasEndTag());
								break;
							case "q":
								if (afterBlankLine)
								{
									if (quoteLevel > 0)
									{
										for (i = quoteLevel; i > 0; i--)
										{
											if ((i % 2) == 1)
												usfmFile.WriteString("“");
											else
												usfmFile.WriteString("‘");
										}
									}
								}
								else
								{
									afterBlankLine = true;
								}
								usfmFile.WriteSFM(sfm, level, id, !tags.info(sfm).hasEndTag());
								break;
                            case "languageCode":
                                usfxFile.Read();
                                if (usfxFile.NodeType == XmlNodeType.Text)
                                    languageCode = usfxFile.Value;
                                break;
                            default:
								usfmFile.WriteSFM(sfm, level, id, !tags.info(sfm).hasEndTag());
								break;
						}
					}
					else if (usfxFile.NodeType == XmlNodeType.EndElement)
					{
						switch (usfxFile.Name)
						{
							case "book":
								// close output file.
								usfmFile.Close();
								bookId = "";
								break;
							case "description":
								fig.description = s;
								break;
							case "catalog":
								fig.catalog = s;
								break;
							case "size":
                                fig.size = s;
								break;
							case "location":
                                fig.location = s;
								break;
							case "copyright":
                                fig.copyright = s;
								break;
							case "caption":
                                fig.caption = s;
								break;
							case "reference":
                                fig.reference = s;
								break;
							case "fig":
								ignore = false;
								usfmFile.WriteString(fig.figSpec);
								if (sfmPairStack.Count > 0)
								{
									tagPair = (stringPair)sfmPairStack.Peek();
									if (usfxFile.Name == tagPair.old)
									{
										usfmFile.WriteSFM(tagPair.niu, "", "", false);
										sfmPairStack.Pop();
									}
								}
								break;
							case "c":
							case "v":
							case "generated":
								ignore = false;
								break;
							case "quoteStart":
							case "quoteRemind":
							case "quoteEnd":
								// Do nothing.
								break;
							default:
								if (sfmPairStack.Count > 0)
								{
									tagPair = (stringPair)sfmPairStack.Peek();
									if (usfxFile.Name == tagPair.old)
									{
										usfmFile.WriteSFM(tagPair.niu, "", "", false);
										sfmPairStack.Pop();
									}
								}
								break;
						}
					}
					else if (usfxFile.NodeType == XmlNodeType.Text)
					{
						s = usfxFile.Value;
						if (!ignore)
						{
							afterBlankLine = false;
							usfmFile.WriteString(usfxFile.Value);
						}
					}
					else if (usfxFile.NodeType == XmlNodeType.SignificantWhitespace)
					{
						usfmFile.WriteString(" ");
					}
				}
                usfmFile.Close();
				usfxFile.Close();
			}
			catch (System.Exception ex)
			{
				Logit.WriteError("Conversion of USFX file "+inFileName+" to USFM files in "+outDir+" (named"+outFileName+") FAILED.");
				Logit.WriteLine(ex.ToString());
			}
		}

		int[] eSwordBookNumber = new int[]
			{
				0, 1, 2, 3, 4, 5, 6, 7, 8, 9,
				10, 11, 12, 13, 14, 15, 16, 17, 18, 19,
				20, 21, 22, 23, 24, 25, 26, 27, 28, 29,
				30,	31, 32, 33, 34, 35, 36, 37, 38, 39,
				67, // Tobit
				68, // Judith
				0, // GkEsther
				69, // Wisdom
				70, // Sirach
				71, // Baruch
				0, // Let
				0, // Azar
				0, // Susanna
				0, // Bel
				72, // 1 Mac
				73, // 2 Mac
				0, // 1 Esdras
				0, // Man
				0, // P1
				0, // 3 Mac
				0, // 2 Esdras
				0, // 4 Mac
				0, // AddDan
				0, 0, 0, 0, 0,
				40, 41, 42, 43, 44, 45, 46, 47, 48, 49,
				50, 51, 52, 53, 54, 55, 56, 57, 58, 59,
				60, 61, 62, 63, 64, 65, 66, 0
			};

		public static string unicode2rtf(string s)
		{
			StringBuilder sb = new StringBuilder();
			int i, c;
			for (i = 0; i < s.Length; i++)
			{
				c = (int)s[i];
				if (c == '“')
					sb.Append(@"\ldblquote ");
				else if (c == '‘')
					sb.Append(@"\lquote ");
				else if (c == '’')
					sb.Append(@"\rquote ");
				else if (c == '”')
					sb.Append(@"\rdblquote ");
				else if (c == '—')
					sb.Append(@"\emdash ");
				else if (c > 127)
				{
					sb.Append(@"\u"+c.ToString()+" ");
				}
				else
				{
					sb.Append(s[i]);
				}
			}
			return sb.ToString();
		}

		public void USFXtoeSword(string inFileName, string outDir, string outFileName)
		{
			int i;
			int quoteLevel = 0;
			int bookIDNum = 0;
			int chapNum = 1;
			int verseNum = 1;
			int footnoteStartVerse = 127;	// First verse of this chapter with a footnote if <= footnoteEndVerse
			int footnoteEndVerse = 0;	// Last verse of this chapter with a footnote if > 0
			// int footnoteNumber = 0;	// Number of footnotes past the beginning of the chapter
			int footnoteCounter = 0;	// increments with every chapter containing at least one footnote
			int footnoteBook = 0;	// Book number this note set pertains to
			int footnoteChap = 0;	// Chapter number this note set pertains to
			int autoincrement = 0;	// verses past the beginning of the Bible
			string footnotes = "";
			string chapter = "0";
			string verse = "0";
			string id = "";
			string sfm = "";
			string style = "";
			string level = "";
			string who = "";
			string s = "";
			string description = "";
			string catalog = "";
			string size = "";
			string location = "";
			string copyright = "";
			string caption = "";
			string reference = "";
			TextAttributeTracker textAttributes = new TextAttributeTracker();
			bool ignore = false;
			bool inFootnote = false;
			bool firstLine = true;
			StreamWriter bblFile;	// Bible text
			StreamWriter cmtFile;	// Footnotes as comments
			
			Logit.WriteLine("inFileName="+inFileName+"  outDir="+outDir+"  outFileName="+outFileName);

			try
			{
				XmlFileReader usfxFile = new XmlFileReader(inFileName);
				if (!Directory.Exists(outDir))
				{
					Directory.CreateDirectory(outDir);
				}
                Encoding WesternEuropean = Encoding.GetEncoding(1252);
				bblFile = new StreamWriter(Path.Combine(outDir, outFileName + "_bbl.csv"), false, WesternEuropean);
				cmtFile = new StreamWriter(Path.Combine(outDir, outFileName + "_cmt.csv"), false, WesternEuropean);
				usfxFile.Read();
				while (!usfxFile.EOF)
				{
					usfxFile.Read();
					if (usfxFile.NodeType == XmlNodeType.Element)
					{
						// The SFM name is the element name UNLESS sfm attribute overrides it.
						sfm = usfxFile.Name;
						id = level = style = who = "";
						if (usfxFile.HasAttributes)
						{
							for (i = 0; i < usfxFile.AttributeCount; i++)
							{
								usfxFile.MoveToAttribute(i);
								switch (usfxFile.Name)
								{
									case "id":
										id = usfxFile.Value;
										break;
									case "charset":
										id = usfxFile.Value;
										break;
									case "level":
										level = usfxFile.Value;
										break;
									case "sfm":
										sfm = usfxFile.Value;
										break;
									case "style":
										style = usfxFile.Value;
										break;
									case "attribute":
										id = usfxFile.Value;
										break;
									case "caller":
										// footnote caller, sfm attribute
										id = usfxFile.Value;
										break;
									case "who":
										// makes sense with <quoteStart>
										who = usfxFile.Value;
										break;
									case "xmlns:ns0":
									case "xmlns:xsi":
									case "xsi:noNamespaceSchemaLocation":
										// ignore these
										break;
									default:
										Logit.WriteError("Unrecognized attribute: "+usfxFile.Name+"="+usfxFile.Value);
										break;
								}
							}
							usfxFile.MoveToElement();
						}
						switch (usfxFile.Name)
						{
							case "book":
								if (id != "")
								{
									bookIDNum = eSwordBookNumber[bkInfo.Order(id)];
								}
								break;
							case "id":
								bookIDNum = eSwordBookNumber[bkInfo.Order(id)];
								if (textAttributes.stackPointer >= 0)
								{
									bblFile.Write("}");
								}
								if (firstLine)
									firstLine = false;
								else
									bblFile.WriteLine('"');
								if (footnoteEndVerse > 0)
								{
									cmtFile.WriteLine(footnoteCounter++.ToString()+',',
										footnoteBook, ',', footnoteChap, ',',
										footnoteStartVerse, ',',
										footnoteEndVerse,
										",\"{\\par\\b\\fs30 FOOTNOTES}\\par ",
										footnotes, '"');
								}
								footnoteStartVerse = 127;
								footnoteEndVerse = 0;
								// footnoteNumber = 0;
								footnotes = "";
								bblFile.WriteLine("\"");
								bblFile.Write(autoincrement.ToString(), ',',
									bookIDNum, ",1,1,\"");
								autoincrement++;
									if (textAttributes.stackPointer >= 0)
									{
										bblFile.Write("{");
										// TODO: add appropriate text attributes here
									}
								break;
							case "c":
								if (!usfxFile.IsEmptyElement)
								{
									ignore = true;
								}
								chapNum = Int32.Parse(id);
								if (chapNum > 1)
								{
									if (textAttributes.stackPointer >= 0)
									{
										bblFile.Write("}");
									}
									bblFile.WriteLine('"');
									if (footnoteEndVerse > 0)
									{
										cmtFile.WriteLine(footnoteCounter++.ToString()+',',
											footnoteBook, ',', footnoteChap, ',',
											footnoteStartVerse, ',',
											footnoteEndVerse,
											",\"{\\par\\b\\fs30 FOOTNOTES}\\par ",
											footnotes, '"');
									}
									bblFile.WriteLine("\"");
									bblFile.Write(autoincrement.ToString(), ',',
										bookIDNum, ",", chapNum,",1,\"");
									autoincrement++;
									if (textAttributes.stackPointer >= 0)
									{
										bblFile.Write("{");
										// TODO: add appropriate text attributes here
									}
								}
								break;
							case "v":
								verse = id;
								verseNum = Int32.Parse(id);
								if (verseNum > 1)
								{
									if (textAttributes.stackPointer >= 0)
									{
										bblFile.Write("}");
									}
									bblFile.WriteLine('"');
									if (footnoteEndVerse > 0)
									{
										cmtFile.WriteLine(footnoteCounter++.ToString()+',',
											footnoteBook, ',', footnoteChap, ',',
											footnoteStartVerse, ',',
											footnoteEndVerse,
											",\"{\\par\\b\\fs30 FOOTNOTES}\\par ",
											footnotes, '"');
									}
									bblFile.WriteLine("\"");
									bblFile.Write(autoincrement.ToString(), ',',
										bookIDNum, ",", chapNum,",", verseNum, ",\"");
									autoincrement++;
									if (textAttributes.stackPointer >= 0)
									{
										bblFile.Write("{");
										// TODO: add appropriate text attributes here
									}
								}
								if (!usfxFile.IsEmptyElement)
								{
									ignore = true;
								}
								break;
							case "fig":
								if (!usfxFile.IsEmptyElement)
								{
									ignore = true;
								}
								break;
							case "generated":
								if (!usfxFile.IsEmptyElement)
								{
									ignore = true;
								}
								break;
							case "table":
								// Do nothing... the table row and column elements are enough.
								break;
							case "quoteStart":
								quoteLevel++;
								break;
							case "quoteRemind":
								break;
							case "quoteEnd":
								quoteLevel--;
								if (quoteLevel < 0)
								{
									Logit.WriteLine("Warning: unmatched quoteEnd at "+
										bookIDNum+" "+chapter+":"+verse);
									quoteLevel = 0;
								}
								break;
							case "p":
								break;
							case "b":
								break;
							case "q":
								break;
							default:
								break;
						}
					}
					else if (usfxFile.NodeType == XmlNodeType.EndElement)
					{
						switch (usfxFile.Name)
						{
							case "book":
								// close output file.
								bookIDNum = 0;
								break;
							case "description":
								description = s;
								break;
							case "catalog":
								catalog = s;
								break;
							case "size":
								size = s;
								break;
							case "location":
								location = s;
								break;
							case "copyright":
								copyright = s;
								break;
							case "caption":
								caption = s;
								break;
							case "reference":
								reference = s;
								break;
							case "fig":
								ignore = false;
								break;
							case "c":
							case "v":
							case "generated":
								ignore = false;
								break;
							default:
								break;
						}
					}
					else if (usfxFile.NodeType == XmlNodeType.Text)
					{
						s = usfxFile.Value;
						if (!ignore)
						{
							if (inFootnote)
							{
								cmtFile.Write(unicode2rtf(usfxFile.Value));
							}
							else
							{
								//afterBlankLine = false;
								bblFile.Write(unicode2rtf(usfxFile.Value));
							}
						}
					}
					else if (usfxFile.NodeType == XmlNodeType.SignificantWhitespace)
					{
						bblFile.Write(" ");
					}
				}
				bblFile.Close();
				cmtFile.Close();
				usfxFile.Close();
			}
			catch (System.Exception ex)
			{
				Logit.WriteError("Conversion of USFX file "+inFileName+" to USFM files in "+outDir+" (named"+outFileName+") FAILED.");
				Logit.WriteLine(ex.ToString());
			}
		}
	}

    public class usfxToHtmlConverter
    {
    	protected const string IndexFileName = "index.htm";
    	protected string usfxFileName;
        protected XmlTextReader usfx;
        protected string element;
        protected string sfm;
        protected string id;
        protected string style;
        protected string level;
        protected string caller;
        protected string htmDir;
        protected string currentBookAbbrev;
        protected string currentBookHeader;
        protected string currentBookTitle;
        protected string currentChapter;
        protected string currentChapterAlternate;
        protected string wordForChapter;
        protected string currentChapterPublished;
        protected string vernacularLongTitle;
        protected string languageCode;
        int chapterNumber;
        int verseNumber;
        protected string currentVerse;
        protected string currentVersePublished;
        protected string currentVerseAlternate;
        protected string currentFileName = "";
        protected string previousFileName = "";
        protected string chapterLabel = "";
        protected string psalmLabel = "";
        protected string langName = "";
        protected string langId = "";
        protected string footerTextHTML = "";
        protected string copyrightLinkHTML = "";
        protected string homeLinkHTML = "";
        protected StringBuilder preVerse = new StringBuilder(String.Empty);
        protected string previousChapterText;
        protected string nextChapterText;
        protected ArrayList chapterFileList = new ArrayList();
        protected int chapterFileIndex = 0;
        public static ArrayList bookList = new ArrayList();
        protected int bookListIndex = 0;
        protected StringBuilder footnotesToWrite;
        protected StreamWriter htm;
        //int one = 1;
        bool inPreverse;
        bool inFootnote;
        bool inFootnoteStyle;
        bool inTextStyle;
        bool inParagraph;
        bool ignore;
        bool ignoreIntros = false;
        bool ignoreNotes = false;
        bool chopChapter;
        private bool hasContentsPage;
        //bool containsDC;
        bool newChapterFound;
        bool convertDigitsToKhmer;
        BibleBookInfo bookInfo = new BibleBookInfo();
        BibleBookRecord bookRecord;

		/// <summary>
		/// Null except when we have seen an open element with name p and style "Parallel Passage Reference" but have not yet seen the corresponding end element.
		/// Set to empty when we see the open element, any intermediate text is added to it.
		/// The end element then generates the complete cross-ref.
		/// </summary>
		string parallelPassage;

		public usfxToHtmlConverter()
		{
			ConcordanceLinkText = "Concordance";
		}

		/// <summary>
		/// True if we plan to generate a concordance to go with this file.
		/// </summary>
		public bool GeneratingConcordance { get; set; }

        public string LocalizeNumerals(string s)
        {
            if (convertDigitsToKhmer)
                return fileHelper.KhmerDigits(s);
            else
                return s;
        }

        FootNoteCaller footNoteCall = new FootNoteCaller("* † ‡ § ** †† ‡‡ §§ *** ††† ‡‡‡ §§§");

        protected void ProcessParagraphStart(bool beforeVerse)
        {
            if (sfm.Length == 0)
                sfm = usfx.Name;
            string cssStyle = sfm;
            if (level != "1")
                cssStyle = cssStyle + level;
            StartHtmlParagraph(cssStyle, beforeVerse);
            if (usfx.IsEmptyElement)
                EndHtmlParagraph();
        }

        protected string GetNamedAttribute(string attributeName)
        {
            string result = usfx.GetAttribute(attributeName);
            if (result == null)
                result = String.Empty;
            return result;
        }

        private string navButtonCode;
        protected Boolean eatSpace = false;

        protected void WriteNavButtons()
        {
            int i;
            string s = string.Empty;
            string firstChapterFile = string.Empty;
            string chFile;
            StringBuilder sb = new StringBuilder(s);
            StringBuilder bsb = new StringBuilder(s);
            BibleBookRecord br;

            // Write the "Home" link if one is desired.
            if ((homeLinkHTML != null) && (homeLinkHTML.Trim().Length > 0))
            {
                htm.WriteLine("<div class=\"navButtons\">{0}</div>", homeLinkHTML);
            }

            // We use 2 digits for chapter numbers in file names except for in the Psalms, where we use 3.
            string formatString = "00";
            int chapNumSize = 2;
            if (currentBookAbbrev.CompareTo("PSA") == 0)
            {
                formatString = "000";
                chapNumSize = 3;
            }

            // Figure out what the file name of the contents page or first chapter is, allowing for
            // possible (likely) missing contents pages or chapter 1.
            if (hasContentsPage)
            {
                i = 0;
                firstChapterFile = String.Format("{0}{1}.htm", currentBookAbbrev, i.ToString(formatString));
            }
            else
            {
                i = 0;
                while ((i < chapterFileList.Count) && (firstChapterFile == String.Empty))
                {
                    chFile = (string)chapterFileList[i];
                    if (chFile.StartsWith(currentBookAbbrev))
                    {
                        // The first match for this book might be chapter 1 or, if that is missing, a later chapter.
                        firstChapterFile = chFile + ".htm";
                    }
                    i++;
                }

            }


            if (bookListIndex >= 0)
            {
                htm.WriteLine("<div class=\"navButtons\"><a href=\"index.htm\">{0}</a> <a href=\"{1}\">{2}</a> {3}</div>",
                    langName, firstChapterFile, bookRecord.vernacularHeader, currentChapterPublished);
                bsb.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" align=\"center\"><tbody><tr><td>");
                bsb.Append("<form name=\"bkch1\"><div class=\"navChapters\">");
                bsb.Append("<select name=\"bksch1\" onChange=\"location=document.bkch1.bksch1.options[document.bkch1.bksch1.selectedIndex].value;\">");
                for (i = 0; i < bookList.Count; i++)
                {
                    br = (BibleBookRecord)bookList[i];
                    if (i == bookListIndex)
                    {
                        bsb.Append(OptionSelectedOpeningElement + br.vernacularHeader + "</option>\r\n");
                    }
                    else
                    {
                        foreach (string chapFile in chapterFileList)
                        {
                            if (chapFile.StartsWith(br.tla))
                            {
                                bsb.Append("<option value=\"" + chapFile + ".htm\">" + br.vernacularHeader + "</option>\r\n");
                                break;
                            }
                        }
                    }
                }
                bsb.Append("</select></div>");
                bsb.Append("</form>");

                bsb.Append("</td><td><form name=\"ch1\">");

                s = "<div class=\"navChapters\">";
                sb.Append(s);
                bsb.Append(s);
                if (previousFileName.Length > 0)
                {
                    s = String.Format("<a href=\"{0}#V0\">&lt;&nbsp;</a>",
                        Path.GetFileName(previousFileName));
                    sb.Append(s);
                    bsb.Append(s);
                }
                bsb.Append("<select name=\"ch1sel\" onChange=\"location=document.ch1.ch1sel.options[document.ch1.ch1sel.selectedIndex].value;\">");

                i = 0;
                string linkText = LocalizeNumerals("0");
                if (hasContentsPage)
                {
                    if (0 == chapterNumber)
                    {
                        sb.Append(" &nbsp;"+linkText+"&nbsp; ");
                        bsb.Append(OptionSelectedOpeningElement+linkText+"</option>");
                    }
                    else
                    {
                        sb.Append(String.Format(" <a href=\"{0}\">&nbsp;{1}&nbsp;</a> ",
                            String.Format("{0}{1}.htm", currentBookAbbrev, i.ToString(formatString)), linkText));
                        bsb.Append(String.Format("<option value=\"{0}\">{1}</option>",
                            String.Format("{0}{1}.htm", currentBookAbbrev, i.ToString(formatString)), linkText));
                    }
                }
                int nextChapIndex = -1;
                for (i = 0; i < chapterFileList.Count; i++)
                {
                    chFile = (string)chapterFileList[i];
                    int cn;
                    if (chFile.StartsWith(currentBookAbbrev) && (int.TryParse(chFile.Substring(chFile.Length - chapNumSize), out cn)))
                    {
                        // The first match for this book is the next chapter from the contents chapter (0).
                        if (nextChapIndex == -1)
                            nextChapIndex = i;
                        linkText = LocalizeNumerals(cn.ToString());
                        if (cn == chapterNumber)
                        {
                            sb.Append(String.Format(" &nbsp;{0}&nbsp; ", linkText));
                            bsb.Append(String.Format(OptionSelectedOpeningElement + "{0}</option>\r\n", linkText));
                            nextChapIndex = i + 1;
                        }
                        else
                        {
                            sb.Append(String.Format("<a href=\" {0}.htm#V0\">&nbsp;{1}&nbsp;</a> ",
                                chFile, linkText));
                            bsb.Append(String.Format("<option value=\"{0}.htm#V0\">{1}</option>\r\n",
                                chFile, linkText));
                        }
                    }
                }
                bsb.Append("</select>");
                if ((nextChapIndex >= chapterFileList.Count) || (nextChapIndex < 0))
                    nextChapIndex = 0;
                s = String.Format("<a href=\"{0}#V0\">&nbsp;&gt;</a></div>",
                    (string)chapterFileList[nextChapIndex] + ".htm");
                sb.Append(s);
                bsb.Append(s);
                bsb.Append("</form></td></tr></tbody></table>");
            }
            else
            {
                bsb.Append("<form name=\"bkch1\"><div class=\"navChapters\">");
                bsb.Append("<select name=\"bksch1\" onChange=\"location=document.bkch1.bksch1.options[document.bkch1.bksch1.selectedIndex].value;\">");
                bsb.Append(OptionSelectedOpeningElement + "---</option>\r\n");
                for (i = 0; i < bookList.Count; i++)
                {
                    br = (BibleBookRecord)bookList[i];
                    foreach (string chapFile in chapterFileList)
                    {
                        if (chapFile.StartsWith(br.tla))
                        {
                            bsb.Append("<option value=\"" + chapFile + ".htm\">" + br.vernacularHeader + "</option>\r\n");
                            break;
                        }
                    }
                }
                bsb.Append("</select></div>");
                bsb.Append("</form>");
            }
            navButtonCode = bsb.ToString();
            if (convertDigitsToKhmer)
                htm.WriteLine(sb.ToString());
            else
                htm.WriteLine(navButtonCode);
            navButtonCode = navButtonCode.Replace("ch1", "ch2");
        }

		/// <summary>
		/// In html, option selected can start out with just this, but xhtml requires the attribute to have a value.
		/// </summary>
    	protected virtual string OptionSelectedOpeningElement
    	{
    		get { return "<option selected>"; }
    	}

    	protected void RepeatNavButtons()
        {
            htm.WriteLine(navButtonCode);
        }

        protected void OpenHtmlFile()
        {
            OpenHtmlFile("", true);
            htm.WriteLine("<div class=\"main\">");
        }

		protected void OpenHtmlFile(string fileName)
		{
			OpenHtmlFile(fileName, false);
		}

        protected void OpenHtmlFile(string fileName, bool mainScriptureFile)
        {
            CloseHtmlFile();
            footNoteCall.reset();
            string chap = currentChapter;
            noteNumber = 0;
            if ((fileName != null) && (fileName.Length > 0))
            {
                currentFileName = Path.Combine(htmDir, fileName);
            }
            else
            {
                if (currentBookAbbrev == "PSA")
                    chap = String.Format("{0}", chapterNumber.ToString("000"));
                else
                    chap = String.Format("{0}", chapterNumber.ToString("00"));
                currentFileName = Path.Combine(htmDir, currentBookAbbrev + chap + ".htm");
            }
			MakeFramesFor(currentFileName);
            htm = new StreamWriter(currentFileName, false, Encoding.UTF8);
			// It is important that the DOCTYPE declaration should be a single line, and that the <html> element starts the second line.
			// This is because the concordance parser uses a ReadLine to skip the DOCTYPE declaration in order to read the rest of the file as XML.
        	htm.WriteLine(
        		"<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Transitional//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd\">");
			htm.WriteLine("<html xmlns:msxsl=\"urn:schemas-microsoft-com:xslt\" xmlns:user=\"urn:nowhere\">");
            htm.WriteLine("<head>");
			WriteCompleteElement("<META http-equiv=\"Content-Type\" content=\"text/html; charset=UTF-8\"");
            htm.WriteLine("<meta name=\"viewport\" content=\"width=device-width\" />");
			WriteCompleteElement("<link rel=\"stylesheet\" href=\"prophero.css\" type=\"text/css\"");
			if (mainScriptureFile)
			{
				htm.WriteLine("<script src=\"TextFuncs.js\" type=\"text/javascript\"></script>");
			}
            htm.WriteLine("<title>{0} {1} {2}</title></head>",
                langName, currentBookHeader, currentChapterPublished);
			WriteCompleteElement(string.Format("<meta name=\"keywords\" content=\"{0}, {1}, Holy Bible, Scripture, Bible, Scriptures, New Testament, Old Testament, Gospel\"",
                langName, langId));
            htm.WriteLine("<body class=\"mainDoc\"{0}>", GeneratingConcordance ? " onload=\"onLoad()\"" : "");
            
            WriteNavButtons();
        }

		/// <summary>
		/// Hook to allow subclass to generate corresponding frame file(s) for each main file.
		/// </summary>
		/// <param name="htmPath"></param>
		protected virtual void MakeFramesFor(string htmPath)
		{
			// Default for non-frame version does nothing.		
		}

		/// <summary>
		/// Write out a line text that makes up a complete element except for the closing angle bracket. In HTML, we just add the closing bracket.
		/// In XHTML, we need a closing slash before that bracket.
		/// </summary>
		/// <param name="htm"></param>
		/// <param name="content"></param>
		void WriteCompleteElement(string content)
		{
			htm.WriteLine(content + CloseOfContentlessElement);
		}

		/// <summary>
		/// The way to close an element that is not going to have any content. In HTML, we can just put a closing bracket. In XHTML, we need a slash before it.
		/// </summary>
    	protected virtual string CloseOfContentlessElement
    	{
    		get { return ">"; }
    	}

        protected void WriteHtmlFootnotes()
        {
            if (ignoreNotes)
                footnotesToWrite = new StringBuilder(String.Empty);
            if (footnotesToWrite.Length > 0)
            {
                WriteHorizontalRule();
                htm.WriteLine(footnotesToWrite);
                footnotesToWrite = new StringBuilder(String.Empty);
            }
			WriteHorizontalRule();
		}

    	private void WriteHorizontalRule()
    	{
			WriteCompleteElement("<hr");
    	}

    	protected void CloseHtmlFile()
        {
            if (htm != null)
            {
                EndHtmlNote();
                EndHtmlTextStyle();
                EndHtmlTable();
                EndHtmlParagraph();
                htm.WriteLine("<div class=\"pageFooter\">");
                WriteHtmlFootnotes();
                htm.WriteLine("</div></div>"); // end of div.pageFooter and div.main
                htm.WriteLine("<div class=\"pageFooter\">");
                RepeatNavButtons();
                if ((footerTextHTML != null) && (footerTextHTML.Trim().Length > 0))
                {
                    htm.WriteLine(footerTextHTML);
                }
                if ((copyrightLinkHTML != null) && (copyrightLinkHTML.Trim().Length > 0))
                {
                    htm.WriteLine("<p align=\"center\">" + copyrightLinkHTML + "</p>");
                }
                htm.WriteLine("</div><div class=\"pageFooter\">");
                htm.WriteLine("</div></body></html>");
                htm.Close();
                htm = null;
                previousFileName = currentFileName;
            }
            chopChapter = false;
        }

        public int maxPreverseLength = 0;
        public int maxVerseLength = 0;

        protected void StartVerse()
        {
            EndHtmlTextStyle(); // USFM and USFX disallow text styles crossing verse boundaries.
            // (If text styles could cross verse boundaries, we could just remember what the last
            //  style was and restart it, but that would make displaying any arbitrary range of
            //  verses harder if it were required.)
            if (htm == null)
                OpenHtmlFile();
            if (preVerse.Length > 0)
            {
                htm.WriteLine(preVerse.ToString());
                if (preVerse.Length > maxPreverseLength)
                    maxPreverseLength = preVerse.Length;
                preVerse = new StringBuilder(String.Empty);
                inPreverse = false;
            }
            htm.Write(string.Format(" <span class=\"verse\"> <a name=\"V{1}\">{0}&nbsp;</a></span>",
                currentVersePublished, verseNumber.ToString()));
            eatSpace = true;
        }

        /// <summary>
        /// Start an HTML paragraph with the specified style and initial text.
        /// If the paragraph is noncanonical introductory material, it should
        /// be marked as isPreverse to indicate that it should be in the file
        /// with the next verse, not the previous one.
        /// </summary>
        /// <param name="style">Paragraph style name to use for CSS class</param>
        /// <param name="text">Initial text of the paragraph, if any</param>
        /// <param name="isPreverse">true iff this paragraph style is non canonical,
        /// like section titles, book introductions, and such</param>
        protected void StartHtmlParagraph(string style, bool isPreverse)
        {
            EndHtmlParagraph();
            inParagraph = true;
            if (isPreverse)
                inPreverse = true;
            if (newChapterFound)
            {
                if (currentBookAbbrev.CompareTo("PSA") == 0)
                {
                    WriteHtml(String.Format("<div class=\"chapterlabel\"><a name=\"V0\">{0} {1}</a></div>", psalmLabel, currentChapterPublished));
                }
                else
                {
                    WriteHtml(String.Format("<div class=\"chapterlabel\"><a name=\"V0\">{0} {1}</a></div>", chapterLabel, currentChapterPublished));
                }
                newChapterFound = false;
            }
            string s = String.Format("<div class=\"{0}\">", style);
            WriteHtml(s);

        }

        protected void EndHtmlParagraph()
        {
            if (inParagraph)
            {
                WriteHtml("</div>\r\n");
                inParagraph = false;
            }
        }

        protected void StartHtmlTextStyle(string style)
        {
            if (!ignore)
            {
                EndHtmlTextStyle();
                inTextStyle = true;
                WriteHtml(String.Format("<span class=\"{0}\">", style));
            }
        }

        protected void EndHtmlTextStyle()
        {
            if (inTextStyle)
            {
                WriteHtml("</span>");
                inTextStyle = false;
            }
        }

        public static string EscapeHtml(string s)
        {
            s = s.Replace("<", "&lt;");
            s = s.Replace(">", "&gt;");
            s = s.Replace("&", "&amp;");
            return s;
        }

		protected void WritUnescapedeHtmlText(string text)
		{
			if (!ignore)
			{
				if (inFootnote)
				{
					footnotesToWrite.Append(text);
					// Also written to main document section for pop-up.
				}
				else
				{
					if (eatSpace)
					{
						text = text.TrimStart();
						eatSpace = false;
					}
				}
				WriteHtml(text);
			}
		}

        protected void WriteHtmlText(string text)
        {
            if (!ignore)
            {
                if (inFootnote)
                {
                    footnotesToWrite.Append(text);
                    // Also written to main document section for pop-up.
                }
                else
                {
                    if (eatSpace)
                    {
                        text = text.TrimStart();
                        eatSpace = false;
                    }
                }
                WriteHtml(EscapeHtml(text));
            }
        }

        private void WriteHtmlOptionalLineBreak()
        {
            WriteHtml("<br>");
        }

        private void WriteHtml(string s)
        {
            if (!ignore)
            {
                if (htm == null)
                    inPreverse = true;
                if (inPreverse)
                    preVerse.Append(s);
                else
                    htm.Write(s);
            }
        }

        private int noteNumber = 0;

        private string noteName()
        {
            noteNumber++;
            return "FN" + noteNumber.ToString();
        }

        protected void StartHtmlNote(string style, string marker)
        {
            EndHtmlNote();
            if (ignoreNotes)
            {
                ignore = true;
                return;
            }
            inFootnoteStyle = false;
            inFootnote = true;
            string noteId = noteName();
            if (string.Compare(marker, "+") == 0)
            {
                if (style == "f")
                    marker = footNoteCall.Marker();
                else
                    marker = "✡";
            }
            if (string.Compare(marker, "-") == 0)
                marker = "";
            WriteHtml(String.Format("<a href=\"#{0}\" onclick=\"hilite('{0}')\"><span class=\"notemark\">{1}</span><span class=\"popup\">",
                noteId, marker));
            // Numeric chapter and verse numbers are used in internal references instead of the text versions, which may
            // include dashes in verse bridges.
            footnotesToWrite.Append(String.Format("<p class=\"{0}\" id=\"{1}\"><span class=\"notemark\">{2}</span><a class=\"notebackref\" href=\"#V{3}\">{4}:{5}:</a>\r\n",
                style, noteId, marker, verseNumber.ToString(), currentChapterPublished, currentVersePublished));
        }

        protected void EndHtmlNote()
        {
            if (inFootnote)
            {
                EndHtmlNoteStyle();
                WriteHtml("</span></a>");   // End popup text
                footnotesToWrite.Append("</p>\r\n");    // End footnote paragraph
                inFootnote = false;
            }
        }

        protected void StartHtmlNoteStyle(string style)
        {
            EndHtmlNoteStyle();
            inFootnoteStyle = true;
            footnotesToWrite.Append(String.Format("<span class=\"{0}\">", style)); // for bottom of chapter
        }

        protected void EndHtmlNoteStyle()
        {
            if (inFootnoteStyle)
            {
                footnotesToWrite.Append("</span>");
                inFootnoteStyle = false;
            }
        }

        bool inHeader = true;
        bool inTable = false;
        bool inTableRow = false;
        bool inTableCol = false;
        bool inTableBold = false;

        protected void StartHtmlTable()
        {
            EndHtmlParagraph();
            if (!inTable)
            {
                WriteHtml("<table border=\"2\" cellpadding=\"2\" cellspacing=\"2\"><tbody>\r\n");
                inTable = true;
            }
        }

        protected void EndHtmlTableCol()
        {
            if (inTableBold)
            {
                WriteHtml("</b>");
                inTableBold = false;
            }
            if (inTableCol)
            {
                WriteHtml("</td>");
                inTableCol = false;
            }
        }

        protected void EndHtmlTableRow()
        {
            EndHtmlTableCol();
            if (inTableRow)
            {
                WriteHtml("</tr>");
                inTableRow = false;
            }
        }

        protected void EndHtmlTable()
        {
            EndHtmlTableRow();
            if (inTable)
            {
                WriteHtml("</tbody></table>\r\n");
                inTable = false;
            }
        }

        private int lastChapterNumber = -1;

        protected void LeaveHeader()
        {
            if (inHeader || (lastChapterNumber != chapterNumber))
            {
                inHeader = false;
                lastChapterNumber = chapterNumber;
                if (currentBookHeader.Length < 1)
                {
                    if (vernacularLongTitle.Length > 0)
                        currentBookHeader = vernacularLongTitle;
                    else if (bookRecord.vernacularAbbreviation.Length > 0)
                        currentBookHeader = bookRecord.vernacularAbbreviation;
                    else
                        currentBookHeader = currentBookAbbrev;  // If you get here, the SFM input is pretty bad.
                                                    bookRecord.vernacularHeader = currentBookHeader;
                                if (bookRecord.vernacularName.Length < 1)
                                    bookRecord.vernacularName = vernacularLongTitle;

                }
                bookRecord.vernacularHeader = currentBookHeader;
                if (bookRecord.vernacularName.Length < 1)
                    bookRecord.vernacularName = vernacularLongTitle;
                string chap;
                if (currentBookAbbrev == "PSA")
                    chap = String.Format("{0}{1}", currentBookAbbrev, chapterNumber.ToString("000"));
                else
                    chap = String.Format("{0}{1}", currentBookAbbrev, chapterNumber.ToString("00"));
                chapterFileList.Add(chap);
                CloseHtmlFile();
            }
        }

        private void ProcessChapter()
        {
            currentChapter = id;
            currentChapterPublished = LocalizeNumerals(currentChapter);
            currentChapterAlternate = String.Empty;
            currentVerse = currentVersePublished = currentVerseAlternate = String.Empty;
            verseNumber = 0;
            if (!usfx.IsEmptyElement)
            {
                usfx.Read();
                if (usfx.NodeType == XmlNodeType.Text)
                    currentChapterPublished = LocalizeNumerals(usfx.Value.Trim());
            }
            int chNum;
            if (Int32.TryParse(id, out chNum))
                chapterNumber = chNum;
            else
                chapterNumber++;
            chopChapter = true;
            newChapterFound = true;
            CloseHtmlFile();
            inPreverse = true;
            chapterFileIndex++;
            inHeader = false;
        }

        private void VirtualChapter()
        {
            currentChapter = "1";
            currentChapterPublished = LocalizeNumerals(currentChapter);
            chapterNumber = 1;
            currentChapterAlternate = String.Empty;
            currentVerse = currentVersePublished = currentVerseAlternate = String.Empty;
            verseNumber = 0;
            chopChapter = true;
            if (!bookInfo.isPeripheral(currentBookAbbrev))
               newChapterFound = true;
            CloseHtmlFile();
            inPreverse = true;
            chapterFileIndex++;
        }


        private void ProcessVerse()
        {
            currentVerse = id;
            currentVersePublished = LocalizeNumerals(currentVerse);
            currentVerseAlternate = "";
            if (!usfx.IsEmptyElement)
            {
                usfx.Read();
                if (usfx.NodeType == XmlNodeType.Text)
                {
                    currentVersePublished = LocalizeNumerals(usfx.Value.Trim());
                }
            }
            int vnum;
            if (Int32.TryParse(id, out vnum))
            {
                verseNumber = vnum;
            }
            else
            {
                verseNumber++;
            }
            StartVerse();
        }

       
        /// <summary>
        /// Creates a new USFX file that has the local (location-specific) and global changes made to it
        /// that are specified in the globalSubstFile and localSubstFile. In those two files, the separator
        /// character for each line is whatever character is the first one on the line, which can be any
        /// printing Unicode character that isn't contained within any of the fields on the line.
        /// </summary>
        /// <param name="inFile">USFX Scripture input file</param>
        /// <param name="outFile">USFX Scripture output file with specified changes made</param>
        /// <param name="globalSubstFile">Regular expression global substitutions, with each line containing
        /// a separator character, match string, separator character, replacement string, separator character.
        /// Anything after the 3rd separator character is ignored as a comment. For example:
        /// /(\W)thee(\W)/$1you$2/ This is a comment.
        /// /(\W)thou(\W)/$1you$2/
        /// </param>
        /// <param name="localSubstFile">Plain text location-based one-time substitutions, with each line
        /// containing a separator character, book abbreviation, separator character, chapter number,
        /// separator character, verse number, separator character, 'N' if in a footnote, otherwise space,
        /// separator character, find text, separator character, replace text, separator character, optional
        /// comment. Lines must be in canonical order, as written in the input USFX file. For example:
        /// ,LEV,20,26, ,Yahweh,the LORD, This is a comment.
        /// ,LEV,21,1, ,Yahweh,The LORD,
        /// </param>
        /// <returns></returns>
        public bool FilterUsfx(string inFile, string outFile, string localSubstFile, bool includeApocrypha)
        {
            // Logit.WriteLine("Filtering from " + inFile + " to " + outFile + " using " + localSubstFile);
            if (includeApocrypha)
                Logit.WriteLine("Including Apocrypha.");
            bool result = false;
            bool doLocalSubst = false;
            bool lookInFootnote = false;
            bool includeThis = true;
            string localSubstLine = String.Empty;
            string[] localSubstParts={"", "", "", "", "", ""};
            string phrase;
            StreamReader dasr = null;
            string daList = String.Empty;
            XmlTextWriter usfxOut;
            chapterNumber = verseNumber = 0;
            currentBookAbbrev = currentBookTitle = currentChapterPublished = String.Empty;
            currentChapter = currentFileName = currentVerse = languageCode = String.Empty;
            inPreverse = inFootnote = inFootnoteStyle = inTextStyle = inParagraph = chopChapter = false;
            bookRecord = (BibleBookRecord)bookInfo.books["FRT"];
            try
            {
                if ((localSubstFile != null) && (File.Exists(localSubstFile)))
                {
                    dasr = new StreamReader(localSubstFile);
                    localSubstLine = dasr.ReadLine();
                    localSubstParts = localSubstLine.Split(new char[] { localSubstLine[0] });
                    lookInFootnote = (localSubstParts[4] == "N");
                    doLocalSubst = true;
                }
                usfx = new XmlTextReader(inFile);
                usfx.WhitespaceHandling = WhitespaceHandling.All;
                usfxOut = new XmlTextWriter(outFile, Encoding.UTF8);
                
                
                while (usfx.Read())
                {
                    // Console.Write("{0} {1}:{2} {3}       \r", currentBookAbbrev, currentChapter, currentVerse, localSubstLine);

                    if (usfx.NodeType == XmlNodeType.Element)
                    {
                        
                        level = GetNamedAttribute("level");
                        style = GetNamedAttribute("style");
                        sfm = GetNamedAttribute("sfm");
                        caller = GetNamedAttribute("caller");
                        id = GetNamedAttribute("id");

                        switch (usfx.Name)
                        {
                            case "book":
                                if (id.Length > 2)
                                {
                                    currentBookAbbrev = id;
                                    bookRecord = (BibleBookRecord)bookInfo.books[currentBookAbbrev];
                                }
                                currentBookHeader = vernacularLongTitle = String.Empty;
                                currentChapter = currentVerse = "0";
                                chapterNumber = 0;
                                verseNumber = 0;
                                if ((bookRecord == null) || (bookRecord.testament == "a") || (bookRecord.testament == "x"))
                                {
                                    includeThis = includeApocrypha;
                                    // if (includeThis)
                                    //     Console.WriteLine("Including {0}", currentBookAbbrev);
                                    // else
                                    //     Console.WriteLine("Skipping {0}", currentBookAbbrev);
                                }
                                else
                                {
                                    includeThis = true;
                                    // Console.WriteLine("Processing {0}", currentBookAbbrev);
                                }
                                break;
                            case "c":
                                currentChapter = id;
                                currentVerse = "0";
                                verseNumber = 0;
                                int chNum;
                                if (Int32.TryParse(id, out chNum))
                                    chapterNumber = chNum;
                                else
                                    chapterNumber++;
                                bookRecord.numChapters = Math.Max(bookRecord.numChapters, chapterNumber);
                                break;
                            case "v":
                                currentVerse = id;
                                if (!usfx.IsEmptyElement)
                                {
                                    usfx.Read();
                                    if (usfx.NodeType == XmlNodeType.Text)
                                    {
                                        currentVersePublished = LocalizeNumerals(usfx.Value.Trim());
                                    }
                                }
                                int vnum;
                                if (Int32.TryParse(id, out vnum))
                                {
                                    verseNumber = vnum;
                                }
                                else
                                {
                                    verseNumber++;
                                }
                                break;
                            case "f":
                                inFootnote = true;
                                break;
                        }
                        if (includeThis)
                        {
                            usfxOut.WriteStartElement(usfx.Name);
                            usfxOut.WriteAttributes(usfx, false);
                            if (usfx.IsEmptyElement)
                                usfxOut.WriteEndElement();
                        }
                    }
                    if (includeThis)
                    {
                        if ((usfx.NodeType == XmlNodeType.EndElement) && (usfx.NodeType != XmlNodeType.Element))
                        {
                            usfxOut.WriteEndElement();
                            if (usfx.Name == "f")
                                inFootnote = false;
                        }
                        else if (usfx.NodeType == XmlNodeType.Text)
                        {
                            phrase = usfx.Value;

                            // Try local (specific to a verse) substitution first.
                            while (doLocalSubst && (currentBookAbbrev == localSubstParts[1]) &&
                                (currentChapter == localSubstParts[2]) &&
                                (currentVerse == localSubstParts[3]) &&
                                (lookInFootnote == inFootnote) &&
                                phrase.Contains(localSubstParts[5]))
                            {
                                phrase = fileHelper.ReplaceFirst(phrase, localSubstParts[5], localSubstParts[6]);
                                if (dasr.EndOfStream)
                                    doLocalSubst = false;
                                localSubstLine = dasr.ReadLine();
                                if ((localSubstLine == null) || (localSubstLine.Length < 6))
                                {
                                    doLocalSubst = false;
                                }
                                else
                                {
                                    localSubstParts = localSubstLine.Split(new char[] { localSubstLine[0] });
                                    if (localSubstParts.Length < 6)
                                        doLocalSubst = false;
                                    else
                                        lookInFootnote = (localSubstParts[4] == "N");
                                }
                                if (!doLocalSubst)
                                    dasr.Close();
                            }

                           
                            // Write out changed text.
                            if (phrase != null)
                                usfxOut.WriteString(phrase);
                        }
                        else if ((usfx.NodeType == XmlNodeType.Whitespace) || (usfx.NodeType == XmlNodeType.SignificantWhitespace))
                        {
                            usfxOut.WriteWhitespace(usfx.Value);

                        }
                        else if (usfx.NodeType == XmlNodeType.XmlDeclaration)
                        {
                            usfxOut.WriteStartDocument();
                        }
                        else if (usfx.NodeType != XmlNodeType.Element)
                        {
                            usfxOut.WriteRaw(usfx.ReadString());
                        }
                    }
                }
                usfxOut.Close();
                usfx.Close();
            }
            catch (Exception ex)
            {
                Logit.WriteError(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }

            return result;
        }

        public string indexDateStamp = String.Empty;
        public static string conversionProgress = String.Empty;

        /// <summary>
        /// Writes a text file containing SQL code to populate a Scripture database with the verses in this
        /// USFX Bible translation.
        /// </summary>
        /// <param name="usfxName">Name of the USFX source file to read</param>
        /// <param name="sqlFileName">Name of the SQL file to write.</param>
        /// <param name="translationId">Unique ID of this translation</param>
        /// <param name="skipHelps">True iff you want to skip conversion of introductions and notes</param>
        /// <param name="useKhmerDigits">True iff you want to convert chapter and verse numbers to Khmer digits</param>
        /// <returns>true iff the conversion was successful</returns>
        public bool Usfx2Sql(string usfxName, string sqlFileName, string translationId, bool skipHelps = false, bool useKhmerDigits = false)
        {
            bool result = false;
            bool inUsfx = false;
            bool containsDC = false;
            string currentParagraph = string.Empty;
            string activeStyles = string.Empty;
            ignoreIntros = ignoreNotes = skipHelps;
            StringBuilder sqlBookList = new StringBuilder();
            StringBuilder sqlChapterList = new StringBuilder();
            StringBuilder pending = new StringBuilder();
            StringBuilder searchText = new StringBuilder();
            convertDigitsToKhmer = useKhmerDigits;
            
            try
            {
                StreamWriter sw = new StreamWriter(sqlFileName, false);
                usfxFileName = usfxName;
                if (usfx != null)
                    usfx.Close();

                sw.WriteLine("USE prophero;");
                sw.WriteLine(@"CREATE TABLE IF NOT EXISTS 'bibleverses'
('verseid' VARCHAR(64) NOT NULL PRIMARY KEY, // i. e. eng-web.PSA.119.105 or nop.GEN.1.1
'redirect' VARCHAR(64), // for redirecting a query for a verse number that is part of a verse range to the first verse of the range
'publishedChapter' VARCHAR(32),
'publishedVerse' VARCHAR(32),
'activePara' VARCHAR(8), // Active paragraph tag at beginning of a verse record
'text' VARCHAR(2048), // marked-up text after the verse marker
'searchtext' VARCHAR(2048) FULLTEXT INDEX);"); // plain text for searching, stripped of markup and notes

                usfx = new XmlTextReader(usfxName);
                usfx.WhitespaceHandling = WhitespaceHandling.Significant;
                while (usfx.Read())
                {
                    conversionProgress = "Generating sql " + currentBookAbbrev + " " + currentChapter + ":" + currentVerse;
                    System.Windows.Forms.Application.DoEvents();

                    switch (usfx.NodeType)
                    {
                        case XmlNodeType.Element:
                            if (inUsfx)
                            {
                                level = GetNamedAttribute("level");
                                style = GetNamedAttribute("style");
                                sfm = GetNamedAttribute("sfm");
                                caller = GetNamedAttribute("caller");
                                id = GetNamedAttribute("id");

                                switch (usfx.Name)
                                {
                                    case "languageCode":
                                        usfx.Read();
                                        if (usfx.NodeType == XmlNodeType.Text)
                                            languageCode = usfx.Value;
                                        break;
                                    case "sectionBoundary":
                                    case "generated":
                                    case "rem":
                                    case "periph":
                                    case "fig": // Illustrations not yet supported
                                    case "ndx":
                                    case "w":
                                    case "wh":
                                    case "wg":
                                        if (!usfx.IsEmptyElement)
                                            ignore = true;
                                        break;
                                    case "book":
                                        currentBookAbbrev = id;
                                        chapterNumber = 0;
                                        verseNumber = 0;
                                        currentBookHeader = vernacularLongTitle = String.Empty;
                                        if (bookInfo.isApocrypha(id))
                                            containsDC = true;
                                        bookRecord = (BibleBookRecord)bookList[bookListIndex];
                                        currentChapter = "";
                                        break;
                                    case "id":
                                        if (id.Length > 2)
                                            currentBookAbbrev = id;
                                        if (bookRecord.tla != currentBookAbbrev)
                                        {
                                            Logit.WriteLine("Mismatch between id of book element and id element " + id);
                                            bookRecord = (BibleBookRecord)bookList[bookListIndex];
                                        }
                                        if (!usfx.IsEmptyElement)
                                            ignore = true;
                                        inHeader = true;
                                        newChapterFound = false;
                                        break;
                                    case "ide":
                                        // We could read attribute charset, here, if we would do anything with it.
                                        // Ideally, this would be read on first pass, and used as the encoding to read
                                        // the file on the second pass. However, we currently don't support anything
                                        // but utf-8, so it is kind of redundant, except for round-trip conversion back
                                        // to USFM.
                                        if (!usfx.IsEmptyElement)
                                            ignore = true;
                                        break;
                                    case "h":
                                        usfx.Read();
                                        if (usfx.NodeType == XmlNodeType.Text)
                                            currentBookHeader = fileHelper.sqlString(usfx.Value.Trim());
                                        break;
                                    case "cl":
                                        usfx.Read();
                                        if (usfx.NodeType == XmlNodeType.Text)
                                        {
                                            if (chapterNumber == 0)
                                                wordForChapter = usfx.Value.Trim();
                                            else
                                                currentChapterPublished = LocalizeNumerals(usfx.Value.Trim());
                                        }
                                        break;
                                    case "p":
                                        // EndHtmlTable();
                                        if (sfm.CompareTo("mt") == 0)
                                        {
                                            usfx.Read();
                                            if (usfx.NodeType == XmlNodeType.Text)
                                            {
                                                if (vernacularLongTitle.Length > 0)
                                                    vernacularLongTitle = vernacularLongTitle + " " + fileHelper.sqlString(usfx.Value.Trim());
                                                else
                                                    vernacularLongTitle = fileHelper.sqlString(usfx.Value.Trim());
                                            }
                                        }

                       /****************
                                        else if (sfm.CompareTo("ms") == 0)
                                        {
                                            usfx.Read();
                                            if (usfx.NodeType == XmlNodeType.Text)
                                            {
                                                bookRecord.toc.Append(String.Format("<div class=\"toc1\"><a href=\"{0}{1}.htm#V{2}\">{3}</a></div>\r\n",
                                                    currentBookAbbrev, Math.Max(1, chapterNumber).ToString(chapFormat),
                                                    verseNumber.ToString(), usfx.Value.Trim()));
                                            }
                                            hasContentsPage = true;
                                        }

                                        bool beforeVerse = true;
                                        if ((bookRecord.testament.CompareTo("x") == 0) ||
                                            (sfm.Length == 0) || (sfm == "p") || (sfm == "fp") ||
                                            (sfm == "nb") || (sfm == "cls") || (sfm == "li") ||
                                            (sfm == "lit") || (sfm == "m") || (sfm == "mi") ||
                                            (sfm == "pc") || (sfm == "pde") || (sfm == "pdi") ||
                                            (sfm == "ph") || (sfm == "phi") || (sfm == "pi") ||
                                            (sfm == "pm") || (sfm == "pmc") || (sfm == "pmo") ||
                                            (sfm == "pmr") || (sfm == "pr") || (sfm == "ps") ||
                                            (sfm == "psi") || (sfm == "qc") || (sfm == "qm") ||
                                            (sfm == "qr") || (sfm == "pr") || (sfm == "ps"))
                                            beforeVerse = false;
                                        if (ignoreIntros && ((sfm == "ip") || (sfm == "imt") || (sfm == "io") || (sfm == "is") || (sfm == "iot")))
                                            ignore = true;
                                        ProcessParagraphStart(beforeVerse);
                        */
                                        break;
                                    case "q":
                                    case "qs":  // qs is really a text style with paragraph attributes, but HTML/CSS can't handle that.
                                    case "b":
                                        // EndHtmlTable();
                                        //ProcessParagraphStart(false);
                                        break;
                                    case "mt":
                                    case "d":
                                    case "s":
                                        //EndHtmlTable();
                                        //ProcessParagraphStart(true);
                                        break;
                                    case "c":
                                        //EndHtmlTable();
                                        //ProcessChapter();
                                        break;
                                    case "cp":
                                        if (!usfx.IsEmptyElement)
                                        {
                                            usfx.Read();
                                            if (usfx.NodeType == XmlNodeType.Text)
                                                currentChapterPublished = LocalizeNumerals(usfx.Value.Trim());
                                        }
                                        break;
                                    case "ca":
                                        if (!usfx.IsEmptyElement)
                                        {
                                            usfx.Read();
                                            if (usfx.NodeType == XmlNodeType.Text)
                                                currentChapterAlternate = usfx.Value.Trim();
                                        }
                                        break;
                                    case "toc":
                                        if (!usfx.IsEmptyElement)
                                        {
                                            usfx.Read();
                                            if (usfx.NodeType == XmlNodeType.Text)
                                            {
                                                switch (level)
                                                {
                                                    case "1":
                                                        bookRecord.vernacularName = usfx.Value.Trim();
                                                        break;
                                                    case "2":
                                                        bookRecord.shortName = usfx.Value.Trim();
                                                        break;
                                                    case "3":
                                                        bookRecord.vernacularAbbreviation = usfx.Value.Trim();
                                                        break;
                                                }
                                            }
                                        }
                                        break;
                                    case "v":
                                        EndHtmlTable();
                                        if (chapterNumber == 0)
                                            VirtualChapter();
                                        ProcessVerse();
                                        break;
                                    case "va":
                                        EndHtmlTable();
                                        if (!usfx.IsEmptyElement)
                                        {
                                            usfx.Read();
                                            if (usfx.NodeType == XmlNodeType.Text)
                                                currentVerseAlternate = LocalizeNumerals(usfx.Value.Trim());
                                        }
                                        break;
                                    case "vp":
                                        EndHtmlTable();
                                        if (!usfx.IsEmptyElement)
                                        {
                                            usfx.Read();
                                            if (usfx.NodeType == XmlNodeType.Text)
                                                currentVersePublished = LocalizeNumerals(usfx.Value.Trim());
                                        }
                                        break;
                                    case "qt":
                                    case "nd":
                                    case "tl":
                                    case "qac":
                                    case "fm":
                                    case "sls":
                                    case "bk":
                                    case "pn":
                                    case "k":
                                    case "ord":
                                    case "add":
                                    case "sig":
                                    case "bd":
                                    case "it":
                                    case "bdit":
                                    case "sc":
                                    case "no":
                                    case "ior":
                                    case "wj":
                                    case "cs":
                                        if (sfm.Length == 0)
                                            sfm = usfx.Name;
                                        StartHtmlTextStyle(sfm);
                                        break;
                                    case "milestone":
                                        switch (sfm)
                                        {
                                            case "th":
                                                EndHtmlTableCol();
                                                StartHtmlTable();
                                                WriteHtml("<td><b>");
                                                inTableCol = true;
                                                inTableBold = true;
                                                break;
                                            case "tr":
                                                EndHtmlTableRow();
                                                StartHtmlTable();
                                                WriteHtml("<tr>");
                                                inTableRow = true;
                                                break;
                                            case "thr":
                                                EndHtmlTableCol();
                                                WriteHtml("<td align=\"right\"><b>");
                                                inTableBold = true;
                                                inTableCol = true;
                                                break;
                                            case "tc":
                                                EndHtmlTableCol();
                                                WriteHtml("<td>");
                                                inTableCol = true;
                                                break;
                                            case "tcr":
                                                EndHtmlTableCol();
                                                WriteHtml("<td align=\"right\">");
                                                inTableCol = true;
                                                break;
                                            case "hr":
                                                EndHtmlTable();
                                                WriteHtml("<hr>");
                                                break;
                                        }
                                        break;

                                    case "f":
                                    case "x":
                                        if (ignoreNotes)
                                            ignore = true;
                                        StartHtmlNote(usfx.Name, caller);
                                        break;
                                    case "fk":
                                    case "fq":
                                    case "fqa":
                                    case "fv":
                                    case "ft":
                                    case "xo":
                                    case "xk":
                                    case "xt":
                                        StartHtmlNoteStyle(usfx.Name);
                                        break;
                                    case "xdc":
                                    case "fdc":
                                    case "dc":
                                        if (!containsDC)
                                            ignore = true;
                                        break;
                                    case "optionalLineBreak":
                                        WriteHtmlOptionalLineBreak();
                                        break;

                                }
                            }
                            else
                            {
                                if (usfx.Name == "usfx")
                                    inUsfx = true;
                            }
                            break; // End of element
                        case XmlNodeType.Whitespace:
                        case XmlNodeType.SignificantWhitespace:
                        case XmlNodeType.Text:
                            WriteHtmlText(usfx.Value);
                            break;
                        case XmlNodeType.EndElement:
                            if (inUsfx)
                            {
                                switch (usfx.Name)
                                {
                                    case "usfx":
                                        inUsfx = false;
                                        break;
                                    case "ide":
                                    case "generated":
                                    case "sectionBoundary":
                                    case "rem":
                                    case "periph":
                                    case "fig": // Illustrations not yet supported
                                    case "ndx":
                                    case "w":
                                    case "wh":
                                    case "wg":
                                    case "id":
                                        ignore = false;
                                        break;
                                    case "book":
                                        if (bookRecord.testament.CompareTo("x") == 0)
                                        {
                                            if (chapterNumber == 0)
                                            {
                                                chapterNumber++;
                                                if (htm == null)
                                                    OpenHtmlFile();
                                                if (preVerse.Length > 0)
                                                {
                                                    htm.WriteLine(preVerse.ToString());
                                                    //preVerse = new StringBuilder(String.Empty);
                                                    inPreverse = false;
                                                }
                                            }
                                        }
                                        CloseHtmlFile();
                                        bookListIndex++;
                                        break;
                                    case "p":
                                    case "q":
                                    case "qs":  // qs is really a text style with paragraph attributes, but HTML/CSS can't handle that.
                                    case "b":
                                    case "mt":
                                        EndHtmlParagraph();
                                        if (chopChapter)
                                            CloseHtmlFile();
                                        if (ignoreIntros)
                                            ignore = false;
                                        break;
                                    case "ms":
                                    case "d":
                                    case "s":
                                        if (bookRecord.testament.CompareTo("x") == 0)
                                        {
                                            if (chapterNumber == 0)
                                            {
                                                chapterNumber++;
                                            }
                                            if (htm == null)
                                                OpenHtmlFile();
                                            if (preVerse.Length > 0)
                                            {
                                                htm.WriteLine(preVerse.ToString());
                                                //preVerse = new StringBuilder(String.Empty);
                                                inPreverse = false;
                                            }
                                            verseNumber++;
                                            htm.Write("<a name=\"C{0}V{1}\"></a>",
                                                chapterNumber.ToString(), verseNumber.ToString());
                                        }
                                        EndHtmlParagraph();
                                        if (chopChapter)
                                            CloseHtmlFile();
                                        break;
                                    case "qt":
                                    case "nd":
                                    case "tl":
                                    case "qac":
                                    case "fm":
                                    case "sls":
                                    case "bk":
                                    case "pn":
                                    case "k":
                                    case "ord":
                                    case "add":
                                    case "sig":
                                    case "bd":
                                    case "it":
                                    case "bdit":
                                    case "sc":
                                    case "no":
                                    case "ior":
                                    case "wj":
                                    case "cs":
                                        EndHtmlTextStyle();
                                        break;
                                    case "f":
                                    case "x":
                                        EndHtmlNote();
                                        if (ignoreNotes)
                                            ignore = false;
                                        break;
                                    case "fk":
                                    case "fq":
                                    case "fqa":
                                    case "fv":
                                    case "ft":
                                    case "xo":
                                    case "xk":
                                    case "xt":
                                        EndHtmlNoteStyle();
                                        break;
                                    case "xdc":
                                    case "fdc":
                                    case "dc":
                                        ignore = false;
                                        break;

                                }
                            }
                            break;

                    }   // End of massive case statement
                }   // End of USFX reading while loop
                usfx.Close();
                sw.Close();
                result = true;
            }
            catch (Exception ex)
            {
                Logit.WriteError(ex.Message);
            }

            return result;
        }

		/// <summary>
		///  (Localizable) string to display as the text of the link the Concordance.
		/// </summary>
		public string ConcordanceLinkText { get; set; }

        /// <summary>
        /// Converts the USFX file usfxName to a set of HTML files, one file per chapter, in the
        /// directory htmlDir, with reference to CSS files in cssDir. The output file names will
        /// be the standard 3-letter abbreviation for the book, followed by a dash and the 2 or
        /// 3 digit chapter number, followed optionally by namePart, then .htm.
        /// </summary>
        /// <param name="usfxName">Name of the USFX file to convert to HTML files</param>
        /// <param name="htmlDir">Directory to put the HTML files into</param>
        /// <param name="languageName">Name of the language for page headers</param>
        /// <param name="languageId">New Ethnologue 3-letter code for this language</param>
        /// <param name="chapterLabelName">Vernacular name for "Chapter"</param>
        /// <param name="psalmLabelName">Vernacular name for "Psalm"</param>
        /// <param name="copyrightLink">HTML for copyright link, like &lt;href a="copyright.htm"&gt;©&lt;/a&gt;</param>
        /// <param name="homeLink">HTML for link to home page</param>
        /// <param name="footerHtml">HTML for footer "fine print" text</param>
        /// <returns>true iff the conversion succeeded</returns>
        public bool ConvertUsfxToHtml(string usfxName, string htmlDir, string languageName, string languageId, string translationId,
            string chapterLabelName, string psalmLabelName, string copyrightLink, string homeLink, string footerHtml,
            string indexHtml, string licenseHtml, bool useKhmerDigits = false, bool skipHelps = false, string goText = "Go!")
        {
            bool result = false;
            bool inUsfx = false;
            ignoreIntros = ignoreNotes = skipHelps;
            footerTextHTML = footerHtml;
            copyrightLinkHTML = copyrightLink;
            homeLinkHTML = homeLink;
            htmDir = htmlDir;
            langId = languageId;
            langName = languageName;
            chapterLabel = chapterLabelName;
            psalmLabel = psalmLabelName;
            chapterNumber = verseNumber = 0;
            bookList.Clear();
            convertDigitsToKhmer = useKhmerDigits;
            currentBookAbbrev = currentBookTitle = currentChapterPublished = wordForChapter = String.Empty;
            currentChapter = currentFileName = currentVerse = languageCode = String.Empty;
            inPreverse = inFootnote = inFootnoteStyle = inTextStyle = inParagraph = chopChapter = false;
            bookRecord = (BibleBookRecord)bookInfo.books["FRT"];
			// This flag is set true during the first pass through the USFX file (in preparation for generating navigation files)
			// if any apocryphal books are encountered. If this does not happen (that is, there is no apocryphal material), in the 
			// main pass we ignore elements dc, xdc, and fdc.
			// I (JohnT) don't know WHY these elements should be ignored unless the translation includes apocrypha, but I confirmed
			// with Michael that it is intentional.
			bool containsDC = false;
            newChapterFound = false;
            ignore = false;
            StringBuilder toc = new StringBuilder();
            string chapFormat = "00";

            footnotesToWrite = new StringBuilder(String.Empty);
            preVerse = new StringBuilder(String.Empty);
            try
            {
                usfxFileName = usfxName;
                if (usfx != null)
                    usfx.Close();
                if ((htmlDir == null) || (htmlDir.Length < 1))
                {
                    Logit.WriteError("HTML output directory must be specified.");
                    return false;
                }

                // Pass 1: navigation generation

                usfx = new XmlTextReader(usfxName);
                usfx.WhitespaceHandling = WhitespaceHandling.Significant;
                while (usfx.Read())
                {
                    if (usfx.NodeType == XmlNodeType.Element)
                    {
                        level = GetNamedAttribute("level");
                        style = GetNamedAttribute("style");
                        sfm = GetNamedAttribute("sfm");
                        caller = GetNamedAttribute("caller");
                        id = GetNamedAttribute("id");

                        switch (usfx.Name)
                        {
                            case "languageCode":
                                usfx.Read();
                                if (usfx.NodeType == XmlNodeType.Text)
                                    languageCode = usfx.Value;
                                break;
                            case "book":
                                hasContentsPage = false;
                                inHeader = true;
                                currentBookHeader = vernacularLongTitle = String.Empty;
                                if (id.Length > 2)
                                {
                                    currentBookAbbrev = id;
                                    bookRecord = (BibleBookRecord)bookInfo.books[currentBookAbbrev];
                                    bookRecord.toc = new StringBuilder();
                                }
                                if (id.CompareTo("PSA") == 0)
                                    chapFormat = "000";
                                else
                                    chapFormat = "00";
                                chapterNumber = 0;
                                verseNumber = 0;
                                if (bookInfo.isApocrypha(id))
                                    containsDC = true; // if we have any apocrypha, this is set true for use in the next pass
                                break;
                            case "id":
                                if (id.Length > 2)
                                    currentBookAbbrev = id;
                                bookRecord = (BibleBookRecord)bookInfo.books[currentBookAbbrev];
                                if (!usfx.IsEmptyElement)
                                    ignore = true;
                                break;
                            case "p":
                                if (sfm.CompareTo("mt") == 0)
                                {
                                    usfx.Read();
                                    if (usfx.NodeType == XmlNodeType.Text)
                                    {
                                        if (vernacularLongTitle.Length > 0)
                                            vernacularLongTitle = vernacularLongTitle + " " + EscapeHtml(usfx.Value.Trim());
                                        else
                                            vernacularLongTitle = EscapeHtml(usfx.Value.Trim());
                                    }
                                }
                                else if (sfm.CompareTo("ms") == 0)
                                {
                                    usfx.Read();
                                    if (usfx.NodeType == XmlNodeType.Text)
                                    {
                                        bookRecord.toc.Append(String.Format("<div class=\"toc1\"><a href=\"{0}{1}.htm#V{2}\">{3}</a></div>\r\n",
                                            currentBookAbbrev, Math.Max(1, chapterNumber).ToString(chapFormat),
                                            verseNumber.ToString(), usfx.Value.Trim()));
                                    }
                                    hasContentsPage = true;
                                }
                                else
                                {
                                    if (chapterNumber == 0)
                                        VirtualChapter();
                                    LeaveHeader();
                                }
                                break;
                            case "h":
                                usfx.Read();
                                if (usfx.NodeType == XmlNodeType.Text)
                                {
                                    currentBookHeader = EscapeHtml(usfx.Value.Trim());
                                    bookRecord.toc.Append(String.Format("<div class=\"toc1\"><a href=\"{0}{1}.htm\">{2}</a></div>\r\n",
                                        currentBookAbbrev, chapFormat.Substring(1) + "1",
                                        currentBookHeader));
                                    bookRecord.vernacularHeader = currentBookHeader;
                                }

                                break;
                            case "s":
                            case "d":
                                usfx.Read();
                                if (usfx.NodeType == XmlNodeType.Text)
                                {
                                    bookRecord.toc.Append(String.Format("<div class=\"toc2\"><a href=\"{0}{1}.htm#V{2}\">{3}</a></div>\r\n",
                                        currentBookAbbrev, Math.Max(1, chapterNumber).ToString(chapFormat),
                                        verseNumber.ToString(), usfx.Value.Trim()));
                                }
                                hasContentsPage = true;
                                break;
                            case "cl":
                                usfx.Read();
                                if (usfx.NodeType == XmlNodeType.Text)
                                {
                                    if (chapterNumber == 0)
                                        wordForChapter = usfx.Value.Trim();
                                    else
                                        currentChapterPublished = LocalizeNumerals(usfx.Value.Trim());
                                }
                                break;
                            case "c":
                                currentChapter = id;
                                currentChapterPublished = LocalizeNumerals(currentChapter);
                                currentChapterAlternate = String.Empty;
                                currentVerse = currentVersePublished = currentVerseAlternate = String.Empty;
                                verseNumber = 0;
                                if (!usfx.IsEmptyElement)
                                {
                                    usfx.Read();
                                    if (usfx.NodeType == XmlNodeType.Text)
                                        currentChapterPublished = LocalizeNumerals(usfx.Value.Trim());
                                }
                                int chNum;
                                if (Int32.TryParse(id, out chNum))
                                    chapterNumber = chNum;
                                else
                                    chapterNumber++;
                                bookRecord.numChapters = Math.Max(bookRecord.numChapters, chapterNumber);
                                LeaveHeader();
                                inPreverse = true;
                                chopChapter = true;
                                break;
                            case "cp":
                                if (!usfx.IsEmptyElement)
                                {
                                    usfx.Read();
                                    if (usfx.NodeType == XmlNodeType.Text)
                                        currentChapterPublished = LocalizeNumerals(usfx.Value.Trim());
                                }
                                break;
                            case "ca":
                                if (!usfx.IsEmptyElement)
                                {
                                    usfx.Read();
                                    if (usfx.NodeType == XmlNodeType.Text)
                                        currentChapterAlternate = usfx.Value.Trim();
                                }
                                break;
                            case "toc":
                                if (!usfx.IsEmptyElement)
                                {
                                    usfx.Read();
                                    if (usfx.NodeType == XmlNodeType.Text)
                                    {
                                        switch (level)
                                        {
                                            case "1":
                                                bookRecord.vernacularName = usfx.Value.Trim();
                                                break;
                                            case "2":
                                                bookRecord.shortName = usfx.Value.Trim();
                                                break;
                                            case "3":
                                                bookRecord.vernacularAbbreviation = usfx.Value.Trim();
                                                break;
                                        }
                                    }
                                }
                                break;
                            case "v":
                                currentVerse = id;
                                currentVersePublished = LocalizeNumerals(currentVerse);
                                currentVerseAlternate = "";
                                if (!usfx.IsEmptyElement)
                                {
                                    usfx.Read();
                                    if (usfx.NodeType == XmlNodeType.Text)
                                    {
                                        currentVersePublished = LocalizeNumerals(usfx.Value.Trim());
                                    }
                                }
                                int vnum;
                                if (Int32.TryParse(id, out vnum))
                                {
                                    verseNumber = vnum;
                                }
                                else
                                {
                                    verseNumber++;
                                }
                                StartVerse();
                                break;
                        }
                    }
                    else if (usfx.NodeType == XmlNodeType.EndElement)
                    {
                        switch (usfx.Name)
                        {
                            case "book":
                                // Write book table of contents
                                if (!hasContentsPage)
                                    bookRecord.toc.Length = 0;
                                bookRecord.vernacularName = vernacularLongTitle;
                                
                                bookList.Add(bookRecord);
                                break;
                            case "d":
                            case "s":
                                if (bookRecord.testament.CompareTo("x") == 0)
                                {
                                    if (chapterNumber == 0)
                                    {
                                        chapterNumber = 1;
                                        verseNumber = 1;
                                    }
                                    verseNumber++;
                                }
                                break;
                            case "mt":
                            case "h":
                                bookRecord.vernacularHeader = currentBookHeader;
                                break;
                        }
                    }
                    conversionProgress = "navigation " + currentBookAbbrev + " " + currentChapter + ":" + currentVerse;
                    System.Windows.Forms.Application.DoEvents();
                }
                usfx.Close();

				if (bookList.Count < 1)
				{
					Logit.WriteError("No books found to convert in " + usfxName);
					return false;
				}

                // Index page
                GenerateIndexFile(translationId, indexHtml, goText);

            	// Copyright page
                currentBookAbbrev = "CPR";
                bookListIndex = -1;
                OpenHtmlFile("copyright.htm");
                htm.WriteLine("<div class=\"main\">");
                bookListIndex = 0;
                bookRecord = (BibleBookRecord)bookList[0];
                if (bookRecord.tla.CompareTo("PSA") == 0)
                    chapFormat = "000";
                else
                    chapFormat = "00";
                htm.WriteLine("<div class=\"toc\"><a href=\"{0}.htm\">{1}</a></div>",
                    chapterFileList[0], goText);
                htm.WriteLine(licenseHtml);
                htm.WriteLine("<p>&nbsp;<br/><br/></p>");
                if (indexDateStamp != String.Empty)
                  htm.WriteLine("<div class=\"fine\">{0}</div>", indexDateStamp);
                CloseHtmlFile();
                indexDateStamp = String.Empty;

                // Pass 2: content generation

                usfx = new XmlTextReader(usfxName);
                usfx.WhitespaceHandling = WhitespaceHandling.All;

                chapterFileIndex = 0;
                bookListIndex = 0;
                while (usfx.Read())
                {
                    conversionProgress = "Generating HTML " + currentBookAbbrev + " " + currentChapter + ":" + currentVerse;
                    System.Windows.Forms.Application.DoEvents();

                    switch (usfx.NodeType)
                    {
                        case XmlNodeType.Element:
                            if (inUsfx)
                            {
                                level = GetNamedAttribute("level");
                                style = GetNamedAttribute("style");
                                sfm = GetNamedAttribute("sfm");
                                caller = GetNamedAttribute("caller");
                                id = GetNamedAttribute("id");

                                switch (usfx.Name)
                                {
                                    case "languageCode":
                                        usfx.Read();
                                        if (usfx.NodeType == XmlNodeType.Text)
                                            languageCode = usfx.Value;
                                        break;
                                    case "sectionBoundary":
                                    case "generated":
                                    case "rem":
                                    case "periph":
                                    case "fig": // Illustrations not yet supported
                                    case "ndx":
                                    case "w":
                                    case "wh":
                                    case "wg":
                                        if (!usfx.IsEmptyElement)
                                            ignore = true;
                                        break;
                                    case "book":
                                        currentBookAbbrev = id;
                                        chapterNumber = 0;
                                        verseNumber = 0;
                                        if (bookInfo.isApocrypha(id))
                                            containsDC = true; // probably redundant, should be set in previous pass if ANY apocrypha
                                        bookRecord = (BibleBookRecord)bookList[bookListIndex];
                                        currentBookHeader = bookRecord.vernacularHeader;
                                        hasContentsPage = bookRecord.toc.Length > 0;
                                        currentChapter = "";
                                        if (hasContentsPage)
                                        {
                                            currentChapterPublished = "0";
                                            OpenHtmlFile();
                                            htm.WriteLine("<div class=\"toc\"><a href=\"index.htm\">^</a></div>\r\n{0}",
                                                bookRecord.toc.ToString());
                                            CloseHtmlFile();
                                        }
                                        break;
                                    case "id":
                                        if (id.Length > 2)
                                            currentBookAbbrev = id;
                                        if (!usfx.IsEmptyElement)
                                            ignore = true;
                                        inHeader = true;
                                        newChapterFound = false;
                                        break;
                                    case "ide":
                                        // We could read attribute charset, here, if we would do anything with it.
                                        // Ideally, this would be read on first pass, and used as the encoding to read
                                        // the file on the second pass. However, we currently don't support anything
                                        // but utf-8, so it is kind of redundant, except for round-trip conversion back
                                        // to USFM.
                                        if (!usfx.IsEmptyElement)
                                            ignore = true;
                                        break;
                                    case "h":
                                        usfx.Read();
                                        if (usfx.NodeType == XmlNodeType.Text)
                                            currentBookHeader = EscapeHtml(usfx.Value.Trim());
                                        break;
                                    case "cl":
                                        usfx.Read();
                                        if (usfx.NodeType == XmlNodeType.Text)
                                        {
                                            if (chapterNumber == 0)
                                                wordForChapter = usfx.Value.Trim();
                                            else
                                                currentChapterPublished = LocalizeNumerals(usfx.Value.Trim());
                                        }
                                        break;
                                    case "p":
                                        EndHtmlTable();
                                        bool beforeVerse = true;
                                        if ((bookRecord.testament.CompareTo("x") == 0) ||
                                            (sfm.Length == 0) || (sfm == "p") || (sfm == "fp") ||
                                            (sfm == "nb") || (sfm == "cls") || (sfm == "li") ||
                                            (sfm == "lit") || (sfm == "m") || (sfm == "mi") ||
                                            (sfm == "pc") || (sfm == "pde") || (sfm == "pdi") ||
                                            (sfm == "ph") || (sfm == "phi") || (sfm == "pi") ||
                                            (sfm == "pm") || (sfm == "pmc") || (sfm == "pmo") ||
                                            (sfm == "pmr") || (sfm == "pr") || (sfm == "ps") ||
                                            (sfm == "psi") || (sfm == "qc") || (sfm == "qm") ||
                                            (sfm == "qr") || (sfm == "pr") || (sfm == "ps"))
                                            beforeVerse = false;
                                        if (ignoreIntros && ((sfm == "ip") || (sfm == "imt") || (sfm == "io") || (sfm == "is") || (sfm == "iot")))
                                            ignore = true;
                                        ProcessParagraphStart(beforeVerse);
										if (style == "Parallel Passage Reference")
											parallelPassage = ""; // start accumulating cross-ref data
                                        break;
                                    case "q":
                                    case "qs":  // qs is really a text style with paragraph attributes, but HTML/CSS can't handle that.
                                    case "b":
                                        EndHtmlTable();
                                        ProcessParagraphStart(false);
                                        break;
                                    case "mt":
                                    case "d":
                                    case "s":
                                        EndHtmlTable();
                                        ProcessParagraphStart(true);
                                        break;
                                    case "c":
                                        EndHtmlTable();
                                        ProcessChapter();
                                        break;
                                    case "cp":
                                        if (!usfx.IsEmptyElement)
                                        {
                                            usfx.Read();
                                            if (usfx.NodeType == XmlNodeType.Text)
                                                currentChapterPublished = LocalizeNumerals(usfx.Value.Trim());
                                        }
                                        break;
                                    case "ca":
                                        if (!usfx.IsEmptyElement)
                                        {
                                            usfx.Read();
                                            if (usfx.NodeType == XmlNodeType.Text)
                                                currentChapterAlternate = usfx.Value.Trim();
                                        }
                                        break;
                                    case "toc":
                                        if (!usfx.IsEmptyElement)
                                        {
                                            usfx.Read();
                                            if (usfx.NodeType == XmlNodeType.Text)
                                            {
                                                switch (level)
                                                {
                                                    case "1":
                                                        bookRecord.vernacularName = usfx.Value.Trim();
                                                        break;
                                                    case "2":
                                                        bookRecord.shortName = usfx.Value.Trim();
                                                        break;
                                                    case "3":
                                                        bookRecord.vernacularAbbreviation = usfx.Value.Trim();
                                                        break;
                                                }
                                            }
                                        }
                                        break;
                                    case "v":
                                        EndHtmlTable();
                                        if (chapterNumber == 0)
                                            VirtualChapter();
                                        ProcessVerse();
                                        break;
                                    case "va":
                                        EndHtmlTable();
                                        if (!usfx.IsEmptyElement)
                                        {
                                            usfx.Read();
                                            if (usfx.NodeType == XmlNodeType.Text)
                                                currentVerseAlternate = LocalizeNumerals(usfx.Value.Trim());
                                        }
                                        break;
                                    case "vp":
                                        EndHtmlTable();
                                        if (!usfx.IsEmptyElement)
                                        {
                                            usfx.Read();
                                            if (usfx.NodeType == XmlNodeType.Text)
                                                currentVersePublished = LocalizeNumerals(usfx.Value.Trim());
                                        }
                                        break;
                                    case "qt":
                                    case "nd":
                                    case "tl":
                                    case "qac":
                                    case "fm":
                                    case "sls":
                                    case "bk":
                                    case "pn":
                                    case "k":
                                    case "ord":
                                    case "add":
                                    case "sig":
                                    case "bd":
                                    case "it":
                                    case "bdit":
                                    case "sc":
                                    case "no":
                                    case "ior":
                                    case "wj":
                                    case "cs":
                                        if (sfm.Length == 0)
                                            sfm = usfx.Name;
                                        StartHtmlTextStyle(sfm);
                                        break;
                                    case "milestone":
                                        switch (sfm)
                                        {
                                            case "th":
                                                EndHtmlTableCol();
                                                StartHtmlTable();
                                                WriteHtml("<td><b>");
                                                inTableCol = true;
                                                inTableBold = true;
                                                break;
                                            case "tr":
                                                EndHtmlTableRow();
                                                StartHtmlTable();
                                                WriteHtml("<tr>");
                                                inTableRow = true;
                                                break;
                                            case "thr":
                                                EndHtmlTableCol();
                                                WriteHtml("<td align=\"right\"><b>");
                                                inTableBold = true;
                                                inTableCol = true;
                                                break;
                                            case "tc":
                                                EndHtmlTableCol();
                                                WriteHtml("<td>");
                                                inTableCol = true;
                                                break;
                                            case "tcr":
                                                EndHtmlTableCol();
                                                WriteHtml("<td align=\"right\">");
                                                inTableCol = true;
                                                break;
                                            case "hr":
                                                EndHtmlTable();
												WriteHorizontalRule();
                                          break;
                                        }
                                        break;

                                    case "f":
                                    case "x":
                                        if (ignoreNotes)
                                            ignore = true;
                                        StartHtmlNote(usfx.Name, caller);
                                        break;
                                    case "fk":
                                    case "fq":
                                    case "fqa":
                                    case "fv":
                                    case "ft":
                                    case "xo":
                                    case "xk":
                                    case "xt":
                                        StartHtmlNoteStyle(usfx.Name);
                                        break;
                                    case "xdc":
                                    case "fdc":
                                    case "dc":
										// Suppress these three fields unless there is apocrypha somewhere in the translation (detected in first pass)
                                        if (!containsDC)
                                            ignore = true;
                                        break;
                                    case "optionalLineBreak":
                                        WriteHtmlOptionalLineBreak();
                                        break;

                                }
                            }
                            else
                            {
                                if (usfx.Name == "usfx")
                                    inUsfx = true;
                            }
                            break;
                        case XmlNodeType.Whitespace:
                        case XmlNodeType.SignificantWhitespace:
                        case XmlNodeType.Text:
							if (parallelPassage != null)
								parallelPassage = parallelPassage + usfx.Value;
							else
								WriteHtmlText(usfx.Value);
                            break;
                        case XmlNodeType.EndElement:
                            if (inUsfx)
                            {
                                switch (usfx.Name)
                                {
                                    case "usfx":
                                        inUsfx = false;
                                        break;
                                    case "ide":
                                    case "generated":
                                    case "sectionBoundary":
                                    case "rem":
                                    case "periph":
                                    case "fig": // Illustrations not yet supported
                                    case "ndx":
                                    case "w":
                                    case "wh":
                                    case "wg":
                                    case "id":
                                        ignore = false;
                                        break;
                                    case "book":
                                        if (bookRecord.testament.CompareTo("x") == 0)
                                        {
                                            if (chapterNumber == 0)
                                            {
                                                chapterNumber++;
                                                if (htm == null)
                                                    OpenHtmlFile();
                                                if (preVerse.Length > 0)
                                                {
                                                    htm.WriteLine(preVerse.ToString());
                                                    preVerse = new StringBuilder(String.Empty);
                                                    inPreverse = false;
                                                }
                                            }
                                        }
                                        CloseHtmlFile();
                                        bookListIndex++;
                                        break;
                                    case "p":
										if (parallelPassage != null)
										{
											string crossRef = parallelPassage;
											parallelPassage = null; // stop accumulating cross ref info!
											// Escape it BEFORE we add the cross-ref markup, which may well include
											// special characters.
											WritUnescapedeHtmlText(ConvertCrossRefsToHotLinks(EscapeHtml(crossRef)));
										}
										goto case "mt";
                                    case "q":
                                    case "qs":  // qs is really a text style with paragraph attributes, but HTML/CSS can't handle that.
                                    case "b":
                                    case "mt":
										// also done for case "p" after possibly converting cross refs.
                                        EndHtmlParagraph();
                                        if (chopChapter)
                                            CloseHtmlFile();
                                        if (ignoreIntros)
                                            ignore = false;
                                        break;
                                    case "ms":
                                    case "d":
                                    case "s":
                                        if (bookRecord.testament.CompareTo("x") == 0)
                                        {
                                            if (chapterNumber == 0)
                                            {
                                                chapterNumber++;
                                            }
                                            if (htm == null)
                                                OpenHtmlFile();
                                            if (preVerse.Length > 0)
                                            {
                                                htm.WriteLine(preVerse.ToString());
                                                preVerse = new StringBuilder(String.Empty);
                                                inPreverse = false;
                                            }
                                            verseNumber++;
                                            htm.Write("<a name=\"C{0}V{1}\"></a>",
                                                chapterNumber.ToString(), verseNumber.ToString());
                                        }
                                        EndHtmlParagraph();
                                        if (chopChapter)
                                            CloseHtmlFile();
                                        break;
                                    case "qt":
                                    case "nd":
                                    case "tl":
                                    case "qac":
                                    case "fm":
                                    case "sls":
                                    case "bk":
                                    case "pn":
                                    case "k":
                                    case "ord":
                                    case "add":
                                    case "sig":
                                    case "bd":
                                    case "it":
                                    case "bdit":
                                    case "sc":
                                    case "no":
                                    case "ior":
                                    case "wj":
                                    case "cs":
                                        EndHtmlTextStyle();
                                        break;
                                    case "f":
                                    case "x":
                                        EndHtmlNote();
                                        if (ignoreNotes)
                                            ignore = false;
                                        break;
                                    case "fk":
                                    case "fq":
                                    case "fqa":
                                    case "fv":
                                    case "ft":
                                    case "xo":
                                    case "xk":
                                    case "xt":
                                        EndHtmlNoteStyle();
                                        break;
                                    case "xdc":
                                    case "fdc":
                                    case "dc":
                                        ignore = false;
                                        break;

                                }
                            }
                            break;

                    }

                    result = true;
                }
            }
            catch (Exception ex)
            {
                Logit.WriteError("Error converting " + usfxName + " to html in " + htmlDir + ": " + ex.Message);
                Logit.WriteLine(ex.StackTrace);
                Logit.WriteLine(currentBookAbbrev + " " + currentChapter + ":" + currentVerse);
                result = false;
            }
            finally
            {
                if (usfx != null)
                    usfx.Close();
            }
            conversionProgress = String.Empty;
            return result;
        }

    	protected virtual void GenerateIndexFile(string translationId, string indexHtml, string goText)
    	{
    		string chapFormat;
    		currentChapter = currentChapterPublished = "";
    		chapterNumber = 0;
    		bookListIndex = -1;
    		currentBookAbbrev = string.Empty;
    		currentBookHeader = string.Empty;
    		OpenHtmlFile(IndexFileName);
    		bookListIndex = 0;

    		/*if ((homeLinkHTML != null) && (homeLinkHTML.Trim().Length > 0))
                {
                    htm.WriteLine("<div class=\"dcbookLine\">{0}</div>", homeLinkHTML);
                }
                */
    		htm.WriteLine("<div class=\"bookList\">");
    		int i;
    		BibleBookRecord br;
    		string buttonClass = "bookLine";
    		string contentsName;
    		string chapFmt;
    		for (i = 0; i < bookList.Count; i++)
    		{
    			br = (BibleBookRecord) bookList[i];
    			if ((br.testament.CompareTo("a") == 0) || (br.testament.CompareTo("x") == 0))
    				buttonClass = "dcbookLine";
    			else
    				buttonClass = "bookLine";
    			if (br.tla == "PSA")
    				chapFmt = "000";
    			else
    				chapFmt = "00";
    			contentsName = br.tla + chapFmt + ".htm";
    			if (File.Exists(Path.Combine(htmDir, contentsName)))
    			{
    				htm.WriteLine("<div class=\"{0}\"><a href=\"{1}\">{2}</a></div>", buttonClass, contentsName, br.vernacularHeader);
    			}
    			else
    			{
    				foreach (string chapFile in chapterFileList)
    				{
    					if (chapFile.StartsWith(br.tla))
    					{
    						htm.WriteLine("<div class=\"{0}\"><a href=\"{1}.htm\">{2}</a></div>", buttonClass, chapFile, br.vernacularHeader);
    						break;
    					}
    				}
    			}
    		}
    		if ((copyrightLinkHTML != null) && (copyrightLinkHTML.Trim().Length > 0))
    		{
    			htm.WriteLine("<div class=\"dcbookLine\">{0}</div>", copyrightLinkHTML);
    		}
    		htm.WriteLine("</div>"); // End of bookList div
    		htm.WriteLine("<div class=\"mainindex\">");

    		bookRecord = (BibleBookRecord) bookList[0];
    		if (bookRecord.tla.CompareTo("PSA") == 0)
    			chapFormat = "000";
    		else
    			chapFormat = "00";
    		htm.WriteLine("<div class=\"toc\"><a href=\"{0}.htm\">{1}</a></div>",
    		              chapterFileList[0], goText);
    		htm.WriteLine(indexHtml, langId, translationId);
    		if (GeneratingConcordance)
    		{
    			htm.WriteLine("<div class=\"toc1\"><a href=\"conc/treeMaster.htm\">" + ConcordanceLinkText + "</a></div>");
    		}
    		htm.WriteLine("<p>&nbsp;<br/><br/></p>");
    		if (indexDateStamp != String.Empty)
    		{
    			htm.WriteLine("<div class=\"fine\">{0}</div>", indexDateStamp);
    		}
    		CloseHtmlFile();
    	}

    	/// <summary>
		/// Maps from book names that occur in cross-refs to the file name prefix used for that book
		/// e.g. 1 Corinthians -> 1Co, which will cause 1 Corinthians 3:5 to map to 1Co03#V5.
		/// Client must supply this to get cross-reference conversion.
		/// </summary>
    	public Dictionary<string, string> CrossRefToFilePrefixMap;

		/// <summary>
		/// Convert references to hot links. A simple input is something like "Lukas 12:3". Output would then be
		/// <a href="LUK12.htm#V3>Lucas 12:3</a>.
		/// The name to file previx conversion is performed by finding "Lukas" in CrossRefToFilePrefixMap, with the value "LUK".
		/// But it's more complicated than that; we get cases like
		/// (Mateos 26:26-29; Markus 14:22-25; Lukas 22:14-20) -- extra punctuation and multiple refs
		/// (Carita Ulang so'al Jalan Idop 4:35,39; 6:4) - name not found!
		/// (1 Korintus 5:1-13) - range!
		/// (Utusan dong pung Carita 22:6-16, 26:12-18) - list of refs in same book
		/// " Efesus 5:22, Kolose 3:18" - commas separate complete refs!
		/// "Hahuu (Jénesis , Kejadian ) 15:13-14; Ézodu (Keluaran ) 3:12" - book name is complex and has comma!
		/// The algorithm is:
		/// 0. Wherever a recognized book name occurs, change it to something definitely not containing problem
		/// punctuation: #%#%bookN#
		/// 1. Split the string at semi-colons or commas; handle each one independently,
		/// except if we get a book or chapter, remember for subsequent ones.
		/// 2. Each item from the above is split at commas. Consider all to come from same book and chapter, if later ones don't specify those.
		/// 3. In first of each comma group, search for a match for known book name. If found, or if we are carrying a book name forward, we can make a hot link.
		/// 4. Convert occurrences of #%#%bookN back.
		/// </summary>
		/// <param name="chunk1"></param>
		/// <returns></returns>
		private string ConvertCrossRefsToHotLinks(string chunk1)
		{
			if (CrossRefToFilePrefixMap == null || CrossRefToFilePrefixMap.Count == 0)
				return chunk1;
			string chunk = chunk1;
			int ibook = 0;
			Dictionary<string, string> subsBookToFile = new Dictionary<string, string>(CrossRefToFilePrefixMap.Count);
			foreach (string bookName in CrossRefToFilePrefixMap.Keys)
			{
				string subskey = "#%#%book" + ibook + "#"; // final hatch to prevent book1 matching book10 on convert back
				subsBookToFile[subskey] = CrossRefToFilePrefixMap[bookName];
				chunk = chunk.Replace(bookName, subskey);
				ibook++;
			}
			string[] mainRefs = chunk.Split(';');
			StringBuilder output = new StringBuilder(chunk.Length * 5 + 50);
			// Ref may be simple verse number, chapter:verse, verse-verse, chapter:verse-verse
			Regex reRef = new Regex("[0-9]+(:[0-9]+)?(-[0-9]+)?");
			Regex reNum = new Regex("[0-9]+");
			Regex reAlpha = new Regex(@"\w");
			string fileName = ""; // empty until we get a book name match.
			// This is both a default for books that don't have chapters, and also,
			// if we find a chapter in ANY reference, we keep it for subsequent ones.
			// This handles cases like Matt 26:3,4 (which gets split into two items by the comma).
			string chap = "1";
			foreach (string item in mainRefs)
			{
				// Put back the semi-colons we split on.
				if (output.Length > 0)
					output.Append(";");
				string[] refs = item.Split(',');
				bool fFirst = true;
				foreach (string target in refs)
				{
					if (!fFirst)
					{
						output.Append(","); // put these back too.
					}
					string match = "";
					int bookNameMatchOffset = -1;
					foreach (string bookName in subsBookToFile.Keys)
					{
						if (bookName.Length > match.Length)
						{
							int matchOffsetT = target.IndexOf(bookName);
							if (matchOffsetT >= 0)
							{
								bookNameMatchOffset = matchOffsetT;
								match = bookName;
								fileName = subsBookToFile[match];
							}
						}
					}
					if (fileName == "")
					{
						// haven't found a book name, here or in previous item; don't convert this item
						output.Append(target);
						fFirst = false;
						continue;
					}

					// Look for something like a reference. Also, check that we don't have
					// alphabetic text that did NOT match one of our books if we didn't match
					// a book; otherwise, something like Titus 4:2; Isaiah 12:3 makes both links
					// to Titus. Note that we take the last match for the reference, otherwise, 1 Timothy 2:4
					// finds the '1' as the reference. Grr.
					int startNumSearch = 0;
					if (bookNameMatchOffset >= 0)
						startNumSearch = bookNameMatchOffset + match.Length; // start searching after the book name if any
					MatchCollection matches = reRef.Matches(target, startNumSearch);
					Match m = null;
					if (matches.Count != 0)
						m = matches[matches.Count - 1];
					if (m == null || (bookNameMatchOffset < 0 && reAlpha.Match(target, 0, m.Index) != Match.Empty))
					{
						// Nothing looks like a reference, just output what we have.
						// Also, stop carrying book and chapter forward.
						fileName = "";
						chap = "1";
						output.Append(target);
						fFirst = false;
						continue;
					}
					// Construct the anchor.
					string[] parts = m.Value.Split(':');
					string anchor;
					string verse = parts[0];
					// Do NOT reset chap unless two parts; see above.
					if (parts.Length == 2)
					{
						chap = parts[0];
						verse = parts[1];
					}
					verse = reNum.Match(verse).Value; // Take the first number in the verse part.
					var htmName = HtmName(fileName, chap);
					anchor = string.Format("{0}#V{1}", htmName, verse);

					// The anchor starts at the beginning of the numeric reference, unless we
					// matched a book name, in which case, start at the beginning of if.
					int start = m.Index;
					if (bookNameMatchOffset >= 0)
					{
						start = bookNameMatchOffset;
					}
					InsertHotLink(output, target, m, start, anchor);
					fFirst = false;
				}
			}
			string result = output.ToString();
			ibook = 0;
			foreach (string bookName in CrossRefToFilePrefixMap.Keys)
			{
				result = result.Replace("#%#%book" + ibook + "#", bookName);
				ibook++;
			}

			return result;
		}

    	/// <summary>
		/// Given a book ID and a chapter number, generate the corresponding HTML file name.
		/// </summary>
		/// <param name="bookId"></param>
		/// <param name="chap"></param>
		/// <returns></returns>
		public static string HtmName(string bookId, int chap)
		{
			return HtmName(bookId, chap.ToString(CultureInfo.InvariantCulture));
		}

		/// <summary>
		/// Given a book ID and a chapter identifier string, generate the corresponding HTML file name.
		/// </summary>
		/// <param name="bookId"></param>
		/// <param name="chap"></param>
		/// <returns></returns>
    	public static string HtmName(string bookId, string chap)
    	{
    		if (chap.Length < 2)
    			chap = "0" + chap;
    		// We use 2 digits for chapter numbers in file names except for in the Psalms, where we use 3.
    		if (bookId.Substring(0, 3).ToLowerInvariant() == "psa" && chap.Length < 3)
    			chap = "0" + chap;
			string htmName = bookId + chap + ".htm";
    		return htmName;
    	}

    	/// <summary>
		/// Insert into the string builder a string formed by replacing (in input) from start to the end of what m matched
		/// with a hot link.
		/// The anchor to which it links is supplied; the body of the link is what was replaced.
		/// </summary>
		/// <param name="output"></param>
		/// <param name="input"></param>
		/// <param name="input"></param>
		/// <param name="m"></param>
		/// <param name="anchor"></param>
		private void InsertHotLink(StringBuilder output, string input, Match m, int start, string anchor)
		{
			// Put anything in the input before the reference
			output.Append(input.Substring(0, start));
			// The next bit will be part of the anchor, so start it.
			output.Append("<a href=\"");
			output.Append(anchor);
			output.Append("\">");
			// The bit that should be the text of the anchor: input from start to end of reference.
			output.Append(input.Substring(start, m.Index + m.Length - start));
			// terminate the anchor
			output.Append("</a>");
			// And add anything else, possibly final punctuation
			output.Append(input.Substring(m.Index + m.Length));
		}
    }

	public class SFConverter
	{
		static public Scriptures scripture;
		static public XMLini appIni;
		static public XMLini jobIni;
		static private string appHomeDir;

		static public string AppDir(string fName)
		{
			if (appHomeDir == null)
			{
				appHomeDir = Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath);
			}
			return (Path.Combine(appHomeDir,fName));
		}

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
            return fName;
        }

        static public void ProcessFilespec(string fileSpec)
        {
            ProcessFilespec(fileSpec, null);
        }

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

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Get one command line option from a switch. The option may or may not
		/// be separated from the switch by white space.
		/// </summary>
		/// -----------------------------------------------------------------------------------

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
