﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Data;
using System.IO;
using System.Collections;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Schema;
using System.Diagnostics;

namespace WordSend
{
    // Converts USFX to Modified OSIS. The modification is subtle in that the results still validate
    // against the OSIS Schema, but do not strictly comply with some of the more problematical
    // assertions of the OSIS manual. The output is designed for import to Sword Project modules,
    // and not recommended for archival use, because some of the noncanonical features of the
    // work are stripped out due to incompatibilities between the formats.
    public class usfxToMosisConverter
    {
        // The following need to be set by the caller.
        public string languageCode;
        public string translationId;
        public DateTime revisionDateTime;
        public string vernacularTitle;
        public string contentCreator;
        public string contentContributor;
        public string englishDescription;
        public string lwcDescription;
        public string printPublisher;
        public string ePublisher;
        public string languageName;
        public string vernacularLanguageName;
        public string dialect;
        public string copyrightNotice;
        public string rightsNotice;
        public string infoPage;
        public string swordDir;
        public string swordRestricted;
        public Options projectOptions = null;

        // Internal variables:

        protected string osisWorkId;
        protected string usfxFileName;
        protected XmlTextReader usfx;
        protected string mosisFileName;
        protected XmlTextWriter mosis;

        protected string element;
        protected string sfm;
        protected string id;
        protected string style;
        protected string level;
        protected string caller;
        protected string htmDir;
        protected string currentBookAbbrev;
        protected string currentBookHeader;
        protected string currentBookTitle;
        protected string osisBook;
        protected string osisVerseId;   // First or only verse in verse bridge
        protected string osisVersesId;  // Space-separated list of verses in verse bridge
        protected string verseeID;
        protected string vpeID;
        protected string chaptereID;
        protected string epeID;
        protected string altChapterID;
        protected string qeID;
        protected int serialNumber = 0;
        protected int noteNumber = 0;
        protected string lastNoteVerse = "";
        protected string currentChapter;
        protected string currentChapterAlternate;
        protected string wordForChapter;
        protected string currentChapterPublished;
        protected string currentBCV;
        protected int chapterNumber; // of the file we are currently generating
        int verseNumber;
        protected string currentVerse;
        protected string currentVersePublished;
        protected string currentVerseAlternate;
        protected string currentFileName = "";
        protected string previousFileName = "";
        protected string chapterLabel = "";
        protected string psalmLabel = "";
        protected string langName = "";
        protected string langId = "";
        protected StringBuilder preVerse = new StringBuilder(String.Empty);
        protected string previousChapterText;
        protected string nextChapterText;
        protected ArrayList chapterFileList = new ArrayList();
        protected int chapterFileIndex = 0;
        protected bool ignore = false;
        protected bool inLineGroup = false;
        protected bool inPoetryLine = false;
        protected bool inNote = false;
        protected bool inReference = false;
        protected int listLevel = 0;
        protected int itemLevel = 0;
        protected int indentLevel = 0;
        protected string osisFileName;
        protected string currentElement;
        protected string strongs;
        protected string strongMorph;
        protected string lastElementWritten = String.Empty;
        protected int mosisNestLevel = 0;
        protected ArrayList elementContext = new ArrayList();
        Dictionary<string, int> containers = new Dictionary<string, int>();


        public static ArrayList bookList = new ArrayList();

        public LanguageCodeInfo langCodes;

        protected void OpenContainer(string divName, bool nestable)
        {
            int nesting;
            if (!containers.TryGetValue(divName, out nesting))
            {
                containers[divName] = nesting = 0;
            }
            if (!nestable)
            {
                if (nesting > 0)
                {
                    WriteMosisEndElement();
                    nesting--;
                    containers[divName] = nesting;
                }
            }
            StartMosisElement(divName);
            nesting++;
            containers[divName] = nesting;
        }

        protected void CloseContainer(string divName)
        {
            int nesting;
            if (!containers.TryGetValue(divName, out nesting))
                nesting = 0;
            if (nesting > 0)
            {
                WriteMosisEndElement();
                nesting--;
                containers[divName] = nesting;
            }
        }


        protected void StartMosisElement(string elementName)
        {
            if (mosis == null)
            {
                Logit.WriteError("USFX missing languageCode?");
                return;
            }
            mosis.WriteStartElement(elementName);
            lastElementWritten = "<" + elementName;
            elementContext.Add(elementName);
            mosisNestLevel++;
        }

        protected void WriteMosisElementString(string elementName, string s)
        {
            mosis.WriteElementString(elementName, s);
            lastElementWritten = "<" + elementName + ">" + s + "</" + elementName + ">";
        }

        protected void WriteMosisEndElement()
        {
            mosis.WriteEndElement();
            mosisNestLevel--;
            if (mosisNestLevel < 0)
            {
                Logit.WriteError("More end elements than start elements at " + osisVerseId);
                mosisNestLevel = 0;
            }
            elementContext.RemoveAt(mosisNestLevel);
        }

        protected void CheckElementLevel(int level, string msg)
        {
            if (level != mosisNestLevel)
            {
                Logit.WriteError("Error writing MOSIS: nest level is " + mosisNestLevel.ToString() + ", but expected level is " + level.ToString() + " at " + osisVerseId + " " + msg);
                int i;
                for (i = 0; i < mosisNestLevel; i++)
                    Logit.WriteError(String.Format("   {0}: {1}", i, elementContext[i]));
                Logit.WriteError("Last element written: " + lastElementWritten);
            }
        }

        protected void CheckMinimumLevel(int level, string msg)
        {
            if (mosisNestLevel < level)
            {
                Logit.WriteError("Error writing MOSIS: nest level is " + mosisNestLevel.ToString() + ", but expected level is >= " + level.ToString() + " at " + osisVerseId + " " + msg);
                int i;
                for (i = 0; i < mosisNestLevel; i++)
                    Logit.WriteError(String.Format("   {0}: {1}", i, elementContext[i]));
                Logit.WriteError("Last element written: " + lastElementWritten);
            }
        }

        
        protected int bookListIndex = 0;
        protected StringBuilder footnotesToWrite;
        protected StreamWriter htm;
        public BibleBookInfo bookInfo = new BibleBookInfo();

        BibleBookRecord bookRecord;

        public static string OsisDateTime(DateTime dt)
        {   // If the time is midnight or missing, the T and the time are ommitted, as allowed by the standard.
            return dt.ToString("yyyy.MM.ddTHH.mm.ss").Replace("T00.00.00", "");
        }

        protected string NoteId()
        {
            if (lastNoteVerse != osisVerseId)
            {
                noteNumber = 1;
                lastNoteVerse = osisVerseId;
            }
            else
            {
                noteNumber++;
            }
            return osisVerseId + "!note." + noteNumber.ToString();
        }

        protected string StartId()
        {
            serialNumber++;
            return osisVerseId + ".seID." + serialNumber.ToString("00000");
        }

        protected void SetListLevel(int level, bool canonical = false)
        {
            while ((listLevel > level) || (itemLevel > level))
            {
                if (itemLevel > level)
                {
                    CloseContainer("item");
                    itemLevel--;
                }
                if (listLevel > level)
                {
                    CloseContainer("list");
                    listLevel--;
                }
            }

            while ((listLevel < level) || (itemLevel < level))
            {
                if (listLevel < level)
                {
                    OpenContainer("list", true);
                    listLevel++;
                }
                if (itemLevel < listLevel)
                {
                    OpenContainer("item", true);
                    if (!canonical)
                    {
                        mosis.WriteAttributeString("canonical", "false");
                    }
                    itemLevel++;
                }
            }
        }

        protected void EndCurrentVerse()
        {
            if (vpeID.Length > 0)
            {
                StartMosisElement("verse");
                mosis.WriteAttributeString("eID", vpeID);
                WriteMosisEndElement();    // verse
                vpeID = String.Empty;
            }
            if (verseeID.Length > 0)
            {
                StartMosisElement("verse");
                mosis.WriteAttributeString("eID", verseeID);
                WriteMosisEndElement();    // verse
                verseeID = String.Empty;
            }
        }

        protected void EndCurrentChapter()
        {
            EndIntroduction();
            EndCurrentVerse();
            SetListLevel(0);
            EndLineGroup();
            EndTitledPsalm();
            if (currentBookAbbrev == "PSA")
                EndSection();
            if (altChapterID.Length > 0)
            {
                StartMosisElement("chapter");
                mosis.WriteAttributeString("eID", altChapterID);
                WriteMosisEndElement();
                altChapterID = string.Empty;
            }
            if (epeID.Length > 0)
            {
                StartMosisElement("chapter");
                mosis.WriteAttributeString("eID", epeID);
                WriteMosisEndElement();
                epeID = string.Empty;
            }
            if (chaptereID.Length > 0)
            {
                StartMosisElement("chapter");
                mosis.WriteAttributeString("eID", chaptereID);
                WriteMosisEndElement();    // chapter
                chaptereID = String.Empty;
            }

        }

        protected void WriteElementStringIfNotEmpty(string localName, string value)
        {
            if (value != null)
            {
                if (value.Length > 0)
                    WriteMosisElementString(localName, value);
            }
        }

        protected void WriteElementAndAttributeStringsIfNotEmpty(string elementName, string elementString, string attributeName, string attributeString)
        {
            if (elementString != null)
            {
                if (elementString.Length > 0)
                {
                    StartMosisElement(elementName);
                    mosis.WriteAttributeString(attributeName, attributeString);
                    mosis.WriteString(elementString);
                    WriteMosisEndElement();    // elementName
                }
            }
        }

        protected const string localOsisSchema = "osisCore.2.1.1.xsd";
        /* protected const string osisSchema = "http://ebible.org/osisCore.2.1.1.xsd"; */
        protected const string osisSchema = "osisCore.2.1.1.xsd";
        protected const string osisNamespace = "http://www.bibletechnologies.net/2003/OSIS/namespace";

