using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Xml;

namespace sepp
{
	public partial class OptionsDlg : Form
	{
		private Options m_options;
		private ProjectManager m_manager;
		private string m_originalCollation;

		public OptionsDlg(ProjectManager manager)
		{
			m_manager = manager;
			InitializeComponent();
		}

		internal void InitDlg(Options options)
		{
			m_options = options;
			LoadOptions();
		}

		void LoadOptions()
		{
			LoadInfoTab();
			LoadMiscTab();
			LoadConcTab();
			LoadSortTab();
			LoadLocalizeTab();
			LoadBookNamesTab();
			LoadBackMatterTab();
			LoadAdvancedTab();
            LoadHtmlTab();
		}

		private void LoadSortTab()
		{
			m_originalCollation = tbxCollating.Text;
			foreach (CultureInfo info in CultureInfo.GetCultures(CultureTypes.AllCultures))
				comboSort.Items.Add(info.Name);
			switch(m_options.m_collationMode)
			{
				case CollationMode.kDefault:
					comboSort.SelectedIndex = 0;
					tbxCollating.Text = "";
					break;
				case CollationMode.kLocale:
					comboSort.SelectedItem = m_options.m_sortSpec;
					// Enhance JohnT: do something intelligent if it fails?
					tbxCollating.Text = "";
					break;
				case CollationMode.kCustomSimple:
					comboSort.SelectedIndex = 1;
					tbxCollating.Text = m_options.m_sortSpec;
					break;
				case CollationMode.kCustomICU:
					comboSort.SelectedIndex = 2;
					tbxCollating.Text = m_options.m_sortSpec;
					break;
			}
		}

		private void LoadBackMatterTab()
		{
			lstBackMatter.SuspendLayout();
			foreach(ExtraFileInfo info in m_options.ExtraFiles)
			{
				ListViewItem item = new ListViewItem(info.FileName);
				lstBackMatter.Items.Add(item);
				item.SubItems.Add(info.HotLinkText);
				SetLastSubItemName(item, "Edit");
			}
			lstBackMatter.ResumeLayout();
		}

		private void LoadBookNamesTab()
		{
			lstBookNames.SuspendLayout();
			bool fGotAsterisk = false;
			foreach (BookNameColumnInfo info in m_options.BookNameColumns)
			{
				string langName = info.LanguageName;
				if (langName == "*")
				{
					langName = projectColName;
					fGotAsterisk = true;
				}
				MakeBookNameItem(langName, info.ColuumnHeaderText);
			}
			if (!fGotAsterisk)
				MakeBookNameItem(projectColName, "");
			lstBookNames.ResumeLayout();
		}

		private void MakeBookNameItem(string langName, string colHeader)
		{
			ListViewItem item = new ListViewItem(langName);
			lstBookNames.Items.Add(item);
			item.SubItems.Add(colHeader);
			SetLastSubItemName(item, "Edit");
		}

		private void LoadAdvancedTab()
		{
			tbxNonCanonical.Text = m_options.m_nonCanonicalClassesSrc;
			tbxExtraClasses.Text = m_options.m_excludeClassesSrc;
			tbxNotesClass.Text= m_options.NotesClass;
		}

		private void LoadLocalizeTab()
		{
			tbxHeadingRef.Text = m_options.m_headingRefSrc;
			tbxNotesRef.Text = m_options.m_notesRefSrc;
			tbxConcordance.Text = m_options.ConcordanceLinkText;
			tbxLoading.Text = m_options.LoadingLabelText;
			tbxIntroduction.Text = m_options.IntroductionLinkText;
			tbxBookChap.Text = m_options.m_bookChapText;
			tbxPrevChap.Text = m_options.m_prevChapText;
			tbxNextChap.Text = m_options.m_nextChapText;
            chapterLabelTextBox.Text = m_options.m_chapterLabel;
            psalmLabelTextBox.Text = m_options.m_psalmLabel;

		}
		private void LoadMiscTab()
		{
			chkChapterPerfile.Checked = m_options.ChapterPerFile;
			listInputProcesses.SuspendLayout();
			listInputProcesses.Items.Clear();
			foreach (string filename in m_options.PreprocessingTables)
				listInputProcesses.Items.Add(filename);
            languageNameTextBox.Text = m_options.m_languageName;
            ethnologueCodeTextBox.Text = m_options.m_languageId;
			listInputProcesses.ResumeLayout();
		}

