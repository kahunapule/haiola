using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.IO;
using System.Xml;
using BibleFileLib;
using Microsoft.Win32;
using WordSend;
using sepp;

namespace haiola
{
    public partial class haiolaForm : Form
    {
        public static haiolaForm MasterInstance;
        private const string kFootnotesProcess = "footnotes.process"; // identifier for the (so far only one) file transformation process built into Haiola
        private XMLini xini;    // Main program XML initialization file
        private string m_currentTemplate;   // Current template project
        public string dataRootDir; // Default is BibleConv in the user's Documents folder
        public string m_inputDirectory; // Always under dataRootDir, defaults to Documents/BibleConv/input
        public string m_outputDirectory; // curently Site, always under dataRootDir
        public string m_inputProjectDirectory; //e.g., full path to BibleConv\input\Kupang
        public string m_outputProjectDirectory; // e.g., full path to BibleConv\output\Kupang
        public string m_project = String.Empty; // e.g., Kupang
        public string currentConversion;   // e.g., "Preprocessing" or "Portable HTML"
        public static bool fAllRunning = false;
        public string m_xiniPath;  // e.g., BibleConv\input\Kupang\options.xini
        public XMLini projectXini;
        public Options m_options;
        public BibleBookInfo bkInfo;
        public DateTime sourceDate = new DateTime(1611, 1, 1);
        PluginManager plugin;


        public haiolaForm()
        {
            InitializeComponent();
            MasterInstance = this;
            if (Directory.GetCurrentDirectory().EndsWith(@"Debug"))
            {
                DateTime today = DateTime.UtcNow;
                StreamWriter sw = new StreamWriter(@"..\..\Version.cs", false, System.Text.Encoding.UTF8);
                sw.Write(@"using System;
namespace haiola
{
	/// <summary>
	/// This is a generated file. You should not edit it directly, but edit haiola.cs instead.
	/// </summary>
	public class Version
	{
		public static string date = ");

                sw.WriteLine("\"{0}\";", today.ToString("yyyy-MM-dd"));
                sw.WriteLine("		public static string year = @\"{0}\";", today.ToString("yyyy"));
                sw.WriteLine("		public static string time = @\"{0}\";", today.ToString("HH:mm:ss"));
                sw.WriteLine(@"		public Version()
		{
		}
	}
}");
                sw.Close();
            }
            plugin = new PluginManager();
            batchLabel.Text = String.Format("Haiola version {0}.{1} ©2003-{2} SIL, EBT, && eBible.org. Released under Gnu LGPL 3 or later.",
                Version.date, Version.time, Version.year);
            extensionLabel.Text = plugin.PluginMessage();
        }

        bool GetRootDirectory()
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






        private void btnSetRootDirectory_Click(object sender, EventArgs e)
        {
            SaveOptions();
            if (GetRootDirectory())
                LoadWorkingDirectory(true);
        }

        private void haiolaForm_Load(object sender, EventArgs e)
        {
            xini = new XMLini(Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "haiola"),
    "haiola.xini"));
            dataRootDir = xini.ReadString("dataRootDir", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BibleConv"));
            m_inputDirectory = Path.Combine(dataRootDir, "input");
            if (!Directory.Exists(m_inputDirectory))
                if (!GetRootDirectory())
                    Application.Exit();
            LoadWorkingDirectory(true);
            
            Application.DoEvents();
            triggerautorun = Program.autorun;
            if (triggerautorun)
            {
                startTime = DateTime.UtcNow;
                timer1.Enabled = true;
            }
        }
        private void EnsureTemplateFile(string fileName)
        {
        	EnsureTemplateFile(fileName, m_inputDirectory);
        }
 
        private void EnsureTemplateFile(string fileName, string destDirectory)
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

        private void LoadWorkingDirectory(bool all)
        {
            int projCount = 0;
            int projReady = 0;
            m_projectsList.BeginUpdate();
            m_projectsList.Items.Clear();
            m_inputDirectory = Path.Combine(dataRootDir, "input");
            m_outputDirectory = Path.Combine(dataRootDir, "output");
            fileHelper.EnsureDirectory(dataRootDir);
            fileHelper.EnsureDirectory(m_inputDirectory);
            fileHelper.EnsureDirectory(m_outputDirectory);
            workDirLabel.Text = dataRootDir;

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

                if (File.Exists(Path.Combine(path, "options.xini")) && 
                    (Directory.Exists(Path.Combine(path, "Source")) || Directory.Exists(Path.Combine(path, "usfx"))))
                {
                    m_projectsList.SetItemChecked(m_projectsList.Items.Count - 1, all || !m_options.lastRunResult);
                    projReady++;
                }
                else
                {
                    m_projectsList.SetItemChecked(m_projectsList.Items.Count - 1, false);
                }
            }
            m_projectsList.EndUpdate();
            statsLabel.Text = projReady.ToString() + " of " + projCount.ToString() + " project directories are ready to run.";
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
        }

