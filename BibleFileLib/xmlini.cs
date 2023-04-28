﻿using System;
using System.Collections;
using System.Xml;
using System.Xml.Schema;
using System.IO;

namespace WordSend
{

	/// <summary>
	/// The purpose of this class is to provide very simple persistence of variables
	/// in the manner of old .ini files, but without sections. Files are persisted in
	/// XML files based on ini.dtd. All variables are keyed based on a case-sensitive
	/// string. If this class gets heavily used, it should be refactored as a database
	/// rather than a simple XML file, but a simple file is easier to backup and
    /// restore.
	/// </summary>
	public class XMLini
	{
		private string fileName;
		private Hashtable hashTbl;
        public static bool readOnly = false;

        private bool ReadIniFile(string fName)
        {
            XmlTextReader xml = null;
            string k = null;
            string v = null;
            string elementName = null;
            bool result = false;

            if (!File.Exists(fName))
                return false;
            try
            {
                xml = new XmlTextReader(fName);
                xml.WhitespaceHandling = WhitespaceHandling.None;
                xml.MoveToContent();
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
                result = true;
            }
            catch
            {
                Logit.WriteLine("Error reading " + fName);
            }
            finally
            {
                if (xml != null)
                    xml.Close();
            }
            return result;
        }

        /// -----------------------------------------------------------------------------------
        /// <summary>
        /// Initializes a new instance of the <see cref="XMLini"/> class.
        /// </summary>
        /// -----------------------------------------------------------------------------------
        public XMLini(string iniName)
		{
			hashTbl = new Hashtable();
            string bakName = Path.ChangeExtension(iniName, ".bak");
			string dirName = Path.GetDirectoryName(iniName);
            if ((!String.IsNullOrEmpty(dirName)) && (!Directory.Exists(dirName)))
                Directory.CreateDirectory(dirName);

            // Open the named XML file and read in all entries into keys and values
			// arrays.
			fileName = iniName;
            
            if (File.Exists(fileName))
            {
                if (!ReadIniFile(fileName))
                {
                    fileHelper.Dally();
                    hashTbl = new Hashtable();
                    if (!ReadIniFile(fileName))
                    {
                        Logit.WriteError("Failed second read attempt on " + fileName + "; trying backup.");
                        fileHelper.Dally();
                        hashTbl = new Hashtable();
                        if (!ReadIniFile(bakName))
                            Logit.WriteError("Failed reading backup " + bakName);
                    }
                }
            }
            else if (File.Exists(bakName))
            {
                ReadIniFile(bakName);
            }
		}

        /// <summary>
        /// Using ~ as an escape character, ensure the output string has no less than, greater than, or ampersand characters
        /// in a reversible way. Expands the string when any of those 4 characters are in it.
        /// </summary>
        /// <param name="s">unencoded string</param>
        /// <returns>encoded string</returns>
        public static string encodeStringForXml(string s)
        {
            if ((s == null) || (s == String.Empty))
                return String.Empty;
            string result = fileHelper.fromPUA(s);
            result = result.Replace("~", "~~");
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
            string result = fileHelper.fromPUA(s);
            result = result.Replace("~a", "&");
            result = result.Replace("~g", ">");
            result = result.Replace("~l", "<");
            result = result.Replace("~~", "~");
            return result;
        }

        /// <summary>
        /// Write a string with value val under key
        /// </summary>
        /// <param name="key">name of string</param>
        /// <param name="val">string to write</param>
        public void WriteString(string key, string val)
        {
            hashTbl[key] = encodeStringForXml(val);
        }

        /// <summary>
        /// Write integer i with name key
        /// </summary>
        /// <param name="key">name of integer</param>
        /// <param name="i">value to write under key</param>
        public void WriteInt(string key, int i)
        {
            hashTbl[key] = i.ToString();
        }

        /// <summary>
        /// Write a boolean value under the given key
        /// </summary>
        /// <param name="key">name of boolean value</param>
        /// <param name="b">boolean value to write</param>
        public void WriteBool(string key, bool b)
        {
            hashTbl[key] = b.ToString();
        }

