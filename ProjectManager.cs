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
		}

		private void SetupPreprocessing(XmlNode node)
		{
			m_button_OW_to_USFM.Text = "Preprocess";
			m_tablePaths = new string[node.ChildNodes.Count];
			for (int i = 0; i < m_tablePaths.Length; i++)
				m_tablePaths[i] = Path.Combine(m_workDir,node.ChildNodes[i].InnerText);
		}

		CheckedListBox.CheckedItemCollection ActiveFiles
		{
			get { return m_filesList.CheckedItems; }
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
			foreach (XmlNode item in node.ChildNodes)
			{
				string fileName = item.Attributes["name"].Value;
				m_filesList.Items.Add(fileName, CheckState.Checked);
			}
		}

		private void m_runButton_Click(object sender, EventArgs e)
		{
			ConcGenerator generator = new ConcGenerator(
				Path.Combine(m_workDir, @"ConcInput"), Path.Combine(m_siteDir, @"Conc"), m_optionsPath);
			generator.Run(m_filesList.CheckedItems);
		}

		private void m_button_OW_to_USFM_Click(object sender, EventArgs e)
		{
			string srcDir = "OW";
			if (m_tablePaths != null)
				srcDir = "Source";
			OW_To_USFM converter = new OW_To_USFM(Path.Combine(m_workDir, srcDir), Path.Combine(m_workDir, @"USFM"));
			if (m_tablePaths != null)
			{
				converter.TablePaths = m_tablePaths;
			}
			converter.Run(m_filesList.CheckedItems);

		}

		private void m_button_USFM_to_OSIS_Click(object sender, EventArgs e)
		{
			USFM_to_OSIS converter = new USFM_to_OSIS(Path.Combine(m_workDir, @"USFM"), Path.Combine(m_workDir, @"OSIS"), m_optionsPath);
			converter.Run(m_filesList.CheckedItems);
		}

		private void m_buttonOSIS_to_HTML_Click(object sender, EventArgs e)
		{
			OSIS_to_HTML converter = new OSIS_to_HTML(
				Path.Combine(m_workDir, @"OSIS"), Path.Combine(m_workDir, @"HTML"),
				Path.Combine(m_siteDir, @"Conc"), Path.Combine(m_workDir, @"Sepp Options.xml"));
			converter.Run(m_filesList.CheckedItems);

		}

		private void m_buttonHTML_to_XHTML_Click(object sender, EventArgs e)
		{
			HTML_TO_XHTML converter = new HTML_TO_XHTML(Path.Combine(m_workDir, @"HTML"), Path.Combine(m_workDir, @"ConcInput"));
			converter.Run(m_filesList.CheckedItems);

		}

		private void m_buttonChapIndex_Click(object sender, EventArgs e)
		{
			OSIS_to_ChapIndex generator = new OSIS_to_ChapIndex(Path.Combine(m_workDir, @"OSIS"), Path.Combine(m_siteDir, @"Conc"),
				Path.Combine(m_workDir, @"Intro"), Path.Combine(m_workDir, @"Extras"),
				Path.Combine(m_workDir, @"Sepp Options.xml"));
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

	}
}