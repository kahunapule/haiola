using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using sepp;
using WordSend;

namespace haiola
{
	public class Options
	{
		private XMLini ini;
		private List<string> m_tableNames;
		private List<string> m_postprocesses;
		private List<string> m_altLinks;
		private bool changed = false;

		public void Reload(string iniName)
		{
			if (changed)
				Write();
			m_tableNames = null;
			m_postprocesses = null;
			m_altLinks = null;
			_books = null;
			_referenceAbbeviationsMap = null;
			_crossRefToFilePrefixMap = null;
			LegacyOptions oldOpts = null;
			int i, fileCount;

			if (!File.Exists(iniName))
			{
				// Check for legacy options file.
				string seppOptions = Path.Combine(Path.GetDirectoryName(iniName), "Sepp Options.xml");
				if (File.Exists(seppOptions))
				{
					oldOpts = new LegacyOptions();
					oldOpts.LoadOptions(seppOptions);
				}
			}
			ini = new XMLini(iniName);
			if (oldOpts != null)
			{
				// Read old options that are in current use into new options format.
				ini.WriteString("languageName", oldOpts.m_languageName);
				ini.WriteString("languageId", oldOpts.m_languageId);
				ini.WriteString("chapterLabel", oldOpts.m_chapterLabel);
				ini.WriteString("psalmLabel", oldOpts.m_psalmLabel);
				ini.WriteString("copyrightLink", oldOpts.m_copyrightLink);
				ini.WriteString("homeLink", oldOpts.m_homeLink);
				ini.WriteString("footerHtml", oldOpts.m_footerHtml);
				ini.WriteString("indexHtml", oldOpts.m_indexHtml);
				ini.WriteString("licenseHtml", oldOpts.m_licenseHtml);
				ini.WriteBool("useKhmerDigits", oldOpts.m_useKhmerDigits);
				ini.WriteBool("ignoreExtras", oldOpts.m_ignoreExtras);
				fileCount = oldOpts.PreprocessingTables.Count;
				ini.WriteInt("numProcessingFiles", fileCount);
				for (i = 0; i < fileCount; i++)
				{
					ini.WriteString("processingFile" + i.ToString(), oldOpts.PreprocessingTables[i]);
				}
				ini.Write();
			}
		}

		/// <summary>
		/// Initializes options from the specified options file.
		/// If that file doesn't exist, initialzes data from the Sepp Options.xml file in the same folder,
		/// if it exists.
		/// </summary>
		/// <param name="iniName"></param>
		public Options(string iniName)
		{
			Phrases = new List<string>();
			Reload(iniName);
		}

		public string languageName
		{
			get { return ini.ReadString("languageName", String.Empty); }
			set { ini.WriteString("languageName", value.Trim()); }
		}

		public string languageNameInEnglish
		{
			get { return ini.ReadString("languageNameInEnglish", String.Empty); }
			set { ini.WriteString("languageNameInEnglish", value.Trim()); }
		}

		public string languageId
		{
			get { return ini.ReadString("languageId", String.Empty); }
			set { ini.WriteString("languageId", value.Trim()); }
		}

		public string translationId
		{
			get
			{
				string dialect = ini.ReadString("dialect", String.Empty);
				if (dialect.Length > 0)
					dialect = "-" + dialect;
				return ini.ReadString("translationId", ini.ReadString("languageId", String.Empty) + dialect);
			}
			set { ini.WriteString("translationId", value.Trim()); }
		}


		public string dialect
		{
			get { return ini.ReadString("dialect", String.Empty); }
			set { ini.WriteString("dialect", value.Trim()); }
		}

        public string xoFormat
        {
            get { return ini.ReadString("xoFormat", "%c:%v:"); }
            set { ini.WriteString("xoFormat", value); }
        }

        public string customCssFileName
        {
            get { return ini.ReadString("customCssFileName", "prophero.css"); }
            set { ini.WriteString("customCssFileName", value); }
        }

