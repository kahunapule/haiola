using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace sepp
{
	public partial class Form1 : Form
	{
		public Form1()
		{
			InitializeComponent();
		}

		private void m_runButton_Click(object sender, EventArgs e)
		{
			ConcGenerator generator = new ConcGenerator(@"c:\BibleConv\ConcInput", @"c:\BibleConv\ConcOutput", @"C:\BibleConv\Sepp Options.xml");
			generator.Run();
		}

		private void m_button_OW_to_USFM_Click(object sender, EventArgs e)
		{
			OW_To_USFM converter = new OW_To_USFM(@"c:\BibleConv\OW", @"c:\BibleConv\USFM");
			converter.Run();

		}

		private void m_button_USFM_to_OSIS_Click(object sender, EventArgs e)
		{
			USFM_to_OSIS converter = new USFM_to_OSIS(@"c:\BibleConv\USFM", @"c:\BibleConv\OSIS");
			converter.Run();
		}

		private void m_buttonOSIS_to_HTML_Click(object sender, EventArgs e)
		{
			OSIS_to_HTML converter = new OSIS_to_HTML(@"c:\BibleConv\OSIS", @"c:\BibleConv\HTML");
			converter.Run();

		}

		private void m_buttonHTML_to_XHTML_Click(object sender, EventArgs e)
		{
			HTML_TO_XHTML converter = new HTML_TO_XHTML(@"c:\BibleConv\HTML", @"c:\BibleConv\ConcInput");
			converter.Run();

		}

		private void m_buttonChapIndex_Click(object sender, EventArgs e)
		{
			OSIS_to_ChapIndex generator = new OSIS_to_ChapIndex(@"c:\BibleConv\OSIS", @"c:\BibleConv\ConcOutput", @"C:\BibleConv\Sepp Options.xml");
			generator.Run();

		}

	}
}