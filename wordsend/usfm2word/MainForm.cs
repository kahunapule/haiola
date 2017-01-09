// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2004, SIL International. All Rights Reserved.   
// <copyright from='2004' to='2004' company='SIL International'>
//		Copyright © 2004, SIL International. All Rights Reserved.   
//    
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
// File: MainForm.cs
// Responsibility: (Kahunapule) Michael P. Johnson
// Last reviewed: 
// 
// <remarks>
// This file is contains the main form for the Windows UI version
// of the USFM to WordML converter. Most of the inner workings of the
// WordSend Bible format conversion project are in the external DLL
// BibleFileLib.dll.
// </remarks>
// --------------------------------------------------------------------------------------------

using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.Win32;
using WordSend;

namespace WordSend
{

	/// <summary>
	/// This is the main UI starting point-- a tabbed dialog box/wizard sort
	/// of thing.
	/// </summary>
	public class MainForm : System.Windows.Forms.Form
	{
		private System.Windows.Forms.TabControl tabControl1;
		private System.Windows.Forms.TabPage cvTab;
		private System.Windows.Forms.TabPage footnoteTab;
		private System.Windows.Forms.TabPage usfmFileTab;
		private System.Windows.Forms.TabPage PsalmsTabPage;
		private System.Windows.Forms.TabPage outputTabPage;
		private System.Windows.Forms.TabPage optionsFileTabPage;
		private System.Windows.Forms.TabPage goTabPage;
		private System.Windows.Forms.OpenFileDialog openFileDialog1;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Button deleteButton;
		private System.Windows.Forms.ListBox usfmFileListBox;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.TextBox outputTextBox;
		private System.Windows.Forms.SaveFileDialog saveFileDialog1;
		private System.Windows.Forms.Button browseOutputButton;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Button readOptionsButton;
		private System.Windows.Forms.Button saveOptionsButton;
		private System.Windows.Forms.Label optionsFileLabel;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.RadioButton dropCapRadioButton;
		private System.Windows.Forms.CheckBox chapter1CheckBox;
		private System.Windows.Forms.CheckBox verse1CheckBox;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.TextBox chapterNameTextBox;
		private System.Windows.Forms.Label label8;
		private System.Windows.Forms.TextBox chapterSuffixTextBox;
		private System.Windows.Forms.Label label9;
		private System.Windows.Forms.Label label10;
		private System.Windows.Forms.GroupBox groupBox2;
		private System.Windows.Forms.Label label11;
		private System.Windows.Forms.Label label12;
		private System.Windows.Forms.TextBox vernacularPsalmTextBox;
		private System.Windows.Forms.Label label13;
		private System.Windows.Forms.TextBox vernacularPsalmSuffixTextBox;
		private System.Windows.Forms.CheckBox labelPsalmV1CheckBox;
		private System.Windows.Forms.CheckBox labelPsalm1CheckBox;
		private System.Windows.Forms.RadioButton normalPsalmTitleRadioButton;
		private System.Windows.Forms.RadioButton dropCapPsalmRadioButton;
		private System.Windows.Forms.Button browseUSFMButton;
		private System.Windows.Forms.RadioButton chapterHeadingRadioButton;
		private System.Windows.Forms.Label label14;
		private System.Windows.Forms.TextBox verseSuffixTextBox;
		private System.Windows.Forms.CheckBox insertCallingVerseRefCheckBox;
		private System.Windows.Forms.CheckBox customFootnoteCallerCheckBox;
		private System.Windows.Forms.TextBox customFootnoteCallerTextBox;
		private System.Windows.Forms.CheckBox insertXrefVerseCheckBox;
		private System.Windows.Forms.CheckBox customXrefCallerCheckBox;
		private System.Windows.Forms.TextBox customXrefCallerTextBox;
		private System.Windows.Forms.Button convertNowButton;
		private System.Windows.Forms.Button exitButton;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;
		private System.Windows.Forms.Label label15;
		private System.Windows.Forms.Button createOptionsButton;
		private System.Windows.Forms.TabPage templatePage;
		private System.Windows.Forms.Button templateBrowseButton;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.Label templateFileLabel;
		private System.Windows.Forms.Label coprLabel;
		private System.Windows.Forms.Button backButton;
		private System.Windows.Forms.Button nextButton;
		private System.Windows.Forms.CheckBox openWordCheckBox;
		private System.Windows.Forms.Label label17;
		private System.Windows.Forms.TextBox versePrefixTextBox;
		private System.Windows.Forms.ListBox statusListBox;
		private System.Windows.Forms.CheckBox embedUsfxCheckBox;
		private System.Windows.Forms.GroupBox groupBox3;
		private System.Windows.Forms.CheckBox suppressIndentCheckBox;
		private System.Windows.Forms.Label label18;
		private System.Windows.Forms.NumericUpDown numLinesNumericUpDown;
		private System.Windows.Forms.TextBox horizTextBox;
		private System.Windows.Forms.TextBox sizeTextBox;
		private System.Windows.Forms.Label label19;
		private System.Windows.Forms.Label label20;
		private System.Windows.Forms.Label label21;
		private System.Windows.Forms.Label label22;
		private System.Windows.Forms.TextBox spacingTextBox;
		private System.Windows.Forms.TextBox positionTextBox;
		private System.Windows.Forms.TabPage MergetabPage;
		private System.Windows.Forms.Label label23;
		private System.Windows.Forms.CheckBox xrefCheckBox;
		private System.Windows.Forms.Button browseXrefButton;
		private System.Windows.Forms.Label xrefLabel;
		private System.Windows.Forms.Label label24;
		private System.Windows.Forms.Button changeListButton;
		private System.Windows.Forms.Label label25;
		private System.Windows.Forms.Label substitutionFileLabel;
		private System.Windows.Forms.Label label26;
		private System.Windows.Forms.CheckBox enableSubstitutionsCheckBox;
		private System.Windows.Forms.OpenFileDialog openFileDialog2;
		private System.Windows.Forms.TabPage extrasTabPage;
		private System.Windows.Forms.Button usfxButton;
		private System.Windows.Forms.Label label27;
		private System.Windows.Forms.TextBox nameTextBox;
		private System.Windows.Forms.Label label16;
		private System.Windows.Forms.Button helpButton;
		private System.Windows.Forms.CheckBox autoCalcDropCapCheckBox;
		private System.Windows.Forms.Label label29;
		private System.Windows.Forms.TextBox dropCapBeforeTextBox;
		private System.Windows.Forms.Label label28;
		private System.Windows.Forms.CheckBox cropCheckBox;
		private System.Windows.Forms.Label label30;
		private System.Windows.Forms.TextBox pageWidthTextBox;
		private System.Windows.Forms.Label label31;
		private System.Windows.Forms.TextBox pageLengthTextBox;

		private string jobOptionsName;
		private string logName;
        private bool paratextMode;

		public void ReadAppIni()
		{
			int i;

			SFConverter.appIni = new XMLini(
				Environment.GetEnvironmentVariable("APPDATA")+"\\SIL\\WordSend\\usfm2word.xml");

			jobOptionsName = SFConverter.appIni.ReadString("jobOptionsName",
				Environment.GetEnvironmentVariable("APPDATA")+"\\SIL\\WordSend\\joboptions.xini");

			logName = "WordSendLog.txt";
            paratextMode = false;

			for (i = 0; i < commandLine.Length; i++)
			{	// Scan the command line
				string s = commandLine[i];
				if ((s != null) && (s.Length > 0))
				{
					if (((s[0] == '-') || (s[0] == '/')) && (s.Length > 1))
					{	// command line switch: take action
						switch (Char.ToLower(s[1]))
						{
							case 'j':	// Job options file name
								jobOptionsName = SFConverter.GetOption(ref i, commandLine);
								break;
							case 'l':	// Set log name
								logName = SFConverter.GetOption(ref i, commandLine);
								break;
                            case 'p':
                                paratextMode = true;
                                break;
							default:
								MessageBox.Show("Unrecognized command line switch: " + commandLine[i]);
								break;
						}
					}
					else
					{
						jobOptionsName = commandLine[i];
					}
				}
			}
			optionsFileLabel.Text = jobOptionsName;
		}

		public void WriteAppIni()
		{
			SFConverter.appIni.WriteString("jobOptionsName", jobOptionsName);
			SFConverter.appIni.Write();
		}

		protected LengthString horizFromText;
		protected LengthString dropCapSpacing;
		protected LengthString dropCapSize;
		protected LengthString dropCapPosition;
		protected LengthString dropCapBefore;
		protected double toTwips;

		public void ReadJobIni(string jobOptName)
		{
			SFConverter.jobIni = new XMLini(jobOptName);

			usfmFileListBox.Items.Clear();
			int numSfmFiles = SFConverter.jobIni.ReadInt("numSfmFiles", 0);
			int i;
			for (i = 0; i < numSfmFiles; i++)
			{
				usfmFileListBox.Items.Add((object)
					SFConverter.jobIni.ReadString("sfmFile"+i.ToString(), "*.sfm"));
			}
			templateFileLabel.Text = SFConverter.jobIni.ReadString("templateName",
				Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "Scripture.xml"));
			outputTextBox.Text = SFConverter.jobIni.ReadString("outputFileName", SFConverter.AppDir("output.xml"));
			openWordCheckBox.Checked = SFConverter.jobIni.ReadBool("openWord", true);
			dropCapRadioButton.Checked = SFConverter.jobIni.ReadBool("dropCap", true);
			chapterHeadingRadioButton.Checked = !dropCapRadioButton.Checked;
			chapter1CheckBox.Checked = SFConverter.jobIni.ReadBool("chapter1", true);
			verse1CheckBox.Checked = SFConverter.jobIni.ReadBool("verse1", true);
			chapterNameTextBox.Text = SFConverter.jobIni.ReadString("chapterName", "Chapter");
			chapterSuffixTextBox.Text = SFConverter.jobIni.ReadString("chapterSuffix", "");
			versePrefixTextBox.Text = SFConverter.jobIni.ReadString("versePrefix", "");
			verseSuffixTextBox.Text = SFConverter.jobIni.ReadString("verseSuffix", "nbhs");
			dropCapPsalmRadioButton.Checked = SFConverter.jobIni.ReadBool("dropCapPsalm", false);
			normalPsalmTitleRadioButton.Checked = !dropCapPsalmRadioButton.Checked;
			labelPsalm1CheckBox.Checked = SFConverter.jobIni.ReadBool("labelPsalm1", true);
			labelPsalmV1CheckBox.Checked = SFConverter.jobIni.ReadBool("labelPsalmV1", true);
			vernacularPsalmTextBox.Text = SFConverter.jobIni.ReadString("vernacularPsalm", "Psalm");
			vernacularPsalmSuffixTextBox.Text = SFConverter.jobIni.ReadString("vernacularPsalmSuffix", "");
			insertCallingVerseRefCheckBox.Checked = SFConverter.jobIni.ReadBool("insertCallingVerseRef", true);
			customFootnoteCallerCheckBox.Checked = SFConverter.jobIni.ReadBool("useCustomFootnoteCaller", false);
			customFootnoteCallerTextBox.Text = SFConverter.jobIni.ReadString("customFootnoteCaller", "* † ‡");
			customFootnoteCallerTextBox.Enabled = customFootnoteCallerCheckBox.Checked;
			insertXrefVerseCheckBox.Checked = SFConverter.jobIni.ReadBool("insertXrefVerse", true);
			customXrefCallerCheckBox.Checked = SFConverter.jobIni.ReadBool("useCustomXrefCaller", true);
			customXrefCallerTextBox.Text = SFConverter.jobIni.ReadString("customXrefCaller", "✡");
			customXrefCallerTextBox.Enabled = customXrefCallerCheckBox.Checked;
			openWordCheckBox.Checked = SFConverter.jobIni.ReadBool("openWord", true);
			embedUsfxCheckBox.Checked =  SFConverter.jobIni.ReadBool("embedUsfx", true);
			suppressIndentCheckBox.Checked = SFConverter.jobIni.ReadBool("suppressIndentWithDropCap", false);
			numLinesNumericUpDown.Value = SFConverter.jobIni.ReadInt("dropCapLines", 2);
			nameTextBox.Text = SFConverter.jobIni.ReadString("nameSpace", "");

