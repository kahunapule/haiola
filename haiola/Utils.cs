using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using System.Xml;

namespace sepp
{
	public class Utils
	{
		/// <summary>
		/// Given a single pathname ending in X.Y, find all the files matching X-Z.Y,
		/// and return them. Typically the Z's are numeric, and the numeric ones should be
		/// returned in numeric order. There may be one non-numeric item (typically TOC)
		/// which should be returned first.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
        /*
		public static List<string> ChapFiles(string pattern)
		{
			string dirname = Path.GetDirectoryName(pattern);
			string filename = Path.GetFileName(pattern);
			string ext = Path.GetExtension(filename);
			filename = Path.GetFileNameWithoutExtension(filename);
			List<string> files = new List<string>(Directory.GetFiles(dirname, filename + "-*" + ext));
			files.Sort(CompareFilesBySuffix);
			for (int i = 0; i < files.Count; i++)
				files[i] = Path.Combine(dirname, files[i]);
			return files;
		}
       
		private static int CompareFilesBySuffix(string x, string y)
		{
			return GetSeq(x).CompareTo(GetSeq(y));
		}
 
		private static int GetSeq(string x)
		{
			int indexOfHyphen = x.LastIndexOf("-");
			int indexOfDot = x.LastIndexOf(".");
			string key = x.Substring(indexOfHyphen + 1, indexOfDot - indexOfHyphen - 1);
			int result = -1;
			Int32.TryParse(key, out result);
			return result;
		}
        */

		/// <summary>
		/// When working in chapter-per-file mode, call this to obtain the file without the added piece.
		/// </summary>
		/// <param name="filename"></param>
		/// <returns></returns>
        /*
		public static string MainFileName(string filename)
		{
			int indexOfHyphen = filename.LastIndexOf("-");
			int indexOfDot = filename.LastIndexOf(".");
			return filename.Remove(indexOfHyphen, indexOfDot - indexOfHyphen);
		}
        */


        public static void DeleteDirectory(string destinationPath)
        {
            if (Directory.Exists(destinationPath))
            {
                try
                {
                    Directory.Delete(destinationPath, true);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(String.Format("Unable to delete directory {0}. Details: {1}", destinationPath, ex.Message), "Error");
                }
            }
        }

		/// Create the directory if it does not exist already. Return true if a problem occurs.
		/// </summary>
		/// <param name="destinationPath"></param>
		/// <returns></returns>
		internal static bool EnsureDirectory(string destinationPath)
		{
			if (!Directory.Exists(destinationPath))
			{
				try
				{
					Directory.CreateDirectory(destinationPath);
				}
				catch (Exception ex)
				{
					MessageBox.Show(String.Format("Unable to create directory {0}. Details: {1}", destinationPath, ex.Message), "Error");
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Fix the string to be safe in a text region of XML.
		/// </summary>
		/// <param name="sInput"></param>
		/// <returns></returns>

		public static string MakeSafeXml(string sInput)
		{
			string sOutput = sInput;

			if (sOutput != null && sOutput.Length != 0)
			{
				sOutput = sOutput.Replace("&", "&amp;");
				sOutput = sOutput.Replace("<", "&lt;");
				sOutput = sOutput.Replace(">", "&gt;");
			}
			return sOutput;
		}

		public static string AttVal(XmlNode node, string name, string defVal)
		{
			XmlAttribute att = node.Attributes[name];
			if (att != null)
				return att.Value;
			return defVal;
		}

		public static int IntAttVal(XmlNode node, string name, int defVal)
		{
			XmlAttribute att = node.Attributes[name];
			if (att != null)
			{
				try
				{
					return Int32.Parse(att.Value);
				}
				catch(Exception)
				{
					// if anything goes wrong use the default.
				}
			}
			return defVal;
		}

		/// <summary>
		/// Set dictionary to contain each of the space-separated words in the list (with value true).
		/// </summary>
		/// <param name="dict"></param>
		/// <param name="list"></param>
		public static void BuildDictionary(Dictionary<string, bool> dict, string list)
		{
			dict.Clear();
			foreach (string key in list.Split(' '))
				dict[key] = true;
		}

        /*
        public static string ExePath
        {
            get
            {
                return Path.GetDirectoryName(System.IO.Path.GetFullPath(Environment.GetCommandLineArgs()[0]));
            }
        }
        */
        /// <summary>
        /// Get a full path for the required file. In debug builds it is often found in ..\..\X; in an installed release build it is in the same directory
        /// as the program. Throw an exception if not found at either place.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        /* replaced by SFConverter.FindAuxFile
        public static string GetUtilityFile(string fileName)
        {
            if (File.Exists(fileName))
                return fileName;
            string fileDir = Path.GetDirectoryName(fileName);
            string nameOnly = Path.GetFileName(fileName);
            string filePath = Path.Combine(fileDir, "..");
            string result = Path.Combine(filePath, nameOnly);
            if (File.Exists(result))
                return result;
            filePath = Path.Combine(filePath, "..");
            result = Path.Combine(filePath, nameOnly);
            if (File.Exists(result))
                return result;
            result = Path.Combine(ExePath, nameOnly);
            if (File.Exists(result))
                return result;
            result = Path.Combine(Path.Combine(Path.Combine(ExePath, ".."), ".."), nameOnly);
            if (File.Exists(result))
                return result;
            throw new Exception("Could not find required file " + fileName);
        }
        */
        /* Unused
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
        
        */
	}
}
