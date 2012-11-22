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
	public partial class WarningSplash : Form
	{
		public WarningSplash()
		{
			InitializeComponent();
		}

		public Boolean DoNotShowAgain { get; set; }

		private void btnOK_Click(object sender, EventArgs e)
		{
			DoNotShowAgain = m_chkDoNotShowAgain.Checked;
			this.Close();
		}

        private void helpButton_Click(object sender, EventArgs e)
        {
            string helpFilePath = @"http://haiola.org/haiola.htm";
            try
            {
                string safari = @"/Applications/Safari.app/Contents/MacOS/Safari";
                if (File.Exists(safari))
                {
                    System.Diagnostics.Process.Start(safari, helpFilePath);
                }
                else
                {
                    System.Diagnostics.Process.Start(helpFilePath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error displaying " + helpFilePath);
            }
            
        }
	}
}
