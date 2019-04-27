using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.Drawing;
using System.Text.RegularExpressions;
using System.IO;
using System.Xml;
using BibleFileLib;
using WordSend;
using System.Net;
using System.Windows.Forms; // For Application.ProcessMessages

namespace WordSend
{
    public class global
    {
        public BibleBookInfo bkInfo;
        public XMLini xini;    // Main program XML initialization file
        public XMLini projectXini;  // XML inititalization file for the current project
        public string outputDirectory; // always under dataRootDir
        public string outputProjectDirectory; // e.g., full path to BibleConv\output\Kupang
        public Options projectOptions;
        public DateTime sourceDate = new DateTime(1611, 1, 1);
        private const string kFootnotesProcess = "footnotes.process"; // identifier for the (so far only one) file transformation process built into Haiola
        public string inputDirectory; // Always under dataRootDir, defaults to Documents/BibleConv/input
        public string inputProjectDirectory; //e.g., full path to BibleConv\input\Kupang
        public string dataRootDir; // Default is BibleConv in the user's Documents folder
        public string currentProject = String.Empty; // e.g., Kupang
        public string projectXiniPath;  // e.g., BibleConv\input\Kupang\options.xini
        public string preferredCover;
        public ethnorecord er;
        public LanguageCodeInfo languageCodes;


        public BoolStringDelegate GUIWriteString;
        public BoolStringDelegate UpdateStatus;
        public bool useConsole = false;
        public bool loggedError = false;

        public global()
        {
            languageCodes = new LanguageCodeInfo();
        }

        public string m_swordSuffix
        {
            get { return xini.ReadString("swordSuffix", String.Empty); }
            set { xini.WriteString("swordSuffix", value); }
        }

        public bool getFCBHkeys
        {
            get { return xini.ReadBool("downloadFcbhAudio", false); }
            set { xini.WriteBool("downloadFcbhAudio", value); }
        }

        public bool generateUsfm3Fig
        {
            get { return xini.ReadBool("generateUsfm3Fig", false); }
            set { xini.WriteBool("generateUsfm3Fig", value); }
        }

        public bool runXetex
        {
            get { return xini.ReadBool("runXetex", false); }
            set { xini.WriteBool("runXetex", value); xini.Write(); }
        }

        protected string GuessParatextProjectsDir()
        {
            string path;
            path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Paratext Projects");
            if (File.Exists(Path.Combine(path, "usfm.sty")))
                return path;
            path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ParatextProjects");
            if (File.Exists(Path.Combine(path, "usfm.sty")))
                return path;
            path = "~/ParatextProjects";
            if (File.Exists(Path.Combine(path, "usfm.sty")))
                return path;
            path = "C:\\My Paratext Projects";
            if (File.Exists(Path.Combine(path, "usfm.sty")))
                return path;
            return string.Empty;
        }

        protected string GuessParatext8ProjectsDir()
        {
            string path;
            path = "C:\\My Paratext 8 Projects";
            if (File.Exists(Path.Combine(path, "usfm.sty")))
                return path;
            path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Paratext 8 Projects");
            if (File.Exists(Path.Combine(path, "usfm.sty")))
                return path;
            path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Paratext 8 Projects");
            if (File.Exists(Path.Combine(path, "usfm.sty")))
                return path;
            path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Paratext8Projects");
            if (File.Exists(Path.Combine(path, "usfm.sty")))
                return path;
            path = "~/Paratext8Projects";
            if (File.Exists(Path.Combine(path, "usfm.sty")))
                return path;
            path = "~/Paratext 8 Projects";
            if (File.Exists(Path.Combine(path, "usfm.sty")))
                return path;
            return string.Empty;
        }

        public string paratextProjectsDir
        {
            get { return xini.ReadString("paratextProjectsDir", GuessParatextProjectsDir()); }
            set { xini.WriteString("paratextProjectsDir", value.Trim()); }
        }

        public string paratext8ProjectsDir
        {
            get { return xini.ReadString("paratext8ProjectsDir", GuessParatext8ProjectsDir()); }
            set { xini.WriteString("paratext8ProjectsDir", value.Trim()); }
        }

        public string currentTemplate
        {
            get { return xini.ReadString("currentTemplate", String.Empty); }
            set { xini.WriteString("currentTemplate", value.Trim()); }
        }



        public bool ShowStatus(string s)
        {
            if (UpdateStatus != null)
                return UpdateStatus(s);
            return true;
        }

        public void WriteError(string s)
        {
            WriteLine(s);
            loggedError = true;
        }

