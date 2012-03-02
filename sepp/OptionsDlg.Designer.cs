namespace sepp
{
	partial class OptionsDlg
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(OptionsDlg));
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.licenseTextBox = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.indexPageTextBox = new System.Windows.Forms.TextBox();
            this.footerHtmlTextBox = new System.Windows.Forms.TextBox();
            this.homeLinkTextBox = new System.Windows.Forms.TextBox();
            this.copyrightLinkTextBox = new System.Windows.Forms.TextBox();
            this.label28 = new System.Windows.Forms.Label();
            this.label27 = new System.Windows.Forms.Label();
            this.label26 = new System.Windows.Forms.Label();
            this.tabMisc = new System.Windows.Forms.TabPage();
            this.ignoreExtrasCheckBox = new System.Windows.Forms.CheckBox();
            this.KhmerNumeralsRadioButton = new System.Windows.Forms.RadioButton();
            this.ArabicNumeralsRadioButton = new System.Windows.Forms.RadioButton();
            this.languageNameTextBox = new System.Windows.Forms.TextBox();
            this.ethnologueCodeTextBox = new System.Windows.Forms.TextBox();
            this.label25 = new System.Windows.Forms.Label();
            this.label24 = new System.Windows.Forms.Label();
            this.btnRemoveInputProcess = new System.Windows.Forms.Button();
            this.btnAddInputProcess = new System.Windows.Forms.Button();
            this.listInputProcesses = new System.Windows.Forms.ListBox();
            this.label22 = new System.Windows.Forms.Label();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1.SuspendLayout();
            this.tabMisc.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.SuspendLayout();
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "File";
            this.columnHeader1.Width = 327;
            // 
            // columnHeader2
            // 
            this.columnHeader2.Text = "Hotlink Text";
            this.columnHeader2.Width = 321;
            // 
            // btnOK
            // 
            this.btnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOK.Enabled = false;
            this.btnOK.Location = new System.Drawing.Point(917, 423);
            this.btnOK.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 24);
            this.btnOK.TabIndex = 21;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(816, 423);
            this.btnCancel.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 24);
            this.btnCancel.TabIndex = 20;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.licenseTextBox);
            this.tabPage1.Controls.Add(this.label2);
            this.tabPage1.Controls.Add(this.label1);
            this.tabPage1.Controls.Add(this.indexPageTextBox);
            this.tabPage1.Controls.Add(this.footerHtmlTextBox);
            this.tabPage1.Controls.Add(this.homeLinkTextBox);
            this.tabPage1.Controls.Add(this.copyrightLinkTextBox);
            this.tabPage1.Controls.Add(this.label28);
            this.tabPage1.Controls.Add(this.label27);
            this.tabPage1.Controls.Add(this.label26);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.tabPage1.Size = new System.Drawing.Size(977, 381);
            this.tabPage1.TabIndex = 8;
            this.tabPage1.Text = "HTML";
            this.tabPage1.UseVisualStyleBackColor = true;
            this.tabPage1.Click += new System.EventHandler(this.tabPage1_Click);
            // 
            // licenseTextBox
            // 
            this.licenseTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.licenseTextBox.Location = new System.Drawing.Point(21, 281);
            this.licenseTextBox.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.licenseTextBox.Multiline = true;
            this.licenseTextBox.Name = "licenseTextBox";
            this.licenseTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.licenseTextBox.Size = new System.Drawing.Size(944, 82);
            this.licenseTextBox.TabIndex = 8;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(19, 266);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(275, 13);
            this.label2.TabIndex = 7;
            this.label2.Text = "HTML full copyright, permissions, and acknowledgments ";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(19, 109);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(334, 13);
            this.label1.TabIndex = 6;
            this.label1.Text = "HTML to insert into index page. {0} will be replaced with language ID.";
            // 
            // indexPageTextBox
            // 
            this.indexPageTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.indexPageTextBox.Location = new System.Drawing.Point(21, 125);
            this.indexPageTextBox.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.indexPageTextBox.Multiline = true;
            this.indexPageTextBox.Name = "indexPageTextBox";
            this.indexPageTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.indexPageTextBox.Size = new System.Drawing.Size(948, 129);
            this.indexPageTextBox.TabIndex = 5;
            // 
            // footerHtmlTextBox
            // 
            this.footerHtmlTextBox.AcceptsReturn = true;
            this.footerHtmlTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.footerHtmlTextBox.Location = new System.Drawing.Point(21, 69);
            this.footerHtmlTextBox.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.footerHtmlTextBox.Multiline = true;
            this.footerHtmlTextBox.Name = "footerHtmlTextBox";
            this.footerHtmlTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.footerHtmlTextBox.Size = new System.Drawing.Size(948, 39);
            this.footerHtmlTextBox.TabIndex = 3;
            // 
            // homeLinkTextBox
            // 
            this.homeLinkTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.homeLinkTextBox.Location = new System.Drawing.Point(186, 6);
            this.homeLinkTextBox.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.homeLinkTextBox.Name = "homeLinkTextBox";
            this.homeLinkTextBox.Size = new System.Drawing.Size(785, 20);
            this.homeLinkTextBox.TabIndex = 1;
            // 
            // copyrightLinkTextBox
            // 
            this.copyrightLinkTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.copyrightLinkTextBox.Location = new System.Drawing.Point(186, 32);
            this.copyrightLinkTextBox.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.copyrightLinkTextBox.Name = "copyrightLinkTextBox";
            this.copyrightLinkTextBox.Size = new System.Drawing.Size(785, 20);
            this.copyrightLinkTextBox.TabIndex = 2;
            this.copyrightLinkTextBox.Text = "<a href=\"copyright.htm\">©</a>";
            // 
            // label28
            // 
            this.label28.AutoSize = true;
            this.label28.Location = new System.Drawing.Point(19, 54);
            this.label28.Name = "label28";
            this.label28.Size = new System.Drawing.Size(37, 13);
            this.label28.TabIndex = 4;
            this.label28.Text = "Footer";
            // 
            // label27
            // 
            this.label27.AutoSize = true;
            this.label27.Location = new System.Drawing.Point(19, 9);
            this.label27.Name = "label27";
            this.label27.Size = new System.Drawing.Size(131, 13);
            this.label27.TabIndex = 2;
            this.label27.Text = "Home link (blank for none)";
            // 
            // label26
            // 
            this.label26.AutoSize = true;
            this.label26.Location = new System.Drawing.Point(19, 35);
            this.label26.Name = "label26";
            this.label26.Size = new System.Drawing.Size(147, 13);
            this.label26.TabIndex = 0;
            this.label26.Text = "Copyright link (blank for none)";
            // 
            // tabMisc
            // 
            this.tabMisc.Controls.Add(this.ignoreExtrasCheckBox);
            this.tabMisc.Controls.Add(this.KhmerNumeralsRadioButton);
            this.tabMisc.Controls.Add(this.ArabicNumeralsRadioButton);
            this.tabMisc.Controls.Add(this.languageNameTextBox);
            this.tabMisc.Controls.Add(this.ethnologueCodeTextBox);
            this.tabMisc.Controls.Add(this.label25);
            this.tabMisc.Controls.Add(this.label24);
            this.tabMisc.Controls.Add(this.btnRemoveInputProcess);
            this.tabMisc.Controls.Add(this.btnAddInputProcess);
            this.tabMisc.Controls.Add(this.listInputProcesses);
            this.tabMisc.Controls.Add(this.label22);
            this.tabMisc.Location = new System.Drawing.Point(4, 22);
            this.tabMisc.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.tabMisc.Name = "tabMisc";
            this.tabMisc.Size = new System.Drawing.Size(977, 381);
            this.tabMisc.TabIndex = 4;
            this.tabMisc.Text = "Identification";
            this.tabMisc.UseVisualStyleBackColor = true;
            this.tabMisc.Click += new System.EventHandler(this.tabMisc_Click);
            // 
            // ignoreExtrasCheckBox
            // 
            this.ignoreExtrasCheckBox.AutoSize = true;
            this.ignoreExtrasCheckBox.Location = new System.Drawing.Point(315, 261);
            this.ignoreExtrasCheckBox.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.ignoreExtrasCheckBox.Name = "ignoreExtrasCheckBox";
            this.ignoreExtrasCheckBox.Size = new System.Drawing.Size(244, 17);
            this.ignoreExtrasCheckBox.TabIndex = 11;
            this.ignoreExtrasCheckBox.Text = "Omit notes, crossreferences, and introductions";
            this.ignoreExtrasCheckBox.UseVisualStyleBackColor = true;
            // 
            // KhmerNumeralsRadioButton
            // 
            this.KhmerNumeralsRadioButton.AutoSize = true;
            this.KhmerNumeralsRadioButton.Location = new System.Drawing.Point(31, 282);
            this.KhmerNumeralsRadioButton.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.KhmerNumeralsRadioButton.Name = "KhmerNumeralsRadioButton";
            this.KhmerNumeralsRadioButton.Size = new System.Drawing.Size(100, 17);
            this.KhmerNumeralsRadioButton.TabIndex = 10;
            this.KhmerNumeralsRadioButton.Text = "Khmer numerals";
            this.KhmerNumeralsRadioButton.UseVisualStyleBackColor = true;
            // 
            // ArabicNumeralsRadioButton
            // 
            this.ArabicNumeralsRadioButton.AutoSize = true;
            this.ArabicNumeralsRadioButton.Checked = true;
            this.ArabicNumeralsRadioButton.Location = new System.Drawing.Point(31, 259);
            this.ArabicNumeralsRadioButton.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.ArabicNumeralsRadioButton.Name = "ArabicNumeralsRadioButton";
            this.ArabicNumeralsRadioButton.Size = new System.Drawing.Size(131, 17);
            this.ArabicNumeralsRadioButton.TabIndex = 9;
            this.ArabicNumeralsRadioButton.TabStop = true;
            this.ArabicNumeralsRadioButton.Text = "Hindu-Arabic numerals";
            this.ArabicNumeralsRadioButton.UseVisualStyleBackColor = true;
            // 
            // languageNameTextBox
            // 
            this.languageNameTextBox.Location = new System.Drawing.Point(105, 37);
            this.languageNameTextBox.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.languageNameTextBox.Name = "languageNameTextBox";
            this.languageNameTextBox.Size = new System.Drawing.Size(265, 20);
            this.languageNameTextBox.TabIndex = 8;
            // 
            // ethnologueCodeTextBox
            // 
            this.ethnologueCodeTextBox.Location = new System.Drawing.Point(165, 5);
            this.ethnologueCodeTextBox.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.ethnologueCodeTextBox.MaxLength = 3;
            this.ethnologueCodeTextBox.Name = "ethnologueCodeTextBox";
            this.ethnologueCodeTextBox.Size = new System.Drawing.Size(39, 20);
            this.ethnologueCodeTextBox.TabIndex = 6;
            this.ethnologueCodeTextBox.TextChanged += new System.EventHandler(this.ethnologueCodeTextBox_TextChanged);
            // 
            // label25
            // 
            this.label25.AutoSize = true;
            this.label25.Location = new System.Drawing.Point(12, 39);
            this.label25.Name = "label25";
            this.label25.Size = new System.Drawing.Size(87, 13);
            this.label25.TabIndex = 7;
            this.label25.Text = "Language name:";
            // 
            // label24
            // 
            this.label24.AutoSize = true;
            this.label24.Location = new System.Drawing.Point(12, 9);
            this.label24.Name = "label24";
            this.label24.Size = new System.Drawing.Size(136, 13);
            this.label24.TabIndex = 5;
            this.label24.Text = "Language Ethnologe code:";
            // 
            // btnRemoveInputProcess
            // 
            this.btnRemoveInputProcess.Location = new System.Drawing.Point(413, 158);
            this.btnRemoveInputProcess.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btnRemoveInputProcess.Name = "btnRemoveInputProcess";
            this.btnRemoveInputProcess.Size = new System.Drawing.Size(75, 24);
            this.btnRemoveInputProcess.TabIndex = 4;
            this.btnRemoveInputProcess.Text = "Remove";
            this.btnRemoveInputProcess.UseVisualStyleBackColor = true;
            this.btnRemoveInputProcess.Click += new System.EventHandler(this.btnRemoveInputProcess_Click);
            // 
            // btnAddInputProcess
            // 
            this.btnAddInputProcess.Location = new System.Drawing.Point(411, 119);
            this.btnAddInputProcess.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btnAddInputProcess.Name = "btnAddInputProcess";
            this.btnAddInputProcess.Size = new System.Drawing.Size(75, 24);
            this.btnAddInputProcess.TabIndex = 3;
            this.btnAddInputProcess.Text = "Add...";
            this.btnAddInputProcess.UseVisualStyleBackColor = true;
            this.btnAddInputProcess.Click += new System.EventHandler(this.btnAddInputProcess_Click);
            // 
            // listInputProcesses
            // 
            this.listInputProcesses.FormattingEnabled = true;
            this.listInputProcesses.Location = new System.Drawing.Point(15, 110);
            this.listInputProcesses.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.listInputProcesses.Name = "listInputProcesses";
            this.listInputProcesses.Size = new System.Drawing.Size(361, 134);
            this.listInputProcesses.TabIndex = 2;
            // 
            // label22
            // 
            this.label22.AutoSize = true;
            this.label22.Location = new System.Drawing.Point(12, 78);
            this.label22.Name = "label22";
            this.label22.Size = new System.Drawing.Size(538, 13);
            this.label22.TabIndex = 1;
            this.label22.Text = "If you are using the option of applying your own transformation to USFM,  specify" +
                " the necessary conversions here";
            // 
            // tabControl1
            // 
            this.tabControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControl1.Controls.Add(this.tabMisc);
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Location = new System.Drawing.Point(11, 12);
            this.tabControl1.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(985, 407);
            this.tabControl1.TabIndex = 0;
            // 
            // OptionsDlg
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1008, 458);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.tabControl1);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.Name = "OptionsDlg";
            this.Text = "Options";
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
            this.tabMisc.ResumeLayout(false);
            this.tabMisc.PerformLayout();
            this.tabControl1.ResumeLayout(false);
            this.ResumeLayout(false);

		}

		#endregion

        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ColumnHeader columnHeader2;
		private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TextBox footerHtmlTextBox;
        private System.Windows.Forms.TextBox homeLinkTextBox;
        private System.Windows.Forms.TextBox copyrightLinkTextBox;
        private System.Windows.Forms.Label label28;
        private System.Windows.Forms.Label label27;
        private System.Windows.Forms.Label label26;
        private System.Windows.Forms.TabPage tabMisc;
        private System.Windows.Forms.TextBox languageNameTextBox;
        private System.Windows.Forms.TextBox ethnologueCodeTextBox;
        private System.Windows.Forms.Label label25;
        private System.Windows.Forms.Label label24;
        private System.Windows.Forms.Button btnRemoveInputProcess;
        private System.Windows.Forms.Button btnAddInputProcess;
        private System.Windows.Forms.ListBox listInputProcesses;
        private System.Windows.Forms.Label label22;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TextBox indexPageTextBox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox licenseTextBox;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.RadioButton KhmerNumeralsRadioButton;
        private System.Windows.Forms.RadioButton ArabicNumeralsRadioButton;
        private System.Windows.Forms.CheckBox ignoreExtrasCheckBox;
	}
}