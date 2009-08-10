using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Xml;

namespace sepp
{
	public class Options
	{
		// The XML document from which we originally read the options.
		// We retain this partly in hopes of retaining useful comments.
		private XmlDocument m_doc;

		#region Basic Data
		// Primary option data, directly corresponding to the things stored in the XML file or shown in the dialog.
		private string m_copyright;
		private bool m_chapterPerFile;
		List<ExtraFileInfo> m_extraFiles = new List<ExtraFileInfo>();
		string m_wordformingChars;
		int m_maxContextLength = 50;
		int m_minContextLength = 35;
		ConcGenerator.IndexTypes m_indexType = ConcGenerator.IndexTypes.alphaTree;
		List<string> m_phrases = new List<string>();
		bool m_mergeCase; // if wordforms differing only by case occur merge them
		private List<InputFileInfo> m_inputFiles = new List<InputFileInfo>();
		int m_maxFrequency = Int32.MaxValue; // exclude words occurring more often than this.
		internal string m_excludeWordsSrc;
		string m_notesClass; // element with this class contains footnotes; references should not be output.
		internal string m_excludeClassesSrc;
		internal string m_nonCanonicalClassesSrc;
		// Note: not currently the same as the variable m_tablePaths in ProjectManager; this one is a list of file names,
		// rather than an array of paths. Eventually I plan to retire that variable and use this.
		private List<string> m_tableNames = new List<string>();
		private string m_inputEncoding;

		// Localization
		internal string m_notesRefSrc;
		internal string m_headingRefSrc;
		internal string m_bookChapText; // text for the 'Books and Chapters' hot link, from options file.
		string m_introText; // text for the 'Introduction' hot link, from options file.
		string m_concLinkText; // text for the 'Concordance' hot link, from options file.
		string m_loading;
		internal string m_prevChapText = "Previous Chapter"; // text for "Previous Chapter" link in chapter-per-file
		internal string m_nextChapText = "Next Chapter"; // text for "Next Chapter" link in chapter-per-file
		internal List<BookNameColumnInfo> m_bookNameCclumns = new List<BookNameColumnInfo>();
		internal string m_sortSpec;
		internal CollationMode m_collationMode = CollationMode.kDefault;
		// private string m_supportFilesPath = @"C:\BibleConv\FilesToCopyToOutput"; // Enhance: save to options, edit in dialog.

		#endregion Basic Data

		#region Derived Data
		// Data computable from basic data

		// Keys are book names used in references; values are HTM file names.
		Dictionary<string, string> m_bookNameToFile = new Dictionary<string, string>();
		// Key is file name, value is next file in sequence.
		Dictionary<string, string> m_nextFiles = new Dictionary<string, string>();
		List<string> m_files = new List<string>(); // Files to process in desired order.
		Dictionary<string, string> m_abbreviations = new Dictionary<string, string>(); // Key is HTM file name, value is abbreviation to use in refs.
		Dictionary<string, string> m_introFiles = new Dictionary<string, string>(); // Key is XML file name, value is corresponding intro file.
		Dictionary<string, bool> m_excludeWords = new Dictionary<string, bool>(); // key is words to exclude, ignore value
		Dictionary<string, bool> m_excludeClasses = new Dictionary<string, bool>(); // exclude elements with these classes
		Dictionary<string, bool> m_nonCanonicalClasses = new Dictionary<string, bool>(); // elements with these classes are not canonical
		string m_notesRef; // use this string as the 'reference' for words within notesClass.
		string m_headingRef; // use this string as the 'reference' for other non-Canonical words.
		#endregion Derived Data

		/// <summary>
		/// Directory for supporting files (relative to root work dir
		/// </summary>
        /*
		public string SupportFilesPath
		{
			get { return m_supportFilesPath; }
		}
        */

		public bool ChapterPerFile
		{
			get { return m_chapterPerFile; }
			set { m_chapterPerFile = value; }
		}

		public string NextChapterText
		{
			get { return m_nextChapText;  }
		}

		public string PreviousChapterText
		{
			get { return m_prevChapText; }
		}
		/// <summary>
		/// List of phrases which should become their own concordance entries.
		/// </summary>
		public List<string> Phrases
		{
			get { return m_phrases; }
			set { m_phrases = value;}
		}

		public List<string> PreprocessingTables
		{
			get { return m_tableNames;  }
			internal set { m_tableNames = value; }
		}

		public string NotesClass
		{
			get { return m_notesClass; }
			internal set { m_notesClass = value; }
		}

