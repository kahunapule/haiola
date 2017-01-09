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
			LoadMiscTab();
			LoadHtmlTab();
		}



		private void MakeBookNameItem(string langName, string colHeader)
		{
			ListViewItem item = new ListViewItem(langName);
			item.SubItems.Add(colHeader);
			SetLastSubItemName(item, "Edit");
		}

		private void LoadMiscTab()
		{
			listInputProcesses.SuspendLayout();
			listInputProcesses.Items.Clear();
			foreach (string filename in m_options.PreprocessingTables)
				listInputProcesses.Items.Add(filename);
            languageNameTextBox.Text = m_options.m_languageName;
            ethnologueCodeTextBox.Text = m_options.m_languageId;
			listInputProcesses.ResumeLayout();
            KhmerNumeralsRadioButton.Checked = m_options.m_useKhmerDigits;
            ignoreExtrasCheckBox.Checked = m_options.m_ignoreExtras;
		}

		
        private void LoadHtmlTab()
        {
            copyrightLinkTextBox.Text = m_options.m_copyrightLink;
            homeLinkTextBox.Text = m_options.m_homeLink;
            footerHtmlTextBox.Text = m_options.m_footerHtml;
            indexPageTextBox.Text = m_options.m_indexHtml;
            licenseTextBox.Text = m_options.m_licenseHtml;
        }


		// Enhance: validate (e.g., numeric fields) before attempting save.
		void SaveOptions()
		{
			SaveMiscTab();
            SaveHtmlTab();
		}

		internal const string projectColName = "<project>"; // Enhance: init from first item originally in lstBookNames


        private void SaveHtmlTab()
        {
            m_options.m_copyrightLink = copyrightLinkTextBox.Text;
            m_options.m_homeLink = homeLinkTextBox.Text;
            m_options.m_footerHtml = footerHtmlTextBox.Text;
            m_options.m_indexHtml = indexPageTextBox.Text;
            m_options.m_licenseHtml = licenseTextBox.Text;
        }

		private void SaveMiscTab()
		{
			List<string> newTables = new List<string>(listInputProcesses.Items.Count);
			foreach (string fileName in listInputProcesses.Items)
				newTables.Add(fileName);
			m_options.PreprocessingTables = newTables;
            m_options.m_languageName = languageNameTextBox.Text;
            m_options.m_languageId = ethnologueCodeTextBox.Text;
            m_options.m_useKhmerDigits = KhmerNumeralsRadioButton.Checked;
            m_options.m_ignoreExtras = ignoreExtrasCheckBox.Checked;
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

		internal void SetLastSubItemName(ListViewItem item, string val)
		{
			ListViewItem.ListViewSubItem lastItem = item.SubItems[item.SubItems.Count - 1];
			lastItem.Name = val;
		}

		private void taBookNames_Click(object sender, EventArgs e)
		{

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

		private void btnOK_Click(object sender, EventArgs e)
		{
			SaveOptions();
		}

		private void btnRemoveInputProcess_Click(object sender, EventArgs e)
		{
			if (listInputProcesses.SelectedIndices.Count > 0)
				listInputProcesses.Items.RemoveAt(listInputProcesses.SelectedIndices[0]);
		}

        private void ethnologueCodeTextBox_TextChanged(object sender, EventArgs e)
        {
            btnOK.Enabled = ethnologueCodeTextBox.Text.Length == 3;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {

        }

        private void tabPage1_Click(object sender, EventArgs e)
        {

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