		private void LoadConcTab()
		{
			chkMergeCase.Checked = m_options.MergeCase;
			tbxWordformingChars.Text = m_options.WordformingChars;
			tbxExcludeWords.Text = m_options.ExcludeWordsSrc;
			tbxMaxFreq.Text = m_options.MaxFreqSrc;
			tbxPhrases.Text = m_options.PhrasesSrc;
			tbxMinContext.Text = m_options.MinContextLength.ToString();
			tbxMaxContext.Text = m_options.MaxContextLength.ToString();

		}

        private void LoadHtmlTab()
        {
            copyrightLinkTextBox.Text = m_options.m_copyrightLink;
            homeLinkTextBox.Text = m_options.m_homeLink;
            footerHtmlTextBox.Text = m_options.m_footerHtml;
        }


		// Enhance: validate (e.g., numeric fields) before attempting save.
		void SaveOptions()
		{
			SaveInfoTab();
			SaveMiscTab();
			SaveConcTab();
			SaveSortTab();
			SaveLocalizeTab();
			SaveBookNamesTab();
			SaveBackMatterTab();
			SaveAdvancedTab();
            SaveHtmlTab();
		}

		private void SaveSortTab()
		{
			m_options.m_sortSpec = GetCollationData();
			m_options.m_collationMode = GetCollationMode();
		}

		private void SaveBackMatterTab()
		{
			List<ExtraFileInfo> extraFiles = new List<ExtraFileInfo>();
			foreach (ListViewItem item in lstBackMatter.Items)
			{
				if (!string.IsNullOrEmpty(item.SubItems[1].Text))
					extraFiles.Add(new ExtraFileInfo(item.Text, item.SubItems[1].Text));
			}
			m_options.ExtraFiles = extraFiles;
		}

		internal const string projectColName = "<project>"; // Enhance: init from first item originally in lstBookNames

		private void SaveBookNamesTab()
		{
			List<BookNameColumnInfo> cols = new List<BookNameColumnInfo>();
			foreach (ListViewItem item in lstBookNames.Items)
			{
				if (!string.IsNullOrEmpty(item.SubItems[1].Text))
				{
					string name = item.Text;
					if (name == projectColName)
						name = "*";
					cols.Add(new BookNameColumnInfo(name, item.SubItems[1].Text));
				}
			}
			m_options.BookNameColumns = cols;
		}

		private void SaveAdvancedTab()
		{
			m_options.m_nonCanonicalClassesSrc = tbxNonCanonical.Text;
			m_options.m_excludeClassesSrc = tbxExtraClasses.Text;
			m_options.NotesClass = tbxNotesClass.Text;
		}

        private void SaveHtmlTab()
        {
            m_options.m_copyrightLink = copyrightLinkTextBox.Text;
            m_options.m_homeLink = homeLinkTextBox.Text;
            m_options.m_footerHtml = footerHtmlTextBox.Text;
        }

		private void SaveLocalizeTab()
		{
			m_options.m_headingRefSrc = tbxHeadingRef.Text;
			m_options.m_notesRefSrc = tbxNotesRef.Text;
			m_options.ConcordanceLinkText = tbxConcordance.Text;
			m_options.LoadingLabelText = tbxLoading.Text;
			m_options.IntroductionLinkText = tbxIntroduction.Text;
			m_options.m_bookChapText = tbxBookChap.Text;
			m_options.m_prevChapText = tbxPrevChap.Text;
			m_options.m_nextChapText = tbxNextChap.Text;
            m_options.m_chapterLabel = chapterLabelTextBox.Text;
            m_options.m_psalmLabel = psalmLabelTextBox.Text;
		}

		private void SaveMiscTab()
		{
			m_options.ChapterPerFile = chkChapterPerfile.Checked;
			List<string> newTables = new List<string>(listInputProcesses.Items.Count);
			foreach (string fileName in listInputProcesses.Items)
				newTables.Add(fileName);
			m_options.PreprocessingTables = newTables;
            m_options.m_languageName = languageNameTextBox.Text;
            m_options.m_languageId = ethnologueCodeTextBox.Text;

		}

