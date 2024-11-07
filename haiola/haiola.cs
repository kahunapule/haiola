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
        public global globe;
        //public PluginManager plugin;


        public haiolaForm()
        {
            XMLini.readOnly = true;
            InitializeComponent();
            globe = new global();
            globe.UpdateStatus = updateConversionProgress;
            globe.GUIWriteString = showMessageString;
            Logit.UpdateStatus = updateConversionProgress;

            MasterInstance = this;
            //plugin = new PluginManager();
            batchLabel.Text = String.Format("Haiola version {0}.{1} © 2003-{2} SIL, EBT, && eBible.org. Released under Gnu LGPL 3 or later.",
                Version.date, Version.time, Version.year);
            Logit.versionString = batchLabel.Text;
            //extensionLabel.Text = plugin.PluginMessage();
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
            dlg.SelectedPath = globe.dataRootDir;
            dlg.Description =
                @"Please select a folder to contain your working directories.";
            dlg.ShowNewFolderButton = true;
            if (dlg.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return false;
            globe.dataRootDir = dlg.SelectedPath;
            globe.xini.WriteString("globe.dataRootDir", globe.dataRootDir);
            globe.xini.Write();
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



        private string pngbtaLogo;

        private void haiolaForm_Load(object sender, EventArgs e)
        {
            int i;
            globe.xini = new XMLini(Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "haiola"),
    "haiola.xini"));
            globe.dataRootDir = globe.xini.ReadString("globe.dataRootDir", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BibleConv"));
            globe.xini.WriteString("globe.dataRootDir", globe.dataRootDir);
            globe.xini.Write();
            globe.inputDirectory = Path.Combine(globe.dataRootDir, "input");
            if (!Directory.Exists(globe.inputDirectory))
                if (!GetRootDirectory())
                    Application.Exit();
            dataDirLabel.Text = "Data root folder: " + globe.dataRootDir;
            pngbtaLogo = Path.Combine(globe.inputDirectory, "pngbta.jpg");
            eth = new Ethnologue();
            LoadWorkingDirectory(false, false, false);
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
                headerFontComboBox.Items.Add(fontNames[i]);
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
            string lockFile;
            loadingDirectory = true;
            bool isReady = false;
            projCount = 0;
            projReady = 0;
            int thisIndex = 0;
            int nextIndex = startIndex;
            projSelected = 0;
            readssf Ssf = new readssf();
            SaveOptions();
            m_projectsList.BeginUpdate();
            m_projectsList.Items.Clear();
            m_projectsList.Sorted = false;
            globe.inputDirectory = Path.Combine(globe.dataRootDir, "input");
            globe.outputDirectory = Path.Combine(globe.dataRootDir, "output");
            fileHelper.EnsureDirectory(globe.dataRootDir);
            fileHelper.EnsureDirectory(globe.inputDirectory);
            fileHelper.EnsureDirectory(globe.outputDirectory);

            globe.EnsureTemplateFile("haiola.css");
            globe.EnsureTemplateFile("prophero.css");
            globe.EnsureTemplateFile("fixquotes.re");

            foreach (object o in m_projectsList.CheckedItems)
            {
                globe.SetcurrentProject((string)o);
                globe.projectXiniPath = Path.Combine(globe.inputProjectDirectory, "options.xini");
                displayOptions(false);
                lockFile = Path.Combine(globe.inputProjectDirectory, "lock");
                Utils.DeleteFile(lockFile);
                globe.projectOptions.done = false;
                globe.projectOptions.Write();
            }



            foreach (string path in Directory.GetDirectories(globe.inputDirectory))
            {
                string project = Path.GetFileName(path);
                m_projectsList.Items.Add(project);
                projCount++;
                globe.currentProject = project;
                globe.inputProjectDirectory = Path.Combine(globe.inputDirectory, globe.currentProject);
                globe.outputProjectDirectory = Path.Combine(globe.outputDirectory, globe.currentProject);
                globe.projectXiniPath = Path.Combine(globe.inputProjectDirectory, "options.xini");
                if (globe.projectOptions == null)
                {
                    globe.projectOptions = new Options(globe.projectXiniPath, globe);
                }
                else
                {
                    globe.projectOptions.Reload(globe.projectXiniPath);
                }

                if ((!String.IsNullOrEmpty(globe.projectOptions.languageId)) && 
                    (
                    Directory.Exists(globe.projectOptions.customSourcePath) ||
                    Directory.Exists(Path.Combine(path, "Source")) ||
                    Directory.Exists(Path.Combine(path, "usfx")) ||
                    Directory.Exists(Path.Combine(path, "usx")) ))
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
                    globe.projectOptions.selected = false;
                }
                else if (all)
                {
                    if (thisIndex == nextIndex)
                    {
                        globe.projectOptions.selected = isReady;
                        nextIndex += increment;
                    }
                    else
                    {
                        globe.projectOptions.selected = false;
                    }
                    thisIndex++;
                }
                else if (failed)
                {
                    if (isReady)
                    {
                        globe.projectOptions.selected = !globe.projectOptions.lastRunResult;
                        /*
                        string pdflog = Path.Combine(globe.outputProjectDirectory, "pdflog.txt");
                        if ((!globe.projectOptions.selected) && File.Exists(pdflog))
                        {
                            FileInfo fi = new FileInfo(pdflog);
                            if (fi.Length > 0)
                                globe.projectOptions.selected = true;
                        }
                        */
                    }
                }
                else
                {
                    globe.projectOptions.selected = isReady && globe.projectOptions.selected;
                }
                m_projectsList.SetItemChecked(m_projectsList.Items.Count - 1, globe.projectOptions.selected);
                if (all || failed)
                {
                    lockFile = Path.Combine(globe.inputProjectDirectory, "lock");
                    globe.projectOptions.done = false;
                    Utils.DeleteFile(lockFile);
                }
                globe.projectOptions.Write();
                globe.projectOptions.done = false;
                if (globe.projectOptions.selected)
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
                MessageBox.Show(this, "No projects found in " + globe.inputDirectory
                                      +
                                      ". You should create a folder there for your project and place your input files in the appropriate subdirectory. Press the 'Help' button.",
                                "No Projects", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                WorkOnAllButton.Enabled = false;
            }
            loadingDirectory = false;
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
                globe.projectOptions.lastRunResult = false;
            }
            else if (Logit.loggedWarning)
            {
                BackColor = messagesListBox.BackColor = Color.Yellow;
                globe.projectOptions.warningsFound = true;
            }
            Application.DoEvents();
            return fileHelper.fAllRunning;
        }

        private void logProjectStart(string s)
        {
            loggedLineCount = 0;
            messagesListBox.Items.Add(DateTime.Now.ToString() + " " + s);
            messagesListBox.SelectedIndex = messagesListBox.Items.Count - 1;
            Logit.loggedError = false;
            globe.projectOptions.lastRunResult = true;
            globe.projectOptions.warningsFound = false;
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








        public string FindSource(string projDirName, bool fingerPrintIt = false)
        {
            globe.SetcurrentProject(projDirName);
            string source = string.Empty;
            if (!String.IsNullOrEmpty(globe.GetSourceKind(globe.projectOptions.customSourcePath, fingerPrintIt)))
                return globe.projectOptions.customSourcePath;
            if (!String.IsNullOrEmpty(globe.projectOptions.paratext8Project))
            {
                source = Path.Combine(globe.paratext8ProjectsDir, globe.projectOptions.paratext8Project);
                if (Directory.Exists(source))
                {
                    globe.projectOptions.customSourcePath = source;
                    globe.projectOptions.Write();
                    return source;
                }
            }
            if (!String.IsNullOrEmpty(globe.projectOptions.paratextProject))
            {
                source = Path.Combine(globe.paratextProjectsDir, globe.projectOptions.paratextProject);
                if (Directory.Exists(source))
                {
                    globe.projectOptions.customSourcePath = source;
                    globe.projectOptions.Write();
                    return source;
                }
            }
            source = Path.Combine(globe.inputProjectDirectory, "Source");
            if (Directory.Exists(source))
            {
                globe.projectOptions.customSourcePath = source;
                globe.projectOptions.Write();
                return source;
            }
            else
            {
                source = Path.Combine(globe.inputProjectDirectory, "usfx");
                if (Directory.Exists(source))
                {
                    globe.projectOptions.customSourcePath = source;
                    globe.projectOptions.Write();
                    return source;
                }
                else
                {
                    source = Path.Combine(globe.inputProjectDirectory, "usx");
                    if (Directory.Exists(source))
                    {
                        globe.projectOptions.customSourcePath = source;
                        globe.projectOptions.Write();
                        return source;
                    }
                }
            }
            return string.Empty;
        }



        ArrayList toDoList;
        ArrayList laterList;
        ArrayList doFirstList;
        private class ProjectEntry
        {
            public string name;
            public string depends;

            /// <summary>
            /// Initialize this projectEntry Instance with the given name and the name of the project this project depends on.
            /// </summary>
            /// <param name="n">This project's name</param>
            /// <param name="d">Name of the project this project depends on, if any.</param>
            public ProjectEntry(string n, string d)
            {
                name = n;
                depends = d;
            }
        }

        public class toDoSorter : IComparer
        {
            // Calls CaseInsensitiveComparer.Compare with the parameters reversed.
            int IComparer.Compare(Object x, Object y)
            {
                ProjectEntry a = (ProjectEntry)x;
                ProjectEntry b = (ProjectEntry)y;
                return ((new CaseInsensitiveComparer()).Compare(a.name, b.name));
            }
        }


        private void InsertProjectEntry(ProjectEntry pe)
        {
            if (string.IsNullOrEmpty(pe.depends))
            {
                toDoList.Add(pe);
            }
            else
            {
                laterList.Add(pe);
            }
        }

        public Ethnologue eth;


        /// <summary>
        /// Take the project input (exactly one of USFM, USFX, or USX) and create
        /// the distribution formats we need.
        /// </summary>
        /// <param name="projDirName">project input directory</param>
        private void ProcessOneProject(string projDirName)
        {
            Logit.GUIWriteString = showMessageString;
            Logit.UpdateStatus = updateConversionProgress;

            globe.SetcurrentProject(projDirName);
            globe.projectXiniPath = Path.Combine(globe.inputProjectDirectory, "options.xini");
            displayOptions(true);

            Fingerprint thumb = new Fingerprint();

            if (!String.IsNullOrEmpty(globe.projectOptions.dependsOn))
            {
                if (fileHelper.isLocked(Path.Combine(globe.inputDirectory, globe.projectOptions.dependsOn)))
                    return;
            }
            if (globe.projectOptions.done || !fileHelper.lockProject(globe.inputProjectDirectory))
            {
                return;
            }
            logProjectStart("Processing " + globe.projectOptions.translationId + " in " + globe.inputProjectDirectory);
            Application.DoEvents();
            if (!fileHelper.fAllRunning)
            {
                fileHelper.unlockProject();
                return;
            }


            if (globe.projectOptions.PrepublicationChecks &&
                (globe.projectOptions.publicDomain || globe.projectOptions.redistributable || File.Exists(Path.Combine(globe.inputProjectDirectory, "certify.txt"))) &&
                File.Exists(globe.eBibleCertified))
            {
                globe.certified = globe.eBibleCertified;
                globe.projectOptions.eBibleCertified = true;
            }
            else
            {
                globe.certified = null;
                globe.projectOptions.eBibleCertified = false;
            }
            globe.projectOptions.Write();

            // Find out what kind of input we have (USFX, USFM, or USX)
            // and produce USFX, USFM, (and in the future) USX outputs.

            globe.orderFile = Path.Combine(globe.inputProjectDirectory, "bookorder.txt");
            if (!File.Exists(globe.orderFile))
                globe.orderFile = SFConverter.FindAuxFile("bookorder.txt");
            StreamReader sr = new StreamReader(globe.orderFile);
            globe.projectOptions.allowedBookList = sr.ReadToEnd();
            sr.Close();

            if (!globe.GetSource())
            {
                Logit.WriteError("No source directory found for " + projDirName + "!");
                fileHelper.unlockProject();
                return;
            }
            if ((globe.projectOptions.currentFingerprint == globe.projectOptions.builtFingerprint) && !globe.rebuild)
            {
                Logit.WriteLine("Skipping up-to-date project " + projDirName + " built: " + globe.projectOptions.lastRunDate.ToString());
                globe.projectOptions.done = true;
                globe.projectOptions.Write();
                fileHelper.unlockProject();
                return;
            }
            Utils.DeleteDirectory(Path.Combine(globe.outputProjectDirectory, "search"));
            Utils.DeleteDirectory(Path.Combine(globe.outputProjectDirectory, "readaloud"));
            Utils.DeleteDirectory(Path.Combine(globe.outputProjectDirectory, "WordML"));
            Utils.DeleteDirectory(Path.Combine(globe.outputProjectDirectory, "sql"));

            Application.DoEvents();
            globe.preferredCover = globe.CreateCover();
            Application.DoEvents();
            // Create verseText.xml with unformatted canonical text only in verse containers.
            if (fileHelper.fAllRunning)
                globe.PrepareSearchText();
            Application.DoEvents();
            // Create epub file
            string epubDir = Path.Combine(globe.outputProjectDirectory, "epub");
            if (fileHelper.fAllRunning && globe.projectOptions.makeEub)
            {
                Utils.DeleteDirectory(epubDir);
                globe.ConvertUsfxToEPub();
            }
            Application.DoEvents();
            // Create HTML output for posting on web sites.
            string htmlDir = Path.Combine(globe.outputProjectDirectory, "html");
            if (fileHelper.fAllRunning && globe.projectOptions.makeHtml)
            {
                Utils.DeleteDirectory(htmlDir);
                globe.ConvertUsfxToPortableHtml();
            }
            Application.DoEvents();
            string WordMLDir = Path.Combine(globe.outputProjectDirectory, "WordML");
            if (fileHelper.fAllRunning && globe.projectOptions.makeWordML)
            {   // Write out WordML document
                // Note: this conversion departs from the standard architecture of making the USFX file the hub, because the WordML writer code was already done in WordSend,
                // and expected USFM input. Therefore, we read the normalized USFM files, which should be present even if the project input is USFX or USX.
                // If this code needs much maintenance in the future, it may be better to refactor the WordML output to go from USFX to WordML directly.
                // Then again, USFX to Open Document Text would be better.
                try
                {
                    Utils.DeleteDirectory(WordMLDir);
                    globe.currentConversion = "Reading normalized USFM";
                    string logFile = Path.Combine(globe.outputProjectDirectory, "WordMLConversionReport.txt");
                    Logit.OpenFile(logFile);
                    SFConverter.scripture = new Scriptures(globe);
                    string seedFile = Path.Combine(globe.inputProjectDirectory, "Scripture.xml");
                    if (!File.Exists(seedFile))
                    {
                        seedFile = Path.Combine(globe.inputDirectory, "Scripture.xml");
                    }
                    if (!File.Exists(seedFile))
                    {
                        seedFile = SFConverter.FindAuxFile("Scripture.xml");
                    }
                    SFConverter.scripture.templateName = seedFile;
                    SFConverter.ProcessFilespec(Path.Combine(Path.Combine(globe.outputProjectDirectory, "usfm"), "*.usfm"));
                    globe.currentConversion = "Writing WordML";
                    Utils.EnsureDirectory(WordMLDir);
                    SFConverter.scripture.WriteToWordML(Path.Combine(WordMLDir, globe.projectOptions.translationId + "_word.xml"));
                }
                catch (Exception ex)
                {

                    Logit.WriteError("Error writing WordML file: " + ex.Message);
                    Logit.WriteError(ex.StackTrace);
                    globe.projectOptions.makeWordML = false;
                }
                makeWordMLCheckBox.Checked = globe.projectOptions.makeWordML;
                Logit.CloseFile();
            }
            Application.DoEvents();
            // Create sile files for conversion to PDF.
            string sileDir = Path.Combine(globe.outputProjectDirectory, "sile");
            if (fileHelper.fAllRunning && globe.projectOptions.makeSile)
            {
                Utils.DeleteDirectory(sileDir);
                globe.ConvertUsfxToSile();
            }
            Application.DoEvents();
            // Create Modified OSIS output for conversion to Sword format.
            string mosisDir = Path.Combine(globe.outputProjectDirectory, "mosis");
            if (fileHelper.fAllRunning && globe.projectOptions.makeSword)
            {
                Utils.DeleteDirectory(mosisDir);
                globe.ConvertUsfxToMosis();
            }
            Application.DoEvents();
            string xetexDir = Path.Combine(globe.outputProjectDirectory, "xetex");
            if (fileHelper.fAllRunning && globe.projectOptions.makePDF)
            {
                globe.ConvertUsfxToPDF(xetexDir);
            }
            Application.DoEvents();
            string browserBibleDir = Path.Combine(globe.outputProjectDirectory, "browserBible");
            DateTime browserBibleCreated = Directory.GetCreationTime(browserBibleDir);
            if (fileHelper.fAllRunning && globe.projectOptions.makeBrowserBible)
            {
                Utils.DeleteDirectory(browserBibleDir);
                globe.currentConversion = "Writing browser Bible module";
                globe.EnsureTemplateFile("haiola.css");
                globe.EnsureTemplateFile("prophero.css");
                string logFile = Path.Combine(globe.outputProjectDirectory, "browserBibleModuleConversionReport.txt");
                Logit.OpenFile(logFile);
                Logit.GUIWriteString = showMessageString;
                Logit.UpdateStatus = updateConversionProgress;

                WordSend.WriteBrowserBibleModule wism = new WriteBrowserBibleModule();
                wism.globe = globe;
                wism.certified = globe.certified;
                wism.WriteTheModule();
            }

            Application.DoEvents();
            // Run custom per project scripts.
            if (fileHelper.fAllRunning)
            {
                globe.DoPostprocess();
                globe.projectOptions.done = true;
                globe.projectOptions.selected = !globe.projectOptions.lastRunResult;
                if (globe.projectOptions.lastRunResult)
                {
                    globe.projectOptions.lastRunDate = DateTime.UtcNow;
                    globe.projectOptions.builtFingerprint = globe.projectOptions.currentFingerprint;
                }
                else
                    globe.projectOptions.lastRunDate = DateTime.MinValue;
                globe.projectOptions.Write();
            }
            fileHelper.unlockProject();
            LoadStatisticsTab();
            Application.DoEvents();
        }


        private void ProcessAllMarked()
        {
            toDoList = new ArrayList();
            laterList = new ArrayList();
            doFirstList = new ArrayList();
            int i;
            int numdone = 0;
            int nummarked = 0;
            TimeSpan ts, remainingTime;
            string command = Path.Combine(globe.inputDirectory, "postprocess.bat");

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
            messagesListBox.Items.Add("Processing all marked projects. Rebuild=" + globe.rebuild.ToString());
            messagesListBox.BackColor = Color.LightGreen;
            tabControl1.SelectedTab = RunTabPage;
            BackColor = Color.LightGreen;
            startTime = DateTime.UtcNow;
            WorkOnAllButton.Text = "Stop";
            Application.DoEvents();
            timer1.Enabled = true;
            //SaveOptions();



            foreach (object o in m_projectsList.CheckedItems)
            {
                globe.SetcurrentProject((string)o);
                globe.projectXiniPath = Path.Combine(globe.inputProjectDirectory, "options.xini");
                displayOptions(false);
                if (!globe.projectOptions.done)
                {
                    InsertProjectEntry(new ProjectEntry((string)o, globe.projectOptions.dependsOn));
                    nummarked++;
                }
            }
            foreach (ProjectEntry pe in laterList)
            {
                bool notFound = true;
                ProjectEntry peTemp;
                i = 0;
                while ((i < toDoList.Count) && notFound)
                {
                    if (pe.depends == ((ProjectEntry)toDoList[i]).name)
                    {
                        notFound = false;
                        peTemp = (ProjectEntry)toDoList[i];
                        toDoList.RemoveAt(i);
                        doFirstList.Add(peTemp);
                    }
                    i++;
                }
            }
            IComparer myComparer = new toDoSorter();
            toDoList.Sort(myComparer);
            laterList.Sort(myComparer);
            foreach (ProjectEntry pe in doFirstList)
            {
                ProcessOneProject(pe.name);
                int j = m_projectsList.Items.IndexOf(pe.name);
                m_projectsList.SetItemChecked(j, false);    // The user must explicitly re-select this project to retry.
                m_projectsList_SelectedIndexChanged(null, null);

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
            foreach (ProjectEntry pe in toDoList)
            {
                ProcessOneProject(pe.name);
                int j = m_projectsList.Items.IndexOf(pe.name);
                m_projectsList.SetItemChecked(j, false);    // The user must explicitly re-select this project to retry.
                m_projectsList_SelectedIndexChanged(null, null);

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
            foreach (ProjectEntry pe in laterList)
            {
                ProcessOneProject(pe.name);
                int j = m_projectsList.Items.IndexOf(pe.name);
                m_projectsList.SetItemChecked(j, false);    // The user must explicitly re-select this project to retry.
                m_projectsList_SelectedIndexChanged(null, null);

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
                globe.currentConversion = "Running " + command;
                batchLabel.Text = globe.currentConversion;
                Application.DoEvents();
                if (!fileHelper.RunCommand(command, "", globe.outputDirectory))
                    MessageBox.Show(fileHelper.runCommandError, "Error " + globe.currentConversion);
            }
            globe.currentConversion = String.Empty;

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
            /*
            else
            {
                string index = Path.Combine(Path.Combine(globe.outputProjectDirectory, "html"), "index.htm");
                if (File.Exists(index))
                    System.Diagnostics.Process.Start(index);
            }
            */
        }


        /// <summary>
        /// Handler for the button press to process all marked projects.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">Event Arguments</param>
    	private void WorkOnAllButton_Click(object sender, EventArgs e)
        {
            if (fileHelper.fAllRunning)
            {
                fileHelper.fAllRunning = false;
            }
            else
            {
                WorkOnAllButton.Text = "Stop";
                Application.DoEvents();
                SaveOptions();
                Logit.UpdateStatus = updateConversionProgress;
                Logit.GUIWriteString = showMessageString;
                ProcessAllMarked();
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
            string completionDate;
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
                                        if (Utils.IsEmpty(traditionalAbbreviationTextBox.Text))
                                            traditionalAbbreviationTextBox.Text = MetadataText();
                                        break;
                                    case "identification/description":
                                        if (Utils.IsEmpty(descriptionTextBox.Text))
                                            descriptionTextBox.Text = MetadataText();
                                        break;
                                    case "identification/dateCompleted":
                                        if (Utils.IsEmpty(copyrightYearTextBox.Text))
                                        {
                                            completionDate = MetadataText();
                                            if (completionDate.Length > 4)
                                                copyrightYearTextBox.Text = completionDate.Substring(0, 4);
                                            else
                                                copyrightYearTextBox.Text = completionDate;
                                        }
                                        break;
                                    case "confidential":
                                        // privateCheckBox.Checked = MetadataText() == "true";
                                        break;
                                    case "contents/bookList":
                                        defaultBookList = metadataXml.GetAttribute("default") == "true";
                                        break;
                                    case "contents/bookList/description":
                                        if (defaultBookList)
                                            globe.projectOptions.canonTypeEnglish = MetadataText();
                                        break;
                                    case "contents/bookList/descriptionLocal":
                                        if (defaultBookList)
                                            globe.projectOptions.canonTypeLocal = MetadataText();
                                        break;
                                    case "agencies/creator":
                                        if (Utils.IsEmpty(creatorTextBox.Text))
                                            creatorTextBox.Text = MetadataText();
                                        break;
                                    case "agencies/rightsHolder/name":
                                        if (Utils.IsEmpty(copyrightOwnerTextBox.Text))
                                            copyrightOwnerTextBox.Text = MetadataText();
                                        break;
                                    case "agencies/rightsHolder/url":
                                        if (Utils.IsEmpty(copyrightOwnerUrlTextBox.Text))
                                            copyrightOwnerUrlTextBox.Text = MetadataText();
                                        break;
                                    case "agencies/rightsHolder/abbr":
                                        if (Utils.IsEmpty(coprAbbrevTextBox.Text))
                                            coprAbbrevTextBox.Text = MetadataText();
                                        break;
                                    /*
                                    case "agencies/rightsHolder":
                                        coprAbbrevTextBox.Text = metadataXml.GetAttribute("abbr");
                                        localRightsHolderTextBox.Text = metadataXml.GetAttribute("local");
                                        copyrightOwnerUrlTextBox.Text = metadataXml.GetAttribute("url");
                                        if (Utils.IsEmpty(copyrightOwnerTextBox.Text))
                                            copyrightOwnerTextBox.Text = MetadataText();
                                        break;
                                    */
                                    case "agencies/publisher/name":
                                        if (Utils.IsEmpty(printPublisherTextBox.Text))
                                            printPublisherTextBox.Text = MetadataText();
                                        break;
                                    case "agencies/contributor/name":
                                        if (Utils.IsEmpty(contributorTextBox.Text))
                                            contributorTextBox.Text = MetadataText();
                                        break;
                                    case "language/iso":
                                        ethnologueCodeTextBox.Text = MetadataText();
                                        break;
                                    case "language/nameLocal":
                                        if (Utils.IsEmpty(languageNameTextBox.Text))
                                            languageNameTextBox.Text = engLangNameTextBox.Text = MetadataText();
                                        break;
                                    case "language/name":
                                        if (Utils.IsEmpty(engLangNameTextBox.Text))
                                            engLangNameTextBox.Text = MetadataText();
                                        break;
                                    case "language/ldml":
                                        ldmlTextBox.Text = MetadataText();
                                        break;
                                    case "language/rod":
                                        if (Utils.IsEmpty(rodCodeTextBox.Text))
                                            rodCodeTextBox.Text = MetadataText();
                                        break;
                                    case "language/script":
                                        if (Utils.IsEmpty(scriptTextBox.Text))
                                            scriptTextBox.Text = MetadataText();
                                        break;
                                    case "language/scriptDirection":
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
                                        if (Utils.IsEmpty(localRightsHolderTextBox.Text))
                                            localRightsHolderTextBox.Text = MetadataText();
                                        break;
                                    case "contact/rightsHolderAbbreviation":
                                        if (Utils.IsEmpty(coprAbbrevTextBox.Text))
                                            coprAbbrevTextBox.Text = MetadataText();
                                        break;
                                    case "contact/rightsHolderURL":
                                        if (Utils.IsEmpty(copyrightOwnerUrlTextBox.Text))
                                            copyrightOwnerUrlTextBox.Text = MetadataText();
                                        break;
                                    case "contact/rightsHolderFacebook":
                                        if (Utils.IsEmpty(facebookTextBox.Text))
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
                                        break;
                                    case "names/name":
                                        bookCode = metadataXml.GetAttribute("id").ToUpperInvariant();
                                        if (bookCode.Length==8)
                                            bookCode = bookCode.Substring(5, 3);
                                        bkRec = (BibleBookRecord)BookInfo.books[bookCode];
                                        if (bkRec != null)
                                            br = bkRec;
                                        /*
                                        else
                                            MessageBox.Show("Bad book code in metadata.xml: " + bookCode, "Error reading " + fileName);
                                        */
                                        break;
                                    case "names/name/abbr":
                                        br.vernacularAbbreviation = MetadataText();
                                        break;
                                    case "names/name/short":
                                        br.vernacularShortName = MetadataText();
                                        break;
                                    case "names/name/long":
                                        br.vernacularLongName = MetadataText();
                                        break;
                                    case "bookNames/book":
                                        bookCode = metadataXml.GetAttribute("code");
                                        bkRec = (BibleBookRecord)BookInfo.books[bookCode];
                                        if (bkRec != null)
                                            br = bkRec;
                                        /*
                                        else
                                            MessageBox.Show("Bad book code in metadata.xml: "+bookCode,"Error reading " + fileName);
                                        */
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
                        BookInfo.WriteDefaultBookNames(Path.Combine(globe.outputProjectDirectory, "BookNames.xml"));
                    }
                }
            }
	        catch (Exception ex)
	        {
                MessageBox.Show(ex.Message, "Error trying to read " + fileName);
                result = false;
        	}
            globe.projectOptions.Write();
            return result;
        }

        /// <summary>
        /// Reads the given DMLMetadata XML file and displays the found values.
        /// </summary>
        /// <param name="fileName">Full path and file name of the DBLMetadata file, usually ending in usx/license.xml</param>
        /// <returs>true iff some metdata was found and read</returs>
        public bool ReadLicense(string fileName)
        {
            bool result = false;
            string nodePath;
            try
            {
                if (File.Exists(fileName))
                {
                    metadataXml = new XmlFileReader(fileName);
                    metadataXml.MoveToContent();
                    if ((metadataXml.NodeType == XmlNodeType.Element) && (metadataXml.Name == "license"))
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
                                    case "dateLicense":
                                        break;
                                    case "dateLicenseExpiry":
                                        globe.projectOptions.licenseExpiration = MetadataText();
                                        licenseExpirationDateTextBox.Text = globe.projectOptions.licenseExpiration;
                                        break;
                                    case "publicationRights/allowIntroductions":
                                        break;
                                    case "publicationRights/allowFootnotes":
                                        break;
                                    case "publicationRights/allowCrossReferences":
                                        break;
                                    case "publicationRights/allowExtendedNotes":
                                        break;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error trying to read " + fileName);
                result = false;
            }
            globe.projectOptions.Write();
            return result;
        }

        /// <summary>
        /// Read options from the correct .xini file and display them.
        /// </summary>
        private void displayOptions(bool fingerprintIt = false)
        {
            if (globe.projectOptions == null)
            {
                globe.projectOptions = new Options(globe.projectXiniPath, globe);
            }
            else
            {
                globe.projectOptions.Reload(globe.projectXiniPath);
            }
            if (globe.projectOptions.languageId.Length == 3)
            {
                globe.er = eth.ReadEthnologue(globe.projectOptions.languageId);
                if (globe.projectOptions.country.Length == 0)
                    globe.projectOptions.country = globe.er.countryName;
                if (globe.projectOptions.countryCode.Length == 0)
                    globe.projectOptions.countryCode = globe.er.countryId;
                if (globe.projectOptions.languageNameInEnglish.Length == 0)
                    globe.projectOptions.languageNameInEnglish = globe.er.langName;
            }
            SFConverter.jobIni = globe.projectOptions.ini;
            ethnologueCodeTextBox.Text = globe.projectOptions.languageId;
            translationIDLabel.Text = globe.projectOptions.translationId = globe.currentProject; // This was globe.globe.projectOptions.translationId, but now we force short translation ID and input directory name to match.
            traditionalAbbreviationTextBox.Text = globe.projectOptions.translationTraditionalAbbreviation;
            languageNameTextBox.Text = globe.projectOptions.languageName;
            engLangNameTextBox.Text = globe.projectOptions.languageNameInEnglish;
            dialectTextBox.Text = globe.projectOptions.dialect;
            creatorTextBox.Text = globe.projectOptions.contentCreator;
            contributorTextBox.Text = globe.projectOptions.contributor;
            titleTextBox.Text = globe.projectOptions.vernacularTitle;
            descriptionTextBox.Text = globe.projectOptions.EnglishDescription;
            lwcDescriptionTextBox.Text = globe.projectOptions.lwcDescription;
            updateDateTimePicker.MaxDate = DateTime.Now.AddDays(2);
            updateDateTimePicker.Value = globe.projectOptions.contentUpdateDate;
            privateCheckBox.Checked = globe.projectOptions.privateProject;
            pdRadioButton.Checked = globe.projectOptions.publicDomain;
            ccbyndncRadioButton.Checked = globe.projectOptions.ccbyndnc;
            wbtVerbatimRadioButton.Checked = globe.projectOptions.wbtverbatim;
            ccbyncRadioButton.Checked = globe.projectOptions.ccbync;
            CCBySaRadioButton.Checked = globe.projectOptions.ccbysa;
            ccbyRadioButton.Checked = globe.projectOptions.ccby;
            licenseExpirationDateTextBox.Text = globe.projectOptions.licenseExpiration;
            CCByNdRadioButton.Checked = globe.projectOptions.ccbynd;
            otherRadioButton.Checked = globe.projectOptions.otherLicense;
            allRightsRadioButton.Checked = globe.projectOptions.allRightsReserved;
            anonymousCheckBox.Checked = globe.projectOptions.anonymous;
            silentRadioButton.Checked = globe.projectOptions.silentCopyright;
            copyrightOwnerTextBox.Text = globe.projectOptions.copyrightOwner;
            copyrightOwnerUrlTextBox.Text = globe.projectOptions.copyrightOwnerUrl;
            copyrightYearTextBox.Text = globe.projectOptions.copyrightYears;
            coprAbbrevTextBox.Text = globe.projectOptions.copyrightOwnerAbbrev;
            rightsStatementTextBox.Text = globe.projectOptions.rightsStatement;
            printPublisherTextBox.Text = globe.projectOptions.printPublisher;
            electronicPublisherTextBox.Text = globe.projectOptions.electronicPublisher;
            stripExtrasCheckBox.Checked = globe.projectOptions.ignoreExtras;
            xoTextBox.Text = globe.projectOptions.xoFormat;
            customCssTextBox.Text = globe.projectOptions.customCssFileName;
            stripOriginCheckBox.Checked = globe.projectOptions.stripNoteOrigin;
            prepublicationChecksCheckBox.Checked = globe.projectOptions.PrepublicationChecks;
            webSiteReadyCheckBox.Checked = globe.projectOptions.WebSiteReady;
            e10dblCheckBox.Checked = globe.projectOptions.ETENDBL;
            archivedCheckBox.Checked = globe.projectOptions.Archived;
            subsetCheckBox.Checked = globe.projectOptions.subsetProject;
            audioRecordingCopyrightTextBox.Text = globe.projectOptions.AudioCopyrightNotice;
            rodCodeTextBox.Text = globe.projectOptions.rodCode;
            ldmlTextBox.Text = globe.projectOptions.ldml;
            scriptTextBox.Text = globe.projectOptions.script;
            localRightsHolderTextBox.Text = globe.projectOptions.localRightsHolder;
            facebookTextBox.Text = globe.projectOptions.facebook;
            countryTextBox.Text = globe.projectOptions.country;
            countryCodeTextBox.Text = globe.projectOptions.countryCode;
            extendUsfmCheckBox.Checked = globe.projectOptions.extendUsfm;
            cropCheckBox.Checked = globe.projectOptions.includeCropMarks;
            chapter1CheckBox.Checked = globe.projectOptions.chapter1;
            verse1CheckBox.Checked = globe.projectOptions.verse1;
            pageWidthTextBox.Text = globe.projectOptions.pageWidth;
            pageLengthTextBox.Text = globe.projectOptions.pageLength;
            regenerateNoteOriginsCheckBox.Checked = globe.projectOptions.RegenerateNoteOrigins;
            cvSeparatorTextBox.Text = globe.projectOptions.CVSeparator;
            downloadsAllowedCheckBox.Checked = globe.projectOptions.downloadsAllowed;
            customSourceFolderTextBox.Text = globe.projectOptions.customSourcePath;
            if ((globe.projectOptions.SwordName.Length < 1) && (globe.projectOptions.translationId.Length > 1))
            {
                globe.projectOptions.SwordName = globe.projectOptions.translationId.Replace("-", "").Replace("_", "");
                if ((globe.projectOptions.copyrightYears.Length >= 4) && !Char.IsDigit(globe.projectOptions.SwordName[globe.projectOptions.SwordName.Length - 1]))
                    globe.projectOptions.SwordName += globe.projectOptions.copyrightYears.Substring(globe.projectOptions.copyrightYears.Length - 4);
            }
            if ((globe.projectOptions.SwordName.Length > 0) && !globe.projectOptions.SwordName.EndsWith(globe.m_swordSuffix))
            {
                globe.projectOptions.SwordName += globe.m_swordSuffix;
            }
            swordNameTextBox.Text = globe.projectOptions.SwordName;
            oldSwordIdTextBox.Text = globe.projectOptions.ObsoleteSwordName;
            globe.rebuild = globe.xini.ReadBool("rebuild", false);
            // globe.runXetex = globe.xini.ReadBool("runXetex", false);
            makeBrowserBibleCheckBox.Checked = globe.projectOptions.makeBrowserBible;
            makeEPubCheckBox.Checked = globe.projectOptions.makeEub;
            makeHtmlCheckBox.Checked = globe.projectOptions.makeHtml;
            makeSileCheckBox.Checked = globe.projectOptions.makeSile;
            makePDFCheckBox.Checked = globe.projectOptions.makePDF;
            makeSwordCheckBox.Checked = globe.projectOptions.makeSword;
            makeWordMLCheckBox.Checked = globe.projectOptions.makeWordML;
            disablePrintingFigoriginsCheckBox.Checked = globe.projectOptions.disablePrintingFigOrigins;
            apocryphaCheckBox.Checked = globe.projectOptions.includeApocrypha;
            if (globe.projectOptions.eBibledotorgunique)
                recheckedCheckBox.Visible = true;
            recheckedCheckBox.Checked = globe.projectOptions.rechecked;

            fcbhIdTextBox.Text = globe.projectOptions.fcbhId;
            shortTitleTextBox.Text = globe.projectOptions.shortTitle;
            if (shortTitleTextBox.Text.Length < 1)
                shortTitleTextBox.Text = globe.projectOptions.EnglishDescription;
            if (shortTitleTextBox.Text.Length > 254)
            {
                MessageBox.Show("Short title must be < 255 characters long.");
                shortTitleTextBox.Text = shortTitleTextBox.Text.Substring(0, 254);
            }
                        
            templateLabel.Text = "Current template: " + globe.currentTemplate;
            copyFromTemplateButton.Enabled = (globe.currentTemplate.Length > 0) && (globe.currentTemplate != globe.currentProject);
            makeTemplateButton.Enabled = globe.currentTemplate != globe.currentProject;
            if (!fileHelper.fAllRunning)
            {
                if (globe.projectOptions.lastRunResult)
                    BackColor = Color.LightGreen;
                else
                    BackColor = Color.LightPink;
            }

            listInputProcesses.SuspendLayout();
            listInputProcesses.Items.Clear();
            foreach (string filename in globe.projectOptions.preprocessingTables)
                listInputProcesses.Items.Add(filename);
            listInputProcesses.ResumeLayout();

            postprocessListBox.SuspendLayout();
            postprocessListBox.Items.Clear();
            foreach (string filename in globe.projectOptions.postprocesses)
                postprocessListBox.Items.Add(filename);
            postprocessListBox.ResumeLayout();

            // Insert more checkbox settings here.
            homeLinkTextBox.Text = globe.projectOptions.homeLink;
            goTextTextBox.Text = globe.projectOptions.goText;
            footerHtmlTextBox.Text = globe.projectOptions.footerHtml;
            indexPageTextBox.Text = globe.projectOptions.indexHtml;
            promoTextBox.Text = globe.projectOptions.promoHtml;
            licenseTextBox.Text = globe.projectOptions.licenseHtml;
            versificationComboBox.Text = globe.projectOptions.versificationScheme;
            numberSystemComboBox.Text = fileHelper.SetDigitLocale(globe.projectOptions.numberSystem);
            numberSystemLabel.Text = fileHelper.NumberSample();
            textDirectionComboBox.Text = globe.projectOptions.textDir;
            homeDomainTextBox.Text = globe.projectOptions.homeDomain;
            relaxNestingSyntaxCheckBox.Checked = globe.projectOptions.relaxUsfmNesting;
            fontComboBox.Text = globe.projectOptions.fontFamily;
            headerFontComboBox.Text = globe.projectOptions.headerFooterFont;
            JesusFilmLinkTextTextBox.Text = globe.projectOptions.JesusFilmLinkText;
            JesusFilmLinkTargetTextBox.Text = globe.projectOptions.JesusFilmLinkTarget;
            dependsComboBox.Text = globe.projectOptions.dependsOn;
            footNoteCallersTextBox.Text = globe.projectOptions.footNoteCallers;
            crossreferenceCallersTextBox.Text = globe.projectOptions.xrefCallers;
            commentTextBox.Text = globe.projectOptions.commentText;
            redistributableCheckBox.Checked = globe.projectOptions.redistributable;
            licenseTextBox.Enabled = customPermissionsCheckBox.Checked = globe.projectOptions.customPermissions;
            //ISBNLabel.Text = "ISBN " + globe.projectOptions.isbn13;

            if (globe.projectOptions.eBibleCertified)
                certLabel.Text = "certified";
            else
                certLabel.Text = "";

            if (globe.projectOptions.commonChars)
                commonCharactersLabel.Text = "Common fonts OK.";
            else
                commonCharactersLabel.Text = "Extended Unicode font required.";

			LoadStatisticsTab();
            globe.SetCopyrightStrings();

            if (ReadMetadata(Path.Combine(Path.Combine(globe.inputProjectDirectory, "usx"), "metadata.xml")))
                SaveOptions();
            if (ReadLicense(Path.Combine(Path.Combine(globe.inputProjectDirectory, "usx"), "license.xml")))
                SaveOptions();
            string src = FindSource(globe.currentProject, fingerprintIt);
            if (src == string.Empty)
            {
                sourceLabel.BackColor = Color.Yellow;
                sourceLabel.ForeColor = Color.Red;
                sourceLabel.Text = "NO SOURCE DIRECTORY! Please read Help.";
                tabControl1.SelectedIndex = 1;
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
Peripherals: {12} books",
                                globe.projectOptions.otBookCount, globe.projectOptions.otChapCount, globe.projectOptions.otVerseCount, globe.projectOptions.otVerseMax,
                                globe.projectOptions.ntBookCount, globe.projectOptions.ntChapCount, globe.projectOptions.ntVerseCount, globe.projectOptions.ntVerseMax,
                                globe.projectOptions.adBookCount, globe.projectOptions.adChapCount, globe.projectOptions.adVerseCount, globe.projectOptions.adVerseMax,
                                globe.projectOptions.pBookCount);
        }



        private void m_projectsList_SelectedIndexChanged(object sender, EventArgs e)
        {
            globe.currentProject = m_projectsList.SelectedItem.ToString();
            globe.inputProjectDirectory = Path.Combine(globe.inputDirectory, globe.currentProject);
            globe.outputProjectDirectory = Path.Combine(globe.outputDirectory, globe.currentProject);
            globe.projectXiniPath = Path.Combine(globe.inputProjectDirectory, "options.xini");
            displayOptions(true);
        }

        public void SaveOptions()
        {
            if (globe.projectOptions == null)
                return;
            globe.projectOptions.languageId = ethnologueCodeTextBox.Text;
            globe.projectOptions.translationTraditionalAbbreviation = traditionalAbbreviationTextBox.Text;
            globe.projectOptions.languageName = languageNameTextBox.Text;
            globe.projectOptions.languageNameInEnglish = engLangNameTextBox.Text;
            globe.projectOptions.dialect = dialectTextBox.Text;
            globe.projectOptions.contentCreator = creatorTextBox.Text;
            globe.projectOptions.contributor = contributorTextBox.Text;
            globe.projectOptions.vernacularTitle = titleTextBox.Text;
            globe.projectOptions.EnglishDescription = descriptionTextBox.Text;
            globe.projectOptions.lwcDescription = lwcDescriptionTextBox.Text;
            globe.projectOptions.contentUpdateDate = updateDateTimePicker.Value;
            globe.projectOptions.publicDomain = pdRadioButton.Checked;
            globe.projectOptions.ccbyndnc = ccbyndncRadioButton.Checked;
            globe.projectOptions.licenseExpiration = licenseExpirationDateTextBox.Text;
            globe.projectOptions.ccbync = ccbyncRadioButton.Checked;
            globe.projectOptions.wbtverbatim = wbtVerbatimRadioButton.Checked;
            globe.projectOptions.ccby = ccbyRadioButton.Checked;
            globe.projectOptions.ccbysa = CCBySaRadioButton.Checked;
            globe.projectOptions.ccbynd = CCByNdRadioButton.Checked;
            globe.projectOptions.otherLicense = otherRadioButton.Checked;
            globe.projectOptions.anonymous = anonymousCheckBox.Checked;
            globe.projectOptions.allRightsReserved = allRightsRadioButton.Checked;
            globe.projectOptions.silentCopyright = silentRadioButton.Checked;
            globe.projectOptions.copyrightOwner = copyrightOwnerTextBox.Text.Trim();
            copyrightOwnerUrlTextBox.Text = copyrightOwnerUrlTextBox.Text.Trim();
            if ((copyrightOwnerUrlTextBox.Text.Length > 1) && !copyrightOwnerUrlTextBox.Text.ToLowerInvariant().StartsWith("http"))
                copyrightOwnerUrlTextBox.Text = "https://" + copyrightOwnerUrlTextBox.Text;
            globe.projectOptions.copyrightOwnerUrl = copyrightOwnerUrlTextBox.Text;
            globe.projectOptions.copyrightYears = copyrightYearTextBox.Text.Trim();
            globe.projectOptions.copyrightOwnerAbbrev = coprAbbrevTextBox.Text.Trim();
            globe.projectOptions.rightsStatement = rightsStatementTextBox.Text;
            globe.projectOptions.printPublisher = printPublisherTextBox.Text.Trim();
            globe.projectOptions.electronicPublisher = electronicPublisherTextBox.Text.Trim();
            globe.projectOptions.ignoreExtras = stripExtrasCheckBox.Checked;
            globe.projectOptions.textDir = textDirectionComboBox.Text;
            globe.projectOptions.xoFormat = xoTextBox.Text;
            globe.projectOptions.customCssFileName = customCssTextBox.Text;
            globe.projectOptions.stripNoteOrigin = stripOriginCheckBox.Checked;
            globe.projectOptions.PrepublicationChecks = prepublicationChecksCheckBox.Checked;
            globe.projectOptions.WebSiteReady = webSiteReadyCheckBox.Checked;
            globe.projectOptions.ETENDBL = e10dblCheckBox.Checked;
            globe.projectOptions.Archived = archivedCheckBox.Checked;
            globe.projectOptions.subsetProject = subsetCheckBox.Checked;
            globe.projectOptions.JesusFilmLinkText = JesusFilmLinkTextTextBox.Text;
            globe.projectOptions.JesusFilmLinkTarget = JesusFilmLinkTargetTextBox.Text;
            globe.projectOptions.AudioCopyrightNotice = audioRecordingCopyrightTextBox.Text;
            globe.projectOptions.rodCode = rodCodeTextBox.Text;
            globe.projectOptions.ldml = ldmlTextBox.Text;
            globe.projectOptions.script = scriptTextBox.Text;
            globe.projectOptions.localRightsHolder = localRightsHolderTextBox.Text;
            globe.projectOptions.facebook = facebookTextBox.Text;
            globe.projectOptions.country = countryTextBox.Text;
            globe.projectOptions.countryCode = countryCodeTextBox.Text;
            globe.projectOptions.extendUsfm = extendUsfmCheckBox.Checked;
            globe.projectOptions.fcbhId = fcbhIdTextBox.Text.Replace(".","");
            if (string.IsNullOrEmpty(globe.projectOptions.fcbhId))
                globe.projectOptions.fcbhId = globe.projectOptions.translationId.ToUpperInvariant();
            globe.projectOptions.shortTitle = shortTitleTextBox.Text;
            globe.projectOptions.footNoteCallers = footNoteCallersTextBox.Text;
            globe.projectOptions.xrefCallers = crossreferenceCallersTextBox.Text;
            globe.projectOptions.commentText = commentTextBox.Text;
            globe.projectOptions.redistributable = redistributableCheckBox.Checked & !globe.projectOptions.privateProject;
            globe.projectOptions.downloadsAllowed = downloadsAllowedCheckBox.Checked & !globe.projectOptions.privateProject;
            globe.projectOptions.customPermissions = customPermissionsCheckBox.Checked;
            globe.projectOptions.includeCropMarks = cropCheckBox.Checked;
            globe.projectOptions.chapter1 = chapter1CheckBox.Checked;
            globe.projectOptions.verse1 = verse1CheckBox.Checked;
            globe.projectOptions.pageWidth = pageWidthTextBox.Text;
            globe.projectOptions.pageLength = pageLengthTextBox.Text;
            globe.projectOptions.RegenerateNoteOrigins = regenerateNoteOriginsCheckBox.Checked;
            globe.projectOptions.CVSeparator = cvSeparatorTextBox.Text;
            globe.projectOptions.customSourcePath = customSourceFolderTextBox.Text;
            globe.projectOptions.SwordName = swordNameTextBox.Text;
            globe.projectOptions.ObsoleteSwordName = oldSwordIdTextBox.Text;
            globe.xini.WriteBool("rebuild", globe.rebuild);
            globe.projectOptions.makeBrowserBible = makeBrowserBibleCheckBox.Checked;
            globe.projectOptions.makeEub = makeEPubCheckBox.Checked;
            globe.projectOptions.makeHtml = makeHtmlCheckBox.Checked;
            globe.projectOptions.makeSile = makeSileCheckBox.Checked;
            globe.projectOptions.makePDF = makePDFCheckBox.Checked;
            globe.projectOptions.makeSword = makeSwordCheckBox.Checked;
            globe.projectOptions.makeWordML = makeWordMLCheckBox.Checked;
            globe.projectOptions.disablePrintingFigOrigins = disablePrintingFigoriginsCheckBox.Checked;
            globe.projectOptions.includeApocrypha = apocryphaCheckBox.Checked;

            List<string> tableNames = new List<string>();
            foreach (string filename in listInputProcesses.Items)
                tableNames.Add(filename);
            globe.projectOptions.preprocessingTables = tableNames;
            
            List<string> postprocessNames = new List<string>();
            foreach (string filename in postprocessListBox.Items)
                postprocessNames.Add(filename);
            globe.projectOptions.postprocesses = postprocessNames;
            
            /*
            List<string> alternateLinks = new List<string>();
            foreach (string alternateLink in altLinkListBox.Items)
                alternateLinks.Add(alternateLink);
            globe.globe.projectOptions.altLinks = alternateLinks;
             */
            
            // Insert more checkbox settings here.
            globe.projectOptions.homeLink = homeLinkTextBox.Text;
            globe.projectOptions.goText = goTextTextBox.Text;
            globe.projectOptions.footerHtml = footerHtmlTextBox.Text;
            globe.projectOptions.indexHtml = indexPageTextBox.Text;
            globe.projectOptions.promoHtml = promoTextBox.Text;
            globe.projectOptions.licenseHtml = licenseTextBox.Text;
            globe.projectOptions.versificationScheme = versificationComboBox.Text;
            globe.projectOptions.numberSystem = fileHelper.SetDigitLocale(numberSystemComboBox.Text.Trim());
            globe.projectOptions.privateProject = privateCheckBox.Checked;
            globe.projectOptions.relaxUsfmNesting = relaxNestingSyntaxCheckBox.Checked;
            globe.projectOptions.rechecked = recheckedCheckBox.Checked;

            globe.projectOptions.homeDomain = homeDomainTextBox.Text.Trim();
            globe.projectOptions.fontFamily = fontComboBox.Text.Trim();
            globe.projectOptions.headerFooterFont = headerFontComboBox.Text.Trim();
            if (dependsComboBox.SelectedItem != null)
                globe.projectOptions.dependsOn = dependsComboBox.Text;

            globe.projectOptions.Write();
        }

        private void btnAddInputProcess_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.CheckFileExists = true;
            dlg.InitialDirectory = globe.inputProjectDirectory;
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
                if ((newFileDir.ToLowerInvariant() != globe.inputProjectDirectory.ToLowerInvariant()) &&
                    (newFileDir.ToLowerInvariant() != globe.inputDirectory.ToLowerInvariant()) &&
                    (newFileDir.ToLowerInvariant() != globe.dataRootDir.ToLowerInvariant()))
                {
                    if (MessageBox.Show(this, "Preprocessing files must be in the work directory. Copy there?", "Note", MessageBoxButtons.YesNo) ==
                        DialogResult.Yes)
                    {
                        File.Copy(newFilePath, Path.Combine(globe.inputProjectDirectory, newFileName));
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
            if (globe.currentConversion != progressMessage)
            {
                globe.currentConversion = progressMessage;
            }
            Application.DoEvents();
            return fileHelper.fAllRunning;
        }

        private DateTime startTime = new DateTime(1, 1, 1);
        private bool triggerautorun;

        private void timer1_Tick(object sender, EventArgs e)
        {
            string progress;
            string runtime = String.Empty;
            if (globe.currentConversion == "Concordance")
                progress = ConcGenerator.Stage + " " + ConcGenerator.Progress;
            else
                progress = globe.currentConversion + " " + WordSend.usfxToHtmlConverter.conversionProgress;
            if (fileHelper.fAllRunning)
                runtime = (DateTime.UtcNow - startTime).ToString("g") + " " + globe.currentProject + " ";
            batchLabel.Text = runtime + progress;
            //extensionLabel.Text = plugin.PluginMessage();
            if (triggerautorun)
            {
                triggerautorun = false;
                XMLini.readOnly = false;
                reloadButton_Click(sender, e);
                Application.DoEvents();
                WorkOnAllButton_Click(sender, e);
            }
        }

        /*
    	private string ConversionProgress
    	{
    		get
    		{
				if (globe.currentConversion == "Concordance")
					return ConcGenerator.Stage + " " + ConcGenerator.Progress;
    			return globe.currentConversion + " " + WordSend.usfxToHtmlConverter.conversionProgress;
    		}
    	}
        */

        private void statusNow(string s)
        {
            batchLabel.Text = (DateTime.UtcNow - startTime).ToString().Substring(0, 8) + " " + globe.currentProject + " " + s;
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
            string command = Path.Combine(globe.inputDirectory, "postprocess.bat");
            Utils.DeleteFile(Path.Combine(globe.inputProjectDirectory, "lock"));

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
                tabControl1.SelectedTab = RunTabPage;
                startTime = DateTime.UtcNow;
                BackColor = Color.LightGreen;
                Application.DoEvents();
                timer1.Enabled = true;
                WorkOnAllButton.Text = "Stop";
                globe.projectOptions.done = false;
                fileHelper.unlockProject(globe.inputProjectDirectory);
                SaveOptions();
                ProcessOneProject(SelectedProject);
                Application.DoEvents();
                if (fileHelper.fAllRunning && File.Exists(command))
                {
                    globe.currentConversion = "Running " + command;
                    batchLabel.Text = globe.currentConversion;
                    Application.DoEvents();
                    if (!fileHelper.RunCommand(command, "", globe.outputDirectory))
                        MessageBox.Show(fileHelper.runCommandError, "Error " + globe.currentConversion);
                }
                globe.currentConversion = String.Empty;
                batchLabel.Text = (DateTime.UtcNow - startTime).ToString(@"g") + " " + "Done.";
                messagesListBox.Items.Add(batchLabel.Text);
                int j = m_projectsList.Items.IndexOf(SelectedProject);
                m_projectsList.SetItemChecked(j, false);
                m_projectsList_SelectedIndexChanged(null, null);
                /*
                string indexhtm = Path.Combine(Path.Combine(globe.outputProjectDirectory, "html"), "index.htm");
                if (File.Exists(indexhtm))
                    System.Diagnostics.Process.Start(indexhtm);
                */
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

 
        private void helpButton_Click(object sender, EventArgs e)
        {
            showHelp("haiola.htm");
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
            globe.currentTemplate = globe.currentProject;
            templateLabel.Text = "Current template: " + globe.currentTemplate;
            copyFromTemplateButton.Enabled = false;
            globe.xini.Write();
        }

        private void copyFromTemplateButton_Click(object sender, EventArgs e)
        {
            Options templateOptions = new Options(Path.Combine(Path.Combine(globe.inputDirectory, globe.currentTemplate), "options.xini"), globe);
            homeLinkTextBox.Text = globe.projectOptions.homeLink = templateOptions.homeLink;
            goTextTextBox.Text = globe.projectOptions.goText = templateOptions.goText;
            footerHtmlTextBox.Text = globe.projectOptions.footerHtml = templateOptions.footerHtml;
            indexPageTextBox.Text = globe.projectOptions.indexHtml = templateOptions.indexHtml;
            licenseTextBox.Text = globe.projectOptions.licenseHtml = templateOptions.licenseHtml;
            customCssTextBox.Text = globe.projectOptions.customCssFileName = templateOptions.customCssFileName;
            fontComboBox.Text = globe.projectOptions.fontFamily = templateOptions.fontFamily;
            headerFontComboBox.Text = globe.projectOptions.headerFooterFont = templateOptions.headerFooterFont;
            globe.projectOptions.postprocesses = templateOptions.postprocesses;
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



        private void m_projectsList_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (!loadingDirectory)
            {
                SaveOptions();
                displayOptions(false);
                globe.projectOptions.selected = e.NewValue == CheckState.Checked;
                if (globe.projectOptions.selected)
                {
                    if (!fileHelper.fAllRunning)
                        globe.projectOptions.done = false;
                    projSelected++;
                }
                else
                    projSelected--;
                statsLabel.Text = projReady.ToString() + " of " + projCount.ToString() + " project directories are ready to run; " + projSelected.ToString() + " selected."; ;
                globe.projectOptions.Write();
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
                numberSystemLabel.Font = newFont;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "ERROR SETTING FONT");
            }
        }

        private void allRightsRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            if (allRightsRadioButton.Checked)
            {
                redistributableCheckBox.Checked = false;
                groupBox1.BackColor = Color.LightPink;
                globe.projectOptions.allRightsReserved = true;
            }
        }

        private void numberSystemComboBox_SelectedValueChanged(object sender, EventArgs e)
        {
            globe.projectOptions.numberSystem = numberSystemComboBox.Text;
            fileHelper.SetDigitLocale(globe.projectOptions.numberSystem);
            numberSystemLabel.Text = fileHelper.NumberSample();
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
            {
                redistributableCheckBox.Checked = downloadsAllowedCheckBox.Checked = true;
                groupBox1.BackColor = Color.LightGreen;
            }
        }

        private void ccRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            if (ccbyndncRadioButton.Checked)
            {
                redistributableCheckBox.Checked = downloadsAllowedCheckBox.Checked = true;
                groupBox1.BackColor = Color.LightYellow;
            }
        }

        private void customPermissionsCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            licenseTextBox.Enabled = customPermissionsCheckBox.Checked;
        }

        private void CCBySaRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            if (CCBySaRadioButton.Checked)
            {
                redistributableCheckBox.Checked = downloadsAllowedCheckBox.Checked = true;
                groupBox1.BackColor = Color.LightGreen;
            }
        }

        private void CCByNdRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            if (CCByNdRadioButton.Checked)
            {
                redistributableCheckBox.Checked = downloadsAllowedCheckBox.Checked = true;
                groupBox1.BackColor = Color.LightGreen;
            }
        }

        private void otherRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            if (otherRadioButton.Checked)
                groupBox1.BackColor = Color.LightPink;

        }

        private void relaxNestingSyntaxCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            globe.projectOptions.relaxUsfmNesting = relaxNestingSyntaxCheckBox.Checked;
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
                globe.projectOptions.privateProject = true;
            }
            else
            {
                redistributableCheckBox.Enabled = true;
                downloadsAllowedCheckBox.Enabled = true;
                globe.projectOptions.privateProject = false;
            }
        }

        private void redistributableCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            downloadsAllowedCheckBox.Checked |= redistributableCheckBox.Checked;
        }

        private void downloadsAllowedCheckBox_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void ccbyRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            if (ccbyRadioButton.Checked)
            {
                redistributableCheckBox.Checked = downloadsAllowedCheckBox.Checked = true;
                groupBox1.BackColor = Color.LightGreen;
            }
        }

        private void silentRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            if (silentRadioButton.Checked)
            {
                groupBox1.BackColor = Color.LightPink;
            }
        }

        private void createNewProjectButton_Click(object sender, EventArgs e)
        {
            NewProjectForm np = new NewProjectForm();
            np.ShowDialog();
            if (!String.IsNullOrEmpty(np.newProjectName))
            {
                LoadWorkingDirectory(false, false, true);
                m_projectsList.Text = np.newProjectName;
            }
        }

        private void wbtVerbatimRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            if (wbtVerbatimRadioButton.Checked)
            {
                redistributableCheckBox.Checked = false;
                downloadsAllowedCheckBox.Checked = true;
                groupBox1.BackColor = Color.LightPink;
                globe.projectOptions.wbtverbatim = true;
            }
        }

        private void WBTUSXimportButton_Click(object sender, EventArgs e)
        {
            fileHelper.fAllRunning = true;
            fileHelper.RunCommand("unzipusx", "", globe.inputDirectory);
            fileHelper.fAllRunning = false;
            displayOptions(true);
            copyFromTemplateButton_Click(sender, e);
            homeDomainTextBox.Text = "ebible.org";
            electronicPublisherTextBox.Text = "eBible.org";
            wbtVerbatimRadioButton.Checked = true;
            if (fcbhIdTextBox.Text.Length < 6)
                fcbhIdTextBox.Text += "WBT";
            if (fcbhIdTextBox.Text.Length > 6)
                fcbhIdTextBox.Text = fcbhIdTextBox.Text.Substring(0, 6);
            stripOriginCheckBox.Checked = false;
            regenerateNoteOriginsCheckBox.Checked = false;
            string yr = copyrightYearTextBox.Text;
            if (yr.Length > 7)
            {
                if ((yr[4] == '-') && (yr[7] == '-'))
                    yr = yr.Substring(0, 4);
            }
            copyrightYearTextBox.Text = yr;
            swordNameTextBox.Text = translationIDLabel.Text + yr + "eb";
            updateDateTimePicker.Value = DateTime.Now;
            tabControl1.SelectedIndex++;

        }

        private void haiolaForm_Activated(object sender, EventArgs e)
        {
            bool developmentCopy = File.Exists("/home/kahunapule/sync/source/haiola/haiola/order.txt");
            XMLini.readOnly = false;
            WBTUSXimportButton.Visible = developmentCopy;
            WBTUSXimportButton.Enabled = developmentCopy;
        }

        private void findSourceFolderButton_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dlg = new FolderBrowserDialog();
            dlg.SelectedPath = customSourceFolderTextBox.Text;
            dlg.Description =
                @"Please select the folder where your USFM, USFX, or USX source files are for this project.";
            
            dlg.ShowNewFolderButton = true;
            if ((dlg.ShowDialog() != System.Windows.Forms.DialogResult.OK) || (dlg.SelectedPath == null))
                return;
            if (Directory.Exists(dlg.SelectedPath))
            {
                customSourceFolderTextBox.Text = dlg.SelectedPath;
            }

        }

        private void customSourceFolderTextBox_TextChanged(object sender, EventArgs e)
        {
            if (!String.IsNullOrEmpty(globe.GetSourceKind(customSourceFolderTextBox.Text, !XMLini.readOnly)))
            {
                globe.projectOptions.paratextProject = String.Empty;
                globe.projectOptions.paratext8Project = String.Empty;
                globe.projectOptions.customSourcePath = customSourceFolderTextBox.Text;
                globe.projectOptions.Write();
            }
        }

    }
}
