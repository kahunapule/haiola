using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace sepp
{
	public partial class Master : Form
	{
		public Master()
		{
			InitializeComponent();
		}

		string m_workDirectory = @"c:\BibleConv\Work";
		string m_siteDirectory; // curently c:\BibleConv\Site

		private void Master_Load(object sender, EventArgs e)
		{
			foreach (string path in Directory.GetDirectories(m_workDirectory))
			{
				m_projectsList.Items.Add(Path.GetFileName(path));
			}
			for (int i = 0; i < m_projectsList.Items.Count; i++)
				m_projectsList.SetItemChecked(i, true);
			m_projectsList.SetSelected(0, true);
			m_siteDirectory = Path.Combine(Path.GetDirectoryName(m_workDirectory), "Site");
		}

		private void ProjectButton_Click(object sender, EventArgs e)
		{
			string project = m_projectsList.SelectedItem as string;
			string projectWorkPath = Path.Combine(m_workDirectory, project);
			string projectSitePath = Path.Combine(m_siteDirectory, project);
			ProjectManager manager = new ProjectManager(projectWorkPath, projectSitePath);
			manager.ShowDialog();
		}

		private void MasterIndexButton_Click(object sender, EventArgs e)
		{
			string m_outputDirName = Path.Combine(m_siteDirectory, "Resources");
			Utils.EnsureDirectory(m_outputDirName);
			string header = "<!doctype HTML PUBLIC \"-//W3C//DTD HTML 4.0 Transitional//EN\">\n<html>\n"
				+ "<link rel=\"stylesheet\" href=\"display.css\" type=\"text/css\">"
				+ "<head>"
				+ "</head>\n<body class=\"LanguageIndex\">\n<ul>";
			string trailer = "</ul></body>\n</html>\n";
			string path = Path.Combine(m_outputDirName, "LanguageIndex.htm");
			TextWriter writer = new StreamWriter(path, false, Encoding.UTF8);
			writer.Write(header);

			Progress status = new Progress(m_projectsList.CheckedItems.Count);
			status.Show();
			int count = 0;

			foreach (string projectName in m_projectsList.CheckedItems)
			{
				status.File = projectName;

				string name = Utils.MakeSafeXml(projectName);
				writer.Write("<li><a target=\"body\" href=\"..\\" + name + "\\Conc\\root.htm\">" + name + "</a></li>\n");

				count++;
				status.Value = count;
			}
			writer.Write(trailer);
			writer.Close();

			status.Close();
		}
	}
}