		private void SaveConcTab()
		{
			m_options.MergeCase = chkMergeCase.Checked;
			m_options.WordformingChars = tbxWordformingChars.Text;
			m_options.ExcludeWordsSrc = tbxExcludeWords.Text;
			m_options.MaxFreqSrc = tbxMaxFreq.Text; // Enhance: validate
			m_options.PhrasesSrc = tbxPhrases.Text;
			int temp;
			if (int.TryParse(tbxMinContext.Text, out temp))
				m_options.MinContextLength = temp;
			if (int.TryParse(tbxMaxContext.Text, out temp))
				m_options.MaxContextLength = temp;
		}

		private void LoadInfoTab()
		{
			lstFiles.SuspendLayout();
			lstFiles.Items.Clear();
			foreach (InputFileInfo info in m_options.InputFiles)
				lstFiles.Items.Add(MakeFileItem(info.StandardAbbr, info.FileName, info.VernAbbr, info.CrossRefName, info.IntroFileName));
			lstFiles.ResumeLayout();
		}

		ListViewItem MakeFileItem(string abbr, string fileName, string vernAbbr, string xrefName, string introFile)
		{
			ListViewItem item = new ListViewItem(abbr);
			SetLastSubItemName(item, "StdAbbr");
			item.SubItems.Add(Path.GetFileNameWithoutExtension(fileName));
			item.SubItems.Add(vernAbbr);
			SetLastSubItemName(item, "Edit");
			item.SubItems.Add(xrefName);
			SetLastSubItemName(item, "Edit");
			item.SubItems.Add(introFile);
			SetLastSubItemName(item, m_manager.IntroDir);
			return item;
		}

		private void SaveInfoTab()
		{
			List<InputFileInfo> result = new List<InputFileInfo>();
			foreach (ListViewItem item in lstFiles.Items)
			{
				result.Add(new InputFileInfo(
					item.SubItems[1].Text,
					item.Text,
					item.SubItems[2].Text,
					item.SubItems[3].Text,
					item.SubItems[4].Text));
			}
			m_options.InputFiles = result;
		}

		internal void SetLastSubItemName(ListViewItem item, string val)
		{
			ListViewItem.ListViewSubItem lastItem = item.SubItems[item.SubItems.Count - 1];
			lastItem.Name = val;
		}


		private void taBookNames_Click(object sender, EventArgs e)
		{

		}

		private void btnMoveBookNameUp_Click(object sender, EventArgs e)
		{
			MoveListItem(lstBookNames, -1);
		}

		private void btnMoveBookNameDown_Click(object sender, EventArgs e)
		{
			MoveListItem(lstBookNames, 1);

		}

		private void btnMoveUpBackMatter_Click(object sender, EventArgs e)
		{
			MoveListItem(lstBackMatter, -1);
		}

		private void MoveListItem(ListView list, int delta)
		{

			if (list.SelectedIndices.Count == 0)
				return;
			int oldIndex = list.SelectedIndices[0];
			int newIndex = oldIndex + delta;
			if (newIndex < 0 || newIndex >= list.Items.Count)
				return;
			list.SuspendLayout();
			ListViewItem item = list.Items[oldIndex];
			list.Items.RemoveAt(oldIndex);
			list.Items.Insert(newIndex, item);
			item.Selected = true;
			list.Focus(); // Seems to be necessary to get the highlight to show up.
			list.ResumeLayout();
		}

		private void btnMoveDownBackMatter_Click(object sender, EventArgs e)
		{
			MoveListItem(lstBackMatter, 1);
		}

		private void lstFiles_SelectedIndexChanged(object sender, EventArgs e)
		{

		}

		private void textBox2_TextChanged(object sender, EventArgs e)
		{

		}

		private void tabMisc_Click(object sender, EventArgs e)
		{

		}

