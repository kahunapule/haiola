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
	public partial class Form1 : Form
	{
		string m_optionsPath = @"C:\BibleConv\Sepp Options.xml";
		string m_rootDir = @"C:\BibleConv";
		public Form1()
		{
			InitializeComponent();

			XmlDocument optionsDoc = new XmlDocument();
			optionsDoc.Load(m_optionsPath);
			XmlNode root = optionsDoc.DocumentElement;
			foreach (XmlNode node in root.ChildNodes)
			{
				switch (node.Name)
				{
					case "files":
						BuildFileList(node);
						break;
				}
			}
		}

		CheckedListBox.CheckedItemCollection ActiveFiles
		{
			get { return m_filesList.CheckedItems; }
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
				Path.Combine(m_rootDir, @"ConcInput"), Path.Combine(m_rootDir, @"Conc"), m_optionsPath);
			generator.Run(m_filesList.CheckedItems);
		}

		private void m_button_OW_to_USFM_Click(object sender, EventArgs e)
		{
			OW_To_USFM converter = new OW_To_USFM(Path.Combine(m_rootDir, @"OW"), Path.Combine(m_rootDir, @"USFM"));
			converter.Run(m_filesList.CheckedItems);

		}

		private void m_button_USFM_to_OSIS_Click(object sender, EventArgs e)
		{
			USFM_to_OSIS converter = new USFM_to_OSIS(Path.Combine(m_rootDir, @"USFM"), Path.Combine(m_rootDir, @"OSIS"));
			converter.Run(m_filesList.CheckedItems);
		}

		private void m_buttonOSIS_to_HTML_Click(object sender, EventArgs e)
		{
			OSIS_to_HTML converter = new OSIS_to_HTML(
				Path.Combine(m_rootDir, @"OSIS"), Path.Combine(m_rootDir, @"HTML"),
				Path.Combine(m_rootDir, @"Conc"), Path.Combine(m_rootDir, @"Sepp Options.xml"));
			converter.Run(m_filesList.CheckedItems);

		}

		private void m_buttonHTML_to_XHTML_Click(object sender, EventArgs e)
		{
			HTML_TO_XHTML converter = new HTML_TO_XHTML(Path.Combine(m_rootDir, @"HTML"), Path.Combine(m_rootDir, @"ConcInput"));
			converter.Run(m_filesList.CheckedItems);

		}

		private void m_buttonChapIndex_Click(object sender, EventArgs e)
		{
			OSIS_to_ChapIndex generator = new OSIS_to_ChapIndex(Path.Combine(m_rootDir, @"OSIS"), Path.Combine(m_rootDir, @"Conc"),
				Path.Combine(m_rootDir, @"Intro"), Path.Combine(m_rootDir, @"Extras"),
				Path.Combine(m_rootDir, @"Sepp Options.xml"));
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

	}
}