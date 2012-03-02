using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Xml;
using Microsoft.Win32;
using WordSend;
using sepp;

namespace haiola
{
    public partial class haiolaForm : Form
    {
        public static haiolaForm MasterInstance;
        private XMLini xini;    // Main program XML initialization file
        public string dataRootDir; // Default is BibleConv in the user's documents folder
        string m_workDirectory; // Work, always under dataRootDir
        public string m_siteDirectory; // curently Site, always under dataRootDir
        string m_workDir; //e.g., @"C:\BibleConv\Work\Kupang"
        string m_siteDir; // e.g., c:\BibleConv\Site\Kupang
        string m_project; // e.g., Kupang
        string currentConversion;   // e.g., "Preprocessing" or "Portable HTML"
        public bool autorun = false;
        static bool fAllRunning = false;
        public string m_xiniPath;  // e.g., @"C:\BibleConv\Work\Kupang\options.xini";
        public XMLini projectXini;
        public Options m_options;
        BibleBookInfo bkInfo;
        public DateTime sourceDate = new DateTime(1611, 1, 1);



        public haiolaForm()
        {
            InitializeComponent();
            MasterInstance = this;
            batchLabel.Text = String.Format("Haiola version {0}. ©2003-{1} SIL, EBT, && YWAM. Released under Gnu LGPL 3 or later.", Version.date, Version.year);

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
        }

        bool GetRootDirectory()
        {
            FolderBrowserDialog dlg = new FolderBrowserDialog();
            dlg.SelectedPath = dataRootDir;
            dlg.Description =
                @"Select a folder to contain your working directories.";
            dlg.ShowNewFolderButton = true;
            if (dlg.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return false;
            dataRootDir = dlg.SelectedPath;
            m_workDirectory = Path.Combine(dataRootDir, "Work");
            m_siteDirectory = Path.Combine(dataRootDir, "Site");
            xini.WriteString("dataRootDir", dataRootDir);
            xini.Write();
            string templateDir = Path.Combine(fileHelper.ExePath, "BibleConv");
            if (!Directory.Exists(templateDir))
                templateDir = Path.Combine(fileHelper.ExePath, Path.Combine("..", Path.Combine("..", Path.Combine("..", "BibleConv"))));
            if (!Directory.Exists(templateDir))
                templateDir = Path.Combine(fileHelper.ExePath, "BibleConv");
            fileHelper.CopyDirectory(templateDir, dataRootDir);
            return true;
        }






        private void btnSetRootDirectory_Click(object sender, EventArgs e)
        {
            SaveOptions();
            if (GetRootDirectory())
                LoadWorkingDirectory();
        }

        private void haiolaForm_Load(object sender, EventArgs e)
        {
            xini = new XMLini(Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "haiola"),
    "haiola.xini"));
            dataRootDir = xini.ReadString("dataRootDir", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BibleConv"));
            m_workDirectory = Path.Combine(dataRootDir, "Work");
            m_siteDirectory = Path.Combine(dataRootDir, "Site");
            if (!Directory.Exists(m_workDirectory))
                if (!GetRootDirectory())
                    Application.Exit();
            LoadWorkingDirectory();
            Application.DoEvents();
            if (autorun)
            {
                WorkOnAllButton_Click(sender, e);
                Close();
            }

        }