        public void WriteLine(string s)
        {
            if (useConsole)
                Console.WriteLine(s);
            if (GUIWriteString != null)
                GUIWriteString(s);
            if ((!useConsole) && (GUIWriteString == null))
                System.Windows.Forms.MessageBox.Show(s);
        }


        /// <summary>
        /// Reads the \id line to get the standard abbreviation of this file to figure out what
        /// a good name for its standardized file name might be.
        /// </summary>
        /// <param name="pathName">Full path to the file to read the \id line from.</param>
        /// <returns>Sort order plus 3-letter abbreviation of the Bible book (or front/back matter), upper case,
        /// unless the file lacks an \id line, in which case in returns and empty string.</returns>
        public string MakeUpUsfmFileName(string pathName, out string id)
        {
            if (bkInfo == null)
                bkInfo = new WordSend.BibleBookInfo();
            // Use the ID line.
            string result = "";
            id = "";
            string line;
            string chap = "";
            StreamReader sr = new StreamReader(pathName);
            while ((!sr.EndOfStream) && (result.Length < 1))
            {
                line = sr.ReadLine();
                if (line.StartsWith(@"\id ") && (line.Length > 6))
                {
                    id = result = line.Substring(4, 3).ToUpper(System.Globalization.CultureInfo.InvariantCulture);
                }
                else if ((line.StartsWith(@"\c ")) && (line.Length > 3))
                {
                    chap = line.Substring(3).Trim();
                    int pos = chap.IndexOf(' ');
                    if (pos >= 0)
                    {
                        chap = chap.Substring(0, pos);
                    }
                }
            }
            sr.Close();
            if (chap == "1")
                chap = "";
            if (result.Length > 0)
            {
                result = bkInfo.Order(result).ToString("D2") + "-" + result + chap;
            }
            return result;
        }


        /// <summary>
        /// This does a range-restricted replacement.
        /// 1. Find a match for first pattern.
        /// 2. Find next match for second pattern.
        /// 3. Replace the text between (but not including) the matches with the result
        /// of doing a substitution of the third pattern with the fourth
        /// 4. Continue the search starting after the end match.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="input"></param>
        /// <returns></returns>
        string DoRangeReplacement(string source, string input)
        {
            string temp = input;
            char delim = source[1];
            string[] parts = source.Split(new char[] { delim });
            var start = new Regex(parts[1]); // parts[0] is the colon before the first delimiter
            var finish = new Regex(parts[2]);
            var pattern = new Regex(parts[3]);
            string replacement = parts[4];
            replacement = replacement.Replace("$r", "\r"); // Allow $r in replacement to become a true cr
            replacement = replacement.Replace("$n", "\n"); // Allow (literal) $n in replacement to become a true newline

            int startIndex = 0;
            while (startIndex < temp.Length)
            {
                var nextMatch = start.Match(temp, startIndex);
                if (!nextMatch.Success)
                    break;
                startIndex = nextMatch.Index + nextMatch.Length;
                nextMatch = finish.Match(temp, startIndex);
                if (!nextMatch.Success)
                    break;
                int finishIndex = nextMatch.Index;
                string substitute = pattern.Replace(temp.Substring(startIndex, finishIndex - startIndex), replacement);
                temp = temp.Substring(0, startIndex) + substitute + temp.Substring(finishIndex);
                startIndex = startIndex + substitute.Length + nextMatch.Length;
            }
            return temp;
        }



        /// <summary>
        /// States for state machine in FixMultipleFootnotes
        /// </summary>
        enum FnStates
        {
            fnsAwaitingVt,
            fnsProcessingVt,
            fnsCollectingFt
        }