        public bool stripNoteOrigin
        {
            get { return ini.ReadBool("stripNoteOrigin", true); }
            set { ini.WriteBool("stripNoteOrigin", value); }
        }

        public bool Archived
        {
            get { return ini.ReadBool("Archived", false); }
            set { ini.WriteBool("Archived", value); }
        }

        public bool PrepublicationChecks
        {
            get { return ini.ReadBool("PrepublicationChecks", false); }
            set { ini.WriteBool("PrepublicationChecks", value); }
        }

        public bool WebSiteReady
        {
            get { return ini.ReadBool("WebSiteReady", false); }
            set { ini.WriteBool("WebSiteReady", value); }
        }

        public bool ETENDBL
        {
            get { return ini.ReadBool("ETENDBL", false); }
            set { ini.WriteBool("ETENDBL", value); }
        }

        public string contentCreator
		{
			get { return ini.ReadString("contentCreator", "Wycliffe Bible Translators"); }
			set { ini.WriteString("contentCreator", value.Trim()); }
		}

		public string contributor
		{
			get { return ini.ReadString("contributor", String.Empty); }
			set { ini.WriteString("contributor", value.Trim()); }
		}

		public string vernacularTitle
		{
			get { return ini.ReadString("vernacularTitle", String.Empty); }
			set { ini.WriteString("vernacularTitle", value.Trim()); }
		}

		public string EnglishDescription
		{
			get { return ini.ReadString("EnglishDescription", String.Empty); }
			set { ini.WriteString("EnglishDescription", value.Trim()); }
		}

		public string lwcDescription
		{
			get { return ini.ReadString("lwcDescription", String.Empty); }
			set { ini.WriteString("lwcDescription", value.Trim()); }
		}

		public DateTime contentUpdateDate
		{
			get { return ini.ReadDateTime("contentUpdateDate", DateTime.Today); }
			set { ini.WriteDateTime("contentUpdateDate", value); }
		}

		public bool ignoreExtras
		{
			get { return ini.ReadBool("ignoreExtras", false); }
			set { ini.WriteBool("ignoreExtras", value); }
		}

        public bool relaxUsfmNesting
        {
            get { return ini.ReadBool("relaxUsfmNesting", false); }
            set { ini.WriteBool("relaxUsfmNesting", value); }
        }

 /*
        public bool stripPictures
        {
            get { return ini.ReadBool("stripPictures", true); }
            set { ini.WriteBool("stripPictures", value); }
        }
        */

		public bool publicDomain
		{
			get { return ini.ReadBool("publicDomain", false); }
			set { ini.WriteBool("publicDomain", value); }
		}


		public bool creativeCommons
		{
			get { return ini.ReadBool("creativeCommons", true); }
			set { ini.WriteBool("creativeCommons", value); }
		}

		public bool otherLicense
		{
			get { return ini.ReadBool("otherLicense", false); }
			set { ini.WriteBool("otherLicense", value); }
		}

		public bool allRightsReserved
		{
			get { return ini.ReadBool("allRightsReserved", false); }
			set { ini.WriteBool("allRightsReserved", value); }
		}


		public bool silentCopyright
		{
			get { return ini.ReadBool("silentCopyright", false); }
			set { ini.WriteBool("silentCopyright", value); }
		}

		public bool preprocess
		{
			get { return ini.ReadBool("preprocess", true); }
			set { ini.WriteBool("preprocess", value); }
		}

		public bool doUsfmToUsfx
		{
			get { return ini.ReadBool("doUsfmToUsfx", true); }
			set { ini.WriteBool("doUsfmToUsfx", value); }
		}

		public bool doPortableHtml
		{
			get { return ini.ReadBool("doPortableHtml", true); }
			set { ini.WriteBool("doPortableHtml", value); }
		}

		public bool doDailyMail
		{
			get { return ini.ReadBool("doDailyMail", true); }
			set { ini.WriteBool("doDailyMail", value); }
		}

		public bool doSearchDb
		{
			get { return ini.ReadBool("doSearchDb", true); }
			set { ini.WriteBool("doSearchDb", value); }
		}

