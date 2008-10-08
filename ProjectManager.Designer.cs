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
			this.m_runButton = new System.Windows.Forms.Button();
			this.m_button_OW_to_USFM = new System.Windows.Forms.Button();
			this.m_button_USFM_to_OSIS = new System.Windows.Forms.Button();
			this.m_buttonOSIS_to_HTML = new System.Windows.Forms.Button();
			this.m_buttonHTML_to_XHTML = new System.Windows.Forms.Button();
			this.m_buttonChapIndex = new System.Windows.Forms.Button();
			this.m_filesList = new System.Windows.Forms.CheckedListBox();
			this.m_buttonCheckAll = new System.Windows.Forms.Button();
			this.m_buttonUncheckAll = new System.Windows.Forms.Button();
			this.m_bookNameButton = new System.Windows.Forms.Button();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.optionsButton = new System.Windows.Forms.Button();
			this.buttonAllSteps = new System.Windows.Forms.Button();
			this.btnCopySupportFiles = new System.Windows.Forms.Button();
			this.groupBox1.SuspendLayout();
			this.SuspendLayout();
			// 
			// m_runButton
			// 
			this.m_runButton.Location = new System.Drawing.Point(17, 280);
			this.m_runButton.Name = "m_runButton";
			this.m_runButton.Size = new System.Drawing.Size(143, 23);
			this.m_runButton.TabIndex = 0;
			this.m_runButton.Text = "Build concordance";
			this.m_runButton.UseVisualStyleBackColor = true;
			this.m_runButton.Click += new System.EventHandler(this.m_runButton_Click);
			// 
			// m_button_OW_to_USFM
			// 
			this.m_button_OW_to_USFM.Location = new System.Drawing.Point(17, 26);
			this.m_button_OW_to_USFM.Name = "m_button_OW_to_USFM";
			this.m_button_OW_to_USFM.Size = new System.Drawing.Size(143, 23);
			this.m_button_OW_to_USFM.TabIndex = 1;
			this.m_button_OW_to_USFM.Text = "OW to USFM";
			this.m_button_OW_to_USFM.UseVisualStyleBackColor = true;
			this.m_button_OW_to_USFM.Click += new System.EventHandler(this.m_button_Input_to_USFM_Click);
			// 
			// m_button_USFM_to_OSIS
			// 
			this.m_button_USFM_to_OSIS.Location = new System.Drawing.Point(17, 66);
			this.m_button_USFM_to_OSIS.Name = "m_button_USFM_to_OSIS";
			this.m_button_USFM_to_OSIS.Size = new System.Drawing.Size(143, 23);
			this.m_button_USFM_to_OSIS.TabIndex = 2;
			this.m_button_USFM_to_OSIS.Text = "USFM to OSIS";
			this.m_button_USFM_to_OSIS.UseVisualStyleBackColor = true;
			this.m_button_USFM_to_OSIS.Click += new System.EventHandler(this.m_button_USFM_to_OSIS_Click);
			// 
			// m_buttonOSIS_to_HTML
			// 
			this.m_buttonOSIS_to_HTML.Location = new System.Drawing.Point(17, 108);
			this.m_buttonOSIS_to_HTML.Name = "m_buttonOSIS_to_HTML";
			this.m_buttonOSIS_to_HTML.Size = new System.Drawing.Size(143, 23);
			this.m_buttonOSIS_to_HTML.TabIndex = 3;
			this.m_buttonOSIS_to_HTML.Text = "OSIS to HTML";
			this.m_buttonOSIS_to_HTML.UseVisualStyleBackColor = true;
			this.m_buttonOSIS_to_HTML.Click += new System.EventHandler(this.m_buttonOSIS_to_HTML_Click);
			// 
			// m_buttonHTML_to_XHTML
			// 
			this.m_buttonHTML_to_XHTML.Location = new System.Drawing.Point(17, 150);
			this.m_buttonHTML_to_XHTML.Name = "m_buttonHTML_to_XHTML";
			this.m_buttonHTML_to_XHTML.Size = new System.Drawing.Size(143, 23);
			this.m_buttonHTML_to_XHTML.TabIndex = 4;
			this.m_buttonHTML_to_XHTML.Text = "HTML to XHTML";
			this.m_buttonHTML_to_XHTML.UseVisualStyleBackColor = true;
			this.m_buttonHTML_to_XHTML.Click += new System.EventHandler(this.m_buttonHTML_to_XHTML_Click);
			// 
			// m_buttonChapIndex
			// 
			this.m_buttonChapIndex.Location = new System.Drawing.Point(17, 237);
			this.m_buttonChapIndex.Name = "m_buttonChapIndex";
			this.m_buttonChapIndex.Size = new System.Drawing.Size(143, 23);
			this.m_buttonChapIndex.TabIndex = 5;
			this.m_buttonChapIndex.Text = "Build chapter index";
			this.m_buttonChapIndex.UseVisualStyleBackColor = true;
			this.m_buttonChapIndex.Click += new System.EventHandler(this.m_buttonChapIndex_Click);
			// 
			// m_filesList
			// 
			this.m_filesList.FormattingEnabled = true;
			this.m_filesList.Location = new System.Drawing.Point(198, 30);
			this.m_filesList.Name = "m_filesList";
			this.m_filesList.Size = new System.Drawing.Size(209, 274);
			this.m_filesList.TabIndex = 6;
			// 
			// m_buttonCheckAll
			// 
			this.m_buttonCheckAll.Location = new System.Drawing.Point(206, 314);
			this.m_buttonCheckAll.Name = "m_buttonCheckAll";
			this.m_buttonCheckAll.Size = new System.Drawing.Size(89, 32);
			this.m_buttonCheckAll.TabIndex = 7;
			this.m_buttonCheckAll.Text = "Check all";
			this.m_buttonCheckAll.UseVisualStyleBackColor = true;
			this.m_buttonCheckAll.Click += new System.EventHandler(this.m_buttonCheckAll_Click);
			// 
			// m_buttonUncheckAll
			// 
			this.m_buttonUncheckAll.Location = new System.Drawing.Point(301, 314);
			this.m_buttonUncheckAll.Name = "m_buttonUncheckAll";
			this.m_buttonUncheckAll.Size = new System.Drawing.Size(91, 31);
			this.m_buttonUncheckAll.TabIndex = 8;
			this.m_buttonUncheckAll.Text = "Uncheck all";
			this.m_buttonUncheckAll.UseVisualStyleBackColor = true;
			this.m_buttonUncheckAll.Click += new System.EventHandler(this.m_buttonUncheckAll_Click);
			// 
			// m_bookNameButton
			// 
			this.m_bookNameButton.Location = new System.Drawing.Point(17, 194);
			this.m_bookNameButton.Name = "m_bookNameButton";
			this.m_bookNameButton.Size = new System.Drawing.Size(143, 23);
			this.m_bookNameButton.TabIndex = 9;
			this.m_bookNameButton.Text = "Build book name page";
			this.m_bookNameButton.UseVisualStyleBackColor = true;
			this.m_bookNameButton.Click += new System.EventHandler(this.m_bookNameButton_Click);
			// 
			// groupBox1
			// 
			this.groupBox1.Controls.Add(this.btnCopySupportFiles);
			this.groupBox1.Controls.Add(this.m_bookNameButton);
			this.groupBox1.Controls.Add(this.m_buttonUncheckAll);
			this.groupBox1.Controls.Add(this.m_buttonCheckAll);
			this.groupBox1.Controls.Add(this.m_filesList);
			this.groupBox1.Controls.Add(this.m_buttonChapIndex);
			this.groupBox1.Controls.Add(this.m_buttonHTML_to_XHTML);
			this.groupBox1.Controls.Add(this.m_buttonOSIS_to_HTML);
			this.groupBox1.Controls.Add(this.m_button_USFM_to_OSIS);
			this.groupBox1.Controls.Add(this.m_button_OW_to_USFM);
			this.groupBox1.Controls.Add(this.m_runButton);
			this.groupBox1.Location = new System.Drawing.Point(113, 12);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(415, 364);
			this.groupBox1.TabIndex = 10;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Individual steps and files";
			// 
			// optionsButton
			// 
			this.optionsButton.Location = new System.Drawing.Point(16, 58);
			this.optionsButton.Name = "optionsButton";
			this.optionsButton.Size = new System.Drawing.Size(75, 23);
			this.optionsButton.TabIndex = 11;
			this.optionsButton.Text = "Edit options";
			this.optionsButton.UseVisualStyleBackColor = true;
			this.optionsButton.Click += new System.EventHandler(this.optionsButton_Click);
			// 
			// buttonAllSteps
			// 
			this.buttonAllSteps.Location = new System.Drawing.Point(16, 113);
			this.buttonAllSteps.Name = "buttonAllSteps";
			this.buttonAllSteps.Size = new System.Drawing.Size(75, 23);
			this.buttonAllSteps.TabIndex = 12;
			this.buttonAllSteps.Text = "Do All Steps";
			this.buttonAllSteps.UseVisualStyleBackColor = true;
			this.buttonAllSteps.Click += new System.EventHandler(this.buttonAllSteps_Click);
			// 
			// btnCopySupportFiles
			// 
			this.btnCopySupportFiles.Location = new System.Drawing.Point(17, 322);
			this.btnCopySupportFiles.Name = "btnCopySupportFiles";
			this.btnCopySupportFiles.Size = new System.Drawing.Size(143, 23);
			this.btnCopySupportFiles.TabIndex = 10;
			this.btnCopySupportFiles.Text = "Copy support files";
			this.btnCopySupportFiles.UseVisualStyleBackColor = true;
			this.btnCopySupportFiles.Click += new System.EventHandler(this.btnCopySupportFiles_Click);
			// 
			// ProjectManager
			// 
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
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Button m_runButton;
		private System.Windows.Forms.Button m_button_OW_to_USFM;
		private System.Windows.Forms.Button m_button_USFM_to_OSIS;
		private System.Windows.Forms.Button m_buttonOSIS_to_HTML;
		private System.Windows.Forms.Button m_buttonHTML_to_XHTML;
		private System.Windows.Forms.Button m_buttonChapIndex;
		private System.Windows.Forms.CheckedListBox m_filesList;
		private System.Windows.Forms.Button m_buttonCheckAll;
		private System.Windows.Forms.Button m_buttonUncheckAll;
		private System.Windows.Forms.Button m_bookNameButton;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.Button optionsButton;
		private System.Windows.Forms.Button buttonAllSteps;
		private System.Windows.Forms.Button btnCopySupportFiles;
	}
}

