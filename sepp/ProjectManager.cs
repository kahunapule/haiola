using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.IO;
using System.Diagnostics;
using WordSend;

namespace sepp
{
	public partial class ProjectManager : Form
	{
		string m_optionsPath; // e.g.,@"C:\BibleConv\Work\Kupang\Sepp Options.xml";
		private Options m_options;
		string m_workDir; //e.g., @"C:\BibleConv\Work\Kupang"
		string m_siteDir; // e.g., c:\BibleConv\Site\Kupang
		string[] m_tablePaths; // if non-null, OW_TO_USFM is replaced with Preprocess from Source directory using these tables.
		public ProjectManager(string workDir, string siteDir)
		{
			m_workDir = workDir;
			m_siteDir = siteDir;
			InitializeComponent();
            Text = "Project Tasks - " + workDir;
		}

		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);
			Reload();
		}

		private void Reload()
		{
		XmlDocument optionsDoc = new XmlDocument();
			m_optionsPath = Path.Combine(m_workDir, "Sepp Options.xml");
			if (!File.Exists(m_optionsPath))
			{
                if (!GenerateOptions(m_optionsPath))
                {
                    this.Close();
                    return;
                }
                /*
                Don't bother the user with this question, any more. It gets annoying when working with massive numbers of languages.
				if (MessageBox.Show(
					"File " + m_optionsPath + " was not found. You must create an options file for your project. Would you like a default options file created automatically?",
					"Error",
					MessageBoxButtons.YesNo) != DialogResult.Yes || !GenerateOptions(m_optionsPath))
				{
					this.Close();
					return;
				}
                */
			}
			optionsDoc.Load(m_optionsPath);
			XmlNode root = optionsDoc.DocumentElement;
			foreach (XmlNode node in root.ChildNodes)
			{
				switch (node.Name)
				{
					case "files":
						BuildFileList(node);
						break;
					case "preprocess":
						SetupPreprocessing(node);
						break;
				}
			}
			m_options = new Options();
			m_options.LoadOptions(m_optionsPath);
		}

		private void SetupPreprocessing(XmlNode node)
		{
			if (node.ChildNodes.Count == 0)
				return;
			m_button_OW_to_USFM.Text = "Preprocess";
			m_tablePaths = new string[node.ChildNodes.Count];
			for (int i = 0; i < m_tablePaths.Length; i++)
			{
				string tableName = node.ChildNodes[i].InnerText;
				if (Path.GetExtension(tableName).Length > 0)
					tableName = Path.Combine(m_workDir, tableName);
				m_tablePaths[i] = tableName;
			}
		}

		CheckedListBox.CheckedItemCollection ActiveFiles
		{
			get { return m_filesList.CheckedItems; }
		}

		internal string WorkPath
		{
			get { return m_workDir; }
		}

		/// <summary>
		///  The directory that is the root of all projects, one up from WorkPath.
		/// </summary>
		internal string RootWorkPath
		{
			get { return Path.GetDirectoryName(WorkPath);  }
		}

		internal string ExtrasPath
		{
			get { return Path.Combine(m_workDir, "Extras"); }
		}

		internal string OurWordPath
		{
			get { return Path.Combine(m_workDir, "OW"); }
		}

		internal string PreProcessPath
		{
			get { return Path.Combine(m_workDir, "Source"); }
		}

		// Generate a missing options file.
		private bool GenerateOptions(string m_optionsPath)
		{
			string basePath = Path.Combine(Path.GetDirectoryName(m_workDir), "Sepp Options.xml");
			if (!File.Exists(basePath))
			{
				MessageBox.Show("Options pattern file " + basePath + " not found. Unable to proceed.", "Sorry");
				return false;
			}
			XmlDocument doc = new XmlDocument();
			doc.Load(basePath);
			XmlElement fileSeqElt = (XmlElement)doc.SelectSingleNode("//files");
			fileSeqElt.RemoveAll();
			XmlWriterSettings settings = new XmlWriterSettings();
			settings.Encoding = Encoding.UTF8;
			settings.Indent = true;
			XmlWriter writer = XmlWriter.Create(m_optionsPath, settings);
			doc.WriteTo(writer);
			writer.Close();
			return true;
		}

		private void BuildFileList(XmlNode node)
		{
			m_filesList.SuspendLayout();
			m_filesList.Items.Clear();
			foreach (XmlNode item in node.ChildNodes)
			{
				string fileName = item.Attributes["name"].Value;
				m_filesList.Items.Add(fileName, CheckState.Checked);
			}
			m_filesList.ResumeLayout();
		}

		string ConcPath
		{
			get { return Path.Combine(m_siteDir, @"Conc"); }
		}

		const string OwDir = "OW";
		private const string SourceDir = "Source";
		internal string SourcePath
		{
			get { return Path.Combine(m_workDir, SourceDir); }
		}

		private void m_button_Input_to_USFM_Click(object sender, EventArgs e)
		{
            m_button_OW_to_USFM.Enabled = false;
            Application.DoEvents();
			string srcDir = OwDir;
			if (ConvertingSourceToUsfm())
				srcDir = SourceDir;
			OW_To_USFM converter = new OW_To_USFM(Path.Combine(m_workDir, srcDir), Path.Combine(m_workDir, @"USFM"), m_options);
			if (m_tablePaths != null)
			{
				converter.TablePaths = m_tablePaths;
			}
			converter.Run();
            m_button_OW_to_USFM.Enabled = true;
            Application.DoEvents();
        }

		private bool ConvertingSourceToUsfm()
		{
			return m_tablePaths != null;
		}

		String UsfmDir = @"USFM";
		String OsisDir = @"OSIS";
		private void m_button_USFM_to_OSIS_Click(object sender, EventArgs e)
		{
			USFM_to_OSIS converter = new USFM_to_OSIS(UsfmPath, OsisPath, m_optionsPath);
			converter.Run(m_filesList.CheckedItems);
		}

		private string OsisPath
		{
			get { return Path.Combine(m_workDir, OsisDir); }
		}

		internal string UsfmPath
		{
			get { return Path.Combine(m_workDir, UsfmDir); }
		}

        private string UsfxPath
        {
            get { return Path.Combine(m_workDir, "usfx"); }
        }

		private string HtmlPath()
		{
			return Path.Combine(m_workDir, @"HTML");
		}

        private string HtmPath
        {
            get { return m_siteDir; /* was Path.Combine(m_workDir, "htm");*/ }
        }

		internal string IntroDir
		{
			get { return Path.Combine(m_workDir, @"Intro");  }
		}


		private void m_buttonUncheckAll_Click(object sender, EventArgs e)
		{
			List<int> indexes = new List<int>();
			foreach (object o in m_filesList.CheckedItems)
				indexes.Add(m_filesList.Items.IndexOf(o));
			foreach(int i in indexes)
				m_filesList.SetItemChecked(i, false);
		}

		private void m_buttonCheckAll_Click(object sender, EventArgs e)
		{
			for (int i = 0; i < m_filesList.Items.Count; i++)
				m_filesList.SetItemChecked(i, true);
		}

		private void optionsButton_Click(object sender, EventArgs e)
		{
			OptionsDlg dlg = new OptionsDlg(this);
			dlg.InitDlg(m_options);
			if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
			{
				m_options.SaveOptions(m_optionsPath);
				Reload();
			}
		}

		private void buttonAllSteps_Click(object sender, EventArgs e)
		{
            try
            {
                buttonAllSteps.Enabled = false;
                if (OwCheckBox.Checked && (Directory.Exists(Path.Combine(m_workDir, OwDir)) || ConvertingSourceToUsfm()))
                    m_button_Input_to_USFM_Click(this, e);
                if (UsfxCheckBox.Checked && Directory.Exists(UsfmPath))
                {
                    UsfmToUsfxButton_Click(sender, e);
                }
                Application.DoEvents();
                if (OsisCheckBox.Checked)
                    m_button_USFM_to_OSIS_Click(this, e);
                if (HtmlCheckBox.Checked)
                    UsfxToHtmlButton_Click(sender, e);
                if (copySupportFilesCheckBox.Checked)
                    btnCopySupportFiles_Click(this, e);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace);
            }
            buttonAllSteps.Enabled = true;
            Refresh();
        }

        public void automaticRun()
        {
            buttonAllSteps_Click(null, null);
            Close();
        }

		private void btnCopySupportFiles_Click(object sender, EventArgs e)
		{
            btnCopySupportFiles.Enabled = false;
            Application.DoEvents();
			string supportDir = Path.Combine(Master.MasterInstance.dataRootDir, "FilesToCopyToOutput");
            Utils.CopyDirectory(Path.Combine(supportDir, "css"), Path.Combine(Master.MasterInstance.m_siteDirectory, "css"));
            btnCopySupportFiles.Enabled = true;
            Application.DoEvents();
		}

		private void DoCopy(string path, string destPath)
		{
			if (File.Exists(destPath) && File.GetLastWriteTime(path) < File.GetLastWriteTime(destPath))
			{
				if (MessageBox.Show(this, "Replace the newer file " + destPath + " with the older file " + path + "?",
					"Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.No)
				{
					return;
				}
			}
			File.Copy(path, destPath, true);
		}

        private void UsfmToUsfxButton_Click(object sender, EventArgs e)
        {
            UsfmToUsfxButton.Enabled = false;
            Application.DoEvents();
            Utils.EnsureDirectory(UsfxPath);

            Logit.OpenFile(Path.Combine(UsfxPath, "ConversionReports.txt"));
            SFConverter.scripture = new Scriptures();

            // Read the input USFM files into internal data structures.
            SFConverter.ProcessFilespec(Path.Combine(UsfmPath, "*.usfm"));

            // Write out the USFX file.
            SFConverter.scripture.languageCode = m_options.m_languageId;
            SFConverter.scripture.WriteUSFX(Path.Combine(UsfxPath, "usfx.xml"));
            Logit.CloseFile();
            UsfmToUsfxButton.Enabled = true;
            Application.DoEvents();
        }

        private void UsfxToHtmlButton_Click(object sender, EventArgs e)
        {
            UsfxToHtmlButton.Enabled = false;
            Application.DoEvents();
            Utils.EnsureDirectory(HtmPath);
            usfxToHtmlConverter toHtm = new usfxToHtmlConverter();
            Logit.OpenFile(Path.Combine(UsfxPath, "HTMLConversionReport.txt"));
            toHtm.ConvertUsfxToHtml(Path.Combine(UsfxPath, "usfx.xml"), HtmPath,
                m_options.m_languageName,
                m_options.m_languageId,
                m_options.m_chapterLabel,
                m_options.m_psalmLabel,
                m_options.m_copyrightLink,
                m_options.m_homeLink,
                m_options.m_footerHtml,
                m_options.m_indexHtml,
                m_options.m_licenseHtml);
            Logit.CloseFile();
            string postprocess = Path.Combine(Master.MasterInstance.m_siteDirectory, "postprocess.bat");
            if (File.Exists(postprocess))
                Process.Start(postprocess, m_options.m_languageId);

            UsfxToHtmlButton.Enabled = true;
            Application.DoEvents();
        }
	}
}