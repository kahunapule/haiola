namespace WordSend
{
    partial class SetupForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SetupForm));
            this.projectDirectoryLabel = new System.Windows.Forms.Label();
            this.projectDirectoryButton = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.button1 = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // projectDirectoryLabel
            // 
            this.projectDirectoryLabel.AutoSize = true;
            this.projectDirectoryLabel.Location = new System.Drawing.Point(9, 48);
            this.projectDirectoryLabel.Name = "projectDirectoryLabel";
            this.projectDirectoryLabel.Size = new System.Drawing.Size(146, 13);
            this.projectDirectoryLabel.TabIndex = 1;
            this.projectDirectoryLabel.Text = "Please set a project directory.";
            // 
            // projectDirectoryButton
            // 
            this.projectDirectoryButton.Location = new System.Drawing.Point(12, 15);
            this.projectDirectoryButton.Name = "projectDirectoryButton";
            this.projectDirectoryButton.Size = new System.Drawing.Size(178, 30);
            this.projectDirectoryButton.TabIndex = 2;
            this.projectDirectoryButton.Text = "Set project &Directory";
            this.projectDirectoryButton.UseVisualStyleBackColor = true;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(9, 124);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(216, 13);
            this.label3.TabIndex = 4;
            this.label3.Text = "Please select your Paratext project directory.";
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(12, 89);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(178, 32);
            this.button1.TabIndex = 5;
            this.button1.Text = "Find &Paratext projects directory";
            this.button1.UseVisualStyleBackColor = true;
            // 
            // SetupForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(499, 281);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.projectDirectoryButton);
            this.Controls.Add(this.projectDirectoryLabel);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "SetupForm";
            this.Text = "Haiola Settings";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label projectDirectoryLabel;
        private System.Windows.Forms.Button projectDirectoryButton;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button button1;
    }
}