		public bool doXetex
		{
			get { return ini.ReadBool("doXetex", true); }
			set { ini.WriteBool("doXetex", value); }
		}

		public bool doOdf
		{
			get { return ini.ReadBool("doOdf", true); }
			set { ini.WriteBool("doOdf", value); }
		}

		public string copyrightOwner
		{
			get { return ini.ReadString("copyrightOwner", String.Empty); }
			set { ini.WriteString("copyrightOwner", value.Trim()); }
		}

        public string copyrightOwnerAbbrev
        {
            get { return ini.ReadString("copyrightOwnerAbbrev", String.Empty); }
            set { ini.WriteString("copyrightOwnerAbbrev", value.Trim()); }
        }

        public string copyrightOwnerUrl
        {
            get { return ini.ReadString("copyrightOwnerUrl", String.Empty); }
            set { ini.WriteString("copyrightOwnerUrl", value.Trim()); }
        }

        public string copyrightYears
		{
			get { return ini.ReadString("copyrightYears", String.Empty); }
			set { ini.WriteString("copyrightYears", value.Trim()); }
		}

		public string rightsStatement
		{
			get { return ini.ReadString("rightsStatement", String.Empty); }
			set { ini.WriteString("rightsStatement", value.Trim()); }
		}

		public string printPublisher
		{
			get { return ini.ReadString("printPublisher", String.Empty); }
			set { ini.WriteString("printPublisher", value.Trim()); }
		}

		public string electronicPublisher
		{
			get { return ini.ReadString("electronicPublisher", "PNG Bible Translation Association"); }
			set { ini.WriteString("electronicPublisher", value.Trim()); }
		}

		public string homeLink
		{
			get { return ini.ReadString("homeLink", String.Empty); }
			set { ini.WriteString("homeLink", value.Trim()); }
		}

		public string copyrightLink
		{
			get { return ini.ReadString("copyrightLink", "<a href=\"copyright.htm\">©</a>"); }
			set { ini.WriteString("copyrightLink", value.Trim()); }
		}

		public string goText
		{
			get { return ini.ReadString("goText", "Go!"); }
            set { ini.WriteString("goText", value.Trim()); }
		}

		public string footerHtml
		{
			get { return ini.ReadString("footerHtml", String.Empty); }
            set { ini.WriteString("footerHtml", value.Trim()); }
		}

		public string indexHtml
		{
			get { return ini.ReadString("indexHtml", String.Empty); }
            set { ini.WriteString("indexHtml", value.Trim()); }
		}

		public string licenseHtml
		{
			get { return ini.ReadString("licenseHtml", String.Empty); }
            set { ini.WriteString("licenseHtml", value.Trim()); }
		}

		public string versificationScheme
		{
			get { return ini.ReadString("versificationScheme", "Automatic"); }
            set { ini.WriteString("versificationScheme", value.Trim()); }
		}

		public string psalmLabel
		{
			get { return ini.ReadString("psalmLabel", String.Empty); }
            set { ini.WriteString("psalmLabel", value.Trim()); }
		}

		public string chapterLabel
		{
			get { return ini.ReadString("chapterLabel", String.Empty); }
			set { ini.WriteString("chapterLabel", value); }
		}


		public string chapterAndVerseSeparator
		{
			get { return ini.ReadString("chapterAndVerseSeparator", ":"); }
			set { ini.WriteString("chapterAndVerseSeparator", value); }
		}

		public string rangeSeparator
		{
			get { return ini.ReadString("rangeSeparator", "-"); }
			set { ini.WriteString("rangeSeparator", value); }
		}

		public string multiRefSameChapterSeparator
		{
			get { return ini.ReadString("multiRefSameChapterSeparator", ","); }
			set { ini.WriteString("multiRefSameChapterSeparator", value); }
		}

		public string multiRefDifferentChapterSeparator
		{
			get { return ini.ReadString("multiRefDifferentChapterSeparator", ";"); }
			set { ini.WriteString("multiRefDifferentChapterSeparator", value); }
		}