		private void btnAddInputProcess_Click(object sender, EventArgs e)
		{
			OpenFileDialog dlg = new OpenFileDialog();
			dlg.CheckFileExists = true;
			dlg.InitialDirectory = m_manager.WorkPath;
			//dlg.Multiselect = true;
			dlg.Filter = "Change files (*.cct; *.re)|*.cct;*.re|CC tables(*.cct)|*.cct|Regular Expressions(*.re)|*.re";
			if (dlg.ShowDialog(this) == DialogResult.OK)
			{
				string newFilePath = dlg.FileName;
				string newFileName = Path.GetFileName(newFilePath);
				string newFileDir = Path.GetDirectoryName(newFilePath);
				if (newFileDir.ToLowerInvariant() != m_manager.WorkPath.ToLowerInvariant())
				{
					if (MessageBox.Show(this, "Preprocessing files must be in the work directory. Copy it there?", "Note", MessageBoxButtons.YesNo) ==
						DialogResult.Yes)
					{
						File.Copy(newFilePath, Path.Combine(m_manager.WorkPath, newFileName));
					}
					else
					{
						return;
					}
				}
				// Enhance: this provides no way to insert at start. Make a move up button?
				int insertAt = listInputProcesses.Items.Count;
				if (listInputProcesses.SelectedIndices.Count > 0)
					insertAt = listInputProcesses.SelectedIndices[0] + 1;
				listInputProcesses.Items.Insert(insertAt, newFileName);
			}
		}

		private void btnAdjustFiles_Click(object sender, EventArgs e)
		{
			if (AdjustFileList(m_manager.OurWordPath))
				return;
			if (AdjustFileList(m_manager.PreProcessPath))
				return;
			if (AdjustFileList(m_manager.UsfmPath))
				return;
		}

		private readonly string[] canonicalAbbrs = {
    		"Gen",
    		"Exo",
    		"Lev",
    		"Num",
    		"Deu",
    		"Jos",
    		"Jdg",
    		"Rut",
    		"1Sa",
    		"2Sa",
    		"1Ki",
    		"2Ki",
    		"1Ch",
    		"2Ch",
    		"Ezr",
    		"Neh",
    		"Est",
            "Job",
    		"Psa",
    		"Pro",
    		"Ecc",
    		"Sng",
    		"Isa",
    		"Jer",
    		"Lam",
    		"Ezk",
    		"Dan",
    		"Hos",
    		"Jol",
    		"Amo",
    		"Oba",
    		"Jon",
    		"Mic",
    		"Nam",
    		"Hab",
    		"Zep",
    		"Hag",
    		"Zec",
    		"Mal",
    		"Mat",
    		"Mrk",
    		"Luk",
    		"Jhn",
    		"Act",
    		"Rom",
    		"1Co",
    		"2Co",
    		"Gal",
    		"Eph",
    		"Php",
    		"Col",
    		"1Th",
    		"2Th",
    		"1Ti",
    		"2Ti",
    		"Tit",
    		"Phm",
    		"Heb",
    		"Jas",
    		"1Pe",
    		"2Pe",
    		"1Jn",
    		"2Jn",
    		"3Jn",
    		"Jud",
    		"Rev",
            "???"
    	};

		/// <summary>
		/// Add missing files from the specified directory.
		/// We consider any files in the directory candidates, except ones ending in .txt
		/// which may be reports from earlier runs.
		/// Also remove any files no longer present from the list.
		/// </summary>
		/// <param name="sourceDir"></param>
		/// <returns></returns>
		private bool AdjustFileList(string sourceDir)
		{
			if (!Directory.Exists(sourceDir))
				return false;
			Dictionary<string, ListViewItem> existingFiles = new Dictionary<string, ListViewItem>();
			foreach (ListViewItem item in lstFiles.Items)
				existingFiles[item.SubItems[1].Text.ToLowerInvariant()] = item;

			lstFiles.SuspendLayout();

			foreach (string path in Directory.GetFiles(sourceDir))
			{
                string lcpath = Path.GetFileName(path).ToLowerInvariant();
				if ((lcpath.CompareTo("conversionreports.txt") == 0) || (Path.GetExtension(lcpath).CompareTo(".bak")== 0))
					continue;
				string fileName = Path.ChangeExtension(Path.GetFileName(path), "xml").ToLowerInvariant();
				ListViewItem existingItem;
				string key = Path.GetFileNameWithoutExtension(fileName).ToLowerInvariant();
				if (existingFiles.TryGetValue(key, out existingItem))
				{
					existingFiles.Remove(key);
					continue;
				}
				// Try to guess the abbreviation from the file name
				string stdAbbr = GuessStandardAbbr(path);
                string xrefAbbr = GuessXRef(path);
				int insertAt = FigureInsertPosition(stdAbbr);
				// Enhance JohnT: is there a way to obtain a better guess from a USFM/OW file?
				// Enhance JohnT: almost sure there is a field from which we can get a good guess for this.
				// Enhance JohnT: If there is a file in the Intro directory whose name contains fileName, guess that.
                // Kahunapule: default the crossreference abbreviations to non-abbreviated vernacular book names, not
                // English abbreviations, assuming that the reader will understand those better. They don't have to be
                // abbreviated, but they do have to be understandable to the target audience.
				lstFiles.Items.Insert(insertAt, MakeFileItem(stdAbbr, Path.GetFileNameWithoutExtension(path), xrefAbbr,
					xrefAbbr, ""));
			}
			// Remove any items for which files no longer exist.
			foreach(ListViewItem item in existingFiles.Values)
				lstFiles.Items.Remove(item);
			lstFiles.ResumeLayout();

			return true;
		}