			horizFromText = new LengthString(SFConverter.jobIni.ReadString("horizFromText", "72 twips"), 72, 't');
			horizTextBox.Text = horizFromText.Text;
			dropCapSpacing = new LengthString(SFConverter.jobIni.ReadString("dropCapSpacing", "459 twips"), 459.0, 't');
			spacingTextBox.Text = dropCapSpacing.Text;
			dropCapSize = new LengthString(SFConverter.jobIni.ReadString("dropCapSize", "26.5 pt"), 53, 'h');
			sizeTextBox.Text = dropCapSize.Text;
			dropCapPosition = new LengthString(SFConverter.jobIni.ReadString("dropCapPosition", "-3 pt"), -6.0, 'h');
			positionTextBox.Text = dropCapPosition.Text;
			dropCapBefore = new LengthString(SFConverter.jobIni.ReadString("dropCapBefore", "0 pt"), 0.0, 'p');
			dropCapBeforeTextBox.Text = dropCapBefore.Text;

			cropCheckBox.Checked = SFConverter.jobIni.ReadBool("includeCropMarks", false);
			pageWidthTextBox.Text = new LengthString(SFConverter.jobIni.ReadString("pageWidth", "150 mm"), 150.0, 'm').Text;
			pageLengthTextBox.Text = new LengthString(SFConverter.jobIni.ReadString("pageLength", "216 mm"), 216.0, 'm').Text;