		public string verseNumberLocation
		{
			get { return ini.ReadString("verseNumberLocation", "Begin"); }
			set { ini.WriteString("verseNumberLocation", value); }
		}

		public string footnoteMarkerStyle
		{
			get { return ini.ReadString("footnoteMarkerStyle", "Circulate"); }
			set { ini.WriteString("footnoteMarkerStyle", value); }
		}

		public string footnoteMarkerResetAt
		{
			get { return ini.ReadString("footnoteMarkerResetAt", "Book"); }
			set { ini.WriteString("footnoteMarkerResetAt", value); }
		}

		public string footnoteMarkers
		{
			get { return ini.ReadString("footnoteMarkers", "* † ‡ §"); }
			set { ini.WriteString("footnoteMarkers", value); }
		}

		public string textDir
		{
			get { return ini.ReadString("textDir", "ltr"); }
			set { ini.WriteString("textDir", value); }
		}

		public string osis2SwordOptions
		{
			get { return ini.ReadString("osis2SwordOptions", String.Empty); }
			set { ini.WriteString("osis2SwordOptions", value); }
		}

		public bool privateProject
		{
			get { return ini.ReadBool("privateProject", false); }
			set { ini.WriteBool("privateProject", value); }
		}

		public string otmlRenderChapterNumber
		{
			get { return ini.ReadString("otmlRenderChapterNumber", "No"); }
			set { ini.WriteString("otmlRenderChapterNumber", value); }
		}

		public string homeDomain
		{
			get { return ini.ReadString("homeDomain", String.Empty); }
			set { ini.WriteString("homeDomain", value); }
		}

		public bool useKhmerDigits
		{
			get { return ini.ReadBool("useKhmerDigits", false); }
			set { ini.WriteBool("useKhmerDigits", value); }
		}

		public bool useArabicDigits
		{
			get { return ini.ReadBool("useArabicDigits", !ini.ReadBool("useKhmerDigits", false)); }
			set { ini.WriteBool("useArabicDigits", value); }
		}

        public bool lastRunResult
        {
            get { return ini.ReadBool("lastRunResult", false); }
            set { ini.WriteBool("lastRunResult", value); }
        }


        public string numberSystem
        {
            get { return ini.ReadString("numberSystem", ini.ReadBool("useKhmerDigits", false) ? "Khmer" : "Default"); }
            set { ini.WriteString("numberSystem", value); }
        }

		public List<string> preprocessingTables
		{
			get
			{
				int i, count;
				if (m_tableNames == null)
				{
					m_tableNames = new List<string>();
					count = ini.ReadInt("numProcessingFiles", 1);
					for (i = 0; i < count; i++)
					{
						m_tableNames.Add(ini.ReadString("processingFile" + i.ToString(), "fixquotes.re"));
					}
				}
				return m_tableNames;
			}
			set
			{
				m_tableNames = value;
				int i, count;
				count = value.Count;
				ini.WriteInt("numProcessingFiles", count);
				for (i = 0; i < count; i++)
				{
					ini.WriteString("processingFile" + i.ToString(), value[i]);
				}
			}
		}

		public List<string> postprocesses
		{
			get
			{
				int i, count;
				if (m_postprocesses == null)
				{
					m_postprocesses = new List<string>();
					count = ini.ReadInt("numPostprocesses", 0);
					for (i = 0; i < count; i++)
					{
						m_postprocesses.Add(ini.ReadString("postprocess" + i.ToString(), ""));
					}
				}
				return m_postprocesses;
			}
			set
			{
				m_postprocesses = value;
				int i, theCount;
				theCount = value.Count;
				ini.WriteInt("numPostprocesses", theCount);
				for (i = 0; i < theCount; i++)
				{
					ini.WriteString("postprocess" + i.ToString(), value[i]);
				}
			}
		}

