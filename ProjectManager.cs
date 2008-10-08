using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.IO;

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
				if (MessageBox.Show(
					"File " + m_optionsPath + " was not found. You must create an options file for your project. Would you like a default options file created automatically?",
					"Error",
					MessageBoxButtons.YesNo) != DialogResult.Yes || !GenerateOptions(m_optionsPath))
				{
					this.Close();
					return;
				}
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
				m_tablePaths[i] = Path.Combine(m_workDir,node.ChildNodes[i].InnerText);
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
			Dictionary<string, string> engAbbrs = new Dictionary<string, string>();
			Dictionary<string, string> parallelNames = new Dictionary<string, string>();
			Dictionary<string, string> fileNames = new Dictionary<string, string>();
			List<string> order = new List<string>();
			foreach (XmlElement fileElt in fileSeqElt.ChildNodes)
			{
				if (fileElt.Name == "file")
				{
					XmlAttribute attEng = fileElt.Attributes["eng"];
					XmlAttribute attAbbr = fileElt.Attributes["abbr"];
					XmlAttribute attParallel = fileElt.Attributes["parallel"];
					if (attAbbr != null)
					{
						order.Add(attAbbr.Value);
						if (attEng != null)
							engAbbrs[attAbbr.Value] = attEng.Value;
						if (attParallel != null)
							parallelNames[attAbbr.Value] = attParallel.Value;
					}
				}
			}
			int cAbbreviationsInvented = 0;
			string srcDir = Path.Combine(m_workDir, "OW");
			if (!Directory.Exists(srcDir))
			{
				srcDir = Path.Combine(m_workDir, "USFM");
			}
			foreach (string filePath in Directory.GetFiles(srcDir))
			{
				// For historical reasons the file name in the optinos file is supposed to end in xml
				string fileName = Path.ChangeExtension(Path.GetFileName(filePath), "xml");
				// See if it matches any of our existing abbreviations.
				string fileNameUpper = fileName.ToUpper();
				bool fGotIt = false;
				foreach (string abbr in order)
				{
					if (fileNameUpper.IndexOf(abbr.ToUpper()) != -1)
					{
						fileNames[abbr] = fileName;
						fGotIt = true;
						break;
					}
				}
				if (!fGotIt)
				{
					string abbr1;
					// at least in Kupang, abbr comes after first hyphen
					int firstHyphen = fileName.IndexOf('-');
					int secondHyphen = -1;
					if (firstHyphen != -1)
						secondHyphen = fileName.IndexOf('-', firstHyphen + 1);
					if (secondHyphen > 0)
					{
						abbr1 = fileName.Substring(firstHyphen + 1, secondHyphen - firstHyphen - 1);
					}
					else if (fileName.Length > 5 && Char.IsDigit(fileName[0]) && Char.IsDigit(fileName[1]))
					{
						// Another very common pattern is a two-digit number to order things properly and then
						// the book abbreviation.
						if (Char.IsDigit(fileName[2]))
							abbr1 = fileName.Substring(2, 2).ToUpper() + fileName.Substring(4, 1).ToLower();
						else
							abbr1 = fileName.Substring(2, 1).ToUpper() + fileName.Substring(3, 2).ToLower();
					}
					else
					{
						abbr1 = "abbr" + (++cAbbreviationsInvented);
					}
					order.Add(abbr1);
					fileNames[abbr1] = fileName;
				}
			}
			// Remove existig elements (but preserve attributes).
			XmlAttributeCollection attrs = fileSeqElt.Attributes;
			fileSeqElt.RemoveAll();
			foreach (XmlAttribute attr in attrs)
				fileSeqElt.Attributes.Append(attr);
			// Now generate a new set of file elements.
			foreach (string key in order)
			{
				string fileName1;
				if (!fileNames.TryGetValue(key, out fileName1))
					continue; // not matched, skip.
				XmlElement fileNode = doc.CreateElement("file");
				fileSeqElt.AppendChild(fileNode);
				fileNode.SetAttribute("name", fileName1);
				fileNode.SetAttribute("abbr", key);
				string eng;
				if (engAbbrs.TryGetValue(key, out eng))
					fileNode.SetAttribute("eng", eng);
				string parallel;
				if (parallelNames.TryGetValue(key, out parallel))
					fileNode.SetAttribute("parallel", parallel);
			}
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

		private void m_runButton_Click(object sender, EventArgs e)
		{
			ConcGenerator generator = new ConcGenerator(
				Path.Combine(m_workDir, @"ConcInput"), ConcPath, m_optionsPath, m_options);
			generator.Run(m_filesList.CheckedItems);
		}

		const string OwDir = "OW";
		private const string SourceDir = "Source";
		internal string SourcePath
		{
			get { return Path.Combine(m_workDir, SourceDir); }
		}

		private void m_button_Input_to_USFM_Click(object sender, EventArgs e)
		{
			string srcDir = OwDir;
			if (ConvertingSourceToUsfm())
				srcDir = SourceDir;
			OW_To_USFM converter = new OW_To_USFM(Path.Combine(m_workDir, srcDir), Path.Combine(m_workDir, @"USFM"));
			if (m_tablePaths != null)
			{
				converter.TablePaths = m_tablePaths;
			}
			converter.Run(m_filesList.CheckedItems);
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

		private void m_buttonOSIS_to_HTML_Click(object sender, EventArgs e)
		{
			OSIS_to_HTML converter = new OSIS_to_HTML(
				Path.Combine(m_workDir, @"OSIS"), Path.Combine(m_workDir, @"HTML"),
				Path.Combine(m_siteDir, @"Conc"), Path.Combine(m_workDir, @"Sepp Options.xml"),
				m_options);
			converter.Run(m_filesList.CheckedItems);

		}

		private void m_buttonHTML_to_XHTML_Click(object sender, EventArgs e)
		{
			HTML_TO_XHTML converter = new HTML_TO_XHTML(Path.Combine(m_workDir, @"HTML"), Path.Combine(m_workDir, @"ConcInput"), m_options);
			converter.Run(m_filesList.CheckedItems);

		}

		internal string IntroDir
		{
			get { return Path.Combine(m_workDir, @"Intro");  }
		}

		private void m_buttonChapIndex_Click(object sender, EventArgs e)
		{
			OSIS_to_ChapIndex generator = new OSIS_to_ChapIndex(Path.Combine(m_workDir, @"OSIS"), Path.Combine(m_siteDir, @"Conc"),
				IntroDir, ExtrasPath,
				m_options);
			generator.Run(m_filesList.CheckedItems);

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

		private void m_bookNameButton_Click(object sender, EventArgs e)
		{
			BookNamePageGenerator generator = new BookNamePageGenerator(Path.GetDirectoryName(m_workDir), m_workDir,
				Path.Combine(m_workDir, @"Sepp Options.xml"));
			generator.Run(m_filesList.CheckedItems);
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
			if (Directory.Exists(Path.Combine(m_workDir, OwDir)) || ConvertingSourceToUsfm())
				m_button_Input_to_USFM_Click(this, e);
			if (Directory.Exists(UsfmPath))
				m_button_USFM_to_OSIS_Click(this, e);
			// For now OSIS must exist, somehow.
			if (!Directory.Exists(OsisPath))
			{
				MessageBox.Show(this, "Could not find or create required OSIS files", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}
			m_buttonOSIS_to_HTML_Click(this, e);
			m_buttonHTML_to_XHTML_Click(this, e);
			m_bookNameButton_Click(this, e);
			m_buttonChapIndex_Click(this, e);
			m_runButton_Click(this, e);
			btnCopySupportFiles_Click(this, e);
		}

		private void btnCopySupportFiles_Click(object sender, EventArgs e)
		{
			string supportDir = m_options.SupportFilesPath;
			foreach (string path in Directory.GetFiles(supportDir))
			{
				string fileName = Path.GetFileName(path);
				if (fileName == "index.htm")
				{
					// This one goes in a different directory. Also make two copies, one for use as a stand-alone,
					// and one when as part of a larger site.
					DoCopy(path, Path.Combine(m_siteDir, fileName));
					DoCopy(path, Path.Combine(m_siteDir, "index.html"));
				}
				else
				{
					DoCopy(path, Path.Combine(ConcPath, fileName));
				}
			}
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

	}
}