        protected void OpenMosisFile(string mosisFileName)
        {
            string schemaPath = Path.Combine(Path.GetDirectoryName(mosisFileName), localOsisSchema);
            
            mosis = new XmlTextWriter(mosisFileName, Encoding.UTF8);
            mosis.Formatting = Formatting.Indented;
            mosis.WriteStartDocument();
            OpenContainer("osis", false);
            mosis.WriteAttributeString("xmlns", osisNamespace);
            mosis.WriteAttributeString("xmlns:xsi", "http://www.w3.org/2001/XMLSchema-instance");
            mosis.WriteAttributeString("xsi:schemaLocation", osisNamespace + " " + osisSchema);
            OpenContainer("osisText", false);
            mosis.WriteAttributeString("osisIDWork", osisWorkId);
            mosis.WriteAttributeString("osisRefWork", "bible");
            string osisLang = langCodes.ShortCode(languageCode);
            if (osisLang.Length > 2)
                osisLang = "x-" + osisLang;
            mosis.WriteAttributeString("xml:lang", osisLang);
            mosis.WriteAttributeString("canonical", "true");

            OpenContainer("header", false);
            StartMosisElement("revisionDesc");
            WriteMosisElementString("date", OsisDateTime(DateTime.UtcNow));
            WriteMosisElementString("p", @"generated");
            WriteMosisEndElement();
            StartMosisElement("revisionDesc");
            WriteMosisElementString("date", OsisDateTime(revisionDateTime));
            WriteMosisElementString("p", "created or updated OSIS file contents");
            WriteMosisEndElement();    // revisionDesc
            StartMosisElement("work");    // Insert Dublin Core identity here.
            mosis.WriteAttributeString("osisWork", osisWorkId);
            WriteElementAndAttributeStringsIfNotEmpty("title", vernacularTitle, "type", "x-vernacular");
            if (!projectOptions.anonymous)
            {
                WriteElementStringIfNotEmpty("contributor", contentContributor);
                WriteElementStringIfNotEmpty("creator", contentCreator);
            }
            WriteElementAndAttributeStringsIfNotEmpty("description", englishDescription, "type", "x-english");
            WriteElementAndAttributeStringsIfNotEmpty("description", lwcDescription, "type", "x-lwc");
            WriteElementAndAttributeStringsIfNotEmpty("publisher", printPublisher, "type", "x-print");
            WriteElementAndAttributeStringsIfNotEmpty("publisher", ePublisher, "type", "x-electronic");
            WriteElementAndAttributeStringsIfNotEmpty("identifier", translationId, "type", "x-ebible-id");
            WriteElementAndAttributeStringsIfNotEmpty("language", languageCode, "type", "x-ethnologue");
            if (dialect.Length > 0)
            {
                WriteElementAndAttributeStringsIfNotEmpty("language", dialect + " dialect of " + languageName, "type", "x-in-english");
            }
            else
            {
                WriteElementAndAttributeStringsIfNotEmpty("language", languageName, "type", "x-in-english");
            }
            WriteElementAndAttributeStringsIfNotEmpty("language",vernacularLanguageName, "type", "x-vernacular");
            WriteElementAndAttributeStringsIfNotEmpty("rights", copyrightNotice, "type", "x-copyright");
            WriteElementAndAttributeStringsIfNotEmpty("rights", rightsNotice, "type", "x-license");
            StartMosisElement("refSystem");
            mosis.WriteString("Bible");
            WriteMosisEndElement();    // refSystem
            WriteMosisEndElement();    // work
            StartMosisElement("work");
            mosis.WriteAttributeString("osisWork","bible");
            StartMosisElement("type");
            mosis.WriteAttributeString("type", "OSIS");
            mosis.WriteString("Bible");
            WriteMosisEndElement();    // type
            WriteMosisElementString("refSystem", "Bible");
            //mosis.WriteAttributeString("path", osisWorkId);
            WriteMosisEndElement();    //work
            CloseContainer("header");
        }

        protected void CloseMosisFile()
        {
            EndTestament();
            CloseContainer("osisText");    // osisText
            CloseContainer("osis");
            mosis.WriteEndDocument();
            mosis.Close();
            mosis = null;
            fileHelper.NormalizeLineEnds(OsisFileName);
        }

        protected string currentTestament;

        protected void EndTestament()
        {
            if (currentTestament != String.Empty)
            {
                CloseContainer("div");  // div type="bookGroup"
                currentTestament = String.Empty;
            }
        }

        protected void StartTestament(string testament)
        {
            if (currentTestament != testament)
            {
                EndTestament();
                OpenContainer("div", true);
                mosis.WriteAttributeString("type", "bookGroup");
                switch (testament)
                {
                    case "o":
                        mosis.WriteAttributeString("canonical", "true");
                        WriteMosisElementString("title", "Old Testament");
                        break;
                    case "a":
                        mosis.WriteAttributeString("canonical", "true");
                        WriteMosisElementString("title", "Apocrypha/Deuterocanon");
                        break;
                    case "n":
                        mosis.WriteAttributeString("canonical", "true");
                        WriteMosisElementString("title", "New Testament");
                        break;
                    case "x":
                        mosis.WriteAttributeString("canonical", "false");
                        WriteMosisElementString("title", "Peripherals and helps");
                        break;
                }
                currentTestament = testament;
            }
        }


        protected string GetNamedAttribute(string attributeName)
        {
            string result = usfx.GetAttribute(attributeName);
            if (result == null)
                result = String.Empty;
            return result;
        }


       protected void EndLineGroup()
        {
            if (inLineGroup)
            {
                CloseContainer("lg");
                inLineGroup = false;
            }
        }

        protected void StartLineGroup()
        {
            EndLineGroup();
            OpenContainer("lg", false);
            inLineGroup = true;
        }

        protected void StartElementWithAttribute(string elementName, string attributeName = null, string attributeValue = null,
            string attribute2Name = null, string attribute2Value = null, string attribute3Name = null, string attribute3Value = null)
        {
            StartMosisElement(elementName);
            if ((attributeName != null) && (attributeName.Length > 0) && (attributeValue != null))
            {
                mosis.WriteAttributeString(attributeName, attributeValue);
            }
            if ((attribute2Name != null) && (attribute2Name.Length > 0) && (attribute2Value != null))
            {
                mosis.WriteAttributeString(attribute2Name, attribute2Value);
            }
            if ((attribute3Name != null) && (attribute3Name.Length > 0) && (attribute3Value != null))
            {
                mosis.WriteAttributeString(attribute3Name, attribute3Value);
            }
            if (usfx.IsEmptyElement)
            {
                WriteMosisEndElement();
            }
        }

        public string indexDateStamp = String.Empty;

        protected void SkipElement()
        {
            string skip = usfx.Name;
            if (!usfx.IsEmptyElement)
            {
                while (usfx.Read() && !((usfx.NodeType == XmlNodeType.EndElement) && (usfx.Name == skip)))
                {
                    // Keep looking for the end of this element.
                }
            }
        }

        private void ValidationCallBack(object sender, ValidationEventArgs error)
        {
            if (error.Severity == XmlSeverityType.Error)
            {
                Logit.WriteError("ERROR in " + osisFileName + " at " + osisVerseId + " after " + currentElement + ":");
                Logit.WriteError(error.Message);
            }
            else
            {
                Logit.WriteError("Warning in " + osisFileName + " at " + osisVerseId + " after " + currentElement + ":");
                Logit.WriteError(error.Message);
            }
        }

        string OsisFileName;
        protected bool inIntroduction;
        protected bool inMajorSection;
        protected bool inSection;
        protected bool inSubSection;
        protected bool inTitledPsalm;

        protected void StartTitledPsalm()
        {
            if (!usfx.IsEmptyElement)
            {
                EndTitledPsalm();   // Here for the use of \d in acrostic headings
                OpenContainer("div", true);
                inTitledPsalm = true;
            }
        }

        protected void EndTitledPsalm()
        {
            if (inTitledPsalm)
            {
                CloseContainer("div");
                inTitledPsalm = false;
            }
        }

        /// <summary>
        /// Start a majorSection division
        /// </summary>
        protected void StartMajorSection()
        {
            if (!usfx.IsEmptyElement)
            {
                EndMajorSection();
                OpenContainer("div", true);
                mosis.WriteAttributeString("type", "majorSection");
                inMajorSection = true;
            }
        }

        /// <summary>
        /// End a majorSection division
        /// </summary>
        protected void EndMajorSection()
        {
            EndSubSection();
            EndTitledPsalm();
            EndSection();
            if (inMajorSection)
            {
                CloseContainer("div");  // type="majorSection"
                inMajorSection = false;
            }
        }

        protected void StartSection()
        {
            if (!usfx.IsEmptyElement)
            {
                EndSection();
                OpenContainer("div", true);
                mosis.WriteAttributeString("type", "section");
                inSection = true;
            }
        }

        /// <summary>
        /// End a section div
        /// </summary>
        protected void EndSection()
        {
            EndSubSection();
            if (inSection)
            {
                CloseContainer("div");
                inSection = false;
            }
        }

        /// <summary>
        /// Start a subSection div
        /// </summary>
        protected void StartSubSection()
        {
            if (!usfx.IsEmptyElement)
            {
                EndSubSection();
                OpenContainer("div", true);
                mosis.WriteAttributeString("type", "section");
                inSubSection = true;
            }
        }

        /// <summary>
        /// End a subsection div
        /// </summary>
        protected void EndSubSection()
        {
            if (inSubSection)
            {
                CloseContainer("div");
                inSubSection = false;
            }
        }

        /// <summary>
        /// Start an introduction division
        /// </summary>
        protected void StartIntroduction()
        {
            if (!inIntroduction)
            {
                EndMajorSection();
                if (!usfx.IsEmptyElement)
                {
                    if (!inIntroduction)
                    {
                        OpenContainer("div", true);
                        mosis.WriteAttributeString("type", "introduction");
                        mosis.WriteAttributeString("canonical", "false");
                        inIntroduction = true;
                    }
                }
            }
        }

        /// <summary>
        /// End an introduction division
        /// </summary>
        protected void EndIntroduction()
        {
            if (inIntroduction)
            {
                EndLineGroup();
                CloseContainer("div"); // type="introduction" canonical="false"
                inIntroduction = false;
            }
        }


        /// <summary>
        /// Writes a locale file for the Sword Project
        /// </summary>
        /// <param name="localeFileName">Name of the Locale file</param>
        public void WriteLocaleFile(string localeFileName)
        {
            StreamWriter locale;
            string abbr, abbr1;
            try
            {
                locale = new StreamWriter(localeFileName, false, Encoding.UTF8);
                locale.WriteLine("[Meta]");
                locale.WriteLine("Name={0}", languageCode);
                locale.WriteLine("Description={0}", vernacularLanguageName);
                locale.WriteLine("Encoding=UTF-8");
                locale.WriteLine();
                locale.WriteLine("[Text]");
                foreach (BibleBookRecord br in bookInfo.bookArray)
                {
                    if (br != null)
                    {
                        if (!String.IsNullOrEmpty(br.vernacularShortName))
                            locale.WriteLine("{0}={1}", br.swordShortName, br.vernacularShortName);
                    }
                }
                locale.WriteLine();
                locale.WriteLine("[Book Abbrevs]");
                foreach (BibleBookRecord br in bookInfo.bookArray)
                {
                    if (br != null)
                    {
                        if (!String.IsNullOrEmpty(br.vernacularAbbreviation))
                        {
                            abbr = br.vernacularAbbreviation.ToUpper(CultureInfo.InvariantCulture);
                            abbr1 = abbr.Replace(" ", "");
                            locale.WriteLine("{0}={1}", abbr, br.osisName);
                            if (abbr1.Length != abbr.Length)
                                locale.WriteLine("{0}={1}", abbr1, br.osisName);
                        }
                    }
                }
                locale.WriteLine();
                locale.Close();
            }
            catch (Exception ex)
            {
                Logit.WriteError("Error writing " + localeFileName + Environment.NewLine + ex.Message);
            }
        }

