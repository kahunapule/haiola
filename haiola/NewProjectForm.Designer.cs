namespace WordSend
{
    partial class NewProjectForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(NewProjectForm));
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.translationShortIDTextBox = new System.Windows.Forms.TextBox();
            this.cancelButton = new System.Windows.Forms.Button();
            this.doneButton = new System.Windows.Forms.Button();
            this.projectDirectoryLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // textBox1
            // 
            this.textBox1.Enabled = false;
            this.textBox1.Location = new System.Drawing.Point(9, 11);
            this.textBox1.Multiline = true;
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(776, 62);
            this.textBox1.TabIndex = 0;
            this.textBox1.Text = resources.GetString("textBox1.Text");
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(8, 83);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(171, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Bible translation short ID (required):";
            // 
            // translationShortIDTextBox
            // 
            this.translationShortIDTextBox.Location = new System.Drawing.Point(185, 83);
            this.translationShortIDTextBox.Name = "translationShortIDTextBox";
            this.translationShortIDTextBox.Size = new System.Drawing.Size(202, 20);
            this.translationShortIDTextBox.TabIndex = 2;
            this.translationShortIDTextBox.TextChanged += new System.EventHandler(this.translationShortIDTextBox_TextChanged);
            // 
            // cancelButton
            // 
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelButton.Location = new System.Drawing.Point(9, 140);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(165, 38);
            this.cancelButton.TabIndex = 8;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            this.cancelButton.Click += new System.EventHandler(this.cancelButton_Click);
            // 
            // doneButton
            // 
            this.doneButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.doneButton.Location = new System.Drawing.Point(596, 140);
            this.doneButton.Name = "doneButton";
            this.doneButton.Size = new System.Drawing.Size(189, 37);
            this.doneButton.TabIndex = 9;
            this.doneButton.Text = "Done";
            this.doneButton.UseVisualStyleBackColor = true;
            this.doneButton.Click += new System.EventHandler(this.doneButton_Click);
            // 
            // projectDirectoryLabel
            // 
            this.projectDirectoryLabel.AutoSize = true;
            this.projectDirectoryLabel.Location = new System.Drawing.Point(8, 106);
            this.projectDirectoryLabel.Name = "projectDirectoryLabel";
            this.projectDirectoryLabel.Size = new System.Drawing.Size(325, 13);
            this.projectDirectoryLabel.TabIndex = 10;
            this.projectDirectoryLabel.Text = "Please enter a valid directory name for the Bible translation short ID.";
            // 
            // NewProjectForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 204);
            this.Controls.Add(this.projectDirectoryLabel);
            this.Controls.Add(this.doneButton);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.translationShortIDTextBox);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.textBox1);
            this.Name = "NewProjectForm";
            this.Text = "Create New Project - Haiola";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox translationShortIDTextBox;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.Button doneButton;
        private System.Windows.Forms.Label projectDirectoryLabel;
    }
}