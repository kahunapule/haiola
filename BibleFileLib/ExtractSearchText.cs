using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Data;
using System.IO;
using System.Collections;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Schema;
using System.Diagnostics;

namespace WordSend
{
    /// <summary>
    /// This class strips everything but versification data and canonical search text out of a
    /// usfx file and normalizes the white space to single plain spaces in the verses. This makes
    /// full text searches easier, since they don't have to trip over footnotes, formatting, etc.
    /// It creates an XML file with verses as containers. This file is then used as input for
    /// both old-style concordance generation and full text search.
    /// </summary>
    public class ExtractSearchText
    {
        public int LongestWordLength;
        public Options projectOptions = null;
        protected string currentBook;
        protected string BibleWorksBook;
        protected string currentChapter;
        protected string currentVerse;
        protected string currentPlace;
        protected string osisBook;
        protected string osisVerse;
        protected XmlTextReader usfx;
        protected XmlTextWriter verseFile;
        protected XmlTextWriter lemmaFile;
        protected StreamWriter vplFile;
        bool inVerse;
        bool inPsalmTitle;
        bool verseEndedWithSpace;
        static BibleBookInfo bookInfo;
        BibleBookRecord bookRecord;
        StringBuilder verseText;
        StringBuilder lemmaText;

        /// <summary>
        /// Initialize this instance of ExtractSearchText
        /// </summary>
        public ExtractSearchText()
        {
            currentBook = String.Empty;
            currentChapter = String.Empty;
            currentVerse = String.Empty;
            currentPlace = String.Empty;
            inVerse = false;
            inPsalmTitle = false;
            verseEndedWithSpace = false;
            bookInfo = new BibleBookInfo();
            LongestWordLength = 0;
        }

        /// <summary>
        /// Skips a USFX element.
        /// </summary>
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

        /// <summary>
        /// Writes the unformatted canonical text of a verse to the output file.
        /// </summary>
        protected void EndVerse()
        {
            int wordLength;

            if (inVerse)
            {
                // Normalize regular white spaces
                verseText.Replace('\r', ' ');
                verseText.Replace('\n', ' ');
                verseText.Replace('\t', ' ');
                verseText.Replace("        ", " ");
                verseText.Replace("    ", " ");
                verseText.Replace("   ", " ");
                verseText.Replace("  ", " ");
                verseText.Replace("  ", " ");

                // Normalize small caps to regular caps
                verseText.Replace('ᴀ', 'A');
                verseText.Replace('ʙ', 'B');
                verseText.Replace('ᴄ', 'C');
                verseText.Replace('ᴅ', 'D');
                verseText.Replace('ᴇ', 'E');
                verseText.Replace('ꜰ', 'F');
                verseText.Replace('ɢ', 'G');
                verseText.Replace('ʜ', 'H');
                verseText.Replace('ɪ', 'I');
                verseText.Replace('ᴊ', 'J');
                verseText.Replace('ᴋ', 'K');
                verseText.Replace('ʟ', 'L');
                verseText.Replace('ᴍ', 'M');
                verseText.Replace('ɴ', 'N');
                verseText.Replace("ɴ\u0303", "N\u0303");
                verseText.Replace('ᴏ', 'O');
                verseText.Replace('ᴘ', 'P');
                verseText.Replace('ꞯ', 'Q');
                verseText.Replace('ʀ', 'R');
                verseText.Replace('ꜱ', 'S');
                verseText.Replace('ᴛ', 'T');
                verseText.Replace('ᴜ', 'U');
                verseText.Replace('ᴠ', 'V');
                verseText.Replace('ᴡ', 'W');
                verseText.Replace('ʏ', 'Y');
                verseText.Replace('ᴢ', 'Z');

                // Write verse to file.
                verseFile.WriteStartElement("v");
                verseFile.WriteAttributeString("b", currentBook);
                verseFile.WriteAttributeString("c", currentChapter);
                verseFile.WriteAttributeString("v", currentVerse);
                // Add an OSIS ID for the only verse or first verse of a verse bridge
                int dashPos = currentVerse.IndexOf('-');
                if (dashPos > 0)
                    currentVerse = currentVerse.Substring(0, dashPos);
                osisVerse = osisBook + "." + currentChapter + "." + currentVerse;
                string s = verseText.ToString();
                wordLength = Utils.MaxWordLength(s);
                if (wordLength > LongestWordLength)
                    LongestWordLength = wordLength;
                if (verseEndedWithSpace)
                    s = s.TrimStart(null);
                if (BibleWorksBook.Length == 3)
                {
                    vplFile.WriteLine("{0} {1}:{2} {3}", BibleWorksBook, currentChapter, currentVerse, s.Replace("  ", " ").Trim());
                }
                verseFile.WriteString(s.Replace("[", "").Replace("]", ""));
                verseEndedWithSpace = s.EndsWith(" ");
                verseFile.WriteEndElement();    // v
                inVerse = false;
                verseText.Length = 0;
                lemmaFile.WriteStartElement("v");
                lemmaFile.WriteAttributeString("b", currentBook);
                lemmaFile.WriteAttributeString("c", currentChapter);
                lemmaFile.WriteAttributeString("v", currentVerse);
                lemmaFile.WriteString(lemmaText.ToString().Replace(',',' ').Trim());
                lemmaFile.WriteEndElement();    // v
                lemmaText.Length = 0;
            }
        }

