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
            this.paratextDirLabel = new System.Windows.Forms.Label();
            this.findParatextButton = new System.Windows.Forms.Button();
            this.helpButton = new System.Windows.Forms.Button();
            this.closeButton = new System.Windows.Forms.Button();
            this.loadFCBHKeysCheckBox = new System.Windows.Forms.CheckBox();
            this.coprLabel = new System.Windows.Forms.Label();
            this.extensionLabel = new System.Windows.Forms.Label();
            this.swordSuffixTextBox = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
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
            this.projectDirectoryButton.TabIndex = 1;
            this.projectDirectoryButton.Text = "Set project &Directory";
            this.projectDirectoryButton.UseVisualStyleBackColor = true;
            this.projectDirectoryButton.Click += new System.EventHandler(this.projectDirectoryButton_Click);
            // 
            // paratextDirLabel
            // 
            this.paratextDirLabel.AutoSize = true;
            this.paratextDirLabel.Location = new System.Drawing.Point(9, 124);
            this.paratextDirLabel.Name = "paratextDirLabel";
            this.paratextDirLabel.Size = new System.Drawing.Size(216, 13);
            this.paratextDirLabel.TabIndex = 4;
            this.paratextDirLabel.Text = "Please select your Paratext project directory.";
            // 
            // findParatextButton
            // 
            this.findParatextButton.Location = new System.Drawing.Point(12, 89);
            this.findParatextButton.Name = "findParatextButton";
            this.findParatextButton.Size = new System.Drawing.Size(178, 32);
            this.findParatextButton.TabIndex = 2;
            this.findParatextButton.Text = "Find &Paratext projects directory";
            this.findParatextButton.UseVisualStyleBackColor = true;
            this.findParatextButton.Click += new System.EventHandler(this.findParatextButton_Click);
            // 
            // helpButton
            // 
            this.helpButton.Location = new System.Drawing.Point(12, 183);
            this.helpButton.Name = "helpButton";
            this.helpButton.Size = new System.Drawing.Size(129, 30);
            this.helpButton.TabIndex = 4;
            this.helpButton.Text = "&Help";
            this.helpButton.UseVisualStyleBackColor = true;
            // 
            // closeButton
            // 
            this.closeButton.Location = new System.Drawing.Point(436, 182);
            this.closeButton.Name = "closeButton";
            this.closeButton.Size = new System.Drawing.Size(128, 31);
            this.closeButton.TabIndex = 5;
            this.closeButton.Text = "&Close";
            this.closeButton.UseVisualStyleBackColor = true;
            this.closeButton.Click += new System.EventHandler(this.closeButton_Click);
            // 
            // loadFCBHKeysCheckBox
            // 
            this.loadFCBHKeysCheckBox.AutoSize = true;
            this.loadFCBHKeysCheckBox.Location = new System.Drawing.Point(308, 98);
            this.loadFCBHKeysCheckBox.Name = "loadFCBHKeysCheckBox";
            this.loadFCBHKeysCheckBox.Size = new System.Drawing.Size(174, 17);
            this.loadFCBHKeysCheckBox.TabIndex = 3;
            this.loadFCBHKeysCheckBox.Text = "Load keys from FCBH web site.";
            this.loadFCBHKeysCheckBox.UseVisualStyleBackColor = true;
            this.loadFCBHKeysCheckBox.CheckedChanged += new System.EventHandler(this.loadFCBHKeysCheckBox_CheckedChanged);
            // 
            // coprLabel
            // 
            this.coprLabel.AutoSize = true;
            this.coprLabel.Location = new System.Drawing.Point(9, 148);
            this.coprLabel.Name = "coprLabel";
            this.coprLabel.Size = new System.Drawing.Size(28, 13);
            this.coprLabel.TabIndex = 6;
            this.coprLabel.Text = "copr";
            // 
            // extensionLabel
            // 
            this.extensionLabel.AutoSize = true;
            this.extensionLabel.Location = new System.Drawing.Point(9, 166);
            this.extensionLabel.Name = "extensionLabel";
            this.extensionLabel.Size = new System.Drawing.Size(52, 13);
            this.extensionLabel.TabIndex = 7;
            this.extensionLabel.Text = "extension";
            // 
            // swordSuffixTextBox
            // 
            this.swordSuffixTextBox.Location = new System.Drawing.Point(256, 35);
            this.swordSuffixTextBox.Name = "swordSuffixTextBox";
            this.swordSuffixTextBox.Size = new System.Drawing.Size(46, 20);
            this.swordSuffixTextBox.TabIndex = 8;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(253, 19);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(102, 13);
            this.label1.TabIndex = 9;
            this.label1.Text = "Sword Module suffix";
            // 
            // SetupForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(576, 225);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.swordSuffixTextBox);
            this.Controls.Add(this.extensionLabel);
            this.Controls.Add(this.coprLabel);
            this.Controls.Add(this.loadFCBHKeysCheckBox);
            this.Controls.Add(this.closeButton);
            this.Controls.Add(this.helpButton);
            this.Controls.Add(this.findParatextButton);
            this.Controls.Add(this.paratextDirLabel);
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
        private System.Windows.Forms.Label paratextDirLabel;
        private System.Windows.Forms.Button findParatextButton;
        private System.Windows.Forms.Button helpButton;
        private System.Windows.Forms.Button closeButton;
        private System.Windows.Forms.CheckBox loadFCBHKeysCheckBox;
        private System.Windows.Forms.Label coprLabel;
        private System.Windows.Forms.Label extensionLabel;
        private System.Windows.Forms.TextBox swordSuffixTextBox;
        private System.Windows.Forms.Label label1;
    }
}