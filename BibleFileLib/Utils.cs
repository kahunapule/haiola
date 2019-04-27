using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Security.Cryptography;

namespace WordSend
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

        /// <summary>
        /// True iff a given string is empty of except for white space, i. e. looks empty
        /// </summary>
        /// <param name="s">string to check for emptiness</param>
        /// <returns>true iff string is empty except for possible white space</returns>
        public static bool IsEmpty(string s)
        {
            return (String.IsNullOrEmpty(s.Trim()));
        }


        /// <summary>
        /// Returns the maximum word length found in the string s.
        /// </summary>
        /// <param name="s">String with words in it.</param>
        /// <returns>Length of the longest word in the string s</returns>
        public static int MaxWordLength(string s)
        {
            int result = 0;
            if (string.IsNullOrEmpty(s))
                return result;
            int wordLength = 0;
            int i;
            for (i = 0; i < s.Length; i++)
            {
                if (!fileHelper.IsNormalWhiteSpace(s[i]))
                {
                    wordLength++;
                }
                else
                {
                    if (wordLength > result)
                        result = wordLength;
                    wordLength = 0;
                }
            }
            if (wordLength > result)
                result = wordLength;
            return result;
        }

        public static string zeroPad(int minSize, string s)
        {
            string result = s;
            while (result.Length < minSize)
                result = "0" + result;
            return result;
        }

        public static void DeleteDirectory(string destinationPath)
        {
            try
            {
                if (Directory.Exists(destinationPath))
                {
                    Directory.Delete(destinationPath, true);
                }
                if (File.Exists(destinationPath))
                {
                    File.Delete(destinationPath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(String.Format("Unable to delete directory {0}. Details: {1}", destinationPath, ex.Message), "Error");
            }
        }


        public static void DeleteFile(string destinationPath)
        {
            try
            {
                if (File.Exists(destinationPath))
                {
                    File.Delete(destinationPath);
                }
                if (Directory.Exists(destinationPath))
                {
                    Directory.Delete(destinationPath, true);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(String.Format("Unable to delete file {0}. Details: {1}", destinationPath, ex.Message), "Error");
            }
        }

        /// Create the directory if it does not exist already. Return true if a problem occurs.
        /// </summary>
        /// <param name="destinationPath"></param>
        /// <returns></returns>
        public static bool EnsureDirectory(string destinationPath)
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

        /// <summary>
        /// Computes the SHA1 hash of a file.
        /// </summary>
        /// <param name="FileName">File name (including path) to hash</param>
        /// <returns>Hexadecimal string of hash (40 digits)</returns>
        public static string SHA1HashFile(string FileName)
        {
            byte[] hash;
            StringBuilder sb = new StringBuilder();
            SHA1 sha = new SHA1CryptoServiceProvider();
            FileStream fs = new FileStream(FileName, FileMode.Open);
            hash = sha.ComputeHash(fs);
            fs.Close();
            foreach (byte b in hash)
            {
                sb.Append(b.ToString("x2"));
            }
            return sb.ToString();
        }

        /// <summary>
        /// Compute an SHA1 hash of an arbitrary string
        /// </summary>
        /// <param name="stuffToHash">String to hash</param>
        /// <returns>Hexadecimal string of hash (40 digits)</returns>
        public static string SHA1HashString(string stuffToHash)
        {
            byte[] hash;
            byte[] utf8bytes = System.Text.Encoding.UTF8.GetBytes(stuffToHash);
            StringBuilder sb = new StringBuilder();
            SHA1 sha = new SHA1CryptoServiceProvider();
            hash = sha.ComputeHash(utf8bytes);
            foreach (byte b in hash)
            {
                sb.Append(b.ToString("x2"));
            }
            return sb.ToString();
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

    /// <summary>
    /// This class exists primarily to allow simpler use of the SharpZipLib function to
    /// add a file to a .zip file AND to control the compression mode.
    /// </summary>
    public class FileDataSource : ICSharpCode.SharpZipLib.Zip.IStaticDataSource
    {
        private string FileName;    // Name of the file we are opening into a stream

        public FileDataSource(string fileName)
        {
            FileName = fileName;
        }

        public Stream GetSource()
        {
            return new FileStream(FileName, FileMode.Open);
        }
    }
}
