using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.IO;
using System.Xml;
using BibleFileLib;
using WordSend;
using System.Drawing.Text;
using System.Net;

namespace haiola
{
    public partial class haiolaForm : Form
    {
        public static haiolaForm MasterInstance;
        private const string kFootnotesProcess = "footnotes.process"; // identifier for the (so far only one) file transformation process built into Haiola
        public XMLini xini;    // Main program XML initialization file
        private string m_currentTemplate;   // Current template project
        public string dataRootDir; // Default is BibleConv in the user's Documents folder
        public string m_inputDirectory; // Always under dataRootDir, defaults to Documents/BibleConv/input
        public string m_outputDirectory; // curently Site, always under dataRootDir
        public string m_inputProjectDirectory; //e.g., full path to BibleConv\input\Kupang
        public string m_outputProjectDirectory; // e.g., full path to BibleConv\output\Kupang
        public string m_swordSuffix
        {
            get { return xini.ReadString("swordSuffix", String.Empty); }
            set { xini.WriteString("swordSuffix", value); }
        }
        public string m_project = String.Empty; // e.g., Kupang
        public string currentConversion;   // e.g., "Preprocessing" or "Portable HTML"
        public string m_xiniPath;  // e.g., BibleConv\input\Kupang\options.xini
        public XMLini projectXini;
        public Options m_options;
        public BibleBookInfo bkInfo;
        public DateTime sourceDate = new DateTime(1611, 1, 1);
        public bool getFCBHkeys;
        public PluginManager plugin;

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

        public string paratextProjectsDir
        {
            get { return xini.ReadString("paratextProjectsDir", GuessParatextProjectsDir()); }
            set { xini.WriteString("paratextProjectsDir", value.Trim()); }
        }


        public haiolaForm()
        {
            InitializeComponent();
            MasterInstance = this;
            plugin = new PluginManager();
            batchLabel.Text = String.Format("Haiola version {0}.{1} © 2003-{2} SIL, EBT, && eBible.org. Released under Gnu LGPL 3 or later.",
                Version.date, Version.time, Version.year);
            extensionLabel.Text = plugin.PluginMessage();
            if (plugin.PluginLoaded())
            {
                makeInScriptCheckBox.Visible = makeInScriptCheckBox.Enabled = true;
            }
            if (Program.autorun)
                this.WindowState = System.Windows.Forms.FormWindowState.Minimized;
        }