        /// <summary>
        /// Find the last copyright year in a list of copyright years.
        /// </summary>
        /// <param name="dates"></param>
        /// <returns></returns>
        public string LastCopyrightYear(string dates)
        {
            string result = String.Empty;
            StringBuilder sb = new StringBuilder();
            int i;
            for (i = 0; i < dates.Length; i++)
            {
                if (Char.IsDigit(dates[i]))
                {
                    sb.Append(dates[i]);
                    if (sb.Length == 4)
                    {
                        result = sb.ToString();
                        sb.Clear();
                    }
                }
                else
                {
                    sb.Clear(); ;
                }
            }

            return result;
        }

        /// <summary>
        /// Converts HTML paragraph breaks and centering to RTF. The only RTF tags supported by Sword are \par, \pard, \qc, and \u{num} (and we don't need the last three).
        /// Therefore, paragraph and line breaks go to \par, entities for greater and less than are substituted, and other markup discarded.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public string HTMLtoRTF(string s)
        {
            string result = s.Replace("\n", " ").Replace("\r", " ");
            result = System.Text.RegularExpressions.Regex.Replace(result, @"<[pP] *>\?", @"\par ");
            result = System.Text.RegularExpressions.Regex.Replace(result, @"<[bB][rR] *\?>", @"\par ");
            result = System.Text.RegularExpressions.Regex.Replace(result, @"<li>", @"\par ");
            result = System.Text.RegularExpressions.Regex.Replace(result, @"<[^>]*>", "");
            result = result.Replace("&gt;", ">").Replace("&lt;", "<").Replace("&GT;", ">").Replace("&LT;", "<").Replace("  ", " ");
            return result;
        }

        public string HTMLtoContinuation(string s)
        {
            char[] charsToTrim = { '\\', '\n', '\r', '\t' };
            string result = s.Replace("\r","").Replace("\n", " \\\n");
            result = System.Text.RegularExpressions.Regex.Replace(result, @"<[pP] *>\?", " \\\n");
            result = System.Text.RegularExpressions.Regex.Replace(result, @"<[bB][rR] *\?>", " \\\n");
            result = System.Text.RegularExpressions.Regex.Replace(result, @"<li>", " * ");
            result = System.Text.RegularExpressions.Regex.Replace(result, @"<[^>]*>", "");
            result = result.Replace("&gt;", ">").Replace("&lt;", "<").Replace("&GT;", ">").Replace("&LT;", "<").Replace("        ", " ").Replace("    ", " ").Replace("  ", " ").Replace("  ", " ").Replace("  ", " ").Replace("  ", " ").Replace("  ", " ");
            result = result.Trim().TrimEnd(charsToTrim);
            return result;
        }


        public void DeleteSwordModule(string swordName)
        {
            string command, parameters;
            swordZipDir = Path.Combine(swordDir, "zip");
            try 
	        {
                Utils.EnsureDirectory(swordZipDir);
                Utils.DeleteFile(Path.Combine(swordZipDir, swordName + ".zip"));
                Utils.DeleteFile(Path.Combine(Path.Combine(swordDir, "mods.d"), swordName + ".conf"));
                Utils.DeleteDirectory(Path.Combine(Path.Combine(Path.Combine(Path.Combine(swordDir, "modules"), "texts"), "ztext"), swordName));
                command = "tar";
                parameters = "czvf mods.d.tar.gz mods.d/";
                fileHelper.RunCommand(command, parameters, swordDir);
	        }
	        catch (Exception ex)
	        {
                Logit.WriteError("Error deleting sword module "+swordName+": " + ex.Message);
	        }
        }


        private string baseDir;
        private string swordModuleDir;
        private string swordOsisName;
        private string swordZipDir;


        /// <summary>
        /// Calls osis2mod to create a sword module. Also calls external programs tar and zip to make a proper Sword repository structure.
        /// If the module is not restricted, the module is added to the swordDir repository, otherwise it goes to swordRestricted.
        /// </summary>
        /// <param name="swordName">Name of the Sword module</param>
        /// <param name="restricted">true iff this module is not legally cleared for free public posting</param>
        /// <returns>Module install size</returns>
        public long WriteSwordModule(string swordName, bool restricted)
        {
            swordZipDir = Path.Combine(baseDir, "zip");
            swordModuleDir = Path.Combine(baseDir, "modules");
            swordOsisName = OsisFileName; // Path.ChangeExtension(OsisFileName, ".sosis");
            string oldSwordModuleDir = Path.Combine(Path.Combine(swordModuleDir, "texts"), "ztext");
            string command;
            string parameters;
            long result = 0;
            // StreamWriter swordOsis;
            // StreamReader mosis;
            Logit.ShowStatus("writing Sword module");
            try
            {
                // Clear out old stuff
                Utils.DeleteDirectory(Path.Combine(oldSwordModuleDir, swordName));
                // set up directory structure
                Utils.EnsureDirectory(baseDir);
                Utils.EnsureDirectory(swordDir);
                Utils.EnsureDirectory(swordZipDir);
                Utils.DeleteFile(Path.Combine(swordZipDir, swordName + ".zip"));
                Utils.EnsureDirectory(swordModuleDir);
                swordModuleDir = Path.Combine(swordModuleDir, "texts");
                Utils.EnsureDirectory(swordModuleDir);
                swordModuleDir = Path.Combine(swordModuleDir, "ztext");
                Utils.EnsureDirectory(swordModuleDir);
                swordModuleDir = Path.Combine(swordModuleDir, swordName);
                Utils.EnsureDirectory(swordModuleDir);
                // Here is where we actually make the module.
                command = "osis2mod";
                parameters = "\"" + swordModuleDir + "\" \"" + swordOsisName + "\" -z -b 4 -v " + projectOptions.swordVersification;
                if ((projectOptions.languageId == "hbo") || (projectOptions.languageId == "heb"))
                    parameters = parameters + " -N";
                fileHelper.RunCommand(command, parameters, baseDir);

                // Logit.WriteLine("cd " + baseDir);
                // Logit.WriteLine(command);
                string[] fileList = Directory.GetFiles(swordModuleDir, "*.*");
                foreach (string fileName in fileList)
                {
                    FileInfo info = new FileInfo(fileName);
                    result += info.Length;
                }
            }
            catch (Exception ex)
            {
                Logit.WriteError("Error writing Sword module: " + ex.Message);
            }
            return result;
        }