		public List<string> altLinks
		{
			get
			{
				int i, count;
				if (m_altLinks == null)
				{
					m_altLinks = new List<string>();
					count = ini.ReadInt("numAltLinks", 0);
					for (i = 0; i < count; i++)
					{
						m_altLinks.Add(ini.ReadString("altLink" + i.ToString(), String.Empty));
					}
				}
				return m_altLinks;
			}
			set
			{
				m_altLinks = value;
				int i, theCount;
				theCount = value.Count;
				ini.WriteInt("numAltLinks", theCount);
				for (i = 0; i < theCount; i++)
				{
					ini.WriteString("altLink" + i.ToString(), value[i]);
				}
			}
		}

		/// <summary>
		/// Writes options to disk.
		/// </summary>
		/// <returns>true iff the write was successful</returns>
		public bool Write()
		{
			return ini.Write();
		}

		#region ConcOptions

		/// <summary>
		/// Whether to generate a concordance at all; other conc options are irrelevant if not.
		/// </summary>
		public bool GenerateConcordance
		{
			get { return ini.ReadBool("generateConcordance", false); }
			set { ini.WriteBool("generateConcordance", value); }
		}

		/// <summary>
		/// if wordforms differing only by case occur merge them
		/// </summary>
		public bool MergeCase
		{
			get { return ini.ReadBool("mergeCase", false); }
			set { ini.WriteBool("mergeCase", value); }
		}

		/// <summary>
		/// overrides, list of characters that should be considered word-forming in defiance of Unicode.
		/// </summary>
		public string WordformingChars
		{
			get { return ini.ReadString("wordformingChars", String.Empty); }
			set { ini.WriteString("wordformingChars", value); }
		}

		/// <summary>
		/// the maximum frequency of occurrence that a word may have and still be include in the concordance.
		/// </summary>
		public int MaxFrequency
		{
			get { return ini.ReadInt("maxFrequency", int.MaxValue); }
			set { ini.WriteInt("maxFrequency", value); }
		}

		/// <summary>
		/// The text property saved in files and displayed in a text box that controls the maximum
		/// frequency of occurrence that a word may have and still be include in the concordance.
		/// </summary>
		internal string MaxFreqSrc
		{
			get { return MaxFrequency == Int32.MaxValue ? "unlimited" : MaxFrequency.ToString(); }
			set
			{
				if (value.ToLowerInvariant() == "unlimited") // todo?? Localize
					MaxFrequency = Int32.MaxValue;
				int temp;
				if (int.TryParse(value, out temp))
					MaxFrequency = temp;
				else
					MaxFrequency = Int32.MaxValue; // ignore any value we can't make sense of.
				if (MaxFrequency <= 0)
					MaxFrequency = Int32.MaxValue;
			}
		}

		/// <summary>
		/// Minimum characters to display in context area; if a word break cannot be found between this and MaxContextLength,
		/// break a word.
		/// </summary>
		public int MinContextLength
		{
			get { return ini.ReadInt("minContextLength", 35); }
			set { ini.WriteInt("minContextLength", value); }
		}

		/// <summary>
		/// Max characters to include in context. Fewer may be used to avoid breaking words or because of sentence boundaries.
		/// </summary>
		public int MaxContextLength
		{
			get { return ini.ReadInt("maxContextLength", 35); }
			set { ini.WriteInt("maxContextLength", value); }
		}

		/// <summary>
		/// String stored in files and the options dialog that lists phrases that should have distinct concordance entries.
		/// </summary>
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
		/// List of phrases which should become their own concordance entries.
		/// Todo JohnT: persist in ini
		/// </summary>
		public List<string> Phrases { get; internal set; }

		/// <summary>
		/// List of words not to concord.
		/// </summary>
		public string ExcludeWords
		{
			get { return ini.ReadString("excludeWords", String.Empty); }
			set { ini.WriteString("excludeWords", value); }
		}

		#endregion

		private Dictionary<string, string> _crossRefToFilePrefixMap;

