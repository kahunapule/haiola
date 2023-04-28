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
using System.Data.Common;
using static System.Net.Mime.MediaTypeNames;
using System.Diagnostics;
using System.Globalization;

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
        public string audioDir; // Location of mp3 audio files
        public string currentProject = String.Empty; // e.g., Kupang
        public string projectXiniPath;  // e.g., BibleConv\input\Kupang\options.xini
        public string preferredCover;
        public ethnorecord er;
        public LanguageCodeInfo languageCodes;
        public string currentConversion;   // e.g., "Preprocessing" or "Portable HTML"
        public Fingerprint thumb;
        public string certified = null;
        public string orderFile;
        public bool clKludge = false;


        public BoolStringDelegate GUIWriteString;
        public BoolStringDelegate UpdateStatus;
        public bool useConsole = false;
        public bool loggedError = false;

        public string eBibleCertified = @"/share/Documents/Electronic Scripture Publishing/eBible.org_certified.jpg";

        public global()
        {
            languageCodes = new LanguageCodeInfo();
        }

        public string m_swordSuffix
        {
            get { return xini.ReadString("swordSuffix", String.Empty); }
            set { xini.WriteString("swordSuffix", value); }
        }

        public bool generateUsfm3Fig
        {
            get { return xini.ReadBool("generateUsfm3Fig", true); }
            set { xini.WriteBool("generateUsfm3Fig", value); }
        }

        public bool rebuild
        {
            get { return xini.ReadBool("rebuild", false); }
            set { xini.WriteBool("rebuild", value); }
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


        public string GetEpubID()
        {
            if (String.IsNullOrEmpty(projectOptions.epubId))
            {
                string hash = Utils.SHA1HashString(projectOptions.translationId + "|" + projectOptions.fcbhId + "|" + DateTime.UtcNow.ToString("dd M yyyy HH:mm:ss.fffffff") + " https://Haiola.org ");
                StringBuilder uuid = new StringBuilder(hash);
                uuid[8] = uuid[13] = uuid[18] = uuid[23] = '-';
                uuid[14] = '5';
                switch (uuid[19])
                {
                    case '0':
                    case '1':
                    case '2':
                        uuid[19] = '8';
                        break;
                    case '3':
                    case '4':
                    case '5':
                        uuid[19] = '9';
                        break;
                    case '6':
                    case '7':
                    case 'c':
                        uuid[19] = 'a';
                        break;
                    case 'd':
                    case 'e':
                    case 'f':
                        uuid[19] = 'b';
                        break;
                }
                uuid.Length = 36;   // Truncate
                projectOptions.epubId = uuid.ToString();
                projectOptions.Write();
            }
            return projectOptions.epubId;
        }


        public void DoPostprocess()
        {
            if (Logit.loggedError)
            {
                Logit.WriteLine("Skipping postprocessing due to prior errors on this project.");
            }
            else
            {
                List<string> postproclist = projectOptions.postprocesses;
                string command;
                foreach (string proc in postproclist)
                {
                    command = proc.Replace("%d", currentProject);
                    command = command.Replace("%t", projectOptions.translationId);
                    command = command.Replace("%i", projectOptions.fcbhId);
                    command = command.Replace("%e", projectOptions.languageId);
                    command = command.Replace("%h", projectOptions.homeDomain);
                    command = command.Replace("%p", projectOptions.privateProject ? "private" : "public");
                    command = command.Replace("%r", projectOptions.redistributable ? "redistributable" : "restricted");
                    command = command.Replace("%o", projectOptions.downloadsAllowed ? "downloadable" : "onlineonly");
                    Logit.WriteLine("Running " + command);
                    if (!fileHelper.RunCommand(command))
                        Logit.WriteError(fileHelper.runCommandError + " Error " + currentConversion);
                    currentConversion = String.Empty;
                    if (!fileHelper.fAllRunning)
                        return;
                }
            }
        }




        /// <summary>
        /// Convert USFX to PDF
        /// </summary>
        public void ConvertUsfxToPDF(string xetexDir)
        {
            if ((projectOptions.languageId.Length < 3) || (projectOptions.translationId.Length < 3))
                return;
            Usfx2XeTeX toXeTex = new Usfx2XeTeX();
            toXeTex.texDir = xetexDir;
            toXeTex.sqlFileName = string.Empty; // Inhibit re-making SQL file.
            currentConversion = "writing XeTeX";
            string UsfxPath = Path.Combine(outputProjectDirectory, "usfx");
            if (!Directory.Exists(UsfxPath))
            {
                Logit.WriteError(UsfxPath + " not found!");
                return;
            }
            Utils.DeleteDirectory(xetexDir);
            Utils.EnsureDirectory(outputDirectory);
            Utils.EnsureDirectory(outputProjectDirectory);
            Utils.EnsureDirectory(xetexDir);
            string logFile = Path.Combine(outputProjectDirectory, "xetexConversionReport.txt");
            Logit.OpenFile(logFile);

            string fontSource = Path.Combine(dataRootDir, "fonts");
            string fontName = projectOptions.fontFamily.ToLower().Replace(' ', '_');


            toXeTex.projectOptions = projectOptions;
            toXeTex.projectOutputDir = outputProjectDirectory;
            toXeTex.redistributable = projectOptions.redistributable;
            toXeTex.epubIdentifier = GetEpubID();
            toXeTex.stripPictures = false;
            toXeTex.indexDate = DateTime.UtcNow;
            toXeTex.indexDateStamp = "PDF generated using Haiola and XeLaTeX on " + toXeTex.indexDate.ToString("d MMM yyyy") +
                " from source files dated " + sourceDate.ToString("d MMM yyyy") + @"\par ";
            toXeTex.GeneratingConcordance = false;
            toXeTex.CrossRefToFilePrefixMap = projectOptions.CrossRefToFilePrefixMap;
            toXeTex.contentCreator = projectOptions.contentCreator;
            toXeTex.contributor = projectOptions.contributor;
            string usfxFilePath = Path.Combine(UsfxPath, "usfx.xml");
            toXeTex.bookInfo.ReadPublicationOrder(orderFile);
            toXeTex.MergeXref(Path.Combine(inputProjectDirectory, "xref.xml"));
            //TODO: eliminate side effects in expandPercentEscapes
            // Side effect: expandPercentEscapes sets longCopyrightMessage and shortCopyrightMessage.
            toXeTex.sourceLink = expandPercentEscapes("<a href=\"https://%h/%t\">%v</a>");
            toXeTex.longCopr = longCopyrightMessage;
            toXeTex.shortCopr = shortCopyrightMessage;
            toXeTex.textDirection = projectOptions.textDir;
            toXeTex.stripManualNoteOrigins = projectOptions.stripNoteOrigin;
            toXeTex.noteOriginFormat = projectOptions.xoFormat;
            toXeTex.englishDescription = projectOptions.EnglishDescription;
            toXeTex.preferredFont = projectOptions.fontFamily;
            toXeTex.fcbhId = projectOptions.fcbhId;
            // toXeTex.callXetex = globe.runXetex;
            toXeTex.coverName = Path.GetFileName(preferredCover);
            if (projectOptions.PrepublicationChecks &&
                (projectOptions.publicDomain || projectOptions.redistributable || File.Exists(Path.Combine(inputProjectDirectory, "certify.txt"))) &&
                File.Exists(certified))
            {
                File.Copy(certified, Path.Combine(xetexDir, "eBible.org_certified.jpg"));
                // toXeTex.indexDateStamp = toXeTex.indexDateStamp + "<br /><a href='https://eBible.org/certified/' target='_blank'><img src='eBible.org_certified.jpg' alt='eBible.org certified' /></a>";
            }
            toXeTex.xrefCall.SetMarkers(projectOptions.xrefCallers);
            toXeTex.footNoteCall.SetMarkers(projectOptions.footNoteCallers);
            toXeTex.inputDir = inputDirectory;
            toXeTex.projectInputDir = inputProjectDirectory;
            toXeTex.ConvertUsfxToHtml(usfxFilePath, xetexDir,
                projectOptions.vernacularTitle,
                projectOptions.languageId,
                projectOptions.translationId,
                projectOptions.chapterLabel,
                projectOptions.psalmLabel,
                shortCopyrightMessage,
                expandPercentEscapes(projectOptions.homeLink),
                expandPercentEscapes(projectOptions.footerHtml),
                expandPercentEscapes(projectOptions.indexHtml),
                copyrightPermissionsStatement(),
                projectOptions.ignoreExtras,
                projectOptions.goText);
            toXeTex.bookInfo.RecordStats(projectOptions);
            projectOptions.commonChars = toXeTex.commonChars;
            projectOptions.Write();
            Logit.CloseFile();
            if (Logit.loggedError)
            {
                projectOptions.lastRunResult = false;
            }
            if (Logit.loggedWarning)
            {
                projectOptions.warningsFound = true;
            }
        }





        /// <summary>
        /// Pick a pair of related dark and light colors for this translation's default cover
        /// based on the epub ID
        /// </summary>
        /// <returns>0, 1, 2, or 3</returns>
        protected int MakeUpColor(out string dark, out string light)
        {
            string s = GetEpubID().Substring(0, 6);
            int color = Convert.ToInt32(s, 16);
            switch (projectOptions.epubId[7])
            {
                case '0':
                case '7':
                case 'd':
                case '2':
                    color &= 0x00003f;
                    break;
                case '9':
                case '8':
                case '1':
                    color &= 0x003f00;
                    break;
                case '3':
                case 'a':
                case 'e':
                    color &= 0x003f3f;
                    break;
                case '4':
                case 'b':
                    color &= 0x3f3f00;
                    break;
                case '5':
                case 'c':
                    color &= 0x3f003f;
                    break;
                default:
                    color &= 0x3f3f3f;
                    break;
            }
            color &= 0x3f3f3f;
            if ((color & 0xf0f0f0) == 0)
            {
                dark = "#000000";
                light = "#ffffff";
            }
            else
            {
                dark = "#" + color.ToString("x6");
                color += 0xc0c0c0;
                light = "#" + color.ToString("x6");
            }
            return Convert.ToInt32(projectOptions.epubId.Substring(10, 1), 16) & 3;
        }



        /// <summary>
        /// Inserts up to 2 line breaks into a string at points less than or equal to maxWidth characters long
        /// </summary>
        /// <param name="s">String to word wrap</param>
        /// <param name="maxWidth">Maximum line length</param>
        /// <returns>string with line breaks addded</returns>
        public string ShortWordWrap(string s, int maxWidth)
        {
            int lineLength = 0;
            int lineCount = 0;
            string[] words = s.Split(new Char[] { ' ', '\t', '\n' });
            string result = String.Empty;
            foreach (string word in words)
            {
                if (result.Length == 0)
                {
                    result = word;
                    lineLength = word.Length;
                }
                else
                {
                    if ((lineLength + word.Length >= maxWidth) && (lineCount < 2))
                    {
                        result = result + "\n" + word;
                        lineLength = word.Length;
                        lineCount++;
                    }
                    else
                    {
                        result = result + " " + word;
                        lineLength += word.Length + 1;
                    }
                }
            }
            return result.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
        }


        /// <summary>
        /// Create or copy cover file(s), putting the results in the output project cover directory.
        /// Get .svg from project input directory, or create one, in that order.
        /// Get .png from project input directory, or create one from svg, in that order.
        /// Get .jpg from project input directory.
        /// Return the preferred available file name for a cover.
        /// </summary>
        /// <param name="small">true iff the cover is allowed to be small (defaults to false if parameter is missing)</param>
        /// <returns>The preferred cover name</returns>
        public string CreateCover(bool small = false)
        {
            string dark, light, svgPath, pngPath;
            StreamWriter sw;
            string coverOutput = Path.Combine(outputProjectDirectory, "cover");
            if (!small)
                Utils.DeleteDirectory(coverOutput);
            Utils.EnsureDirectory(coverOutput);

            // Get the best cover.svg available.
            string coverIn = Path.Combine(inputProjectDirectory, "cover.svg");
            string coverOut = Path.Combine(coverOutput, "cover.svg");
            svgPath = coverOut;
            if (File.Exists(coverIn))
            {   // We have cover.svg in our input directory.
                fileHelper.CopyFile(coverIn, coverOut);
            }
            else
            {   // Create a default svg cover.
                string coverTemplate = SFConverter.FindAuxFile("covertemplate.svg");
                if (File.Exists(coverTemplate))
                {
                    StreamReader sr = new StreamReader(coverTemplate);
                    string svg = sr.ReadToEnd();
                    sr.Close();
                    Char[] newLine = new Char[] { '\n' };
                    string[] mainTitle;
                    if (projectOptions.vernacularTitle.Length > 50)
                    {
                        svg = svg.Replace("font-size=\"100\"", "font-size=\"60\"");
                        mainTitle = ShortWordWrap(projectOptions.vernacularTitle, 48).Split(newLine);
                    }
                    else if (projectOptions.vernacularTitle.Length < 24)
                    {
                        svg = svg.Replace("font-size=\"100\"", "font-size=\"140\"");
                        mainTitle = ShortWordWrap(projectOptions.vernacularTitle, 17).Split(newLine);
                    }
                    else
                    {
                        mainTitle = ShortWordWrap(projectOptions.vernacularTitle, 20).Split(newLine);
                    }
                    string[] description = ShortWordWrap(projectOptions.EnglishDescription, 60).Split(newLine);
                    sw = new StreamWriter(coverOut);
                    svg = svg.Replace("^f", projectOptions.fontFamily).Replace("^4", description[0]);
                    if (mainTitle.Length > 1)
                    {
                        svg = svg.Replace("^1", mainTitle[0]);
                        svg = svg.Replace("^2", mainTitle[1]);
                        if (mainTitle.Length > 2)
                        {
                            svg = svg.Replace("^3", mainTitle[2]);
                        }
                        else
                        {
                            svg = svg.Replace("^3", "");
                        }
                    }
                    else
                    {
                        svg = svg.Replace("^1", "").Replace("^2", mainTitle[0]).Replace("^3", "");
                    }
                    if (description.Length > 1)
                    {
                        svg = svg.Replace("^5", description[1]);
                        if (description.Length > 2)
                        {
                            svg = svg.Replace("^6", description[2]);
                        }
                        else
                        {
                            svg = svg.Replace("^6", "");
                        }
                    }
                    else
                    {
                        svg = svg.Replace("^5", "").Replace("^6", "");
                    }
                    svg = svg.Replace("#000000", "#zback").Replace("#ffffff", "#zfg");
                    if (MakeUpColor(out dark, out light) > 1)
                    {
                        svg = svg.Replace("#zback", dark).Replace("#zfg", light);
                    }
                    else
                    {
                        svg = svg.Replace("#zfg", dark).Replace("#zback", light);
                    }
                    sw.Write(svg);
                    sw.Close();
                }
                else
                {
                    Logit.WriteError("The file covertemplate.svg is missing from the auxilliary program files.");
                }
            }

            // Look for .png files.
            pngPath = coverOut = Path.ChangeExtension(coverOut, "png");
            coverIn = Path.ChangeExtension(coverIn, "png");
            if (File.Exists(coverIn))
            {
                fileHelper.CopyFile(coverIn, coverOut);
            }
            // Look for .jpg files.
            coverOut = Path.ChangeExtension(coverOut, "jpg");
            coverIn = Path.ChangeExtension(coverIn, "jpg");
            if (File.Exists(coverIn))
            {
                fileHelper.CopyFile(coverIn, coverOut);
            }

            if (File.Exists(coverOut))
            {   // We have a jpg cover. Is it big enough?
                if (small)
                    return coverOut;
                /*
                System.Drawing.Image img = System.Drawing.Image.FromFile(coverOut);
                if ((img.Width >= 450) && (img.Height >= 700))
                {
                    if (!File.Exists(pngPath) && (Path.DirectorySeparatorChar == '/'))
                    {
                        fileHelper.RunCommand("convert \"" + coverOut + "\" \"" + pngPath + "\"");
                    }
                    return coverOut;
                }
                */
            }
            if (File.Exists(pngPath))
                return pngPath;
            if (Path.DirectorySeparatorChar == '/')
            {
                fileHelper.RunCommand("convertgraphics", "\"" + svgPath + "\" \"" + pngPath + "\"", "");
            }
            if (File.Exists(pngPath))
                return pngPath;
            return svgPath;
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
            if (useConsole || (GUIWriteString == null))
                Console.WriteLine(s);
            if (GUIWriteString != null)
                GUIWriteString(s);
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
            char nbsp = '\u00A0';
            string nobreakspace = nbsp.ToString();
/*            fileDate = File.GetLastWriteTimeUtc(inputPath);
            if (fileDate > sourceDate)
            {
                sourceDate = fileDate;
                projectOptions.SourceFileDate = sourceDate;
            }
*/
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
                                replacement = replacement.Replace("$s", nobreakspace); // Nonbreaking space
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
                    Logit.WriteError("Can't find preprocessing file " + tp);
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
            if (File.Exists(bookNamesCopy))
                File.Delete(bookNamesCopy);
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
                    (fileType != ".VRS") && (fileType != ".INI") &&
                    (fileType != ".CSV") && (fileType != ".TSV") &&
                    (fileType != ".CCT") && (fileType != ".TTF") &&
                    (!inputFile.EndsWith("~")) &&
                    (!lowerName.StartsWith("regexbackup")) &&
                    (!filename.StartsWith(".")) &&
                    (lowerName != "autocorrect.txt") &&
                    (lowerName != "tmp.txt") &&
                    (lowerName != "changes.txt") &&
                    (lowerName != "hyphenatedWords.txt") &&
                    (lowerName != "wordboundariesoutput.txt") &&
                    (lowerName != "printdraftchanges.txt"))
                {
                    ShowStatus("preprocessing " + filename);
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
<a href='http://creativecommons.org/licenses/by/4.0/'>Creative Commons Attribution license 4.0.</a></p>
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
            else if (projectOptions.ccbync)
            {
                copr.Append(@"<p>This translation is made available to you under the terms of the
<a href='http://creativecommons.org/licenses/by-nc/4.0/'>Creative Commons Attribution-No Derivatives license 4.0.</a></p>
<p>You may share, redistribute, or adapt this Bible translation or extracts from it in any format, provided that:</p>
<ul>
<li>You include the above copyright and source information.</li>
<li>You do not use this work for commercial purposes.</li>
</ul>
<p>Pictures included with Scriptures and other documents on this site are licensed just for use with those Scriptures and documents.
For other uses, please contact the respective copyright owners.</p>
");
            }
            else if (projectOptions.wbtverbatim)
            {
                copr.Append(@"<p>All rights reserved.</p>
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

        public void SetcurrentProject(string projDirName)
        {
            currentProject = projDirName;
            inputProjectDirectory = Path.Combine(inputDirectory, currentProject);
            outputProjectDirectory = Path.Combine(outputDirectory, currentProject);
            fileHelper.EnsureDirectory(outputProjectDirectory);
        }



        public string GetUsfxFilePath()
        {
            return Path.Combine(GetUsfxDirectoryPath(), "usfx.xml");
        }

        public string GetUsfxDirectoryPath()
        {
            return Path.Combine(outputProjectDirectory, "usfx");
        }


        private void ConvertUsfmToUsfx()
        {
            string UsfmDir = Path.Combine(outputProjectDirectory, "extendedusfm");
            string UsfxPath = GetUsfxDirectoryPath();
            string usfxName = GetUsfxFilePath();
            if (!Directory.Exists(UsfmDir))
            {
                UsfmDir = Path.Combine(outputProjectDirectory, "usfm");
            }
            if (!Directory.Exists(UsfmDir))
            {
                Logit.WriteError("ERROR: " + UsfmDir + " not found!");
                return;
            }
            // Start with an EMPTY USFX directory to avoid problems with old files
            Utils.DeleteDirectory(UsfxPath);
            fileHelper.EnsureDirectory(UsfxPath);
            UpdateStatus("converting from USFM to USFX; reading USFM");
            if ((projectOptions.languageId.Length < 3) || (projectOptions.translationId.Length < 3))
            {
                Logit.WriteError(string.Format(
                                    "ERROR: language and translation ids (%0 and %1) must be at least three characters each",
                                    projectOptions.languageId, projectOptions.translationId));
                return;
            }
            Utils.EnsureDirectory(UsfxPath);
            string logFile = Path.Combine(outputProjectDirectory, "ConversionReports.txt");
            Logit.OpenFile(logFile);
            SFConverter.scripture = new Scriptures(this);
            Logit.loggedError = false;
            Logit.loggedWarning = false;
            // Read a copy of BookNames.xml copied from the source USFM directory, if any.
            SFConverter.scripture.bkInfo.ReadDefaultBookNames(Path.Combine(outputProjectDirectory, "BookNames.xml"));
            SFConverter.scripture.assumeAllNested = projectOptions.relaxUsfmNesting;
            // Read the input USFM files into internal data structures.
            SFConverter.ProcessFilespec(Path.Combine(UsfmDir, "*.usfm"), Encoding.UTF8);
            UpdateStatus("converting from USFM to USFX; writing USFX");

            // Write out the USFX file.
            SFConverter.scripture.languageCode = projectOptions.languageId;
            SFConverter.scripture.WriteUSFX(usfxName);
            SFConverter.scripture.bkInfo.ReadUsfxVernacularNames(Path.Combine(Path.Combine(outputProjectDirectory, "usfx"), "usfx.xml"));
            string bookNames = Path.Combine(outputProjectDirectory, "BookNames.xml");
            SFConverter.scripture.bkInfo.WriteDefaultBookNames(bookNames);
            File.Copy(bookNames, Path.Combine(Path.Combine(outputProjectDirectory, "usfx"), "BookNames.xml"), true);
            bool runResult = projectOptions.lastRunResult;
            bool errorState = Logit.loggedError;
            fileHelper.revisePua(usfxName);
            if (!SFConverter.scripture.hasRefTags)
            {
                projectOptions.makeHotLinks = true;
                SFConverter.scripture.ReadRefTags(usfxName);
            }
            if (!SFConverter.scripture.ValidateUsfx(usfxName))
            {
                if (projectOptions.makeHotLinks && File.Exists(Path.ChangeExtension(usfxName, ".norefxml")))
                {
                    File.Move(usfxName, Path.ChangeExtension(usfxName, ".bad"));
                    File.Move(Path.ChangeExtension(usfxName, ".norefxml"), usfxName);
                    Logit.WriteLine("Retrying validation on usfx.xml without expanded references.");
                    if (SFConverter.scripture.ValidateUsfx(usfxName))
                    {
                        Logit.loggedError = errorState;
                        projectOptions.lastRunResult = runResult;
                        Logit.WriteLine("Validation passed without expanded references.");
                        projectOptions.makeHotLinks = false;
                    }
                    else
                    {
                        Logit.WriteError("Second validation failed.");
                    }
                }
                else
                {
                    Logit.WriteError("USFX validation failed. Please correct the input markup. Paratext basic checks, including schema checks, are recommended.");
                }
            }
            Logit.CloseFile();
            if (Logit.loggedError)
            {
                projectOptions.lastRunResult = false;
            }
            if (Logit.loggedWarning)
            {
                projectOptions.warningsFound = true;
            }
            UpdateStatus("converted USFM to USFX.");
        }


        /// <summary>
        /// Reads the first 3 lines of a text file into a string.
        /// </summary>
        /// <param name="FileName">name of the text file to read from</param>
        /// <returns>String with the first 3 lines of the file concatenated together</returns>
        public string ReadFirstLines(string FileName)
        {
            StreamReader sr = new StreamReader(FileName);
            string result = sr.ReadLine();
            result = result + sr.ReadLine();
            result = result + sr.ReadLine();
            sr.Close();
            return result;
        }

        public string HashMetadata()
        {
            thumb.HashString(projectOptions.languageId);
            thumb.HashString(projectOptions.translationId);
            thumb.HashString(projectOptions.translationTraditionalAbbreviation);
            thumb.HashString(projectOptions.languageName);
            thumb.HashString(projectOptions.languageNameInEnglish);
            thumb.HashString(projectOptions.dialect);
            thumb.HashString(projectOptions.vernacularTitle);
            thumb.HashString(projectOptions.EnglishDescription);
            thumb.HashString(projectOptions.lwcDescription);
            thumb.HashString(projectOptions.copyrightOwner);
            thumb.HashString(projectOptions.copyrightOwnerUrl);
            thumb.HashString(projectOptions.copyrightYears);
            thumb.HashString(projectOptions.copyrightOwnerAbbrev);
            thumb.HashString(projectOptions.rightsStatement);
            thumb.HashString(projectOptions.AudioCopyrightNotice);
            thumb.HashString(projectOptions.rodCode);
            thumb.HashString(projectOptions.ldml);
            thumb.HashString(projectOptions.script);
            thumb.HashString(projectOptions.country);
            thumb.HashString(projectOptions.countryCode);
            thumb.HashString(projectOptions.SwordName);
            thumb.HashString(projectOptions.ObsoleteSwordName);
            thumb.HashString(projectOptions.homeLink);
            thumb.HashString(projectOptions.goText);
            thumb.HashString(projectOptions.promoHtml);
            thumb.HashString(projectOptions.licenseHtml);
            thumb.HashBool(projectOptions.privateProject);
            thumb.HashBool(projectOptions.publicDomain);
            thumb.HashBool(projectOptions.ccbyndnc);
            thumb.HashBool(projectOptions.wbtverbatim);
            thumb.HashBool(projectOptions.ccbync);
            thumb.HashBool(projectOptions.ccbysa);
            thumb.HashBool(projectOptions.ccby);
            thumb.HashBool(projectOptions.ccbynd);
            thumb.HashBool(projectOptions.otherLicense);
            thumb.HashBool(projectOptions.allRightsReserved);
            thumb.HashBool(projectOptions.anonymous);
            thumb.HashBool(projectOptions.silentCopyright);
            return thumb.Finalize();
        }

        /// <summary>
        /// Examines file(s) in the named directory to determine what sort of input files are there based on their
        /// preambles, and to a lesser extent, on their suffixes. If the input is USX, it is expected to be in a DBL
        /// bundle, with metadata.xml in the named directory. The first .zip file found in the directory will be
        /// unzipped.
        /// </summary>
        /// <param name="dirName">The directory containing the source files (or the root level of the source files in the case of a DBL bundle</param>
        /// <returns>A string indicating the kind of source file, one of "usfx", "usx", or "usfm".</returns>
        public string GetSourceKind(string dirName, bool fingerprintIt = false)
        {
            string result = String.Empty;
            string s;
            string suffix;
            int i;
            DateTime fileDate;
            sourceDate = DateTime.MinValue;
            if (fingerprintIt)
                thumb = new Fingerprint();

            try
            {
                if (!Directory.Exists(dirName))
                    return result;
                string[] fileNames = Directory.GetFiles(dirName);
                foreach (string fileName in fileNames)
                {
                    suffix = Path.GetExtension(fileName).ToLowerInvariant();
                    if (suffix == ".zip")
                    {
                        fileHelper.RunCommand("unzip", "-n \"" + fileName + "\"", dirName);
                        string receivedDir = Path.Combine(inputProjectDirectory, "Received");
                        fileHelper.EnsureDirectory(receivedDir);
                        string destName = Path.Combine(receivedDir, Path.GetFileName(fileName));
                        if (File.Exists(destName))
                            File.Delete(destName);
                        File.Move(fileName, destName);
                    }
                }
                fileNames = Directory.GetFiles(dirName);
                Array.Sort(fileNames);
                for (i = 0; (i < fileNames.Length) /*&& (result == String.Empty) thumbprint at all files */; i++)
                {
                    string fileName = fileNames[i];
                    if (File.Exists(fileName))
                    {
                        suffix = Path.GetExtension(fileName).ToLowerInvariant();
                        if (!".zip .bak .lds .ssf .dbg .wdl .sty .htm .kb2 . html .css .swp .id .dic .ldml .json .vrs .ini .csv .tsv .cct".Contains(suffix)
                                && !fileName.EndsWith("~"))
                        {
                            fileDate = File.GetLastWriteTimeUtc(fileName);
                            if (fileDate > sourceDate)
                            {
                                sourceDate = fileDate;
                                projectOptions.SourceFileDate = sourceDate;
                            }
                            s = ReadFirstLines(fileName);
                            if (!String.IsNullOrEmpty(s))
                            {
                                if (s.Contains("\\id "))
                                    result = "usfm";
                                else if (s.Contains("<usfx"))
                                    result = "usfx";
                                else if (s.Contains("<usx"))
                                    result = "usx";
                                else if (s.Contains("<osis"))
                                    result = "osis";
                                if (fingerprintIt)
                                    thumb.HashFile(fileName);
                            }
                        }
                    }
                    else if (Directory.Exists(fileName))
                    {
                        result = GetSourceKind(fileName, fingerprintIt);
                    }
                }
                if (String.IsNullOrEmpty(result))
                {
                    string[] subdirectoryEntries = Directory.GetDirectories(dirName);
                    for (i = 0; (i < subdirectoryEntries.Length) && (result == String.Empty); i++)
                    {
                        result = GetSourceKind(subdirectoryEntries[i], fingerprintIt);
                    }
                }
                if (fingerprintIt)
                {
                    projectOptions.currentFingerprint = HashMetadata();
                    if ((projectOptions.currentFingerprint != projectOptions.builtFingerprint) && (sourceDate > projectOptions.lastRunDate))
                    {
                        projectOptions.contentUpdateDate = sourceDate;
                    }
                }



            }
            catch (Exception ex)
            {
                Logit.WriteError(ex.Message);
                Logit.WriteError(ex.StackTrace);
            }

            return result;
        }


        /// <summary>
        /// Create USFM from USFX
        /// </summary>
        private void NormalizeUsfm()
        {
            string logFile;
            try
            {

                string UsfmDir = Path.Combine(outputProjectDirectory, "extendedusfm");
                string UsfxName = Path.Combine(Path.Combine(outputProjectDirectory, "usfx"), "usfx.xml");
                if (!File.Exists(UsfxName))
                {
                    Logit.WriteError("ERROR normalizing USFM from USFX: " + UsfxName + " not found!");
                    return;
                }
                // Start with an EMPTY USFM directory to avoid problems with old files 
                Utils.DeleteDirectory(UsfmDir);
                fileHelper.EnsureDirectory(UsfmDir);
                UpdateStatus("Normalizing extended USFM from USFX. ");
                if (!fileHelper.fAllRunning)
                    return;
                logFile = Path.Combine(outputProjectDirectory, "usfx2usfm2_log.txt");
                Logit.OpenFile(logFile);
                SFConverter.scripture = new Scriptures(this);
                Logit.loggedError = false;
                Logit.loggedWarning = false;
                SFConverter.scripture.USFXtoUSFM(UsfxName, UsfmDir, projectOptions.translationId + ".usfm", true, projectOptions);

                UsfmDir = Path.Combine(outputProjectDirectory, "usfm");
                Utils.DeleteDirectory(UsfmDir);
                fileHelper.EnsureDirectory(UsfmDir);
                currentConversion = "Normalizing USFM from USFX. ";
                SFConverter.scripture.USFXtoUSFM(UsfxName, UsfmDir, projectOptions.translationId + ".usfm", false, projectOptions);
                Logit.CloseFile();
                if (Logit.loggedError)
                {
                    projectOptions.lastRunResult = false;
                }
                currentConversion = "Converted USFX to USFM.";
            }
            catch (Exception ex)
            {
                Logit.WriteError("Error normalizing USFM from USFX: "+ ex.Message);
            }
        }



        /// <summary>
        /// Import all USX files in a directory to USFX, then normalize by converting to USFM and back to USFX.
        /// </summary>
        /// <param name="SourceDir">directory containing USX files</param>
        private void ImportUsx(string SourceDir)
        {
            string usfxDir = Path.Combine(outputProjectDirectory, "usfx");
            string tmpname = Path.Combine(usfxDir, "tempusfx.xml");
            string bookNamesFile = Path.Combine(SourceDir, "BookNames.xml");
            try
            {
                string logFile = Path.Combine(outputProjectDirectory, "UsxConversionReports.txt");
                Logit.OpenFile(logFile);
                Logit.loggedError = false;
                Logit.loggedWarning = false;
                // Sanity check
                if (!Directory.Exists(SourceDir))
                {
                    Logit.WriteError("ERROR: "+SourceDir + " not found!");
                    return;
                }

                UpdateStatus("converting from USX to temporary USFX");

                // Convert from USX to USFX. The first USFX file will be out of order and lack <ve/> tags.
                // Start with an EMPTY USFM directory to avoid problems with old files 
                fileHelper.EnsureDirectory(usfxDir);
                string usfxName = Path.Combine(usfxDir, "usfx.xml");
                fileHelper.CopyFile(bookNamesFile, Path.Combine(usfxDir, "BookNames.xml"), true);
                Usx2Usfx uu = new Usx2Usfx();
                currentConversion = "Converting " + SourceDir + " to " + usfxName;
                uu.Convert(SourceDir, usfxName);

                try
                {
                    fileHelper.CopyFile(usfxName, tmpname, true);
                }
                catch (Exception ex)
                {
                    Logit.WriteError("ERROR copying tempusfx.xml: "+ ex.Message);
                }

                // Convert from USFX to USFM.
                UpdateStatus("converting from initial USFX to USFM");
                if (!fileHelper.fAllRunning)
                    return;
                string usfmDir = Path.Combine(outputProjectDirectory, "usfm");
                Utils.DeleteDirectory(usfmDir);
                fileHelper.EnsureDirectory(usfmDir);
                fileHelper.CopyFile(bookNamesFile, Path.Combine(usfmDir, "BookNames.xml"), true);
                SFConverter.scripture = new Scriptures(this);
                SFConverter.scripture.bkInfo.ReadDefaultBookNames(bookNamesFile);
                SFConverter.scripture.USFXtoUSFM(usfxName, usfmDir, projectOptions.translationId + ".usfm", false, projectOptions);

                usfmDir = Path.Combine(outputProjectDirectory, "extendedusfm");
                Utils.DeleteDirectory(usfmDir);
                fileHelper.EnsureDirectory(usfmDir);
                fileHelper.CopyFile(bookNamesFile, Path.Combine(usfmDir, "BookNames.xml"), true);
                SFConverter.scripture = new Scriptures(this);
                SFConverter.scripture.bkInfo.ReadDefaultBookNames(bookNamesFile);
                SFConverter.scripture.USFXtoUSFM(usfxName, usfmDir, projectOptions.translationId + ".usfm", true, projectOptions);

                // Recreate USFX from USFM, this time with <ve/> tags and in canonical order
                SFConverter.scripture.assumeAllNested = projectOptions.relaxUsfmNesting;
                // Read the input USFM files into internal data structures.
                SFConverter.ProcessFilespec(Path.Combine(usfmDir, "*.usfm"), Encoding.UTF8);
                UpdateStatus("converting from USFM to USFX; writing USFX");

                // Write out the USFX file.
                SFConverter.scripture.languageCode = projectOptions.languageId;
                SFConverter.scripture.WriteUSFX(usfxName);
                string bookNames = Path.Combine(Path.Combine(outputProjectDirectory, "usfx"), "BookNames.xml");
                SFConverter.scripture.bkInfo.WriteDefaultBookNames(bookNames);
                bool errorState = Logit.loggedError;
                bool runResult = projectOptions.lastRunResult;
                fileHelper.revisePua(usfxName);
                SFConverter.scripture.ReadRefTags(usfxName);
                if (!SFConverter.scripture.ValidateUsfx(usfxName))
                {
                    File.Move(usfxName, Path.ChangeExtension(usfxName, ".bad"));
                    File.Move(Path.ChangeExtension(usfxName, ".norefxml"), usfxName);
                    Logit.WriteLine("Retrying validation on usfx.xml without expanded references.");
                    if (SFConverter.scripture.ValidateUsfx(usfxName))
                    {
                        projectOptions.lastRunResult = runResult;
                        Logit.loggedError = errorState;
                        Logit.WriteLine("Validation passed without expanded references.");
                        projectOptions.makeHotLinks = false;
                    }
                    else
                    {
                        Logit.WriteError("Second validation failed.");
                    }
                }
                Logit.CloseFile();
                if (Logit.loggedError)
                {
                    projectOptions.lastRunResult = false;
                }
                else
                {

                    Utils.DeleteFile(tmpname);
                }
                if (Logit.loggedWarning)
                {
                    projectOptions.warningsFound = true;
                }

                UpdateStatus("converted USFM to USFX.");
            }
            catch (Exception ex)
            {

                Logit.WriteError("Error importing USX: "+ex.Message);
            }
        }



        /// <summary>
        /// Import a USFX source file.
        /// </summary>
        /// <param name="SourceDir"></param>
        private void ImportUsfx(string SourceDir)
        {
            string logFile;
            try
            {

                string UsfmDir = Path.Combine(outputProjectDirectory, "extendedusfm");
                if (!Directory.Exists(SourceDir))
                {
                    Logit.WriteError("ERROR: " + SourceDir + " not found!");
                    return;
                }
                // Start with an EMPTY USFM directory to avoid problems with old files 
                Utils.DeleteDirectory(UsfmDir);
                fileHelper.EnsureDirectory(UsfmDir);
                string usfxDir = Path.Combine(outputProjectDirectory, "usfx");
                fileHelper.EnsureDirectory(usfxDir);
                string[] inputFileNames = Directory.GetFiles(SourceDir);
                if (inputFileNames.Length == 0)
                {
                    Logit.WriteError("ERROR: no files found in " + SourceDir);
                    return;
                }

                foreach (string inputFile in inputFileNames)
                {
                    string filename = Path.GetFileName(inputFile);
                    string fileType = Path.GetExtension(filename).ToUpper();
                    if ((fileType == ".USFX") || (fileType == ".XML"))
                    {
                        UpdateStatus("processing " + filename);
                        if (!fileHelper.fAllRunning)
                            break;
                        XmlTextReader xr = new XmlTextReader(inputFile);
                        if (xr.MoveToContent() == XmlNodeType.Element)
                        {
                            if (xr.Name == "usfx")
                            {
                                DateTime fileDate;
                                fileDate = File.GetLastWriteTimeUtc(inputFile);
                                sourceDate = projectOptions.SourceFileDate;
                                if (fileDate > projectOptions.SourceFileDate)
                                {
                                    sourceDate = fileDate;
                                    projectOptions.SourceFileDate = sourceDate;
                                }

                                logFile = Path.Combine(outputProjectDirectory, "usfx2usfm_log.txt");
                                Logit.OpenFile(logFile);
                                SFConverter.scripture = new Scriptures(this);
                                Logit.loggedError = false;
                                Logit.loggedWarning = false;
                                UpdateStatus("converting from USFX to USFM");
                                SFConverter.scripture.USFXtoUSFM(inputFile, UsfmDir, projectOptions.translationId + ".usfm", true, projectOptions);
                                UsfmDir = Path.Combine(outputProjectDirectory, "usfm");
                                // Start with an EMPTY USFM directory to avoid problems with old files 
                                Utils.DeleteDirectory(UsfmDir);
                                fileHelper.EnsureDirectory(UsfmDir);
                                SFConverter.scripture.USFXtoUSFM(inputFile, UsfmDir, projectOptions.translationId + ".usfm", false, projectOptions);
                                Logit.CloseFile();
                                if (Logit.loggedError)
                                {
                                    projectOptions.lastRunResult = false;
                                }
                                if (Logit.loggedWarning)
                                {
                                    projectOptions.warningsFound = true;
                                }
                                currentConversion = "converted USFX to USFM.";
                                File.Copy(inputFile, Path.Combine(usfxDir, "usfx.xml"), true);
                            }
                            else if (xr.Name == "vernacularParms")
                            {
                                // TODO: Insert code here to read metadata in this file into options file.
                                File.Copy(inputFile, Path.Combine(usfxDir, "vernacularParms.xml"), true);
                            }
                            else if (xr.Name == "vernacularParmsMiscellaneous")
                            {
                                // TODO: Insert code here to read this file into options file.
                                File.Copy(inputFile, Path.Combine(usfxDir, "vernacularParmsMiscellaneous.xml"), true);
                            }
                        }
                        xr.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                Logit.WriteError("Error importing USFX: "+ ex.Message);
            }
        }

        public void PrepareSearchText()
        {
            string logFile = Path.Combine(outputProjectDirectory, "SearchReport.txt");
            Logit.OpenFile(logFile);
            try
            {
                ExtractSearchText est = new ExtractSearchText();
                est.projectOptions = projectOptions;
                string vplPath = Path.Combine(outputProjectDirectory, "vpl");
                string readAloudPath = Path.Combine(outputProjectDirectory, "readaloud");
                string UsfxPath = Path.Combine(outputProjectDirectory, "usfx");
                string auxPath = Path.Combine(outputProjectDirectory, "search");
                string verseText = Path.Combine(auxPath, "verseText.xml");
                string sqlFile = Path.Combine(outputProjectDirectory, "sql");
                Utils.EnsureDirectory(auxPath);
                Utils.EnsureDirectory(sqlFile);
                Utils.EnsureDirectory(readAloudPath);
                est.Filter(Path.Combine(UsfxPath, "usfx.xml"), verseText);
                est.WriteSearchSql(verseText, currentProject, Path.Combine(sqlFile, currentProject + "_vpl.sql"));
                est.WriteAudioScriptText(verseText, readAloudPath, currentProject);
                est.WriteSearchSql(Path.ChangeExtension(verseText, ".lemma"), currentProject, Path.Combine(sqlFile, currentProject + "_lemma.sql"));
                if (est.LongestWordLength > projectOptions.longestWordLength)
                    projectOptions.longestWordLength = est.LongestWordLength;
                // Copy search text files to VPL output.
                Utils.DeleteDirectory(vplPath);
                Utils.EnsureDirectory(vplPath);
                File.Copy(verseText, Path.Combine(vplPath, currentProject + "_vpl.xml"));
                File.Copy(Path.Combine(auxPath, "verseText.vpltxt"), Path.Combine(vplPath, currentProject + "_vpl.txt"));
                File.Copy(Path.Combine(inputDirectory, "haiola.css"), Path.Combine(vplPath, "haiola.css"));
                StreamWriter htm = new StreamWriter(Path.Combine(vplPath, currentProject + "_about.htm"));
                htm.WriteLine("<!DOCTYPE html>");
                htm.WriteLine("<html>");
                htm.WriteLine("<head>");
                htm.WriteLine("<meta charset=\"UTF-8\" />");
                htm.WriteLine("<link rel=\"stylesheet\" href=\"haiola.css\" type=\"text/css\" />");
                htm.WriteLine("<meta name=\"viewport\" content=\"user-scalable=yes, initial-scale=1, minimum-scale=1, width=device-width\"/>");
                htm.WriteLine("<title>About {0}_vpl</title>", currentProject);
                htm.WriteLine("</head>");
                htm.WriteLine("<body class=\"mainDoc\"");
                htm.WriteLine("<p>This archive contains BIBLE TEXT ONLY. All formatting, paragraph breaks, notes, introductions, noncanonical section titles, etc., have been removed. The file ending \"_vpl.txt\" is designed for import into BibleWorks and similar Bible study programs. The file ending \"_vpl.xml\" contains the same information, but is in XML format and uses standard SIL/UBS book abbreviations.");
                htm.WriteLine("The file ending \"_vpl.sql\" contains the same information formatted to create a SQL data table.</p>");
                htm.WriteLine(@"<p>Check for updates and other Bible translations in this format at <a href='https:\\eBible.org\Scriptures\'>http:\\eBible.org\Scriptures\</a></p>");
                htm.WriteLine("<hr />");
                htm.WriteLine(copyrightPermissionsStatement());
                htm.WriteLine("</body></html>");
                htm.Close();
                if (Logit.loggedError)
                {
                    projectOptions.lastRunResult = false;
                }
                if (Logit.loggedWarning)
                {
                    projectOptions.warningsFound = true;
                }
            }
            catch (Exception ex)
            {
                Logit.WriteError("Error preparing search text: " + ex.Message);
            }
            Logit.CloseFile();
        }


        /// <summary>
        /// Convert USFX to Modified OSIS
        /// </summary>
        public void ConvertUsfxToMosis()
        {
            currentConversion = "writing MOSIS";
            if ((projectOptions.languageId.Length < 3) || (projectOptions.translationId.Length < 3))
                return;
            string UsfxPath = Path.Combine(outputProjectDirectory, "usfx");
            string mosisPath = Path.Combine(outputProjectDirectory, "mosis");
            if (!Directory.Exists(UsfxPath))
            {
                Logit.WriteError(UsfxPath + " not found!");
                return;
            }
            string usfxFilePath = Path.Combine(UsfxPath, "usfx.xml");
            string mosisFilePath = Path.Combine(mosisPath, projectOptions.translationId + "_osis.xml");

            Utils.EnsureDirectory(outputDirectory);
            Utils.EnsureDirectory(outputProjectDirectory);
            Utils.EnsureDirectory(mosisPath);

            usfxToMosisConverter toMosis = new usfxToMosisConverter();
            toMosis.commandLineKludge = clKludge;
            if (projectOptions.redistributable && File.Exists(eBibleCertified))
            {
                projectOptions.textSourceUrl = "https://eBible.org/Scriptures/";
            }
            else
            {
                projectOptions.textSourceUrl = "";
            }
            toMosis.languageCode = projectOptions.languageId;
            toMosis.translationId = projectOptions.translationId;
            toMosis.revisionDateTime = projectOptions.contentUpdateDate;
            toMosis.vernacularTitle = projectOptions.vernacularTitle;
            toMosis.contentCreator = projectOptions.contentCreator;
            toMosis.contentContributor = projectOptions.contributor;
            toMosis.englishDescription = projectOptions.EnglishDescription;
            toMosis.lwcDescription = projectOptions.lwcDescription;
            toMosis.printPublisher = projectOptions.printPublisher;
            toMosis.ePublisher = projectOptions.electronicPublisher;
            toMosis.languageName = projectOptions.languageNameInEnglish;
            toMosis.dialect = projectOptions.dialect;
            toMosis.vernacularLanguageName = projectOptions.languageName;
            toMosis.projectOptions = projectOptions;
            toMosis.swordDir = Path.Combine(dataRootDir, "sword");
            toMosis.swordRestricted = Path.Combine(dataRootDir, "swordRestricted");
            toMosis.copyrightNotice = projectOptions.publicDomain ? "public domain" : "Copyright © " + projectOptions.copyrightYears + " " + projectOptions.copyrightOwner;
            toMosis.rightsNotice = projectOptions.rightsStatement + "<br/> ";
            if (projectOptions.publicDomain)
            {
                toMosis.rightsNotice += @" This work is in the Public Domain. That means that it is not copyrighted.
 It is still subject to God's Law concerning His Word, including the Great Commission (Matthew 28:18-20).
";
            }
            else if (projectOptions.ccbyndnc)
            {
                toMosis.rightsNotice += @" This Bible translation is made available to you under the terms of the
 Creative Commons Attribution-Noncommercial-No Derivative Works license (http://creativecommons.org/licenses/by-nc-nd/4.0/).";
            }
            else if (projectOptions.ccbysa)
            {
                toMosis.rightsNotice += @" This Bible translation is made available to you under the terms of the
 Creative Commons Attribution-Share-Alike license (http://creativecommons.org/licenses/by-sa/4.0/).";
            }
            else if (projectOptions.ccbynd)
            {
                toMosis.rightsNotice += @" This Bible translation is made available to you under the terms of the
 Creative Commons Attribution-No Derivatives license (http://creativecommons.org/licenses/by-na/4.0/).";
            }
            toMosis.infoPage = copyrightPermissionsStatement();
            string logFile = Path.Combine(outputProjectDirectory, "MosisConversionReport.txt");
            Logit.OpenFile(logFile);
            toMosis.langCodes = languageCodes;
            toMosis.ConvertUsfxToMosis(usfxFilePath, mosisFilePath);
            Logit.CloseFile();
            if (Logit.loggedError)
            {
                projectOptions.lastRunResult = false;
            }
            if (Logit.loggedWarning)
            {
                projectOptions.warningsFound = true;
            }
        }




        /// <summary>
        /// Convert USFX to sile-friendly XML, one file per book
        /// </summary>
        public void ConvertUsfxToSile()
        {
            currentConversion = "writing sile";
            if ((projectOptions.languageId.Length < 3) || (projectOptions.translationId.Length < 3))
                return;
            string UsfxPath = Path.Combine(outputProjectDirectory, "usfx");
            string silePath = Path.Combine(outputProjectDirectory, "sile");
            if (!Directory.Exists(UsfxPath))
            {
                Logit.WriteError(UsfxPath + " not found!");
                return;
            }
            string usfxFilePath = Path.Combine(UsfxPath, "usfx.xml");

            Utils.EnsureDirectory(outputDirectory);
            Utils.EnsureDirectory(outputProjectDirectory);
            Utils.EnsureDirectory(silePath);
            Usfx2SILE sileConverter = new Usfx2SILE(this);
            sileConverter.ConvertUsfxToSile(usfxFilePath, silePath);
        }

        /// <summary>
        /// Ensure that a named template file exists in the input directory.
        /// </summary>
        /// <param name="fileName">Name of file to find.</param>
        public void EnsureTemplateFile(string fileName)
        {
            EnsureTemplateFile(fileName, inputDirectory);
        }

        /// <summary>
        /// Ensure that a named template file exists in the named directory.
        /// </summary>
        /// <param name="fileName">File to find in program files</parm>
        /// <param name="destDirectory">Proper template location.</param>
        public void EnsureTemplateFile(string fileName, string destDirectory)
        {
            try
            {
                string sourcePath = WordSend.SFConverter.FindAuxFile(fileName);
                string destPath = Path.Combine(destDirectory, fileName);
                if ((!File.Exists(destPath)) && (File.Exists(sourcePath)))
                {
                    File.Copy(sourcePath, destPath);
                }
            }
            catch (Exception ex)
            {
                Logit.WriteError(ex.Message + " Error ensuring " + fileName + " is in " + inputDirectory);
            }
        }




        public void ConvertUsfxToPortableHtml()
        {
            int i;
            if ((projectOptions.languageId.Length < 3) || (projectOptions.translationId.Length < 3))
                return;
            currentConversion = "writing portable HTML";
            string UsfxPath = Path.Combine(outputProjectDirectory, "usfx");
            string htmlPath = Path.Combine(outputProjectDirectory, "html");
            if (!Directory.Exists(UsfxPath))
            {
                Logit.WriteError(UsfxPath + " not found!");
                return;
            }
            Utils.EnsureDirectory(outputDirectory);
            Utils.EnsureDirectory(outputProjectDirectory);
            Utils.EnsureDirectory(htmlPath);
            string propherocss = Path.Combine(htmlPath, projectOptions.customCssFileName);

            Utils.DeleteFile(propherocss);
            // Copy cascading style sheet from project directory, or if not there, BibleConv/input/.
            string specialCss = Path.Combine(inputProjectDirectory, projectOptions.customCssFileName);
            if (File.Exists(specialCss))
                File.Copy(specialCss, propherocss);
            else
                File.Copy(Path.Combine(inputDirectory, projectOptions.customCssFileName), propherocss);

            // Copy any extra files from the htmlextras directory in the project directory to the output.
            // This is for introduction files, pictures, etc.
            string htmlExtras = Path.Combine(inputProjectDirectory, "htmlextras");
            if (Directory.Exists(htmlExtras))
            {
                WordSend.fileHelper.CopyDirectory(htmlExtras, htmlPath);
            }

            usfxToHtmlConverter toHtm;
            if (projectOptions.UseFrames)
            {
                var framedConverter = new UsfxToFramedHtmlConverter();
                framedConverter.HideNavigationButtonText = projectOptions.HideNavigationButtonText;
                framedConverter.ShowNavigationButtonText = projectOptions.ShowNavigationButtonText;
                toHtm = framedConverter;
            }
            else
            {
                if (projectOptions.GenerateMobileHtml)
                {
                    toHtm = new usfx2MobileHtml();
                }
                else
                {
                    toHtm = new usfxToHtmlConverter();
                }
            }
            toHtm.Jesusfilmlink = projectOptions.JesusFilmLinkTarget;
            toHtm.Jesusfilmtext = projectOptions.JesusFilmLinkText;
            toHtm.stripPictures = false;
            toHtm.htmlextrasDir = Path.Combine(inputProjectDirectory, "htmlextras");
            string logFile = Path.Combine(outputProjectDirectory, "HTMLConversionReport.txt");
            Logit.OpenFile(logFile);
            string theIndexDate = toHtm.indexDate.ToString("d MMM yyyy");
            string thesourceDate = sourceDate.ToString("d MMM yyyy");
            if (File.Exists(eBibleCertified) && !projectOptions.privateProject)
            {
                toHtm.indexDateStamp = "HTML generated with <a href='https://haiola.org'>Haiola</a> by <a href='https://eBible.org'>eBible.org</a> " +
                    toHtm.indexDate.ToString("d MMM yyyy") +
                    " from source files dated " + sourceDate.ToString("d MMM yyyy") +
                    "</a><br/>";
            }
            else
            {
                toHtm.indexDateStamp = "HTML generated by <a href='https://haiola.org'>Haiola</a> " + toHtm.indexDate.ToString("d MMM yyyy") +
                    " from source files dated " + sourceDate.ToString("d MMM yyyy");
            }
            toHtm.GeneratingConcordance = projectOptions.GenerateConcordance || projectOptions.UseFrames;
            toHtm.CrossRefToFilePrefixMap = projectOptions.CrossRefToFilePrefixMap;
            string usfxFilePath = Path.Combine(UsfxPath, "usfx.xml");
            toHtm.bookInfo.ReadPublicationOrder(orderFile);
            toHtm.MergeXref(Path.Combine(inputProjectDirectory, "xref.xml"));
            toHtm.sourceLink = expandPercentEscapes("<a href=\"https://%h/%t\">%v</a>");
            toHtm.textDirection = projectOptions.textDir;
            toHtm.customCssName = projectOptions.customCssFileName;
            toHtm.stripManualNoteOrigins = projectOptions.stripNoteOrigin;
            toHtm.noteOriginFormat = projectOptions.xoFormat;
            toHtm.englishDescription = projectOptions.EnglishDescription;
            toHtm.preferredFont = projectOptions.fontFamily;
            toHtm.fcbhId = projectOptions.fcbhId;
            toHtm.redistributable = projectOptions.redistributable;
            toHtm.coverName = String.Empty;// = Path.GetFileName(preferredCover);
            toHtm.projectOutputDir = outputProjectDirectory;
            toHtm.projectOptions = projectOptions;

            //string coverPath = Path.Combine(htmlPath, toHtm.coverName);
            //File.Copy(preferredCover, coverPath);

            if (!String.IsNullOrEmpty(certified))
            {
                try
                {
                    File.Copy(certified, Path.Combine(htmlPath, "eBible.org_certified.jpg"));
                    toHtm.indexDateStamp = toHtm.indexDateStamp + "<br /><a href='https://eBible.org/certified/' target='_blank'><img src='eBible.org_certified.jpg' alt='eBible.org certified' /></a>";
                }
                catch (Exception ex)
                {
                    Logit.WriteLine(ex.Message);
                }
            }
            toHtm.xrefCall.SetMarkers(projectOptions.xrefCallers);
            toHtm.projectInputDir = inputProjectDirectory;
            toHtm.footNoteCall.SetMarkers(projectOptions.footNoteCallers);
            toHtm.ConvertUsfxToHtml(usfxFilePath, htmlPath,
                projectOptions.vernacularTitle,
                projectOptions.languageId,
                projectOptions.translationId,
                projectOptions.chapterLabel,
                projectOptions.psalmLabel,
                "<a href='copyright.htm'>" + usfxToHtmlConverter.EscapeHtml(shortCopyrightMessage) + "</a>",
                expandPercentEscapes(projectOptions.homeLink),
                expandPercentEscapes(projectOptions.footerHtml),
                expandPercentEscapes(projectOptions.indexHtml),
                copyrightPermissionsStatement(),
                projectOptions.ignoreExtras,
                projectOptions.goText);
            toHtm.bookInfo.RecordStats(projectOptions);
            projectOptions.commonChars = toHtm.commonChars;
            projectOptions.Write();
            string fontsDir = Path.Combine(htmlPath, "fonts");
            fileHelper.EnsureDirectory(fontsDir);
            string fontSource = Path.Combine(dataRootDir, "fonts");
            string fontName = projectOptions.fontFamily.ToLower().Replace(' ', '_');
            fileHelper.CopyFile(Path.Combine(fontSource, fontName + ".ttf"), Path.Combine(fontsDir, fontName + ".ttf"));
            fileHelper.CopyFile(Path.Combine(fontSource, fontName + ".woff"), Path.Combine(fontsDir, fontName + ".woff"));
            fileHelper.CopyFile(Path.Combine(fontSource, fontName + ".eot"), Path.Combine(fontsDir, fontName + ".eot"));
            Logit.CloseFile();
            if (Logit.loggedError)
            {
                projectOptions.lastRunResult = false;
            }
            if (Logit.loggedWarning)
            {
                projectOptions.warningsFound = true;
            }

            Logit.WriteLine("Writing auxilliary metadata files.");
            if (!fileHelper.fAllRunning)
                return;

            // We currently have the information handy to write some auxilliary XML files
            // that contain metadata. We will put these in the USFX directory.

            XmlTextWriter xml = new XmlTextWriter(Path.Combine(UsfxPath, projectOptions.translationId + "-VernacularParms.xml"), System.Text.Encoding.UTF8);
            xml.Formatting = System.Xml.Formatting.Indented;
            xml.WriteStartDocument();
            xml.WriteStartElement("vernacularParms");
            // List vernacular full book titles from \toc1 (or \mt)
            foreach (WordSend.BibleBookRecord br in WordSend.usfxToHtmlConverter.bookList)
            {
                xml.WriteStartElement("scriptureBook");
                xml.WriteAttributeString("ubsAbbreviation", br.tla);
                xml.WriteAttributeString("parm", "vernacularFullName");
                xml.WriteString(br.vernacularLongName);
                xml.WriteEndElement();  // scriptureBook
            }
            // List vernacular short names for running headers and links from \toc2 (or \h)
            foreach (WordSend.BibleBookRecord br in WordSend.usfxToHtmlConverter.bookList)
            {
                xml.WriteStartElement("scriptureBook");
                xml.WriteAttributeString("ubsAbbreviation", br.tla);
                xml.WriteAttributeString("parm", "vernacularAbbreviatedName");
                xml.WriteString(br.vernacularShortName);
                xml.WriteEndElement();  // scriptureBook
            }
            // List vernacular abbreviations from \toc3
            foreach (WordSend.BibleBookRecord br in WordSend.usfxToHtmlConverter.bookList)
            {
                if (!String.IsNullOrEmpty(br.vernacularAbbreviation))
                {
                    xml.WriteStartElement("scriptureBook");
                    xml.WriteAttributeString("ubsAbbreviation", br.tla);
                    xml.WriteAttributeString("parm", "vernacularBookAbbreviation");
                    xml.WriteString(br.vernacularAbbreviation);
                    xml.WriteEndElement();  // scriptureBook
                }
            }
            // Dublin Core library card data
            xml.WriteStartElement("dcMeta");
            xml.WriteAttributeString("xmlns:xsi", "http://www.w3.org/2001/XMLSchema-instance");
            xml.WriteAttributeString("xsi:schemaLocation", "http://dublincore.org/schemas/xmls/qdc/2008/02/11/dc.xsd");
            xml.WriteAttributeString("xmlns:dc", "http://purl.org/dc/elements/1.1/");
            xml.WriteElementString("dc:creator", projectOptions.contentCreator);
            xml.WriteElementString("dc:contributor", projectOptions.contributor);
            string title = projectOptions.vernacularTitle;
            if (title.Length == 0)
                title = projectOptions.EnglishDescription;
            xml.WriteElementString("dc:title", title);
            xml.WriteElementString("dc:description", projectOptions.EnglishDescription);
            xml.WriteElementString("dc:date", projectOptions.contentUpdateDate.ToString("yyyy-MM-dd"));
            xml.WriteElementString("dc:format", "digital");
            xml.WriteElementString("dc:language", projectOptions.languageId);
            xml.WriteElementString("dc:publisher", projectOptions.electronicPublisher);
            string rights = String.Empty;
            string shortRights = projectOptions.translationId + " Scripture ";
            string copyright;
            if (projectOptions.publicDomain)
            {
                copyright = rights = "Public Domain";
                shortRights = shortRights + "is in the Public Domain.";
            }
            else if (projectOptions.anonymous)
            {
                copyright = "Copyright © " + projectOptions.copyrightYears + ". ";
            }
            else
            {
                copyright = "Copyright © " + projectOptions.copyrightYears + " " + projectOptions.copyrightOwner;
            }
            copyright += Environment.NewLine + projectOptions.rightsStatement + Environment.NewLine;
            if (projectOptions.ccbyndnc)
            {
                rights = copyright + @"
This work is made available to you under the terms of the Creative Commons Attribution-Noncommercial-No Derivative Works license at https://creativecommons.org/licenses/by-nc-nd/4.0/." +
                Environment.NewLine;
                shortRights = shortRights + copyright + " Creative Commons BY-NC-ND license.";
            }
            else if (projectOptions.ccbynd)
            {
                rights = copyright + @"
This work is made available to you under the terms of the Creative Commons Attribution-No Derivative Works license at https://creativecommons.org/licenses/by-nd/4.0/." +
                Environment.NewLine;
                shortRights = shortRights + copyright + " Creative Commons BY-ND license.";
            }
            else if (projectOptions.ccbysa)
            {
                rights = copyright + @"
This work is made available to you under the terms of the Creative Commons Attribution-Share-Alike license at https://creativecommons.org/licenses/by-sa/4.0/." +
                Environment.NewLine;
                shortRights = shortRights + copyright + " Creative Commons BY-SA license.";
            }
            else if (projectOptions.ccbync)
            {
                rights = copyright + @"
This work is made available to you under the terms of the Creative Commons Attribution-Noncommercial license at https://creativecommons.org/licenses/by-nc/4.0/." +
                Environment.NewLine;
                shortRights = shortRights + copyright + " Creative Commons BY-NC license.";
            }
            else if (projectOptions.wbtverbatim)
            {
                rights = copyright + @"
All rights reserved." +
                Environment.NewLine;
                shortRights = shortRights + copyright + " All rights reserved.";
            }
            else if (projectOptions.otherLicense)
            {
                rights = copyright + Environment.NewLine + projectOptions.rightsStatement;
                shortRights = shortRights + copyright;
            }
            else if (projectOptions.allRightsReserved)
            {
                rights = copyright + " All rights reserved.";
                shortRights = shortRights + rights;
            }
            rights = rights + projectOptions.rightsStatement;
            xml.WriteElementString("dc:rights", rights);
            xml.WriteElementString("dc:identifier", String.Empty);
            xml.WriteElementString("dc:type", String.Empty);
            xml.WriteEndElement();  // dcMeta
            xml.WriteElementString("numberSystem", projectOptions.numberSystem);
            xml.WriteElementString("chapterAndVerseSeparator", projectOptions.CVSeparator);
            xml.WriteElementString("rangeSeparator", projectOptions.rangeSeparator);
            xml.WriteElementString("multiRefSameChapterSeparator", projectOptions.multiRefSameChapterSeparator);
            xml.WriteElementString("multiRefDifferentChapterSeparator", projectOptions.multiRefDifferentChapterSeparator);
            xml.WriteElementString("verseNumberLocation", projectOptions.verseNumberLocation);
            xml.WriteElementString("footnoteMarkerStyle", projectOptions.footnoteMarkerStyle);
            xml.WriteElementString("footnoteMarkerResetAt", projectOptions.footnoteMarkerResetAt);
            xml.WriteElementString("footnoteMarkers", projectOptions.footNoteCallers);
            xml.WriteElementString("BookSourceForMarkerXt", projectOptions.BookSourceForMarkerXt);
            xml.WriteElementString("BookSourceForMarkerR", projectOptions.BookSourceForMarkerR);
            xml.WriteElementString("iso", projectOptions.languageId);
            xml.WriteElementString("isoVariant", projectOptions.dialect);
            xml.WriteElementString("langName", projectOptions.languageName);
            xml.WriteElementString("textDir", projectOptions.textDir);
            xml.WriteElementString("hasNotes", (!projectOptions.ignoreExtras).ToString()); //TODO: check to see if translation has notes or not.
            xml.WriteElementString("coverTitle", projectOptions.vernacularTitle);
            xml.WriteEndElement();	// vernacularParms
            xml.WriteEndDocument();
            xml.Close();

            xml = new XmlTextWriter(Path.Combine(UsfxPath, projectOptions.translationId + "-VernacularAdditional.xml"), System.Text.Encoding.UTF8);
            xml.Formatting = System.Xml.Formatting.Indented;
            xml.WriteStartDocument();
            xml.WriteStartElement("vernacularParmsMiscellaneous");
            xml.WriteElementString("translationId", projectOptions.translationId);
            // xml.WriteElementString("otmlId", " ");
            xml.WriteElementString("versificationScheme", projectOptions.versificationScheme);
            xml.WriteElementString("checkVersification", "No");
            // xml.WriteElementString("osis2SwordOptions", globe.globe.projectOptions.osis2SwordOptions);
            // xml.WriteElementString("otmlRenderChapterNumber", globe.globe.projectOptions.otmlRenderChapterNumber);
            xml.WriteElementString("copyright", shortRights);
            xml.WriteEndElement();	// vernacularParmsMiscellaneous
            xml.WriteEndDocument();
            xml.Close();

            // Write the ETEN DBL MetaData.xml file in the usfx directory.
            string metaXmlName = Path.Combine(UsfxPath, projectOptions.translationId + "metadata.xml");
            xml = new XmlTextWriter(metaXmlName, System.Text.Encoding.UTF8);
            xml.Formatting = System.Xml.Formatting.Indented;
            xml.WriteStartDocument();
            // xml.WriteProcessingInstruction("xml-model", "href=\"metadataWbt-1.3.rnc\" type=\"application/relax-ng-compact-syntax\"");
            xml.WriteStartElement("DBLMetadata");
            string etendblid = projectOptions.paratextGuid;
            if (etendblid.Length > 16)
                etendblid = etendblid.Substring(0, 16);
            xml.WriteAttributeString("id", etendblid);
            // xml.WriteAttributeString("revision", "4");
            xml.WriteAttributeString("type", "text");
            xml.WriteAttributeString("typeVersion", "1.5");
            xml.WriteStartElement("identification");
            xml.WriteElementString("name", projectOptions.shortTitle);
            xml.WriteElementString("nameLocal", projectOptions.vernacularTitle);
            xml.WriteElementString("abbreviation", projectOptions.translationId);
            string abbreviationLocal = projectOptions.translationTraditionalAbbreviation;
            if (abbreviationLocal.Length < 2)
            {
                abbreviationLocal = projectOptions.translationId.ToUpperInvariant();
            }
            xml.WriteElementString("abbreviationLocal", abbreviationLocal);
            string scope = "Portion only";
            if (projectOptions.ntBookCount == 27)
            {
                if (projectOptions.otBookCount == 39)
                {
                    if (projectOptions.adBookCount > 0)
                    {
                        scope = "Bible with Deuterocanon";
                    }
                    else
                    {
                        scope = "Bible without Deuterocanon";
                    }
                }
                else if (projectOptions.otBookCount > 0)
                {
                    if ((projectOptions.otBookCount == 1) && (projectOptions.otChapCount == 150))
                        scope = "New Testament and Psalms";
                    else
                        scope = "New Testament and Shorter Old Testament";
                }
                else
                {
                    scope = "NT";   // "New Testament only" is also allowed here.
                }
            }
            else if (projectOptions.otBookCount == 39)
            {
                if (projectOptions.ntBookCount == 0)
                {
                    if (projectOptions.adBookCount > 0)
                        scope = "Old Testament with Deuterocanon";
                    else
                        scope = "Old Testament only";
                }
            }
            xml.WriteElementString("scope", scope);
            xml.WriteElementString("description", projectOptions.EnglishDescription);
            string yearCompleted = projectOptions.copyrightYears.Trim();
            if (yearCompleted.Length > 4)
                yearCompleted = yearCompleted.Substring(yearCompleted.Length - 4);
            xml.WriteElementString("dateCompleted", yearCompleted);
            xml.WriteStartElement("systemId");
            xml.WriteAttributeString("fullname", projectOptions.shortTitle);
            xml.WriteAttributeString("name", projectOptions.paratextProject);
            xml.WriteAttributeString("type", "paratext");

            xml.WriteEndElement();
            xml.WriteElementString("bundleProducer", "");
            xml.WriteEndElement();  // identification
            xml.WriteElementString("confidential", "false");
            /*
            xml.WriteStartElement("agencies");
            string etenPartner = "WBT";
            if ((globe.globe.projectOptions.publicDomain == true) || globe.globe.projectOptions.copyrightOwner.ToUpperInvariant().Contains("EBIBLE"))
                etenPartner = "eBible.org";
            else if (globe.globe.projectOptions.copyrightOwner.ToUpperInvariant().Contains("SOCIETY"))
                etenPartner = "UBS";
            else if (globe.globe.projectOptions.copyrightOwner.ToUpperInvariant().Contains("BIBLICA"))
                etenPartner = "Biblica";
            else if (globe.globe.projectOptions.copyrightOwnerAbbrev.ToUpperInvariant().Contains("PBT"))
                etenPartner = "PBT";
            else if (globe.globe.projectOptions.copyrightOwnerAbbrev.ToUpperInvariant().Contains("SIM"))
                etenPartner = "SIM";
            xml.WriteElementString("etenPartner", etenPartner);
            xml.WriteElementString("creator", globe.globe.projectOptions.contentCreator);
            xml.WriteElementString("publisher", globe.globe.projectOptions.electronicPublisher);
            xml.WriteElementString("contributor", globe.globe.projectOptions.contributor);
            xml.WriteEndElement();  // agencies
            */
            xml.WriteStartElement("language");
            xml.WriteElementString("iso", projectOptions.languageId);
            xml.WriteElementString("name", projectOptions.languageNameInEnglish);
            xml.WriteElementString("nameLocal", projectOptions.languageName);
            xml.WriteElementString("ldml", projectOptions.ldml);
            xml.WriteElementString("rod", projectOptions.rodCode);
            xml.WriteElementString("script", projectOptions.script);
            xml.WriteElementString("scriptDirection", projectOptions.textDir.ToUpperInvariant());
            string numerals = projectOptions.numberSystem;
            if (numerals == "Arabic")
            {
                numerals = "Hindi";
            }
            else if ((numerals == "Default") || (numerals == "Hindu-Arabic"))
            {
                numerals = "Arabic";
            }
            xml.WriteElementString("numerals", numerals);
            xml.WriteEndElement();  // language
            xml.WriteStartElement("country");
            xml.WriteElementString("iso", projectOptions.countryCode);
            xml.WriteElementString("name", projectOptions.country);
            xml.WriteEndElement();  // country
            xml.WriteStartElement("type");
            if (projectOptions.copyrightYears.Length > 4)
                xml.WriteElementString("translationType", "Revision");
            else
                xml.WriteElementString("translationType", "New");
            xml.WriteElementString("audience", "Common");
            xml.WriteEndElement();  // type
            xml.WriteStartElement("bookNames");
            foreach (WordSend.BibleBookRecord br in WordSend.usfxToHtmlConverter.bookList)
            {
                if (!String.IsNullOrEmpty(br.vernacularAbbreviation))
                {
                    xml.WriteStartElement("book");
                    xml.WriteAttributeString("code", br.tla);
                    xml.WriteElementString("long", br.vernacularLongName);
                    xml.WriteElementString("short", br.vernacularShortName);
                    xml.WriteElementString("abbr", br.vernacularAbbreviation);
                    xml.WriteEndElement();  // book
                }
            }
            xml.WriteEndElement();  // bookNames
            xml.WriteStartElement("contents");
            xml.WriteStartElement("bookList");
            xml.WriteAttributeString("default", "true");
            xml.WriteAttributeString("id", "1");
            xml.WriteElementString("name", projectOptions.shortTitle);
            xml.WriteElementString("nameLocal", projectOptions.vernacularTitle);
            xml.WriteElementString("abbreviation", projectOptions.translationId);
            xml.WriteElementString("abbreviationLocal", abbreviationLocal);
            xml.WriteElementString("description", projectOptions.canonTypeEnglish);    // Book list description, like common, Protestant, or Catholic
            xml.WriteElementString("descriptionLocal", projectOptions.canonTypeLocal);
            xml.WriteStartElement("books");
            foreach (WordSend.BibleBookRecord br in WordSend.usfxToHtmlConverter.bookList)
            {
                if (!String.IsNullOrEmpty(br.vernacularAbbreviation))
                {
                    xml.WriteStartElement("book");
                    xml.WriteAttributeString("code", br.tla);
                    xml.WriteEndElement();  // book
                }
            }
            xml.WriteEndElement();  // books
            xml.WriteEndElement();  // bookList
            xml.WriteEndElement();  // contents
            /*
            xml.WriteStartElement("progress");
            foreach (WordSend.BibleBookRecord br in WordSend.usfxToHtmlConverter.bookList)
            {
                if (!String.IsNullOrEmpty(br.vernacularAbbreviation))
                {
                    xml.WriteStartElement("book");
                    xml.WriteAttributeString("code", br.tla);
                    xml.WriteAttributeString("stage", "4");
                    xml.WriteEndElement();  // book
                }
            }
            xml.WriteEndElement();  // progress
            */
            if (!projectOptions.publicDomain)
            {
                xml.WriteStartElement("contact");
                xml.WriteElementString("rightsHolder", projectOptions.copyrightOwner);
                string localRights = projectOptions.localRightsHolder.Trim();
                if (localRights.Length == 0)
                    localRights = projectOptions.copyrightOwner;
                xml.WriteElementString("rightsHolderLocal", localRights);
                string rightsHolderAbbreviation = projectOptions.copyrightOwnerAbbrev.Trim();
                if (rightsHolderAbbreviation.Length < 1)
                {
                    string s = projectOptions.copyrightOwner.Trim().ToUpperInvariant().Replace(" OF ", " ");
                    if (s.StartsWith("THE "))
                        s = s.Substring(4);
                    if (s.EndsWith("."))
                        s = s.Substring(0, s.Length - 1);
                    if (s.EndsWith("INC"))
                        s = s.Substring(0, s.Length - 4);
                    bool afterSpace = true;
                    for (i = 0; i < s.Length; i++)
                    {
                        if (s[i] == ' ')
                        {
                            afterSpace = true;
                        }
                        else
                        {
                            if (afterSpace && Char.IsLetter(s[i]))
                            {
                                rightsHolderAbbreviation = rightsHolderAbbreviation + s[i];
                                afterSpace = false;
                            }
                        }
                    }
                }
                xml.WriteElementString("rightsHolderAbbreviation", rightsHolderAbbreviation);
                string ownerUrl = projectOptions.copyrightOwnerUrl;
                if (ownerUrl.ToUpperInvariant().StartsWith("HTTPS://"))
                    ownerUrl = ownerUrl.Substring(8);
                if (ownerUrl.ToUpperInvariant().StartsWith("HTTP://"))
                    ownerUrl = ownerUrl.Substring(7);
                xml.WriteElementString("rightsHolderURL", ownerUrl);
                xml.WriteElementString("rightsHolderFacebook", projectOptions.facebook);
                xml.WriteEndElement();  // contact
            }
            xml.WriteStartElement("copyright");
            xml.WriteStartElement("statement");
            xml.WriteAttributeString("contentType", "xhtml");
            xml.WriteElementString("p", copyright);
            xml.WriteEndElement();  // statement
            xml.WriteEndElement();  // copyright
            xml.WriteStartElement("promotion");
            xml.WriteStartElement("promoVersionInfo");
            xml.WriteAttributeString("contentType", "xhtml");
            if (projectOptions.promoHtml.Trim().Length > 3)
                xml.WriteString(projectOptions.promoHtml);
            else
                xml.WriteElementString("p", rights);
            xml.WriteEndElement();  // promoVersionInfo
            xml.WriteStartElement("promoEmail");
            xml.WriteAttributeString("contentType", "xhtml");
            xml.WriteString(@"Thank you for downloading ");
            xml.WriteString(projectOptions.vernacularTitle);
            xml.WriteString(@"! Now you'll have anytime, anywhere access to God's Word on your mobile device—even if 
you're outside of service coverage or not connected to the Internet. It also means faster service whenever you read that version since it's 
stored on your device. Enjoy! This download was made possible by ");
            xml.WriteString(projectOptions.copyrightOwner.Trim(new char[] { ' ', '.' }));
            xml.WriteString(@". We really appreciate their passion for making the Bible available to millions of people around the world. Because of 
their generosity, people like you can open up the Bible and hear from God no matter where you are. You can learn more about them at ");
            xml.WriteString(projectOptions.copyrightOwnerUrl);
            xml.WriteString(@".");
            xml.WriteEndElement();  // promoEmail
            xml.WriteEndElement();  // promotion
            xml.WriteStartElement("archiveStatus");
            xml.WriteElementString("archivistName", "");
            xml.WriteElementString("dateArchived", "");
            xml.WriteElementString("dateUpdated", "");
            xml.WriteElementString("comments", "");
            xml.WriteEndElement();  // archiveStatus
            xml.WriteElementString("format", "text/xml");
            xml.WriteEndElement();  // DBLMetadata
            xml.Close();


            if (projectOptions.UseFrames)
            {
                // Look for Introduction files in the output. (They were copied htmlextras earlier.)
                // Do this before making the chapter index, since we tell it to look for them there.
                foreach (var path in Directory.GetFiles(htmlPath, "*" + UsfxToChapterIndex.IntroductionSuffix))
                {
                    string destFileName = Path.Combine(htmlPath, Path.GetFileName(path));
                    toHtm.MakeFramesFor(destFileName);
                }
                // Generate the ChapterIndex file
                var ciMaker = new UsfxToChapterIndex();
                ciMaker.IntroductionDirectory = htmlPath;
                ciMaker.IntroductionLinkText = projectOptions.IntroductionLinkText;
                ciMaker.ConcordanceLinkText = projectOptions.ConcordanceLinkText;
                string chapIndexPath = Path.Combine(htmlPath, UsfxToChapterIndex.ChapIndexFileName);
                ciMaker.Generate(usfxFilePath, chapIndexPath);
                EnsureTemplateFile("chapIndex.css", htmlPath);
                EnsureTemplateFile("frameFuncs.js", htmlPath);
                EnsureTemplateFile("Navigation.js", htmlPath);
            }

            // Todo JohnT: move this to a new method, and the condition to the method that calls this.
            if (projectOptions.GenerateConcordance || projectOptions.UseFrames)
            {
                currentConversion = "Concordance";
                string concordanceDirectory = Path.Combine(htmlPath, "conc");
                Logit.WriteLine("Deleting " + concordanceDirectory);
                Utils.DeleteDirectory(concordanceDirectory); // Blow away any previous results
                Logit.WriteLine("Creating " + concordanceDirectory);
                Utils.EnsureDirectory(concordanceDirectory);
                string excludedClasses =
                    "toc toc1 toc2 navButtons pageFooter chapterlabel r verse"; // from old prophero: "verse chapter notemark crmark crossRefNote parallel parallelSub noteBackRef popup crpopup overlap";
                string headingClasses = "mt mt2 s"; // old prophero: "sectionheading maintitle2 footnote sectionsubheading";
                var concGenerator = new ConcGenerator(inputProjectDirectory, concordanceDirectory)
                {
                    // Currently configurable options
                    MergeCase = projectOptions.MergeCase,
                    MaxContextLength = projectOptions.MaxContextLength,
                    MinContextLength = projectOptions.MinContextLength,
                    WordformingChars = projectOptions.WordformingChars,
                    MaxFrequency = projectOptions.MaxFrequency,
                    Phrases = projectOptions.Phrases,
                    ExcludeWords = new HashSet<string>(projectOptions.ExcludeWords.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)),
                    ReferenceAbbeviationsMap = projectOptions.ReferenceAbbeviationsMap,
                    BookChapText = projectOptions.BooksAndChaptersLinkText,
                    ConcordanceLinkText = projectOptions.ConcordanceLinkText,

                    // Options we may want to make configurable for localization.
                    // Todo: configure comparison function
                    IndexType = ConcGenerator.IndexTypes.alphaTreeMf,
                    NotesRef = "note",
                    HeadingRef = "head",


                    // Options we need to configure correctly based on the HTML we generate
                    ExcludeClasses = new HashSet<string>(excludedClasses.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)),
                    NotesClass = "footnotes", // todo: fix if Haiola generates HTML with footnotes that should be concorded
                    NonCanonicalClasses = new HashSet<string>(headingClasses.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
                };
                // concGenerator.Run(new List<string>(Directory.GetFiles(xhtmlPath)));
                Logit.WriteLine("Generating concordance.");

                concGenerator.Run(Path.Combine(Path.Combine(outputProjectDirectory, "search"), "verseText.xml"));

                var concFrameGenerator = new ConcFrameGenerator()
                { ConcDirectory = concordanceDirectory, LangName = projectOptions.vernacularTitle };
                concFrameGenerator.customCssName = projectOptions.customCssFileName;
                concFrameGenerator.Run();
                EnsureTemplateFile("mktree.css", concordanceDirectory);
                EnsureTemplateFile("plus.gif", concordanceDirectory);
                EnsureTemplateFile("minus.gif", concordanceDirectory);
                EnsureTemplateFile("display.css", concordanceDirectory);
                EnsureTemplateFile("TextFuncs.js", htmlPath);
            }
        }


        public void ConvertUsfxToEPub()
        {
            if ((projectOptions.languageId.Length < 3) || (projectOptions.translationId.Length < 3))
                return;
            currentConversion = "writing ePub";
            string UsfxPath = Path.Combine(outputProjectDirectory, "usfx");
            string epubPath = Path.Combine(outputProjectDirectory, "epub");
            string htmlPath = Path.Combine(epubPath, "OEBPS");
            if (!Directory.Exists(UsfxPath))
            {
                Logit.WriteError(UsfxPath + " not found!");
                return;
            }
            Utils.EnsureDirectory(outputDirectory);
            Utils.EnsureDirectory(outputProjectDirectory);
            Utils.EnsureDirectory(epubPath);
            Utils.EnsureDirectory(htmlPath);
            string epubCss = Path.Combine(htmlPath, "epub.css");
            string logFile = Path.Combine(outputProjectDirectory, "epubConversionReport.txt");
            Logit.OpenFile(logFile);

            string fontSource = Path.Combine(dataRootDir, "fonts");
            string fontName = projectOptions.fontFamily.ToLower().Replace(' ', '_');
            // Copy cascading style sheet from project directory, or if not there, create a font specification section and append it to BibleConv/input/epub.
            string specialCss = Path.Combine(inputProjectDirectory, "epub.css");
            if (File.Exists(specialCss))
            {
                File.Copy(specialCss, epubCss);
            }
            else
            {
                StreamReader sr;
                specialCss = Path.Combine(inputDirectory, "epub.css");
                if (File.Exists(specialCss))
                    sr = new StreamReader(specialCss);
                else
                    sr = new StreamReader(SFConverter.FindAuxFile("epub.css"));
                string epubStyleSheet = sr.ReadToEnd();
                sr.Close();
                StreamWriter sw = new StreamWriter(epubCss);
                sw.WriteLine("@font-face {{font-family:'{0}';src: url('{0}.ttf') format('truetype');src: url('{0}.woff') format('woff');font-weight:normal;font-style:normal}}",
                    fontName);
                sw.WriteLine("html,body,div.main,div.footnote,div,ol.nav,h1,ul.tnav {{font-family:'{0}','{1}','Liberation Sans','liberationsans_regular','sans-serif'}}", fontName, projectOptions.fontFamily);
                if (projectOptions.commonChars)
                {
                    sw.WriteLine(".chapterlabel,.mt,.tnav,h1.title,a.xx,a.oo,a.nn {{'Liberation Sans','liberationsans_regular','sans-serif'}}");
                }
                sw.WriteLine("* {margin:0;padding:0}");
                sw.WriteLine("html,body	{0}height:100%;font-size:1.0em;line-height:{1}em{2}", "{", (projectOptions.script.ToLowerInvariant() == "latin") ? "1.2" : "2.5", "}");
                sw.Write(epubStyleSheet);
                sw.Close();
            }
            fileHelper.DebugWrite("copying files");
            fileHelper.CopyFile(Path.Combine(fontSource, fontName + ".woff"), Path.Combine(htmlPath, fontName + ".woff"));
            fileHelper.CopyFile(Path.Combine(fontSource, fontName + ".ttf"), Path.Combine(htmlPath, fontName + ".ttf"));
            fileHelper.CopyFile(Path.Combine(fontSource, "liberationsans_regular.woff"), Path.Combine(htmlPath, "liberationsans_regular.woff"));
            fileHelper.CopyFile(Path.Combine(fontSource, "liberationsans_regular.ttf"), Path.Combine(htmlPath, "liberationsans_regular.ttf"));
            fileHelper.DebugWrite("fonts copied");

            ePubWriter toEpub = new ePubWriter();
            toEpub.projectOptions = projectOptions;
            toEpub.projectOutputDir = outputProjectDirectory;
            toEpub.epubDirectory = epubPath;
            toEpub.redistributable = projectOptions.redistributable;
            toEpub.epubIdentifier = GetEpubID();
            toEpub.stripPictures = false;
            toEpub.indexDate = DateTime.UtcNow;
            fileHelper.DebugWrite("Checking eBible.org unique status");

            if (projectOptions.eBibledotorgunique && !projectOptions.privateProject)
            {
                toEpub.indexDateStamp = "ePub generated by <a href='https://eBible.org'>eBible.org</a> using <a href='https://haiola.org'>Haiola</a> on " + toEpub.indexDate.ToString("d MMM yyyy") +
                        " from source files dated " + sourceDate.ToString("d MMM yyyy") +
                        "<br/>";
            }
            else
            {
                toEpub.indexDateStamp = "ePub generated by <a href='https://haiola.org'>Haiola</a> " + toEpub.indexDate.ToString("d MMM yyyy") +
                    " from source files dated " + sourceDate.ToString("d MMM yyyy");
            }
            fileHelper.DebugWrite("Index date stamp created.");

            toEpub.GeneratingConcordance = projectOptions.GenerateConcordance || projectOptions.UseFrames;
            toEpub.CrossRefToFilePrefixMap = projectOptions.CrossRefToFilePrefixMap;
            toEpub.contentCreator = projectOptions.contentCreator;
            toEpub.contributor = projectOptions.contributor;
            fileHelper.DebugWrite("UsfxPath = "+UsfxPath);

            string usfxFilePath = Path.Combine(UsfxPath, "usfx.xml");
            toEpub.bookInfo.ReadPublicationOrder(orderFile);
            fileHelper.DebugWrite("Merge path assignment");

            toEpub.MergeXref(Path.Combine(inputProjectDirectory, "xref.xml"));
            //TODO: eliminate side effects in expandPercentEscapes
            // Side effect: expandPercentEscapes sets longCopyrightMessage and shortCopyrightMessage.
            toEpub.sourceLink = expandPercentEscapes("<a href=\"https://%h/%t\">%v</a>");
            fileHelper.DebugWrite("Escapes expanded.");

            toEpub.longCopr = longCopyrightMessage;
            toEpub.shortCopr = shortCopyrightMessage;
            toEpub.textDirection = projectOptions.textDir;
            toEpub.customCssName = "epub.css";
            toEpub.stripManualNoteOrigins = projectOptions.stripNoteOrigin;
            toEpub.noteOriginFormat = projectOptions.xoFormat;
            toEpub.englishDescription = projectOptions.EnglishDescription;
            toEpub.preferredFont = projectOptions.fontFamily;
            toEpub.fcbhId = projectOptions.fcbhId;
            toEpub.coverName = Path.GetFileName(preferredCover);
            string coverPath = Path.Combine(htmlPath, toEpub.coverName);
            fileHelper.DebugWrite("copying cover");
            File.Copy(preferredCover, coverPath);
            if (projectOptions.PrepublicationChecks &&
                (projectOptions.publicDomain || projectOptions.redistributable || File.Exists(Path.Combine(inputProjectDirectory, "certify.txt"))) &&
                File.Exists(certified))
            {
                File.Copy(certified, Path.Combine(htmlPath, "eBible.org_certified.jpg"));
                toEpub.indexDateStamp = toEpub.indexDateStamp + "<br /><a href='https://eBible.org/certified/' target='_blank'><img src='eBible.org_certified.jpg' alt='eBible.org certified' /></a>";
            }
            toEpub.xrefCall.SetMarkers(projectOptions.xrefCallers);
            toEpub.footNoteCall.SetMarkers(projectOptions.footNoteCallers);
            toEpub.projectInputDir = inputProjectDirectory;
            fileHelper.DebugWrite("Writing epub to "+htmlPath);
            toEpub.ConvertUsfxToHtml(usfxFilePath, htmlPath,
                projectOptions.vernacularTitle,
                projectOptions.languageId,
                projectOptions.translationId,
                projectOptions.chapterLabel,
                projectOptions.psalmLabel,
                "<a class='xx' href='copyright.xhtml'>" + usfxToHtmlConverter.EscapeHtml(shortCopyrightMessage) + "</a>",
                expandPercentEscapes(projectOptions.homeLink),
                expandPercentEscapes(projectOptions.footerHtml),
                expandPercentEscapes(projectOptions.indexHtml),
                copyrightPermissionsStatement(),
                projectOptions.ignoreExtras,
                projectOptions.goText);
            toEpub.bookInfo.RecordStats(projectOptions);
            projectOptions.commonChars = toEpub.commonChars;
            projectOptions.Write();
            Logit.CloseFile();
            if (Logit.loggedError)
            {
                projectOptions.lastRunResult = false;
            }
            if (Logit.loggedWarning)
            {
                projectOptions.warningsFound = true;
            }
        }


        public bool GetSource()
        {
            DateTime fileDate;
            bool result = false;
            string source = projectOptions.customSourcePath;
            if (String.IsNullOrEmpty(source))
                return result;
            string sourceKind = GetSourceKind(source, !XMLini.readOnly);
            fileDate = File.GetLastWriteTimeUtc(source);
            if (fileDate > projectOptions.SourceFileDate)
            {
                projectOptions.SourceFileDate = fileDate;
            }
            sourceDate = projectOptions.SourceFileDate;

            if ((projectOptions.currentFingerprint == projectOptions.builtFingerprint) && !rebuild)
            {
                Logit.WriteLine("Skipping up-to-date project " + source);
                return true;
            }

            Utils.DeleteDirectory(Path.Combine(outputProjectDirectory, "usfx"));
            Utils.DeleteDirectory(Path.Combine(outputProjectDirectory, "usfm1"));
            Utils.DeleteDirectory(Path.Combine(outputProjectDirectory, "sfm"));
            Utils.DeleteDirectory(Path.Combine(outputProjectDirectory, "extendedusfm"));
            Utils.DeleteDirectory(Path.Combine(outputProjectDirectory, "usfm"));

            switch (sourceKind)
            {
                case "usfm":
                    PreprocessUsfmFiles(source);
                    if (fileHelper.fAllRunning)
                    {
                        ConvertUsfmToUsfx();
                        NormalizeUsfm();
                    }
                    result = true;
                    break;
                case "usfx":
                    ImportUsfx(source);
                    NormalizeUsfm();
                    result = true;
                    break;
                case "usx":
                    ImportUsx(source);
                    string metadataXml = Path.Combine(source, "metadata.xml");
                    if (File.Exists(metadataXml))
                    {
                        fileDate = File.GetLastWriteTimeUtc(metadataXml);
                        if (fileDate > sourceDate)
                        {
                            sourceDate = fileDate;
                        }
                        projectOptions.SourceFileDate = sourceDate;
                    }
                    result = true;
                    break;
                case "osis":
                    Logit.WriteError("OSIS IMPORT NOT IMPLEMENTED.");
                    result = false;
                    break;
            }
            return result;
        }


    }
}