        /// <summary>
        /// Reads the \id line to get the standard abbreviation of this file to figure out what
        /// a good name for its standardized file name might be.
        /// </summary>
        /// <param name="pathName">Full path to the file to read the \id line from.</param>
        /// <returns>Sort order plus 3-letter abbreviation of the Bible book (or front/back matter), upper case,
        /// unless the file lacks an \id line, in which case in returns and empty string.</returns>
        public string MakeUpUsfmFileName(string pathName)
        {
            if (bkInfo == null)
                bkInfo = new WordSend.BibleBookInfo();
            // Use the ID line.
            string result = "";
            string line;
            string chap = "";
            StreamReader sr = new StreamReader(pathName);
            while ((!sr.EndOfStream) && (result.Length < 1))
            {
                line = sr.ReadLine();
                if (line.StartsWith(@"\id ") && (line.Length > 6))
                {
                    result = line.Substring(4, 3).ToUpper(System.Globalization.CultureInfo.InvariantCulture);
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
                sourceDate = fileDate;
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
                            sourceDate = fileDate;
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
			StreamWriter output = new StreamWriter(outputPath);
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


        public void PreprocessUsfmFiles()
        {
            string SourceDir = Path.Combine(m_inputProjectDirectory, "Source");
            string UsfmDir = Path.Combine(m_outputProjectDirectory, "usfm1");
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
                string fileType = Path.GetExtension(filename).ToUpper();
                if ((fileType != ".BAK") && (fileType != ".LDS") &&
                    (fileType != ".SSF") && (fileType != ".DBG") &&
                    (fileType != ".WDL") && (fileType != ".STY") &&
                    (fileType != ".XML") && (fileType != ".HTM") &&
                    (fileType != ".KB2") && (fileType != ".HTML") &&
                    (fileType != ".CSS") && (fileType != ".SWP") &&
                    (fileType != ".VRS") && (!inputFile.EndsWith("~")) &&
                    (filename.ToLower() != "autocorrect.txt"))
                {
                    currentConversion = "preprocessing " + filename;
                    Application.DoEvents();
                    if (!fAllRunning)
                        break;
                    string outputFileName = MakeUpUsfmFileName(inputFile) + ".usfm";
                    if (outputFileName.Length < 8)
                    {
                        MessageBox.Show(this, "No proper \\id line found in "+inputFile, "ERROR");
                        break;
                    }
                    string outputFilePath = Path.Combine(UsfmDir, outputFileName);
                    PreprocessOneFile(inputFile, m_options.preprocessingTables, outputFilePath);
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
            return fAllRunning;
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
            string UsfmDir = Path.Combine(m_outputProjectDirectory, "usfm1");
            string UsfxPath = GetUsfxDirectoryPath();
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
            SFConverter.scripture = new Scriptures();
            Logit.loggedError = false;
            SFConverter.scripture.assumeAllNested = m_options.relaxUsfmNesting;
            // Read the input USFM files into internal data structures.
            SFConverter.ProcessFilespec(Path.Combine(UsfmDir, "*.usfm"), Encoding.UTF8);
            currentConversion = "converting from USFM to USFX; writing USFX";
            Application.DoEvents();

            // Write out the USFX file.
            SFConverter.scripture.languageCode = m_options.languageId;
            SFConverter.scripture.WriteUSFX(GetUsfxFilePath());
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

        public string expandPercentEscapes(string s)
        {
            s = s.Replace("%d", m_project);
            s = s.Replace("%e", m_options.languageId);
            s = s.Replace("%h", m_options.homeDomain);
            string sc, lc;
            if (m_options.publicDomain)
                lc = sc = "Public Domain";
            else if (m_options.silentCopyright)
                lc = sc = String.Empty;
            else if (m_options.copyrightOwnerAbbrev.Length > 0)
            {
                sc = "© " + m_options.copyrightYears + " " + m_options.copyrightOwnerAbbrev;
                lc = "Copyright © " + m_options.copyrightYears + " " + m_options.copyrightOwner;
            }
            else
            {
                sc = "© " + m_options.copyrightYears + " " + m_options.copyrightOwner;
                lc = "Copyright " + sc;
            }
            if ((lc.Length > 0) && (m_options.copyrightOwnerUrl.Length > 0))
            {
                lc = "copyright © " + m_options.copyrightYears + " <a href=\"" + m_options.copyrightOwnerUrl + "\">" + m_options.copyrightOwner + "</a>";
            }
            s = s.Replace("%c", sc);
            s = s.Replace("%C", lc);

            s = s.Replace("%l", m_options.languageName);
            s = s.Replace("%L", m_options.languageNameInEnglish);
            s = s.Replace("%D", m_options.dialect);
            s = s.Replace("%a", m_options.contentCreator);
            s = s.Replace("%A", m_options.contributor);
            s = s.Replace("%v", m_options.vernacularTitle);
            s = s.Replace("%n", m_options.EnglishDescription);
            s = s.Replace("%N", m_options.lwcDescription);
            s = s.Replace("%p", m_options.privateProject ? "private" : "public");
            s = s.Replace("%r", (!m_options.privateProject) && (m_options.publicDomain || m_options.creativeCommons) ? "redistributable" : "restricted");
            s = s.Replace("%T", m_options.contentUpdateDate.ToString("yyyy-MM-dd"));
            s = s.Replace("%o", m_options.rightsStatement);
            s = s.Replace("%w", m_options.printPublisher);
            s = s.Replace("%i", m_options.electronicPublisher);
            string result = s.Replace("%t", m_options.translationId);
            return result;
        }

    	private void ConvertUsfxToPortableHtml()
        {
            currentConversion = "writing portable HTML";
            if ((m_options.languageId.Length < 3) || (m_options.translationId.Length < 3))
                return;
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
            string propherocss = Path.Combine(htmlPath, "prophero.css");
            if (File.Exists(propherocss))
                File.Delete(propherocss);
            // Copy prophero.css from project directory, or if not there, BibleConv/input/prophero.css.
            string specialCss = Path.Combine(m_inputProjectDirectory, "prophero.css");
            if (File.Exists(specialCss))
                File.Copy(specialCss, propherocss);
            else
                File.Copy(Path.Combine(m_inputDirectory, "prophero.css"), propherocss);

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
				toHtm = new usfxToHtmlConverter();
			}
            toHtm.stripPictures = false;
            toHtm.htmlextrasDir = Path.Combine(m_inputProjectDirectory, "htmlextras");
            string logFile = Path.Combine(m_outputProjectDirectory, "HTMLConversionReport.txt");
            Logit.OpenFile(logFile);
            Logit.GUIWriteString = showMessageString;
            Logit.UpdateStatus = updateConversionProgress;
            toHtm.indexDateStamp = "HTML generated " + DateTime.UtcNow.ToString("d MMM yyyy") +
                " from source files dated " + sourceDate.ToString("d MMM yyyy");
        	toHtm.GeneratingConcordance = m_options.GenerateConcordance;
    		toHtm.CrossRefToFilePrefixMap = m_options.CrossRefToFilePrefixMap;
    		string usfxFilePath = Path.Combine(UsfxPath, "usfx.xml");
            string orderFile = Path.Combine(m_inputProjectDirectory, "bookorder.txt");
            if (!File.Exists(orderFile))
                orderFile = SFConverter.FindAuxFile("bookorder.txt");
            toHtm.bookInfo.ReadPublicationOrder(orderFile);
            toHtm.MergeXref(Path.Combine(m_inputProjectDirectory, "xref.xml"));
            toHtm.sourceLink = expandPercentEscapes("<a href=\"http://%h/%t\">%v</a>");
            toHtm.textDirection = m_options.textDir;
            toHtm.customCssName = m_options.customCssFileName;
            toHtm.stripManualNoteOrigins = m_options.stripNoteOrigin;
            toHtm.noteOriginFormat = m_options.xoFormat;
    		toHtm.ConvertUsfxToHtml(usfxFilePath, htmlPath,
                m_options.vernacularTitle,
                m_options.languageId,
                m_options.translationId,
                m_options.chapterLabel,
                m_options.psalmLabel,
                expandPercentEscapes(m_options.copyrightLink),
                expandPercentEscapes(m_options.homeLink),
                expandPercentEscapes(m_options.footerHtml),
                expandPercentEscapes(m_options.indexHtml),
                expandPercentEscapes(m_options.licenseHtml),
                m_options.ignoreExtras,
                m_options.goText);
            Logit.CloseFile();
            if (Logit.loggedError)
            {
                m_options.lastRunResult = false;
            }

            currentConversion = "Writing auxilliary metadata files.";
            Application.DoEvents();
            if (!fAllRunning)
                return;

            // We currently have the information handy to write some auxilliary XML files
            // that contain metadata. We will put these in the USFX directory.

            XmlTextWriter xml = new XmlTextWriter(Path.Combine(UsfxPath, m_options.translationId + "-VernacularParms.xml"), System.Text.Encoding.UTF8);
            xml.Formatting = Formatting.Indented;
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
            string copyright = "Copyright © " + m_options.copyrightOwner + " " + m_options.copyrightYears + ".";
            if (m_options.publicDomain)
            {
                copyright = rights = "Public Domain";
                shortRights = shortRights + "is in the Public Domain.";
            }
            else if (m_options.creativeCommons)
            {
                rights = copyright + @"
This work is made available to you under the terms of the Creative Commons Attribution-Noncommercial-No Derivative Works license at http://creativecommons.org/licenses/by-nc-nd/3.0/.
In addition, you have permission to convert the text to different file formats, as long as you don't change any of the text or punctuation of the content." +
                "\r\n" + m_options.rightsStatement;
                shortRights = shortRights + copyright + " Creative Commons BY-NC-ND license.";
            }
            else if (m_options.otherLicense)
            {
                rights = copyright + "\r\n" + m_options.rightsStatement;
                shortRights = shortRights + copyright;
            }
            else if (m_options.allRightsReserved)
            {
                rights = copyright + " All rights reserved.";
                shortRights = shortRights + rights;
                if (m_options.rightsStatement.Length > 0)
                    rights = rights + "\r\n" + m_options.rightsStatement;
            }
            xml.WriteElementString("dc:rights", rights);
            xml.WriteElementString("dc:identifier", String.Empty);
            xml.WriteElementString("dc:type", String.Empty);
            xml.WriteEndElement();  // dcMeta
            // TODO: Generalize the following line for more than 2 number systems.
            xml.WriteElementString("numberSystem", m_options.useKhmerDigits ? "Khmer" : "European");
            xml.WriteElementString("chapterAndVerseSeparator", m_options.chapterAndVerseSeparator);
            xml.WriteElementString("rangeSeparator", m_options.rangeSeparator);
            xml.WriteElementString("multiRefSameChapterSeparator", m_options.multiRefSameChapterSeparator);
            xml.WriteElementString("multiRefDifferentChapterSeparator", m_options.multiRefDifferentChapterSeparator);
            xml.WriteElementString("verseNumberLocation", m_options.verseNumberLocation);
            xml.WriteElementString("footnoteMarkerStyle", m_options.footnoteMarkerStyle);
            xml.WriteElementString("footnoteMarkerResetAt", m_options.footnoteMarkerResetAt);
            xml.WriteElementString("footnoteMarkers", m_options.footnoteMarkers);
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
            xml.Formatting = Formatting.Indented;
            xml.WriteStartDocument();
            xml.WriteStartElement("vernacularParmsMiscellaneous");
            xml.WriteElementString("translationId", m_options.translationId);
            xml.WriteElementString("otmlId", " ");
            xml.WriteElementString("versificationScheme", m_options.versificationScheme);
            xml.WriteElementString("checkVersification", "No");
            xml.WriteElementString("osis2SwordOptions", m_options.osis2SwordOptions);
            xml.WriteElementString("otmlRenderChapterNumber", m_options.otmlRenderChapterNumber);
            xml.WriteElementString("copyright", shortRights);
            xml.WriteEndElement();	// vernacularParmsMiscellaneous
            xml.WriteEndDocument();
            xml.Close();
            xml.Close();

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
				if (m_options.GenerateConcordance)
					ciMaker.ConcordanceLinkText = m_options.ConcordanceLinkText;
				string chapIndexPath = Path.Combine(htmlPath, UsfxToChapterIndex.ChapIndexFileName);
				ciMaker.Generate(usfxFilePath, chapIndexPath);
				EnsureTemplateFile("chapIndex.css", htmlPath);
				EnsureTemplateFile("frameFuncs.js", htmlPath);
				EnsureTemplateFile("Navigation.js", htmlPath);
			}

			// Todo JohnT: move this to a new method, and the condition to the method that calls this.
			if (generateConcordanceCheckBox.Checked)
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
				concFrameGenerator.Run();
				EnsureTemplateFile("mktree.css", concordanceDirectory);
				EnsureTemplateFile("plus.gif", concordanceDirectory);
				EnsureTemplateFile("minus.gif", concordanceDirectory);
				EnsureTemplateFile("display.css", concordanceDirectory);
				EnsureTemplateFile("TextFuncs.js", htmlPath);
			}
        }

        /// <summary>
        /// Runs a command like it was typed on the command line.
        /// Uses bash as the command interpretor on Linux & Mac OSX; cmd.exe on Windows.
        /// </summary>
        /// <param name="command">Command to run, with or without full path.</param>
        public void RunCommand(string command)
        {
            System.Diagnostics.Process runningCommand = null;
            try
            {
                if (Path.DirectorySeparatorChar == '/')
                {
                    runningCommand = System.Diagnostics.Process.Start("bash", " -c '" + command + "'");
                }
                else
                {
                    runningCommand = System.Diagnostics.Process.Start("cmd.exe", " /c " + command);
                }
                if (runningCommand != null)
                {
                    while (fAllRunning && !runningCommand.HasExited)
                    {
                        System.Windows.Forms.Application.DoEvents();
                        System.Threading.Thread.Sleep(200);
                    }
                    if ((!runningCommand.HasExited) && (!fAllRunning))
                    {
                        runningCommand.Kill();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Unable to run command " + command);
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


        public void DoPostprocess()
        {
            List<string> postproclist = m_options.postprocesses;
            string command;
            foreach (string proc in postproclist)
            {
                command = proc.Replace("%d", m_project);
                command = command.Replace("%t", m_options.translationId);
                command = command.Replace("%e", m_options.languageId);
                command = command.Replace("%h", m_options.homeDomain);
                command = command.Replace("%p", m_options.privateProject ? "private" : "public");
                command = command.Replace("%r", (!m_options.privateProject) && (m_options.publicDomain || m_options.creativeCommons) ? "redistributable" : "restricted");
                currentConversion = "Running " + command;
                batchLabel.Text = currentConversion;
                Application.DoEvents();
                RunCommand(command);
                currentConversion = String.Empty;
                if (!fAllRunning)
                    return;
            }
            

        }

        private void NormalizeUsfm()
        {
            string logFile;
            try
            {
                
                string UsfmDir = Path.Combine(m_outputProjectDirectory, "usfm");
                string UsfxName = Path.Combine(Path.Combine(m_outputProjectDirectory, "usfx"), "usfx.xml");
                if (!File.Exists(UsfxName))
                {
                    MessageBox.Show(this, UsfxName + " not found!", "ERROR normalizing USFM from USFX");
                    return;
                }
                // Start with an EMPTY USFM directory to avoid problems with old files 
                Utils.DeleteDirectory(UsfmDir);
                fileHelper.EnsureDirectory(UsfmDir);
                currentConversion = "Normalizing USFM from USFX. ";
                Application.DoEvents();
                if (!fAllRunning)
                    return;
                logFile = Path.Combine(m_outputProjectDirectory, "usfx2usfm2_log.txt");
                Logit.OpenFile(logFile);
                Logit.GUIWriteString = showMessageString;
                Logit.UpdateStatus = updateConversionProgress;
                SFConverter.scripture = new Scriptures();
                Logit.loggedError = false;
                SFConverter.scripture.USFXtoUSFM(UsfxName, UsfmDir, m_options.translationId + ".usfm");
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

        private void ImportUsfx(string SourceDir)
        {
            string logFile;
            try
            {
                
                string UsfmDir = Path.Combine(m_outputProjectDirectory, "usfm");
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
                    string fileType = Path.GetExtension(filename).ToUpper();
                    if ((fileType == ".USFX") || (fileType == ".XML"))
                    {
                        currentConversion = "processing " + filename;
                        Application.DoEvents();
                        if (!fAllRunning)
                            break;
                        XmlTextReader xr = new XmlTextReader(inputFile);
                        if (xr.MoveToContent() == XmlNodeType.Element)
                        {
                            if (xr.Name == "usfx")
                            {

                                logFile = Path.Combine(m_outputProjectDirectory, "usfx2usfm_log.txt");
                                Logit.OpenFile(logFile);
                                Logit.GUIWriteString = showMessageString;
                                Logit.UpdateStatus = updateConversionProgress;
                                SFConverter.scripture = new Scriptures();
                                Logit.loggedError = false;
                                currentConversion = "converting from USFX to USFM";
                                Application.DoEvents();
                                SFConverter.scripture.USFXtoUSFM(inputFile, UsfmDir, m_options.translationId + ".usfm");
                                Logit.CloseFile();
                                if (Logit.loggedError)
                                {
                                    m_options.lastRunResult = false;
                                }
                                currentConversion = "converted USFX to USFM.";
                            }
                            else if (xr.Name == "vernacularParms")
                            {
                                // TODO: Insert code here to read metadata in this file into options file.
                            }
                            else if (xr.Name == "vernacularParmsMiscellaneous")
                            {
                                // TODO: Insert code here to read this file into options file.
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
            toMosis.copyrightNotice = m_options.publicDomain ? "public domain" : "Copyright © " + m_options.copyrightYears + " " + m_options.copyrightOwner;
            if (m_options.publicDomain)
            {
                toMosis.rightsNotice = @"This work is in the Public Domain. That means that it is not copyrighted.
 It is still subject to God's Law concerning His Word, including the Great Commission (Matthew 28:18-20).
";
            }
            else if (m_options.creativeCommons)
            {
                toMosis.rightsNotice = @"This Bible translation is made available to you under the terms of the
 Creative Commons Attribution-Noncommercial-No Derivative Works license (http://creativecommons.org/licenses/by-nc-nd/3.0/).
 In addition, you have permission to make derivative works that are only extracts and/or file format changes, but which do not alter any of the words or punctuation.
";
            }
            else
            {
                toMosis.rightsNotice = String.Empty;
            }
            if (m_options.rightsStatement.Length > 0)
            {
                toMosis.rightsNotice += m_options.rightsStatement;
            }
            string logFile = Path.Combine(m_outputProjectDirectory, "MosisConversionReport.txt");
            Logit.OpenFile(logFile);
            Logit.GUIWriteString = showMessageString;
            toMosis.ConvertUsfxToMosis(usfxFilePath, mosisFilePath);
            Logit.CloseFile();
            if (Logit.loggedError)
            {
                m_options.lastRunResult = false;
            }
        }

        private void PrepareSearchText()
        {
            ExtractSearchText est = new ExtractSearchText();
            string UsfxPath = Path.Combine(m_outputProjectDirectory, "usfx");
            string auxPath = Path.Combine(m_outputProjectDirectory, "search");
            Utils.EnsureDirectory(auxPath);
            est.Filter(Path.Combine(UsfxPath, "usfx.xml"), Path.Combine(auxPath, "verseText.xml"));
        }


        

        /// <summary>
        /// Take the project input (exactly one of USFM, USFX, or USX) and create
        /// the distribution formats we need.
        /// </summary>
        /// <param name="projDirName">full path to project input directory</param>
        private void ProcessOneProject(string projDirName)
        {
            SetCurrentProject(projDirName);
        	m_xiniPath = Path.Combine(m_inputProjectDirectory, "options.xini");
            displayOptions();
            logProjectStart("Processing " + m_options.translationId + " in " + m_inputProjectDirectory);
            Application.DoEvents();
            if (!fAllRunning)
                return;
            // Find out what kind of input we have (USFX, USFM, or USX)
            // and produce USFX, USFM, (and in the future) USX outputs.
            GetUsfx(projDirName);
            NormalizeUsfm();
            UpdateBooksList();
        	Application.DoEvents();
            // Create verseText.xml with unformatted canonical text only in verse containers.
            if (fAllRunning)
                PrepareSearchText();
            // Create HTML output for posting on web sites.
            if (fAllRunning)
                ConvertUsfxToPortableHtml();
            Application.DoEvents();
            // Create Modified OSIS output for conversion to Sword format.
            if (fAllRunning)
                ConvertUsfxToMosis();
            Application.DoEvents();
            // Run proprietary extension conversions, if any.
            if (fAllRunning)
                plugin.DoProprietaryConversions();
            Application.DoEvents();
            // Run custom per project scripts.
            if (fAllRunning)
                DoPostprocess();
            Application.DoEvents();
            m_options.Write();
        }

    	private void SetCurrentProject(string projDirName)
    	{
    		m_project = projDirName;
    		m_inputProjectDirectory = Path.Combine(m_inputDirectory, m_project);
    		m_outputProjectDirectory = Path.Combine(m_outputDirectory, m_project);
    		fileHelper.EnsureDirectory(m_outputProjectDirectory);
    	}

    	private void GetUsfx(string projDirName)
    	{
			SetCurrentProject(projDirName);
			string source = Path.Combine(m_inputProjectDirectory, "Source");
    		if (Directory.Exists(source))
    		{
    			PreprocessUsfmFiles();
    		}
    		else
    		{
    			source = Path.Combine(m_inputProjectDirectory, "usfx");
    			if (Directory.Exists(source))
    			{
    				ImportUsfx(source);
    			}
    			else
    			{
    				source = Path.Combine(m_inputProjectDirectory, "usx");
    				if (Directory.Exists(source))
    				{
    					//TODO: Create ImportUsx(source);
    				}
    			}
    		}
    		Application.DoEvents();
    		if (fAllRunning)
    			ConvertUsfmToUsfx();
    	}

    	private void WorkOnAllButton_Click(object sender, EventArgs e)
        {
            if (fAllRunning)
            {
                fAllRunning = false;
                WorkOnAllButton.Enabled = false;
                WorkOnAllButton.Text = "Stopping...";
                Application.DoEvents();
                return;
            }
            fAllRunning = true;
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
            Application.DoEvents();
            timer1.Enabled = true;
            WorkOnAllButton.Text = "Stop";
            SaveOptions();
            foreach (object o in m_projectsList.CheckedItems)
            {
                ProcessOneProject((string)o);
                Application.DoEvents();
                if (!fAllRunning)
                    break;
            }
            currentConversion = String.Empty;
            fAllRunning = false;
            batchLabel.Text = (DateTime.UtcNow - startTime).ToString() + " " + "Done.";
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
                Close();
            else
            {
                string index = Path.Combine(Path.Combine(m_outputProjectDirectory, "html"), "index.htm");
                if (File.Exists(index))
                    System.Diagnostics.Process.Start(index);
            }
        }

        private void reloadButton_Click(object sender, EventArgs e)
        {
            SaveOptions();
            LoadWorkingDirectory(true);
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
            int i;
            SaveOptions();
            for (i = 0; i < m_projectsList.Items.Count; i++)
                m_projectsList.SetItemChecked(i, false);
        }

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
            ethnologueCodeTextBox.Text = m_options.languageId;
            translationIdTextBox.Text = m_options.translationId;
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
            pdRadioButton.Checked = m_options.publicDomain;
            ccRadioButton.Checked = m_options.creativeCommons;
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
            
            m_currentTemplate = xini.ReadString("currentTemplate", String.Empty);
            templateLabel.Text = "Current template: " + m_currentTemplate;
            copyFromTemplateButton.Enabled = (m_currentTemplate.Length > 0) && (m_currentTemplate != m_project);
            makeTemplateButton.Enabled = m_currentTemplate != m_project;
            if (!fAllRunning)
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

            altLinkTextBox.Text = String.Empty;
            altLinkListBox.SuspendLayout();
            altLinkListBox.Items.Clear();
            foreach (string a in m_options.altLinks)
                altLinkListBox.Items.Add(a);
            altLinkListBox.ResumeLayout();
            
            postprocessListBox.SuspendLayout();
            postprocessListBox.Items.Clear();
            foreach (string filename in m_options.postprocesses)
                postprocessListBox.Items.Add(filename);
            postprocessListBox.ResumeLayout();

            // Insert more checkbox settings here.
            homeLinkTextBox.Text = m_options.homeLink;
            copyrightLinkTextBox.Text = m_options.copyrightLink;
            goTextTextBox.Text = m_options.goText;
            footerHtmlTextBox.Text = m_options.footerHtml;
            indexPageTextBox.Text = m_options.indexHtml;
            licenseTextBox.Text = m_options.licenseHtml;
            versificationComboBox.Text = m_options.versificationScheme;
            numberSystemComboBox.Text = fileHelper.SetDigitLocale(m_options.numberSystem);
            textDirectionComboBox.Text = m_options.textDir;
            //arabicNumeralsRadioButton.Checked = m_options.useArabicDigits;
            //khmerNumeralsRadioButton.Checked = m_options.useKhmerDigits;
            privateCheckBox.Checked = m_options.privateProject;
            homeDomainTextBox.Text = m_options.homeDomain;
            relaxNestingSyntaxCheckBox.Checked = m_options.relaxUsfmNesting;

        	LoadConcTab();
			LoadBooksTab();
        	LoadFramesTab();
        }

		private void LoadConcTab()
		{
			generateConcordanceCheckBox.Checked = m_options.GenerateConcordance;
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
			m_options.GenerateConcordance = generateConcordanceCheckBox.Checked;
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
					MessageBox.Show("Duplicate book name: " + crossRefName, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
					continue;
				}
				// Enhance JohnT: the way I'm reversing the book name to ID thing here means that it will
				// crash if the user supplies the same book name for two distinct books. It would be nicer to
				// give an elegant message. Should probably do something special about empty strings also.
				crossRefsToIds.Add(crossRefName, key);
			}
			m_options.Books = books;
			m_options.ReferenceAbbeviationsMap = idsToVernAbbrs;
			m_options.CrossRefToFilePrefixMap = crossRefsToIds;
		}

		private void SaveFramesTab()
		{
			m_options.UseFrames = useFramesCheckBox.Checked;
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
			useFramesCheckBox.Checked = m_options.UseFrames;
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

        private void SaveOptions()
        {
            if (m_options == null)
                return;
            m_options.languageId = ethnologueCodeTextBox.Text;
            m_options.translationId = translationIdTextBox.Text;
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
            m_options.creativeCommons = ccRadioButton.Checked;
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
Di
            List<string> tableNames = new List<string>();
            foreach (string filename in listInputProcesses.Items)
                tableNames.Add(filename);
            m_options.preprocessingTables = tableNames;
            
            List<string> postprocessNames = new List<string>();
            foreach (string filename in postprocessListBox.Items)
                postprocessNames.Add(filename);
            m_options.postprocesses = postprocessNames;
            
            List<string> alternateLinks = new List<string>();
            foreach (string alternateLink in altLinkListBox.Items)
                alternateLinks.Add(alternateLink);
            m_options.altLinks = alternateLinks;
            
            // Insert more checkbox settings here.
            m_options.homeLink = homeLinkTextBox.Text;
            m_options.goText = goTextTextBox.Text;
            m_options.copyrightLink = copyrightLinkTextBox.Text;
            m_options.footerHtml = footerHtmlTextBox.Text;
            m_options.indexHtml = indexPageTextBox.Text;
            m_options.licenseHtml = licenseTextBox.Text;
            m_options.versificationScheme = versificationComboBox.Text;
            m_options.numberSystem = fileHelper.SetDigitLocale(numberSystemComboBox.Text.Trim());
            m_options.privateProject = privateCheckBox.Checked;
            m_options.relaxUsfmNesting = relaxNestingSyntaxCheckBox.Checked;

            m_options.homeDomain = homeDomainTextBox.Text.Trim();

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
        private void btnAddSpecialProcess_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(this,
                            "Currently Haiola only has one special process, one which re-arranges \vt fields containing multiple |fn anchors followed by corresponding \ft fields.",
                            "Note", MessageBoxButtons.OKCancel) == DialogResult.OK)
            {
                listInputProcesses.Items.Add(kFootnotesProcess);
            }
        }

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
            return fAllRunning;
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
            if (fAllRunning)
                runtime = (DateTime.UtcNow - startTime).ToString().Substring(0, 8) + " " + m_project + " ";
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
            if (fAllRunning)
            {
                fAllRunning = false;
                WorkOnAllButton.Enabled = false;
                WorkOnAllButton.Text = "Stopping...";
                Application.DoEvents();
                return;
            }
            fAllRunning = true;
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
            SaveOptions();
            ProcessOneProject(SelectedProject);

            currentConversion = String.Empty;
            fAllRunning = false;
            batchLabel.Text = (DateTime.UtcNow - startTime).ToString() + " " + "Done.";
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
            System.Diagnostics.Process.Start(Path.Combine(Path.Combine(m_outputProjectDirectory, "html"), "index.htm"));
        }

    	private string SelectedProject
    	{
    		get { return (string)m_projectsList.SelectedItem; }
    	}

    	private void statsButton_Click(object sender, EventArgs e)
        {
            int numProjects = 0;
            int numTranslations = 0;
            int urlid = 0;
            int numLanguages = 0;
            int numDialects = 0;
            int numSites = 0;
            int c;
            int coprCount = 0;
            string dialect;
            string homedomain;
            string copr = String.Empty;
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
            StreamWriter sqlFile = new StreamWriter(Path.Combine(m_outputDirectory, "Bible_list.sql"), false, System.Text.Encoding.UTF8);
            StreamWriter altUrlFile = new StreamWriter(Path.Combine(m_outputDirectory, "urllist.sql"), false, System.Text.Encoding.UTF8);
            StreamWriter scorecard = new StreamWriter(Path.Combine(m_outputDirectory, "scorecard.txt"), false, System.Text.Encoding.UTF8);
            sqlFile.WriteLine("USE Prophero;");
            sqlFile.WriteLine("DROP TABLE IF EXISTS 'bible_list';");
            sqlFile.WriteLine(@"CREATE TABLE 'bible_list' ('translationid' VARCHAR(64) NOT NULL,
'languagecode' VARCHAR(4) NOT NULL, 'languagename' VARCHAR(128), 'languagenameinenglish' VARCHAR(128),
'dialect' VARCHAR(128), 'homedomain' VARCHAR(128), 'title' VARCHAR(256), 'description' VARCHAR(1024),
'free' BOOL, 'copyright' VARCHAR(1024), 'updatedate' DATE, 'publicationurl' VARCHAR(1024), PRIMARY KEY('translationid')) DEFAULT CHARSET=utf8;");
            sqlFile.WriteLine("LOCK TABLES 'bible_list' WRITE;");

            altUrlFile.WriteLine("USE Prophero;");
            altUrlFile.WriteLine(@"DROP TABLE IF EXISTS 'urllist';");
            altUrlFile.WriteLine(@"CREATE TABLE 'urllist' ('urlid' INT UNSIGNED NOT NULL,
'languagecode' VARCHAR(4) NOT NULL, 'translationid' VARCHAR(64) NOT NULL, 'url' VARCHAR(1024) NOT NULL);");
            sqlFile.WriteLine("LOCK TABLES 'urllist' WRITE;");

            sw.WriteLine("\"languageCode\",\"translationId\",\"languageName\",\"languageNameInEnglish\",\"dialect\",\"homeDomain\",\"title\",\"description\",\"Free\",\"Copyright\",\"UpdateDate\",\"publicationURL\"");
            foreach (object o in m_projectsList.Items)
            {
                m_project = (string)o;
                m_inputProjectDirectory = Path.Combine(m_inputDirectory, m_project);
                m_outputProjectDirectory = Path.Combine(m_outputDirectory, m_project);

                m_xiniPath = Path.Combine(m_inputProjectDirectory, "options.xini");
                displayOptions();
                numProjects++;
                if ((!m_options.privateProject) && (m_options.languageId.Length > 1))
                {
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
                    sw.WriteLine("\"{0}\",\"{1}\",\"{2}\",\"{3}\",\"{4}\",\"{5}\",\"{6}\",\"{7}\",\"{8}\",\"{9}\",\"{10}\",\"http://{5}/{1}/\"",
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
                        m_options.contentUpdateDate.ToString("yyyy-MM-dd"));
                    sqlFile.WriteLine("INSERT INTO 'bible_list' VALUES \"{0}\",\"{1}\",\"{2}\",\"{3}\",\"{4}\",\"{5}\",\"{6}\",\"{7}\",\"{8}\",\"{9}\",\"{10}\",\"http://{5}/{0}/\";",
                        m_options.translationId,
                        m_options.languageId,
                        fileHelper.sqlString(m_options.languageName),
                        fileHelper.sqlString(m_options.languageNameInEnglish),
                        fileHelper.sqlString(m_options.dialect),
                        fileHelper.sqlString(m_options.homeDomain.Trim()),
                        fileHelper.sqlString(m_options.vernacularTitle.Trim()),
                        fileHelper.sqlString(m_options.EnglishDescription.Trim()),
                        (m_options.publicDomain || m_options.creativeCommons).ToString(),
                        fileHelper.sqlString(m_options.publicDomain ? "public domain" : "Copyright © " + m_options.copyrightYears + " " + m_options.copyrightOwner),
                        m_options.contentUpdateDate.ToString("yyyy-MM-dd"));
                    if (m_options.homeDomain.Length > 0)
                    {
                        altUrlFile.WriteLine("INSERT INTO 'urllist' VALUES '{0}', '{1}', '{2}', '<a href=\\\"http://{3}/{2}/\\\">{4}</a>';",
                            urlid.ToString(), m_options.languageId, m_options.translationId, fileHelper.sqlString(m_options.homeDomain.Trim()), fileHelper.sqlString(m_options.vernacularTitle.Trim()));
                        urlid++;
                    }
                    foreach (string altUrl in m_options.altLinks)
                    {
                        altUrlFile.WriteLine("INSERT INTO 'urllist' VALUES '{0}', '{1}', '{2}', '{3}';", urlid.ToString(), m_options.languageId, m_options.translationId, fileHelper.sqlString(altUrl));
                        urlid++;
                    }
                }
            }
            sw.Close();
            sqlFile.WriteLine("UNLOCK TABLES;");
            altUrlFile.WriteLine("UNLOCK TABLES;");
            sqlFile.Close();
            altUrlFile.Close();
            fAllRunning = false;
            currentConversion = numProjects.ToString() + " projects; " + numTranslations.ToString() + " public. " + urlid.ToString() + " URLs " + numSites.ToString() + " sites " + numLanguages.ToString() + " languages " + numDialects.ToString() + " dialects (including languages). ";

            scorecard.WriteLine("Haiola project statistics as of {0} UTC", DateTime.UtcNow.ToString("R"));
            scorecard.WriteLine("{0} URLs", urlid.ToString());
            scorecard.WriteLine("{0} sites", numSites.ToString());
            scorecard.WriteLine("{0} languages", numLanguages.ToString());
            scorecard.WriteLine("{0} dialects", numDialects.ToString());
            scorecard.WriteLine("{0} public translations", numTranslations.ToString());
            scorecard.WriteLine("{0} projects", numProjects.ToString());
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

        private void helpButton_Click(object sender, EventArgs e)
        {
            showHelp("haiola.htm");
        }

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
            copyrightLinkTextBox.Text = m_options.copyrightLink = templateOptions.copyrightLink;
            goTextTextBox.Text = m_options.goText = templateOptions.goText;
            footerHtmlTextBox.Text = m_options.footerHtml = templateOptions.footerHtml;
            indexPageTextBox.Text = m_options.indexHtml = templateOptions.indexHtml;
            licenseTextBox.Text = m_options.licenseHtml = templateOptions.licenseHtml;
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
            LoadWorkingDirectory(false);
        }

    }
}
