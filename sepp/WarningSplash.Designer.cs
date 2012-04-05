namespace sepp
{
	partial class WarningSplash
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(WarningSplash));
            this.label1 = new System.Windows.Forms.Label();
            this.m_chkDoNotShowAgain = new System.Windows.Forms.CheckBox();
            this.btnOK = new System.Windows.Forms.Button();
            this.helpButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(13, 20);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(507, 377);
            this.label1.TabIndex = 0;
            this.label1.Text = resources.GetString("label1.Text");
            // 
            // m_chkDoNotShowAgain
            // 
            this.m_chkDoNotShowAgain.AutoSize = true;
            this.m_chkDoNotShowAgain.Location = new System.Drawing.Point(15, 422);
            this.m_chkDoNotShowAgain.Name = "m_chkDoNotShowAgain";
            this.m_chkDoNotShowAgain.Size = new System.Drawing.Size(175, 17);
            this.m_chkDoNotShowAgain.TabIndex = 7;
            this.m_chkDoNotShowAgain.Text = "Don\'t show this message again.";
            this.m_chkDoNotShowAgain.UseVisualStyleBackColor = true;
            // 
            // btnOK
            // 
            this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOK.Location = new System.Drawing.Point(445, 421);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 23);
            this.btnOK.TabIndex = 8;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // helpButton
            // 
            this.helpButton.Location = new System.Drawing.Point(292, 422);
            this.helpButton.Name = "helpButton";
            this.helpButton.Size = new System.Drawing.Size(135, 21);
            this.helpButton.TabIndex = 9;
            this.helpButton.Text = "&Help";
            this.helpButton.UseVisualStyleBackColor = true;
            this.helpButton.Click += new System.EventHandler(this.helpButton_Click);
            // 
            // WarningSplash
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(532, 461);
            this.ControlBox = false;
            this.Controls.Add(this.helpButton);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.m_chkDoNotShowAgain);
            this.Controls.Add(this.label1);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "WarningSplash";
            this.Text = "Notice";
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

        private System.Windows.Forms.Label label1;
		private System.Windows.Forms.CheckBox m_chkDoNotShowAgain;
		private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button helpButton;
	}
}