		/// <summary>
		/// Guess the book name to look for in cross-refs. We use \h if we can find one at all, otherwise, \mt1
        /// or \mt (omitting \mt2 and \mt3).
		/// </summary>
		/// <param name="path">File name to look for crossreference book name.</param>
		/// <returns></returns>
		private string GuessXRef(string path)
		{
			string fallback = "";
			StreamReader reader = new StreamReader(path, Encoding.UTF8);
			while (!reader.EndOfStream)
			{
				string line = reader.ReadLine();
				if (line.StartsWith(@"\h"))
				{
					return line.Substring(2).Trim();
				}
				if (line.StartsWith(@"\mt "))
					fallback = line.Substring(3).Trim();
                if (line.StartsWith(@"\mt1 "))
                    fallback = line.Substring(4).Trim();
            }
			return fallback;
		}

		private Dictionary<string, int> canonicalIndex;

		// Decide where in the list to insert the item.
		private int FigureInsertPosition(string stdAbbr)
		{
			if (canonicalIndex == null)
			{
				canonicalIndex = new Dictionary<string, int>();
				for (int i = 0; i < canonicalAbbrs.Length; i++)
					canonicalIndex[canonicalAbbrs[i].ToLowerInvariant()] = i;
			}
			int index = GetIndexOfAbbr(stdAbbr.ToLowerInvariant());
			int result = 0;
			// return the index in lstFiles.Items of the first item whose abbr comes after the new one.
			// If there is no such we will insert at the end.
			// If there are duplicates the new one will go after the old.
			foreach (ListViewItem item in lstFiles.Items)
			{
				int itemIndex = GetIndexOfAbbr(item.Text.ToLowerInvariant());
				if (itemIndex > index)
					return result; // index of item in Items, not of abbr in canonicalAbbrs
				result++;
			}
			return result; // by now after end.
		}

		private int GetIndexOfAbbr(string stdAbbr)
		{
			int index = canonicalAbbrs.Length;
			canonicalIndex.TryGetValue(stdAbbr, out index);
			return index;
		}

		private string GuessStandardAbbr(string pathName)
		{
            // First guess: use the ID line.
            string result = "";
            string line;
            StreamReader sr = new StreamReader(pathName);
            while ((result.Length < 1) && (!sr.EndOfStream))
            {
                line = sr.ReadLine();
                if (line.StartsWith(@"\id ") && (line.Length > 6))
                {
                    result = line.Substring(4, 3).ToUpper(CultureInfo.InvariantCulture);
                }
            }
            sr.Close();
            if (result.Length > 0)
                return result;
            // Second guess: ask the user with a triple question mark.
            return "???";

		}


		private void lstFiles_MouseUp(object sender, MouseEventArgs e)
		{
			MouseUpInList(lstFiles, e);
		}

