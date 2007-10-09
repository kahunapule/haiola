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
			this.m_projectsList = new System.Windows.Forms.CheckedListBox();
			this.MasterIndexButton = new System.Windows.Forms.Button();
			this.ProjectButton = new System.Windows.Forms.Button();
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
			this.m_projectsList.TabIndex = 7;
			// 
			// MasterIndexButton
			// 
			this.MasterIndexButton.Location = new System.Drawing.Point(359, 30);
			this.MasterIndexButton.Name = "MasterIndexButton";
			this.MasterIndexButton.Size = new System.Drawing.Size(244, 23);
			this.MasterIndexButton.TabIndex = 8;
			this.MasterIndexButton.Text = "Generate master language index";
			this.MasterIndexButton.UseVisualStyleBackColor = true;
			this.MasterIndexButton.Click += new System.EventHandler(this.MasterIndexButton_Click);
			// 
			// ProjectButton
			// 
			this.ProjectButton.Location = new System.Drawing.Point(361, 89);
			this.ProjectButton.Name = "ProjectButton";
			this.ProjectButton.Size = new System.Drawing.Size(242, 23);
			this.ProjectButton.TabIndex = 9;
			this.ProjectButton.Text = "Work on selected project";
			this.ProjectButton.UseVisualStyleBackColor = true;
			this.ProjectButton.Click += new System.EventHandler(this.ProjectButton_Click);
			// 
			// Master
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(631, 358);
			this.Controls.Add(this.ProjectButton);
			this.Controls.Add(this.MasterIndexButton);
			this.Controls.Add(this.m_projectsList);
			this.Name = "Master";
			this.Text = "Scripture Electonic Publishing Program";
			this.Load += new System.EventHandler(this.Master_Load);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.CheckedListBox m_projectsList;
		private System.Windows.Forms.Button MasterIndexButton;
		private System.Windows.Forms.Button ProjectButton;
	}
}