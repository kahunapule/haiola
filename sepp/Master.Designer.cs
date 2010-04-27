namespace sepp
{
	partial class Master
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Master));
            this.m_projectsList = new System.Windows.Forms.CheckedListBox();
            this.ProjectButton = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.btnSetRootDirectory = new System.Windows.Forms.Button();
            this.m_btnSplitFile = new System.Windows.Forms.Button();
            this.WorkOnAllButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // m_projectsList
            // 
            this.m_projectsList.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.m_projectsList.FormattingEnabled = true;
            this.m_projectsList.Location = new System.Drawing.Point(12, 12);
            this.m_projectsList.Name = "m_projectsList";
            this.m_projectsList.Size = new System.Drawing.Size(292, 334);
            this.m_projectsList.TabIndex = 5;
            // 
            // ProjectButton
            // 
            this.ProjectButton.Location = new System.Drawing.Point(355, 12);
            this.ProjectButton.Name = "ProjectButton";
            this.ProjectButton.Size = new System.Drawing.Size(244, 23);
            this.ProjectButton.TabIndex = 1;
            this.ProjectButton.Text = "Work on selected project";
            this.ProjectButton.UseVisualStyleBackColor = true;
            this.ProjectButton.Click += new System.EventHandler(this.ProjectButton_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(352, 293);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(160, 13);
            this.label1.TabIndex = 10;
            this.label1.Text = "Version 0.990 of 1 January 2010";
            // 
            // btnSetRootDirectory
            // 
            this.btnSetRootDirectory.Location = new System.Drawing.Point(355, 88);
            this.btnSetRootDirectory.Name = "btnSetRootDirectory";
            this.btnSetRootDirectory.Size = new System.Drawing.Size(244, 23);
            this.btnSetRootDirectory.TabIndex = 3;
            this.btnSetRootDirectory.Text = "Set root data directory";
            this.btnSetRootDirectory.UseVisualStyleBackColor = true;
            this.btnSetRootDirectory.Click += new System.EventHandler(this.btnSetRootDirectory_Click);
            // 
            // m_btnSplitFile
            // 
            this.m_btnSplitFile.Location = new System.Drawing.Point(355, 211);
            this.m_btnSplitFile.Name = "m_btnSplitFile";
            this.m_btnSplitFile.Size = new System.Drawing.Size(244, 23);
            this.m_btnSplitFile.TabIndex = 4;
            this.m_btnSplitFile.Text = "Split a multi-book file";
            this.m_btnSplitFile.UseVisualStyleBackColor = true;
            this.m_btnSplitFile.Click += new System.EventHandler(this.m_btnSplitFile_Click);
            // 
            // WorkOnAllButton
            // 
            this.WorkOnAllButton.Location = new System.Drawing.Point(355, 50);
            this.WorkOnAllButton.Name = "WorkOnAllButton";
            this.WorkOnAllButton.Size = new System.Drawing.Size(244, 23);
            this.WorkOnAllButton.TabIndex = 2;
            this.WorkOnAllButton.Text = "Run selected steps on selected projects";
            this.WorkOnAllButton.UseVisualStyleBackColor = true;
            this.WorkOnAllButton.Click += new System.EventHandler(this.WorkOnAllButton_Click);
            // 
            // Master
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(631, 358);
            this.Controls.Add(this.WorkOnAllButton);
            this.Controls.Add(this.m_btnSplitFile);
            this.Controls.Add(this.btnSetRootDirectory);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.ProjectButton);
            this.Controls.Add(this.m_projectsList);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Master";
            this.Text = "Prophero Scripture Electronic Publishing Program";
            this.Load += new System.EventHandler(this.Master_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

        private System.Windows.Forms.CheckedListBox m_projectsList;
		private System.Windows.Forms.Button ProjectButton;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Button btnSetRootDirectory;
        private System.Windows.Forms.Button m_btnSplitFile;
        private System.Windows.Forms.Button WorkOnAllButton;
	}
}