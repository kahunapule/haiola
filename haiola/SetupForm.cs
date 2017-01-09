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
            projectDirectoryLabel.Text = hf.dataRootDir;
            paratextDirLabel.Text = hf.paratextProjectsDir;
            loadFCBHKeysCheckBox.Checked = hf.getFCBHkeys;
            loadFCBHKeysCheckBox.Visible = hf.plugin.PluginLoaded();
            coprLabel.Text = String.Format("Haiola version {0}.{1} ©2003-{2} SIL, EBT, && eBible.org. Released under Gnu LGPL 3 or later.",
                haiola.Version.date, haiola.Version.time, haiola.Version.year); 
            extensionLabel.Text = hf.plugin.PluginMessage();
            swordSuffixTextBox.Text = hf.m_swordSuffix;
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
                projectDirectoryLabel.Text = hf.dataRootDir;
            }
        }

        private void findParatextButton_Click(object sender, EventArgs e)
        {
            hf.SaveOptions();
            FolderBrowserDialog dlg = new FolderBrowserDialog();
            dlg.SelectedPath = hf.paratextProjectsDir;
            dlg.Description =
                @"Please select your existing Paratext Projects folder.";
            dlg.ShowNewFolderButton = true;
            if ((dlg.ShowDialog() != System.Windows.Forms.DialogResult.OK) || (dlg.SelectedPath == null))
                return;
            if (File.Exists(Path.Combine(dlg.SelectedPath, "usfm.sty")))
            {
                hf.paratextProjectsDir = dlg.SelectedPath;
                paratextDirLabel.Text = hf.paratextProjectsDir;
                hf.xini.Write();
                hf.LoadParatextProjectList();
            }
        }

        private void loadFCBHKeysCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            hf.getFCBHkeys = loadFCBHKeysCheckBox.Checked;
            hf.xini.WriteBool("downloadFcbhAudio", hf.getFCBHkeys);
            hf.xini.Write();
        }

        private void closeButton_Click(object sender, EventArgs e)
        {
            hf.m_swordSuffix = swordSuffixTextBox.Text;
            hf.xini.Write();
            Close();
        }

    }
}
