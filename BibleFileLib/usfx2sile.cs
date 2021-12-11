using System;
using System.Collections.Generic;
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
    public class Usfx2SILE
    {
        protected string sileFileName = String.Empty;
        protected XmlTextWriter sileFile = null;
        private XmlTextReader usfx;
        public string sileDir;
        public bool callSile = true;
        protected Hashtable pageCounts; // Number of pages in each PDF file as a string. Found in last line of sile output delimited by [].
        private FootNoteCaller footnoteMark;
        private FootNoteCaller xrefMark;
        private LanguageCodeInfo langCodes;
        private string currentBookHeader;
<<<<<<< HEAD
        private string runningBookHeader;
=======
>>>>>>> 654734e24038a45d7e40054f7a6b81dafbb10ecb
        private string toc1;
        private string toc2;
        private string toc3;
        private string currentBookTitle;
<<<<<<< HEAD
        private string chapterLabel;
=======
        private string chapterHeader;
        private string chapterLabel;
        private string verseHeader;
        private string cvSeparator;
>>>>>>> 654734e24038a45d7e40054f7a6b81dafbb10ecb
        private string currentBookAbbrev;
        private string currentChapter, currentChapterPublished, currentChapterAlternate;
        private string currentVerse, currentVersePublished, currentVerseAlternate;
        private string cv = String.Empty;
        private string level;
        private string style;
        private string sfm;
        private string caller;
        private string id;
<<<<<<< HEAD
        private bool chapterWritten;
        private bool runningHeaderWritten;
=======
        private bool inToc1, inToc2, inToc3;
        private bool titleWritten;
        private bool chapterWritten;
>>>>>>> 654734e24038a45d7e40054f7a6b81dafbb10ecb
        private BibleBookRecord bookRecord;
        private BibleBookInfo bookInfo = new BibleBookInfo();
        public global globe;



        public Usfx2SILE(global global)
        {
            globe = global;
            langCodes = new LanguageCodeInfo();
            footnoteMark = new FootNoteCaller(SFConverter.jobIni.ReadString("customFootnoteCaller", "* † ‡"));
<<<<<<< HEAD
            xrefMark = new FootNoteCaller(SFConverter.jobIni.ReadString("xrefCallers", "a b c d e f g h i j k l m n o p q r s t u v w x y z"));
=======
            xrefMark = new FootNoteCaller(SFConverter.jobIni.ReadString("customXrefCaller", "#"));
>>>>>>> 654734e24038a45d7e40054f7a6b81dafbb10ecb
        }


        private void CloseSileFile()
        {
            if (sileFile != null)
            {
                sileFile.WriteEndElement(); // Close root element (book).
                sileFile.Close();
                sileFile = null;
            }
        }

        private void OpenSileFile()
        {
            CloseSileFile();    // Close it if it is open (i.e. for the last book).
            sileFileName = Path.Combine(sileDir, currentBookAbbrev + "_src.sil");
            sileFile = new XmlTextWriter(sileFileName, Encoding.UTF8);
            sileFile.WriteStartDocument();
            sileFile.WriteStartElement("book");
            sileFile.WriteAttributeString("id", currentBookAbbrev);
        }

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

<<<<<<< HEAD
        /// <summary>
        /// Reads the text enclosed in an XML element, discarding any enclosed XML elements, if any.
        /// </summary>
        /// <returns>The text inside this element.</returns>
        private string ReadElementText()
        {
            string result = string.Empty;
            string thisElement = usfx.Name;
            if (!usfx.IsEmptyElement)
            {
                while (!((usfx.NodeType == XmlNodeType.EndElement) && (usfx.Name == thisElement)))
                {
                    usfx.Read();
                    if (usfx.NodeType == XmlNodeType.Text)
                    {
                        result = result + usfx.Value;
                    }
                    else if (usfx.NodeType == XmlNodeType.Whitespace)
                    {
                        result = result + " ";
                    }
                    else if (usfx.NodeType == XmlNodeType.Element)
                    {   // This is probably something like <optionalLineBreak/> in a toc entry, which is not helpful.
                        result = result + " ";
                        SkipElement();
                    }
                }
            }
            return result.Trim();
=======
        private string ReadElementText()
        {
            string result = string.Empty;
            if (!usfx.IsEmptyElement)
            {
                usfx.Read();
                if (usfx.NodeType == XmlNodeType.Text)
                    result = usfx.Value;
                if (usfx.NodeType != XmlNodeType.EndElement)
                    usfx.Read();
            }
            return result;
>>>>>>> 654734e24038a45d7e40054f7a6b81dafbb10ecb
        }

        private void PrintChapter()
        {
            if (!chapterWritten)
            {
<<<<<<< HEAD
                if (!String.IsNullOrEmpty(currentChapterPublished))
                {
                    sileFile.WriteStartElement("chapter");
                    sileFile.WriteString(currentChapterPublished);
                    sileFile.WriteEndElement();
                }
                chapterWritten = true;
            }
            if (!runningHeaderWritten)
            {
                if (string.IsNullOrEmpty(runningBookHeader))
                {
                    runningBookHeader = currentBookHeader;
                    if ((toc2.Length > 2) && (toc2.Length < runningBookHeader.Length))
                        runningBookHeader = toc2;
                    if ((toc3.Length > 1) && (toc2.Length > 18))
                        runningBookHeader = toc3;
                }
                sileFile.WriteElementString("ShortTitle", runningBookHeader);
                runningHeaderWritten = true;
            }
        }

        private string RunningHeader()
        {
            string result;

            /*
            if (string.IsNullOrEmpty(runningBookHeader))
            {
                runningBookHeader = currentBookHeader;
                if ((toc2.Length > 2) && (toc2.Length < runningBookHeader.Length))
                    runningBookHeader = toc2;
                if ((toc3.Length > 1) && (toc2.Length > 18))
                    runningBookHeader = toc3;
            }
            result = runningBookHeader;
            */
            result = string.Empty;
            /*
            if (result == null)
                result = string.Empty;
            */
            if (!string.IsNullOrEmpty(currentChapterPublished))
                result = result + " " + currentChapterPublished;
            if (!string.IsNullOrEmpty(currentVersePublished))
                result = result + globe.projectOptions.CVSeparator + currentVersePublished;
            return result;
        }

        public void ConvertUsfxToSile(string usfxFileName, string sileDirectory)
        {
            string s;

            if (globe.projectOptions == null)
            {
                Logit.WriteError("The variable globe.projectOptions is null in ConvertUsfxToSile.");
                return;
            }
            Logit.OpenFile(Path.Combine(globe.outputProjectDirectory, "usxf2sileerrors.txt"));
            try
            {


                sileDir = sileDirectory;
                usfx = new XmlTextReader(usfxFileName);
                usfx.WhitespaceHandling = WhitespaceHandling.All;

                while (usfx.Read())
                {
                    Logit.ShowStatus("converting to SILE " + cv);
                    if (usfx.NodeType == XmlNodeType.Element)
                    {
                        level = usfx.GetAttribute("level");
                        style = usfx.GetAttribute("style");
                        sfm = usfx.GetAttribute("sfm");
                        caller = usfx.GetAttribute("caller");
                        id = usfx.GetAttribute("id");
                        switch (usfx.Name)
                        {
                            case "languageCode":
                                SkipElement();
                                break;
                            case "book":
                                currentBookHeader = currentBookTitle = String.Empty;
                                toc1 = toc2 = toc3 = String.Empty;
                                currentChapter = currentChapterPublished = currentChapterAlternate = String.Empty;
                                currentVerse = currentVersePublished = currentVerseAlternate = String.Empty;
                                runningBookHeader = String.Empty;
                                chapterWritten = false;
                                runningHeaderWritten = false;
                                if (id.Length > 2)
                                {
                                    currentBookAbbrev = id;
                                    bookRecord = (BibleBookRecord)bookInfo.books[currentBookAbbrev];
                                }
                                if ((bookRecord == null) || (id.Length <= 2))
                                {
                                    Logit.WriteError("Cannot process unknown book: " + currentBookAbbrev);
                                    return;
                                }
                                if ((bookRecord.testament == "a") && !globe.projectOptions.includeApocrypha)
                                {
                                    SkipElement();
                                }
                                else if (!globe.projectOptions.allowedBookList.Contains(bookRecord.tla))// Check for presence of book in bookorder.txt
                                {
                                    SkipElement();
                                }
                                else
                                {   // We have a book we want to write out.
                                    OpenSileFile();
                                }
                                break;
                            case "fe":  // End note. Rarely used, fortunately, but in the standards. Treat as regular footnote.
                            case "f":   //  footnote
                                if (!usfx.IsEmptyElement)
                                {
                                    if (caller == "-")
                                    {
                                        caller = String.Empty;
                                    }
                                    else if ((caller == "+") || (String.IsNullOrEmpty(caller)))
                                    {
                                        caller = footnoteMark.Marker();
                                    }
                                    sileFile.WriteStartElement("f");
                                    sileFile.WriteAttributeString("caller", caller);
                                }
                                break;
                            case "x":   // Cross references
                                if (!usfx.IsEmptyElement)
                                {
                                    if (caller == "-")
                                    {
                                        caller = String.Empty;
                                    }
                                    else if ((caller == "+") || (String.IsNullOrEmpty(caller)))
                                    {
                                        caller = xrefMark.Marker();
                                    }
                                    sileFile.WriteStartElement("x");
                                    sileFile.WriteAttributeString("caller", caller);
                                }
                                break;
                            case "ide": // We don't really use this tag, as we require UTF-8 input.
                            case "fm":  // Should not actually be in any field texts. Safe to skip.
                            case "idx": // Peripherals - Back Matter Index
                                SkipElement();
                                break;
                            case "ie":  // Introduction end
                                SkipElement();
                                break;
                            case "id":
                                if (id != currentBookAbbrev)
                                {
                                    Logit.WriteError("Book ID in <id> and <book> do not match: " + currentBookAbbrev + " is not " + id);
                                }
                                SkipElement();  // Strip out comment portion.
                                break;
                            case "toc": // Table of Contents entries
                                if (String.IsNullOrEmpty(level) || (level == "1"))
                                {
                                    toc1 = ReadElementText();
                                }
                                else if (level == "2")
                                {
                                    toc2 = ReadElementText();
                                }
                                else if (level == "3")
                                {
                                    toc3 = ReadElementText();
                                }
                                else
                                {
                                    SkipElement();
                                }
                                break;
                            case "usfm":
                            case "rem": // Comment; not part of the actual text
                                SkipElement();
                                break;
                            case "h":
                                runningBookHeader = currentBookHeader = ReadElementText();
                                break;
                            case "c":
                                currentChapter = id;
                                currentChapterPublished = fileHelper.LocalizeDigits(currentChapter);
                                currentChapterAlternate = String.Empty;
                                currentVerse = currentVersePublished = currentVerseAlternate = String.Empty;
                                if (!usfx.IsEmptyElement)
                                {
                                    currentChapterPublished = chapterLabel + fileHelper.LocalizeDigits(ReadElementText());
                                }
                                chapterWritten = false;
                                break;
                            case "cl":
                                if (currentChapter == String.Empty)
                                {
                                    chapterLabel = ReadElementText() + " ";
                                }
                                else
                                {
                                    currentChapterPublished = ReadElementText();
                                }
                                break;
                            case "cp":
                                if (!usfx.IsEmptyElement)
                                {
                                    currentChapterPublished = ReadElementText();
                                }
                                break;
                            case "v":
                                PrintChapter();
                                currentVerse = id.Replace("\u200F", "");    // Strip out RTL character
                                currentVersePublished = fileHelper.LocalizeDigits(currentVerse);
                                currentVerseAlternate = "";
                                if (!usfx.IsEmptyElement)
                                {
                                    s = fileHelper.LocalizeDigits(ReadElementText());
                                    if (!String.IsNullOrEmpty(s))
                                        currentVersePublished = s;
                                }
                                sileFile.WriteElementString("v", currentVersePublished);
                                sileFile.WriteElementString("hdr", RunningHeader());
                                break;
                            case "va":  // Not used in header.
                                PrintChapter();
                                s = fileHelper.LocalizeDigits(ReadElementText());
                                sileFile.WriteElementString("va", s);
                                break;
                            case "vp":
                                PrintChapter();
                                s = fileHelper.LocalizeDigits(ReadElementText());
                                if (!String.IsNullOrEmpty(s))
                                    currentVersePublished = s;
                                sileFile.WriteElementString("v", currentVersePublished);
                                sileFile.WriteElementString("hdr", RunningHeader());
                                break;
                            case "p":
                            case "q":
                                s = usfx.Name;
                                if (!String.IsNullOrEmpty(sfm))
                                    s = sfm;
                                if (!String.IsNullOrEmpty(level))
                                    s = s + level;
                                sileFile.WriteStartElement(s);
                                break;
                            case "periph":
                                SkipElement();
                                break;
                            case "optionalLineBreak":
                                sileFile.WriteStartElement(usfx.Name);
                                if (usfx.IsEmptyElement)
                                    sileFile.WriteEndElement();
                                break;
                            case "ve":  // Not useful in typesetting for print.
                            case "cs":  // Rare or new character style: don't know what it should be, so throw away tag & keep text.
                            case "gw":  // Do nothing. Not sure what to do with glossary words, yet.
                            case "xt":  // Do nothing.
                            case "ft":  // Ignore. It does nothing useful, but is an artifact of USFM exclusive character styles.
                            case "usfx":    // Nothing to do, here.
                                break;
                            case "dc":
                            case "xdc":
                            case "fdc":
                                if (!globe.projectOptions.includeApocrypha)
                                    SkipElement();
                                break;
                            case "d":
                            case "s":
                                if (sileFile != null)
                                {
                                    PrintChapter(); // Print chapter number if we haven't done it yet.
                                    s = usfx.Name;
                                    if (!String.IsNullOrEmpty(level))
                                        s = s + level;
                                    sileFile.WriteStartElement(s);
                                    if (usfx.IsEmptyElement)
                                        sileFile.WriteEndElement();
                                }
                                break;
                            default:
                                if (sileFile != null)
                                {
                                    if (usfx.Name == "p")
                                    {
                                        PrintChapter(); // Print chapter number if we haven't done it yet.
                                    }
                                    sileFile.WriteStartElement(usfx.Name);
                                    if (id != null)
                                        sileFile.WriteAttributeString("id", id);
                                    if (caller != null)
                                        sileFile.WriteAttributeString("caller", caller);
                                    if (level != null)
                                        sileFile.WriteAttributeString("level", level);
                                    if (sfm != null)
                                    {
                                        sileFile.WriteAttributeString("sfm", sfm);
                                        if ((usfx.Name == "p") && (sfm.StartsWith("i")))
                                            sileFile.WriteAttributeString("hdr", RunningHeader());
                                    }
                                    if (style != null)
                                        sileFile.WriteAttributeString("style", style);
                                    if (usfx.IsEmptyElement)
                                        sileFile.WriteEndElement();
                                }
                                break;
                        }

                    }
                    else if (usfx.NodeType == XmlNodeType.EndElement)
                    {
                        switch (usfx.Name)
                        {
                            case "ve":  // Why is this not empty???
                            case "cs":  // Rare or new character style: don't know what it should be, so throw away tag & keep text.
                            case "gw":  // Do nothing. Not sure what to do with glossary words, yet.
                            case "xt":  // Do nothing.
                            case "ft":  // Ignore. It does nothing useful, but is an artifact of USFM exclusive character styles.
                            case "usfx":    // Nothing to do, here.
                            case "dc":
                            case "xdc":
                            case "fdc":
                                break;
                            case "book":
                                CloseSileFile();
                                break;
                            default:
                                if (sileFile != null)
                                {
                                    sileFile.WriteEndElement();
                                }
                                break;
                        }
                    }
                    else if (((usfx.NodeType == XmlNodeType.Text) || (usfx.NodeType == XmlNodeType.SignificantWhitespace) || (usfx.NodeType == XmlNodeType.Whitespace)))
                    {
                        if (sileFile != null)
                            sileFile.WriteString(usfx.Value);
                    }
                }
                globe.projectOptions.SileVersionDate = DateTime.Now;
                globe.projectOptions.Write();
            }
            catch(Exception ex)
            {
                Logit.WriteError("Error writing sile files in sileDirectory:");
                Logit.WriteError(ex.Message);
                Logit.WriteError(ex.StackTrace);
                Logit.WriteError("At "+currentBookAbbrev+" "+currentChapter+":"+currentVerse);
            }
            Logit.CloseFile();
            return;
        }
=======
                sileFile.WriteStartElement("chapter");
                sileFile.WriteAttributeString("id", currentChapter);
                sileFile.WriteString(currentChapterPublished);
                sileFile.WriteEndElement();
                chapterWritten = true;
            }
        }

        public bool ConvertUsfxToSile(string usfxFileName, string sileDirectory)
        {
            bool result = false;

            sileDir = sileDirectory;
            usfx = new XmlTextReader(usfxFileName);
            usfx.WhitespaceHandling = WhitespaceHandling.All;

            while (usfx.Read())
            {
                Logit.ShowStatus("converting to SILE " + cv);
                if (usfx.NodeType == XmlNodeType.Element)
                {
                    level = usfx.GetAttribute("level");
                    style = usfx.GetAttribute("style");
                    sfm = usfx.GetAttribute("sfm");
                    caller = usfx.GetAttribute("caller");
                    id = usfx.GetAttribute("id");
                    switch (usfx.Name)
                    {
                        case "languageCode":
                            SkipElement();
                            break;
                        case "book":
                            currentBookHeader = currentBookTitle = String.Empty;
                            toc1 = toc2 = toc3 = String.Empty;
                            inToc1 = inToc2 = inToc3 = false;
                            currentChapter = currentChapterPublished = currentChapterAlternate = String.Empty;
                            currentVerse = currentVersePublished = currentVerseAlternate = String.Empty;
                            titleWritten = false;
                            chapterWritten = false;
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
                            if ((bookRecord.testament == "a") && !globe.projectOptions.includeApocrypha)
                            {
                                SkipElement();
                            }
                            else if (!globe.projectOptions.allowedBookList.Contains(bookRecord.tla))// Check for presence of book in bookorder.txt
                            {
                                SkipElement();
                            }
                            else
                            {   // We have a book we want to write out.
                                OpenSileFile();
                            }
                            break;
                        case "fe":  // End note. Rarely used, fortunately, but in the standards. Treat as regular footnote.
                        case "f":   //  footnote
                            if (!usfx.IsEmptyElement)
                            {
                                if (caller == "-")
                                {
                                    caller = String.Empty;
                                }
                                else if ((caller == "+") || (String.IsNullOrEmpty(caller)))
                                {
                                    caller = footnoteMark.Marker();
                                }
                                sileFile.WriteStartElement("f");
                                sileFile.WriteAttributeString("caller", caller);
                            }
                            break;
                        case "x":   // Cross references
                            if (!usfx.IsEmptyElement)
                            {
                                if (caller == "-")
                                {
                                    caller = String.Empty;
                                }
                                else if ((caller == "+") || (String.IsNullOrEmpty(caller)))
                                {
                                    caller = xrefMark.Marker();
                                }
                                sileFile.WriteStartElement("x");
                                sileFile.WriteAttributeString("caller", caller);
                            }
                            break;
                        case "ide":
                        case "fm":  // Should not actually be in any field texts. Safe to skip.
                        case "idx": // Peripherals - Back Matter Index
                            SkipElement();
                            break;
                        case "ie":  // Introduction end
                            SkipElement();
                            break;
                        case "id":
                            if (id != currentBookAbbrev)
                            {
                                Logit.WriteError("Book ID in <id> and <book> do not match: " + currentBookAbbrev + " is not " + id);
                            }
                            SkipElement();  // Strip out comment portion.
                            break;
                        case "toc": // Table of Contents entries
                            if (String.IsNullOrEmpty(level) || (level == "1"))
                            {
                                inToc1 = true;
                            }
                            else if (level == "2")
                            {
                                inToc2 = true;
                            }
                            else if (level == "3")
                            {
                                inToc3 = true;
                            }
                            else
                            {
                                SkipElement();
                            }
                            break;
                        case "rem": // Comment; not part of the actual text
                            SkipElement();
                            break;
                        case "h":
                            currentBookHeader = ReadElementText().Trim();
                            break;
                        case "c":
                            currentChapter = id;
                            currentChapterPublished = fileHelper.LocalizeDigits(currentChapter);
                            currentChapterAlternate = String.Empty;
                            currentVerse = currentVersePublished = currentVerseAlternate = String.Empty;
                            currentChapterPublished = chapterLabel + fileHelper.LocalizeDigits(ReadElementText().Trim());
                            chapterWritten = false;
                            break;
                        case "cl":
                            if (currentChapter == String.Empty)
                            {
                                chapterLabel = ReadElementText().Trim() + " ";
                            }
                            else
                            {
                                currentChapterPublished = ReadElementText().Trim();
                            }
                            break;
                        case "cp":
                            if (!usfx.IsEmptyElement)
                            {
                                currentChapterPublished = ReadElementText().Trim();
                            }
                            break;
                        case "v":
                            PrintChapter();
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
                            break;
                        case "va":  // Not supported by The Sword Project
                            SkipElement();
                            break;
                        case "vp":
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
                        case "periph":
                            SkipElement();
                            break;
                        case "cs":  // Rare or new character style: don't know what it should be, so throw away tag & keep text.
                            break;
                        case "gw":  // Do nothing. Not sure what to do with glossary words, yet.
                        case "xt":  // Do nothing.
                        case "ft":
                            // Ignore. It does nothing useful, but is an artifact of USFM exclusive character styles.
                            break;
                        case "usfx":
                            // Nothing to do, here.
                            break;
                        case "dc":
                        case "xdc":
                        case "fdc":
                            if (!globe.projectOptions.includeApocrypha)
                                SkipElement();
                            break;
                        default:
                            sileFile.WriteStartElement(usfx.Name);
                            if (id != null)
                                sileFile.WriteAttributeString("id", id);
                            if (caller != null)
                                sileFile.WriteAttributeString("caller", caller);
                            if (level != null)
                                sileFile.WriteAttributeString("level", level);
                            if (sfm != null)
                                sileFile.WriteAttributeString("sfm", sfm);
                            if (style != null)
                                sileFile.WriteAttributeString("style", style);
                            if (usfx.IsEmptyElement)
                                sileFile.WriteEndElement();
                            break;
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
                            case "ref":
                                if (inReference)
                                {
                                    WriteMosisEndElement(); // reference
                                    inReference = false;
                                }
                                break;
                            case "fe":
                            case "f":
                            case "x":
                                if (inNote)
                                {
                                    inNote = false;
                                    WriteMosisEndElement(); // End of note
                                }
                                break;
                            case "add":
                                if (!inNote)
                                    WriteMosisEndElement();
                                break;
                            case "qs":
                                if (inLineGroup)
                                {
                                    WriteMosisEndElement();
                                    inPoetryLine = false;
                                }

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
                            case "k":
                            case "no":
                            case "pn":
                            case "qac":
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
                            case "ord":
                                // case "xq": Not useful for Sword modules.
                                WriteMosisEndElement();    // note, hi, reference, title, l, transChange, etc.
                                break;
                            case "it":
                                if (!inStrongs)
                                {
                                    WriteMosisEndElement();
                                }
                                break;
                            case "nd":
                                WriteMosisEndElement(); // divineName
                                WriteMosisEndElement(); // seg
                                break;
                            case "xk":
                            case "fl":
                            case "zcr":
                            case "zcb":
                            case "zcg":
                            case "zcy":
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
                    else
                        mosis.WriteString(usfx.Value);
                }
            }
            return result;
        }
        




>>>>>>> 654734e24038a45d7e40054f7a6b81dafbb10ecb
    }
}