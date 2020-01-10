// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003-2018, SIL International, EBT, Michael Paul Johnson, and eBible.org.
// <copyright from='2003' to='2018' company='SIL International, EBT, Michael Paul Johnson, and eBible.org'>
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
    public class usfx2BrowserBible:usfxToHtmlConverter
    {
        /// <summary>
        /// Constructor, setting default value(s)
        /// </summary>
        public usfx2BrowserBible()
        {
        }

        /// <summary>
        /// Write the navigation elements at the bottom of the page.
        /// </summary>
        protected override void RepeatNavButtons()
        {
            htm.WriteLine("<div class='footer'><div class='nav'>");
            if (!String.IsNullOrEmpty(previd))
                htm.WriteLine("<a class='prev' href='" + previd + ".html'>&#9664;</a>");
            htm.WriteLine("<a class='home' href='index.html'>&#9776;</a>");
            if (!String.IsNullOrEmpty(nextid))
                htm.WriteLine("<a class='next' href='" + nextid + ".html'>&#9654;</a>");
            htm.WriteLine("</div></div>");
        }


        /// <summary>
        /// Write navigational links to get to another chapter from here.
        /// 
        /// </summary>
        protected override void WriteNavButtons()
        {
            ChapterInfo ci;
            nextid = previd = String.Empty;

            if (allChapterIndex >= bookInfo.allChapters.Count)
                allChapterIndex = 0;
            ci = (ChapterInfo)bookInfo.allChapters[allChapterIndex];
            if (ci.chapterId != chapterId)
            {
                allChapterIndex = 0;
                while ((allChapterIndex < bookInfo.allChapters.Count) && (ci.chapterId != chapterId))
                {
                    allChapterIndex++;
                    if (allChapterIndex < bookInfo.allChapters.Count)
                        ci = (ChapterInfo)bookInfo.allChapters[allChapterIndex];
                }
            }
            if (ci.chapterId != chapterId)
            {
                allChapterIndex = 0;
                return;
            }
            if (allChapterIndex > 0)
            {
                ci = (ChapterInfo)bookInfo.allChapters[allChapterIndex - 1];
                previd = ci.chapterId;
            }
            allChapterIndex++;
            if (allChapterIndex < bookInfo.allChapters.Count)
            {
                ci = (ChapterInfo)bookInfo.allChapters[allChapterIndex];
                nextid = ci.chapterId;
            }
            htm.WriteLine("<div class='header'><div class='nav'>");
            htm.WriteLine("<a class='home' href='index.html'> &#9776; </a><a class='location {0}' href='{1}.html'> {2} {3} </a>",
                 fontClass, currentBookCode, currentBookHeader, currentChapterPublished);
            if (!String.IsNullOrEmpty(previd))
                htm.WriteLine("<a class='prev' href='" + previd + ".html'> &#9664; </a>");
            if (!String.IsNullOrEmpty(nextid))
                htm.WriteLine("<a class='next' href='" + nextid + ".html'> &#9654; </a>");
            htm.WriteLine("</div></div>");

        }

        /// <summary>
        /// Figure out what the file name of the contents page or first chapter of the current book is, allowing for
        /// possible (likely) missing contents pages or chapter 1.
        /// </summary>
        /// <param name="formatString"></param>
        /// <returns></returns>
        protected override string FirstChapterFile(string formatString)
        {
            string firstChapterFile = String.Empty;

            int bookIndex = 0;
            int chapterIndex;
            BibleBookRecord br;
            ChapterInfo ci;

            while ((bookIndex < bookInfo.publishArray.Length) && (firstChapterFile == String.Empty))
            {
                br = bookInfo.publishArray[bookIndex];
                if (br.IsPresent && (br.tla == currentBookAbbrev))
                {
                    chapterIndex = 0;
                    while ((chapterIndex < br.chaptersFound.Count) && (firstChapterFile == String.Empty))
                    {
                        ci = (ChapterInfo)br.chaptersFound[chapterIndex];
                        if (ci.chapterInteger > 0)
                        {
                            firstChapterFile = String.Format("{0}{1}.html", br.shortCode, ci.chapterInteger.ToString());
                        }
                        chapterIndex++;
                    }
                }
                bookIndex++;
            }
            return firstChapterFile;
        }

        /// <summary>
        /// Name of the first file which has actual Bible text
        /// </summary>
        /// <returns>First file with actual Bible text</returns>
        protected override string StartingFile()
        {
            string startHere = String.Empty;
            int bookIndex = 0;
            int chapterIndex;
            BibleBookRecord br;
            ChapterInfo ci;

            while ((bookIndex < bookInfo.publishArray.Length) && (startHere == String.Empty))
            {
                br = bookInfo.publishArray[bookIndex];
                if (br.IsPresent && ((br.testament == "o") || (br.testament == "n") || (br.testament == "a")))
                {
                    chapterIndex = 0;
                    while ((chapterIndex < br.chaptersFound.Count) && (startHere == String.Empty))
                    {
                        ci = (ChapterInfo)br.chaptersFound[chapterIndex];
                        if (ci.chapterInteger > 0)
                        {
                            startHere = String.Format("{0}{1}", br.shortCode, ci.chapterInteger.ToString());
                        }
                        chapterIndex++;
                    }
                }
                bookIndex++;
            }
            return startHere;
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
        /// Open an HTML file named with the given name if non-empty, or with the book abbreviation followed by "." and the chapter number then ".html"
        /// and write the HTML header.
        /// </summary>
        /// <param name="fileName">Name of file to open if other than a Bible chapter.</param>
        /// <param name="mainScriptureFile">true iff TextFunc.js is to be included.</param>
        protected override void OpenHtmlFile(string fileName, bool mainScriptureFile, bool skipNav = false)
        {
            string s;
            string shortLang = langCodes.ShortCode(languageIdentifier);

            if ((shortLang == "en") && (countryCode.Length == 2))
            {
                shortLang = shortLang + "-" + countryCode;
            }

            CloseHtmlFile();
            if ((fileName != null) && (fileName.Length > 0))
            {
                currentFileName = Path.Combine(htmDir, fileName);
            }
            else
            {
                currentFileName = Path.Combine(htmDir, chapterId + ".html");
            }
            //htm = new StreamWriter(currentFileName, false, Encoding.UTF8);
            htm = new StreamWriter(Path.ChangeExtension(currentFileName, ".html"), false, Encoding.UTF8);
            //htm.WriteLine("{");
            htm.WriteLine("<!DOCTYPE html>");
            htm.WriteLine(@"<html><head><meta charset='UTF-8' />
<meta name='viewport' content='width=device-width, initial-scale=1.0, user-scalable=no' />
<title>{0} {1} {2}</title>
<link href='{3}' rel='stylesheet' />
<link href='fallback.css' rel='stylesheet' />
</head><body dir='{4}' class='section-document'>", translationName, currentBookHeader, currentChapter, customCssName, textDirection);
            WriteNavButtons();
            if (translationIdentifier == languageIdentifier)
            {
                s = String.Format("<div class='chapter section {0} {1} {5} {6}' dir='{2}' data-id='{0}' data-nextid='{3}' data-previd='{4}' lang='{7}'>",
                   chapterId, translationIdentifier, textDirection, nextid, previd, currentBookCode,
                   fontClass,
                   shortLang);
            }
            else
            {
                s = String.Format("<div class='chapter section {0} {1} {2} {6} {7}' dir='{3}' data-id='{0}' data-nextid='{4}' data-previd='{5}' lang='{8}'>",
                   chapterId, translationIdentifier, languageIdentifier, textDirection, nextid, previd, currentBookCode,
                   fontClass,
                   shortLang);
            }
            htm.Write("{0}", s);
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
                EndHtmlParagraph();
                EndChapter();
                htm.WriteLine("</div>");
                htm.WriteLine("<div class='footnotes'>");
                htm.WriteLine(footnotesToWrite.ToString());
                htm.WriteLine("</div>");
                footnotesToWrite.Length = 0;
                RepeatNavButtons();
                htm.WriteLine("</body></html>");
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
                    WriteHtml(escapeHtml);
                }
                
            }
        }

        protected bool inVerse = false;

        /// <summary>
        /// Start a verse span element
        /// </summary>
        protected void StartVerseSpan()
        {
            sqlVerseId = verseId;
            sqlCanonOrder = ((BibleBookRecord)bookInfo.books[currentBookAbbrev]).sortOrder.ToString("000") + "." + Utils.zeroPad(3, currentChapter) + "." +
                Utils.zeroPad(3, currentVerse);
            WriteHtml(string.Format("<span class='v {0}' data-id='{1}'><span class='{4} v-{2}'>{3}&#160;</span>",
                verseClass, verseId, currentVerse,
                currentVersePublished, verseNumber == 1 ? "verse1 v-num" : "v-num"));

            inVerse = true;
            verseSpanSuspended = false;
        }


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
                WriteHtml(preVerse.ToString());
                preVerse.Length = 0;
            }
            StartVerseSpan();
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
            if (inVerse && (htm != null))
                WriteHtml("</span>");
            if ((sqlVerseTable != null) && (null != sqlVerseContents))
            {
                string verseContents = sqlVerseContents.Replace("\"", "\\\"").ToString();
                sqlVerseTable.WriteLine($"INSERT INTO {sqlTableName} VALUES (\"{sqlCanonOrder}\",\"{currentBookAbbrev}\",\"{currentChapter}\",\"{sqlVerseId}\",\"{verseContents}\");");
                sqlVerseContents.Length = 0;
            }
            inVerse = false;
            verseSpanSuspended = false;
        }

        private int noteNum = 0;

        /// <summary>
        /// Start an HTML note with both pop-up and page-bottom notes.
        /// </summary>
        /// <param name="style">"f" for footnote and "x" for cross reference</param>
        /// <param name="marker">"+" for automatic caller, "-" for no caller (useless for a popup), or a verbatim note caller</param>
        protected override void StartHtmlNote(string style, string marker)
        {
            string automaticOrigin = String.Empty;
            EndHtmlNote();
            string noteId = String.Format("note-{0}", noteNum++);
            if (ignoreNotes)
            {
                ignore = true;
                return;
            }
            inFootnoteStyle = false;
            inFootnote = true;
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
                marker = "+";
            WriteHtml(String.Format("<span class='note' id='foot{0}'><a href='#{0}' class='key'>{1}</a></span>",
                noteId, marker));
            if (!String.IsNullOrEmpty(noteOriginFormat))
            {
                automaticOrigin = noteOriginFormat.Replace("%c", currentChapterPublished).Replace("%v", currentVersePublished);
            }

            footnotesToWrite.Append(String.Format("{3}<span class='footnote' id='{0}'><span class='key'>{1} </span><a href='#foot{0}' class='backref'>{2}</a> <span class='text'>",
                noteId, marker, automaticOrigin, Environment.NewLine));
        }

        /// <summary>
        /// Finish writing an HTML note
        /// </summary>
        protected override void EndHtmlNote()
        {
            if (inFootnote)
            {
                EndHtmlNoteStyle();
                //WriteHtml("</span></span>");   // End popup text
                //xRef = null;
                footnotesToWrite.Append("</span></span>");
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
                    WriteHtml(String.Format("<div class='c'>{0} {1}</div>", psalmLabel, currentChapterPublished));
                }
                else
                {
                    WriteHtml(String.Format("<div class='c'>{0}</div>", currentChapterPublished));
                }
                newChapterFound = false;
            }
            string s = String.Format("<div class='{0}'>", style);
            if (style == "b")
                s = s + " &#160; ";
            WriteHtml(s);
            RestartVerseSpan();
        }

        protected bool verseSpanSuspended = false;  // True iff we are interrupting a verse span to start a new paragraph

        /// <summary>
        /// Stops a verse highlighting span to cross paragraph boundaries, keeping span nested within div elements.
        /// </summary>
        protected void SuspendVerseSpan()
        {
            if (inVerse && !verseSpanSuspended)
            {
                WriteHtml("</span>");
                verseSpanSuspended = true;
            }
        }

        /// <summary>
        /// Restart a verse highlighting span that crossed paragraph boundaries.
        /// </summary>
        protected void RestartVerseSpan()
        {
            if (inVerse && verseSpanSuspended)
            {
                WriteHtml(string.Format("<span class='v {0}' data-id='{1}'>",
                          verseClass, verseId));
                verseSpanSuspended = false;
            }
        }


        /// <summary>
        /// Finish up an HTML paragraph, which is really a div element.
        /// </summary>
        protected override void EndHtmlParagraph()
        {
            SuspendVerseSpan();
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
                //WriteHtml("</div></div>");    Those divs are not used, now.
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
            chapterId = currentBookCode + chapterNumber.ToString();
            verseId = chapterId + "_0";
            chopChapter = true;
            newChapterMarkNeeded = newChapterFound = true;
            CloseHtmlFile();
            sqlVerseContents = new StringBuilder();
            chapterFileIndex++;
            inHeader = false;
            inChapter = true;
        }


        public string b64CoverName;

        /// <summary>
        /// Write copyright and license information in about.html
        /// </summary>
        /// <param name="chapFormat">Not used- any string</param>
        /// <param name="licenseHtml">Text of the copyright and license information</param>
        /// <param name="goText">Link text for going to book list</param>
        protected override void WriteCopyrightPage(string chapFormat, string licenseHtml, string goText)
        {
            // string coverLink, b64cover;
            // Copyright page
            CloseHtmlFile();

            /*
            if (File.Exists(b64CoverName))
            {
                StreamReader sr = new StreamReader(b64CoverName);
                b64cover = sr.ReadToEnd();
                sr.Close();
                coverLink = String.Format("<a href='content/texts/{0}/{1}'><img class='bibcover' src='data:image/png;base64,{2}' style='padding: 0 15px 15px 0; float:left' /></a>",
                    fcbhId, coverName, b64cover);
            }
            else
            {
                coverLink = "<img class='bibcover' src='content/texts/" + fcbhId + "/" + coverName + "' height='200' width:'130' style='padding: 0 15px 15px 0; float:left' />";
            }
            */

            // Writing about.html using metadata we already know.
            currentFileName = Path.Combine(htmDir, "about.html");
            htm = new StreamWriter(currentFileName, false, Encoding.UTF8);
            htm.WriteLine("<!DOCTYPE html>");
            htm.WriteLine("<html><head>");
            htm.WriteLine("<link href='fallback.css' rel='stylesheet' />");
            htm.WriteLine("<link href='{0}' rel='stylesheet' />", customCssName);
            htm.WriteLine("<meta name='viewport' content='width=device-width', initial-scale=1'>");
            htm.WriteLine("<meta charset='UTF-8' /><title>{0}</title>", translationName);
            htm.WriteLine("</head><body>");
            // htm.WriteLine("<h1>{0}</h1>", translationName);
            htm.WriteLine("<div class=\"about\">");
            // Expand cover image link and copy it here IF it exists
            // licenseHtml = coverLink + licenseHtml;
            htm.WriteLine(licenseHtml);

            htm.WriteLine("</div><p>&#160;<br/><br/></p>");
            if (indexDateStamp != String.Empty)
                htm.WriteLine("<div class=\"fine\">{0}</div>", indexDateStamp);
            htm.WriteLine("</body></html>");
            CloseHtmlFile();

        }


        /// <summary>
        /// Append a line to the Table of Contents
        /// </summary>
        /// <param name="level">Table of contents line class, i.e. toc1, toc2, toc3...</param>
        /// <param name="title">Title to add</param>
        protected override void AppendToc(string level, string title)
        {
            if ((level == "toc2") && (title == "0"))
                return;
            if (!firstTitle)
            {
                bookRecord.toc.Append(",");
                bookRecord.toc.Append(Environment.NewLine);
            }
            if (level.StartsWith("toc") && (level.Length > 3))
                level = level.Substring(3);
            bookRecord.toc.Append("{");
            bookRecord.toc.Append(Environment.NewLine);
            bookRecord.toc.Append(String.Format("\"level\":\"{0}\",{3}\"title\":\"{1}\",{3}\"verseId\":\"{2}\"",
                level,
                fileHelper.escapeJsonString(title),
                verseId,
                Environment.NewLine));
            bookRecord.toc.Append(Environment.NewLine);
            bookRecord.toc.Append("}");
            firstTitle = false;
        }

        /// <summary>
        /// Add chapter to table of contents
        /// </summary>
        protected override void ChapterToc()
        {
            if (newChapterFound)
            {
                AppendToc("2", currentChapterPublished);
                newChapterFound = false;
            }
        }


        /// <summary>
        /// Open an HTML fallback book or chapter index file.
        /// </summary>
        /// <param name="sw">StreamWriter to open</param>
        /// <param name="fileName">Name of file to open</param>
        /// <param name="title">HTML title of file</param>
        /// <param name="bodyClass">Body class tag to include</param>
        private void openIndexHtml(ref StreamWriter sw, string fileName, string title, string bodyClass)
        {
            sw = new StreamWriter(fileName, false, Encoding.UTF8);
            sw.WriteLine(@"<!DOCTYPE html>
<html>
<head>
<meta charset='utf-8' />
<meta name='viewport' content='width=device-width, initial-scale=1.0, user-scalable=no' />
<title>{0}</title>
<link href='{3}' rel='stylesheet' />
<link href='fallback.css' rel='stylesheet' />
</head>
<body dir='{1}' class='{2} {4}'>
<div class='header'><div class='nav'>", title, textDirection, bodyClass, customCssName, fontClass);
        }


        /// <summary>
        /// Write table of contents json file and index files for plain HTML fallback
        /// </summary>
        /// <param name="translationId">translation ID code</param>
        /// <param name="indexHtml">Unused</param>
        /// <param name="goText">Text for link to starting point</param>
        protected override void GenerateIndexFile(string translationId, string indexHtml, string goText)
        {
            int i, j;
            StringBuilder sb;
            StreamWriter contents = new StreamWriter(Path.Combine(htmDir, "contents.json"));
            StreamWriter bookIndexFile = null;
            StreamWriter chapIndexFile = null;
            BibleBookRecord br;
            ChapterInfo ci;
            contents.Write("{\n");
            contents.Write("\"id\":\"contents\",\n");
            contents.Write("\"tableOfContents\":[\n");
            for (i = 0; i < bookInfo.publishArray.Length; i++)
            {
                if (bookInfo.publishArray[i] != null)
                {
                    sb = bookInfo.publishArray[i].toc;
                    if ((sb != null) && (sb.Length > 0) && (bookInfo.publishArray[i].HasContent))
                    {
                        contents.Write(sb.ToString()+"\n");
                    }
                }
            }
            contents.Write("]\n}\n");
            contents.Close();

            openIndexHtml(ref bookIndexFile, Path.Combine(htmDir, "index.html"), translationName, "text-index");
            bookIndexFile.WriteLine("<a class='name {1}' href='../index.html'>{0}</a></div></div>", translationName, fontClass);
            bookIndexFile.WriteLine("<ul class='division-list'>");
            for (i = 0; i < bookInfo.publishArrayCount; i++)
            {
                if (bookInfo.publishArray[i] != null)
                {
                    br = (BibleBookRecord)bookInfo.publishArray[i];
                    if (br.chapterFiles != null)
                    {
                        bookIndexFile.WriteLine("<li><a href='{0}.html' class='{1}{1}'>{2}</a></li>", br.shortCode, br.testament, br.vernacularShortName);
                        openIndexHtml(ref chapIndexFile, Path.Combine(htmDir, br.shortCode + ".html"), translationName + " " + br.vernacularShortName, "section-list");
                        chapIndexFile.WriteLine("<span class='name {1}'>{0}</span>", br.vernacularShortName, fontClass);
                        chapIndexFile.WriteLine("<a class='home' href='index.html'>^</a>");
                        chapIndexFile.WriteLine("</div></div>");
                        chapIndexFile.WriteLine("<ul class='section-list {0}'>", fontClass);
                        for (j = 0; j < br.chaptersFound.Count; j++)
                        {
                            if (br.chaptersFound[j] != null)
                            {
                                ci = (ChapterInfo)br.chaptersFound[j];
                                chapIndexFile.WriteLine("<li><a href='{0}.html'>{1} {2}</a></li>", ci.chapterId, br.vernacularShortName, fileHelper.LocalizeDigits(ci.published));
                            }
                        }
                        chapIndexFile.WriteLine("</ul></body></html>");
                        chapIndexFile.Close();
                    }
                }
            }
            bookIndexFile.WriteLine("<li><a href='about.html' class='x'>©?</a></li>");
            bookIndexFile.WriteLine("</ul></body></html>");
            bookIndexFile.Close();
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
                {
                    preVerse.Append(s);
                }
                else
                {
                    //htm.Write(fileHelper.escapeJsonString(s));
                    htm.Write(s.Replace("<div", Environment.NewLine + "<div").Replace("<span class='v ", Environment.NewLine+"<span class='v "));
                    if (sqlVerseContents != null)
                    {
                        sqlVerseContents.Append(s);
                    }
                }
            }
        }


        protected override void EndBook()
        {
            if (htm == null)
                OpenHtmlFile();
            if (preVerse.Length > 0)
            {
                WriteHtml(preVerse.ToString());
                preVerse.Length = 0;
            }
            CloseHtmlFile();
        }


        /// <summary>
        /// Write title.json and info.json
        /// </summary>
        protected override void WriteContentsPage()
        {
            int i;
            bool needComma;
            StreamWriter infojson, titlejson;
            ChapterInfo ci;
            if (shortTitle == string.Empty)
                shortTitle = englishDescription;
            string lastBook = string.Empty;
            string countryArray;
            countryArray = "\"countries\":[\"" + countries.Replace(" ", "\",\"") + "\"]";
            /*
            switch (languageIdentifier)
            {
                case "cmn":
                    countryArray = "\"countries\": [\"CN\",\"HK\",\"MO\",\"TW\"]";
                    break;
                case "eng":
                    countryArray = "\"countries\":[\"AU\",\"CA\",\"GB\",\"NZ\",\"US\"]";
                    break;
                case "fra":
                    countryArray = "\"countries\":[\"BE\",\"CD\",\"CI\",\"CM\",\"FR\",\"HT\"]";
                    break;
                case "deu":
                    countryArray = "\"countries\":[\"AT\",\"BE\",\"CH\",\"DE\",\"LI\",\"LU\"]";
                    break;
                case "rus":
                    countryArray = "\"countries\":[\"BY\",\"KG\",\"KZ\",\"RU\",\"TJ\"]";
                    break;
                case "spa":
                    countryArray = "\"countries\": [\"AR\",\"BO\",\"CL\",\"CO\",\"CR\",\"CU\",\"DO\",\"EC\",\"ES\",\"HN\",\"MX\",\"NI\",\"PA\",\"PE\",\"PR\",\"PY\",\"SV\",\"UY\",\"VE\"]";
                    break;
                default:
                    countryArray = "\"countries\":[\"" + countries.Replace(" ", "\",\"") + "\"]";
                    break;
            }
            */
            countryArray = countryArray + ",\n";
            string allCountriesArray = "\"allcountries\":[\"" + countries.Replace(" ", "\",\"") + "\"],\n";
            try
            {
                if (String.IsNullOrEmpty(traditionalAbbreviation))
                    traditionalAbbreviation = translationIdentifier;
                infojson = new StreamWriter(Path.Combine(htmDir, "info.json"));
                titlejson = new StreamWriter(Path.Combine(htmDir, "title.json"));
                infojson.Write("{\n");
                infojson.Write("\"id\":\"{0}\",\n", translationIdentifier);
                infojson.Write("\"haiola_id\":\"{0}\",\n", translationIdentifier);
                infojson.Write("\"fcbh_id\":\"{0}\",\n", fcbhId);
                infojson.Write("\"type\":\"bible\",\n");
                infojson.Write("\"name\":\"{0}\",\n", translationName);
                infojson.Write("\"nameEnglish\":\"{0}\",\n", shortTitle);
                if (projectOptions.hasStrongs)
                {
                    infojson.Write("\"hasLemma\":true,\n");
                }
                else
                {
                    infojson.Write("\"hasLemma\":false,\n");
                }
                infojson.Write("\"abbr\":\"{0}\",\n", traditionalAbbreviation);
                infojson.Write("\"dir\":\"{0}\",\n", textDirection);
                infojson.Write("\"lang\":\"{0}\",\n", languageIdentifier);
                infojson.Write("\"langName\":\"{0}\",\n", languageNameInVernacular);
                infojson.Write("\"langNameEnglish\":\"{0}\",\n", languageNameInEnglish);
                infojson.Write("\"fontClass\":\"{0}\",\n", fontClass);
                infojson.Write("\"script\":\"{0}\",\n", script);
                infojson.Write("\"dialectCode\":\"{0}\",\n", dialectCode);
                infojson.Write("\"audioDirectory\":\"{0}\",\n", fcbhId);
                infojson.Write("\"fcbh_drama_nt\":\"{0}\",\n", fcbhDramaNt);
                infojson.Write("\"fcbh_drama_ot\":\"{0}\",\n", fcbhDramaOt);
                infojson.Write("\"fcbh_audio_nt\":\"{0}\",\n", fcbhAudioNt);
                infojson.Write("\"fcbh_audio_ot\":\"{0}\",\n", fcbhAudioOt);
                infojson.Write("\"fcbh_portion\":\"{0}\",\n", fcbhPortion);
                infojson.Write(numbers+"\n");
                infojson.Write("\"country\":\"{0}\",\n", country);
                infojson.Write("\"countryCode\":\"{0}\",\n", countryCode);
                infojson.Write(countryArray);
                infojson.Write(allCountriesArray);
                infojson.Write("\"stylesheet\":\"{0}\",\n", customCssName);
                infojson.Write("\"timeGenerated\":\"{0}\",\n", DateTime.UtcNow.ToString("s"));
                titlejson.Write("{\n");
                titlejson.Write("\"id\":\"{0}\",\n", translationIdentifier);
                titlejson.Write("\"type\":\"bible\",\n");
                titlejson.Write("\"name\":\"{0}\",\n", translationName);
                titlejson.Write("\"nameEnglish\":\"{0}\",\n", shortTitle);
                if (projectOptions.hasStrongs)
                {
                    titlejson.Write("\"hasLemma\":true,\n");
                }
                else
                {
                    titlejson.Write("\"hasLemma\":false,\n");
                }
                if (projectOptions.redistributable)
                {
                    titlejson.Write("\"redistributable\":true,\n");
                }
                else
                {
                    titlejson.Write("\"redistributable\":false,\n");
                }
                if (projectOptions.redistributable)
                {
                    infojson.Write("\"redistributable\":true,\n");
                }
                else
                {
                    infojson.Write("\"redistributable\":false,\n");
                }
                titlejson.Write("\"abbr\":\"{0}\",\n", traditionalAbbreviation);
                titlejson.Write("\"dir\":\"{0}\",\n", textDirection);
                titlejson.Write("\"lang\":\"{0}\",\n", languageIdentifier);
                titlejson.Write("\"country\":\"{0}\",\n", country);
                titlejson.Write(countryArray);
                titlejson.Write(allCountriesArray);
                titlejson.Write("\"langNameEnglish\":\"{0}\",\n", languageNameInEnglish);
                titlejson.Write("\"langName\":\"{0}\",\n", languageNameInVernacular);
                titlejson.Write("\"fontClass\":\"{0}\"\n", fontClass);
                titlejson.Write("}\n");
                titlejson.Close();
                needComma = false;
                infojson.Write("\"divisionNames\":[");
                for (i = 0; i < bookInfo.publishArrayCount; i++)
                {
                    if (bookInfo.publishArray[i].IsPresent && (bookInfo.publishArray[i].chapterFiles != null) && (bookInfo.publishArray[i].chaptersFound.Count > 0))
                    {   // This book is in the input files and contains at least one character of text.
                        if (needComma)
                        {
                            infojson.Write(",");
                        }
                        infojson.Write("\"{0}\"", bookInfo.publishArray[i].vernacularShortName);
                        needComma = true;
                    }
                }
                infojson.Write("],\n");
                needComma = false;
                infojson.Write("\"divisions\":[");
                for (i = 0; i < bookInfo.publishArrayCount; i++)
                {
                    if (bookInfo.publishArray[i].IsPresent && (bookInfo.publishArray[i].chapterFiles != null) && (bookInfo.publishArray[i].chaptersFound.Count > 0))
                    {   // This book is in the input files and contains at least one character of text.
                        if (needComma)
                            infojson.Write(",");
                        infojson.Write("\"{0}\"", bookInfo.publishArray[i].shortCode);
                        needComma = true;
                    }
                }
                infojson.Write("],\n");
                bool hasShortAbbreviations = true;
                StringBuilder sb = new StringBuilder("\"divisionAbbreviations\":[");
                needComma = false;
                for (i = 0; i < bookInfo.publishArrayCount; i++)
                {
                    if (bookInfo.publishArray[i].IsPresent && (bookInfo.publishArray[i].chapterFiles != null) && (bookInfo.publishArray[i].chaptersFound.Count > 0))
                    {
                        if (needComma)
                            sb.Append(",");
                        string abbr = bookInfo.publishArray[i].vernacularAbbreviation;
                        if (abbr.Length < 3)
                        {
                            sb.Append(String.Format("\"{0}\"", abbr));
                        }
                        else
                        {
                            hasShortAbbreviations = false;
                            i = bookInfo.publishArrayCount;
                        }
                        needComma = true;
                    }
                }
                sb.Append("],");
                if (hasShortAbbreviations)
                    infojson.Write(sb.ToString()+"\n");
                needComma = false;
                infojson.Write("\"sections\":[");
                for (i = 0; i < bookInfo.allChapters.Count; i++)
                {
                    ci = (ChapterInfo)bookInfo.allChapters[i];
                    if ((ci != null) && (ci.chapterId.Length > 2))
                    {
                        if (needComma)
                        {
                            infojson.Write(",");
                            if (ci.chapterId.Substring(0, 2) != lastBook)
                            {
                                infojson.Write("\n");
                            }
                        }
                        infojson.Write("\"{0}\"", ci.chapterId);
                    }
                    needComma = true;
                    lastBook = ci.chapterId.Substring(0, 2);
                }
                infojson.Write("]\n}\n");
                infojson.Close();
            }
            catch (Exception err)
            {
                Logit.WriteError(err.Message + " writing contents files version.json and index.html in " + htmDir);
                Logit.WriteError(err.StackTrace);
            }
        }

        /// <summary>
        /// Start a hyperlink to the destination indicated.
        /// </summary>
        /// <param name="tgt"></param>
        /// <param name="web"></param>
        protected override void StartLink(string tgt, string web)
        {
            string theLink;
            if (tgt.Length > 0)
            {
                BCVInfo bcvRec = bookInfo.ValidateInternalReference(tgt);
                if (bcvRec.exists)
                {
                    string s = String.Format("<span class='bibleref' data-id='{0}{1}_{2}'>",
                        bcvRec.bkInfo.shortCode,
                        bcvRec.chapInfo.chapterInteger.ToString(),
                        bcvRec.vsInfo.startVerse.ToString());
                    if (inFootnote)
                        footnotesToWrite.Append(s);
                    else
                        WriteHtml(s);
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
        protected override void EndLink()
        {
            if (inLink)
            {
                if (inFootnote)
                    footnotesToWrite.Append("</span>");
                else
                    WriteHtml("</span>");
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
                WriteHtml(String.Format("<div class='figure'><img src='{0}'><br/><span class='figcopr' />{1}</br>" +
                   "</span><span class='figref'>{2}</span><span class='figCaption'> {3}</span></div>", figFileName, figCopyright, figReference, figCaption));
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
            string firstVerseString = currentVerse = id;
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

            int dashPlace = currentVerse.IndexOf('-');
            if (dashPlace > 0)
            {
                verseRangeEnd = currentVerse.Substring(dashPlace + 1);
                firstVerseString = currentVerse.Substring(0, dashPlace);
            }
            else
            {
                verseRangeEnd = firstVerseString;
            }
            int vnum;
            if (Int32.TryParse(firstVerseString, out vnum))
            {
                verseNumber = vnum;
            }
            else
            {
                verseNumber++;
            }
            if (Int32.TryParse(verseRangeEnd, out vnum))
            {
                verseRangeEndNumber = vnum;
            }
            else
            {
                verseRangeEndNumber = verseNumber;
            }
            verseOsisId = chapterOsisId + "." + verseNumber.ToString();
            verseClass = verseId = chapterId + "_" + firstVerseString;  // was verseNumber.ToString();
            for (int i = verseNumber + 1; i <= verseRangeEndNumber; i++)
            {
                verseClass = verseClass + " " + chapterId + "_" + i.ToString();
            }
            /* Uncomment if you need a list of verse bridges.
            if (verseRangeEndNumber != verseNumber)
            {
                StreamWriter sw = new StreamWriter(Path.Combine(Path.Combine(Path.Combine(htmDir, ".."), ".."), "versebridgelog.txt"), true);
                sw.WriteLine("{0} {1}", translationIdentifier, currentBCV);
                sw.Close();
            }
            */
            StartVerse();
        }



        protected override void StartStrongs(string StrongsNumber, string plural, string morphology = "", string lemma = "")
        {
            if ((!String.IsNullOrEmpty(StrongsNumber))|| (!String.IsNullOrEmpty(morphology)))
            {
                WriteHtml("<l ");
                if (!String.IsNullOrEmpty(StrongsNumber))
                    WriteHtml("s='{0}'", StrongsNumber);
                if (!String.IsNullOrEmpty(morphology))
                    WriteHtml(" m='{0}'", morphology.Replace("robinson:",""));
                WriteHtml(">");
                inStrongs = true;
            }
        }

        protected override void EndStrongs()
        {
            if (inStrongs)
            {
                WriteHtml("</l>");
                inStrongs = false;
            }
        }


        protected override string MainFileLinkTarget(string bookAbbrev, string chapter)
        {
            BibleBookRecord bkRec = (BibleBookRecord)bookInfo.books[bookAbbrev];
            return MainFileLinkTarget(string.Format("{0}.{1}.htm", bkRec.tla, chapter));
        }

 
        /// <summary>
        /// Given a book ID and a chapter identifier string, generate the corresponding HTML file name.
        /// </summary>
        /// <param name="bookId"></param>
        /// <param name="chap"></param>
        /// <returns></returns>
        public override string HtmName(string bookId, string chap)
        {
            bookRecord = (BibleBookRecord)bookInfo.books[bookId];
            return bookRecord.tla + "." + chap + ".html";
        }

    }
}
