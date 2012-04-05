using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using Microsoft.Win32;
using WordSend;

namespace sepp
{
	public partial class Master : Form
	{
        public static Master MasterInstance;
        private XMLini xini;
        public string dataRootDir; // Default is BibleConv in the user's documents folder
        string m_inputDirectory;
        public string m_siteDirectory; // curently Site, always under dataRootDir
        public bool autorun = false;

		public Master()
		{
			InitializeComponent();
            MasterInstance = this;
		}


		bool GetRootDirectory()
		{
			FolderBrowserDialog dlg = new FolderBrowserDialog();
			dlg.Description =
				@"Select a root dialog (default is BibleConv in your Documents directory) to contain your input and output directories.";
			dlg.ShowNewFolderButton = true;
			if (dlg.ShowDialog() != System.Windows.Forms.DialogResult.OK)
				return false;
            dataRootDir = dlg.SelectedPath;
            xini.WriteString("dataRootDir", dataRootDir);
            xini.Write();
            LoadWorkingDirectory();
            return true;
		}

		private void Master_Load(object sender, EventArgs e)
		{
            xini = new XMLini(Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "haiola"),
                "haiola.xini"));
            dataRootDir = xini.ReadString("dataRootDir", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BibleConv"));
            if (xini.ReadBool("Warning", true))
			{
				WarningSplash dlg = new WarningSplash();
				dlg.ShowDialog(this);
				if (dlg.DoNotShowAgain)
				{
                    xini.WriteBool("Warning", false);
                    xini.Write();
				}
			}
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

		private void LoadWorkingDirectory()
		{
            int projCount = 0;
            int projReady = 0;
            m_projectsList.BeginUpdate();
			m_projectsList.Items.Clear();
            m_inputDirectory = Path.Combine(dataRootDir, "input");
            Utils.EnsureDirectory(m_inputDirectory);
            workDirLabel.Text = m_inputDirectory;
			foreach (string path in Directory.GetDirectories(m_inputDirectory))
			{
				m_projectsList.Items.Add(Path.GetFileName(path));
                projCount++;
                if (File.Exists(Path.Combine(path, "Sepp Options.xml")))
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
				ProjectButton.Enabled = true;
			}
			else
			{
				MessageBox.Show(this, "No projects found in " + m_inputDirectory
									  +
									  ". You should create a folder there for your project and place your input files in the appropriate subdirectory.",
								"No Projects", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				ProjectButton.Enabled = false;
			}
		}

		private void ProjectButton_Click(object sender, EventArgs e)
		{
			string project = m_projectsList.SelectedItem as string;
			string projectWorkPath = Path.Combine(m_inputDirectory, project);
			string projectSitePath = Path.Combine(m_siteDirectory, project);
			ProjectManager manager = new ProjectManager(projectWorkPath, projectSitePath, project);
			manager.ShowDialog();
		}

		private void MasterIndexButton_Click(object sender, EventArgs e)
		{
            /* Removed because I don't want to use or support this function.
             * Feel free to put it back if you do...
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
             */
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
			dlg.InitialDirectory = m_inputDirectory;
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

        static bool fAllRunning = false;

        private void WorkOnAllButton_Click(object sender, EventArgs e)
        {
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
            ProjectButton.Enabled = false;
            foreach (object o in m_projectsList.CheckedItems)
            {
                // m_projectsList.SelectedIndex = m_projectsList.Items.IndexOf(o);
                string project = (string)o;
                batchLabel.Text = project;
                Application.DoEvents();
                string projectWorkPath = Path.Combine(m_inputDirectory, project);
                string projectSitePath = Path.Combine(m_siteDirectory, project);
                ProjectManager manager = new ProjectManager(projectWorkPath, projectSitePath, project);
                manager.automaticRun();
                Application.DoEvents();
                if (!fAllRunning)
                    break;
            }
            fAllRunning = false;
            batchLabel.Text = "Stopped.";
            ProjectButton.Enabled = true;
            WorkOnAllButton.Enabled = true;
            WorkOnAllButton.Text = "Run marked";
/*
            int i;
            for (i = 0; i < m_projectsList.Items.Count; i++)
            {
                m_projectsList.SelectedIndex = i;
                Application.DoEvents();
                object o_project = m_projectsList.SelectedItem;
                string project = (string)o_project;
                string projectWorkPath = Path.Combine(m_inputDirectory, project);
                string projectSitePath = Path.Combine(m_siteDirectory, project);
                ProjectManager manager = new ProjectManager(projectWorkPath, projectSitePath);
                manager.Show();
                manager.automaticRun();
            }
*/
        }

        private void reloadButton_Click(object sender, EventArgs e)
        {
            LoadWorkingDirectory();
        }

        private void checkAllButton_Click(object sender, EventArgs e)
        {
            int i;
            for (i = 0; i < m_projectsList.Items.Count; i++)
                m_projectsList.SetItemChecked(i, true);
        }

        private void unmarkAllButton_Click(object sender, EventArgs e)
        {
            int i;
            for (i = 0; i < m_projectsList.Items.Count; i++)
                m_projectsList.SetItemChecked(i, false);
        }


	}
}