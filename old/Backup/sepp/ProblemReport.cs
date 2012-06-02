using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace sepp
{
	public partial class ProblemReport : Form
	{
		public ProblemReport()
		{
			InitializeComponent();
		}

		public string ReportContents
		{
			get { return m_reportContents.Text; }
			set { m_reportContents.Text = value; }
		}
	}
}