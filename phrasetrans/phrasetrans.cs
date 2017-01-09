using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.IO;

namespace phrasetrans
{

    class newPhrase
    {
        public string phrase;
        public int count;

        public newPhrase()
        {
            phrase = String.Empty;
            count = 0;
        }

        public newPhrase(string replaceWith)
        {
            phrase = replaceWith;
            count = 0;
        }
    }

    public class transPhrases
    {
        public static bool IsNormalWhiteSpace(char ch)
        {
            return (ch == ' ') || (ch == '\r') || (ch == '\n') || (ch == '\t');
        }

        public static int CountTokensInString(string s)
        {
            int result = 1;
            int i, j;
            if (s != null)
            {
                for (i = 0; i < s.Length; i++)
                {
                    if (Char.IsLetter(s[i]))
                    {
                        result++;
                        j = i + 1;
                        while ((j < s.Length) && (Char.IsLetter(s[j])))
                        {
                            i++; j++;
                        }
                    }
                    else if (s[i] == '<')
                    {
                        result++;
                        while ((i < s.Length) && (s[i] != '>'))
                        {
                            i++;
                        }
                    }
                    else if (IsNormalWhiteSpace(s[i]))
                    {
                        result++;
                        j = i + 1;
                        while ((j < s.Length) && (IsNormalWhiteSpace(s[i])))
                        {
                            i++; j++;   // Skip redundant spaces
                        }
                    }
                    else
                    {
                        result++;
                    }
                }
            }
            return result;
        }

        protected Hashtable substTable;
        protected int searchWidth = 2;
        protected long charsRead;
        protected string currentBook = String.Empty;
        protected string currentChapter = "0";
        protected string currentVerse = "0";
        public bool isPohnpeianUpdate = false;


        /// <summary>
        /// Reads one token from an XML file opened in a StreamReader sr.
        /// Each XML start or end element or entity is treated as a token.
        /// Words, punctuation, and spaces are treated as tokens.
        /// Any contiguous run of white space (space, tab, CR, LF) are treated as one space.
        /// Reads past end of file return an empty string. Otherwise, at least one character
        /// is returned.
        /// </summary>
        /// <param name="sr">StreamReader to read from.</param>
        /// <returns>One word, space, XML element, XML end element, or XML entity.</returns>
        protected string readToken(StreamReader sr)
        {
            StringBuilder sb = new StringBuilder();
            string s;
            int ch;
            if (!sr.EndOfStream)
            {
                ch = sr.Read();
                if (ch == (int)'<')
                {
                    sb.Append((char)ch);
                    do
                    {
                        ch = sr.Read();
                        if (ch > 0)
                            sb.Append((char)ch);
                    } while ((ch > 0) && (ch != (int)'>'));
                    s = sb.ToString();
                    if (s.StartsWith("<book id="))
                    {
                        currentBook = s.Substring(10, 3);
                        currentChapter = "0";
                        currentVerse = "0";
                    }
                    else if (s.StartsWith("<c id="))
                    {
                        currentChapter = s.Substring(7);
                        currentChapter = currentChapter.Substring(0, currentChapter.IndexOf('\"'));
                        currentVerse = "0";
                    }
                    else if (s.StartsWith("<v id="))
                    {
                        s = s.Substring(7);
                        currentVerse = s.Substring(0, s.IndexOf('\"'));
                    }

                }
                else if (ch == (int)'&')
                {
                    sb.Append((char)ch);
                    do
                    {
                        ch = sr.Read();
                        if (ch > 0)
                            sb.Append((char)ch);
                    } while ((ch > 0) && (ch != (int)';'));
                }
                else if (IsNormalWhiteSpace((char)ch))
                {
                    sb.Append(' '); // Normalize any normal white space to simple space.
                    while (IsNormalWhiteSpace((char)sr.Peek()))
                        sr.Read();  // Discard redundant white space.
                }
                else if (Char.IsLetter((char)ch))
                {
                    sb.Append((char)ch);
                    while (Char.IsLetter((char)sr.Peek()))
                    {
                        sb.Append((char)sr.Read());
                    }
                }
                else
                {
                    sb.Append((char)ch);
                }
            }
            charsRead += sb.Length;
            s = sb.ToString();
            if (isPohnpeianUpdate)
            {
                if (!(s.StartsWith("<") || s.StartsWith("&")))
                {
                    s = s.Replace('j', 's').Replace('J', 'S');
                }
            }
            return s;
        }

