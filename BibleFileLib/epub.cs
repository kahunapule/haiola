using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Collections;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Schema;
using System.Diagnostics;
using ICSharpCode.SharpZipLib;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;


namespace WordSend
{
    public class ePubWriter : usfxToHtmlConverter
    {   // Write an ePub3 file that is readable by ePub2 readers

        /// <summary>
        /// Constructor, setting default value(s)
        /// </summary>
        public ePubWriter()
        {
            langCodes = new LanguageCodeInfo();
            homeLinkFixed = false;
        }

        public string epubDirectory;

        /// <summary>
        /// Write the navigation elements at the bottom of the page.
        /// </summary>
        protected override void RepeatNavButtons()
        {   // We don't actually do this in epubs, at least so far. This would be redundant with the reader's features.
            // htm.WriteLine(navButtons);
        }


        /// <summary>
        /// Write navigational links to get to another chapter from here.
        /// 
        /// </summary>
        protected override void WriteNavButtons()
        {
            string shortBookCode = String.Empty;
            int i;
            bool looking = true;
            try{
                StringBuilder btns = new StringBuilder(String.Format(@"<ul class='tnav'>
<li><a href='index.xhtml'>{0}</a></li>
</ul>", String.IsNullOrEmpty(currentBookHeader) ? "^" : currentBookHeader));
                if ((currentBookAbbrev != "CPR"))
                {
                    for (i = 0; (i < bookInfo.publishArray.Length) && looking; i++)
                    {
                        if (bookInfo.publishArray[i] != null)
                        {
                            if (bookInfo.publishArray[i].tla == currentBookAbbrev)
                            {
                                looking = false;
                                shortBookCode = bookInfo.publishArray[i].shortCode;
                                if ((bookInfo.publishArray[i].chaptersFound != null) && (bookInfo.publishArray[i].chaptersFound.Count > 1))
                                {
                                    btns.Append("<ul class='tnav'>");
                                    foreach (ChapterInfo ci in bookInfo.publishArray[i].chaptersFound)
                                    {
                                        if ((ci.chapterInteger > 10) && (ci.chapterInteger % 10 == 1))
                                        {
                                            btns.Append("</ul><ul class='tnav'>" + Environment.NewLine);
                                        }
                                        btns.Append(String.Format("<li><a href='{0}.xhtml#{1}{2}_0'>{3}</a></li>" + Environment.NewLine,
                                            currentBookAbbrev, shortBookCode, ci.chapterInteger.ToString(), fileHelper.LocalizeDigits(ci.published)));
                                    }
                                    btns.Append("</ul>" + Environment.NewLine);
                                }
                            }
                        }
                    }
                }
                htm.WriteLine(btns.ToString());
            }
            catch (Exception ex)
            {
                Logit.WriteError("ERROR in ePubWriter::WriteNavButtons():");
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
                return String.Format("{0}{1}.xhtml", currentBookAbbrev, 0.ToString(formatString));
            string firstChapterFile = "";
            for (int i = 0; (i < chapterFileList.Count) && (firstChapterFile == String.Empty); i++)
            {
                string chFile = (string)chapterFileList[i];
                if (chFile.StartsWith(currentBookAbbrev))
                {
                    // The first match for this book might be chapter 1 or, if that is missing, a later chapter.
                    firstChapterFile = chFile + ".xhtml";
                }
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

        /// <summary>
        /// Open an HTML file named with the given name if non-empty, or with the book abbreviation followed by "." and the chapter number then ".xhtml"
        /// and write the HTML header.
        /// </summary>
        /// <param name="fileName">Name of file to open if other than a Bible chapter.</param>
        /// <param name="mainScriptureFile">true iff TextFunc.js is to be included: ignored in this derived class</param>
        protected override void OpenHtmlFile(string fileName, bool mainScriptureFile, bool skipNav = false)
        {
            CloseHtmlFile();
            if ((fileName != null) && (fileName.Length > 0))
            {
                currentFileName = Path.Combine(htmDir, fileName);
            }
            else
            {
                currentFileName = Path.Combine(htmDir, currentBookAbbrev + ".xhtml");
            }
            // MakeFramesFor(currentFileName);
            htm = new StreamWriter(currentFileName, false, Encoding.UTF8);
            // Note: switching to HTML5 syntax, with html-compatible lower case element names and XML-style empty elements (like <br />).
            htm.WriteLine("<!DOCTYPE html>");
            htm.WriteLine("<html xmlns=\"http://www.w3.org/1999/xhtml\" xmlns:epub=\"http://www.idpf.org/2007/ops\" lang=\"{0}\" dir=\"{1}\">", shortLangId, textDirection);
            htm.WriteLine("<head>");
            htm.WriteLine("<meta charset=\"UTF-8\" />");
            htm.WriteLine("<link rel=\"stylesheet\" href=\"{0}\" type=\"text/css\" />", customCssName);
            htm.WriteLine("<meta name=\"viewport\" content=\"user-scalable=yes, initial-scale=1, minimum-scale=1, width=device-width, height=device-height\"/>");
            htm.WriteLine("<title>{0} {1} {2}</title>",
                EscapeHtml(translationName), currentBookHeader, currentChapterPublished);
            htm.WriteLine("</head>");
            htm.WriteLine("<body dir='{0}'>", textDirection);
            WriteNavButtons();
            htm.WriteLine("<div class='main' id='{0}0_0'>", currentBookCode);
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
                {   // Write text only to footnote aside
                    footnotesToWrite.Append(escapeHtml);
                }
                else
                {   // Write to main stream of text
                    if (eatSpace)
                    {
                        text = text.TrimStart();
                        eatSpace = false;
                    }
                    WriteHtml(escapeHtml);
                }
            }
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
                htm.WriteLine(preVerse.ToString());
                preVerse.Length = 0;
            }
            htm.WriteLine();
            htm.Write(string.Format("<span class=\"verse\" id=\"{1}\">{0}&#160;</span>",
                currentVersePublished, verseId));
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
        /// Name of the first file which has actual Bible text
        /// </summary>
        /// <returns>First file with actual Bible text</returns>
        protected override string StartingFile()
        {
            string startHere = String.Empty;
            int bookIndex = 0;
            BibleBookRecord br;

            while ((bookIndex < bookInfo.publishArray.Length) && (startHere == String.Empty))
            {
                br = bookInfo.publishArray[bookIndex];
                if (br.IsPresent && br.includeThisBook && ((br.testament == "o") || (br.testament == "n") || (br.testament == "a")))
                {
                   startHere = String.Format("{0}", br.tla);
                }
                bookIndex++;
            }
            return startHere;
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
                marker = "*";   // Our footnotes won't display without SOME marker, so "no marker" becomes "asterisk" in this publication path.
            WriteHtml(String.Format("<a href='#{0}' epub:type='noteref' class='noteref'>{1}</a>", noteId, marker));
            /** shelving the CSS pop-up note: didn't work an all targets.
            WriteHtml(String.Format("<span class='note'><input type='checkbox' id='{0}' class='popnote'><label for='{0}'><span class='ntlbl'>{1}</span><span class='box'><span class='ftxt'>",
                noteId, marker));
             **/
            // Numeric chapter and verse numbers are used in internal references instead of the text versions, which may
            // include dashes in verse bridges and might be in alternate number systems.
            if (!String.IsNullOrEmpty(noteOriginFormat))
            {
                automaticOrigin = noteOriginFormat.Replace("%c", currentChapterPublished).Replace("%v", currentVersePublished);
            }
            // Send footnote contents to an aside element at the bottom of the document (end note that may be hidden).
            footnotesToWrite.Append(String.Format("<aside epub:type='footnote' id=\"{1}\"><p class=\"{0}\"><a class=\"notebackref\" href=\"#{3}\"><span class=\"notemark\">{2}</span> {4}</a>{5}",
                style, noteId, marker, verseId, automaticOrigin, Environment.NewLine));
            
        }

        /// <summary>
        /// Finish writing an HTML note
        /// </summary>
        protected override void EndHtmlNote()
        {
            if (inFootnote)
            {
                EndHtmlNoteStyle();
                footnotesToWrite.Append("</p></aside>" + Environment.NewLine);    // End footnote paragraph
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
            string chapterIdAttribute;
            if (newChapterMarkNeeded)
                chapterIdAttribute = String.Format(" id='{0}'", verseId);
            else
                chapterIdAttribute = String.Empty;
            if ((!inParagraph) && (style == "nb"))
            {
                style = "m";
                Logit.WriteError("Error: nb paragraph missing predecessor paragraph at " + currentBCV);
            }

            if (style != "nb")  // nb means no break with previous paragraph, so we don't end the div or start a new one for it.
                EndHtmlParagraph();
            
            inParagraph = true;
            if (newChapterFound && (style != "ms") && (style != "b") && !style.StartsWith("i"))
            {
                if (currentBookAbbrev.CompareTo("PSA") == 0)
                {   // Label in line, above the chapter
                    WriteHtml(String.Format("\n<div class='psalmlabel'{0}>{1} {2}</div>", chapterIdAttribute, psalmLabel, currentChapterPublished));
                }
                else // if (style.StartsWith("q"))
                {
                    WriteHtml(String.Format("\n<div class='psalmlabel'{0}>{1} {2}</div>", chapterIdAttribute, chapterLabel, currentChapterPublished));
                }
                /****
                else
                {   // Label off to the left (or right for rtl scripts), just the number
                    WriteHtml(String.Format("\n<div class='chapterlabel'{0}>{1}</div>", chapterIdAttribute, currentChapterPublished));
                }
                ****/
                newChapterMarkNeeded = newChapterFound = false;
                chapterIdAttribute = String.Empty;
            }
            if (style != "nb")
            {
                string s = String.Format("\n<div class='{0}'{1}>", style, chapterIdAttribute);
                newChapterMarkNeeded = false;
                if (style == "b")
                    s = s + " &#160; ";
                WriteHtml(s);
            }
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
            chopChapter = false;
            newChapterMarkNeeded = newChapterFound = true;
            if (currentBookAbbrev != previousBookId)
            {
                CloseHtmlFile();
                chapterFileIndex++;
                previousBookId = currentBookAbbrev;
            }
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
            licenseHtml = "<img class='bibcover' src='"+coverName+"' height='400' width='260' style='padding: 0 15px 15px 0; float:left' />" + licenseHtml;
            // Copyright page
            currentBookAbbrev = "CPR";
            bookListIndex = -1;
            OpenHtmlFile("copyright.xhtml");
            bookListIndex = 0;
            bookRecord = (BibleBookRecord)bookList[0];
            /* Links to linear content from nonlinear content confuse some epub readers, thus the goText link is not useful.
            htm.WriteLine("<div class=\"toc\"><a href=\"{0}.xhtml\">{1}</a></div>",
                StartingFile(), goText);
            */
            htm.WriteLine(licenseHtml);
            htm.WriteLine("<p>&#160;<br/><br/></p>");
            if (indexDateStamp != String.Empty)
                htm.WriteLine("<div class=\"fine\">{0}<br/><br/>{1}</div>", indexDateStamp, epubIdentifier);
            CloseHtmlFile();
            // indexDateStamp = String.Empty;
        }


        /// <summary>
        /// Append a line to the Table of Contents
        /// </summary>
        /// <param name="level">Table of contents line class, i.e. toc1, toc2, toc3...</param>
        /// <param name="title">Title to add</param>
        protected override void AppendToc(string level, string title)
        {
            bookRecord.toc.Append(String.Format("<div class=\"{0}\"><a href=\"{1}.xhtml#{2}{3}_{4}\">{5}</a></div>" + Environment.NewLine,
                level,
                currentBookAbbrev,
                bookInfo.getShortCode(currentBookAbbrev),
                Math.Max(1, chapterNumber).ToString(),
                verseNumber.ToString(),
                title));
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
<meta name='viewport' content='width=device-width, initial-scale=1.0' />
<title>{0}</title>
<link href='{3}' rel='stylesheet' />
</head>
<body dir='{1}' class='{2}'>", title, textDirection, bodyClass, customCssName);
        }


        /// <summary>
        /// Write table of contents file
        /// </summary>
        /// <param name="translationId">translation ID code</param>
        /// <param name="indexHtml">Unused</param>
        /// <param name="goText">Text for link to starting point</param>
        protected override void GenerateIndexFile(string translationId, string indexHtml, string goText)
        {
            string bodyLink = String.Empty;
            string bodyText = String.Empty;
            string glosText = String.Empty;
            string prefaceId = String.Empty;
            string prefaceTitle = String.Empty;

            // Create cover XHTML file.
            StreamWriter sw = new StreamWriter(Path.Combine(htmDir, "cover.xhtml"));
            sw.WriteLine("<!DOCTYPE html>");
            sw.WriteLine("<html xmlns=\"http://www.w3.org/1999/xhtml\" xmlns:epub=\"http://www.idpf.org/2007/ops\">");
            sw.WriteLine("<head>");
            sw.WriteLine("<title>Cover</title>");
            sw.WriteLine("<style type=\"text/css\">");
            sw.WriteLine("<meta name='viewport' content='width=device-width,initial-scale=1'/>");
            sw.WriteLine("body {margin:0em;padding:0em}");
            sw.WriteLine("img {max-width:100%;max-height:100%}");
            sw.WriteLine("</style>");
            sw.WriteLine("</head>");
            sw.WriteLine("<body>");
            sw.WriteLine(" <img src=\"{0}\" alt=\"{1}\" />", coverName, EscapeHtml(translationName));
            sw.WriteLine("</body>");
            sw.WriteLine("</html>");
            sw.Close();


            // Create index file.
            string indexFilePath = Path.Combine(htmDir, "index.xhtml");
            htm = new StreamWriter(indexFilePath, false, Encoding.UTF8);
            htm.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            htm.WriteLine("<html xmlns=\"http://www.w3.org/1999/xhtml\" xmlns:epub=\"http://www.idpf.org/2007/ops\">");
            htm.WriteLine("<head>");
		    htm.WriteLine(" <meta charset=\"utf-8\" />");
            htm.WriteLine(" <meta name='viewport' content='width=device-width,initial-scale=1'/>");
            htm.WriteLine(" <title>{0}</title>", EscapeHtml(translationName));
            htm.WriteLine(" <link href='epub.css' rel='stylesheet' />");
            htm.WriteLine("</head>");
            htm.WriteLine("<body dir='{0}' class='toc'>", textDirection);
            htm.WriteLine(" <nav epub:type=\"toc\" id=\"toc\">");
            htm.WriteLine("  <h1 class=\"title\">{0}</h1>", EscapeHtml(translationName));
            htm.WriteLine("  <ol class='nav'>");
            if ((copyrightLinkHTML != null) && (copyrightLinkHTML.Trim().Length > 0) && !projectOptions.silentCopyright && !projectOptions.anonymous)
            {
                htm.WriteLine("   <li>{0}</li>", copyrightLinkHTML);
            }
            foreach (BibleBookRecord br in bookInfo.publishArray)
            {
                if ((br != null) && br.IsPresent)
                {
                    string bkName = br.vernacularShortName;
                    if (bkName.Length < 1)
                        bkName = br.vernacularHeader;
                    if (bkName.Length < 1)
                        bkName = br.vernacularLongName;
                    htm.WriteLine("   <li><a class='{0}{0}' href='{1}.xhtml'>{2}</a>", br.testament, br.tla, bkName);
                    if ((bodyLink == String.Empty) && (br.testament != "x"))
                    {
                        bodyLink = br.tla;
                        bodyText = bkName;
                    }
                    else if (br.tla == "GLO")
                    {
                        glosText = bkName;
                    }
                    else if ((br.tla == "PRE") || ((br.tla == "FRT") && String.IsNullOrEmpty(prefaceTitle)))
                    {
                        prefaceTitle = bkName;
                        prefaceId = br.tla;
                    }
                    /* Chapters in the TOC make for too much scrolling where the CSS is ignored.
                    if (br.chaptersFound.Count > 1)
                    {
                        htm.WriteLine("   <ol class='chap'>");
                        foreach (ChapterInfo ci in br.chaptersFound)
                        {
                            htm.WriteLine("    <li><a href='{0}.xhtml#{1}{2}_0'>{3}</a></li>", br.tla, br.shortCode, ci.chapterInteger.ToString(), ci.published);
                        }
                        htm.WriteLine("   </ol>");
                    }
                    */
                    htm.WriteLine("   </li>");
                }
            }
            htm.WriteLine("  </ol>");
            htm.WriteLine(" </nav>");
            htm.WriteLine(" <nav epub:type='landmarks' class='hidden' hidden='hidden'>");
            htm.WriteLine(" <h2>Guide</h2>");
            htm.WriteLine(" <ol>");
            htm.WriteLine("  <li><a epub:type='cover' href='cover.xhtml'>cover</a></li>");
            htm.WriteLine("  <li><a epub:type='frontmatter' href='copyright.xhtml'>©?</a></li>");
            htm.WriteLine("  <li><a epub:type='toc' href='#toc'>{0}</a></li>", EscapeHtml(translationName));
            if (!String.IsNullOrEmpty(prefaceTitle))
            {
                htm.WriteLine("  <li><a epub:type='preface' href='{0}.xhtml'>{1}</a></li>", prefaceId, prefaceTitle);
            }
            if (!String.IsNullOrEmpty(bodyText))
            {
                htm.WriteLine("  <li><a epub:type='bodymatter' href='{0}.xhtml'>{1}</a></li>", bodyLink, bodyText);
            }
            if (!String.IsNullOrEmpty(glosText))
            {
                htm.WriteLine("  <li><a epub:type='glossary' href='GLO.xhtml'>{0}</a></li>", glosText);
            }
            htm.WriteLine(" </ol>");
            htm.WriteLine(" </nav>");
            htm.WriteLine("</body>");
            htm.WriteLine("</html>"); // End of document
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
        {   // TODO: write a section header index
            /*
            hasContentsPage = bookRecord.toc.Length > 0;
            currentChapter = "";
            if (hasContentsPage)
            {
                currentChapterPublished = "0";
                OpenHtmlFile();
                htm.Write("<div class='toc'><a href='index.html'>^</a></div>{0}",
                    bookRecord.toc.ToString());
                CloseHtmlFile();
            }
             */
        }

        /// <summary>
        /// Start a hyperlink to the destination indicated.
        /// </summary>
        /// <param name="tgt"></param>
        /// <param name="web"></param>
        protected override void StartLink(string tgt, string web)
        {
            string theLink;
            if (!String.IsNullOrEmpty(tgt))
            {
                BCVInfo bcvRec = bookInfo.ValidateInternalReference(tgt);
                if (bcvRec.exists)
                {
                    string s = String.Format("<a class='bibleref' href='{0}.xhtml#{1}{2}_{3}'>",
                        bcvRec.bkInfo.tla,
                        bcvRec.bkInfo.shortCode,
                        bcvRec.chapInfo.chapterInteger.ToString(),
                        bcvRec.vsInfo.startVerse.ToString());
                    if (inFootnote)
                    {
                        footnotesToWrite.Append(s);
                    }
                    else
                    {
                        WriteHtml(s);
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
                    WriteHtml(theLink);;

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
        /// Add chapter file to list of chapter files.
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


        /* Unused in this format
        protected override string MainFileLinkTarget(string bookAbbrev, string chapter)
        {
            return MainFileLinkTarget(string.Format("{0}.html#{1}{2}", bookAbbrev, bookInfo.getShortCode(bookAbbrev), chapter));
        }
        */


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
            return bookId + chap + ".xhtml";
        }

        protected int playOrder;
        protected XmlTextWriter ncx;

        protected void beginNavPoint(string text, string src)
        {
            ncx.WriteStartElement("navPoint");
            ncx.WriteAttributeString("id", "navPoint"+playOrder.ToString());
            ncx.WriteAttributeString("playOrder", playOrder.ToString());
            playOrder++;
            ncx.WriteStartElement("navLabel");
            ncx.WriteElementString("text", text);
            ncx.WriteEndElement();  // navLabel
            ncx.WriteStartElement("content");
            ncx.WriteAttributeString("src", src);
            ncx.WriteEndElement();  // content
        }

        protected void endNavPoint()
        {
            ncx.WriteEndElement();  // navPoint
        }

        /// <summary>
        /// Has this file got a reference to an SVG file in it?
        /// TODO: Use a smarter regular expression to look for .img in the context of a src attribute in an img tag to eliminate possible false positives.
        /// </summary>
        /// <param name="FilePath">Full path and name of text (XHTML) file to check</param>
        /// <returns>true iff the file contains a reference to a .svg file.</returns>
        protected bool HasSvg(string FilePath)
        {
            try
            {
                StreamReader sr = new StreamReader(FilePath);
                string text = sr.ReadToEnd();
                sr.Close();
                return (text.Contains(".svg"));
            }
            catch (Exception ex)
            {
                Logit.WriteError(ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Zip up the epub contents into a .epub .zip file with the proper mimetype.
        /// </summary>
        protected override void ZipResults()
        {
            string ext, baseName, fileId, prefix;
            int i;
            playOrder = 1;
            BibleBookRecord br;
            try
            {
                string s = fcbhId;
                if (String.IsNullOrEmpty(s))
                    s = translationIdentifier;
                string epubName = Path.Combine(epubDirectory, s + ".epub");
                string metaName = Path.Combine(epubDirectory, "META-INF");
                Utils.EnsureDirectory(metaName);
                string OEBPS = Path.Combine(epubDirectory, "OEBPS");
                //string emptyZip = SFConverter.FindAuxFile("emptypub.zip");
                //fileHelper.CopyFile(emptyZip, epubName);
                string containerName = Path.Combine(metaName, "container.xml");
                StreamWriter sw = new StreamWriter(containerName);
                sw.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"no\"?>");
                sw.WriteLine("<container version=\"1.0\" xmlns=\"urn:oasis:names:tc:opendocument:xmlns:container\">");
                sw.WriteLine(" <rootfiles>");
                sw.WriteLine("  <rootfile full-path=\"OEBPS/content.opf\" media-type=\"application/oebps-package+xml\"/>");
                sw.WriteLine(" </rootfiles>");
                sw.WriteLine("</container>");
                sw.Close();

                sw = new StreamWriter(Path.Combine(metaName, "com.apple.ibooks.display-options.xml"));
                sw.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                sw.WriteLine("<display_options>");
                sw.WriteLine("<platform name=\"*\">");
                sw.WriteLine("<option name=\"specified-fonts\">true</option>");
                sw.WriteLine("</platform>");
                sw.WriteLine("</display_options>");
                sw.Close();

                ncx = new XmlTextWriter(Path.Combine(OEBPS, "toc.ncx"), Encoding.UTF8);
                ncx.Formatting = Formatting.Indented;
                ncx.WriteStartDocument(false);
                ncx.WriteStartElement("ncx");
                ncx.WriteAttributeString("xmlns", "http://www.daisy.org/z3986/2005/ncx/");
                ncx.WriteAttributeString("version", "2005-1");
                ncx.WriteStartElement("head");
                ncx.WriteStartElement("meta");
                ncx.WriteAttributeString("content", "urn:uuid:" + epubIdentifier);
                ncx.WriteAttributeString("name", "dtb:uid");
                ncx.WriteEndElement();  // meta (unique identifier)
                ncx.WriteStartElement("meta");
                ncx.WriteAttributeString("content", "2");
                ncx.WriteAttributeString("name", "dtb:depth");
                ncx.WriteEndElement();  // meta depth
                ncx.WriteStartElement("meta");
                ncx.WriteAttributeString("content", "0");
                ncx.WriteAttributeString("name", "dtb:totalPageCount"); // Required but unused
                ncx.WriteEndElement();  // meta page count
                ncx.WriteStartElement("meta");
                ncx.WriteAttributeString("content", "0");
                ncx.WriteAttributeString("name", "dtb:maxPageNumber");  // Required but unused
                ncx.WriteEndElement();  // meta max page number
                ncx.WriteEndElement();  // head
                ncx.WriteStartElement("docTitle");
                ncx.WriteElementString("text", EscapeHtml(translationName));
                ncx.WriteEndElement();  // docTitle
                ncx.WriteStartElement("navMap");
                beginNavPoint(translationName, "cover.xhtml");
                endNavPoint();
                beginNavPoint("©?", "copyright.xhtml");
                endNavPoint();
                foreach (BibleBookRecord bk in bookInfo.publishArray)
                {
                    if ((bk != null) && bk.IsPresent)
                    {
                        string bkName = bk.vernacularLongName;
                        if (bkName.Length < 1)
                            bkName = bk.vernacularShortName;
                        if (bkName.Length < 1)
                            bkName = bk.vernacularHeader;
                        beginNavPoint(bkName, bk.tla + ".xhtml");
                        /* Chapters in the TOC make for too much scrolling.
                        if (bk.chaptersFound.Count > 1)
                        {
                            foreach (ChapterInfo ci in bk.chaptersFound)
                            {
                                beginNavPoint(ci.published, String.Format("{0}.xhtml#{1}{2}_0", bk.tla, bk.shortCode, ci.chapterInteger.ToString()));
                                endNavPoint();
                            }
                        }
                        */
                        endNavPoint();
                    }
                }
                ncx.WriteEndElement();  // navMap
                ncx.WriteEndElement();  // ncx
                ncx.Close();

                DirectoryInfo di = new DirectoryInfo(OEBPS);
                FileInfo []filesFound = di.GetFiles();
                XmlTextWriter xw = new XmlTextWriter(Path.Combine(OEBPS, "content.opf"), Encoding.UTF8);
                xw.Formatting = Formatting.Indented;
                xw.WriteStartDocument();
                xw.WriteStartElement("package");
                xw.WriteAttributeString("xmlns", "http://www.idpf.org/2007/opf");
                xw.WriteAttributeString("prefix", "ibooks: http://vocabulary.itunes.apple.com/rdf/ibooks/vocabulary-extensions-1.0/");
                xw.WriteAttributeString("version", "3.0");
                xw.WriteAttributeString("unique-identifier", "uid");
                xw.WriteStartElement("metadata");
                xw.WriteAttributeString("xmlns:dc", "http://purl.org/dc/elements/1.1/");
                xw.WriteStartElement("dc:identifier");
                xw.WriteAttributeString("id", "uid");
                xw.WriteString("urn:uuid:" + epubIdentifier);
                xw.WriteEndElement();   // dc:identifier
                xw.WriteElementString("dc:title", EscapeHtml(translationName));
                string fakeLang = shortLangId;
                if (fakeLang.Length > 2)
                    fakeLang = "en";
                xw.WriteElementString("dc:language", fakeLang);
                xw.WriteElementString("dc:rights", longCopr);
                if (!String.IsNullOrEmpty(contentCreator))
                    xw.WriteElementString("dc:creator", contentCreator);
                if (!String.IsNullOrEmpty(contributor))
                    xw.WriteElementString("dc:contributor", contributor);
                xw.WriteStartElement("meta");
                xw.WriteAttributeString("property", "dcterms:modified");
                xw.WriteString(DateTime.UtcNow.ToString("s")+"Z");
                xw.WriteEndElement();   // meta
                xw.WriteStartElement("meta");
                xw.WriteAttributeString("name", "cover");
                xw.WriteAttributeString("content", "cover-image");
                xw.WriteEndElement();   // meta
                xw.WriteStartElement("meta");
                xw.WriteAttributeString("property", "ibooks:specified-fonts");
                xw.WriteString("true");
                xw.WriteEndElement();   // meta
                xw.WriteEndElement();   // metadata
                xw.WriteStartElement("manifest");

                foreach (FileInfo f in filesFound)
                {
                    ext = Path.GetExtension(f.Name).ToLowerInvariant();
                    if (ext.Length > 2)
                    {
                        ext = ext.Substring(1);
                        prefix = ext.Substring(0, 1);
                    }
                    else
                    {
                        prefix = String.Empty;
                    }
                    baseName = Path.GetFileNameWithoutExtension(f.Name);
                    fileId = (prefix+baseName).Replace(".", "").ToLowerInvariant();
                    xw.WriteStartElement("item");
                    xw.WriteAttributeString("href", f.Name);
                    //TODO: search for .svg references instead of assuming to set properties="svg" in xhtml files.
                    if ((fileId == "scover") || (fileId == "jcover") || (fileId == "pcover"))
                    {
                        fileId = "cover-image";
                    }
                    else if (fileId == "ntoc")
                    {
                        fileId = "ncx";
                    }
                    else if (fileId == "xcover")
                    {
                        fileId = "cover";
                    }
                    xw.WriteAttributeString("id", fileId);
                    switch (ext)
                    {
                        case "html":
                        case "htm":
                        case "xhtml":
                           xw.WriteAttributeString("media-type", "application/xhtml+xml");
                            if (HasSvg(Path.Combine(OEBPS, f.Name)))
                                xw.WriteAttributeString("properties", "svg");
                        break;
                        case "css":
                            xw.WriteAttributeString("media-type", "text/css");
                        break;
                        case "jpg":
                        case "jpeg":
                            xw.WriteAttributeString("media-type", "image/jpeg");
                        break;
                        case "svg":
                            xw.WriteAttributeString("media-type", "image/svg+xml");
                        break;
                        case "gif":
                        xw.WriteAttributeString("media-type", "image/gif");
                        break;
                        case "png":
                        xw.WriteAttributeString("media-type", "image/png");
                        break;
                        case "woff":
                        xw.WriteAttributeString("media-type", "application/font-woff");
                        break;
                        case "ttf":
                        xw.WriteAttributeString("media-type", "application/font-ttf");
                        break;
                        case "js":
                        xw.WriteAttributeString("media-type", "application/javascript");
                        break;
                        case "json":
                        xw.WriteAttributeString("media-type", "application/json");
                        break;
                        case "ogg":
                        xw.WriteAttributeString("media-type", "application/ogg");
                        break;
                        case "pdf":
                        xw.WriteAttributeString("media-type", "application/pdf");
                        break;
                        case "mp4":
                        xw.WriteAttributeString("media-type", "audio/mp4");
                        break;
                        case "mp3":
                        case "mpeg":
                        xw.WriteAttributeString("media-type", "audio/mpeg");
                        break;
                        case "txt":
                        xw.WriteAttributeString("media-type", "text/plain");
                        break;
                        case "ncx": // For epub2 compatibility
                        xw.WriteAttributeString("media-type", "application/x-dtbncx+xml");
                        break;
                        default:
                        Logit.WriteError("Unrecognized file type: " + ext);
                        break;
                    }
                    if (f.Name == coverName)
                    {
                        xw.WriteAttributeString("properties", "cover-image");
                    }
                    else if (f.Name == "index.xhtml")
                        xw.WriteAttributeString("properties", "nav");
                    xw.WriteEndElement();   // item
                }
                xw.WriteEndElement();   // manifest
                xw.WriteStartElement("spine");
                xw.WriteAttributeString("toc", "ncx");
                // List front matter then Books of the Bible in reading order
                xw.WriteStartElement("itemref");
                xw.WriteAttributeString("idref", "cover");
                xw.WriteAttributeString("linear", "no");
                xw.WriteEndElement();   // itemref
                xw.WriteStartElement("itemref");
                xw.WriteAttributeString("idref", "xcopyright");
                xw.WriteAttributeString("linear", "no");
                xw.WriteEndElement();   // itemref
                xw.WriteStartElement("itemref");
                xw.WriteAttributeString("idref", "xindex");
                xw.WriteAttributeString("linear", "yes");
                xw.WriteEndElement();   // itemref

                for (i = 0; i < bookInfo.publishArrayCount; i++)
                {
                    if (bookInfo.publishArray[i] != null)
                    {
                        br = (BibleBookRecord)bookInfo.publishArray[i];
                        if (br.IsPresent)
                        {
                            xw.WriteStartElement("itemref");
                            xw.WriteAttributeString("idref", "x"+br.tla.ToLowerInvariant());
                            if (br.testament == "x")
                            {
                                xw.WriteAttributeString("linear", "no");
                            }
                            else
                            {
                                xw.WriteAttributeString("linear", "yes");
                            }
                            xw.WriteEndElement();   // itemref
                        }
                    }
                }
                xw.WriteEndElement();   // spine
                xw.WriteEndElement();   // package
                xw.Close();


                string mimeName = Path.Combine(epubDirectory, "mimetype");
                sw = new StreamWriter(mimeName);
                sw.Write("application/epub+zip");
                sw.Close();

                //TODO: create a proper signatures.xml file instead of just a text list of SHA1 hashes.
                /*
                sw = new StreamWriter(Path.Combine(metaName, "signatures.xml"));
                foreach (FileInfo fi in di.GetFiles())
                {
                    sw.WriteLine("{0} OEBPS/{1}", Utils.SHA1HashFile(fi.FullName), fi.Name);
                }
                sw.Close();
                */
                ZipFile zip = ZipFile.Create(epubName);
                zip.BeginUpdate();
                
                zip.Add(new FileDataSource(mimeName), "mimetype", CompressionMethod.Stored);

                di = new DirectoryInfo(metaName);
                foreach (var fi in di.GetFiles())
                {
                    zip.Add(fi.FullName, Path.Combine("META-INF",fi.Name));
                }
                di = new DirectoryInfo(OEBPS);
                foreach (var fi in di.GetFiles())
                {
                    zip.Add(fi.FullName, Path.Combine("OEBPS",fi.Name));
                }
                zip.CommitUpdate();
                zip.Close();



                Utils.DeleteFile(mimeName);
                Utils.DeleteDirectory(OEBPS);
                Utils.DeleteDirectory(metaName);
                
            }
            catch (Exception ex)
            {
                Logit.WriteError("Error creating epub:");
                Logit.WriteError(ex.Message + "\n" + ex.StackTrace);
            }
        }



    }

}
