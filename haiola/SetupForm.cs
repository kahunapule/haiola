using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using haiola;

namespace WordSend
{
    public partial class SetupForm : Form
    {
        public haiolaForm hf = haiolaForm.MasterInstance;

        public SetupForm()
        {
            InitializeComponent();
            projectDirectoryLabel.Text = hf.globe.dataRootDir;
            RebuildCheckBox.Checked = hf.globe.rebuild;
            // runXetexCheckBox.Checked = hf.globe.runXetex;
            coprLabel.Text = String.Format("Haiola version {0}.{1} ©2003-{2} SIL, EBT, && eBible.org. Released under Gnu LGPL 3 or later.",
                haiola.Version.date, haiola.Version.time, haiola.Version.year); 
            //extensionLabel.Text = hf.plugin.PluginMessage();
            swordSuffixTextBox.Text = hf.globe.m_swordSuffix;
            usfm3figTagsCheckBox.Checked = hf.globe.generateUsfm3Fig;

        }


        /// <summary>
        /// Event handler for Project Directory setting button
        /// </summary>
        /// <param name="sender">not used</param>
        /// <param name="e">not used</param>
        private void projectDirectoryButton_Click(object sender, EventArgs e)
        {
            hf.SaveOptions();
            if (hf.GetRootDirectory())
            {
                hf.LoadWorkingDirectory(true, false, false);
                projectDirectoryLabel.Text = hf.globe.dataRootDir;
            }
        }

        private void findParatextButton_Click(object sender, EventArgs e)
        {
            hf.SaveOptions();
            FolderBrowserDialog dlg = new FolderBrowserDialog();
            dlg.SelectedPath = hf.globe.paratextProjectsDir;
            dlg.Description =
                @"Please select your existing Paratext Projects folder.";
            dlg.ShowNewFolderButton = true;
            if ((dlg.ShowDialog() != System.Windows.Forms.DialogResult.OK) || (dlg.SelectedPath == null))
                return;
            if (File.Exists(Path.Combine(dlg.SelectedPath, "usfm.sty")))
            {
                hf.globe.paratextProjectsDir = dlg.SelectedPath;
                hf.globe.xini.Write();
            }
        }


        private void closeButton_Click(object sender, EventArgs e)
        {
            hf.globe.m_swordSuffix = swordSuffixTextBox.Text;
            hf.globe.generateUsfm3Fig = usfm3figTagsCheckBox.Checked;
            hf.globe.rebuild = RebuildCheckBox.Checked;
            // hf.globe.runXetex = runXetexCheckBox.Checked;
            hf.globe.xini.Write();
            Close();
        }

        private void findParatext8Button_Click(object sender, EventArgs e)
        {
            hf.SaveOptions();
            FolderBrowserDialog dlg = new FolderBrowserDialog();
            dlg.SelectedPath = hf.globe.paratext8ProjectsDir;
            dlg.Description =
                @"Please select your existing Paratext 8 Projects folder.";
            dlg.ShowNewFolderButton = true;
            if ((dlg.ShowDialog() != System.Windows.Forms.DialogResult.OK) || (dlg.SelectedPath == null))
                return;
            if (File.Exists(Path.Combine(dlg.SelectedPath, "usfm.sty")))
            {
                hf.globe.paratext8ProjectsDir = dlg.SelectedPath;
                hf.globe.xini.Write();
            }

        }

    }
}