		/// <summary>
		/// Maps from names used in cross references to the file prefix used for books in that file.
		/// This map is stored in the ini file as a single string, pairs cross ref name>file prefix separated by &.
		/// This works because the names are html-escaped and cannot contain & or > literally.
		/// </summary>
		public Dictionary<string, string> CrossRefToFilePrefixMap
		{
			get
			{
				if (_crossRefToFilePrefixMap == null)
				{
					_crossRefToFilePrefixMap = new Dictionary<string, string>();
					foreach (var pair in ini.ReadString("crossRefs", string.Empty).Split('&'))
					{
						var items = pair.Split('>');
						if (items.Length != 2)
							continue;
						_crossRefToFilePrefixMap[items[0]] = items[1];
					}
				}
				return _crossRefToFilePrefixMap;
			}
			set
			{
				_crossRefToFilePrefixMap = value;
				var sb = new StringBuilder();
				foreach (var kvp in _crossRefToFilePrefixMap)
				{
					sb.Append(kvp.Key);
					sb.Append('>');
					sb.Append(kvp.Value);
					sb.Append('&');
				}
				ini.WriteString("crossRefs", sb.ToString());
			}
		}

		private Dictionary<string, string> _referenceAbbeviationsMap;

		/// <summary>
		/// Maps from standard book ID to the vernacular abbreviation used for that book in the concordance.
		/// This map is stored in the ini file as a single string, pairs id>abbr separated by &.
		/// This works because the names are html-escaped and cannot contain & or > literally.
		/// </summary>
		public Dictionary<string, string> ReferenceAbbeviationsMap
		{
			get
			{
				if (_referenceAbbeviationsMap == null)
				{
					_referenceAbbeviationsMap = new Dictionary<string, string>();
					foreach (var pair in ini.ReadString("refAbbrs", string.Empty).Split('&'))
					{
						var items = pair.Split('>');
						if (items.Length != 2)
							continue;
						_referenceAbbeviationsMap[items[0]] = items[1];
					}
				}
				return _referenceAbbeviationsMap;
			}
			set
			{
				_referenceAbbeviationsMap = value;
				var sb = new StringBuilder();
				foreach (var kvp in _referenceAbbeviationsMap)
				{
					sb.Append(kvp.Key);
					sb.Append('>');
					sb.Append(kvp.Value);
					sb.Append('&');
				}
				ini.WriteString("refAbbrs", sb.ToString());
			}
		}

		private List<string> _books;

		/// <summary>
		/// The (ids of) the books in the project, in the order they appear in the USFX file.
		/// I think something causes this to be canonical.
		/// Stored as a string in the ini, items separated by &. This works because the standard IDs do not contain this symbol
		/// </summary>
		public List<string> Books
		{
			get
			{
				if (_books == null)
				{
					_books = new List<string>();
					foreach (var item in ini.ReadString("books", string.Empty).Split('&'))
					{
						if (item.Length > 0)
							_books.Add(item);
					}
				}
				return _books;
			}
			set
			{
				_books = value;
				var sb = new StringBuilder();
				foreach (var item in _books)
				{
					sb.Append(item);
					sb.Append('&');
				}
				ini.WriteString("books", sb.ToString());
			}
		}

		#region Frames Options

		/// <summary>
		/// True if we should generate the frame-based view, with the Scripture embedded in one pane of a frame
		/// along with some navigation tools.
		/// </summary>
		public bool UseFrames
		{
			get { return ini.ReadBool("useFrames", false); }
			set { ini.WriteBool("useFrames", value); }
		}

		/// <summary>
		/// (Localized) text to use in the link at the top of the ChapterIndex file that brings up the concordance (if generated).
		/// </summary>
		public string ConcordanceLinkText
		{
			get { return ini.ReadString("concLink", "Concordance"); }
			set { ini.WriteString("concLink", value); }
		}

		/// <summary>
		/// (Localized) text to use in the link at the top of the left concordance pane which switches us back to the main index.
		/// </summary>
		public string BooksAndChaptersLinkText
		{
			get { return ini.ReadString("bookChapLink", "Books and Chapters"); }
			set { ini.WriteString("bookChapLink", value); }
		}

