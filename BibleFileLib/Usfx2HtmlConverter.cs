// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003-2013, SIL International, EBT, and eBible.org.
// <copyright from='2003' to='2013' company='SIL International, EBT, and eBible.org'>
//    
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
// Responsibility: (Kahunapule) Michael P. Johnson
// Last reviewed: 
// 
// <remarks>
// Most of the inner workings of the WordSend Bible format conversion
// project are in this DLL. The objects in this object library are
// called by both the command line and the Windows UI versions of the
// USFM to WordML converter. They may also be used by other conversion
// processes.
// </remarks>
// --------------------------------------------------------------------------------------------

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
    /// <summary>
    /// Converts a USFX file to HTML files.
    /// </summary>
    public class usfxToHtmlConverter
    {
        public bool stripPictures = false;
        public string htmlextrasDir = String.Empty;

        protected const string IndexFileName = "index.htm";
        protected string usfxFileName;
        protected XmlTextReader usfx;
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
        protected string currentChapter;
        protected string currentChapterAlternate;
        protected string wordForChapter;
        protected string currentChapterPublished;
        protected string currentBCV;
        protected string vernacularLongTitle;
        protected string languageCode;
        protected int chapterNumber; // of the file we are currently generating
        protected int verseNumber;
        protected string currentVerse;
        protected string currentVersePublished;
        protected string currentVerseAlternate;
        protected string currentFileName = "";
        protected string previousFileName = "";
        protected string chapterLabel = "";
        protected string psalmLabel = "";
        protected string langName = "";
        protected string langId = "";
        protected string footerTextHTML = "";
        protected string copyrightLinkHTML = "";
        protected string homeLinkHTML = "";
        protected StringBuilder preVerse = new StringBuilder(String.Empty);
        protected string previousChapterText;
        protected string nextChapterText;
        protected ArrayList chapterFileList = new ArrayList();
        protected int chapterFileIndex = 0;
        public static ArrayList bookList = new ArrayList();

        protected int bookListIndex = 0;
        protected StringBuilder footnotesToWrite;
        protected StreamWriter htm;
        //int one = 1;
        protected bool inFootnote;
        protected bool inFootnoteStyle;
        protected int inTextStyle;
        protected bool inParagraph;
        protected bool ignore;
        protected bool ignoreIntros = false;
        protected bool ignoreNotes = false;
        protected bool chopChapter;
        protected bool hasContentsPage;
        //bool containsDC;
        protected bool newChapterFound;
        public BibleBookInfo bookInfo = new BibleBookInfo();
        protected BibleBookRecord bookRecord;

        /// <summary>
        /// Null except when we have seen an open element with name p and style "Parallel Passage Reference" but have not yet seen the corresponding end element.
        /// Set to empty when we see the open element, any intermediate text is added to it.
        /// The end element then generates the complete cross-ref.
        /// </summary>
        protected string parallelPassage;

        /// <summary>
        /// Null except when we have seen an "x" element and not yet seen the corresponding "/x". Then we accumulate here the material we will write to
        /// the footnote, after attempting to convert relevant parts to cross-refs.
        /// </summary>
        protected string xRef;

        /// <summary>
        /// Constructor, setting default value(s)
        /// </summary>
        public usfxToHtmlConverter()
        {
            ConcordanceLinkText = "Concordance";
        }

        /// <summary>
        /// True if we plan to generate a concordance to go with this file.
        /// </summary>
        public bool GeneratingConcordance { get; set; }

        /// <summary>
        /// See if we are supposed to link to a real picture in this fig element.
        /// </summary>
        /// <param name="pictureName">name of the picture file</param>
        /// <returns>Actual name of the picture if it exists and we aren't removing pictures.</returns>
        protected string CheckPicture(string pictureName)
        {
            string actualName = String.Empty;
            string picturePath;
            string searchName, foundName, foundExt;
            if ((htmlextrasDir.Length > 5) && (Directory.Exists(htmlextrasDir)) && (!stripPictures))
            {
                pictureName = Path.GetFileName(pictureName);
                picturePath = Path.Combine(htmlextrasDir, pictureName);
                if (File.Exists(picturePath))
                    actualName = pictureName;
                else
                {
                    searchName = Path.GetFileNameWithoutExtension(pictureName).ToLowerInvariant();
                    string[] fileEntries = Directory.GetFiles(htmlextrasDir);
                    foreach (string fileName in fileEntries)
                    {
                        foundName = Path.GetFileNameWithoutExtension(fileName).ToLowerInvariant();
                        if (foundName == searchName)
                        {
                            foundExt = Path.GetExtension(fileName).ToLowerInvariant();
                            if ((foundExt == ".jpg") || (foundExt == ".jpeg") || (foundExt == ".gif") ||
                                (foundExt == ".png") || (foundExt == ".bmp") || (foundExt == ".tif") ||
                                (foundExt == ".tiff"))
                            {
                                return Path.GetFileName(fileName);
                            }
                        }

                    }
                }
            }
            return actualName;
        }

        protected FootNoteCaller footNoteCall = new FootNoteCaller("* † ‡ § ** †† ‡‡ §§ *** ††† ‡‡‡ §§§");

        /// <summary>
        /// Start a new HTML paragraph
        /// </summary>
        /// <param name="beforeVerse">true iff this is before a verse marker</param>
        protected void ProcessParagraphStart(bool beforeVerse)
        {
            if (sfm.Length == 0)
                sfm = usfx.Name;
            string cssStyle = sfm;
            if (level != "1")
                cssStyle = cssStyle + level;
            StartHtmlParagraph(cssStyle, beforeVerse);
            if (usfx.IsEmptyElement)
                EndHtmlParagraph();
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

        protected string navButtonCode;
        protected Boolean eatSpace = false;

        /// <summary>
        /// Write navigational links to get to another book or another chapter from here. Also include links to site home, previous chapter, and next chapter.
        /// </summary>
        protected virtual void WriteNavButtons()
        {
            int i;
            string s = string.Empty;
            StringBuilder sb = new StringBuilder(s);
            StringBuilder bsb = new StringBuilder(s);
            BibleBookRecord br;

            // Write the "Home" link if one is desired.
            if ((homeLinkHTML != null) && (homeLinkHTML.Trim().Length > 0))
            {
                htm.WriteLine("<div class=\"navButtons\">{0}</div>", homeLinkHTML);
            }

            int chapNumSize;
            var formatString = FormatString(out chapNumSize);
            string firstChapterFile = FirstChapterFile(formatString);


            if (bookListIndex >= 0)
            {

                htm.WriteLine("<div class=\"navButtons\"><a href=\"index.htm\">{0}</a> <a href=\"{1}\">{2}</a> {3}</div>",
                                langName, firstChapterFile, bookRecord.vernacularShortName, currentChapterPublished);
                bsb.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" align=\"center\"><tbody><tr><td>");
                bsb.Append("<form name=\"bkch1\"><div class=\"navChapters\">");
                bsb.Append("<select name=\"bksch1\" onChange=\"location=document.bkch1.bksch1.options[document.bkch1.bksch1.selectedIndex].value;\">");
                i = 0;
                while ((i < bookInfo.publishArray.Length) && (bookInfo.publishArray[i] != null))
                {
                    br = (BibleBookRecord)bookInfo.publishArray[i];
                    if (br.tla == bookRecord.tla)
                    {
                        bsb.Append(OptionSelectedOpeningElement + br.vernacularShortName + "</option>\r\n");
                    }
                    else
                    {
                        if (br.HasContent)
                        {
                            bsb.Append("<option value=\"" + br.chapterFiles[0] + ".htm\">" + br.vernacularShortName + "</option>\r\n");
                        }
                    }
                    i++;
                }
                bsb.Append("</select></div>");
                bsb.Append("</form>");

                bsb.Append("</td><td><form name=\"ch1\">");

                s = "<div class=\"navChapters\">";
                sb.Append(s);
                bsb.Append(s);
                if (previousFileName.Length > 0)
                {
                    s = String.Format("<a href=\"{0}#V0\">&lt;&nbsp;</a>",
                        Path.GetFileName(previousFileName));
                    sb.Append(s);
                    bsb.Append(s);
                }
                bsb.Append("<select name=\"ch1sel\" onChange=\"location=document.ch1.ch1sel.options[document.ch1.ch1sel.selectedIndex].value;\">");

                i = 0;
                string linkText = fileHelper.LocalizeDigits("0");
                if (hasContentsPage)
                {
                    if (0 == chapterNumber)
                    {
                        sb.Append(" &nbsp;" + linkText + "&nbsp; ");
                        bsb.Append(OptionSelectedOpeningElement + linkText + "</option>");
                    }
                    else
                    {
                        sb.Append(String.Format(" <a href=\"{0}\">&nbsp;{1}&nbsp;</a> ",
                            String.Format("{0}{1}.htm", currentBookAbbrev, i.ToString(formatString)), linkText));
                        bsb.Append(String.Format("<option value=\"{0}\">{1}</option>",
                            String.Format("{0}{1}.htm", currentBookAbbrev, i.ToString(formatString)), linkText));
                    }
                }
                int nextChapIndex = -1;
                i = 0;
                foreach (string chFile in chapterFileList)
                {
                    int cn;
                    if (chFile.StartsWith(currentBookAbbrev) && (int.TryParse(chFile.Substring(chFile.Length - chapNumSize), out cn)))
                    {
                        // The first match for this book is the next chapter from the contents chapter (0).
                        if (nextChapIndex == -1)
                            nextChapIndex = i;
                        linkText = fileHelper.LocalizeDigits(cn.ToString());
                        if (cn == chapterNumber)
                        {
                            sb.Append(String.Format(" &nbsp;{0}&nbsp; ", linkText));
                            bsb.Append(String.Format(OptionSelectedOpeningElement + "{0}</option>\r\n", linkText));
                            nextChapIndex = i + 1;
                        }
                        else
                        {
                            sb.Append(String.Format("<a href=\" {0}.htm#V0\">&nbsp;{1}&nbsp;</a> ",
                                chFile, linkText));
                            bsb.Append(String.Format("<option value=\"{0}.htm#V0\">{1}</option>\r\n",
                                chFile, linkText));
                        }
                    }
                    i++;
                }
                bsb.Append("</select>");
                if ((nextChapIndex >= chapterFileList.Count) || (nextChapIndex < 0))
                    nextChapIndex = 0;

                s = String.Format("<a href=\"{0}#V0\">&nbsp;&gt;</a></div>",
                    (string)chapterFileList[nextChapIndex] + ".htm");
                sb.Append(s);
                bsb.Append(s);
                bsb.Append("</form></tr></td></tbody></table>");



            }
            else
            {
                htm.WriteLine("<div class=\"navButtons\"><a href=\"index.htm\">{0}</a></div>", langName);
                bsb.Append("<form name=\"bkch1\"><div class=\"navChapters\">");
                bsb.Append("<select name=\"bksch1\" onChange=\"location=document.bkch1.bksch1.options[document.bkch1.bksch1.selectedIndex].value;\">");
                bsb.Append(OptionSelectedOpeningElement + "---</option>\r\n");
                i = 0;
                BibleBookRecord bookRec;
                while ((i < bookInfo.publishArray.Length) && (bookInfo.publishArray[i] != null))
                {
                    bookRec = (BibleBookRecord)bookInfo.publishArray[i];
                    if (bookRec.isPresent && (bookRec.chapterFiles != null) && (bookRec.chapterFiles.Count > 0))
                    {
                        bsb.Append("<option value=\"" + bookRec.chapterFiles[0] + ".htm\">" + bookRec.vernacularShortName + "</option>\r\n");
                    }
                    i++;
                }
                bsb.Append("</select></div>");
                bsb.Append("</form>");
            }


            navButtonCode = bsb.ToString();
            if (fileHelper.LocalizingDigits)
                htm.WriteLine(sb.ToString());
            else
                htm.WriteLine(navButtonCode);
            navButtonCode = navButtonCode.Replace("ch1", "ch2");
        }

        /// <summary>
        /// We use 2 digits for chapter numbers in file names except for in the Psalms, where we use 3.
        /// </summary>
        /// <param name="chapNumSize">2, unless the current book is Psalms, in which case 3 is returned.</param>
        /// <returns>"00" unless the current book is Psalms, in which case "000" is returned.</returns>
        protected string FormatString(out int chapNumSize)
        {
            string formatString = "00";
            chapNumSize = 2;
            if (currentBookAbbrev.CompareTo("PSA") == 0)
            {
                formatString = "000";
                chapNumSize = 3;
            }
            return formatString;
        }

        /// <summary>
        /// Figure out what the file name of the contents page or first chapter of the current book is, allowing for
        /// possible (likely) missing contents pages or chapter 1.
        /// </summary>
        /// <param name="formatString"></param>
        /// <returns></returns>
        protected virtual string FirstChapterFile(string formatString)
        {
            if (hasContentsPage)
                return String.Format("{0}{1}.htm", currentBookAbbrev, 0.ToString(formatString));
            string firstChapterFile = "";
            for (int i = 0; (i < chapterFileList.Count) && (firstChapterFile == String.Empty); i++)
            {
                string chFile = (string)chapterFileList[i];
                if (chFile.StartsWith(currentBookAbbrev))
                {
                    // The first match for this book might be chapter 1 or, if that is missing, a later chapter.
                    firstChapterFile = chFile + ".htm";
                }
            }
            return firstChapterFile;
        }

        /// <summary>
        /// In html, option selected can start out with just this, but xhtml requires the attribute to have a value.
        /// </summary>
        protected virtual string OptionSelectedOpeningElement
        {
            get { return "<option selected=\"selected\">"; }
        }

        /// <summary>
        /// Repeat navigational links at the bottom of the chapter.
        /// </summary>
        protected virtual void RepeatNavButtons()
        {
            htm.WriteLine(navButtonCode);
        }

        /// <summary>
        /// Open a Scripture chapter HTML file and write its HTML header.
        /// </summary>
        protected virtual void OpenHtmlFile()
        {
            OpenHtmlFile("", true);
            htm.WriteLine("<div class=\"main\">");
        }

        /// <summary>
        /// Open an auxilliary (non-Bible chapter) HTML file and write its header.
        /// </summary>
        /// <param name="fileName">Name of HTML file to open.</param>
        protected void OpenHtmlFile(string fileName)
        {
            OpenHtmlFile(fileName, false);
        }

        /// <summary>
        /// Open an HTML file named with the given name if non-empty, or with the 3-letter book abbreviation followed by the chapter number then ".htm"
        /// and write the HTML header.
        /// </summary>
        /// <param name="fileName">Name of file to open if other than a Bible chapter.</param>
        /// <param name="mainScriptureFile">true iff TextFunc.js is to be included.</param>
        protected virtual void OpenHtmlFile(string fileName, bool mainScriptureFile, bool skipNav = false)
        {
            CloseHtmlFile();
            string chap = currentChapter;
            noteNumber = 0;
            if ((fileName != null) && (fileName.Length > 0))
            {
                currentFileName = Path.Combine(htmDir, fileName);
            }
            else
            {
                if (currentBookAbbrev == "PSA")
                    chap = String.Format("{0}", chapterNumber.ToString("000"));
                else
                    chap = String.Format("{0}", chapterNumber.ToString("00"));
                currentFileName = Path.Combine(htmDir, currentBookAbbrev + chap + ".htm");
            }
            MakeFramesFor(currentFileName);
            htm = new StreamWriter(currentFileName, false, Encoding.UTF8);
            // It is important that the DOCTYPE declaration should be a single line, and that the <html> element starts the second line.
            // This is because the concordance parser uses a ReadLine to skip the DOCTYPE declaration in order to read the rest of the file as XML.
            // Note: switching to HTML5 syntax, with XHTML-compatible lower case element names and XML-style empty elements (like <br />).
            htm.WriteLine(
                "<!DOCTYPE html>");
            htm.WriteLine("<head>");
            htm.WriteLine("<meta charset=\"utf-8\" />");
            htm.WriteLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\" />");
            htm.WriteLine("<link rel=\"stylesheet\" href=\"prophero.css\" type=\"text/css\" />");
            htm.WriteLine("<meta name=\"viewport\" content=\"user-scalable=no, initial-scale=1, minimum-scale=1, width=device-width, height=device-height\"/>");
            // htm.WriteLine("<meta name=\"viewport\" content=\"width=device-width\" />");
            if (mainScriptureFile)
            {
                htm.WriteLine("<script src=\"TextFuncs.js\" type=\"text/javascript\"></script>");
            }
            htm.WriteLine("<title>{0} {1} {2}</title>",
                langName, currentBookHeader, currentChapterPublished);
            htm.WriteLine(string.Format("<meta name=\"keywords\" content=\"{0}, {1}, Holy Bible, Scripture, Bible, Scriptures, New Testament, Old Testament, Gospel\" />",
                langName, langId));
            htm.WriteLine("</head>");
            if (skipNav)
            {
                navButtonCode = String.Empty;
            }
            else
            {
                htm.WriteLine("<body class=\"mainDoc\"{0}>", OnLoadArgument());
                WriteNavButtons();
            }
        }

        /// <summary>
        /// Generate the string that should be inserted into the book element to specify any javascript that shoudl be called when the page is loaded.
        /// </summary>
        /// <returns></returns>
        protected virtual string OnLoadArgument()
        {
            return GeneratingConcordance ? " onload=\"onLoad()\"" : "";
        }

        /// <summary>
        /// Hook to allow subclass to generate corresponding frame file(s) for each main file.
        /// </summary>
        /// <param name="htmPath"></param>
        public virtual void MakeFramesFor(string htmPath)
        {
            // Default for non-frame version does nothing.		
        }

        /// <summary>
        /// Note: there is no good reason to NOT include the final slash in HTML, just the same as XHTML. It is good HTML5, and all the browsers
        /// we care about support it.
        /// Write out a line text that makes up a complete element except for the closing angle bracket. In HTML, we COULD just add the closing bracket,
        /// but closing with " />" is also allowed.
        /// In XHTML, we need a closing slash before that bracket.
        /// </summary>
        /// <param name="htm"></param>
        /// <param name="content"></param>
        /* Deprecated:
        protected void WriteCompleteElement(string content)
        {
            htm.WriteLine(content + " />");
            // htm.WriteLine(content + CloseOfContentlessElement);
        }
        */

        /// <summary>
        /// The way to close an element that is not going to have any content. In HTML, we can just put a closing bracket. In XHTML, we need a slash before it.
        /// </summary>
        /* Deprecated
        protected virtual string CloseOfContentlessElement
        {
            get { return ">"; }
        }
        */

        /// <summary>
        /// Write footnotes at the bottom of the chapter file, if any are queued up to be written.
        /// </summary>
        protected void WriteHtmlFootnotes()
        {
            if (ignoreNotes)
                footnotesToWrite = new StringBuilder(String.Empty);
            if (footnotesToWrite.Length > 0)
            {
                WriteHorizontalRule();
                htm.WriteLine(footnotesToWrite);
                footnotesToWrite = new StringBuilder(String.Empty);
            }
            WriteHorizontalRule();
        }

        /// <summary>
        /// Write an XHTML-compatible HTML horizontal rule.
        /// </summary>
        private void WriteHorizontalRule()
        {
            htm.WriteLine("<hr />");
        }

        /// <summary>
        /// Finish up the HTML file contents and close the file.
        /// </summary>
        protected virtual void CloseHtmlFile()
        {
            if (htm != null)
            {
                EndHtmlNote();
                EndHtmlTextStyle();
                EndChapter();
                EndHtmlParagraph();
                htm.WriteLine("<div class=\"pageFooter\">");
                WriteHtmlFootnotes();
                htm.WriteLine("</div></div>"); // end of div.pageFooter and div.main
                htm.WriteLine("<div class=\"pageFooter\">");
                RepeatNavButtons();
                if ((footerTextHTML != null) && (footerTextHTML.Trim().Length > 0))
                {
                    htm.WriteLine(footerTextHTML);
                }
                if ((copyrightLinkHTML != null) && (copyrightLinkHTML.Trim().Length > 0))
                {
                    htm.WriteLine("<p align=\"center\">" + copyrightLinkHTML + "</p>");
                }
                htm.WriteLine("</div><div class=\"pageFooter\">");
                htm.WriteLine("</div></body></html>");
                htm.Close();
                htm = null;
                previousFileName = currentFileName;
            }
            chopChapter = false;
        }

        public int maxVerseLength = 0;
        protected string bookOsisId;
        protected string chapterOsisId;
        protected string verseOsisId;


        /// <summary>
        /// Start a verse with the appropriate marker and anchor.
        /// </summary>
        protected virtual void StartVerse()
        {
            EndHtmlTextStyle(); // USFM and USFX disallow text styles crossing verse boundaries.
            // (If text styles could cross verse boundaries, we could just remember what the last
            //  style was and restart it, but that would make displaying any arbitrary range of
            //  verses harder if it were required.)
            if (htm == null)
                OpenHtmlFile();
            if (preVerse.Length > 0)
            {
                htm.WriteLine(preVerse.ToString());
                preVerse = new StringBuilder(String.Empty);
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
        /// Start an HTML paragraph with the specified style and initial text.
        /// If the paragraph is noncanonical introductory material, it should
        /// be marked as isPreverse to indicate that it should be in the file
        /// with the next verse, not the previous one.
        /// </summary>
        /// <param name="style">Paragraph style name to use for CSS class</param>
        /// <param name="text">Initial text of the paragraph, if any</param>
        /// <param name="isPreverse">true iff this paragraph style is non canonical,
        /// like section titles, book introductions, and such</param>
        protected virtual void StartHtmlParagraph(string style, bool isPreverse)
        {
            EndHtmlParagraph();
            inParagraph = true;
            if (newChapterFound)
            {
                if (currentBookAbbrev.CompareTo("PSA") == 0)
                {
                    WriteHtml(String.Format("<div class=\"chapterlabel\"><a name=\"V0\">{0} {1}</a></div>", psalmLabel, currentChapterPublished));
                }
                else
                {
                    WriteHtml(String.Format("<div class=\"chapterlabel\"><a name=\"V0\">{0} {1}</a></div>", chapterLabel, currentChapterPublished));
                }
                newChapterFound = false;
            }
            string s = String.Format("<div class=\"{0}\">", style);
            WriteHtml(s);

        }

        /// <summary>
        /// Finish up an HTML paragraph, which is really a div element.
        /// </summary>
        protected void EndHtmlParagraph()
        {
            if (inParagraph)
            {
                WriteHtml("</div>\r\n");
                inParagraph = false;
            }
        }

        /// <summary>
        /// Start an HTML text style, which is really a span with the style name as an attribute.
        /// </summary>
        /// <param name="style">name of text style/span class</param>
        protected void StartHtmlTextStyle(string style)
        {
            if (!ignore)
            {
                inTextStyle++;
                WriteHtml(String.Format("<span class=\"{0}\">", style));
            }
        }

        /// <summary>
        /// End an HTML text style (span)
        /// </summary>
        protected void EndHtmlTextStyle()
        {
            if (inTextStyle > 0)
            {
                WriteHtml("</span>");
                inTextStyle--;
            }
        }

        /// <summary>
        /// Turn greater than, less than, and ampersand into HTML entities
        /// </summary>
        /// <param name="s">String to make HTML-safe</param>
        /// <returns>String with greater than, less than, and ampersand encoded as HTML entities</returns>
        public static string EscapeHtml(string s)
        {
            s = s.Replace("<", "&lt;");
            s = s.Replace(">", "&gt;");
            s = s.Replace("&", "&amp;");
            return s;
        }

        /// <summary>
        /// Write HTML text (which may contain HTML elements and entities) without escaping it.
        /// </summary>
        /// <param name="text">raw HTML text to write</param>
        protected void WriteUnescapedHtmlText(string text)
        {
            if (!ignore)
            {
                if (inFootnote)
                {
                    footnotesToWrite.Append(text);
                    // Also written to main document section for pop-up.
                }
                else
                {
                    if (eatSpace)
                    {
                        text = text.TrimStart();
                        eatSpace = false;
                    }
                }
                WriteHtml(text);
            }
        }

        /// <summary>
        /// Write HTML text, escaping greater than, less than, and ampersand.
        /// </summary>
        /// <param name="text">text to write</param>
        protected void WriteHtmlText(string text)
        {
            if (!ignore)
            {
                var escapeHtml = EscapeHtml(text);
                if (inFootnote)
                {
                    if (xRef != null && !inFootnoteStyle)
                        xRef += escapeHtml; // accumluate for hot-link processing.
                    else
                        footnotesToWrite.Append(escapeHtml);
                    // Also written to main document section for pop-up.
                }
                else
                {
                    if (eatSpace)
                    {
                        text = text.TrimStart();
                        eatSpace = false;
                    }
                }
                WriteHtml(escapeHtml);
            }
        }

        /// <summary>
        /// Write an (X)HTML line break element
        /// </summary>
        protected void WriteHtmlOptionalLineBreak()
        {
            WriteHtml("<br/>");
        }

        /// <summary>
        /// Write text to HTML file if open, or queue it up for when an HTML file gets opened.
        /// </summary>
        /// <param name="s"></param>
        protected void WriteHtml(string s)
        {
            if (!ignore)
            {
                if (htm == null)
                    preVerse.Append(s);
                else
                    htm.Write(s);
            }
        }

        protected int noteNumber = 0;

        /// <summary>
        /// Return a unique note name for anchors.
        /// </summary>
        /// <returns>"FN" followed by a unique integer as a string.</returns>
        protected string noteName()
        {
            noteNumber++;
            return "FN" + noteNumber.ToString();
        }

        /// <summary>
        /// Start an HTML note with both pop-up and page-bottom notes.
        /// </summary>
        /// <param name="style">"f" for footnote and "x" for cross reference</param>
        /// <param name="marker">"+" for automatic caller, "-" for no caller (useless for a popup), or a verbatim note caller</param>
        protected virtual void StartHtmlNote(string style, string marker)
        {
            EndHtmlNote();
            if (ignoreNotes)
            {
                ignore = true;
                return;
            }
            inFootnoteStyle = false;
            inFootnote = true;
            string noteId = noteName();
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
            WriteHtml(String.Format("<a href=\"#{0}\" onclick=\"hilite('{0}')\"><span class=\"notemark\">{1}</span><span class=\"popup\">",
                noteId, marker));
            // Numeric chapter and verse numbers are used in internal references instead of the text versions, which may
            // include dashes in verse bridges.
            if ((chapterNumber >= 1) && (verseNumber >= 1))
                footnotesToWrite.Append(String.Format("<p class=\"{0}\" id=\"{1}\"><span class=\"notemark\">{2}</span><a class=\"notebackref\" href=\"#V{3}\">{4}:{5}:</a>\r\n",
                    style, noteId, marker, verseNumber.ToString(), currentChapterPublished, currentVersePublished));
            else
                footnotesToWrite.Append(String.Format("<p class=\"{0}\" id=\"{1}\"><span class=\"notemark\">{2}</span><a class=\"notebackref\" href=\"#V1\">^</a>\r\n",
                    style, noteId, marker, verseNumber.ToString(), currentChapterPublished, currentVersePublished));

        }

        /// <summary>
        /// Finish writing an HTML note
        /// </summary>
        protected virtual void EndHtmlNote()
        {
            if (inFootnote)
            {
                EndHtmlNoteStyle();
                WriteHtml("</span></a>");   // End popup text
                if (xRef != null)
                    footnotesToWrite.Append(ConvertCrossRefsToHotLinks(xRef));
                xRef = null;
                footnotesToWrite.Append("</p>\r\n");    // End footnote paragraph
                inFootnote = false;
            }
        }

        /// <summary>
        /// Start an HTML note style (implemented as a span with the style name as a class name)
        /// </summary>
        /// <param name="style">style name</param>
        protected void StartHtmlNoteStyle(string style)
        {
            EndHtmlNoteStyle();
            inFootnoteStyle = true;
            footnotesToWrite.Append(String.Format("<span class=\"{0}\">", style)); // for bottom of chapter
        }

        /// <summary>
        /// End an HTML note style (span)
        /// </summary>
        protected void EndHtmlNoteStyle()
        {
            if (inFootnoteStyle)
            {
                footnotesToWrite.Append("</span>");
                inFootnoteStyle = false;
            }
        }

        protected bool inHeader = true;
        protected bool inTable = false;
        protected bool inTableRow = false;
        protected bool inTableCol = false;
        protected bool inTableBold = false;

        /// <summary>
        /// Start an HTML table
        /// </summary>
        protected void StartHtmlTable()
        {
            EndHtmlParagraph();
            if (!inTable)
            {
                WriteHtml("<table border=\"2\" cellpadding=\"2\" cellspacing=\"2\"><tbody>\r\n");
                inTable = true;
            }
        }

        /// <summary>
        /// Insert a picture with a caption
        /// </summary>
        /// <param name="figFileName">name of file to display</param>
        /// <param name="figCopyright">copyright message to display</param>
        /// <param name="figCaption">caption to display</param>
        /// <param name="figReference">verse(s) this figure illustrates</param>
        protected virtual void insertHtmlPicture(string figFileName, string figCopyright, string figCaption, string figReference)
        {
            figFileName = CheckPicture(figFileName);
            if (figFileName.Length > 4)
            {
                WriteHtml(String.Format("<div class=\"figure\"><img src=\"{0}\"><br/><span class=\"figcopr\">{1}</br>" +
                   "</span><span class=\"figref\">{2}</span><span class=\"figCaption\"> {3}</span></div>", figFileName, figCopyright, figReference, figCaption));
            }
        }


        /// <summary>
        /// End an HTML table column
        /// </summary>
        protected void EndHtmlTableCol()
        {
            if (inTableBold)
            {
                WriteHtml("</b>");
                inTableBold = false;
            }
            if (inTableCol)
            {
                WriteHtml("</td>");
                inTableCol = false;
            }
        }

        /// <summary>
        /// End and HTML table row
        /// </summary>
        protected void EndHtmlTableRow()
        {
            EndHtmlTableCol();
            if (inTableRow)
            {
                WriteHtml("</tr>");
                inTableRow = false;
            }
        }

        /// <summary>
        /// End and HTML table
        /// </summary>
        protected void EndHtmlTable()
        {
            EndHtmlTableRow();
            if (inTable)
            {
                WriteHtml("</tbody></table>\r\n");
                inTable = false;
            }
        }

        protected int lastChapterNumber = -1;

        /// <summary>
        /// Close the last HTML file if one was open.
        /// </summary>
        protected virtual void LeaveHeader()
        {
            if (inHeader || (lastChapterNumber != chapterNumber))
            {
                inHeader = false;
                lastChapterNumber = chapterNumber;
                string chap;
                if (currentBookAbbrev == "PSA")
                    chap = String.Format("{0}{1}", currentBookAbbrev, chapterNumber.ToString("000"));
                else
                    chap = String.Format("{0}{1}", currentBookAbbrev, chapterNumber.ToString("00"));
                bookRecord.chapterFiles.Add(chap);
                //chapterFileList.Add(chap);
                //CloseHtmlFile();
            }
        }

        protected bool inChapter = false;

        protected virtual void EndChapter()
        {
            if (inChapter)
            {
                inChapter = false;
            }
        }

        /// <summary>
        /// Process a chapter tag
        /// </summary>
        protected virtual void ProcessChapter()
        {
            EndChapter();
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
            chapterOsisId = bookOsisId + "." + chapterNumber.ToString();
            verseOsisId = chapterOsisId + ".0";
            chopChapter = true;
            newChapterFound = true;
            CloseHtmlFile();
            chapterFileIndex++;
            inHeader = false;
            inChapter = true;
        }

        /// <summary>
        /// Start-of-chapter processing for when there is no actual chapter
        /// </summary>
        protected void VirtualChapter()
        {
            currentChapter = "1";
            currentChapterPublished = fileHelper.LocalizeDigits(currentChapter);
            chapterNumber = 1;
            currentChapterAlternate = String.Empty;
            currentVerse = currentVersePublished = currentVerseAlternate = String.Empty;
            verseNumber = 0;
            chopChapter = true;
            if (!bookInfo.isPeripheral(currentBookAbbrev))
                newChapterFound = true;
            CloseHtmlFile();
            chapterFileIndex++;
        }

        /// <summary>
        /// Process a verse marker
        /// </summary>
        protected virtual void ProcessVerse()
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
            verseOsisId = chapterOsisId + "." + verseNumber.ToString();
            StartVerse();
        }


        /// <summary>
        /// Creates a new USFX file that has the local (location-specific) and global changes made to it
        /// that are specified in the globalSubstFile and localSubstFile. In those two files, the separator
        /// character for each line is whatever character is the first one on the line, which can be any
        /// printing Unicode character that isn't contained within any of the fields on the line.
        /// </summary>
        /// <param name="inFile">USFX Scripture input file</param>
        /// <param name="outFile">USFX Scripture output file with specified changes made</param>
        /// <param name="globalSubstFile">Regular expression global substitutions, with each line containing
        /// a separator character, match string, separator character, replacement string, separator character.
        /// Anything after the 3rd separator character is ignored as a comment. For example:
        /// /(\W)thee(\W)/$1you$2/ This is a comment.
        /// /(\W)thou(\W)/$1you$2/
        /// </param>
        /// <param name="localSubstFile">Plain text location-based one-time substitutions, with each line
        /// containing a separator character, book abbreviation, separator character, chapter number,
        /// separator character, verse number, separator character, 'N' if in a footnote, otherwise space,
        /// separator character, find text, separator character, replace text, separator character, optional
        /// comment. Lines must be in canonical order, as written in the input USFX file. For example:
        /// ,LEV,20,26, ,Yahweh,the LORD, This is a comment.
        /// ,LEV,21,1, ,Yahweh,The LORD,
        /// </param>
        /// <returns>true iff success</returns>
        public bool FilterUsfx(string inFile, string outFile, string localSubstFile, bool includeApocrypha)
        {
            // Logit.WriteLine("Filtering from " + inFile + " to " + outFile + " using " + localSubstFile);
            if (includeApocrypha)
                Logit.WriteLine("Including Apocrypha.");
            bool result = false;
            bool doLocalSubst = false;
            bool lookInFootnote = false;
            bool includeThis = true;
            string localSubstLine = String.Empty;
            string[] localSubstParts = { "", "", "", "", "", "" };
            string phrase;
            StreamReader dasr = null;
            string daList = String.Empty;
            XmlTextWriter usfxOut;
            chapterNumber = verseNumber = 0;
            currentBookAbbrev = currentBookTitle = currentChapterPublished = String.Empty;
            currentChapter = currentFileName = currentVerse = languageCode = String.Empty;
            inFootnote = inFootnoteStyle = false;
            inTextStyle = 0;
            inParagraph = chopChapter = false;
            bookRecord = (BibleBookRecord)bookInfo.books["FRT"];
            try
            {
                if ((localSubstFile != null) && (File.Exists(localSubstFile)))
                {
                    dasr = new StreamReader(localSubstFile);
                    localSubstLine = dasr.ReadLine();
                    localSubstParts = localSubstLine.Split(new char[] { localSubstLine[0] });
                    lookInFootnote = (localSubstParts[4] == "N");
                    doLocalSubst = true;
                }
                usfx = new XmlTextReader(inFile);
                usfx.WhitespaceHandling = WhitespaceHandling.All;
                usfxOut = new XmlTextWriter(outFile, Encoding.UTF8);


                while (usfx.Read())
                {
                    // Console.Write("{0} {1}:{2} {3}       \r", currentBookAbbrev, currentChapter, currentVerse, localSubstLine);

                    if (usfx.NodeType == XmlNodeType.Element)
                    {

                        level = GetNamedAttribute("level");
                        style = GetNamedAttribute("style");
                        sfm = GetNamedAttribute("sfm");
                        caller = GetNamedAttribute("caller");
                        id = GetNamedAttribute("id");

                        switch (usfx.Name)
                        {
                            case "book":
                                if (id.Length > 2)
                                {
                                    currentBookAbbrev = id;
                                    bookRecord = (BibleBookRecord)bookInfo.books[currentBookAbbrev];
                                    bookOsisId = bookRecord.osisName;
                                }
                                bookRecord.chapterFiles = new ArrayList();
                                currentBookHeader = vernacularLongTitle = String.Empty;
                                currentChapter = currentVerse = "0";
                                chapterNumber = 0;
                                verseNumber = 0;
                                if ((bookRecord == null) || (bookRecord.testament == "a") || (bookRecord.testament == "x"))
                                {
                                    includeThis = includeApocrypha;
                                    // if (includeThis)
                                    //     Console.WriteLine("Including {0}", currentBookAbbrev);
                                    // else
                                    //     Console.WriteLine("Skipping {0}", currentBookAbbrev);
                                }
                                else
                                {
                                    includeThis = true;
                                    // Console.WriteLine("Processing {0}", currentBookAbbrev);
                                }

                                break;
                            case "c":
                                currentChapter = id;
                                currentVerse = "0";
                                verseNumber = 0;
                                int chNum;
                                if (Int32.TryParse(id, out chNum))
                                    chapterNumber = chNum;
                                else
                                    chapterNumber++;
                                bookRecord.actualChapters = Math.Max(bookRecord.actualChapters, chapterNumber);
                                chapterOsisId = bookOsisId + "." + chapterNumber.ToString();
                                break;
                            case "v":
                                currentVerse = id;
                                if (!usfx.IsEmptyElement)
                                {
                                    usfx.Read();
                                    if (usfx.NodeType == XmlNodeType.Text)
                                    {
                                        currentVersePublished = fileHelper.LocalizeDigits(usfx.Value.Trim());
                                    }
                                }
                                int vnum;
                                if (Int32.TryParse(id, out vnum))
                                {
                                    verseNumber = vnum;
                                }
                                else
                                {
                                    verseNumber++;
                                }
                                verseOsisId = chapterOsisId + "." + verseNumber.ToString();
                                break;
                            case "f":
                                inFootnote = true;
                                break;
                        }
                        if (includeThis)
                        {
                            usfxOut.WriteStartElement(usfx.Name);
                            usfxOut.WriteAttributes(usfx, false);
                            if (usfx.IsEmptyElement)
                                usfxOut.WriteEndElement();
                        }
                    }
                    if (includeThis)
                    {
                        if ((usfx.NodeType == XmlNodeType.EndElement) && (usfx.NodeType != XmlNodeType.Element))
                        {
                            usfxOut.WriteEndElement();
                            if (usfx.Name == "f")
                                inFootnote = false;
                        }
                        else if (usfx.NodeType == XmlNodeType.Text)
                        {
                            phrase = usfx.Value;

                            // Try local (specific to a verse) substitution first.
                            while (doLocalSubst && (currentBookAbbrev == localSubstParts[1]) &&
                                (currentChapter == localSubstParts[2]) &&
                                (currentVerse == localSubstParts[3]) &&
                                (lookInFootnote == inFootnote) &&
                                phrase.Contains(localSubstParts[5]))
                            {
                                phrase = fileHelper.ReplaceFirst(phrase, localSubstParts[5], localSubstParts[6]);
                                if (dasr.EndOfStream)
                                    doLocalSubst = false;
                                localSubstLine = dasr.ReadLine();
                                if ((localSubstLine == null) || (localSubstLine.Length < 6))
                                {
                                    doLocalSubst = false;
                                }
                                else
                                {
                                    localSubstParts = localSubstLine.Split(new char[] { localSubstLine[0] });
                                    if (localSubstParts.Length < 6)
                                        doLocalSubst = false;
                                    else
                                        lookInFootnote = (localSubstParts[4] == "N");
                                }
                                if (!doLocalSubst)
                                    dasr.Close();
                            }


                            // Write out changed text.
                            if (phrase != null)
                                usfxOut.WriteString(phrase);
                        }
                        else if ((usfx.NodeType == XmlNodeType.Whitespace) || (usfx.NodeType == XmlNodeType.SignificantWhitespace))
                        {
                            usfxOut.WriteWhitespace(usfx.Value);

                        }
                        else if (usfx.NodeType == XmlNodeType.XmlDeclaration)
                        {
                            usfxOut.WriteStartDocument();
                        }
                        else if (usfx.NodeType != XmlNodeType.Element)
                        {
                            usfxOut.WriteRaw(usfx.ReadString());
                        }
                    }
                }
                usfxOut.Close();
                usfx.Close();
                result = true;
            }
            catch (Exception ex)
            {
                Logit.WriteError(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }

            return result;
        }

        public string indexDateStamp = String.Empty;
        public static string conversionProgress = String.Empty;


        /// <summary>
        ///  (Localizable) string to display as the text of the link the Concordance.
        /// </summary>
        public string ConcordanceLinkText { get; set; }

        /// <summary>
        /// Return the file we should link to in order to show the specified main file.
        /// In the default layout, this is just the file itself, which occupies the whole window.
        /// </summary>
        /// <param name="mainFileName"></param>
        /// <returns></returns>
        protected virtual string MainFileLinkTarget(string mainFileName)
        {
            return mainFileName;
        }

        protected virtual string MainFileLinkTarget(string bookAbbrev, string chapter)
        {
            return MainFileLinkTarget(string.Format("{0}{1}.htm", bookAbbrev, chapter));
        }

        protected bool inStrongs = false;

        protected virtual void StartStrongs(string StrongsNumber, string plural)
        {
            inStrongs = true;
        }

        protected virtual void EndStrongs()
        {
            inStrongs = false;
        }

        protected CrossReference xref;
        protected bool doXrefMerge = false;
        public void MergeXref(string xrefName)
        {
            doXrefMerge = false;
            try
            {
                if ((xrefName != null) && File.Exists(xrefName))
                {
                    xref = new CrossReference(xrefName);
                    doXrefMerge = true;
                }
            }
            catch (Exception ex)
            {
                Logit.WriteError(ex.Message);
                throw;
            }
        }

        protected virtual void WriteContentsPage()
        {
            hasContentsPage = bookRecord.toc.Length > 0;
            currentChapter = "";
            if (hasContentsPage)
            {
                currentChapterPublished = "0";
                OpenHtmlFile();
                htm.WriteLine("<div class=\"toc\"><a href=\"index.htm\">^</a></div>\r\n{0}",
                    bookRecord.toc.ToString());
                CloseHtmlFile();
            }
        }

        protected virtual void EndVerse()
        {
            if ((preVerse.Length > 0) && (htm != null))
            {
                htm.WriteLine(preVerse.ToString());
                preVerse = new StringBuilder(String.Empty);
            }
        }

        protected virtual void WriteCopyrightPage(string chapFormat, string licenseHtml, string goText)
        {
            // Copyright page
            currentBookAbbrev = "CPR";
            bookListIndex = -1;
            OpenHtmlFile("copyright.htm");
            htm.WriteLine("<div class=\"main\">");
            bookListIndex = 0;
            bookRecord = (BibleBookRecord)bookList[0];
            if (bookRecord.tla.CompareTo("PSA") == 0)
                chapFormat = "000";
            else
                chapFormat = "00";
            htm.WriteLine("<div class=\"toc\"><a href=\"{0}.htm\">{1}</a></div>",
                chapterFileList[0], goText);
            htm.WriteLine(licenseHtml);
            htm.WriteLine("<p>&nbsp;<br/><br/></p>");
            if (indexDateStamp != String.Empty)
                htm.WriteLine("<div class=\"fine\">{0}</div>", indexDateStamp);
            CloseHtmlFile();
            indexDateStamp = String.Empty;

            // Audio/download copyright page
            OpenHtmlFile("copr.htm", false, true);
            htm.WriteLine("<div class=\"main\">");
            htm.WriteLine("<div class=\"toc\">{0}</div></br>", sourceLink);
            htm.WriteLine(licenseHtml);
            if (indexDateStamp != String.Empty)
                htm.WriteLine("<div class=\"fine\">{0}</div>", indexDateStamp);
            CloseHtmlFile();
        }

        public string translationIdentifier;
        public string languageIdentifier;

        public string sourceLink = String.Empty;
        public string textDirection = "ltr";

        /// <summary>
        /// Converts the USFX file usfxName to a set of HTML files, one file per chapter, in the
        /// directory htmlDir, with reference to CSS files in cssDir. The output file names will
        /// be the standard 3-letter abbreviation for the book, followed by a dash and the 2 or
        /// 3 digit chapter number, followed optionally by namePart, then .htm.
        /// </summary>
        /// <param name="usfxName">Name of the USFX file to convert to HTML files</param>
        /// <param name="htmlDir">Directory to put the HTML files into</param>
        /// <param name="languageName">Name of the language for page headers</param>
        /// <param name="languageId">New Ethnologue 3-letter code for this language</param>
        /// <param name="chapterLabelName">Vernacular name for "Chapter"</param>
        /// <param name="psalmLabelName">Vernacular name for "Psalm"</param>
        /// <param name="copyrightLink">HTML for copyright link, like &lt;href a="copyright.htm"&gt;©&lt;/a&gt;</param>
        /// <param name="homeLink">HTML for link to home page</param>
        /// <param name="footerHtml">HTML for footer "fine print" text</param>
        /// <returns>true iff the conversion succeeded</returns>
        public virtual bool ConvertUsfxToHtml(string usfxName, string htmlDir, string languageName, string languageId, string translationId,
            string chapterLabelName, string psalmLabelName, string copyrightLink, string homeLink, string footerHtml,
            string indexHtml, string licenseHtml, bool skipHelps, string goText)
        {
            bool result = false;
            bool inUsfx = false;
            string figDescription = String.Empty;
            string figFileName = String.Empty;
            string figSize = String.Empty;
            string figLocation = String.Empty;
            string figCopyright = String.Empty;
            string figCaption = String.Empty;
            string figReference = String.Empty; // Figure parameters
            translationIdentifier = translationId;
            languageIdentifier = languageId;
            ignoreIntros = ignoreNotes = skipHelps;
            footerTextHTML = footerHtml;
            copyrightLinkHTML = copyrightLink;
            homeLinkHTML = homeLink;
            htmDir = htmlDir;
            langId = languageId;
            langName = languageName;
            chapterLabel = chapterLabelName;
            psalmLabel = psalmLabelName;
            chapterNumber = verseNumber = 0;
            bookList.Clear();
            bookOsisId = chapterOsisId = verseOsisId = String.Empty;
            currentBookAbbrev = currentBookTitle = currentChapterPublished = wordForChapter = String.Empty;
            currentChapter = currentFileName = currentVerse = languageCode = String.Empty;
            ChapterInfo ci = new ChapterInfo();
            inFootnote = inFootnoteStyle = false;
            inTextStyle = 0;
            inParagraph = chopChapter = false;
            bookRecord = (BibleBookRecord)bookInfo.books["FRT"];
            // This flag is set true during the first pass through the USFX file (in preparation for generating navigation files)
            // if any apocryphal books are encountered. If this does not happen (that is, there is no apocryphal material), in the 
            // main pass we ignore elements dc, xdc, and fdc.
            // I (JohnT) don't know WHY these elements should be ignored unless the translation includes apocrypha, but I confirmed
            // with Michael that it is intentional.
            // Michael: The rationale behind the use of the dc, xdc, and fdc elements is to facilitate creating Bibles with and
            // without Apocrypha/Deuterocanon from a common source. The United Bible Societies and other customers and partners of
            // ours operate in an ecumenical framework where this is important. When producing a volume limited to the 66 books
            // of the Old and New Testaments, it is best to leave out cross references leading to the other 22 (or more, depending
            // on which denominational authority you ask) books. However, those cross references are useful when those other books
            // are present in a volume.

            bool containsDC = false;
            newChapterFound = false;
            bool foundThisBook;
            ignore = false;
            StringBuilder toc = new StringBuilder();
            string chapFormat = "00";
            int i;

            footnotesToWrite = new StringBuilder(String.Empty);
            preVerse = new StringBuilder(String.Empty);
            try
            {
                usfxFileName = usfxName;
                bookInfo.ReadUsfxVernacularNames(usfxFileName);
                if (usfx != null)
                    usfx.Close();
                if ((htmlDir == null) || (htmlDir.Length < 1))
                {
                    Logit.WriteError("HTML output directory must be specified.");
                    return false;
                }

                // Pass 1: navigation generation

                usfx = new XmlTextReader(usfxName);
                usfx.WhitespaceHandling = WhitespaceHandling.Significant;
                while (usfx.Read())
                {
                    if (usfx.NodeType == XmlNodeType.Element)
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
                            case "book":
                                hasContentsPage = false;
                                inHeader = true;
                                currentBookHeader = vernacularLongTitle = String.Empty;
                                if (id.Length > 2)
                                {
                                    currentBookAbbrev = id;
                                    bookRecord = (BibleBookRecord)bookInfo.books[currentBookAbbrev];
                                    bookRecord.chaptersFound = new ArrayList(151);
                                    bookOsisId = bookRecord.osisName;
                                    chapterOsisId = bookOsisId + ".0";
                                    verseOsisId = chapterOsisId + ".0";
                                    if (bookRecord == null)
                                    {
                                        Logit.WriteError("Cannot process unknown book: " + currentBookAbbrev);
                                        return false;
                                    }
                                    foundThisBook = false;
                                    for (i = 0; (i < bookInfo.publishArray.Length) && (bookInfo.publishArray[i] != null) && !foundThisBook; i++)
                                    {
                                        if (bookInfo.publishArray[i].tla == bookRecord.tla)
                                            foundThisBook = true;
                                    }
                                    if (!foundThisBook)
                                    {
                                        bookRecord.isPresent = false;
                                        while ((usfx.Name != "book") || (usfx.NodeType != XmlNodeType.EndElement))
                                        {   // Skip book
                                            usfx.Read();
                                        }
                                    }
                                    else
                                    {
                                        bookRecord.toc = new StringBuilder();
                                        bookRecord.chapterFiles = new ArrayList();
                                    }
                                }
                                if (id.CompareTo("PSA") == 0)
                                    chapFormat = "000";
                                else
                                    chapFormat = "00";
                                chapterNumber = 0;
                                verseNumber = 0;
                                if (bookInfo.isApocrypha(id))
                                    containsDC = true; // if we have any apocrypha, this is set true for use in the next pass
                                break;
                            case "id":
                                if (id.Length > 2)
                                    currentBookAbbrev = id;
                                bookRecord = (BibleBookRecord)bookInfo.books[currentBookAbbrev];
                                if (!usfx.IsEmptyElement)
                                    ignore = true;
                                currentChapter = "0";
                                chapterNumber = 0;
                                currentChapterPublished = fileHelper.LocalizeDigits(currentChapter);
                                currentChapterAlternate = String.Empty;
                                currentVerse = "0";
                                currentVersePublished = currentVerseAlternate = String.Empty;
                                verseNumber = 0;
                                footNoteCall.reset();
                                break;
                            case "p":
                                if (sfm.CompareTo("mt") == 0)
                                {
                                    usfx.Read();
                                    if (usfx.NodeType == XmlNodeType.Text)
                                    {
                                        if (vernacularLongTitle.Length > 0)
                                            vernacularLongTitle = vernacularLongTitle + " " + EscapeHtml(usfx.Value.Trim());
                                        else
                                            vernacularLongTitle = EscapeHtml(usfx.Value.Trim());
                                    }
                                }
                                else if (sfm.CompareTo("ms") == 0)
                                {
                                    usfx.Read();
                                    if (usfx.NodeType == XmlNodeType.Text)
                                    {
                                        bookRecord.toc.Append(String.Format("<div class=\"toc1\"><a target=\"_top\" href=\"{0}#V{1}\">{2}</a></div>\r\n",
                                            MainFileLinkTarget(currentBookAbbrev, Math.Max(1, chapterNumber).ToString(chapFormat)),
                                            verseNumber.ToString(), usfx.Value.Trim()));
                                    }
                                    hasContentsPage = true;
                                }
                                else
                                {
                                    if (chapterNumber == 0)
                                        VirtualChapter();
                                    LeaveHeader();
                                }
                                break;
                            case "h":
                                usfx.Read();
                                if (usfx.NodeType == XmlNodeType.Text)
                                {
                                    currentBookHeader = EscapeHtml(usfx.Value.Trim());
                                    bookRecord.toc.Append(String.Format("<div class=\"toc1\"><a target=\"_top\" href=\"{0}\">{1}</a></div>\r\n",
                                        MainFileLinkTarget(currentBookAbbrev, chapFormat.Substring(1) + "1"), currentBookHeader));
                                }

                                break;
                            case "s":
                            case "d":
                                usfx.Read();
                                if (usfx.NodeType == XmlNodeType.Text)
                                {
                                    bookRecord.toc.Append(String.Format("<div class=\"toc2\"><a target=\"_top\" href=\"{0}#V{1}\">{2}</a></div>\r\n",
                                        MainFileLinkTarget(currentBookAbbrev, Math.Max(1, chapterNumber).ToString(chapFormat)),
                                        verseNumber.ToString(), usfx.Value.Trim()));
                                }
                                hasContentsPage = true;
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
                            case "c":
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
                                chapterOsisId = bookOsisId + "." + chapterNumber.ToString();
                                verseOsisId = chapterOsisId + ".0";
                                bookRecord.actualChapters = Math.Max(bookRecord.actualChapters, chapterNumber);
                                ci = new ChapterInfo();
                                ci.chapterInteger = chapterNumber;
                                ci.alternate = ci.actual = currentChapter;
                                ci.published = currentChapterPublished;
                                ci.osisChapter = chapterOsisId;
                                bookRecord.chaptersFound.Add(ci);
                                LeaveHeader();
                                chopChapter = true;
                                break;
                            case "cp":
                                if (!usfx.IsEmptyElement)
                                {
                                    usfx.Read();
                                    if (usfx.NodeType == XmlNodeType.Text)
                                    {
                                        currentChapterPublished = fileHelper.LocalizeDigits(usfx.Value.Trim());
                                        ci.published = currentChapterPublished;
                                    }
                                }
                                break;
                            case "ca":
                                if (!usfx.IsEmptyElement)
                                {
                                    usfx.Read();
                                    if (usfx.NodeType == XmlNodeType.Text)
                                    {
                                        currentChapterAlternate = usfx.Value.Trim();
                                        ci.alternate = currentChapterAlternate;
                                    }
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
                                int vnum;
                                if (Int32.TryParse(id, out vnum))
                                {
                                    verseNumber = vnum;
                                }
                                else
                                {
                                    verseNumber++;
                                }
                                verseOsisId = chapterOsisId + "." + verseNumber.ToString();
                                break;
                        }
                    }
                    else if (usfx.NodeType == XmlNodeType.EndElement)
                    {
                        switch (usfx.Name)
                        {
                            case "book":
                                // Write book table of contents
                                if (!hasContentsPage)
                                    bookRecord.toc.Length = 0;
                                bookRecord.vernacularName = vernacularLongTitle;
                                break;
                            case "d":
                            case "s":
                                if (bookRecord.testament.CompareTo("x") == 0)
                                {
                                    if (chapterNumber == 0)
                                    {
                                        chapterNumber = 1;
                                        verseNumber = 1;
                                    }
                                    verseNumber++;
                                }
                                break;
                        }
                    }
                    else if ((usfx.NodeType == XmlNodeType.Text) && (bookRecord != null) && (bookRecord.tla == currentBookAbbrev))
                    {   // We don't count a book as present unless there is some text in it.
                        bookRecord.isPresent = true;
                    }
                    conversionProgress = "navigation " + currentBookAbbrev + " " + currentChapter + ":" + currentVerse;
                    //System.Windows.Forms.Application.DoEvents();
                }
                usfx.Close();

                try
                {

                    for (i = 0; (i < BibleBookInfo.MAXNUMBOOKS) && (bookInfo.publishArray[i] != null); i++)
                    {
                        if (bookInfo.publishArray[i].isPresent && (bookInfo.publishArray[i].chapterFiles != null))
                        {   // This book is in the input files and contains at least one character of text.
                            bookList.Add(bookInfo.publishArray[i]);
                            foreach (string chapFileName in bookInfo.publishArray[i].chapterFiles)
                            {
                                if (chapFileName != null)
                                    chapterFileList.Add(chapFileName);
                            }
                        }
                    }

                }
                catch (Exception err)
                {
                    Logit.WriteError(err.Message + " creating chapter file list.");
                    Logit.WriteError(err.StackTrace);
                    throw;
                }
                if (bookList.Count < 1)
                {
                    Logit.WriteError("No books found to convert in " + usfxName);
                    return false;
                }

                // Index page
                GenerateIndexFile(translationId, indexHtml, goText);

                WriteCopyrightPage(chapFormat, licenseHtml, goText);

                // Pass 2: content generation

                usfx = new XmlTextReader(usfxName);
                usfx.WhitespaceHandling = WhitespaceHandling.All;

                chapterFileIndex = 0;
                bookListIndex = 0;
                while (usfx.Read())
                {
                    conversionProgress = "Generating HTML " + currentBookAbbrev + " " + currentChapter + ":" + currentVerse;
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
                                                figDescription = EscapeHtml(usfx.Value.Trim());
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
                                                figCopyright = EscapeHtml(usfx.Value.Trim());
                                        }
                                        break;
                                    case "caption":
                                        if (!usfx.IsEmptyElement)
                                        {
                                            usfx.Read();
                                            if (usfx.NodeType == XmlNodeType.Text)
                                                figCaption = EscapeHtml(usfx.Value.Trim());
                                        }
                                        break;
                                    case "reference":
                                        if (!usfx.IsEmptyElement)
                                        {
                                            usfx.Read();
                                            if (usfx.NodeType == XmlNodeType.Text)
                                                figReference = EscapeHtml(usfx.Value.Trim());
                                        }
                                        break;

                                    case "book":
                                        currentBookAbbrev = id;
                                        chapterNumber = 0;
                                        verseNumber = 0;
                                        bookRecord = (BibleBookRecord)bookInfo.books[id];
                                        bookOsisId = bookRecord.osisName;
                                        chapterOsisId = bookOsisId + ".0";
                                        verseOsisId = chapterOsisId + ".0";
                                        currentBookHeader = bookRecord.vernacularShortName;
                                        if (!bookRecord.isPresent)
                                        {   // Skip book not on publication list or containing no chapters.
                                            while ((usfx.Name != "book") || (usfx.NodeType != XmlNodeType.EndElement))
                                            {
                                                usfx.Read();
                                            }
                                        }
                                        else
                                        {
                                            WriteContentsPage();
                                        }
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
                                            currentBookHeader = EscapeHtml(usfx.Value.Trim());
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
                                        ProcessParagraphStart(beforeVerse);
                                        if (style == "Parallel Passage Reference")
                                            parallelPassage = ""; // start accumulating cross-ref data
                                        break;
                                    case "q":
                                    case "qs":  // qs is really a text style with paragraph attributes, but HTML/CSS can't handle that.
                                    case "b":
                                        ProcessParagraphStart(false);
                                        break;
                                    case "mt":
                                    case "d":
                                    case "s":
                                        ProcessParagraphStart(true);
                                        break;
                                    case "hr":
                                        EndHtmlParagraph();
                                        WriteHtml("<hr />");
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
                                    case "ve":
                                        EndVerse();
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
                                    case "w":
                                    case "zw":
                                        StartStrongs(GetNamedAttribute("s"), GetNamedAttribute("plural"));
                                        if (usfx.IsEmptyElement)
                                            EndStrongs();
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
                            if (parallelPassage != null)
                                parallelPassage = parallelPassage + usfx.Value;
                            else
                                WriteHtmlText(usfx.Value);
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
                                            insertHtmlPicture(figFileName, figCopyright, figCaption, figReference);
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
                                                }
                                            }
                                        }
                                        CloseHtmlFile();
                                        bookListIndex++;
                                        break;
                                    case "p":
                                        if (parallelPassage != null)
                                        {
                                            string crossRef = parallelPassage;
                                            parallelPassage = null; // stop accumulating cross ref info!
                                            // Escape it BEFORE we add the cross-ref markup, which may well include
                                            // special characters.
                                            WriteUnescapedHtmlText(ConvertCrossRefsToHotLinks(EscapeHtml(crossRef)));
                                        }
                                        goto case "mt";
                                    case "q":
                                    case "qs":  // qs is really a text style with paragraph attributes, but HTML/CSS can't handle that.
                                    case "b":
                                    case "mt":
                                        // also done for case "p" after possibly converting cross refs.
                                        EndHtmlParagraph();
                                        if (chopChapter)
                                            CloseHtmlFile();
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
                                            }
                                            verseNumber++;
                                            htm.Write("<a name=\"C{0}V{1}\"></a>",
                                                chapterNumber.ToString(), verseNumber.ToString());
                                        }
                                        EndHtmlParagraph();
                                        if (chopChapter)
                                            CloseHtmlFile();
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
                                    case "zw":
                                    case "w":
                                        EndStrongs();
                                        break;

                                }
                            }
                            break;
                    }
                }
                result = true;
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
            conversionProgress = String.Empty;
            return result;
        }

        protected virtual void GenerateIndexFile(string translationId, string indexHtml, string goText)
        {
            string chapFormat;
            currentChapter = currentChapterPublished = "";
            chapterNumber = 0;
            bookListIndex = -1;
            currentBookAbbrev = string.Empty;
            currentBookHeader = string.Empty;
            OpenHtmlFile(IndexFileName);
            bookListIndex = 0;

            /*if ((homeLinkHTML != null) && (homeLinkHTML.Trim().Length > 0))
                {
                    htm.WriteLine("<div class=\"dcbookLine\">{0}</div>", homeLinkHTML);
                }
                */
            htm.WriteLine("<div class=\"bookList\">");
            int i;
            BibleBookRecord br;
            string buttonClass = "bookLine";
            string contentsName;
            //string chapFmt;
            for (i = 0; i < bookList.Count; i++)
            {
                br = (BibleBookRecord)bookList[i];
                if ((br.testament.CompareTo("a") == 0) || (br.testament.CompareTo("x") == 0))
                    buttonClass = "dcbookLine";
                else
                    buttonClass = "bookLine";
                if (br.tla == "PSA")
                    chapFormat = "000";
                else
                    chapFormat = "00";
                contentsName = br.tla + chapFormat + ".htm";
                if (File.Exists(Path.Combine(htmDir, contentsName)))
                {
                    htm.WriteLine("<div class=\"{0}\"><a href=\"{1}\">{2}</a></div>", buttonClass, contentsName, br.vernacularShortName);
                }
                else
                {
                    foreach (string chapFile in chapterFileList)
                    {
                        if (chapFile.StartsWith(br.tla))
                        {
                            htm.WriteLine("<div class=\"{0}\"><a href=\"{1}.htm\">{2}</a></div>", buttonClass, chapFile, br.vernacularShortName);
                            break;
                        }
                    }
                }
            }
            if ((copyrightLinkHTML != null) && (copyrightLinkHTML.Trim().Length > 0))
            {
                htm.WriteLine("<div class=\"dcbookLine\">{0}</div>", copyrightLinkHTML);
            }
            htm.WriteLine("</div>"); // End of bookList div
            htm.WriteLine("<div class=\"mainindex\">");

            bookRecord = (BibleBookRecord)bookList[0];
            if (bookRecord.tla.CompareTo("PSA") == 0)
                chapFormat = "000";
            else
                chapFormat = "00";
            htm.WriteLine("<div class=\"toc\"><a href=\"{0}.htm\">{1}</a></div>",
                          chapterFileList[0], goText);
            htm.WriteLine(indexHtml, langId, translationId);
            if (GeneratingConcordance)
            {
                htm.WriteLine("<div class=\"toc1\"><a href=\"conc/treeMaster.htm\" rel=\"nofollow\">" + ConcordanceLinkText + "</a></div>");
            }
            htm.WriteLine("<p>&nbsp;<br/><br/></p>");
            if (indexDateStamp != String.Empty)
            {
                htm.WriteLine("<div class=\"fine\">{0}</div>", indexDateStamp);
            }
            CloseHtmlFile();
        }

        /// <summary>
        /// Maps from book names that occur in cross-refs to the file name prefix used for that book
        /// e.g. 1 Corinthians -> 1Co, which will cause 1 Corinthians 3:5 to map to 1Co03#V5.
        /// Client must supply this to get cross-reference conversion.
        /// </summary>
        public Dictionary<string, string> CrossRefToFilePrefixMap;

        /// <summary>
        /// Convert references to hot links. A simple input is something like "Lukas 12:3". Output would then be
        /// <a href="LUK12.htm#V3>Lucas 12:3</a>.
        /// The name to file previx conversion is performed by finding "Lukas" in CrossRefToFilePrefixMap, with the value "LUK".
        /// But it's more complicated than that; we get cases like
        /// (Mateos 26:26-29; Markus 14:22-25; Lukas 22:14-20) -- extra punctuation and multiple refs
        /// (Carita Ulang so'al Jalan Idop 4:35,39; 6:4) - name not found!
        /// (1 Korintus 5:1-13) - range!
        /// (Utusan dong pung Carita 22:6-16, 26:12-18) - list of refs in same book
        /// " Efesus 5:22, Kolose 3:18" - commas separate complete refs!
        /// "Hahuu (Jénesis , Kejadian ) 15:13-14; Ézodu (Keluaran ) 3:12" - book name is complex and has comma!
        /// The algorithm is:
        /// 0. Wherever a recognized book name occurs, change it to something definitely not containing problem
        /// punctuation: #%#%bookN#
        /// 1. Split the string at semi-colons or commas; handle each one independently,
        /// except if we get a book or chapter, remember for subsequent ones.
        /// 2. Each item from the above is split at commas. Consider all to come from same book and chapter, if later ones don't specify those.
        /// 3. In first of each comma group, search for a match for known book name. If found, or if we are carrying a book name forward, we can make a hot link.
        /// 4. Convert occurrences of #%#%bookN back.
        /// </summary>
        /// <param name="chunk1"></param>
        /// <returns></returns>
        protected string ConvertCrossRefsToHotLinks(string chunk1)
        {
            if (CrossRefToFilePrefixMap == null || CrossRefToFilePrefixMap.Count == 0)
                return chunk1;
            string chunk = chunk1;
            int ibook = 0;
            Dictionary<string, string> subsBookToFile = new Dictionary<string, string>(CrossRefToFilePrefixMap.Count);
            foreach (string bookName in CrossRefToFilePrefixMap.Keys)
            {
                string subskey = "#%#%book" + ibook + "#"; // final hatch to prevent book1 matching book10 on convert back
                subsBookToFile[subskey] = CrossRefToFilePrefixMap[bookName];
                chunk = chunk.Replace(bookName, subskey);
                ibook++;
            }
            string[] mainRefs = chunk.Split(';');
            StringBuilder output = new StringBuilder(chunk.Length * 5 + 50);
            // Ref may be simple verse number, chapter:verse, verse-verse, chapter:verse-verse
            Regex reRef = new Regex("[0-9]+(:[0-9]+)?(-[0-9]+)?");
            Regex reNum = new Regex("[0-9]+");
            Regex reAlpha = new Regex(@"\w");
            string fileName = ""; // empty until we get a book name match.
            // This is both a default for books that don't have chapters, and also,
            // if we find a chapter in ANY reference, we keep it for subsequent ones.
            // This handles cases like Matt 26:3,4 (which gets split into two items by the comma).
            string chap = "1";
            foreach (string item in mainRefs)
            {
                // Put back the semi-colons we split on.
                if (output.Length > 0)
                    output.Append(";");
                string[] refs = item.Split(',');
                bool fFirst = true;
                foreach (string target in refs)
                {
                    if (!fFirst)
                    {
                        output.Append(","); // put these back too.
                    }
                    string match = "";
                    int bookNameMatchOffset = -1;
                    foreach (string bookName in subsBookToFile.Keys)
                    {
                        if (bookName.Length > match.Length)
                        {
                            int matchOffsetT = target.IndexOf(bookName);
                            if (matchOffsetT >= 0)
                            {
                                bookNameMatchOffset = matchOffsetT;
                                match = bookName;
                                fileName = subsBookToFile[match];
                            }
                        }
                    }
                    if (fileName == "")
                    {
                        // haven't found a book name, here or in previous item; don't convert this item
                        output.Append(target);
                        fFirst = false;
                        continue;
                    }

                    // Look for something like a reference. Also, check that we don't have
                    // alphabetic text that did NOT match one of our books if we didn't match
                    // a book; otherwise, something like Titus 4:2; Isaiah 12:3 makes both links
                    // to Titus. Note that we take the last match for the reference, otherwise, 1 Timothy 2:4
                    // finds the '1' as the reference. Grr.
                    int startNumSearch = 0;
                    if (bookNameMatchOffset >= 0)
                        startNumSearch = bookNameMatchOffset + match.Length; // start searching after the book name if any
                    MatchCollection matches = reRef.Matches(target, startNumSearch);
                    Match m = null;
                    if (matches.Count != 0)
                        m = matches[matches.Count - 1];
                    if (m == null || (bookNameMatchOffset < 0 && reAlpha.Match(target, 0, m.Index) != Match.Empty))
                    {
                        // Nothing looks like a reference, just output what we have.
                        // Also, stop carrying book and chapter forward.
                        fileName = "";
                        chap = "1";
                        output.Append(target);
                        fFirst = false;
                        continue;
                    }
                    // Construct the anchor.
                    string[] parts = m.Value.Split(':');
                    string anchor;
                    string verse = parts[0];
                    // Do NOT reset chap unless two parts; see above.
                    if (parts.Length == 2)
                    {
                        chap = parts[0];
                        verse = parts[1];
                    }
                    verse = reNum.Match(verse).Value; // Take the first number in the verse part.
                    var htmName = HtmName(fileName, chap);
                    anchor = string.Format("{0}#V{1}", htmName, verse);

                    // The anchor starts at the beginning of the numeric reference, unless we
                    // matched a book name, in which case, start at the beginning of if.
                    int start = m.Index;
                    if (bookNameMatchOffset >= 0)
                    {
                        start = bookNameMatchOffset;
                    }
                    InsertHotLink(output, target, m, start, anchor);
                    fFirst = false;
                }
            }
            string result = output.ToString();
            ibook = 0;
            foreach (string bookName in CrossRefToFilePrefixMap.Keys)
            {
                result = result.Replace("#%#%book" + ibook + "#", bookName);
                ibook++;
            }

            return result;
        }

        /// <summary>
        /// Given a book ID and a chapter number, generate the corresponding HTML file name.
        /// </summary>
        /// <param name="bookId"></param>
        /// <param name="chap"></param>
        /// <returns></returns>
        public static string HtmName(string bookId, int chap)
        {
            string ch = chap.ToString(CultureInfo.InvariantCulture);
            if (ch.Length < 2)
                ch = "0" + ch;
            // We use 2 digits for chapter numbers in file names except for in the Psalms, where we use 3.
            if (bookId.Substring(0, 3).ToLowerInvariant() == "psa" && ch.Length < 3)
                ch = "0" + ch;
            return bookId + ch + ".htm";
        }

        /// <summary>
        /// Given a book ID and a chapter identifier string, generate the corresponding HTML file name.
        /// </summary>
        /// <param name="bookId"></param>
        /// <param name="chap"></param>
        /// <returns></returns>
        public virtual string HtmName(string bookId, string chap)
        {
            if (chap.Length < 2)
                chap = "0" + chap;
            // We use 2 digits for chapter numbers in file names except for in the Psalms, where we use 3.
            if (bookId == "PSA" && chap.Length < 3)
                chap = "0" + chap;
            return bookId + chap + ".htm";
        }

        /// <summary>
        /// Insert into the string builder a string formed by replacing (in input) from start to the end of what m matched
        /// with a hot link.
        /// The anchor to which it links is supplied; the body of the link is what was replaced.
        /// </summary>
        /// <param name="output"></param>
        /// <param name="input"></param>
        /// <param name="input"></param>
        /// <param name="m"></param>
        /// <param name="anchor"></param>
        protected void InsertHotLink(StringBuilder output, string input, Match m, int start, string anchor)
        {
            // Put anything in the input before the reference
            output.Append(input.Substring(0, start));
            // The next bit will be part of the anchor, so start it.
            output.Append(HotlinkLeadIn);
            output.Append(MainFileLinkTarget(anchor));
            output.Append("\">");
            // The bit that should be the text of the anchor: input from start to end of reference.
            output.Append(input.Substring(start, m.Index + m.Length - start));
            // terminate the anchor
            output.Append("</a>");
            // And add anything else, possibly final punctuation
            output.Append(input.Substring(m.Index + m.Length));
        }

        /// <summary>
        /// The start of a hot link. Framed html converter overrides to make the target _top.
        /// </summary>
        protected virtual string HotlinkLeadIn
        {
            get { return "<a href=\""; }
        }
    }
}