        /// <summary>
        /// We're looking for patterns like this:
        /// 
        /// \vt verse text |fn more verse text |fn still more
        /// \ft first footnote body
        /// \ft second footnote body
        /// \x
        /// 
        /// And converting to have only one |fn in a \vt, by moving the rest of the \vt into a new \vt after the first \ft, like this
        /// \vt verse text |fn
        /// \ft first footnote body
        /// \vt more verse text |fn still more
        /// \ft second footnote body
        /// \x
        /// 
        /// It should handle even more then two.
        /// 
        /// This works because OW input always has all the \ft's for a \vt, each on a line by itself, right after
        /// the \vt field (after an earlier process stripped out the associated \btvt and \btft). Also, adjacent \vt blocks just get merged...
        /// in fact the next step strips out the \vt markers...and the \ft material gets moved to the bottom somewhere.
        /// 
        /// It is done because the later RE-based processing steps can't handle more than one |fn before the associated \ft.
        /// </summary>
        /// <param name="inputBytes"></param>
        /// <param name="cbyteInput"></param>
        private void FixMultipleFootnotes(ref byte[] inputBytes, ref int cbyteInput)
        {
            byte[] outBuffer = new byte[cbyteInput * 2 + 100]; // super-generous
            int cbyteOut = 0;
            byte[] marker = Encoding.UTF8.GetBytes(@"\vt ");
            int i = 0;
            byte[] anchor = Encoding.UTF8.GetBytes("|fn");
            byte[] footnote = Encoding.UTF8.GetBytes(@"\ft ");
            List<int> anchors = new List<int>(); // places within the current \vt where we found |fn
            List<int> footnotes = new List<int>(); // places within the current \vt where we found \ft
            FnStates state = FnStates.fnsAwaitingVt;
            int ichStartVt = -1; // will cause crash if accidentally used before otherwise initialized.
            while (i < cbyteInput - 2)// 2 is length of shortest target
            {
                switch (state)
                {
                    case FnStates.fnsAwaitingVt:
                        if (FindMarker(inputBytes, i, marker))
                        {
                            state = FnStates.fnsProcessingVt;
                            ichStartVt = i;
                        }
                        else
                        {
                            outBuffer[cbyteOut++] = inputBytes[i];
                        }
                        break;
                    case FnStates.fnsProcessingVt:
                        if (FindMarker(inputBytes, i, footnote))
                        {
                            footnotes.Add(i); // note it
                            state = FnStates.fnsCollectingFt;
                        }
                        else if (FindMarker(inputBytes, i, anchor))
                        {
                            anchors.Add(i);
                        }
                        else if (i > 0 && inputBytes[i - 1] == 10 && inputBytes[i] == 92) // backslash at start of line
                        {
                            // We found some marker that terminates things. Note that it MIGHT be another \vt.
                            cbyteOut = HandleEndOfVtBlock(inputBytes, outBuffer, cbyteOut, marker, i, anchor, anchors, footnotes, ichStartVt);
                            state = FnStates.fnsAwaitingVt;
                            continue; // SKIP i++ so we can check to see whether this marker is \vt and starts a new block.
                        }
                        break;
                    case FnStates.fnsCollectingFt:
                        if (FindMarker(inputBytes, i, footnote))
                        {
                            footnotes.Add(i); // note it
                        }
                        else if (i > 0 && inputBytes[i - 1] == 10 && inputBytes[i] == 92) // backslash at start of line
                        {
                            cbyteOut = HandleEndOfVtBlock(inputBytes, outBuffer, cbyteOut, marker, i, anchor, anchors, footnotes, ichStartVt);
                            state = FnStates.fnsAwaitingVt;
                            continue; // SKIP i++ so we can check to see whether this marker is \vt and starts a new block.
                        }
                        break;
                }
                i++;
            }
            if (state != FnStates.fnsAwaitingVt)
            {
                // input terminated within a \vt block. Include the remaining text
                cbyteOut = HandleEndOfVtBlock(inputBytes, outBuffer, cbyteOut, marker, cbyteInput, anchor, anchors, footnotes, ichStartVt);
            }
            else
            {
                // Copy the last few bytes
                while (i < cbyteInput)
                    outBuffer[cbyteOut++] = inputBytes[i++];
            }
            // switch buffers and counts.
            inputBytes = outBuffer;
            cbyteInput = cbyteOut;
        }

