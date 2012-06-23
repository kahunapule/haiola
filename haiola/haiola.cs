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
using BibleFileLib;
using Microsoft.Win32;
using WordSend;
using sepp;

namespace haiola
{
    public partial class haiolaForm : Form
    {
        public static haiolaForm MasterInstance;
        private XMLini xini;    // Main program XML initialization file
        public string dataRootDir; // Default is BibleConv in the user's Documents folder
        string m_inputDirectory; // Always under dataRootDir, defaults to Documents/BibleConv/input
        public string m_outputDirectory; // curently Site, always under dataRootDir
        string m_inputProjectDirectory; //e.g., full path to BibleConv\input\Kupang
        string m_outputProjectDirectory; // e.g., full path to BibleConv\output\Kupang
        string m_project; // e.g., Kupang
        string currentConversion;   // e.g., "Preprocessing" or "Portable HTML"
        public bool autorun = false;
        static bool fAllRunning = false;
        public string m_xiniPath;  // e.g., BibleConv\input\Kupang\options.xini
        public XMLini projectXini;
        public Options m_options;
        BibleBookInfo bkInfo;
        public DateTime sourceDate = new DateTime(1611, 1, 1);



        public haiolaForm()
        {
            InitializeComponent();
            MasterInstance = this;
            batchLabel.Text = String.Format("Haiola version {0}.{1} ©2003-{2} SIL, EBT, && YWAM. Released under Gnu LGPL 3 or later.", Version.date, Version.time, Version.year);

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
                LoadWorkingDirectory();
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
            LoadWorkingDirectory();
            Application.DoEvents();
            if (autorun)
            {
                WorkOnAllButton_Click(sender, e);
                Close();
            }
        }
        private void EnsureTemplateFile(string fileName)
        {
        	EnsureTemplateFile(fileName, m_inputDirectory);
        }
 
