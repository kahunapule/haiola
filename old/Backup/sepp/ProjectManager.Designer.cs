namespace sepp
{
	partial class ProjectManager
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ProjectManager));
            this.m_button_OW_to_USFM = new System.Windows.Forms.Button();
            this.m_button_USFM_to_OSIS = new System.Windows.Forms.Button();
            this.m_filesList = new System.Windows.Forms.CheckedListBox();
            this.m_buttonCheckAll = new System.Windows.Forms.Button();
            this.m_buttonUncheckAll = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.copySupportFilesCheckBox = new System.Windows.Forms.CheckBox();
            this.OsisCheckBox = new System.Windows.Forms.CheckBox();
            this.HtmlCheckBox = new System.Windows.Forms.CheckBox();
            this.UsfxCheckBox = new System.Windows.Forms.CheckBox();
            this.OwCheckBox = new System.Windows.Forms.CheckBox();
            this.UsfxToHtmlButton = new System.Windows.Forms.Button();
            this.UsfmToUsfxButton = new System.Windows.Forms.Button();
            this.btnCopySupportFiles = new System.Windows.Forms.Button();
            this.optionsButton = new System.Windows.Forms.Button();
            this.buttonAllSteps = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // m_button_OW_to_USFM
            // 
            this.m_button_OW_to_USFM.Location = new System.Drawing.Point(17, 26);
            this.m_button_OW_to_USFM.Name = "m_button_OW_to_USFM";
            this.m_button_OW_to_USFM.Size = new System.Drawing.Size(143, 23);
            this.m_button_OW_to_USFM.TabIndex = 2;
            this.m_button_OW_to_USFM.Text = "OW to USFM";
            this.m_button_OW_to_USFM.UseVisualStyleBackColor = true;
            this.m_button_OW_to_USFM.Click += new System.EventHandler(this.m_button_Input_to_USFM_Click);
            // 
            // m_button_USFM_to_OSIS
            // 
            this.m_button_USFM_to_OSIS.Location = new System.Drawing.Point(17, 148);
            this.m_button_USFM_to_OSIS.Name = "m_button_USFM_to_OSIS";
            this.m_button_USFM_to_OSIS.Size = new System.Drawing.Size(143, 23);
            this.m_button_USFM_to_OSIS.TabIndex = 8;
            this.m_button_USFM_to_OSIS.Text = "USFM to OSIS";
            this.m_button_USFM_to_OSIS.UseVisualStyleBackColor = true;
            this.m_button_USFM_to_OSIS.Click += new System.EventHandler(this.m_button_USFM_to_OSIS_Click);
            // 
            // m_filesList
            // 
            this.m_filesList.FormattingEnabled = true;
            this.m_filesList.Location = new System.Drawing.Point(198, 30);
            this.m_filesList.Name = "m_filesList";
            this.m_filesList.Size = new System.Drawing.Size(209, 274);
            this.m_filesList.TabIndex = 22;
            // 
            // m_buttonCheckAll
            // 
            this.m_buttonCheckAll.Location = new System.Drawing.Point(206, 314);
            this.m_buttonCheckAll.Name = "m_buttonCheckAll";
            this.m_buttonCheckAll.Size = new System.Drawing.Size(89, 32);
            this.m_buttonCheckAll.TabIndex = 23;
            this.m_buttonCheckAll.Text = "Check all";
            this.m_buttonCheckAll.UseVisualStyleBackColor = true;
            this.m_buttonCheckAll.Click += new System.EventHandler(this.m_buttonCheckAll_Click);
            // 
            // m_buttonUncheckAll
            // 
            this.m_buttonUncheckAll.Location = new System.Drawing.Point(301, 314);
            this.m_buttonUncheckAll.Name = "m_buttonUncheckAll";
            this.m_buttonUncheckAll.Size = new System.Drawing.Size(91, 31);
            this.m_buttonUncheckAll.TabIndex = 24;
            this.m_buttonUncheckAll.Text = "Uncheck all";
            this.m_buttonUncheckAll.UseVisualStyleBackColor = true;
            this.m_buttonUncheckAll.Click += new System.EventHandler(this.m_buttonUncheckAll_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.copySupportFilesCheckBox);
            this.groupBox1.Controls.Add(this.OsisCheckBox);
            this.groupBox1.Controls.Add(this.HtmlCheckBox);
            this.groupBox1.Controls.Add(this.UsfxCheckBox);
            this.groupBox1.Controls.Add(this.OwCheckBox);
            this.groupBox1.Controls.Add(this.UsfxToHtmlButton);
            this.groupBox1.Controls.Add(this.UsfmToUsfxButton);
            this.groupBox1.Controls.Add(this.btnCopySupportFiles);
            this.groupBox1.Controls.Add(this.m_buttonUncheckAll);
            this.groupBox1.Controls.Add(this.m_buttonCheckAll);
            this.groupBox1.Controls.Add(this.m_filesList);
            this.groupBox1.Controls.Add(this.m_button_USFM_to_OSIS);
            this.groupBox1.Controls.Add(this.m_button_OW_to_USFM);
            this.groupBox1.Location = new System.Drawing.Point(113, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(415, 364);
            this.groupBox1.TabIndex = 10;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Individual steps and files";
            // 
            // copySupportFilesCheckBox
            // 
            this.copySupportFilesCheckBox.AutoSize = true;
            this.copySupportFilesCheckBox.Checked = true;
            this.copySupportFilesCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.copySupportFilesCheckBox.Location = new System.Drawing.Point(166, 327);
            this.copySupportFilesCheckBox.Name = "copySupportFilesCheckBox";
            this.copySupportFilesCheckBox.Size = new System.Drawing.Size(15, 14);
            this.copySupportFilesCheckBox.TabIndex = 21;
            this.copySupportFilesCheckBox.UseVisualStyleBackColor = true;
            // 
            // OsisCheckBox
            // 
            this.OsisCheckBox.AutoSize = true;
            this.OsisCheckBox.Location = new System.Drawing.Point(166, 152);
            this.OsisCheckBox.Name = "OsisCheckBox";
            this.OsisCheckBox.Size = new System.Drawing.Size(15, 14);
            this.OsisCheckBox.TabIndex = 9;
            this.OsisCheckBox.UseVisualStyleBackColor = true;
            // 
            // HtmlCheckBox
            // 
            this.HtmlCheckBox.AutoSize = true;
            this.HtmlCheckBox.Checked = true;
            this.HtmlCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.HtmlCheckBox.Location = new System.Drawing.Point(166, 91);
            this.HtmlCheckBox.Name = "HtmlCheckBox";
            this.HtmlCheckBox.Size = new System.Drawing.Size(15, 14);
            this.HtmlCheckBox.TabIndex = 7;
            this.HtmlCheckBox.UseVisualStyleBackColor = true;
            // 
            // UsfxCheckBox
            // 
            this.UsfxCheckBox.AutoSize = true;
            this.UsfxCheckBox.Checked = true;
            this.UsfxCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.UsfxCheckBox.Location = new System.Drawing.Point(166, 63);
            this.UsfxCheckBox.Name = "UsfxCheckBox";
            this.UsfxCheckBox.Size = new System.Drawing.Size(15, 14);
            this.UsfxCheckBox.TabIndex = 5;
            this.UsfxCheckBox.UseVisualStyleBackColor = true;
            // 
            // OwCheckBox
            // 
            this.OwCheckBox.AutoSize = true;
            this.OwCheckBox.Checked = true;
            this.OwCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.OwCheckBox.Location = new System.Drawing.Point(166, 30);
            this.OwCheckBox.Name = "OwCheckBox";
            this.OwCheckBox.Size = new System.Drawing.Size(15, 14);
            this.OwCheckBox.TabIndex = 3;
            this.OwCheckBox.UseVisualStyleBackColor = true;
            // 
            // UsfxToHtmlButton
            // 
            this.UsfxToHtmlButton.Location = new System.Drawing.Point(17, 87);
            this.UsfxToHtmlButton.Name = "UsfxToHtmlButton";
            this.UsfxToHtmlButton.Size = new System.Drawing.Size(142, 22);
            this.UsfxToHtmlButton.TabIndex = 6;
            this.UsfxToHtmlButton.Text = "USFX to HTML";
            this.UsfxToHtmlButton.UseVisualStyleBackColor = true;
            this.UsfxToHtmlButton.Click += new System.EventHandler(this.UsfxToHtmlButton_Click);
            // 
            // UsfmToUsfxButton
            // 
            this.UsfmToUsfxButton.Location = new System.Drawing.Point(17, 55);
            this.UsfmToUsfxButton.Name = "UsfmToUsfxButton";
            this.UsfmToUsfxButton.Size = new System.Drawing.Size(143, 23);
            this.UsfmToUsfxButton.TabIndex = 4;
            this.UsfmToUsfxButton.Text = "USFM to USFX";
            this.UsfmToUsfxButton.UseVisualStyleBackColor = true;
            this.UsfmToUsfxButton.Click += new System.EventHandler(this.UsfmToUsfxButton_Click);
            // 
            // btnCopySupportFiles
            // 
            this.btnCopySupportFiles.Location = new System.Drawing.Point(17, 322);
            this.btnCopySupportFiles.Name = "btnCopySupportFiles";
            this.btnCopySupportFiles.Size = new System.Drawing.Size(143, 23);
            this.btnCopySupportFiles.TabIndex = 20;
            this.btnCopySupportFiles.Text = "Copy support files";
            this.btnCopySupportFiles.UseVisualStyleBackColor = true;
            this.btnCopySupportFiles.Click += new System.EventHandler(this.btnCopySupportFiles_Click);
            // 
            // optionsButton
            // 
            this.optionsButton.Location = new System.Drawing.Point(4, 58);
            this.optionsButton.Name = "optionsButton";
            this.optionsButton.Size = new System.Drawing.Size(103, 23);
            this.optionsButton.TabIndex = 0;
            this.optionsButton.Text = "Edit options";
            this.optionsButton.UseVisualStyleBackColor = true;
            this.optionsButton.Click += new System.EventHandler(this.optionsButton_Click);
            // 
            // buttonAllSteps
            // 
            this.buttonAllSteps.Location = new System.Drawing.Point(4, 113);
            this.buttonAllSteps.Name = "buttonAllSteps";
            this.buttonAllSteps.Size = new System.Drawing.Size(103, 23);
            this.buttonAllSteps.TabIndex = 1;
            this.buttonAllSteps.Text = "Do selected steps";
            this.buttonAllSteps.UseVisualStyleBackColor = true;
            this.buttonAllSteps.Click += new System.EventHandler(this.buttonAllSteps_Click);
            // 
            // ProjectManager
            // 
            this.AcceptButton = this.buttonAllSteps;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(537, 388);
            this.Controls.Add(this.buttonAllSteps);
            this.Controls.Add(this.optionsButton);
            this.Controls.Add(this.groupBox1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "ProjectManager";
            this.Text = "Project Tasks";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);

		}

		#endregion

        private System.Windows.Forms.Button m_button_OW_to_USFM;
        private System.Windows.Forms.Button m_button_USFM_to_OSIS;
		private System.Windows.Forms.CheckedListBox m_filesList;
		private System.Windows.Forms.Button m_buttonCheckAll;
        private System.Windows.Forms.Button m_buttonUncheckAll;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.Button optionsButton;
		private System.Windows.Forms.Button buttonAllSteps;
		private System.Windows.Forms.Button btnCopySupportFiles;
        private System.Windows.Forms.Button UsfmToUsfxButton;
        private System.Windows.Forms.Button UsfxToHtmlButton;
        private System.Windows.Forms.CheckBox copySupportFilesCheckBox;
        private System.Windows.Forms.CheckBox OsisCheckBox;
        private System.Windows.Forms.CheckBox HtmlCheckBox;
        private System.Windows.Forms.CheckBox UsfxCheckBox;
        private System.Windows.Forms.CheckBox OwCheckBox;
	}
}

