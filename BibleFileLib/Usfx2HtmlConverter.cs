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
// project are in BibleFileLib.dll. The objects in this object library are
// called by both the command line and the GUI versions of the
// USFM to relevant converters. They may also be used by other conversion
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
        public bool commonChars = true;
        public string htmlextrasDir = String.Empty;
        public LanguageCodeInfo langCodes;
        public string languageNameInEnglish = String.Empty;
        public string shortTitle = String.Empty;
        public string fcbhId = String.Empty;
        public string projectOutputDir = String.Empty;
        public string languageNameInVernacular = String.Empty;
        public string traditionalAbbreviation = String.Empty;
        public string preferredFont;
        protected string fontClass;
        public string script = String.Empty;
        public string dialectCode = String.Empty;
        public string countries = String.Empty;
        public string certified;
        public string contentCreator;
        public string contributor;
        public bool redistributable;

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
        protected string currentVernacularAbbreviation;
        protected string currentBookTitle;
        protected string currentChapter;
        protected string currentChapterAlternate;
        protected string wordForChapter;
        protected string currentChapterPublished;
        protected string currentBCV;
        protected string vernacularLongTitle;   // From mt tags
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
        protected string previousBookId = String.Empty;
        protected string translationName = "";
        protected string langId = "";
        protected string shortLangId = "";
        protected string footerTextHTML = "";
        protected string copyrightLinkHTML = "";
        protected string homeLinkHTML = "";
        protected StringBuilder preVerse = new StringBuilder(String.Empty);
        protected string previousChapterText;
        protected string nextChapterText;
        protected ArrayList chapterFileList = new ArrayList();
        protected int chapterFileIndex = 0;
        public static ArrayList bookList = new ArrayList();
        public Options projectOptions;

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
        protected bool newChapterMarkNeeded;
        protected bool inFirstTableRow = false;
        protected StringBuilder tableFirstRow, tableDeclaration;

        public BibleBookInfo bookInfo = new BibleBookInfo();
        protected BibleBookRecord bookRecord;

        // Used in Browser Bible audio integration:
        public string fcbhDramaNt;
        public string fcbhDramaOt;
        public string fcbhAudioNt;
        public string fcbhAudioOt;
        public string fcbhPortion;
        public string country;
        public string numbers;
        public string countryCode;
        
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
            langCodes = new LanguageCodeInfo();
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

        public FootNoteCaller footNoteCall = new FootNoteCaller("* † ‡ § ** †† ‡‡ §§ *** ††† ‡‡‡ §§§");

        public FootNoteCaller xrefCall = new FootNoteCaller("✡");

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
            if (usfx.IsEmptyElement && usfx.Name != "nb")
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
        protected int allChapterIndex;
        protected string nextid;
        protected string previd;

        /// <summary>
        /// Write navigational links to get to another book or another chapter from here. Also include links to site home, previous chapter, and next chapter.
        /// </summary>
        protected virtual void WriteNavButtons()
        {
            int i;
            string s = String.Empty;
            StringBuilder sb = new StringBuilder(s);
            StringBuilder bsb = new StringBuilder(s);
            BibleBookRecord br;
            try
            {

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
                                EscapeHtml(translationName), firstChapterFile, bookRecord.vernacularShortName, currentChapterPublished);
                bsb.Append("<table border=\"0\" align=\"center\"><tbody><tr><td>");
                bsb.Append("<form name=\"bkch1\"><div class=\"navChapters\">");
                bsb.Append("<select name=\"bksch1\" onChange=\"location=document.bkch1.bksch1.options[document.bkch1.bksch1.selectedIndex].value;\">");
                i = 0;
                while (i < bookInfo.publishArrayCount)
                {
                    if (bookInfo.publishArray[i] != null)
                    {
                        br = (BibleBookRecord)bookInfo.publishArray[i];
                        if (br.tla == bookRecord.tla)
                        {
                            bsb.Append(OptionSelectedOpeningElement + br.vernacularShortName + "</option>" + Environment.NewLine);
                        }
                        else
                        {
                            if (br.HasContent && (br.chapterFiles.Count > 0))
                            {
                                bsb.Append("<option value=\"" + br.chapterFiles[0] + ".htm\">" + br.vernacularShortName + "</option>" + Environment.NewLine);
                            }
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
                            bsb.Append(String.Format(OptionSelectedOpeningElement + "{0}</option>{1}", linkText, Environment.NewLine));
                            nextChapIndex = i + 1;
                        }
                        else
                        {
                            sb.Append(String.Format("<a href=\" {0}.htm#V0\">&nbsp;{1}&nbsp;</a> ",
                                chFile, linkText));
                            bsb.Append(String.Format("<option value=\"{0}.htm#V0\">{1}</option>{2}",
                                chFile, linkText, Environment.NewLine));
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
                htm.WriteLine("<div class=\"navButtons\"><a href=\"index.htm\">{0}</a></div>", EscapeHtml(translationName));
                bsb.Append("<form name=\"bkch1\"><div class=\"navChapters\">");
                bsb.Append("<select name=\"bksch1\" onChange=\"location=document.bkch1.bksch1.options[document.bkch1.bksch1.selectedIndex].value;\">");
                bsb.Append(OptionSelectedOpeningElement + "---</option>"+Environment.NewLine);
                i = 0;
                BibleBookRecord bookRec;
                while (i < bookInfo.publishArrayCount)
                {
                    bookRec = (BibleBookRecord)bookInfo.publishArray[i];
                    if (bookRec.isPresent && (bookRec.chapterFiles != null) && (bookRec.chapterFiles.Count > 0))
                    {
                        bsb.Append("<option value=\"" + bookRec.chapterFiles[0] + ".htm\">" + bookRec.vernacularShortName + "</option>"+Environment.NewLine);
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
            catch (Exception ex)
            {
                Logit.WriteError("ERROR in usfxToHtmlConverter::WriteNavButtons():");
                Logit.WriteError(ex.Message);
            }
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
            string firstChapterFile = String.Empty;

            int bookIndex = 0;
            int chapterIndex;
            BibleBookRecord br;
            ChapterInfo ci;

            while ((bookIndex < bookInfo.publishArray.Length) && (firstChapterFile == String.Empty))
            {
                br = bookInfo.publishArray[bookIndex];
                if (br.isPresent && (br.tla == currentBookAbbrev))
                {
                    chapterIndex = 0;
                    while ((chapterIndex < br.chaptersFound.Count) && (firstChapterFile == String.Empty))
                    {
                        ci = (ChapterInfo)br.chaptersFound[chapterIndex];
                        if (ci.chapterInteger > 0)
                        {
                            firstChapterFile = String.Format("{0}{1}.htm", br.tla, ci.chapterInteger.ToString(formatString));
                        }
                        chapterIndex++;
                    }
                }
                bookIndex++;
            }
            return firstChapterFile;
        }

        protected string StartingShortCode = "MT";

        /// <summary>
        /// Name of the first file which has actual Bible text
        /// </summary>
        /// <returns>First file with actual Bible text</returns>
        protected virtual string StartingFile()
        {
            string genbookStart = String.Empty;
            string startHere = String.Empty;
            string chFormat;
            int bookIndex = 0;
            int chapterIndex;
            BibleBookRecord br;
            ChapterInfo ci;

            while ((bookIndex < bookInfo.publishArray.Length) && (startHere == String.Empty))
            {
                br = bookInfo.publishArray[bookIndex];
                if ((br != null) && br.isPresent)
                {
                    if ((br.testament == "o") || (br.testament == "n") || (br.testament == "a"))
                    {
                        chapterIndex = 0;
                        while ((chapterIndex < br.chaptersFound.Count) && (startHere == String.Empty))
                        {
                            ci = (ChapterInfo)br.chaptersFound[chapterIndex];
                            if (ci.chapterInteger > 0)
                            {
                                if (br.tla == "PSA")
                                    chFormat = "000";
                                else
                                    chFormat = "00";
                                startHere = String.Format("{0}{1}", br.tla, ci.chapterInteger.ToString(chFormat));
                                StartingShortCode = br.shortCode;
                            }
                            chapterIndex++;
                        }
                    }
                    else if (genbookStart == string.Empty)
                    {
                        chapterIndex = 0;
                        while ((chapterIndex < br.chaptersFound.Count) && (genbookStart == String.Empty))
                        {
                            ci = (ChapterInfo)br.chaptersFound[chapterIndex];
                            if (ci.chapterInteger > 0)
                            {
                                if (br.tla == "PSA")
                                    chFormat = "000";
                                else
                                    chFormat = "00";
                                genbookStart = String.Format("{0}{1}", br.tla, ci.chapterInteger.ToString(chFormat));
                                StartingShortCode = br.shortCode;
                            }
                            chapterIndex++;
                        }
                    }

                }
                bookIndex++;
            }
            if (startHere == String.Empty)
                return genbookStart;
            return startHere;
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
            htm.WriteLine("<!DOCTYPE html>");
            htm.WriteLine("<html lang=\"{0}\" dir=\"{1}\">", shortLangId, textDirection);
            htm.WriteLine("<head>");
            htm.WriteLine("<meta charset=\"UTF-8\" />");
//            htm.WriteLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\" />");
            htm.WriteLine("<link rel=\"stylesheet\" href=\"{0}\" type=\"text/css\" />", customCssName);
            htm.WriteLine("<meta name=\"viewport\" content=\"user-scalable=yes, initial-scale=1, minimum-scale=1, width=device-width\"/>");
//            htm.WriteLine("<meta name=\"viewport\" content=\"user-scalable=yes, initial-scale=1, minimum-scale=1, width=device-width, height=device-height\"/>");
            if (mainScriptureFile)
            {
                htm.WriteLine("<script src=\"TextFuncs.js\" type=\"text/javascript\"></script>");
            }
            htm.WriteLine("<title>{0} {1} {2}</title>",
                translationName, currentBookHeader, currentChapterPublished, fontClass);
            htm.WriteLine(string.Format("<meta name=\"keywords\" content=\"{0}, {1}, Holy Bible, Scripture, Bible, Scriptures, New Testament, Old Testament, Gospel\" />",
                translationName, langId));
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
        protected virtual void WriteHorizontalRule()
        {
            EndHtmlParagraph();
            htm.WriteLine("<hr />");
        }

        protected virtual void PageBreak()
        {
            WriteHorizontalRule();
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
                htm.WriteLine("<hr>");
                htm.WriteLine("<div class=\"pageFooter\">");
                RepeatNavButtons();
                htm.WriteLine("</div><div class=\"pageFooter\">");
                WriteHtmlFootnotes();
                htm.WriteLine("</div></div>"); // end of div.pageFooter and div.main
                htm.WriteLine("<div class=\"pageFooter\">");
                if ((footerTextHTML != null) && (footerTextHTML.Trim().Length > 0))
                {
                    htm.WriteLine(footerTextHTML);
                }
                if ((copyrightLinkHTML != null) && (copyrightLinkHTML.Trim().Length > 0) && !projectOptions.silentCopyright)
                {
                    htm.WriteLine("<p align=\"center\">" + copyrightLinkHTML + "</p>");
                }
                htm.WriteLine("</div></body></html>");
                htm.Close();
                htm = null;
                previousFileName = currentFileName;
                noteNumber = 0;
            }
            chopChapter = false;
        }

        public int maxVerseLength = 0;
        protected string bookOsisId;
        protected string chapterOsisId;
        protected string verseOsisId;
        protected string chapterId;
        protected string verseId;


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
                preVerse.Length = 0;
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
            if (chapterNumber > 0)
            {
                if (htm == null)
                    OpenHtmlFile();
                if (preVerse.Length > 0)
                {
                    htm.WriteLine(preVerse.ToString());
                    preVerse.Length = 0;
                }
            }

            EndHtmlParagraph();
            inParagraph = true;
            if (newChapterFound && (style != "ms") && (style != "b") && !style.StartsWith("i"))
            {
                if (currentBookAbbrev.CompareTo("PSA") == 0)
                {
                    WriteHtml(String.Format("<div class='chapterlabel'><a name=\"V0\">{0} {1}</a></div>", psalmLabel, currentChapterPublished));
                }
                else
                {
                    WriteHtml(String.Format("<div class='chapterlabel'><a name=\"V0\">{0} {1}</a></div>", chapterLabel, currentChapterPublished));
                }
                newChapterFound = false;
            }
            string s = String.Format("<div class='{0}'>", style);
            if (style == "b")
                s = s + " &nbsp; ";
            WriteHtmlText(s);
        }

        /// <summary>
        /// Finish up an HTML paragraph, which is really a div element.
        /// </summary>
        protected virtual void EndHtmlParagraph()
        {
            if (inParagraph)
            {
                WriteHtml("</div>");
                inParagraph = false;
            }
        }

        /// <summary>
        /// Start an HTML text style, which is really a span with the style name as an attribute.
        /// </summary>
        /// <param name="style">name of text style/span class</param>
        protected virtual void StartHtmlTextStyle(string style)
        {
            if (!ignore)
            {
                inTextStyle++;
                WriteHtml(String.Format("<span class='{0}'>", style));
            }
        }

        protected virtual void StartCenteredColumn()
        {
            WriteHtml("<td>");
            inTableCol = true;
        }

        protected virtual void StartRightJustifiedColumn()
        {
            WriteHtml("<td align='right'>");
            inTableCol = true;
        }


        /// <summary>
        /// End an HTML text style (span)
        /// </summary>
        protected virtual void EndHtmlTextStyle()
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
        /// <returns>String with greater than, less than, and ampersand encoded as HTML entities. Also inserts narrow nonbreaking spaces between consecutive curly quote marks.</returns>
        public static string EscapeHtml(string s)
        {
            s = s.Replace("<", "&lt;");
            s = s.Replace(">", "&gt;");
            s = s.Replace("&", "&amp;");
            // Narrow nobreak space, \u202F, has serious font coverage problems, so going with \u00AO, instead.
            // s = s.Replace("’”", "’\u202F”").Replace("”’", "”\u202F’").Replace("“‘", "“\u202F‘").Replace("‘“", "‘\u202F“");
            s = s.Replace("’”", "’\u00A0”").Replace("”’", "”\u00A0’").Replace("“‘", "“\u00A0‘").Replace("‘“", "‘\u00A0“");
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
        protected virtual void WriteHtmlText(string text)
        {
            text = text.Replace('\r', ' ').Replace('\n', ' ').Replace("  ", " ");
            if (!ignore)
            {
                var escapeHtml = EscapeHtml(text);
                if (inFootnote)
                {
                    /*
                    if (xRef != null && !inFootnoteStyle)
                        xRef += escapeHtml; // accumluate for hot-link processing.
                    else
                     */
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
        protected virtual void WriteHtmlOptionalLineBreak()
        {
            WriteHtml("<br/>");
        }

        public string sqlFileName;
        public string sqlTableName;
        protected string sqlVerseId;
        protected StringBuilder sqlVerseContents;
        protected StreamWriter sqlVerseTable;
        protected string sqlCanonOrder;

        /// <summary>
        /// Write text to HTML file if open, or queue it up for when an HTML file gets opened.
        /// </summary>
        /// <param name="s"></param>
        protected virtual void WriteHtml(string s)
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
        /// <param name="style">"ef" for extended footnote, "f" for footnote, "x" for cross reference, or "ex" for extended cross reference</param>
        /// <param name="marker">"+" for automatic caller, "-" for no caller (useless for a popup), or a verbatim note caller</param>
        protected virtual void StartHtmlNote(string style, string marker)
        {
            string automaticOrigin = String.Empty;

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
                if ((style == "f") || (style == "ef"))
                {
                    marker = footNoteCall.Marker();
                }
                else
                {
                    marker = xrefCall.Marker();
                }
            }
            if (string.Compare(marker, "-") == 0)
                marker = "";
            WriteHtml(String.Format("<a href=\"#{0}\" onclick=\"hilite('{0}')\"><span class=\"notemark\">{1}</span><span class=\"popup\">",
                noteId, marker));
            // Numeric chapter and verse numbers are used in internal references instead of the text versions, which may
            // include dashes in verse bridges.
            if (!String.IsNullOrEmpty(noteOriginFormat))
            {
                automaticOrigin = noteOriginFormat.Replace("%c", currentChapterPublished).Replace("%v", currentVersePublished);
            }
            if ((chapterNumber >= 1) && (verseNumber >= 1))
                footnotesToWrite.Append(String.Format("<p class=\"{0}\" id=\"{1}\"><span class=\"notemark\">{2}</span><a class=\"notebackref\" href=\"#V{3}\">{4}</a>{5}",
                    style, noteId, marker, verseNumber.ToString(), automaticOrigin, Environment.NewLine));
            else
                footnotesToWrite.Append(String.Format("<p class=\"{0}\" id=\"{1}\"><span class=\"notemark\">{2}</span><a class=\"notebackref\" href=\"#V1\">^</a>{3}",
                    style, noteId, marker,Environment.NewLine));

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
                /*
                if (xRef != null)
                    footnotesToWrite.Append(ConvertCrossRefsToHotLinks(xRef));
                xRef = null;
                */
                footnotesToWrite.Append("</p>"+Environment.NewLine);    // End footnote paragraph
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
        protected virtual void StartHtmlTable()
        {
            EndHtmlParagraph();
            if (!inTable)
            {
                WriteHtml("<table border=\"1\"><tbody>"+Environment.NewLine);
                inTable = true;
            }
        }

        
        protected virtual void StartHeaderColumn()
        { 
            WriteHtml("<td><b>");
            inTableCol = true;
            inTableBold = true;
        }

        protected virtual void StartHeaderColumnRight()
        {
            WriteHtml("<td align='right'><b>");
            inTableBold = true;
            inTableCol = true;
        }

        protected virtual void StartTableRow()
        {
            WriteHtml("<tr>");
            inTableRow = true;
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
                WriteHtml(String.Format("<div class=\"figure\"><img src=\"{0}\"><br/><span class=\"figcopr\" />{1}</br>" +
                   "</span><span class=\"figref\">{2}</span><span class=\"figCaption\"> {3}</span></div>", figFileName, figCopyright, figReference, figCaption));
            }
        }


        /// <summary>
        /// End an HTML table column
        /// </summary>
        protected virtual void EndHtmlTableCol()
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
        protected virtual void EndHtmlTableRow()
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
        protected virtual void EndHtmlTable()
        {
            EndHtmlTableRow();
            if (inTable)
            {
                WriteHtml("</tbody></table>"+Environment.NewLine);
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
            chapterId = currentBookCode + chapterNumber.ToString();
            verseOsisId = chapterOsisId + ".0";
            verseId = chapterId + "_0";
            chopChapter = true;
            newChapterMarkNeeded = newChapterFound = true;
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
            currentChapterPublished = String.Empty;
            chapterNumber = 1;
            currentChapterAlternate = String.Empty;
            currentVerse = currentVersePublished = currentVerseAlternate = String.Empty;
            verseNumber = 0;
            chopChapter = true;
            if (!bookInfo.isPeripheral(currentBookAbbrev))
                newChapterFound = true;
            if (previousBookId != currentBookAbbrev)
            {
                CloseHtmlFile();
                chapterFileIndex++;
                previousBookId = currentBookAbbrev;
            }
        }


        protected virtual void FlushPreverse()
        {
            if (preVerse.Length > 0)
            {
                VirtualChapter();
                currentVerse = "0";
                verseNumber = 0;
                currentVersePublished = String.Empty;
                currentVerseAlternate = String.Empty;
                currentBCV = currentBookAbbrev + " " + currentChapter + ":" + currentVerse;
                verseOsisId = chapterOsisId + "." + verseNumber.ToString();
                verseId = chapterId + "_" + currentVerse;   // was verseNumber.ToString();
                EndHtmlTextStyle(); // USFM and USFX disallow text styles crossing verse boundaries.
                                    // (If text styles could cross verse boundaries, we could just remember what the last
                                    //  style was and restart it, but that would make displaying any arbitrary range of
                                    //  verses harder if it were required.)
                if (htm == null)
                    OpenHtmlFile();
                if (preVerse.Length > 0)
                {
                    htm.WriteLine(preVerse.ToString());
                    preVerse.Length = 0;
                }
            }
        }


        /// <summary>
        /// Process a verse marker (2nd pass)
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
            verseId = chapterId + "_" + currentVerse;   // was verseNumber.ToString();
            StartVerse();
        }

        protected virtual void DisplayPublishedVerse()
        {

        }

        protected virtual void DisplayAlternateVerse()
        {

        }

        /// <summary>
        /// Creates a new USFX file that has the local (location-specific) and global changes made to it
        /// that are specified in the globalSubstFile and localSubstFile. In those two files, the separator
        /// character for each line is whatever character is the first one on the line, which can be any
        /// printing Unicode character that isn't contained within any of the fields on the line.
        /// </summary>
        /// <param name="inFile">USFX Scripture input file</param>
        /// <param name="outFile">USFX Scripture output file with specified changes made</param>
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

                Console.WriteLine("Reading {0}", inFile);

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
                                    if (bookRecord == null)
                                    {
                                        Console.WriteLine("No book record for {0}!", currentBookAbbrev);
                                    }
                                    bookOsisId = bookRecord.osisName;
                                }
                                bookRecord.chapterFiles = new ArrayList();
                                currentBookHeader = vernacularLongTitle = String.Empty;
                                currentChapter = currentVerse = "0";
                                chapterNumber = 0;
                                verseNumber = 0;
                                if (bookRecord == null)
                                {
                                    includeThis = false;
                                }
                                else if ((bookRecord.testament == "a") || (bookRecord.testament == "x"))
                                {
                                    includeThis = includeApocrypha;
                                }
                                else
                                {
                                    includeThis = true;
                                    // Console.WriteLine("Processing {0}", currentBookAbbrev);
                                }
                                if (includeThis)
                                    Console.WriteLine("Including {0}", currentBookAbbrev);
                                else
                                    Console.WriteLine("Skipping {0}", currentBookAbbrev);
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
                                chapterId = currentBookCode + chapterNumber.ToString();
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
                                verseId = chapterId + "_" + currentVerse;   // was verseNumber.ToString();
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
        public DateTime indexDate = DateTime.UtcNow;
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

        /// <summary>
        /// Start markup for extended Bible study features
        /// </summary>
        /// <param name="StrongsNumber">Greek or Hebrew lexicon number</param>
        /// <param name="plural">"true" if plural, "false" if not, otherwise unspecified</param>
        /// <param name="morphology">Morphology coding per http://eBible.org/usfx/parsing.txt </param>
        /// <param name="lemma">Root word for dictionary lookup</param>
        protected virtual void StartStrongs(string StrongsNumber, string plural, string morphology = "", string lemma = "")
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
                htm.Write("<div class='toc'><a href='index.htm'>^</a></div>{0}",
                    bookRecord.toc.ToString());
                CloseHtmlFile();
            }
        }

        protected virtual void EndVerse()
        {
            if ((preVerse.Length > 0) && (htm != null))
            {
                htm.Write(preVerse.ToString());
                preVerse.Length = 0;
            }
        }

        protected virtual void WriteCopyrightPage(string chapFormat, string licenseHtml, string goText)
        {
            // licenseHtml = "<img src='" + coverName + "' height='200' width:'130' style='padding: 0 15px 15px 0; float:left' />" + licenseHtml;

            // Copyright page
            if (projectOptions.silentCopyright)
                return;
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
                StartingFile(), goText);
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

        protected virtual void EndBook()
        {
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
                    preVerse.Length = 0;
                }
            }
            CloseHtmlFile();
        }
        

        protected bool inLink = false;

        /// <summary>
        /// Start a hyperlink to the destination indicated.
        /// </summary>
        /// <param name="tgt"></param>
        /// <param name="web"></param>
        protected virtual void StartLink(string tgt, string web)
        {
            string chapterFormat = "00";
            string theLink;
            if (tgt.Length > 0)
            {
                BCVInfo bcvRec = bookInfo.ValidateInternalReference(tgt);
                if (bcvRec.exists)
                {
                    if (bcvRec.bkInfo.tla.CompareTo("PSA") == 0)
                        chapterFormat = "000";
                    theLink = String.Format("<a href='{0}{1}.htm#V{2}'>", bcvRec.bkInfo.tla, bcvRec.chapInfo.chapterInteger.ToString(chapterFormat), 
                    bcvRec.vsInfo.startVerse.ToString());
                    if (inFootnote)
                    {
                        footnotesToWrite.Append(theLink);
                    }
                    else
                    {
                        WriteHtml(theLink);
                    }
                    inLink = true;
                }
            }
            else if (web.Length > 0)
            {
                inLink = true;
                theLink = String.Format("<a href=\"{0}\">", web);
                if (inFootnote)
                    footnotesToWrite.Append(theLink);
                else
                    htm.Write(theLink);
            }
        }

        /// <summary>
        /// End a link started by StartLink.
        /// </summary>
        protected virtual void EndLink()
        {
            if (inLink)
            {
                if (inFootnote)
                {
                    footnotesToWrite.Append("</a>");
                }
                else
                {
                    WriteHtml("</a>");
                }
                inLink = false;
            }
        }

        /// <summary>
        /// Append a line to the Table of Contents
        /// </summary>
        /// <param name="level">Table of contents line class, i.e. toc1, toc2, toc3...</param>
        /// <param name="title">Title to add</param>
        protected virtual void AppendToc(string level, string title)
        {
            bookRecord.toc.Append(String.Format("<div class=\"{0}\"><a href=\"{1}#V{2}\">{3}</a></div>"+Environment.NewLine,
                level,
                MainFileLinkTarget(currentBookAbbrev, Math.Max(1, chapterNumber).ToString(chapFormat)),
                verseNumber.ToString(), usfx.Value.Trim()));
        }

        /// <summary>
        /// Intended to be overriden in child class
        /// </summary>
        protected virtual void ChapterToc()
        {
            newChapterFound = false;
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



        protected string chapFormat;
        public string translationIdentifier;
        public string languageIdentifier;
        public string epubIdentifier;
        public string sourceLink = String.Empty;
        public string textDirection = "ltr";
        public string englishDescription;
        public string noteOriginFormat = "%c:%v";  // Automatic note origin format
        public string longCopr;
        public string shortCopr;
        public string coverName;
        public bool stripManualNoteOrigins = true;  // These are normally totally redundant with the automatic note origins.
        public string customCssName = "prophero.css";   // Name of the css file to use for this project.
        protected bool inOrigin = false;
        protected bool hasLemma = false;
        protected bool refTagFound;
        protected string currentBookCode;  // Current book short code
        protected bool firstTitle;
        protected bool homeLinkFixed = false;
        protected string sectionTitle = String.Empty;
        protected bool inSectionTitle = false;
        protected bool inToc = false;
        public string inputDir;
        public string projectInputDir;

        /// <summary>
        /// Converts the USFX file usfxName to a set of HTML files, one file per chapter, in the
        /// directory htmlDir, with reference to CSS files in cssDir. The output file names will
        /// be the standard 3-letter abbreviation for the book, followed by a dash and the 2 or
        /// 3 digit chapter number, followed optionally by namePart, then .htm.
        /// </summary>
        /// <param name="usfxName">Name of the USFX file to convert to HTML files</param>
        /// <param name="htmlDir">Directory to put the HTML files into</param>
        /// <param name="vernacularTitle">Vernacular title for page headers</param>
        /// <param name="languageId">New Ethnologue 3-letter code for this language</param>
        /// <param name="chapterLabelName">Vernacular name for "Chapter"</param>
        /// <param name="psalmLabelName">Vernacular name for "Psalm"</param>
        /// <param name="copyrightLink">HTML for copyright link, like &lt;href a="copyright.htm"&gt;©&lt;/a&gt;</param>
        /// <param name="homeLink">HTML for link to home page</param>
        /// <param name="footerHtml">HTML for footer "fine print" text</param>
        /// <returns>true iff the conversion succeeded</returns>
        public virtual bool ConvertUsfxToHtml(string usfxName, string htmlDir, string vernacularTitle, string languageId, string translationId,
            string chapterLabelName, string psalmLabelName, string copyrightLink, string homeLink, string footerHtml,
            string indexHtml, string licenseHtml, bool skipHelps, string goText)
        {
            bool result = false;
            bool inUsfx = false;
            inToc = false;
            hasLemma = false;
            string figDescription = String.Empty;
            string figFileName = String.Empty;
            string figSize = String.Empty;
            string figLocation = String.Empty;
            string figCopyright = String.Empty;
            string figCaption = String.Empty;
            string figReference = String.Empty; // Figure parameters
            if (projectOptions.commonChars || projectOptions.languageId == "eng")
                fontClass = "latin";
            else
                fontClass = preferredFont.ToLower().Replace(' ', '_');
            firstTitle = true;
            homeLinkFixed = false;
            commonChars = true;
            translationIdentifier = translationId;
            noteNumber = 0;
            languageIdentifier = languageId;
            ignoreIntros = ignoreNotes = skipHelps;
            footerTextHTML = footerHtml;
            copyrightLinkHTML = copyrightLink;
            homeLinkHTML = homeLink;
            htmDir = htmlDir;
            langId = languageId;
            shortLangId = langCodes.ShortCode(langId);
            translationName = vernacularTitle;
            chapterLabel = chapterLabelName;
            psalmLabel = psalmLabelName;
            chapterNumber = verseNumber = 0;
            htmlextrasDir = Path.Combine(projectInputDir, "htmlextras");
            bookList.Clear();
            bookOsisId = chapterOsisId = verseOsisId = chapterId = verseId = String.Empty;
            currentBookAbbrev = currentBookTitle = currentChapterPublished = wordForChapter = String.Empty;
            currentChapter = currentFileName = currentVerse = languageCode = String.Empty;
            currentBookCode = String.Empty;
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
            refTagFound = false;    // If ref tags are present, use those instead of the previously-written logic for recognizing internal links.
            bool foundThisBook;
            ignore = false;
            StringBuilder toc = new StringBuilder();
            chapFormat = "00";
            int i, j;

            footnotesToWrite = new StringBuilder(String.Empty);
            preVerse = new StringBuilder(String.Empty);
            try
            {
                if (!string.IsNullOrEmpty(sqlFileName))
                {
                    sqlVerseTable = new StreamWriter(sqlFileName, false, Encoding.UTF8);
                    sqlVerseTable.WriteLine(@"USE sofia;
DROP TABLE IF EXISTS sofia.{0};
CREATE TABLE {0} (
    verseID VARCHAR(16) NOT NULL PRIMARY KEY,
    canon_order VARCHAR(12) NOT NULL,
    verseContents TEXT CHARACTER SET UTF8 NOT NULL) ENGINE=MyISAM;
LOCK TABLES {0} WRITE;", sqlTableName);
                }

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
                                    if ((!projectOptions.includeApocrypha) && (bookRecord.testament == "a"))
                                    {
                                        SkipElement();
                                        bookRecord.actualChapters = 0;
                                        bookRecord.isPresent = false;
                                    }
                                    else if (!projectOptions.allowedBookList.Contains(bookRecord.tla))
                                    {
                                        SkipElement();
                                        bookRecord.actualChapters = 0;
                                        bookRecord.isPresent = false;
                                    }
                                    else
                                    {
                                        bookOsisId = bookRecord.osisName;
                                        currentBookCode = bookRecord.shortCode;
                                        chapterOsisId = bookOsisId + ".0";
                                        chapterId = currentBookCode + "0";
                                        verseOsisId = chapterOsisId + ".0";
                                        verseId = chapterId + "_0";
                                        if (bookRecord == null)
                                        {
                                            Logit.WriteError("Cannot process unknown book: " + currentBookAbbrev);
                                            return false;
                                        }
                                        foundThisBook = false;
                                        for (i = 0; (i < bookInfo.publishArrayCount) && !foundThisBook; i++)
                                        {
                                            {
                                                if (bookInfo.publishArray[i].tla == bookRecord.tla)
                                                    foundThisBook = true;
                                            }
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
                                        if (id.CompareTo("PSA") == 0)
                                            chapFormat = "000";
                                        else
                                            chapFormat = "00";
                                        chapterNumber = 0;
                                        verseNumber = 0;
                                        newChapterMarkNeeded = newChapterFound = false;
                                        if (bookInfo.isApocrypha(id))
                                            containsDC = true; // if we have any apocrypha, this is set true for use in the next pass
                                    }
                                }
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
                                xrefCall.reset();
                                newChapterMarkNeeded = newChapterFound = false;
                                break;
                            case "p":
                                ChapterToc();
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
                                        AppendToc("toc1", usfx.Value.Trim());
                                    }
                                    hasContentsPage = true;
                                }
                                else
                                {
                                    if (chapterNumber == 0)
                                        VirtualChapter();
                                    if (!((bookRecord.testament == "o") || (bookRecord.testament == "n") || (bookRecord.testament == "a")))
                                        LeaveHeader();
                                }
                                break;
                            case "ref":
                                refTagFound = true;
                                break;
                            case "h":
                                usfx.Read();
                                if (usfx.NodeType == XmlNodeType.Text)
                                {
                                    AppendToc("toc1", usfx.Value.Trim());
                                }

                                break;
                            case "s":
                            case "d":
                                ChapterToc();
                                hasContentsPage = true;
                                inSectionTitle = true;
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
                                chapterId = currentBookCode + chapterNumber.ToString();
                                verseOsisId = chapterOsisId + ".0";
                                verseId = chapterId + "_0";
                                bookRecord.actualChapters = Math.Max(bookRecord.actualChapters, chapterNumber);
                                LeaveHeader();
                                chopChapter = true;
                                newChapterFound = true;
                                break;
                            case "cp":
                                if (!usfx.IsEmptyElement)
                                {
                                    usfx.Read();
                                    if (usfx.NodeType == XmlNodeType.Text)
                                    {
                                        currentChapterPublished = fileHelper.LocalizeDigits(usfx.Value.Trim());
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
                                                if (string.IsNullOrEmpty(bookRecord.vernacularName))
                                                    bookRecord.vernacularName = usfx.Value.Trim();
                                                break;
                                            case "2":
                                                if (string.IsNullOrEmpty(bookRecord.vernacularShortName))
                                                    bookRecord.vernacularShortName = usfx.Value.Trim();
                                                break;
                                            case "3":
                                                if (string.IsNullOrEmpty(bookRecord.vernacularAbbreviation))
                                                    bookRecord.vernacularAbbreviation = usfx.Value.Trim();
                                                break;
                                            case "4":
                                                if (string.IsNullOrEmpty(bookRecord.vernacularAltName))
                                                    bookRecord.vernacularAltName = usfx.Value.Trim();
                                                break;
                                        }
                                    }
                                    inToc = true;
                                }
                                break;
                            case "v":
                                ChapterToc();
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
                                verseId = chapterId + "_" + currentVerse;   // was verseNumber.ToString();
                                break;
                            case "w":
                            case "zw":
                                hasLemma = true;
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
                                if (string.IsNullOrEmpty(bookRecord.vernacularName))
                                    bookRecord.vernacularName = vernacularLongTitle;
                                break;
                            case "d":
                            case "s":
                                if (inSectionTitle)
                                {
                                    AppendToc("toc2", sectionTitle.Trim());
                                    inSectionTitle = false;
                                    sectionTitle = String.Empty;
                                }
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
                            case "toc":
                                inToc = false;
                                break;
                        }
                    }
                    else if ((usfx.NodeType == XmlNodeType.Text) && (bookRecord != null) && (bookRecord.tla == currentBookAbbrev))
                    {   // We don't count a book as present unless there is some text in it.
                        string s = usfx.Value;
                        if (s.Trim().Length > 1)
                            bookRecord.isPresent = true;
                        if (inSectionTitle)
                            sectionTitle = sectionTitle + usfx.Value;
                        if (commonChars)
                        {
                            for (j = 0; j < usfx.Value.Length; j++)
                            {   // Check for characters not supported by common fonts
                                int c = (int)usfx.Value[j];
                                if ((c > 0x206F) || (c == 'ꞌ') || /* ꞌ is saltillo */
                                    ((c > 0x180) && (c < 0x2000)))
                                {
                                    commonChars = false;
                                }
                            }
                        }
                    }
                    conversionProgress = "navigation " + currentBookAbbrev + " " + currentChapter + ":" + currentVerse;
                    //System.Windows.Forms.Application.DoEvents();
                }
                usfx.Close();

                try
                {
                    for (i = 0; (i < bookInfo.publishArrayCount); i++)
                    {
                        if (bookInfo.publishArray[i].isPresent && (bookInfo.publishArray[i].chapterFiles != null))
                        {   // This book is in the input files and contains at least one character of text.
                            bookList.Add(bookInfo.publishArray[i]);
                            foreach (string chapFileName in bookInfo.publishArray[i].chapterFiles)
                            {
                                if (!String.IsNullOrEmpty(chapFileName))
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


                bookOsisId = chapterOsisId = verseOsisId = chapterId = verseId = String.Empty;
                currentBookAbbrev = currentBookTitle = currentChapterPublished = String.Empty;
                currentChapter = currentFileName = currentVerse = String.Empty;
                chapterNumber = verseNumber = 0;


                // Index page
                GenerateIndexFile(translationId, indexHtml, goText);

                WriteCopyrightPage(chapFormat, licenseHtml, goText);

                // Pass 2: content generation

                usfx = new XmlTextReader(usfxName);
                usfx.WhitespaceHandling = WhitespaceHandling.All;
                allChapterIndex = 0;
                chapterFileIndex = 0;
                bookListIndex = 0;
                projectOptions.hasStrongs = false;
                while (usfx.Read())
                {
                    conversionProgress = "Generating file " + currentBookAbbrev + " " + currentChapter + ":" + currentVerse;
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
                                            if (projectOptions.disablePrintingFigOrigins)
                                            {
                                                figReference = String.Empty;
                                            }
                                            else
                                            {
                                                if (usfx.NodeType == XmlNodeType.Text)
                                                    figReference = EscapeHtml(usfx.Value.Trim());
                                            }
                                        }
                                        break;
                                    case "ref":
                                        // Not yet used: string src = GetNamedAttribute("src");
                                        string tgt = GetNamedAttribute("tgt");
                                        string web = GetNamedAttribute("web");
                                        StartLink(tgt, web);
                                        break;
                                    case "book":
                                        currentBookAbbrev = id;
                                        chapterNumber = 0;
                                        verseNumber = 0;
                                        bookRecord = (BibleBookRecord)bookInfo.books[id];
                                        if ((!projectOptions.includeApocrypha) && (bookRecord.testament == "a"))
                                        {
                                            SkipElement();
                                        }
                                        else if (!projectOptions.allowedBookList.Contains(bookRecord.tla))
                                        {
                                            SkipElement();
                                        }
                                        else
                                        {
                                            currentBookCode = bookRecord.shortCode;
                                            bookOsisId = bookRecord.osisName;
                                            chapterOsisId = bookOsisId + ".0";
                                            chapterId = currentBookCode + "0";
                                            verseOsisId = chapterOsisId + ".0";
                                            verseId = chapterId + "_0";
                                            currentBookHeader = bookRecord.vernacularShortName;
                                            currentVernacularAbbreviation = bookRecord.vernacularAbbreviation;
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
                                        xrefCall.reset();
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
                                            (sfm.Length == 0) || (sfm == "p") ||
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
                                        WriteHorizontalRule();
                                        break;
                                    case "pb":
                                        PageBreak();
                                        break;
                                    case "c":
                                        ProcessChapter();
                                        if (chapterNumber > 1)
                                        {
                                            footNoteCall.reset();
                                            xrefCall.reset();
                                        }
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
                                        ignore = true;
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
                                            DisplayAlternateVerse();
                                        }
                                        break;
                                    case "vp":
                                        if (!usfx.IsEmptyElement)
                                        {
                                            usfx.Read();
                                            if (usfx.NodeType == XmlNodeType.Text)
                                                currentVersePublished = fileHelper.LocalizeDigits(usfx.Value.Trim());
                                            DisplayPublishedVerse();
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
                                        StartHeaderColumn();
                                        break;
                                    case "tr":
                                        StartTableRow();
                                        break;
                                    case "thr":
                                        StartHeaderColumnRight();
                                        break;
                                    case "tc":
                                        StartCenteredColumn();
                                        break;
                                    case "tcr":
                                        StartRightJustifiedColumn();
                                        break;
                                    case "ef":
                                    case "ex":
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
                                    case "xk":
                                    case "xt":
                                        StartHtmlNoteStyle(usfx.Name);
                                        break;
                                    case "fp":
                                        WriteHtml("<br/>");
                                        break;
                                    case "fr":
                                    case "xo":
                                        if (stripManualNoteOrigins)
                                        {
                                            if (!usfx.IsEmptyElement)
                                            {
                                                usfx.Read();    // Send manual cross reference origin to bit bucket
                                            }
                                        }
                                        else
                                        {
                                            StartHtmlNoteStyle(usfx.Name);
                                        }
                                        break;
                                    case "xdc":
                                    case "fdc":
                                    case "dc":
                                        // Suppress these three fields unless there is apocrypha somewhere in the translation (detected in first pass)
                                        if ((!projectOptions.includeApocrypha) || (!containsDC))
                                            ignore = true;
                                        break;
                                    case "optionalLineBreak":
                                        WriteHtmlOptionalLineBreak();
                                        break;
                                    case "w":
                                    case "zw":
                                        projectOptions.hasStrongs = true;
                                        StartStrongs(GetNamedAttribute("s"), GetNamedAttribute("plural"), GetNamedAttribute("m"), GetNamedAttribute("l"));
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
                                        FlushPreverse();
                                        EndBook();
                                        bookListIndex++;
                                        break;
                                    case "p":
                                    case "q":
                                        if (chopChapter)
                                        {
                                            EndHtmlParagraph();
                                            CloseHtmlFile();
                                        }
                                        if (ignoreIntros)
                                            ignore = false;
                                        break;
                                    case "qs":  // qs is really a text style with paragraph attributes, but HTML/CSS can't handle that.
                                    case "b":
                                    case "mt":
                                        EndHtmlParagraph(); // nb paragraph won't follow these.
                                        if (chopChapter)
                                        {
                                            CloseHtmlFile();
                                        }
                                        if (ignoreIntros)
                                            ignore = false;
                                        break;
                                    case "ref":
                                        EndLink();
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
                                                preVerse.Length = 0; ;
                                            }
                                            verseNumber++;
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
                                    case "rq":
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
                                    case "ef":
                                    case "ex":
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
                                    case "xk":
                                    case "xt":
                                        break;
                                    case "fr":
                                    case "xo":
                                        EndHtmlNoteStyle();
                                        break;
                                    case "xdc":
                                    case "fdc":
                                    case "dc":
                                    case "toc":
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
                if ((!String.IsNullOrEmpty(sqlFileName)) && (sqlVerseTable != null))
                {
                    sqlVerseTable.WriteLine("UNLOCK TABLES;");
                    sqlVerseTable.Close();
                    sqlVerseTable = null;
                }
                projectOptions.Write();
                ZipResults();
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



        protected virtual void ZipResults()
        {
        }

        public string Jesusfilmtext = String.Empty;
        public string Jesusfilmlink = String.Empty;

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
            if ((copyrightLinkHTML != null) && (copyrightLinkHTML.Trim().Length > 0) && !projectOptions.silentCopyright)
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
                          StartingFile(), goText);
            htm.WriteLine(indexHtml, langId, translationId);
            if (!String.IsNullOrEmpty(Jesusfilmlink))
            {
                if (String.IsNullOrEmpty(Jesusfilmtext))
                {   // Interpret as an embed code.
                    htm.WriteLine("<div class='toc1'>{0}</div>", Jesusfilmlink);
                }
                else
                {   // Interpret as a link.
                    htm.WriteLine("<div class='toc1'><a href='{0}' target='_blank'>{1}</a></div>", Jesusfilmlink, Jesusfilmtext);
                }
            }
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
        /// Note: this is now a fall-back function, used when ref tags are not found in the USFX source file. If there is even one ref tag,
        /// then those are used exclusively, as a more reliable method of handling all of the possible cases.
        /// </summary>
        /// <param name="chunk1"></param>
        /// <returns>input string, but with HTML links added</returns>
        /* Deprecated, replaced by XSL process that works with a wider variety of links.
        protected string ConvertCrossRefsToHotLinks(string chunk1)
        {
            if (refTagFound || CrossRefToFilePrefixMap == null || CrossRefToFilePrefixMap.Count == 0)
                return chunk1;  // If there are <ref> tags, use those instead of this function.
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
        }*/

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