        private static int HandleEndOfVtBlock(byte[] inputBytes, byte[] outBuffer, int cbyteOut, byte[] marker, int i, byte[] anchor, List<int> anchors, List<int> footnotes, int ichStartVt)
        {
            // If all is not consistent, give up and let it fail at next stage.
            // If only one anchor, no need to fix.
            if (anchors.Count != footnotes.Count || anchors.Count < 2)
            {
                // no need to re-arrange, just copy everything since \vt
                cbyteOut = CopySegToOutput(inputBytes, outBuffer, cbyteOut, ichStartVt, i);
            }
            else
            {
                // Re-arrange so only one anchor per \vt.
                // First copy everything from \vt to the end of the first |fn
                cbyteOut = CopySegToOutput(inputBytes, outBuffer, cbyteOut, ichStartVt, anchors[0] + anchor.Length);
                // the -1 should put a newline before the first \ft; copying up to footnotes[1] ends with a newline, too.
                cbyteOut = CopySegToOutput(inputBytes, outBuffer, cbyteOut, footnotes[0] - 1, footnotes[1]);
                footnotes.Add(i); // last footnote terminates at current character position.
                for (int ifn = 1; ifn < anchors.Count; ifn++)
                {
                    // Copy an extra \vt.
                    cbyteOut = CopySegToOutput(inputBytes, outBuffer, cbyteOut, ichStartVt, ichStartVt + marker.Length);
                    // Copy the text between the previous footnote and this one, including the |f
                    int ichStart = anchors[ifn - 1] + anchor.Length;
                    // Drop one leading space or newline; newline between \vt blocks is equivalent.
                    if (inputBytes[ichStart] == 32 || inputBytes[ichStart] == 10)
                        ichStart++;
                    cbyteOut = CopySegToOutput(inputBytes, outBuffer, cbyteOut, ichStart, anchors[ifn] + anchor.Length);
                    // Copy the next footnote, including the preceding and following newlines
                    cbyteOut = CopySegToOutput(inputBytes, outBuffer, cbyteOut, footnotes[ifn] - 1, footnotes[ifn + 1]);
                }
                if (anchors[anchors.Count - 1] + anchor.Length + 1 < footnotes[0])
                {
                    int ichStart = anchors[anchors.Count - 1] + anchor.Length;
                    // Drop one leading space or newline; newline between \vt blocks is equivalent.
                    if (inputBytes[ichStart] == 32 || inputBytes[ichStart] == 10)
                        ichStart++;
                    if (ichStart + 1 < footnotes[0]) // further check in case ONLY one space
                    {
                        // We have text following the last anchor. Make yet another \vt
                        cbyteOut = CopySegToOutput(inputBytes, outBuffer, cbyteOut, ichStartVt, ichStartVt + marker.Length);
                        // And copy the trailing text. It runs from after the last anchor to before the first footnote body.
                        cbyteOut = CopySegToOutput(inputBytes, outBuffer, cbyteOut, ichStart, footnotes[0]);
                    }
                }
            }
            anchors.Clear();
            footnotes.Clear();
            return cbyteOut;
        }

        /// <summary>
        /// Helper function for FixMultipleFootnotes. Copies characters from ichMin to (but not including) ichLim
        /// from inputBytes to outBuffer, starting at cbyteOut. Returns a new value for cbyteOut, the next character
        /// after the material copied.
        /// </summary>
        /// <param name="inputBytes"></param>
        /// <param name="outBuffer"></param>
        /// <param name="cbyteOut"></param>
        /// <param name="ichStart"></param>
        /// <param name="ichLim"></param>
        /// <returns></returns>
        private static int CopySegToOutput(byte[] inputBytes, byte[] outBuffer, int cbyteOut, int ichStart, int ichLim)
        {
            for (int k = ichStart; k < ichLim; k++)
                outBuffer[cbyteOut++] = inputBytes[k];
            return cbyteOut;
        }

        /// <summary>
        /// Helper function for FixMultipleFootnotes. Determines whether the byte sequence target occurs at offset i in inputBytes.
        /// </summary>
        /// <param name="inputBytes"></param>
        /// <param name="i"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        private static bool FindMarker(byte[] inputBytes, int i, byte[] target)
        {
            bool gotIt = true;
            for (int j = 0; j < target.Length; j++)
            {
                if (inputBytes[i + j] != target[j])
                {
                    gotIt = false;
                    break;
                }
            }
            return gotIt;
        }



