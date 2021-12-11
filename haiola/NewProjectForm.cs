using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using haiola;

namespace WordSend
{
    public partial class NewProjectForm : Form
    {
        public NewProjectForm()
        {
            InitializeComponent();
            doneButton.Enabled = false;
            inputPath = hf.globe.inputDirectory;
        }

        public haiolaForm hf = haiolaForm.MasterInstance;
        private string inputPath;
        public string newProjectName = "";
        private string configPath = "";

        private void cancelButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private bool validateShortName(string shortName)
        {
            bool result = false;
            if (!String.IsNullOrEmpty(shortName))
            {
                configPath = Path.Combine(inputPath, shortName);
                if (!Path.IsPathRooted(shortName) && !Directory.Exists(configPath) && !File.Exists(configPath))
                        result = true;
            }
            doneButton.Enabled = result;
            return result;
        }



        private void translationShortIDTextBox_TextChanged(object sender, EventArgs e)
        {
            string s;
            string replacement = "";
            var regexSearch = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            var r = new Regex(string.Format("[{0}]", Regex.Escape(regexSearch)));
            s = r.Replace(translationShortIDTextBox.Text, replacement);
            if (s != translationShortIDTextBox.Text)
                translationShortIDTextBox.Text = s;
            if (validateShortName(translationShortIDTextBox.Text))
            {
                projectDirectoryLabel.Text = "Project configuration will be stored in " + configPath;
            }
            else
            {
                projectDirectoryLabel.Text = "Please enter a valid NEW directory name for the project ID.";
            }

        }

        private void doneButton_Click(object sender, EventArgs e)
        {
            if (validateShortName(translationShortIDTextBox.Text))
            {
                newProjectName = translationShortIDTextBox.Text;
                configPath = Path.Combine(inputPath, newProjectName);
                Directory.CreateDirectory(configPath);
                Close();
            }
            else
            {
                MessageBox.Show("Invalid name for short translation ID and directory.");
            }

        }
    }
}