        /// <summary>
        /// Writes a Sword configuration file for the current module.
        /// </summary>
        public void WriteSwordConfig()
        {
            Logit.ShowStatus("writing Sword config");
            if (projectOptions.downloadsAllowed)
                baseDir = swordDir;
            else
                baseDir = swordRestricted;
            Utils.EnsureDirectory(baseDir);
            string modsd = Path.Combine(baseDir, "mods.d");
            Utils.EnsureDirectory(modsd);
            string yr = LastCopyrightYear(projectOptions.copyrightYears);
            string swordName = projectOptions.SwordName;
            string command, parameters;
            long installSize = 0;
            char[] separators = new char[] { ' ', ',' };
            string[] oldNames = new string[] { String.Empty };
                
            if (String.IsNullOrEmpty(swordName))
            {
                swordName = projectOptions.translationId.Replace("-", "").Replace("_", "");
                if ((yr.Length == 4) && !Char.IsDigit(swordName[swordName.Length-1]))
                    swordName = swordName + yr;
                projectOptions.SwordName = swordName;
            }

            // Don't waste time building private projects Sword modules.
            if (projectOptions.privateProject)
            {
                DeleteSwordModule(projectOptions.SwordName);
                foreach (string sname in oldNames)
                {
                    DeleteSwordModule(projectOptions.SwordName);
                }
                return;
            }

            if (!String.IsNullOrEmpty(projectOptions.ObsoleteSwordName))
            {
                oldNames = projectOptions.ObsoleteSwordName.Split(separators, StringSplitOptions.RemoveEmptyEntries);
                foreach (string oldName in oldNames)
                {
                    DeleteSwordModule(oldName);
                }
            }

            // Don't create a Sword module if there were errors in the build so far.
            if (Logit.loggedError)
            {
                Logit.WriteLine("Skipping Sword module creation due to prior errors.");
                return;
            }

            projectOptions.swordVersification = SwordVs.GetSwordVersification();
            string swordConfName = Path.Combine(modsd, swordName + ".conf");

            installSize = WriteSwordModule(swordName, false);
            
            try
            {
                Utils.EnsureDirectory(modsd);
                StreamWriter config = new StreamWriter(swordConfName);
                config.WriteLine("## Sword module configuration file");
                config.WriteLine();
                config.WriteLine("## Required elements");
                config.WriteLine("# Module unique identifier");
                config.WriteLine("[{0}]", swordName);
                config.WriteLine("");
                config.WriteLine("# Short well-known abbreviation for the module");
                if (String.IsNullOrEmpty(projectOptions.translationTraditionalAbbreviation))
                {
                    config.WriteLine("Abbreviation={0}", translationId.ToUpperInvariant().Replace("-", "").Replace("_", ""));
                }
                else
                {
                    config.WriteLine("Abbreviation={0}", projectOptions.translationTraditionalAbbreviation);
                }
                config.WriteLine();
                config.WriteLine("# Short description of the module");
                if (!String.IsNullOrEmpty(projectOptions.vernacularTitle))
                    config.WriteLine("Description={0}", projectOptions.vernacularTitle);
                else if (!String.IsNullOrEmpty(projectOptions.shortTitle))
                    config.WriteLine("Description={0}", projectOptions.shortTitle);
                else if (!String.IsNullOrEmpty(projectOptions.EnglishDescription))
                    config.WriteLine("Description={0}", projectOptions.EnglishDescription);
                else if (!String.IsNullOrEmpty(projectOptions.vernacularTitle))
                    config.WriteLine("Description={0}", projectOptions.vernacularTitle);
                config.WriteLine();
                config.WriteLine("# Path to the module data files relative to the Sword module library root directory");
                config.WriteLine("DataPath=./modules/texts/ztext/{0}/", swordName);
                config.WriteLine();
                config.WriteLine("# Driver used for reading the module");
                config.WriteLine("ModDrv=zText");
                config.WriteLine();
                config.WriteLine("## Required elements with defaults");
                config.WriteLine("# Markup used in the module");
                config.WriteLine("SourceType=OSIS");
                config.WriteLine();
                config.WriteLine("# Character encoding used in the module and this conf file");
                config.WriteLine("Encoding=UTF-8");
                config.WriteLine();
                config.WriteLine("# Compression algorithm");
                config.WriteLine("CompressType=ZIP");
                config.WriteLine();
                config.WriteLine("# How much of the work is compressed into a block");
                config.WriteLine("BlockType=BOOK");
                config.WriteLine();
                config.WriteLine("# Closest standard versification to what is used in this work");
                config.WriteLine("Versification={0}", projectOptions.swordVersification);
                config.WriteLine();
                config.WriteLine("## Elements required for proper rendering");
                config.WriteLine("# Never use <q> to supply quotation punctuation. All required and allowed punctuation is in the text already.");
                config.WriteLine("OSISqToTick=false");
                if (projectOptions.textDir == "rtl")
                {
                    config.WriteLine("# Primary text reading direction is right to left.");
                    config.WriteLine("Direction=RtoL");
                }
                config.WriteLine();
                config.WriteLine("# Recommended font");
                config.WriteLine("Font={0}", projectOptions.fontFamily);
                config.WriteLine("# Filters");
                if (OSISLemma)
                    config.WriteLine("GlobalOptionFilter=OSISLemma");
                if (OSISStrongs)
                {
                    config.WriteLine("Feature=StrongsNumbers");
                    config.WriteLine("GlobalOptionFilter=OSISStrongs");
                }
                if (OSISFootnotes)
                    config.WriteLine("GlobalOptionFilter=OSISFootnotes");
                if (OSISScripref)
                    config.WriteLine("GlobalOptionFilter=OSISScripref");
                if (OSISMorph)
                    config.WriteLine("GlobalOptionFilter=OSISMorph");
                if (OSISHeadings)
                    config.WriteLine("GlobalOptionFilter=OSISHeadings");
                if (OSISRedLetterWords)
                    config.WriteLine("GlobalOptionFilter=OSISRedLetterWords");
                if (projectOptions.languageId == "hbo" || projectOptions.languageId == "heb")
                {
                    config.WriteLine("GlobalOptionFilter=UTF8Cantillation");
                    config.WriteLine("GlobalOptionFilter=UTF8HebrewPoints");
                }
                config.WriteLine();
                config.WriteLine("# Longer description of the text");
                config.WriteLine("About={0}", HTMLtoRTF(projectOptions.promoHtml + "<br/>" + infoPage));
                config.WriteLine();
                config.WriteLine("# Sword module version number and date");
                if ((projectOptions.SwordVersionDate <= projectOptions.SourceFileDate) || (projectOptions.SwordVersionDate <= projectOptions.contentUpdateDate))
                {
                    projectOptions.SwordMajorVersion++;
                    projectOptions.SwordMinorVersion = 0;
                }
                else
                {
                    projectOptions.SwordMinorVersion++;
                    if (projectOptions.SwordMinorVersion > 99)
                    {
                        projectOptions.SwordMajorVersion++;
                        projectOptions.SwordMinorVersion = 0;
                    }
                }
                projectOptions.SwordVersionDate = DateTime.Now;
                projectOptions.Write();
                config.WriteLine("SwordVersionDate={0}", projectOptions.SwordVersionDate.ToString("yyyy-MM-dd"));
                config.WriteLine("Version={0}.{1}", projectOptions.SwordMajorVersion, projectOptions.SwordMinorVersion);
                if (projectOptions.eBibledotorgunique)
                    config.WriteLine("History_{0}.{1}=Automatically generated on {2} from source files dated {3} by eBible.org (http://eBible.org) with funding through World Outreach Missions",
                        projectOptions.SwordMajorVersion, projectOptions.SwordMinorVersion,
                        DateTime.Now.Date.ToString("yyyy-MM-dd"),
                        projectOptions.SourceFileDate.Date.ToString("yyyy-MM-dd"));
                else
                    config.WriteLine("History_{0}.{1}=Automatically generated on {2} from source files dated {3} using Haiola (http://haiola.org)",
                        projectOptions.SwordMajorVersion, projectOptions.SwordMinorVersion,
                        DateTime.Now.Date.ToString("yyyy-MM-dd"),
                        projectOptions.SourceFileDate.Date.ToString("yyyy-MM-dd"));

                config.WriteLine();
                config.WriteLine("# Minimum version of the Sword libary needed to load this module");
                config.WriteLine("MinimumVersion=1.7.0");
                config.WriteLine();
                config.WriteLine("# This is a Bible translation.");
                config.WriteLine("Category=Biblical Texts");
                config.WriteLine();
                config.WriteLine("# Library of Congress subject heading");
                config.WriteLine("LCSH=Bible. {0}.", projectOptions.languageNameInEnglish);
                config.WriteLine();
                config.WriteLine("# Language code (2 letters if available, otherwise 3 letters)");
                config.WriteLine("Lang={0}", shortLang);
                config.WriteLine();
                config.WriteLine("# Number of bytes this module takes up on disk");
                config.WriteLine("InstallSize={0}", installSize.ToString());
                config.WriteLine();
                config.WriteLine("# OSIS isn't currently being developed or changed, so this is a constant version number.");
                config.WriteLine("OSISVersion=2.1.1");
                config.WriteLine();
                config.WriteLine("# Copyright and licensing information");
                if (projectOptions.publicDomain)
                {
                    config.WriteLine("Copyright=PUBLIC DOMAIN");
                    config.WriteLine("CopyrightNotes=This work is not copyrighted. It is in the Public Domain. You may copy and publish this work freely, but you may not claim copyright on it.");
                    config.WriteLine("DistributionLicense=Public Domain");
                    config.WriteLine("ShortCopyright=PUBLIC DOMAIN");
                }
                else
                {
                    config.WriteLine("Copyright={0}", copyrightNotice);
                    config.WriteLine("CopyrightHolder={0}", projectOptions.copyrightOwner);
                    config.WriteLine("CopyrightDate={0}", yr);
                    config.WriteLine("CopyrightNotes={0}", HTMLtoContinuation(infoPage));
                    string coprAbbrev = projectOptions.copyrightOwnerAbbrev;
                    if (String.IsNullOrEmpty(coprAbbrev))
                        coprAbbrev = projectOptions.copyrightOwner;
                    config.WriteLine("ShortCopyright=© {0} {1}", projectOptions.copyrightYears, coprAbbrev);
                    if (projectOptions.ccbyndnc) // TODO: adjust licensing closer to reality
                    {
                        config.WriteLine("DistributionLicense=Creative Commons: by-nc-nd");
                    }
                    else if (projectOptions.ccbynd)
                    {
                        config.WriteLine("DistributionLicense=Creative Commons: by-nd");
                    }
                    else if (projectOptions.ccbysa)
                    {
                        config.WriteLine("DistributionLicense=Creative Commons: by-sa");
                    }
                    else if (projectOptions.redistributable)
                    {
                        config.WriteLine("DistributionLicense=Copyrighted; Free non-commercial distribution");
                    }
                    else if (projectOptions.downloadsAllowed)
                    {
                        config.WriteLine("DistributionLicense=Copyrighted; Permission to distribute granted to eBible.org");    // TODO: generalize permittee
                    }
                }
                if (!String.IsNullOrEmpty(projectOptions.textSourceUrl))
                {
                    config.WriteLine("TextSource={0}", projectOptions.textSourceUrl);
                }
                if (!String.IsNullOrEmpty(projectOptions.ObsoleteSwordName))
                {
                    config.WriteLine();
                    config.WriteLine("# Old module names made obsolete by this module");
                    foreach (string oldName in oldNames)
                    {
                        if (oldName.ToUpperInvariant() != swordName.ToUpperInvariant())
                            config.WriteLine("Obsoletes={0}", oldName);
                    }
                }
                config.Close();
                projectOptions.Write();

                // Zip the module and recompress the index.
                Utils.DeleteFile(Path.Combine(Path.Combine(baseDir,"zip"),swordName+".zip"));
                //command = "rm";
                //parameters = "zip/" + swordName + ".zip";
                //fileHelper.RunCommand(command, parameters, baseDir);
                command = "zip";
                parameters = "-r -D zip/" + swordName + ".zip mods.d/" + swordName + ".conf modules/texts/ztext/" + swordName;
                fileHelper.RunCommand(command, parameters, baseDir);
                command = "tar";
                parameters = "czvf mods.d.tar.gz mods.d/";
                fileHelper.RunCommand(command, parameters, baseDir);
            }
            catch (Exception ex)
            {
                Logit.WriteError("Error writing Sword config: " + ex.Message + ex.StackTrace);
            }
        }



        protected bool OSISStrongs;
        protected bool OSISLemma;
        protected bool OSISFootnotes;
        protected bool OSISScripref;
        protected bool OSISMorph;
        protected bool OSISHeadings;
        protected bool OSISRedLetterWords;
        protected string shortLang;
        protected SwordVersifications SwordVs;
        public bool commandLineKludge = false;



        static void ValidationCallback(object sender, ValidationEventArgs e)
        {
            Logit.WriteError("OSIS validation error: " + e.Message);
        }


