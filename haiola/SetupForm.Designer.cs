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
            this.helpButton = new System.Windows.Forms.Button();
            this.closeButton = new System.Windows.Forms.Button();
            this.coprLabel = new System.Windows.Forms.Label();
            this.swordSuffixTextBox = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.usfm3figTagsCheckBox = new System.Windows.Forms.CheckBox();
            this.RebuildCheckBox = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // projectDirectoryLabel
            // 
            this.projectDirectoryLabel.AutoSize = true;
            this.projectDirectoryLabel.Location = new System.Drawing.Point(9, 48);
            this.projectDirectoryLabel.Name = "projectDirectoryLabel";
            this.projectDirectoryLabel.Size = new System.Drawing.Size(464, 13);
            this.projectDirectoryLabel.TabIndex = 1;
            this.projectDirectoryLabel.Text = "Please select or create a folder to hold your project folders.  PLEASE READ DOCUM" +
    "ENTATION.";
            // 
            // projectDirectoryButton
            // 
            this.projectDirectoryButton.Location = new System.Drawing.Point(12, 15);
            this.projectDirectoryButton.Name = "projectDirectoryButton";
            this.projectDirectoryButton.Size = new System.Drawing.Size(178, 30);
            this.projectDirectoryButton.TabIndex = 1;
            this.projectDirectoryButton.Text = "Set data &Directory";
            this.projectDirectoryButton.UseVisualStyleBackColor = true;
            this.projectDirectoryButton.Click += new System.EventHandler(this.projectDirectoryButton_Click);
            // 
            // helpButton
            // 
            this.helpButton.Location = new System.Drawing.Point(12, 250);
            this.helpButton.Name = "helpButton";
            this.helpButton.Size = new System.Drawing.Size(129, 30);
            this.helpButton.TabIndex = 4;
            this.helpButton.Text = "&Help";
            this.helpButton.UseVisualStyleBackColor = true;
            // 
            // closeButton
            // 
            this.closeButton.Location = new System.Drawing.Point(436, 249);
            this.closeButton.Name = "closeButton";
            this.closeButton.Size = new System.Drawing.Size(128, 31);
            this.closeButton.TabIndex = 5;
            this.closeButton.Text = "&Close";
            this.closeButton.UseVisualStyleBackColor = true;
            this.closeButton.Click += new System.EventHandler(this.closeButton_Click);
            // 
            // coprLabel
            // 
            this.coprLabel.AutoSize = true;
            this.coprLabel.Location = new System.Drawing.Point(9, 194);
            this.coprLabel.Name = "coprLabel";
            this.coprLabel.Size = new System.Drawing.Size(28, 13);
            this.coprLabel.TabIndex = 6;
            this.coprLabel.Text = "copr";
            // 
            // swordSuffixTextBox
            // 
            this.swordSuffixTextBox.Location = new System.Drawing.Point(427, 98);
            this.swordSuffixTextBox.Name = "swordSuffixTextBox";
            this.swordSuffixTextBox.Size = new System.Drawing.Size(46, 20);
            this.swordSuffixTextBox.TabIndex = 8;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(90, 101);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(317, 13);
            this.label1.TabIndex = 9;
            this.label1.Text = "Sword Module suffix to add to all Sword module names, if needed:";
            // 
            // usfm3figTagsCheckBox
            // 
            this.usfm3figTagsCheckBox.AutoSize = true;
            this.usfm3figTagsCheckBox.Location = new System.Drawing.Point(379, 124);
            this.usfm3figTagsCheckBox.Name = "usfm3figTagsCheckBox";
            this.usfm3figTagsCheckBox.Size = new System.Drawing.Size(164, 17);
            this.usfm3figTagsCheckBox.TabIndex = 12;
            this.usfm3figTagsCheckBox.Text = "Generate USFM 3 figure tags";
            this.usfm3figTagsCheckBox.UseVisualStyleBackColor = true;
            // 
            // RebuildCheckBox
            // 
            this.RebuildCheckBox.AutoSize = true;
            this.RebuildCheckBox.Location = new System.Drawing.Point(379, 148);
            this.RebuildCheckBox.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.RebuildCheckBox.Name = "RebuildCheckBox";
            this.RebuildCheckBox.Size = new System.Drawing.Size(62, 17);
            this.RebuildCheckBox.TabIndex = 242;
            this.RebuildCheckBox.Text = "Rebuild";
            this.RebuildCheckBox.UseVisualStyleBackColor = true;
            // 
            // SetupForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(576, 292);
            this.Controls.Add(this.RebuildCheckBox);
            this.Controls.Add(this.usfm3figTagsCheckBox);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.swordSuffixTextBox);
            this.Controls.Add(this.coprLabel);
            this.Controls.Add(this.closeButton);
            this.Controls.Add(this.helpButton);
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
        private System.Windows.Forms.Button helpButton;
        private System.Windows.Forms.Button closeButton;
        private System.Windows.Forms.Label coprLabel;
        private System.Windows.Forms.TextBox swordSuffixTextBox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.CheckBox usfm3figTagsCheckBox;
        private System.Windows.Forms.CheckBox RebuildCheckBox;
    }
}