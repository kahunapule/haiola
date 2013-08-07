using System;
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
    // and not recommended for archival use, because some of the non-Scripture features of the
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
        protected bool eatPoetryLineEnd = false;
        protected bool mtStarted = false;
        protected int listLevel = 0;
        protected int itemLevel = 0;
        protected int indentLevel = 0;
        protected string osisFileName;
        protected string currentElement;
        protected string strongs;
        protected string lastElementWritten = String.Empty;
        protected int mosisNestLevel = 0;

        public static ArrayList bookList = new ArrayList();

        public LanguageCodeInfo langCodes;


        protected void StartMosisElement(string elementName)
        {
            mosis.WriteStartElement(elementName);
            lastElementWritten = "<" + elementName;
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
        }

        protected void CheckElementLevel(int level, string msg)
        {
            if (level != mosisNestLevel)
            {
                Logit.WriteError("Error writing MOSIS: nest level is " + mosisNestLevel.ToString() + ", but expected level is " + level.ToString() + " at " + osisVerseId + " " + msg);
                Logit.WriteError("Last element written: " + lastElementWritten);
            }
        }

        protected void CheckMinimumLevel(int level, string msg)
        {
            if (mosisNestLevel < level)
            {
                Logit.WriteError("Error writing MOSIS: nest level is " + mosisNestLevel.ToString() + ", but expected level is >= " + level.ToString() + " at " + osisVerseId + " " + msg);
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
            while (listLevel > level)
            {   // Let the XmlTextWriter sort out the actual order of the list and item end elements.
                listLevel--;
                WriteMosisEndElement(); // list or item
            }
            while (itemLevel > level)
            {
                itemLevel--;
                WriteMosisEndElement(); // item or list
            }

            while (listLevel < level)
            {
                if (itemLevel < listLevel)
                {
                    itemLevel++;
                    StartMosisElement("item");
                }
                listLevel++;
                StartMosisElement("list");
                if (!canonical)
                {
                    mosis.WriteAttributeString("canonical", "false");
                }
            }
            if (itemLevel < level)
            {
                StartMosisElement("item");
                itemLevel++;
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
        protected const string osisSchema = "http://www.bibletechnologies.net/osisCore.2.1.1.xsd";
        protected const string osisNamespace = "http://www.bibletechnologies.net/2003/OSIS/namespace";

        protected void OpenMosisFile(string mosisFileName)
        {
            string schemaPath = Path.Combine(Path.GetDirectoryName(mosisFileName), localOsisSchema);
            if (!File.Exists(schemaPath))
                File.Copy(SFConverter.FindAuxFile(localOsisSchema), schemaPath);
            
            mosis = new XmlTextWriter(mosisFileName, Encoding.UTF8);
            mosis.Formatting = Formatting.None;
            mosis.WriteStartDocument();
            StartMosisElement("osis");
            mosis.WriteAttributeString("xmlns", osisNamespace);
            mosis.WriteAttributeString("xmlns:xsi", "http://www.w3.org/2001/XMLSchema-instance");
            mosis.WriteAttributeString("xsi:schemaLocation", osisNamespace + " " + osisSchema);
            StartMosisElement("osisText");
            mosis.WriteAttributeString("osisIDWork", osisWorkId);
            mosis.WriteAttributeString("osisRefWork", "bible");
            string osisLang = langCodes.ShortCode(languageCode);
            if (osisLang.Length > 2)
                osisLang = "x-" + osisLang;
            mosis.WriteAttributeString("xml:lang", osisLang);
            mosis.WriteAttributeString("canonical", "true");
            StartMosisElement("header");
            StartMosisElement("revisionDesc");
            WriteMosisElementString("date", OsisDateTime(DateTime.UtcNow));
            WriteMosisElementString("p",
@"This Modified OSIS file was generated from a USFX source as part of the process to convert to a SWORD module using Haiola
 open source software from http://haiola.org.
 Some non-canonical parts of the USFX source file that are not currently supported by The Sword Project (http://crosswire.org) were
 stripped out, as were some of the pure presentational elements that make no sense in a Bible study program,
 but the canonical Scripture text and punctuation remain. Sometimes more than one source format markup was mapped to the same
 OSIS markup, which may result in some minor formatting changes. The usage of the <q> element in this file is not compliant with
 the OSIS User Manual in that <q> markup is stopped and restarted at all verse boundaries, and NEVER intended to trigger generation
 of any punctuation. ALL correct punctuation for quotations for this language, dialect, and translation style, if any, are already
 in the text of the Scriptures. This is why the n attribute of <q> is always the empty string. Because of these limitations, this
 file may be good to include in archives, BUT not to the exclusion of the source USFM, USFX, or USX file(s) from which this Modified
 OSIS file was generated.
");
            WriteMosisEndElement();
            StartMosisElement("revisionDesc");
            WriteMosisElementString("date", OsisDateTime(revisionDateTime));
            WriteMosisElementString("p", "created or updated OSIS file contents");
            WriteMosisEndElement();    // revisionDesc
            StartMosisElement("work");    // Insert Dublin Core identity here.
            mosis.WriteAttributeString("osisWork", osisWorkId);
            WriteElementAndAttributeStringsIfNotEmpty("title", vernacularTitle, "type", "x-vernacular");
            WriteElementStringIfNotEmpty("contributor", contentContributor);
            WriteElementStringIfNotEmpty("creator", contentCreator);
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
            WriteMosisEndElement();    // header
        }

        protected void CloseMosisFile()
        {
            EndTestament();
            WriteMosisEndElement();    // osisText
            WriteMosisEndElement();    // osis
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
                WriteMosisEndElement();    // div type="bookGroup"
                currentTestament = String.Empty;
            }
        }

        protected void StartTestament(string testament)
        {
            if (currentTestament != testament)
            {
                EndTestament();
                StartMosisElement("div");
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
                WriteMosisEndElement();    // lg
                inLineGroup = false;
            }
        }

        protected void StartLineGroup()
        {
            EndLineGroup();
            StartMosisElement("lg");
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
                StartElementWithAttribute("div");
                inTitledPsalm = true;
            }
        }

        protected void EndTitledPsalm()
        {
            if (inTitledPsalm)
            {
                WriteMosisEndElement();
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
                StartElementWithAttribute("div", "type", "majorSection");
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
                WriteMosisEndElement();
                inMajorSection = false;
            }
        }

        protected void StartSection()
        {
            if (!usfx.IsEmptyElement)
            {
                EndSection();
                StartElementWithAttribute("div", "type", "section");
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
                WriteMosisEndElement();
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
                StartElementWithAttribute("div", "type", "subSection");
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
                WriteMosisEndElement();
                inSubSection = false;
            }
        }

        /// <summary>
        /// Start an introduction division
        /// </summary>
        protected void StartIntroduction()
        {
            EndMajorSection();
            if (!usfx.IsEmptyElement)
            {
                if (!inIntroduction)
                {
                    StartElementWithAttribute("div", "type", "introduction", "canonical", "false");
                    inIntroduction = true;
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
                WriteMosisEndElement(); // div type="introduction" canonical="false"
                inIntroduction = false;
            }
        }

        public bool ConvertUsfxToMosis(string usfxFileName, string mosisFileName)
        {
            OsisFileName = mosisFileName;
            mosisNestLevel = 0;
            indentLevel = 0;
            inNote = false;
            inIntroduction = inMajorSection = inSection = inSubSection = inTitledPsalm = false;
            int i;
            try
            {
                bookInfo.ReadUsfxVernacularNames(usfxFileName);
                osisFileName = mosisFileName;
                lastNoteVerse = String.Empty;
                noteNumber = serialNumber = 0;
                langCodes = new LanguageCodeInfo();
                string shortLang = langCodes.ShortCode(languageCode);
                osisWorkId = "Bible." + shortLang;
                if (translationId.Length > 4)
                {
                    osisWorkId += "." + translationId.Substring(4);
                }
                osisWorkId = osisWorkId.Replace('-', '.');
                OpenMosisFile(mosisFileName);
                CheckElementLevel(2, "just wrote header");
                altChapterID = chaptereID = epeID = vpeID = qeID = verseeID = currentTestament = osisVersesId = osisVerseId = String.Empty;
                mtStarted = false;
                inPoetryLine = false;
                eatPoetryLineEnd = false;

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

                        if (mtStarted && !((usfx.Name == "h") || ((usfx.Name == "p") && (sfm == "mt"))))
                        {
                            StartMosisElement("title");
                            mosis.WriteAttributeString("type", "main");
                            if (currentBookHeader.Length > 0)
                            {
                                mosis.WriteAttributeString("short", currentBookHeader);
                            }
                            mosis.WriteString(currentBookTitle);
                            WriteMosisEndElement();
                            mtStarted = false;
                        }

                        switch (usfx.Name)
                        {
                            case "languageCode":
                                usfx.Read();
                                if (usfx.NodeType == XmlNodeType.Text)
                                    languageCode = usfx.Value;
                                break;
                            case "book":
                                currentBookHeader = currentBookTitle = String.Empty;
                                currentChapter = currentChapterPublished = currentChapterAlternate = String.Empty;
                                currentVerse = currentVersePublished = currentVerseAlternate = String.Empty;
                                mtStarted = false;
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
                                if (!inNote)
                                    StartElementWithAttribute("transChange", "type", "added");
                                break;
                            case "bd":
                                StartElementWithAttribute("hi", "type", "bold");
                                break;
                            case "bdit":
                                StartElementWithAttribute("hi", "type", "bold");
                                StartElementWithAttribute("hi", "type", "italic");
                                break;
                            case "it":
                                StartElementWithAttribute("hi", "type", "italic");
                                break;
                            case "em":
                                StartElementWithAttribute("hi", "type", "emphasis");
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
                                break;
                            case "sig":
                                StartElementWithAttribute("signed");
                                break;
                            case "sls":
                                StartElementWithAttribute("foreign", "type", "x-secondaryLanguage");
                                break;
                            case "tl":
                                StartElementWithAttribute("foreign");
                                break;
                            case "b":
                                EndLineGroup();
                                SetListLevel(0, true);
                                EndIntroduction();
                                WriteMosisElementString("lb", "");
                                break;
                            case "bk":
                                StartElementWithAttribute("reference", "type", "x-bookName");
                                break;
                            case "f":   //  footnote
                                StartElementWithAttribute("note", "type", "translation", "osisRef", osisVerseId, "osisID", NoteId());
                                mosis.WriteAttributeString("placement", "foot");
                                inNote = true;
                                break;
                            case "fe":  // End note. Rarely used, fortunately, but in the standards.
                                StartElementWithAttribute("note", "type", "translation", "osisRef", osisVerseId, "osisID", NoteId());
                                mosis.WriteAttributeString("placement", "end");
                                inNote = true;
                                break;
                            case "x":   // Cross references
                                StartElementWithAttribute("note", "type", "crossReference", "osisRef", osisVerseId, "osisID", NoteId());
                                inNote = true;

                                // TODO: parse contents to surround with <reference osisRef="..."> element.
                                break;
                            case "glo":
                            case "ide":
                            case "fig": // Don't bother with figures and OSIS. Not supported by the readers, so not worth the effort.
                            case "fdc":
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
                            case "fp":
                                StartElementWithAttribute("p", "type", "x-footnote");
                                break;
                            case "fq":
                                StartElementWithAttribute("q", "who", "unknown", "type", "x-footnote");
                                break;
                            case "fqa":
                                StartElementWithAttribute("rdg");
                                break;
                            case "fr":
                                StartElementWithAttribute("reference", "type", "source", "osisRef", osisVerseId);
                                break;
                            case "fk":
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
                            case "toc": // No standard equivalent in OSIS
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
                                EndIntroduction();
                                EndCurrentVerse();
                                currentVerse = id;
                                currentVersePublished = fileHelper.LocalizeDigits(currentVerse);
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
                                StartMosisElement("verse");
                                mosis.WriteAttributeString("osisID", osisVersesId);
                                mosis.WriteAttributeString("sID", verseeID);
                                mosis.WriteAttributeString("n", currentVersePublished);
                                WriteMosisEndElement();    // verse
                                CheckMinimumLevel(5, "starting verse " + osisVersesId + " / " + osisVerseId);
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
                                StartElementWithAttribute("w", "gloss", "s:" + strongs);
                                break;
                            case "d":
                                EndLineGroup();
                                SetListLevel(0, true);
                                if (!usfx.IsEmptyElement)
                                {
                                    StartTitledPsalm();
                                    StartElementWithAttribute("title", "type", "psalm", "canonical", "true");
                                }
                                break;
                            case "dc":
                                StartElementWithAttribute("transChange", "type", "added", "edition", "dc");
                                break;
                            case "nd":
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
                                break;
                            case "q":
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
                                StartElementWithAttribute("l", "level", level);
                                if (!usfx.IsEmptyElement)
                                    inPoetryLine = true;
                                break;
                            case "qs":
                                if (inLineGroup)
                                {
                                    if (inPoetryLine)
                                    {
                                        eatPoetryLineEnd = true;
                                        WriteMosisEndElement();
                                        inPoetryLine = false;
                                    }
                                    StartElementWithAttribute("l", "type", "selah");
                                }
                                break;
                            case "table":
                                StartMosisElement("table");
                                break;
                            case "tr":
                                StartMosisElement("row");
                                break;
                            case "th":
                                StartElementWithAttribute("cell", "align", "left", "type", "x-header");
                                break;
                            case "thr":
                                StartElementWithAttribute("cell", "align", "right", "type", "x-header");
                                break;
                            case "tc":
                                StartElementWithAttribute("cell", "align", "left");
                                break;
                            case "tcr":
                                StartElementWithAttribute("cell", "align", "right");
                                break;
                            case "periph":
                                SkipElement();
                                break;
                            case "milestone":
                                StartElementWithAttribute("milestone", "type", "x-" + sfm);
                                Logit.WriteLine("Warning: milestone encountered at " + osisVerseId);
                                break;
                            case "p":
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
                                if ((sfm == "mt") && (!usfx.IsEmptyElement))
                                {
                                    mtStarted = true;
                                    usfx.Read();
                                    if (usfx.NodeType == XmlNodeType.Text)
                                    {
                                        currentBookTitle += " " + usfx.Value;
                                    }
                                    usfx.Read();
                                    if (usfx.IsEmptyElement)
                                    {
                                        if (usfx.Name != "optionalLineBreak")
                                        {   // Silently discard optional line breaks in titles. They are optional and usually meaningless in electronic publishing.
                                            Logit.WriteError("Unexpected element in main title: " + usfx.Name);
                                        }
                                        usfx.Read();
                                        if (usfx.NodeType == XmlNodeType.Text)
                                        {
                                            currentBookTitle = currentBookTitle.Trim() + " " + usfx.Value;
                                            usfx.Read();
                                        }
                                    }
                                    currentBookTitle = currentBookTitle.Trim();
                                    if (usfx.NodeType != XmlNodeType.EndElement)
                                        Logit.WriteError("Unexpected element at "+osisVerseId+" after <p sfm=\"mt\">: " + usfx.Name + " " + usfx.NodeType.ToString());
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
                                            StartElementWithAttribute("l", "level", level, "canonical", "false");
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
                                        case "iot":
                                        case "ior":
                                        case "io":  // TODO: implement levels with nested list/item/list/item...
                                            StartIntroduction();
                                            StartElementWithAttribute("div", "type", "outline", "canonical", "false");
                                            break;
                                        case "ili":
                                            StartIntroduction();
                                            if (level == String.Empty)
                                                level = "1";
                                            indentLevel = int.Parse(level.Trim());
                                            SetListLevel(indentLevel, false);
                                            break;
                                        case "pi":
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
                                StartElementWithAttribute("note", "type", "crossReference", "osisRef", osisVerseId, "osisID", NoteId());
                                mosis.WriteAttributeString("placement", "inline");
                                inNote = true;
                                // TODO: parse contents to surround with <reference osisRef="..."> element.
                                break;
                            case "cs":  // Rare or new character style: don't know what it should be, so throw away tag & keep text.
                                break;
                            case "xt":  // Do nothing. This tag is meaningless in OSIS.
                                break;
                            case "wj":
                                StartMosisElement("q");
                                mosis.WriteAttributeString("who", "Jesus");
                                qeID = StartId();
                                mosis.WriteAttributeString("sID", qeID);
                                mosis.WriteAttributeString("marker", String.Empty);
                                WriteMosisEndElement();    // q
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
                            case "xq":  // Not useful for Sword modules.
                                // StartElementWithAttribute("q", "marker", "");
                                break;
                            default:
                                Logit.WriteLine("Unhandled tag: " + usfx.Name + " at " + osisVerseId);
                                break;
                        }
                    }
                    else if (usfx.NodeType == XmlNodeType.EndElement)
                    {
                        switch (usfx.Name)
                        {
                            case "w":
                            case "zw":
                                WriteMosisEndElement();
                                break;
                            case "wj":
                                StartMosisElement("q");
                                mosis.WriteAttributeString("eID", qeID);
                                WriteMosisEndElement();    // q
                                break;
                            case "book":
                                EndLineGroup();
                                EndCurrentVerse();
                                EndCurrentChapter();
                                EndIntroduction();
                                EndMajorSection();
                                WriteMosisEndElement();  // div type="book"
                                CheckElementLevel(3, "closed book");
                                break;
                            case "bdit":
                                WriteMosisEndElement();    // hi italic
                                WriteMosisEndElement();    // hi bold
                                break;
                            case "p":
                                if (itemLevel > 0)
                                    itemLevel--;
                                CheckMinimumLevel(5, "Ending " + usfx.Name + " " + osisVerseId);
                                inNote = false;
                                if (eatPoetryLineEnd)
                                {
                                    eatPoetryLineEnd = false;
                                }
                                else
                                {
                                    WriteMosisEndElement();
                                }
                                break;
                            case "q":
                                if (eatPoetryLineEnd)
                                {
                                    eatPoetryLineEnd = false;
                                }
                                else
                                {
                                    WriteMosisEndElement();
                                }
                                break;
                            case "fe":
                            case "f":
                            case "x":
                                inNote = false;
                                WriteMosisEndElement();
                                break;
                            case "add":
                                if (!inNote)
                                    WriteMosisEndElement();
                                break;
                            case "qs":
                                if (inLineGroup)
                                    WriteMosisEndElement();
                                break;
                            case "bd":
                            case "bk":
                            case "cl":
                            case "d":
                            case "dc":
                            case "em":
                            case "fk":
                            case "fp":
                            case "fq":
                            case "fqa":
                            case "fr":
                            case "fv":
                            case "it":
                            case "k":
                            case "nd":
                            case "no":
                            case "pn":
                            case "qt":
                            case "r":
                            case "rq":
                            case "s":
                            case "sc":
                            case "sig":
                            case "sls":
                            case "table":
                            case "tc":
                            case "tcr":
                            case "th":
                            case "thr":
                            case "tl":
                            case "tr":
                            case "xo":
                            // case "xq": Not useful for Sword modules.
                                WriteMosisEndElement();    // note, hi, reference, title, l, transChange, etc.
                                break;
                        }
                    }
                    else if (((usfx.NodeType == XmlNodeType.Text) || (usfx.NodeType == XmlNodeType.SignificantWhitespace)) && !ignore)
                    {
                        mosis.WriteString(usfx.Value);
                    }
                }
                Logit.ShowStatus("writing " + mosisFileName);
                CloseMosisFile();

                // Validate this file against the Schema

                osisVerseId = "header";
                Logit.ShowStatus("reading OSIS Schema and " + mosisFileName);
                lastElementWritten = "validating MOSIS file";
                currentElement = "";
                Directory.SetCurrentDirectory(Path.GetDirectoryName(SFConverter.FindAuxFile(localOsisSchema)));
                XmlTextReader txtreader = new XmlTextReader(mosisFileName);
                // XmlValidatingReader is used for compatibility with Mono, in spite of the warning message.
                XmlValidatingReader reader = new XmlValidatingReader(txtreader);

                // Set the validation event handler

                reader.ValidationEventHandler += new ValidationEventHandler(ValidationCallBack);

                // Read XML data

                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        currentElement = reader.Name;
                        string verseId = reader.GetAttribute("osisID");
                        if (verseId != null)
                        {
                            osisVerseId = verseId;
                            Logit.ShowStatus("validating mosis " + osisVerseId);
                        }
                    }
                }
                reader.Close();






/*  NOTE: A WARNING MESSAGE SAYS THAT System.Xml.XmlValidatingReader is obsolete, BUT it still works, and the
 *  suggested replacement function, commented out below, does not work with Mono. Functionality in Mono is
 *  more important than being warning-free in this case. When the following code works in Mono on Linux,
 *  the above code could be commented out, instead.
                XmlReaderSettings settings = new XmlReaderSettings();
                settings.Schemas.Add(osisNamespace, osisSchema);
                settings.ValidationType = ValidationType.Schema;
                settings.ValidationFlags = XmlSchemaValidationFlags.ReportValidationWarnings;
                settings.ValidationEventHandler += new ValidationEventHandler(ValidationCallBack);
                XmlReader mr = XmlTextReader.Create(mosisFileName, settings);

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
 * 
 */
            }
            catch (Exception ex)
            {
                Logit.WriteError("Exception at " + osisVerseId + " " + lastElementWritten);
                Logit.WriteError(ex.Message + "\r\n" + ex.StackTrace);
            }
            return true;
        }
    }
}
