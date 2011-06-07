using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

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
	}
}