		/// <summary>
		/// Some cases below are unique to a particular list.
		/// </summary>
		/// <param name="list"></param>
		/// <param name="e"></param>
		private void MouseUpInList(ListView list, MouseEventArgs e)
		{
			ListViewHitTestInfo hti = list.HitTest(e.Location);
			ListViewItem.ListViewSubItem si = hti.SubItem;
			if (si == null || string.IsNullOrEmpty(si.Name))
				return;
			if (si.Name == "StdAbbr")
			{
				// Click on the item itself...that is the canonical abbreviation...make a combo.
				ComboBox cb = new ComboBox();
				Rectangle bounds = new Rectangle(si.Bounds.Left, si.Bounds.Top,
					list.Columns[0].Width, si.Bounds.Height);
				bounds.Intersect(list.ClientRectangle);
				cb.Bounds = bounds;
				cb.Tag = hti.Item;
				cb.DropDownStyle = ComboBoxStyle.DropDownList;
				foreach (string abbr in canonicalAbbrs)
				{
					cb.Items.Add(abbr);
					if (abbr == hti.Item.Text)
						cb.SelectedIndex = cb.Items.Count - 1;
				}
				// Enhance JohnT: what should we do if no item is selected at this point and si.Text is not null?
				// That means a non-standard standard abbreviation!
				list.Controls.Add(cb);
				// Set these up last, especially selected index changed, which can get fired as we set up the items.
				cb.LostFocus += new EventHandler(cb_LostFocus_Abbr);
				cb.SelectedIndexChanged += new EventHandler(cb_LostFocus_Abbr);
				cb.Focus();
				return;
			}
			if (si.Name == "Edit")
			{
				// Make a text box to edit the subitem contents.
				TextBox tb = new TextBox();
				tb.Bounds = si.Bounds;
				tb.Text = si.Text;
				tb.LostFocus += new EventHandler(tb_LostFocus);
				tb.Tag = si;
				list.Controls.Add(tb);
				tb.SelectAll();
				tb.Focus();
			}
			else if (Directory.Exists(si.Name))
			{
				// Make a combo box to select one of the files in the directory.
				ComboBox cb = new ComboBox();
				Rectangle bounds = si.Bounds;
				bounds.Intersect(list.ClientRectangle);
				cb.Bounds = bounds;
				cb.LostFocus += new EventHandler(cb_LostFocus);
				cb.Tag = si;
				cb.DropDownStyle = ComboBoxStyle.DropDownList;
				foreach (string filePath in Directory.GetFiles(si.Name))
				{
					string fileName = Path.GetFileName(filePath);
					cb.Items.Add(fileName);
					if (fileName == si.Text)
						cb.SelectedIndex = cb.Items.Count - 1;
				}
				// Enhance JohnT: what should we do if no item is selected at this point and si.Text is not null?
				// That means the file name found in the options file is not present!
				list.Controls.Add(cb);
				cb.Focus();
			}
		}
		void cb_LostFocus_Abbr(object sender, EventArgs e)
		{
			ComboBox cb = sender as ComboBox;
			ListViewItem item = (cb).Tag as ListViewItem;
			string newText = cb.Text;
			cb.Parent.Controls.Remove(cb);
			if (String.IsNullOrEmpty(newText))
				return;
			item.Text = newText;
			int index = FigureInsertPosition(newText);
			if (index != 0 && lstFiles.Items[index - 1] == item)
				return; // already in correct position.
			lstFiles.SuspendLayout();
			lstFiles.Items.Remove(item);
			lstFiles.Items.Insert(FigureInsertPosition(newText), item);
			lstFiles.ResumeLayout();
		}

		void cb_LostFocus(object sender, EventArgs e)
		{
			ComboBox cb = sender as ComboBox;
			ListViewItem.ListViewSubItem si = (cb).Tag as ListViewItem.ListViewSubItem;
			si.Text = cb.Text;
			cb.Parent.Controls.Remove(cb);
		}

		void tb_LostFocus(object sender, EventArgs e)
		{
			TextBox tb = sender as TextBox;
			ListViewItem.ListViewSubItem si = (tb).Tag as ListViewItem.ListViewSubItem;
			si.Text = tb.Text;
			tb.Parent.Controls.Remove(tb);
		}

		private void OptionsDlg_Load(object sender, EventArgs e)
		{

		}

		private void btnOK_Click(object sender, EventArgs e)
		{
			SaveOptions();
		}

		private void btnRemoveInputProcess_Click(object sender, EventArgs e)
		{
			if (listInputProcesses.SelectedIndices.Count > 0)
				listInputProcesses.Items.RemoveAt(listInputProcesses.SelectedIndices[0]);
		}

		private void btnAdjustBmFiles_Click(object sender, EventArgs e)
		{
			new FileListAdjuster().Run(this, lstBackMatter, m_manager.ExtrasPath, "*.*");
		}

		private void lstBackMatter_MouseUp(object sender, MouseEventArgs e)
		{
			MouseUpInList(lstBackMatter, e);
		}

