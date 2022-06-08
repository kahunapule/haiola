// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003-2017, SIL International, EBT, Michael Paul Johnson, and eBible.org.
// <copyright from='2003' to='2017' company='SIL International, EBT, Michael Paul Johnson, and eBible.org'>
//    
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
// Responsibility: (Kahunapule) Michael P. Johnson
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
using System.Linq;

namespace WordSend
{
    public class usfx2MobileHtml : usfxToHtmlConverter
    {   // Simple HTML optimized for reading Bibles on mobile platforms, but also usable on larger screens

        /// <summary>
        /// Constructor, setting default value(s)
        /// </summary>
        public usfx2MobileHtml()
        {
            langCodes = new LanguageCodeInfo();
            homeLinkFixed = false;
        }

        protected string prevChapterLink;
        protected string nextChapterLink;
        protected string navButtons;


        protected void CheckHomeLink()
        {
            if (!homeLinkFixed)
            {
                int i;
                // Strip out the actual link from the home link, stripping out text and images.
                i = homeLinkHTML.IndexOf("href=\"");
                if (i > 0)
                {
                    homeLinkHTML = homeLinkHTML.Substring(i + 6);
                    i = homeLinkHTML.IndexOf('"');
                    if (i > 0)
                        homeLinkHTML = homeLinkHTML.Substring(0, i);
                }
                else if (homeLinkHTML.Trim().Length == 0)
                {
                    homeLinkHTML = "../";
                }
                homeLinkFixed = true;
            }
        }

        /// <summary>
        /// Write the navigation elements at the bottom of the page.
        /// </summary>
        protected override void RepeatNavButtons()
        {
            htm.WriteLine(navButtons);
        }