        public bool ValidateMosisFile(string xmlFilePath, string xsdFilePath)
        {
            bool result = false;
            XmlReader reader;

            try
            {
                XmlReaderSettings settings = new XmlReaderSettings();
                settings.ValidationType = ValidationType.Schema;

                XmlSchemaSet schemas = new XmlSchemaSet();
                if (commandLineKludge)
                {
                    schemas.Add("http://www.bibletechnologies.net/2003/OSIS/namespace", xsdFilePath);
                    schemas.Add("http://www.w3.org/XML/1998/namespace", "xml.xsd");
                }

                settings.Schemas = schemas;
                settings.ValidationEventHandler += ValidationCallback;

                using (reader = XmlReader.Create(xmlFilePath, settings))
                {
                    try
                    {
                        while (reader.Read()) { }
                        return true;
                    }
                    catch (XmlException ex)
                    {
                        Logit.WriteError("Invalid OSIS XML: " + ex.Message);
                        return false;
                    }
                    catch (XmlSchemaValidationException ex)
                    {
                        Console.WriteLine("Invalid MOSIS XML: " + ex.Message);
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Logit.WriteError("OSIS validation of " + xmlFilePath + " failed: " + ex.Message);
            }
            return result;
        }

        /// <summary>
        /// Convert USFX file to Modified OSIS for Sword Project import
        /// </summary>
        /// <param name="usfxFileName">Full path and file name of USFX file</param>
        /// <param name="mosisFileName">Full path and file name of MOSIS file</param>
        /// <returns>true iff successful</returns>
        public bool ConvertUsfxToMosis(string usfxFileName, string mosisFileName)
        {
            OsisFileName = mosisFileName;
            mosisNestLevel = 0;
            indentLevel = 0;
            inNote = false;
           // bool inNoteDiv = false;
            bool inToc1 = false;
            bool inToc2 = false;
            bool titleWritten = false;
            bool inStrongs = false;
            bool inPsalmTitle = false;
            bool inCell = false;
            bool inSC = false;
            bool inHi = false;
            bool inTl = false;
            bool inOsisNote = false;
            OSISRedLetterWords = OSISFootnotes = OSISHeadings = OSISMorph = OSISStrongs = OSISScripref = false;
            SwordVs = new SwordVersifications();

            string toc1 = String.Empty;
            string toc2 = String.Empty;
            string lastSfm = String.Empty;
 //           string tagName;
            inIntroduction = inMajorSection = inSection = inSubSection = inTitledPsalm = false;
            int i;
            try
            {
                bookInfo.ReadUsfxVernacularNames(usfxFileName);
                osisFileName = mosisFileName;
                lastNoteVerse = String.Empty;
                noteNumber = serialNumber = 0;
                //langCodes = new LanguageCodeInfo();
                altChapterID = chaptereID = epeID = vpeID = qeID = verseeID = currentTestament = osisVersesId = osisVerseId = String.Empty;
                inPoetryLine = false;

                usfx = new XmlTextReader(usfxFileName);
                usfx.WhitespaceHandling = WhitespaceHandling.All;
                while (usfx.Read())
                {
                    Logit.ShowStatus("converting to mosis " + osisVerseId);
                    if (usfx.NodeType == XmlNodeType.Element)
                    {
                        level = GetNamedAttribute("level");
                        style = GetNamedAttribute("style");
                        sfm = GetNamedAttribute("sfm");
                        caller = GetNamedAttribute("caller");
                        id = GetNamedAttribute("id");
                        strongs = GetNamedAttribute("s");
                        strongMorph = GetNamedAttribute("m");
                        /*if (!strongMorph.StartsWith("strongMorph:"))
                            strongMorph = String.Empty;
                        */
                        if (inToc1 || inToc2)
                        {
                            if ((usfx.Name == "f") || (usfx.Name == "fe") || (usfx.Name == "x") || (usfx.Name == "ef") || (usfx.Name == "ex"))
                            {
                                SkipElement();
                                Logit.WriteLine("Warning: note in title at " + currentBookAbbrev + " not written to OSIS file.");
                            }
                            else if (inToc1 && usfx.Name == "it")
                            {
                                toc1 += "<seg><hi type=\"italic\">";
                            }
                            else
                            {
                                Logit.WriteLine("Warning: " + usfx.Name + " markup in title at " + currentBookAbbrev + " not written to OSIS file");
                            }

                        }
                        else
                        {
                            if (usfx.Name != "v")
                                if (inPsalmTitle)
                                {
                                    inPsalmTitle = false;
                                    EndLineGroup();
                                    SetListLevel(0, true);
                                    if (!usfx.IsEmptyElement)
                                    {
                                        StartTitledPsalm();
                                        StartElementWithAttribute("title", "type", "psalm", "canonical", "true");
                                    }
                                    mosis.WriteString(usfx.Value);
                                }
                           /* tagName = usfx.Name;
                            if ("char para".Contains(tagName))
                            {
                                if (!string.IsNullOrEmpty(sfm))
                                    tagName = sfm;
                                else if (!string.IsNullOrEmpty(style))
                                    tagName = style;
                            }
                           */
                            switch (usfx.Name)
                            {
                                case "languageCode":
                                    usfx.Read();
                                    if (usfx.NodeType == XmlNodeType.Text)
                                        languageCode = usfx.Value;
                                    shortLang = langCodes.ShortCode(languageCode);
                                    osisWorkId = "Bible." + shortLang;
                                    StringBuilder sb = new StringBuilder();
                                    int ix;
                                    for (ix = 0; ix < translationId.Length; ix++)
                                    {
                                        if (Char.IsLetterOrDigit(translationId[ix]))
                                            sb.Append(translationId[ix]);
                                    }
                                    if (translationId.Length > 0)
                                    {
                                        osisWorkId += "." + sb.ToString();
                                    }
                                    osisWorkId = osisWorkId.Replace('-', '.');
                                    OpenMosisFile(mosisFileName);
                                    CheckElementLevel(2, "just wrote header");
                                    break;
                                case "book":
                                    currentBookHeader = currentBookTitle = String.Empty;
                                    toc1 = toc2 = String.Empty;
                                    currentChapter = currentChapterPublished = currentChapterAlternate = String.Empty;
                                    currentVerse = currentVersePublished = currentVerseAlternate = String.Empty;
                                    titleWritten = false;
                                    if (id.Length > 2)
                                    {
                                        currentBookAbbrev = id;
                                        bookRecord = (BibleBookRecord)bookInfo.books[currentBookAbbrev];
                                    }
                                    if ((bookRecord == null) || (id.Length <= 2))
                                    {
                                        Logit.WriteError("Cannot process unknown book: " + currentBookAbbrev);
                                        return false;
                                    }
                                    if (bookRecord.testament == "x")
                                    {   // Skip peripherals in Mosis. Sword can't handle them.
                                        SkipElement();
                                    }
                                    else if ((bookRecord.testament == "a") && !projectOptions.includeApocrypha)
                                    {
                                        SkipElement();
                                    }
                                    else if (!projectOptions.allowedBookList.Contains(bookRecord.tla))// Check for presence of book in bookorder.txt
                                    {
                                        SkipElement();
                                    }
                                    else
                                    {   // We have a book Sword can handle. (OSIS can actually handle anything, including cookie recipes, but it doesn't matter if nobody knows what you mean.)
                                        StartTestament(bookRecord.testament);
                                        osisVersesId = osisVerseId = osisBook = bookRecord.osisName;
                                        chapterNumber = 0;
                                        verseNumber = 0;
                                        StartElementWithAttribute("div", "type", "book", "osisID", osisBook, "canonical", (bookRecord.testament != "x").ToString().ToLower());
                                        CheckElementLevel(4, "opened new book");
                                    }
                                    break;
                                case "add":
                                    if (!(inNote || inSC))
                                        StartElementWithAttribute("transChange", "type", "added");
                                    break;
                                case "bd":
                                case "qac":
                                    StartElementWithAttribute("hi", "type", "bold");
                                    break;
                                case "bdit":
                                    StartElementWithAttribute("hi", "type", "bold");
                                    StartElementWithAttribute("hi", "type", "italic");
                                    break;
                                case "it":
                                    if (!inStrongs)
                                    {
                                        StartElementWithAttribute("hi", "type", "italic");
                                        inHi = true;
                                    }
                                    break;
                                case "em":
                                    StartElementWithAttribute("hi", "type", "emphasis");
                                    inHi = true;
                                    break;
                                case "optionalLineBreak":
                                    // Discard. No true standard equivalent, and not very meaningful in electronic publishing.
                                    break;
                                case "pn":
                                    StartElementWithAttribute("name");
                                    break;
                                case "qt":
                                    StartElementWithAttribute("seg", "type", "otPassage");
                                    break;
                                case "sc":
                                    StartElementWithAttribute("hi", "type", "small-caps");
                                    inSC = true;
                                    break;
                                case "sig":
                                    StartElementWithAttribute("signed");
                                    break;
                                case "sls":
                                    StartElementWithAttribute("foreign", "type", "x-secondaryLanguage");
                                    break;
                                case "tl":
                                    StartElementWithAttribute("foreign");
                                    inTl = true;
                                    break;
                                case "b":
                                    EndLineGroup();
                                    SetListLevel(0, true);
                                    EndIntroduction();
                                    WriteMosisElementString("lb", "");
                                    break;
                                case "bk":
                                    // if (inNote)
                                        StartElementWithAttribute("hi", "type", "italic");
                                    // else NOTE: <reference> tag not used here because embedded reference within this element fails OSIS validation. Real case: pma.
                                    //    StartElementWithAttribute("reference", "type", "x-bookName");
                                    break;
                                case "f":   //  footnote
                                    if (inLineGroup && !inPoetryLine)
                                    {
                                        SkipElement();
                                        Logit.WriteLine("Warning: skipped OSIS footnote outside of line in linegroup at " + osisVerseId);
                                    }
                                    else
                                    {
                                        OSISFootnotes = true;
                                        inOsisNote = true;
                                        StartElementWithAttribute("note", "type", "translation", "osisRef", osisVerseId, "osisID", NoteId());
                                        mosis.WriteAttributeString("placement", "foot");
                                        inNote = true;
                                    }
                                    break;
                                case "fe":  // End note. Rarely used, fortunately, but in the standards.
                                    if (inLineGroup && !inPoetryLine)
                                    {
                                        SkipElement();
                                        Logit.WriteLine("Warning: skipped OSIS endnote outside of line in linegroup at " + osisVerseId);
                                    }
                                    else
                                    {
                                        OSISFootnotes = true;
                                        inOsisNote= true;
                                        StartElementWithAttribute("note", "type", "translation", "osisRef", osisVerseId, "osisID", NoteId());
                                        mosis.WriteAttributeString("placement", "end");
                                        inNote = true;
                                    }
                                    break;
                                case "x":   // Cross references
                                    if (inLineGroup && !inPoetryLine)
                                    {
                                        SkipElement();
                                        Logit.WriteLine("Warning: skipped OSIS cross reference note outside of line in linegroup at " + osisVerseId);
                                    }
                                    else
                                    {
                                        OSISScripref = true;
                                        inOsisNote = true;
                                        StartElementWithAttribute("note", "type", "crossReference", "osisRef", osisVerseId, "osisID", NoteId());
                                        inNote = true;
                                    }
                                    break;
                                case "glo":
                                case "ide":
                                case "fig": // Don't bother with figures and OSIS. Not supported by the readers, so not worth the effort.
                                case "fm":  // Should not actually be in any field texts. Safe to skip.
                                case "idx": // Peripherals - Back Matter Index
                                    SkipElement();
                                    break;
                                case "ie":  // Introduction end
                                    EndIntroduction();
                                    break;
                                case "iex": // Introduction explanatory or bridge text
                                    StartIntroduction();
                                    SkipElement();
                                    break;
                                case "fp":  // Paragraph break within a footnote... but USFM/USFX only mark a point, not a container, so we use a line break
                                    StartElementWithAttribute("lb");
                                    // StartElementWithAttribute("p", "type", "x-footnote");
                                    break;
                                case "fq":
                                    StartElementWithAttribute("q", "who", "unknown", "type", "x-footnote");
                                    break;
                                case "fqa":
                                    StartElementWithAttribute("rdg");
                                    break;
                                case "fl":
                                case "zcr":
                                case "zcb":
                                case "zcg":
                                case "zcy":
                                    // Not supported.
                                    break;
                                case "fr":
                                    OpenContainer("reference", false);
                                    mosis.WriteAttributeString("type", "source");
                                    mosis.WriteAttributeString("osisRef", osisVerseId);
                                    break;
                                case "fk":
                                case "fw":
                                    StartElementWithAttribute("catchWord");
                                    break;
                                case "fv":
                                    StartElementWithAttribute("seg", "type", "verseNumber");
                                    break;
                                case "k":
                                    StartElementWithAttribute("seg", "type", "keyword");
                                    break;
                                case "id":
                                    if (id != currentBookAbbrev)
                                    {
                                        Logit.WriteError("Book ID in <id> and <book> do not match: " + currentBookAbbrev + " is not " + id);
                                    }
                                    SkipElement();  // Strip out comment portion.
                                    break;
                                case "ztoc":
                                case "toc": // Table of Contents entries
                                    if (level == "1")
                                    {
                                        inToc1 = true;
                                    }
                                    else if (level == "2")
                                    {
                                        inToc2 = true;
                                    }
                                    else
                                    {
                                        SkipElement();
                                    }
                                    break;
                                case "usfm":    // Irrelevant to OSIS
                                case "rem": // Comment; not part of the actual text
                                    SkipElement();
                                    break;
                                case "h":
                                    if (!usfx.IsEmptyElement)
                                    {
                                        usfx.Read();
                                        if (usfx.NodeType == XmlNodeType.Text)
                                            currentBookHeader = usfx.Value.Trim();
                                    }
                                    break;
                                case "c":
                                    EndCurrentChapter();
                                    currentChapter = id;
                                    currentChapterPublished = fileHelper.LocalizeDigits(currentChapter);
                                    // Special case for AddPs chapter 151: make it chapter 1 to make CrossWire happy.
                                    if ((osisBook == "AddPs") && (id == "151"))
                                    {
                                        currentChapter = id = "1";
                                    }
                                    currentChapterAlternate = String.Empty;
                                    currentVerse = currentVersePublished = currentVerseAlternate = String.Empty;
                                    verseNumber = 0;
                                    if (!usfx.IsEmptyElement)
                                    {
                                        usfx.Read();
                                        if (usfx.NodeType == XmlNodeType.Text)
                                            currentChapterPublished = fileHelper.LocalizeDigits(usfx.Value.Trim());
                                        if (usfx.NodeType != XmlNodeType.EndElement)
                                            usfx.Read();
                                    }
                                    int chNum;
                                    if (Int32.TryParse(id, out chNum))
                                        chapterNumber = chNum;
                                    else
                                        chapterNumber++;
                                    bookRecord.actualChapters = Math.Max(bookRecord.actualChapters, chapterNumber);
                                    osisVersesId = osisVerseId = osisBook + "." + currentChapter;
                                    chaptereID = StartId();
                                    mosis.WriteString(Environment.NewLine);
                                    StartMosisElement("chapter");
                                    mosis.WriteAttributeString("osisID", osisVerseId);
                                    mosis.WriteAttributeString("sID", chaptereID);
                                    mosis.WriteAttributeString("n", currentChapterPublished);
                                    WriteMosisEndElement();
                                    break;
                                case "cl":
                                    if (chapterNumber == 0)
                                    {
                                        SkipElement();  // There is no standard OSIS equivalent to cl before chapter 1.
                                    }
                                    else
                                    {   // Per OSIS User Manual of 6 March 2006, page 130, the title type should be "chatperLabel" [sic] instead of "chapter".
                                        // I'm assuming that was a typo. The OSIS Schema requires "chapter" instead.
                                        SetListLevel(0, true);
                                        EndLineGroup();
                                        StartElementWithAttribute("title", "type", "chapter");
                                    }
                                    break;
                                case "ca":
                                    EndIntroduction();
                                    SkipElement();
                                    /* This feature is not supported by The Sword Project.
                                    altChapterID = StartId();
                                    if (!usfx.IsEmptyElement)
                                    {
                                        usfx.Read();
                                        if (usfx.NodeType == XmlNodeType.Text)
                                        {
                                            currentChapterAlternate = usfx.Value.Trim();
                                            usfx.Read();
                                            if (usfx.NodeType != XmlNodeType.EndElement)
                                            {
                                                Logit.WriteError("Unexpected node type after <ca> text at " + osisVerseId + ": " + usfx.NodeType.ToString());
                                            }
                                            else if (usfx.Name != "ca")
                                            {
                                                Logit.WriteError("Unexpected node name after <ca> text at " + osisVerseId + ": " + usfx.Name);
                                            }
                                            else
                                            {
                                                StartElementWithAttribute("chapter", "osisRef", osisVerseId, "type", "x-alternate", "n", currentChapterAlternate);
                                            }
                                        }
                                        else
                                        {
                                            Logit.WriteError("ca is empty at " + osisVerseId);
                                        }
                                    }
                                    */
                                    break;
                                case "cp":
                                    SkipElement();
                                    /* This feature is not supported by the Sword Project.
                                    epeID = StartId();
                                    if (!usfx.IsEmptyElement)
                                    {
                                        usfx.Read();
                                        if (usfx.NodeType == XmlNodeType.Text)
                                        {
                                            currentChapterPublished = usfx.Value.Trim();
                                            usfx.Read();
                                            if (usfx.NodeType != XmlNodeType.EndElement)
                                            {
                                                Logit.WriteError("Unexpected node type after <cp> text at " + osisVerseId + ": " + usfx.NodeType.ToString());
                                            }
                                            else if (usfx.Name != "cp")
                                            {
                                                Logit.WriteError("Unexpected node name after <cp> text at " + osisVerseId + ": " + usfx.Name);
                                            }
                                            else
                                            {
                                                StartElementWithAttribute("chapter", "osisRef", osisVerseId, "type", "x-published", "n", currentChapterPublished);
                                            }
                                        }
                                        else
                                        {
                                            Logit.WriteError("cp is empty at " + osisVerseId);
                                        }
                                    }
                                    */
                                    break;
                                case "v":
                                    if (inPsalmTitle)
                                    {
                                        inPsalmTitle = false;
                                        SetListLevel(0, true);
                                        EndIntroduction();
                                        if ((verseNumber == 0) && (chapterNumber == 1) && !inMajorSection && !inSection && !inSubSection && !inTitledPsalm)
                                        {
                                            StartMajorSection();
                                        }
                                        if (level == String.Empty)
                                            level = "1";
                                        if ((level == "1") || !inLineGroup)
                                        {
                                            StartLineGroup();
                                        }
                                        StartMosisElement("l");
                                        mosis.WriteAttributeString("level", "1");
                                        inPoetryLine = true;
                                    }
                                    EndIntroduction();
                                    EndCurrentVerse();
                                    currentVersePublished = fileHelper.LocalizeDigits(id);
                                    currentVerse = id.Replace("\u200F", "");    // Strip out RTL character
                                    currentVerseAlternate = "";
                                    if (!usfx.IsEmptyElement)
                                    {
                                        usfx.Read();
                                        if (usfx.NodeType == XmlNodeType.Text)
                                        {
                                            currentVersePublished = fileHelper.LocalizeDigits(usfx.Value.Trim());
                                        }
                                        if (usfx.NodeType != XmlNodeType.EndElement)
                                            usfx.Read();

                                    }
                                    int vnum;
                                    int dashPlace = currentVerse.IndexOf('-');
                                    string firstVerseInBridge = id;
                                    if (dashPlace > 0)
                                    {
                                        firstVerseInBridge = firstVerseInBridge.Substring(0, dashPlace);
                                    }
                                    if (Int32.TryParse(firstVerseInBridge, out vnum))
                                    {
                                        verseNumber = vnum;
                                    }
                                    else
                                    {
                                        verseNumber++;
                                    }
                                    int endRange = verseNumber;
                                    osisVersesId = osisVerseId = osisBook + "." + currentChapter + "." + firstVerseInBridge;
                                    if (dashPlace > 0)
                                    {   // -1 = no verse range; 0 = bad syntax; 1 or more = verse range
                                        if (Int32.TryParse(id.Substring(dashPlace + 1), out endRange))
                                        {
                                            for (i = verseNumber + 1; i <= endRange; i++)
                                            {
                                                osisVersesId += " " + osisBook + "." + currentChapter + "." + i.ToString();
                                            }
                                        }
                                    }
                                    verseeID = StartId();
                                    mosis.WriteString(Environment.NewLine);
                                    StartMosisElement("verse");
                                    mosis.WriteAttributeString("osisID", osisVersesId);
                                    mosis.WriteAttributeString("sID", verseeID);
                                    mosis.WriteAttributeString("n", currentVersePublished);
                                    WriteMosisEndElement();    // verse
                                    CheckMinimumLevel(5, "starting verse " + osisVersesId + " / " + osisVerseId);
                                    SwordVs.IsIncluded(currentBookAbbrev, chapterNumber, verseNumber);
                                    if (endRange > verseNumber)
                                    {
                                        SwordVs.IsIncluded(currentBookAbbrev, chapterNumber, endRange);
                                    }
                                    break;
                                case "va":  // Not supported by The Sword Project
                                    SkipElement();
                                    break;
                                case "vp":
                                    EndIntroduction();
                                    SkipElement();
                                    /* This feature is not supported by The Sword Project.
                                    if (!usfx.IsEmptyElement)
                                    {
                                        usfx.Read();
                                        if (usfx.NodeType == XmlNodeType.Text)
                                        {
                                            currentVersePublished = usfx.Value.Trim();
                                            if (currentVersePublished.Length > 0)
                                            {
                                                vpeID = StartId();
                                                StartMosisElement("verse");
                                                mosis.WriteAttributeString("osisID", osisVerseId);
                                                mosis.WriteAttributeString("sID", verseeID);
                                                mosis.WriteAttributeString("n", currentVersePublished);
                                                WriteMosisEndElement();    // verse
                                            }
                                        }
                                    }
                                     */
                                    break;
                                case "ve":
                                    EndCurrentVerse();
                                    break;
                                case "w":
                                case "zw":
                                    if (!usfx.IsEmptyElement)
                                    {
                                        if (!String.IsNullOrEmpty(strongs))
                                        {
                                            StartElementWithAttribute("w", "lemma", ("strong:" + strongs.Trim()).Replace(" G", " strong:G").Replace(" H", " strong:H"));
                                            OSISStrongs = true;
                                            inStrongs = true;
                                            if (strongs.Contains("lemma"))
                                                OSISLemma = true;
                                        }
                                        if (!String.IsNullOrEmpty(strongMorph))
                                        {
                                            OSISMorph = true;
                                            if (!inStrongs)
                                            {
                                                StartElementWithAttribute("w", "morph", strongMorph);
                                                inStrongs = true;
                                            }
                                            else
                                            {
                                                mosis.WriteAttributeString("morph", strongMorph);
                                            }
                                        }
                                    }
                                    break;
                                case "d":
                                case "qd":
                                    // Compensate for Sword defect: Psalm descriptive title as verse one
                                    // loses canonical data if the descriptive title ends before verse one ends.
                                    // Therefore, in this case, we change paragraph type to \q1 (<l>).
                                    inPsalmTitle = true;
                                    break;
                                case "nd":
                                    if (!inCell)
                                        StartElementWithAttribute("seg");
                                    StartElementWithAttribute("divineName");
                                    break;
                                case "no":
                                    StartElementWithAttribute("hi", "type", "normal");
                                    break;
                                case "s":
                                    SetListLevel(0, true);
                                    EndLineGroup();
                                    EndIntroduction();
                                    if (!usfx.IsEmptyElement)
                                    {
                                        if ((level == "1") || (level == ""))
                                        {
                                            StartSection();
                                        }
                                        else // Collapsing levels 2 and 3 to subsection.
                                        {
                                            StartSubSection();
                                        }
                                        StartElementWithAttribute("title", "type", "sub", "canonical", "false");
                                    }
                                    OSISHeadings = true;
                                    break;
                                case "q":
                                    EndIntroduction();
                                    SetListLevel(0, true);
                                    if ((verseNumber == 0) && (chapterNumber == 1) && !inMajorSection && !inSection && !inSubSection && !inTitledPsalm)
                                    {
                                        StartMajorSection();
                                    }
                                    if (level == String.Empty)
                                        level = "1";
                                    if (!inLineGroup)
                                    {
                                        StartLineGroup();
                                    }
                                    OpenContainer("l", false);
                                    mosis.WriteAttributeString("level", level);
                                    if (usfx.IsEmptyElement)
                                    {
                                        CloseContainer("l");
                                        inPoetryLine = false;
                                    }
                                    else
                                    {
                                        inPoetryLine = true;
                                    }
                                    break;
                                case "qr":
                                    EndIntroduction();
                                    SetListLevel(0, true);
                                    if ((verseNumber == 0) && (chapterNumber == 1) && !inMajorSection && !inSection && !inSubSection && !inTitledPsalm)
                                    {
                                        StartMajorSection();
                                    }
                                    level = "4";
                                    if (!inLineGroup)
                                    {
                                        StartLineGroup();
                                    }
                                    OpenContainer("l", false);
                                    mosis.WriteAttributeString("level", level);
                                    if (!usfx.IsEmptyElement)
                                        inPoetryLine = true;
                                    break;
                                case "qs":
                                    EndIntroduction();
                                    SetListLevel(0, true);
                                    if ((verseNumber == 0) && (chapterNumber == 1) && !inMajorSection && !inSection && !inSubSection && !inTitledPsalm)
                                    {
                                        StartMajorSection();
                                    }
                                    if (level == String.Empty)
                                        level = "1";
                                    if (inLineGroup)
                                    {
                                        OpenContainer("l", false);
                                        mosis.WriteAttributeString("type", "selah");
                                        if (!usfx.IsEmptyElement)
                                            inPoetryLine = true;
                                    }
                                    break;
                                case "ref":
                                    string tgt = GetNamedAttribute("tgt");  // OSIS supports verse reference targets, but not web href links.
                                    if (!usfx.IsEmptyElement)
                                    {
                                        if ((tgt.Length > 6) && (!tgt.Contains("-")))
                                        {
                                            string[] bcv = tgt.Split(new Char[] { '.' });
                                            if (bcv.Length >= 3)
                                            {
                                                OpenContainer("reference", false);
                                                mosis.WriteAttributeString("osisRef", bookInfo.OsisID(bcv[0]) + "." + bcv[1] + "." + bcv[2]);
                                                inReference = true;
                                            }
                                        }
                                    }
                                    break;
                                case "table":
                                    SetListLevel(0, true);
                                    OpenContainer("table", false);
                                    break;
                                case "tr":
                                    OpenContainer("row", false);
                                    break;
                                case "th":
                                    StartElementWithAttribute("cell", "align", "left", "type", "x-header");
                                    inCell = true;
                                    break;
                                case "thr":
                                    StartElementWithAttribute("cell", "align", "right", "type", "x-header");
                                    inCell = true;
                                    break;
                                case "tc":
                                    StartElementWithAttribute("cell", "align", "left");
                                    inCell = true;
                                    break;
                                case "tcr":
                                    StartElementWithAttribute("cell", "align", "right");
                                    inCell = true;
                                    break;
                                case "periph":
                                    SkipElement();
                                    break;
                                case "milestone":
                                    StartElementWithAttribute("milestone", "type", "x-" + sfm);
                                    Logit.WriteLine("Warning: milestone encountered at " + osisVerseId);
                                    break;
                                case "p":
                                    lastSfm = sfm;
                                    if (sfm != "iq")
                                    {
                                        EndLineGroup();
                                    }
                                    if (level == String.Empty)
                                        level = "1";
                                    if ((sfm != "li") && (sfm != "ili"))
                                    {
                                        SetListLevel(0, true);
                                    }
                                    if (sfm == "mt")
                                    {
                                        if (!titleWritten)
                                        {
                                            StartMosisElement("title");
                                            mosis.WriteAttributeString("type", "main");
                                            toc1 = toc1.Trim();
                                            toc2 = toc2.Trim();
                                            if (currentBookHeader.Length > 0)
                                            {
                                                mosis.WriteAttributeString("short", currentBookHeader);
                                            }
                                            else if (toc2.Length > 0)
                                            {
                                                mosis.WriteAttributeString("short", toc2);
                                            }
                                            if (toc1.Length > 0)
                                                mosis.WriteString(toc1);
                                            else if (toc2.Length > 0)
                                                mosis.WriteString(toc2);
                                            else
                                                mosis.WriteString(currentBookHeader);
                                            WriteMosisEndElement();
                                            titleWritten = true;
                                        }
                                        SkipElement();
                                    }
                                    else
                                    {
                                        switch (sfm)
                                        {
                                            case "cd":
                                            case "intro":
                                                StartIntroduction();
                                                StartElementWithAttribute("p", "canonical", "false");
                                                break;
                                            case "nb":
                                                StartElementWithAttribute("p");
                                                break;
                                            case "m":
                                            case "pi":
                                            case "":
                                                EndIntroduction();
                                                if ((verseNumber == 0) && (chapterNumber == 1) && !inMajorSection && !inSection && !inSubSection && !inTitledPsalm)
                                                {
                                                    StartMajorSection();
                                                }
                                                StartElementWithAttribute("p");
                                                break;
                                            case "cls":
                                                EndIntroduction();
                                                StartElementWithAttribute("closer");
                                                break;
                                            case "hr":  // Horizontal rule not supported. Try a line break.
                                                EndIntroduction();
                                                StartElementWithAttribute("lb");
                                                break;
                                            case "ie":
                                                SkipElement();
                                                EndIntroduction();
                                                break;
                                            case "ib":
                                                EndLineGroup();
                                                SetListLevel(0, true);
                                                StartIntroduction();
                                                StartElementWithAttribute("lb");
                                                break;
                                            case "im":
                                                StartIntroduction();
                                                StartElementWithAttribute("p", "canonical", "false");
                                                break;
                                            case "imq":
                                            case "imi":
                                            case "ip":
                                            case "ipi":
                                            case "ipq":
                                            case "ipr":
                                            case "iot":
                                                StartIntroduction();
                                                StartElementWithAttribute("p", "canonical", "false");
                                                break;
                                            case "keyword":
                                                StartElementWithAttribute("seg", "type", "keyword");
                                                break;
                                            case "iq":
                                                SetListLevel(0, true);
                                                StartIntroduction();
                                                if (level == String.Empty)
                                                    level = "1";
                                                if ((level == "1") || !inLineGroup)
                                                {
                                                    StartLineGroup();
                                                }
                                                OpenContainer("l", false);
                                                mosis.WriteAttributeString("level", level);
                                                mosis.WriteAttributeString("canonical", "false");
                                                if (!usfx.IsEmptyElement)
                                                    inPoetryLine = true;
                                                break;
                                            case "imte":
                                            case "imt":
                                                StartIntroduction();
                                                StartElementWithAttribute("title", "type", "main", "canonical", "false", "level", level);
                                                break;
                                            case "is":
                                                StartIntroduction();
                                                StartElementWithAttribute("title", "type", "sub", "canonical", "false");
                                                break;
                                            case "io":  // TODO: implement levels with nested list/item/list/item...
                                                StartIntroduction();
                                                StartElementWithAttribute("div", "type", "outline", "canonical", "false");
                                                break;
                                            case "ior":
                                                StartElementWithAttribute("hi", "type", "italic");
                                                break;
                                            case "ili":
                                                StartIntroduction();
                                                if (level == String.Empty)
                                                    level = "1";
                                                indentLevel = int.Parse(level.Trim());
                                                SetListLevel(indentLevel, false);
                                                break;
                                            case "li":
                                                EndIntroduction();
                                                if (level == String.Empty)
                                                    level = "1";
                                                indentLevel = int.Parse(level.Trim());
                                                SetListLevel(indentLevel, true);
                                                break;
                                            case "r":
                                                EndIntroduction();
                                                StartElementWithAttribute("title", "type", "parallel");
                                                break;
                                            case "sp":
                                                SetListLevel(0, true);
                                                EndLineGroup();
                                                EndIntroduction();
                                                if (!usfx.IsEmptyElement)
                                                {
                                                    StartElementWithAttribute("title", "type", "sub", "canonical", "false");
                                                }
                                                break;
                                            case "ms":
                                                if (!usfx.IsEmptyElement)
                                                {
                                                    StartMajorSection();
                                                    StartElementWithAttribute("title", "canonical", "false");
                                                }
                                                break;
                                            case "mr":
                                                StartElementWithAttribute("title", "canonical", "false", "type", "scope");
                                                break;
                                            default:
                                                if (!sfm.StartsWith("i"))
                                                    EndIntroduction();
                                                StartElementWithAttribute("p", "type", "x-" + sfm);
                                                break;
                                        }
                                    }
                                    break;
                                case "rq":
                                    if (inLineGroup)
                                    {   // OSIS can't handle notes in line groups or other notes.
                                        SkipElement();
                                    }
                                    else if (!inOsisNote)
                                    {
                                        OpenContainer("note", false);
                                        mosis.WriteAttributeString("type", "crossReference");
                                        mosis.WriteAttributeString("osisRef", osisVerseId);
                                        mosis.WriteAttributeString("osisID", NoteId());
                                        mosis.WriteAttributeString("placement", "inline");
                                        inNote = true;
                                    }
                                    // TODO: parse contents to surround with <reference osisRef="..."> element.
                                    break;
                                case "cs":  // Rare or new character style: don't know what it should be, so throw away tag & keep text.
                                    break;
                                case "gw":  // Do nothing. Not sure what to do with glossary words, yet.
                                case "xt":  // Do nothing. This tag is meaningless in OSIS.
                                case "xta":
                                case "wh":
                                case "wg":
                                case "wa":
                                    break;
                                case "wj":
                                    if ((!inHi) && (!inTl))
                                    {
                                        StartMosisElement("q");
                                        mosis.WriteAttributeString("who", "Jesus");
                                        mosis.WriteAttributeString("marker", String.Empty);
                                        OSISRedLetterWords = true;
                                    }
                                    break;
                                case "ft":
                                    // Ignore. It does nothing useful, but is an artifact of USFM exclusive character styles.
                                    break;
                                case "usfx":
                                    // Nothing to do, here. (Already covered by OSIS declaration.)
                                    break;
                                case "xo":  // Origin reference
                                    StartElementWithAttribute("reference", "osisRef", osisVerseId);
                                    break;
                                case "xk":  // Not supported in OSIS.
                                case "xq":  // Not useful for Sword modules.
                                case "char":    // Not supported by Sword.
                                    // StartElementWithAttribute("q", "marker", "");
                                    break;
                                case "sup":
                                case "ord":
                                    StartElementWithAttribute("hi", "type", "super");
                                    break;
                                case "dc":
                                case "xdc":
                                case "fdc":
                                    if (!projectOptions.includeApocrypha)
                                        SkipElement();
                                    break;
                                case "ior":
                                    StartElementWithAttribute("hi", "type", "italic");
                                    break;
                                case "iot":
                                case "io":  // TODO: implement levels with nested list/item/list/item...
                                    StartIntroduction();
                                    StartElementWithAttribute("div", "type", "outline", "canonical", "false");
                                    break;
                                default:
                                    Logit.WriteLine("Unhandled tag: " + usfx.Name + " at " + osisVerseId);
                                    break;
                            }
                        }
                    }
                    else if (usfx.NodeType == XmlNodeType.EndElement)
                    {
                        if (inToc1 || inToc2)
                        {
                            if (usfx.Name == "toc")
                            {
                                inToc2 = inToc1 = false;
                            }
                            else if (inToc1 && usfx.Name == "it")
                            {
                                toc1 += "</hi></seg>";
                                inHi = false;
                            }
                            else
                            {
                                Logit.WriteLine("Warning: " + usfx.Name + " end markup in title at " + currentBookAbbrev + " not written to OSIS file");
                            }

                        }
                        else
                        {

                            switch (usfx.Name)
                            {
                                case "w":
                                case "zw":
                                    if (inStrongs)
                                    {
                                        WriteMosisEndElement();
                                        inStrongs = false;
                                    }
                                    break;
                                case "wj":
                                    if ((!inHi) && (!inTl))
                                    {
                                        WriteMosisEndElement();    // q
                                    }
                                    break;
                                case "book":
                                    EndLineGroup();
                                    EndCurrentVerse();
                                    EndCurrentChapter();
                                    EndIntroduction();
                                    EndMajorSection();
                                    if (mosisNestLevel > 3)
                                        WriteMosisEndElement();  // div type="book"
                                    else
                                        Logit.WriteWarning("Warning writing MOSIS: nest level is " + mosisNestLevel.ToString() + ", but expected level is 4 at " + osisVerseId);
                                    CheckElementLevel(3, "closed book");
                                    break;
                                case "bdit":
                                    WriteMosisEndElement();    // hi italic
                                    WriteMosisEndElement();    // hi bold
                                    break;
                                case "p":
                                    CheckMinimumLevel(5, "Ending " + usfx.Name + " " + osisVerseId);
                                    inNote = false;
                                    if (lastSfm == "iq")
                                    {
                                        CloseContainer("l");
                                    }
                                    else
                                    if ((lastSfm == "li") || (lastSfm == "ili"))
                                    {
                                        SetListLevel(0);
                                    }
                                    else
                                    {
                                        WriteMosisEndElement();
                                    }
                                    break;
                                case "qr":
                                case "q":
                                    CloseContainer("l");
                                    inPoetryLine = false;
                                    break;
                                case "ref":
                                    if (inReference)
                                    {
                                        CloseContainer("reference");
                                        inReference = false;
                                    }
                                    break;
                                case "fe":
                                case "f":
                                case "x":
                                    inOsisNote = false;
                                    if (inNote)
                                    {
                                        inNote = false;
                                        WriteMosisEndElement(); // End of note
                                    }
                                    break;
                                case "add":
                                    if (!(inNote || inSC))
                                        WriteMosisEndElement();
                                    break;
                                case "qs":
                                    if (inLineGroup)
                                    {
                                        CloseContainer("l");
                                        inPoetryLine = false;
                                    }

                                    break;
                                case "table":
                                    CloseContainer("table");
                                    break;
                                case "tr":
                                    CloseContainer("row");
                                    break;
                                case "rq":
                                    if (!inOsisNote)
                                        CloseContainer("note");
                                    break;
                                case "fr":
                                    CloseContainer("reference");
                                    break;
                                case "iq":
                                    CloseContainer("l");
                                    break;
                                case "em":
                                    WriteMosisEndElement();    // note, hi, reference, title, l, transChange, etc.
                                    inHi = false;
                                    break;
                                case "tl":
                                    WriteMosisEndElement();    // note, hi, reference, title, l, transChange, etc.
                                    inTl = false;
                                    break;
                                case "bd":
                                case "bk":
                                case "cl":
                                case "d":
                                case "qd":
                                case "dc":
                                case "fk":
                                case "fp":
                                case "fq":
                                case "fqa":
                                case "fv":
                                case "fw":
                                case "k":
                                case "no":
                                case "pn":
                                case "qac":
                                case "qt":
                                case "r":
                                case "s":
                                case "sig":
                                case "sls":
                                case "xo":
                                case "sup":
                                case "ord":
                                case "iot":
                                case "ior":
                                case "io":
                                    // case "xq": Not useful for Sword modules.
                                    WriteMosisEndElement();    // note, hi, reference, title, l, transChange, etc.
                                    break;
                                case "tc":
                                case "tcr":
                                case "th":
                                case "thr":
                                    WriteMosisEndElement();
                                    inCell = false;
                                    break;
                                case "sc":
                                    inSC = false;
                                    WriteMosisEndElement();
                                    break;
                                case "it":
                                    if (!inStrongs)
                                    {
                                        WriteMosisEndElement();
                                        inHi = false;
                                    }
                                    break;
                                case "nd":
                                    WriteMosisEndElement(); // divineName
                                    if (!inCell)
                                        WriteMosisEndElement(); // seg
                                    break;
                                case "xk":
                                case "fl":
                                case "zcr":
                                case "zcb":
                                case "zcg":
                                case "zcy":
                                case "xt":
                                case "gw":
                                case "wa":
                                    // not supported.
                                    break;
                                    /* Can't get to this case (caught in "if" above)
                                case "toc":
                                    inToc2 = inToc1 = false;
                                    break;*/
                            }
                        }
                    }
                    else if (((usfx.NodeType == XmlNodeType.Text) || (usfx.NodeType == XmlNodeType.SignificantWhitespace) || (usfx.NodeType == XmlNodeType.Whitespace)) && !ignore)
                    {
                        if (inToc1)
                            toc1 = toc1 + usfx.Value;
                        else if (inToc2)
                            toc2 = toc2 + usfx.Value;
                        else if (inPsalmTitle && (usfx.NodeType == XmlNodeType.Text))
                        {
                            inPsalmTitle = false;
                            EndLineGroup();
                            SetListLevel(0, true);
                            if (!usfx.IsEmptyElement)
                            {
                                StartTitledPsalm();
                                StartElementWithAttribute("title", "type", "psalm", "canonical", "true");
                            }
                            mosis.WriteString(usfx.Value);
                        }
                        else
                            mosis.WriteString(usfx.Value);
                    }
                }
                Logit.ShowStatus("writing " + mosisFileName);
                CloseMosisFile();

                string mosisDirectory = Path.GetDirectoryName(mosisFileName);
                WriteLocaleFile(Path.Combine(mosisDirectory, "locale-" + languageCode + ".conf"));

                // Validate this file against the Schema
                fileHelper.DebugWrite("reading OSIS Schema and " + mosisFileName);
                string schemaFullPath = SFConverter.FindAuxFile(localOsisSchema);
                
                string schemaDir = Path.GetDirectoryName(schemaFullPath);
                if (schemaDir.Trim().Length > 0)
                    Directory.SetCurrentDirectory(schemaDir);
                string localSchemaName = Path.Combine(mosisDirectory, localOsisSchema);
                fileHelper.DebugWrite("Copying " + schemaFullPath + " to " + localSchemaName);
                File.Copy(schemaFullPath, localSchemaName);
                string xmlxsdPath = SFConverter.FindAuxFile("xml.xsd");
                string localXmlXsdPath = Path.Combine(mosisDirectory, "xml.xsd");
                fileHelper.DebugWrite("Copying "+ xmlxsdPath + " to " + localXmlXsdPath);
                File.Copy(xmlxsdPath, localXmlXsdPath);
                
                ValidateMosisFile(mosisFileName, Path.GetFileName(schemaFullPath));
                /*

                osisVerseId = "header";
                lastElementWritten = "validating MOSIS file";
                currentElement = "";
                string schemaFullPath = SFConverter.FindAuxFile(localOsisSchema);
                string schemaDir = Path.GetDirectoryName(schemaFullPath);
                string localSchemaName = Path.Combine(mosisDirectory, localOsisSchema);
                fileHelper.DebugWrite("Copying " + schemaFullPath + " to " + localSchemaName);
                File.Copy(schemaFullPath, localSchemaName);

                XmlReaderSettings settings = new XmlReaderSettings();
                settings.Schemas.Add(osisNamespace, osisSchema);
                settings.ValidationType = ValidationType.Schema;
                settings.ValidationFlags = XmlSchemaValidationFlags.ReportValidationWarnings;
                settings.ValidationEventHandler += new ValidationEventHandler(ValidationCallBack);
                fileHelper.DebugWrite("Creating validating reader for " + mosisFileName);
                XmlReader mr = XmlTextReader.Create(mosisFileName, settings);
                fileHelper.DebugWrite("Reading " + mosisFileName);

                while (mr.Read())
                {
                    if (mr.NodeType == XmlNodeType.Element)
                    {
                        currentElement = mr.Name;
                        string verseId = mr.GetAttribute("osisID");
                        if (verseId != null)
                        {
                            osisVerseId = verseId;
                            Logit.ShowStatus("validating mosis " + osisVerseId);
                        }
                    }
                }
                mr.Close();
                */
                fileHelper.DebugWrite("Deleting " + localSchemaName + " and "+localXmlXsdPath);
                Utils.DeleteFile(localSchemaName);
                Utils.DeleteFile(localXmlXsdPath);
                if (projectOptions != null)
                {
                    WriteSwordConfig();
                }
            }
            catch (Exception ex)
            {
                Logit.WriteError("Exception at " + osisVerseId + " " + lastElementWritten);
                Logit.WriteError(ex.Message + Environment.NewLine + ex.StackTrace);
            }
            return true;
        }
    }
}
