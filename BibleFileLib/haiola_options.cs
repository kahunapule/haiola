using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Xml;

namespace WordSend
{
	public class Options
	{
		public XMLini ini;
		private List<string> m_tableNames;
		private List<string> m_postprocesses;
		private List<string> m_altLinks;
		private bool changed = false;

		public void Reload(string iniName)
		{
            try
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
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error loading ini file " + iniName);
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

        public string allowedBookList;

        public string AudioCopyrightNotice
        {
            get { return ini.ReadString("AudioCopyrightNotice", String.Empty); }
            set { ini.WriteString("AudioCopyrightNotice", value); }
        }

        public string fcbhId
        {
            get { return ini.ReadString("fcbhId", translationId.ToUpperInvariant()).Replace(".",""); }
            set { ini.WriteString("fcbhId", value); }
        }

        public string shortTitle
        {
            get { return ini.ReadString("shortTitle", String.Empty); }
            set { ini.WriteString("shortTitle", value); }
        }

        public string epubId
        {
            get { return ini.ReadString("epubId", String.Empty); }
            set { ini.WriteString("epubId", value); }
        }

        public bool redistributable
        {
            get { return ((ini.ReadBool("redistributable", false) || publicDomain || ccbyndnc || ccbysa || ccbynd) && !privateProject); }
            set { ini.WriteBool("redistributable", (!privateProject) && (value || publicDomain || ccbyndnc || ccbysa || ccbynd)); }
        }

        public bool ccbysa
        {
            get { return (ini.ReadBool("ccbysa", false)); }
            set { ini.WriteBool("ccbysa", value); }
        }

        public bool rechecked
        {
            get { return ini.ReadBool("rechecked", false); }
            set { ini.WriteBool("rechecked", value); }
        }

        public bool ccbynd
        {
            get { return (ini.ReadBool("ccbynd", !(ccbysa || publicDomain || ccbyndnc || allRightsReserved || silentCopyright || otherLicense))); }
            set { ini.WriteBool("ccbynd", value); }
        }

        public bool anonymous
        {
            get { return (ini.ReadBool("anonymous", false)); }
            set { ini.WriteBool("anonymous", value); }
        }

        public bool done
        {
            get { return (ini.ReadBool("done", false)); }
            set { ini.WriteBool("done", value); }
        }

        public bool makeHtml
        {
            get { return (ini.ReadBool("makeHtml", true)); }
            set { ini.WriteBool("makeHtml", value); }
        }

        public bool makeSword
        {
            get { return (ini.ReadBool("makeSword", true)); }
            set { ini.WriteBool("makeSword", value); }
        }

        public bool makeWordML
        {
            get { return (ini.ReadBool("makeWordML", true)); }
            set { ini.WriteBool("makeWordML", value); }
        }

        public int longestWordLength
        {
            get { return (ini.ReadInt("longestWordLength", 0)); }
            set { ini.WriteInt("longestWordLength", value); }
        }

        public string isbn13
        {
            get { return ini.ReadString("isbn13", String.Empty); }
            set { ini.WriteString("isbn13", value); }
        }

        /*
        public string isbn10
        {
            get { return ini.ReadString("isbn10", String.Empty); }
            set { ini.WriteString("isbn10", value); }
        }
        */

        public bool makeEub
        {
            get { return (ini.ReadBool("makeEpub", true)); }
            set { ini.WriteBool("makeEpub", value); }
        }

        public bool dbsCover
        {
            get { return (ini.ReadBool("dbsCover", false)); }
            set { ini.WriteBool("dbsCOver", value); }
        }

        public int printPdfPageCount
        {
            get { return (ini.ReadInt("printPdfPageCount", 0)); }
            set { ini.WriteInt("printPdfPageCount", value); }
        }

        public bool makePDF
        {
            get { return (ini.ReadBool("makePDF", true)); }
            set { ini.WriteBool("makePDF", value); }
        }

        public bool makeInScript
        {
            get { return (ini.ReadBool("makeInScript", true)); }
            set { ini.WriteBool("makeInScript", value); }
        }

        public bool makeHotLinks
        {
            get { return (ini.ReadBool("makeHotLinks", true)); }
            set { ini.WriteBool("makeHotLinks", value); }
        }

        public bool customPermissions
        {
            get { return ini.ReadBool("customPermissions", false); }
            set { ini.WriteBool("customPermissions", value); }
        }

        public bool dbshelp
        {
            get { return ini.ReadBool("dbshelp", false); }
            set { ini.WriteBool("dbshelp", value); }
        }

        public bool commonChars
        {
            get { return ini.ReadBool("commonChars", true); }
            set { ini.WriteBool("commonChars", value); }
        }

        public string commentText
        {
            get { return ini.ReadString("commentText", "comment"); }
            set { ini.WriteString("commentText", value); }
        }

        public string canonTypeEnglish
        {
            get { return ini.ReadString("canonTypeEnglish", "common"); }
            set { ini.WriteString("canonTypeEnglish", value); }
        }

        public string canonTypeLocal
        {
            get { return ini.ReadString("canonTypeLocal", String.Empty); }
            set { ini.WriteString("canonTypeLocal", value); }
        }


        public bool includeApocrypha
        {
            get { return ini.ReadBool("includeApocrypha", true); }
            set { ini.WriteBool("includeApocrypha", value); }
        }

        public bool extendUsfm
        {
            get { return ini.ReadBool("extendUsfm", false); }
            set { ini.WriteBool("extendUsfm", value); }
        }

        public string fcbhDramaNT
        {
            get { return ini.ReadString("fcbhDramaNT", String.Empty); }
            set { ini.WriteString("fcbhDramaNT", value); }
        }

        public string fcbhAudioNT
        {
            get { return ini.ReadString("fcbhAudioNT", String.Empty); }
            set { ini.WriteString("fcbhAudioNT", value); }
        }

        public string fcbhDramaOT
        {
            get { return ini.ReadString("fcbhDramaOT", String.Empty); }
            set { ini.WriteString("fcbhDramaOT", value); }
        }

        public string fcbhAudioOT
        {
            get { return ini.ReadString("fcbhAudioOT", String.Empty); }
            set { ini.WriteString("fcbhAudioOT", value); }
        }

        public string fcbhAudioPortion
        {
            get { return ini.ReadString("fcbhAudioPortion", String.Empty); }
            set { ini.WriteString("fcbhAudioPortion", value); }
        }

        public int otBookCount
        {
            get { return ini.ReadInt("otBookCount", 0); }
            set { ini.WriteInt("otBookCount", value); }
        }

        public int ntBookCount
        {
            get { return ini.ReadInt("ntBookCount", 0); }
            set { ini.WriteInt("ntBookCount", value); }
        }

        public bool eBibleCertified
        {
            get { return ini.ReadBool("eBibleCertified", false); }
            set { ini.WriteBool("eBibleCertified", value); }
        }

        public bool DBSandeBible
        {
            get { return (!privateProject) && ini.ReadBool("DBSandeBible", (!privateProject) && File.Exists(@"/home/kahunapule/sync/doc/Electronic Scripture Publishing/eBible.org_certified.jpg")); }
            set { ini.WriteBool("DBSandeBible", value); }
        }
            
        public int adBookCount
        {
            get { return ini.ReadInt("adBookCount", 0); }
            set { ini.WriteInt("adBookCount", value); }
        }

        public int pBookCount
        {
            get { return ini.ReadInt("pBookCount", 0); }
            set { ini.WriteInt("pBookCount", value); }
        }

        public int otChapCount
        {
            get { return ini.ReadInt("otChapCount", 0); }
            set { ini.WriteInt("otChapCount", value); }
        }

        public int ntChapCount
        {
            get { return ini.ReadInt("ntChapCount", 0); }
            set { ini.WriteInt("ntChapCount", value); }
        }

        public int adChapCount
        {
            get { return ini.ReadInt("adChapCount", 0); }
            set { ini.WriteInt("adChapCount", value); }
        }

        public int otVerseCount
        {
            get { return ini.ReadInt("otVerseCount", 0); }
            set { ini.WriteInt("otVerseCount", value); }
        }

        public int ntVerseCount
        {
            get { return ini.ReadInt("ntVerseCount", 0); }
            set { ini.WriteInt("ntVerseCount", value); }
        }

        public int adVerseCount
        {
            get { return ini.ReadInt("adVerseCount", 0); }
            set { ini.WriteInt("adVerseCount", value); }
        }

        public int otVerseMax
        {
            get { return ini.ReadInt("otVerseMax", 0); }
            set { ini.WriteInt("otVerseMax", value); }
        }

        public int ntVerseMax
        {
            get { return ini.ReadInt("ntVerseMax", 0); }
            set { ini.WriteInt("ntVerseMax", value); }
        }

        public int adVerseMax
        {
            get { return ini.ReadInt("adVerseMax", 0); }
            set { ini.WriteInt("adVerseMax", value); }
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

        public string JesusFilmLinkText
        {
            get { return ini.ReadString("JesusFilmLinkText", "<img src='../JesusFilmtn.jpg' />"); }
            set { ini.WriteString("JesusFilmLinkText", value.Trim()); }
        }

        public string JesusFilmLinkTarget
        {
            get { return ini.ReadString("JesusFilmLinkTarget", String.Empty); }
            set { ini.WriteString("JesusFilmLinkTarget", value.Trim()); }
        }

        public string SwordName
        {
            get
            {
                return ini.ReadString("SwordName", translationId);
            }
            set { ini.WriteString("SwordName", value.Trim()); }
        }

        public string ObsoleteSwordName
        {
            get { return ini.ReadString("ObsoleteSwordName", String.Empty); }
            set { ini.WriteString("ObsoleteSwordName", value.Trim()); }
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

        public bool rebuild
        {
            get { return ini.ReadBool("rebuild", false); }
            set { ini.WriteBool("rebuild", value); }
        }

        public bool runXetex
        {
            get { return ini.ReadBool("runXini", false); }
            set { ini.WriteBool("runXini", value); }
        }

        public string fontFamily
        {
            get { return ini.ReadString("fontFamily", "Gentium"); }
            set { ini.WriteString("fontFamily", value.Trim()); }
        }

        public bool subsetProject
        {
            get { return ini.ReadBool("subsetProject", false); }
            set { ini.WriteBool("subsetProject", value); }
        }

        public string pageWidth
        {
            get { return ini.ReadString("pageWidth", "150 mm"); }
            set { ini.WriteString("pageWidth", value); }
        }

        public string pageLength
        {
            get { return ini.ReadString("pageLength", "216 mm"); }
            set { ini.WriteString("pageLength", value); }
        }

        public bool includeCropMarks
        {
            get { return ini.ReadBool("includeCropMarks", false); }
            set { ini.WriteBool("includeCropMarks", value); }
        }

        public bool verse1
        {
            get { return ini.ReadBool("verse1", true); }
            set { ini.WriteBool("verse1", value); }
        }

        public bool chapter1
        {
            get { return ini.ReadBool("chapter1", true); }
            set { ini.WriteBool("chapter1", value); }
        }

		public string dialect
		{
			get { return ini.ReadString("dialect", String.Empty); }
			set { ini.WriteString("dialect", value.Trim()); }
		}

        public string xoFormat
        {
            get { return ini.ReadString("xoFormat", "%c:%v"); }
            set { ini.WriteString("xoFormat", value); }
        }

        public string translationTraditionalAbbreviation
        {
            get { return ini.ReadString("translationTraditionalAbbreviation", String.Empty); }
            set { ini.WriteString("translationTraditionalAbbreviation", value); }
        }

        public string ldml
        {
            get { return ini.ReadString("ldml", String.Empty); }
            set { ini.WriteString("ldml", value); }
        }

        public string rodCode
        {
            get { return ini.ReadString("rodCode", String.Empty); }
            set { ini.WriteString("rodCode", value); }
        }

        public string script
        {
            get { return ini.ReadString("script", "Latin"); }
            set { ini.WriteString("script", value); }
        }

        public string localRightsHolder
        {
            get { return ini.ReadString("localRightsHolder", String.Empty); }
            set { ini.WriteString("localRightsHolder", value); }
        }

        public string facebook
        {
            get { return ini.ReadString("facebook", String.Empty); }
            set { ini.WriteString("facebook", value); }
        }

        public string customCssFileName
        {
            get { return ini.ReadString("customCssFileName", "haiola.css"); }
            set { ini.WriteString("customCssFileName", value); }
        }

        public string footNoteCallers
        {
            get { return ini.ReadString("footNoteCallers", "* † ‡ §"); }
            set { ini.WriteString("footNoteCallers", value); }
        }

        public string xrefCallers
        {
            get { return ini.ReadString("xrefCallers", "✡"); }
            set { ini.WriteString("xrefCallers", value); }
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

        public bool downloadsAllowed
        {
            get { return ini.ReadBool("downloadsAllowed", redistributable || ccbyndnc || publicDomain || ccbysa || ccbynd) && !privateProject; }
            set { ini.WriteBool("downloadsAllowed", value); }
        }

        public bool PrepublicationChecks
        {
            get { return ini.ReadBool("PrepublicationChecks", false); }
            set { ini.WriteBool("PrepublicationChecks", value); }
        }

        public string textSourceUrl
        {
            get { return ini.ReadString("textSourceUrl", String.Empty); }
            set { ini.WriteString("textSourceUrl", value); }
        }

        public bool WebSiteReady
        {
            get { return ini.ReadBool("WebSiteReady", false); }
            set { ini.WriteBool("WebSiteReady", value); }
        }

        public bool selected
        {
            get { return ini.ReadBool("selected", false); }
            set { ini.WriteBool("selected", value); }
        }

        public bool ETENDBL
        {
            get { return ini.ReadBool("ETENDBL", false); }
            set { ini.WriteBool("ETENDBL", value); }
        }

        public string contentCreator
		{
			get { return anonymous ? "anonymous" : ini.ReadString("contentCreator", String.Empty); }
			set { ini.WriteString("contentCreator", value.Trim()); }
		}

		public string contributor
		{
			get { return anonymous ? "anonymous" : ini.ReadString("contributor", String.Empty); }
			set { ini.WriteString("contributor", value.Trim()); }
		}

        public string paratextProject
        {
            get { return ini.ReadString("paratextProject", String.Empty); }
            set { ini.WriteString("paratextProject", String.IsNullOrEmpty(value) ? String.Empty : value.Trim()); }
        }

        public string paratext8Project
        {
            get { return ini.ReadString("paratext8Project", String.Empty); }
            set { ini.WriteString("paratext8Project", String.IsNullOrEmpty(value) ? String.Empty : value.Trim()); }
        }

        public string paratextUniqueId
        {
            get { return ini.ReadString("paratextUniqueId", String.Empty); }
            set { ini.WriteString("paratextUniqueId", value.Trim()); }
        }

        public string paratextGuid
        {
            get { return ini.ReadString("paratextGuid", String.Empty); }
            set { ini.WriteString("paratextGuid", value); }
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

        public DateTime SwordVersionDate
        {
            get { return ini.ReadDateTime("SwordVersionDate", contentUpdateDate); }
            set { ini.WriteDateTime("SwordVersionDate", value); }
        }

        public DateTime SourceFileDate
        {
            get { return ini.ReadDateTime("SourceFileDate", contentUpdateDate); }
            set { ini.WriteDateTime("SourceFileDate", value); }
        }

        public int SwordMajorVersion
        {
            get { return ini.ReadInt("SwordMajorVersion", 0); }
            set { ini.WriteInt("SwordMajorVersion", value); }
        }

        public int SwordMinorVersion
        {
            get { return ini.ReadInt("SwordMinorVersion", 200); }
            set { ini.WriteInt("SwordMinorVersion", value); }
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

        public bool hasStrongs
        {
            get { return ini.ReadBool("hasStrongs", false); }
            set { ini.WriteBool("hasStrongs", value); }
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


		public bool ccbyndnc
		{
			get { return ini.ReadBool("creativeCommons", false); }
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

        /*
		public bool preprocess
		{
			get { return ini.ReadBool("preprocess", true); }
			set { ini.WriteBool("preprocess", value); }
		}
        */

        /*
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
        */

		public string copyrightOwner
		{
			get { return anonymous?"anonymous":ini.ReadString("copyrightOwner", String.Empty); }
			set { ini.WriteString("copyrightOwner", value.Trim()); }
		}

        public string copyrightOwnerAbbrev
        {
            get { return anonymous ? String.Empty : ini.ReadString("copyrightOwnerAbbrev", String.Empty); }
            set { ini.WriteString("copyrightOwnerAbbrev", value.Trim()); }
        }

        public string copyrightOwnerUrl
        {
            get { return anonymous ? String.Empty : ini.ReadString("copyrightOwnerUrl", String.Empty); }
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

        public string country
        {
            get { return ini.ReadString("country", String.Empty); }
            set { ini.WriteString("country", value); }
        }

        public string countryCode
        {
            get { return ini.ReadString("countryCode", String.Empty); }
            set { ini.WriteString("countryCode", value); }
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

        public bool disablePrintingFigOrigins
        {
            get { return ini.ReadBool("disablePrintingFigOrigins", true); }
            set { ini.WriteBool("disablePrintingFigOrigins", value); }
        }

		public string footerHtml
		{
			get { return ini.ReadString("footerHtml", String.Empty).Replace("<br>", "<br />"); }
            set { ini.WriteString("footerHtml", value.Trim().Replace("<br>", "<br />")); }
		}

		public string indexHtml
		{
            get { return ini.ReadString("indexHtml", String.Empty).Replace("<br>", "<br />"); }
            set { ini.WriteString("indexHtml", value.Trim().Replace("<br>", "<br />")); }
		}

		public string licenseHtml
		{
            get { return ini.ReadString("licenseHtml", String.Empty).Replace("<br>", "<br />"); }
            set { ini.WriteString("licenseHtml", value.Trim().Replace("<br>", "<br />")); }
		}

        public string promoHtml
        {
            get { return ini.ReadString("promoHtml", String.Empty).Replace("<br>", "<br />"); }
            set { ini.WriteString("promoHtml", value.Replace("<br>", "<br />")); }
        }

		public string versificationScheme
		{
			get { return ini.ReadString("versificationScheme", "Automatic"); }
            set { ini.WriteString("versificationScheme", value.Trim()); }
		}

        public string swordVersification
        {
            get { return ini.ReadString("swordVersification", "NRSVA"); }
            set { ini.WriteString("swordVersification", value); }
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

        /* Replaced by CVSeparator
		public string chapterAndVerseSeparator
		{
			get { return ini.ReadString("chapterAndVerseSeparator", ":"); }
			set { ini.WriteString("chapterAndVerseSeparator", value); }
		}
        */

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

        public bool RegenerateNoteOrigins
        {
            get { return ini.ReadBool("RegenerateNoteOrigins", true); }
            set { ini.WriteBool("RegenerateNoteOrigins", value); }
        }

        public string verseNumberLocation
		{
			get { return ini.ReadString("verseNumberLocation", "Begin"); }
			set { ini.WriteString("verseNumberLocation", value); }
		}

        public string CVSeparator   // Same as Paratext ssf ChapterVerseSeparator
        {
            get { return ini.ReadString("CVSeparator", ":"); }
            set { ini.WriteString("CVSeparator", value); }
        }

        /* Replaced by RangeSeparator
        public string RangeIndicator
        {
            get { return ini.ReadString("RangeIndicator", "-"); }
            set { ini.WriteString("RangeIndicator", value); }
        }
        */

        /* Replaced by multiRefSameChapterSeparator
        public string SequenceIndicator
        {
            get { return ini.ReadString("SequenceIndicator", ", "); }
            set { ini.WriteString("SequenceIndicator", value); }
        }
        */

        /* Replaced by multiRefDifferentChapterSeparator
        public string ChapterRangeIndicator
        {
            get { return ini.ReadString("ChapterRangeIndicator", "–"); }
            set { ini.WriteString("ChapterRangeIndicator", value); }
        }
        */

        public string BookSequenceSeparator
        {
            get { return ini.ReadString("BookSequenceSeparator", ";"); }
            set { ini.WriteString("BookSequenceSeparator", value); }
        }

        public string ChapterNumberSeparator
        {
            get { return ini.ReadString("ChapterNumberSeparator", ";"); }
            set { ini.WriteString("ChapterNumberSeparator", value); }
        }

        public string BookSourceForMarkerXt
        {
            get { return ini.ReadString("BookSourceForMarkerXt", "ShortName"); }
            set { ini.WriteString("BookSourceForMarkerXt", value); }
        }

        public string BookSourceForMarkerR
        {
            get { return ini.ReadString("BookSourceForMarkerR", "ShortName"); }
            set { ini.WriteString("BookSourceForMarkerR", value); }
        }

        public string dependsOn
        {
            get { return ini.ReadString("dependsOn", String.Empty); }
            set { ini.WriteString("dependsOn", value); }
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
			get
            {
                string s = ini.ReadString("homeDomain", string.Empty);
                if (s == "pngscriptures.org")
                    s = "png.bible";
                ini.WriteString("homeDomain", s);
                return s;
                // return ini.ReadString("homeDomain", string.Empty);
            }
			set
            {
                ini.WriteString("homeDomain", value); }
		    }

        /*
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
        */

        public bool lastRunResult
        {
            get { return ini.ReadBool("lastRunResult", false); }
            set { ini.WriteBool("lastRunResult", value); }
        }

        public bool warningsFound
        {
            get { return ini.ReadBool("warningsFound", false); }
            set { ini.WriteBool("warningsFound", value); }
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
        /// Generate Mobile HTML instead of Concordance or the old HTML with JavaScript
        /// </summary>
        public bool GenerateMobileHtml
        {
            get { return ini.ReadBool("GenerateMobileHtml", true); }
            set { ini.WriteBool("GenerateMobileHtml", value); }
        }

        /// <summary>
        /// Generate legacy HTML instead of Mobile HTML or Concordance HTML
        /// </summary>
        public bool LegacyHtml
        {
            get { return ini.ReadBool("LegacyHtml", false); }
            set { ini.WriteBool("LegacyHtml", value); }
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
		public string MaxFreqSrc
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
		public string PhrasesSrc
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
		public List<string> Phrases { get; set; }

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
		/// along with some navigation tools. This persistent variable is redefined to imply concordance generation,
        /// since we don't test or support frames in the regular HTML any more. HTML 5 pretty much did away with
        /// frames.
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
        public string m_indexHtml = "<div class=\"toc1\"><a href=\"http://png.bible/{0}/{0}_html.zip\">{0}_html.zip</a></div>"+Environment.NewLine+
"<div class=\"toc1\"><a href=\"http://png.bible/resources.php?id={0}\">More downloads</a></div>"+Environment.NewLine+
//"<div class=\"toc1\"><a href=\"http://pnglanguages.org/pacific/png/show_lang_entry.asp?id={0}\">Linguistic publications</a></div>"+Environment.NewLine+
"<div class=\"toc1\"><a href=\"http://www.ethnologue.org/language/{0}\">Ethnologue</a></div>"+Environment.NewLine;
        public string m_licenseHtml = String.Empty;
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