		public List<InputFileInfo> InputFiles
		{
			get { return m_inputFiles; }
			set
			{
				m_inputFiles = value;
				UpdateDerivedData();
			}
		}

		public string ConcordanceLinkText
		{
			get { return m_concLinkText; }
			internal set { m_concLinkText = value; }
		}

		public List<ExtraFileInfo> ExtraFiles
		{
			get { return m_extraFiles; }
			internal set { m_extraFiles = value; }
		}

		public string LoadingLabelText
		{
			get { return m_loading; }
			internal set { m_loading = value; }
		}

		public string IntroductionLinkText
		{
			get { return m_introText; }
			internal set { m_introText = value; }
		}

		public List<string> MainFiles
		{
			get { return m_files; }
		}

		/// <summary>
		/// if wordforms differing only by case occur merge them
		/// </summary>
		public bool MergeCase
		{
			get { return m_mergeCase; }
			internal set { m_mergeCase = value; }
		}

		/// <summary>
		/// overrides, list of characters that should be considered word-forming in defiance of Unicode.
		/// </summary>
		public string WordformingChars
		{
			get { return m_wordformingChars; }
			internal set { m_wordformingChars = value; }
		}

		public int MaxFrequency
		{
			get { return m_maxFrequency; }
		}

		internal string MaxFreqSrc
		{
			get { return m_maxFrequency == Int32.MaxValue ? "unlimited" : m_maxFrequency.ToString(); }
			set
			{
				if (value == "unlimited")
					m_maxFrequency = Int32.MaxValue;
				if (!int.TryParse(value, out m_maxFrequency))
					m_maxFrequency = Int32.MaxValue; // ignore any value we can't make sense of.
				if (m_maxFrequency <= 0)
					m_maxFrequency = Int32.MaxValue;
			}
		}

		/// <summary>
		/// Minimum characters to display in context area; if a word break cannot be found between this and MaxContextLength,
		/// break a word.
		/// </summary>
		public int MinContextLength
		{
			get { return m_minContextLength; }
			internal set { m_minContextLength = value;}
		}

		/// <summary>
		/// Max characters to include in context. Fewer may be used to avoid breaking words or because of sentence boundaries.
		/// </summary>
		public int MaxContextLength
		{
			get { return m_maxContextLength; }
			internal set { m_maxContextLength = value; }
		}

