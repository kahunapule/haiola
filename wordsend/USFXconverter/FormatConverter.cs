using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;

namespace WordSend
{
	/// <summary>
	/// Summary description for Form1.
	/// </summary>
	public class USFXForm : System.Windows.Forms.Form
	{
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Button browseInputButton;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Button browseOutputButton;
		private System.Windows.Forms.Button convertButton;
		private System.Windows.Forms.TextBox inputNameTextBox;
		private System.Windows.Forms.TextBox outputTextBox;
		private System.Windows.Forms.Label typicalLabel;
		private System.Windows.Forms.Label suffixLabel;
		private System.Windows.Forms.Label messageLabel;
		private System.Windows.Forms.ListBox statusListBox;
		private System.Windows.Forms.ListBox sourceFormatListBox;
		private System.Windows.Forms.ListBox destinationFormatListBox;
		private System.Windows.Forms.TextBox outNameTextBox;
		private System.Windows.Forms.Button optionsButton;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		const int USFM = 0;
		const int USFX = 1;
		const int WORDML = 2;
		const int OSIS = 3;
		const int XSEM = 4;
		const int GBF = 5;
		const int HTML = 6;
		const int TEX = 7;
		private System.Windows.Forms.OpenFileDialog openFileDialog1;
		private System.Windows.Forms.SaveFileDialog saveFileDialog1;
		private System.Windows.Forms.Label label5;
		const int TEXT = 8;

