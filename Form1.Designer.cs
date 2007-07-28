namespace sepp
{
	partial class Form1
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
			this.m_runButton = new System.Windows.Forms.Button();
			this.m_button_OW_to_USFM = new System.Windows.Forms.Button();
			this.m_button_USFM_to_OSIS = new System.Windows.Forms.Button();
			this.m_buttonOSIS_to_HTML = new System.Windows.Forms.Button();
			this.m_buttonHTML_to_XHTML = new System.Windows.Forms.Button();
			this.m_buttonChapIndex = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// m_runButton
			// 
			this.m_runButton.Location = new System.Drawing.Point(23, 231);
			this.m_runButton.Name = "m_runButton";
			this.m_runButton.Size = new System.Drawing.Size(143, 23);
			this.m_runButton.TabIndex = 0;
			this.m_runButton.Text = "Build concordance";
			this.m_runButton.UseVisualStyleBackColor = true;
			this.m_runButton.Click += new System.EventHandler(this.m_runButton_Click);
			// 
			// m_button_OW_to_USFM
			// 
			this.m_button_OW_to_USFM.Location = new System.Drawing.Point(23, 13);
			this.m_button_OW_to_USFM.Name = "m_button_OW_to_USFM";
			this.m_button_OW_to_USFM.Size = new System.Drawing.Size(143, 23);
			this.m_button_OW_to_USFM.TabIndex = 1;
			this.m_button_OW_to_USFM.Text = "OW to USFM";
			this.m_button_OW_to_USFM.UseVisualStyleBackColor = true;
			this.m_button_OW_to_USFM.Click += new System.EventHandler(this.m_button_OW_to_USFM_Click);
			// 
			// m_button_USFM_to_OSIS
			// 
			this.m_button_USFM_to_OSIS.Location = new System.Drawing.Point(23, 53);
			this.m_button_USFM_to_OSIS.Name = "m_button_USFM_to_OSIS";
			this.m_button_USFM_to_OSIS.Size = new System.Drawing.Size(143, 23);
			this.m_button_USFM_to_OSIS.TabIndex = 2;
			this.m_button_USFM_to_OSIS.Text = "USFM_to_OSIS";
			this.m_button_USFM_to_OSIS.UseVisualStyleBackColor = true;
			this.m_button_USFM_to_OSIS.Click += new System.EventHandler(this.m_button_USFM_to_OSIS_Click);
			// 
			// m_buttonOSIS_to_HTML
			// 
			this.m_buttonOSIS_to_HTML.Location = new System.Drawing.Point(23, 95);
			this.m_buttonOSIS_to_HTML.Name = "m_buttonOSIS_to_HTML";
			this.m_buttonOSIS_to_HTML.Size = new System.Drawing.Size(143, 23);
			this.m_buttonOSIS_to_HTML.TabIndex = 3;
			this.m_buttonOSIS_to_HTML.Text = "OSIS_to_HTML";
			this.m_buttonOSIS_to_HTML.UseVisualStyleBackColor = true;
			this.m_buttonOSIS_to_HTML.Click += new System.EventHandler(this.m_buttonOSIS_to_HTML_Click);
			// 
			// m_buttonHTML_to_XHTML
			// 
			this.m_buttonHTML_to_XHTML.Location = new System.Drawing.Point(23, 137);
			this.m_buttonHTML_to_XHTML.Name = "m_buttonHTML_to_XHTML";
			this.m_buttonHTML_to_XHTML.Size = new System.Drawing.Size(143, 23);
			this.m_buttonHTML_to_XHTML.TabIndex = 4;
			this.m_buttonHTML_to_XHTML.Text = "HTML_to_XHTML";
			this.m_buttonHTML_to_XHTML.UseVisualStyleBackColor = true;
			this.m_buttonHTML_to_XHTML.Click += new System.EventHandler(this.m_buttonHTML_to_XHTML_Click);
			// 
			// m_buttonChapIndex
			// 
			this.m_buttonChapIndex.Location = new System.Drawing.Point(23, 179);
			this.m_buttonChapIndex.Name = "m_buttonChapIndex";
			this.m_buttonChapIndex.Size = new System.Drawing.Size(143, 23);
			this.m_buttonChapIndex.TabIndex = 5;
			this.m_buttonChapIndex.Text = "Build chapter index";
			this.m_buttonChapIndex.UseVisualStyleBackColor = true;
			this.m_buttonChapIndex.Click += new System.EventHandler(this.m_buttonChapIndex_Click);
			// 
			// Form1
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(292, 266);
			this.Controls.Add(this.m_buttonChapIndex);
			this.Controls.Add(this.m_buttonHTML_to_XHTML);
			this.Controls.Add(this.m_buttonOSIS_to_HTML);
			this.Controls.Add(this.m_button_USFM_to_OSIS);
			this.Controls.Add(this.m_button_OW_to_USFM);
			this.Controls.Add(this.m_runButton);
			this.Name = "Form1";
			this.Text = "Form1";
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Button m_runButton;
		private System.Windows.Forms.Button m_button_OW_to_USFM;
		private System.Windows.Forms.Button m_button_USFM_to_OSIS;
		private System.Windows.Forms.Button m_buttonOSIS_to_HTML;
		private System.Windows.Forms.Button m_buttonHTML_to_XHTML;
		private System.Windows.Forms.Button m_buttonChapIndex;
	}
}