        public void filter(string inFileName, string outFileName, string transFileName)
        {
            string[] choppedLine = { "", "", "", "" };
            ArrayList al;
            int i, j, linewidth;
            string match;
            string logFile = Path.Combine(Path.GetDirectoryName(outFileName), "phrasetranslog.txt");
            bool found;
            newPhrase np;
            charsRead = 0;

            try 
	        {
                substTable = new Hashtable(1021);
                Console.WriteLine("Reading phrase translation file {0}", transFileName);
		        StreamReader sr = new StreamReader(transFileName);
                string line;
                while (!sr.EndOfStream)
                {
                    line = sr.ReadLine();
                    if ((line != null) && (line.Length > 5))
                    {
                        choppedLine = line.Split(new char[] { line[0] });
                        if ((choppedLine.Length >= 3) && (choppedLine[1].Length > 0))
                        {
                            np = new newPhrase(choppedLine[2]);
                            substTable.Add(choppedLine[1], np);
                            searchWidth = Math.Max(searchWidth, CountTokensInString(choppedLine[1]));
                        }
                    }
                }
                sr.Close();
                // Now that the substitution table is in the hash table, process the input file.
                al = new ArrayList(searchWidth);
                sr = new StreamReader(inFileName);
                StreamWriter sw = new StreamWriter(outFileName);
                Console.WriteLine("Reading {0} and writing {1}.", inFileName, outFileName);
                StreamWriter log = new StreamWriter(logFile);
                long fileLen = sr.BaseStream.Length;
                string s;
                linewidth = 0;
                do
                {
                    // (Re)fill list of strings to match
                    while (al.Count <= searchWidth)
                    {
                        al.Add(readToken(sr));
                    }
                    // Console.Write("{0}/{1}  {2}%   \r", charsRead, fileLen, 100 * charsRead / fileLen);
                    j = searchWidth;
                    found = false;
                    do
                    {
                        match = String.Empty;
                        for (i = 0; i < j; i++)
                        {
                            match = match + (string)al[i];
                        }
                        // Console.WriteLine(match);
                        if (substTable.Contains(match))
                        {
                            found = true;
                            np = (newPhrase)substTable[match];
                            sw.Write(np.phrase);
                            np.count++;
                            linewidth += np.phrase.Length;
                            for (i = 0; i < j; i++)
                            {
                                al.RemoveAt(0);
                            }
                            s = String.Format("{0} {1}:{2} {3} -> {4}   ",
                                currentBook, currentChapter, currentVerse,
                                match, np.phrase);
                            Console.WriteLine(s);
                            log.WriteLine(s);
                        }
                        j--;
                    } while ((j > 0) && !found);
                    if (!found)
                    {
                        s = (string)al[0];
                        if (s.Length > 0)
                        {
                            if (s == " ")
                            {
                                if ((linewidth > 100) || (((string)al[1])[0] == '<'))
                                {
                                    sw.WriteLine();
                                    linewidth = 0;
                                }
                                else
                                {
                                    sw.Write(s);
                                    linewidth++;
                                }
                            }
                            else
                            {
                                sw.Write(s);
                                linewidth += s.Length;
                            }
                            al.RemoveAt(0);
                        }
                    }
                } while (((string)al[0]).Length > 0);
                sw.Close();
                log.Close();
                sr.Close();
                Console.WriteLine("{0} written.", outFileName);

                // Make a log of what we found.
                logFile = Path.Combine(Path.GetDirectoryName(outFileName), "phrasetranslist.txt");
                sw = new StreamWriter(logFile);
                al = new ArrayList(substTable.Count);
                newPhrase n;
                foreach (DictionaryEntry de in substTable)
                {
                    n = (newPhrase)de.Value;
                    s = String.Format("|{0}|{1}|{2}", de.Key, n.phrase, n.count.ToString());
                    // Console.WriteLine(s);
                    al.Add(s);
                }
                Console.WriteLine("Writing {0}", logFile);
                al.Sort();
                foreach (string ss in al)
                {
                    sw.WriteLine(ss);
                }
                sw.Close();
                Console.WriteLine("{0} written.", logFile);
	        }
	        catch (Exception ex)
	        {
		        Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
	        }
        }
    }

    class Program
    {
        static bool showBanner = true;
        static transPhrases tp;
        static void Main(string[] args)
        {
            if (args.Length >= 3)
            {
                tp = new transPhrases();
                if (args.Length == 4)
                {
                    if (args[3] == "-pon")
                    {
                        tp.isPohnpeianUpdate = true;
                    }
                }
                tp.filter(args[0], args[1], args[2]);
                showBanner = false;
            }
            if (showBanner)
            {
                Console.WriteLine(@"
Syntax:
xlat.exe infile.xml outfile.xml substfile.txt
infile.xml is the USFX file to read
outfile.xml is the USFX file to write
substfile.txt is the text file with substitutions to make. Longer matches
  take priority over shorter ones. The substition file contains one line per
  substitution, with fields separated by whatever character is first on the
  line. There are 3 fields: find text, replace text, and optional comment.
  Matches will always be on word boundaries. XML tokens delimited by < and >
  or & and ; count as a word. The log files phrasetranslist.txt and
  phrasetranslog.txt are written to the same folder as the output file.
");
            }
        }
    }
}