		public USFXForm()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
			AdjustUI();
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if (components != null) 
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(USFXForm));
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.inputNameTextBox = new System.Windows.Forms.TextBox();
            this.browseInputButton = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.outputTextBox = new System.Windows.Forms.TextBox();
            this.browseOutputButton = new System.Windows.Forms.Button();
            this.typicalLabel = new System.Windows.Forms.Label();
            this.outNameTextBox = new System.Windows.Forms.TextBox();
            this.convertButton = new System.Windows.Forms.Button();
            this.statusListBox = new System.Windows.Forms.ListBox();
            this.suffixLabel = new System.Windows.Forms.Label();
            this.messageLabel = new System.Windows.Forms.Label();
            this.sourceFormatListBox = new System.Windows.Forms.ListBox();
            this.destinationFormatListBox = new System.Windows.Forms.ListBox();
            this.optionsButton = new System.Windows.Forms.Button();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.saveFileDialog1 = new System.Windows.Forms.SaveFileDialog();
            this.label5 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // label2
            // 
            this.label2.Location = new System.Drawing.Point(352, 8);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(24, 16);
            this.label2.TabIndex = 6;
            this.label2.Text = "to:";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(8, 8);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(184, 16);
            this.label1.TabIndex = 4;
            this.label1.Text = "Convert Scripture file format from:";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // inputNameTextBox
            // 
            this.inputNameTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.inputNameTextBox.Location = new System.Drawing.Point(8, 112);
            this.inputNameTextBox.Name = "inputNameTextBox";
            this.inputNameTextBox.Size = new System.Drawing.Size(576, 20);
            this.inputNameTextBox.TabIndex = 8;
            // 
            // browseInputButton
            // 
            this.browseInputButton.Location = new System.Drawing.Point(8, 32);
            this.browseInputButton.Name = "browseInputButton";
            this.browseInputButton.Size = new System.Drawing.Size(96, 24);
            this.browseInputButton.TabIndex = 9;
            this.browseInputButton.Text = "&Browse Input";
            this.browseInputButton.Click += new System.EventHandler(this.browseInputButton_Click);
            // 
            // label3
            // 
            this.label3.Location = new System.Drawing.Point(8, 56);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(64, 24);
            this.label3.TabIndex = 10;
            this.label3.Text = "Input file(s):";
            this.label3.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
            // 
            // label4
            // 
            this.label4.Location = new System.Drawing.Point(8, 144);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(88, 16);
            this.label4.TabIndex = 11;
            this.label4.Text = "Output directory:";
            this.label4.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
            // 
            // outputTextBox
            // 
            this.outputTextBox.Location = new System.Drawing.Point(8, 168);
            this.outputTextBox.Name = "outputTextBox";
            this.outputTextBox.Size = new System.Drawing.Size(576, 20);
            this.outputTextBox.TabIndex = 12;
            // 
            // browseOutputButton
            // 
            this.browseOutputButton.Location = new System.Drawing.Point(96, 144);
            this.browseOutputButton.Name = "browseOutputButton";
            this.browseOutputButton.Size = new System.Drawing.Size(96, 24);
            this.browseOutputButton.TabIndex = 13;
            this.browseOutputButton.Text = "Browse &Output";
            // 
            // typicalLabel
            // 
            this.typicalLabel.Location = new System.Drawing.Point(168, 200);
            this.typicalLabel.Name = "typicalLabel";
            this.typicalLabel.Size = new System.Drawing.Size(192, 24);
            this.typicalLabel.TabIndex = 15;
            this.typicalLabel.Text = "Typical output file name: 19-PSA-23-";
            this.typicalLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // outNameTextBox
            // 
            this.outNameTextBox.Location = new System.Drawing.Point(360, 200);
            this.outNameTextBox.Name = "outNameTextBox";
            this.outNameTextBox.Size = new System.Drawing.Size(152, 20);
            this.outNameTextBox.TabIndex = 16;
            this.outNameTextBox.Text = "out";
            // 
            // convertButton
            // 
            this.convertButton.Enabled = false;
            this.convertButton.Location = new System.Drawing.Point(432, 232);
            this.convertButton.Name = "convertButton";
            this.convertButton.Size = new System.Drawing.Size(144, 24);
            this.convertButton.TabIndex = 17;
            this.convertButton.Text = "Convert now";
            this.convertButton.Click += new System.EventHandler(this.convertButton_Click);
            // 
            // statusListBox
            // 
            this.statusListBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.statusListBox.Items.AddRange(new object[] {
            "Under construction...",
            "check for an update at http://eBible.org/wordsend/"});
            this.statusListBox.Location = new System.Drawing.Point(0, 264);
            this.statusListBox.Name = "statusListBox";
            this.statusListBox.Size = new System.Drawing.Size(592, 147);
            this.statusListBox.TabIndex = 18;
            // 
            // suffixLabel
            // 
            this.suffixLabel.Location = new System.Drawing.Point(512, 200);
            this.suffixLabel.Name = "suffixLabel";
            this.suffixLabel.Size = new System.Drawing.Size(72, 24);
            this.suffixLabel.TabIndex = 19;
            this.suffixLabel.Text = ".txt";
            this.suffixLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // messageLabel
            // 
            this.messageLabel.Location = new System.Drawing.Point(200, 136);
            this.messageLabel.Name = "messageLabel";
            this.messageLabel.Size = new System.Drawing.Size(384, 32);
            this.messageLabel.TabIndex = 20;
            this.messageLabel.Text = "Please select formats to convert from and to.";
            this.messageLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // sourceFormatListBox
            // 
            this.sourceFormatListBox.Items.AddRange(new object[] {
            "USFM",
            "USFX",
            "WordML+USFX",
            "OSIS",
            "XSEM",
            "GBF"});
            this.sourceFormatListBox.Location = new System.Drawing.Point(200, 0);
            this.sourceFormatListBox.Name = "sourceFormatListBox";
            this.sourceFormatListBox.Size = new System.Drawing.Size(152, 69);
            this.sourceFormatListBox.TabIndex = 21;
            this.sourceFormatListBox.SelectedIndexChanged += new System.EventHandler(this.sourceFormatListBox_SelectedIndexChanged);
            // 
            // destinationFormatListBox
            // 
            this.destinationFormatListBox.Items.AddRange(new object[] {
            "USFM",
            "USFX",
            "WordML or WordML+USFX",
            "OSIS",
            "XSEM",
            "GBF",
            "HTML",
            "TEX",
            "TEXT"});
            this.destinationFormatListBox.Location = new System.Drawing.Point(376, 0);
            this.destinationFormatListBox.Name = "destinationFormatListBox";
            this.destinationFormatListBox.Size = new System.Drawing.Size(200, 69);
            this.destinationFormatListBox.TabIndex = 22;
            this.destinationFormatListBox.SelectedIndexChanged += new System.EventHandler(this.sourceFormatListBox_SelectedIndexChanged);
            // 
            // optionsButton
            // 
            this.optionsButton.Location = new System.Drawing.Point(240, 232);
            this.optionsButton.Name = "optionsButton";
            this.optionsButton.Size = new System.Drawing.Size(136, 24);
            this.optionsButton.TabIndex = 23;
            this.optionsButton.Text = "Options";
            this.optionsButton.Click += new System.EventHandler(this.optionsButton_Click);
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.AddExtension = false;
            // 
            // label5
            // 
            this.label5.BackColor = System.Drawing.Color.Yellow;
            this.label5.Font = new System.Drawing.Font("Times New Roman", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.ForeColor = System.Drawing.Color.Red;
            this.label5.Location = new System.Drawing.Point(0, 216);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(232, 48);
            this.label5.TabIndex = 24;
            this.label5.Text = "UI mockup only, not yet functional";
            this.label5.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // USFXForm
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.ClientSize = new System.Drawing.Size(592, 421);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.optionsButton);
            this.Controls.Add(this.destinationFormatListBox);
            this.Controls.Add(this.sourceFormatListBox);
            this.Controls.Add(this.messageLabel);
            this.Controls.Add(this.suffixLabel);
            this.Controls.Add(this.statusListBox);
            this.Controls.Add(this.convertButton);
            this.Controls.Add(this.outNameTextBox);
            this.Controls.Add(this.typicalLabel);
            this.Controls.Add(this.browseOutputButton);
            this.Controls.Add(this.outputTextBox);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.browseInputButton);
            this.Controls.Add(this.inputNameTextBox);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "USFXForm";
            this.Text = "Scripture File Format Converter";
            this.ResumeLayout(false);
            this.PerformLayout();

		}
		#endregion

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main() 
		{
			Application.Run(new USFXForm());
		}

		private void AdjustUI()
		{
			bool ok = true;

			if (sourceFormatListBox.SelectedIndex < 0)
			{
				messageLabel.Text = "Please select the source file format.";
				sourceFormatListBox.BackColor = Color.Yellow;
				ok = false;
			}
			else
			{
				sourceFormatListBox.BackColor = Color.White;
				if (destinationFormatListBox.SelectedIndex < 0)
				{
					ok = false;
					destinationFormatListBox.BackColor = Color.Yellow;
					messageLabel.Text = "Please select the destination file format.";
				}
				else
				{
					destinationFormatListBox.BackColor = Color.White;
					messageLabel.Text = "";
				}
			}
			if (ok)
			{
				browseInputButton.Enabled = true;
				browseOutputButton.Enabled = true;
				outNameTextBox.Enabled = true;
				convertButton.Enabled = true;
				optionsButton.Enabled = true;
			}
			else
			{
				browseInputButton.Enabled = false;
				browseOutputButton.Enabled = false;
				outNameTextBox.Enabled = false;
				optionsButton.Enabled = false;
				convertButton.Enabled = false;
			}

/****************

			switch (sourceFormatListBox.SelectedIndex)
			{
				case USFM:
					openFileDialog1.Filter = "USFM files (*.ptx;*.sf;*.sfm;*.usfm;*.txt)|*.ptx;*.sf;*.sfm;*.usfm;*.txt|All files (*)|*.*;*)";
					break;
				case USFX:
				case WORDML:
				case OSIS:
				case XSEM:
					OpenFileDialog1.Filter = "XML files (*.xml)|*.xml|All files|*.*";
					break;
				case GBF:
					OpenFileDialog1.Filter = "GBF files (*.gbf)|*.gbf|All files|*.*";
					break;
				default:
					OpenFileDialog1.Filter = "All files|*.*";
					break;
			}
			switch (destinationFormatListBox.SelectedIndex)
			{
				case USFM:
					suffixLabel.Text = ".sfm";
					break;
				case USFX:
					suffixLabel.Text = "-usfx.xml";
					break;
				case WORDML:
					suffixLabel.Text = "-msw.xml";
					break;
				case OSIS:
					suffixLabel.Text = "-osis.xml";
					break;
				case XSEM:
					suffixLabel.Text = "-xsem.xml";
					break;
				case GBF:
					suffixLabel.Text = ".gbf";
					break;
				case HTML:
					suffixLabel.Text = ".htm";
					break;
				case TEX:
					suffixLabel.Text = ".tex";
					break;
				case TEXT:
					suffixLabel.Text = ".txt";
					break;
				default:
					suffixLabel.Text = "";
					break;
			}
			
************************/


		}

		public void WriteToStatusListBox(string s)
		{
			statusListBox.Items.Add((object) s);
			statusListBox.SelectedIndex = statusListBox.Items.Count -1;
		}

		private void convertButton_Click(object sender, System.EventArgs e)
		{
			Logit.OpenFile("WordSendLog.txt");
			Logit.GUIWriteString = new StringDelegate(WriteToStatusListBox);

			Logit.WriteLine("Sorry, this program is still under construction.");

			Logit.CloseFile();
		}

		private void sourceFormatListBox_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			AdjustUI();
		}

		private void radioButton1_CheckedChanged(object sender, System.EventArgs e)
		{
			AdjustUI();
		}

		private void browseInputButton_Click(object sender, System.EventArgs e)
		{
			openFileDialog1.FileName = "";
			openFileDialog1.Title = "Please select input file(s).";
			openFileDialog1.Multiselect = true;
			if (openFileDialog1.ShowDialog() == DialogResult.OK)
			{
				SFConverter.jobIni.WriteString("usfmDir",
					System.IO.Path.GetDirectoryName(openFileDialog1.FileNames[0]));
//				usfmFileListBox.Items.AddRange(openFileDialog1.FileNames);
			}

		}

		private void optionsButton_Click(object sender, System.EventArgs e)
		{
			MessageBox.Show("This is a test. If this had been a real functional program, something useful would have happened when you pressed that button.");
		}

	}
}