        /// <summary>
        /// Reads a string stored under key with given default value
        /// </summary>
        /// <param name="key">name of string</param>
        /// <param name="deflt">default value of string</param>
        /// <returns>string stored under key</returns>
        public string ReadString(string key, string deflt)
        {
            string result = unencodeStringForXml((string)hashTbl[key]);
            if (result == null)
            {
                result = deflt;
            }
            return result;
        }

        /// <summary>
        /// Read an integer stored under key
        /// </summary>
        /// <param name="key">name of the item</param>
        /// <param name="deflt">default value to return if none stored yet</param>
        /// <returns>integer stored under key</returns>
        public int ReadInt(string key, int deflt)
        {
            int result = deflt;
            try
            {
                string s = (string)hashTbl[key];
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

        /// <summary>
        /// Read boolean value with key name and default value
        /// </summary>
        /// <param name="key">Name of the item</param>
        /// <param name="deflt">Default value to return if none stored yet</param>
        /// <returns>Boolean value stored under key</returns>
        public bool ReadBool(string key, bool deflt)
        {
            bool result = deflt;
            try
            {
                string s = (string)hashTbl[key];
                if ((s == null) || (s == ""))
                    return deflt;
                result = bool.Parse(s);
            }
            catch
            {
            }
            return result;
        }

        /// <summary>
        /// Write a DateTime item to be retrieved with the given key
        /// </summary>
        /// <param name="key">name of the item</param>
        /// <param name="dt">DateTime to store</param>
        public void WriteDateTime(string key, DateTime dt)
        {
            hashTbl[key] = dt.ToString("o");
        }

        /// <summary>
        /// Read DateTime corresponding to key
        /// </summary>
        /// <param name="key">key -- name of this DateTime item</param>
        /// <param name="deflt">Default DateTime if none is stored, yet</param>
        /// <returns>DateTime item corresponding to key</returns>
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

        /// <summary>
        /// Flush the hash table to an XML file ("save").
        /// </summary>
        /// <returns>true iff success</returns>
        public bool TryWrite()
        {
            XmlTextWriter xml = null;
            bool result = false;
            string bakFileName = Path.ChangeExtension(fileName, "bak");
            try
            {
                Utils.DeleteFile(bakFileName);
                if (File.Exists(fileName))
                    File.Move(fileName, bakFileName);
                try
                {
                    xml = new XmlTextWriter(fileName, System.Text.Encoding.UTF8);
                    xml.WriteStartDocument();
                    xml.Formatting = Formatting.Indented;
                    xml.WriteStartElement("ini");

                    IDictionaryEnumerator enu = hashTbl.GetEnumerator();
                    while (enu.MoveNext())
                    {
                        xml.WriteStartElement("entry");
                        xml.WriteElementString("key", (string)enu.Key);
                        xml.WriteElementString("value", (string)enu.Value);
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
                Logit.WriteLine("Failed write to " + fileName);
            }
            return result;
        }


        /// <summary>
        /// Flush the hash table to an XML file ("save").
        /// </summary>
        /// <returns>true iff success</returns>
        public bool Write()
        {
            if (readOnly) return true;
            bool result = false;
            if (!TryWrite())
            {
                fileHelper.Dally();
                if (!TryWrite())
                    Logit.WriteError("Can't write to " + fileName);
                else
                    result = true;
            }
            else
                result = true;
            return result;
        }


        /// <summary>
        /// Returns the name of the executable file we are running in.
        /// </summary>
        /// <returns>The name of this executable file.</returns>
        public static string ExecutableName()
        {
            string result;
            try
            {
                result = System.IO.Path.GetFullPath(Environment.GetCommandLineArgs()[0]);
            }
            catch (Exception)
            {
                Logit.WriteError("Can't find complete executable name!");
                result = "haiola.exe";
            }
            return (result);
        }

        /// <summary>
        /// Returns the name of the My Documents or Documents folder for this OS
        /// </summary>
        /// <returns>The name of the default Documents folder</returns>
        public static string MyDocsDir()
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        }

        /// <summary>
        /// Returns the name of a folder used to store application data
        /// </summary>
        /// <returns>The path name of a folder to store application data in for this OS</returns>
        public static string AppDataDir()
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        }
    }
}