		internal string PhrasesSrc
		{
			get
			{
				StringBuilder bldr = new StringBuilder();
				foreach (string phrase in Phrases)
					bldr.AppendLine(phrase);
				return bldr.ToString();
			}

			set
			{
				Phrases.Clear();
				foreach (string phrase in value.Split(new string[] {Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries))
					Phrases.Add(phrase);
			}
		}

		/// <summary>
		/// List of words not to concord.
		/// </summary>
		public string ExcludeWordsSrc
		{
			get { return m_excludeWordsSrc; }
			internal set { m_excludeWordsSrc = value; }
		}

		/// <summary>
		/// Dictionary mapping XML file name to HTML file name of introduction for book, if any.
		/// </summary>
		public Dictionary<string, string> IntroFiles
		{
			get { return m_introFiles; }
		}

		public List<BookNameColumnInfo> BookNameColumns
		{
			get { return m_bookNameCclumns;  }
			internal set { m_bookNameCclumns = value; }
		}

		/// <summary>
		/// The name (from the list understood by Encoding.GetEncoding(string)) of the encoding to use for the OW_To_USFM step.
		/// This is really only relevant when the input is SFM NOT OW, since OW uses utf-8.
		/// </summary>
		public string InputEncoding
		{
			get { return m_inputEncoding; }
		}
		public void LoadOptions(string optionsPath)
		{
			m_doc = new XmlDocument();
			m_doc.Load(optionsPath);
			XmlNode root = m_doc.DocumentElement;
			foreach (XmlNode node in root.ChildNodes)
			{
				switch (node.Name)
				{
					case "options":
						m_mergeCase = Utils.AttVal(node, "mergeCase", "false") == "true";
						m_wordformingChars = Utils.AttVal(node, "wordFormingCharacters", "");
						m_maxContextLength = Utils.IntAttVal(node, "maxContext", m_maxContextLength);
						m_minContextLength = Utils.IntAttVal(node, "minContext", m_minContextLength);
						m_chapterPerFile = Utils.AttVal(node, "chapterPerFile", "false") == "true";
						string indexType = Utils.AttVal(node, "indexType", "alphaTree");
						m_inputEncoding = Utils.AttVal(node, "inputEncoding", "utf-8");
						switch (indexType)
						{
							case "alphaTree":
								m_indexType = ConcGenerator.IndexTypes.alphaTree;
								break;
							case "rangeTree":
								m_indexType = ConcGenerator.IndexTypes.rangeTree;
								break;
							case "twoLevelRange":
								m_indexType = ConcGenerator.IndexTypes.twoLevelRange;
								break;
							case "alphaTreeMf":
								m_indexType = ConcGenerator.IndexTypes.alphaTreeMf;
								break;
						}
						break;
					case "files":
						BuildFileList(node);
						break;
					case "copyright":
						m_copyright = node.InnerText;
						break;
					case "introduction":
						m_introText = node.InnerText;
						break;
					case "concordance":
						m_concLinkText = node.InnerText;
						break;
					case "extraFiles":
						BuildExtraFiles(node);
						break;
					case "loading":
						m_loading = node.InnerText;
						break;
					case "phrases":
						foreach (XmlNode child in node)
							if (child.Name == "phrase")
								m_phrases.Add(child.InnerText);
						break;
					case "excludeWords":
						MaxFreqSrc = Utils.AttVal(node, "moreFrequentThan", "unlimited");
						m_excludeWordsSrc = node.InnerText;
						break;
					case "excludeClasses":
						m_excludeClassesSrc = node.InnerText;
						break;
					case "specialClasses":
						m_notesClass = Utils.AttVal(node, "notesClass", "");
						m_notesRefSrc = Utils.AttVal(node, "notesRef", "-----");
						m_headingRefSrc = Utils.AttVal(node, "headingRef", "-----");
						m_nonCanonicalClassesSrc = node.InnerText;
						break;
					case "bookChap":
						m_bookChapText = node.InnerText;
						break;
					case "nextChapter":
						m_nextChapText = node.InnerText;
						break;
					case "prevChapter":
						m_prevChapText = node.InnerText;
						break;
					case "preprocess":
						SetupPreprocessing(node);
						break;
					case "bookNameColumns":
						BuildBookNameCols(node);
						break;
					case "collation":
						GetComparer(node);
						break;
				}
			}
			UpdateDerivedData();
		}
		private void GetComparer(XmlNode node)
		{
			string compareId = Utils.AttVal(node, "comparer", null);
			switch (compareId)
			{
				case null:
					m_collationMode = CollationMode.kDefault;
					return;
				case "CustomSimple":
					m_collationMode = CollationMode.kCustomSimple;
					break;
				case "CustomICU":
					m_collationMode = CollationMode.kCustomICU;
					break;
				default:
					m_collationMode = CollationMode.kLocale;
					m_sortSpec = compareId;
					return;
			}
			m_sortSpec = node.InnerText;
		}

		void SaveComparer()
		{
			XmlNode collNode = NodeNamed("collation");
			collNode.RemoveAll();
			switch (m_collationMode)
			{
				case CollationMode.kDefault:
					return;
				case CollationMode.kLocale:
					SetAttr(collNode, "comparer", m_sortSpec);
					return;
				case CollationMode.kCustomSimple:
					SetAttr(collNode, "comparer", "CustomSimple");
					break;
				case CollationMode.kCustomICU:
					SetAttr(collNode, "comparer", "CustomICU");
					break;
			}
			collNode.InnerText = m_sortSpec;
		}

		private void SetupPreprocessing(XmlNode node)
		{
			m_tableNames = new List<string>(node.ChildNodes.Count);
			for (int i = 0; i < node.ChildNodes.Count; i++)
				m_tableNames.Add(node.ChildNodes[i].InnerText);
		}

		private void BuildBookNameCols(XmlNode node)
		{
			m_bookNameCclumns.Clear();
			foreach (XmlNode item in node.ChildNodes)
			{
				string langName = Utils.AttVal(item, "name", "*");
				string colHeader = item.InnerText;
				m_bookNameCclumns.Add(new BookNameColumnInfo(langName, colHeader));
			}
		}

		private void BuildExtraFiles(XmlNode node)
		{
			m_extraFiles.Clear();
			foreach (XmlNode fileNode in node.ChildNodes)
			{
				if (fileNode.Attributes["name"] == null)
					continue;
				if (fileNode.Attributes["linkText"] == null)
					continue;
				string fileName = fileNode.Attributes["name"].Value;
				string linkText = fileNode.Attributes["linkText"].Value;
				m_extraFiles.Add(new ExtraFileInfo(fileName, linkText));
			}

		}
		public void SaveOptions(string optionsPath)
		{
			if (m_doc == null)
			{
				m_doc = new XmlDocument();
			}
			XmlNode optionsNode = NodeNamed("options");
			SetAttr(optionsNode, "mergeCase", m_mergeCase);
			SetAttr(optionsNode, "wordFormingCharacters", m_wordformingChars);
			SetAttr(optionsNode, "maxContext", m_maxContextLength);
			SetAttr(optionsNode, "minContext", m_minContextLength);
			SetAttr(optionsNode, "chapterPerFile", m_chapterPerFile);
			SetAttr(optionsNode, "indexType", m_indexType.ToString());
			SaveFileList();
			SetInnerText("copyright", m_copyright);
			SetInnerText("introduction", m_introText);
			SetInnerText("concordance", m_concLinkText);
			SetInnerText("loading", m_loading);
			XmlNode excludeWordsNode = SetInnerText("excludeWords", m_excludeWordsSrc);
			SetAttr(excludeWordsNode, "moreFrequentThan", MaxFreqSrc);
			WriteExtraFiles();
			WritePhrases();
			SetInnerText("excludeClasses", m_excludeClassesSrc);

			XmlNode scNode = SetInnerText("specialClasses", m_nonCanonicalClassesSrc);
			SetAttr(scNode, "notesClass", m_notesClass);
			SetAttr(scNode, "notesRef", m_notesRefSrc);
			SetAttr(scNode, "headingRef", m_headingRefSrc);
			SetInnerText("bookChap", m_bookChapText);
			SetInnerText("nextChapter", m_nextChapText);
			SetInnerText("prevChapter", m_prevChapText);
			SavePreprocessing();
			SaveBookNameCols();
			SaveComparer();
			m_doc.Save(optionsPath);
		}

		internal IComparer<string> ComparerFor(CollationMode mode, string data)
		{
			switch(mode)
			{
				case CollationMode.kDefault:
					break;
				case CollationMode.kCustomSimple:
					return new Palaso.WritingSystems.Collation.SimpleRulesCollator(data);
				case CollationMode.kCustomICU:
					return new Palaso.WritingSystems.Collation.IcuRulesCollator(data);
				case CollationMode.kLocale:
					try
					{
						CultureInfo info = new CultureInfo(data);
						return StringComparer.Create(info, true);
					}
					catch (ArgumentException)
					{
						MessageBox.Show("Cannot interpret " + data + " as a collation ID. Using default collation.", "Error", MessageBoxButtons.OK,
							MessageBoxIcon.Warning);
					}
					break;
			}
			return StringComparer.InvariantCultureIgnoreCase;
		}

		private void SaveBookNameCols()
		{
			XmlNode bncNode = NodeNamed("bookNameColumns");
			bncNode.RemoveAll();
			foreach(BookNameColumnInfo info in m_bookNameCclumns)
			{
				XmlNode row = MakeNode(bncNode, "column");
				SetAttr(row, "name", info.LanguageName);
				row.InnerText = info.ColuumnHeaderText;
			}
		}

		private void WriteExtraFiles()
		{
			XmlNode extraNode = NodeNamed("extraFiles");
			extraNode.RemoveAll();
			foreach(ExtraFileInfo efi in m_extraFiles)
			{
				XmlNode node = MakeNode(extraNode, "file");
				SetAttr(node, "name", efi.FileName);
				SetAttr(node, "linkText", efi.HotLinkText);
			}
		}

		private void WritePhrases()
		{
			XmlNode phrases = NodeNamed("phrases");
			// Retain any correct existing phrases; this both saves time and may preserve some comments.
			Dictionary<string, XmlNode> oldPhrases = new Dictionary<string, XmlNode>(phrases.ChildNodes.Count);
			foreach (XmlNode child in phrases)
				oldPhrases[child.InnerText] = child;
			foreach (string phrase in m_phrases)
			{
				XmlNode oldNode;
				if (oldPhrases.TryGetValue(phrase, out oldNode))
					oldPhrases.Remove(phrase); // prevent later deletion.
				else
					MakeNode(phrases, "phrase").InnerText = phrase;
			}
			// Get rid of any we no longer want.
			foreach (XmlNode node in oldPhrases.Values)
				phrases.RemoveChild(node);
		}

		private XmlNode MakeNode(XmlNode parent, string name)
		{
			XmlNode result = m_doc.CreateElement(name);
			parent.AppendChild(result);
			return result;
		}

		private XmlNode SetInnerText(string name, string val)
		{
			XmlNode node = NodeNamed(name);
			node.InnerText = val;
			return node;
		}

		private void SaveFileList()
		{
			XmlNode filesNode = NodeNamed("files");
			filesNode.RemoveAll();
			foreach (InputFileInfo info in m_inputFiles)
			{
				XmlNode fileNode = MakeNode(filesNode, "file");
				SetAttr(fileNode, "name", info.FileName);
				SetAttr(fileNode, "eng", info.StandardAbbr);
				SetAttr(fileNode, "abbr", info.VernAbbr);
				SetAtrrIfNotNull(fileNode, "intro", info.IntroFileName);
				SetAtrrIfNotNull(fileNode, "parallel", info.CrossRefName);
			}
		}

		private void SavePreprocessing()
		{
			XmlNode ppNode = NodeNamed("preprocess");
			ppNode.RemoveAll();
			foreach (string filename in m_tableNames)
			{
				XmlNode fileNode = MakeNode(ppNode, "table");
				fileNode.InnerText = filename;
			}
		}

		private void SetAtrrIfNotNull(XmlNode fileNode, string name, string val)
		{
			if (val != null)
				SetAttr(fileNode, name, val);
		}

		private void SetAttr(XmlNode node, string name, bool val)
		{
			SetAttr(node, name, (val ? "true" : "false"));
		}

		private void SetAttr(XmlNode node, string name, int val)
		{
			SetAttr(node, name, val.ToString());
		}

		private void SetAttr(XmlNode node, string name, string val)
		{
			XmlAttribute attr = node.Attributes[name];
			if (attr == null)
			{
				attr = m_doc.CreateAttribute(name);
				node.Attributes.Append(attr);
			}
			attr.Value = val;
		}

		XmlNode NodeNamed(string name)
		{
			foreach (XmlNode child in m_doc.DocumentElement.ChildNodes)
				if (child.Name == name)
					return child;
			XmlNode result = m_doc.CreateElement(name);
			m_doc.DocumentElement.AppendChild(result);
			return result;
		}

		private void BuildFileList(XmlNode node)
		{
			foreach (XmlNode item in node.ChildNodes)
			{
				string fileName = item.Attributes["name"].Value;
				string stdAbbr = Utils.AttVal(item, "eng", null);
				string parallel = Utils.AttVal(item, "parallel", null);
				string abbr = item.Attributes["abbr"].Value;
				string introFile = Utils.AttVal(item, "intro", null);
				m_inputFiles.Add(new InputFileInfo(fileName, stdAbbr, abbr, parallel, introFile));
			}
		}

		/// <summary>
		/// Main data has changed, make derived data conform.
		/// </summary>
		void UpdateDerivedData()
		{
			m_files.Clear();
			m_abbreviations.Clear();
			m_introFiles.Clear();
			m_nextFiles.Clear();
			m_bookNameToFile.Clear();
			string prevFile = "none";// will become a dummy key
			foreach (InputFileInfo info in m_inputFiles)
			{
				m_files.Add(info.FileName);

				string htmlFileName = Path.ChangeExtension(info.FileName, "htm");
				m_abbreviations[htmlFileName] = info.VernAbbr;
				m_nextFiles[prevFile] = htmlFileName;
				prevFile = htmlFileName;

				if (info.IntroFileName != null)
					m_introFiles[info.FileName] = info.IntroFileName;
				if (info.CrossRefName != null)
					m_bookNameToFile[info.CrossRefName] = htmlFileName;
			}
			m_nextFiles[prevFile] = null; // last file has no next.

			Utils.BuildDictionary(m_excludeWords, m_excludeWordsSrc);
			Utils.BuildDictionary(m_excludeClasses, m_excludeClassesSrc);
			m_headingRef = Utils.MakeSafeXml(m_headingRefSrc + ": ");
			Utils.BuildDictionary(m_nonCanonicalClasses, m_nonCanonicalClassesSrc);
			m_notesRef = Utils.MakeSafeXml(m_notesRefSrc + ": ");

		}
	}

	public class ExtraFileInfo
	{
		public string FileName;
		public string HotLinkText;
		public ExtraFileInfo(string fileName, string linkText)
		{
			FileName = fileName;
			HotLinkText = linkText;
		}
	}

	public class BookNameColumnInfo
	{
		public string LanguageName;
		public string ColuumnHeaderText;
		public BookNameColumnInfo(string langName, string colHeader)
		{
			LanguageName = langName;
			ColuumnHeaderText = colHeader;
		}
	}

	public enum CollationMode
	{
		kDefault,
		kCustomSimple,
		kCustomICU,
		kLocale
	}
}
