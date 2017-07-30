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
        public PluginManager plugin;
        public string currentConversion;   // e.g., "Preprocessing" or "Portable HTML"

        public haiolaForm()
        {
            InitializeComponent();
            globe = new global();
            globe.UpdateStatus = updateConversionProgress;
            globe.GUIWriteString = showMessageString;

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

        /* Function deprecated due to input file not being maintained.

        private Hashtable fcbhDbsIds;

        private void readFcbhIds()
        {
            string fcbhIdFileName = Path.Combine(globe.inputDirectory, "fcbhids.csv");
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
        */

        public LanguageCodeInfo languageCodes;

        private string dbsLogo, pngbtaLogo;

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
            dbsLogo = Path.Combine(globe.inputDirectory, "dbs.jpg");
            pngbtaLogo = Path.Combine(globe.inputDirectory, "pngbta.jpg");
            eth = new Ethnologue();
            languageCodes = new LanguageCodeInfo();
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
            }
        }

        /// <summary>
        /// Ensure that a named template file exists in the input directory.
        /// </summary>
        /// <param name="fileName">Name of file to find.</param>
        public void EnsureTemplateFile(string fileName)
        {
        	EnsureTemplateFile(fileName, globe.inputDirectory);
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
                MessageBox.Show(ex.Message, "Error ensuring " + fileName + " is in " + globe.inputDirectory);
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
            LoadParatext8ProjectList();
            m_projectsList.BeginUpdate();
            m_projectsList.Items.Clear();
            m_projectsList.Sorted = false;
            globe.inputDirectory = Path.Combine(globe.dataRootDir, "input");
            globe.outputDirectory = Path.Combine(globe.dataRootDir, "output");
            fileHelper.EnsureDirectory(globe.dataRootDir);
            fileHelper.EnsureDirectory(globe.inputDirectory);
            fileHelper.EnsureDirectory(globe.outputDirectory);

            EnsureTemplateFile("haiola.css");
            EnsureTemplateFile("prophero.css");
            EnsureTemplateFile("fixquotes.re");

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
                    globe.projectOptions = new Options(globe.projectXiniPath);
                }
                else
                {
                    globe.projectOptions.Reload(globe.projectXiniPath);
                }

                bool gotParatextProject = false;
                if (!String.IsNullOrEmpty(globe.paratextProjectsDir))
                {
                    if (!String.IsNullOrEmpty(globe.projectOptions.paratextProject))
                    {
                        string ParatextProjectDir = Path.Combine(globe.paratextProjectsDir, globe.projectOptions.paratextProject);
                        if (Directory.Exists(ParatextProjectDir))
                        {
                            gotParatextProject = true;
                            Ssf.ReadParatextSsf(globe.projectOptions, ParatextProjectDir + ".ssf");
                        }
                    }
                }
                if ((!String.IsNullOrEmpty(globe.projectOptions.languageId)) && 
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
                    globe.projectOptions.selected = isReady && !globe.projectOptions.lastRunResult;
                }
                else
                {
                    globe.projectOptions.selected = isReady && globe.projectOptions.selected;
                }
                m_projectsList.SetItemChecked(m_projectsList.Items.Count - 1, globe.projectOptions.selected);
                globe.projectOptions.Write();
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
            globe.projectOptions.lastRunResult = true;
            globe.projectOptions.warningsFound = false;
        }

        private void ConvertUsfmToUsfx()
        {
            string UsfmDir = Path.Combine(globe.outputProjectDirectory, "extendedusfm");
            string UsfxPath = GetUsfxDirectoryPath();
            string usfxName = GetUsfxFilePath();
            if (!Directory.Exists(UsfmDir))
            {
                UsfmDir = Path.Combine(globe.outputProjectDirectory, "usfm");
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
            if ((globe.projectOptions.languageId.Length < 3) || (globe.projectOptions.translationId.Length < 3))
            {
                MessageBox.Show(this,
                                string.Format(
                                    "language and translation ids (%0 and %1) must be at least three characters each",
                                    globe.projectOptions.languageId, globe.projectOptions.translationId),
                                "ERROR");
                return;
            }
            Utils.EnsureDirectory(UsfxPath);
            string logFile = Path.Combine(globe.outputProjectDirectory, "ConversionReports.txt");
            Logit.OpenFile(logFile);
            Logit.UpdateStatus = updateConversionProgress;
            Logit.GUIWriteString = showMessageString;
            SFConverter.scripture = new Scriptures(globe.projectOptions);
            Logit.loggedError = false;
            Logit.loggedWarning = false;
            // Read a copy of BookNames.xml copied from the source USFM directory, if any.
            SFConverter.scripture.bkInfo.ReadDefaultBookNames(Path.Combine(globe.outputProjectDirectory, "BookNames.xml"));
            SFConverter.scripture.assumeAllNested = globe.projectOptions.relaxUsfmNesting;
            // Read the input USFM files into internal data structures.
            SFConverter.ProcessFilespec(Path.Combine(UsfmDir, "*.usfm"), Encoding.UTF8);
            currentConversion = "converting from USFM to USFX; writing USFX";
            Application.DoEvents();

            // Write out the USFX file.
            SFConverter.scripture.languageCode = globe.projectOptions.languageId;
            SFConverter.scripture.WriteUSFX(usfxName);
            SFConverter.scripture.bkInfo.ReadUsfxVernacularNames(Path.Combine(Path.Combine(globe.outputProjectDirectory, "usfx"), "usfx.xml"));
            string bookNames = Path.Combine(globe.outputProjectDirectory, "BookNames.xml");
            SFConverter.scripture.bkInfo.WriteDefaultBookNames(bookNames);
            File.Copy(bookNames, Path.Combine(Path.Combine(globe.outputProjectDirectory, "usfx"), "BookNames.xml"), true);
            bool runResult = globe.projectOptions.lastRunResult;
            bool errorState = Logit.loggedError;
            fileHelper.revisePua(usfxName);
            if (!SFConverter.scripture.hasRefTags)
            {
                globe.projectOptions.makeHotLinks = true;
                SFConverter.scripture.ReadRefTags(usfxName);
            }
            if (!SFConverter.scripture.ValidateUsfx(usfxName))
            {
                if (globe.projectOptions.makeHotLinks && File.Exists(Path.ChangeExtension(usfxName, ".norefxml")))
                {
                    File.Move(usfxName, Path.ChangeExtension(usfxName, ".bad"));
                    File.Move(Path.ChangeExtension(usfxName, ".norefxml"), usfxName);
                    Logit.WriteLine("Retrying validation on usfx.xml without expanded references.");
                    if (SFConverter.scripture.ValidateUsfx(usfxName))
                    {
                        Logit.loggedError = errorState;
                        globe.projectOptions.lastRunResult = runResult;
                        Logit.WriteLine("Validation passed without expanded references.");
                        globe.projectOptions.makeHotLinks = false;
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
                globe.projectOptions.lastRunResult = false;
            }
            if (Logit.loggedWarning)
            {
                globe.projectOptions.warningsFound = true;
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
    		return Path.Combine(globe.outputProjectDirectory, "usfx");
    	}


        public string shortCopyrightMessage, longCopyrightMessage, copyrightLink;

        /// <summary>
        /// Sets the shortCopyrightMessage, longCopyrightMessage, and copyrightLink variables based on the
        /// current globe.m_options values.
        /// </summary>
        public void SetCopyrightStrings()
        {
            if (globe.projectOptions.publicDomain)
            {
                shortCopyrightMessage = longCopyrightMessage = "Public Domain";
                copyrightLink = "<a href='http://en.wikipedia.org/wiki/Public_domain'>Public Domain</a>";
            }
            else if (globe.projectOptions.silentCopyright)
            {
                longCopyrightMessage = shortCopyrightMessage = copyrightLink = String.Empty;
            }
            else if (globe.projectOptions.copyrightOwnerAbbrev.Length > 0)
            {
                shortCopyrightMessage = "© " + globe.projectOptions.copyrightYears + " " + globe.projectOptions.copyrightOwnerAbbrev;
                longCopyrightMessage = "Copyright © " + globe.projectOptions.copyrightYears + " " + globe.projectOptions.copyrightOwner;
                if (globe.projectOptions.copyrightOwnerUrl.Length > 11)
                    copyrightLink = "copyright © " + globe.projectOptions.copyrightYears + " <a href=\"" + globe.projectOptions.copyrightOwnerUrl + "\">" + usfxToHtmlConverter.EscapeHtml(globe.projectOptions.copyrightOwner) + "</a>";
                else
                    copyrightLink = longCopyrightMessage;
            }
            else
            {
                shortCopyrightMessage = "© " + globe.projectOptions.copyrightYears + " " + globe.projectOptions.copyrightOwner;
                longCopyrightMessage = "Copyright " + shortCopyrightMessage;
                if (globe.projectOptions.copyrightOwnerUrl.Length > 11)
                    copyrightLink = "copyright © " + globe.projectOptions.copyrightYears + " <a href=\"" + globe.projectOptions.copyrightOwnerUrl + "\">" + usfxToHtmlConverter.EscapeHtml(globe.projectOptions.copyrightOwner) + "</a>";
                else
                    copyrightLink = longCopyrightMessage;
            }
            if (globe.projectOptions.AudioCopyrightNotice.Length > 1)
            {
                longCopyrightMessage = longCopyrightMessage + "; ℗ " + usfxToHtmlConverter.EscapeHtml(globe.projectOptions.AudioCopyrightNotice);
                copyrightLink = copyrightLink + "<br />℗ " + usfxToHtmlConverter.EscapeHtml(globe.projectOptions.AudioCopyrightNotice);
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
            if (globe.projectOptions.privateProject)
                globe.projectOptions.redistributable = globe.projectOptions.downloadsAllowed = false;
            if (globe.projectOptions.redistributable)
                distributionScope = "redistributable";
            else if (globe.projectOptions.downloadsAllowed)
                distributionScope = "downloadable";
            else
                distributionScope = "restricted";
            s = s.Replace("%d", globe.currentProject);
            s = s.Replace("%e", globe.projectOptions.languageId);
            s = s.Replace("%h", globe.projectOptions.homeDomain);
            s = s.Replace("%c", shortCopyrightMessage);
            s = s.Replace("%C", copyrightLink);
            s = s.Replace("%l", globe.projectOptions.languageName);
            s = s.Replace("%L", globe.projectOptions.languageNameInEnglish);
            s = s.Replace("%D", globe.projectOptions.dialect);
            s = s.Replace("%a", globe.projectOptions.contentCreator);
            s = s.Replace("%A", globe.projectOptions.contributor);
            s = s.Replace("%v", globe.projectOptions.vernacularTitle);
            s = s.Replace("%f", "<a href=\"" + globe.projectOptions.facebook + "\">" + globe.projectOptions.facebook + "</a>");
            s = s.Replace("%F", globe.projectOptions.fcbhId);
            s = s.Replace("%n", globe.projectOptions.EnglishDescription);
            s = s.Replace("%N", globe.projectOptions.lwcDescription);
            s = s.Replace("%p", globe.projectOptions.privateProject ? "private" : "public");
            s = s.Replace("%r", distributionScope);
            s = s.Replace("%T", globe.projectOptions.contentUpdateDate.ToString("yyyy-MM-dd"));
            s = s.Replace("%o", globe.projectOptions.rightsStatement);
            s = s.Replace("%x", globe.projectOptions.promoHtml);
            s = s.Replace("%w", globe.projectOptions.printPublisher);
            s = s.Replace("%i", globe.projectOptions.electronicPublisher);
            s = s.Replace("%P", globe.projectOptions.AudioCopyrightNotice);
            s = s.Replace("%t", globe.projectOptions.translationId);
            string result = s.Replace("%%", "%");
            return result;
        }

        public string GetEpubID()
        {
            if (String.IsNullOrEmpty(globe.projectOptions.epubId))
            {
                string hash = Utils.SHA1HashString(globe.projectOptions.translationId + "|" + globe.projectOptions.fcbhId + "|" + DateTime.UtcNow.ToString("dd M yyyy HH:mm:ss.fffffff") + " http://Haiola.org ");
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
                globe.projectOptions.epubId = uuid.ToString();
                globe.projectOptions.Write();
            }
            return globe.projectOptions.epubId;
        }

        public string copyrightPermissionsStatement()
        {
            string fontClass = globe.projectOptions.fontFamily.ToLower().Replace(' ', '_');
            if (globe.projectOptions.customPermissions)
                return expandPercentEscapes(globe.projectOptions.licenseHtml);
            StringBuilder copr = new StringBuilder();
            copr.Append(String.Format("<h1 class='{2}'>{0}</h1>\n<h2>{1}</h2>\n",
                usfxToHtmlConverter.EscapeHtml(globe.projectOptions.vernacularTitle), 
                usfxToHtmlConverter.EscapeHtml(globe.projectOptions.EnglishDescription), fontClass));
            if (!String.IsNullOrEmpty(globe.projectOptions.lwcDescription))
                copr.Append(String.Format("<h2>{0}</h2>\n", usfxToHtmlConverter.EscapeHtml(globe.projectOptions.lwcDescription)));
            copr.Append(String.Format("<p>{0}<br />\n",copyrightLink));
            if (!String.IsNullOrEmpty(globe.projectOptions.rightsStatement))
                copr.Append(String.Format("{0}<br />\n", globe.projectOptions.rightsStatement));
            copr.Append(String.Format("Language: <a href='http://www.ethnologue.org/language/{0}' class='{2}' target='_blank'>{1}",
                globe.projectOptions.languageId, globe.projectOptions.languageName, fontClass));
            if (globe.projectOptions.languageName != globe.projectOptions.languageNameInEnglish)
                copr.Append(String.Format(" ({0})", usfxToHtmlConverter.EscapeHtml(globe.projectOptions.languageNameInEnglish)));
            copr.Append("</a><br />\n");
            if (!String.IsNullOrEmpty(globe.projectOptions.dialect))
                copr.Append(String.Format("Dialect: {0}<br />", usfxToHtmlConverter.EscapeHtml(globe.projectOptions.dialect)));
            /*
            if (!String.IsNullOrEmpty(globe.m_options.printPublisher))
                copr.Append(String.Format("Primary print publisher: {0}<br />\n", usfxToHtmlConverter.EscapeHtml(globe.m_options.printPublisher)));
            if (!String.IsNullOrEmpty(globe.m_options.electronicPublisher))
                copr.Append(String.Format("Electronic publisher: {0}<br />\n", usfxToHtmlConverter.EscapeHtml(globe.m_options.electronicPublisher)));
            */
            if (!String.IsNullOrEmpty(globe.projectOptions.contentCreator))
                copr.Append(String.Format("Translation by: {0}<br />\n", usfxToHtmlConverter.EscapeHtml(globe.projectOptions.contentCreator)));
            if ((!String.IsNullOrEmpty(globe.projectOptions.contributor)) && (globe.projectOptions.contentCreator != globe.projectOptions.contributor))
                copr.Append(String.Format("Contributor: {0}<br />\n", usfxToHtmlConverter.EscapeHtml(globe.projectOptions.contributor)));
            copr.Append("</p>\n");
            if (!String.IsNullOrEmpty(globe.projectOptions.promoHtml))
                copr.Append(globe.projectOptions.promoHtml);
            if (globe.projectOptions.ccbyndnc)
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
            if (globe.projectOptions.ccbysa)
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
            if (globe.projectOptions.ccbynd)
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
            copr.Append(String.Format("<p>{0}</p>\n", globe.projectOptions.contentUpdateDate.ToString("yyyy-MM-dd")));
            return copr.ToString();
        }

        private void ConvertUsfxToEPub()
        {
            if ((globe.projectOptions.languageId.Length < 3) || (globe.projectOptions.translationId.Length < 3))
                return;
            currentConversion = "writing ePub";
            string UsfxPath = Path.Combine(globe.outputProjectDirectory, "usfx");
            string epubPath = Path.Combine(globe.outputProjectDirectory, "epub");
            string htmlPath = Path.Combine(epubPath, "OEBPS");
            if (!Directory.Exists(UsfxPath))
            {
                MessageBox.Show(this, UsfxPath + " not found!", "ERROR");
                return;
            }
            Utils.EnsureDirectory(globe.outputDirectory);
            Utils.EnsureDirectory(globe.outputProjectDirectory);
            Utils.EnsureDirectory(epubPath);
            Utils.EnsureDirectory(htmlPath);
            string epubCss = Path.Combine(htmlPath, "epub.css");
            string logFile = Path.Combine(globe.outputProjectDirectory, "epubConversionReport.txt");
            Logit.OpenFile(logFile);
            Logit.GUIWriteString = showMessageString;
            Logit.UpdateStatus = updateConversionProgress;

            string fontSource = Path.Combine(globe.dataRootDir, "fonts");
            string fontName = globe.projectOptions.fontFamily.ToLower().Replace(' ', '_');
            // Copy cascading style sheet from project directory, or if not there, create a font specification section and append it to BibleConv/input/epub.
            string specialCss = Path.Combine(globe.inputProjectDirectory, "epub.css");
            if (File.Exists(specialCss))
            {
                File.Copy(specialCss, epubCss);
            }
            else
            {
                StreamReader sr;
                specialCss = Path.Combine(globe.inputDirectory, "epub.css");
                if (File.Exists(specialCss))
                    sr = new StreamReader(specialCss);
                else
                    sr = new StreamReader(SFConverter.FindAuxFile("epub.css"));
                string epubStyleSheet = sr.ReadToEnd();
                sr.Close();
                StreamWriter sw = new StreamWriter(epubCss);
                sw.WriteLine("@font-face {{font-family:'{0}';src: url('{0}.ttf') format('truetype');src: url('{0}.woff') format('woff');font-weight:normal;font-style:normal}}",
                    fontName);
                sw.WriteLine("html,body,div.main,div.footnote,div,ol.nav,h1,ul.tnav {{font-family:'{0}','{1}','Liberation Sans','liberationsans_regular','sans-serif'}}",fontName, globe.projectOptions.fontFamily);
                if (globe.projectOptions.commonChars)
                {
                    sw.WriteLine(".chapterlabel,.mt,.tnav,h1.title,a.xx,a.oo,a.nn {{'Liberation Sans','liberationsans_regular','sans-serif'}}");
                }
                sw.WriteLine("* {margin:0;padding:0}");
                sw.WriteLine("html,body	{0}height:100%;font-size:1.0em;line-height:{1}em{2}", "{", (globe.projectOptions.script.ToLowerInvariant() == "latin")?"1.2":"2.5", "}");
                sw.Write(epubStyleSheet);
                sw.Close();
            }
            fileHelper.CopyFile(Path.Combine(fontSource, fontName + ".woff"), Path.Combine(htmlPath, fontName + ".woff"));
            fileHelper.CopyFile(Path.Combine(fontSource, fontName + ".ttf"), Path.Combine(htmlPath, fontName + ".ttf"));
            fileHelper.CopyFile(Path.Combine(fontSource, "liberationsans_regular.woff"), Path.Combine(htmlPath, "liberationsans_regular.woff"));
            fileHelper.CopyFile(Path.Combine(fontSource, "liberationsans_regular.ttf"), Path.Combine(htmlPath, "liberationsans_regular.ttf"));
            ePubWriter toEpub = new ePubWriter();
            toEpub.projectOptions = globe.projectOptions;
            toEpub.projectOutputDir = globe.outputProjectDirectory;
            toEpub.epubDirectory = epubPath;
            toEpub.redistributable = globe.projectOptions.redistributable;
            toEpub.epubIdentifier = GetEpubID();
            toEpub.stripPictures = false;
            toEpub.indexDate = DateTime.UtcNow;
            if (globe.projectOptions.DBSandeBible && !globe.projectOptions.privateProject)
            {
                toEpub.indexDateStamp = "ePub generated by <a href='http://eBible.org'>eBible.org</a> using <a href='http://haiola.org'>Haiola</a> on " + toEpub.indexDate.ToString("d MMM yyyy") +
                        " from source files dated " + globe.sourceDate.ToString("d MMM yyyy") +
                        "<br/>";
                if (globe.projectOptions.countryCode == "PG" && File.Exists(pngbtaLogo) && globe.projectOptions.redistributable)
                {
                    toEpub.indexDateStamp = "<a href='pngbta.org'><img src='pngbta.jpg' alt='PNG Bible Translation Association' title='Published by the PNG Bible Translation Association'/></a><br/>Posted on <a href='http://png.bible'>png.bible</a> and <a href='http://TokPlesBaibel.org'>TokPlesBaibel.org</a> by the <a href='http://pngbta.org'>PNG Bible Translation Association</a>.<br/>" + toEpub.indexDateStamp;
                    fileHelper.CopyFile(pngbtaLogo, Path.Combine(htmlPath, "pngbta.jpg"));
                }
            }
            else
            {
                toEpub.indexDateStamp = "ePub generated by <a href='http://haiola.org'>Haiola</a> " + toEpub.indexDate.ToString("d MMM yyyy") +
                    " from source files dated " + globe.sourceDate.ToString("d MMM yyyy");
            }
            toEpub.GeneratingConcordance = globe.projectOptions.GenerateConcordance || globe.projectOptions.UseFrames;
            toEpub.CrossRefToFilePrefixMap = globe.projectOptions.CrossRefToFilePrefixMap;
            toEpub.contentCreator = globe.projectOptions.contentCreator;
            toEpub.contributor = globe.projectOptions.contributor;
            string usfxFilePath = Path.Combine(UsfxPath, "usfx.xml");
            toEpub.bookInfo.ReadPublicationOrder(orderFile);
            toEpub.MergeXref(Path.Combine(globe.inputProjectDirectory, "xref.xml"));
            //TODO: eliminate side effects in expandPercentEscapes
            // Side effect: expandPercentEscapes sets longCopyrightMessage and shortCopyrightMessage.
            toEpub.sourceLink = expandPercentEscapes("<a href=\"http://%h/%t\">%v</a>");
            toEpub.longCopr = longCopyrightMessage;
            toEpub.shortCopr = shortCopyrightMessage;
            toEpub.textDirection = globe.projectOptions.textDir;
            toEpub.customCssName = "epub.css";
            toEpub.stripManualNoteOrigins = globe.projectOptions.stripNoteOrigin;
            toEpub.noteOriginFormat = globe.projectOptions.xoFormat;
            toEpub.englishDescription = globe.projectOptions.EnglishDescription;
            toEpub.preferredFont = globe.projectOptions.fontFamily;
            toEpub.fcbhId = globe.projectOptions.fcbhId;
            toEpub.coverName = Path.GetFileName(preferredCover);
            string coverPath = Path.Combine(htmlPath, toEpub.coverName);
            File.Copy(preferredCover, coverPath);
            if (globe.projectOptions.PrepublicationChecks &&
                (globe.projectOptions.publicDomain || globe.projectOptions.redistributable || File.Exists(Path.Combine(globe.inputProjectDirectory, "certify.txt"))) &&
                File.Exists(certified))
            {
                File.Copy(certified, Path.Combine(htmlPath, "eBible.org_certified.jpg"));
                toEpub.indexDateStamp = toEpub.indexDateStamp + "<br /><a href='http://eBible.org/certified/' target='_blank'><img src='eBible.org_certified.jpg' alt='eBible.org certified' /></a>";
            }
            toEpub.xrefCall.SetMarkers(globe.projectOptions.xrefCallers);
            toEpub.footNoteCall.SetMarkers(globe.projectOptions.footNoteCallers);
            toEpub.projectInputDir = globe.inputProjectDirectory;
            toEpub.ConvertUsfxToHtml(usfxFilePath, htmlPath,
                globe.projectOptions.vernacularTitle,
                globe.projectOptions.languageId,
                globe.projectOptions.translationId,
                globe.projectOptions.chapterLabel,
                globe.projectOptions.psalmLabel,
                "<a class='xx' href='copyright.xhtml'>" +  usfxToHtmlConverter.EscapeHtml(shortCopyrightMessage) + "</a>",
                expandPercentEscapes(globe.projectOptions.homeLink),
                expandPercentEscapes(globe.projectOptions.footerHtml),
                expandPercentEscapes(globe.projectOptions.indexHtml),
                copyrightPermissionsStatement(),
                globe.projectOptions.ignoreExtras,
                globe.projectOptions.goText);
            toEpub.bookInfo.RecordStats(globe.projectOptions);
            globe.projectOptions.commonChars = toEpub.commonChars;
            globe.projectOptions.Write();
            LoadStatisticsTab();
            Logit.CloseFile();
            if (Logit.loggedError)
            {
                globe.projectOptions.lastRunResult = false;
            }
            if (Logit.loggedWarning)
            {
                globe.projectOptions.warningsFound = true;
            }
        }

        private void ConvertUsfxToPortableHtml()
        {
            int i;
            if ((globe.projectOptions.languageId.Length < 3) || (globe.projectOptions.translationId.Length < 3))
                return;
            currentConversion = "writing portable HTML";
            string UsfxPath = Path.Combine(globe.outputProjectDirectory, "usfx");
            string htmlPath = Path.Combine(globe.outputProjectDirectory, "html");
            if (!Directory.Exists(UsfxPath))
            {
                MessageBox.Show(this, UsfxPath + " not found!", "ERROR");
                return;
            }
            Utils.EnsureDirectory(globe.outputDirectory);
            Utils.EnsureDirectory(globe.outputProjectDirectory);
            Utils.EnsureDirectory(htmlPath);
            string propherocss = Path.Combine(htmlPath, globe.projectOptions.customCssFileName);

                Utils.DeleteFile(propherocss);
            // Copy cascading style sheet from project directory, or if not there, BibleConv/input/.
            string specialCss = Path.Combine(globe.inputProjectDirectory, globe.projectOptions.customCssFileName);
            if (File.Exists(specialCss))
                File.Copy(specialCss, propherocss);
            else
                File.Copy(Path.Combine(globe.inputDirectory, globe.projectOptions.customCssFileName), propherocss);

            // Copy any extra files from the htmlextras directory in the project directory to the output.
            // This is for introduction files, pictures, etc.
            string htmlExtras = Path.Combine(globe.inputProjectDirectory, "htmlextras");
            if (Directory.Exists(htmlExtras))
            {
                WordSend.fileHelper.CopyDirectory(htmlExtras, htmlPath);
            }
            
            usfxToHtmlConverter toHtm;
			if (globe.projectOptions.UseFrames)
			{
				var framedConverter = new UsfxToFramedHtmlConverter();
				framedConverter.HideNavigationButtonText = globe.projectOptions.HideNavigationButtonText;
				framedConverter.ShowNavigationButtonText = globe.projectOptions.ShowNavigationButtonText;
				toHtm = framedConverter;
			}
			else
			{
                if (globe.projectOptions.GenerateMobileHtml)
                {
                    toHtm = new usfx2MobileHtml();
                }
                else
                {
                    toHtm = new usfxToHtmlConverter();
                }
			}
            toHtm.Jesusfilmlink = globe.projectOptions.JesusFilmLinkTarget;
            toHtm.Jesusfilmtext = globe.projectOptions.JesusFilmLinkText;
            toHtm.stripPictures = false;
            toHtm.htmlextrasDir = Path.Combine(globe.inputProjectDirectory, "htmlextras");
            string logFile = Path.Combine(globe.outputProjectDirectory, "HTMLConversionReport.txt");
            Logit.OpenFile(logFile);
            Logit.GUIWriteString = showMessageString;
            Logit.UpdateStatus = updateConversionProgress;
            string theIndexDate = toHtm.indexDate.ToString("d MMM yyyy");
            string thesourceDate = globe.sourceDate.ToString("d MMM yyyy");
            if (File.Exists(dbsLogo) && !globe.projectOptions.privateProject)
            {
                toHtm.indexDateStamp = "HTML generated with <a href='http://haiola.org'>Haiola</a> by <a href='http://eBible.org'>eBible.org</a> " +
                    toHtm.indexDate.ToString("d MMM yyyy") +
                    " from source files dated " + globe.sourceDate.ToString("d MMM yyyy") +
                    "</a><br/>";
                //fileHelper.CopyFile(dbsLogo, Path.Combine(htmlPath, "dbs.jpg"));
                if (globe.projectOptions.countryCode == "PG" && File.Exists(pngbtaLogo) && globe.projectOptions.redistributable)
                {
                    toHtm.indexDateStamp = "<a href='pngbta.org'><img src='pngbta.jpg' alt='PNG Bible Translation Association' title='Published by the PNG Bible Translation Association'/></a><br/>Posted on <a href='http://png.bible'>png.bible</a> and <a href='http://TokPlesBaibel.org'>TokPlesBaibel.org</a> by the <a href='http://pngbta.org'>PNG Bible Translation Association</a>.<br/>" + toHtm.indexDateStamp;
                    fileHelper.CopyFile(pngbtaLogo, Path.Combine(htmlPath, "pngbta.jpg"));
                }
            }
            else
            {
                toHtm.indexDateStamp = "HTML generated by <a href='http://haiola.org'>Haiola</a> " + toHtm.indexDate.ToString("d MMM yyyy") +
                    " from source files dated " + globe.sourceDate.ToString("d MMM yyyy");
            }
        	toHtm.GeneratingConcordance = globe.projectOptions.GenerateConcordance || globe.projectOptions.UseFrames;
    		toHtm.CrossRefToFilePrefixMap = globe.projectOptions.CrossRefToFilePrefixMap;
    		string usfxFilePath = Path.Combine(UsfxPath, "usfx.xml");
            toHtm.bookInfo.ReadPublicationOrder(orderFile);
            toHtm.MergeXref(Path.Combine(globe.inputProjectDirectory, "xref.xml"));
            toHtm.sourceLink = expandPercentEscapes("<a href=\"http://%h/%t\">%v</a>");
            toHtm.textDirection = globe.projectOptions.textDir;
            toHtm.customCssName = globe.projectOptions.customCssFileName;
            toHtm.stripManualNoteOrigins = globe.projectOptions.stripNoteOrigin;
            toHtm.noteOriginFormat = globe.projectOptions.xoFormat;
            toHtm.englishDescription = globe.projectOptions.EnglishDescription;
            toHtm.preferredFont = globe.projectOptions.fontFamily;
            toHtm.fcbhId = globe.projectOptions.fcbhId;
            toHtm.redistributable = globe.projectOptions.redistributable;
            toHtm.coverName = String.Empty;// = Path.GetFileName(preferredCover);
            toHtm.projectOutputDir = globe.outputProjectDirectory;
            toHtm.projectOptions = globe.projectOptions;

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
            toHtm.xrefCall.SetMarkers(globe.projectOptions.xrefCallers);
            toHtm.projectInputDir = globe.inputProjectDirectory;
            toHtm.footNoteCall.SetMarkers(globe.projectOptions.footNoteCallers);
            toHtm.ConvertUsfxToHtml(usfxFilePath, htmlPath,
                globe.projectOptions.vernacularTitle,
                globe.projectOptions.languageId,
                globe.projectOptions.translationId,
                globe.projectOptions.chapterLabel,
                globe.projectOptions.psalmLabel,
                "<a href='copyright.htm'>" + usfxToHtmlConverter.EscapeHtml(shortCopyrightMessage) + "</a>",
                expandPercentEscapes(globe.projectOptions.homeLink),
                expandPercentEscapes(globe.projectOptions.footerHtml),
                expandPercentEscapes(globe.projectOptions.indexHtml),
                copyrightPermissionsStatement(),
                globe.projectOptions.ignoreExtras,
                globe.projectOptions.goText);
            toHtm.bookInfo.RecordStats(globe.projectOptions);
            globe.projectOptions.commonChars = toHtm.commonChars;
            globe.projectOptions.Write();
            string fontsDir = Path.Combine(htmlPath, "fonts");
            fileHelper.EnsureDirectory(fontsDir);
            string fontSource = Path.Combine(globe.dataRootDir, "fonts");
            string fontName = globe.projectOptions.fontFamily.ToLower().Replace(' ', '_');
            fileHelper.CopyFile(Path.Combine(fontSource, fontName + ".ttf"), Path.Combine(fontsDir, fontName + ".ttf"));
            fileHelper.CopyFile(Path.Combine(fontSource, fontName + ".woff"), Path.Combine(fontsDir, fontName + ".woff"));
            fileHelper.CopyFile(Path.Combine(fontSource, fontName + ".eot"), Path.Combine(fontsDir, fontName + ".eot"));
            LoadStatisticsTab();
            Logit.CloseFile();
            if (Logit.loggedError)
            {
                globe.projectOptions.lastRunResult = false;
            }
            if (Logit.loggedWarning)
            {
                globe.projectOptions.warningsFound = true;
            }

            currentConversion = "Writing auxilliary metadata files.";
            Application.DoEvents();
            if (!fileHelper.fAllRunning)
                return;

            // We currently have the information handy to write some auxilliary XML files
            // that contain metadata. We will put these in the USFX directory.

            XmlTextWriter xml = new XmlTextWriter(Path.Combine(UsfxPath, globe.projectOptions.translationId + "-VernacularParms.xml"), System.Text.Encoding.UTF8);
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
            xml.WriteElementString("dc:creator", globe.projectOptions.contentCreator);
            xml.WriteElementString("dc:contributor", globe.projectOptions.contributor);
            string title = globe.projectOptions.vernacularTitle;
            if (title.Length == 0)
                title = globe.projectOptions.EnglishDescription;
            xml.WriteElementString("dc:title", title);
            xml.WriteElementString("dc:description", globe.projectOptions.EnglishDescription);
            xml.WriteElementString("dc:date", globe.projectOptions.contentUpdateDate.ToString("yyyy-MM-dd"));
            xml.WriteElementString("dc:format", "digital");
            xml.WriteElementString("dc:language", globe.projectOptions.languageId);
            xml.WriteElementString("dc:publisher", globe.projectOptions.electronicPublisher);
            string rights = String.Empty;
            string shortRights = globe.projectOptions.translationId + " Scripture ";
            string copyright = "Copyright © " + globe.projectOptions.copyrightYears + " " +  globe.projectOptions.copyrightOwner;
            if (globe.projectOptions.publicDomain)
            {
                copyright = rights = "Public Domain";
                shortRights = shortRights + "is in the Public Domain.";
            }
            else if (globe.projectOptions.ccbyndnc)
            {
                rights = copyright + @"
This work is made available to you under the terms of the Creative Commons Attribution-Noncommercial-No Derivative Works license at http://creativecommons.org/licenses/by-nc-nd/4.0/.
You may convert the text to different file formats or make extracts, as long as you don't change any of the text or punctuation of the content." +
                Environment.NewLine + globe.projectOptions.rightsStatement;
                shortRights = shortRights + copyright + " Creative Commons BY-NC-ND license.";
            }
            else if (globe.projectOptions.ccbynd)
            {
                rights = copyright + @"
This work is made available to you under the terms of the Creative Commons Attribution-No Derivative Works license at http://creativecommons.org/licenses/by-nd/4.0/." +
                Environment.NewLine + globe.projectOptions.rightsStatement;
                shortRights = shortRights + copyright + " Creative Commons BY-ND license.";
            }
            else if (globe.projectOptions.ccbysa)
            {
                rights = copyright + @"
This work is made available to you under the terms of the Creative Commons Attribution-Share-Alike license at http://creativecommons.org/licenses/by-sa/4.0/." +
                Environment.NewLine + globe.projectOptions.rightsStatement;
                shortRights = shortRights + copyright + " Creative Commons BY-SA license.";
            }
            else if (globe.projectOptions.otherLicense)
            {
                rights = copyright + Environment.NewLine + globe.projectOptions.rightsStatement;
                shortRights = shortRights + copyright;
            }
            else if (globe.projectOptions.allRightsReserved)
            {
                rights = copyright + " All rights reserved.";
                shortRights = shortRights + rights;
                if (globe.projectOptions.rightsStatement.Length > 0)
                    rights = rights + Environment.NewLine + globe.projectOptions.rightsStatement;
            }
            xml.WriteElementString("dc:rights", rights);
            xml.WriteElementString("dc:identifier", String.Empty);
            xml.WriteElementString("dc:type", String.Empty);
            xml.WriteEndElement();  // dcMeta
            xml.WriteElementString("numberSystem", globe.projectOptions.numberSystem);
            xml.WriteElementString("chapterAndVerseSeparator", globe.projectOptions.CVSeparator);
            xml.WriteElementString("rangeSeparator", globe.projectOptions.rangeSeparator);
            xml.WriteElementString("multiRefSameChapterSeparator", globe.projectOptions.multiRefSameChapterSeparator);
            xml.WriteElementString("multiRefDifferentChapterSeparator", globe.projectOptions.multiRefDifferentChapterSeparator);
            xml.WriteElementString("verseNumberLocation", globe.projectOptions.verseNumberLocation);
            xml.WriteElementString("footnoteMarkerStyle", globe.projectOptions.footnoteMarkerStyle);
            xml.WriteElementString("footnoteMarkerResetAt", globe.projectOptions.footnoteMarkerResetAt);
            xml.WriteElementString("footnoteMarkers", globe.projectOptions.footNoteCallers);
            xml.WriteElementString("BookSourceForMarkerXt", globe.projectOptions.BookSourceForMarkerXt);
            xml.WriteElementString("BookSourceForMarkerR", globe.projectOptions.BookSourceForMarkerR);
            xml.WriteElementString("iso", globe.projectOptions.languageId);
            xml.WriteElementString("isoVariant", globe.projectOptions.dialect);
            xml.WriteElementString("langName", globe.projectOptions.languageName);
            xml.WriteElementString("textDir", globe.projectOptions.textDir);
            xml.WriteElementString("hasNotes", (!globe.projectOptions.ignoreExtras).ToString()); //TODO: check to see if translation has notes or not.
            xml.WriteElementString("coverTitle", globe.projectOptions.vernacularTitle);
            xml.WriteEndElement();	// vernacularParms
            xml.WriteEndDocument();
            xml.Close();

            xml = new XmlTextWriter(Path.Combine(UsfxPath, globe.projectOptions.translationId + "-VernacularAdditional.xml"), System.Text.Encoding.UTF8);
            xml.Formatting = System.Xml.Formatting.Indented;
            xml.WriteStartDocument();
            xml.WriteStartElement("vernacularParmsMiscellaneous");
            xml.WriteElementString("translationId", globe.projectOptions.translationId);
            // xml.WriteElementString("otmlId", " ");
            xml.WriteElementString("versificationScheme", globe.projectOptions.versificationScheme);
            xml.WriteElementString("checkVersification", "No");
            // xml.WriteElementString("osis2SwordOptions", globe.m_options.osis2SwordOptions);
            // xml.WriteElementString("otmlRenderChapterNumber", globe.m_options.otmlRenderChapterNumber);
            xml.WriteElementString("copyright", shortRights);
            xml.WriteEndElement();	// vernacularParmsMiscellaneous
            xml.WriteEndDocument();
            xml.Close();

            // Write the ETEN DBL MetaData.xml file in the usfx directory.
            string metaXmlName = Path.Combine(UsfxPath, globe.projectOptions.translationId + "metadata.xml");
            xml = new XmlTextWriter(metaXmlName, System.Text.Encoding.UTF8);
            xml.Formatting = System.Xml.Formatting.Indented;
            xml.WriteStartDocument();
            // xml.WriteProcessingInstruction("xml-model", "href=\"metadataWbt-1.3.rnc\" type=\"application/relax-ng-compact-syntax\"");
            xml.WriteStartElement("DBLMetadata");
            string etendblid = globe.projectOptions.paratextGuid;
            if (etendblid.Length > 16)
                etendblid = etendblid.Substring(0, 16);
            xml.WriteAttributeString("id", etendblid);
            // xml.WriteAttributeString("revision", "4");
            xml.WriteAttributeString("type", "text");
            xml.WriteAttributeString("typeVersion", "1.5");
            xml.WriteStartElement("identification");
            xml.WriteElementString("name", globe.projectOptions.shortTitle);
            xml.WriteElementString("nameLocal", globe.projectOptions.vernacularTitle);
            xml.WriteElementString("abbreviation", globe.projectOptions.translationId);
            string abbreviationLocal = globe.projectOptions.translationTraditionalAbbreviation;
            if (abbreviationLocal.Length < 2)
            {
                abbreviationLocal = globe.projectOptions.translationId.ToUpperInvariant();
            }
            xml.WriteElementString("abbreviationLocal", abbreviationLocal);
            string scope = "Portion only";
            if (globe.projectOptions.ntBookCount == 27)
            {
                if (globe.projectOptions.otBookCount == 39)
                {
                    if (globe.projectOptions.adBookCount > 0)
                    {
                        scope = "Bible with Deuterocanon";
                    }
                    else
                    {
                        scope = "Bible without Deuterocanon";
                    }
                }
                else if (globe.projectOptions.otBookCount > 0)
                {
                    if ((globe.projectOptions.otBookCount == 1) && (globe.projectOptions.otChapCount == 150))
                        scope = "New Testament and Psalms";
                    else
                        scope = "New Testament and Shorter Old Testament";
                }
                else
                {
                    scope = "NT";   // "New Testament only" is also allowed here.
                }
            }
            else if (globe.projectOptions.otBookCount == 39)
            {
                if (globe.projectOptions.ntBookCount == 0)
                {
                    if (globe.projectOptions.adBookCount > 0)
                        scope = "Old Testament with Deuterocanon";
                    else
                        scope = "Old Testament only";
                }
            }
            xml.WriteElementString("scope", scope);
            xml.WriteElementString("description", globe.projectOptions.EnglishDescription);
            string yearCompleted = globe.projectOptions.copyrightYears.Trim();
            if (yearCompleted.Length > 4)
                yearCompleted = yearCompleted.Substring(yearCompleted.Length - 4);
            xml.WriteElementString("dateCompleted", yearCompleted);
            xml.WriteStartElement("systemId");
            xml.WriteAttributeString("fullname", globe.projectOptions.shortTitle);
            xml.WriteAttributeString("name", globe.projectOptions.paratextProject);
            xml.WriteAttributeString("type", "paratext");

            xml.WriteEndElement();
            xml.WriteElementString("bundleProducer", "");
            xml.WriteEndElement();  // identification
            xml.WriteElementString("confidential", "false");
            /*
            xml.WriteStartElement("agencies");
            string etenPartner = "WBT";
            if ((globe.m_options.publicDomain == true) || globe.m_options.copyrightOwner.ToUpperInvariant().Contains("EBIBLE"))
                etenPartner = "eBible.org";
            else if (globe.m_options.copyrightOwner.ToUpperInvariant().Contains("SOCIETY"))
                etenPartner = "UBS";
            else if (globe.m_options.copyrightOwner.ToUpperInvariant().Contains("BIBLICA"))
                etenPartner = "Biblica";
            else if (globe.m_options.copyrightOwnerAbbrev.ToUpperInvariant().Contains("PBT"))
                etenPartner = "PBT";
            else if (globe.m_options.copyrightOwnerAbbrev.ToUpperInvariant().Contains("SIM"))
                etenPartner = "SIM";
            xml.WriteElementString("etenPartner", etenPartner);
            xml.WriteElementString("creator", globe.m_options.contentCreator);
            xml.WriteElementString("publisher", globe.m_options.electronicPublisher);
            xml.WriteElementString("contributor", globe.m_options.contributor);
            xml.WriteEndElement();  // agencies
            */
            xml.WriteStartElement("language");
            xml.WriteElementString("iso", globe.projectOptions.languageId);
            xml.WriteElementString("name", globe.projectOptions.languageNameInEnglish);
            xml.WriteElementString("nameLocal", globe.projectOptions.languageName);
            xml.WriteElementString("ldml", globe.projectOptions.ldml);
            xml.WriteElementString("rod", globe.projectOptions.rodCode);
            xml.WriteElementString("script", globe.projectOptions.script);
            xml.WriteElementString("scriptDirection", globe.projectOptions.textDir.ToUpperInvariant());
            string numerals = globe.projectOptions.numberSystem;
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
            xml.WriteElementString("iso", globe.projectOptions.countryCode);
            xml.WriteElementString("name", globe.projectOptions.country);
            xml.WriteEndElement();  // country
            xml.WriteStartElement("type");
            if (globe.projectOptions.copyrightYears.Length > 4)
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
            xml.WriteElementString("name", globe.projectOptions.shortTitle);
            xml.WriteElementString("nameLocal", globe.projectOptions.vernacularTitle);
            xml.WriteElementString("abbreviation", globe.projectOptions.translationId);
            xml.WriteElementString("abbreviationLocal", abbreviationLocal);
            xml.WriteElementString("description", globe.projectOptions.canonTypeEnglish);    // Book list description, like common, Protestant, or Catholic
            xml.WriteElementString("descriptionLocal", globe.projectOptions.canonTypeLocal);
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
            if (!globe.projectOptions.publicDomain)
            {
                xml.WriteStartElement("contact");
                xml.WriteElementString("rightsHolder", globe.projectOptions.copyrightOwner);
                string localRights = globe.projectOptions.localRightsHolder.Trim();
                if (localRights.Length == 0)
                    localRights = globe.projectOptions.copyrightOwner;
                xml.WriteElementString("rightsHolderLocal", localRights);
                string rightsHolderAbbreviation = globe.projectOptions.copyrightOwnerAbbrev.Trim();
                if (rightsHolderAbbreviation.Length < 1)
                {
                    string s = globe.projectOptions.copyrightOwner.Trim().ToUpperInvariant().Replace(" OF ", " ");
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
                string ownerUrl = globe.projectOptions.copyrightOwnerUrl;
                if (ownerUrl.ToUpperInvariant().StartsWith("HTTPS://"))
                    ownerUrl = ownerUrl.Substring(8);
                if (ownerUrl.ToUpperInvariant().StartsWith("HTTP://"))
                    ownerUrl = ownerUrl.Substring(7);
                xml.WriteElementString("rightsHolderURL", ownerUrl);
                xml.WriteElementString("rightsHolderFacebook", globe.projectOptions.facebook);
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
            if (globe.projectOptions.promoHtml.Trim().Length > 3)
                xml.WriteString(globe.projectOptions.promoHtml);
            else
                xml.WriteElementString("p", rights);
            xml.WriteEndElement();  // promoVersionInfo
            xml.WriteStartElement("promoEmail");
            xml.WriteAttributeString("contentType", "xhtml");
            xml.WriteString(@"Thank you for downloading ");
            xml.WriteString(globe.projectOptions.vernacularTitle);
            xml.WriteString(@"! Now you'll have anytime, anywhere access to God's Word on your mobile device—even if 
you're outside of service coverage or not connected to the Internet. It also means faster service whenever you read that version since it's 
stored on your device. Enjoy! This download was made possible by ");
            xml.WriteString(globe.projectOptions.copyrightOwner.Trim(new char[]{' ', '.'}));
            xml.WriteString(@". We really appreciate their passion for making the Bible available to millions of people around the world. Because of 
their generosity, people like you can open up the Bible and hear from God no matter where you are. You can learn more about them at ");
            xml.WriteString(globe.projectOptions.copyrightOwnerUrl);
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


            if (globe.projectOptions.UseFrames)
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
				ciMaker.IntroductionLinkText = globe.projectOptions.IntroductionLinkText;
    			ciMaker.ConcordanceLinkText = globe.projectOptions.ConcordanceLinkText;
				string chapIndexPath = Path.Combine(htmlPath, UsfxToChapterIndex.ChapIndexFileName);
				ciMaker.Generate(usfxFilePath, chapIndexPath);
				EnsureTemplateFile("chapIndex.css", htmlPath);
				EnsureTemplateFile("frameFuncs.js", htmlPath);
				EnsureTemplateFile("Navigation.js", htmlPath);
			}

			// Todo JohnT: move this to a new method, and the condition to the method that calls this.
			if (globe.projectOptions.GenerateConcordance || globe.projectOptions.UseFrames)
			{
                /*****
				currentConversion = "generate XHTML for concordance";
				usfxToHtmlConverter toXhtm = new usfxToXhtmlConverter();
				Logit.OpenFile(Path.Combine(m_outputProjectDirectory, "XHTMLConversionReport.txt"));

				toXhtm.indexDateStamp = "XHTML generated " + DateTime.UtcNow.ToString("d MMM yyyy") +
				                       " from source files dated " + globe.sourceDate.ToString("d MMM yyyy");
				string xhtmlPath = Path.Combine(m_outputProjectDirectory, "xhtml");
				Utils.EnsureDirectory(xhtmlPath);
				// No point in doing this...doesn't change the concordance generated, just makes generation slower.
				// Reinstate it if the XHTML is used for anything besides generating the concordance.
				//toXhtm.CrossRefToFilePrefixMap = globe.m_options.CrossRefToFilePrefixMap;
				toXhtm.ConvertUsfxToHtml(usfxFilePath, xhtmlPath,
				                        globe.m_options.vernacularTitle,
				                        globe.m_options.languageId,
				                        globe.m_options.translationId,
				                        globe.m_options.chapterLabel,
				                        globe.m_options.psalmLabel,
				                        globe.m_options.copyrightLink,
				                        globe.m_options.homeLink,
				                        globe.m_options.footerHtml,
				                        globe.m_options.indexHtml,
				                        globe.m_options.licenseHtml,
				                        globe.m_options.useKhmerDigits,
				                        globe.m_options.ignoreExtras,
				                        globe.m_options.goText);
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
				var concGenerator = new ConcGenerator(globe.inputProjectDirectory, concordanceDirectory)
				                    	{
											// Currently configurable options
											MergeCase = globe.projectOptions.MergeCase,
											MaxContextLength = globe.projectOptions.MaxContextLength,
											MinContextLength =  globe.projectOptions.MinContextLength,
											WordformingChars = globe.projectOptions.WordformingChars,
											MaxFrequency = globe.projectOptions.MaxFrequency,
											Phrases = globe.projectOptions.Phrases,
											ExcludeWords = new HashSet<string>(globe.projectOptions.ExcludeWords.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)),
											ReferenceAbbeviationsMap = globe.projectOptions.ReferenceAbbeviationsMap,
											BookChapText = globe.projectOptions.BooksAndChaptersLinkText,
											ConcordanceLinkText = globe.projectOptions.ConcordanceLinkText,

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
                
                concGenerator.Run(Path.Combine(Path.Combine(globe.outputProjectDirectory, "search"), "verseText.xml"));

				var concFrameGenerator = new ConcFrameGenerator()
				                         	{ConcDirectory = concordanceDirectory, LangName = globe.projectOptions.vernacularTitle};
                concFrameGenerator.customCssName = globe.projectOptions.customCssFileName;
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
            switch (globe.projectOptions.epubId[7])
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
            return Convert.ToInt32(globe.projectOptions.epubId.Substring(10, 1), 16) & 3;
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
            string coverOutput = Path.Combine(globe.outputProjectDirectory, "cover");
            if (!small)
                Utils.DeleteDirectory(coverOutput);
            Utils.EnsureDirectory(coverOutput);

            // Get the best cover.svg available.
            string coverIn = Path.Combine(globe.inputProjectDirectory, "cover.svg");
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
                    if (globe.projectOptions.vernacularTitle.Length > 50)
                    {
                        svg = svg.Replace("font-size=\"100\"", "font-size=\"60\"");
                        mainTitle = ShortWordWrap(globe.projectOptions.vernacularTitle, 48).Split(newLine);
                    }
                    else if (globe.projectOptions.vernacularTitle.Length < 24)
                    {
                        svg = svg.Replace("font-size=\"100\"", "font-size=\"140\"");
                        mainTitle = ShortWordWrap(globe.projectOptions.vernacularTitle, 17).Split(newLine);
                    }
                    else
                    {
                        mainTitle = ShortWordWrap(globe.projectOptions.vernacularTitle, 20).Split(newLine);
                    }
                    string[] description = ShortWordWrap(globe.projectOptions.EnglishDescription, 60).Split(newLine);
                    sw = new StreamWriter(coverOut);
                    svg = svg.Replace("^f", globe.projectOptions.fontFamily).Replace("^4", description[0]);
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
            /*
            string dbsCoverIn = Path.Combine(globe.inputProjectDirectory, "dbscover.png");
            globe.projectOptions.dbsCover = false;
            */
            if (File.Exists(coverIn))
            {
                fileHelper.CopyFile(coverIn, coverOut);
            }
            /*
            else if (File.Exists(dbsCoverIn))
            {
                fileHelper.CopyFile(dbsCoverIn, coverOut);
                globe.projectOptions.dbsCover = true;
            }
            */
            // Look for .jpg files.
            coverOut = Path.ChangeExtension(coverOut, "jpg");
            coverIn = Path.ChangeExtension(coverIn, "jpg");
            /*
            dbsCoverIn = Path.ChangeExtension(dbsCoverIn, "jpg");
            */
            if (File.Exists(coverIn))
            {
                fileHelper.CopyFile(coverIn, coverOut);
            }
            /*
            else if (File.Exists(dbsCoverIn))
            {
                fileHelper.CopyFile(dbsCoverIn, coverOut);
                globe.projectOptions.dbsCover = true;
            }
            */

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
            if (Logit.loggedError)
            {
                Logit.WriteLine("Skipping postprocessing due to prior errors on this project.");
            }
            else
            {
                List<string> postproclist = globe.projectOptions.postprocesses;
                string command;
                foreach (string proc in postproclist)
                {
                    command = proc.Replace("%d", globe.currentProject);
                    command = command.Replace("%t", globe.projectOptions.translationId);
                    command = command.Replace("%i", globe.projectOptions.fcbhId);
                    command = command.Replace("%e", globe.projectOptions.languageId);
                    command = command.Replace("%h", globe.projectOptions.homeDomain);
                    command = command.Replace("%p", globe.projectOptions.privateProject ? "private" : "public");
                    command = command.Replace("%r", globe.projectOptions.redistributable ? "redistributable" : "restricted");
                    command = command.Replace("%o", globe.projectOptions.downloadsAllowed ? "downloadable" : "onlineonly");
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
        }

        /// <summary>
        /// Create USFM from USFX
        /// </summary>
        private void NormalizeUsfm()
        {
            string logFile;
            try
            {
                
                string UsfmDir = Path.Combine(globe.outputProjectDirectory, "extendedusfm");
                string UsfxName = Path.Combine(Path.Combine(globe.outputProjectDirectory, "usfx"), "usfx.xml");
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
                logFile = Path.Combine(globe.outputProjectDirectory, "usfx2usfm2_log.txt");
                Logit.OpenFile(logFile);
                Logit.GUIWriteString = showMessageString;
                Logit.UpdateStatus = updateConversionProgress;
                SFConverter.scripture = new Scriptures(globe.projectOptions);
                Logit.loggedError = false;
                Logit.loggedWarning = false;
                SFConverter.scripture.USFXtoUSFM(UsfxName, UsfmDir, globe.projectOptions.translationId + ".usfm", true, globe.projectOptions);

                UsfmDir = Path.Combine(globe.outputProjectDirectory, "usfm");
                Utils.DeleteDirectory(UsfmDir);
                fileHelper.EnsureDirectory(UsfmDir);
                currentConversion = "Normalizing USFM from USFX. ";
                SFConverter.scripture.USFXtoUSFM(UsfxName, UsfmDir, globe.projectOptions.translationId + ".usfm", false, globe.projectOptions);
                Logit.CloseFile();
                if (Logit.loggedError)
                {
                    globe.projectOptions.lastRunResult = false;
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
            string usfxDir = Path.Combine(globe.outputProjectDirectory, "usfx");
            string tmpname = Path.Combine(usfxDir, "tempusfx.xml");
            string bookNamesFile = Path.Combine(SourceDir, "BookNames.xml");
            try
            {
                string logFile = Path.Combine(globe.outputProjectDirectory, "UsxConversionReports.txt");
                Logit.OpenFile(logFile);
                Logit.UpdateStatus = updateConversionProgress;
                Logit.GUIWriteString = showMessageString;
                Logit.loggedError = false;
                Logit.loggedWarning = false;
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
                string usfmDir = Path.Combine(globe.outputProjectDirectory, "usfm");
                Utils.DeleteDirectory(usfmDir);
                fileHelper.EnsureDirectory(usfmDir);
                fileHelper.CopyFile(bookNamesFile, Path.Combine(usfmDir, "BookNames.xml"), true);
                SFConverter.scripture = new Scriptures(globe.projectOptions);
                SFConverter.scripture.USFXtoUSFM(usfxName, usfmDir, globe.projectOptions.translationId + ".usfm", false, globe.projectOptions);

                usfmDir = Path.Combine(globe.outputProjectDirectory, "extendedusfm");
                Utils.DeleteDirectory(usfmDir);
                fileHelper.EnsureDirectory(usfmDir);
                fileHelper.CopyFile(bookNamesFile, Path.Combine(usfmDir, "BookNames.xml"), true);
                SFConverter.scripture = new Scriptures(globe.projectOptions);
                SFConverter.scripture.USFXtoUSFM(usfxName, usfmDir, globe.projectOptions.translationId + ".usfm", true, globe.projectOptions);

                // Recreate USFX from USFM, this time with <ve/> tags and in canonical order
                SFConverter.scripture.bkInfo.ReadDefaultBookNames(Path.Combine(globe.outputProjectDirectory, "BookNames.xml"));
                SFConverter.scripture.assumeAllNested = globe.projectOptions.relaxUsfmNesting;
                // Read the input USFM files into internal data structures.
                SFConverter.ProcessFilespec(Path.Combine(usfmDir, "*.usfm"), Encoding.UTF8);
                currentConversion = "converting from USFM to USFX; writing USFX";
                Application.DoEvents();

                // Write out the USFX file.
                SFConverter.scripture.languageCode = globe.projectOptions.languageId;
                SFConverter.scripture.WriteUSFX(usfxName);
                string bookNames = Path.Combine(Path.Combine(globe.outputProjectDirectory, "usfx"), "BookNames.xml");
                SFConverter.scripture.bkInfo.WriteDefaultBookNames(bookNames);
                bool errorState = Logit.loggedError;
                bool runResult = globe.projectOptions.lastRunResult;
                fileHelper.revisePua(usfxName);
                SFConverter.scripture.ReadRefTags(usfxName);
                if (!SFConverter.scripture.ValidateUsfx(usfxName))
                {
                    File.Move(usfxName, Path.ChangeExtension(usfxName, ".bad"));
                    File.Move(Path.ChangeExtension(usfxName, ".norefxml"), usfxName);
                    Logit.WriteLine("Retrying validation on usfx.xml without expanded references.");
                    if (SFConverter.scripture.ValidateUsfx(usfxName))
                    {
                        globe.projectOptions.lastRunResult = runResult;
                        Logit.loggedError = errorState;
                        Logit.WriteLine("Validation passed without expanded references.");
                        globe.projectOptions.makeHotLinks = false;
                    }
                    else
                    {
                        Logit.WriteError("Second validation failed.");
                    }
                }
                Logit.CloseFile();
                if (Logit.loggedError)
                {
                    globe.projectOptions.lastRunResult = false;
                }
                else
                {

                    Utils.DeleteFile(tmpname);
                }
                if (Logit.loggedWarning)
                {
                    globe.projectOptions.warningsFound = true;
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
                
                string UsfmDir = Path.Combine(globe.outputProjectDirectory, "extendedusfm");
                if (!Directory.Exists(SourceDir))
                {
                    MessageBox.Show(this, SourceDir + " not found!", "ERROR");
                    return;
                }
                // Start with an EMPTY USFM directory to avoid problems with old files 
                Utils.DeleteDirectory(UsfmDir);
                fileHelper.EnsureDirectory(UsfmDir);
                string usfxDir = Path.Combine(globe.outputProjectDirectory, "usfx");
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
                                if (fileDate > globe.projectOptions.SourceFileDate)
                                {
                                    globe.sourceDate = fileDate;
                                    globe.projectOptions.SourceFileDate = globe.sourceDate;
                                }

                                logFile = Path.Combine(globe.outputProjectDirectory, "usfx2usfm_log.txt");
                                Logit.OpenFile(logFile);
                                Logit.GUIWriteString = showMessageString;
                                Logit.UpdateStatus = updateConversionProgress;
                                SFConverter.scripture = new Scriptures(globe.projectOptions);
                                Logit.loggedError = false;
                                Logit.loggedWarning = false;
                                currentConversion = "converting from USFX to USFM";
                                Application.DoEvents();
                                SFConverter.scripture.USFXtoUSFM(inputFile, UsfmDir, globe.projectOptions.translationId + ".usfm", true, globe.projectOptions);
                                UsfmDir = Path.Combine(globe.outputProjectDirectory, "usfm");
                                // Start with an EMPTY USFM directory to avoid problems with old files 
                                Utils.DeleteDirectory(UsfmDir);
                                fileHelper.EnsureDirectory(UsfmDir);
                                SFConverter.scripture.USFXtoUSFM(inputFile, UsfmDir, globe.projectOptions.translationId + ".usfm", false, globe.projectOptions);
                                Logit.CloseFile();
                                if (Logit.loggedError)
                                {
                                    globe.projectOptions.lastRunResult = false;
                                }
                                if (Logit.loggedWarning)
                                {
                                    globe.projectOptions.warningsFound = true;
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
            if ((globe.projectOptions.languageId.Length < 3) || (globe.projectOptions.translationId.Length < 3))
                return;
            Usfx2XeTeX toXeTex = new Usfx2XeTeX();
            toXeTex.texDir = xetexDir;
            toXeTex.sqlFileName = string.Empty; // Inhibit re-making SQL file.
            currentConversion = "writing XeTeX";
            string UsfxPath = Path.Combine(globe.outputProjectDirectory, "usfx");
            if (!Directory.Exists(UsfxPath))
            {
                MessageBox.Show(this, UsfxPath + " not found!", "ERROR");
                return;
            }
            Utils.DeleteDirectory(xetexDir);
            Utils.EnsureDirectory(globe.outputDirectory);
            Utils.EnsureDirectory(globe.outputProjectDirectory);
            Utils.EnsureDirectory(xetexDir);
            string logFile = Path.Combine(globe.outputProjectDirectory, "xetexConversionReport.txt");
            Logit.OpenFile(logFile);
            Logit.GUIWriteString = showMessageString;
            Logit.UpdateStatus = updateConversionProgress;

            string fontSource = Path.Combine(globe.dataRootDir, "fonts");
            string fontName = globe.projectOptions.fontFamily.ToLower().Replace(' ', '_');

          
            toXeTex.projectOptions = globe.projectOptions;
            toXeTex.projectOutputDir = globe.outputProjectDirectory;
            toXeTex.redistributable = globe.projectOptions.redistributable;
            toXeTex.epubIdentifier = GetEpubID();
            toXeTex.stripPictures = false;
            toXeTex.indexDate = DateTime.UtcNow;
            toXeTex.indexDateStamp = "PDF generated using Haiola and XeLaTeX on " + toXeTex.indexDate.ToString("d MMM yyyy") +
                " from source files dated " + globe.sourceDate.ToString("d MMM yyyy") + @"\par ";
            toXeTex.GeneratingConcordance = false;
            toXeTex.CrossRefToFilePrefixMap = globe.projectOptions.CrossRefToFilePrefixMap;
            toXeTex.contentCreator = globe.projectOptions.contentCreator;
            toXeTex.contributor = globe.projectOptions.contributor;
            string usfxFilePath = Path.Combine(UsfxPath, "usfx.xml");
            toXeTex.bookInfo.ReadPublicationOrder(orderFile);
            toXeTex.MergeXref(Path.Combine(globe.inputProjectDirectory, "xref.xml"));
            //TODO: eliminate side effects in expandPercentEscapes
            // Side effect: expandPercentEscapes sets longCopyrightMessage and shortCopyrightMessage.
            toXeTex.sourceLink = expandPercentEscapes("<a href=\"http://%h/%t\">%v</a>");
            toXeTex.longCopr = longCopyrightMessage;
            toXeTex.shortCopr = shortCopyrightMessage;
            toXeTex.textDirection = globe.projectOptions.textDir;
            toXeTex.stripManualNoteOrigins = globe.projectOptions.stripNoteOrigin;
            toXeTex.noteOriginFormat = globe.projectOptions.xoFormat;
            toXeTex.englishDescription = globe.projectOptions.EnglishDescription;
            toXeTex.preferredFont = globe.projectOptions.fontFamily;
            toXeTex.fcbhId = globe.projectOptions.fcbhId;
            toXeTex.coverName = Path.GetFileName(preferredCover);
            if (globe.projectOptions.PrepublicationChecks &&
                (globe.projectOptions.publicDomain || globe.projectOptions.redistributable || File.Exists(Path.Combine(globe.inputProjectDirectory, "certify.txt"))) &&
                File.Exists(certified))
            {
                File.Copy(certified, Path.Combine(xetexDir, "eBible.org_certified.jpg"));
                // toXeTex.indexDateStamp = toXeTex.indexDateStamp + "<br /><a href='http://eBible.org/certified/' target='_blank'><img src='eBible.org_certified.jpg' alt='eBible.org certified' /></a>";
            }
            toXeTex.xrefCall.SetMarkers(globe.projectOptions.xrefCallers);
            toXeTex.footNoteCall.SetMarkers(globe.projectOptions.footNoteCallers);
            toXeTex.inputDir = globe.inputDirectory;
            toXeTex.projectInputDir = globe.inputProjectDirectory;
            toXeTex.ConvertUsfxToHtml(usfxFilePath, xetexDir,
                globe.projectOptions.vernacularTitle,
                globe.projectOptions.languageId,
                globe.projectOptions.translationId,
                globe.projectOptions.chapterLabel,
                globe.projectOptions.psalmLabel,
                shortCopyrightMessage,
                expandPercentEscapes(globe.projectOptions.homeLink),
                expandPercentEscapes(globe.projectOptions.footerHtml),
                expandPercentEscapes(globe.projectOptions.indexHtml),
                copyrightPermissionsStatement(),
                globe.projectOptions.ignoreExtras,
                globe.projectOptions.goText);
            toXeTex.bookInfo.RecordStats(globe.projectOptions);
            globe.projectOptions.commonChars = toXeTex.commonChars;
            globe.projectOptions.Write();
            LoadStatisticsTab();
            Logit.CloseFile();
            if (Logit.loggedError)
            {
                globe.projectOptions.lastRunResult = false;
            }
            if (Logit.loggedWarning)
            {
                globe.projectOptions.warningsFound = true;
            }
        }

        /// <summary>
        /// Convert USFX to Modified OSIS
        /// </summary>
        private void ConvertUsfxToMosis()
        {
            currentConversion = "writing MOSIS";
            if ((globe.projectOptions.languageId.Length < 3) || (globe.projectOptions.translationId.Length < 3))
                return;
            string UsfxPath = Path.Combine(globe.outputProjectDirectory, "usfx");
            string mosisPath = Path.Combine(globe.outputProjectDirectory, "mosis");
            if (!Directory.Exists(UsfxPath))
            {
                MessageBox.Show(this, UsfxPath + " not found!", "ERROR");
                return;
            }
            string usfxFilePath = Path.Combine(UsfxPath, "usfx.xml");
            string mosisFilePath = Path.Combine(mosisPath, globe.projectOptions.translationId + "_osis.xml");

            Utils.EnsureDirectory(globe.outputDirectory);
            Utils.EnsureDirectory(globe.outputProjectDirectory);
            Utils.EnsureDirectory(mosisPath);

            usfxToMosisConverter toMosis = new usfxToMosisConverter();
            if (globe.projectOptions.redistributable && File.Exists(eBibleCertified))
            {
                globe.projectOptions.textSourceUrl = "https://eBible.org/Scriptures/";
            }
            else
            {
                globe.projectOptions.textSourceUrl = "";
            }
            toMosis.languageCode = globe.projectOptions.languageId;
            toMosis.translationId = globe.projectOptions.translationId;
            toMosis.revisionDateTime = globe.projectOptions.contentUpdateDate;
            toMosis.vernacularTitle = globe.projectOptions.vernacularTitle;
            toMosis.contentCreator = globe.projectOptions.contentCreator;
            toMosis.contentContributor = globe.projectOptions.contributor;
            toMosis.englishDescription = globe.projectOptions.EnglishDescription;
            toMosis.lwcDescription = globe.projectOptions.lwcDescription;
            toMosis.printPublisher = globe.projectOptions.printPublisher;
            toMosis.ePublisher = globe.projectOptions.electronicPublisher;
            toMosis.languageName = globe.projectOptions.languageNameInEnglish;
            toMosis.dialect = globe.projectOptions.dialect;
            toMosis.vernacularLanguageName = globe.projectOptions.languageName;
            toMosis.projectOptions = globe.projectOptions;
            toMosis.swordDir = Path.Combine(globe.dataRootDir, "sword");
            toMosis.swordRestricted = Path.Combine(globe.dataRootDir, "swordRestricted");
            toMosis.copyrightNotice = globe.projectOptions.publicDomain ? "public domain" : "Copyright © " + globe.projectOptions.copyrightYears + " " + globe.projectOptions.copyrightOwner;
            if (globe.projectOptions.publicDomain)
            {
                toMosis.rightsNotice = @"This work is in the Public Domain. That means that it is not copyrighted.
 It is still subject to God's Law concerning His Word, including the Great Commission (Matthew 28:18-20).
";
            }
            else if (globe.projectOptions.ccbyndnc)
            {
                toMosis.rightsNotice = @"This Bible translation is made available to you under the terms of the
 Creative Commons Attribution-Noncommercial-No Derivative Works license (http://creativecommons.org/licenses/by-nc-nd/4.0/).";
            }
            else if (globe.projectOptions.ccbysa)
            {
                toMosis.rightsNotice = @"This Bible translation is made available to you under the terms of the
 Creative Commons Attribution-Share-Alike license (http://creativecommons.org/licenses/by-sa/4.0/).";
            }
            else if (globe.projectOptions.ccbynd)
            {
                toMosis.rightsNotice = @"This Bible translation is made available to you under the terms of the
 Creative Commons Attribution-No Derivatives license (http://creativecommons.org/licenses/by-na/4.0/).";
            }
            else
            {
                toMosis.rightsNotice = String.Empty;
            }
            if (globe.projectOptions.rightsStatement.Length > 0)
            {
                toMosis.rightsNotice += globe.projectOptions.rightsStatement;
            }
            toMosis.infoPage = copyrightPermissionsStatement();
            string logFile = Path.Combine(globe.outputProjectDirectory, "MosisConversionReport.txt");
            Logit.OpenFile(logFile);
            Logit.GUIWriteString = showMessageString;
            toMosis.langCodes = languageCodes;
            toMosis.ConvertUsfxToMosis(usfxFilePath, mosisFilePath);
            Logit.CloseFile();
            if (Logit.loggedError)
            {
                globe.projectOptions.lastRunResult = false;
            }
            if (Logit.loggedWarning)
            {
                globe.projectOptions.warningsFound = true;
            }
        }

        private void PrepareSearchText()
        {
            string logFile = Path.Combine(globe.outputProjectDirectory, "SearchReport.txt");
            Logit.OpenFile(logFile);
            try
            {
                ExtractSearchText est = new ExtractSearchText();
                string vplPath = Path.Combine(globe.outputProjectDirectory, "vpl");
                string UsfxPath = Path.Combine(globe.outputProjectDirectory, "usfx");
                string auxPath = Path.Combine(globe.outputProjectDirectory, "search");
                string verseText = Path.Combine(auxPath, "verseText.xml");
                // string sqlFile = Path.Combine(globe.m_outputProjectDirectory, "MySQL");
                string sqlFile = Path.Combine(globe.outputProjectDirectory, "sql");
                Logit.GUIWriteString = showMessageString;
                Logit.UpdateStatus = updateConversionProgress;
                Utils.EnsureDirectory(auxPath);
                Utils.EnsureDirectory(sqlFile);
                est.Filter(Path.Combine(UsfxPath, "usfx.xml"), verseText);
                est.WriteSearchSql(verseText, globe.currentProject, Path.Combine(sqlFile, globe.currentProject + "_vpl.sql"));
                est.WriteSearchSql(Path.ChangeExtension(verseText, ".lemma"), globe.currentProject, Path.Combine(sqlFile, globe.currentProject + "_lemma.sql"));
                if (est.LongestWordLength > globe.projectOptions.longestWordLength)
                    globe.projectOptions.longestWordLength = est.LongestWordLength;
                // Copy search text files to VPL output.
                Utils.DeleteDirectory(vplPath);
                Utils.EnsureDirectory(vplPath);
                File.Copy(verseText, Path.Combine(vplPath, globe.currentProject + "_vpl.xml"));
                File.Copy(Path.Combine(auxPath, "verseText.vpltxt"), Path.Combine(vplPath, globe.currentProject + "_vpl.txt"));
                File.Copy(Path.Combine(globe.inputDirectory, "haiola.css"), Path.Combine(vplPath, "haiola.css"));
                StreamWriter htm = new StreamWriter(Path.Combine(vplPath, globe.currentProject + "_about.htm"));
                htm.WriteLine("<!DOCTYPE html>");
                htm.WriteLine("<html>");
                htm.WriteLine("<head>");
                htm.WriteLine("<meta charset=\"UTF-8\" />");
                htm.WriteLine("<link rel=\"stylesheet\" href=\"haiola.css\" type=\"text/css\" />");
                htm.WriteLine("<meta name=\"viewport\" content=\"user-scalable=yes, initial-scale=1, minimum-scale=1, width=device-width\"/>");
                htm.WriteLine("<title>About {0}_vpl</title>", globe.currentProject);
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
                    globe.projectOptions.lastRunResult = false;
                }
                if (Logit.loggedWarning)
                {
                    globe.projectOptions.warningsFound = true;
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
            SetcurrentProject(projDirName);
        	globe.projectXiniPath = Path.Combine(globe.inputProjectDirectory, "options.xini");
            displayOptions();
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

            Utils.DeleteDirectory(Path.Combine(globe.outputProjectDirectory, "search"));
            Utils.DeleteDirectory(Path.Combine(globe.outputProjectDirectory, "usfm1"));
            Utils.DeleteDirectory(Path.Combine(globe.outputProjectDirectory, "sfm"));
            Utils.DeleteDirectory(Path.Combine(globe.outputProjectDirectory, "extendedusfm"));
            Utils.DeleteDirectory(Path.Combine(globe.outputProjectDirectory, "usfm"));
            Utils.DeleteDirectory(Path.Combine(globe.outputProjectDirectory, "usfx"));


            if (globe.projectOptions.PrepublicationChecks &&
                (globe.projectOptions.publicDomain || globe.projectOptions.redistributable || File.Exists(Path.Combine(globe.inputProjectDirectory, "certify.txt"))) &&
                File.Exists(eBibleCertified))
            {
                certified = eBibleCertified;
                globe.projectOptions.eBibleCertified = true;
            }
            else
            {
                certified = null;
                globe.projectOptions.eBibleCertified = false;
            }
            globe.projectOptions.rebuild = RebuildCheckBox.Checked;
            globe.projectOptions.Write();

            // Find out what kind of input we have (USFX, USFM, or USX)
            // and produce USFX, USFM, (and in the future) USX outputs.

            orderFile = Path.Combine(globe.inputProjectDirectory, "bookorder.txt");
            if (!File.Exists(orderFile))
                orderFile = SFConverter.FindAuxFile("bookorder.txt");
            StreamReader sr = new StreamReader(orderFile);
            globe.projectOptions.allowedBookList = sr.ReadToEnd();
            sr.Close();



            if (!GetUsfx(projDirName))
            {
                Logit.WriteError("No source directory found for " + projDirName + "!");
                fileHelper.unlockProject();
                return;
            }
            Utils.DeleteDirectory(Path.Combine(globe.outputProjectDirectory, "sql"));

            UpdateBooksList();
            Application.DoEvents();
            preferredCover = CreateCover();
            Application.DoEvents();
            // Create verseText.xml with unformatted canonical text only in verse containers.
            if (fileHelper.fAllRunning)
                PrepareSearchText();
            Application.DoEvents();
            // Create epub file
            string epubDir = Path.Combine(globe.outputProjectDirectory, "epub");
            if (fileHelper.fAllRunning && globe.projectOptions.makeEub && (globe.projectOptions.rebuild || globe.projectOptions.SourceFileDate > Directory.GetCreationTime(epubDir)))
            {
                Utils.DeleteDirectory(epubDir);
                ConvertUsfxToEPub();
            }
            Application.DoEvents();
            // Create HTML output for posting on web sites.
            string htmlDir = Path.Combine(globe.outputProjectDirectory, "html");
            if (fileHelper.fAllRunning && globe.projectOptions.makeHtml && (globe.projectOptions.rebuild || globe.projectOptions.SourceFileDate > Directory.GetCreationTime(htmlDir)))
            {
                Utils.DeleteDirectory(htmlDir);
                ConvertUsfxToPortableHtml();
            }
            Application.DoEvents();
            string WordMLDir = Path.Combine(globe.outputProjectDirectory, "WordML");
            if (fileHelper.fAllRunning && globe.projectOptions.makeWordML && (globe.projectOptions.rebuild || globe.projectOptions.SourceFileDate > Directory.GetCreationTime(WordMLDir)))
            {   // Write out WordML document
                // Note: this conversion departs from the standard architecture of making the USFX file the hub, because the WordML writer code was already done in WordSend,
                // and expected USFM input. Therefore, we read the normalized USFM files, which should be present even if the project input is USFX or USX.
                // If this code needs much maintenance in the future, it may be better to refactor the WordML output to go from USFX to WordML directly.
                // Then again, USFX to Open Document Text would be better.
                try
                {
                    Utils.DeleteDirectory(WordMLDir);
                    currentConversion = "Reading normalized USFM";
                    string logFile = Path.Combine(globe.outputProjectDirectory, "WordMLConversionReport.txt");
                    Logit.OpenFile(logFile);
                    SFConverter.scripture = new Scriptures(globe.projectOptions);
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
                    currentConversion = "Writing WordML";
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
            // Create Modified OSIS output for conversion to Sword format.
            string mosisDir = Path.Combine(globe.outputProjectDirectory, "mosis");
            if (fileHelper.fAllRunning && globe.projectOptions.makeSword && (globe.projectOptions.rebuild || (globe.projectOptions.SourceFileDate > globe.projectOptions.SwordVersionDate)))
            {
                Utils.DeleteDirectory(mosisDir);
                ConvertUsfxToMosis();
            }
            Application.DoEvents();
            string xetexDir = Path.Combine(globe.outputProjectDirectory, "xetex");
            if (fileHelper.fAllRunning && globe.projectOptions.makePDF /* && (globe.m_options.rebuild || (globe.m_options.SourceFileDate > Directory.GetCreationTime(xetexDir)))*/)
            {
                ConvertUsfxToPDF(xetexDir);
            }
            Application.DoEvents();
            // Run proprietary extension conversions, if any.
            string inscriptDir = Path.Combine(Path.Combine(globe.dataRootDir, "inscript"), globe.projectOptions.fcbhId);
            DateTime inscriptCreated = Directory.GetCreationTime(inscriptDir);
            if (fileHelper.fAllRunning && globe.projectOptions.makeInScript && (globe.projectOptions.rebuild || globe.projectOptions.SourceFileDate > inscriptCreated))
            {
                Utils.DeleteDirectory(inscriptDir);
                plugin.DoProprietaryConversions();
            }
            Application.DoEvents();
            // Run custom per project scripts.
            if (fileHelper.fAllRunning)
            {
                DoPostprocess();
                globe.projectOptions.done = true;
                globe.projectOptions.Write();
            }
            fileHelper.unlockProject();
            Application.DoEvents();
        }

    	private void SetcurrentProject(string projDirName)
    	{
    		globe.currentProject = projDirName;
    		globe.inputProjectDirectory = Path.Combine(globe.inputDirectory, globe.currentProject);
    		globe.outputProjectDirectory = Path.Combine(globe.outputDirectory, globe.currentProject);
    		fileHelper.EnsureDirectory(globe.outputProjectDirectory);
    	}

        private string FindSource(string projDirName)
        {
            SetcurrentProject(projDirName);
            string source;
            string result = string.Empty;
            if (!String.IsNullOrEmpty((string)paratext8ComboBox.SelectedItem))
            {
                source = Path.Combine(globe.paratext8ProjectsDir, (string)paratext8ComboBox.SelectedItem);
                if (Directory.Exists(source))
                {
                    return source;
                }
            }
            if (!String.IsNullOrEmpty((string)paratextcomboBox.SelectedItem))
            {
                source = Path.Combine(globe.paratextProjectsDir, (string)paratextcomboBox.SelectedItem);
                if (Directory.Exists(source))
                {
                    return source;
                }
            }
            source = Path.Combine(globe.inputProjectDirectory, "Source");
            if (Directory.Exists(source))
            {
                return source;
            }
            else
            {
                source = Path.Combine(globe.inputProjectDirectory, "usfx");
                if (Directory.Exists(source))
                {
                    return source;
                }
                else
                {
                    source = Path.Combine(globe.inputProjectDirectory, "usx");
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
			SetcurrentProject(projDirName);
			string source;
            if (!String.IsNullOrEmpty((string)paratext8ComboBox.SelectedItem))
            {
                source = Path.Combine(globe.paratext8ProjectsDir, (string)paratext8ComboBox.SelectedItem);
                if (Directory.Exists(source))
                {
                    globe.PreprocessUsfmFiles(source);
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
                    Logit.WriteError("Paratext 8 project directory " + source + " not found!");
                }
            }
            if (!String.IsNullOrEmpty((string)paratextcomboBox.SelectedItem))
            {
                source = Path.Combine(globe.paratextProjectsDir, (string)paratextcomboBox.SelectedItem);
                if (Directory.Exists(source))
                {
                    globe.PreprocessUsfmFiles(source);
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
            source = Path.Combine(globe.inputProjectDirectory, "Source");
            if (Directory.Exists(source))
            {
                globe.PreprocessUsfmFiles(source);
                Application.DoEvents();
                if (fileHelper.fAllRunning)
                {
                    ConvertUsfmToUsfx();
                    NormalizeUsfm();
                }
            }
            else
            {
                source = Path.Combine(globe.inputProjectDirectory, "usfx");
                if (Directory.Exists(source))
                {
                    ImportUsfx(source);
                    NormalizeUsfm();
                }
                else
                {
                    source = Path.Combine(globe.inputProjectDirectory, "usx");
                    if (Directory.Exists(source))
                    {
                        ImportUsx(source);
                        string metadataXml = Path.Combine(source, "metadata.xml");
                        if (File.Exists(metadataXml))
                        {
                            DateTime fileDate = File.GetLastWriteTimeUtc(metadataXml);
                            if (fileDate > globe.sourceDate)
                            {
                                globe.sourceDate = fileDate;
                                globe.projectOptions.SourceFileDate = globe.sourceDate;
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
            messagesListBox.BackColor = Color.LightGreen;
            tabControl1.SelectedTab = messagesTabPage;
            BackColor = Color.LightGreen;
            startTime = DateTime.UtcNow;
            WorkOnAllButton.Text = "Stop";
            Application.DoEvents();
            timer1.Enabled = true;
            //SaveOptions();


            fcbhIds = null;
            if (globe.getFCBHkeys)
                GetFcbhIds();


            foreach (object o in m_projectsList.CheckedItems)
            {
                SetcurrentProject((string)o);
                globe.projectXiniPath = Path.Combine(globe.inputProjectDirectory, "options.xini");
                displayOptions();
                if (!globe.projectOptions.done)
                {
                    insertProjectEntry(new projectEntry((string)o, globe.projectOptions.dependsOn));
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
                string index = Path.Combine(Path.Combine(globe.outputProjectDirectory, "html"), "index.htm");
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
                    SetcurrentProject((string)o);
                    globe.projectXiniPath = Path.Combine(globe.inputProjectDirectory, "options.xini");
                    displayOptions();
                    lockFile = Path.Combine(globe.inputProjectDirectory, "lock");
                    Utils.DeleteFile(lockFile);
                    globe.projectOptions.done = false;
                    globe.projectOptions.Write();
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
                                            globe.projectOptions.canonTypeEnglish = MetadataText();
                                        break;
                                    case "contents/bookList/descriptionLocal":
                                        if (defaultBookList)
                                            globe.projectOptions.canonTypeLocal = MetadataText();
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
        /// Read options from the correct .xini file and display them.
        /// </summary>
        private void displayOptions()
        {
            if (globe.projectOptions == null)
            {
                globe.projectOptions = new Options(globe.projectXiniPath);
            }
            else
            {
                globe.projectOptions.Reload(globe.projectXiniPath);
            }
            if (globe.projectOptions.languageId.Length == 3)
            {
                ethnorecord er = eth.ReadEthnologue(globe.projectOptions.languageId);
                if (globe.projectOptions.country.Length == 0)
                    globe.projectOptions.country = er.countryName;
                if (globe.projectOptions.countryCode.Length == 0)
                    globe.projectOptions.countryCode = er.countryId;
                if (globe.projectOptions.languageNameInEnglish.Length == 0)
                    globe.projectOptions.languageNameInEnglish = er.langName;
            }
            SFConverter.jobIni = globe.projectOptions.ini;
            ethnologueCodeTextBox.Text = globe.projectOptions.languageId;
            translationIdTextBox.Text = globe.currentProject; // This was globe.m_options.translationId, but now we force short translation ID and input directory name to match.
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
            ccRadioButton.Checked = globe.projectOptions.ccbyndnc;
            CCBySaRadioButton.Checked = globe.projectOptions.ccbysa;
            CCByNdRadioButton.Checked = globe.projectOptions.ccbynd;
            otherRadioButton.Checked = globe.projectOptions.otherLicense;
            allRightsRadioButton.Checked = globe.projectOptions.allRightsReserved;
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
            paratextcomboBox.SelectedItem = globe.projectOptions.paratextProject;
            paratext8ComboBox.SelectedItem = globe.projectOptions.paratext8Project;
            audioRecordingCopyrightTextBox.Text = globe.projectOptions.AudioCopyrightNotice;
            rodCodeTextBox.Text = globe.projectOptions.rodCode;
            ldmlTextBox.Text = globe.projectOptions.ldml;
            scriptTextBox.Text = globe.projectOptions.script;
            localRightsHolderTextBox.Text = globe.projectOptions.localRightsHolder;
            facebookTextBox.Text = globe.projectOptions.facebook;
            countryTextBox.Text = globe.projectOptions.country;
            countryCodeTextBox.Text = globe.projectOptions.countryCode;
            extendUsfmCheckBox.Checked = globe.projectOptions.extendUsfm;
            chapterLabelTextBox.Text = globe.projectOptions.chapterLabel;
            psalmLabelTextBox.Text = globe.projectOptions.psalmLabel;
            cropCheckBox.Checked = globe.projectOptions.includeCropMarks;
            chapter1CheckBox.Checked = globe.projectOptions.chapter1;
            verse1CheckBox.Checked = globe.projectOptions.verse1;
            pageWidthTextBox.Text = globe.projectOptions.pageWidth;
            pageLengthTextBox.Text = globe.projectOptions.pageLength;
            regenerateNoteOriginsCheckBox.Checked = globe.projectOptions.RegenerateNoteOrigins;
            cvSeparatorTextBox.Text = globe.projectOptions.CVSeparator;
            downloadsAllowedCheckBox.Checked = globe.projectOptions.downloadsAllowed;
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
            RebuildCheckBox.Checked = globe.projectOptions.rebuild = globe.xini.ReadBool("rebuild", false);
            runXetexCheckBox.Checked = globe.projectOptions.rebuild = globe.xini.ReadBool("runXetex", false);
            makeInScriptCheckBox.Checked = globe.projectOptions.makeInScript;
            makeEPubCheckBox.Checked = globe.projectOptions.makeEub;
            makeHtmlCheckBox.Checked = globe.projectOptions.makeHtml;
            makePDFCheckBox.Checked = globe.projectOptions.makePDF;
            makeSwordCheckBox.Checked = globe.projectOptions.makeSword;
            makeWordMLCheckBox.Checked = globe.projectOptions.makeWordML;
            disablePrintingFigoriginsCheckBox.Checked = globe.projectOptions.disablePrintingFigOrigins;
            apocryphaCheckBox.Checked = globe.projectOptions.includeApocrypha;
            if (globe.projectOptions.DBSandeBible)
                recheckedCheckBox.Visible = true;
            recheckedCheckBox.Checked = globe.projectOptions.rechecked;

            /*
            if ((globe.projectOptions.fcbhId == String.Empty) && (fcbhDbsIds != null))
            {
                globe.projectOptions.fcbhId = (string)fcbhDbsIds[globe.currentProject];
                if (globe.projectOptions.fcbhId == null)
                    globe.projectOptions.fcbhId = String.Empty;
            }
            */
            fcbhIdTextBox.Text = globe.projectOptions.fcbhId;
            shortTitleTextBox.Text = globe.projectOptions.shortTitle;
            if (shortTitleTextBox.Text.Length < 1)
                shortTitleTextBox.Text = globe.projectOptions.EnglishDescription;
                        
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

        	LoadConcTab();
			LoadBooksTab();
        	LoadFramesTab();
            LoadStatisticsTab();
            SetCopyrightStrings();

            if (ReadMetadata(Path.Combine(Path.Combine(globe.inputProjectDirectory, "usx"), "metadata.xml")))
                SaveOptions();
            string src = FindSource(globe.currentProject);
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
                                globe.projectOptions.otBookCount, globe.projectOptions.otChapCount, globe.projectOptions.otVerseCount, globe.projectOptions.otVerseMax,
                                globe.projectOptions.ntBookCount, globe.projectOptions.ntChapCount, globe.projectOptions.ntVerseCount, globe.projectOptions.ntVerseMax,
                                globe.projectOptions.adBookCount, globe.projectOptions.adChapCount, globe.projectOptions.adVerseCount, globe.projectOptions.adVerseMax,
                                globe.projectOptions.pBookCount,
                                globe.projectOptions.fcbhDramaOT, globe.projectOptions.fcbhDramaNT, globe.projectOptions.fcbhAudioOT, globe.projectOptions.fcbhAudioNT, globe.projectOptions.fcbhAudioPortion);
        }

		private void LoadConcTab()
		{
            concordanceRadioButton.Checked = globe.projectOptions.GenerateConcordance;
            mobileHtmlRadioButton.Checked = globe.projectOptions.GenerateMobileHtml;
            legacyHtmlRadioButton.Checked = globe.projectOptions.LegacyHtml;
            chkMergeCase.Checked = globe.projectOptions.MergeCase;
			tbxWordformingChars.Text = globe.projectOptions.WordformingChars;
			tbxExcludeWords.Text = globe.projectOptions.ExcludeWords;
			tbxMaxFreq.Text = globe.projectOptions.MaxFreqSrc;
			tbxPhrases.Text = globe.projectOptions.PhrasesSrc;
			tbxMinContext.Text = globe.projectOptions.MinContextLength.ToString();
			tbxMaxContext.Text = globe.projectOptions.MaxContextLength.ToString();

		}

		private void SaveConcTab()
		{
			globe.projectOptions.GenerateConcordance = concordanceRadioButton.Checked;
            globe.projectOptions.GenerateMobileHtml = mobileHtmlRadioButton.Checked;
            globe.projectOptions.LegacyHtml = legacyHtmlRadioButton.Checked;
            globe.projectOptions.MergeCase = chkMergeCase.Checked;
			globe.projectOptions.WordformingChars = tbxWordformingChars.Text;
			globe.projectOptions.ExcludeWords = tbxExcludeWords.Text;
			globe.projectOptions.MaxFreqSrc = tbxMaxFreq.Text; // Enhance: validate
			globe.projectOptions.PhrasesSrc = tbxPhrases.Text;
			int temp;
			if (int.TryParse(tbxMinContext.Text, out temp))
				globe.projectOptions.MinContextLength = temp;
			if (int.TryParse(tbxMaxContext.Text, out temp))
				globe.projectOptions.MaxContextLength = temp;
		}

		private void LoadBooksTab()
		{
			listBooks.BeginUpdate();
			listBooks.Items.Clear();
			Dictionary<string, string> idsToCrossRefs = new Dictionary<string, string>();
			foreach (var kvp in globe.projectOptions.CrossRefToFilePrefixMap)
				idsToCrossRefs[kvp.Value] = kvp.Key;
			foreach (var key in globe.projectOptions.Books)
			{
				string vernAbbr;
				if (!globe.projectOptions.ReferenceAbbeviationsMap.TryGetValue(key, out vernAbbr))
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
                    MessageBox.Show("Duplicate book name: " + crossRefName + " @ " + key, "Error in " + globe.projectOptions.translationId, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    crossRefsToIds.Add(crossRefName, key);
                }
			}
			globe.projectOptions.Books = books;
			globe.projectOptions.ReferenceAbbeviationsMap = idsToVernAbbrs;
			globe.projectOptions.CrossRefToFilePrefixMap = crossRefsToIds;
		}

		private void SaveFramesTab()
		{
            globe.projectOptions.UseFrames = framedConcordanceRadioButton.Checked;
			globe.projectOptions.ConcordanceLinkText = concordanceLinkTextBox.Text;
			globe.projectOptions.BooksAndChaptersLinkText = booksAndChaptersLinkTextBox.Text;
			globe.projectOptions.IntroductionLinkText = introductionLinkTextBox.Text;
			globe.projectOptions.PreviousChapterLinkText = previousChapterLinkTextBox.Text;
			globe.projectOptions.NextChapterLinkText = nextChapterLinkTextBox.Text;
			globe.projectOptions.HideNavigationButtonText = hideNavigationPanesTextBox.Text;
			globe.projectOptions.ShowNavigationButtonText = showNavigationTextBox.Text;
		}

		private void LoadFramesTab()
		{
            framedConcordanceRadioButton.Checked = globe.projectOptions.UseFrames;
			concordanceLinkTextBox.Text = globe.projectOptions.ConcordanceLinkText;
			booksAndChaptersLinkTextBox.Text = globe.projectOptions.BooksAndChaptersLinkText;
			introductionLinkTextBox.Text = globe.projectOptions.IntroductionLinkText;
			previousChapterLinkTextBox.Text = globe.projectOptions.PreviousChapterLinkText;
			nextChapterLinkTextBox.Text = globe.projectOptions.NextChapterLinkText;
			hideNavigationPanesTextBox.Text = globe.projectOptions.HideNavigationButtonText;
			showNavigationTextBox.Text = globe.projectOptions.ShowNavigationButtonText;
		}

        private void m_projectsList_SelectedIndexChanged(object sender, EventArgs e)
        {
            globe.currentProject = m_projectsList.SelectedItem.ToString();
            globe.inputProjectDirectory = Path.Combine(globe.inputDirectory, globe.currentProject);
            globe.outputProjectDirectory = Path.Combine(globe.outputDirectory, globe.currentProject);
            globe.projectXiniPath = Path.Combine(globe.inputProjectDirectory, "options.xini");
            displayOptions();
        }

        public void SaveOptions()
        {
            if (globe.projectOptions == null)
                return;
            globe.projectOptions.languageId = ethnologueCodeTextBox.Text;
            globe.projectOptions.translationId = translationIdTextBox.Text;
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
            globe.projectOptions.ccbyndnc = ccRadioButton.Checked;
            globe.projectOptions.ccbysa = CCBySaRadioButton.Checked;
            globe.projectOptions.ccbynd = CCByNdRadioButton.Checked;
            globe.projectOptions.otherLicense = otherRadioButton.Checked;
            globe.projectOptions.allRightsReserved = allRightsRadioButton.Checked;
            globe.projectOptions.silentCopyright = silentRadioButton.Checked;
            globe.projectOptions.copyrightOwner = copyrightOwnerTextBox.Text.Trim();
            copyrightOwnerUrlTextBox.Text = copyrightOwnerUrlTextBox.Text.Trim();
            if ((copyrightOwnerUrlTextBox.Text.Length > 1) && !copyrightOwnerUrlTextBox.Text.ToLowerInvariant().StartsWith("http://"))
                copyrightOwnerUrlTextBox.Text = "http://" + copyrightOwnerUrlTextBox.Text;
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
            globe.projectOptions.paratextProject = (string)paratextcomboBox.SelectedItem;
            globe.projectOptions.paratext8Project = (string)paratext8ComboBox.SelectedItem;
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
            globe.projectOptions.shortTitle = shortTitleTextBox.Text;
            globe.projectOptions.footNoteCallers = footNoteCallersTextBox.Text;
            globe.projectOptions.xrefCallers = crossreferenceCallersTextBox.Text;
            globe.projectOptions.commentText = commentTextBox.Text;
            globe.projectOptions.redistributable = redistributableCheckBox.Checked & !globe.projectOptions.privateProject;
            globe.projectOptions.downloadsAllowed = downloadsAllowedCheckBox.Checked & !globe.projectOptions.privateProject;
            globe.projectOptions.customPermissions = customPermissionsCheckBox.Checked;
            globe.projectOptions.chapterLabel = chapterLabelTextBox.Text;
            globe.projectOptions.psalmLabel = psalmLabelTextBox.Text;
            globe.projectOptions.includeCropMarks = cropCheckBox.Checked;
            globe.projectOptions.chapter1 = chapter1CheckBox.Checked;
            globe.projectOptions.verse1 = verse1CheckBox.Checked;
            globe.projectOptions.pageWidth = pageWidthTextBox.Text;
            globe.projectOptions.pageLength = pageLengthTextBox.Text;
            globe.projectOptions.RegenerateNoteOrigins = regenerateNoteOriginsCheckBox.Checked;
            globe.projectOptions.CVSeparator = cvSeparatorTextBox.Text;
            globe.projectOptions.SwordName = swordNameTextBox.Text;
            globe.projectOptions.ObsoleteSwordName = oldSwordIdTextBox.Text;
            globe.projectOptions.rebuild = RebuildCheckBox.Checked;
            globe.xini.WriteBool("rebuild", globe.projectOptions.rebuild);
            globe.projectOptions.makeInScript = makeInScriptCheckBox.Checked;
            globe.projectOptions.makeEub = makeEPubCheckBox.Checked;
            globe.projectOptions.makeHtml = makeHtmlCheckBox.Checked;
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
            globe.m_options.altLinks = alternateLinks;
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
            globe.projectOptions.fontFamily = fontComboBox.Text;
            if (dependsComboBox.SelectedItem != null)
                globe.projectOptions.dependsOn = dependsComboBox.Text;

			SaveConcTab();
			SaveBooksTab();
        	SaveFramesTab();

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
                runtime = (DateTime.UtcNow - startTime).ToString("g") + " " + globe.currentProject + " ";
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
                tabControl1.SelectedTab = messagesTabPage;
                startTime = DateTime.UtcNow;
                BackColor = Color.LightGreen;
                Application.DoEvents();
                timer1.Enabled = true;
                WorkOnAllButton.Text = "Stop";
                globe.projectOptions.done = false;
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
                string indexhtm = Path.Combine(Path.Combine(globe.outputProjectDirectory, "html"), "index.htm");
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
        /// Find FCBH ID(s) for the currently-displayed globe.m_options record
        /// </summary>
        protected void MatchFcbhIds()
        {
            if ((!globe.getFCBHkeys) || (fcbhIds == null))
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
                if (fcbh.language_iso == globe.projectOptions.languageId)
                {
                    localFcbhId = globe.projectOptions.fcbhId;
                    if (localFcbhId.Length < 6)
                        localFcbhId = "@@@@@@"; // No match, but don't choke Substring().
                    localFcbhId = localFcbhId.Substring(0, 6);
                    if ((fcbh.version_code == globe.projectOptions.translationTraditionalAbbreviation) ||
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
            globe.projectOptions.fcbhAudioNT = ntAudio;
            globe.projectOptions.fcbhDramaNT = ntDrama;
            globe.projectOptions.fcbhAudioOT = otAudio;
            globe.projectOptions.fcbhDramaOT = otDrama;
            globe.projectOptions.fcbhAudioPortion = portion;
            globe.projectOptions.Write();
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


        private void helpButton_Click(object sender, EventArgs e)
        {
            showHelp("haiola.htm");
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
            globe.currentTemplate = globe.currentProject;
            templateLabel.Text = "Current template: " + globe.currentTemplate;
            copyFromTemplateButton.Enabled = false;
            globe.xini.Write();
        }

        private void copyFromTemplateButton_Click(object sender, EventArgs e)
        {
            Options templateOptions = new Options(Path.Combine(Path.Combine(globe.inputDirectory, globe.currentTemplate), "options.xini"));
            homeLinkTextBox.Text = globe.projectOptions.homeLink = templateOptions.homeLink;
            goTextTextBox.Text = globe.projectOptions.goText = templateOptions.goText;
            footerHtmlTextBox.Text = globe.projectOptions.footerHtml = templateOptions.footerHtml;
            indexPageTextBox.Text = globe.projectOptions.indexHtml = templateOptions.indexHtml;
            licenseTextBox.Text = globe.projectOptions.licenseHtml = templateOptions.licenseHtml;
            customCssTextBox.Text = globe.projectOptions.customCssFileName = templateOptions.customCssFileName;
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

        /// <summary>
        /// Load the Paratext Projects combo box item list based on projects found in the Paratext directory.
        /// </summary>
        public void LoadParatextProjectList()
        {
            if (Directory.Exists(globe.paratextProjectsDir))
            {
                paratextcomboBox.Items.Clear();
                paratextcomboBox.Items.Add(String.Empty);
                string[] dirList = Directory.GetDirectories(globe.paratextProjectsDir);
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


        /// <summary>
        /// Load the Paratext 8 Projects combo box item list based on projects found in the Paratext directory.
        /// </summary>
        public void LoadParatext8ProjectList()
        {
            if (Directory.Exists(globe.paratext8ProjectsDir))
            {
                paratext8ComboBox.Items.Clear();
                paratext8ComboBox.Items.Add(String.Empty);
                string[] dirList = Directory.GetDirectories(globe.paratext8ProjectsDir);
                foreach (string d in dirList)
                {
                    string bookNames = Path.Combine(d, "BookNames.xml");
                    if (File.Exists(bookNames))
                    {
                        paratext8ComboBox.Items.Add(Path.GetFileNameWithoutExtension(d));
                    }
                }
            }
        }



        private void paratextButton_Click(object sender, EventArgs e)
        {
            SaveOptions();
            FolderBrowserDialog dlg = new FolderBrowserDialog();
            dlg.SelectedPath = globe.paratextProjectsDir;
            dlg.Description =
                @"Please select your existing Paratext Projects folder.";
            dlg.ShowNewFolderButton = true;
            if ((dlg.ShowDialog() != System.Windows.Forms.DialogResult.OK) || (dlg.SelectedPath == null))
                return;
            if (File.Exists(Path.Combine(dlg.SelectedPath, "usfm.sty")))
            {
                globe.paratextProjectsDir = dlg.SelectedPath;
                globe.xini.Write();
                LoadParatextProjectList();
                LoadParatext8ProjectList();
            }
        }

        private void m_projectsList_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (!loadingDirectory)
            {
                SaveOptions();
                displayOptions();
                globe.projectOptions.selected = e.NewValue == CheckState.Checked;
                if (globe.projectOptions.selected)
                    projSelected++;
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
            globe.projectOptions.numberSystem = numberSystemComboBox.Text;
            fileHelper.SetDigitLocale(globe.projectOptions.numberSystem);
            numberSystemLabel.Text = fileHelper.NumberSample();
        }

        private void paratextcomboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            string src = FindSource(globe.currentProject);
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
            globe.xini.WriteBool("runXetex", runXetexCheckBox.Checked);
            globe.xini.Write();
        }

        private void relaxNestingSyntaxCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            globe.projectOptions.relaxUsfmNesting = relaxNestingSyntaxCheckBox.Checked;
        }

        private void paratext8ComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            paratextcomboBox_SelectedIndexChanged(sender, e);
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

        private void RebuildCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            globe.xini.WriteBool("rebuild", RebuildCheckBox.Checked);
            globe.xini.Write();
        }

        
    }
}