        private void PreprocessOneFile(string inputPath, List<string> tablePaths, string outputPath)
        {
            DateTime fileDate;
            fileDate = File.GetLastWriteTimeUtc(inputPath);
            if (fileDate > sourceDate)
            {
                sourceDate = fileDate;
                projectOptions.SourceFileDate = sourceDate;
            }
            string input;
            // Read file into input
            // Instead of asking the user what the character encoding is, we guess that it is either
            // Windows 1252 or UTF-8, and choose which one of those based on the assumed presence of
            // surrogates in UTF-8, unless there is a byte-order mark.
            Encoding enc = fileHelper.IdentifyFileCharset(inputPath);
            // MessageBox.Show(inputPath + " is encoded as " + enc.ToString());
            StreamReader reader = new StreamReader(inputPath, enc /* Encoding.GetEncoding(globe.globe.projectOptions.InputEncoding) */);
            input = reader.ReadToEnd() + "\0";
            reader.Close();

            // Copy input into buffer
            byte[] inputBytes = Encoding.UTF8.GetBytes(input); // replaced by previous output for subsequent iterations.
            int cbyteInput = inputBytes.Length; // replaced by output length for subsequent iterations
            byte[] outBuffer = inputBytes; // in case 0 iterations
            int nOutLen = inputBytes.Length;
            foreach (string tp in tablePaths)
            {
                if (tp.EndsWith(".process"))
                {
                    // This will become a switch if we get more processes
                    if (tp == kFootnotesProcess)
                    {
                        FixMultipleFootnotes(ref inputBytes, ref cbyteInput);
                        outBuffer = inputBytes; // in case last pass
                        nOutLen = cbyteInput; // in case last pass.
                    }
                    else
                    {
                        WriteLine("ERROR: Process " + tp + " not known.");
                        return;
                    }
                    continue;
                }
                string tablePath = Path.Combine(inputProjectDirectory, tp);
                if (!File.Exists(tablePath))
                {
                    tablePath = Path.Combine(inputDirectory, tp);
                }
                if (!File.Exists(tablePath))
                {
                    tablePath = SFConverter.FindAuxFile(tp);
                }
                if (File.Exists(tablePath))
                {
                    if (tablePath.EndsWith(".re"))
                    {
                        // Apply a regular expression substitution
                        string temp = Encoding.UTF8.GetString(inputBytes, 0, cbyteInput - 1); // leave out final null
                        StreamReader tableReader = new StreamReader(tablePath, Encoding.UTF8);
                        fileDate = File.GetLastWriteTimeUtc(tablePath);
                        if (fileDate > sourceDate)
                        {
                            sourceDate = fileDate;
                            projectOptions.SourceFileDate = sourceDate;
                        }
                        while (!tableReader.EndOfStream)
                        {
                            string source = tableReader.ReadLine();
                            if (source.Trim().Length == 0)
                                continue;

                            char delim = source[0];
                            if (delim == ':')
                                temp = DoRangeReplacement(source, temp);
                            else
                            {
                                string[] parts = source.Split(new char[] { delim });
                                string pattern = parts[1]; // parts[0] is the empty string before the first delimiter
                                string replacement = parts[2];
                                replacement = replacement.Replace("$r", "\r"); // Allow $r in replacement to become a true cr
                                replacement = replacement.Replace("$n", "\n"); // Allow (literal) $n in replacement to become a true newline
                                temp = System.Text.RegularExpressions.Regex.Replace(temp, pattern, replacement);
                            }
                        }
                        tableReader.Close();
                        temp = temp + "\0";
                        outBuffer = Encoding.UTF8.GetBytes(temp);
                        inputBytes = outBuffer;
                        cbyteInput = nOutLen = inputBytes.Length;
                    }
                }
                else
                {
                    MessageBox.Show("Can't find preprocessing file " + tp, "Error in preprocessing file list");
                }
            }

            // Convert the output back to a file
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)); // make sure it exists.
            StreamWriter output = new StreamWriter(outputPath, false, Encoding.UTF8);
            // Make sure no trailing nulls get written to file.
            while (nOutLen > 0 && outBuffer[nOutLen - 1] == 0)
                nOutLen--;
            string outputString = Encoding.UTF8.GetString(outBuffer, 0, nOutLen);
            output.Write(outputString);
            output.Close();