        /// <summary>
        /// Write navigational links to get to another chapter from here.
        /// 
        /// </summary>
        protected override void WriteNavButtons()
        {
            int i;
            int prevChapIndex = 0;
            string s = String.Empty;
            prevChapterLink = String.Empty;
            nextChapterLink = String.Empty;
            int chapNumSize;
            try
            {
                string formatString = FormatString(out chapNumSize);
                string firstChapterFile = FirstChapterFile(formatString);
                string thisBookName = currentBookHeader;
                if (currentBookHeader.Trim().Length == 0)
                {
                    if (currentBookAbbrev == "CPR")
                        thisBookName = "^";
                    else
                        thisBookName = String.Empty;
                }

                CheckHomeLink();

                if (bookListIndex >= 0)
                {
                    if (currentChapter.Trim().Length == 0)
                    {
                        if (chapterNumber < 0)
                            currentChapter = "0";
                        else
                            currentChapter = chapterNumber.ToString();
                    }

                    string chFile;
                    int nextChapIndex = -1;
                    for (i = 0; (i < chapterFileList.Count) && (nextChapIndex < 0); i++)
                    {
                        chFile = (string)chapterFileList[i];
                        int cn;
                        if ((!String.IsNullOrEmpty(chFile)) && chFile.StartsWith(currentBookAbbrev) && (int.TryParse(chFile.Substring(chFile.Length - chapNumSize), out cn)))
                        {
                            if (cn == chapterNumber)
                            {
                                nextChapIndex = i + 1;
                                prevChapIndex = i - 1;
                            }
                        }
                    }
                    if ((nextChapIndex >= chapterFileList.Count) || (nextChapIndex < 0))
                        nextChapIndex = 0;
                    nextChapterLink = String.Format("{0}.htm", (string)chapterFileList[nextChapIndex]);
                    if ((prevChapIndex >= 0) && (prevChapIndex < chapterFileList.Count))
                        prevChapterLink = String.Format("{0}.htm", (string)chapterFileList[prevChapIndex]);
                    else
                        prevChapterLink = "index.htm";
                }
                else
                {
                    nextChapterLink = String.Format("{0}.htm", (string)chapterFileList[0]);
                }
                if (currentBookAbbrev != "CPR")
                {
                    navButtons = String.Format(@"<ul class='tnav'>
<li><a href='index.htm'>{0}</a></li>
<li><a href='{1}'>&lt;</a></li>
<li><a href='{2}.htm'>{3}</a></li>
<li><a href='{4}'>&gt;</a></li>
</ul>", thisBookName, prevChapterLink, currentBookAbbrev, fileHelper.LocalizeDigits(currentChapter), nextChapterLink);
                }
                else
                {
                    navButtons = String.Format(@"<ul class='tnav'>
<li><a href='index.htm'>{0}</a></li>
<li><a href='{1}'>&lt;</a></li>
<li><a href='{2}'>&gt;</a></li>
</ul>", thisBookName, prevChapterLink, nextChapterLink);
                }
                htm.WriteLine(navButtons);
            }
            catch (Exception ex)
            {
                Logit.WriteError("ERROR in usfx2MobileHtml::WriteNavButtons():");
                Logit.WriteError(ex.Message);
            }
        }

        /// <summary>
        /// Figure out what the file name of the contents page or first chapter of the current book is, allowing for
        /// possible (likely) missing contents pages or chapter 1.
        /// </summary>
        /// <param name="formatString"></param>
        /// <returns></returns>
        protected override string FirstChapterFile(string formatString)
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
                if ((br != null) && br.IsPresent && (br.tla == currentBookAbbrev) && (br.chaptersFound != null))
                {
                    chapterIndex = 0;
                    while ((chapterIndex < br.chaptersFound.Count) && (firstChapterFile == String.Empty))
                    {
                        ci = (ChapterInfo)br.chaptersFound[chapterIndex];
                        if ((ci != null) && (ci.chapterInteger > 0))
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

        /// <summary>
        /// Open a Scripture chapter HTML file and write its HTML header.
        /// </summary>
        protected override void OpenHtmlFile()
        {
            OpenHtmlFile("", true);
        }

        // protected StreamWriter htm2;    // Riding the fence between HTML and JSON formats

        /// <summary>
        /// Open an HTML file named with the given name if non-empty, or with the book abbreviation followed by "." and the chapter number then ".htm"
        /// and write the HTML header.
        /// </summary>
        /// <param name="fileName">Name of file to open if other than a Bible chapter.</param>
        /// <param name="mainScriptureFile">true iff TextFunc.js is to be included: ignored in this derived class</param>
        protected override void OpenHtmlFile(string fileName, bool mainScriptureFile, bool skipNav = false)
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
            // MakeFramesFor(currentFileName);
            htm = new StreamWriter(currentFileName, false, Encoding.UTF8);
            // Note: switching to HTML5 syntax, with XHTML-compatible lower case element names and XML-style empty elements (like <br />).
            htm.WriteLine("<!DOCTYPE html>");
            htm.WriteLine("<html lang=\"{0}\">", shortLangId);
            htm.WriteLine("<head>");
            htm.WriteLine("<meta charset=\"UTF-8\" />");
            htm.WriteLine("<link rel=\"stylesheet\" href=\"{0}\" type=\"text/css\" />", customCssName);
            htm.WriteLine("<meta name=\"viewport\" content=\"user-scalable=yes, initial-scale=1, minimum-scale=1, width=device-width\"/>");
            htm.WriteLine("<title>{0} {1} {2}</title>",
                translationName, currentBookHeader, currentChapterPublished);
            htm.WriteLine(string.Format("<meta name=\"keywords\" content=\"{0}, {1}, Holy Bible, Scripture, Bible, Scriptures, New Testament, Old Testament, Gospel\" />",
                translationName, langId));
            htm.WriteLine("</head>");
            htm.WriteLine("<body>");
            WriteNavButtons();
            htm.WriteLine("<div class=\"main\">");
        }

        /// <summary>
        /// Finish up the HTML file contents and close the file.
        /// </summary>
        protected override void CloseHtmlFile()
        {
            if (htm != null)
            {
                EndHtmlNote();
                EndHtmlTextStyle();
                EndChapter();
                EndHtmlParagraph();
                RepeatNavButtons();
                htm.WriteLine("<div class=\"footnote\">");
                WriteHtmlFootnotes();
                htm.WriteLine("</div>"); // end of div.footnote
                if ((!String.IsNullOrEmpty(footerTextHTML)) || (!String.IsNullOrEmpty(copyrightLinkHTML)))
                {
                    htm.WriteLine("<div class=\"copyright\">");
                    htm.WriteLine(String.Format("{0}{1}<p align=\"center\">{2}</p>", footerTextHTML, Environment.NewLine, (projectOptions.anonymous || projectOptions.silentCopyright) ? String.Empty : copyrightLinkHTML));
                    htm.WriteLine("</div>");    // copyright
                }
                htm.WriteLine("</div></body></html>");  // close main div class="main"
                htm.Close();
                htm = null;
                previousFileName = currentFileName;
                noteNumber = 0;
            }
            chopChapter = false;
        }

        /// <summary>
        /// Write HTML text, escaping greater than, less than, and ampersand.
        /// </summary>
        /// <param name="text">text to write</param>
        protected override void WriteHtmlText(string text)
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


        public int maxPreverseLength = 0;
        protected bool inVerse = false;

      
        /// <summary>
        /// Start a verse with the appropriate marker and anchor.
        /// </summary>
        protected override void StartVerse()
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
            htm.Write(string.Format(" <span class=\"verse\" id=\"V{1}\">{0}&#160;</span>",
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
        /// End a verse span
        /// </summary>
        protected override void EndVerse()
        {
            if ((preVerse.Length > 0) && (htm != null))
            {
                htm.Write(preVerse.ToString());
                preVerse.Length = 0;
            }
        }

        /// <summary>
        /// Start an HTML note with both pop-up and page-bottom notes.
        /// </summary>
        /// <param name="style">"f" for footnote and "x" for cross reference</param>
        /// <param name="marker">"+" for automatic caller, "-" for no caller (useless for a popup), or a verbatim note caller</param>
        protected override void StartHtmlNote(string style, string marker)
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
            WriteHtml(String.Format("<a href=\"#{0}\" class=\"notemark\">{1}<span class=\"popup\">",
                noteId, marker));
            // Numeric chapter and verse numbers are used in internal references instead of the text versions, which may
            // include dashes in verse bridges.
            if (!String.IsNullOrEmpty(noteOriginFormat))
            {
                automaticOrigin = noteOriginFormat.Replace("%c", currentChapterPublished).Replace("%v", currentVersePublished);
            }
            if ((chapterNumber >= 1) && (verseNumber >= 1))
                footnotesToWrite.Append(String.Format("<p class=\"{0}\" id=\"{1}\"><span class=\"notemark\">{2}</span><a class=\"notebackref\" href=\"#V{3}\">{4}</a>{5}",
                    style, noteId, marker, verseNumber.ToString(), automaticOrigin,Environment.NewLine));
            else
                footnotesToWrite.Append(String.Format("<p class=\"{0}\" id=\"{1}\"><span class=\"notemark\">{2}</span><a class=\"notebackref\" href=\"#V1\">^</a>{3}",
                    style, noteId, marker,Environment.NewLine));
        }

        /// <summary>
        /// Finish writing an HTML note
        /// </summary>
        protected override void EndHtmlNote()
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
        /// Start an HTML paragraph with the specified style and initial text.
        /// If the paragraph is noncanonical introductory material, it should
        /// be marked as isPreverse to indicate that it should be in the file
        /// with the next verse, not the previous one.
        /// </summary>
        /// <param name="style">Paragraph style name to use for CSS class</param>
        /// <param name="text">Initial text of the paragraph, if any</param>
        /// <param name="isPreverse">true iff this paragraph style is non canonical,
        /// like section titles, book introductions, and such</param>
        protected override void StartHtmlParagraph(string style, bool isPreverse)
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
                    WriteHtml(String.Format("<div class='chapterlabel' id=\"V0\">{0} {1}</div>", psalmLabel, currentChapterPublished));
                }
                else
                {
                    WriteHtml(String.Format("<div class='chapterlabel' id=\"V0\">{0} {1}</div>", chapterLabel, currentChapterPublished));
                }
                newChapterFound = false;
            }
            string s = String.Format("<div class='{0}'>", style);
            if (style == "b")
                s = s + " &#160; ";
            WriteHtml(s);
        }

        /// <summary>
        /// Finish up an HTML paragraph, which is really a div element.
        /// </summary>
        protected override void EndHtmlParagraph()
        {
            if (inParagraph)
            {
                WriteHtml("</div>");
                inParagraph = false;
            }
        }


        protected override void EndChapter()
        {
            if (inChapter)
            {
                inChapter = false;
            }
        }



        /// <summary>
        /// Process a chapter tag
        /// </summary>
        protected override void ProcessChapter()
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
        /// Write copyright and license information in about.html
        /// </summary>
        /// <param name="chapFormat">Not used- any string</param>
        /// <param name="licenseHtml">Text of the copyright and license information</param>
        /// <param name="goText">Link text for going to book list</param>
        protected override void WriteCopyrightPage(string chapFormat, string licenseHtml, string goText)
        {
            // licenseHtml = "<img class='bibcover' src='" + coverName + "' height='200' width:'130' style='padding: 0 15px 15px 0; float:left' />" + licenseHtml;

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
            htm.WriteLine("<p>&#160;<br/><br/></p>");
            if (indexDateStamp != String.Empty)
                htm.WriteLine("<div class=\"fine\">{0}</div>", indexDateStamp);
            CloseHtmlFile();
            // indexDateStamp = String.Empty;

            // Audio/download copyright page
            OpenHtmlFile("copr.htm", false, true);
            htm.WriteLine("<div class=\"main\">");
            htm.WriteLine("<div class=\"toc\">{0}</div></br>", sourceLink);
            htm.WriteLine(licenseHtml);
            if (indexDateStamp != String.Empty)
                htm.WriteLine("<div class=\"fine\">{0}</div>", indexDateStamp);
            CloseHtmlFile();
        }


        /// <summary>
        /// Append a line to the Table of Contents
        /// </summary>
        /// <param name="level">Table of contents line class, i.e. toc1, toc2, toc3...</param>
        /// <param name="title">Title to add</param>
        protected override void AppendToc(string level, string title)
        {
            bookRecord.toc.Append(String.Format("<div class=\"{0}\"><a href=\"{1}#V{2}\">{3}</a></div>"+Environment.NewLine,
                level,
                MainFileLinkTarget(currentBookAbbrev, Math.Max(1, chapterNumber).ToString(chapFormat)),
                verseNumber.ToString(), title));
            firstTitle = false;
        }

        /// <summary>
        /// Add chapter to table of contents
        /// </summary>
        protected override void ChapterToc()
        {
            newChapterFound = false;
        }

        /// <summary>
        /// Open an HTML fallback book or chapter index file.
        /// </summary>
        /// <param name="sw">StreamWriter to open</param>
        /// <param name="fileName">Name of file to open</param>
        /// <param name="title">HTML title of file</param>
        /// <param name="bodyClass">Body class tag to include</param>
        private void OpenIndexHtml(ref StreamWriter sw, string fileName, string title, string bodyClass)
        {
            sw = new StreamWriter(fileName, false, Encoding.UTF8);
            sw.WriteLine(@"<!DOCTYPE html>
<html>
<head>
<meta charset='utf-8' />
<meta name='viewport' content='width=device-width, initial-scale=1.0, user-scalable=no' />
<title>{0}</title>
<link href='{2}' rel='stylesheet' />
</head>
<body class='{1} {3}'>", title, bodyClass, customCssName, fontClass);
        }



        /// <summary>
        /// Write table of contents file
        /// </summary>
        /// <param name="translationId">translation ID code</param>
        /// <param name="indexHtml">Unused</param>
        /// <param name="goText">Text for link to starting point</param>
        protected override void GenerateIndexFile(string translationId, string indexHtml, string goText)
        {
            string indexFilePath = Path.Combine(htmDir, IndexFileName);
            string chapFormat;
            int i, j;
            currentChapter = currentChapterPublished = "";
            chapterNumber = 0;
            currentBookAbbrev = string.Empty;
            currentBookHeader = string.Empty;
            OpenIndexHtml(ref htm, indexFilePath, translationName + " " + currentBookHeader, "bklist");
            BibleBookRecord br;
            ChapterInfo ci;
            StreamWriter chapIndexFile = null;

            CheckHomeLink();
            htm.WriteLine("<h1><a href='{0}'>{1}</a></h1>", homeLinkHTML, translationName);
            bookListIndex = 0;
            htm.WriteLine("<div class='bookList'><ul class='vnav'>");
            string buttonClass;
            string contentsName;
            for (i = 0; i < bookList.Count; i++)
            {
                br = (BibleBookRecord)bookList[i];
                buttonClass = String.Format("{0}{0}", br.testament, br.testament);
                if (br.tla == "PSA")
                    chapFormat = "000";
                else
                    chapFormat = "00";
                contentsName = br.tla + chapFormat + ".htm";
                if (File.Exists(Path.Combine(htmDir, contentsName)))
                {   // If there is a section title contents page
                    htm.WriteLine("<li><a class='{0}' href='{1}'>{2}</a></li>", buttonClass, contentsName, br.vernacularShortName);
                }
                else
                {
                    foreach (string chapFile in chapterFileList)
                    {
                        if (chapFile.StartsWith(br.tla))
                        {
                            htm.WriteLine("<li><a class='{0}' href='{1}.htm'>{2}</a></li>", buttonClass, chapFile, br.vernacularShortName);
                            break;
                        }
                    }
                }
            }
            if ((copyrightLinkHTML != null) && (copyrightLinkHTML.Trim().Length > 0) && !projectOptions.silentCopyright)
            {
                htm.WriteLine("<li>{0}</li>", copyrightLinkHTML);
            }
            htm.WriteLine("</ul></div>"); // End of bookList div

            htm.WriteLine("<div class=\"mainindex\">");
            for (i = 0; i < bookInfo.publishArrayCount; i++)
            {
                if (bookInfo.publishArray[i] != null)
                {
                    br = (BibleBookRecord)bookInfo.publishArray[i];
                    if (br.tla == "PSA")
                        chapFormat = "000";
                    else
                        chapFormat = "00";
                    if (br.chapterFiles != null)
                    {
                        OpenIndexHtml(ref chapIndexFile, Path.Combine(htmDir, br.tla + ".htm"), translationName + " " + br.vernacularShortName, "chlist");
                        chapIndexFile.WriteLine("<h1><a href='{0}'>{1}</a></h1><h1><a href='index.htm'>{2}</a></h1>",
                            homeLinkHTML, translationName, br.vernacularShortName);
                        chapIndexFile.WriteLine("<ul class='tnav'>");
                        if (br.toc.Length > 0)
                        {
                            chapIndexFile.WriteLine("<li><a href='{0}{1}.htm'>{2}</a></li>", br.tla, 0.ToString(chapFormat), fileHelper.LocalizeDigits("0"));
                        }
                        for (j = 0; j < br.chaptersFound.Count; j++)
                        {
                            if (br.chaptersFound[j] != null)
                            {
                                ci = (ChapterInfo)br.chaptersFound[j];
                                chapIndexFile.WriteLine("<li><a href='{0}{1}.htm'>{2}</a></li>", br.tla, ci.chapterInteger.ToString(chapFormat), fileHelper.LocalizeDigits(ci.published));
                                if (((ci.chapterInteger % 5) == 0) && (ci.chapterInteger > 0) && (j < br.chaptersFound.Count - 1))
                                {
                                    chapIndexFile.WriteLine("</ul><ul class='tnav'>");
                                }
                            }
                        }
                        chapIndexFile.WriteLine("</ul></body></html>");
                        chapIndexFile.Close();
                    }
                }
            }
            




            bookRecord = (BibleBookRecord)bookList[0];
            htm.WriteLine("<div class=\"toc\"><a href=\"{0}.htm\">{1}</a></div>",
                          StartingFile(), goText);
            if (Directory.Exists("/home/kahunapule/vhosts/ebible.org/httpdocs/epub"))
            {
                if (redistributable)
                {
                    htm.WriteLine("<div class=\"toc\">epub3: <a href=\"https://eBible.org/epub/{0}.epub\">{0}.epub</a></div>", translationId);
                    if (File.Exists(String.Format("/home/kahunapule/vhosts/ebible.org/httpdocs/epub/{0}.mobi", translationId)))
                        htm.WriteLine("<div class=\"toc\">Kindle mobi: <a href=\"https://eBible.org/epub/{0}.mobi\">{0}.mobi</a></div>", translationId);
                    if (projectOptions.homeDomain == "png.bible")
                    {
                        htm.WriteLine("<div class=\"toc\"><a href=\"https://PNG.Bible/pdf/{0}/\" target=\"_blank\">PDF</a></div>", translationId);
                    }
                    else
                    {
                        htm.WriteLine("<div class=\"toc\"><a href=\"https://eBible.org/pdf/{0}/\" target=\"_blank\">PDF</a></div>", translationId);
                    }
                    htm.WriteLine("<div class=\"toc\"><a href=\"https://ebible.org/study/?w1=bible&t1=local%3A{0}&v1={1}1_1\" target=\"_blank\">Browser Bible</a></div>",
                        translationId, StartingShortCode);
                    htm.WriteLine("<div Class=\"toc\"><a href=\"https://ebible.org/sword/zip/{0}.zip\">Crosswire Sword module</a></div>", projectOptions.SwordName);
                    if (!indexHtml.Contains("details.php"))
                    {
                        htm.WriteLine("<div Class=\"toc\"><a href=\"https://ebible.org/find/details.php?id={0}\">More formats to read or download...</a>", translationId);
                    }
                }
            }
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
            htm.WriteLine("<p>&#160;<br/><br/></p>");
            if (indexDateStamp != String.Empty)
            {
                htm.WriteLine("<div class=\"fine\">{0}</div>", indexDateStamp);
            }
            htm.WriteLine("</div></body></html>");
            htm.Close();
            htm = null;
        }


        protected void WriteHtml(string s, object o1, object o2, object o3, object o4)
        {
            WriteHtml(String.Format(s, o1, o2, o3, o4));
        }

        protected void WriteHtml(string s, object o1, object o2, object o3)
        {
            WriteHtml(String.Format(s, o1, o2, o3));
        }

        protected void WriteHtml(string s, object o1, object o2)
        {
            WriteHtml(String.Format(s, o1, o2));
        }

        protected void WriteHtml(string s, object o)
        {
            WriteHtml(String.Format(s, o));
        }


        /// <summary>
        /// Write text to HTML file if open, or queue it up for when an HTML file gets opened.
        /// </summary>
        /// <param name="s"></param>
        protected override void WriteHtml(string s)
        {
            if (!ignore)
            {
                if (htm == null)
                    preVerse.Append(s);
                else
                    htm.Write(s);
            }
        }


        protected override void EndBook()
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


        /// <summary>
        /// Write contents page with section title links
        /// </summary>
        protected override void WriteContentsPage()
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

        /// <summary>
        /// Start a hyperlink to the destination indicated.
        /// </summary>
        /// <param name="tgt"></param>
        /// <param name="web"></param>
        protected override void StartLink(string tgt, string web)
        {
            string chapterFormat = "00";
            string theLink;
            if (!String.IsNullOrEmpty(tgt))
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
            else if (!String.IsNullOrEmpty(web))
            {
                inLink = true;
                theLink = String.Format("<a href=\"{0}\">", web);
                if (inFootnote)
                    footnotesToWrite.Append(theLink);
                else
                    WriteHtml(theLink);
            }
        }

        /// <summary>
        /// End a link started by StartLink.
        /// </summary>
        protected override void EndLink()
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
        /// Insert a picture with a caption
        /// </summary>
        /// <param name="figFileName">name of file to display</param>
        /// <param name="figCopyright">copyright message to display</param>
        /// <param name="figCaption">caption to display</param>
        /// <param name="figReference">verse(s) this figure illustrates</param>
        protected override void insertHtmlPicture(string figFileName, string figCopyright, string figCaption, string figReference)
        {
            figFileName = CheckPicture(figFileName);
            if (figFileName.Length > 4)
            {
                WriteHtml(String.Format("<div class=\"figure\"><img src=\"{0}\"><br/><span class=\"figcopr\" />{1}</br>" +
                   "</span><span class=\"figref\">{2}</span><span class=\"figCaption\"> {3}</span></div>", figFileName, figCopyright, figReference, figCaption));
            }
        }

        /// <summary>
        /// Close the last HTML file if one was open.
        /// </summary>
        protected override void LeaveHeader()
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

        protected string verseRangeEnd;
        protected int verseRangeEndNumber;
        protected string verseClass;

        /// <summary>
        /// Process a verse marker
        /// </summary>
        protected override void ProcessVerse()
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



        protected override void StartStrongs(string StrongsNumber, string plural, string morphology = "", string lemma = "")
        {
            inStrongs = true;
        }

        protected override void EndStrongs()
        {
            inStrongs = false;
        }


        protected override string MainFileLinkTarget(string bookAbbrev, string chapter)
        {
            return MainFileLinkTarget(string.Format("{0}{1}.htm", bookAbbrev, chapter));
        }


        /// <summary>
        /// Given a book ID and a chapter identifier string, generate the corresponding HTML file name.
        /// </summary>
        /// <param name="bookId"></param>
        /// <param name="chap"></param>
        /// <returns></returns>
        public override string HtmName(string bookId, string chap)
        {
            if (chap.Length < 2)
                chap = "0" + chap;
            // We use 2 digits for chapter numbers in file names except for in the Psalms, where we use 3.
            if (bookId == "PSA" && chap.Length < 3)
                chap = "0" + chap;
            return bookId + chap + ".htm";
        }

    }
}
