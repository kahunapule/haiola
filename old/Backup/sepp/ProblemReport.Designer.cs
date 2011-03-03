namespace sepp
{
	partial class ProblemReport
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
			this.m_reportContents = new System.Windows.Forms.RichTextBox();
			this.SuspendLayout();
			// 
			// m_reportContents
			// 
			this.m_reportContents.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_reportContents.Location = new System.Drawing.Point(0, 0);
			this.m_reportContents.Name = "m_reportContents";
			this.m_reportContents.Size = new System.Drawing.Size(677, 411);
			this.m_reportContents.TabIndex = 0;
			this.m_reportContents.Text = "";
			// 
			// ProblemReport
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(677, 411);
			this.Controls.Add(this.m_reportContents);
			this.Name = "ProblemReport";
			this.Text = "Problem Report";
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.RichTextBox m_reportContents;

	}
}