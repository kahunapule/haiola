using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace sepp
{
	/// <summary>
	/// Manage progress. Enhance: add cancel button
	/// </summary>
	public partial class Progress : Form
	{
		// Make one
		public Progress(int range)
		{
			InitializeComponent();
			m_progressBar.Maximum = range;
		}

		public string File
		{
			set { m_bookLabel.Text = value; Update(); }
		}

		public int Value
		{
			set { m_progressBar.Value = value; Update(); }
		}

	}
}