        Encoding utf8encoding;

        /// <summary>
        /// Reads a USFX file and prepares it for full text search (or concordance generation)
        /// by extracting only the canonical text within verses (and the canonical Psalm titles,
        /// which are prepended to verse 1 text), stripping out all formatting, footnotes, etc.,
        /// and normalizing all white space to single spaces. These verse text strings are put
        /// into an XML file with one "v" element per verse, with book, chapter, and verse given
        /// in attributes b, c, and v, respectively.
        /// </summary>
        /// <param name="usfxFileName">Name of the USFX file to extract canonical text from</param>
        /// <param name="verseFileName">Name of XML unformatted verse text only file</param>
        /// <returns></returns>
        public bool Filter(string usfxFileName, string verseFileName)
        {
            string level = String.Empty;
            string style = String.Empty;
            string sfm = String.Empty;
            string caller = String.Empty;
            string id = String.Empty;
            string strongs = String.Empty;
            verseText = new StringBuilder();
            lemmaText = new StringBuilder();
            bool result = false;

            try
            {
                utf8encoding = new UTF8Encoding(false);
                vplFile = new StreamWriter(Path.ChangeExtension(verseFileName, ".vpltxt"), false, utf8encoding);   
                lemmaFile = new XmlTextWriter(Path.ChangeExtension(verseFileName, ".lemma"), utf8encoding);
                lemmaFile.Formatting = Formatting.Indented;
                lemmaFile.WriteStartDocument();
                lemmaFile.WriteStartElement("lemmaFile");
                usfx = new XmlTextReader(usfxFileName);
                usfx.WhitespaceHandling = WhitespaceHandling.All;
                verseFile = new XmlTextWriter(verseFileName, utf8encoding);
                verseFile.Formatting = Formatting.Indented;
                verseFile.WriteStartDocument();
                verseFile.WriteStartElement("verseFile");
                while (usfx.Read())
                {
                    if (!Logit.ShowStatus("extracting search text " + currentPlace))
                        return false;
                    if (usfx.NodeType == XmlNodeType.Element)
                    {
                        level = fileHelper.GetNamedAttribute(usfx, "level");
                        style = fileHelper.GetNamedAttribute(usfx, "style");
                        sfm = fileHelper.GetNamedAttribute(usfx, "sfm");
                        caller = fileHelper.GetNamedAttribute(usfx, "caller");
                        id = fileHelper.GetNamedAttribute(usfx, "id");

                        switch (usfx.Name)
                        {
                            case "book":
                                currentChapter = String.Empty;
                                currentVerse = String.Empty;
                                if (id.Length == 3)
                                {
                                    currentBook = id;
                                    bookRecord = (BibleBookRecord)bookInfo.books[currentBook];
                                    osisBook = bookRecord.osisName;
                                    BibleWorksBook = bookRecord.bibleworksCode;
                                }
                                if ((bookRecord == null) || (id.Length != 3))
                                {
                                    Logit.WriteError("Cannot process unknown book: " + currentBook);
                                    SkipElement();
                                }
                                if (bookRecord.testament == "x")
                                {   // Skip peripherals.
                                    SkipElement();
                                }
                                if (!projectOptions.allowedBookList.Contains(bookRecord.tla))
                                    SkipElement();
                                currentPlace = currentBook;
                                break;
                            case "id":
                                if (id != currentBook)
                                {
                                    Logit.WriteError("Book ID in <id> and <book> do not match; " + currentBook + " is not " + id);
                                }
                                SkipElement();  // Strip out comment portion.
                                break;
                            case "h":
                                usfx.Read();
                                if (usfx.NodeType == XmlNodeType.Text)
                                {
                                    bookRecord.vernacularShortName = usfx.Value.Trim();
                                }
                                break;
                            case "toc":
                                usfx.Read();
                                if (usfx.NodeType == XmlNodeType.Text)
                                {
                                    if (level == "1")
                                    {
                                        bookRecord.vernacularLongName = usfx.Value.Trim();
                                    }
                                    else if (level == "2")
                                    {
                                        string sn = usfx.Value.Trim();
                                        if ((bookRecord.vernacularShortName.Length < 2) || (sn.Length < bookRecord.vernacularShortName.Length))
                                          bookRecord.vernacularShortName = sn;
                                    }
                                }
                                break;
                            case "c":
                                EndVerse(); // In case file lacks <ve /> elements.
                                currentChapter = id;
                                currentVerse = String.Empty;
                                currentPlace = currentBook + "_" + currentChapter;
                                SkipElement(); // Doesn't skip chapter, just the published chapter number, if present.
                                break;
                            case "v":
                                EndVerse(); // In case file lacks <ve /> elements.
                                inVerse = true;
                                currentVerse = id;
                                currentPlace = currentBook + "_" + currentChapter + "_" + currentVerse;
                                SkipElement();  // Just in case there is a published verse number present.
                                break;
                            case "ve":
                                EndVerse();
                                break;
                            case "b":   // blank line
                            case "optionalLineBreak":
                            case "qs":
                            case "th":
                            case "thr":
                            case "tc":
                            case "tcr":
                                if (inVerse)
                                    verseText.Append(' ');
                                break;
                            case "d":   // Make canonical psalm titles searchable
                            case "qd":
                                inPsalmTitle = true;
                                break;
                            case "add":
                                verseText.Append("[");
                                break;
                            case "nd":
                                //verseText.Append("{");
                                break;
                            case "ref": // Ignore
                                break;
                            case "languageCode":
                            case "f":   //  footnote
                            case "fe":  // End note. Rarely used, fortunately, but in the standards.
                            case "x":   // Cross references
                            case "glo":
                            case "ide":
                            case "fig": // figure
                            case "fdc":
                            case "fm":  // Should not actually be in any field texts. Safe to skip.
                            case "idx": // Peripherals - Back Matter Index
                            case "ie":  // Introduction end
                            case "iex": // Introduction explanatory or bridge text
                            case "fp":
                            case "usfm":
                            case "rem": // Comment; not part of the actual text
                            case "cl":
                            case "ca":
                            case "vp":
                            case "periph":
                            case "milestone":
                            case "rq":
                            case "s":
                                SkipElement();
                                break;
                            case "w":
                                strongs = fileHelper.GetNamedAttribute(usfx, "s");
                                if (!String.IsNullOrEmpty(strongs))
                                    lemmaText.Append(strongs + " ");
                                break;
                            case "p":
                                if (sfm.StartsWith("i"))
                                    SkipElement();
                                else
                                {
                                    switch (sfm)
                                    {
                                        case "cd":
                                        case "intro":
                                        case "hr":  // Horizontal rule not supported. Try a line break.
                                        case "ib":
                                        case "im":
                                        case "imq":
                                        case "imi":
                                        case "ip":
                                        case "ipi":
                                        case "ipq":
                                        case "ipr":
                                        case "mt":
                                        case "keyword":
                                        case "iq":
                                        case "imte":
                                        case "imt":
                                        case "is":
                                        case "iot":
                                        case "ior":
                                        case "io":
                                        case "ili":
                                        case "r":
                                            SkipElement();
                                            break;
                                    }
                                }
                                break;
                        }
                    }
                    else if (usfx.NodeType == XmlNodeType.EndElement)
                    {
                        switch (usfx.Name)
                        {
                            case "book":
                                EndVerse(); // In case file lacks <ve /> elements.
                                currentBook = currentChapter = currentVerse = String.Empty;
                                break;
                            case "d":
                            case "qd":
                                inPsalmTitle = false;
                                break;
                            case "add":
                                verseText.Append("]");
                                break;
                            case "nd":
                                // verseText.Append("}");
                                break;
                            case "ref": // Ignore
                                break;
                        }
                    }
                    else if (usfx.NodeType == XmlNodeType.Text)
                    {
                        if (inVerse || inPsalmTitle)
                            verseText.Append(usfx.Value);
                    }
                    else if ((usfx.NodeType == XmlNodeType.SignificantWhitespace) || (usfx.NodeType == XmlNodeType.Whitespace))
                    {
                        if (inVerse || inPsalmTitle)
                            verseText.Append(" ");
                    }
                }
                Logit.ShowStatus("writing " + verseFileName);
                verseFile.WriteEndElement();    // verseFile
                lemmaFile.WriteEndElement();    // lemmaFile
                verseFile.Close();
                lemmaFile.Close();
                vplFile.Close();
                usfx.Close();
                result = true;
            }
            catch (Exception ex)
            {
                Logit.WriteError(ex.Message);
            }
            return result;
        }