		/// <summary>
		/// (Localized) text to use in the link to use in the ChapterIndex file to any introduction file for a book.
		/// </summary>
		public string IntroductionLinkText
		{
			get { return ini.ReadString("introLink", "Introduction"); }
			set { ini.WriteString("introLink", value); }
		}

		/// <summary>
		/// (Localized) text to use in the link at the top of each chapter to the previous chapter
		/// </summary>
		public string PreviousChapterLinkText
		{
			get { return ini.ReadString("prevChapLink", "Previous Chapter"); }
			set { ini.WriteString("prevChapLink", value); }
		}

		/// <summary>
		/// (Localized) text to use in the link at the top of each chapter to the next chapter
		/// </summary>
		public string NextChapterLinkText
		{
			get { return ini.ReadString("nextChapLink", "Next Chapter"); }
			set { ini.WriteString("nextChapLink", value); }
		}

		/// <summary>
		/// (Localized) text to use in the button at the top of each chapter to to hide navigation panes
		/// </summary>
		public string HideNavigationButtonText
		{
			get { return ini.ReadString("hideNav", "Hide Navigation Panes"); }
			set { ini.WriteString("hideNav", value); }
		}

		/// <summary>
		/// (Localized) text to use in the button at the top of each chapter to to show navigation panes.
		/// This appears when JavaScript is disabled, or after hiding the navigation panes, or when a search engine has brought us direct to a child page.
		/// </summary>
		public string ShowNavigationButtonText
		{
			get { return ini.ReadString("showNav", "Show Navigation Panes"); }
			set { ini.WriteString("showNav", value); }
		}
		#endregion
	}

