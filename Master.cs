using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using Microsoft.Win32;

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

		bool GetRootDirectory()
		{
			FolderBrowserDialog dlg = new FolderBrowserDialog();
			dlg.Description =
				@"Select a root dialog (default is C:\BibleConv) to contain your working directories and the root of your prototype site.";
			dlg.ShowNewFolderButton = true;
			if (dlg.ShowDialog() != System.Windows.Forms.DialogResult.OK)
				return false;
			m_workDirectory = Path.Combine(dlg.SelectedPath, "Work");
			return true;
		}

		private void Master_Load(object sender, EventArgs e)
		{
			RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\SIL\Prophero", true);
			if (key == null || key.GetValue("Warning") == null)
			{
				WarningSplash dlg = new WarningSplash();
				dlg.ShowDialog(this);
				if (dlg.DoNotShowAgain)
				{
					if (key == null)
						key = Registry.CurrentUser.CreateSubKey(@"Software\SIL\Prophero");
					key.SetValue("Warning", "no");
				}
			}
			if (!Directory.Exists(m_workDirectory))
				if (!GetRootDirectory())
					Application.Exit();
			LoadWorkingDirectory();
		}

		private void LoadWorkingDirectory()
		{
			m_projectsList.Items.Clear();
			if (!Directory.Exists(m_workDirectory))
				Directory.CreateDirectory(m_workDirectory);
			foreach (string path in Directory.GetDirectories(m_workDirectory))
			{
				m_projectsList.Items.Add(Path.GetFileName(path));
			}
			for (int i = 0; i < m_projectsList.Items.Count; i++)
				m_projectsList.SetItemChecked(i, true);
			if (m_projectsList.Items.Count != 0)
			{
				m_projectsList.SetSelected(0, true);
				ProjectButton.Enabled = true;
			}
			else
			{
				MessageBox.Show(this, "No projects found in " + m_workDirectory
									  +
									  ". You should create a folder there for your project and place your input files in the appropriate subdirectory.",
								"No Projects", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				ProjectButton.Enabled = false;
			}
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

		private void btnSetRootDirectory_Click(object sender, EventArgs e)
		{
			if (GetRootDirectory())
				LoadWorkingDirectory();
		}

        private void m_btnSplitFile_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
			dlg.CheckFileExists = true;
			dlg.InitialDirectory = m_workDirectory;
			//dlg.Multiselect = true;
			dlg.Filter = "SFM files(*.sfm)|*.sfm|All files|*.*";
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                string sourceFilePath = dlg.FileName;
                string sourceDir = Path.GetDirectoryName(sourceFilePath);
                string sourceFileName = Path.GetFileName(sourceFilePath);
                string destDir = Path.Combine(sourceDir, "Source");
                Utils.EnsureDirectory(destDir);
                string prefix = sourceFileName.Substring(0, 3);
                StreamReader source = new StreamReader(sourceFilePath, Encoding.UTF8);
                StreamWriter writer = null;
                while (!source.EndOfStream)
                {
                    string line = source.ReadLine();
                    if (line.StartsWith("\\id "))
                    {
                        if (writer != null)
                            writer.Close();
                        string outputPath = Path.Combine(destDir, prefix + "-" + line.Substring(4).Trim() + ".sfm");
                        writer = new StreamWriter(outputPath, false, Encoding.UTF8);
                    }
                    writer.WriteLine(line);
                }
                if (writer != null)
                    writer.Close();
            }
        }
	}
}