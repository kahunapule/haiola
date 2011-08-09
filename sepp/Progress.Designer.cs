namespace sepp
{
	partial class Progress
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
            this.m_bookLabel = new System.Windows.Forms.Label();
            this.m_progressBar = new System.Windows.Forms.ProgressBar();
            this.SuspendLayout();
            // 
            // m_bookLabel
            // 
            this.m_bookLabel.AutoSize = true;
            this.m_bookLabel.Location = new System.Drawing.Point(22, 25);
            this.m_bookLabel.Name = "m_bookLabel";
            this.m_bookLabel.Size = new System.Drawing.Size(32, 13);
            this.m_bookLabel.TabIndex = 0;
            this.m_bookLabel.Text = "Book";
            // 
            // m_progressBar
            // 
            this.m_progressBar.Location = new System.Drawing.Point(13, 55);
            this.m_progressBar.Name = "m_progressBar";
            this.m_progressBar.Size = new System.Drawing.Size(267, 23);
            this.m_progressBar.TabIndex = 1;
            this.m_progressBar.Click += new System.EventHandler(this.m_progressBar_Click);
            // 
            // Progress
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(292, 109);
            this.ControlBox = false;
            this.Controls.Add(this.m_progressBar);
            this.Controls.Add(this.m_bookLabel);
            this.Name = "Progress";
            this.Text = "Progress";
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label m_bookLabel;
		private System.Windows.Forms.ProgressBar m_progressBar;
	}
}