	public class LegacyOptions
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
		List<string> m_phrases = new List<string>();
		bool m_mergeCase; // if wordforms differing only by case occur merge them
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
        public string m_languageId = string.Empty;
        public string m_languageName = string.Empty;
        public string m_chapterLabel = string.Empty;
        public string m_psalmLabel = string.Empty;
        public string m_copyrightLink = "<a href=\"copyright.htm\">©</a>";
        public string m_homeLink = "<a href=\"../index.htm\"><img alt=\"^\" src=\"../css/home_sm.png\" border=\"0\" /></a>";
        public string m_footerHtml = string.Empty;
        public string m_indexHtml = "<div class=\"toc1\"><a href=\"http://pngscriptures.org/{0}/{0}_html.zip\">{0}_html.zip</a></div>\r\n"+
"<div class=\"toc1\"><a href=\"http://pngscriptures.org/resources.php?id={0}\">More downloads</a></div>\r\n"+
"<div class=\"toc1\"><a href=\"http://pnglanguages.org/pacific/png/show_lang_entry.asp?id={0}\">Linguistic publications</a></div>\r\n"+
"<div class=\"toc1\"><a href=\"http://www.ethnologue.org/show_language.asp?code={0}\">Ethnologue</a></div>\r\n";
        public string m_licenseHtml = "<h1>The New Testament in the ____ Language</h1>\r\n"+
"<p>Copyright © ____ <a href=\"http://www.wycliffe.org/\">Wycliffe, Inc.</a></p>\r\n"+
"<p>This translation is made available to you under the terms of the <a href=\"http://creativecommons.org/licenses/by-nc-nd/3.0/\">Creative\r\n"+
"Commons Attribution-Noncommercial-No Derivative Works license.</a> \r\n"+
"In addition, you have permission to port the text to different file\r\n"+
"formats, as long as you don't change any of the text or punctuation of\r\n"+
"the Bible.</p>\r\n"+
"<p>You may share, copy, distribute, transmit, and extract portions or\r\n"+
"quotations from this work, provided that:<br />\r\n"+
"</p>\r\n"+
"<ul>\r\n"+
"<li>You include the above copyright information and that you make it\r\n"+
"clear that the work came from <a href=\"http://pngscriptures.org/\">http://pngscriptures.org/</a>.</li>\r\n"+
"<li>You do not sell this work for a profit.F< />\r\n"+
"</li>\r\n"+
"<li>You do not make any derivative works that change any of the\r\n"+
"actual words or punctuation of the Scriptures.</li>\r\n"+
"</ul>\r\n"+
"<p>Permissions beyond the scope of this license may be available if you\r\n"+
"<a href=\"http://pngscriptures.org/contact.htm\">contact us</a>\r\n"+
"with your request. If you want to revise a\r\n"+
"translation, use a translation in an adaptation, or use a translation\r\n"+
"commercially, we will relay your request to the appropriate copyright\r\n"+
"owner.</p>\r\n"+
"<p><a rel=\"license\"\r\n"+
"href=\"http://creativecommons.org/licenses/by-nc-nd/3.0/\"><img\r\n"+
"alt=\"Creative Commons License\" style=\"border-width: 0pt;\"\r\n"+
"src=\"http://i.creativecommons.org/l/by-nc-nd/3.0/88x31.png\" /></a></p>\r\n"+
"<p>If you have further questions about this web site or the <a href=\"../terms.htm\">Terms of\r\n"+
"Use</a>, <a href=\"http://pngscriptures.org/contact.htm\">please contact us</a>.\r\n"+
"</p>";
        public bool m_useKhmerDigits = false;
        public bool m_ignoreExtras = false;
        

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
            m_languageId = "";
            m_languageName = "";
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
						break;
                        /*****
					case "files":
						BuildFileList(node);
						break;
                         * ******/
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
                    case "languageId":
                        m_languageId = node.InnerText;
                        break;
                    case "languageName":
                        m_languageName = node.InnerText;
                        break;
                    case "copyrightLink":
                        m_copyrightLink = UnescapeHtml(node.InnerText);
                        break;
                    case "homeLink":
                        m_homeLink = UnescapeHtml(node.InnerText);
                        break;
                    case "footerHtml":
                        m_footerHtml = UnescapeHtml(node.InnerText);
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
                    case "chapterLabel":
                        m_chapterLabel = node.InnerText;
                        break;
                    case "psalmLabel":
                        m_psalmLabel = node.InnerText;
                        break;
                    case "indexHtml":
                        m_indexHtml = node.InnerText;
                        break;
                    case "licenseHtml":
                        m_licenseHtml = node.InnerText;
                        break;
                    case "useKhmerDigits":
                        m_useKhmerDigits = node.InnerText == "yes";
                        break;
                    case "ignoreExtras":
                        m_ignoreExtras = node.InnerText == "yes";
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

        public string EscapeHtml(string s)
        {
            string result = s.Replace("<", "~~lt");
            result = result.Replace(">", "~~gt");
            return result;
        }

        public string UnescapeHtml(string s)
        {
            string result = s.Replace("~~gt", ">");
            result = result.Replace("~~lt", "<");
            return result;
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
			/* SaveFileList(); */
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
            SetInnerText("languageId", m_languageId);
            SetInnerText("languageName", m_languageName);
            SetInnerText("copyrightLink", EscapeHtml(m_copyrightLink));
            SetInnerText("homeLink", EscapeHtml(m_homeLink));
            SetInnerText("footerHtml", EscapeHtml(m_footerHtml));
            SetInnerText("chapterLabel", m_chapterLabel);
            SetInnerText("psalmLabel", m_psalmLabel);
            SetInnerText("indexHtml", m_indexHtml);
            SetInnerText("licenseHtml", m_licenseHtml);
            SetInnerText("useKhmerDigits", m_useKhmerDigits ? "yes" : "no");
            SetInnerText("ignoreExtras", m_ignoreExtras ? "yes" : "no");
			SavePreprocessing();
			SaveBookNameCols();
			SaveComparer();
			m_doc.Save(optionsPath);
		}

        /***********
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
         * **************/

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

        /***********
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
         * ***********/

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

        /*************
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
         * *************/

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
            /************
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
             * ********/
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
