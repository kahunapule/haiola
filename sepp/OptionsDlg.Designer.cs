namespace sepp
{
	partial class OptionsDlg
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(OptionsDlg));
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.footerHtmlTextBox = new System.Windows.Forms.TextBox();
            this.homeLinkTextBox = new System.Windows.Forms.TextBox();
            this.copyrightLinkTextBox = new System.Windows.Forms.TextBox();
            this.label28 = new System.Windows.Forms.Label();
            this.label27 = new System.Windows.Forms.Label();
            this.label26 = new System.Windows.Forms.Label();
            this.tabBackMatter = new System.Windows.Forms.TabPage();
            this.btnAdjustBmFiles = new System.Windows.Forms.Button();
            this.btnMoveDownBackMatter = new System.Windows.Forms.Button();
            this.btnMoveUpBackMatter = new System.Windows.Forms.Button();
            this.label16 = new System.Windows.Forms.Label();
            this.label15 = new System.Windows.Forms.Label();
            this.lstBackMatter = new System.Windows.Forms.ListView();
            this.columnFiles = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHotlink = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.tabLocalization = new System.Windows.Forms.TabPage();
            this.label30 = new System.Windows.Forms.Label();
            this.psalmLabelTextBox = new System.Windows.Forms.TextBox();
            this.chapterLabelTextBox = new System.Windows.Forms.TextBox();
            this.tbxNextChap = new System.Windows.Forms.TextBox();
            this.tbxPrevChap = new System.Windows.Forms.TextBox();
            this.tbxLoading = new System.Windows.Forms.TextBox();
            this.tbxIntroduction = new System.Windows.Forms.TextBox();
            this.tbxBookChap = new System.Windows.Forms.TextBox();
            this.tbxConcordance = new System.Windows.Forms.TextBox();
            this.tbxNotesRef = new System.Windows.Forms.TextBox();
            this.tbxHeadingRef = new System.Windows.Forms.TextBox();
            this.label29 = new System.Windows.Forms.Label();
            this.label18 = new System.Windows.Forms.Label();
            this.label17 = new System.Windows.Forms.Label();
            this.label14 = new System.Windows.Forms.Label();
            this.label13 = new System.Windows.Forms.Label();
            this.label12 = new System.Windows.Forms.Label();
            this.label11 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.tabMisc = new System.Windows.Forms.TabPage();
            this.languageNameTextBox = new System.Windows.Forms.TextBox();
            this.ethnologueCodeTextBox = new System.Windows.Forms.TextBox();
            this.label25 = new System.Windows.Forms.Label();
            this.label24 = new System.Windows.Forms.Label();
            this.btnRemoveInputProcess = new System.Windows.Forms.Button();
            this.btnAddInputProcess = new System.Windows.Forms.Button();
            this.listInputProcesses = new System.Windows.Forms.ListBox();
            this.label22 = new System.Windows.Forms.Label();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1.SuspendLayout();
            this.tabBackMatter.SuspendLayout();
            this.tabLocalization.SuspendLayout();
            this.tabMisc.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.SuspendLayout();
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "File";
            this.columnHeader1.Width = 327;
            // 
            // columnHeader2
            // 
            this.columnHeader2.Text = "Hotlink Text";
            this.columnHeader2.Width = 321;
            // 
            // btnOK
            // 
            this.btnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOK.Location = new System.Drawing.Point(606, 452);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 23);
            this.btnOK.TabIndex = 21;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(496, 452);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 20;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.footerHtmlTextBox);
            this.tabPage1.Controls.Add(this.homeLinkTextBox);
            this.tabPage1.Controls.Add(this.copyrightLinkTextBox);
            this.tabPage1.Controls.Add(this.label28);
            this.tabPage1.Controls.Add(this.label27);
            this.tabPage1.Controls.Add(this.label26);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(685, 400);
            this.tabPage1.TabIndex = 8;
            this.tabPage1.Text = "HTML";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // footerHtmlTextBox
            // 
            this.footerHtmlTextBox.AcceptsReturn = true;
            this.footerHtmlTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.footerHtmlTextBox.Location = new System.Drawing.Point(22, 83);
            this.footerHtmlTextBox.Multiline = true;
            this.footerHtmlTextBox.Name = "footerHtmlTextBox";
            this.footerHtmlTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.footerHtmlTextBox.Size = new System.Drawing.Size(656, 121);
            this.footerHtmlTextBox.TabIndex = 3;
            // 
            // homeLinkTextBox
            // 
            this.homeLinkTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.homeLinkTextBox.Location = new System.Drawing.Point(186, 6);
            this.homeLinkTextBox.Name = "homeLinkTextBox";
            this.homeLinkTextBox.Size = new System.Drawing.Size(493, 20);
            this.homeLinkTextBox.TabIndex = 1;
            this.homeLinkTextBox.Text = "<a href=\"../index.htm\">^</a>";
            // 
            // copyrightLinkTextBox
            // 
            this.copyrightLinkTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.copyrightLinkTextBox.Location = new System.Drawing.Point(186, 32);
            this.copyrightLinkTextBox.Name = "copyrightLinkTextBox";
            this.copyrightLinkTextBox.Size = new System.Drawing.Size(493, 20);
            this.copyrightLinkTextBox.TabIndex = 2;
            this.copyrightLinkTextBox.Text = "<a href=\"copyright.htm\">©</a>";
            // 
            // label28
            // 
            this.label28.AutoSize = true;
            this.label28.Location = new System.Drawing.Point(19, 63);
            this.label28.Name = "label28";
            this.label28.Size = new System.Drawing.Size(37, 13);
            this.label28.TabIndex = 4;
            this.label28.Text = "Footer";
            // 
            // label27
            // 
            this.label27.AutoSize = true;
            this.label27.Location = new System.Drawing.Point(19, 9);
            this.label27.Name = "label27";
            this.label27.Size = new System.Drawing.Size(131, 13);
            this.label27.TabIndex = 2;
            this.label27.Text = "Home link (blank for none)";
            // 
            // label26
            // 
            this.label26.AutoSize = true;
            this.label26.Location = new System.Drawing.Point(19, 35);
            this.label26.Name = "label26";
            this.label26.Size = new System.Drawing.Size(147, 13);
            this.label26.TabIndex = 0;
            this.label26.Text = "Copyright link (blank for none)";
            // 
            // tabBackMatter
            // 
            this.tabBackMatter.Controls.Add(this.btnAdjustBmFiles);
            this.tabBackMatter.Controls.Add(this.btnMoveDownBackMatter);
            this.tabBackMatter.Controls.Add(this.btnMoveUpBackMatter);
            this.tabBackMatter.Controls.Add(this.label16);
            this.tabBackMatter.Controls.Add(this.label15);
            this.tabBackMatter.Controls.Add(this.lstBackMatter);
            this.tabBackMatter.Location = new System.Drawing.Point(4, 22);
            this.tabBackMatter.Name = "tabBackMatter";
            this.tabBackMatter.Size = new System.Drawing.Size(685, 400);
            this.tabBackMatter.TabIndex = 5;
            this.tabBackMatter.Text = "Back Matter";
            this.tabBackMatter.UseVisualStyleBackColor = true;
            // 
            // btnAdjustBmFiles
            // 
            this.btnAdjustBmFiles.Location = new System.Drawing.Point(12, 360);
            this.btnAdjustBmFiles.Name = "btnAdjustBmFiles";
            this.btnAdjustBmFiles.Size = new System.Drawing.Size(114, 23);
            this.btnAdjustBmFiles.TabIndex = 5;
            this.btnAdjustBmFiles.Text = "Adjust File List";
            this.btnAdjustBmFiles.UseVisualStyleBackColor = true;
            this.btnAdjustBmFiles.Click += new System.EventHandler(this.btnAdjustBmFiles_Click);
            // 
            // btnMoveDownBackMatter
            // 
            this.btnMoveDownBackMatter.Location = new System.Drawing.Point(593, 360);
            this.btnMoveDownBackMatter.Name = "btnMoveDownBackMatter";
            this.btnMoveDownBackMatter.Size = new System.Drawing.Size(75, 23);
            this.btnMoveDownBackMatter.TabIndex = 4;
            this.btnMoveDownBackMatter.Text = "Move Down";
            this.btnMoveDownBackMatter.UseVisualStyleBackColor = true;
            this.btnMoveDownBackMatter.Click += new System.EventHandler(this.btnMoveDownBackMatter_Click);
            // 
            // btnMoveUpBackMatter
            // 
            this.btnMoveUpBackMatter.Location = new System.Drawing.Point(481, 360);
            this.btnMoveUpBackMatter.Name = "btnMoveUpBackMatter";
            this.btnMoveUpBackMatter.Size = new System.Drawing.Size(75, 23);
            this.btnMoveUpBackMatter.TabIndex = 3;
            this.btnMoveUpBackMatter.Text = "Move Up";
            this.btnMoveUpBackMatter.UseVisualStyleBackColor = true;
            this.btnMoveUpBackMatter.Click += new System.EventHandler(this.btnMoveUpBackMatter_Click);
            // 
            // label16
            // 
            this.label16.AutoSize = true;
            this.label16.Location = new System.Drawing.Point(9, 36);
            this.label16.Name = "label16";
            this.label16.Size = new System.Drawing.Size(544, 13);
            this.label16.TabIndex = 2;
            this.label16.Text = "Files not given hotlink text are copied to the output but not included in TOC. Th" +
                "ey may be linked to from other files.";
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.Location = new System.Drawing.Point(9, 12);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(542, 13);
            this.label15.TabIndex = 1;
            this.label15.Text = "Back matter files are found in the \"extras\" folder. Type hotlink text into the se" +
                "cond column to include in main TOC.";
            // 
            // lstBackMatter
            // 
            this.lstBackMatter.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnFiles,
            this.columnHotlink});
            this.lstBackMatter.Location = new System.Drawing.Point(12, 68);
            this.lstBackMatter.Name = "lstBackMatter";
            this.lstBackMatter.Size = new System.Drawing.Size(656, 276);
            this.lstBackMatter.TabIndex = 0;
            this.lstBackMatter.UseCompatibleStateImageBehavior = false;
            this.lstBackMatter.View = System.Windows.Forms.View.Details;
            // 
            // columnFiles
            // 
            this.columnFiles.Text = "File";
            this.columnFiles.Width = 327;
            // 
            // columnHotlink
            // 
            this.columnHotlink.Text = "Hotlink Text";
            this.columnHotlink.Width = 321;
            // 
            // tabLocalization
            // 
            this.tabLocalization.Controls.Add(this.label30);
            this.tabLocalization.Controls.Add(this.psalmLabelTextBox);
            this.tabLocalization.Controls.Add(this.chapterLabelTextBox);
            this.tabLocalization.Controls.Add(this.tbxNextChap);
            this.tabLocalization.Controls.Add(this.tbxPrevChap);
            this.tabLocalization.Controls.Add(this.tbxLoading);
            this.tabLocalization.Controls.Add(this.tbxIntroduction);
            this.tabLocalization.Controls.Add(this.tbxBookChap);
            this.tabLocalization.Controls.Add(this.tbxConcordance);
            this.tabLocalization.Controls.Add(this.tbxNotesRef);
            this.tabLocalization.Controls.Add(this.tbxHeadingRef);
            this.tabLocalization.Controls.Add(this.label29);
            this.tabLocalization.Controls.Add(this.label18);
            this.tabLocalization.Controls.Add(this.label17);
            this.tabLocalization.Controls.Add(this.label14);
            this.tabLocalization.Controls.Add(this.label13);
            this.tabLocalization.Controls.Add(this.label12);
            this.tabLocalization.Controls.Add(this.label11);
            this.tabLocalization.Controls.Add(this.label10);
            this.tabLocalization.Controls.Add(this.label9);
            this.tabLocalization.Location = new System.Drawing.Point(4, 22);
            this.tabLocalization.Name = "tabLocalization";
            this.tabLocalization.Size = new System.Drawing.Size(685, 400);
            this.tabLocalization.TabIndex = 3;
            this.tabLocalization.Text = "Localization";
            this.tabLocalization.UseVisualStyleBackColor = true;
            // 
            // label30
            // 
            this.label30.AutoSize = true;
            this.label30.Location = new System.Drawing.Point(15, 255);
            this.label30.Name = "label30";
            this.label30.Size = new System.Drawing.Size(94, 13);
            this.label30.TabIndex = 31;
            this.label30.Text = "Psalm label (if any)";
            // 
            // psalmLabelTextBox
            // 
            this.psalmLabelTextBox.Location = new System.Drawing.Point(356, 255);
            this.psalmLabelTextBox.Name = "psalmLabelTextBox";
            this.psalmLabelTextBox.Size = new System.Drawing.Size(155, 20);
            this.psalmLabelTextBox.TabIndex = 10;
            // 
            // chapterLabelTextBox
            // 
            this.chapterLabelTextBox.Location = new System.Drawing.Point(357, 229);
            this.chapterLabelTextBox.Name = "chapterLabelTextBox";
            this.chapterLabelTextBox.Size = new System.Drawing.Size(154, 20);
            this.chapterLabelTextBox.TabIndex = 9;
            // 
            // tbxNextChap
            // 
            this.tbxNextChap.Location = new System.Drawing.Point(356, 203);
            this.tbxNextChap.Name = "tbxNextChap";
            this.tbxNextChap.Size = new System.Drawing.Size(156, 20);
            this.tbxNextChap.TabIndex = 8;
            this.tbxNextChap.Text = "Next Chapter";
            // 
            // tbxPrevChap
            // 
            this.tbxPrevChap.Location = new System.Drawing.Point(357, 177);
            this.tbxPrevChap.Name = "tbxPrevChap";
            this.tbxPrevChap.Size = new System.Drawing.Size(156, 20);
            this.tbxPrevChap.TabIndex = 7;
            this.tbxPrevChap.Text = "Previous Chapter";
            // 
            // tbxLoading
            // 
            this.tbxLoading.Location = new System.Drawing.Point(356, 151);
            this.tbxLoading.Name = "tbxLoading";
            this.tbxLoading.Size = new System.Drawing.Size(156, 20);
            this.tbxLoading.TabIndex = 6;
            this.tbxLoading.Text = "Loading...";
            // 
            // tbxIntroduction
            // 
            this.tbxIntroduction.Location = new System.Drawing.Point(357, 125);
            this.tbxIntroduction.Name = "tbxIntroduction";
            this.tbxIntroduction.Size = new System.Drawing.Size(156, 20);
            this.tbxIntroduction.TabIndex = 5;
            this.tbxIntroduction.Text = "Introduction";
            // 
            // tbxBookChap
            // 
            this.tbxBookChap.Location = new System.Drawing.Point(356, 99);
            this.tbxBookChap.Name = "tbxBookChap";
            this.tbxBookChap.Size = new System.Drawing.Size(156, 20);
            this.tbxBookChap.TabIndex = 4;
            this.tbxBookChap.Text = "Books and Chapters";
            // 
            // tbxConcordance
            // 
            this.tbxConcordance.Location = new System.Drawing.Point(356, 73);
            this.tbxConcordance.Name = "tbxConcordance";
            this.tbxConcordance.Size = new System.Drawing.Size(156, 20);
            this.tbxConcordance.TabIndex = 3;
            this.tbxConcordance.Text = "Concordance";
            // 
            // tbxNotesRef
            // 
            this.tbxNotesRef.Location = new System.Drawing.Point(356, 47);
            this.tbxNotesRef.Name = "tbxNotesRef";
            this.tbxNotesRef.Size = new System.Drawing.Size(156, 20);
            this.tbxNotesRef.TabIndex = 2;
            this.tbxNotesRef.Text = "fn";
            // 
            // tbxHeadingRef
            // 
            this.tbxHeadingRef.Location = new System.Drawing.Point(357, 21);
            this.tbxHeadingRef.Name = "tbxHeadingRef";
            this.tbxHeadingRef.Size = new System.Drawing.Size(156, 20);
            this.tbxHeadingRef.TabIndex = 1;
            this.tbxHeadingRef.Text = "head";
            // 
            // label29
            // 
            this.label29.AutoSize = true;
            this.label29.Location = new System.Drawing.Point(14, 229);
            this.label29.Name = "label29";
            this.label29.Size = new System.Drawing.Size(103, 13);
            this.label29.TabIndex = 28;
            this.label29.Text = "Chapter label (if any)";
            // 
            // label18
            // 
            this.label18.AutoSize = true;
            this.label18.Location = new System.Drawing.Point(14, 203);
            this.label18.Name = "label18";
            this.label18.Size = new System.Drawing.Size(180, 13);
            this.label18.TabIndex = 26;
            this.label18.Text = "Text of button linking to next chapter";
            // 
            // label17
            // 
            this.label17.AutoSize = true;
            this.label17.Location = new System.Drawing.Point(15, 177);
            this.label17.Name = "label17";
            this.label17.Size = new System.Drawing.Size(200, 13);
            this.label17.TabIndex = 24;
            this.label17.Text = "Text of button linking to previous chapter";
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Location = new System.Drawing.Point(14, 151);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(191, 13);
            this.label14.TabIndex = 22;
            this.label14.Text = "String displayed while a page is loading";
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(15, 125);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(297, 13);
            this.label13.TabIndex = 20;
            this.label13.Text = "Hotlink to the Introduction of any individual book that has one";
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(15, 99);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(326, 13);
            this.label12.TabIndex = 18;
            this.label12.Text = "Hotlink in the concordace index back to the main Table of Contents";
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(14, 73);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(273, 13);
            this.label11.TabIndex = 16;
            this.label11.Text = "Hotlink in the main table of contents to the Concordance";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(14, 47);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(251, 13);
            this.label10.TabIndex = 14;
            this.label10.Text = "String to use as the \'reference\' of words in footnotes";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(15, 21);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(250, 13);
            this.label9.TabIndex = 12;
            this.label9.Text = "String to use as the \'reference\' of words in headings";
            // 
            // tabMisc
            // 
            this.tabMisc.Controls.Add(this.languageNameTextBox);
            this.tabMisc.Controls.Add(this.ethnologueCodeTextBox);
            this.tabMisc.Controls.Add(this.label25);
            this.tabMisc.Controls.Add(this.label24);
            this.tabMisc.Controls.Add(this.btnRemoveInputProcess);
            this.tabMisc.Controls.Add(this.btnAddInputProcess);
            this.tabMisc.Controls.Add(this.listInputProcesses);
            this.tabMisc.Controls.Add(this.label22);
            this.tabMisc.Location = new System.Drawing.Point(4, 22);
            this.tabMisc.Name = "tabMisc";
            this.tabMisc.Size = new System.Drawing.Size(685, 400);
            this.tabMisc.TabIndex = 4;
            this.tabMisc.Text = "Identification";
            this.tabMisc.UseVisualStyleBackColor = true;
            this.tabMisc.Click += new System.EventHandler(this.tabMisc_Click);
            // 
            // languageNameTextBox
            // 
            this.languageNameTextBox.Location = new System.Drawing.Point(105, 36);
            this.languageNameTextBox.Name = "languageNameTextBox";
            this.languageNameTextBox.Size = new System.Drawing.Size(266, 20);
            this.languageNameTextBox.TabIndex = 8;
            // 
            // ethnologueCodeTextBox
            // 
            this.ethnologueCodeTextBox.Location = new System.Drawing.Point(166, 5);
            this.ethnologueCodeTextBox.MaxLength = 3;
            this.ethnologueCodeTextBox.Name = "ethnologueCodeTextBox";
            this.ethnologueCodeTextBox.Size = new System.Drawing.Size(39, 20);
            this.ethnologueCodeTextBox.TabIndex = 6;
            // 
            // label25
            // 
            this.label25.AutoSize = true;
            this.label25.Location = new System.Drawing.Point(12, 39);
            this.label25.Name = "label25";
            this.label25.Size = new System.Drawing.Size(87, 13);
            this.label25.TabIndex = 7;
            this.label25.Text = "Language name:";
            // 
            // label24
            // 
            this.label24.AutoSize = true;
            this.label24.Location = new System.Drawing.Point(12, 9);
            this.label24.Name = "label24";
            this.label24.Size = new System.Drawing.Size(136, 13);
            this.label24.TabIndex = 5;
            this.label24.Text = "Language Ethnologe code:";
            // 
            // btnRemoveInputProcess
            // 
            this.btnRemoveInputProcess.Location = new System.Drawing.Point(413, 158);
            this.btnRemoveInputProcess.Name = "btnRemoveInputProcess";
            this.btnRemoveInputProcess.Size = new System.Drawing.Size(75, 23);
            this.btnRemoveInputProcess.TabIndex = 4;
            this.btnRemoveInputProcess.Text = "Remove";
            this.btnRemoveInputProcess.UseVisualStyleBackColor = true;
            this.btnRemoveInputProcess.Click += new System.EventHandler(this.btnRemoveInputProcess_Click);
            // 
            // btnAddInputProcess
            // 
            this.btnAddInputProcess.Location = new System.Drawing.Point(411, 119);
            this.btnAddInputProcess.Name = "btnAddInputProcess";
            this.btnAddInputProcess.Size = new System.Drawing.Size(75, 23);
            this.btnAddInputProcess.TabIndex = 3;
            this.btnAddInputProcess.Text = "Add...";
            this.btnAddInputProcess.UseVisualStyleBackColor = true;
            this.btnAddInputProcess.Click += new System.EventHandler(this.btnAddInputProcess_Click);
            // 
            // listInputProcesses
            // 
            this.listInputProcesses.FormattingEnabled = true;
            this.listInputProcesses.Location = new System.Drawing.Point(15, 110);
            this.listInputProcesses.Name = "listInputProcesses";
            this.listInputProcesses.Size = new System.Drawing.Size(362, 134);
            this.listInputProcesses.TabIndex = 2;
            // 
            // label22
            // 
            this.label22.AutoSize = true;
            this.label22.Location = new System.Drawing.Point(12, 78);
            this.label22.Name = "label22";
            this.label22.Size = new System.Drawing.Size(538, 13);
            this.label22.TabIndex = 1;
            this.label22.Text = "If you are using the option of applying your own transformation to USFM,  specify" +
                " the necessary conversions here";
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabMisc);
            this.tabControl1.Controls.Add(this.tabLocalization);
            this.tabControl1.Controls.Add(this.tabBackMatter);
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Location = new System.Drawing.Point(11, 12);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(693, 426);
            this.tabControl1.TabIndex = 0;
            // 
            // OptionsDlg
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(716, 487);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.tabControl1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "OptionsDlg";
            this.Text = "Options";
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
            this.tabBackMatter.ResumeLayout(false);
            this.tabBackMatter.PerformLayout();
            this.tabLocalization.ResumeLayout(false);
            this.tabLocalization.PerformLayout();
            this.tabMisc.ResumeLayout(false);
            this.tabMisc.PerformLayout();
            this.tabControl1.ResumeLayout(false);
            this.ResumeLayout(false);

		}

		#endregion

        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ColumnHeader columnHeader2;
		private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TextBox footerHtmlTextBox;
        private System.Windows.Forms.TextBox homeLinkTextBox;
        private System.Windows.Forms.TextBox copyrightLinkTextBox;
        private System.Windows.Forms.Label label28;
        private System.Windows.Forms.Label label27;
        private System.Windows.Forms.Label label26;
        private System.Windows.Forms.TabPage tabBackMatter;
        private System.Windows.Forms.Button btnAdjustBmFiles;
        private System.Windows.Forms.Button btnMoveDownBackMatter;
        private System.Windows.Forms.Button btnMoveUpBackMatter;
        private System.Windows.Forms.Label label16;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.ListView lstBackMatter;
        private System.Windows.Forms.ColumnHeader columnFiles;
        private System.Windows.Forms.ColumnHeader columnHotlink;
        private System.Windows.Forms.TabPage tabLocalization;
        private System.Windows.Forms.Label label30;
        private System.Windows.Forms.TextBox psalmLabelTextBox;
        private System.Windows.Forms.TextBox chapterLabelTextBox;
        private System.Windows.Forms.TextBox tbxNextChap;
        private System.Windows.Forms.TextBox tbxPrevChap;
        private System.Windows.Forms.TextBox tbxLoading;
        private System.Windows.Forms.TextBox tbxIntroduction;
        private System.Windows.Forms.TextBox tbxBookChap;
        private System.Windows.Forms.TextBox tbxConcordance;
        private System.Windows.Forms.TextBox tbxNotesRef;
        private System.Windows.Forms.TextBox tbxHeadingRef;
        private System.Windows.Forms.Label label29;
        private System.Windows.Forms.Label label18;
        private System.Windows.Forms.Label label17;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.TabPage tabMisc;
        private System.Windows.Forms.TextBox languageNameTextBox;
        private System.Windows.Forms.TextBox ethnologueCodeTextBox;
        private System.Windows.Forms.Label label25;
        private System.Windows.Forms.Label label24;
        private System.Windows.Forms.Button btnRemoveInputProcess;
        private System.Windows.Forms.Button btnAddInputProcess;
        private System.Windows.Forms.ListBox listInputProcesses;
        private System.Windows.Forms.Label label22;
        private System.Windows.Forms.TabControl tabControl1;
	}
}