        private void LoadWorkingDirectory()
        {
            int projCount = 0;
            int projReady = 0;
            m_projectsList.BeginUpdate();
            m_projectsList.Items.Clear();
            fileHelper.EnsureDirectory(dataRootDir);
            fileHelper.EnsureDirectory(m_workDirectory);
            workDirLabel.Text = dataRootDir;
            foreach (string path in Directory.GetDirectories(m_workDirectory))
            {
                m_projectsList.Items.Add(Path.GetFileName(path));
                projCount++;
                if (File.Exists(Path.Combine(path, "options.xini")))
                {
                    m_projectsList.SetItemChecked(m_projectsList.Items.Count - 1, true);
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
                MessageBox.Show(this, "No projects found in " + m_workDirectory
                                      +
                                      ". You should create a folder there for your project and place your input files in the appropriate subdirectory.",
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
            StreamReader sr = new StreamReader(pathName);
            while ((result.Length < 1) && (!sr.EndOfStream))
            {
                line = sr.ReadLine();
                if (line.StartsWith(@"\id ") && (line.Length > 6))
                {
                    result = line.Substring(4, 3).ToUpper(System.Globalization.CultureInfo.InvariantCulture);
                }
            }
            sr.Close();
            if (result.Length > 0)
            {
                result = bkInfo.Order(result).ToString("D2") + "-" + result;
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
                string tablePath = Utils.GetUtilityFile(Path.Combine(m_workDir, tp));
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
						string[] parts = source.Split(new char[] {delim});
						string pattern = parts[1]; // parts[0] is the empty string before the first delimiter
						string replacement = parts[2];
						temp = System.Text.RegularExpressions.Regex.Replace(temp, pattern, replacement);
					}
					tableReader.Close();
					temp = temp + "\0";
					outBuffer = Encoding.UTF8.GetBytes(temp);
					inputBytes = outBuffer;
					cbyteInput = nOutLen = inputBytes.Length;
				}
			}

            // Convert the output back to a file
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



        public void PreprocessUsfmFiles()
        {
            string SourceDir = Path.Combine(m_workDir, "Source");
            string UsfmDir = Path.Combine(m_workDir, "USFM");
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
                    (fileType != ".CSS") &&
                    (fileType != ".VRS") && (!inputFile.EndsWith("~")))
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

        private void ConvertUsfmToUsfx()
        {
            string UsfmDir = Path.Combine(m_workDir, "USFM");
            string UsfxPath = Path.Combine(m_workDir, "usfx");
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
                return;
            Utils.EnsureDirectory(UsfxPath);
            string logFile = Path.Combine(UsfxPath, "ConversionReports.txt");
            Logit.OpenFile(logFile);
            SFConverter.scripture = new Scriptures();
            Logit.loggedError = false;

            // Read the input USFM files into internal data structures.
            SFConverter.ProcessFilespec(Path.Combine(UsfmDir, "*.usfm"), Encoding.UTF8);
            currentConversion = "converting from USFM to USFX; writing USFX";
            Application.DoEvents();

            // Write out the USFX file.
            SFConverter.scripture.languageCode = m_options.languageId;
            SFConverter.scripture.WriteUSFX(Path.Combine(UsfxPath, "usfx.xml"));
            Logit.CloseFile();
            if (Logit.loggedError)
            {
                StreamReader log = new StreamReader(logFile);
                string errors = log.ReadToEnd();
                log.Close();
                MessageBox.Show(errors, "Errors in " + logFile);
            }
            currentConversion = "converted USFM to USFX.";
            Application.DoEvents();
        }

        private void ConvertUsfxToPortableHtml()
        {
            currentConversion = "writing portable HTML";
            if ((m_options.languageId.Length < 3) || (m_options.translationId.Length < 3))
                return;
            string UsfxPath = Path.Combine(m_workDir, "usfx");
            if (!Directory.Exists(UsfxPath))
            {
                MessageBox.Show(this, UsfxPath + " not found!", "ERROR");
                return;
            }
            Utils.EnsureDirectory(m_siteDirectory);
            Utils.EnsureDirectory(m_siteDir);
            string propherocss = Path.Combine(m_siteDir, "prophero.css");
            if (File.Exists(propherocss))
                File.Delete(propherocss);
            // Copy prophero.css from project directory, or if not there, FilesToCopyToOutput/css/prophero.css.
            string specialCss = Path.Combine(m_workDir, "prophero.css");
            if (File.Exists(specialCss))
                File.Copy(specialCss, propherocss);
            else
                File.Copy(Path.Combine(Path.Combine(Path.Combine(dataRootDir, "FilesToCopyToOutput"), "css"), "prophero.css"), propherocss);
            
            usfxToHtmlConverter toHtm = new usfxToHtmlConverter();
            Logit.OpenFile(Path.Combine(UsfxPath, "HTMLConversionReport.txt"));

            toHtm.indexDateStamp = "HTML generated " + DateTime.UtcNow.ToString("d MMM yyyy") +
                " from source files dated " + sourceDate.ToString("d MMM yyyy");
            toHtm.ConvertUsfxToHtml(Path.Combine(UsfxPath, "usfx.xml"), m_siteDir,
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
            // List vernacular full book titles
            foreach (WordSend.BibleBookRecord br in WordSend.usfxToHtmlConverter.bookList)
            {
                xml.WriteStartElement("scriptureBook");
                xml.WriteAttributeString("ubsAbbreviation", br.tla);
                xml.WriteAttributeString("parm", "vernacularFullName");
                xml.WriteString(br.vernacularName);
                xml.WriteEndElement();  // scriptureBook
            }
            // List vernacular short names for running headers and links
            foreach (WordSend.BibleBookRecord br in WordSend.usfxToHtmlConverter.bookList)
            {
                xml.WriteStartElement("scriptureBook");
                xml.WriteAttributeString("ubsAbbreviation", br.tla);
                xml.WriteAttributeString("parm", "vernacularAbbreviatedName");
                xml.WriteString(br.vernacularHeader);
                xml.WriteEndElement();  // scriptureBook
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
        }

        /// <summary>
        /// Runs a command like it was typed on the command line.
        /// Uses bash as the command interpretor on Linux & Mac OSX; cmd.exe on Windows.
        /// </summary>
        /// <param name="command">Command to run, with or without full path.</param>
        public void RunCommand(string command)
        {
            System.Diagnostics.Process runningCommand = null;
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
                currentConversion = "Running " + command;
                batchLabel.Text = currentConversion;
                Application.DoEvents();
                RunCommand(command);
                currentConversion = String.Empty;
                if (!fAllRunning)
                    return;
            }
            

        }

        private void WorkOnAllButton_Click(object sender, EventArgs e)
        {
            btnSetRootDirectory.Enabled = false;
            reloadButton.Enabled = false;
            m_projectsList.Enabled = false;
            checkAllButton.Enabled = false;
            unmarkAllButton.Enabled = false;
            runHighlightedButton.Enabled = false;
            timer1.Enabled = true;
            if (fAllRunning)
            {
                fAllRunning = false;
                WorkOnAllButton.Enabled = false;
                WorkOnAllButton.Text = "Stopping...";
                Application.DoEvents();
                return;
            }
            fAllRunning = true;
            WorkOnAllButton.Text = "Stop";
            SaveOptions();
            foreach (object o in m_projectsList.CheckedItems)
            {
                m_project = (string)o;
                m_workDir = Path.Combine(m_workDirectory, m_project);
                m_siteDir = Path.Combine(m_siteDirectory, m_project);
                m_xiniPath = Path.Combine(m_workDir, "options.xini");
                displayOptions();

                Application.DoEvents();
                if (!fAllRunning)
                    break;
                PreprocessUsfmFiles();
                Application.DoEvents();
                if (!fAllRunning)
                    break;
                ConvertUsfmToUsfx();
                Application.DoEvents();
                if (!fAllRunning)
                    break;
                ConvertUsfxToPortableHtml();
                Application.DoEvents();
                if (!fAllRunning)
                    break;
                DoPostprocess();
                Application.DoEvents();
                if (!fAllRunning)
                    break;
            }
            fAllRunning = false;
            currentConversion = String.Empty;
            timer1.Enabled = false;
            batchLabel.Text = "Stopped.";
            m_projectsList_SelectedIndexChanged(null, null);
            WorkOnAllButton.Enabled = true;
            WorkOnAllButton.Text = "Run marked";
            m_projectsList.Enabled = true;
            checkAllButton.Enabled = true;
            unmarkAllButton.Enabled = true;
            btnSetRootDirectory.Enabled = true;
            reloadButton.Enabled = true;
            runHighlightedButton.Enabled = true;
        }

        private void reloadButton_Click(object sender, EventArgs e)
        {
            SaveOptions();
            LoadWorkingDirectory();
        }

        private void checkAllButton_Click(object sender, EventArgs e)
        {
            int i;
            SaveOptions();
            for (i = 0; i < m_projectsList.Items.Count; i++)
                m_projectsList.SetItemChecked(i, true);

        }

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
            updateDateTimePicker.MaxDate = DateTime.Now.AddDays(2);
            updateDateTimePicker.Value = m_options.contentUpdateDate;
            pdRadioButton.Checked = m_options.publicDomain;
            ccRadioButton.Checked = m_options.creativeCommons;
            otherRadioButton.Checked = m_options.otherLicense;
            allRightsRadioButton.Checked = m_options.allRightsReserved;
            silentRadioButton.Checked = m_options.silentCopyright;
            copyrightOwnerTextBox.Text = m_options.copyrightOwner;
            copyrightYearTextBox.Text = m_options.copyrightYears;
            rightsStatementTextBox.Text = m_options.rightsStatement;
            printPublisherTextBox.Text = m_options.printPublisher;
            electronicPublisherTextBox.Text = m_options.electronicPublisher;
            stripExtrasCheckBox.Checked = m_options.ignoreExtras;
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
            copyrightLinkTextBox.Text = m_options.copyrightLink;
            goTextTextBox.Text = m_options.goText;
            footerHtmlTextBox.Text = m_options.footerHtml;
            indexPageTextBox.Text = m_options.indexHtml;
            licenseTextBox.Text = m_options.licenseHtml;
            versificationComboBox.Text = m_options.versificationScheme;
            arabicNumeralsRadioButton.Checked = m_options.useArabicDigits;
            khmerNumeralsRadioButton.Checked = m_options.useKhmerDigits;
            privateCheckBox.Checked = m_options.privateProject;
            homeDomainTextBox.Text = m_options.homeDomain;
        }

        private void m_projectsList_SelectedIndexChanged(object sender, EventArgs e)
        {
            m_project = m_projectsList.SelectedItem.ToString();
            m_workDir = Path.Combine(m_workDirectory, m_project);
            m_siteDir = Path.Combine(m_siteDirectory, m_project);
            m_xiniPath = Path.Combine(m_workDir, "options.xini");
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
            m_options.contentUpdateDate = updateDateTimePicker.Value;
            m_options.publicDomain = pdRadioButton.Checked;
            m_options.creativeCommons = ccRadioButton.Checked;
            m_options.otherLicense = otherRadioButton.Checked;
            m_options.allRightsReserved = allRightsRadioButton.Checked;
            m_options.silentCopyright = silentRadioButton.Checked;
            m_options.copyrightOwner = copyrightOwnerTextBox.Text;
            m_options.copyrightYears = copyrightYearTextBox.Text;
            m_options.rightsStatement = rightsStatementTextBox.Text;
            m_options.printPublisher = printPublisherTextBox.Text;
            m_options.electronicPublisher = electronicPublisherTextBox.Text;
            m_options.ignoreExtras = stripExtrasCheckBox.Checked;
            List<string> tableNames = new List<string>();
            foreach (string filename in listInputProcesses.Items)
                tableNames.Add(filename);
            m_options.preprocessingTables = tableNames;
            List<string> postprocessNames = new List<string>();
            foreach (string filename in postprocessListBox.Items)
                postprocessNames.Add(filename);
            m_options.postprocesses = postprocessNames;
            // Insert more checkbox settings here.
            m_options.homeLink = homeLinkTextBox.Text;
            m_options.goText = goTextTextBox.Text;
            m_options.copyrightLink = copyrightLinkTextBox.Text;
            m_options.footerHtml = footerHtmlTextBox.Text;
            m_options.indexHtml = indexPageTextBox.Text;
            m_options.licenseHtml = licenseTextBox.Text;
            m_options.versificationScheme = versificationComboBox.Text;
            m_options.useArabicDigits = arabicNumeralsRadioButton.Checked;
            m_options.useKhmerDigits = khmerNumeralsRadioButton.Checked;
            m_options.privateProject = privateCheckBox.Checked;
            m_options.homeDomain = homeDomainTextBox.Text.Trim();
            m_options.Write();
        }

        private void btnAddInputProcess_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.CheckFileExists = true;
            dlg.InitialDirectory = m_workDir;
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
                if ((newFileDir.ToLowerInvariant() != m_workDir.ToLowerInvariant()) &&
                    (newFileDir.ToLowerInvariant() != m_workDirectory.ToLowerInvariant()) &&
                    (newFileDir.ToLowerInvariant() != dataRootDir.ToLowerInvariant()))
                {
                    if (MessageBox.Show(this, "Preprocessing files must be in the work directory. Copy there?", "Note", MessageBoxButtons.YesNo) ==
                        DialogResult.Yes)
                    {
                        File.Copy(newFilePath, Path.Combine(m_workDir, newFileName));
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

        private void timer1_Tick(object sender, EventArgs e)
        {
            batchLabel.Text = DateTime.UtcNow.ToString("HH:mm:ss") + " " + m_project + " " +
                currentConversion + " " + WordSend.usfxToHtmlConverter.conversionProgress;
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
            btnSetRootDirectory.Enabled = false;
            reloadButton.Enabled = false;
            m_projectsList.Enabled = false;
            checkAllButton.Enabled = false;
            unmarkAllButton.Enabled = false;
            runHighlightedButton.Enabled = false;
            timer1.Enabled = true;
            if (fAllRunning)
            {
                fAllRunning = false;
                WorkOnAllButton.Enabled = false;
                WorkOnAllButton.Text = "Stopping...";
                Application.DoEvents();
                return;
            }
            fAllRunning = true;
            WorkOnAllButton.Text = "Stop";
            SaveOptions();
            m_project = (string)m_projectsList.SelectedItem;
            m_workDir = Path.Combine(m_workDirectory, m_project);
            m_siteDir = Path.Combine(m_siteDirectory, m_project);
            m_xiniPath = Path.Combine(m_workDir, "options.xini");
            displayOptions();

            Application.DoEvents();
            if (fAllRunning)
                PreprocessUsfmFiles();
            Application.DoEvents();
            if (fAllRunning)
                ConvertUsfmToUsfx();
            Application.DoEvents();
            if (fAllRunning)
                ConvertUsfxToPortableHtml();
            Application.DoEvents();
            if (fAllRunning)
                DoPostprocess();
            fAllRunning = false;
            currentConversion = String.Empty;
            timer1.Enabled = false;
            batchLabel.Text = "Stopped.";
            m_projectsList_SelectedIndexChanged(null, null);
            WorkOnAllButton.Enabled = true;
            WorkOnAllButton.Text = "Run marked";
            m_projectsList.Enabled = true;
            checkAllButton.Enabled = true;
            unmarkAllButton.Enabled = true;
            btnSetRootDirectory.Enabled = true;
            reloadButton.Enabled = true;
            runHighlightedButton.Enabled = true;

        }

        private void statsButton_Click(object sender, EventArgs e)
        {
            int numProjects = 0;
            int numTranslations = 0;
            btnSetRootDirectory.Enabled = false;
            reloadButton.Enabled = false;
            m_projectsList.Enabled = false;
            checkAllButton.Enabled = false;
            unmarkAllButton.Enabled = false;
            runHighlightedButton.Enabled = false;
            WorkOnAllButton.Enabled = false;
            timer1.Enabled = true;
            SaveOptions();
            StreamWriter sw = new StreamWriter(Path.Combine(m_workDirectory, "translations.csv"));
            sw.WriteLine("\"languageCode\",\"translationId\",\"languageName\",\"languageNameInEnglish\",\"dialect\",\"homeDomain\",\"title\",\"description\",\"Free\",\"Copyright\",\"UpdateDate\",\"publicationURL\"");
            foreach (object o in m_projectsList.Items)
            {
                m_project = (string)o;
                m_workDir = Path.Combine(m_workDirectory, m_project);
                m_siteDir = Path.Combine(m_siteDirectory, m_project);
                m_xiniPath = Path.Combine(m_workDir, "options.xini");
                displayOptions();
                numProjects++;
                if ((!m_options.privateProject) && (m_options.languageId.Length > 1))
                {
                    numTranslations++;
                    sw.WriteLine("\"{0}\",\"{1}\",\"{2}\",\"{3}\",\"{4}\",\"{5}\",\"{6}\",\"{7}\",\"{8}\",\"{9}\",\"{10}\",\"http://{5}/{1}/\"",
                        m_options.languageId,
                        m_options.translationId,
                        m_options.languageName,
                        m_options.languageNameInEnglish,
                        m_options.dialect,
                        m_options.homeDomain.Trim(),
                        m_options.vernacularTitle.Trim(),
                        m_options.EnglishDescription.Trim(),
                        (m_options.publicDomain || m_options.creativeCommons).ToString(),
                        m_options.publicDomain ? "public domain" : "Copyright © " + m_options.copyrightYears + " " + m_options.copyrightOwner,
                        m_options.contentUpdateDate.ToString("yyyy-MM-dd"));
                }
            }
            sw.Close();
            fAllRunning = false;
            currentConversion = numProjects.ToString() + " projects; " + numTranslations.ToString() + " public.";
            timer1.Enabled = false;
            statsLabel.Text = batchLabel.Text = currentConversion;
            m_projectsList_SelectedIndexChanged(null, null);
            WorkOnAllButton.Enabled = true;
            WorkOnAllButton.Text = "Run marked";
            m_projectsList.Enabled = true;
            checkAllButton.Enabled = true;
            unmarkAllButton.Enabled = true;
            btnSetRootDirectory.Enabled = true;
            reloadButton.Enabled = true;
            runHighlightedButton.Enabled = true;
            WorkOnAllButton.Enabled = true;
        }

        
       
    }
}