        /// <summary>
        /// Write a text file for each chapter with no verse numbers or notes for direct conversion to audio
        /// </summary>
        /// <param name="verseFileName">Name of the existing XML verse per line file</param>
        /// <param name="outputDirectory">Directory to write output files to</param>
        /// <param name="translationId">Bible translation ID</param>
        public void WriteAudioScriptText(string verseFileName, string outputDirectory, string translationId)
        {
            XmlTextReader searchTextXml = new XmlTextReader(verseFileName);
            string book, ch, verseText, canon_order;
            string prevChap = "";
            string prevBook = "";
            string paddedChapter;
            string bookName;
            StreamWriter sf;
            BibleBookRecord bbr;

            sf = new StreamWriter(Path.Combine(outputDirectory, translationId + "_000_000_000_read.txt"), false, Encoding.UTF8);
            sf.WriteLine(@"This set of files contains a script of canonical text, chapter by chapter,
for the purpose of reading to make an audio recording.
All footnotes, introductions, and verse numbers have been stripped out.
");
            while (searchTextXml.Read())
            {
                if ((searchTextXml.NodeType == XmlNodeType.Element) && (searchTextXml.Name == "v"))
                {
                    book = fileHelper.GetNamedAttribute(searchTextXml, "b");
                    ch = fileHelper.GetNamedAttribute(searchTextXml, "c");
                    paddedChapter = ch;
                    if (paddedChapter.Length < 2)
                        paddedChapter = "0" + paddedChapter;
                    if ((book == "PSA") && (paddedChapter.Length < 3))
                        paddedChapter = "0" + paddedChapter;
                    if ((ch != prevChap) || (book != prevBook))
                    {
                        prevChap = ch;
                        prevBook = book;
                        sf.Close();
                        bbr = (BibleBookRecord)bookInfo.books[book];
                        canon_order = bbr.sortOrder.ToString("000");
                        if ((ch == "1") && !string.IsNullOrEmpty(bbr.vernacularLongName))
                            bookName = bbr.vernacularLongName;
                        else
                            bookName = bbr.vernacularShortName;
                        sf = new StreamWriter(Path.Combine(outputDirectory, translationId+"_"+canon_order+"_"+book+"_"+paddedChapter+"_read.txt"),false,Encoding.UTF8);
                        sf.WriteLine(bookName + ".");
                        if (translationId.StartsWith("eng"))
                            sf.Write("Chapter ");
                        sf.WriteLine(ch + ".");
                    }
                    searchTextXml.Read();
                    if (searchTextXml.NodeType == XmlNodeType.Text)
                    {
                        verseText = searchTextXml.Value;
                        sf.WriteLine(verseText);
                    }
                }
            }
            sf.Close();
            searchTextXml.Close();
        }


        /// <summary>
        /// Write an SQL file for MySQL from the VPL XML search text file.
        /// </summary>
        /// <param name="verseFileName">Name of the XML verse per line file.</param>
        /// <param name="translationId">Bible translation ID</param>
        /// <param name="sqlName">Name of the SQL file to write.</param>
        public void WriteSearchSql(string verseFileName, string translationId, string sqlName)
        {
            Hashtable verseDupCheck = new Hashtable(64007);
            string tableName = Path.GetFileNameWithoutExtension(sqlName).Replace('-','_');
            XmlTextReader searchTextXml = new XmlTextReader(verseFileName);
            string book, bk, ch, vs, startVerse, endVerse, verseID, verseText, canon_order;
            int i;
            int dup = 0;
            StreamWriter sqlFile = new StreamWriter(sqlName, false, System.Text.Encoding.UTF8);
            sqlFile.WriteLine("USE sofia;");
            sqlFile.WriteLine("DROP TABLE IF EXISTS sofia.{0};", tableName);
            sqlFile.WriteLine(@"CREATE TABLE {0} (
  verseID VARCHAR(16) NOT NULL PRIMARY KEY,
  canon_order VARCHAR(12) NOT NULL,
  book VARCHAR(3) NOT NULL,
  chapter VARCHAR(3) NOT NULL,
  startVerse VARCHAR(3) NOT NULL,
  endVerse VARCHAR(3) NOT NULL,
  verseText TEXT CHARACTER SET UTF8 NOT NULL) ENGINE=MyISAM;", tableName);
            sqlFile.WriteLine("LOCK TABLES {0} WRITE;", tableName);
            while (searchTextXml.Read())
            {
                if ((searchTextXml.NodeType == XmlNodeType.Element) && (searchTextXml.Name == "v"))
                {
                    book = fileHelper.GetNamedAttribute(searchTextXml, "b");
                    bk = bookInfo.getShortCode(book);
                    ch = fileHelper.GetNamedAttribute(searchTextXml, "c");
                    vs = endVerse = startVerse = fileHelper.GetNamedAttribute(searchTextXml, "v");
                    // Verse numbers might be verse bridges, like "20-22" or simple numbers, like "20".
                    i = vs.IndexOf('-');
                    if (i > 0)
                    {
                        startVerse = startVerse.Substring(0, i);
                        if (vs.Length > i)
                            endVerse = vs.Substring(i+1);
                    }
                    verseID = bk + ch + "_" + startVerse;
                    canon_order = ((BibleBookRecord)bookInfo.books[book]).sortOrder.ToString("000")+"_"+ch+"_"+startVerse;
                    if (verseDupCheck[verseID] != null)
                    {
                        Logit.WriteError("Duplicate verse ID: " + verseID);
                        dup++;
                        verseID = verseID + "_" + dup.ToString();
                    }
                    verseDupCheck[verseID] = vs;
                    searchTextXml.Read();
                    if (searchTextXml.NodeType == XmlNodeType.Text)
                    {
                        verseText = searchTextXml.Value;
                        sqlFile.WriteLine("INSERT INTO {0} VALUES (\"{1}\",\"{2}\",\"{3}\",\"{4}\",\"{5}\",\"{6}\",\"{7}\");",
                            tableName,
                            verseID,
                            canon_order,
                            book,
                            ch,
                            startVerse,
                            endVerse,
                            verseText.Replace("\"","\\\""));
                    }
                }
            }
            searchTextXml.Close();
            sqlFile.WriteLine("ALTER TABLE {0} ADD FULLTEXT(verseText);", tableName);
            sqlFile.WriteLine("UNLOCK TABLES;");
            sqlFile.Close();
        }


    }
}