        /// <summary>
        /// Prompt the user for the Haiola project directory to use.
        /// </summary>
        /// <returns>true iff a selection was made</returns>
        public bool GetRootDirectory()
        {
            FolderBrowserDialog dlg = new FolderBrowserDialog();
            dlg.SelectedPath = dataRootDir;
            dlg.Description =
                @"Please select a folder to contain your working directories.";
            dlg.ShowNewFolderButton = true;
            if (dlg.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return false;
            dataRootDir = dlg.SelectedPath;
            xini.WriteString("dataRootDir", dataRootDir);
            xini.Write();
            return true;
        }


        /// <summary>
        /// Select the root data directory for Haiola projects (usually Documents/BibleConv)
        /// </summary>
        /// <param name="sender">Form sending event</param>
        /// <param name="e">Event parameters</param>
        private void btnSetRootDirectory_Click(object sender, EventArgs e)
        {
            SetupForm setup = new SetupForm();
            setup.ShowDialog();
        }

        private Hashtable fcbhDbsIds;

        private void readFcbhIds()
        {
            string fcbhIdFileName = Path.Combine(m_inputDirectory, "fcbhids.csv");
            string line;
            string [] fields;
            if (File.Exists(fcbhIdFileName))
            {
                try
                {
                    fcbhDbsIds = new Hashtable();
                    StreamReader sr = new StreamReader(fcbhIdFileName);
                    while (null != (line = sr.ReadLine()))
                    {
                        fields = line.Split(new Char[] { ',' });
                        if ((fields.Length > 3) && (!String.IsNullOrEmpty(fields[2])) && (!String.IsNullOrEmpty(fields[1])))
                        {
                            fcbhDbsIds[fields[2]] = fields[1];  // fields[2] == haiola ID/original DBS ID; fields[1] = FCBH ID
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error reading fcbhids.csv");
                }
            }
        }

        public LanguageCodeInfo languageCodes;

        private string dbsLogo, pngbtaLogo;

        private void haiolaForm_Load(object sender, EventArgs e)
        {
            int i;
            xini = new XMLini(Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "haiola"),
    "haiola.xini"));
            dataRootDir = xini.ReadString("dataRootDir", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BibleConv"));
            xini.WriteString("dataRootDir", dataRootDir);
            xini.Write();
            m_inputDirectory = Path.Combine(dataRootDir, "input");
            if (!Directory.Exists(m_inputDirectory))
                if (!GetRootDirectory())
                    Application.Exit();
            dbsLogo = Path.Combine(m_inputDirectory, "dbs.jpg");
            pngbtaLogo = Path.Combine(m_inputDirectory, "pngbta.jpg");
            readFcbhIds();
            eth = new Ethnologue();
            languageCodes = new LanguageCodeInfo();
            LoadWorkingDirectory(false, false, false);
            getFCBHkeys = xini.ReadBool("downloadFcbhAudio", false);
            Application.DoEvents();
            triggerautorun = Program.autorun;
            if (triggerautorun)
            {
                startTime = DateTime.UtcNow;
                timer1.Enabled = true;
            }
            InstalledFontCollection availableFonts = new InstalledFontCollection();;
            FontFamily[] availableFontFamilies = availableFonts.Families;
            ArrayList fontNames = new ArrayList(availableFontFamilies.Length);
            for (i = 0; i < availableFontFamilies.Length; i++)
            {
                fontNames.Add(availableFontFamilies[i].Name);
            }
            fontNames.Sort();
            for (i = 0; i < fontNames.Count; i++)
            {
                fontComboBox.Items.Add(fontNames[i]);
            }
        }

        /// <summary>
        /// Ensure that a named template file exists in the input directory.
        /// </summary>
        /// <param name="fileName">Name of file to find.</param>
        public void EnsureTemplateFile(string fileName)
        {
        	EnsureTemplateFile(fileName, m_inputDirectory);
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
                MessageBox.Show(ex.Message, "Error ensuring " + fileName + " is in " + m_inputDirectory);
            }
        }

        private bool loadingDirectory = false;
        private int projSelected = 0;
        private int projReady = 0;
        private int projCount = 0;

        /// <summary>
        /// Load the working directory and set the requested projects as selected to run.
        /// </summary>
        /// <param name="all">Set all ready projects.</param>
        /// <param name="failed">Set projects that are ready but failed last run.</param>
        /// <param name="none">Clear all selection checkboxes.</param>
        /// <param name="startIndex">Starting index for select all (default = 0)</param>
        /// <param name="increment">Increment for select all subset (default = 1)</param>
        public void LoadWorkingDirectory(bool all, bool failed = false, bool none = false, int startIndex = 0, int increment = 1)
        {
            loadingDirectory = true;
            bool isReady = false;
            projCount = 0;
            projReady = 0;
            int thisIndex = 0;
            int nextIndex = startIndex;
            projSelected = 0;
            readssf Ssf = new readssf();
            SaveOptions();
            LoadParatextProjectList();
            m_projectsList.BeginUpdate();
            m_projectsList.Items.Clear();
            m_projectsList.Sorted = false;
            m_inputDirectory = Path.Combine(dataRootDir, "input");
            m_outputDirectory = Path.Combine(dataRootDir, "output");
            fileHelper.EnsureDirectory(dataRootDir);
            fileHelper.EnsureDirectory(m_inputDirectory);
            fileHelper.EnsureDirectory(m_outputDirectory);

            EnsureTemplateFile("haiola.css");
            EnsureTemplateFile("prophero.css");
            EnsureTemplateFile("fixquotes.re");

            foreach (string path in Directory.GetDirectories(m_inputDirectory))
            {
                string project = Path.GetFileName(path);
                m_projectsList.Items.Add(project);
                projCount++;
                m_project = project;
                m_inputProjectDirectory = Path.Combine(m_inputDirectory, m_project);
                m_outputProjectDirectory = Path.Combine(m_outputDirectory, m_project);
                m_xiniPath = Path.Combine(m_inputProjectDirectory, "options.xini");
                if (m_options == null)
                {
                    m_options = new Options(m_xiniPath);
                }
                else
                {
                    m_options.Reload(m_xiniPath);
                }

                bool gotParatextProject = false;
                if (!String.IsNullOrEmpty(paratextProjectsDir))
                {
                    if (!String.IsNullOrEmpty(m_options.paratextProject))
                    {
                        string ParatextProjectDir = Path.Combine(paratextProjectsDir, m_options.paratextProject);
                        if (Directory.Exists(ParatextProjectDir))
                        {
                            gotParatextProject = true;
                            Ssf.ReadParatextSsf(m_options, ParatextProjectDir + ".ssf");
                        }
                    }
                }
                if ((!String.IsNullOrEmpty(m_options.languageId)) && 
                    (gotParatextProject ||
                    Directory.Exists(Path.Combine(path, "Source")) ||
                    Directory.Exists(Path.Combine(path, "usfx")) ||
                    Directory.Exists(Path.Combine(path, "usx"))))
                {
                    isReady = true;
                    projReady++;
                }
                else
                {
                    isReady = false;
                }
                if (none)
                {
                    m_options.selected = false;
                }
                else if (all)
                {
                    if (thisIndex == nextIndex)
                    {
                        m_options.selected = isReady;
                        nextIndex += increment;
                    }
                    else
                    {
                        m_options.selected = false;
                    }
                    thisIndex++;
                }
                else if (failed)
                {
                    m_options.selected = isReady && !m_options.lastRunResult;
                }
                else
                {
                    m_options.selected = isReady && m_options.selected;
                }
                m_projectsList.SetItemChecked(m_projectsList.Items.Count - 1, m_options.selected);
                m_options.Write();
                if (m_options.selected)
                    projSelected++;
            }
            dependsComboBox.BeginUpdate();
            dependsComboBox.Items.Clear();
            dependsComboBox.Items.Add(String.Empty);
            foreach (object o in m_projectsList.Items)
            {
                dependsComboBox.Items.Add(o);
            }
            dependsComboBox.EndUpdate();
            m_projectsList.Sorted = true;
            m_projectsList.EndUpdate();
            statsLabel.Text = projReady.ToString() + " of " + projCount.ToString() + " project directories are ready to run; " + projSelected.ToString() + " selected.";
            if (m_projectsList.Items.Count != 0)
            {
                m_projectsList.SetSelected(0, true);
                WorkOnAllButton.Enabled = true;
            }
            else
            {
                MessageBox.Show(this, "No projects found in " + m_inputDirectory
                                      +
                                      ". You should create a folder there for your project and place your input files in the appropriate subdirectory. Press the 'Help' button.",
                                "No Projects", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                WorkOnAllButton.Enabled = false;
            }
            loadingDirectory = false;
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
        

        private void PreprocessOneFile(string inputPath, List<string> tablePaths, string outputPath)
		{
            DateTime fileDate;
            fileDate = File.GetLastWriteTimeUtc(inputPath);
            if (fileDate > sourceDate)
            {
                sourceDate = fileDate;
                m_options.SourceFileDate = sourceDate;
            }
			string input;
			// Read file into input
            // Instead of asking the user what the character encoding is, we guess that it is either
            // Windows 1252 or UTF-8, and choose which one of those based on the assumed presence of
            // surrogates in UTF-8, unless there is a byte-order mark.
            Encoding enc = fileHelper.IdentifyFileCharset(inputPath);
            // MessageBox.Show(inputPath + " is encoded as " + enc.ToString());
			StreamReader reader = new StreamReader(inputPath, enc /* Encoding.GetEncoding(m_options.InputEncoding) */);
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
                        MessageBox.Show(this, "Process " + tp + " not known", "Error");
                        return;
                    }
                    continue;
                }
			    string tablePath = Path.Combine(m_inputProjectDirectory, tp);
                if (!File.Exists(tablePath))
                {
                    tablePath = Path.Combine(m_inputDirectory, tp);
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
                            m_options.SourceFileDate = sourceDate;
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
                                string[] parts = source.Split(new char[] {delim});
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
            string[] parts = source.Split(new char[] {delim});
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


        public void PreprocessUsfmFiles(string SourceDir)
        {
            // First, copy BookNames.xml for ready reference. We will update it later.
            string bookNamesCopy = Path.Combine(m_outputProjectDirectory, "BookNames.xml");
            string bookNamesSource = Path.Combine(SourceDir, "BookNames.xml");
            if (File.Exists(bookNamesSource))
                File.Copy(bookNamesSource, bookNamesCopy, true);

            // Now, get on with preprocessing the USFM files.
            Logit.GUIWriteString = showMessageString;
            Logit.OpenFile(Path.Combine(m_outputProjectDirectory, "preprocesslog.txt"));
            // string SourceDir = Path.Combine(m_inputProjectDirectory, "Source");
            /*
            StreamReader sr = new StreamReader(orderFile);
            string allowedBookList = sr.ReadToEnd();
            sr.Close();
            */
            string bookId;
            string UsfmDir = Path.Combine(m_outputProjectDirectory, "extendedusfm");
            if (!Directory.Exists(SourceDir))
            {
                MessageBox.Show(this, SourceDir + " not found!", "ERROR");
                return;
            }
            // Start with an EMPTY USFM directory to avoid problems with old files 
            Utils.DeleteDirectory(UsfmDir);
            fileHelper.EnsureDirectory(UsfmDir);
            string[] inputFileNames = Directory.GetFiles(SourceDir);
            if (inputFileNames.Length == 0)
            {
                MessageBox.Show(this, "No files found in " + SourceDir, "ERROR");
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
                    (fileType != ".VRS") && (fileType != ".INI") && (fileType != ".CSV") &&
                    (fileType != ".CCT") && (!inputFile.EndsWith("~")) &&
                    (lowerName != "autocorrect.txt") &&
                    (lowerName != "tmp.txt") &&
                    (lowerName != "changes.txt") &&
                    (lowerName != "hyphenatedWords.txt") &&
                    (lowerName != "wordboundariesoutput.txt") &&
                    (lowerName != "printdraftchanges.txt"))
                {
                    currentConversion = "preprocessing " + filename;
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
                        if (m_options.allowedBookList.Contains(bookId))
                        {
                            string outputFilePath = Path.Combine(UsfmDir, outputFileName);
                            PreprocessOneFile(inputFile, m_options.preprocessingTables, outputFilePath);
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

        private int loggedLineCount = 0;

        public bool showMessageString(string s)
        {
            loggedLineCount++;
            if (loggedLineCount < 9)
            {
                messagesListBox.Items.Add(s);
                messagesListBox.SelectedIndex = messagesListBox.Items.Count - 1;
            }
            else if (loggedLineCount == 10)
            {
                messagesListBox.Items.Add("MORE ERRORS ARE LISTED IN " + Logit.logFileName);
                messagesListBox.SelectedIndex = messagesListBox.Items.Count - 1;

            }
            if (Logit.loggedError)
            {
                BackColor = messagesListBox.BackColor = Color.LightPink;
                m_options.lastRunResult = false;
            }
            Application.DoEvents();
            return fileHelper.fAllRunning;
        }

        private void logProjectStart(string s)
        {
            loggedLineCount = 0;
            messagesListBox.Items.Add(DateTime.Now.ToString() + " " + s);
            messagesListBox.SelectedIndex = messagesListBox.Items.Count - 1;
            m_options.lastRunResult = true;
        }

        private void ConvertUsfmToUsfx()
        {
            string UsfmDir = Path.Combine(m_outputProjectDirectory, "extendedusfm");
            string UsfxPath = GetUsfxDirectoryPath();
            string usfxName = GetUsfxFilePath();
            if (!Directory.Exists(UsfmDir))
            {
                UsfmDir = Path.Combine(m_outputProjectDirectory, "usfm");
            }
            if (!Directory.Exists(UsfmDir))
            {
                MessageBox.Show(this, UsfmDir + " not found!", "ERROR");
                return;
            }
            // Start with an EMPTY USFX directory to avoid problems with old files
            Utils.DeleteDirectory(UsfxPath);
            fileHelper.EnsureDirectory(UsfxPath);
            currentConversion = "converting from USFM to USFX; reading USFM";
            Application.DoEvents();
            Utils.DeleteDirectory(UsfxPath);
            if ((m_options.languageId.Length < 3) || (m_options.translationId.Length < 3))
            {
                MessageBox.Show(this,
                                string.Format(
                                    "language and translation ids (%0 and %1) must be at least three characters each",
                                    m_options.languageId, m_options.translationId),
                                "ERROR");
                return;
            }
            Utils.EnsureDirectory(UsfxPath);
            string logFile = Path.Combine(m_outputProjectDirectory, "ConversionReports.txt");
            Logit.OpenFile(logFile);
            Logit.UpdateStatus = updateConversionProgress;
            Logit.GUIWriteString = showMessageString;
            SFConverter.scripture = new Scriptures(m_options);
            Logit.loggedError = false;
            // Read a copy of BookNames.xml copied from the source USFM directory, if any.
            SFConverter.scripture.bkInfo.ReadDefaultBookNames(Path.Combine(m_outputProjectDirectory, "BookNames.xml"));
            SFConverter.scripture.assumeAllNested = m_options.relaxUsfmNesting;
            // Read the input USFM files into internal data structures.
            SFConverter.ProcessFilespec(Path.Combine(UsfmDir, "*.usfm"), Encoding.UTF8);
            currentConversion = "converting from USFM to USFX; writing USFX";
            Application.DoEvents();

            // Write out the USFX file.
            SFConverter.scripture.languageCode = m_options.languageId;
            SFConverter.scripture.WriteUSFX(usfxName);
            SFConverter.scripture.bkInfo.ReadUsfxVernacularNames(Path.Combine(Path.Combine(m_outputProjectDirectory, "usfx"), "usfx.xml"));
            string bookNames = Path.Combine(m_outputProjectDirectory, "BookNames.xml");
            SFConverter.scripture.bkInfo.WriteDefaultBookNames(bookNames);
            File.Copy(bookNames, Path.Combine(Path.Combine(m_outputProjectDirectory, "usfx"), "BookNames.xml"), true);
            bool runResult = m_options.lastRunResult;
            bool errorState = Logit.loggedError;
            fileHelper.revisePua(usfxName);
            if (!SFConverter.scripture.hasRefTags)
            {
                m_options.makeHotLinks = true;
                SFConverter.scripture.ReadRefTags(usfxName);
            }
            if (!SFConverter.scripture.ValidateUsfx(usfxName))
            {
                if (m_options.makeHotLinks && File.Exists(Path.ChangeExtension(usfxName, ".norefxml")))
                {
                    File.Move(usfxName, Path.ChangeExtension(usfxName, ".bad"));
                    File.Move(Path.ChangeExtension(usfxName, ".norefxml"), usfxName);
                    Logit.WriteLine("Retrying validation on usfx.xml without expanded references.");
                    if (SFConverter.scripture.ValidateUsfx(usfxName))
                    {
                        Logit.loggedError = errorState;
                        m_options.lastRunResult = runResult;
                        Logit.WriteLine("Validation passed without expanded references.");
                        m_options.makeHotLinks = false;
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
                m_options.lastRunResult = false;
            }
            currentConversion = "converted USFM to USFX.";
            Application.DoEvents();
        }

    	private string GetUsfxFilePath()
    	{
    		return Path.Combine(GetUsfxDirectoryPath(), "usfx.xml");
    	}

    	private string GetUsfxDirectoryPath()
    	{
    		return Path.Combine(m_outputProjectDirectory, "usfx");
    	}


        public string shortCopyrightMessage, longCopyrightMessage, copyrightLink;

        /// <summary>
        /// Sets the shortCopyrightMessage, longCopyrightMessage, and copyrightLink variables based on the
        /// current m_options values.
        /// </summary>
        public void SetCopyrightStrings()
        {
            if (m_options.publicDomain)
            {
                shortCopyrightMessage = longCopyrightMessage = "Public Domain";
                copyrightLink = "<a href='http://en.wikipedia.org/wiki/Public_domain'>Public Domain</a>";
            }
            else if (m_options.silentCopyright)
            {
                longCopyrightMessage = shortCopyrightMessage = copyrightLink = String.Empty;
            }
            else if (m_options.copyrightOwnerAbbrev.Length > 0)
            {
                shortCopyrightMessage = "© " + m_options.copyrightYears + " " + m_options.copyrightOwnerAbbrev;
                longCopyrightMessage = "Copyright © " + m_options.copyrightYears + " " + m_options.copyrightOwner;
                if (m_options.copyrightOwnerUrl.Length > 11)
                    copyrightLink = "copyright © " + m_options.copyrightYears + " <a href=\"" + m_options.copyrightOwnerUrl + "\">" + usfxToHtmlConverter.EscapeHtml(m_options.copyrightOwner) + "</a>";
                else
                    copyrightLink = longCopyrightMessage;
            }
            else
            {
                shortCopyrightMessage = "© " + m_options.copyrightYears + " " + m_options.copyrightOwner;
                longCopyrightMessage = "Copyright " + shortCopyrightMessage;
                if (m_options.copyrightOwnerUrl.Length > 11)
                    copyrightLink = "copyright © " + m_options.copyrightYears + " <a href=\"" + m_options.copyrightOwnerUrl + "\">" + usfxToHtmlConverter.EscapeHtml(m_options.copyrightOwner) + "</a>";
                else
                    copyrightLink = longCopyrightMessage;
            }
            if (m_options.AudioCopyrightNotice.Length > 1)
            {
                longCopyrightMessage = longCopyrightMessage + "; ℗ " + usfxToHtmlConverter.EscapeHtml(m_options.AudioCopyrightNotice);
                copyrightLink = copyrightLink + "<br />℗ " + usfxToHtmlConverter.EscapeHtml(m_options.AudioCopyrightNotice);
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
            if (m_options.privateProject)
                m_options.redistributable = m_options.downloadsAllowed = false;
            if (m_options.redistributable)
                distributionScope = "redistributable";
            else if (m_options.downloadsAllowed)
                distributionScope = "downloadable";
            else
                distributionScope = "restricted";
            s = s.Replace("%d", m_project);
            s = s.Replace("%e", m_options.languageId);
            s = s.Replace("%h", m_options.homeDomain);
            s = s.Replace("%c", shortCopyrightMessage);
            s = s.Replace("%C", copyrightLink);
            s = s.Replace("%l", m_options.languageName);
            s = s.Replace("%L", m_options.languageNameInEnglish);
            s = s.Replace("%D", m_options.dialect);
            s = s.Replace("%a", m_options.contentCreator);
            s = s.Replace("%A", m_options.contributor);
            s = s.Replace("%v", m_options.vernacularTitle);
            s = s.Replace("%f", "<a href=\"" + m_options.facebook + "\">" + m_options.facebook + "</a>");
            s = s.Replace("%F", m_options.fcbhId);
            s = s.Replace("%n", m_options.EnglishDescription);
            s = s.Replace("%N", m_options.lwcDescription);
            s = s.Replace("%p", m_options.privateProject ? "private" : "public");
            s = s.Replace("%r", distributionScope);
            s = s.Replace("%T", m_options.contentUpdateDate.ToString("yyyy-MM-dd"));
            s = s.Replace("%o", m_options.rightsStatement);
            s = s.Replace("%x", m_options.promoHtml);
            s = s.Replace("%w", m_options.printPublisher);
            s = s.Replace("%i", m_options.electronicPublisher);
            s = s.Replace("%P", m_options.AudioCopyrightNotice);
            s = s.Replace("%t", m_options.translationId);
            string result = s.Replace("%%", "%");
            return result;
        }

        public string GetEpubID()
        {
            if (String.IsNullOrEmpty(m_options.epubId))
            {
                string hash = Utils.SHA1HashString(m_options.translationId + "|" + m_options.fcbhId + "|" + DateTime.UtcNow.ToString("dd M yyyy HH:mm:ss.fffffff") + " http://Haiola.org ");
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
                m_options.epubId = uuid.ToString();
                m_options.Write();
            }
            return m_options.epubId;
        }

        public string copyrightPermissionsStatement()
        {
            string fontClass = m_options.fontFamily.ToLower().Replace(' ', '_');
            if (m_options.customPermissions)
                return expandPercentEscapes(m_options.licenseHtml);
            StringBuilder copr = new StringBuilder();
            copr.Append(String.Format("<h1 class='{2}'>{0}</h1>\n<h2>{1}</h2>\n",
                usfxToHtmlConverter.EscapeHtml(m_options.vernacularTitle), 
                usfxToHtmlConverter.EscapeHtml(m_options.EnglishDescription), fontClass));
            if (!String.IsNullOrEmpty(m_options.lwcDescription))
                copr.Append(String.Format("<h2>{0}</h2>\n", usfxToHtmlConverter.EscapeHtml(m_options.lwcDescription)));
            copr.Append(String.Format("<p>{0}<br />\n",copyrightLink));
            if (!String.IsNullOrEmpty(m_options.rightsStatement))
                copr.Append(String.Format("{0}<br />\n", m_options.rightsStatement));
            copr.Append(String.Format("Language: <a href='http://www.ethnologue.org/language/{0}' class='{2}' target='_blank'>{1}",
                m_options.languageId, m_options.languageName, fontClass));
            if (m_options.languageName != m_options.languageNameInEnglish)
                copr.Append(String.Format(" ({0})", usfxToHtmlConverter.EscapeHtml(m_options.languageNameInEnglish)));
            copr.Append("</a><br />\n");
            if (!String.IsNullOrEmpty(m_options.dialect))
                copr.Append(String.Format("Dialect: {0}<br />", usfxToHtmlConverter.EscapeHtml(m_options.dialect)));
            /*
            if (!String.IsNullOrEmpty(m_options.printPublisher))
                copr.Append(String.Format("Primary print publisher: {0}<br />\n", usfxToHtmlConverter.EscapeHtml(m_options.printPublisher)));
            if (!String.IsNullOrEmpty(m_options.electronicPublisher))
                copr.Append(String.Format("Electronic publisher: {0}<br />\n", usfxToHtmlConverter.EscapeHtml(m_options.electronicPublisher)));
            */
            if (!String.IsNullOrEmpty(m_options.contentCreator))
                copr.Append(String.Format("Translation by: {0}<br />\n", usfxToHtmlConverter.EscapeHtml(m_options.contentCreator)));
            if ((!String.IsNullOrEmpty(m_options.contributor)) && (m_options.contentCreator != m_options.contributor))
                copr.Append(String.Format("Contributor: {0}<br />\n", usfxToHtmlConverter.EscapeHtml(m_options.contributor)));
            copr.Append("</p>\n");
            if (!String.IsNullOrEmpty(m_options.promoHtml))
                copr.Append(m_options.promoHtml);
            if (m_options.ccbyndnc)
            {
                copr.Append(@"<p>This translation is made available to you under the terms of the
<a href='http://creativecommons.org/licenses/by-nc-nd/4.0/'>Creative Commons Attribution-Noncommercial-No Derivatives license 4.0.</a></p>
<p>You may share and redistribute this Bible translation or extracts from it in any format, provided that:</p>
<ul>
<li>You include the above copyright information.</li>
<li>You do not sell this work for a profit.</li>
<li>You do not change any of the actual words or punctuation of the Scriptures.</li>
</ul>
<p>Pictures included with Scriptures and other documents on this site are licensed just for use with those Scriptures and documents.
For other uses, please contact the respective copyright owners.</p>
");
            }
            if (m_options.ccbysa)
            {
                copr.Append(@"<p>This translation is made available to you under the terms of the
<a href='http://creativecommons.org/licenses/by-sa/4.0/'>Creative Commons Attribution Share-Alike license 4.0.</a></p>
<p>You have permission to share and redistribute this Bible translation in any format and to make reasonable revisions and adaptations of this translation, provided that:</p>
<ul>
<li>You include the above copyright information.</li>
<li>If you make any changes to the text, you must indicate that you did so in a way that makes it clear that the original licensor is not necessarily endorsing your changes.</li>
<li>If you redistribute this text, you must distribute your contributions under the same license as the original.</li>
</ul>
<p>Pictures included with Scriptures and other documents on this site are licensed just for use with those Scriptures and documents.
For other uses, please contact the respective copyright owners.</p>
<p>Note that in addition to the rules above, revising and adapting God's Word involves a great responsibility to be true to God's Word. See Revelation 22:18-19.</p>
");
            }
            if (m_options.ccbynd)
            {
                copr.Append(@"<p>This translation is made available to you under the terms of the
<a href='http://creativecommons.org/licenses/by-nd/4.0/'>Creative Commons Attribution-No Derivatives license 4.0.</a></p>
<p>You may share and redistribute this Bible translation or extracts from it in any format, provided that:</p>
<ul>
<li>You include the above copyright information.</li>
<li>You do not make any derivative works that change any of the actual words or punctuation of the Scriptures.</li>
</ul>
<p>Pictures included with Scriptures and other documents on this site are licensed just for use with those Scriptures and documents.
For other uses, please contact the respective copyright owners.</p>
");
            }
            copr.Append(String.Format("<p>{0}</p>\n", m_options.contentUpdateDate.ToString("yyyy-MM-dd")));
            return copr.ToString();
        }

        private void ConvertUsfxToEPub()
        {
            if ((m_options.languageId.Length < 3) || (m_options.translationId.Length < 3))
                return;
            currentConversion = "writing ePub";
            string UsfxPath = Path.Combine(m_outputProjectDirectory, "usfx");
            string epubPath = Path.Combine(m_outputProjectDirectory, "epub");
            string htmlPath = Path.Combine(epubPath, "OEBPS");
            if (!Directory.Exists(UsfxPath))
            {
                MessageBox.Show(this, UsfxPath + " not found!", "ERROR");
                return;
            }
            Utils.EnsureDirectory(m_outputDirectory);
            Utils.EnsureDirectory(m_outputProjectDirectory);
            Utils.EnsureDirectory(epubPath);
            Utils.EnsureDirectory(htmlPath);
            string epubCss = Path.Combine(htmlPath, "epub.css");
            string logFile = Path.Combine(m_outputProjectDirectory, "epubConversionReport.txt");
            Logit.OpenFile(logFile);
            Logit.GUIWriteString = showMessageString;
            Logit.UpdateStatus = updateConversionProgress;

            string fontSource = Path.Combine(dataRootDir, "fonts");
            string fontName = m_options.fontFamily.ToLower().Replace(' ', '_');
            // Copy cascading style sheet from project directory, or if not there, create a font specification section and append it to BibleConv/input/epub.
            string specialCss = Path.Combine(m_inputProjectDirectory, "epub.css");
            if (File.Exists(specialCss))
            {
                File.Copy(specialCss, epubCss);
            }
            else
            {
                StreamReader sr;
                specialCss = Path.Combine(m_inputDirectory, "epub.css");
                if (File.Exists(specialCss))
                    sr = new StreamReader(specialCss);
                else
                    sr = new StreamReader(SFConverter.FindAuxFile("epub.css"));
                string epubStyleSheet = sr.ReadToEnd();
                sr.Close();
                StreamWriter sw = new StreamWriter(epubCss);
                sw.WriteLine("@font-face {{font-family:'{0}';src: url('{0}.ttf') format('truetype');src: url('{0}.woff') format('woff');font-weight:normal;font-style:normal}}",
                    fontName);
                sw.WriteLine("html,body,div.main,div.footnote,div,ol.nav,h1,ul.tnav {{font-family:'{0}','{1}','Liberation Sans','liberationsans_regular','sans-serif'}}",fontName, m_options.fontFamily);
                if (m_options.commonChars)
                {
                    sw.WriteLine(".chapterlabel,.mt,.tnav,h1.title,a.xx,a.oo,a.nn {{'Liberation Sans','liberationsans_regular','sans-serif'}}");
                }
                sw.WriteLine("* {margin:0;padding:0}");
                sw.WriteLine("html,body	{0}height:100%;font-size:1.0em;line-height:{1}em{2}", "{", (m_options.script.ToLowerInvariant() == "latin")?"1.2":"2.5", "}");
                sw.Write(epubStyleSheet);
                sw.Close();
            }
            fileHelper.CopyFile(Path.Combine(fontSource, fontName + ".woff"), Path.Combine(htmlPath, fontName + ".woff"));
            fileHelper.CopyFile(Path.Combine(fontSource, fontName + ".ttf"), Path.Combine(htmlPath, fontName + ".ttf"));
            fileHelper.CopyFile(Path.Combine(fontSource, "liberationsans_regular.woff"), Path.Combine(htmlPath, "liberationsans_regular.woff"));
            fileHelper.CopyFile(Path.Combine(fontSource, "liberationsans_regular.ttf"), Path.Combine(htmlPath, "liberationsans_regular.ttf"));
            ePubWriter toEpub = new ePubWriter();
            toEpub.projectOptions = m_options;
            toEpub.projectOutputDir = m_outputProjectDirectory;
            toEpub.epubDirectory = epubPath;
            toEpub.redistributable = m_options.redistributable;
            toEpub.epubIdentifier = GetEpubID();
            toEpub.stripPictures = false;
            toEpub.indexDate = DateTime.UtcNow;
            if (File.Exists(dbsLogo) && !m_options.privateProject)
            {
                toEpub.indexDateStamp = "ePub generated by <a href='http://eBible.org'>eBible.org</a> in cooperation with the <a href='http://dbs.org' target='_blank'>Digital Bible Society</a> " + toEpub.indexDate.ToString("d MMM yyyy") +
                    " from source files dated " + sourceDate.ToString("d MMM yyyy") +
                    "<br/><a href='http://www.dbs.org' target='_blank'><img src='dbs.jpg' alt='Digital Bible Society' width='410' height='100' title='Published by the Digital Bible Society'/></a>";
                fileHelper.CopyFile(dbsLogo, Path.Combine(htmlPath, "dbs.jpg"));
                if (m_options.redistributable)
                {
                    toEpub.indexDateStamp = toEpub.indexDateStamp + "<br/>Also available on <a href='http://eBible.org'>eBible.org</a>.";
                }
            }
            else
            {
                toEpub.indexDateStamp = "ePub generated " + toEpub.indexDate.ToString("d MMM yyyy") +
                    " from source files dated " + sourceDate.ToString("d MMM yyyy");
            }
            if (m_options.countryCode == "PG" && File.Exists(pngbtaLogo) && m_options.redistributable)
            {
                toEpub.indexDateStamp = "<a href='pngbta.org'><img src='pngbta.jpg' alt='PNG Bible Translation Association' title='Published by the PNG Bible Translation Association'/></a><br/>Posted on <a href='http://PNGScriptures.org'>PNGScriptures.org</a> and <a href='http://TokPlesBaibel.org'>TokPlesBaibel.org</a> by the <a href='http://pngbta.org'>PNG Bible Translation Association</a>.<br/>" + toEpub.indexDateStamp;
                fileHelper.CopyFile(pngbtaLogo, Path.Combine(htmlPath, "pngbta.jpg"));
            }
            toEpub.GeneratingConcordance = m_options.GenerateConcordance || m_options.UseFrames;
            toEpub.CrossRefToFilePrefixMap = m_options.CrossRefToFilePrefixMap;
            toEpub.contentCreator = m_options.contentCreator;
            toEpub.contributor = m_options.contributor;
            string usfxFilePath = Path.Combine(UsfxPath, "usfx.xml");
            toEpub.bookInfo.ReadPublicationOrder(orderFile);
            toEpub.MergeXref(Path.Combine(m_inputProjectDirectory, "xref.xml"));
            //TODO: eliminate side effects in expandPercentEscapes
            // Side effect: expandPercentEscapes sets longCopyrightMessage and shortCopyrightMessage.
            toEpub.sourceLink = expandPercentEscapes("<a href=\"http://%h/%t\">%v</a>");
            toEpub.longCopr = longCopyrightMessage;
            toEpub.shortCopr = shortCopyrightMessage;
            toEpub.textDirection = m_options.textDir;
            toEpub.customCssName = "epub.css";
            toEpub.stripManualNoteOrigins = m_options.stripNoteOrigin;
            toEpub.noteOriginFormat = m_options.xoFormat;
            toEpub.englishDescription = m_options.EnglishDescription;
            toEpub.preferredFont = m_options.fontFamily;
            toEpub.fcbhId = m_options.fcbhId;
            toEpub.coverName = Path.GetFileName(preferredCover);
            string coverPath = Path.Combine(htmlPath, toEpub.coverName);
            File.Copy(preferredCover, coverPath);
            if (m_options.PrepublicationChecks &&
                (m_options.publicDomain || m_options.redistributable || File.Exists(Path.Combine(m_inputProjectDirectory, "certify.txt"))) &&
                File.Exists(certified))
            {
                File.Copy(certified, Path.Combine(htmlPath, "eBible.org_certified.jpg"));
                toEpub.indexDateStamp = toEpub.indexDateStamp + "<br /><a href='http://eBible.org/certified/' target='_blank'><img src='eBible.org_certified.jpg' alt='eBible.org certified' /></a>";
            }
            toEpub.xrefCall.SetMarkers(m_options.xrefCallers);
            toEpub.footNoteCall.SetMarkers(m_options.footNoteCallers);
            toEpub.projectInputDir = m_inputProjectDirectory;
            toEpub.ConvertUsfxToHtml(usfxFilePath, htmlPath,
                m_options.vernacularTitle,
                m_options.languageId,
                m_options.translationId,
                m_options.chapterLabel,
                m_options.psalmLabel,
                "<a class='xx' href='copyright.xhtml'>" +  usfxToHtmlConverter.EscapeHtml(shortCopyrightMessage) + "</a>",
                expandPercentEscapes(m_options.homeLink),
                expandPercentEscapes(m_options.footerHtml),
                expandPercentEscapes(m_options.indexHtml),
                copyrightPermissionsStatement(),
                m_options.ignoreExtras,
                m_options.goText);
            toEpub.bookInfo.RecordStats(m_options);
            m_options.commonChars = toEpub.commonChars;
            m_options.Write();
            LoadStatisticsTab();
            Logit.CloseFile();
            if (Logit.loggedError)
            {
                m_options.lastRunResult = false;
            }
        }

    	private void ConvertUsfxToPortableHtml()
        {
            int i;
            if ((m_options.languageId.Length < 3) || (m_options.translationId.Length < 3))
                return;
            currentConversion = "writing portable HTML";
            string UsfxPath = Path.Combine(m_outputProjectDirectory, "usfx");
            string htmlPath = Path.Combine(m_outputProjectDirectory, "html");
            if (!Directory.Exists(UsfxPath))
            {
                MessageBox.Show(this, UsfxPath + " not found!", "ERROR");
                return;
            }
            Utils.EnsureDirectory(m_outputDirectory);
            Utils.EnsureDirectory(m_outputProjectDirectory);
            Utils.EnsureDirectory(htmlPath);
            string propherocss = Path.Combine(htmlPath, m_options.customCssFileName);
            if (File.Exists(propherocss))
                File.Delete(propherocss);
            // Copy cascading style sheet from project directory, or if not there, BibleConv/input/.
            string specialCss = Path.Combine(m_inputProjectDirectory, m_options.customCssFileName);
            if (File.Exists(specialCss))
                File.Copy(specialCss, propherocss);
            else
                File.Copy(Path.Combine(m_inputDirectory, m_options.customCssFileName), propherocss);

            // Copy any extra files from the htmlextras directory in the project directory to the output.
            // This is for introduction files, pictures, etc.
            string htmlExtras = Path.Combine(m_inputProjectDirectory, "htmlextras");
            if (Directory.Exists(htmlExtras))
            {
                WordSend.fileHelper.CopyDirectory(htmlExtras, htmlPath);
            }
            
            usfxToHtmlConverter toHtm;
			if (m_options.UseFrames)
			{
				var framedConverter = new UsfxToFramedHtmlConverter();
				framedConverter.HideNavigationButtonText = m_options.HideNavigationButtonText;
				framedConverter.ShowNavigationButtonText = m_options.ShowNavigationButtonText;
				toHtm = framedConverter;
			}
			else
			{
                if (m_options.GenerateMobileHtml)
                {
                    toHtm = new usfx2MobileHtml();
                }
                else
                {
                    toHtm = new usfxToHtmlConverter();
                }
			}
            toHtm.Jesusfilmlink = m_options.JesusFilmLinkTarget;
            toHtm.Jesusfilmtext = m_options.JesusFilmLinkText;
            toHtm.stripPictures = false;
            toHtm.htmlextrasDir = Path.Combine(m_inputProjectDirectory, "htmlextras");
            string logFile = Path.Combine(m_outputProjectDirectory, "HTMLConversionReport.txt");
            Logit.OpenFile(logFile);
            Logit.GUIWriteString = showMessageString;
            Logit.UpdateStatus = updateConversionProgress;
            string theIndexDate = toHtm.indexDate.ToString("d MMM yyyy");
            string theSourceDate = sourceDate.ToString("d MMM yyyy");
            if (File.Exists(dbsLogo) && !m_options.privateProject)
            {
                toHtm.indexDateStamp = "HTML generated by <a href='http://eBible.org'>eBible.org in cooperation with the <a href='http://dbs.org' target='_blank'>Digital Bible Society</a> " + theIndexDate +
                    " from source files dated " + theSourceDate +
                    "<br/><a href='http://www.dbs.org' target='_blank'><img src='dbs.jpg' alt='Digital Bible Society' width='410' height='100' title='Published by the Digital Bible Society'/></a>";
                fileHelper.CopyFile(dbsLogo, Path.Combine(htmlPath, "dbs.jpg"));
                if (m_options.redistributable)
                {
                    toHtm.indexDateStamp = toHtm.indexDateStamp + "<br/>Also available on <a href='http://eBible.org'>eBible.org</a>.";
                }
            }
            else
            {
                toHtm.indexDateStamp = "HTML generated " + toHtm.indexDate.ToString("d MMM yyyy") +
                    " from source files dated " + sourceDate.ToString("d MMM yyyy");
            }
            if (m_options.countryCode == "PG" && File.Exists(pngbtaLogo) && m_options.redistributable)
            {
                toHtm.indexDateStamp = "<a href='pngbta.org'><img src='pngbta.jpg' alt='PNG Bible Translation Association' title='Published by the PNG Bible Translation Association'/></a><br/>Posted on <a href='http://PNGScriptures.org'>PNGScriptures.org</a> and <a href='http://TokPlesBaibel.org'>TokPlesBaibel.org</a> by the <a href='http://pngbta.org'>PNG Bible Translation Association</a>.<br/>" + toHtm.indexDateStamp;
                fileHelper.CopyFile(pngbtaLogo, Path.Combine(htmlPath, "pngbta.jpg"));
            }
        	toHtm.GeneratingConcordance = m_options.GenerateConcordance || m_options.UseFrames;
    		toHtm.CrossRefToFilePrefixMap = m_options.CrossRefToFilePrefixMap;
    		string usfxFilePath = Path.Combine(UsfxPath, "usfx.xml");
            toHtm.bookInfo.ReadPublicationOrder(orderFile);
            toHtm.MergeXref(Path.Combine(m_inputProjectDirectory, "xref.xml"));
            toHtm.sourceLink = expandPercentEscapes("<a href=\"http://%h/%t\">%v</a>");
            toHtm.textDirection = m_options.textDir;
            toHtm.customCssName = m_options.customCssFileName;
            toHtm.stripManualNoteOrigins = m_options.stripNoteOrigin;
            toHtm.noteOriginFormat = m_options.xoFormat;
            toHtm.englishDescription = m_options.EnglishDescription;
            toHtm.preferredFont = m_options.fontFamily;
            toHtm.fcbhId = m_options.fcbhId;
            toHtm.redistributable = m_options.redistributable;
            toHtm.coverName = String.Empty;// = Path.GetFileName(preferredCover);
            toHtm.projectOutputDir = m_outputProjectDirectory;
            toHtm.projectOptions = m_options;

            //string coverPath = Path.Combine(htmlPath, toHtm.coverName);
            //File.Copy(preferredCover, coverPath);

            if (!String.IsNullOrEmpty(certified))
            {
                try
                {
                    File.Copy(certified, Path.Combine(htmlPath, "eBible.org_certified.jpg"));
                    toHtm.indexDateStamp = toHtm.indexDateStamp + "<br /><a href='http://eBible.org/certified/' target='_blank'><img src='eBible.org_certified.jpg' alt='eBible.org certified' /></a>";
                }
                catch (Exception ex)
                {
                    Logit.WriteLine(ex.Message);
                }
            }
            toHtm.xrefCall.SetMarkers(m_options.xrefCallers);
            toHtm.projectInputDir = m_inputProjectDirectory;
            toHtm.footNoteCall.SetMarkers(m_options.footNoteCallers);
            toHtm.ConvertUsfxToHtml(usfxFilePath, htmlPath,
                m_options.vernacularTitle,
                m_options.languageId,
                m_options.translationId,
                m_options.chapterLabel,
                m_options.psalmLabel,
                "<a href='copyright.htm'>" + usfxToHtmlConverter.EscapeHtml(shortCopyrightMessage) + "</a>",
                expandPercentEscapes(m_options.homeLink),
                expandPercentEscapes(m_options.footerHtml),
                expandPercentEscapes(m_options.indexHtml),
                copyrightPermissionsStatement(),
                m_options.ignoreExtras,
                m_options.goText);
            toHtm.bookInfo.RecordStats(m_options);
            m_options.commonChars = toHtm.commonChars;
            m_options.Write();
            string fontsDir = Path.Combine(htmlPath, "fonts");
            fileHelper.EnsureDirectory(fontsDir);
            string fontSource = Path.Combine(dataRootDir, "fonts");
            string fontName = m_options.fontFamily.ToLower().Replace(' ', '_');
            fileHelper.CopyFile(Path.Combine(fontSource, fontName + ".ttf"), Path.Combine(fontsDir, fontName + ".ttf"));
            fileHelper.CopyFile(Path.Combine(fontSource, fontName + ".woff"), Path.Combine(fontsDir, fontName + ".woff"));
            fileHelper.CopyFile(Path.Combine(fontSource, fontName + ".eot"), Path.Combine(fontsDir, fontName + ".eot"));
            LoadStatisticsTab();
            Logit.CloseFile();
            if (Logit.loggedError)
            {
                m_options.lastRunResult = false;
            }

            currentConversion = "Writing auxilliary metadata files.";
            Application.DoEvents();
            if (!fileHelper.fAllRunning)
                return;

            // We currently have the information handy to write some auxilliary XML files
            // that contain metadata. We will put these in the USFX directory.

            XmlTextWriter xml = new XmlTextWriter(Path.Combine(UsfxPath, m_options.translationId + "-VernacularParms.xml"), System.Text.Encoding.UTF8);
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
            xml.WriteElementString("dc:creator", m_options.contentCreator);
            xml.WriteElementString("dc:contributor", m_options.contributor);
            string title = m_options.vernacularTitle;
            if (title.Length == 0)
                title = m_options.EnglishDescription;
            xml.WriteElementString("dc:title", title);
            xml.WriteElementString("dc:description", m_options.EnglishDescription);
            xml.WriteElementString("dc:date", m_options.contentUpdateDate.ToString("yyyy-MM-dd"));
            xml.WriteElementString("dc:format", "digital");
            xml.WriteElementString("dc:language", m_options.languageId);
            xml.WriteElementString("dc:publisher", m_options.electronicPublisher);
            string rights = String.Empty;
            string shortRights = m_options.translationId + " Scripture ";
            string copyright = "Copyright © " + m_options.copyrightYears + " " +  m_options.copyrightOwner;
            if (m_options.publicDomain)
            {
                copyright = rights = "Public Domain";
                shortRights = shortRights + "is in the Public Domain.";
            }
            else if (m_options.ccbyndnc)
            {
                rights = copyright + @"
This work is made available to you under the terms of the Creative Commons Attribution-Noncommercial-No Derivative Works license at http://creativecommons.org/licenses/by-nc-nd/4.0/.
You may convert the text to different file formats or make extracts, as long as you don't change any of the text or punctuation of the content." +
                Environment.NewLine + m_options.rightsStatement;
                shortRights = shortRights + copyright + " Creative Commons BY-NC-ND license.";
            }
            else if (m_options.ccbynd)
            {
                rights = copyright + @"
This work is made available to you under the terms of the Creative Commons Attribution-No Derivative Works license at http://creativecommons.org/licenses/by-nd/4.0/." +
                Environment.NewLine + m_options.rightsStatement;
                shortRights = shortRights + copyright + " Creative Commons BY-ND license.";
            }
            else if (m_options.ccbysa)
            {
                rights = copyright + @"
This work is made available to you under the terms of the Creative Commons Attribution-Share-Alike license at http://creativecommons.org/licenses/by-sa/4.0/." +
                Environment.NewLine + m_options.rightsStatement;
                shortRights = shortRights + copyright + " Creative Commons BY-SA license.";
            }
            else if (m_options.otherLicense)
            {
                rights = copyright + Environment.NewLine + m_options.rightsStatement;
                shortRights = shortRights + copyright;
            }
            else if (m_options.allRightsReserved)
            {
                rights = copyright + " All rights reserved.";
                shortRights = shortRights + rights;
                if (m_options.rightsStatement.Length > 0)
                    rights = rights + Environment.NewLine + m_options.rightsStatement;
            }
            xml.WriteElementString("dc:rights", rights);
            xml.WriteElementString("dc:identifier", String.Empty);
            xml.WriteElementString("dc:type", String.Empty);
            xml.WriteEndElement();  // dcMeta
            xml.WriteElementString("numberSystem", m_options.numberSystem);
            xml.WriteElementString("chapterAndVerseSeparator", m_options.CVSeparator);
            xml.WriteElementString("rangeSeparator", m_options.rangeSeparator);
            xml.WriteElementString("multiRefSameChapterSeparator", m_options.multiRefSameChapterSeparator);
            xml.WriteElementString("multiRefDifferentChapterSeparator", m_options.multiRefDifferentChapterSeparator);
            xml.WriteElementString("verseNumberLocation", m_options.verseNumberLocation);
            xml.WriteElementString("footnoteMarkerStyle", m_options.footnoteMarkerStyle);
            xml.WriteElementString("footnoteMarkerResetAt", m_options.footnoteMarkerResetAt);
            xml.WriteElementString("footnoteMarkers", m_options.footNoteCallers);
            xml.WriteElementString("BookSourceForMarkerXt", m_options.BookSourceForMarkerXt);
            xml.WriteElementString("BookSourceForMarkerR", m_options.BookSourceForMarkerR);
            xml.WriteElementString("iso", m_options.languageId);
            xml.WriteElementString("isoVariant", m_options.dialect);
            xml.WriteElementString("langName", m_options.languageName);
            xml.WriteElementString("textDir", m_options.textDir);
            xml.WriteElementString("hasNotes", (!m_options.ignoreExtras).ToString()); //TODO: check to see if translation has notes or not.
            xml.WriteElementString("coverTitle", m_options.vernacularTitle);
            xml.WriteEndElement();	// vernacularParms
            xml.WriteEndDocument();
            xml.Close();

            xml = new XmlTextWriter(Path.Combine(UsfxPath, m_options.translationId + "-VernacularAdditional.xml"), System.Text.Encoding.UTF8);
            xml.Formatting = System.Xml.Formatting.Indented;
            xml.WriteStartDocument();
            xml.WriteStartElement("vernacularParmsMiscellaneous");
            xml.WriteElementString("translationId", m_options.translationId);
            // xml.WriteElementString("otmlId", " ");
            xml.WriteElementString("versificationScheme", m_options.versificationScheme);
            xml.WriteElementString("checkVersification", "No");
            // xml.WriteElementString("osis2SwordOptions", m_options.osis2SwordOptions);
            // xml.WriteElementString("otmlRenderChapterNumber", m_options.otmlRenderChapterNumber);
            xml.WriteElementString("copyright", shortRights);
            xml.WriteEndElement();	// vernacularParmsMiscellaneous
            xml.WriteEndDocument();
            xml.Close();

            // Write the ETEN DBL MetaData.xml file in the usfx directory.
            string metaXmlName = Path.Combine(UsfxPath, m_options.translationId + "metadata.xml");
            xml = new XmlTextWriter(metaXmlName, System.Text.Encoding.UTF8);
            xml.Formatting = System.Xml.Formatting.Indented;
            xml.WriteStartDocument();
            // xml.WriteProcessingInstruction("xml-model", "href=\"metadataWbt-1.3.rnc\" type=\"application/relax-ng-compact-syntax\"");
            xml.WriteStartElement("DBLMetadata");
            string etendblid = m_options.paratextGuid;
            if (etendblid.Length > 16)
                etendblid = etendblid.Substring(0, 16);
            xml.WriteAttributeString("id", etendblid);
            // xml.WriteAttributeString("revision", "4");
            xml.WriteAttributeString("type", "text");
            xml.WriteAttributeString("typeVersion", "1.5");
            xml.WriteStartElement("identification");
            xml.WriteElementString("name", m_options.shortTitle);
            xml.WriteElementString("nameLocal", m_options.vernacularTitle);
            xml.WriteElementString("abbreviation", m_options.translationId);
            string abbreviationLocal = m_options.translationTraditionalAbbreviation;
            if (abbreviationLocal.Length < 2)
            {
                abbreviationLocal = m_options.translationId.ToUpperInvariant();
            }
            xml.WriteElementString("abbreviationLocal", abbreviationLocal);
            string scope = "Portion only";
            if (m_options.ntBookCount == 27)
            {
                if (m_options.otBookCount == 39)
                {
                    if (m_options.adBookCount > 0)
                    {
                        scope = "Bible with Deuterocanon";
                    }
                    else
                    {
                        scope = "Bible without Deuterocanon";
                    }
                }
                else if (m_options.otBookCount > 0)
                {
                    if ((m_options.otBookCount == 1) && (m_options.otChapCount == 150))
                        scope = "New Testament and Psalms";
                    else
                        scope = "New Testament and Shorter Old Testament";
                }
                else
                {
                    scope = "NT";   // "New Testament only" is also allowed here.
                }
            }
            else if (m_options.otBookCount == 39)
            {
                if (m_options.ntBookCount == 0)
                {
                    if (m_options.adBookCount > 0)
                        scope = "Old Testament with Deuterocanon";
                    else
                        scope = "Old Testament only";
                }
            }
            xml.WriteElementString("scope", scope);
            xml.WriteElementString("description", m_options.EnglishDescription);
            string yearCompleted = m_options.copyrightYears.Trim();
            if (yearCompleted.Length > 4)
                yearCompleted = yearCompleted.Substring(yearCompleted.Length - 4);
            xml.WriteElementString("dateCompleted", yearCompleted);
            xml.WriteStartElement("systemId");
            xml.WriteAttributeString("fullname", m_options.shortTitle);
            xml.WriteAttributeString("name", m_options.paratextProject);
            xml.WriteAttributeString("type", "paratext");

            xml.WriteEndElement();
            xml.WriteElementString("bundleProducer", "");
            xml.WriteEndElement();  // identification
            xml.WriteElementString("confidential", "false");
            /*
            xml.WriteStartElement("agencies");
            string etenPartner = "WBT";
            if ((m_options.publicDomain == true) || m_options.copyrightOwner.ToUpperInvariant().Contains("EBIBLE"))
                etenPartner = "eBible.org";
            else if (m_options.copyrightOwner.ToUpperInvariant().Contains("SOCIETY"))
                etenPartner = "UBS";
            else if (m_options.copyrightOwner.ToUpperInvariant().Contains("BIBLICA"))
                etenPartner = "Biblica";
            else if (m_options.copyrightOwnerAbbrev.ToUpperInvariant().Contains("PBT"))
                etenPartner = "PBT";
            else if (m_options.copyrightOwnerAbbrev.ToUpperInvariant().Contains("SIM"))
                etenPartner = "SIM";
            xml.WriteElementString("etenPartner", etenPartner);
            xml.WriteElementString("creator", m_options.contentCreator);
            xml.WriteElementString("publisher", m_options.electronicPublisher);
            xml.WriteElementString("contributor", m_options.contributor);
            xml.WriteEndElement();  // agencies
            */
            xml.WriteStartElement("language");
            xml.WriteElementString("iso", m_options.languageId);
            xml.WriteElementString("name", m_options.languageNameInEnglish);
            xml.WriteElementString("nameLocal", m_options.languageName);
            xml.WriteElementString("ldml", m_options.ldml);
            xml.WriteElementString("rod", m_options.rodCode);
            xml.WriteElementString("script", m_options.script);
            xml.WriteElementString("scriptDirection", m_options.textDir.ToUpperInvariant());
            string numerals = m_options.numberSystem;
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
            xml.WriteElementString("iso", m_options.countryCode);
            xml.WriteElementString("name", m_options.country);
            xml.WriteEndElement();  // country
            xml.WriteStartElement("type");
            if (m_options.copyrightYears.Length > 4)
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
            xml.WriteElementString("name", m_options.shortTitle);
            xml.WriteElementString("nameLocal", m_options.vernacularTitle);
            xml.WriteElementString("abbreviation", m_options.translationId);
            xml.WriteElementString("abbreviationLocal", abbreviationLocal);
            xml.WriteElementString("description", m_options.canonTypeEnglish);    // Book list description, like common, Protestant, or Catholic
            xml.WriteElementString("descriptionLocal", m_options.canonTypeLocal);
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
            if (!m_options.publicDomain)
            {
                xml.WriteStartElement("contact");
                xml.WriteElementString("rightsHolder", m_options.copyrightOwner);
                string localRights = m_options.localRightsHolder.Trim();
                if (localRights.Length == 0)
                    localRights = m_options.copyrightOwner;
                xml.WriteElementString("rightsHolderLocal", localRights);
                string rightsHolderAbbreviation = m_options.copyrightOwnerAbbrev.Trim();
                if (rightsHolderAbbreviation.Length < 1)
                {
                    string s = m_options.copyrightOwner.Trim().ToUpperInvariant().Replace(" OF ", " ");
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
                string ownerUrl = m_options.copyrightOwnerUrl;
                if (ownerUrl.ToUpperInvariant().StartsWith("HTTPS://"))
                    ownerUrl = ownerUrl.Substring(8);
                if (ownerUrl.ToUpperInvariant().StartsWith("HTTP://"))
                    ownerUrl = ownerUrl.Substring(7);
                xml.WriteElementString("rightsHolderURL", ownerUrl);
                xml.WriteElementString("rightsHolderFacebook", m_options.facebook);
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
            if (m_options.promoHtml.Trim().Length > 3)
                xml.WriteString(m_options.promoHtml);
            else
                xml.WriteElementString("p", rights);
            xml.WriteEndElement();  // promoVersionInfo
            xml.WriteStartElement("promoEmail");
            xml.WriteAttributeString("contentType", "xhtml");
            xml.WriteString(@"Thank you for downloading ");
            xml.WriteString(m_options.vernacularTitle);
            xml.WriteString(@"! Now you'll have anytime, anywhere access to God's Word on your mobile device—even if 
you're outside of service coverage or not connected to the Internet. It also means faster service whenever you read that version since it's 
stored on your device. Enjoy! This download was made possible by ");
            xml.WriteString(m_options.copyrightOwner.Trim(new char[]{' ', '.'}));
            xml.WriteString(@". We really appreciate their passion for making the Bible available to millions of people around the world. Because of 
their generosity, people like you can open up the Bible and hear from God no matter where you are. You can learn more about them at ");
            xml.WriteString(m_options.copyrightOwnerUrl);
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

            /*
            string rncVersion = "metadataWbt-1.3.rnc";
            string rnc = SFConverter.FindAuxFile(rncVersion);
            File.Copy(rnc, Path.Combine(UsfxPath, rncVersion));

            if (m_options.paratextProject.Length > 0)
            {   // If this project is from Paratext, send the metadata back to the Paratext project shared directory if it isn't there already.
                if (Directory.Exists(paratextProjectsDir))
                {
                    string sharedDir = Path.Combine(Path.Combine(paratextProjectsDir, m_options.paratextProject), "shared");
                    Utils.EnsureDirectory(sharedDir);
                    string rncTarget = Path.Combine(sharedDir, rncVersion);
                    if (!File.Exists(rncTarget))
                        File.Copy(rnc, rncTarget, true);
                    string metaxml = Path.Combine(sharedDir, m_options.translationId + "MetaData.xml");
                    if (!File.Exists(metaxml))
                        File.Copy(metaXmlName, metaxml);
                    string licenseName = Path.Combine(sharedDir, m_options.translationId + "License.xml");
                    if (!File.Exists(licenseName))
                    {
                        xml = new XmlTextWriter(licenseName, Encoding.UTF8);
                        xml.WriteStartDocument();
                        xml.WriteStartElement("license");
                        xml.WriteAttributeString("id", "");
                        DateTime yetzt = DateTime.UtcNow;
                        xml.WriteElementString("dateLicense", yetzt.ToString("yyyy'-'MM'-'dd"));
                        yetzt = yetzt.AddYears(100);
                        xml.WriteElementString("dateLicenseExpiry", yetzt.ToString("yyyy'-'MM'-'dd"));
                        xml.WriteStartElement("publicationRights");
                        xml.WriteElementString("allowOffline", "true");
                        xml.WriteElementString("allowIntroductions", "true");
                        xml.WriteElementString("allowFootnotes", "true");
                        xml.WriteElementString("allowCrossreferences", "true");
                        xml.WriteElementString("allowExtendedNotes", "true");
                        xml.WriteEndElement();  // publicationRights
                        xml.WriteEndElement();  // license
                        xml.Close();
                    }
                }
            }
            */

            if (m_options.UseFrames)
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
				ciMaker.IntroductionLinkText = m_options.IntroductionLinkText;
    			ciMaker.ConcordanceLinkText = m_options.ConcordanceLinkText;
				string chapIndexPath = Path.Combine(htmlPath, UsfxToChapterIndex.ChapIndexFileName);
				ciMaker.Generate(usfxFilePath, chapIndexPath);
				EnsureTemplateFile("chapIndex.css", htmlPath);
				EnsureTemplateFile("frameFuncs.js", htmlPath);
				EnsureTemplateFile("Navigation.js", htmlPath);
			}

			// Todo JohnT: move this to a new method, and the condition to the method that calls this.
			if (m_options.GenerateConcordance || m_options.UseFrames)
			{
                /*****
				currentConversion = "generate XHTML for concordance";
				usfxToHtmlConverter toXhtm = new usfxToXhtmlConverter();
				Logit.OpenFile(Path.Combine(m_outputProjectDirectory, "XHTMLConversionReport.txt"));

				toXhtm.indexDateStamp = "XHTML generated " + DateTime.UtcNow.ToString("d MMM yyyy") +
				                       " from source files dated " + sourceDate.ToString("d MMM yyyy");
				string xhtmlPath = Path.Combine(m_outputProjectDirectory, "xhtml");
				Utils.EnsureDirectory(xhtmlPath);
				// No point in doing this...doesn't change the concordance generated, just makes generation slower.
				// Reinstate it if the XHTML is used for anything besides generating the concordance.
				//toXhtm.CrossRefToFilePrefixMap = m_options.CrossRefToFilePrefixMap;
				toXhtm.ConvertUsfxToHtml(usfxFilePath, xhtmlPath,
				                        m_options.vernacularTitle,
				                        m_options.languageId,
				                        m_options.translationId,
				                        m_options.chapterLabel,
				                        m_options.psalmLabel,
				                        m_options.copyrightLink,
				                        m_options.homeLink,
				                        m_options.footerHtml,
				                        m_options.indexHtml,
				                        m_options.licenseHtml,
				                        m_options.useKhmerDigits,
				                        m_options.ignoreExtras,
				                        m_options.goText);
				Logit.CloseFile();
                ******/
				currentConversion = "Concordance";
				string concordanceDirectory = Path.Combine(htmlPath, "conc");
                statusNow("Deleting " + concordanceDirectory);
				Utils.DeleteDirectory(concordanceDirectory); // Blow away any previous results
                statusNow("Creating " + concordanceDirectory);
				Utils.EnsureDirectory(concordanceDirectory);
				string excludedClasses =
					"toc toc1 toc2 navButtons pageFooter chapterlabel r verse"; // from old prophero: "verse chapter notemark crmark crossRefNote parallel parallelSub noteBackRef popup crpopup overlap";
				string headingClasses = "mt mt2 s"; // old prophero: "sectionheading maintitle2 footnote sectionsubheading";
				var concGenerator = new ConcGenerator(m_inputProjectDirectory, concordanceDirectory)
				                    	{
											// Currently configurable options
											MergeCase = m_options.MergeCase,
											MaxContextLength = m_options.MaxContextLength,
											MinContextLength =  m_options.MinContextLength,
											WordformingChars = m_options.WordformingChars,
											MaxFrequency = m_options.MaxFrequency,
											Phrases = m_options.Phrases,
											ExcludeWords = new HashSet<string>(m_options.ExcludeWords.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)),
											ReferenceAbbeviationsMap = m_options.ReferenceAbbeviationsMap,
											BookChapText = m_options.BooksAndChaptersLinkText,
											ConcordanceLinkText = m_options.ConcordanceLinkText,

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
                statusNow("Generating concordance.");
                
                concGenerator.Run(Path.Combine(Path.Combine(m_outputProjectDirectory, "search"), "verseText.xml"));

				var concFrameGenerator = new ConcFrameGenerator()
				                         	{ConcDirectory = concordanceDirectory, LangName = m_options.vernacularTitle};
                concFrameGenerator.customCssName = m_options.customCssFileName;
				concFrameGenerator.Run();
				EnsureTemplateFile("mktree.css", concordanceDirectory);
				EnsureTemplateFile("plus.gif", concordanceDirectory);
				EnsureTemplateFile("minus.gif", concordanceDirectory);
				EnsureTemplateFile("display.css", concordanceDirectory);
				EnsureTemplateFile("TextFuncs.js", htmlPath);
			}
        }

        public void showHelp(string helpFile)
        {
            try
            {
                string helpFilePath = SFConverter.FindAuxFile(helpFile);
                string safari = @"/Applications/Safari.app/Contents/MacOS/Safari";
                if (File.Exists(safari))
                {
                    System.Diagnostics.Process.Start(safari, helpFilePath);
                }
                else
                {
                    System.Diagnostics.Process.Start(helpFilePath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error displaying " + helpFile);
            }
            
        }

        /// <summary>
        /// Inserts up to 2 line breaks into a string at points less than or equal to maxWidth characters long
        /// </summary>
        /// <param name="s">String to word wrap</param>
        /// <param name="maxWidth">Maximum line length</param>
        /// <returns>string with line breaks addded</returns>
        protected string ShortWordWrap(string s, int maxWidth)
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
        /// Pick a pair of related dark and light colors for this translation's default cover
        /// based on the epub ID
        /// </summary>
        /// <returns>0, 1, 2, or 3</returns>
        protected int MakeUpColor(out string dark, out string light)
        {
            string s = GetEpubID().Substring(0, 6);
            int color = Convert.ToInt32(s, 16);
            switch (m_options.epubId[7])
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
            return Convert.ToInt32(m_options.epubId.Substring(10, 1), 16) & 3;
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
        public string CreateCover(bool small=false)
        {
            string dark, light, svgPath, pngPath;
            StreamWriter sw;
            string coverOutput = Path.Combine(m_outputProjectDirectory, "cover");
            if (!small)
                Utils.DeleteDirectory(coverOutput);
            Utils.EnsureDirectory(coverOutput);

            // Get the best cover.svg available.
            string coverIn = Path.Combine(m_inputProjectDirectory, "cover.svg");
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
                    if (m_options.vernacularTitle.Length > 50)
                    {
                        svg = svg.Replace("font-size=\"100\"", "font-size=\"60\"");
                        mainTitle = ShortWordWrap(m_options.vernacularTitle, 48).Split(newLine);
                    }
                    else if (m_options.vernacularTitle.Length < 24)
                    {
                        svg = svg.Replace("font-size=\"100\"", "font-size=\"140\"");
                        mainTitle = ShortWordWrap(m_options.vernacularTitle, 17).Split(newLine);
                    }
                    else
                    {
                        mainTitle = ShortWordWrap(m_options.vernacularTitle, 20).Split(newLine);
                    }
                    string[] description = ShortWordWrap(m_options.EnglishDescription, 60).Split(newLine);
                    sw = new StreamWriter(coverOut);
                    svg = svg.Replace("^f", m_options.fontFamily).Replace("^4", description[0]);
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
            string dbsCoverIn = Path.Combine(m_inputProjectDirectory, "dbscover.png");
            m_options.dbsCover = false;
            if (File.Exists(coverIn))
            {
                fileHelper.CopyFile(coverIn, coverOut);
            }
            else if (File.Exists(dbsCoverIn))
            {
                fileHelper.CopyFile(dbsCoverIn, coverOut);
                m_options.dbsCover = true;
            }

            // Look for .jpg files.
            coverOut = Path.ChangeExtension(coverOut, "jpg");
            coverIn = Path.ChangeExtension(coverIn, "jpg");
            dbsCoverIn = Path.ChangeExtension(dbsCoverIn, "jpg");
            if (File.Exists(coverIn))
            {
                fileHelper.CopyFile(coverIn, coverOut);
            }
            else if (File.Exists(dbsCoverIn))
            {
                fileHelper.CopyFile(dbsCoverIn, coverOut);
                m_options.dbsCover = true;
            }

            if (File.Exists(coverOut))
            {   // We have a jpg cover. Is it big enough?
                if (small)
                    return coverOut;
                Image img = Image.FromFile(coverOut);
                if ((img.Width >= 450) && (img.Height >= 700))
                {
                    if (!File.Exists(pngPath) && (Path.DirectorySeparatorChar == '/'))
                    {
                        fileHelper.RunCommand("convert \"" + coverOut + "\" \"" + pngPath + "\"");
                    }
                    return coverOut;
                }
            }
            if (File.Exists(pngPath))
                return pngPath;
            if (Path.DirectorySeparatorChar == '/')
            {
                fileHelper.RunCommand("convert \"" + svgPath + "\" \"" + pngPath + "\"");
            }
            if (File.Exists(pngPath))
                return pngPath;
            return svgPath;
        }



        public void DoPostprocess()
        {
            List<string> postproclist = m_options.postprocesses;
            string command;
            foreach (string proc in postproclist)
            {
                command = proc.Replace("%d", m_project);
                command = command.Replace("%t", m_options.translationId);
                command = command.Replace("%i", m_options.fcbhId);
                command = command.Replace("%e", m_options.languageId);
                command = command.Replace("%h", m_options.homeDomain);
                command = command.Replace("%p", m_options.privateProject ? "private" : "public");
                command = command.Replace("%r", m_options.redistributable ? "redistributable" : "restricted");
                command = command.Replace("%o", m_options.downloadsAllowed ? "downloadable":"onlineonly");
                currentConversion = "Running " + command;
                batchLabel.Text = currentConversion;
                Application.DoEvents();
                if (!fileHelper.RunCommand(command))
                    MessageBox.Show(fileHelper.runCommandError, "Error " + currentConversion);
                currentConversion = String.Empty;
                if (!fileHelper.fAllRunning)
                    return;
            }
            

        }

        /// <summary>
        /// Create USFM from USFX
        /// </summary>
        private void NormalizeUsfm()
        {
            string logFile;
            try
            {
                
                string UsfmDir = Path.Combine(m_outputProjectDirectory, "extendedusfm");
                string UsfxName = Path.Combine(Path.Combine(m_outputProjectDirectory, "usfx"), "usfx.xml");
                if (!File.Exists(UsfxName))
                {
                    MessageBox.Show(this, UsfxName + " not found!", "ERROR normalizing USFM from USFX");
                    return;
                }
                // Start with an EMPTY USFM directory to avoid problems with old files 
                Utils.DeleteDirectory(UsfmDir);
                fileHelper.EnsureDirectory(UsfmDir);
                currentConversion = "Normalizing extended USFM from USFX. ";
                Application.DoEvents();
                if (!fileHelper.fAllRunning)
                    return;
                logFile = Path.Combine(m_outputProjectDirectory, "usfx2usfm2_log.txt");
                Logit.OpenFile(logFile);
                Logit.GUIWriteString = showMessageString;
                Logit.UpdateStatus = updateConversionProgress;
                SFConverter.scripture = new Scriptures(m_options);
                Logit.loggedError = false;
                SFConverter.scripture.USFXtoUSFM(UsfxName, UsfmDir, m_options.translationId + ".usfm", true, m_options);

                UsfmDir = Path.Combine(m_outputProjectDirectory, "usfm");
                Utils.DeleteDirectory(UsfmDir);
                fileHelper.EnsureDirectory(UsfmDir);
                currentConversion = "Normalizing USFM from USFX. ";
                SFConverter.scripture.USFXtoUSFM(UsfxName, UsfmDir, m_options.translationId + ".usfm", false, m_options);
                Logit.CloseFile();
                if (Logit.loggedError)
                {
                    m_options.lastRunResult = false;
                }
                currentConversion = "Converted USFX to USFM.";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error normalizing USFM from USFX");
            }
        }

        /// <summary>
        /// Import all USX files in a directory to USFX, then normalize by converting to USFM and back to USFX.
        /// </summary>
        /// <param name="SourceDir">directory containing USX files</param>
        private void ImportUsx(string SourceDir)
        {
            string usfxDir = Path.Combine(m_outputProjectDirectory, "usfx");
            string tmpname = Path.Combine(usfxDir, "tempusfx.xml");
            string bookNamesFile = Path.Combine(SourceDir, "BookNames.xml");
            try
            {
                string logFile = Path.Combine(m_outputProjectDirectory, "UsxConversionReports.txt");
                Logit.OpenFile(logFile);
                Logit.UpdateStatus = updateConversionProgress;
                Logit.GUIWriteString = showMessageString;
                Logit.loggedError = false;

                // Sanity check
                if (!Directory.Exists(SourceDir))
                {
                    MessageBox.Show(this, SourceDir + " not found!", "ERROR");
                    return;
                }

                currentConversion = "converting from USX to temporary USFX";
                Application.DoEvents();

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
                    MessageBox.Show(ex.Message, "ERROR copying tempusfx.xml");
                    
                }
                
                // Convert from USFX to USFM.
                currentConversion = "converting from initial USFX to USFM";
                Application.DoEvents();
                if (!fileHelper.fAllRunning)
                    return;
                string usfmDir = Path.Combine(m_outputProjectDirectory, "usfm");
                Utils.DeleteDirectory(usfmDir);
                fileHelper.EnsureDirectory(usfmDir);
                fileHelper.CopyFile(bookNamesFile, Path.Combine(usfmDir, "BookNames.xml"), true);
                SFConverter.scripture = new Scriptures(m_options);
                SFConverter.scripture.USFXtoUSFM(usfxName, usfmDir, m_options.translationId + ".usfm", false, m_options);

                usfmDir = Path.Combine(m_outputProjectDirectory, "extendedusfm");
                Utils.DeleteDirectory(usfmDir);
                fileHelper.EnsureDirectory(usfmDir);
                fileHelper.CopyFile(bookNamesFile, Path.Combine(usfmDir, "BookNames.xml"), true);
                SFConverter.scripture = new Scriptures(m_options);
                SFConverter.scripture.USFXtoUSFM(usfxName, usfmDir, m_options.translationId + ".usfm", true, m_options);

                // Recreate USFX from USFM, this time with <ve/> tags and in canonical order
                SFConverter.scripture.bkInfo.ReadDefaultBookNames(Path.Combine(m_outputProjectDirectory, "BookNames.xml"));
                SFConverter.scripture.assumeAllNested = m_options.relaxUsfmNesting;
                // Read the input USFM files into internal data structures.
                SFConverter.ProcessFilespec(Path.Combine(usfmDir, "*.usfm"), Encoding.UTF8);
                currentConversion = "converting from USFM to USFX; writing USFX";
                Application.DoEvents();

                // Write out the USFX file.
                SFConverter.scripture.languageCode = m_options.languageId;
                SFConverter.scripture.WriteUSFX(usfxName);
                string bookNames = Path.Combine(Path.Combine(m_outputProjectDirectory, "usfx"), "BookNames.xml");
                SFConverter.scripture.bkInfo.WriteDefaultBookNames(bookNames);
                bool errorState = Logit.loggedError;
                bool runResult = m_options.lastRunResult;
                fileHelper.revisePua(usfxName);
                SFConverter.scripture.ReadRefTags(usfxName);
                if (!SFConverter.scripture.ValidateUsfx(usfxName))
                {
                    File.Move(usfxName, Path.ChangeExtension(usfxName, ".bad"));
                    File.Move(Path.ChangeExtension(usfxName, ".norefxml"), usfxName);
                    Logit.WriteLine("Retrying validation on usfx.xml without expanded references.");
                    if (SFConverter.scripture.ValidateUsfx(usfxName))
                    {
                        m_options.lastRunResult = runResult;
                        Logit.loggedError = errorState;
                        Logit.WriteLine("Validation passed without expanded references.");
                        m_options.makeHotLinks = false;
                    }
                    else
                    {
                        Logit.WriteError("Second validation failed.");
                    }
                }
                Logit.CloseFile();
                if (Logit.loggedError)
                {
                    m_options.lastRunResult = false;
                }
                else
                {
                    if (File.Exists(tmpname))
                        File.Delete(tmpname);
                }
                currentConversion = "converted USFM to USFX.";
                Application.DoEvents();
            }
            catch (Exception ex)
            {
                
                 MessageBox.Show(ex.Message, "Error importing USX");
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
                
                string UsfmDir = Path.Combine(m_outputProjectDirectory, "extendedusfm");
                if (!Directory.Exists(SourceDir))
                {
                    MessageBox.Show(this, SourceDir + " not found!", "ERROR");
                    return;
                }
                // Start with an EMPTY USFM directory to avoid problems with old files 
                Utils.DeleteDirectory(UsfmDir);
                fileHelper.EnsureDirectory(UsfmDir);
                string usfxDir = Path.Combine(m_outputProjectDirectory, "usfx");
                fileHelper.EnsureDirectory(usfxDir);
                string[] inputFileNames = Directory.GetFiles(SourceDir);
                if (inputFileNames.Length == 0)
                {
                    MessageBox.Show(this, "No files found in " + SourceDir, "ERROR");
                    return;
                }

                foreach (string inputFile in inputFileNames)
                {
                    string filename = Path.GetFileName(inputFile);
                    string fileType = Path.GetExtension(filename).ToUpper();
                    if ((fileType == ".USFX") || (fileType == ".XML"))
                    {
                        currentConversion = "processing " + filename;
                        Application.DoEvents();
                        if (!fileHelper.fAllRunning)
                            break;
                        XmlTextReader xr = new XmlTextReader(inputFile);
                        if (xr.MoveToContent() == XmlNodeType.Element)
                        {
                            if (xr.Name == "usfx")
                            {
                                DateTime fileDate;
                                fileDate = File.GetLastWriteTimeUtc(inputFile);
                                if (fileDate > m_options.SourceFileDate)
                                {
                                    sourceDate = fileDate;
                                    m_options.SourceFileDate = sourceDate;
                                }

                                logFile = Path.Combine(m_outputProjectDirectory, "usfx2usfm_log.txt");
                                Logit.OpenFile(logFile);
                                Logit.GUIWriteString = showMessageString;
                                Logit.UpdateStatus = updateConversionProgress;
                                SFConverter.scripture = new Scriptures(m_options);
                                Logit.loggedError = false;
                                currentConversion = "converting from USFX to USFM";
                                Application.DoEvents();
                                SFConverter.scripture.USFXtoUSFM(inputFile, UsfmDir, m_options.translationId + ".usfm", true, m_options);
                                UsfmDir = Path.Combine(m_outputProjectDirectory, "usfm");
                                // Start with an EMPTY USFM directory to avoid problems with old files 
                                Utils.DeleteDirectory(UsfmDir);
                                fileHelper.EnsureDirectory(UsfmDir);
                                SFConverter.scripture.USFXtoUSFM(inputFile, UsfmDir, m_options.translationId + ".usfm", false, m_options);
                                Logit.CloseFile();
                                if (Logit.loggedError)
                                {
                                    m_options.lastRunResult = false;
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
                        Application.DoEvents();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error importing USFX");
            }
        }

        /// <summary>
        /// Convert USFX to PDF
        /// </summary>
        private void ConvertUsfxToPDF(string xetexDir)
        {
            if ((m_options.languageId.Length < 3) || (m_options.translationId.Length < 3))
                return;
            Usfx2XeTeX toXeTex = new Usfx2XeTeX();
            toXeTex.texDir = xetexDir;
            toXeTex.sqlFileName = string.Empty; // Inhibit re-making SQL file.
            currentConversion = "writing XeTeX";
            string UsfxPath = Path.Combine(m_outputProjectDirectory, "usfx");
            if (!Directory.Exists(UsfxPath))
            {
                MessageBox.Show(this, UsfxPath + " not found!", "ERROR");
                return;
            }
            Utils.DeleteDirectory(xetexDir);
            Utils.EnsureDirectory(m_outputProjectDirectory);
            Utils.EnsureDirectory(m_outputDirectory);
            Utils.EnsureDirectory(xetexDir);
            string logFile = Path.Combine(m_outputProjectDirectory, "xetexConversionReport.txt");
            Logit.OpenFile(logFile);
            Logit.GUIWriteString = showMessageString;
            Logit.UpdateStatus = updateConversionProgress;

            string fontSource = Path.Combine(dataRootDir, "fonts");
            string fontName = m_options.fontFamily.ToLower().Replace(' ', '_');

          
            toXeTex.projectOptions = m_options;
            toXeTex.projectOutputDir = m_outputProjectDirectory;
            toXeTex.redistributable = m_options.redistributable;
            toXeTex.epubIdentifier = GetEpubID();
            toXeTex.stripPictures = false;
            toXeTex.indexDate = DateTime.UtcNow;
            toXeTex.indexDateStamp = "PDF generated on " + toXeTex.indexDate.ToString("d MMM yyyy") +
                " from source files dated " + sourceDate.ToString("d MMM yyyy") + @"\par ";
            toXeTex.GeneratingConcordance = false;
            toXeTex.CrossRefToFilePrefixMap = m_options.CrossRefToFilePrefixMap;
            toXeTex.contentCreator = m_options.contentCreator;
            toXeTex.contributor = m_options.contributor;
            string usfxFilePath = Path.Combine(UsfxPath, "usfx.xml");
            toXeTex.bookInfo.ReadPublicationOrder(orderFile);
            toXeTex.MergeXref(Path.Combine(m_inputProjectDirectory, "xref.xml"));
            //TODO: eliminate side effects in expandPercentEscapes
            // Side effect: expandPercentEscapes sets longCopyrightMessage and shortCopyrightMessage.
            toXeTex.sourceLink = expandPercentEscapes("<a href=\"http://%h/%t\">%v</a>");
            toXeTex.longCopr = longCopyrightMessage;
            toXeTex.shortCopr = shortCopyrightMessage;
            toXeTex.textDirection = m_options.textDir;
            toXeTex.stripManualNoteOrigins = m_options.stripNoteOrigin;
            toXeTex.noteOriginFormat = m_options.xoFormat;
            toXeTex.englishDescription = m_options.EnglishDescription;
            toXeTex.preferredFont = m_options.fontFamily;
            toXeTex.fcbhId = m_options.fcbhId;
            toXeTex.coverName = Path.GetFileName(preferredCover);
            if (m_options.PrepublicationChecks &&
                (m_options.publicDomain || m_options.redistributable || File.Exists(Path.Combine(m_inputProjectDirectory, "certify.txt"))) &&
                File.Exists(certified))
            {
                File.Copy(certified, Path.Combine(xetexDir, "eBible.org_certified.jpg"));
                // toXeTex.indexDateStamp = toXeTex.indexDateStamp + "<br /><a href='http://eBible.org/certified/' target='_blank'><img src='eBible.org_certified.jpg' alt='eBible.org certified' /></a>";
            }
            toXeTex.xrefCall.SetMarkers(m_options.xrefCallers);
            toXeTex.footNoteCall.SetMarkers(m_options.footNoteCallers);
            toXeTex.inputDir = m_inputDirectory;
            toXeTex.projectInputDir = m_inputProjectDirectory;
            toXeTex.ConvertUsfxToHtml(usfxFilePath, xetexDir,
                m_options.vernacularTitle,
                m_options.languageId,
                m_options.translationId,
                m_options.chapterLabel,
                m_options.psalmLabel,
                shortCopyrightMessage,
                expandPercentEscapes(m_options.homeLink),
                expandPercentEscapes(m_options.footerHtml),
                expandPercentEscapes(m_options.indexHtml),
                copyrightPermissionsStatement(),
                m_options.ignoreExtras,
                m_options.goText);
            toXeTex.bookInfo.RecordStats(m_options);
            m_options.commonChars = toXeTex.commonChars;
            m_options.Write();
            LoadStatisticsTab();
            Logit.CloseFile();
            if (Logit.loggedError)
            {
                m_options.lastRunResult = false;
            }
        }

        /// <summary>
        /// Convert USFX to Modified OSIS
        /// </summary>
        private void ConvertUsfxToMosis()
        {
            currentConversion = "writing MOSIS";
            if ((m_options.languageId.Length < 3) || (m_options.translationId.Length < 3))
                return;
            string UsfxPath = Path.Combine(m_outputProjectDirectory, "usfx");
            string mosisPath = Path.Combine(m_outputProjectDirectory, "mosis");
            if (!Directory.Exists(UsfxPath))
            {
                MessageBox.Show(this, UsfxPath + " not found!", "ERROR");
                return;
            }
            string usfxFilePath = Path.Combine(UsfxPath, "usfx.xml");
            string mosisFilePath = Path.Combine(mosisPath, m_options.translationId + "_osis.xml");

            Utils.EnsureDirectory(m_outputDirectory);
            Utils.EnsureDirectory(m_outputProjectDirectory);
            Utils.EnsureDirectory(mosisPath);

            usfxToMosisConverter toMosis = new usfxToMosisConverter();
            if (m_options.redistributable && File.Exists(eBibleCertified))
            {
                m_options.textSourceUrl = "https://eBible.org/Scriptures/";
            }
            else
            {
                m_options.textSourceUrl = "";
            }
            toMosis.languageCode = m_options.languageId;
            toMosis.translationId = m_options.translationId;
            toMosis.revisionDateTime = m_options.contentUpdateDate;
            toMosis.vernacularTitle = m_options.vernacularTitle;
            toMosis.contentCreator = m_options.contentCreator;
            toMosis.contentContributor = m_options.contributor;
            toMosis.englishDescription = m_options.EnglishDescription;
            toMosis.lwcDescription = m_options.lwcDescription;
            toMosis.printPublisher = m_options.printPublisher;
            toMosis.ePublisher = m_options.electronicPublisher;
            toMosis.languageName = m_options.languageNameInEnglish;
            toMosis.dialect = m_options.dialect;
            toMosis.vernacularLanguageName = m_options.languageName;
            toMosis.projectOptions = m_options;
            toMosis.swordDir = Path.Combine(dataRootDir, "sword");
            toMosis.swordRestricted = Path.Combine(dataRootDir, "swordRestricted");
            toMosis.copyrightNotice = m_options.publicDomain ? "public domain" : "Copyright © " + m_options.copyrightYears + " " + m_options.copyrightOwner;
            if (m_options.publicDomain)
            {
                toMosis.rightsNotice = @"This work is in the Public Domain. That means that it is not copyrighted.
 It is still subject to God's Law concerning His Word, including the Great Commission (Matthew 28:18-20).
";
            }
            else if (m_options.ccbyndnc)
            {
                toMosis.rightsNotice = @"This Bible translation is made available to you under the terms of the
 Creative Commons Attribution-Noncommercial-No Derivative Works license (http://creativecommons.org/licenses/by-nc-nd/4.0/).";
            }
            else if (m_options.ccbysa)
            {
                toMosis.rightsNotice = @"This Bible translation is made available to you under the terms of the
 Creative Commons Attribution-Share-Alike license (http://creativecommons.org/licenses/by-sa/4.0/).";
            }
            else if (m_options.ccbynd)
            {
                toMosis.rightsNotice = @"This Bible translation is made available to you under the terms of the
 Creative Commons Attribution-No Derivatives license (http://creativecommons.org/licenses/by-na/4.0/).";
            }
            else
            {
                toMosis.rightsNotice = String.Empty;
            }
            if (m_options.rightsStatement.Length > 0)
            {
                toMosis.rightsNotice += m_options.rightsStatement;
            }
            toMosis.infoPage = copyrightPermissionsStatement();
            string logFile = Path.Combine(m_outputProjectDirectory, "MosisConversionReport.txt");
            Logit.OpenFile(logFile);
            Logit.GUIWriteString = showMessageString;
            toMosis.langCodes = languageCodes;
            toMosis.ConvertUsfxToMosis(usfxFilePath, mosisFilePath);
            Logit.CloseFile();
            if (Logit.loggedError)
            {
                m_options.lastRunResult = false;
            }
        }

        private void PrepareSearchText()
        {
            string logFile = Path.Combine(m_outputProjectDirectory, "SearchReport.txt");
            Logit.OpenFile(logFile);
            try
            {
                ExtractSearchText est = new ExtractSearchText();
                string vplPath = Path.Combine(m_outputProjectDirectory, "vpl");
                string UsfxPath = Path.Combine(m_outputProjectDirectory, "usfx");
                string auxPath = Path.Combine(m_outputProjectDirectory, "search");
                string verseText = Path.Combine(auxPath, "verseText.xml");
                // string sqlFile = Path.Combine(m_outputDirectory, "MySQL");
                string sqlFile = Path.Combine(m_outputProjectDirectory, "sql");
                Logit.GUIWriteString = showMessageString;
                Logit.UpdateStatus = updateConversionProgress;
                Utils.EnsureDirectory(auxPath);
                Utils.EnsureDirectory(sqlFile);
                est.Filter(Path.Combine(UsfxPath, "usfx.xml"), verseText);
                est.WriteSearchSql(verseText, m_project, Path.Combine(sqlFile, m_project + "_vpl.sql"));
                est.WriteSearchSql(Path.ChangeExtension(verseText, ".lemma"), m_project, Path.Combine(sqlFile, m_project + "_lemma.sql"));
                if (est.LongestWordLength > m_options.longestWordLength)
                    m_options.longestWordLength = est.LongestWordLength;
                // Copy search text files to VPL output.
                Utils.DeleteDirectory(vplPath);
                Utils.EnsureDirectory(vplPath);
                File.Copy(verseText, Path.Combine(vplPath, m_project + "_vpl.xml"));
                File.Copy(Path.Combine(auxPath, "verseText.vpltxt"), Path.Combine(vplPath, m_project + "_vpl.txt"));
                File.Copy(Path.Combine(m_inputDirectory, "haiola.css"), Path.Combine(vplPath, "haiola.css"));
                StreamWriter htm = new StreamWriter(Path.Combine(vplPath, m_project + "_about.htm"));
                htm.WriteLine("<!DOCTYPE html>");
                htm.WriteLine("<html>");
                htm.WriteLine("<head>");
                htm.WriteLine("<meta charset=\"UTF-8\" />");
                htm.WriteLine("<link rel=\"stylesheet\" href=\"haiola.css\" type=\"text/css\" />");
                htm.WriteLine("<meta name=\"viewport\" content=\"user-scalable=yes, initial-scale=1, minimum-scale=1, width=device-width\"/>");
                htm.WriteLine("<title>About {0}_vpl</title>", m_project);
                htm.WriteLine("</head>");
                htm.WriteLine("<body class=\"mainDoc\"");
                htm.WriteLine("<p>This archive contains BIBLE TEXT ONLY. All formatting, paragraph breaks, notes, introductions, noncanonical section titles, etc., have been removed. The file ending \"_vpl.txt\" is designed for import into BibleWorks and similar Bible study programs. The file ending \"_vpl.xml\" contains the same information, but is in XML format and uses standard SIL/UBS book abbreviations.");
                htm.WriteLine("The file ending \"_vpl.sql\" contains the same information formatted to create a SQL data table.</p>");
                htm.WriteLine(@"<p>Check for updates and other Bible translations in this format at <a href='https:\\Bible.cx\Scriptures\'>https:\\Bible.cx\Scriptures\</a> or <a href='ftp:\\eBible.org\pub\Scriptures\'>ftp:\\eBible.org\pub\Scriptures\</a></p>");
                htm.WriteLine("<hr />");
                htm.WriteLine(copyrightPermissionsStatement());
                htm.WriteLine("</body></html>");
                htm.Close();
                if (Logit.loggedError)
                {
                    m_options.lastRunResult = false;
                }
            }
            catch (Exception ex)
            {
                Logit.WriteError("Error preparing search text: " + ex.Message);
            }
            Logit.CloseFile();
        }

        protected string orderFile;
        private string eBibleCertified = @"/home/kahunapule/sync/doc/Electronic Scripture Publishing/eBible.org_certified.jpg";
        public string certified = null;
        public string preferredCover;

        /// <summary>
        /// Take the project input (exactly one of USFM, USFX, or USX) and create
        /// the distribution formats we need.
        /// </summary>
        /// <param name="projDirName">project input directory</param>
        private void ProcessOneProject(string projDirName)
        {
            Logit.GUIWriteString = showMessageString;
            SetCurrentProject(projDirName);
        	m_xiniPath = Path.Combine(m_inputProjectDirectory, "options.xini");
            displayOptions();
            if (m_options.done || !fileHelper.lockProject(m_inputProjectDirectory))
            {
                return;
            }
            logProjectStart("Processing " + m_options.translationId + " in " + m_inputProjectDirectory);
            Application.DoEvents();
            if (!fileHelper.fAllRunning)
            {
                fileHelper.unlockProject();
                return;
            }

            Utils.DeleteDirectory(Path.Combine(m_outputProjectDirectory, "search"));
            Utils.DeleteDirectory(Path.Combine(m_outputProjectDirectory, "usfm1"));
            Utils.DeleteDirectory(Path.Combine(m_outputProjectDirectory, "sfm"));
            Utils.DeleteDirectory(Path.Combine(m_outputProjectDirectory, "extendedusfm"));
            Utils.DeleteDirectory(Path.Combine(m_outputProjectDirectory, "usfm"));
            Utils.DeleteDirectory(Path.Combine(m_outputProjectDirectory, "usfx"));


            if (m_options.PrepublicationChecks &&
                (m_options.publicDomain || m_options.redistributable || File.Exists(Path.Combine(m_inputProjectDirectory, "certify.txt"))) &&
                File.Exists(eBibleCertified))
            {
                certified = eBibleCertified;
                m_options.eBibleCertified = true;
            }
            else
            {
                certified = null;
                m_options.eBibleCertified = false;
            }
            m_options.rebuild = RebuildCheckBox.Checked;
            m_options.Write();

            // Find out what kind of input we have (USFX, USFM, or USX)
            // and produce USFX, USFM, (and in the future) USX outputs.

            orderFile = Path.Combine(m_inputProjectDirectory, "bookorder.txt");
            if (!File.Exists(orderFile))
                orderFile = SFConverter.FindAuxFile("bookorder.txt");
            StreamReader sr = new StreamReader(orderFile);
            m_options.allowedBookList = sr.ReadToEnd();
            sr.Close();



            if (!GetUsfx(projDirName))
            {
                Logit.WriteError("No source directory found for " + projDirName + "!");
                fileHelper.unlockProject();
                return;
            }
            Utils.DeleteDirectory(Path.Combine(m_outputProjectDirectory, "sql"));

            UpdateBooksList();
            Application.DoEvents();
            preferredCover = CreateCover();
            Application.DoEvents();
            // Create verseText.xml with unformatted canonical text only in verse containers.
            if (fileHelper.fAllRunning)
                PrepareSearchText();
            Application.DoEvents();
            // Create epub file
            string epubDir = Path.Combine(m_outputProjectDirectory, "epub");
            if (fileHelper.fAllRunning && m_options.makeEub && (m_options.rebuild || m_options.SourceFileDate > Directory.GetCreationTime(epubDir)))
            {
                Utils.DeleteDirectory(epubDir);
                ConvertUsfxToEPub();
            }
            Application.DoEvents();
            // Create HTML output for posting on web sites.
            string htmlDir = Path.Combine(m_outputProjectDirectory, "html");
            if (fileHelper.fAllRunning && m_options.makeHtml && (m_options.rebuild || m_options.SourceFileDate > Directory.GetCreationTime(htmlDir)))
            {
                Utils.DeleteDirectory(htmlDir);
                ConvertUsfxToPortableHtml();
            }
            Application.DoEvents();
            string WordMLDir = Path.Combine(m_outputProjectDirectory, "WordML");
            if (fileHelper.fAllRunning && m_options.makeWordML && (m_options.rebuild || m_options.SourceFileDate > Directory.GetCreationTime(WordMLDir)))
            {   // Write out WordML document
                // Note: this conversion departs from the standard architecture of making the USFX file the hub, because the WordML writer code was already done in WordSend,
                // and expected USFM input. Therefore, we read the normalized USFM files, which should be present even if the project input is USFX or USX.
                // If this code needs much maintenance in the future, it may be better to refactor the WordML output to go from USFX to WordML directly.
                // Then again, USFX to Open Document Text would be better.
                try
                {
                    Utils.DeleteDirectory(WordMLDir);
                    currentConversion = "Reading normalized USFM";
                    string logFile = Path.Combine(m_outputProjectDirectory, "WordMLConversionReport.txt");
                    Logit.OpenFile(logFile);
                    SFConverter.scripture = new Scriptures(m_options);
                    string seedFile = Path.Combine(m_inputProjectDirectory, "Scripture.xml");
                    if (!File.Exists(seedFile))
                    {
                        seedFile = Path.Combine(m_inputDirectory, "Scripture.xml");
                    }
                    if (!File.Exists(seedFile))
                    {
                        seedFile = SFConverter.FindAuxFile("Scripture.xml");
                    }
                    SFConverter.scripture.templateName = seedFile;
                    SFConverter.ProcessFilespec(Path.Combine(Path.Combine(m_outputProjectDirectory, "usfm"), "*.usfm"));
                    currentConversion = "Writing WordML";
                    Utils.EnsureDirectory(WordMLDir);
                    SFConverter.scripture.WriteToWordML(Path.Combine(WordMLDir, m_options.translationId + "_word.xml"));
                }
                catch (Exception ex)
                {

                    Logit.WriteError("Error writing WordML file: " + ex.Message);
                    Logit.WriteError(ex.StackTrace);
                    m_options.makeWordML = false;
                }
                makeWordMLCheckBox.Checked = m_options.makeWordML;
                Logit.CloseFile();
            }
            Application.DoEvents();
            // Create Modified OSIS output for conversion to Sword format.
            string mosisDir = Path.Combine(m_outputProjectDirectory, "mosis");
            if (fileHelper.fAllRunning && m_options.makeSword && (m_options.rebuild || (m_options.SourceFileDate > m_options.SwordVersionDate)))
            {
                Utils.DeleteDirectory(mosisDir);
                ConvertUsfxToMosis();
            }
            Application.DoEvents();
            string xetexDir = Path.Combine(m_outputProjectDirectory, "xetex");
            if (fileHelper.fAllRunning && m_options.makePDF /* && (m_options.rebuild || (m_options.SourceFileDate > Directory.GetCreationTime(xetexDir)))*/)
            {
                ConvertUsfxToPDF(xetexDir);
            }
            Application.DoEvents();
            // Run proprietary extension conversions, if any.
            string inscriptDir = Path.Combine(Path.Combine(dataRootDir, "inscript"), m_options.fcbhId);
            DateTime inscriptCreated = Directory.GetCreationTime(inscriptDir);
            if (fileHelper.fAllRunning && m_options.makeInScript && (m_options.rebuild || m_options.SourceFileDate > inscriptCreated))
            {
                Utils.DeleteDirectory(inscriptDir);
                plugin.DoProprietaryConversions();
            }
            Application.DoEvents();
            // Run custom per project scripts.
            if (fileHelper.fAllRunning)
            {
                DoPostprocess();
                m_options.done = true;
                m_options.Write();
            }
            fileHelper.unlockProject();
            Application.DoEvents();
        }

    	private void SetCurrentProject(string projDirName)
    	{
    		m_project = projDirName;
    		m_inputProjectDirectory = Path.Combine(m_inputDirectory, m_project);
    		m_outputProjectDirectory = Path.Combine(m_outputDirectory, m_project);
    		fileHelper.EnsureDirectory(m_outputProjectDirectory);
    	}

        private string FindSource(string projDirName)
        {
            SetCurrentProject(projDirName);
            string source;
            string result = string.Empty;
            if (!String.IsNullOrEmpty((string)paratextcomboBox.SelectedItem))
            {
                source = Path.Combine(paratextProjectsDir, (string)paratextcomboBox.SelectedItem);
                if (Directory.Exists(source))
                {
                    return source;
                }
            }
            source = Path.Combine(m_inputProjectDirectory, "Source");
            if (Directory.Exists(source))
            {
                return source;
            }
            else
            {
                source = Path.Combine(m_inputProjectDirectory, "usfx");
                if (Directory.Exists(source))
                {
                    return source;
                }
                else
                {
                    source = Path.Combine(m_inputProjectDirectory, "usx");
                    if (Directory.Exists(source))
                    {
                        return source;
                    }
                }
            }
            return result;
        }

    	private bool GetUsfx(string projDirName)
    	{
			SetCurrentProject(projDirName);
			string source;
            if (!String.IsNullOrEmpty((string)paratextcomboBox.SelectedItem))
            {
                source = Path.Combine(paratextProjectsDir, (string)paratextcomboBox.SelectedItem);
                if (Directory.Exists(source))
                {
                    PreprocessUsfmFiles(source);
                    Application.DoEvents();
                    if (fileHelper.fAllRunning)
                    {
                        ConvertUsfmToUsfx();
                        NormalizeUsfm();
                    }
                    return true;
                }
                else
                {
                    Logit.WriteError("Paratext project directory " + source + " not found!");
                }
            }
            source = Path.Combine(m_inputProjectDirectory, "Source");
            if (Directory.Exists(source))
            {
                PreprocessUsfmFiles(source);
                Application.DoEvents();
                if (fileHelper.fAllRunning)
                {
                    ConvertUsfmToUsfx();
                    NormalizeUsfm();
                }
            }
            else
            {
                source = Path.Combine(m_inputProjectDirectory, "usfx");
                if (Directory.Exists(source))
                {
                    ImportUsfx(source);
                    NormalizeUsfm();
                }
                else
                {
                    source = Path.Combine(m_inputProjectDirectory, "usx");
                    if (Directory.Exists(source))
                    {
                        ImportUsx(source);
                        string metadataXml = Path.Combine(source, "metadata.xml");
                        if (File.Exists(metadataXml))
                        {
                            DateTime fileDate = File.GetLastWriteTimeUtc(metadataXml);
                            if (fileDate > sourceDate)
                            {
                                sourceDate = fileDate;
                                m_options.SourceFileDate = sourceDate;
                            }
                        }

                    }
                    else
                    {
                        return false;
                    }
                }
            }
            return true;
    	}

        ArrayList toDoList = new ArrayList();
        ArrayList laterList = new ArrayList();
        private class projectEntry
        {
            public string name;
            public string depends;

            /// <summary>
            /// Initialize this projectEntry Instance with the given name and the name of the project this project depends on.
            /// </summary>
            /// <param name="n">This project's name</param>
            /// <param name="d">Name of the project this project depends on, if any.</param>
            public projectEntry(string n, string d)
            {
                name = n;
                depends = d;
            }
        }

        private void insertProjectEntry(projectEntry pe)
        {
            int i;
            if (string.IsNullOrEmpty(pe.depends))
            {
                toDoList.Add(pe);
            }
            else
            {
                bool notFound = true;
                i = 0;
                while ((i < toDoList.Count) && notFound)
                {
                    if (pe.depends == ((projectEntry)toDoList[i]).name)
                    {
                        notFound = false;
                        toDoList.Insert(i + 1, pe);
                    }
                    i++;
                }
                if (notFound)
                {
                    laterList.Add(pe);
                }
            }
        }

        public Ethnologue eth;


        private void processAllMarked()
        {
            toDoList = new ArrayList();
            laterList = new ArrayList();
            int i;
            int numdone = 0;
            int nummarked = 0;
            TimeSpan ts, remainingTime;
            string command = Path.Combine(m_inputDirectory, "postprocess.bat");

            if (fileHelper.fAllRunning)
            {
                fileHelper.fAllRunning = false;
                WorkOnAllButton.Enabled = false;
                WorkOnAllButton.Text = "Stopping...";
                Application.DoEvents();
                return;
            }
            fileHelper.fAllRunning = true;
            btnSetRootDirectory.Enabled = false;
            reloadButton.Enabled = false;
            m_projectsList.Enabled = false;
            markRetryButton.Enabled = false;
            unmarkAllButton.Enabled = false;
            runHighlightedButton.Enabled = false;
            messagesListBox.Items.Clear();
            messagesListBox.BackColor = Color.LightGreen;
            tabControl1.SelectedTab = messagesTabPage;
            BackColor = Color.LightGreen;
            startTime = DateTime.UtcNow;
            WorkOnAllButton.Text = "Stop";
            Application.DoEvents();
            timer1.Enabled = true;
            //SaveOptions();


            fcbhIds = null;
            if (getFCBHkeys)
                GetFcbhIds();


            foreach (object o in m_projectsList.CheckedItems)
            {
                SetCurrentProject((string)o);
                m_xiniPath = Path.Combine(m_inputProjectDirectory, "options.xini");
                displayOptions();
                if (!m_options.done)
                {
                    insertProjectEntry(new projectEntry((string)o, m_options.dependsOn));
                    nummarked++;
                }
            }
            i = laterList.Count * 4;
            while ((laterList.Count > 0) && (i >= 0))
            {
                projectEntry pe = (projectEntry)laterList[0];
                laterList.RemoveAt(0);
                insertProjectEntry(pe);
                nummarked++;
                i--;
            }
            foreach (projectEntry pe in laterList)
            {   // Dependencies not selected, so do these anyway.
                toDoList.Add(pe);
                nummarked++;
            }
            foreach (projectEntry pe in toDoList)
            {
                ProcessOneProject(pe.name);
                numdone++;
                ts = DateTime.UtcNow - startTime;
                double secondsLeft = (nummarked - numdone) * (ts.TotalSeconds / numdone);
                remainingTime = new TimeSpan(0, 0, 0, (int)secondsLeft);
                statsLabel.Text = (ts.TotalSeconds / numdone).ToString("N0") + " seconds per project. " + numdone.ToString() + " of " + nummarked.ToString() +
                    " projects done; " + remainingTime.ToString("c") + " remaining.";

                Application.DoEvents();
                if (!fileHelper.fAllRunning)
                    break;
            }
            Application.DoEvents();
            if (fileHelper.fAllRunning && File.Exists(command))
            {
                currentConversion = "Running " + command;
                batchLabel.Text = currentConversion;
                Application.DoEvents();
                if (!fileHelper.RunCommand(command))
                    MessageBox.Show(fileHelper.runCommandError, "Error " + currentConversion);
            }
            currentConversion = String.Empty;

            fileHelper.fAllRunning = false;
            batchLabel.Text = (DateTime.UtcNow - startTime).ToString(@"g") + " " + "Done.";
            messagesListBox.Items.Add(batchLabel.Text);
            m_projectsList_SelectedIndexChanged(null, null);
            WorkOnAllButton.Enabled = true;
            WorkOnAllButton.Text = "Run marked";
            m_projectsList.Enabled = true;
            markRetryButton.Enabled = true;
            unmarkAllButton.Enabled = true;
            btnSetRootDirectory.Enabled = true;
            reloadButton.Enabled = true;
            runHighlightedButton.Enabled = true;
            if (Program.autorun)
            {
                Close();
            }
            else
            {
                string index = Path.Combine(Path.Combine(m_outputProjectDirectory, "html"), "index.htm");
                if (File.Exists(index))
                    System.Diagnostics.Process.Start(index);
            }

        }


        /// <summary>
        /// Handler for the button press to process all marked projects.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">Event Arguments</param>
    	private void WorkOnAllButton_Click(object sender, EventArgs e)
        {
            string lockFile;
            if (fileHelper.fAllRunning)
            {
                fileHelper.fAllRunning = false;
            }
            else
            {
                WorkOnAllButton.Text = "Stop";
                Application.DoEvents();
                SaveOptions();

                foreach (object o in m_projectsList.CheckedItems)
                {
                    SetCurrentProject((string)o);
                    m_xiniPath = Path.Combine(m_inputProjectDirectory, "options.xini");
                    displayOptions();
                    lockFile = Path.Combine(m_inputProjectDirectory, "lock");
                    if (File.Exists(lockFile))
                        File.Delete(lockFile);
                    m_options.done = false;
                    m_options.Write();
                }


                processAllMarked();
            }
        }

        /// <summary>
        /// Reload working directory and mark all ready projects for running.
        /// </summary>
        /// <param name="sender">form</param>
        /// <param name="e">button event parameters</param>
        private void reloadButton_Click(object sender, EventArgs e)
        {
            SaveOptions();
            LoadWorkingDirectory(true, false, false);
        }

        /*
        private void checkAllButton_Click(object sender, EventArgs e)
        {
            int i;
            SaveOptions();
            for (i = 0; i < m_projectsList.Items.Count; i++)
                m_projectsList.SetItemChecked(i, true);

        }
        */ 

        private void unmarkAllButton_Click(object sender, EventArgs e)
        {
            SaveOptions();
            LoadWorkingDirectory(false, false, true);
        }


        private XmlFileReader metadataXml;

        /// <summary>
        /// Reads the next string (element contents) from the DBLMetadata XML file
        /// </summary>
        /// <returns>decoded string</returns>
        private string MetadataText()
        {
            string result = String.Empty;
            if ((!metadataXml.IsEmptyElement) && metadataXml.Read())
            {
                if (metadataXml.NodeType == XmlNodeType.Text)
                {
                    result = System.Web.HttpUtility.HtmlDecode(metadataXml.Value);
                }
            }
            return result;
        }

        /// <summary>
        /// Reads the given DMLMetadata XML file and displays the found values.
        /// </summary>
        /// <param name="fileName">Full path and file name of the DBLMetadata file, usually ending in usx/metadata.xml</param>
        /// <returs>true iff some metdata was found and read</returs>
        public bool ReadMetadata(string fileName)
        {
            bool result = false;
            bool defaultBookList = false;
            BibleBookInfo BookInfo = new WordSend.BibleBookInfo();
            BibleBookRecord br = new BibleBookRecord();
            BibleBookRecord bkRec;
            string bookCode;
            string nodePath;
            try 
        	{	        
                if (File.Exists(fileName))
                {
                    metadataXml = new XmlFileReader(fileName);
                    metadataXml.MoveToContent();
                    if ((metadataXml.NodeType == XmlNodeType.Element) && (metadataXml.Name == "DBLMetadata"))
                    {   // This is the file we are looking for
                        result = true;
                        while (metadataXml.Read())
                        {
                            if (metadataXml.NodeType == XmlNodeType.Element)
                            {
                                nodePath = metadataXml.NodePath().Substring(13);
                                nodePath = nodePath.Substring(0, nodePath.Length - 1);
                                switch (nodePath)
                                {
                                    case "identification/name":
                                        if (Utils.IsEmpty(titleTextBox.Text))
                                            titleTextBox.Text = MetadataText();
                                        break;
                                    case "identification/nameLocal":
                                        titleTextBox.Text = MetadataText();
                                        break;
                                    case "identification/abbreviation":
                                        if (Utils.IsEmpty(traditionalAbbreviationTextBox.Text))
                                            traditionalAbbreviationTextBox.Text = MetadataText();
                                        break;
                                    case "identification/abbreviationLocal":
                                        traditionalAbbreviationTextBox.Text = MetadataText();
                                        break;
                                    case "identification/description":
                                        if (Utils.IsEmpty(descriptionTextBox.Text))
                                            descriptionTextBox.Text = MetadataText();
                                        break;
                                    case "identification/dateCompleted":
                                        if (Utils.IsEmpty(copyrightYearTextBox.Text))
                                            copyrightYearTextBox.Text = MetadataText();
                                        break;
                                    case "confidential":
                                        // privateCheckBox.Checked = MetadataText() == "true";
                                        break;
                                    case "contents/bookList":
                                        defaultBookList = metadataXml.GetAttribute("default") == "true";
                                        break;
                                    case "contents/bookList/description":
                                        if (defaultBookList)
                                            m_options.canonTypeEnglish = MetadataText();
                                        break;
                                    case "contents/bookList/descriptionLocal":
                                        if (defaultBookList)
                                            m_options.canonTypeLocal = MetadataText();
                                        break;
                                    case "agencies/creator":
                                        creatorTextBox.Text = MetadataText();
                                        break;
                                    case "agencies/rightsHolder":
                                        coprAbbrevTextBox.Text = metadataXml.GetAttribute("abbr");
                                        localRightsHolderTextBox.Text = metadataXml.GetAttribute("local");
                                        copyrightOwnerUrlTextBox.Text = metadataXml.GetAttribute("url");
                                        if (Utils.IsEmpty(copyrightOwnerTextBox.Text))
                                            copyrightOwnerTextBox.Text = MetadataText();
                                        break;
                                    case "agencies/publisher":
                                        printPublisherTextBox.Text = MetadataText();
                                        break;
                                    case "agencies/contributor":
                                        contributorTextBox.Text = MetadataText();
                                        break;
                                    case "language/iso":
                                        ethnologueCodeTextBox.Text = MetadataText();
                                        break;
                                    case "language/name":
                                        if (Utils.IsEmpty(languageNameTextBox.Text) && Utils.IsEmpty(engLangNameTextBox.Text))
                                            languageNameTextBox.Text = engLangNameTextBox.Text = MetadataText();
                                        break;
                                    case "language/ldml":
                                        ldmlTextBox.Text = MetadataText();
                                        break;
                                    case "language/rod":
                                        if (Utils.IsEmpty(rodCodeTextBox.Text))
                                            rodCodeTextBox.Text = MetadataText();
                                        break;
                                    case "language/script":
                                        scriptTextBox.Text = MetadataText();
                                        break;
                                    case "languge/scriptDirection":
                                        textDirectionComboBox.Text = MetadataText().ToLowerInvariant();
                                        break;
                                    case "language/numerals":
                                        string numeralSystem = MetadataText().Trim();
                                        if ((numeralSystem.ToLowerInvariant() == "arabic") || (numeralSystem == String.Empty))
                                            numeralSystem = "Hindu-Arabic";
                                        numberSystemComboBox.Text = numeralSystem;
                                        break;
                                    case "country/iso":
                                        if (countryCodeTextBox.Text.Trim().Length != 2)
                                            countryCodeTextBox.Text = MetadataText();
                                        break;
                                    case "country/name":
                                        countryTextBox.Text = MetadataText();
                                        break;
                                    case "contact/rightsHolder":
                                        if (Utils.IsEmpty(copyrightOwnerTextBox.Text))
                                            copyrightOwnerTextBox.Text = MetadataText();
                                        break;
                                    case "contact/rightsHolderLocal":
                                        localRightsHolderTextBox.Text = MetadataText();
                                        break;
                                    case "contact/rightsHolderAbbreviation":
                                        coprAbbrevTextBox.Text = MetadataText();
                                        break;
                                    case "contact/rightsHolderURL":
                                        copyrightOwnerUrlTextBox.Text = MetadataText();
                                        break;
                                    case "contact/rightsHolderFacebook":
                                        facebookTextBox.Text = MetadataText();
                                        break;
                                    case "copyright/statement":
                                        if (Utils.IsEmpty(rightsStatementTextBox.Text))
                                            rightsStatementTextBox.Text = MetadataText().Replace("<p>", " ").Replace("</p>", " ").Trim();
                                        break;
                                    case "promotion/promoVersionInfo":
                                        StringBuilder sb = new StringBuilder();
                                        bool reading = true;
                                        if (!metadataXml.IsEmptyElement)
                                        {
                                            while (reading && metadataXml.Read())
                                            if ((metadataXml.NodeType == XmlNodeType.Text) || (metadataXml.NodeType == XmlNodeType.SignificantWhitespace) ||
                                                (metadataXml.NodeType == XmlNodeType.Whitespace))
                                            {
                                                sb.Append(metadataXml.Value);
                                            }
                                            else if (metadataXml.NodeType == XmlNodeType.Element)
                                            {
                                                if (metadataXml.IsEmptyElement)
                                                    sb.Append("<" + metadataXml.Name + "/>");
                                                else
                                                    sb.Append("<" + metadataXml.Name + ">");
                                            }
                                            else if (metadataXml.NodeType == XmlNodeType.EndElement)
                                            {
                                                if (metadataXml.Name == "promoVersionInfo")
                                                {
                                                    reading = false;
                                                }
                                                else
                                                {
                                                    sb.Append("</" + metadataXml.Name + ">");
                                                }
                                            }
                                        }
                                        promoTextBox.Text = sb.ToString();
                                        if (promoTextBox.Text.Contains("Creative Commons License"))
                                            ccRadioButton.Checked = true;
                                        break;
                                    case "bookNames/book":
                                        bookCode = metadataXml.GetAttribute("code");
                                        bkRec = (BibleBookRecord)BookInfo.books[bookCode];
                                        if (bkRec != null)
                                            br = bkRec;
                                        else
                                            MessageBox.Show("Bad book code in metadata.xml: "+bookCode,"Error reading " + fileName);
                                        break;
                                    case "bookNames/book/long":
                                        br.vernacularLongName = MetadataText();
                                        break;
                                    case "bookNames/book/short":
                                        br.vernacularShortName = MetadataText();
                                        break;
                                    case "bookNames/book/abbr":
                                        br.vernacularAbbreviation = MetadataText();
                                        break;

                                }
                            }
                        }
                        BookInfo.WriteDefaultBookNames(Path.Combine(m_outputProjectDirectory, "BookNames.xml"));
                    }
                }
            }
	        catch (Exception ex)
	        {
                MessageBox.Show(ex.Message, "Error trying to read " + fileName);
                result = false;
        	}
            m_options.Write();
            return result;
        }

        /// <summary>
        /// Read options from the correct .xini file and display them.
        /// </summary>
        private void displayOptions()
        {
            if (m_options == null)
            {
                m_options = new Options(m_xiniPath);
            }
            else
            {
                m_options.Reload(m_xiniPath);
            }
            if (m_options.languageId.Length == 3)
            {
                ethnorecord er = eth.ReadEthnologue(m_options.languageId);
                if (m_options.country.Length == 0)
                    m_options.country = er.countryName;
                if (m_options.countryCode.Length == 0)
                    m_options.countryCode = er.countryId;
                if (m_options.languageNameInEnglish.Length == 0)
                    m_options.languageNameInEnglish = er.langName;
            }
            SFConverter.jobIni = m_options.ini;
            ethnologueCodeTextBox.Text = m_options.languageId;
            translationIdTextBox.Text = m_project; // This was m_options.translationId, but now we force short translation ID and input directory name to match.
            traditionalAbbreviationTextBox.Text = m_options.translationTraditionalAbbreviation;
            languageNameTextBox.Text = m_options.languageName;
            engLangNameTextBox.Text = m_options.languageNameInEnglish;
            dialectTextBox.Text = m_options.dialect;
            creatorTextBox.Text = m_options.contentCreator;
            contributorTextBox.Text = m_options.contributor;
            titleTextBox.Text = m_options.vernacularTitle;
            descriptionTextBox.Text = m_options.EnglishDescription;
            lwcDescriptionTextBox.Text = m_options.lwcDescription;
            updateDateTimePicker.MaxDate = DateTime.Now.AddDays(2);
            updateDateTimePicker.Value = m_options.contentUpdateDate;
            privateCheckBox.Checked = m_options.privateProject;
            pdRadioButton.Checked = m_options.publicDomain;
            ccRadioButton.Checked = m_options.ccbyndnc;
            CCBySaRadioButton.Checked = m_options.ccbysa;
            CCByNdRadioButton.Checked = m_options.ccbynd;
            otherRadioButton.Checked = m_options.otherLicense;
            allRightsRadioButton.Checked = m_options.allRightsReserved;
            silentRadioButton.Checked = m_options.silentCopyright;
            copyrightOwnerTextBox.Text = m_options.copyrightOwner;
            copyrightOwnerUrlTextBox.Text = m_options.copyrightOwnerUrl;
            copyrightYearTextBox.Text = m_options.copyrightYears;
            coprAbbrevTextBox.Text = m_options.copyrightOwnerAbbrev;
            rightsStatementTextBox.Text = m_options.rightsStatement;
            printPublisherTextBox.Text = m_options.printPublisher;
            electronicPublisherTextBox.Text = m_options.electronicPublisher;
            stripExtrasCheckBox.Checked = m_options.ignoreExtras;
            xoTextBox.Text = m_options.xoFormat;
            customCssTextBox.Text = m_options.customCssFileName;
            stripOriginCheckBox.Checked = m_options.stripNoteOrigin;
            prepublicationChecksCheckBox.Checked = m_options.PrepublicationChecks;
            webSiteReadyCheckBox.Checked = m_options.WebSiteReady;
            e10dblCheckBox.Checked = m_options.ETENDBL;
            archivedCheckBox.Checked = m_options.Archived;
            subsetCheckBox.Checked = m_options.subsetProject;
            paratextcomboBox.SelectedItem = m_options.paratextProject;
            audioRecordingCopyrightTextBox.Text = m_options.AudioCopyrightNotice;
            rodCodeTextBox.Text = m_options.rodCode;
            ldmlTextBox.Text = m_options.ldml;
            scriptTextBox.Text = m_options.script;
            localRightsHolderTextBox.Text = m_options.localRightsHolder;
            facebookTextBox.Text = m_options.facebook;
            countryTextBox.Text = m_options.country;
            countryCodeTextBox.Text = m_options.countryCode;
            extendUsfmCheckBox.Checked = m_options.extendUsfm;
            chapterLabelTextBox.Text = m_options.chapterLabel;
            psalmLabelTextBox.Text = m_options.psalmLabel;
            cropCheckBox.Checked = m_options.includeCropMarks;
            chapter1CheckBox.Checked = m_options.chapter1;
            verse1CheckBox.Checked = m_options.verse1;
            pageWidthTextBox.Text = m_options.pageWidth;
            pageLengthTextBox.Text = m_options.pageLength;
            regenerateNoteOriginsCheckBox.Checked = m_options.RegenerateNoteOrigins;
            cvSeparatorTextBox.Text = m_options.CVSeparator;
            downloadsAllowedCheckBox.Checked = m_options.downloadsAllowed;
            if ((m_options.SwordName.Length < 1) && (m_options.translationId.Length > 1))
            {
                m_options.SwordName = m_options.translationId.Replace("-", "").Replace("_", "");
                if ((m_options.copyrightYears.Length >= 4) && !Char.IsDigit(m_options.SwordName[m_options.SwordName.Length - 1]))
                    m_options.SwordName += m_options.copyrightYears.Substring(m_options.copyrightYears.Length - 4);
            }
            if ((m_options.SwordName.Length > 0) && !m_options.SwordName.EndsWith(m_swordSuffix))
            {
                m_options.SwordName += m_swordSuffix;
            }
            swordNameTextBox.Text = m_options.SwordName;
            oldSwordIdTextBox.Text = m_options.ObsoleteSwordName;
            RebuildCheckBox.Checked = m_options.rebuild = xini.ReadBool("rebuild", false);
            runXetexCheckBox.Checked = m_options.rebuild = xini.ReadBool("runXetex", false);
            makeInScriptCheckBox.Checked = m_options.makeInScript;
            makeEPubCheckBox.Checked = m_options.makeEub;
            makeHtmlCheckBox.Checked = m_options.makeHtml;
            makePDFCheckBox.Checked = m_options.makePDF;
            makeSwordCheckBox.Checked = m_options.makeSword;
            makeWordMLCheckBox.Checked = m_options.makeWordML;
            disablePrintingFigoriginsCheckBox.Checked = m_options.disablePrintingFigOrigins;
            apocryphaCheckBox.Checked = m_options.includeApocrypha;
            if (m_options.DBSandeBible)
                recheckedCheckBox.Visible = true;
            recheckedCheckBox.Checked = m_options.rechecked;


            if ((m_options.fcbhId == String.Empty) && (fcbhDbsIds != null))
            {
                m_options.fcbhId = (string)fcbhDbsIds[m_project];
                if (m_options.fcbhId == null)
                    m_options.fcbhId = String.Empty;
            }
            fcbhIdTextBox.Text = m_options.fcbhId;
            shortTitleTextBox.Text = m_options.shortTitle;
            if (shortTitleTextBox.Text.Length < 1)
                shortTitleTextBox.Text = m_options.EnglishDescription;
                        
            m_currentTemplate = xini.ReadString("currentTemplate", String.Empty);
            templateLabel.Text = "Current template: " + m_currentTemplate;
            copyFromTemplateButton.Enabled = (m_currentTemplate.Length > 0) && (m_currentTemplate != m_project);
            makeTemplateButton.Enabled = m_currentTemplate != m_project;
            if (!fileHelper.fAllRunning)
            {
                if (m_options.lastRunResult)
                    BackColor = Color.LightGreen;
                else
                    BackColor = Color.LightPink;
            }

            listInputProcesses.SuspendLayout();
            listInputProcesses.Items.Clear();
            foreach (string filename in m_options.preprocessingTables)
                listInputProcesses.Items.Add(filename);
            listInputProcesses.ResumeLayout();

            postprocessListBox.SuspendLayout();
            postprocessListBox.Items.Clear();
            foreach (string filename in m_options.postprocesses)
                postprocessListBox.Items.Add(filename);
            postprocessListBox.ResumeLayout();

            // Insert more checkbox settings here.
            homeLinkTextBox.Text = m_options.homeLink;
            goTextTextBox.Text = m_options.goText;
            footerHtmlTextBox.Text = m_options.footerHtml;
            indexPageTextBox.Text = m_options.indexHtml;
            promoTextBox.Text = m_options.promoHtml;
            licenseTextBox.Text = m_options.licenseHtml;
            versificationComboBox.Text = m_options.versificationScheme;
            numberSystemComboBox.Text = fileHelper.SetDigitLocale(m_options.numberSystem);
            numberSystemLabel.Text = fileHelper.NumberSample();
            textDirectionComboBox.Text = m_options.textDir;
            homeDomainTextBox.Text = m_options.homeDomain;
            relaxNestingSyntaxCheckBox.Checked = m_options.relaxUsfmNesting;
            fontComboBox.Text = m_options.fontFamily;
            JesusFilmLinkTextTextBox.Text = m_options.JesusFilmLinkText;
            JesusFilmLinkTargetTextBox.Text = m_options.JesusFilmLinkTarget;
            dependsComboBox.Text = m_options.dependsOn;
            footNoteCallersTextBox.Text = m_options.footNoteCallers;
            crossreferenceCallersTextBox.Text = m_options.xrefCallers;
            commentTextBox.Text = m_options.commentText;
            redistributableCheckBox.Checked = m_options.redistributable;
            licenseTextBox.Enabled = customPermissionsCheckBox.Checked = m_options.customPermissions;
            ISBNLabel.Text = "ISBN " + m_options.isbn13;

            if (m_options.eBibleCertified)
                certLabel.Text = "certified";
            else
                certLabel.Text = "";

            if (m_options.commonChars)
                commonCharactersLabel.Text = "Common fonts OK.";
            else
                commonCharactersLabel.Text = "Extended Unicode font required.";

        	LoadConcTab();
			LoadBooksTab();
        	LoadFramesTab();
            LoadStatisticsTab();
            SetCopyrightStrings();

            if (ReadMetadata(Path.Combine(Path.Combine(m_inputProjectDirectory, "usx"), "metadata.xml")))
                SaveOptions();
            string src = FindSource(m_project);
            if (src == string.Empty)
            {
                sourceLabel.BackColor = Color.Yellow;
                sourceLabel.ForeColor = Color.Red;
                sourceLabel.Text = "NO SOURCE DIRECTORY! Please read Help.";
            }
            else
            {
                sourceLabel.BackColor = BackColor;
                sourceLabel.ForeColor = Color.Black;
                sourceLabel.Text = src;
            }
            if (ethnologueCodeTextBox.Text.Trim().Length == 3)
                ethnologueCodeTextBox.BackColor = Color.White;
            else
                ethnologueCodeTextBox.BackColor = Color.Yellow;
        }

        private void LoadStatisticsTab()
        {
            statisticsTextBox.Text = String.Format(@"Old Testament: {0} books;  {1} chapters;  {2} verses;  {3} verse range; 
New Testament: {4} books;  {5} chapters;  {6} verses;  {7} verse range: 
Apocrypha/Deuterocanon: {8} books;  {9} chapters;  {10} verses;  {11} verse range;
Peripherals: {12} books

FCBH Dramatized OT: {13}  FCBH Dramatized NT: {14}  FCBH OT: {15}  FCBH NT: {16}  FCBH Portions: {17}",
                                m_options.otBookCount, m_options.otChapCount, m_options.otVerseCount, m_options.otVerseMax,
                                m_options.ntBookCount, m_options.ntChapCount, m_options.ntVerseCount, m_options.ntVerseMax,
                                m_options.adBookCount, m_options.adChapCount, m_options.adVerseCount, m_options.adVerseMax,
                                m_options.pBookCount,
                                m_options.fcbhDramaOT, m_options.fcbhDramaNT, m_options.fcbhAudioOT, m_options.fcbhAudioNT, m_options.fcbhAudioPortion);
        }

		private void LoadConcTab()
		{
            concordanceRadioButton.Checked = m_options.GenerateConcordance;
            mobileHtmlRadioButton.Checked = m_options.GenerateMobileHtml;
            legacyHtmlRadioButton.Checked = m_options.LegacyHtml;
            chkMergeCase.Checked = m_options.MergeCase;
			tbxWordformingChars.Text = m_options.WordformingChars;
			tbxExcludeWords.Text = m_options.ExcludeWords;
			tbxMaxFreq.Text = m_options.MaxFreqSrc;
			tbxPhrases.Text = m_options.PhrasesSrc;
			tbxMinContext.Text = m_options.MinContextLength.ToString();
			tbxMaxContext.Text = m_options.MaxContextLength.ToString();

		}

		private void SaveConcTab()
		{
			m_options.GenerateConcordance = concordanceRadioButton.Checked;
            m_options.GenerateMobileHtml = mobileHtmlRadioButton.Checked;
            m_options.LegacyHtml = legacyHtmlRadioButton.Checked;
            m_options.MergeCase = chkMergeCase.Checked;
			m_options.WordformingChars = tbxWordformingChars.Text;
			m_options.ExcludeWords = tbxExcludeWords.Text;
			m_options.MaxFreqSrc = tbxMaxFreq.Text; // Enhance: validate
			m_options.PhrasesSrc = tbxPhrases.Text;
			int temp;
			if (int.TryParse(tbxMinContext.Text, out temp))
				m_options.MinContextLength = temp;
			if (int.TryParse(tbxMaxContext.Text, out temp))
				m_options.MaxContextLength = temp;
		}

		private void LoadBooksTab()
		{
			listBooks.BeginUpdate();
			listBooks.Items.Clear();
			Dictionary<string, string> idsToCrossRefs = new Dictionary<string, string>();
			foreach (var kvp in m_options.CrossRefToFilePrefixMap)
				idsToCrossRefs[kvp.Value] = kvp.Key;
			foreach (var key in m_options.Books)
			{
				string vernAbbr;
				if (!m_options.ReferenceAbbeviationsMap.TryGetValue(key, out vernAbbr))
					vernAbbr = "";
				string crossRefName;
				if (!idsToCrossRefs.TryGetValue(key, out crossRefName))
					crossRefName = "";
				listBooks.Items.Add(MakeBookListItem(key, vernAbbr, crossRefName));
			}
			listBooks.EndUpdate();
		}

		private void SaveBooksTab()
		{
			List<string> books = new List<string>();
			Dictionary<string, string> crossRefsToIds = new Dictionary<string, string>();
			Dictionary<string, string> idsToVernAbbrs = new Dictionary<string, string>();
			foreach (ListViewItem item in listBooks.Items)
			{
				var key = item.Text;
				var vernAbbr = item.SubItems[1].Text;
				var crossRefName = item.SubItems[2].Text;
				books.Add(key);
				idsToVernAbbrs[key] = vernAbbr;
				if (string.IsNullOrEmpty(crossRefName))
					continue;
                if (crossRefsToIds.ContainsKey(crossRefName))
                {
                    MessageBox.Show("Duplicate book name: " + crossRefName + " @ " + key, "Error in " + m_options.translationId, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    crossRefsToIds.Add(crossRefName, key);
                }
			}
			m_options.Books = books;
			m_options.ReferenceAbbeviationsMap = idsToVernAbbrs;
			m_options.CrossRefToFilePrefixMap = crossRefsToIds;
		}

		private void SaveFramesTab()
		{
            m_options.UseFrames = framedConcordanceRadioButton.Checked;
			m_options.ConcordanceLinkText = concordanceLinkTextBox.Text;
			m_options.BooksAndChaptersLinkText = booksAndChaptersLinkTextBox.Text;
			m_options.IntroductionLinkText = introductionLinkTextBox.Text;
			m_options.PreviousChapterLinkText = previousChapterLinkTextBox.Text;
			m_options.NextChapterLinkText = nextChapterLinkTextBox.Text;
			m_options.HideNavigationButtonText = hideNavigationPanesTextBox.Text;
			m_options.ShowNavigationButtonText = showNavigationTextBox.Text;
		}

		private void LoadFramesTab()
		{
            framedConcordanceRadioButton.Checked = m_options.UseFrames;
			concordanceLinkTextBox.Text = m_options.ConcordanceLinkText;
			booksAndChaptersLinkTextBox.Text = m_options.BooksAndChaptersLinkText;
			introductionLinkTextBox.Text = m_options.IntroductionLinkText;
			previousChapterLinkTextBox.Text = m_options.PreviousChapterLinkText;
			nextChapterLinkTextBox.Text = m_options.NextChapterLinkText;
			hideNavigationPanesTextBox.Text = m_options.HideNavigationButtonText;
			showNavigationTextBox.Text = m_options.ShowNavigationButtonText;
		}

        private void m_projectsList_SelectedIndexChanged(object sender, EventArgs e)
        {
            m_project = m_projectsList.SelectedItem.ToString();
            m_inputProjectDirectory = Path.Combine(m_inputDirectory, m_project);
            m_outputProjectDirectory = Path.Combine(m_outputDirectory, m_project);
            m_xiniPath = Path.Combine(m_inputProjectDirectory, "options.xini");
            displayOptions();
        }

        public void SaveOptions()
        {
            if (m_options == null)
                return;
            m_options.languageId = ethnologueCodeTextBox.Text;
            m_options.translationId = translationIdTextBox.Text;
            m_options.translationTraditionalAbbreviation = traditionalAbbreviationTextBox.Text;
            m_options.languageName = languageNameTextBox.Text;
            m_options.languageNameInEnglish = engLangNameTextBox.Text;
            m_options.dialect = dialectTextBox.Text;
            m_options.contentCreator = creatorTextBox.Text;
            m_options.contributor = contributorTextBox.Text;
            m_options.vernacularTitle = titleTextBox.Text;
            m_options.EnglishDescription = descriptionTextBox.Text;
            m_options.lwcDescription = lwcDescriptionTextBox.Text;
            m_options.contentUpdateDate = updateDateTimePicker.Value;
            m_options.publicDomain = pdRadioButton.Checked;
            m_options.ccbyndnc = ccRadioButton.Checked;
            m_options.ccbysa = CCBySaRadioButton.Checked;
            m_options.ccbynd = CCByNdRadioButton.Checked;
            m_options.otherLicense = otherRadioButton.Checked;
            m_options.allRightsReserved = allRightsRadioButton.Checked;
            m_options.silentCopyright = silentRadioButton.Checked;
            m_options.copyrightOwner = copyrightOwnerTextBox.Text.Trim();
            copyrightOwnerUrlTextBox.Text = copyrightOwnerUrlTextBox.Text.Trim();
            if ((copyrightOwnerUrlTextBox.Text.Length > 1) && !copyrightOwnerUrlTextBox.Text.ToLowerInvariant().StartsWith("http://"))
                copyrightOwnerUrlTextBox.Text = "http://" + copyrightOwnerUrlTextBox.Text;
            m_options.copyrightOwnerUrl = copyrightOwnerUrlTextBox.Text;
            m_options.copyrightYears = copyrightYearTextBox.Text.Trim();
            m_options.copyrightOwnerAbbrev = coprAbbrevTextBox.Text.Trim();
            m_options.rightsStatement = rightsStatementTextBox.Text;
            m_options.printPublisher = printPublisherTextBox.Text.Trim();
            m_options.electronicPublisher = electronicPublisherTextBox.Text.Trim();
            m_options.ignoreExtras = stripExtrasCheckBox.Checked;
            m_options.textDir = textDirectionComboBox.Text;
            m_options.xoFormat = xoTextBox.Text;
            m_options.customCssFileName = customCssTextBox.Text;
            m_options.stripNoteOrigin = stripOriginCheckBox.Checked;
            m_options.PrepublicationChecks = prepublicationChecksCheckBox.Checked;
            m_options.WebSiteReady = webSiteReadyCheckBox.Checked;
            m_options.ETENDBL = e10dblCheckBox.Checked;
            m_options.Archived = archivedCheckBox.Checked;
            m_options.subsetProject = subsetCheckBox.Checked;
            m_options.paratextProject = (string)paratextcomboBox.SelectedItem;
            m_options.JesusFilmLinkText = JesusFilmLinkTextTextBox.Text;
            m_options.JesusFilmLinkTarget = JesusFilmLinkTargetTextBox.Text;
            m_options.AudioCopyrightNotice = audioRecordingCopyrightTextBox.Text;
            m_options.rodCode = rodCodeTextBox.Text;
            m_options.ldml = ldmlTextBox.Text;
            m_options.script = scriptTextBox.Text;
            m_options.localRightsHolder = localRightsHolderTextBox.Text;
            m_options.facebook = facebookTextBox.Text;
            m_options.country = countryTextBox.Text;
            m_options.countryCode = countryCodeTextBox.Text;
            m_options.extendUsfm = extendUsfmCheckBox.Checked;
            m_options.fcbhId = fcbhIdTextBox.Text.Replace(".","");
            m_options.shortTitle = shortTitleTextBox.Text;
            m_options.footNoteCallers = footNoteCallersTextBox.Text;
            m_options.xrefCallers = crossreferenceCallersTextBox.Text;
            m_options.commentText = commentTextBox.Text;
            m_options.redistributable = redistributableCheckBox.Checked & !m_options.privateProject;
            m_options.downloadsAllowed = downloadsAllowedCheckBox.Checked & !m_options.privateProject;
            m_options.customPermissions = customPermissionsCheckBox.Checked;
            m_options.chapterLabel = chapterLabelTextBox.Text;
            m_options.psalmLabel = psalmLabelTextBox.Text;
            m_options.includeCropMarks = cropCheckBox.Checked;
            m_options.chapter1 = chapter1CheckBox.Checked;
            m_options.verse1 = verse1CheckBox.Checked;
            m_options.pageWidth = pageWidthTextBox.Text;
            m_options.pageLength = pageLengthTextBox.Text;
            m_options.RegenerateNoteOrigins = regenerateNoteOriginsCheckBox.Checked;
            m_options.CVSeparator = cvSeparatorTextBox.Text;
            m_options.SwordName = swordNameTextBox.Text;
            m_options.ObsoleteSwordName = oldSwordIdTextBox.Text;
            m_options.rebuild = RebuildCheckBox.Checked;
            xini.WriteBool("rebuild", m_options.rebuild);
            m_options.makeInScript = makeInScriptCheckBox.Checked;
            m_options.makeEub = makeEPubCheckBox.Checked;
            m_options.makeHtml = makeHtmlCheckBox.Checked;
            m_options.makePDF = makePDFCheckBox.Checked;
            m_options.makeSword = makeSwordCheckBox.Checked;
            m_options.makeWordML = makeWordMLCheckBox.Checked;
            m_options.disablePrintingFigOrigins = disablePrintingFigoriginsCheckBox.Checked;
            m_options.includeApocrypha = apocryphaCheckBox.Checked;

            List<string> tableNames = new List<string>();
            foreach (string filename in listInputProcesses.Items)
                tableNames.Add(filename);
            m_options.preprocessingTables = tableNames;
            
            List<string> postprocessNames = new List<string>();
            foreach (string filename in postprocessListBox.Items)
                postprocessNames.Add(filename);
            m_options.postprocesses = postprocessNames;
            
            /*
            List<string> alternateLinks = new List<string>();
            foreach (string alternateLink in altLinkListBox.Items)
                alternateLinks.Add(alternateLink);
            m_options.altLinks = alternateLinks;
             */
            
            // Insert more checkbox settings here.
            m_options.homeLink = homeLinkTextBox.Text;
            m_options.goText = goTextTextBox.Text;
            m_options.footerHtml = footerHtmlTextBox.Text;
            m_options.indexHtml = indexPageTextBox.Text;
            m_options.promoHtml = promoTextBox.Text;
            m_options.licenseHtml = licenseTextBox.Text;
            m_options.versificationScheme = versificationComboBox.Text;
            m_options.numberSystem = fileHelper.SetDigitLocale(numberSystemComboBox.Text.Trim());
            m_options.privateProject = privateCheckBox.Checked;
            m_options.relaxUsfmNesting = relaxNestingSyntaxCheckBox.Checked;
            m_options.rechecked = recheckedCheckBox.Checked;

            m_options.homeDomain = homeDomainTextBox.Text.Trim();
            m_options.fontFamily = fontComboBox.Text;
            if (dependsComboBox.SelectedItem != null)
                m_options.dependsOn = dependsComboBox.Text;

			SaveConcTab();
			SaveBooksTab();
        	SaveFramesTab();

            m_options.Write();
        }

        private void btnAddInputProcess_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.CheckFileExists = true;
            dlg.InitialDirectory = m_inputProjectDirectory;
            //dlg.Multiselect = true;
            dlg.Filter = "Regular expression files (*.re)|*.re|All files|*.*";
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                string newFilePath = dlg.FileName;
                string newFileName = Path.GetFileName(newFilePath);
                string newFileDir = Path.GetDirectoryName(newFilePath);
                foreach (object o in listInputProcesses.Items)
                {
                    if (((string)o).CompareTo(newFileName) == 0)
                    {
                        MessageBox.Show(this, newFileName +
                            " is already in the preprocess file list. (The path is assumed to be in the project directory, the work directory, or the Haiola data root directory, which are searched in that order.)",
                            "Note:");
                        return;
                    }
                }
                if ((newFileDir.ToLowerInvariant() != m_inputProjectDirectory.ToLowerInvariant()) &&
                    (newFileDir.ToLowerInvariant() != m_inputDirectory.ToLowerInvariant()) &&
                    (newFileDir.ToLowerInvariant() != dataRootDir.ToLowerInvariant()))
                {
                    if (MessageBox.Show(this, "Preprocessing files must be in the work directory. Copy there?", "Note", MessageBoxButtons.YesNo) ==
                        DialogResult.Yes)
                    {
                        File.Copy(newFilePath, Path.Combine(m_inputProjectDirectory, newFileName));
                    }
                    else
                    {
                        return;
                    }
                }
                listInputProcesses.Items.Add(newFileName);
            }
            SaveOptions();
        }

        /// <summary>
        /// Add a special process (one of the ones more easily implemented in C# than REs). Currently there is only one, so just put it in.
        /// If we add more this will need to bring up a chooser with more explanation.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /*
        private void btnAddSpecialProcess_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(this,
                            "Currently Haiola only has one special process, one which re-arranges \vt fields containing multiple |fn anchors followed by corresponding \ft fields.",
                            "Note", MessageBoxButtons.OKCancel) == DialogResult.OK)
            {
                listInputProcesses.Items.Add(kFootnotesProcess);
            }
        }
        */

        private void btnRemoveInputProcess_Click(object sender, EventArgs e)
        {
            if (listInputProcesses.SelectedIndices.Count > 0)
                listInputProcesses.Items.RemoveAt(listInputProcesses.SelectedIndices[0]);
            SaveOptions();
        }

        private void tabControl1_Leave(object sender, EventArgs e)
        {
            SaveOptions();
        }

        private void moveUpButton_Click(object sender, EventArgs e)
        {
            if (listInputProcesses.Items.Count < 2)
                return;
            int currentSelection = listInputProcesses.SelectedIndex;
            if (currentSelection < 1)
                return;
            string selectedString = (string)listInputProcesses.SelectedItem;
            listInputProcesses.Items.RemoveAt(currentSelection);
            currentSelection--;
            listInputProcesses.Items.Insert(currentSelection, selectedString);
            listInputProcesses.SelectedIndex = currentSelection;
            SaveOptions();
        }

        private void moveDownButton_Click(object sender, EventArgs e)
        {
            if (listInputProcesses.Items.Count < 2)
                return;
            int currentSelection = listInputProcesses.SelectedIndex;
            if ((currentSelection < 0) || (currentSelection >= (listInputProcesses.Items.Count - 1)))
                return;
            string selectedString = (string)listInputProcesses.SelectedItem;
            listInputProcesses.Items.RemoveAt(currentSelection);
            currentSelection++;
            listInputProcesses.Items.Insert(currentSelection, selectedString);
            listInputProcesses.SelectedIndex = currentSelection;
            SaveOptions();
        }

        private void haiolaForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveOptions();
        }

        public bool updateConversionProgress(string progressMessage)
        {
            if (currentConversion != progressMessage)
            {
                currentConversion = progressMessage;
                // batchLabel.Text = (DateTime.UtcNow - startTime).ToString().Substring(0, 8) + " " + m_project + " " + currentConversion;
                Application.DoEvents();
            }
            return fileHelper.fAllRunning;
        }

        private DateTime startTime = new DateTime(1, 1, 1);
        private bool triggerautorun;

        private void timer1_Tick(object sender, EventArgs e)
        {
            string progress;
            string runtime = String.Empty;
            if (currentConversion == "Concordance")
                progress = ConcGenerator.Stage + " " + ConcGenerator.Progress;
            else
                progress = currentConversion + " " + WordSend.usfxToHtmlConverter.conversionProgress;
            if (fileHelper.fAllRunning)
                runtime = (DateTime.UtcNow - startTime).ToString("g") + " " + m_project + " ";
            batchLabel.Text = runtime + progress;
            extensionLabel.Text = plugin.PluginMessage();
            if (triggerautorun)
            {
                triggerautorun = false;
                WorkOnAllButton_Click(sender, e);
            }
        }

        /*
    	private string ConversionProgress
    	{
    		get
    		{
				if (currentConversion == "Concordance")
					return ConcGenerator.Stage + " " + ConcGenerator.Progress;
    			return currentConversion + " " + WordSend.usfxToHtmlConverter.conversionProgress;
    		}
    	}
        */

        private void statusNow(string s)
        {
            batchLabel.Text = (DateTime.UtcNow - startTime).ToString().Substring(0, 8) + " " + m_project + " " + s;
            Application.DoEvents();
        }

    	private void addProgramButton_Click(object sender, EventArgs e)
        {
            if (postprocessTextBox.Text.Length > 0)
            {
                postprocessListBox.Items.Add(postprocessTextBox.Text);
            }
            else
            {
                MessageBox.Show("Specify the process to add in the text box, first.");
            }
            SaveOptions();
        }

        private void deleteButton_Click(object sender, EventArgs e)
        {
            if ((postprocessListBox.SelectedIndices.Count > 0) && (postprocessListBox.SelectedIndex >= 0))
                postprocessListBox.Items.RemoveAt(postprocessListBox.SelectedIndices[0]);
            SaveOptions();
        }

        private void mvUpButton_Click(object sender, EventArgs e)
        {
            if (postprocessListBox.Items.Count < 2)
                return;
            int currentSelection = postprocessListBox.SelectedIndex;
            if (currentSelection < 1)
                return;
            string selectedString = (string)postprocessListBox.SelectedItem;
            postprocessListBox.Items.RemoveAt(currentSelection);
            currentSelection--;
            postprocessListBox.Items.Insert(currentSelection, selectedString);
            postprocessListBox.SelectedIndex = currentSelection;
            SaveOptions();
        }

        private void mvDownButton_Click(object sender, EventArgs e)
        {
            if (postprocessListBox.Items.Count < 2)
                return;
            int currentSelection = postprocessListBox.SelectedIndex;
            if ((currentSelection < 0) || (currentSelection >= (postprocessListBox.Items.Count - 1)))
                return;
            string selectedString = (string)postprocessListBox.SelectedItem;
            postprocessListBox.Items.RemoveAt(currentSelection);
            currentSelection++;
            postprocessListBox.Items.Insert(currentSelection, selectedString);
            postprocessListBox.SelectedIndex = currentSelection;
            SaveOptions();
        }

        private void runHighlightedButton_Click(object sender, EventArgs e)
        {
            string command = Path.Combine(m_inputDirectory, "postprocess.bat");
            try
            {
                if (fileHelper.fAllRunning)
                {
                    fileHelper.fAllRunning = false;
                    WorkOnAllButton.Enabled = false;
                    WorkOnAllButton.Text = "Stopping...";
                    runHighlightedButton.Enabled = false;
                    Application.DoEvents();
                    return;
                }
                fileHelper.fAllRunning = true;
                btnSetRootDirectory.Enabled = false;
                reloadButton.Enabled = false;
                m_projectsList.Enabled = false;
                markRetryButton.Enabled = false;
                unmarkAllButton.Enabled = false;
                runHighlightedButton.Enabled = false;
                messagesListBox.Items.Clear();
                messagesListBox.BackColor = Color.LightGreen;
                tabControl1.SelectedTab = messagesTabPage;
                startTime = DateTime.UtcNow;
                BackColor = Color.LightGreen;
                Application.DoEvents();
                timer1.Enabled = true;
                WorkOnAllButton.Text = "Stop";
                m_options.done = false;
                SaveOptions();
                ProcessOneProject(SelectedProject);
                Application.DoEvents();
                if (fileHelper.fAllRunning && File.Exists(command))
                {
                    currentConversion = "Running " + command;
                    batchLabel.Text = currentConversion;
                    Application.DoEvents();
                    if (!fileHelper.RunCommand(command))
                        MessageBox.Show(fileHelper.runCommandError, "Error " + currentConversion);
                }
                currentConversion = String.Empty;
                batchLabel.Text = (DateTime.UtcNow - startTime).ToString(@"g") + " " + "Done.";
                messagesListBox.Items.Add(batchLabel.Text);
                m_projectsList_SelectedIndexChanged(null, null);
                string indexhtm = Path.Combine(Path.Combine(m_outputProjectDirectory, "html"), "index.htm");
                if (File.Exists(indexhtm))
                    System.Diagnostics.Process.Start(indexhtm);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "ERROR processing highlighted project");
                Logit.WriteError(ex.Message);
                Logit.WriteError(ex.StackTrace);
            }
            fileHelper.fAllRunning = false;
            WorkOnAllButton.Enabled = true;
            WorkOnAllButton.Text = "Run marked";
            m_projectsList.Enabled = true;
            markRetryButton.Enabled = true;
            unmarkAllButton.Enabled = true;
            btnSetRootDirectory.Enabled = true;
            reloadButton.Enabled = true;
            runHighlightedButton.Enabled = true;
        }

        private string SelectedProject
    	{
    		get { return (string)m_projectsList.SelectedItem; }
    	}

 
        public class FcbhAudio
        {
            public string fcbh_id;
            //public string volume_name;
            public string language_iso;
            public string version_code;
            //public string version_name;
            //public string version_english;
            public string collection_code;
            public string media_type;
        }

        public ArrayList fcbhIds;

        /// <summary>
        /// Find FCBH ID(s) for the currently-displayed m_options record
        /// </summary>
        protected void MatchFcbhIds()
        {
            if ((!getFCBHkeys) || (fcbhIds == null))
                return;
            string ntDrama = String.Empty;
            string ntAudio = String.Empty;
            string otDrama = String.Empty;
            string otAudio = String.Empty;
            string portion = String.Empty;
            string localFcbhId = String.Empty;
            foreach (FcbhAudio fcbh in fcbhIds)
            {
                //ntDrama = ntAudio = otDrama = otAudio = portion = String.Empty;
                if (fcbh.language_iso == m_options.languageId)
                {
                    localFcbhId = m_options.fcbhId;
                    if (localFcbhId.Length < 6)
                        localFcbhId = "@@@@@@"; // No match, but don't choke Substring().
                    localFcbhId = localFcbhId.Substring(0, 6);
                    if ((fcbh.version_code == m_options.translationTraditionalAbbreviation) ||
                        (fcbh.fcbh_id.StartsWith(localFcbhId)))
                    {
                        switch (fcbh.collection_code)
                        {
                            case "NT":
                                if (fcbh.media_type == "Drama")
                                {
                                    ntDrama = fcbh.fcbh_id;
                                }
                                else
                                {
                                    ntAudio = fcbh.fcbh_id;
                                }
                                break;
                            case "OT":
                                if (fcbh.media_type == "Drama")
                                {
                                    otDrama = fcbh.fcbh_id;
                                }
                                else
                                {
                                    otAudio = fcbh.fcbh_id;
                                }
                                break;
                            case "AL":
                                portion = fcbh.fcbh_id;
                                break;
                        }
                    }
                }
            }
            m_options.fcbhAudioNT = ntAudio;
            m_options.fcbhDramaNT = ntDrama;
            m_options.fcbhAudioOT = otAudio;
            m_options.fcbhDramaOT = otDrama;
            m_options.fcbhAudioPortion = portion;
            m_options.Write();
        }

        protected void GetFcbhIds()
        {
            if (!plugin.PluginLoaded())
                return;
            try
            {
                fcbhIds = new ArrayList();
                WebClient c = new WebClient();
                var data = c.DownloadString(plugin.ThePlugin.fcbh_token());
                if (data != null)
                {
                    var fcbhlib = Newtonsoft.Json.JsonConvert.DeserializeObject<IList<IDictionary<string,object>>>(data);
                    foreach (Dictionary<string,object> dic in fcbhlib)
                    {
                        FcbhAudio fcbh = new FcbhAudio();
                        fcbh.fcbh_id = dic["fcbh_id"].ToString();
                        //fcbh.volume_name = dic["volume_name"].ToString();
                        fcbh.language_iso = dic["language_iso"].ToString();
                        fcbh.version_code = dic["version_code"].ToString();
                        //fcbh.version_name = dic["version_name"].ToString();
                        //fcbh.version_english = dic["version_english"].ToString();
                        fcbh.collection_code = dic["collection_code"].ToString();
                        fcbh.media_type = dic["media_type"].ToString();
                        fcbhIds.Add(fcbh);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error parsing FCBH audio library list");
            }
        }

        /// <summary>
        /// Generate files summarizing Bible translation project statistics.
        /// </summary>
        /// <param name="sender">sender object</param>
        /// <param name="e">EventArgs</param>
        /*
    	private void statsButton_Click(object sender, EventArgs e)
        {
            int numProjects = 0;
            int numViral = 0;
            int numTranslations = 0;
            int urlid = 0;
            int numLanguages = 0;
            int numDialects = 0;
            int numSites = 0;
            int numSubsets = 0;
            int numCertified = 0;
            int c;
            int coprCount = 0;
            string dialect;
            string homedomain;
            string copr = String.Empty;
            string sqlDir = Path.Combine(m_outputDirectory, "MySQL");
            string inScriptSite;

            Utils.EnsureDirectory(sqlDir);

            Hashtable langTable = new Hashtable();
            Hashtable dialectTable = new Hashtable();
            Hashtable siteTable = new Hashtable();
            Hashtable coprTable = new Hashtable();
            btnSetRootDirectory.Enabled = false;
            reloadButton.Enabled = false;
            m_projectsList.Enabled = false;
            markRetryButton.Enabled = false;
            unmarkAllButton.Enabled = false;
            runHighlightedButton.Enabled = false;
            WorkOnAllButton.Enabled = false;
            startTime = DateTime.UtcNow;
            timer1.Enabled = true;
            SaveOptions();
            StreamWriter sw = new StreamWriter(Path.Combine(m_outputDirectory, "translations.csv"), false, System.Text.Encoding.UTF8);
            StreamWriter sqlFile = new StreamWriter(Path.Combine(sqlDir,"bible_list.sql"), false, System.Text.Encoding.UTF8);
            StreamWriter altUrlFile = new StreamWriter(Path.Combine(sqlDir, "urllist.sql"), false, System.Text.Encoding.UTF8);
            StreamWriter scorecard = new StreamWriter(Path.Combine(m_outputDirectory, "scorecard.txt"), false, System.Text.Encoding.UTF8);
            StreamWriter infoHtm;
            sqlFile.WriteLine("USE sofia;");
            sqlFile.WriteLine("DROP TABLE IF EXISTS sofia.bible_list;");
            sqlFile.WriteLine(@"CREATE TABLE bible_list (
  translationid VARCHAR(64) NOT NULL PRIMARY KEY COLLATE UTF8_GENERAL_CI,
  fcbhid VARCHAR(12),
  languagecode VARCHAR(4) NOT NULL,
  languagename VARCHAR(128),
  languagenameinenglish VARCHAR(128),
  dialect VARCHAR(128),
  homedomain VARCHAR(128),
  title VARCHAR(256),
  shorttitle VARCHAR(256),
  description VARCHAR(1024),
  free BOOL,
  copyright VARCHAR(1024),
  updatedate DATE,
  publicationurl VARCHAR(1024));");
            sqlFile.WriteLine("LOCK TABLES bible_list WRITE;");
            altUrlFile.WriteLine("USE sofia;");
            altUrlFile.WriteLine(@"DROP TABLE IF EXISTS sofia.urllist;");
            altUrlFile.WriteLine(@"CREATE TABLE urllist (
  urlid INT UNSIGNED NOT NULL,
  languagecode VARCHAR(4) NOT NULL,
  translationid VARCHAR(64) NOT NULL COLLATE UTF8_GENERAL_CI,
  url VARCHAR(1024) NOT NULL);");
            altUrlFile.WriteLine("LOCK TABLES urllist WRITE;");

            sw.WriteLine("\"languageCode\",\"translationId\",\"languageName\",\"languageNameInEnglish\",\"dialect\",\"homeDomain\",\"title\",\"description\",\"Redistributable\",\"Copyright\",\"UpdateDate\",\"publicationURL\",\"OTbooks\",\"OTchapters\",\"OTverses\",\"NTbooks\",\"NTchapters\",\"NTverses\",\"DCbooks\",\"DCchapters\",\"DCverses\",\"FCBHID\",\"Certified\",\"inScript\"");
            foreach (object o in m_projectsList.Items)
            {
                m_project = (string)o;
                m_inputProjectDirectory = Path.Combine(m_inputDirectory, m_project);
                m_outputProjectDirectory = Path.Combine(m_outputDirectory, m_project);
                m_xiniPath = Path.Combine(m_inputProjectDirectory, "options.xini");
                displayOptions();
                if (m_options.redistributable || (m_options.languageId == "khm"))
                    inScriptSite = "http://eBible.org/study/?v1=GN1_1&w1=bible&t1=local%3A";
                else
                    inScriptSite = "http://dbs.org/bible/?v1=GN1_1&w1=bible&t1=local%3A";
                if (m_options.eBibleCertified)
                {
                    numCertified++;
                }
                numProjects++;
                MatchFcbhIds();
                if (Directory.Exists(m_outputProjectDirectory))
                {
                    infoHtm = new StreamWriter(Path.Combine(m_outputProjectDirectory, "info.inc"));
                    infoHtm.WriteLine("<tr><td><a href='http://www.ethnologue.com/language/{0}'>{0}</a></td><td><a href='{2}/index.htm'>{1}</a></td><td><a href='{2}/index.htm'>{3}</a></td><td>{4}</td><td>{5}</td><td><a href=\"{6}{7}\">inScript</a></td></tr>",
                        m_options.languageId,
                        m_options.languageName,
                        m_options.translationId,
                        m_options.vernacularTitle,
                        m_options.languageNameInEnglish,
                        m_options.dialect,
                        inScriptSite, m_options.fcbhId);
                    infoHtm.Close();
                }
                if ((!m_options.privateProject) && (m_options.languageId.Length > 1))
                {
                    if (m_options.subsetProject)
                        numSubsets++;
                    else
                        numTranslations++;
                    if (langTable[m_options.languageId] == null)
                    {
                        langTable[m_options.languageId] = 1;
                        numLanguages++;
                    }
                    else
                    {
                        c = (int)langTable[m_options.languageId];
                        langTable[m_options.languageId] = c + 1;
                    }
                    dialect = m_options.languageId + m_options.dialect;
                    if (dialectTable[dialect] == null)
                    {
                        dialectTable[dialect] = 1;
                        numDialects++;
                    }
                    else
                    {
                        c = (int)dialectTable[dialect];
                        dialectTable[dialect] = c + 1;
                    }
                    homedomain = m_options.homeDomain.Trim();
                    if (homedomain.Length > 0)
                    {
                        if (siteTable[homedomain] == null)
                        {
                            siteTable[homedomain] = 1;
                            numSites++;
                        }
                        else
                        {
                            c = (int)siteTable[homedomain];
                            siteTable[homedomain] = c + 1;
                        }
                    }
                    if (m_options.publicDomain || m_options.creativeCommons)
                    {
                        numViral++;
                    }
                    if (m_options.publicDomain)
                    {
                        copr = "Public Domain";
                    }
                    else if (m_options.copyrightOwnerAbbrev != String.Empty)
                    {
                        copr = "© " + m_options.copyrightOwnerAbbrev;
                    }
                    else
                    {
                        copr = "© " + m_options.copyrightOwner;
                    }
                    if (coprTable[copr] == null)
                    {
                        coprTable[copr] = 1;
                    }
                    else
                    {
                        coprCount = (int)coprTable[copr];
                        coprTable[copr] = coprCount + 1;
                    }
                    sw.WriteLine("\"{0}\",\"{1}\",\"{2}\",\"{3}\",\"{4}\",\"{5}\",\"{6}\",\"{7}\",\"{8}\",\"{9}\",\"{10}\",\"http://{5}/{1}/\",\"{11}\",\"{12}\",\"{13}\",\"{14}\",\"{15}\",\"{16}\",\"{17}\",\"{18}\",\"{19}\",\"{20}\",\"{21}\",\"{22}{23}\"",
                        m_options.languageId,
                        m_options.translationId,
                        fileHelper.csvString(m_options.languageName),
                        fileHelper.csvString(m_options.languageNameInEnglish),
                        fileHelper.csvString(m_options.dialect),
                        fileHelper.csvString(m_options.homeDomain.Trim()),
                        fileHelper.csvString(m_options.vernacularTitle.Trim()),
                        fileHelper.csvString(m_options.EnglishDescription.Trim()),
                        (m_options.publicDomain || m_options.creativeCommons).ToString(),
                        fileHelper.csvString(m_options.publicDomain ? "public domain" : "Copyright © " + m_options.copyrightYears + " " + m_options.copyrightOwner),
                        m_options.contentUpdateDate.ToString("yyyy-MM-dd"),
                        m_options.otBookCount,
                        m_options.otChapCount,
                        m_options.otVerseCount,
                        m_options.ntBookCount,
                        m_options.ntChapCount,
                        m_options.ntVerseCount,
                        m_options.adBookCount,
                        m_options.adChapCount,
                        m_options.adVerseCount,
                        m_options.fcbhId,
                        m_options.eBibleCertified,
                        inScriptSite,
                        m_options.fcbhId);
                    sqlFile.WriteLine("INSERT INTO bible_list VALUES (\"{0}\",\"{1}\",\"{2}\",\"{3}\",\"{4}\",\"{5}\",\"{6}\",\"{7}\",\"{8}\",\"{9}\",{10},\"{11}\",\"{12}\",\"http://{6}/{0}/\");",
                        m_options.translationId,
                        m_options.languageId,
                        m_options.fcbhId,
                        fileHelper.sqlString(m_options.languageName),
                        fileHelper.sqlString(m_options.languageNameInEnglish),
                        fileHelper.sqlString(m_options.dialect),
                        fileHelper.sqlString(m_options.homeDomain.Trim()),
                        fileHelper.sqlString(m_options.vernacularTitle.Trim()),
                        fileHelper.sqlString(m_options.shortTitle.Trim()),
                        fileHelper.sqlString(m_options.EnglishDescription.Trim()),
                        (m_options.publicDomain || m_options.creativeCommons)?"1":"0",
                        fileHelper.sqlString(m_options.publicDomain ? "public domain" : "Copyright © " + m_options.copyrightYears + " " + m_options.copyrightOwner),
                        m_options.contentUpdateDate.ToString("yyyy-MM-dd"));
                    if (m_options.homeDomain.Length > 0)
                    {
                        altUrlFile.WriteLine("INSERT INTO urllist VALUES ('{0}', '{1}', '{2}', '<a href=\"http://{3}/{2}/\">{4}</a>');",
                            urlid.ToString(), m_options.languageId, m_options.translationId,
                            fileHelper.sqlString(m_options.homeDomain.Trim()),
                            fileHelper.sqlString(m_options.vernacularTitle.Trim()));
                        urlid++;
                    }
                    foreach (string altUrl in m_options.altLinks)
                    {
                        altUrlFile.WriteLine("INSERT INTO 'urllist' VALUES ('{0}', '{1}', '{2}', '{3}');",
                            urlid.ToString(), m_options.languageId, m_options.translationId, fileHelper.sqlString(altUrl));
                        urlid++;
                    }
                }
            }
            sw.Close();
            sqlFile.WriteLine("UNLOCK TABLES;");
            altUrlFile.WriteLine("UNLOCK TABLES;");
            sqlFile.Close();
            altUrlFile.Close();
            fileHelper.fAllRunning = false;
            currentConversion = numProjects.ToString() + " projects; " + numTranslations.ToString() + " public. " + urlid.ToString() + " URLs " + numSites.ToString() + " sites " + numLanguages.ToString() + " languages " + numDialects.ToString() + " dialects (including languages). ";

            scorecard.WriteLine("Haiola project statistics as of {0} UTC", DateTime.UtcNow.ToString("R"));
            scorecard.WriteLine("{0} languages", numLanguages.ToString());
            scorecard.WriteLine("{0} dialects", numDialects.ToString());
            scorecard.WriteLine("{0} freely redistributable translations", numViral.ToString());
            scorecard.WriteLine("{0} certified translations", numCertified.ToString());
            scorecard.WriteLine("{0} limited-sharing translations", (numTranslations-numViral).ToString());
            scorecard.WriteLine("{0} total public translations", numTranslations.ToString());
            scorecard.WriteLine("{0} subset projects", numSubsets.ToString());
            scorecard.WriteLine("{0} projects", numProjects.ToString());
            scorecard.WriteLine("{0} primary distribution URLs", urlid.ToString());
            scorecard.WriteLine("{0} master sites", numSites.ToString());
            scorecard.WriteLine("Translations by site:");
            foreach (DictionaryEntry de in siteTable)
            {
                scorecard.WriteLine(" {0,4} translations at {1}", (int)de.Value, (string)de.Key);
            }
            scorecard.WriteLine("Languages with multiple translations:");
            foreach (DictionaryEntry de in langTable)
            {
                if ((int)de.Value > 1)
                {
                    scorecard.WriteLine(" {0,4} translations in {1}", (int)de.Value, (string)de.Key);
                }
            }
            scorecard.WriteLine("Dialects with multiple translations:");
            foreach (DictionaryEntry de in dialectTable)
            {
                if ((int)de.Value > 1)
                {
                    scorecard.WriteLine(" {0,4} translations in {1}", (int)de.Value, (string)de.Key);
                }
            }
            scorecard.WriteLine("Copyright ownership:");
            foreach (DictionaryEntry de in coprTable)
            {
                coprCount = (int)de.Value;
                scorecard.WriteLine(" {0,4} {1}", coprCount, (string)de.Key);
            }
            scorecard.Close();

            timer1.Enabled = false;
            statsLabel.Text = currentConversion;
            m_projectsList_SelectedIndexChanged(null, null);
            WorkOnAllButton.Enabled = true;
            WorkOnAllButton.Text = "Run marked";
            m_projectsList.Enabled = true;
            markRetryButton.Enabled = true;
            unmarkAllButton.Enabled = true;
            btnSetRootDirectory.Enabled = true;
            reloadButton.Enabled = true;
            runHighlightedButton.Enabled = true;
            WorkOnAllButton.Enabled = true;
            batchLabel.Text = (DateTime.UtcNow - startTime).ToString() + " " + currentConversion;
        }
        */

        private void helpButton_Click(object sender, EventArgs e)
        {
            showHelp("haiola.htm");
        }

        /*
        private void addLinkButton_Click(object sender, EventArgs e)
        {
            if (altLinkTextBox.Text.Length > 3)
                altLinkListBox.Items.Add(altLinkTextBox.Text);
        }

        private void deleteLinkButton_Click(object sender, EventArgs e)
        {
            if ((altLinkListBox.Items.Count > 0) && (altLinkListBox.SelectedIndex >= 0))
                altLinkListBox.Items.RemoveAt(altLinkListBox.SelectedIndex);
        }
        */

		/// <summary>
		/// Click on the Update button in the Books tab
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void updateButton_Click(object sender, EventArgs e)
		{
			UpdateBooksList();
		}

		private void restoreDefaultsButton_Click(object sender, EventArgs e)
		{
			UpdateBooksList();
		}

    	private void UpdateBooksList()
    	{
            try
            {
                // GetUsfx(SelectedProject);
                var analyzer = new UsfxToBookAndAbbr();
                analyzer.Parse(GetUsfxFilePath());
                Dictionary<string, string> oldNames = new Dictionary<string, string>();
                Dictionary<string, string> oldAbbreviations = new Dictionary<string, string>();
                /****** Always restore defaults automatically.
                if (!restoreDefaults)
                {
                    foreach (ListViewItem item in listBooks.Items)
                    {
                        var key = item.Text;
                        var oldAbbr = item.SubItems[1].Text;
                        var oldName = item.SubItems[2].Text;
                        oldNames[key] = oldName;
                        oldAbbreviations[key] = oldAbbr;
                    }
                }
                *************/
                listBooks.BeginUpdate();
                listBooks.Items.Clear();
                foreach (var key in analyzer.BookIds)
                {
                    string vernacularName;
                    oldNames.TryGetValue(key, out vernacularName);
                    if (string.IsNullOrEmpty(vernacularName))
                        vernacularName = analyzer.VernacularNames[key];
                    string vernacularAbbreviation;
                    oldAbbreviations.TryGetValue(key, out vernacularAbbreviation);
                    if (string.IsNullOrEmpty(vernacularAbbreviation))
                        vernacularAbbreviation = analyzer.ReferenceAbbreviations[key];

                    listBooks.Items.Add(MakeBookListItem(key, vernacularAbbreviation, vernacularName));
                }
                listBooks.EndUpdate();
                SaveBooksTab();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Book list may only be updated after USFX file is generated.");
            }
    	}

    	ListViewItem MakeBookListItem(string abbr, string vernAbbr, string xrefName)
		{
			ListViewItem item = new ListViewItem(abbr);
			SetLastSubItemName(item, "StdAbbr");
			item.SubItems.Add(vernAbbr);
			SetLastSubItemName(item, "Edit"); // identifies an item we can edit for ListBooks_MouseUp
			item.SubItems.Add(xrefName);
			SetLastSubItemName(item, "Edit");
			return item;
		}

		internal void SetLastSubItemName(ListViewItem item, string val)
		{
			ListViewItem.ListViewSubItem lastItem = item.SubItems[item.SubItems.Count - 1];
			lastItem.Name = val;
		}

		private void ListBooks_MouseUp(object sender, MouseEventArgs e)
		{
			ListViewHitTestInfo hti = listBooks.HitTest(e.Location);
			ListViewItem.ListViewSubItem si = hti.SubItem;
            return;
            /********** disable editing
			if (si == null || si.Name != "Edit")
				return;
			// Make a text box to edit the subitem contents.
			TextBox tb = new TextBox();
			tb.Bounds = si.Bounds;
			tb.Text = si.Text;
			tb.LostFocus += new EventHandler(tb_LostFocus);
			tb.Tag = si;
			listBooks.Controls.Add(tb);
			tb.SelectAll();
			tb.Focus();
            *************/
		}
		void tb_LostFocus(object sender, EventArgs e)
		{
			TextBox tb = sender as TextBox;
			ListViewItem.ListViewSubItem si = (tb).Tag as ListViewItem.ListViewSubItem;
			si.Text = tb.Text;
			tb.Parent.Controls.Remove(tb);
		}

        private void makeTemplateButton_Click(object sender, EventArgs e)
        {
            makeTemplateButton.Enabled = false;
            m_currentTemplate = m_project;
            xini.WriteString("currentTemplate", m_currentTemplate);
            templateLabel.Text = "Current template: " + m_currentTemplate;
            copyFromTemplateButton.Enabled = false;
            xini.Write();
        }

        private void copyFromTemplateButton_Click(object sender, EventArgs e)
        {
            Options templateOptions = new Options(Path.Combine(Path.Combine(m_inputDirectory, m_currentTemplate), "options.xini"));
            homeLinkTextBox.Text = m_options.homeLink = templateOptions.homeLink;
            goTextTextBox.Text = m_options.goText = templateOptions.goText;
            footerHtmlTextBox.Text = m_options.footerHtml = templateOptions.footerHtml;
            indexPageTextBox.Text = m_options.indexHtml = templateOptions.indexHtml;
            licenseTextBox.Text = m_options.licenseHtml = templateOptions.licenseHtml;
            customCssTextBox.Text = m_options.customCssFileName = templateOptions.customCssFileName;
            m_options.postprocesses = templateOptions.postprocesses;
            postprocessListBox.SuspendLayout();
            postprocessListBox.Items.Clear();
            foreach (string filename in templateOptions.postprocesses)
                postprocessListBox.Items.Add(filename);
            postprocessListBox.ResumeLayout();
        }

        private void markRetryButton_Click(object sender, EventArgs e)
        {
            SaveOptions();
            LoadWorkingDirectory(false, true, false);
        }

        /// <summary>
        /// Load the Paratext Projects combo box item list based on projects found in the Paratext directory.
        /// </summary>
        public void LoadParatextProjectList()
        {
            if (Directory.Exists(paratextProjectsDir))
            {
                paratextcomboBox.Items.Clear();
                paratextcomboBox.Items.Add(String.Empty);
                string[] dirList = Directory.GetDirectories(paratextProjectsDir);
                foreach (string d in dirList)
                {
                    string ssf = d + ".ssf";
                    string projName = Path.GetFileNameWithoutExtension(ssf);
                    if (File.Exists(ssf))
                    {
                        paratextcomboBox.Items.Add(projName);
                    }
                }
            }
        }

        private void paratextButton_Click(object sender, EventArgs e)
        {
            SaveOptions();
            FolderBrowserDialog dlg = new FolderBrowserDialog();
            dlg.SelectedPath = paratextProjectsDir;
            dlg.Description =
                @"Please select your existing Paratext Projects folder.";
            dlg.ShowNewFolderButton = true;
            if ((dlg.ShowDialog() != System.Windows.Forms.DialogResult.OK) || (dlg.SelectedPath == null))
                return;
            if (File.Exists(Path.Combine(dlg.SelectedPath, "usfm.sty")))
            {
                paratextProjectsDir = dlg.SelectedPath;
                xini.Write();
                LoadParatextProjectList();
            }
        }

        private void m_projectsList_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (!loadingDirectory)
            {
                SaveOptions();
                displayOptions();
                m_options.selected = e.NewValue == CheckState.Checked;
                if (m_options.selected)
                    projSelected++;
                else
                    projSelected--;
                statsLabel.Text = projReady.ToString() + " of " + projCount.ToString() + " project directories are ready to run; " + projSelected.ToString() + " selected."; ;
                m_options.Write();
            }
        }

        private void postprocessListBox_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (postprocessListBox.SelectedItem != null)
                postprocessTextBox.Text = (string)postprocessListBox.SelectedItem;
        }

        private void fontComboBox_SelectedValueChanged(object sender, EventArgs e)
        {
            try
            {
                FontFamily ff = new FontFamily(fontComboBox.Text);
                Font newFont = new System.Drawing.Font(ff, 11.0F, FontStyle.Regular, GraphicsUnit.Point);
                languageNameTextBox.Font = newFont;
                titleTextBox.Font = newFont;
                lwcDescriptionTextBox.Font = rightsStatementTextBox.Font = printPublisherTextBox.Font = newFont;
                homeLinkTextBox.Font = goTextTextBox.Font = newFont;
                footerHtmlTextBox.Font = indexPageTextBox.Font = licenseTextBox.Font = newFont;
                concordanceLinkTextBox.Font = booksAndChaptersLinkTextBox.Font = newFont;
                listBooks.Font = newFont;
                introductionLinkTextBox.Font = newFont;
                previousChapterLinkTextBox.Font = newFont;
                nextChapterLinkTextBox.Font = newFont;
                hideNavigationPanesTextBox.Font = newFont;
                showNavigationTextBox.Font = newFont;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "ERROR SETTING FONT");
            }
        }

        private void resumeButton_Click(object sender, EventArgs e)
        {
            resumeButton.Enabled = false;
            SaveOptions();
            processAllMarked();
            resumeButton.Enabled = true;
        }

        private void allRightsRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            if (allRightsRadioButton.Checked)
                redistributableCheckBox.Checked = false;
        }

        private void numberSystemComboBox_SelectedValueChanged(object sender, EventArgs e)
        {
            m_options.numberSystem = numberSystemComboBox.Text;
            fileHelper.SetDigitLocale(m_options.numberSystem);
            numberSystemLabel.Text = fileHelper.NumberSample();
        }

        private void paratextcomboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            string src = FindSource(m_project);
            if (src == string.Empty)
            {
                sourceLabel.BackColor = Color.Yellow;
                sourceLabel.ForeColor = Color.Red;
                sourceLabel.Text = "NO SOURCE DIRECTORY! Please read Help.";
            }
            else
            {
                sourceLabel.BackColor = BackColor;
                sourceLabel.ForeColor = Color.Black;
                sourceLabel.Text = src;
            }
        }

        private void ethnologueCodeTextBox_TextChanged(object sender, EventArgs e)
        {
            string theCode = ethnologueCodeTextBox.Text.Trim();
            if (theCode.Length == 3)
            {
                ethnologueCodeTextBox.BackColor = Color.White;
                if (!loadingDirectory)
                {
                    ethnorecord er = eth.ReadEthnologue(theCode);
                    countryTextBox.Text = er.countryName;
                    countryCodeTextBox.Text = er.countryId;
                    engLangNameTextBox.Text = er.langName;
                }
            }
            else
            {
                ethnologueCodeTextBox.BackColor = Color.Yellow;
            }
        }

        private void m_projectsList_Click(object sender, EventArgs e)
        {
            SaveOptions();
        }

        private void pdRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            if (pdRadioButton.Checked)
                redistributableCheckBox.Checked = downloadsAllowedCheckBox.Checked = true;
        }

        private void ccRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            if (ccRadioButton.Checked)
                redistributableCheckBox.Checked = downloadsAllowedCheckBox.Checked = true;
        }

        private void customPermissionsCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            licenseTextBox.Enabled = customPermissionsCheckBox.Checked;
        }

        private void CCBySaRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            if (CCBySaRadioButton.Checked)
                redistributableCheckBox.Checked = downloadsAllowedCheckBox.Checked = true;
        }

        private void CCByNdRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            if (CCByNdRadioButton.Checked)
                redistributableCheckBox.Checked = downloadsAllowedCheckBox.Checked = true;
        }

        private void otherRadioButton_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void privateCheckBox_CheckedChanged_1(object sender, EventArgs e)
        {

        }

        private void set1Button_Click(object sender, EventArgs e)
        {
            LoadWorkingDirectory(true, false, false, 0, 5);
        }

        private void set2Button_Click(object sender, EventArgs e)
        {
            LoadWorkingDirectory(true, false, false, 1, 5);
        }

        private void set3Button_Click(object sender, EventArgs e)
        {
            LoadWorkingDirectory(true, false, false, 2, 5);
        }

        private void set4Button_Click(object sender, EventArgs e)
        {
            LoadWorkingDirectory(true, false, false, 3, 5);
        }

        private void set5Button_Click(object sender, EventArgs e)
        {
            LoadWorkingDirectory(true, false, false, 4, 5);
        }

        private void runXetexCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            xini.WriteBool("runXetex", runXetexCheckBox.Checked);
            xini.Write();
        }

        private void relaxNestingSyntaxCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            m_options.relaxUsfmNesting = relaxNestingSyntaxCheckBox.Checked;
        }

        private void privateCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (privateCheckBox.Checked)
            {
                allRightsRadioButton.Checked = true;
                redistributableCheckBox.Checked = false;
                redistributableCheckBox.Enabled = false;
                downloadsAllowedCheckBox.Checked = false;
                downloadsAllowedCheckBox.Enabled = false;
                m_options.privateProject = true;
            }
            else
            {
                redistributableCheckBox.Enabled = true;
                downloadsAllowedCheckBox.Enabled = true;
                m_options.privateProject = false;
            }
        }

        private void redistributableCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            downloadsAllowedCheckBox.Checked |= redistributableCheckBox.Checked;
        }

        private void RebuildCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            xini.WriteBool("rebuild", RebuildCheckBox.Checked);
            xini.Write();
        }

        
    }
}
