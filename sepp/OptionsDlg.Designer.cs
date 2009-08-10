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
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.inputPage = new System.Windows.Forms.TabPage();
            this.btnAdjustFiles = new System.Windows.Forms.Button();
            this.label20 = new System.Windows.Forms.Label();
            this.lstFiles = new System.Windows.Forms.ListView();
            this.colFilesStdAbbr = new System.Windows.Forms.ColumnHeader();
            this.colFilesFile = new System.Windows.Forms.ColumnHeader();
            this.colFilesVernAbbr = new System.Windows.Forms.ColumnHeader();
            this.colFilesXrefs = new System.Windows.Forms.ColumnHeader();
            this.colFilesIntro = new System.Windows.Forms.ColumnHeader();
            this.tabMisc = new System.Windows.Forms.TabPage();
            this.btnRemoveInputProcess = new System.Windows.Forms.Button();
            this.btnAddInputProcess = new System.Windows.Forms.Button();
            this.listInputProcesses = new System.Windows.Forms.ListBox();
            this.label22 = new System.Windows.Forms.Label();
            this.chkChapterPerfile = new System.Windows.Forms.CheckBox();
            this.concPage = new System.Windows.Forms.TabPage();
            this.label21 = new System.Windows.Forms.Label();
            this.tbxPhrases = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.tbxExcludeWords = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.tbxMaxFreq = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.tbxMinContext = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.tbxMaxContext = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.tbxWordformingChars = new System.Windows.Forms.TextBox();
            this.chkMergeCase = new System.Windows.Forms.CheckBox();
            this.tabSorting = new System.Windows.Forms.TabPage();
            this.comboSort = new System.Windows.Forms.ComboBox();
            this.btnTestSort = new System.Windows.Forms.Button();
            this.tbxTestWords = new System.Windows.Forms.TextBox();
            this.tbxCollating = new System.Windows.Forms.TextBox();
            this.label23 = new System.Windows.Forms.Label();
            this.tabLocalization = new System.Windows.Forms.TabPage();
            this.tbxNextChap = new System.Windows.Forms.TextBox();
            this.label18 = new System.Windows.Forms.Label();
            this.tbxPrevChap = new System.Windows.Forms.TextBox();
            this.label17 = new System.Windows.Forms.Label();
            this.tbxLoading = new System.Windows.Forms.TextBox();
            this.label14 = new System.Windows.Forms.Label();
            this.tbxIntroduction = new System.Windows.Forms.TextBox();
            this.label13 = new System.Windows.Forms.Label();
            this.tbxBookChap = new System.Windows.Forms.TextBox();
            this.label12 = new System.Windows.Forms.Label();
            this.tbxConcordance = new System.Windows.Forms.TextBox();
            this.label11 = new System.Windows.Forms.Label();
            this.tbxNotesRef = new System.Windows.Forms.TextBox();
            this.label10 = new System.Windows.Forms.Label();
            this.tbxHeadingRef = new System.Windows.Forms.TextBox();
            this.label9 = new System.Windows.Forms.Label();
            this.taBookNames = new System.Windows.Forms.TabPage();
            this.btnAdjustBookNamesList = new System.Windows.Forms.Button();
            this.btnMoveBookNameDown = new System.Windows.Forms.Button();
            this.btnMoveBookNameUp = new System.Windows.Forms.Button();
            this.lstBookNames = new System.Windows.Forms.ListView();
            this.columnHeader3 = new System.Windows.Forms.ColumnHeader();
            this.columnHeader4 = new System.Windows.Forms.ColumnHeader();
            this.label19 = new System.Windows.Forms.Label();
            this.tabBackMatter = new System.Windows.Forms.TabPage();
            this.btnAdjustBmFiles = new System.Windows.Forms.Button();
            this.btnMoveDownBackMatter = new System.Windows.Forms.Button();
            this.btnMoveUpBackMatter = new System.Windows.Forms.Button();
            this.label16 = new System.Windows.Forms.Label();
            this.label15 = new System.Windows.Forms.Label();
            this.lstBackMatter = new System.Windows.Forms.ListView();
            this.columnFiles = new System.Windows.Forms.ColumnHeader();
            this.columnHotlink = new System.Windows.Forms.ColumnHeader();
            this.advancedPage = new System.Windows.Forms.TabPage();
            this.tbxNotesClass = new System.Windows.Forms.TextBox();
            this.label8 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.tbxNonCanonical = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.tbxExtraClasses = new System.Windows.Forms.TextBox();
            this.columnHeader1 = new System.Windows.Forms.ColumnHeader();
            this.columnHeader2 = new System.Windows.Forms.ColumnHeader();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.clearReloadButton = new System.Windows.Forms.Button();
            this.tabControl1.SuspendLayout();
            this.inputPage.SuspendLayout();
            this.tabMisc.SuspendLayout();
            this.concPage.SuspendLayout();
            this.tabSorting.SuspendLayout();
            this.tabLocalization.SuspendLayout();
            this.taBookNames.SuspendLayout();
            this.tabBackMatter.SuspendLayout();
            this.advancedPage.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.inputPage);
            this.tabControl1.Controls.Add(this.tabMisc);
            this.tabControl1.Controls.Add(this.concPage);
            this.tabControl1.Controls.Add(this.tabSorting);
            this.tabControl1.Controls.Add(this.tabLocalization);
            this.tabControl1.Controls.Add(this.taBookNames);
            this.tabControl1.Controls.Add(this.tabBackMatter);
            this.tabControl1.Controls.Add(this.advancedPage);
            this.tabControl1.Location = new System.Drawing.Point(11, 12);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(693, 426);
            this.tabControl1.TabIndex = 0;
            // 
            // inputPage
            // 
            this.inputPage.Controls.Add(this.clearReloadButton);
            this.inputPage.Controls.Add(this.btnAdjustFiles);
            this.inputPage.Controls.Add(this.label20);
            this.inputPage.Controls.Add(this.lstFiles);
            this.inputPage.Location = new System.Drawing.Point(4, 22);
            this.inputPage.Name = "inputPage";
            this.inputPage.Padding = new System.Windows.Forms.Padding(3);
            this.inputPage.Size = new System.Drawing.Size(685, 400);
            this.inputPage.TabIndex = 1;
            this.inputPage.Text = "Input";
            this.inputPage.UseVisualStyleBackColor = true;
            // 
            // btnAdjustFiles
            // 
            this.btnAdjustFiles.Location = new System.Drawing.Point(9, 365);
            this.btnAdjustFiles.Name = "btnAdjustFiles";
            this.btnAdjustFiles.Size = new System.Drawing.Size(115, 23);
            this.btnAdjustFiles.TabIndex = 3;
            this.btnAdjustFiles.Text = "Adjust File List";
            this.btnAdjustFiles.UseVisualStyleBackColor = true;
            this.btnAdjustFiles.Click += new System.EventHandler(this.btnAdjustFiles_Click);
            // 
            // label20
            // 
            this.label20.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.label20.Location = new System.Drawing.Point(6, 11);
            this.label20.Name = "label20";
            this.label20.Size = new System.Drawing.Size(656, 58);
            this.label20.TabIndex = 2;
            this.label20.Text = resources.GetString("label20.Text");
            // 
            // lstFiles
            // 
            this.lstFiles.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colFilesStdAbbr,
            this.colFilesFile,
            this.colFilesVernAbbr,
            this.colFilesXrefs,
            this.colFilesIntro});
            this.lstFiles.Location = new System.Drawing.Point(6, 72);
            this.lstFiles.Name = "lstFiles";
            this.lstFiles.Size = new System.Drawing.Size(673, 287);
            this.lstFiles.TabIndex = 1;
            this.lstFiles.UseCompatibleStateImageBehavior = false;
            this.lstFiles.View = System.Windows.Forms.View.Details;
            this.lstFiles.SelectedIndexChanged += new System.EventHandler(this.lstFiles_SelectedIndexChanged);
            this.lstFiles.MouseUp += new System.Windows.Forms.MouseEventHandler(this.lstFiles_MouseUp);
            // 
            // colFilesStdAbbr
            // 
            this.colFilesStdAbbr.Text = "Std. Abbr.";
            this.colFilesStdAbbr.Width = 76;
            // 
            // colFilesFile
            // 
            this.colFilesFile.Text = "File";
            this.colFilesFile.Width = 178;
            // 
            // colFilesVernAbbr
            // 
            this.colFilesVernAbbr.Text = "Abbr. for refs";
            this.colFilesVernAbbr.Width = 90;
            // 
            // colFilesXrefs
            // 
            this.colFilesXrefs.Text = "Book name used in cross refs";
            this.colFilesXrefs.Width = 155;
            // 
            // colFilesIntro
            // 
            this.colFilesIntro.Text = "Introduction File";
            this.colFilesIntro.Width = 170;
            // 
            // tabMisc
            // 
            this.tabMisc.Controls.Add(this.btnRemoveInputProcess);
            this.tabMisc.Controls.Add(this.btnAddInputProcess);
            this.tabMisc.Controls.Add(this.listInputProcesses);
            this.tabMisc.Controls.Add(this.label22);
            this.tabMisc.Controls.Add(this.chkChapterPerfile);
            this.tabMisc.Location = new System.Drawing.Point(4, 22);
            this.tabMisc.Name = "tabMisc";
            this.tabMisc.Size = new System.Drawing.Size(685, 400);
            this.tabMisc.TabIndex = 4;
            this.tabMisc.Text = "Miscellaneous";
            this.tabMisc.UseVisualStyleBackColor = true;
            this.tabMisc.Click += new System.EventHandler(this.tabMisc_Click);
            // 
            // btnRemoveInputProcess
            // 
            this.btnRemoveInputProcess.Location = new System.Drawing.Point(413, 129);
            this.btnRemoveInputProcess.Name = "btnRemoveInputProcess";
            this.btnRemoveInputProcess.Size = new System.Drawing.Size(75, 23);
            this.btnRemoveInputProcess.TabIndex = 4;
            this.btnRemoveInputProcess.Text = "Remove";
            this.btnRemoveInputProcess.UseVisualStyleBackColor = true;
            this.btnRemoveInputProcess.Click += new System.EventHandler(this.btnRemoveInputProcess_Click);
            // 
            // btnAddInputProcess
            // 
            this.btnAddInputProcess.Location = new System.Drawing.Point(411, 90);
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
            this.listInputProcesses.Location = new System.Drawing.Point(15, 81);
            this.listInputProcesses.Name = "listInputProcesses";
            this.listInputProcesses.Size = new System.Drawing.Size(362, 134);
            this.listInputProcesses.TabIndex = 2;
            // 
            // label22
            // 
            this.label22.AutoSize = true;
            this.label22.Location = new System.Drawing.Point(19, 51);
            this.label22.Name = "label22";
            this.label22.Size = new System.Drawing.Size(538, 13);
            this.label22.TabIndex = 1;
            this.label22.Text = "If you are using the option of applying your own transformation to USFM,  specify" +
                " the necessary conversions here";
            // 
            // chkChapterPerfile
            // 
            this.chkChapterPerfile.AutoSize = true;
            this.chkChapterPerfile.Location = new System.Drawing.Point(18, 16);
            this.chkChapterPerfile.Name = "chkChapterPerfile";
            this.chkChapterPerfile.Size = new System.Drawing.Size(299, 17);
            this.chkChapterPerfile.TabIndex = 0;
            this.chkChapterPerfile.Text = "Make separate HTML file for each chapter (faster loading)";
            this.chkChapterPerfile.UseVisualStyleBackColor = true;
            // 
            // concPage
            // 
            this.concPage.Controls.Add(this.label21);
            this.concPage.Controls.Add(this.tbxPhrases);
            this.concPage.Controls.Add(this.label5);
            this.concPage.Controls.Add(this.tbxExcludeWords);
            this.concPage.Controls.Add(this.label4);
            this.concPage.Controls.Add(this.tbxMaxFreq);
            this.concPage.Controls.Add(this.label3);
            this.concPage.Controls.Add(this.tbxMinContext);
            this.concPage.Controls.Add(this.label2);
            this.concPage.Controls.Add(this.tbxMaxContext);
            this.concPage.Controls.Add(this.label1);
            this.concPage.Controls.Add(this.tbxWordformingChars);
            this.concPage.Controls.Add(this.chkMergeCase);
            this.concPage.Location = new System.Drawing.Point(4, 22);
            this.concPage.Name = "concPage";
            this.concPage.Padding = new System.Windows.Forms.Padding(3);
            this.concPage.Size = new System.Drawing.Size(685, 400);
            this.concPage.TabIndex = 0;
            this.concPage.Text = "Concordance";
            this.concPage.UseVisualStyleBackColor = true;
            // 
            // label21
            // 
            this.label21.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.label21.AutoSize = true;
            this.label21.Location = new System.Drawing.Point(418, 187);
            this.label21.Name = "label21";
            this.label21.Size = new System.Drawing.Size(242, 13);
            this.label21.TabIndex = 12;
            this.label21.Text = "Make extra entries for these phrases (one per line)";
            // 
            // tbxPhrases
            // 
            this.tbxPhrases.Location = new System.Drawing.Point(421, 207);
            this.tbxPhrases.Multiline = true;
            this.tbxPhrases.Name = "tbxPhrases";
            this.tbxPhrases.Size = new System.Drawing.Size(248, 175);
            this.tbxPhrases.TabIndex = 11;
            this.tbxPhrases.TextChanged += new System.EventHandler(this.textBox2_TextChanged);
            // 
            // label5
            // 
            this.label5.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(17, 187);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(256, 13);
            this.label5.TabIndex = 10;
            this.label5.Text = "Also omit words in the following list (space separated)";
            // 
            // tbxExcludeWords
            // 
            this.tbxExcludeWords.Location = new System.Drawing.Point(13, 207);
            this.tbxExcludeWords.Multiline = true;
            this.tbxExcludeWords.Name = "tbxExcludeWords";
            this.tbxExcludeWords.Size = new System.Drawing.Size(389, 175);
            this.tbxExcludeWords.TabIndex = 9;
            this.tbxExcludeWords.Text = "- --";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(14, 163);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(473, 13);
            this.label4.TabIndex = 8;
            this.label4.Text = "Maximum frequency (words occurring more often than this will be excluded; use \'un" +
                "limited\' if no limit)";
            // 
            // tbxMaxFreq
            // 
            this.tbxMaxFreq.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.tbxMaxFreq.Location = new System.Drawing.Point(579, 160);
            this.tbxMaxFreq.Name = "tbxMaxFreq";
            this.tbxMaxFreq.Size = new System.Drawing.Size(100, 20);
            this.tbxMaxFreq.TabIndex = 7;
            this.tbxMaxFreq.Text = "unlimited";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(14, 131);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(345, 13);
            this.label3.TabIndex = 6;
            this.label3.Text = "Word-split context length (split words if needed to get this much context)";
            // 
            // tbxMinContext
            // 
            this.tbxMinContext.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.tbxMinContext.Location = new System.Drawing.Point(579, 128);
            this.tbxMinContext.Name = "tbxMinContext";
            this.tbxMinContext.Size = new System.Drawing.Size(100, 20);
            this.tbxMinContext.TabIndex = 5;
            this.tbxMinContext.Text = "40";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(14, 96);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(388, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "Maximum context length (number of characters shown before and after key word)";
            // 
            // tbxMaxContext
            // 
            this.tbxMaxContext.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.tbxMaxContext.Location = new System.Drawing.Point(579, 93);
            this.tbxMaxContext.Name = "tbxMaxContext";
            this.tbxMaxContext.Size = new System.Drawing.Size(100, 20);
            this.tbxMaxContext.TabIndex = 3;
            this.tbxMaxContext.Text = "60";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 39);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(403, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Characters that should be treated as parts of words (in addition to standard Unic" +
                "ode)";
            // 
            // tbxWordformingChars
            // 
            this.tbxWordformingChars.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.tbxWordformingChars.Location = new System.Drawing.Point(12, 61);
            this.tbxWordformingChars.Name = "tbxWordformingChars";
            this.tbxWordformingChars.Size = new System.Drawing.Size(667, 20);
            this.tbxWordformingChars.TabIndex = 1;
            this.tbxWordformingChars.Text = "\'-";
            // 
            // chkMergeCase
            // 
            this.chkMergeCase.AutoSize = true;
            this.chkMergeCase.Checked = true;
            this.chkMergeCase.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkMergeCase.Location = new System.Drawing.Point(12, 14);
            this.chkMergeCase.Name = "chkMergeCase";
            this.chkMergeCase.Size = new System.Drawing.Size(300, 17);
            this.chkMergeCase.TabIndex = 0;
            this.chkMergeCase.Text = "Merge wordforms that differ only by case into single entries";
            this.chkMergeCase.UseVisualStyleBackColor = true;
            // 
            // tabSorting
            // 
            this.tabSorting.Controls.Add(this.comboSort);
            this.tabSorting.Controls.Add(this.btnTestSort);
            this.tabSorting.Controls.Add(this.tbxTestWords);
            this.tabSorting.Controls.Add(this.tbxCollating);
            this.tabSorting.Controls.Add(this.label23);
            this.tabSorting.Location = new System.Drawing.Point(4, 22);
            this.tabSorting.Name = "tabSorting";
            this.tabSorting.Size = new System.Drawing.Size(685, 400);
            this.tabSorting.TabIndex = 7;
            this.tabSorting.Text = "Sorting";
            this.tabSorting.UseVisualStyleBackColor = true;
            // 
            // comboSort
            // 
            this.comboSort.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboSort.FormattingEnabled = true;
            this.comboSort.Items.AddRange(new object[] {
            "Default (standard Unicode)",
            "Custom Simple (Shoebox style)",
            "Custom ICU"});
            this.comboSort.Location = new System.Drawing.Point(18, 67);
            this.comboSort.Name = "comboSort";
            this.comboSort.Size = new System.Drawing.Size(229, 21);
            this.comboSort.TabIndex = 12;
            this.comboSort.SelectedIndexChanged += new System.EventHandler(this.comboSort_SelectedIndexChanged);
            // 
            // btnTestSort
            // 
            this.btnTestSort.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnTestSort.Location = new System.Drawing.Point(345, 63);
            this.btnTestSort.Name = "btnTestSort";
            this.btnTestSort.Size = new System.Drawing.Size(117, 23);
            this.btnTestSort.TabIndex = 3;
            this.btnTestSort.Text = "Test Sort";
            this.btnTestSort.UseVisualStyleBackColor = true;
            this.btnTestSort.Click += new System.EventHandler(this.btnTestSort_Click);
            // 
            // tbxTestWords
            // 
            this.tbxTestWords.Location = new System.Drawing.Point(345, 98);
            this.tbxTestWords.Multiline = true;
            this.tbxTestWords.Name = "tbxTestWords";
            this.tbxTestWords.Size = new System.Drawing.Size(326, 290);
            this.tbxTestWords.TabIndex = 11;
            // 
            // tbxCollating
            // 
            this.tbxCollating.Location = new System.Drawing.Point(13, 98);
            this.tbxCollating.Multiline = true;
            this.tbxCollating.Name = "tbxCollating";
            this.tbxCollating.Size = new System.Drawing.Size(326, 290);
            this.tbxCollating.TabIndex = 10;
            this.tbxCollating.Text = "A a\r\nB b\r\nC c\r\nD d\r\nE e\r\nF f\r\nG g\r\nH h\r\nI i\r\nJ j\r\nK k\r\nL l\r\nM m\r\nN n\r\nO o\r\nP p\r\nQ" +
                " q\r\nR r\r\nS s\r\nT t\r\nU u\r\nV v\r\nW w\r\nX x\r\nZ z\r\n";
            // 
            // label23
            // 
            this.label23.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.label23.Location = new System.Drawing.Point(10, 12);
            this.label23.Name = "label23";
            this.label23.Size = new System.Drawing.Size(656, 48);
            this.label23.TabIndex = 1;
            this.label23.Text = resources.GetString("label23.Text");
            // 
            // tabLocalization
            // 
            this.tabLocalization.Controls.Add(this.tbxNextChap);
            this.tabLocalization.Controls.Add(this.label18);
            this.tabLocalization.Controls.Add(this.tbxPrevChap);
            this.tabLocalization.Controls.Add(this.label17);
            this.tabLocalization.Controls.Add(this.tbxLoading);
            this.tabLocalization.Controls.Add(this.label14);
            this.tabLocalization.Controls.Add(this.tbxIntroduction);
            this.tabLocalization.Controls.Add(this.label13);
            this.tabLocalization.Controls.Add(this.tbxBookChap);
            this.tabLocalization.Controls.Add(this.label12);
            this.tabLocalization.Controls.Add(this.tbxConcordance);
            this.tabLocalization.Controls.Add(this.label11);
            this.tabLocalization.Controls.Add(this.tbxNotesRef);
            this.tabLocalization.Controls.Add(this.label10);
            this.tabLocalization.Controls.Add(this.tbxHeadingRef);
            this.tabLocalization.Controls.Add(this.label9);
            this.tabLocalization.Location = new System.Drawing.Point(4, 22);
            this.tabLocalization.Name = "tabLocalization";
            this.tabLocalization.Size = new System.Drawing.Size(685, 400);
            this.tabLocalization.TabIndex = 3;
            this.tabLocalization.Text = "Localization";
            this.tabLocalization.UseVisualStyleBackColor = true;
            // 
            // tbxNextChap
            // 
            this.tbxNextChap.Location = new System.Drawing.Point(357, 287);
            this.tbxNextChap.Name = "tbxNextChap";
            this.tbxNextChap.Size = new System.Drawing.Size(156, 20);
            this.tbxNextChap.TabIndex = 27;
            this.tbxNextChap.Text = "Next Chapter";
            // 
            // label18
            // 
            this.label18.AutoSize = true;
            this.label18.Location = new System.Drawing.Point(15, 290);
            this.label18.Name = "label18";
            this.label18.Size = new System.Drawing.Size(180, 13);
            this.label18.TabIndex = 26;
            this.label18.Text = "Text of button linking to next chapter";
            // 
            // tbxPrevChap
            // 
            this.tbxPrevChap.Location = new System.Drawing.Point(357, 247);
            this.tbxPrevChap.Name = "tbxPrevChap";
            this.tbxPrevChap.Size = new System.Drawing.Size(156, 20);
            this.tbxPrevChap.TabIndex = 25;
            this.tbxPrevChap.Text = "Previous Chapter";
            // 
            // label17
            // 
            this.label17.AutoSize = true;
            this.label17.Location = new System.Drawing.Point(15, 250);
            this.label17.Name = "label17";
            this.label17.Size = new System.Drawing.Size(200, 13);
            this.label17.TabIndex = 24;
            this.label17.Text = "Text of button linking to previous chapter";
            // 
            // tbxLoading
            // 
            this.tbxLoading.Location = new System.Drawing.Point(357, 207);
            this.tbxLoading.Name = "tbxLoading";
            this.tbxLoading.Size = new System.Drawing.Size(156, 20);
            this.tbxLoading.TabIndex = 23;
            this.tbxLoading.Text = "Loading...";
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Location = new System.Drawing.Point(15, 210);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(191, 13);
            this.label14.TabIndex = 22;
            this.label14.Text = "String displayed while a page is loading";
            // 
            // tbxIntroduction
            // 
            this.tbxIntroduction.Location = new System.Drawing.Point(357, 174);
            this.tbxIntroduction.Name = "tbxIntroduction";
            this.tbxIntroduction.Size = new System.Drawing.Size(156, 20);
            this.tbxIntroduction.TabIndex = 21;
            this.tbxIntroduction.Text = "Introduction";
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(15, 177);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(297, 13);
            this.label13.TabIndex = 20;
            this.label13.Text = "Hotlink to the Introduction of any individual book that has one";
            // 
            // tbxBookChap
            // 
            this.tbxBookChap.Location = new System.Drawing.Point(357, 139);
            this.tbxBookChap.Name = "tbxBookChap";
            this.tbxBookChap.Size = new System.Drawing.Size(156, 20);
            this.tbxBookChap.TabIndex = 19;
            this.tbxBookChap.Text = "Books and Chapters";
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(15, 139);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(326, 13);
            this.label12.TabIndex = 18;
            this.label12.Text = "Hotlink in the concordace index back to the main Table of Contents";
            // 
            // tbxConcordance
            // 
            this.tbxConcordance.Location = new System.Drawing.Point(357, 101);
            this.tbxConcordance.Name = "tbxConcordance";
            this.tbxConcordance.Size = new System.Drawing.Size(156, 20);
            this.tbxConcordance.TabIndex = 17;
            this.tbxConcordance.Text = "Concordance";
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(15, 101);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(273, 13);
            this.label11.TabIndex = 16;
            this.label11.Text = "Hotlink in the main table of contents to the Concordance";
            // 
            // tbxNotesRef
            // 
            this.tbxNotesRef.Location = new System.Drawing.Point(357, 61);
            this.tbxNotesRef.Name = "tbxNotesRef";
            this.tbxNotesRef.Size = new System.Drawing.Size(156, 20);
            this.tbxNotesRef.TabIndex = 15;
            this.tbxNotesRef.Text = "fn";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(15, 61);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(251, 13);
            this.label10.TabIndex = 14;
            this.label10.Text = "String to use as the \'reference\' of words in footnotes";
            // 
            // tbxHeadingRef
            // 
            this.tbxHeadingRef.Location = new System.Drawing.Point(357, 21);
            this.tbxHeadingRef.Name = "tbxHeadingRef";
            this.tbxHeadingRef.Size = new System.Drawing.Size(156, 20);
            this.tbxHeadingRef.TabIndex = 13;
            this.tbxHeadingRef.Text = "head";
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
            // taBookNames
            // 
            this.taBookNames.Controls.Add(this.btnAdjustBookNamesList);
            this.taBookNames.Controls.Add(this.btnMoveBookNameDown);
            this.taBookNames.Controls.Add(this.btnMoveBookNameUp);
            this.taBookNames.Controls.Add(this.lstBookNames);
            this.taBookNames.Controls.Add(this.label19);
            this.taBookNames.Location = new System.Drawing.Point(4, 22);
            this.taBookNames.Name = "taBookNames";
            this.taBookNames.Size = new System.Drawing.Size(685, 400);
            this.taBookNames.TabIndex = 6;
            this.taBookNames.Text = "Book Names";
            this.taBookNames.UseVisualStyleBackColor = true;
            this.taBookNames.Click += new System.EventHandler(this.taBookNames_Click);
            // 
            // btnAdjustBookNamesList
            // 
            this.btnAdjustBookNamesList.Location = new System.Drawing.Point(15, 357);
            this.btnAdjustBookNamesList.Name = "btnAdjustBookNamesList";
            this.btnAdjustBookNamesList.Size = new System.Drawing.Size(114, 23);
            this.btnAdjustBookNamesList.TabIndex = 8;
            this.btnAdjustBookNamesList.Text = "Adjust File List";
            this.btnAdjustBookNamesList.UseVisualStyleBackColor = true;
            this.btnAdjustBookNamesList.Click += new System.EventHandler(this.btnAdjustBookNamesList_Click);
            // 
            // btnMoveBookNameDown
            // 
            this.btnMoveBookNameDown.Location = new System.Drawing.Point(596, 357);
            this.btnMoveBookNameDown.Name = "btnMoveBookNameDown";
            this.btnMoveBookNameDown.Size = new System.Drawing.Size(75, 23);
            this.btnMoveBookNameDown.TabIndex = 7;
            this.btnMoveBookNameDown.Text = "Move Down";
            this.btnMoveBookNameDown.UseVisualStyleBackColor = true;
            this.btnMoveBookNameDown.Click += new System.EventHandler(this.btnMoveBookNameDown_Click);
            // 
            // btnMoveBookNameUp
            // 
            this.btnMoveBookNameUp.Location = new System.Drawing.Point(481, 357);
            this.btnMoveBookNameUp.Name = "btnMoveBookNameUp";
            this.btnMoveBookNameUp.Size = new System.Drawing.Size(75, 23);
            this.btnMoveBookNameUp.TabIndex = 6;
            this.btnMoveBookNameUp.Text = "Move Up";
            this.btnMoveBookNameUp.UseVisualStyleBackColor = true;
            this.btnMoveBookNameUp.Click += new System.EventHandler(this.btnMoveBookNameUp_Click);
            // 
            // lstBookNames
            // 
            this.lstBookNames.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader3,
            this.columnHeader4});
            this.lstBookNames.Location = new System.Drawing.Point(15, 79);
            this.lstBookNames.Name = "lstBookNames";
            this.lstBookNames.Size = new System.Drawing.Size(656, 262);
            this.lstBookNames.TabIndex = 5;
            this.lstBookNames.UseCompatibleStateImageBehavior = false;
            this.lstBookNames.View = System.Windows.Forms.View.Details;
            this.lstBookNames.MouseUp += new System.Windows.Forms.MouseEventHandler(this.lstBookNames_MouseUp);
            // 
            // columnHeader3
            // 
            this.columnHeader3.Text = "Language";
            this.columnHeader3.Width = 327;
            // 
            // columnHeader4
            // 
            this.columnHeader4.Text = "Column header in book names file";
            this.columnHeader4.Width = 321;
            // 
            // label19
            // 
            this.label19.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.label19.Location = new System.Drawing.Point(12, 18);
            this.label19.Name = "label19";
            this.label19.Size = new System.Drawing.Size(656, 58);
            this.label19.TabIndex = 0;
            this.label19.Text = resources.GetString("label19.Text");
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
            this.lstBackMatter.MouseUp += new System.Windows.Forms.MouseEventHandler(this.lstBackMatter_MouseUp);
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
            // advancedPage
            // 
            this.advancedPage.Controls.Add(this.tbxNotesClass);
            this.advancedPage.Controls.Add(this.label8);
            this.advancedPage.Controls.Add(this.label7);
            this.advancedPage.Controls.Add(this.tbxNonCanonical);
            this.advancedPage.Controls.Add(this.label6);
            this.advancedPage.Controls.Add(this.tbxExtraClasses);
            this.advancedPage.Location = new System.Drawing.Point(4, 22);
            this.advancedPage.Name = "advancedPage";
            this.advancedPage.Size = new System.Drawing.Size(685, 400);
            this.advancedPage.TabIndex = 2;
            this.advancedPage.Text = "Advanced";
            this.advancedPage.UseVisualStyleBackColor = true;
            // 
            // tbxNotesClass
            // 
            this.tbxNotesClass.Location = new System.Drawing.Point(249, 272);
            this.tbxNotesClass.Name = "tbxNotesClass";
            this.tbxNotesClass.Size = new System.Drawing.Size(143, 20);
            this.tbxNotesClass.TabIndex = 5;
            this.tbxNotesClass.Text = "footnotes";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(16, 272);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(192, 13);
            this.label8.TabIndex = 4;
            this.label8.Text = "Division class which contains footnotes";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(15, 139);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(451, 13);
            this.label7.TabIndex = 3;
            this.label7.Text = "List of HTML element classes containing non-canonical material to concord without" +
                " references";
            // 
            // tbxNonCanonical
            // 
            this.tbxNonCanonical.Location = new System.Drawing.Point(11, 160);
            this.tbxNonCanonical.Multiline = true;
            this.tbxNonCanonical.Name = "tbxNonCanonical";
            this.tbxNonCanonical.Size = new System.Drawing.Size(663, 98);
            this.tbxNonCanonical.TabIndex = 2;
            this.tbxNonCanonical.Text = "sectionheading maintitle2 footnote sectionsubheading";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(15, 11);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(423, 13);
            this.label6.TabIndex = 1;
            this.label6.Text = "List of HTML element classes for which the content should be excluded from concor" +
                "ding";
            // 
            // tbxExtraClasses
            // 
            this.tbxExtraClasses.Location = new System.Drawing.Point(11, 32);
            this.tbxExtraClasses.Multiline = true;
            this.tbxExtraClasses.Name = "tbxExtraClasses";
            this.tbxExtraClasses.Size = new System.Drawing.Size(663, 98);
            this.tbxExtraClasses.TabIndex = 0;
            this.tbxExtraClasses.Text = "verse chapter notemark crmark crossRefNote parallel parallelSub noteBackRef popup" +
                " crpopup";
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
            this.btnOK.TabIndex = 1;
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
            this.btnCancel.TabIndex = 2;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // clearReloadButton
            // 
            this.clearReloadButton.Location = new System.Drawing.Point(152, 365);
            this.clearReloadButton.Name = "clearReloadButton";
            this.clearReloadButton.Size = new System.Drawing.Size(171, 23);
            this.clearReloadButton.TabIndex = 4;
            this.clearReloadButton.Text = "Clear and reload file list";
            this.clearReloadButton.UseVisualStyleBackColor = true;
            this.clearReloadButton.Click += new System.EventHandler(this.clearReloadButton_Click);
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
            this.tabControl1.ResumeLayout(false);
            this.inputPage.ResumeLayout(false);
            this.tabMisc.ResumeLayout(false);
            this.tabMisc.PerformLayout();
            this.concPage.ResumeLayout(false);
            this.concPage.PerformLayout();
            this.tabSorting.ResumeLayout(false);
            this.tabSorting.PerformLayout();
            this.tabLocalization.ResumeLayout(false);
            this.tabLocalization.PerformLayout();
            this.taBookNames.ResumeLayout(false);
            this.tabBackMatter.ResumeLayout(false);
            this.tabBackMatter.PerformLayout();
            this.advancedPage.ResumeLayout(false);
            this.advancedPage.PerformLayout();
            this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.TabControl tabControl1;
		private System.Windows.Forms.TabPage concPage;
		private System.Windows.Forms.TabPage inputPage;
		private System.Windows.Forms.CheckBox chkMergeCase;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TextBox tbxWordformingChars;
		private System.Windows.Forms.TextBox tbxMaxContext;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.TextBox tbxMinContext;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.TextBox tbxMaxFreq;
		private System.Windows.Forms.TextBox tbxExcludeWords;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.TabPage advancedPage;
		private System.Windows.Forms.TextBox tbxExtraClasses;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.TextBox tbxNonCanonical;
		private System.Windows.Forms.TextBox tbxNotesClass;
		private System.Windows.Forms.Label label8;
		private System.Windows.Forms.TabPage tabLocalization;
		private System.Windows.Forms.TextBox tbxLoading;
		private System.Windows.Forms.Label label14;
		private System.Windows.Forms.TextBox tbxIntroduction;
		private System.Windows.Forms.Label label13;
		private System.Windows.Forms.TextBox tbxBookChap;
		private System.Windows.Forms.Label label12;
		private System.Windows.Forms.TextBox tbxConcordance;
		private System.Windows.Forms.Label label11;
		private System.Windows.Forms.TextBox tbxNotesRef;
		private System.Windows.Forms.Label label10;
		private System.Windows.Forms.TextBox tbxHeadingRef;
		private System.Windows.Forms.Label label9;
		private System.Windows.Forms.TabPage tabMisc;
		private System.Windows.Forms.TabPage tabBackMatter;
		private System.Windows.Forms.Label label15;
		private System.Windows.Forms.ListView lstBackMatter;
		private System.Windows.Forms.ColumnHeader columnFiles;
		private System.Windows.Forms.ColumnHeader columnHotlink;
		private System.Windows.Forms.Label label16;
		private System.Windows.Forms.CheckBox chkChapterPerfile;
		private System.Windows.Forms.TextBox tbxNextChap;
		private System.Windows.Forms.Label label18;
		private System.Windows.Forms.TextBox tbxPrevChap;
		private System.Windows.Forms.Label label17;
		private System.Windows.Forms.Button btnMoveDownBackMatter;
		private System.Windows.Forms.Button btnMoveUpBackMatter;
		private System.Windows.Forms.TabPage taBookNames;
		private System.Windows.Forms.Label label19;
		private System.Windows.Forms.Button btnMoveBookNameDown;
		private System.Windows.Forms.Button btnMoveBookNameUp;
		private System.Windows.Forms.ListView lstBookNames;
		private System.Windows.Forms.ColumnHeader columnHeader3;
		private System.Windows.Forms.ColumnHeader columnHeader4;
		private System.Windows.Forms.ColumnHeader columnHeader1;
		private System.Windows.Forms.ColumnHeader columnHeader2;
		private System.Windows.Forms.ListView lstFiles;
		private System.Windows.Forms.ColumnHeader colFilesStdAbbr;
		private System.Windows.Forms.ColumnHeader colFilesFile;
		private System.Windows.Forms.ColumnHeader colFilesVernAbbr;
		private System.Windows.Forms.ColumnHeader colFilesXrefs;
		private System.Windows.Forms.Label label20;
		private System.Windows.Forms.ColumnHeader colFilesIntro;
		private System.Windows.Forms.Button btnAdjustFiles;
		private System.Windows.Forms.Label label21;
		private System.Windows.Forms.TextBox tbxPhrases;
		private System.Windows.Forms.Label label22;
		private System.Windows.Forms.Button btnAddInputProcess;
		private System.Windows.Forms.ListBox listInputProcesses;
		private System.Windows.Forms.Button btnRemoveInputProcess;
		private System.Windows.Forms.Button btnOK;
		private System.Windows.Forms.Button btnCancel;
		private System.Windows.Forms.Button btnAdjustBmFiles;
		private System.Windows.Forms.Button btnAdjustBookNamesList;
		private System.Windows.Forms.TabPage tabSorting;
		private System.Windows.Forms.TextBox tbxCollating;
		private System.Windows.Forms.Label label23;
		private System.Windows.Forms.Button btnTestSort;
		private System.Windows.Forms.TextBox tbxTestWords;
		private System.Windows.Forms.ComboBox comboSort;
        private System.Windows.Forms.Button clearReloadButton;
	}
}