		private void lstBookNames_MouseUp(object sender, MouseEventArgs e)
		{
			MouseUpInList(lstBookNames, e);
		}

		private void btnAdjustBookNamesList_Click(object sender, EventArgs e)
		{
			new BookNamesFileAdjuster().Run(this, lstBookNames, m_manager.RootWorkPath, "BookNames_*.*");
		}

		CollationMode GetCollationMode()
		{
			if (comboSort.SelectedIndex <= 0)
				return CollationMode.kDefault;
			if (comboSort.SelectedIndex == 1)
				return CollationMode.kCustomSimple;
			if (comboSort.SelectedIndex == 2)
				return CollationMode.kCustomICU;
			return CollationMode.kLocale;
		}

		string GetCollationData()
		{
			switch(GetCollationMode())
			{
				case CollationMode.kDefault:
					return "";
				case CollationMode.kLocale:
					return comboSort.SelectedItem.ToString();
				case CollationMode.kCustomSimple:
				case CollationMode.kCustomICU:
					return tbxCollating.Text;
			}
			return ""; // unreachable.
		}

		private void btnTestSort_Click(object sender, EventArgs e)
		{

			List<string> words = new List<string>(tbxTestWords.Text.Split(new string[] {Environment.NewLine},
				StringSplitOptions.RemoveEmptyEntries));
			IComparer<string> comp = m_options.ComparerFor(GetCollationMode(), GetCollationData());
			words.Sort(comp);
			StringBuilder bldr = new StringBuilder();
			foreach (string word in words)
				bldr.AppendLine(word);
			tbxTestWords.Text = bldr.ToString();
		}

		private void comboSort_SelectedIndexChanged(object sender, EventArgs e)
		{
			switch (GetCollationMode())
			{
				case CollationMode.kCustomSimple:
					tbxCollating.Text = m_originalCollation;
					break;
				default:
					tbxCollating.Text = "";
					break;
			}
		}

        private void clearReloadButton_Click(object sender, EventArgs e)
        {
            lstFiles.Items.Clear();
            m_options.InputFiles.Clear();
            btnAdjustFiles_Click(sender, e);
        }



	}

	class FileListAdjuster
	{
		public void Run(OptionsDlg dlg, ListView list, string inputPath, string pattern)
		{
			Dictionary<string, ListViewItem> oldItems = new Dictionary<string, ListViewItem>();
			foreach (ListViewItem item in list.Items)
				oldItems[item.Text] = item;
			list.SuspendLayout();
			bool gotOne = false;
			if (Directory.Exists(inputPath))
			{
				foreach (string path in Directory.GetFiles(inputPath, pattern))
				{
					string itemName = GetItemName(path);
					gotOne = true;
					ListViewItem oldItem;
					string key = itemName.ToLowerInvariant();
					if (oldItems.TryGetValue(key, out oldItem))
						oldItems.Remove(key);
					else
					{
						ListViewItem newItem = new ListViewItem(itemName);
						list.Items.Add(newItem);
						newItem.SubItems.Add("");
						dlg.SetLastSubItemName(newItem, "Edit");
					}
				}
			}
			AdjustDeleteList(oldItems);
			foreach (ListViewItem item in oldItems.Values)
				list.Items.Remove(item);
			list.ResumeLayout();
			if (!gotOne)
				MessageBox.Show(dlg, "Did not find any files in " + inputPath, "Warning", MessageBoxButtons.OK,
								MessageBoxIcon.Warning);
			
		}

		internal virtual string GetItemName(string path)
		{
			return Path.GetFileName(path);
		}

		// Remove from dictionary any items that should not be deleted, even though they do not correspond to files.
		// Default does nothing
		internal virtual void AdjustDeleteList(Dictionary<string, ListViewItem> oldItems)
		{
			
		}
	}

	class BookNamesFileAdjuster : FileListAdjuster
	{
		internal override void AdjustDeleteList(Dictionary<string, ListViewItem> oldItems)
		{
			base.AdjustDeleteList(oldItems);
			oldItems.Remove(OptionsDlg.projectColName);
		}

		internal override string GetItemName(string path)
		{
			return Path.GetFileNameWithoutExtension(path).Substring("BookNames_".Length);
		}
	}
}