			autoCalcDropCapCheckBox.Checked = SFConverter.jobIni.ReadBool("autoCalcDropCap", true);
			xrefLabel.Text = SFConverter.jobIni.ReadString("xrefName",
				Path.Combine(Path.GetDirectoryName(Application.ExecutablePath),"crossreference.xml"));
			xrefCheckBox.Checked = SFConverter.jobIni.ReadBool("mergeXref", false);
			substitutionFileLabel.Text =  SFConverter.jobIni.ReadString("substitutionName",
				Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "fixquotemarks.xml"));
			enableSubstitutionsCheckBox.Checked = SFConverter.jobIni.ReadBool("enableSubstitutions", false);
			changeListButton.Enabled = enableSubstitutionsCheckBox.Checked;
			substitutionFileLabel.Enabled = enableSubstitutionsCheckBox.Checked;
            if (paratextMode)
            {
                tabControl1.SelectedIndex = 1;
            }

		}

		protected string toRealNbsp(string s)
		{
			string result = s.Replace("nbsp", "\u00a0");
			return result.Replace("nbhs", "\u202f");
		}

		public static string[] commandLine;

		public void WriteJobIni(string jobOptName)
		{
			int numSfmFiles = usfmFileListBox.Items.Count;
			SFConverter.jobIni.WriteInt("numSfmFiles", numSfmFiles);
			int i;
			for (i = 0; i < numSfmFiles; i++)
			{
				SFConverter.jobIni.WriteString("sfmFile"+i.ToString(), (string) usfmFileListBox.Items[i]);
			}
			SFConverter.jobIni.WriteString("outputFileName", outputTextBox.Text);
			SFConverter.jobIni.WriteBool("openWord", openWordCheckBox.Checked);
			SFConverter.jobIni.WriteString("templateName", templateFileLabel.Text);
			SFConverter.jobIni.WriteBool("dropCap", dropCapRadioButton.Checked);
			SFConverter.jobIni.WriteBool("chapter1", chapter1CheckBox.Checked);
			SFConverter.jobIni.WriteBool("verse1", verse1CheckBox.Checked);
			SFConverter.jobIni.WriteString("chapterName", chapterNameTextBox.Text);
			SFConverter.jobIni.WriteString("chapterSuffix", chapterSuffixTextBox.Text);
			SFConverter.jobIni.WriteString("versePrefix", versePrefixTextBox.Text);
			SFConverter.jobIni.WriteString("verseNumberPrefix", toRealNbsp(versePrefixTextBox.Text));
			SFConverter.jobIni.WriteString("verseSuffix", verseSuffixTextBox.Text);
			SFConverter.jobIni.WriteString("verseNumberSuffix", toRealNbsp(verseSuffixTextBox.Text));
			SFConverter.jobIni.WriteBool("dropCapPsalm", dropCapPsalmRadioButton.Checked);
			SFConverter.jobIni.WriteBool("labelPsalm1", labelPsalm1CheckBox.Checked);
			SFConverter.jobIni.WriteBool("labelPsalmV1", labelPsalmV1CheckBox.Checked);
			SFConverter.jobIni.WriteString("vernacularPsalm", vernacularPsalmTextBox.Text);
			SFConverter.jobIni.WriteString("vernacularPsalmSuffix", vernacularPsalmSuffixTextBox.Text);
			SFConverter.jobIni.WriteBool("insertCallingVerseRef", insertCallingVerseRefCheckBox.Checked);
			SFConverter.jobIni.WriteBool("useCustomFootnoteCaller", customFootnoteCallerCheckBox.Checked);
			SFConverter.jobIni.WriteString("customFootnoteCaller", customFootnoteCallerTextBox.Text);
			SFConverter.jobIni.WriteBool("insertXrefVerse", insertXrefVerseCheckBox.Checked);
			SFConverter.jobIni.WriteBool("useCustomXrefCaller", customXrefCallerCheckBox.Checked);
			SFConverter.jobIni.WriteString("customXrefCaller", customXrefCallerTextBox.Text);
			SFConverter.jobIni.WriteBool("openWord", openWordCheckBox.Checked);
			SFConverter.jobIni.WriteBool("embedUsfx", embedUsfxCheckBox.Checked);
			SFConverter.jobIni.WriteBool("suppressIndentWithDropCap", suppressIndentCheckBox.Checked);
			SFConverter.jobIni.WriteInt("dropCapLines", (int)numLinesNumericUpDown.Value);
			SFConverter.jobIni.WriteString("horizFromText", horizFromText.Text);
			SFConverter.jobIni.WriteString("dropCapSpacing", dropCapSpacing.Text);
			SFConverter.jobIni.WriteString("dropCapSize", dropCapSize.Text);
			SFConverter.jobIni.WriteString("dropCapPosition", dropCapPosition.Text);
			SFConverter.jobIni.WriteString("dropCapBefore", dropCapBefore.Text);

			SFConverter.jobIni.WriteBool("includeCropMarks", cropCheckBox.Checked);
			SFConverter.jobIni.WriteString("pageWidth", pageWidthTextBox.Text);
			SFConverter.jobIni.WriteString("pageLength", pageLengthTextBox.Text);
			
			SFConverter.jobIni.WriteString("xrefName", xrefLabel.Text);
			SFConverter.jobIni.WriteBool("mergeXref", xrefCheckBox.Checked);
			SFConverter.jobIni.Write(jobOptName);
			SFConverter.jobIni.WriteString("substitutionName", substitutionFileLabel.Text);
			SFConverter.jobIni.WriteBool("enableSubstitutions", enableSubstitutionsCheckBox.Checked);
			SFConverter.jobIni.WriteString("nameSpace", nameTextBox.Text);
			SFConverter.jobIni.WriteBool("autoCalcDropCap", autoCalcDropCapCheckBox.Checked);
		}

		public MainForm()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			// Read persisted data from XMLini files.

			ReadAppIni();
			ReadJobIni(jobOptionsName);

            coprLabel.Text = String.Format("WordSend version {0}.{1} ©2003-{2} SIL, EBT, && eBible.org. Released under Gnu LGPL 3 or later.",
                haiola.Version.date, haiola.Version.time, haiola.Version.year);
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if (components != null) 
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.optionsFileTabPage = new System.Windows.Forms.TabPage();
            this.coprLabel = new System.Windows.Forms.Label();
            this.createOptionsButton = new System.Windows.Forms.Button();
            this.label4 = new System.Windows.Forms.Label();
            this.optionsFileLabel = new System.Windows.Forms.Label();
            this.saveOptionsButton = new System.Windows.Forms.Button();
            this.readOptionsButton = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.footnoteTab = new System.Windows.Forms.TabPage();
            this.label24 = new System.Windows.Forms.Label();
            this.customXrefCallerTextBox = new System.Windows.Forms.TextBox();
            this.customXrefCallerCheckBox = new System.Windows.Forms.CheckBox();
            this.insertXrefVerseCheckBox = new System.Windows.Forms.CheckBox();
            this.customFootnoteCallerTextBox = new System.Windows.Forms.TextBox();
            this.customFootnoteCallerCheckBox = new System.Windows.Forms.CheckBox();
            this.insertCallingVerseRefCheckBox = new System.Windows.Forms.CheckBox();
            this.label14 = new System.Windows.Forms.Label();
            this.templatePage = new System.Windows.Forms.TabPage();
            this.enableSubstitutionsCheckBox = new System.Windows.Forms.CheckBox();
            this.label26 = new System.Windows.Forms.Label();
            this.substitutionFileLabel = new System.Windows.Forms.Label();
            this.label25 = new System.Windows.Forms.Label();
            this.changeListButton = new System.Windows.Forms.Button();
            this.label17 = new System.Windows.Forms.Label();
            this.templateFileLabel = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.templateBrowseButton = new System.Windows.Forms.Button();
            this.usfmFileTab = new System.Windows.Forms.TabPage();
            this.browseUSFMButton = new System.Windows.Forms.Button();
            this.deleteButton = new System.Windows.Forms.Button();
            this.usfmFileListBox = new System.Windows.Forms.ListBox();
            this.label1 = new System.Windows.Forms.Label();
            this.MergetabPage = new System.Windows.Forms.TabPage();
            this.xrefLabel = new System.Windows.Forms.Label();
            this.browseXrefButton = new System.Windows.Forms.Button();
            this.xrefCheckBox = new System.Windows.Forms.CheckBox();
            this.label23 = new System.Windows.Forms.Label();
            this.outputTabPage = new System.Windows.Forms.TabPage();
            this.embedUsfxCheckBox = new System.Windows.Forms.CheckBox();
            this.label15 = new System.Windows.Forms.Label();
            this.browseOutputButton = new System.Windows.Forms.Button();
            this.outputTextBox = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.extrasTabPage = new System.Windows.Forms.TabPage();
            this.pageLengthTextBox = new System.Windows.Forms.TextBox();
            this.label31 = new System.Windows.Forms.Label();
            this.pageWidthTextBox = new System.Windows.Forms.TextBox();
            this.label30 = new System.Windows.Forms.Label();
            this.cropCheckBox = new System.Windows.Forms.CheckBox();
            this.label16 = new System.Windows.Forms.Label();
            this.label27 = new System.Windows.Forms.Label();
            this.nameTextBox = new System.Windows.Forms.TextBox();
            this.usfxButton = new System.Windows.Forms.Button();
            this.cvTab = new System.Windows.Forms.TabPage();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.label28 = new System.Windows.Forms.Label();
            this.dropCapBeforeTextBox = new System.Windows.Forms.TextBox();
            this.label29 = new System.Windows.Forms.Label();
            this.autoCalcDropCapCheckBox = new System.Windows.Forms.CheckBox();
            this.label22 = new System.Windows.Forms.Label();
            this.label21 = new System.Windows.Forms.Label();
            this.label20 = new System.Windows.Forms.Label();
            this.label19 = new System.Windows.Forms.Label();
            this.positionTextBox = new System.Windows.Forms.TextBox();
            this.sizeTextBox = new System.Windows.Forms.TextBox();
            this.spacingTextBox = new System.Windows.Forms.TextBox();
            this.horizTextBox = new System.Windows.Forms.TextBox();
            this.numLinesNumericUpDown = new System.Windows.Forms.NumericUpDown();
            this.label18 = new System.Windows.Forms.Label();
            this.suppressIndentCheckBox = new System.Windows.Forms.CheckBox();
            this.verseSuffixTextBox = new System.Windows.Forms.TextBox();
            this.label10 = new System.Windows.Forms.Label();
            this.versePrefixTextBox = new System.Windows.Forms.TextBox();
            this.label9 = new System.Windows.Forms.Label();
            this.chapterSuffixTextBox = new System.Windows.Forms.TextBox();
            this.label8 = new System.Windows.Forms.Label();
            this.chapterNameTextBox = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.verse1CheckBox = new System.Windows.Forms.CheckBox();
            this.chapter1CheckBox = new System.Windows.Forms.CheckBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.chapterHeadingRadioButton = new System.Windows.Forms.RadioButton();
            this.dropCapRadioButton = new System.Windows.Forms.RadioButton();
            this.label6 = new System.Windows.Forms.Label();
            this.PsalmsTabPage = new System.Windows.Forms.TabPage();
            this.labelPsalmV1CheckBox = new System.Windows.Forms.CheckBox();
            this.labelPsalm1CheckBox = new System.Windows.Forms.CheckBox();
            this.vernacularPsalmSuffixTextBox = new System.Windows.Forms.TextBox();
            this.label13 = new System.Windows.Forms.Label();
            this.vernacularPsalmTextBox = new System.Windows.Forms.TextBox();
            this.label12 = new System.Windows.Forms.Label();
            this.label11 = new System.Windows.Forms.Label();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.normalPsalmTitleRadioButton = new System.Windows.Forms.RadioButton();
            this.dropCapPsalmRadioButton = new System.Windows.Forms.RadioButton();
            this.goTabPage = new System.Windows.Forms.TabPage();
            this.openWordCheckBox = new System.Windows.Forms.CheckBox();
            this.exitButton = new System.Windows.Forms.Button();
            this.convertNowButton = new System.Windows.Forms.Button();
            this.statusListBox = new System.Windows.Forms.ListBox();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.saveFileDialog1 = new System.Windows.Forms.SaveFileDialog();
            this.backButton = new System.Windows.Forms.Button();
            this.nextButton = new System.Windows.Forms.Button();
            this.openFileDialog2 = new System.Windows.Forms.OpenFileDialog();
            this.helpButton = new System.Windows.Forms.Button();
            this.tabControl1.SuspendLayout();
            this.optionsFileTabPage.SuspendLayout();
            this.footnoteTab.SuspendLayout();
            this.templatePage.SuspendLayout();
            this.usfmFileTab.SuspendLayout();
            this.MergetabPage.SuspendLayout();
            this.outputTabPage.SuspendLayout();
            this.extrasTabPage.SuspendLayout();
            this.cvTab.SuspendLayout();
            this.groupBox3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numLinesNumericUpDown)).BeginInit();
            this.groupBox1.SuspendLayout();
            this.PsalmsTabPage.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.goTabPage.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControl1
            // 
            this.tabControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControl1.Controls.Add(this.optionsFileTabPage);
            this.tabControl1.Controls.Add(this.footnoteTab);
            this.tabControl1.Controls.Add(this.templatePage);
            this.tabControl1.Controls.Add(this.usfmFileTab);
            this.tabControl1.Controls.Add(this.MergetabPage);
            this.tabControl1.Controls.Add(this.outputTabPage);
            this.tabControl1.Controls.Add(this.extrasTabPage);
            this.tabControl1.Controls.Add(this.cvTab);
            this.tabControl1.Controls.Add(this.PsalmsTabPage);
            this.tabControl1.Controls.Add(this.goTabPage);
            this.tabControl1.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tabControl1.Location = new System.Drawing.Point(0, 0);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(632, 360);
            this.tabControl1.TabIndex = 1;
            this.tabControl1.SelectedIndexChanged += new System.EventHandler(this.tabControl1_SelectedIndexChanged);
            // 
            // optionsFileTabPage
            // 
            this.optionsFileTabPage.AutoScroll = true;
            this.optionsFileTabPage.Controls.Add(this.coprLabel);
            this.optionsFileTabPage.Controls.Add(this.createOptionsButton);
            this.optionsFileTabPage.Controls.Add(this.label4);
            this.optionsFileTabPage.Controls.Add(this.optionsFileLabel);
            this.optionsFileTabPage.Controls.Add(this.saveOptionsButton);
            this.optionsFileTabPage.Controls.Add(this.readOptionsButton);
            this.optionsFileTabPage.Controls.Add(this.label3);
            this.optionsFileTabPage.Location = new System.Drawing.Point(4, 23);
            this.optionsFileTabPage.Name = "optionsFileTabPage";
            this.optionsFileTabPage.Size = new System.Drawing.Size(624, 333);
            this.optionsFileTabPage.TabIndex = 6;
            this.optionsFileTabPage.Text = "Options files";
            this.optionsFileTabPage.UseVisualStyleBackColor = true;
            // 
            // coprLabel
            // 
            this.coprLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.coprLabel.Location = new System.Drawing.Point(8, 152);
            this.coprLabel.Name = "coprLabel";
            this.coprLabel.Size = new System.Drawing.Size(608, 176);
            this.coprLabel.TabIndex = 12;
            // 
            // createOptionsButton
            // 
            this.createOptionsButton.Location = new System.Drawing.Point(320, 64);
            this.createOptionsButton.Name = "createOptionsButton";
            this.createOptionsButton.Size = new System.Drawing.Size(152, 24);
            this.createOptionsButton.TabIndex = 11;
            this.createOptionsButton.Text = "&Create new options file...";
            this.createOptionsButton.Click += new System.EventHandler(this.createOptionsButton_Click);
            // 
            // label4
            // 
            this.label4.Location = new System.Drawing.Point(8, 96);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(536, 16);
            this.label4.TabIndex = 9;
            this.label4.Text = "The current job options are saved in:";
            // 
            // optionsFileLabel
            // 
            this.optionsFileLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.optionsFileLabel.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.optionsFileLabel.Location = new System.Drawing.Point(8, 120);
            this.optionsFileLabel.Name = "optionsFileLabel";
            this.optionsFileLabel.Size = new System.Drawing.Size(608, 16);
            this.optionsFileLabel.TabIndex = 8;
            this.optionsFileLabel.Text = "options.xini";
            // 
            // saveOptionsButton
            // 
            this.saveOptionsButton.Location = new System.Drawing.Point(168, 64);
            this.saveOptionsButton.Name = "saveOptionsButton";
            this.saveOptionsButton.Size = new System.Drawing.Size(136, 24);
            this.saveOptionsButton.TabIndex = 5;
            this.saveOptionsButton.Text = "&Save options as...";
            this.saveOptionsButton.Click += new System.EventHandler(this.saveOptionsButton_Click);
            // 
            // readOptionsButton
            // 
            this.readOptionsButton.Location = new System.Drawing.Point(8, 64);
            this.readOptionsButton.Name = "readOptionsButton";
            this.readOptionsButton.Size = new System.Drawing.Size(144, 24);
            this.readOptionsButton.TabIndex = 4;
            this.readOptionsButton.Text = "&Read options from...";
            this.readOptionsButton.Click += new System.EventHandler(this.readOptionsButton_Click);
            // 
            // label3
            // 
            this.label3.Location = new System.Drawing.Point(8, 24);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(544, 32);
            this.label3.TabIndex = 0;
            this.label3.Text = "The options file is where your preferences for this project are saved. The best w" +
                "ay to edit this file is to simply set up the options you like using the settings" +
                " in this program.";
            // 
            // footnoteTab
            // 
            this.footnoteTab.Controls.Add(this.label24);
            this.footnoteTab.Controls.Add(this.customXrefCallerTextBox);
            this.footnoteTab.Controls.Add(this.customXrefCallerCheckBox);
            this.footnoteTab.Controls.Add(this.insertXrefVerseCheckBox);
            this.footnoteTab.Controls.Add(this.customFootnoteCallerTextBox);
            this.footnoteTab.Controls.Add(this.customFootnoteCallerCheckBox);
            this.footnoteTab.Controls.Add(this.insertCallingVerseRefCheckBox);
            this.footnoteTab.Controls.Add(this.label14);
            this.footnoteTab.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.footnoteTab.Location = new System.Drawing.Point(4, 23);
            this.footnoteTab.Name = "footnoteTab";
            this.footnoteTab.Size = new System.Drawing.Size(624, 333);
            this.footnoteTab.TabIndex = 2;
            this.footnoteTab.Text = "Footnotes";
            this.footnoteTab.UseVisualStyleBackColor = true;
            // 
            // label24
            // 
            this.label24.Location = new System.Drawing.Point(8, 160);
            this.label24.Name = "label24";
            this.label24.Size = new System.Drawing.Size(544, 40);
            this.label24.TabIndex = 7;
            this.label24.Text = "Note: a blank calling character sequence is equivalent to no caller (i. e. \\f - o" +
                "r \\x -).";
            // 
            // customXrefCallerTextBox
            // 
            this.customXrefCallerTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.customXrefCallerTextBox.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.customXrefCallerTextBox.Location = new System.Drawing.Point(8, 128);
            this.customXrefCallerTextBox.Name = "customXrefCallerTextBox";
            this.customXrefCallerTextBox.Size = new System.Drawing.Size(608, 20);
            this.customXrefCallerTextBox.TabIndex = 6;
            this.customXrefCallerTextBox.Text = "#";
            // 
            // customXrefCallerCheckBox
            // 
            this.customXrefCallerCheckBox.Checked = true;
            this.customXrefCallerCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.customXrefCallerCheckBox.Location = new System.Drawing.Point(8, 112);
            this.customXrefCallerCheckBox.Name = "customXrefCallerCheckBox";
            this.customXrefCallerCheckBox.Size = new System.Drawing.Size(360, 16);
            this.customXrefCallerCheckBox.TabIndex = 5;
            this.customXrefCallerCheckBox.Text = "Use custom character set for autoincrement cross-reference notes:";
            this.customXrefCallerCheckBox.CheckedChanged += new System.EventHandler(this.customXrefCallerCheckBox_CheckedChanged);
            // 
            // insertXrefVerseCheckBox
            // 
            this.insertXrefVerseCheckBox.Location = new System.Drawing.Point(8, 96);
            this.insertXrefVerseCheckBox.Name = "insertXrefVerseCheckBox";
            this.insertXrefVerseCheckBox.Size = new System.Drawing.Size(408, 16);
            this.insertXrefVerseCheckBox.TabIndex = 4;
            this.insertXrefVerseCheckBox.Text = "Insert calling verse reference at the beginning of a crossreference note.";
            // 
            // customFootnoteCallerTextBox
            // 
            this.customFootnoteCallerTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.customFootnoteCallerTextBox.Location = new System.Drawing.Point(8, 64);
            this.customFootnoteCallerTextBox.Name = "customFootnoteCallerTextBox";
            this.customFootnoteCallerTextBox.Size = new System.Drawing.Size(608, 20);
            this.customFootnoteCallerTextBox.TabIndex = 3;
            this.customFootnoteCallerTextBox.Text = "* † ‡ ** †† ‡‡";
            // 
            // customFootnoteCallerCheckBox
            // 
            this.customFootnoteCallerCheckBox.Location = new System.Drawing.Point(8, 48);
            this.customFootnoteCallerCheckBox.Name = "customFootnoteCallerCheckBox";
            this.customFootnoteCallerCheckBox.Size = new System.Drawing.Size(304, 16);
            this.customFootnoteCallerCheckBox.TabIndex = 2;
            this.customFootnoteCallerCheckBox.Text = "Use custom character set for autoincrement footnotes:";
            this.customFootnoteCallerCheckBox.CheckedChanged += new System.EventHandler(this.customFootnoteCallerCheckBox_CheckedChanged);
            // 
            // insertCallingVerseRefCheckBox
            // 
            this.insertCallingVerseRefCheckBox.Location = new System.Drawing.Point(8, 32);
            this.insertCallingVerseRefCheckBox.Name = "insertCallingVerseRefCheckBox";
            this.insertCallingVerseRefCheckBox.Size = new System.Drawing.Size(296, 16);
            this.insertCallingVerseRefCheckBox.TabIndex = 1;
            this.insertCallingVerseRefCheckBox.Text = "Insert calling verse reference at beginning of footnote.";
            // 
            // label14
            // 
            this.label14.Location = new System.Drawing.Point(8, 8);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(424, 16);
            this.label14.TabIndex = 0;
            this.label14.Text = "How would you like footnotes, crossreferences, and study notes to be handled?";
            // 
            // templatePage
            // 
            this.templatePage.Controls.Add(this.enableSubstitutionsCheckBox);
            this.templatePage.Controls.Add(this.label26);
            this.templatePage.Controls.Add(this.substitutionFileLabel);
            this.templatePage.Controls.Add(this.label25);
            this.templatePage.Controls.Add(this.changeListButton);
            this.templatePage.Controls.Add(this.label17);
            this.templatePage.Controls.Add(this.templateFileLabel);
            this.templatePage.Controls.Add(this.label5);
            this.templatePage.Controls.Add(this.templateBrowseButton);
            this.templatePage.Location = new System.Drawing.Point(4, 23);
            this.templatePage.Name = "templatePage";
            this.templatePage.Size = new System.Drawing.Size(624, 333);
            this.templatePage.TabIndex = 8;
            this.templatePage.Text = "Seed file";
            this.templatePage.UseVisualStyleBackColor = true;
            // 
            // enableSubstitutionsCheckBox
            // 
            this.enableSubstitutionsCheckBox.Location = new System.Drawing.Point(192, 176);
            this.enableSubstitutionsCheckBox.Name = "enableSubstitutionsCheckBox";
            this.enableSubstitutionsCheckBox.Size = new System.Drawing.Size(216, 24);
            this.enableSubstitutionsCheckBox.TabIndex = 18;
            this.enableSubstitutionsCheckBox.Text = "Enable substitutions";
            this.enableSubstitutionsCheckBox.CheckedChanged += new System.EventHandler(this.enableSubstitutionsCheckBox_CheckedChanged);
            // 
            // label26
            // 
            this.label26.Location = new System.Drawing.Point(8, 248);
            this.label26.Name = "label26";
            this.label26.Size = new System.Drawing.Size(544, 64);
            this.label26.TabIndex = 17;
            this.label26.Text = resources.GetString("label26.Text");
            // 
            // substitutionFileLabel
            // 
            this.substitutionFileLabel.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.substitutionFileLabel.Location = new System.Drawing.Point(8, 224);
            this.substitutionFileLabel.Name = "substitutionFileLabel";
            this.substitutionFileLabel.Size = new System.Drawing.Size(544, 16);
            this.substitutionFileLabel.TabIndex = 16;
            this.substitutionFileLabel.Text = "fixquotemarks.xml";
            // 
            // label25
            // 
            this.label25.Location = new System.Drawing.Point(8, 208);
            this.label25.Name = "label25";
            this.label25.Size = new System.Drawing.Size(200, 16);
            this.label25.TabIndex = 15;
            this.label25.Text = "The current substitution file is:";
            // 
            // changeListButton
            // 
            this.changeListButton.Location = new System.Drawing.Point(8, 176);
            this.changeListButton.Name = "changeListButton";
            this.changeListButton.Size = new System.Drawing.Size(144, 24);
            this.changeListButton.TabIndex = 14;
            this.changeListButton.Text = "&Select substitution file";
            this.changeListButton.Click += new System.EventHandler(this.changeListButton_Click);
            // 
            // label17
            // 
            this.label17.Location = new System.Drawing.Point(8, 104);
            this.label17.Name = "label17";
            this.label17.Size = new System.Drawing.Size(544, 64);
            this.label17.TabIndex = 13;
            this.label17.Text = resources.GetString("label17.Text");
            // 
            // templateFileLabel
            // 
            this.templateFileLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.templateFileLabel.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.templateFileLabel.Location = new System.Drawing.Point(8, 72);
            this.templateFileLabel.Name = "templateFileLabel";
            this.templateFileLabel.Size = new System.Drawing.Size(608, 16);
            this.templateFileLabel.TabIndex = 12;
            this.templateFileLabel.Text = "Scripture.xml";
            // 
            // label5
            // 
            this.label5.Location = new System.Drawing.Point(8, 48);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(544, 16);
            this.label5.TabIndex = 11;
            this.label5.Text = "The current seed WordML file is:";
            // 
            // templateBrowseButton
            // 
            this.templateBrowseButton.Location = new System.Drawing.Point(8, 16);
            this.templateBrowseButton.Name = "templateBrowseButton";
            this.templateBrowseButton.Size = new System.Drawing.Size(144, 24);
            this.templateBrowseButton.TabIndex = 3;
            this.templateBrowseButton.Text = "&Browse for seed file";
            this.templateBrowseButton.Click += new System.EventHandler(this.templateBrowseButton_Click);
            // 
            // usfmFileTab
            // 
            this.usfmFileTab.Controls.Add(this.browseUSFMButton);
            this.usfmFileTab.Controls.Add(this.deleteButton);
            this.usfmFileTab.Controls.Add(this.usfmFileListBox);
            this.usfmFileTab.Controls.Add(this.label1);
            this.usfmFileTab.Location = new System.Drawing.Point(4, 23);
            this.usfmFileTab.Name = "usfmFileTab";
            this.usfmFileTab.Size = new System.Drawing.Size(624, 333);
            this.usfmFileTab.TabIndex = 0;
            this.usfmFileTab.Text = "USFM files";
            this.usfmFileTab.UseVisualStyleBackColor = true;
            // 
            // browseUSFMButton
            // 
            this.browseUSFMButton.Location = new System.Drawing.Point(8, 40);
            this.browseUSFMButton.Name = "browseUSFMButton";
            this.browseUSFMButton.Size = new System.Drawing.Size(136, 24);
            this.browseUSFMButton.TabIndex = 5;
            this.browseUSFMButton.Text = "&Add files to list...";
            this.browseUSFMButton.Click += new System.EventHandler(this.browseUSFMButton_Click);
            // 
            // deleteButton
            // 
            this.deleteButton.Location = new System.Drawing.Point(160, 40);
            this.deleteButton.Name = "deleteButton";
            this.deleteButton.Size = new System.Drawing.Size(128, 24);
            this.deleteButton.TabIndex = 4;
            this.deleteButton.Text = "&Delete from list";
            this.deleteButton.Click += new System.EventHandler(this.deleteButton_Click);
            // 
            // usfmFileListBox
            // 
            this.usfmFileListBox.AllowDrop = true;
            this.usfmFileListBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.usfmFileListBox.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(192)))));
            this.usfmFileListBox.HorizontalScrollbar = true;
            this.usfmFileListBox.ItemHeight = 14;
            this.usfmFileListBox.Location = new System.Drawing.Point(8, 72);
            this.usfmFileListBox.Name = "usfmFileListBox";
            this.usfmFileListBox.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
            this.usfmFileListBox.Size = new System.Drawing.Size(608, 228);
            this.usfmFileListBox.Sorted = true;
            this.usfmFileListBox.TabIndex = 1;
            this.usfmFileListBox.DragDrop += new System.Windows.Forms.DragEventHandler(this.usfmFileListBox_DragDrop);
            this.usfmFileListBox.DragEnter += new System.Windows.Forms.DragEventHandler(this.usfmFileListBox_DragEnter);
            this.usfmFileListBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.usfmFileListBox_KeyDown);
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(8, 8);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(600, 24);
            this.label1.TabIndex = 0;
            this.label1.Text = "Please specify the input Unicode USFM file(s). You may browse to the files or dra" +
                "g and drop them into the list box.";
            // 
            // MergetabPage
            // 
            this.MergetabPage.Controls.Add(this.xrefLabel);
            this.MergetabPage.Controls.Add(this.browseXrefButton);
            this.MergetabPage.Controls.Add(this.xrefCheckBox);
            this.MergetabPage.Controls.Add(this.label23);
            this.MergetabPage.Location = new System.Drawing.Point(4, 23);
            this.MergetabPage.Name = "MergetabPage";
            this.MergetabPage.Size = new System.Drawing.Size(624, 333);
            this.MergetabPage.TabIndex = 10;
            this.MergetabPage.Text = "Merge";
            this.MergetabPage.UseVisualStyleBackColor = true;
            // 
            // xrefLabel
            // 
            this.xrefLabel.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.xrefLabel.Location = new System.Drawing.Point(8, 88);
            this.xrefLabel.Name = "xrefLabel";
            this.xrefLabel.Size = new System.Drawing.Size(544, 24);
            this.xrefLabel.TabIndex = 4;
            this.xrefLabel.Text = "crossreference.xml";
            // 
            // browseXrefButton
            // 
            this.browseXrefButton.Location = new System.Drawing.Point(280, 56);
            this.browseXrefButton.Name = "browseXrefButton";
            this.browseXrefButton.Size = new System.Drawing.Size(112, 24);
            this.browseXrefButton.TabIndex = 3;
            this.browseXrefButton.Text = "Browse";
            this.browseXrefButton.Click += new System.EventHandler(this.browseXrefButton_Click);
            // 
            // xrefCheckBox
            // 
            this.xrefCheckBox.Location = new System.Drawing.Point(8, 56);
            this.xrefCheckBox.Name = "xrefCheckBox";
            this.xrefCheckBox.Size = new System.Drawing.Size(240, 24);
            this.xrefCheckBox.TabIndex = 1;
            this.xrefCheckBox.Text = "Include crossreferences from external file:";
            // 
            // label23
            // 
            this.label23.Location = new System.Drawing.Point(8, 8);
            this.label23.Name = "label23";
            this.label23.Size = new System.Drawing.Size(544, 40);
            this.label23.TabIndex = 0;
            this.label23.Text = "You may optionally specify a crossreference file to merge with the text. Edit the" +
                " \"target\" element contents in the crossreference.xml file to fit your language.";
            // 
            // outputTabPage
            // 
            this.outputTabPage.Controls.Add(this.embedUsfxCheckBox);
            this.outputTabPage.Controls.Add(this.label15);
            this.outputTabPage.Controls.Add(this.browseOutputButton);
            this.outputTabPage.Controls.Add(this.outputTextBox);
            this.outputTabPage.Controls.Add(this.label2);
            this.outputTabPage.Location = new System.Drawing.Point(4, 23);
            this.outputTabPage.Name = "outputTabPage";
            this.outputTabPage.Size = new System.Drawing.Size(624, 333);
            this.outputTabPage.TabIndex = 5;
            this.outputTabPage.Text = "Output file";
            this.outputTabPage.UseVisualStyleBackColor = true;
            // 
            // embedUsfxCheckBox
            // 
            this.embedUsfxCheckBox.Location = new System.Drawing.Point(288, 56);
            this.embedUsfxCheckBox.Name = "embedUsfxCheckBox";
            this.embedUsfxCheckBox.Size = new System.Drawing.Size(256, 24);
            this.embedUsfxCheckBox.TabIndex = 4;
            this.embedUsfxCheckBox.Text = "Embed USFX (an XML version of USFM)";
            // 
            // label15
            // 
            this.label15.Location = new System.Drawing.Point(8, 128);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(544, 112);
            this.label15.TabIndex = 3;
            this.label15.Text = resources.GetString("label15.Text");
            // 
            // browseOutputButton
            // 
            this.browseOutputButton.Location = new System.Drawing.Point(8, 56);
            this.browseOutputButton.Name = "browseOutputButton";
            this.browseOutputButton.Size = new System.Drawing.Size(104, 24);
            this.browseOutputButton.TabIndex = 2;
            this.browseOutputButton.Text = "&Browse";
            this.browseOutputButton.Click += new System.EventHandler(this.browseOutputButton_Click);
            // 
            // outputTextBox
            // 
            this.outputTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.outputTextBox.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(192)))));
            this.outputTextBox.Location = new System.Drawing.Point(8, 88);
            this.outputTextBox.Name = "outputTextBox";
            this.outputTextBox.Size = new System.Drawing.Size(608, 20);
            this.outputTextBox.TabIndex = 1;
            this.outputTextBox.Text = "Output.xml";
            // 
            // label2
            // 
            this.label2.Location = new System.Drawing.Point(8, 24);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(544, 24);
            this.label2.TabIndex = 0;
            this.label2.Text = "Please specify the name of the WordML output file.";
            // 
            // extrasTabPage
            // 
            this.extrasTabPage.Controls.Add(this.pageLengthTextBox);
            this.extrasTabPage.Controls.Add(this.label31);
            this.extrasTabPage.Controls.Add(this.pageWidthTextBox);
            this.extrasTabPage.Controls.Add(this.label30);
            this.extrasTabPage.Controls.Add(this.cropCheckBox);
            this.extrasTabPage.Controls.Add(this.label16);
            this.extrasTabPage.Controls.Add(this.label27);
            this.extrasTabPage.Controls.Add(this.nameTextBox);
            this.extrasTabPage.Controls.Add(this.usfxButton);
            this.extrasTabPage.Location = new System.Drawing.Point(4, 23);
            this.extrasTabPage.Name = "extrasTabPage";
            this.extrasTabPage.Size = new System.Drawing.Size(624, 333);
            this.extrasTabPage.TabIndex = 11;
            this.extrasTabPage.Text = "Extras";
            this.extrasTabPage.UseVisualStyleBackColor = true;
            // 
            // pageLengthTextBox
            // 
            this.pageLengthTextBox.Location = new System.Drawing.Point(88, 160);
            this.pageLengthTextBox.Name = "pageLengthTextBox";
            this.pageLengthTextBox.Size = new System.Drawing.Size(56, 20);
            this.pageLengthTextBox.TabIndex = 16;
            this.pageLengthTextBox.Text = "216 mm";
            // 
            // label31
            // 
            this.label31.Location = new System.Drawing.Point(8, 160);
            this.label31.Name = "label31";
            this.label31.Size = new System.Drawing.Size(80, 16);
            this.label31.TabIndex = 15;
            this.label31.Text = "page length:";
            this.label31.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // pageWidthTextBox
            // 
            this.pageWidthTextBox.Location = new System.Drawing.Point(88, 136);
            this.pageWidthTextBox.Name = "pageWidthTextBox";
            this.pageWidthTextBox.Size = new System.Drawing.Size(56, 20);
            this.pageWidthTextBox.TabIndex = 14;
            this.pageWidthTextBox.Text = "150 mm";
            // 
            // label30
            // 
            this.label30.Location = new System.Drawing.Point(8, 136);
            this.label30.Name = "label30";
            this.label30.Size = new System.Drawing.Size(80, 16);
            this.label30.TabIndex = 13;
            this.label30.Text = "page width:";
            this.label30.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // cropCheckBox
            // 
            this.cropCheckBox.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.cropCheckBox.Location = new System.Drawing.Point(8, 112);
            this.cropCheckBox.Name = "cropCheckBox";
            this.cropCheckBox.Size = new System.Drawing.Size(136, 24);
            this.cropCheckBox.TabIndex = 12;
            this.cropCheckBox.Text = "include &crop marks";
            this.cropCheckBox.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label16
            // 
            this.label16.Location = new System.Drawing.Point(8, 8);
            this.label16.Name = "label16";
            this.label16.Size = new System.Drawing.Size(536, 48);
            this.label16.TabIndex = 11;
            this.label16.Text = "The \"Export USFX only\" function is  here for experimental reasons, and is not nec" +
                "essary to do the typesetting. Press \"Next\" to go to the page where you can gener" +
                "ate a Microsoft Word XML document.";
            // 
            // label27
            // 
            this.label27.Location = new System.Drawing.Point(160, 72);
            this.label27.Name = "label27";
            this.label27.Size = new System.Drawing.Size(136, 16);
            this.label27.TabIndex = 10;
            this.label27.Text = "Name space designation:";
            // 
            // nameTextBox
            // 
            this.nameTextBox.Location = new System.Drawing.Point(304, 72);
            this.nameTextBox.Name = "nameTextBox";
            this.nameTextBox.Size = new System.Drawing.Size(48, 20);
            this.nameTextBox.TabIndex = 9;
            // 
            // usfxButton
            // 
            this.usfxButton.Location = new System.Drawing.Point(8, 72);
            this.usfxButton.Name = "usfxButton";
            this.usfxButton.Size = new System.Drawing.Size(144, 24);
            this.usfxButton.TabIndex = 8;
            this.usfxButton.Text = "Export USFX only";
            this.usfxButton.Click += new System.EventHandler(this.usfxButton_Click_1);
            // 
            // cvTab
            // 
            this.cvTab.Controls.Add(this.groupBox3);
            this.cvTab.Controls.Add(this.verseSuffixTextBox);
            this.cvTab.Controls.Add(this.label10);
            this.cvTab.Controls.Add(this.versePrefixTextBox);
            this.cvTab.Controls.Add(this.label9);
            this.cvTab.Controls.Add(this.chapterSuffixTextBox);
            this.cvTab.Controls.Add(this.label8);
            this.cvTab.Controls.Add(this.chapterNameTextBox);
            this.cvTab.Controls.Add(this.label7);
            this.cvTab.Controls.Add(this.verse1CheckBox);
            this.cvTab.Controls.Add(this.chapter1CheckBox);
            this.cvTab.Controls.Add(this.groupBox1);
            this.cvTab.Controls.Add(this.label6);
            this.cvTab.Location = new System.Drawing.Point(4, 23);
            this.cvTab.Name = "cvTab";
            this.cvTab.Size = new System.Drawing.Size(624, 333);
            this.cvTab.TabIndex = 1;
            this.cvTab.Text = "Chapter & verse";
            this.cvTab.UseVisualStyleBackColor = true;
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.label28);
            this.groupBox3.Controls.Add(this.dropCapBeforeTextBox);
            this.groupBox3.Controls.Add(this.label29);
            this.groupBox3.Controls.Add(this.autoCalcDropCapCheckBox);
            this.groupBox3.Controls.Add(this.label22);
            this.groupBox3.Controls.Add(this.label21);
            this.groupBox3.Controls.Add(this.label20);
            this.groupBox3.Controls.Add(this.label19);
            this.groupBox3.Controls.Add(this.positionTextBox);
            this.groupBox3.Controls.Add(this.sizeTextBox);
            this.groupBox3.Controls.Add(this.spacingTextBox);
            this.groupBox3.Controls.Add(this.horizTextBox);
            this.groupBox3.Controls.Add(this.numLinesNumericUpDown);
            this.groupBox3.Controls.Add(this.label18);
            this.groupBox3.Controls.Add(this.suppressIndentCheckBox);
            this.groupBox3.Location = new System.Drawing.Point(8, 200);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(608, 120);
            this.groupBox3.TabIndex = 12;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = " Advanced drop cap options ";
            // 
            // label28
            // 
            this.label28.Location = new System.Drawing.Point(16, 88);
            this.label28.Name = "label28";
            this.label28.Size = new System.Drawing.Size(136, 16);
            this.label28.TabIndex = 17;
            this.label28.Text = "Space above drop cap:";
            this.label28.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // dropCapBeforeTextBox
            // 
            this.dropCapBeforeTextBox.Location = new System.Drawing.Point(152, 88);
            this.dropCapBeforeTextBox.Name = "dropCapBeforeTextBox";
            this.dropCapBeforeTextBox.Size = new System.Drawing.Size(72, 20);
            this.dropCapBeforeTextBox.TabIndex = 3;
            this.dropCapBeforeTextBox.Text = "0 pt";
            this.dropCapBeforeTextBox.TextChanged += new System.EventHandler(this.dropCapBeforeTextBox_TextChanged);
            // 
            // label29
            // 
            this.label29.Location = new System.Drawing.Point(464, 16);
            this.label29.Name = "label29";
            this.label29.Size = new System.Drawing.Size(136, 96);
            this.label29.TabIndex = 15;
            this.label29.Text = "You may specify these sizes in inches, millimeters, points, or twips.";
            // 
            // autoCalcDropCapCheckBox
            // 
            this.autoCalcDropCapCheckBox.Location = new System.Drawing.Point(32, 40);
            this.autoCalcDropCapCheckBox.Name = "autoCalcDropCapCheckBox";
            this.autoCalcDropCapCheckBox.Size = new System.Drawing.Size(168, 16);
            this.autoCalcDropCapCheckBox.TabIndex = 1;
            this.autoCalcDropCapCheckBox.Text = "Automatic drop cap size";
            this.autoCalcDropCapCheckBox.CheckedChanged += new System.EventHandler(this.autoCalcDropCapCheckBox_CheckedChanged);
            // 
            // label22
            // 
            this.label22.Location = new System.Drawing.Point(272, 88);
            this.label22.Name = "label22";
            this.label22.Size = new System.Drawing.Size(104, 24);
            this.label22.TabIndex = 11;
            this.label22.Text = "Drop cap position:";
            this.label22.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label21
            // 
            this.label21.Location = new System.Drawing.Point(272, 64);
            this.label21.Name = "label21";
            this.label21.Size = new System.Drawing.Size(104, 24);
            this.label21.TabIndex = 10;
            this.label21.Text = "Drop cap size:";
            this.label21.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label20
            // 
            this.label20.Location = new System.Drawing.Point(272, 40);
            this.label20.Name = "label20";
            this.label20.Size = new System.Drawing.Size(104, 24);
            this.label20.TabIndex = 9;
            this.label20.Text = "Drop cap spacing:";
            this.label20.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label19
            // 
            this.label19.Location = new System.Drawing.Point(224, 16);
            this.label19.Name = "label19";
            this.label19.Size = new System.Drawing.Size(152, 24);
            this.label19.TabIndex = 8;
            this.label19.Text = "Horizontal distance from text:";
            this.label19.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // positionTextBox
            // 
            this.positionTextBox.Location = new System.Drawing.Point(384, 88);
            this.positionTextBox.Name = "positionTextBox";
            this.positionTextBox.Size = new System.Drawing.Size(72, 20);
            this.positionTextBox.TabIndex = 7;
            this.positionTextBox.Text = "-4";
            this.positionTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.positionTextBox.TextChanged += new System.EventHandler(this.positionTextBox_TextChanged);
            // 
            // sizeTextBox
            // 
            this.sizeTextBox.Location = new System.Drawing.Point(384, 64);
            this.sizeTextBox.Name = "sizeTextBox";
            this.sizeTextBox.Size = new System.Drawing.Size(72, 20);
            this.sizeTextBox.TabIndex = 6;
            this.sizeTextBox.Text = "53";
            this.sizeTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.sizeTextBox.TextChanged += new System.EventHandler(this.sizeTextBox_TextChanged);
            // 
            // spacingTextBox
            // 
            this.spacingTextBox.Location = new System.Drawing.Point(384, 40);
            this.spacingTextBox.Name = "spacingTextBox";
            this.spacingTextBox.Size = new System.Drawing.Size(72, 20);
            this.spacingTextBox.TabIndex = 5;
            this.spacingTextBox.Text = "459";
            this.spacingTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.spacingTextBox.TextChanged += new System.EventHandler(this.spacingTextBox_TextChanged);
            // 
            // horizTextBox
            // 
            this.horizTextBox.Location = new System.Drawing.Point(384, 16);
            this.horizTextBox.Name = "horizTextBox";
            this.horizTextBox.Size = new System.Drawing.Size(72, 20);
            this.horizTextBox.TabIndex = 4;
            this.horizTextBox.Text = "72";
            this.horizTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.horizTextBox.TextChanged += new System.EventHandler(this.horizTextBox_TextChanged);
            // 
            // numLinesNumericUpDown
            // 
            this.numLinesNumericUpDown.Location = new System.Drawing.Point(152, 64);
            this.numLinesNumericUpDown.Maximum = new decimal(new int[] {
            4,
            0,
            0,
            0});
            this.numLinesNumericUpDown.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numLinesNumericUpDown.Name = "numLinesNumericUpDown";
            this.numLinesNumericUpDown.Size = new System.Drawing.Size(40, 20);
            this.numLinesNumericUpDown.TabIndex = 2;
            this.numLinesNumericUpDown.Value = new decimal(new int[] {
            2,
            0,
            0,
            0});
            // 
            // label18
            // 
            this.label18.Location = new System.Drawing.Point(16, 64);
            this.label18.Name = "label18";
            this.label18.Size = new System.Drawing.Size(136, 16);
            this.label18.TabIndex = 1;
            this.label18.Text = "Number of lines to drop:";
            this.label18.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // suppressIndentCheckBox
            // 
            this.suppressIndentCheckBox.Location = new System.Drawing.Point(32, 16);
            this.suppressIndentCheckBox.Name = "suppressIndentCheckBox";
            this.suppressIndentCheckBox.Size = new System.Drawing.Size(184, 24);
            this.suppressIndentCheckBox.TabIndex = 0;
            this.suppressIndentCheckBox.Text = "Suppress indent after drop cap";
            // 
            // verseSuffixTextBox
            // 
            this.verseSuffixTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.verseSuffixTextBox.Location = new System.Drawing.Point(472, 144);
            this.verseSuffixTextBox.Name = "verseSuffixTextBox";
            this.verseSuffixTextBox.Size = new System.Drawing.Size(144, 20);
            this.verseSuffixTextBox.TabIndex = 6;
            this.verseSuffixTextBox.Text = " ";
            // 
            // label10
            // 
            this.label10.Location = new System.Drawing.Point(144, 152);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(320, 48);
            this.label10.TabIndex = 5;
            this.label10.Text = "Follow verse number with (normally non-breaking half-space): Use \"nbsp\" for non-b" +
                "reaking space and \"nbhs\" for non-breaking half-space; or insert any Unicode stri" +
                "ng.";
            this.label10.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // versePrefixTextBox
            // 
            this.versePrefixTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.versePrefixTextBox.Location = new System.Drawing.Point(472, 120);
            this.versePrefixTextBox.Name = "versePrefixTextBox";
            this.versePrefixTextBox.Size = new System.Drawing.Size(144, 20);
            this.versePrefixTextBox.TabIndex = 5;
            // 
            // label9
            // 
            this.label9.Location = new System.Drawing.Point(224, 128);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(240, 24);
            this.label9.TabIndex = 8;
            this.label9.Text = "Precede verse number with (normally nothing):";
            this.label9.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // chapterSuffixTextBox
            // 
            this.chapterSuffixTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.chapterSuffixTextBox.Location = new System.Drawing.Point(216, 96);
            this.chapterSuffixTextBox.Name = "chapterSuffixTextBox";
            this.chapterSuffixTextBox.Size = new System.Drawing.Size(400, 20);
            this.chapterSuffixTextBox.TabIndex = 2;
            // 
            // label8
            // 
            this.label8.Location = new System.Drawing.Point(216, 80);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(328, 16);
            this.label8.TabIndex = 6;
            this.label8.Text = "Follow chapter number with (may be blank):";
            // 
            // chapterNameTextBox
            // 
            this.chapterNameTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.chapterNameTextBox.Location = new System.Drawing.Point(216, 48);
            this.chapterNameTextBox.Name = "chapterNameTextBox";
            this.chapterNameTextBox.Size = new System.Drawing.Size(400, 20);
            this.chapterNameTextBox.TabIndex = 1;
            this.chapterNameTextBox.Text = "Chapter";
            // 
            // label7
            // 
            this.label7.Location = new System.Drawing.Point(216, 32);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(288, 16);
            this.label7.TabIndex = 4;
            this.label7.Text = "Precede chapter number with (\"Chapter\" in vernacular):";
            // 
            // verse1CheckBox
            // 
            this.verse1CheckBox.Location = new System.Drawing.Point(16, 128);
            this.verse1CheckBox.Name = "verse1CheckBox";
            this.verse1CheckBox.Size = new System.Drawing.Size(160, 24);
            this.verse1CheckBox.TabIndex = 4;
            this.verse1CheckBox.Text = "Include markers for verse 1";
            // 
            // chapter1CheckBox
            // 
            this.chapter1CheckBox.Location = new System.Drawing.Point(16, 104);
            this.chapter1CheckBox.Name = "chapter1CheckBox";
            this.chapter1CheckBox.Size = new System.Drawing.Size(176, 24);
            this.chapter1CheckBox.TabIndex = 3;
            this.chapter1CheckBox.Text = "Include markers for chapter 1";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.chapterHeadingRadioButton);
            this.groupBox1.Controls.Add(this.dropCapRadioButton);
            this.groupBox1.Location = new System.Drawing.Point(8, 32);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(200, 56);
            this.groupBox1.TabIndex = 1;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = " Chapter label style ";
            // 
            // chapterHeadingRadioButton
            // 
            this.chapterHeadingRadioButton.Location = new System.Drawing.Point(8, 32);
            this.chapterHeadingRadioButton.Name = "chapterHeadingRadioButton";
            this.chapterHeadingRadioButton.Size = new System.Drawing.Size(168, 16);
            this.chapterHeadingRadioButton.TabIndex = 1;
            this.chapterHeadingRadioButton.Text = "Use chapter headings";
            // 
            // dropCapRadioButton
            // 
            this.dropCapRadioButton.Checked = true;
            this.dropCapRadioButton.Location = new System.Drawing.Point(8, 16);
            this.dropCapRadioButton.Name = "dropCapRadioButton";
            this.dropCapRadioButton.Size = new System.Drawing.Size(184, 16);
            this.dropCapRadioButton.TabIndex = 0;
            this.dropCapRadioButton.TabStop = true;
            this.dropCapRadioButton.Text = "Use drop cap chapter numbers.";
            this.dropCapRadioButton.CheckedChanged += new System.EventHandler(this.dropCapRadioButton_CheckedChanged);
            // 
            // label6
            // 
            this.label6.Location = new System.Drawing.Point(8, 8);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(544, 16);
            this.label6.TabIndex = 0;
            this.label6.Text = "How would you like to mark chapters and verses? (Some of these settings are overr" +
                "idden for Psalms.)";
            // 
            // PsalmsTabPage
            // 
            this.PsalmsTabPage.Controls.Add(this.labelPsalmV1CheckBox);
            this.PsalmsTabPage.Controls.Add(this.labelPsalm1CheckBox);
            this.PsalmsTabPage.Controls.Add(this.vernacularPsalmSuffixTextBox);
            this.PsalmsTabPage.Controls.Add(this.label13);
            this.PsalmsTabPage.Controls.Add(this.vernacularPsalmTextBox);
            this.PsalmsTabPage.Controls.Add(this.label12);
            this.PsalmsTabPage.Controls.Add(this.label11);
            this.PsalmsTabPage.Controls.Add(this.groupBox2);
            this.PsalmsTabPage.Location = new System.Drawing.Point(4, 23);
            this.PsalmsTabPage.Name = "PsalmsTabPage";
            this.PsalmsTabPage.Size = new System.Drawing.Size(624, 333);
            this.PsalmsTabPage.TabIndex = 4;
            this.PsalmsTabPage.Text = "Psalms";
            this.PsalmsTabPage.UseVisualStyleBackColor = true;
            // 
            // labelPsalmV1CheckBox
            // 
            this.labelPsalmV1CheckBox.Location = new System.Drawing.Point(16, 160);
            this.labelPsalmV1CheckBox.Name = "labelPsalmV1CheckBox";
            this.labelPsalmV1CheckBox.Size = new System.Drawing.Size(264, 16);
            this.labelPsalmV1CheckBox.TabIndex = 7;
            this.labelPsalmV1CheckBox.Text = "Include marker for verse 1 of each Psalm";
            // 
            // labelPsalm1CheckBox
            // 
            this.labelPsalm1CheckBox.Location = new System.Drawing.Point(16, 136);
            this.labelPsalm1CheckBox.Name = "labelPsalm1CheckBox";
            this.labelPsalm1CheckBox.Size = new System.Drawing.Size(160, 16);
            this.labelPsalm1CheckBox.TabIndex = 6;
            this.labelPsalm1CheckBox.Text = "Include marker for Psalm 1";
            // 
            // vernacularPsalmSuffixTextBox
            // 
            this.vernacularPsalmSuffixTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.vernacularPsalmSuffixTextBox.Location = new System.Drawing.Point(208, 128);
            this.vernacularPsalmSuffixTextBox.Name = "vernacularPsalmSuffixTextBox";
            this.vernacularPsalmSuffixTextBox.Size = new System.Drawing.Size(400, 20);
            this.vernacularPsalmSuffixTextBox.TabIndex = 5;
            // 
            // label13
            // 
            this.label13.Location = new System.Drawing.Point(208, 112);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(224, 16);
            this.label13.TabIndex = 4;
            this.label13.Text = "Follow each Psalm number with:";
            // 
            // vernacularPsalmTextBox
            // 
            this.vernacularPsalmTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.vernacularPsalmTextBox.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(192)))));
            this.vernacularPsalmTextBox.Location = new System.Drawing.Point(208, 80);
            this.vernacularPsalmTextBox.Name = "vernacularPsalmTextBox";
            this.vernacularPsalmTextBox.Size = new System.Drawing.Size(400, 20);
            this.vernacularPsalmTextBox.TabIndex = 3;
            this.vernacularPsalmTextBox.Text = "Psalm";
            // 
            // label12
            // 
            this.label12.Location = new System.Drawing.Point(208, 64);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(328, 16);
            this.label12.TabIndex = 2;
            this.label12.Text = "Precede each Psalm number with (\"Psalm\" in vernacular):";
            // 
            // label11
            // 
            this.label11.Location = new System.Drawing.Point(8, 8);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(544, 32);
            this.label11.TabIndex = 1;
            this.label11.Text = "How would you like each chapter of Psalms to be marked? (These settings override " +
                "\"Chapter and verse\" page settings in the Psalms only.)";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.normalPsalmTitleRadioButton);
            this.groupBox2.Controls.Add(this.dropCapPsalmRadioButton);
            this.groupBox2.Location = new System.Drawing.Point(8, 64);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(184, 56);
            this.groupBox2.TabIndex = 0;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = " Chapter label style ";
            // 
            // normalPsalmTitleRadioButton
            // 
            this.normalPsalmTitleRadioButton.Checked = true;
            this.normalPsalmTitleRadioButton.Location = new System.Drawing.Point(8, 32);
            this.normalPsalmTitleRadioButton.Name = "normalPsalmTitleRadioButton";
            this.normalPsalmTitleRadioButton.Size = new System.Drawing.Size(168, 16);
            this.normalPsalmTitleRadioButton.TabIndex = 1;
            this.normalPsalmTitleRadioButton.TabStop = true;
            this.normalPsalmTitleRadioButton.Text = "Psalm titles (recommended)";
            // 
            // dropCapPsalmRadioButton
            // 
            this.dropCapPsalmRadioButton.Location = new System.Drawing.Point(8, 16);
            this.dropCapPsalmRadioButton.Name = "dropCapPsalmRadioButton";
            this.dropCapPsalmRadioButton.Size = new System.Drawing.Size(152, 16);
            this.dropCapPsalmRadioButton.TabIndex = 0;
            this.dropCapPsalmRadioButton.Text = "Drop cap number";
            // 
            // goTabPage
            // 
            this.goTabPage.Controls.Add(this.openWordCheckBox);
            this.goTabPage.Controls.Add(this.exitButton);
            this.goTabPage.Controls.Add(this.convertNowButton);
            this.goTabPage.Controls.Add(this.statusListBox);
            this.goTabPage.Location = new System.Drawing.Point(4, 23);
            this.goTabPage.Name = "goTabPage";
            this.goTabPage.Size = new System.Drawing.Size(624, 333);
            this.goTabPage.TabIndex = 7;
            this.goTabPage.Text = "Go!";
            this.goTabPage.UseVisualStyleBackColor = true;
            // 
            // openWordCheckBox
            // 
            this.openWordCheckBox.Checked = true;
            this.openWordCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.openWordCheckBox.Location = new System.Drawing.Point(160, 8);
            this.openWordCheckBox.Name = "openWordCheckBox";
            this.openWordCheckBox.Size = new System.Drawing.Size(232, 24);
            this.openWordCheckBox.TabIndex = 6;
            this.openWordCheckBox.Text = "Open in Microsoft Word when done.";
            // 
            // exitButton
            // 
            this.exitButton.Location = new System.Drawing.Point(416, 8);
            this.exitButton.Name = "exitButton";
            this.exitButton.Size = new System.Drawing.Size(136, 24);
            this.exitButton.TabIndex = 5;
            this.exitButton.Text = "E&xit";
            this.exitButton.Click += new System.EventHandler(this.exitButton_Click);
            // 
            // convertNowButton
            // 
            this.convertNowButton.Location = new System.Drawing.Point(8, 8);
            this.convertNowButton.Name = "convertNowButton";
            this.convertNowButton.Size = new System.Drawing.Size(144, 24);
            this.convertNowButton.TabIndex = 4;
            this.convertNowButton.Text = "&Convert to WordML";
            this.convertNowButton.Click += new System.EventHandler(this.convertNowButton_Click);
            // 
            // statusListBox
            // 
            this.statusListBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.statusListBox.ItemHeight = 14;
            this.statusListBox.Items.AddRange(new object[] {
            "Microsoft Word should not be open before you select \"Convert to WordM,\" at least " +
                "not with the target document open.",
            " Please ensure that the options are set the way you want them for each tab above," +
                " then select \"Convert to WordML.\""});
            this.statusListBox.Location = new System.Drawing.Point(8, 40);
            this.statusListBox.Name = "statusListBox";
            this.statusListBox.Size = new System.Drawing.Size(608, 256);
            this.statusListBox.TabIndex = 3;
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.AddExtension = false;
            this.openFileDialog1.Multiselect = true;
            this.openFileDialog1.RestoreDirectory = true;
            // 
            // saveFileDialog1
            // 
            this.saveFileDialog1.AddExtension = false;
            this.saveFileDialog1.DefaultExt = "xml";
            this.saveFileDialog1.RestoreDirectory = true;
            // 
            // backButton
            // 
            this.backButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.backButton.Location = new System.Drawing.Point(8, 368);
            this.backButton.Name = "backButton";
            this.backButton.Size = new System.Drawing.Size(104, 24);
            this.backButton.TabIndex = 3;
            this.backButton.Text = "&Back";
            this.backButton.Click += new System.EventHandler(this.backButton_Click);
            // 
            // nextButton
            // 
            this.nextButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.nextButton.Location = new System.Drawing.Point(512, 368);
            this.nextButton.Name = "nextButton";
            this.nextButton.Size = new System.Drawing.Size(112, 24);
            this.nextButton.TabIndex = 1;
            this.nextButton.Text = "&Next";
            this.nextButton.Click += new System.EventHandler(this.nextButton_Click);
            // 
            // openFileDialog2
            // 
            this.openFileDialog2.AddExtension = false;
            this.openFileDialog2.RestoreDirectory = true;
            // 
            // helpButton
            // 
            this.helpButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.helpButton.Location = new System.Drawing.Point(264, 368);
            this.helpButton.Name = "helpButton";
            this.helpButton.Size = new System.Drawing.Size(88, 24);
            this.helpButton.TabIndex = 2;
            this.helpButton.Text = "&Help";
            this.helpButton.Click += new System.EventHandler(this.helpButton_Click);
            // 
            // MainForm
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 15);
            this.ClientSize = new System.Drawing.Size(632, 397);
            this.Controls.Add(this.helpButton);
            this.Controls.Add(this.nextButton);
            this.Controls.Add(this.backButton);
            this.Controls.Add(this.tabControl1);
            this.Font = new System.Drawing.Font("Arial Unicode MS", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "MainForm";
            this.Text = "USFM to WordML Converter";
            this.Activated += new System.EventHandler(this.MainForm_Activated);
            this.Closing += new System.ComponentModel.CancelEventHandler(this.MainForm_Closing);
            this.tabControl1.ResumeLayout(false);
            this.optionsFileTabPage.ResumeLayout(false);
            this.footnoteTab.ResumeLayout(false);
            this.footnoteTab.PerformLayout();
            this.templatePage.ResumeLayout(false);
            this.usfmFileTab.ResumeLayout(false);
            this.MergetabPage.ResumeLayout(false);
            this.outputTabPage.ResumeLayout(false);
            this.outputTabPage.PerformLayout();
            this.extrasTabPage.ResumeLayout(false);
            this.extrasTabPage.PerformLayout();
            this.cvTab.ResumeLayout(false);
            this.cvTab.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numLinesNumericUpDown)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.PsalmsTabPage.ResumeLayout(false);
            this.PsalmsTabPage.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.goTabPage.ResumeLayout(false);
            this.ResumeLayout(false);

		}
		#endregion

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] args) 
		{
			commandLine = args;
			Application.Run(new MainForm());
		}

		private void delDupeFileSpecs()
		{
			int i, j;
			for (i = 0, j = 1; j < usfmFileListBox.Items.Count; i++, j++)
			{
				while ((j < usfmFileListBox.Items.Count) &&
					((String)usfmFileListBox.Items[i] == (String)usfmFileListBox.Items[j]))
				{
					usfmFileListBox.Items.RemoveAt(j);
				}
				if ((i < usfmFileListBox.Items.Count) && ((String)usfmFileListBox.Items[i] == ""))
					usfmFileListBox.Items.RemoveAt(i);
			}
		}

		private void deleteButton_Click(object sender, System.EventArgs e)
		{
			int i = usfmFileListBox.SelectedIndex;
			while (i >= 0)
			{
				usfmFileListBox.Items.RemoveAt(i);
				i = usfmFileListBox.SelectedIndex;
			}
			delDupeFileSpecs();
		}

		private void exitButton_Click(object sender, System.EventArgs e)
		{
			Close();
		}

		private void MainForm_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			WriteAppIni();
			WriteJobIni(jobOptionsName);
		}

		private void browseUSFMButton_Click(object sender, System.EventArgs e)
		{
			openFileDialog1.InitialDirectory = SFConverter.jobIni.ReadString("usfmDir",
				Environment.GetFolderPath(System.Environment.SpecialFolder.MyComputer));
			openFileDialog1.FileName = "";
			openFileDialog1.Title = "Please select USFM input file(s).";
			openFileDialog1.Filter = "USFM files (*.sfm;*.sf;*.ptx;*.usfm;*.txt)|*.sfm;*.sf;*.ptx;*.usfm;*.txt|All files (*)|*.*;*)";
			openFileDialog1.FilterIndex = 1;
			openFileDialog1.Multiselect = true;
			if (openFileDialog1.ShowDialog() == DialogResult.OK)
			{
				SFConverter.jobIni.WriteString("usfmDir",
					Path.GetDirectoryName(openFileDialog1.FileNames[0]));
				usfmFileListBox.Items.AddRange(openFileDialog1.FileNames);
			}
			delDupeFileSpecs();
		}

		private void browseOutputButton_Click(object sender, System.EventArgs e)
		{
			saveFileDialog1.InitialDirectory = SFConverter.jobIni.ReadString("outFileDir",
				Environment.GetFolderPath(System.Environment.SpecialFolder.MyComputer));
			saveFileDialog1.FileName = SFConverter.jobIni.ReadString("outputFileName", "");
			saveFileDialog1.Title = "Please specify WordML output file name.";
			saveFileDialog1.DefaultExt = ".xml";
			saveFileDialog1.AddExtension = true;
			saveFileDialog1.Filter = "XML files (*.xml)|*.xml|All files|*.*";
			if (saveFileDialog1.ShowDialog() == DialogResult.OK)
			{
				SFConverter.jobIni.WriteString("outputFileName", saveFileDialog1.FileName);
				SFConverter.jobIni.WriteString("outFileDir",
					Path.GetDirectoryName(saveFileDialog1.FileName));
				outputTextBox.Text = saveFileDialog1.FileName;
			}
		}

		/// <summary>
		/// Create a new job options file with all default parameters.
		/// </summary>
		private void createOptionsButton_Click(object sender, System.EventArgs e)
		{
			WriteJobIni(jobOptionsName);
			saveFileDialog1.InitialDirectory = SFConverter.appIni.ReadString("jobIniDir",
				"");
			saveFileDialog1.FileName = "";
			saveFileDialog1.DefaultExt = "xini";
			saveFileDialog1.AddExtension = true;
			saveFileDialog1.Title = "Please specify the new (default) job options file name.";
			saveFileDialog1.Filter = "Job options files (*.xini)|*.xini|All files|*.*";
			saveFileDialog1.FilterIndex = 0;
			if (saveFileDialog1.ShowDialog() == DialogResult.OK)
			{
				jobOptionsName = saveFileDialog1.FileName;
				SFConverter.appIni.WriteString("jobIniDir", Path.GetDirectoryName(jobOptionsName));
				optionsFileLabel.Text = jobOptionsName;
				try
				{
					File.Delete(jobOptionsName);
				}
				catch
				{
				}
				ReadJobIni(jobOptionsName);
			}
		}

		/// <summary>
		/// Save the current job options file, and read in a new one.
		/// </summary>
		private void readOptionsButton_Click(object sender, System.EventArgs e)
		{
			WriteJobIni(jobOptionsName);
			openFileDialog1.Title = "Please select the job options file to read.";
			openFileDialog1.Filter = "Job options files (*.xini)|*.xini|All files|*.*";
			openFileDialog1.FilterIndex = 0;
			openFileDialog1.FileName = "";
			openFileDialog1.Multiselect = false;
			openFileDialog1.InitialDirectory = SFConverter.appIni.ReadString("jobIniDir", "");
			if (openFileDialog1.ShowDialog() == DialogResult.OK)
			{
				jobOptionsName = openFileDialog1.FileName;
				SFConverter.appIni.WriteString("jobIniDir", Path.GetDirectoryName(jobOptionsName));
				optionsFileLabel.Text = jobOptionsName;
				ReadJobIni(jobOptionsName);
			}
		}

		/// <summary>
		/// Save the current job options with a new name.
		/// </summary>
		private void saveOptionsButton_Click(object sender, System.EventArgs e)
		{
			WriteJobIni(jobOptionsName);
			saveFileDialog1.Title = "Please select the new job options file name.";
			saveFileDialog1.InitialDirectory = SFConverter.appIni.ReadString("jobIniDir", "");
			saveFileDialog1.FileName = "";
			saveFileDialog1.DefaultExt = "xini";
			saveFileDialog1.AddExtension = true;
			saveFileDialog1.Filter = "Job options files (*.xini)|*.xini|All files|*.*";
			saveFileDialog1.FilterIndex = 0;
			if (saveFileDialog1.ShowDialog() == DialogResult.OK)
			{
				jobOptionsName = saveFileDialog1.FileName;
				SFConverter.appIni.WriteString("jobIniDir", Path.GetDirectoryName(jobOptionsName));
				optionsFileLabel.Text = jobOptionsName;
				WriteJobIni(jobOptionsName);
			}
		}

		private void usfmFileListBox_DragDrop(object sender, System.Windows.Forms.DragEventArgs e)
		{
			Object item = null;
			if (e.Data.GetDataPresent(DataFormats.UnicodeText))
			{
				item = (object)e.Data.GetData(DataFormats.UnicodeText);
				usfmFileListBox.Items.Add(item);
			}
			else if (e.Data.GetDataPresent(DataFormats.Text))
			{
				item = (object)e.Data.GetData(DataFormats.Text);
				usfmFileListBox.Items.Add(item);
			}
			else if (e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
				foreach (string s in files)
				{
					usfmFileListBox.Items.Add((object) s);
				}
			}
			delDupeFileSpecs();
		}

		private void usfmFileListBox_DragEnter(object sender, System.Windows.Forms.DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.UnicodeText) ||
				 e.Data.GetDataPresent(DataFormats.Text) ||
				 e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				e.Effect = DragDropEffects.Copy | DragDropEffects.Link;
			}
			else
			{
				e.Effect = DragDropEffects.None;
			}
		}

		private void usfmFileListBox_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Delete)
			{
				deleteButton_Click(sender, null);
			}
		}

		private void backButton_Click(object sender, System.EventArgs e)
		{
			if (tabControl1.SelectedIndex > 0)
            tabControl1.SelectedIndex = tabControl1.SelectedIndex - 1;
		}

		private void nextButton_Click(object sender, System.EventArgs e)
		{
			if (tabControl1.SelectedIndex < tabControl1.TabPages.Count-1)
			{
				tabControl1.SelectedIndex = tabControl1.SelectedIndex + 1;
			}
		}

		private void tabControl1_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			if (tabControl1.SelectedIndex > 0)
				backButton.Visible = true;
			else
                backButton.Visible = false;
			if (tabControl1.SelectedIndex < tabControl1.TabPages.Count - 1)
				nextButton.Visible = true;
			else
				nextButton.Visible = false;
		}

		private void MainForm_Activated(object sender, System.EventArgs e)
		{
			tabControl1_SelectedIndexChanged(sender, null);
			dropCapRadioButton_CheckedChanged(sender, e);
		}

		private void templateBrowseButton_Click(object sender, System.EventArgs e)
		{
			openFileDialog1.Title = "Please select the WordML document to read styles from.";
			openFileDialog1.Filter = "XML files (*.xml)|*.xml|All files|*.*";
			openFileDialog1.FilterIndex = 0;
			openFileDialog1.FileName = "";
			openFileDialog1.Multiselect = false;
			openFileDialog1.InitialDirectory = SFConverter.jobIni.ReadString("templateDir", "");
			if (openFileDialog1.ShowDialog() == DialogResult.OK)
			{
				string fName = openFileDialog1.FileName;
				SFConverter.jobIni.WriteString("templateDir", Path.GetDirectoryName(fName));
				templateFileLabel.Text = fName;
				SFConverter.jobIni.WriteString("templateName", fName);
			}
		}

		private void dropCapRadioButton_CheckedChanged(object sender, System.EventArgs e)
		{
			if (dropCapRadioButton.Checked)
			{
				chapterNameTextBox.Enabled = false;
				chapterSuffixTextBox.Enabled = false;
			}
			else
			{
				chapterNameTextBox.Enabled = true;
				chapterSuffixTextBox.Enabled = true;
			}
		}

		public bool WriteToStatusListBox(string s)
		{
			statusListBox.Items.Add((object) s);
			statusListBox.SelectedIndex = statusListBox.Items.Count -1;
            return true;
		}

		private void convertNowButton_Click(object sender, System.EventArgs e)
		{
			int i;
			convertNowButton.Enabled = false;
			usfxButton.Enabled = false;
			Cursor.Current = Cursors.WaitCursor;
			WriteAppIni();
			WriteJobIni(jobOptionsName);
			Logit.GUIWriteString = new BoolStringDelegate(WriteToStatusListBox);
			Logit.OpenFile(logName);
			// Here we instantiate the object that does most of the work.
			SFConverter.scripture = new Scriptures();

			// Read the input USFM files into internal data structures.
			for (i = 0; i < usfmFileListBox.Items.Count; i++)
				SFConverter.ProcessFilespec((string) usfmFileListBox.Items[i]);

			// Write out the WordML file.
			SFConverter.scripture.WriteToWordML(outputTextBox.Text);
            if (SFConverter.scripture.sfmErrorsFound)
            {
                MessageBox.Show("There were unrecognized markers. See the log for details.", "ERROR");
            }

            if (SFConverter.scripture.fatalError)
            {
                Logit.WriteLine("The input file needs to be conformed to USFM. Please correct and try again.");
                MessageBox.Show("Input file formatting problems must be corrected to conform to USFM before a valid WordML file can be generated.");
            }
			else if (openWordCheckBox.Checked)
			{
				Logit.WriteLine("Starting WINWORD.EXE " + outputTextBox.Text);
				Process.Start("WINWORD.EXE", "\""+outputTextBox.Text+"\"");
			}
			Logit.CloseFile();
			Cursor.Current = Cursors.Default;
			convertNowButton.Enabled = true;
			usfxButton.Enabled = true;
		}

		private void horizTextBox_TextChanged(object sender, System.EventArgs e)
		{
			horizFromText.Set(horizTextBox.Text, 72, 't');
			if (horizFromText.Twips < 0)
				horizFromText.Twips = 0;
			if (horizFromText.Twips > 1440)
				horizFromText.Twips = 1440;
		}

		private void spacingTextBox_TextChanged(object sender, System.EventArgs e)
		{
			dropCapSpacing.Set(spacingTextBox.Text, 459, 't');
			if (dropCapSpacing.Twips < 0)
				dropCapSpacing.Twips = 0;
			if (dropCapSpacing.Twips > 3000)
				dropCapSpacing.Twips = 3000;
		}

		private void sizeTextBox_TextChanged(object sender, System.EventArgs e)
		{
			dropCapSize.Set(sizeTextBox.Text, 26.5, 'p');
			if (dropCapSize.Twips < 0)
				dropCapSize.Twips = 0;
			if (dropCapSize.Twips > 3000)
				dropCapSize.Twips = 3000;
		}

		private void positionTextBox_TextChanged(object sender, System.EventArgs e)
		{
			dropCapPosition.Set(positionTextBox.Text, -3.0, 'p');
			if (dropCapPosition.Twips < -1440)
				dropCapPosition.Twips = -1440;
			if (dropCapPosition.Twips > 3000)
				dropCapPosition.Twips = 3000;
		}

		private void browseXrefButton_Click(object sender, System.EventArgs e)
		{
			openFileDialog1.Title = "Please select the crossreference XML file.";
			openFileDialog1.Filter = "XML files (*.xml)|*.xml|All files|*.*";
			openFileDialog1.FilterIndex = 0;
			openFileDialog1.FileName = "";
			openFileDialog1.Multiselect = false;
			openFileDialog1.InitialDirectory = SFConverter.jobIni.ReadString("xrefDir", "");
			if (openFileDialog1.ShowDialog() == DialogResult.OK)
			{
				string fName = openFileDialog1.FileName;
				SFConverter.jobIni.WriteString("xrefDir", Path.GetDirectoryName(fName));
				xrefLabel.Text = fName;
				SFConverter.jobIni.WriteString("xrefName", fName);
			}

		}

		private void customFootnoteCallerCheckBox_CheckedChanged(object sender, System.EventArgs e)
		{
			customFootnoteCallerTextBox.Enabled = customFootnoteCallerCheckBox.Checked;
		}

		private void customXrefCallerCheckBox_CheckedChanged(object sender, System.EventArgs e)
		{
			customXrefCallerTextBox.Enabled = customXrefCallerCheckBox.Checked;
		}

		private void changeListButton_Click(object sender, System.EventArgs e)
		{
			openFileDialog2.Title = "Please select the substitution file to  read.";
			openFileDialog2.Filter = "XML files (*.xml)|*.xml|All files|*.*";
			openFileDialog2.FilterIndex = 0;
			openFileDialog2.FileName = "";
			openFileDialog2.Multiselect = false;
			openFileDialog2.InitialDirectory = SFConverter.jobIni.ReadString("substitutionDir", "");
			if (openFileDialog2.ShowDialog() == DialogResult.OK)
			{
				string fileName = openFileDialog2.FileName;
				SFConverter.jobIni.WriteString("substitutionDir", Path.GetDirectoryName(fileName));
				substitutionFileLabel.Text =  fileName;
				SFConverter.jobIni.WriteString("substitutionName", fileName);
			}

		}

		private void enableSubstitutionsCheckBox_CheckedChanged(object sender, System.EventArgs e)
		{
			changeListButton.Enabled = enableSubstitutionsCheckBox.Checked;
			substitutionFileLabel.Enabled = enableSubstitutionsCheckBox.Checked;
		}

		private void usfxButton_Click_1(object sender, System.EventArgs e)
		{
			int i;

			convertNowButton.Enabled = false;
			usfxButton.Enabled = false;
			Cursor.Current = Cursors.WaitCursor;
			WriteAppIni();
			WriteJobIni(jobOptionsName);
			Logit.GUIWriteString = new BoolStringDelegate(WriteToStatusListBox);
			Logit.OpenFile(logName);
			// Here we instantiate the object that does most of the work.
			SFConverter.scripture = new Scriptures();

			// Read the input USFM files into internal data structures.
			for (i = 0; i < usfmFileListBox.Items.Count; i++)
				SFConverter.ProcessFilespec((string) usfmFileListBox.Items[i]);

			// Write out the WordML file.
			SFConverter.scripture.WriteUSFX(outputTextBox.Text);

			if (openWordCheckBox.Checked)
			{
				Logit.WriteLine("Starting WINWORD.EXE " + outputTextBox.Text);

				Process.Start("WINWORD.EXE", "\""+outputTextBox.Text+"\"");
			}
			Logit.CloseFile();
			Cursor.Current = Cursors.Default;
			convertNowButton.Enabled = true;
			usfxButton.Enabled = true;	
		}

		private void helpButton_Click(object sender, System.EventArgs e)
		{
			string helpfile = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "tab"+(1+tabControl1.SelectedIndex).ToString()+".htm");
			try
			{
				// Help.ShowHelp(this, "file:///"+helpfile);
				Process.Start(helpfile);
			}
			catch
			{
				MessageBox.Show("Sorry, but I couldn't find this help file: "+helpfile);
			}
		}

		private void autoCalcDropCapCheckBox_CheckedChanged(object sender, System.EventArgs e)
		{
			numLinesNumericUpDown.Enabled = horizTextBox.Enabled =
				spacingTextBox.Enabled = sizeTextBox.Enabled =
				positionTextBox.Enabled = dropCapBeforeTextBox.Enabled =
				!autoCalcDropCapCheckBox.Checked;
		}

		private void dropCapBeforeTextBox_TextChanged(object sender, System.EventArgs e)
		{
				dropCapBefore.Set(dropCapBeforeTextBox.Text, 0.0, 'p');
				if (dropCapPosition.Twips < 0)
					dropCapPosition.Points = 0.0;
				if (dropCapPosition.Points > 720.0)
					dropCapPosition.Points = 720.0;
		}


	}
}