            // Eradicate depricated PUA characters
            fileHelper.revisePua(outputPath);
        }




        public void PreprocessUsfmFiles(string SourceDir)
        {
            // First, copy BookNames.xml for ready reference. We will update it later.
            string bookNamesCopy = Path.Combine(outputProjectDirectory, "BookNames.xml");
            string bookNamesSource = Path.Combine(SourceDir, "BookNames.xml");
            if (File.Exists(bookNamesSource))
                File.Copy(bookNamesSource, bookNamesCopy, true);

            // Now, get on with preprocessing the USFM files.
            Logit.GUIWriteString = GUIWriteString;
            Logit.OpenFile(Path.Combine(outputProjectDirectory, "preprocesslog.txt"));
            // string SourceDir = Path.Combine(globe.inputProjectDirectory, "Source");
            /*
            StreamReader sr = new StreamReader(orderFile);
            string allowedBookList = sr.ReadToEnd();
            sr.Close();
            */
            string bookId;
            string UsfmDir = Path.Combine(outputProjectDirectory, "extendedusfm");
            if (!Directory.Exists(SourceDir))
            {
                WriteLine("ERROR: "+SourceDir + " not found!");
                return;
            }
            // Start with an EMPTY USFM directory to avoid problems with old files 
            Utils.DeleteDirectory(UsfmDir);
            fileHelper.EnsureDirectory(UsfmDir);
            string[] inputFileNames = Directory.GetFiles(SourceDir);
            if (inputFileNames.Length == 0)
            {
                WriteLine("ERROR: No files found in " + SourceDir);
                return;
            }

            foreach (string inputFile in inputFileNames)
            {
                string filename = Path.GetFileName(inputFile);
                string lowerName = filename.ToLower();
                string fileType = Path.GetExtension(filename).ToUpper();
                if ((fileType != ".BAK") && (fileType != ".LDS") &&
                    (fileType != ".SSF") && (fileType != ".DBG") &&
                    (fileType != ".WDL") && (fileType != ".STY") &&
                    (fileType != ".XML") && (fileType != ".HTM") &&
                    (fileType != ".KB2") && (fileType != ".HTML") &&
                    (fileType != ".CSS") && (fileType != ".SWP") &&
                    (fileType != ".ID") && (fileType != ".DIC") &&
                    (fileType != ".LDML") && (fileType != ".JSON") &&
                    (fileType != ".VRS") && (fileType != ".INI") && (fileType != ".CSV") && (fileType != ".TSV") &&
                    (fileType != ".CCT") && (!inputFile.EndsWith("~")) &&
                    (lowerName != "autocorrect.txt") &&
                    (lowerName != "tmp.txt") &&
                    (lowerName != "changes.txt") &&
                    (lowerName != "hyphenatedWords.txt") &&
                    (lowerName != "wordboundariesoutput.txt") &&
                    (lowerName != "printdraftchanges.txt"))
                {
                    ShowStatus("preprocessing " + filename);
                    Application.DoEvents();
                    if (!fileHelper.fAllRunning)
                        break;
                    string outputFileName = MakeUpUsfmFileName(inputFile, out bookId) + ".usfm";
                    if (outputFileName.Length < 8)
                    {
                        if (fileType != ".TXT")
                            Logit.WriteLine("No proper \\id line found in " + inputFile);
                    }
                    else
                    {
                        if (projectOptions.allowedBookList.Contains(bookId))
                        {
                            string outputFilePath = Path.Combine(UsfmDir, outputFileName);
                            PreprocessOneFile(inputFile, projectOptions.preprocessingTables, outputFilePath);
                        }
                        /*
                        else
                        {
                            Logit.WriteLine("Skipping book " + bookId + " (not in " + orderFile + ")");
                        }
                        */
                    }
                }
            }
        }


        public string shortCopyrightMessage, longCopyrightMessage, copyrightLink;

        /// <summary>
        /// Sets the shortCopyrightMessage, longCopyrightMessage, and copyrightLink variables based on the
        /// current globe.globe.projectOptions values.
        /// </summary>
        public void SetCopyrightStrings()
        {
            if (projectOptions.publicDomain)
            {
                shortCopyrightMessage = longCopyrightMessage = "Public Domain";
                copyrightLink = "<a href='http://en.wikipedia.org/wiki/Public_domain'>Public Domain</a>";
            }
            else if (projectOptions.silentCopyright)
            {
                longCopyrightMessage = shortCopyrightMessage = copyrightLink = String.Empty;
            }
            else if (projectOptions.anonymous)
            {
                longCopyrightMessage = shortCopyrightMessage = copyrightLink = "© " + projectOptions.copyrightYears + ". ";
            }
            else if (projectOptions.copyrightOwnerAbbrev.Length > 0)
            {
                shortCopyrightMessage = "© " + projectOptions.copyrightYears + " " + projectOptions.copyrightOwnerAbbrev;
                longCopyrightMessage = "Copyright © " + projectOptions.copyrightYears + " " + projectOptions.copyrightOwner;
                if (projectOptions.copyrightOwnerUrl.Length > 11)
                    copyrightLink = "copyright © " + projectOptions.copyrightYears + " <a href=\"" + projectOptions.copyrightOwnerUrl + "\">" + usfxToHtmlConverter.EscapeHtml(projectOptions.copyrightOwner) + "</a>";
                else
                    copyrightLink = longCopyrightMessage;
            }
            else
            {
                shortCopyrightMessage = "© " + projectOptions.copyrightYears + " " + projectOptions.copyrightOwner;
                longCopyrightMessage = "Copyright " + shortCopyrightMessage;
                if (projectOptions.copyrightOwnerUrl.Length > 11)
                    copyrightLink = "copyright © " + projectOptions.copyrightYears + " <a href=\"" + projectOptions.copyrightOwnerUrl + "\">" + usfxToHtmlConverter.EscapeHtml(projectOptions.copyrightOwner) + "</a>";
                else
                    copyrightLink = longCopyrightMessage;
            }
            if (projectOptions.AudioCopyrightNotice.Length > 1)
            {
                longCopyrightMessage = longCopyrightMessage + "; ℗ " + usfxToHtmlConverter.EscapeHtml(projectOptions.AudioCopyrightNotice);
                copyrightLink = copyrightLink + "<br />℗ " + usfxToHtmlConverter.EscapeHtml(projectOptions.AudioCopyrightNotice);
            }
        }


        /// <summary>
        /// Expands % escape codes in a string.
        /// </summary>
        /// <param name="s">String containing 0 or more % codes</param>
        /// <returns>String with values replacing their respective % codes</returns>
        public string expandPercentEscapes(string s)
        {
            string distributionScope;
            if (projectOptions.privateProject)
                projectOptions.redistributable = projectOptions.downloadsAllowed = false;
            if (projectOptions.redistributable)
                distributionScope = "redistributable";
            else if (projectOptions.downloadsAllowed)
                distributionScope = "downloadable";
            else
                distributionScope = "restricted";
            s = s.Replace("%d", currentProject);
            s = s.Replace("%e", projectOptions.languageId);
            s = s.Replace("%h", projectOptions.homeDomain);
            s = s.Replace("%c", shortCopyrightMessage);
            s = s.Replace("%C", copyrightLink);
            s = s.Replace("%l", projectOptions.languageName);
            s = s.Replace("%L", projectOptions.languageNameInEnglish);
            s = s.Replace("%D", projectOptions.dialect);
            s = s.Replace("%a", projectOptions.contentCreator);
            s = s.Replace("%A", projectOptions.contributor);
            s = s.Replace("%v", projectOptions.vernacularTitle);
            s = s.Replace("%f", "<a href=\"" + projectOptions.facebook + "\">" + projectOptions.facebook + "</a>");
            s = s.Replace("%F", projectOptions.fcbhId);
            s = s.Replace("%n", projectOptions.EnglishDescription);
            s = s.Replace("%N", projectOptions.lwcDescription);
            s = s.Replace("%p", projectOptions.privateProject ? "private" : "public");
            s = s.Replace("%r", distributionScope);
            s = s.Replace("%T", projectOptions.contentUpdateDate.ToString("yyyy-MM-dd"));
            s = s.Replace("%o", projectOptions.rightsStatement);
            s = s.Replace("%x", projectOptions.promoHtml);
            s = s.Replace("%w", projectOptions.printPublisher);
            s = s.Replace("%i", projectOptions.electronicPublisher);
            s = s.Replace("%P", projectOptions.AudioCopyrightNotice);
            s = s.Replace("%t", projectOptions.translationId);
            string result = s.Replace("%%", "%");
            return result;
        }


        /// <summary>
        /// Generate a complete copyright and permissions statement
        /// </summary>
        /// <returns>HTML text of the long copyright and permissions statement</returns>
        public string copyrightPermissionsStatement()
        {
            string fontClass = projectOptions.fontFamily.ToLower().Replace(' ', '_');
            if (projectOptions.customPermissions)
                return expandPercentEscapes(projectOptions.licenseHtml);
            StringBuilder copr = new StringBuilder();
            copr.Append(String.Format("<h1 class='{2}'>{0}</h1>\n<h2>{1}</h2>\n",
                usfxToHtmlConverter.EscapeHtml(projectOptions.vernacularTitle),
                usfxToHtmlConverter.EscapeHtml(projectOptions.EnglishDescription), fontClass));
            if (!String.IsNullOrEmpty(projectOptions.lwcDescription))
                copr.Append(String.Format("<h2>{0}</h2>\n", usfxToHtmlConverter.EscapeHtml(projectOptions.lwcDescription)));
            copr.Append(String.Format("<p>{0}<br />\n", copyrightLink));
            copr.Append(String.Format("Language: <a href='http://www.ethnologue.org/language/{0}' class='{2}' target='_blank'>{1}",
                projectOptions.languageId, projectOptions.languageName, fontClass));
            if (projectOptions.languageName != projectOptions.languageNameInEnglish)
                copr.Append(String.Format(" ({0})", usfxToHtmlConverter.EscapeHtml(projectOptions.languageNameInEnglish)));
            copr.Append("</a><br />\n");
            if (!String.IsNullOrEmpty(projectOptions.dialect))
                copr.Append(String.Format("Dialect: {0}<br />\n", usfxToHtmlConverter.EscapeHtml(projectOptions.dialect)));
            if (!(projectOptions.anonymous || String.IsNullOrEmpty(projectOptions.contentCreator)))
                copr.Append(String.Format("Translation by: {0}<br />\n", usfxToHtmlConverter.EscapeHtml(projectOptions.contentCreator)));
            if ((!projectOptions.anonymous) && (!String.IsNullOrEmpty(projectOptions.contributor)) && (projectOptions.contentCreator != projectOptions.contributor))
                copr.Append(String.Format("Contributor: {0}<br />\n", usfxToHtmlConverter.EscapeHtml(projectOptions.contributor)));
            if (!String.IsNullOrEmpty(projectOptions.promoHtml))
                copr.Append("<br />\n" + projectOptions.promoHtml);
            if (!String.IsNullOrEmpty(projectOptions.rightsStatement))
                copr.Append("<br />\n" + projectOptions.rightsStatement);
            copr.Append("</p>\n");
            if (projectOptions.ccby)
            {
                copr.Append(@"<p>This translation is made available to you under the terms of the
<a href='http://creativecommons.org/licenses/by4.0/'>Creative Commons Attribution license 4.0.</a></p>
<p>You may share and redistribute this Bible translation or extracts from it in any format, provided that:</p>
<ul>
<li>You include the above copyright and source information.</li>
<li>If you make any changes to the text, you must indicate that you did so in a way that makes it clear that the original licensor is not necessarily endorsing your changes.</li>
</ul>
<p>Pictures included with Scriptures and other documents on this site are licensed just for use with those Scriptures and documents.
For other uses, please contact the respective copyright owners.</p>
<p>Note that in addition to the rules above, revising and adapting God's Word involves a great responsibility to be true to God's Word. See Revelation 22:18-19.</p>
");
            }
            else if (projectOptions.ccbyndnc)
            {
                copr.Append(@"<p>This translation is made available to you under the terms of the
<a href='http://creativecommons.org/licenses/by-nc-nd/4.0/'>Creative Commons Attribution-Noncommercial-No Derivatives license 4.0.</a></p>
<p>You may share and redistribute this Bible translation or extracts from it in any format, provided that:</p>
<ul>
<li>You include the above copyright and source information.</li>
<li>You do not sell this work for a profit.</li>
<li>You do not change any of the words or punctuation of the Scriptures.</li>
</ul>
<p>Pictures included with Scriptures and other documents on this site are licensed just for use with those Scriptures and documents.
For other uses, please contact the respective copyright owners.</p>
");
            }
            else if (projectOptions.ccbysa)
            {
                copr.Append(@"<p>This translation is made available to you under the terms of the
<a href='http://creativecommons.org/licenses/by-sa/4.0/'>Creative Commons Attribution Share-Alike license 4.0.</a></p>
<p>You have permission to share and redistribute this Bible translation in any format and to make reasonable revisions and adaptations of this translation, provided that:</p>
<ul>
<li>You include the above copyright and source information.</li>
<li>If you make any changes to the text, you must indicate that you did so in a way that makes it clear that the original licensor is not necessarily endorsing your changes.</li>
<li>If you redistribute this text, you must distribute your contributions under the same license as the original.</li>
</ul>
<p>Pictures included with Scriptures and other documents on this site are licensed just for use with those Scriptures and documents.
For other uses, please contact the respective copyright owners.</p>
<p>Note that in addition to the rules above, revising and adapting God's Word involves a great responsibility to be true to God's Word. See Revelation 22:18-19.</p>
");
            }
            else if (projectOptions.ccbynd)
            {
                copr.Append(@"<p>This translation is made available to you under the terms of the
<a href='http://creativecommons.org/licenses/by-nd/4.0/'>Creative Commons Attribution-No Derivatives license 4.0.</a></p>
<p>You may share and redistribute this Bible translation or extracts from it in any format, provided that:</p>
<ul>
<li>You include the above copyright and source information.</li>
<li>You do not make any derivative works that change any of the actual words or punctuation of the Scriptures.</li>
</ul>
<p>Pictures included with Scriptures and other documents on this site are licensed just for use with those Scriptures and documents.
For other uses, please contact the respective copyright owners.</p>
");
            }
            copr.Append(String.Format("<p><br/>{0}</p>\n", projectOptions.contentUpdateDate.ToString("yyyy-MM-dd")));

            if (projectOptions.eBibledotorgunique)
            {
                copr.Append($"<p><a href='https://eBible.org/find/details.php?id={projectOptions.translationId}'>Updates</a>");
                copr.Append("<p><a href='https://eBible.org' target='_blank'>eBible.org</a></p>");

            }


            return copr.ToString();
        }


    }
}
