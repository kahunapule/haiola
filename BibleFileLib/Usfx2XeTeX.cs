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
    public class Usfx2XeTeX : usfxToHtmlConverter
    {
        protected string texFileName;
        protected StreamWriter texFile;
        protected string texDir;

        /// <summary>
        /// Constructor
        /// </summary>
        public Usfx2XeTeX()
        {
        }

        /// <summary>
        /// Write navigational links to get to another chapter from here.
        /// We don't really do this in XeLaTeX documents, so this does nothing but return.
        /// </summary>
        protected override void WriteNavButtons()
        {
        }

        /// <summary>
        /// Open a Scripture chapter HTML file and write its HTML header.
        /// </summary>
        protected override void OpenHtmlFile()
        {
            OpenHtmlFile("", true);
        }

        /// <summary>
        /// In this overridden version, we actually open a TeX file for the whole book, not just a chapter.
        /// </summary>
        /// <param name="fileName">Name of file to open if other than a Bible book.</param>
        /// <param name="mainScriptureFile">true iff TextFunc.js is to be included.</param>
        protected override void OpenHtmlFile(string fileName, bool mainScriptureFile, bool skipNav = false)
        {
            CloseHtmlFile();
            noteNumber = 0;
            if ((fileName != null) && (fileName.Length > 0))
            {
                currentFileName = Path.Combine(htmDir, fileName);
            }
            else
            {
                currentFileName = Path.Combine(htmDir, currentBookAbbrev + ".tex");
            }
            htm = new StreamWriter(currentFileName, false, Encoding.UTF8);
            htm.WriteLine("<!DOCTYPE html>");
            htm.WriteLine("<html lang=\"{0}\" dir=\"{1}\">", languageIdentifier, textDirection);
            htm.WriteLine("<head>");
            htm.WriteLine("<meta charset=\"utf-8\" />");
            htm.WriteLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\" />");
            //htm.WriteLine("<meta name=\"viewport\" content=\"user-scalable=yes, initial-scale=1, minimum-scale=1, width=device-width\"/>");
            //      htm.WriteLine("<meta name=\"viewport\" content=\"user-scalable=yes, initial-scale=1, minimum-scale=1, width=device-width, height=device-height\"/>");
            htm.WriteLine("<script src=\"../../../js/mobile.js\"></script>");
            htm.WriteLine("<link rel=\"stylesheet\" href=\"../../../css/is_" + customCssName + "\" type=\"text/css\" />");
            if (mainScriptureFile)
            {
                htm.WriteLine("<script src=\"TextFuncs.js\" type=\"text/javascript\"></script>");
            }
            htm.WriteLine("<title>{0} {1} {2}</title>",
                translationName, currentBookHeader, currentChapterPublished);
            htm.WriteLine(string.Format("<meta name=\"keywords\" content=\"{0}, {1}, Holy, Scripture, Bible, Scriptures\" />",
                translationName, langId));
            htm.WriteLine("</head>");
            htm.WriteLine("<body class=\"mainDoc\"{0}>", OnLoadArgument());
            WriteNavButtons();
        }


























        /// <summary>
        /// Close the currently open XeTeX file, if one is open.
        /// </summary>
        protected void CloseTexFile()
        {
            EndTextStyle();
            EndParagraph();
            if (texFile != null)
            {
                texFile.Close();
                texFile = null;
            }

        }

        /// <summary>
        /// Open a new XeTeX file for writing to.
        /// </summary>
        /// <param name="xetexFileName">name of XeTeX file to write</param>
        protected void OpenTexFile(string xetexFileName)
        {
            texFileName = Path.Combine(texDir, xetexFileName);
            CloseTexFile();
            texFile = new StreamWriter(xetexFileName, false, Encoding.UTF8);
        }

        /// <summary>
        /// End a run of text with a given style
        /// </summary>
        protected void EndTextStyle()
        {
            if (inTextStyle > 0)
            {
                texFile.Write("}");
                    inTextStyle--;
            }
        }

        /// <summary>
        /// Start a run of text with a predefined set of text attributes.
        /// </summary>
        /// <param name="styleName">USFM/USFX sfm name for style</param>
        /// <param name="text">Run of text to put in this style</param>
        /// <param name="exclusive">true iff this implicitly cancels any existing text styles</param>
        protected void StartTextStyle(string styleName, string text, bool exclusive = false)
        {
            if (exclusive)
                EndTextStyle();
            texFile.Write("{\\tsfm{0} {1}", styleName, text);
            inTextStyle++;
        }

        /// <summary>
        /// End any currently active paragaph
        /// </summary>
        protected void EndParagraph()
        {
            if (inParagraph)
            {
                texFile.Write("}");
                inParagraph = false;
            }
        }

        /// <summary>
        /// Start a paragraph of the specified USFM style
        /// </summary>
        /// <param name="styleName"></param>
        /// <param name="text"></param>
        protected void StartParagraph(bool preVerse)
        {
            EndParagraph();
            if (newChapterFound && !preVerse)
            {
                if (currentBookAbbrev.CompareTo("PSA") == 0)
                {
                    texFile.WriteLine("\\PsalmChap {0} {1}", psalmLabel, currentChapterPublished);
                }
                else
                {
                    if (bookRecord.numChapters == 1)
                    {
                        texFile.WriteLine("\\OneChap");
                    }
                    else
                    {
                        texFile.WriteLine("\\Chap {0}", currentChapterPublished);
                    }
                }
                newChapterFound = false;
            }
            if (sfm.Length == 0)
                sfm = usfx.Name;
            string texCommand = "{\\psfm"+sfm;
            if (level != "1")
                texCommand = texCommand + level;
            texFile.WriteLine(texCommand);
            inParagraph = true;
            if (usfx.IsEmptyElement)
                EndParagraph();
        }

        /// <summary>
        /// Get an attribute with the given name from the current usfx element, or an empty string if it is not present.
        /// </summary>
        /// <param name="attributeName">attribute name</param>
        /// <returns>attribute value or an empty string if not found</returns>
        protected string GetNamedAttribute(string attributeName)
        {
            string result = usfx.GetAttribute(attributeName);
            if (result == null)
                result = String.Empty;
            return result;
        }

        bool inHeader = true;
        protected Boolean eatSpace = false;

        /// <summary>
        /// Process a chapter tag
        /// </summary>
        private void ProcessChapter()
        {
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
            }
            int chNum;
            if (Int32.TryParse(id, out chNum))
                chapterNumber = chNum;
            else
                chapterNumber++;
            newChapterFound = true;
            chapterFileIndex++;
            inHeader = false;
        }

        /// <summary>
        /// Start-of-chapter processing for when there is no actual chapter, but there is a verse
        /// </summary>
        private void VirtualChapter()
        {
            currentChapter = "1";
            currentChapterPublished = fileHelper.LocalizeDigits(currentChapter);
            chapterNumber = 1;
            currentChapterAlternate = String.Empty;
            currentVerse = currentVersePublished = currentVerseAlternate = String.Empty;
            verseNumber = 0;
            if (!bookInfo.isPeripheral(currentBookAbbrev))
                newChapterFound = true;
            chapterFileIndex++;
        }


        protected void EndFootnoteStyle()
        {
            if (inFootnoteStyle)
            {
                texFile.Write("}");
                inFootnoteStyle = false;
            }
        }

        protected void EndFootnote()
        {
            EndFootnoteStyle();
            if (inFootnote)
            {
                texFile.Write("}}");
                inFootnote = false;
            }
        }

        /******

        /// <summary>
        /// Start a footnote
        /// 
        /// </summary>
        /// <param name="style">"f" for footnote and "x" for cross reference</param>
        /// <param name="marker">"+" for automatic caller, "-" for no caller (useless for a popup), or a verbatim note caller</param>
        protected void StartFootnote(string style, string marker)
        {
            EndFootnote();
            if (ignoreNotes)
            {
                ignore = true;
                return;
            }
            inFootnote = true;
            if (string.Compare(marker, "+") == 0)
            {
                if (style == "f")
                    marker = footNoteCall.Marker();
                else
                {
                    // style =="x", cross-ref: start accumulating text we will process for hot link cross-refs
                    xRef = "";
                    marker = "✡";
                }
            }
            if (string.Compare(marker, "-") == 0)
                marker = "";
            // Numeric chapter and verse numbers are used in internal references instead of the text versions, which may
            // include dashes in verse bridges.
            if (!String.IsNullOrEmpty(noteOriginFormat))
            {
                automaticOrigin = noteOriginFormat.Replace("%c", currentChapterPublished).Replace("%v", currentVersePublished);
            }
            if ((chapterNumber >= 1) && (verseNumber >= 1))
                footnotesToWrite.Append(String.Format("<p class=\"{0}\" id=\"{1}\"><span class=\"notemark\">{2}</span><a class=\"notebackref\" href=\"#V{3}\">{4}</a>\r\n",
                    style, noteId, marker, verseNumber.ToString(), automaticOrigin));
            else
                footnotesToWrite.Append(String.Format("<p class=\"{0}\" id=\"{1}\"><span class=\"notemark\">{2}</span><a class=\"notebackref\" href=\"#V1\">^</a>\r\n",
                    style, noteId, marker));

        }




        /// <summary>
        /// Start a verse with the appropriate marker and anchor.
        /// </summary>
        protected void StartVerse()
        {
            EndTextStyle(); // USFM and USFX disallow text styles crossing verse boundaries.
            // (If text styles could cross verse boundaries, we could just remember what the last
            //  style was and restart it, but that would make displaying any arbitrary range of
            //  verses harder if it were required.)
            if (preVerse.Length > 0)
            {
                htm.WriteLine(preVerse.ToString());
                preVerse = new StringBuilder(String.Empty);
                inPreverse = false;
            }
            htm.Write(string.Format(" <span class=\"verse\"> <a name=\"V{1}\">{0}&nbsp;</a></span>",
                currentVersePublished, verseNumber.ToString()));
            eatSpace = true;
            if (doXrefMerge)
            {
                string xn = xref.find(currentBCV);
                if ((xn != null) && (xn.Length > 0))
                {
                    StartHtmlNote("x", "-");
                    WriteHtmlText(xn);
                    EndHtmlNote();
                }
            }

            if (preVerse.Length > 0)
            {
                htm.WriteLine(preVerse.ToString());
                preVerse = new StringBuilder(String.Empty);
                inPreverse = false;
            }
            htm.Write(string.Format(" <span class=\"verse\"> <a name=\"V{1}\">{0}&nbsp;</a></span>",
                currentVersePublished, verseNumber.ToString()));
            eatSpace = true;
            if (doXrefMerge)
            {
                string xn = xref.find(currentBCV);
                if ((xn != null) && (xn.Length > 0))
                {
                    StartHtmlNote("x", "-");
                    WriteHtmlText(xn);
                    EndHtmlNote();
                }
            }
        }

        /// <summary>
        /// Process a verse marker
        /// </summary>
        private void ProcessVerse()
        {
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
            }
            currentBCV = currentBookAbbrev + " " + currentChapter + ":" + currentVerse;
            int vnum;
            if (Int32.TryParse(id, out vnum))
            {
                verseNumber = vnum;
            }
            else
            {
                verseNumber++;
            }
            StartVerse();
        }



        FootNoteCaller footNoteCall = new FootNoteCaller("* † ‡ § ** †† ‡‡ §§ *** ††† ‡‡‡ §§§");

        public static string conversionProgress = String.Empty;

        public bool ConvertUsfxToXeTeX(string usfxName, string texDir, string languageName, string languageId, string translationId,
            string chapterLabelName, string psalmLabelName)
        {
            bool result = false;
            string figDescription = String.Empty;
            string figFileName = String.Empty;
            string figSize = String.Empty;
            string figLocation = String.Empty;
            string figCopyright = String.Empty;
            string figCaption = String.Empty;
            string figReference = String.Empty; // Figure parameters
            bool inUsfx = false;
            try
            {
                usfx = new XmlTextReader(usfxName);
                usfx.WhitespaceHandling = WhitespaceHandling.All;

                chapterFileIndex = 0;
                bookListIndex = 0;
                while (usfx.Read())
                {
                    conversionProgress = "Generating XeTeX source " + currentBookAbbrev + " " + currentChapter + ":" + currentVerse;
                    System.Windows.Forms.Application.DoEvents();

                    switch (usfx.NodeType)
                    {
                        case XmlNodeType.Element:
                            if (inUsfx)
                            {
                                level = GetNamedAttribute("level");
                                style = GetNamedAttribute("style");
                                sfm = GetNamedAttribute("sfm");
                                caller = GetNamedAttribute("caller");
                                id = GetNamedAttribute("id");

                                switch (usfx.Name)
                                {
                                    case "languageCode":
                                        usfx.Read();
                                        if (usfx.NodeType == XmlNodeType.Text)
                                            languageCode = usfx.Value;
                                        break;
                                    case "sectionBoundary":
                                    case "generated":
                                    case "rem":
                                    case "periph":
                                    case "ndx":
                                    case "w":
                                    case "wh":
                                    case "wg":
                                        if (!usfx.IsEmptyElement)
                                            ignore = true;
                                        break;
                                    case "fig": // Illustrations
                                        figDescription = figFileName = figSize = figLocation =
                                            figCopyright = figCaption = figReference = String.Empty;
                                        if (stripPictures)
                                        {
                                            ignore = true;
                                        }
                                        break;
                                    case "description":
                                        if (!usfx.IsEmptyElement)
                                        {
                                            usfx.Read();
                                            if (usfx.NodeType == XmlNodeType.Text)
                                                figDescription = usfx.Value.Trim();
                                        }
                                        break;
                                    case "catalog":
                                        if (!usfx.IsEmptyElement)
                                        {
                                            usfx.Read();
                                            if (usfx.NodeType == XmlNodeType.Text)
                                                figFileName = usfx.Value.Trim();
                                        }
                                        break;
                                    case "size":
                                        if (!usfx.IsEmptyElement)
                                        {
                                            usfx.Read();
                                            if (usfx.NodeType == XmlNodeType.Text)
                                                figSize = usfx.Value.Trim();
                                        }
                                        break;
                                    case "location":
                                        if (!usfx.IsEmptyElement)
                                        {
                                            usfx.Read();
                                            if (usfx.NodeType == XmlNodeType.Text)
                                                figLocation = usfx.Value.Trim();
                                        }
                                        break;
                                    case "copyright":
                                        if (!usfx.IsEmptyElement)
                                        {
                                            usfx.Read();
                                            if (usfx.NodeType == XmlNodeType.Text)
                                                figCopyright = usfx.Value.Trim();
                                        }
                                        break;
                                    case "caption":
                                        if (!usfx.IsEmptyElement)
                                        {
                                            usfx.Read();
                                            if (usfx.NodeType == XmlNodeType.Text)
                                                figCaption = usfx.Value.Trim();
                                        }
                                        break;
                                    case "reference":
                                        if (!usfx.IsEmptyElement)
                                        {
                                            usfx.Read();
                                            if (usfx.NodeType == XmlNodeType.Text)
                                                figReference = usfx.Value.Trim();
                                        }
                                        break;

                                    case "book":
                                        currentBookAbbrev = id;
                                        chapterNumber = 0;
                                        verseNumber = 0;
                                        bookRecord = (BibleBookRecord)bookInfo.books[id];
                                        currentBookHeader = bookRecord.vernacularShortName;
                                        OpenTexFile(currentBookAbbrev + ".tex");
                                        break;
                                    case "id":
                                        if (id.Length > 2)
                                            currentBookAbbrev = id;
                                        if (!usfx.IsEmptyElement)
                                            ignore = true;
                                        inHeader = true;
                                        newChapterFound = false;
                                        currentChapter = "0";
                                        chapterNumber = 0;
                                        currentChapterPublished = fileHelper.LocalizeDigits(currentChapter);
                                        currentChapterAlternate = String.Empty;
                                        currentVerse = "0";
                                        currentVersePublished = currentVerseAlternate = String.Empty;
                                        verseNumber = 0;
                                        footNoteCall.reset();
                                        break;
                                    case "ide":
                                        // We could read attribute charset, here, if we would do anything with it.
                                        // Ideally, this would be read on first pass, and used as the encoding to read
                                        // the file on the second pass. However, we currently don't support anything
                                        // but utf-8, so it is kind of redundant, except for round-trip conversion back
                                        // to USFM.
                                        if (!usfx.IsEmptyElement)
                                            ignore = true;
                                        break;
                                    case "h":
                                        usfx.Read();
                                        if (usfx.NodeType == XmlNodeType.Text)
                                            currentBookHeader = (usfx.Value.Trim());
                                        currentChapter = id;
                                        chapterNumber = 0;
                                        currentChapterPublished = fileHelper.LocalizeDigits(currentChapter);
                                        currentChapterAlternate = String.Empty;
                                        currentVerse = currentVersePublished = currentVerseAlternate = String.Empty;
                                        verseNumber = 0;
                                        break;
                                    case "cl":
                                        usfx.Read();
                                        if (usfx.NodeType == XmlNodeType.Text)
                                        {
                                            if (chapterNumber == 0)
                                                wordForChapter = usfx.Value.Trim();
                                            else
                                                currentChapterPublished = fileHelper.LocalizeDigits(usfx.Value.Trim());
                                        }
                                        break;
                                    case "p":
                                        bool beforeVerse = true;
                                        if ((bookRecord.testament.CompareTo("x") == 0) ||
                                            (sfm.Length == 0) || (sfm == "p") || (sfm == "fp") ||
                                            (sfm == "nb") || (sfm == "cls") || (sfm == "li") ||
                                            (sfm == "lit") || (sfm == "m") || (sfm == "mi") ||
                                            (sfm == "pc") || (sfm == "pde") || (sfm == "pdi") ||
                                            (sfm == "ph") || (sfm == "phi") || (sfm == "pi") ||
                                            (sfm == "pm") || (sfm == "pmc") || (sfm == "pmo") ||
                                            (sfm == "pmr") || (sfm == "pr") || (sfm == "ps") ||
                                            (sfm == "psi") || (sfm == "qc") || (sfm == "qm") ||
                                            (sfm == "qr") || (sfm == "pr") || (sfm == "ps"))
                                            beforeVerse = false;
                                        if (ignoreIntros && ((sfm == "ip") || (sfm == "imt") || (sfm == "io") || (sfm == "is") || (sfm == "iot")))
                                            ignore = true;
                                        StartParagraph(beforeVerse);
                                        break;
                                    case "q":
                                    case "qs":  // qs is really a text style with paragraph attributes, but HTML/CSS can't handle that.
                                    case "b":
                                    case "d":
                                    case "s":
                                        StartParagraph(false);
                                        break;
                                    case "ms":
                                        StartParagraph(true);
                                        break;
                                    case "hr":
                                        EndParagraph();
                                        texFile.WriteLine("\\hline");
                                        break;
                                    case "c":
                                        ProcessChapter();
                                        if (chapterNumber > 1)
                                            footNoteCall.reset();
                                        break;
                                    case "cp":
                                        if (!usfx.IsEmptyElement)
                                        {
                                            usfx.Read();
                                            if (usfx.NodeType == XmlNodeType.Text)
                                                currentChapterPublished = fileHelper.LocalizeDigits(usfx.Value.Trim());
                                        }
                                        break;
                                    case "ca":
                                        if (!usfx.IsEmptyElement)
                                        {
                                            usfx.Read();
                                            if (usfx.NodeType == XmlNodeType.Text)
                                                currentChapterAlternate = usfx.Value.Trim();
                                        }
                                        break;
                                    case "toc":
                                        if (!usfx.IsEmptyElement)
                                        {
                                            if (level == String.Empty)
                                                level = "1";
                                            usfx.Read();
                                            if (usfx.NodeType == XmlNodeType.Text)
                                            {
                                                switch (level)
                                                {
                                                    case "1":
                                                        bookRecord.vernacularName = usfx.Value.Trim();
                                                        break;
                                                    case "2":
                                                        bookRecord.shortName = usfx.Value.Trim();
                                                        break;
                                                    case "3":
                                                        bookRecord.vernacularAbbreviation = usfx.Value.Trim();
                                                        break;
                                                }
                                            }
                                        }
                                        break;
                                    case "v":
                                        if (chapterNumber == 0)
                                            VirtualChapter();
                                        ProcessVerse();
                                        break;
                                    case "va":
                                        if (!usfx.IsEmptyElement)
                                        {
                                            usfx.Read();
                                            if (usfx.NodeType == XmlNodeType.Text)
                                                currentVerseAlternate = fileHelper.LocalizeDigits(usfx.Value.Trim());
                                        }
                                        break;
                                    case "vp":
                                        if (!usfx.IsEmptyElement)
                                        {
                                            usfx.Read();
                                            if (usfx.NodeType == XmlNodeType.Text)
                                                currentVersePublished = fileHelper.LocalizeDigits(usfx.Value.Trim());
                                        }
                                        break;
                                    case "qt":
                                    case "nd":
                                    case "tl":
                                    case "qac":
                                    case "fm":
                                    case "sls":
                                    case "bk":
                                    case "pn":
                                    case "k":
                                    case "ord":
                                    case "add":
                                    case "sig":
                                    case "bd":
                                    case "it":
                                    case "bdit":
                                    case "sc":
                                    case "no":
                                    case "ior":
                                    case "wj":
                                    case "cs":
                                    case "rq":
                                        if (sfm.Length == 0)
                                            sfm = usfx.Name;
                                        StartHtmlTextStyle(sfm);
                                        break;
                                    case "table":
                                        StartHtmlTable();
                                        break;
                                    case "th":
                                        WriteHtml("<td><b>");
                                        inTableCol = true;
                                        inTableBold = true;
                                        break;
                                    case "tr":
                                        WriteHtml("<tr>");
                                        inTableRow = true;
                                        break;
                                    case "thr":
                                        WriteHtml("<td align=\"right\"><b>");
                                        inTableBold = true;
                                        inTableCol = true;
                                        break;
                                    case "tc":
                                        WriteHtml("<td>");
                                        inTableCol = true;
                                        break;
                                    case "tcr":
                                        WriteHtml("<td align=\"right\">");
                                        inTableCol = true;
                                        break;

                                    case "f":
                                    case "x":
                                        if (ignoreNotes)
                                            ignore = true;
                                        StartHtmlNote(usfx.Name, caller);
                                        break;
                                    case "fk":
                                    case "fq":
                                    case "fqa":
                                    case "fl":
                                    case "fv":
                                    case "ft":
                                    case "xo":
                                    case "xk":
                                    case "xt":
                                        StartHtmlNoteStyle(usfx.Name);
                                        break;
                                    case "xdc":
                                    case "fdc":
                                    case "dc":
                                        // Suppress these three fields unless there is apocrypha somewhere in the translation (detected in first pass)
                                        if (!containsDC)
                                            ignore = true;
                                        break;
                                    case "optionalLineBreak":
                                        WriteHtmlOptionalLineBreak();
                                        break;

                                }
                            }
                            else
                            {
                                if (usfx.Name == "usfx")
                                    inUsfx = true;
                            }
                            break;
                        case XmlNodeType.Whitespace:
                        case XmlNodeType.SignificantWhitespace:
                        case XmlNodeType.Text:
                            texFile.Write(usfx.Value);
                            break;
                        case XmlNodeType.EndElement:
                            if (inUsfx)
                            {
                                switch (usfx.Name)
                                {
                                    case "usfx":
                                        inUsfx = false;
                                        break;
                                    case "ide":
                                    case "generated":
                                    case "sectionBoundary":
                                    case "rem":
                                    case "periph":
                                    case "ndx":
                                    case "w":
                                    case "wh":
                                    case "wg":
                                    case "id":
                                        ignore = false;
                                        break;
                                    case "fig":
                                        if (stripPictures)
                                        {
                                            ignore = false;
                                        }
                                        else
                                        {   // Actually insert the picture.
                                            //insertHtmlPicture(figFileName, figCopyright, figCaption, figReference);
                                        }
                                        break;
                                    case "book":
                                        if (bookRecord.testament.CompareTo("x") == 0)
                                        {
                                            if (chapterNumber == 0)
                                            {
                                                chapterNumber++;
                                                if (htm == null)
                                                    OpenHtmlFile();
                                                if (preVerse.Length > 0)
                                                {
                                                    htm.WriteLine(preVerse.ToString());
                                                    preVerse = new StringBuilder(String.Empty);
                                                    inPreverse = false;
                                                }
                                            }
                                        }
                                        CloseHtmlFile();
                                        bookListIndex++;
                                        break;
                                    case "p":
x                                    case "q":
                                    case "qs":  // qs is really a text style with paragraph attributes, but HTML/CSS can't handle that.
                                    case "b":
                                    case "mt":
                                        // also done for case "p" after possibly converting cross refs.
                                        EndParagraph();
                                        if (ignoreIntros)
                                            ignore = false;
                                        break;
                                    case "ms":
                                    case "d":
                                    case "s":
                                        if (bookRecord.testament.CompareTo("x") == 0)
                                        {
                                            if (chapterNumber == 0)
                                            {
                                                chapterNumber++;
                                            }
                                            if (htm == null)
                                                OpenHtmlFile();
                                            if (preVerse.Length > 0)
                                            {
                                                htm.WriteLine(preVerse.ToString());
                                                preVerse = new StringBuilder(String.Empty);
                                                //inPreverse = false;
                                            }
                                            verseNumber++;
                                            htm.Write("<a name=\"C{0}V{1}\"></a>",
                                                chapterNumber.ToString(), verseNumber.ToString());
                                        }
                                        EndParagraph();
                                        break;
                                    case "qt":
                                    case "nd":
                                    case "tl":
                                    case "qac":
                                    case "fm":
                                    case "sls":
                                    case "bk":
                                    case "pn":
                                    case "k":
                                    case "ord":
                                    case "add":
                                    case "sig":
                                    case "bd":
                                    case "it":
                                    case "bdit":
                                    case "sc":
                                    case "no":
                                    case "ior":
                                    case "wj":
                                    case "cs":
                                        EndHtmlTextStyle();
                                        break;
                                    case "table":
                                        EndHtmlTable();
                                        break;
                                    case "tr":
                                        EndHtmlTableRow();
                                        break;
                                    case "thr":
                                    case "th":
                                    case "tc":
                                    case "tcr":
                                        EndHtmlTableCol();
                                        break;
                                    case "f":
                                    case "x":
                                        EndHtmlNote();
                                        if (ignoreNotes)
                                            ignore = false;
                                        break;
                                    case "fk":
                                    case "fq":
                                    case "fqa":
                                    case "fl":
                                    case "fv":
                                    case "ft":
                                    case "xo":
                                    case "xk":
                                    case "xt":
                                        EndHtmlNoteStyle();
                                        break;
                                    case "xdc":
                                    case "fdc":
                                    case "dc":
                                        ignore = false;
                                        break;

                                }
                            }
                            break;

                    }

                    result = true;
                }
            }
            catch (Exception ex)
            {
                Logit.WriteError("Error converting " + usfxName + " to html in " + htmlDir + ": " + ex.Message);
                Logit.WriteLine(ex.StackTrace);
                Logit.WriteLine(currentBookAbbrev + " " + currentChapter + ":" + currentVerse);
                result = false;
            }
            finally
            {
                if (usfx != null)
                    usfx.Close();
            }
            return result;



        }
        ***************/

    }
}
