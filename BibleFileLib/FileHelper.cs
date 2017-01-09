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
    /// Helper functions and utilities related to files and text encoding.
    /// </summary>
    public class fileHelper
    {
        protected static readonly int CHUNK_SIZE = 1048576;

        /// <summary>
        /// Get an attribute from an element in an XmlTextReader or return an empty string
        /// (not null) if that attribute is missing.
        /// </summary>
        /// <param name="usfx">name of the XmlTextReader to get attributes from</param>
        /// <param name="attributeName">name of attribute to find the value of</param>
        /// <returns></returns>
        public static string GetNamedAttribute(XmlTextReader usfx, string attributeName)
        {
            string result = usfx.GetAttribute(attributeName);
            if (result == null)
                result = String.Empty;
            return result;
        }


        static string lockFileName;

        /// <summary>
        /// Reserves a project input directory for exclusive processing when multiple instances of Haiola are running.
        /// </summary>
        /// <param name="workDir">Name of the project input directory.</param>
        /// <returns>true iff the lock was successfully applied</returns>
        public static bool lockProject(string workDir)
        {
            bool result = false;
            StreamWriter lockFile;
            lockFileName = Path.Combine(workDir, "lock");
            try
            {
                if (!File.Exists(lockFileName))
                {
                    lockFile = new StreamWriter(lockFileName);
                    lockFile.WriteLine("locked");
                    lockFile.Close();
                    result = true;
                }
            }
            catch (Exception ex)
            {
                Logit.WriteError("ERROR reserving " + workDir + " for processing: " + ex.Message);
            }
            return result;
        }

        /// <summary>
        /// Companion to the lockProject function
        /// </summary>
        public static void unlockProject()
        {
            File.Delete(lockFileName);
        }


        /// <summary>
        /// Returns the Unicode file encoding if there are proper byte order marks;
        /// otherwise guesses if this is a UTF-8 (no BOM) file or ANSI file. The point
        /// of this function is to save a lot of manual text encoding conversion work.
        /// The accuracy is good at distinguishing Code Page 1252 and the main Unicode
        /// options. If it is something else, this function will guess wrong.
        /// </summary>
        /// <param name="fileName">file to determine text encoding of</param>
        /// <returns>best guess at the text encoding</returns>
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
                        // OK, so we don't have the fortune of byte order marks telling us, so we use
                        // some heuristics.
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
                                if ((chunk[i + 1] & 0xC0) == 0x80)
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
                                if (((chunk[i + 1] & 0xC0) == 0x80) && ((chunk[i + 2] & 0xC0) == 0x80))
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
                Logit.WriteError("Cannot read " + fileName + Environment.NewLine + err.Message +
                    Environment.NewLine + err.StackTrace);
            }
            // Logit.WriteLine("Identified " + fileName + " as " + result.ToString());
            return result;
        }

        /// <summary>
        /// Beware: Char.IsWhiteSpace(char) will return DIFFERENT results for the SAME data
        /// under Microsoft .NET and Mono if you ask it what a zero-width space is. This
        /// function just returns true for space, line ends, and horizontal tabs. This
        /// variant tests the character at the given index in a string.
        /// </summary>
        /// <param name="s">string to check a character in</param>
        /// <param name="index">index of character to check</param>
        /// <returns>true iff the character at index in s is space, carriage return, line feed, or tab</returns>
        public static bool IsNormalWhiteSpace(string s, int index)
        {
            return IsNormalWhiteSpace(s[index]);
        }

        /// <summary>
        /// Beware: Char.IsWhiteSpace(char) will return DIFFERENT results for the SAME data
        /// under Microsoft .NET and Mono if you ask it what a zero-width space is. This
        /// function just returns true for space, line ends, and horizontal tabs.
        /// </summary>
        /// <param name="ch">character to test</param>
        /// <returns>true iff ch is space, carriage return, line feed, or tab</returns>
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
        /// Returns "1" or "0" to indicate true or false in an SQL statement.
        /// </summary>
        /// <param name="boolean">variable to test</param>
        /// <returns>"1" if boolean is true, "0" if boolean is false</returns>
        public static string sqlBool(bool boolean)
        {
            if (boolean)
                return "1";
            return "0";
        }

        public static bool fAllRunning;
        public static string runCommandError;

        /// <summary>
        /// Runs a command like it was typed on the command line.
        /// Uses bash as the command interpretor on Linux & Mac OSX; cmd.exe on Windows.
        /// </summary>
        /// <param name="command">Command to run, with or without full path.</param>
        public static bool RunCommand(string command, string defaultDirectory = "")
        {
            if (!fileHelper.fAllRunning)
            {
                Logit.WriteError("Stopped before running " + command);
                return false;
            }
            runCommandError = String.Empty;
            System.Diagnostics.Process runningCommand = null;
            try
            {
                if (defaultDirectory.Length > 0)
                {
                    command = "cd " + defaultDirectory + " && " + command;
                }
                if (Path.DirectorySeparatorChar == '/')
                {
                    // Logit.WriteLine("Posix detected. Running bash -c " + command);
                    runningCommand = System.Diagnostics.Process.Start("bash", " -c '" + command + "'");
                }
                else
                {
                    if ((defaultDirectory.Length >= 2) && (defaultDirectory[1] == ':'))
                    {
                        command = defaultDirectory.Substring(0,2) + " && " + command;
                    }
                    runningCommand = System.Diagnostics.Process.Start("cmd.exe", " /c " + command);
                }
                if (runningCommand != null)
                {
                    // Logit.WriteLine("Waiting for command to complete.");
                    while (fileHelper.fAllRunning && !runningCommand.HasExited)
                    {
                        System.Windows.Forms.Application.DoEvents();
                        System.Threading.Thread.Sleep(200);
                    }
                    if ((!runningCommand.HasExited) && (!fileHelper.fAllRunning))
                    {
                        Logit.WriteLine("Killing command " + command);
                        runningCommand.Kill();
                    }
                }
            }
            catch (Exception ex)
            {
                runCommandError = ex.Message;
                Logit.WriteError(runCommandError);
                return false;
            }
            return true;
        }



        public static string escapeJsonString(string s)
        {
            return s.Replace("\"", "\\\"").Replace('\r', ' ').Replace('\n', ' ').Replace("  ", " ");
        }

        /// <summary>
        /// Escapes \ and "  as \\ and \", respectively.
        /// </summary>
        /// <param name="s">String to be escaped.</param>
        /// <returns>String with escape substitutions made.</returns>
        public static string csvString(string s)
        {
            s = s.Replace("\\", "\\\\");
            s = s.Replace("\"", "\\\"");
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

        // The following linguistically diverse digit sets might not display correctly unless
        // you have appropriate Unicode fonts installed.
        public const string ArabicDigits = "٠١٢٣٤٥٦٧٨٩";
        public const string BengaliDigits = "০১২৩৪৫৬৭৮৯";
        public const string ChineseSimplifiedDigits = "〇一二三四五六七八九";
        public const string ChineseTraditionalDigits = "零壹貳參肆伍陸柒捌玖";
        public const string ChineseHuaMaDigits = "〇〡〢〣〤〥〦〧〨〩";
        public const string CopticUnits = "\u2c81\u0305\u2C83\u0305\u2C85\u0305\u2C87\u0305\u2C89\u0305\u2C8B\u0305\u2C8D\u0305\u2C8F\u0305\u2C91\u0305";  // First 9 coptic letters with single overbar
        public const string CopticTens = "\u2C93\u0305\u2C95\u0305\u2C97\u0305\u2C99\u0305\u2C9B\u0305\u2C9D\u0305\u2C9F\u0305\u2CA1\u0305\u03E5\u0305";   // 10th thru 18th coptic letters with single overbar
        public const string CopticHundreds = "\u2CA3\u0305\u2CA5\u0305\u2CA7\u0305\u2CA9\u0305\u2CAB\u0305\u2CAD\u0305\u2CAF\u0305\u2CB1\u0305\u2CB3\u0305\u2CB5\u0305";  // 19th thru 27th coptic letters with single overbar
        public const string CopticThousands = "\u2c81\u033F\u2C83\u033F\u2C85\u033F\u2C87\u033F\u2C89\u033F\u2C8B\u033F\u2C8D\u033F\u2C8F\u033F\u2C91\u033F";	//	Thousands are the same as units, but double overbar.
		public const string CopticTenThousands = "\u2C93\u033F\u2C95\u033F\u2C97\u033F\u2C99\u033F\u2C9B\u033F\u2C9D\u033F\u2C9F\u033F\u2CA1\u033F\u03E5\u033F";	// Pattern continues: one bar added per period (10^3)
        public const string DevangariDigits = "०१२३४५६७८९";
        public const string EthiopicDigits = " ፩፪፫፬፭፮፯፰፱";
        public const string EthiopicTens = " ፲፳፴፵፶፷፸፹፺";
        public const string EthiopicHundred = "፻";
        public const string EthiopicTenThousand = "፼";
        public const string GugaratiDigits = "૦૧૨૩૪૫૬૭૮૯";
        public const string GurmukhiDigits = "੦੧੨੩੪੫੬੭੮੯";
        public const string KannadaDigits = "೦೧೨೩೪೫೬೭೮೯";
        public const string KhmerDigits = "០១២៣៤៥៦៧៨៩";
        public const string LaoDigits = "໐໑໒໓໔໕໖໗໘໙";
        public const string LimbuDigits = "᥆᥇᥈᥉᥊᥋᥌᥍᥎᥏";
        public const string MalayalamDigits = "൦൧൨൩൪൫൬൭൮൯";
        public const string MongolianDigits = "᠐᠑᠒᠓᠔᠕᠖᠗᠘᠙";
        public const string BurmeseDigits = "၀၁၂၃၄၅၆၇၈၉";
        public const string OriyaDigits = "୦୧୨୩୪୫୬୭୮୯";
        public const string PersianDigits = "۰۱۲۳۴۵۶۷۸۹";   // Same as Urdu digits
        public const string TamilDigits = "௦௧௨௩௪௫௬௭௮௯";
        public const string TeluguDigits = "౦౧౨౩౪౫౬౭౮౯";
        public const string ThaiDigits = "๐๑๒๓๔๕๖๗๘๙";
        public const string TibetanDigits = "༠༡༢༣༤༥༦༧༨༩";
        public const string UrduDigits = "۰۱۲۳۴۵۶۷۸۹";
        public const string RomanDigits = " ⅠⅡⅢⅣⅤⅥⅦⅧⅨ";
        protected static string CurrentDigits = String.Empty;

        public static string NumberSample()
        {
            if (!String.IsNullOrEmpty(CurrentDigits))
                return CurrentDigits;
            else
                return "0123456789";
        }

        /// <summary>
        /// true iff we are changing digits to an alternate writing system
        /// </summary>
        public static bool LocalizingDigits
        {
            get { return CurrentDigits != String.Empty; }
        }

        /// <summary>
        /// Set the locale for localizing digits for display in Bibles for verse numbers, etc.
        /// </summary>
        /// <param name="digitPlace">string with one of the exact names of supported digit sets</param>
        /// <returns>the set string if successful, or "Default" otherwise</returns>
        public static string SetDigitLocale(string digitPlace)
        {
            switch (digitPlace)
            {
                case "Arabic":
                    CurrentDigits = ArabicDigits;
                    break;
                case "Bengali":
                    CurrentDigits = BengaliDigits;
                    break;
                case "Burmese (Myanmar)":
                    CurrentDigits = BurmeseDigits;
                    break;
                case "Chinese (Simplified)":
                    CurrentDigits = ChineseSimplifiedDigits;
                    break;
                case "Chinese (Traditional)":
                    CurrentDigits = ChineseTraditionalDigits;
                    break;
                case "Chinese (hua ma)":
                    CurrentDigits = ChineseHuaMaDigits;
                    break;
                case "Coptic":
                    CurrentDigits = CopticUnits;
                    break;
                case "Devangari":
                    CurrentDigits = DevangariDigits;
                    break;
                case "Ethiopic (Ge'ez)":
                    CurrentDigits = EthiopicDigits;
                    break;
                case "Gujarati":
                    CurrentDigits = GugaratiDigits;
                    break;
                case "Gurmukhi":
                    CurrentDigits = GurmukhiDigits;
                    break;
                case "Kannada":
                    CurrentDigits = KannadaDigits;
                    break;
                case "Khmer":
                    CurrentDigits = KhmerDigits;
                    break;
                case "Lao":
                    CurrentDigits = LaoDigits;
                    break;
                case "Limbu":
                    CurrentDigits = LimbuDigits;
                    break;
                case "Malayalam":
                    CurrentDigits = MalayalamDigits;
                    break;
                case "Mongolian":
                    CurrentDigits = MongolianDigits;
                    break;
                case "Oriya":
                    CurrentDigits = OriyaDigits;
                    break;
                case "Roman":
                    CurrentDigits = RomanDigits;
                    break;
                case "Tamil":
                    CurrentDigits = TamilDigits;
                    break;
                case "Telugu":
                    CurrentDigits = TeluguDigits;
                    break;
                case "Thai":
                    CurrentDigits = ThaiDigits;
                    break;
                case "Tibetan":
                    CurrentDigits = TibetanDigits;
                    break;
                case "Persian":
                case "Urdu":
                    CurrentDigits = UrduDigits;
                    break;
                case "Hindu-Arabic":
                case "Default":
                default:
                    CurrentDigits = String.Empty;
                    digitPlace = "Default";
                    break;
            }
            return digitPlace;
        }

        /// <summary>
        /// Replaces all numbers with appropriate numbers in the current writing system
        /// </summary>
        /// <param name="s">string that might include numbers</param>
        /// <returns>string with numbers localized</returns>
        public static string LocalizeDigits(string s)
        {
            return ReplaceDigits(s, CurrentDigits);
        }

        /// <summary>
        /// Some writing systems just have exact equivalents for 0 through 9 and the same place values.
        /// Those are easy, with a simple digit-for-digit substitution. Others require some logic beyond
        /// that.
        /// </summary>
        /// <param name="s">String that may have digits to localize</param>
        /// <param name="newDigits">one of the supported digit strings</param>
        /// <returns></returns>
        public static string ReplaceDigits(string s, string newDigits)
        {   // TODO: implement logic for the different Chinese numeral systems, which require more than simple digit substitution, and which have many dialect and usage options.
            if ((newDigits == null) || (newDigits.Length < 10))
            {   // Nothing to do; no conversion specified
                return s;
            }
            if (newDigits == EthiopicDigits)
            {   // Gotta count differently
                return EthiopicNumerals(s);
            }
            else if (newDigits == RomanDigits)
            {   // Seriously old school
                return RomanNumerals(s);
            }
            else if (newDigits == CopticUnits)
            {   // Older than Roman Numerals, but not the same as Heirogliphic numbers or ancient Egyptian numbers
                return CopticNumerals(s);
            }
            else
            {   // Simple digit substitution with normal place values
                StringBuilder sb = new StringBuilder();
                int n;
                foreach (char c in s)
                {
                    if (Char.IsDigit(c))
                    {
                        n = ((int)c) - ((int)'0');
                        sb.Append(newDigits[n]);
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
                return sb.ToString();
            }
        }

        /// <summary>
        /// If the input ch is a localized digit in the string localDigits, return a digit in the range '0'-'9',
        /// otherwise return the input character.
        /// </summary>
        /// <param name="ch">Possible localized digit</param>
        /// <param name="localDigits">String of 0-9 in local digits.</param>
        /// <returns>Standardized digit or input character</returns>
        public static char StandardDigit(char ch)
        {
            char result = ch;
            int i = CurrentDigits.IndexOf(ch);
            if (i >= 0)
                result = (char)(i + (int)'0');
            return result;
        }

        /// <summary>
        /// Coptic numbers have no 0, but have different symbols for units, tens, hundreds, thousands, etc.
        /// </summary>
        /// <param name="s">Digits to convert less than or equal to 999</param>
        /// <returns>String with coptic numerals</returns>
        public static string CopticNumerals(string s)
        {
            StringBuilder sb = new StringBuilder();
                        int i, n;
            int place = 0;
            for (i = s.Length - 1; i >= 0; i--)
            {
                if (Char.IsDigit(s[i]))
                {
                    if (s[i] == '0')
                    {
                        place++;
                    }
                    else
                    {
                        n = 2 * (((int)s[i]) - ((int)'1'));
                        if (place == 0)
                        {
                            sb.Insert(0, CopticUnits[n + 1]); // Letter
                            sb.Insert(0, CopticUnits[n]);   // Combining overbar
                            place++;
                        }
                        else if (place == 1)
                        {
                            sb.Insert(0, CopticTens[n + 1]);
                            sb.Insert(0, CopticTens[n]);
                            place++;
                        }
                        else if (place == 2)
                        {
                            sb.Insert(0, CopticHundreds[n + 1]);
                            sb.Insert(0, CopticHundreds[n]);
                            place++;
                        }
                        else if (place == 3)
                        {
                            sb.Insert(0, CopticThousands[n + 1]);
                            sb.Insert(0, CopticThousands[n]);
                            place++;
                        }
                        else if (place == 4)
                        {
                            sb.Insert(0, CopticTenThousands[n + 1]);
                            sb.Insert(0, CopticTenThousands[n]);
                            place++;
                        }
                        else if (place >= 5)
                        {
                            return s;   // Give up and fail gracefully for numbers bigger than we designed for.
                        }
                    }
                }
                else
                {   // Non-digit: just copy it.
                    place = 0;
                    sb.Insert(0, s[i]);
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Writing big numbers without a 0 is different.
        /// </summary>
        /// <param name="s">String with numbers to localize to Ethiopic</param>
        /// <returns>String with numbers localized to Ethiopic Ge'ez.</returns>
        public static string EthiopicNumerals(string s)
        {
            StringBuilder sb = new StringBuilder();
            int i, n;
            int place = 0;
            for (i = s.Length - 1; i >= 0; i--)
            {
                if (Char.IsDigit(s[i]))
                {
                    n = ((int)s[i]) - ((int)'0');
                    if (place == 0)
                    {
                        if (n > 0)
                        {
                            sb.Insert(0, EthiopicDigits[n]);
                        }
                        place++;
                    }
                    else if (place == 1)
                    {
                        if (n > 0)
                        {
                            sb.Insert(0, EthiopicTens[n]);
                        }
                        place++;
                    }
                    else if (place == 2)
                    {
                        sb.Insert(0, EthiopicHundred);
                        if (n > 0)
                        {
                            sb.Insert(0, EthiopicDigits[n]);
                        }
                        place++;
                    }
                    else if (place == 3)
                    {
                        if (n > 0)
                        {
                            sb.Insert(0, EthiopicTens[n]);
                        }
                        place++;
                    }
                    else if (place == 4)
                    {
                        sb.Insert(0, EthiopicTenThousand);
                        if (n > 0)
                        {
                            sb.Insert(0, EthiopicDigits[n]);
                        }
                        place++;
                    }
                    else if (place == 5)
                    {
                        if (n > 0)
                        {
                            sb.Insert(0, EthiopicDigits[n]);
                        }
                        place = 0;
                    }
                }
                else
                {
                    place = 0;
                    sb.Insert(0, s[i]);
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Roman numerals kind of break down after 3,000 (MMM) in terms of common use,
        /// which seems to be limited to chapters and years in this decade. This function
        /// uses Unicode Roman numerals, but an alternate routine could easily be created
        /// that uses plain letters I, V, X, L, C, and M or i, v, x, l, c, and m by replacing
        /// or providing choices for the strings in RomanUnits in this method.
        /// </summary>
        /// <param name="s">string that may have numbers to "Romanize"</param>
        /// <returns>string with numbers as Roman numerals</returns>
        public static string RomanNumerals(string s)
        {
            string[,] RomanUnits = {{"","Ⅰ","Ⅱ","Ⅲ","Ⅳ","Ⅴ","Ⅵ","Ⅶ","Ⅷ","Ⅸ"},
            {"", "Ⅹ","ⅩⅩ","ⅩⅩⅩ","ⅩⅬ","Ⅼ","ⅬⅩ","ⅬⅩⅩ","ⅬⅩⅩⅩ","ⅩⅭ"},
            {"", "Ⅽ", "ⅭⅭ", "ⅭⅭⅭ", "ⅭⅮ", "Ⅾ", "ⅮⅭ", "ⅮⅭⅭ", "ⅮⅭⅭⅭ", "ⅩⅯ"},
            { "", "Ⅿ", "ⅯⅯ", "ⅯⅯⅯ", "ⅯV̅", "V̅", "V̅Ⅿ", "V̅ⅯⅯ", "V̅ⅯⅯⅯ", "ⅯX̅̅"}};
            StringBuilder sb = new StringBuilder();
            int i, n;
            int place = 0;
            for (i = s.Length - 1; i >= 0; i--)
            {
                if (Char.IsDigit(s[i]))
                {
                    n = ((int)s[i]) - ((int)'0');
                    sb.Insert(0, RomanUnits[place, n]);
                    place++;
                    if (place > 3)
                        place = 0;
                }
                else
                {
                    place = 0;
                    sb.Insert(0, s[i]);
                }
            }
            sb.Replace("ⅩⅡ", "Ⅻ");
            sb.Replace("ⅩⅠ", "Ⅺ");
            return sb.ToString();
        }

        static Random Rnd;

        public static string NoNull(string s)
        {
            if (String.IsNullOrEmpty(s))
                return String.Empty;
            return s;
        }

        /// <summary>
        /// Normalizes both Windows and Linux line ends to all Windows line ends.
        /// Replaces the given file with a normalized file.
        /// </summary>
        /// <param name="FileName">Name of text file to normalize</param>
        public static void NormalizeLineEnds(string FileName)
        {
            int ch;
            char u;
            try
            {
                if (Rnd == null)
                    Rnd = new Random();
                string tempFileName = FileName + "." + Rnd.Next().ToString() + ".tmp";
                StreamReader sr = new StreamReader(FileName);
                StreamWriter sw = new StreamWriter(tempFileName);
                ch = sr.Read();
                while (ch != -1)
                {
                    u = (char)ch;
                    if (u == '\n')
                    {
                        sw.Write(Environment.NewLine);
                    }
                    else if (u != '\r')
                    {
                        sw.Write(u);
                    }
                    ch = sr.Read();
                }
                sw.Close();
                sr.Close();
                // Replace the source file, but don't delete it until the new file is in place.
                string bakFileName = FileName + "." + Rnd.Next().ToString() + ".bak";
                File.Move(FileName, bakFileName);
                File.Move(tempFileName, FileName);
                File.Delete(bakFileName);
            }
            catch (Exception ex)
            {
                Logit.WriteError(ex.Message);
            }
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

        /// <summary>
        /// Copy a file if the source file exists and overwrite is allowed or destination file does not exist.
        /// </summary>
        /// <param name="src">Full path and name of source file</param>
        /// <param name="dst">Full path and name of destination file</param>
        public static void CopyFile(string src, string dst, bool overwrite)
        {
            if (File.Exists(src))
            {
                if (overwrite && File.Exists(dst))
                {
                    File.Delete(dst);
                }
                if (!File.Exists(dst))
                    File.Copy(src, dst, overwrite);
            }
        }

        /// <summary>
        /// Copy a file if the source file exists and destination file does not exist.
        /// </summary>
        /// <param name="src">Full path and name of source file</param>
        /// <param name="dst">Full path and name of destination file</param>
        public static void CopyFile(string src, string dst)
        {
            if (File.Exists(src) && !File.Exists(dst))
               File.Copy(src, dst, false);
        }

        /// Create the directory if it does not exist already. Return true if a problem occurs.
        /// </summary>
        /// <param name="destinationPath">path to create if it doesn't exist already</param>
        /// <returns>true iff the process failed</returns>
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

        public static string fromPUA(string s)
        {
            int v, i, badChar;
            StringBuilder sb = new StringBuilder(s.Length);
            for (i = 0; i < s.Length; i++)
            {
                v = (int)s[i];
                switch (v)
                {
                    case 0xE041:
                        v = 0x89E0;
                        break;
                    case 0xE042:
                        v = 0x89E2;
                        break;
                    case 0xE043:
                        v = 0x89DC;
                        break;
                    case 0xE044:
                        v = 0x89E6;
                        break;
                    case 0xE046:
                        v = 0x8A86;
                        break;
                    case 0xE047:
                        v = 0x8A7F;
                        break;
                    case 0xE048:
                        v = 0x8A61;
                        break;
                    case 0xE049:
                        v = 0x8A3F;
                        break;
                    case 0xE04A:
                        v = 0x8A77;
                        break;
                    case 0xE04B:
                        v = 0x7740; // or 0x8A82?
                        break;
                    case 0xE04C:
                        v = 0x8A84;
                        break;
                    case 0xE04D:
                        v = 0x8A75;
                        break;
                    case 0xE04E:
                        v = 0x8A83;
                        break;
                    case 0xE04F:
                        v = 0x8A81;
                        break;
                    case 0xE050:
                        v = 0x8A74;
                        break;

                    case 0xE207:
                        v = 0x1207;
                        break;
                    case 0xE247:
                        v = 0x1247;
                        break;
                    case 0xE287:
                        v = 0x1287;
                        break;
                    case 0xE347:
                        v = 0x1347;
                        break;
                    case 0xE408:
                        v = 0x1360;
                        break;
                    case 0xE020:
                        v = 0x1380;
                        break;
                    case 0xE022:
                        v = 0x1381;
                        break;
                    case 0xE024:
                        v = 0x1382;
                        break;
                    case 0xE025:
                        v = 0x1383;
                        break;
                    case 0xE028:
                        v = 0x1384;
                        break;
                    case 0xE02A:
                        v = 0x1385;
                        break;
                    case 0xE02C:
                        v = 0x1386;
                        break;
                    case 0xE02D:
                        v = 0x1387;
                        break;
                    case 0xE030:
                        v = 0x1388;
                        break;
                    case 0xE032:
                        v = 0x1389;
                        break;
                    case 0xE530:
                        v = 0x1390;
                        break;
                    case 0xE531:
                        v = 0x1391;
                        break;
                    case 0xE532:
                        v = 0x1392;
                        break;
                    case 0xE533:
                        v = 0x1395;
                        break;
                    case 0xE534:
                        v = 0x1396;
                        break;
                    case 0xE535:
                        v = 0x1397;
                        break;
                    case 0xE536:
                        v = 0x1398;
                        break;
                    case 0xE537:
                        v = 0x1399;
                        break;
                    case 0xE2AF:
                        v = 0x12AF;
                        break;
                    case 0xE2CF:
                        v = 0x12CF;
                        break;
                    case 0xE30F:
                        v = 0x130F;
                        break;
                    case 0xE31F:
                        v = 0x131f;
                        break;
                    case 0xE538:
                        v = 0x135F;
                        break;
                    case 0xE034:
                        v = 0x138A;
                        break;
                    case 0xE035:
                        v = 0x138B;
                        break;
                    case 0xE038:
                        v = 0x138C;
                        break;
                    case 0xE03A:
                        v = 0x138D;
                        break;
                    case 0xE03C:
                        v = 0x138E;
                        break;
                    case 0xE03D:
                        v = 0x138F;
                        break;
                    case 0xE20F:
                        v = 0x2D80;
                        break;
                    case 0xE21F:
                        v = 0x2D81;
                        break;
                    case 0xE22F:
                        v = 0x2D82;
                        break;
                    case 0xE237:
                        v = 0x2D83;
                        break;
                    case 0xE23F:
                        v = 0x2D84;
                        break;
                    case 0xE267:
                        v = 0x2D85;
                        break;
                    case 0xE277:
                        v = 0x2D86;
                        break;
                    case 0xE27F:
                        v = 0x2D87;
                        break;
                    case 0xE297:
                        v = 0x2D88;
                        break;
                    case 0xE29F:
                        v = 0x2D89;
                        break;
                    case 0xE2A7:
                        v = 0x2D8A;
                        break;
                    case 0xE2DF:
                        v = 0x2D8B;
                        break;
                    case 0xE2F7:
                        v = 0x2D8C;
                        break;
                    case 0xE2FF:
                        v = 0x2D8D;
                        break;
                    case 0xE307:
                        v = 0x2D8E;
                        break;
                    case 0xE327:
                        v = 0x2D8F;
                        break;
                    case 0xE32F:
                        v = 0x2D90;
                        break;
                    case 0xE337:
                        v = 0x2D91;
                        break;
                    case 0xE357:
                        v = 0x2D92;
                        break;
                    case 0xE078:
                        v = 0x2D93;
                        break;
                    case 0xE07A:
                        v = 0x2D94;
                        break;
                    case 0xE07C:
                        v = 0x2D95;
                        break;
                    case 0xE07D:
                        v = 0x2D96;
                        break;
                    case 0xE051:
                        v = 0x2DA9;
                        break;
                    case 0xE052:
                        v = 0x2DAA;
                        break;
                    case 0xE053:
                        v = 0x2DAB;
                        break;
                    case 0xE054:
                        v = 0x2DAC;
                        break;
                    case 0xE055:
                        v = 0x2DAD;
                        break;
                    case 0xE056:
                        v = 0x2DAE;
                        break;
                    case 0xE058:
                        v = 0x2DB0;
                        break;
                    case 0xE059:
                        v = 0x2DB1;
                        break;
                    case 0xE05A:
                        v = 0x2DB2;
                        break;
                    case 0xE05B:
                        v = 0x2DB3;
                        break;
                    case 0xE05C:
                        v = 0x2DB4;
                        break;
                    case 0xE05D:
                        v = 0x2DB5;
                        break;
                    case 0xE05E:
                        v = 0x2DB6;
                        break;
                    case 0xE060:
                        v = 0x2DB8;
                        break;
                    case 0xE061:
                        v = 0x2DB9;
                        break;
                    case 0xE062:
                        v = 0x2DBA;
                        break;
                    case 0xE063:
                        v = 0x2DBB;
                        break;
                    case 0xE064:
                        v = 0x2DBC;
                        break;
                    case 0xE065:
                        v = 0x2DBD;
                        break;
                    case 0xE066:
                        v = 0x2DBE;
                        break;
                    case 0xE000:
                        v = 0x2DC0;
                        break;
                    case 0xE001:
                        v = 0x2DC1;
                        break;
                    case 0xE002:
                        v = 0x2DC2;
                        break;
                    case 0xE003:
                        v = 0x2DC3;
                        break;
                    case 0xE004:
                        v = 0x2DC4;
                        break;
                    case 0xE005:
                        v = 0x2DC5;
                        break;
                    case 0xE006:
                        v = 0x2DC6;
                        break;
                    case 0xE008:
                        v = 0x2DC8;
                        break;
                    case 0xE009:
                        v = 0x2DC9;
                        break;
                    case 0xE00A:
                        v = 0x2DCA;
                        break;
                    case 0xE00B:
                        v = 0x2DCB;
                        break;
                    case 0xE00C:
                        v = 0x2DCC;
                        break;
                    case 0xE00D:
                        v = 0x2DCD;
                        break;
                    case 0xE00E:
                        v = 0x2DCE;
                        break;
                    case 0xE010:
                        v = 0x2DD0;
                        break;
                    case 0xE011:
                        v = 0x2DD1;
                        break;
                    case 0xE012:
                        v = 0x2DD2;
                        break;
                    case 0xE013:
                        v = 0x2DD3;
                        break;
                    case 0xE014:
                        v = 0x2DD4;
                        break;
                    case 0xE015:
                        v = 0x2DD5;
                        break;
                    case 0xE016:
                        v = 0x2DD6;
                        break;
                    case 0xE018:
                        v = 0x2DD8;
                        break;
                    case 0xE019:
                        v = 0x2DD9;
                        break;
                    case 0xE01A:
                        v = 0x2DDA;
                        break;
                    case 0xE01B:
                        v = 0x2DDB;
                        break;
                    case 0xE01C:
                        v = 0x2DDC;
                        break;
                    case 0xE01D:
                        v = 0x2DDD;
                        break;
                    case 0xE01E:
                        v = 0x2DDE;
                        break;
                    case 0xE231:
                        v = 0xAB01;
                        break;
                    case 0xE232:
                        v = 0xAB02;
                        break;
                    case 0xE233:
                        v = 0xAB03;
                        break;
                    case 0xE234:
                        v = 0xAB04;
                        break;
                    case 0xE235:
                        v = 0xAB05;
                        break;
                    case 0xE236:
                        v = 0xAB06;
                        break;
                    case 0xE2F9:
                        v = 0xAB09;
                        break;
                    case 0xE2FA:
                        v = 0xAB0A;
                        break;
                    case 0xE2FB:
                        v = 0xAB0B;
                        break;
                    case 0xE2FC:
                        v = 0xAB0C;
                        break;
                    case 0xE2FD:
                        v = 0xAB0D;
                        break;
                    case 0xE2FE:
                        v = 0xAB0E;
                        break;
                    case 0xE2D9:
                        v = 0xAB11;
                        break;
                    case 0xE2DA:
                        v = 0xAB12;
                        break;
                    case 0xE2DB:
                        v = 0xAB13;
                        break;
                    case 0xE2DC:
                        v = 0xAB14;
                        break;
                    case 0xE2DD:
                        v = 0xAB15;
                        break;
                    case 0xE2DE:
                        v = 0xAB16;
                        break;
                    case 0xE328:
                        v = 0xAB20;
                        break;
                    case 0xE329:
                        v = 0xAB21;
                        break;
                    case 0xE32A:
                        v = 0xAB22;
                        break;
                    case 0xE32B:
                        v = 0xAB23;
                        break;
                    case 0xE32C:
                        v = 0xAB24;
                        break;
                    case 0xE32D:
                        v = 0xAB25;
                        break;
                    case 0xE32E:
                        v = 0xAB26;
                        break;
                    case 0xE338:
                        v = 0xAB28;
                        break;
                    case 0xE339:
                        v = 0xAB29;
                        break;
                    case 0xE33A:
                        v = 0xAB2A;
                        break;
                    case 0xE33B:
                        v = 0xAB2B;
                        break;
                    case 0xE33C:
                        v = 0xAB2C;
                        break;
                    case 0xE33D:
                        v = 0xAB2D;
                        break;
                    case 0xE33E:
                        v = 0xAB2E;
                        break;

                    case 0xE301:
                        v = ' ';
                        break;
                    case 0xE308:
                        v = ' ';
                        break;
                    case 0xE30E:
                        v = ' ';
                        break;
                    case 0xE310:
                        v = ' ';
                        break;
                    case 0xE311:
                        v = ' ';
                        break;
                    case 0xE313:
                        v = ' ';
                        break;
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
                {
                    badChar = v;
                    Logit.WriteError("WARNING: PUA character U+" + badChar.ToString("X") + "='" + (char)badChar + "' found.");
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// This function's purpose in life is to help eradicate deprecated Unicode SIL PUA characters by
        /// replacing them with their new, improved, fully standard Unicode equivalents. The substitutions
        /// are hard coded, converted to C# from a list provided by SIL GPS. It makes corrections in place
        /// by overwriting the file with the revised version.
        /// </summary>
        /// <param name="fName">name of file to update encoding in</param>
        public static void revisePua(string fName)
        {
            // int u;
            int v, i;
            try
            {
                StreamReader sr = new StreamReader(fName);
                string s;
                s = sr.ReadToEnd();
                sr.Close();
                StreamWriter sw = new StreamWriter(fName);
                sw.Write(fromPUA(s));
                sw.Close();
            }
            catch (Exception ex)
            {
                Logit.WriteError("Unable to correct PUA encoding in " + fName + Environment.NewLine + ex.Message);
            }
        }
    }
}