        private void EnsureTemplateFile(string fileName, string destDirecgtory)
        {
            try
            {
                string sourcePath = WordSend.SFConverter.FindAuxFile(fileName);
				string destPath = Path.Combine(destDirecgtory, fileName);
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

        private void LoadWorkingDirectory()
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
                            string[] parts = source.Split(new char[] { delim });
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
                else
                {
                    MessageBox.Show("Can't find preprocessing file " + tp, "Error in preprocessing file list");
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
            string SourceDir = Path.Combine(m_inputProjectDirectory, "Source");
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
            string UsfmDir = Path.Combine(m_outputProjectDirectory, "usfm");
            string UsfxPath = GetUsfxDirectoryPath();
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
            string logFile = Path.Combine(m_outputProjectDirectory, "ConversionReports.txt");
            Logit.OpenFile(logFile);
            SFConverter.scripture = new Scriptures();
            Logit.loggedError = false;

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
                StreamReader log = new StreamReader(logFile);
                string errors = log.ReadToEnd();
                log.Close();
            	string message = errors;
				if (errors.Length > 5000)
				{
					// Super-long messages freeze things up
					message = message.Substring(0, 5000) + "\n...and more (see log file)";
				}
                MessageBox.Show(this, message, "Errors in " + logFile);
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
            
            usfxToHtmlConverter toHtm = m_options.UseFrames ? new UsfxToFramedHtmlConverter() : new usfxToHtmlConverter();
            Logit.OpenFile(Path.Combine(m_outputProjectDirectory, "HTMLConversionReport.txt"));

            toHtm.indexDateStamp = "HTML generated " + DateTime.UtcNow.ToString("d MMM yyyy") +
                " from source files dated " + sourceDate.ToString("d MMM yyyy");
        	toHtm.GeneratingConcordance = m_options.GenerateConcordance;
    		toHtm.CrossRefToFilePrefixMap = m_options.CrossRefToFilePrefixMap;
    		string usfxFilePath = Path.Combine(UsfxPath, "usfx.xml");
    		toHtm.ConvertUsfxToHtml(usfxFilePath, htmlPath,
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

			if (m_options.UseFrames)
			{
				// Generate the ChapterIndex file
				var ciMaker = new UsfxToChapterIndex();
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

				currentConversion = "Concordance";
				string concordanceDirectory = Path.Combine(htmlPath, "conc");
				Utils.DeleteDirectory(concordanceDirectory); // Blow away any previous results
				Utils.EnsureDirectory(concordanceDirectory);
				string excludedClasses =
					"toc toc1 toc2 navButtons pageFooter chapterlabel r verse"; // from old prophero: "verse chapter notemark crmark crossRefNote parallel parallelSub noteBackRef popup crpopup overlap";
				string headingClasses = "mt mt2 s"; // old prophero: "sectionheading maintitle2 footnote sectionsubheading";
				var concGenerator = new ConcGenerator(xhtmlPath, concordanceDirectory)
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
				concGenerator.Run(new List<string>(Directory.GetFiles(xhtmlPath)));

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
                                SFConverter.scripture = new Scriptures();
                                Logit.loggedError = false;
                                currentConversion = "converting from USFX to USFM";
                                Application.DoEvents();
                                SFConverter.scripture.USFXtoUSFM(inputFile, UsfmDir, m_options.translationId + ".usfm");
                                Logit.CloseFile();
                                if (Logit.loggedError)
                                {
                                    StreamReader log = new StreamReader(logFile);
                                    string errors = log.ReadToEnd();
                                    log.Close();
                                    MessageBox.Show(errors, "Errors in " + logFile);
                                }
                                currentConversion = "converted USFM to USFX.";
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

        private void ProcessOneProject(string projDirName)
        {
            SetCurrentProject(projDirName);
        	m_xiniPath = Path.Combine(m_inputProjectDirectory, "options.xini");
            displayOptions();

            Application.DoEvents();
            if (!fAllRunning)
                return;
            GetUsfx(projDirName);
        	Application.DoEvents();
            if (fAllRunning)
                ConvertUsfxToPortableHtml();
            Application.DoEvents();
            if (fAllRunning)
                DoPostprocess();
            Application.DoEvents();
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
            btnSetRootDirectory.Enabled = false;
            reloadButton.Enabled = false;
            m_projectsList.Enabled = false;
            checkAllButton.Enabled = false;
            unmarkAllButton.Enabled = false;
            runHighlightedButton.Enabled = false;
            startTime = DateTime.UtcNow;
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
                ProcessOneProject((string)o);
                /*
                m_project = (string)o;
                m_inputProjectDirectory = Path.Combine(m_inputDirectory, m_project);
                m_outputProjectDirectory = Path.Combine(m_outputDirectory, m_project);
                fileHelper.EnsureDirectory(m_outputProjectDirectory);
                m_xiniPath = Path.Combine(m_inputProjectDirectory, "options.xini");
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
                 */
                Application.DoEvents();
                if (!fAllRunning)
                    break;
            }
            fAllRunning = false;
            currentConversion = String.Empty;
            timer1.Enabled = false;
            batchLabel.Text = (DateTime.UtcNow - startTime).ToString() + " " + "Done.";
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
            lwcDescriptionTextBox.Text = m_options.lwcDescription;
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
            arabicNumeralsRadioButton.Checked = m_options.useArabicDigits;
            khmerNumeralsRadioButton.Checked = m_options.useKhmerDigits;
            privateCheckBox.Checked = m_options.privateProject;
            homeDomainTextBox.Text = m_options.homeDomain;

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
		}

		private void LoadFramesTab()
		{
			useFramesCheckBox.Checked = m_options.UseFrames;
			concordanceLinkTextBox.Text = m_options.ConcordanceLinkText;
			booksAndChaptersLinkTextBox.Text = m_options.BooksAndChaptersLinkText;
			introductionLinkTextBox.Text = m_options.IntroductionLinkText;
			previousChapterLinkTextBox.Text = m_options.PreviousChapterLinkText;
			nextChapterLinkTextBox.Text = m_options.NextChapterLinkText;
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
            m_options.useArabicDigits = arabicNumeralsRadioButton.Checked;
            m_options.useKhmerDigits = khmerNumeralsRadioButton.Checked;
            m_options.privateProject = privateCheckBox.Checked;
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

        private DateTime startTime = new DateTime(1, 1, 1);

        private void timer1_Tick(object sender, EventArgs e)
        {
            batchLabel.Text = (DateTime.UtcNow - startTime).ToString() + " " + m_project + " " +
                ConversionProgress;
        }

    	private string ConversionProgress
    	{
    		get
    		{
				if (currentConversion == "Concordance")
					return ConcGenerator.Stage + " " + ConcGenerator.Progress;
    			return currentConversion + " " + WordSend.usfxToHtmlConverter.conversionProgress;
    		}
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
            startTime = DateTime.UtcNow;
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
            ProcessOneProject(SelectedProject);

            fAllRunning = false;
            currentConversion = String.Empty;
            timer1.Enabled = false;
            batchLabel.Text = (DateTime.UtcNow - startTime).ToString() + " " + "Done.";
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

    	private string SelectedProject
    	{
    		get { return (string)m_projectsList.SelectedItem; }
    	}

    	private void statsButton_Click(object sender, EventArgs e)
        {
            int numProjects = 0;
            int numTranslations = 0;
            int urlid = 0;
            btnSetRootDirectory.Enabled = false;
            reloadButton.Enabled = false;
            m_projectsList.Enabled = false;
            checkAllButton.Enabled = false;
            unmarkAllButton.Enabled = false;
            runHighlightedButton.Enabled = false;
            WorkOnAllButton.Enabled = false;
            startTime = DateTime.UtcNow;
            timer1.Enabled = true;
            SaveOptions();
            StreamWriter sw = new StreamWriter(Path.Combine(m_outputDirectory, "translations.csv"));
            StreamWriter sqlFile = new StreamWriter(Path.Combine(m_outputDirectory, "Bible_list.sql"));
            StreamWriter altUrlFile = new StreamWriter(Path.Combine(m_outputDirectory, "urllist.sql"));
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
                    sw.WriteLine("\"{0}\",\"{1}\",\"{2}\",\"{3}\",\"{4}\",\"{5}\",\"{6}\",\"{7}\",\"{8}\",\"{9}\",\"{10}\",\"http://{5}/{1}/\"",
                        m_options.languageId,
                        m_options.translationId,
                        fileHelper.sqlString(m_options.languageName),
                        fileHelper.sqlString(m_options.languageNameInEnglish),
                        fileHelper.sqlString(m_options.dialect),
                        fileHelper.sqlString(m_options.homeDomain.Trim()),
                        fileHelper.sqlString(m_options.vernacularTitle.Trim()),
                        fileHelper.sqlString(m_options.EnglishDescription.Trim()),
                        (m_options.publicDomain || m_options.creativeCommons).ToString(),
                        fileHelper.sqlString(m_options.publicDomain ? "public domain" : "Copyright © " + m_options.copyrightYears + " " + m_options.copyrightOwner),
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
            currentConversion = numProjects.ToString() + " projects; " + numTranslations.ToString() + " public. " + urlid.ToString() + " URLs.";
            timer1.Enabled = false;
            statsLabel.Text = currentConversion;
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
			UpdateBooksList(false);
		}

		private void restoreDefaultsButton_Click(object sender, EventArgs e)
		{
			UpdateBooksList(true);
		}

    	private void UpdateBooksList(bool restoreDefaults)
    	{
    		fAllRunning = true;
    		GetUsfx(SelectedProject);
    		var analyzer = new UsfxToBookAndAbbr();
    		analyzer.Parse(GetUsfxFilePath());
    		Dictionary<string, string> oldNames = new Dictionary<string, string>();
    		Dictionary<string, string> oldAbbreviations = new Dictionary<string, string>();
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

		}
		void tb_LostFocus(object sender, EventArgs e)
		{
			TextBox tb = sender as TextBox;
			ListViewItem.ListViewSubItem si = (tb).Tag as ListViewItem.ListViewSubItem;
			si.Text = tb.Text;
			tb.Parent.Controls.Remove(tb);
		}

		private void clearReloadButton_Click(object sender, EventArgs e)
		{

		}

		private void btnAdjustFiles_Click(object sender, EventArgs e)
		{

		}

		private void textBox2_TextChanged(object sender, EventArgs e)
		{

		}

		private void comboSort_SelectedIndexChanged(object sender, EventArgs e)
		{

		}

		private void btnTestSort_Click(object sender, EventArgs e)
		{

		}

		private void btnAdjustBookNamesList_Click(object sender, EventArgs e)
		{

		}

		private void btnMoveBookNameDown_Click(object sender, EventArgs e)
		{

		}

		private void btnMoveBookNameUp_Click(object sender, EventArgs e)
		{

		}

		private void btnAdjustBmFiles_Click(object sender, EventArgs e)
		{

		}

		private void btnMoveDownBackMatter_Click(object sender, EventArgs e)
		{

		}

		private void btnMoveUpBackMatter_Click(object sender, EventArgs e)
		{

		}
    }
}
