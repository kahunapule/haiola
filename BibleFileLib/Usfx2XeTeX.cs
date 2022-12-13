﻿using System;
using System.Text;
using System.IO;
using System.Collections;
using System.Text.RegularExpressions;
using System.Xml;

namespace WordSend
{

    public class TexFont
    {   // This class represents a completely specified font, which may differ from the underlying font in one or more
        // of its attributes. For example, it may be the same as the underlying baseFont except that it is red.
        private string name;    // Name of the font as listed in the Operating System
        private bool? isBold;    // True iff the font is bold or null if undefined
        private bool? isItalic; // True iff the font is italic or null if undefined
        private string textColor;   // Text representation of 6 digit color RRGGBB
        private string numPoints;  // Number of points as a string. Decimal point is allowed, i.e. "9.5".

        public string FontName  // Name of the font per the Operating System
        {
            get
            {
                if (!String.IsNullOrEmpty(name))
                    return name;
                if (baseFont != null)
                    return baseFont.FontName;
                return "Gentium";
            }
            set { name = value; }
        }

        public bool bold   // True iff the font is bold
        {
            get
            {
                if (isBold != null)
                    return (bool) isBold;
                if (baseFont != null)
                    return baseFont.bold;
                return false;
            }
            set { isBold = bold; }
        }

        public bool italic // True iff the font is italicized
        {
            get
            {
                if (isItalic != null)
                    return (bool) isItalic;
                if (baseFont != null)
                    return baseFont.italic;
                return false;
            }
            set { isItalic = value; }
        }

        public string color    // 6 digit color RRGGBB
        {
            get
            {
                if (!String.IsNullOrEmpty(textColor))
                    return textColor;
                if (baseFont != null)
                    return baseFont.color;
                return "000000";
            }
            set { textColor = value; }
        }

        public string points   // number of points as a string. Decimal point is allowed, i.e. "9.5".
        {
            get
            {
                if (!String.IsNullOrEmpty(numPoints))
                    return numPoints;
                if (baseFont != null)
                    return baseFont.points;
                return "8";
            }
            set { numPoints = value; }
        }

        public TexFont baseFont;    // The attributes active below this one.

        public TexFont()
        {
            name = null;
            isBold = isItalic = null;
            textColor = numPoints = null;
            baseFont = null;
        }

        public TexFont(TexFont underlying)
        {
            name = null;
            isBold = isItalic = null;
            textColor = numPoints = null;
            baseFont = underlying;
        }
    }




    public class Usfx2XeTeX : usfxToHtmlConverter
    {
        protected string texFileName;
        protected StreamWriter texFile;
        public string texDir;
        public bool callXetex;
        protected Hashtable pageCounts;   // Number of pages in each PDF file as a string.

        /// <summary>
        /// Constructor
        /// </summary>
        public Usfx2XeTeX()
        {
            langCodes = new LanguageCodeInfo();
        }

        public const string LEFTBRACE = "{";
        public const string RIGHTBRACE = "}";

        /// <summary>
        /// Write the navigation elements at the bottom of the page.
        /// </summary>
        protected override void RepeatNavButtons()
        {   // We don't actually do this in XeTeX/PDF files, at least so far.
            // htm.WriteLine(navButtons);
        }



        /*
        /// <summary>
        /// Figure out what the file name of current book is.
        /// This is trivially simple in the case of XeTeX files, which contain whole books.
        /// </summary>
        /// <param name="formatString"></param>
        /// <returns></returns>
        protected override string FirstChapterFile(string formatString)
        {
            return currentBookAbbrev + ".tex";
        }
        */


        /// <summary>
        /// Open a Scripture chapter XeTeX file.
        /// </summary>
        protected override void OpenHtmlFile()
        {
            OpenHtmlFile("", true);
        }

        /// <summary>
        /// In this overridden version, we actually open a TeX file for the whole book, not just a chapter.
        /// </summary>
        /// <param name="fileName">Name of file to open if other than a Bible book.</param>
        /// <param name="mainScriptureFile">ignored </param>
        protected override void OpenHtmlFile(string fileName, bool mainScriptureFile = true, bool skipNav = false)
        {
            string runningHeader = currentBookHeader;
            if (runningHeader.Length > 34)
            {
                if (bookRecord.vernacularShortName.Length < runningHeader.Length)
                    runningHeader = bookRecord.vernacularShortName;
            }
            if (runningHeader.Length > 34)
            {
                if ((currentBookAbbrev == "CPR") || (currentBookAbbrev == "FRT") || (currentBookAbbrev == "TDX") || (currentBookAbbrev == "GLO") ||
                    (currentBookAbbrev == "INT") || (currentBookAbbrev.StartsWith("X")) || (currentBookAbbrev == "BAK"))
                    runningHeader = string.Empty;
                else
                {
                    Logit.WriteLine("Short book header for " + currentBookAbbrev + " is not short: " + currentBookHeader);
                    if ((currentVernacularAbbreviation.Length > 1) && (currentVernacularAbbreviation.Length < runningHeader.Length))
                    {
                        runningHeader = currentVernacularAbbreviation;
                        Logit.WriteLine("Using abbreviation '" + currentVernacularAbbreviation + "' for header.");
                    }
                }
            }
            if (runningHeader.Length > 34)
            {
                Logit.WriteWarning("No abbreviation or short name found for " + currentBookHeader + " (" + currentBookAbbrev + ").");
            }
            Utils.EnsureDirectory(texDir);
            CloseHtmlFile();
            if (String.IsNullOrEmpty(fileName))
            {
                currentFileName = Path.Combine(texDir, currentBookAbbrev + "_src.tex");
            }
            else
            {
                currentFileName = Path.Combine(texDir, fileName);
            }
            // TODO: refactor to use htm instead of texFile throughout OR get the htm references out of the core function in Usfx2HtmlConverter.cs
            htm = texFile = new StreamWriter(currentFileName, false, Encoding.UTF8);
            
            if ((projectOptions.textDir == "rtl") && (currentBookAbbrev != "CPR"))
                texFile.WriteLine("\\begin{0}RTL{1}", LEFTBRACE, RIGHTBRACE);
            if (mainScriptureFile || (currentBookAbbrev == "GLO"))
                texFile.WriteLine("\\NormalFont\\ShortTitle{0}{1}{2}", LEFTBRACE, runningHeader, RIGHTBRACE);
            else if (currentBookAbbrev != "CPR")
                texFile.WriteLine("\\PeriphTitle{0}{1}{2}", LEFTBRACE, runningHeader, RIGHTBRACE);
        }


        /// <summary>
        /// Finish up the styles and close XeTeX file
        /// </summary>
        protected override void CloseHtmlFile()
        {
            if (texFile != null)
            {
                EndHtmlNote();
                EndHtmlTextStyle();
                // EndChapter();
                EndHtmlParagraph();
                // RepeatNavButtons();
                // WriteHtmlFootnotes();
                if ((projectOptions.textDir == "rtl") && (currentBookAbbrev != "CPR"))
                    texFile.WriteLine("\\end{0}RTL{1}", LEFTBRACE, RIGHTBRACE);
                texFile.Close();
                htm = texFile = null;
                previousFileName = currentFileName;
                noteNumber = 0;
            }
            chopChapter = false;
        }


        protected bool inGreek = false;
        protected bool inHebrew = false;

        protected string DetectGreekHebrew(string line)
        {
            StringBuilder sb = new StringBuilder();
            int i;
            for (i = 0; i < line.Length; i++)
            {
                if (((line[i] >= '\u0590') && (line[i] <= '\u05FF')) || ((line[i] >= '\uFB1D') && (line[i] <= '\uFB4F')))
                {   // Hebrew character
                    if (inGreek)
                    {
                        inGreek = false;
                        sb.Append('}');
                    }
                    if (!inHebrew)
                    {
                        inHebrew = true;
                        sb.Append(@"{\HEB ");
                    }
                }
                else if (((line[i] >= '\u0370') && (line[i] <= '\u03FF')) || ((line[i] >= '\u1F00') && (line[i] <= '\u1FFF')))
                {   // Greek
                    if (inHebrew)
                    {
                        inHebrew = false;
                        sb.Append(RIGHTBRACE);
                    }
                    if (!inGreek)
                    {
                        inGreek = true;
                        sb.Append(@"{\GRC ");
                    }
                }
                else if ((line[i] < '\u0300') || (line[i] > '\u036F'))
                {   // Not a combining diacritic mark
                    if (inGreek)
                    {
                        inGreek = false;
                        sb.Append('}');
                    }
                    if (inHebrew)
                    {
                        inHebrew = false;
                        sb.Append(RIGHTBRACE);
                    }
                }
                sb.Append(line[i]);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Write XeTeX text, escaping TeX special characters
        /// </summary>
        /// <param name="text">text to write</param>
        protected override void WriteHtmlText(string text)
        {
            text = text.Replace(@"\", @"$\backslash$").Replace("$", @"\$").Replace("#", @"\#").Replace("%", @"\%")
                .Replace("&", @"\&").Replace("_", @"\_").Replace(LEFTBRACE, @"$\{$").Replace("’”", "’\u00A0”")
                .Replace("^", "\u2303")    // Replace circumflex with look-alike up arrowhead to avoid TeX math command behavior and old style diacritic behavior
                .Replace(RIGHTBRACE, @"$\}$").Replace("”’", "”\u00A0’").Replace("‘“", "‘\u00A0“").Replace("“‘", "“\u00A0‘").Replace("ʻ", "{ʻ}");
            if (!ignore)
            {
                if (eatSpace)
                {
                    text = text.TrimStart();
                    eatSpace = false;
                }
                WriteHtml(text);    // DetectGreekHebrew(text));
            }
        }

        /// <summary>
        /// Converts a USFM or USFX label, like "q2" into a marker usable as a XeTeX identifier, like @"\QB".
        /// We convert to upper case to reduce collisions with XeLaTeX commands, which are normally lower case.
        /// </summary>
        /// <param name="sfm">USFM or USFX marker to convert</param>
        /// <returns>XeTeX marker</returns>
        protected string Usfm2Tex(string sfm)
        {
            if (sfm == "p") // avoid collision with XeLaTeX macro
                return "\\PP";
            if (sfm == "s") // avoid collision with TeX macro for section symbol
                return "\\SH";
            if (sfm == "s2") // avoid collision with TeX macro for section symbol
                return "\\SHB";
            if (sfm == "s3") // avoid collision with TeX macro for section symbol
                return "\\SHC";
            if (sfm == "s4") // avoid collision with TeX macro for section symbol
                return "\\SHD";
            if (sfm == "qc")    // Usual pattern breaks down with this marker, which visually is the same as \pc, anyway.
                return "\\PC";
            if (sfm == "m")
                return "\\MM";
            if (sfm == "b")
                return "\\BB";
            if (sfm == "wj")
            {
                eatSpace = true;
                return "\\WJ";
            }
            return "\\" + sfm.ToUpperInvariant().Replace("1", "").Replace("2", "B").Replace("3", "C").Replace("4", "D");
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
            if (texFile == null)
                OpenHtmlFile();
            if (preVerse.Length > 0)
            {
                texFile.Write(preVerse.ToString());
                preVerse.Length = 0;
            }
            if (verseNumber == 1)
            {
                WriteHtml("\\VerseOne{0}{1}{2}", LEFTBRACE, currentVersePublished, RIGHTBRACE);
            }
            else
            {
                WriteHtml("\\VS{0}{1}{2}", LEFTBRACE, currentVersePublished, RIGHTBRACE);
            }
            eatSpace = true;
            if (doXrefMerge)
            {
                string xn = xref.find(currentBCV);
                if (!String.IsNullOrEmpty(xn))
                {
                    StartHtmlNote("x", "-");
                    WriteHtmlText(xn);
                    EndHtmlNote();
                }
            }
        }

        /// <summary>
        /// End a verse.
        /// </summary>
        protected override void EndVerse()
        {
            if ((preVerse.Length > 0) && (texFile != null))
            {
                texFile.Write(preVerse.ToString());
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
                if (br.IsPresent && ((br.testament == "o") || (br.testament == "n") || (br.testament == "a")))
                {
                   startHere = String.Format("{0}.tex", br.tla);
                }
                bookIndex++;
            }
            return startHere;
        }

        /// <summary>
        /// Start a TeX note.
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
            marker = EscapeTex(marker);
            if (string.Compare(marker, "-") == 0)
            {
                marker = "";   // No footnote marker; just put the footnote at the bottom of the page.
            }
            else if (string.Compare(marker, "+") == 0)
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
            if ((style == "f") || (style == "ef"))
            {
                WriteHtml("\\FTNT{0}{1}{2}{0}", LEFTBRACE, marker, RIGHTBRACE);
                // Numeric chapter and verse numbers are used in internal references instead of the text versions, which may
                // include dashes in verse bridges and might be in alternate number systems.
                if (!String.IsNullOrEmpty(noteOriginFormat))
                {
                    WriteHtml("{{\\FR {0} }}", noteOriginFormat.Replace("%c", currentChapterPublished).Replace("%v", currentVersePublished));
                }
            }
            else
            {
                WriteHtml("\\XREF{0}{1}{2}{0}", LEFTBRACE, marker, RIGHTBRACE);
                // Numeric chapter and verse numbers are used in internal references instead of the text versions, which may
                // include dashes in verse bridges and might be in alternate number systems.
                if (!String.IsNullOrEmpty(noteOriginFormat))
                {
                    WriteHtml("{{\\XO {0} }}", noteOriginFormat.Replace("%c", currentChapterPublished).Replace("%v", currentVersePublished));
                }
            }
        }


        /// <summary>
        /// Finish writing an HTML note
        /// </summary>
        protected override void EndHtmlNote()
        {
            if (inFootnote)
            {
                EndHtmlNoteStyle();
                WriteHtml(RIGHTBRACE);
                inFootnote = false;
            }
        }

        protected bool rtl = false;

        /// <summary>
        /// Start a TeX paragraph with the specified style and initial text.
        /// If the paragraph is noncanonical introductory material, it should
        /// be marked as isPreverse to indicate that it should be in the file
        /// with the next verse, not the previous one.
        /// </summary>
        /// <param name="style">Paragraph style name to use for CSS class</param>
        /// <param name="isPreverse">true iff this paragraph style is non canonical,
        /// like section titles, book introductions, and such</param>
        protected override void StartHtmlParagraph(string style, bool isPreverse)
        {
            rtl = projectOptions.textDir == "rtl";
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
                    WriteHtml("\\PsalmChap{0}{1}{2}", LEFTBRACE, currentChapterPublished, RIGHTBRACE);
                }
                else if (style.StartsWith("q"))
                {
                    WriteHtml("\\PoetryChap{0}{1}{2}", LEFTBRACE, currentChapterPublished, RIGHTBRACE);
                }
                else if (bookRecord.numChapters == 1)
                {
                    WriteHtml("\\OneChap ");
                }
                else if (chapterNumber == 1)
                {
                    WriteHtml("\\ChapOne{0}{1}{2}", LEFTBRACE, currentChapterPublished, RIGHTBRACE);
                }
                else
                {
                    WriteHtml("\\Chap{0}{1}{2}", LEFTBRACE, currentChapterPublished, RIGHTBRACE);
                }

                newChapterMarkNeeded = newChapterFound = false;
            }
            if (style != "nb")
            {
                WriteHtml(LEFTBRACE);
                WriteHtml("{0} ", Usfm2Tex(style));
            }
        }


        /// <summary>
        /// Finish up a TeX paragraph.
        /// </summary>
        protected override void EndHtmlParagraph()
        {
            if (inParagraph)
            {
                WriteHtml("\\par ");
                WriteHtml(RIGHTBRACE);
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

 
        protected ArrayList FindThis = null;
        protected ArrayList ReplaceWith = null;

        /// <summary>
        /// Converts a limited subset of xhtml to XeTeX
        /// </summary>
        /// <param name="s">xhtml fragment to convert</param>
        /// <returns>XeTeX fragment</returns>
        public string Html2Tex(string s)
        {
            string line;
            char sep;
            int i;
            try
            {
                FindThis = new ArrayList();
                ReplaceWith = new ArrayList();
                string regexFileName = SFConverter.FindAuxFile("htmltotex.re");
                // Logit.WriteLine(regexFileName);
                StreamReader sr = new StreamReader(regexFileName);
                while (!sr.EndOfStream)
                {
                    line = sr.ReadLine().Trim();
                    if (line.Length > 3)
                    {
                        sep = line[0];
                        string[] parts = line.Split(new char[] { sep });
                        FindThis.Add(parts[1]); // parts[0] is the empty string before the first delimiter
                        ReplaceWith.Add(Regex.Unescape(parts[2]));
                    }
                }
                sr.Close();
                s = EscapeTex(s);
                for (i=0; i < FindThis.Count; i++)
                {
                    s = Regex.Replace(s, (string)FindThis[i], (string)ReplaceWith[i]);
                }
                // s = DoBiDi(s); Not needed!
                s = s.Replace("</p>", "\\PAR ");
                s = s.Replace("<strong>", @"{\bf ");
                s = s.Replace("</strong>", @"}");
                if (s.Contains("<") || s.Contains(">"))
                    Logit.WriteError("HTML to TeX conversion error: "+s);
                //s = s.Replace("<", " ").Replace(">", " ");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error converting from xhtml fragment to XeTeX fragment");
                Console.WriteLine(ex.Message);
            }
            return s;

        }

        /******
        public string copyrightPermissionsStatement()
        {
            string fontClass = projectOptions.fontFamily.ToLower().Replace(' ', '_');
            if (projectOptions.customPermissions)
                return expandPercentEscapes(projectOptions.licenseHtml);
            StringBuilder copr = new StringBuilder();
            copr.Append(String.Format("<h1 class='{2}'>{0}</h1>\n<h2>{1}</h2>\n",
                usfxToHtmlConverter.EscapeHtml(projectOptions.vernacularTitle),
                usfxToHtmlConverter.EscapeHtml(projectOptions.EnglishDescription), fontClass));
            if (!String.IsNullOrEmpty(projectOptions.lwcDescription))
                copr.Append(String.Format("<h2>{0}</h2>\n", usfxToHtmlConverter.EscapeHtml(projectOptions.lwcDescription)));
            copr.Append(String.Format("<p>{0}<br />\n", copyrightLink));
            if (!String.IsNullOrEmpty(projectOptions.rightsStatement))
                copr.Append(String.Format("{0}<br />\n", projectOptions.rightsStatement));
            copr.Append(String.Format("Language: <a href='http://www.ethnologue.org/language/{0}' class='{2}' target='_blank'>{1}",
                projectOptions.languageId, projectOptions.languageName, fontClass));
            if (projectOptions.languageName != projectOptions.languageNameInEnglish)
                copr.Append(String.Format(" ({0})", usfxToHtmlConverter.EscapeHtml(projectOptions.languageNameInEnglish)));
            copr.Append("</a><br />\n");
            if (!String.IsNullOrEmpty(projectOptions.dialect))
                copr.Append(String.Format("Dialect: {0}<br />", usfxToHtmlConverter.EscapeHtml(projectOptions.dialect)));
            if (!String.IsNullOrEmpty(projectOptions.contentCreator))
                copr.Append(String.Format("Translation by: {0}<br />\n", usfxToHtmlConverter.EscapeHtml(projectOptions.contentCreator)));
            if ((!String.IsNullOrEmpty(projectOptions.contributor)) && (projectOptions.contentCreator != projectOptions.contributor))
                copr.Append(String.Format("Contributor: {0}<br />\n", usfxToHtmlConverter.EscapeHtml(projectOptions.contributor)));
            copr.Append("</p>\n");
            if (!String.IsNullOrEmpty(projectOptions.promoHtml))
                copr.Append(projectOptions.promoHtml);
            if (projectOptions.creativeCommons)
            {
                copr.Append(@"<p>This translation is made available to you under the terms of the
<a href='http://creativecommons.org/licenses/by-nc-nd/4.0/'>Creative Commons Attribution-noncommercial-no derivatives license.</a> 
You have permission to port the text to different file formats, as long as you don't change any of the text or punctuation of
the Bible.</p>
<p>You may share, copy, distribute, transmit, and extract portions or quotations from this work, provided that:</p>
<ul>
<li>You include the above copyright information.</li>
<li>You do not sell this work for a profit.</li>
<li>You do not make any derivative works that change any of the actual words or punctuation of the Scriptures.</li>
</ul>
<p>Pictures included with Scriptures and other documents on this site are licensed just for use with those Scriptures and documents.
For other uses, please contact the respective copyright owners.</p>
");
            }
            copr.Append(String.Format("<p>{0}</p>\n", projectOptions.contentUpdateDate.ToString("yyyy-MM-dd")));
            return copr.ToString();
        }
        ************/


        /// <summary>
        /// Write copyright and license information in CPR.tex
        /// </summary>
        /// <param name="chapFormat">Not used- any string</param>
        /// <param name="licenseHtml">Text of the copyright and license information</param>
        /// <param name="goText">Link text for going to book list</param>
        protected override void WriteCopyrightPage(string chapFormat, string licenseHtml, string goText)
        {
            /*
            if (projectOptions.isbn13.Length > 13)
            {
                licenseHtml = licenseHtml + "<p><br/>ISBN " + projectOptions.isbn13 + "</p>";
            }
            */
            licenseHtml = Html2Tex(licenseHtml);
            // Copyright page
            currentBookAbbrev = "CPR";
            bookListIndex = -1;
            currentBookHeader = EscapeTex(shortCopr);
            OpenHtmlFile("CPR.tex");
            bookListIndex = 0;
            bookRecord = (BibleBookRecord)bookList[0];
            texFile.WriteLine(@"\PeriphTitle{ }");
            texFile.Write(licenseHtml);
            texFile.Write("\n\\par\\vskip 1ex\\hrule\\vskip 0.5ex\\par {0}\\TINY ", LEFTBRACE);
            texFile.WriteLine(String.Format("{0} {1}{2}", indexDateStamp, epubIdentifier, RIGHTBRACE));
            texFile.WriteLine(@"\vfill\eject");
            CloseHtmlFile();

        }



        /// <summary>
        /// Append a line to the Table of Contents
        /// </summary>
        /// <param name="level">Table of contents line class, i.e. toc1, toc2, toc3...</param>
        /// <param name="title">Title to add</param>
        protected override void AppendToc(string level, string title)
        {
            /* UNDER CONSTRUCTION
            bookRecord.toc.Append(String.Format("<div class=\"{0}\"><a href=\"{1}.xhtml#{2}{3}_{4}\">{5}</a></div>" + Environment.NewLine,
                level,
                currentBookAbbrev,
                bookInfo.getShortCode(currentBookAbbrev),
                Math.Max(1, chapterNumber).ToString(),
                verseNumber.ToString(),
                title));
             */
            firstTitle = false;
        }


        /// <summary>
        /// Add chapter to table of contents
        /// </summary>
        protected override void ChapterToc()
        {
            newChapterFound = false;
        }

        protected char normalJustification = 'l';
        protected char endJustification = 'r';
        protected bool needAmpersand = false;

        /// <summary>
        /// Start an HTML table
        /// </summary>
        protected override void StartHtmlTable()
        {
            EndHtmlParagraph();
            if (projectOptions.textDir == "rtl")
            {
                normalJustification = 'r';
                endJustification = 'l';
            }
            else if (projectOptions.textDir == "ltr")
            {
                normalJustification = 'l';
                endJustification = 'r';
            }
            else
            {
                normalJustification = 'c';
                endJustification = 'r';
            }
            needAmpersand = false;
            if (!inTable)
            {
                WriteHtml(@"\begin{center}" + Environment.NewLine);
                WriteHtml(@"\begin{tabular}{|");
                tableFirstRow = new StringBuilder();
                tableDeclaration = new StringBuilder();
                inTable = true;
                inFirstTableRow = true;
            }
        }


        protected override void StartHeaderColumn()
        {
            if (inFirstTableRow)
            {
                tableDeclaration.Append(normalJustification);
                tableDeclaration.Append("|");
            }
            if (needAmpersand)
                WriteHtml(@" & ");
            WriteHtml(@"\BDB ");
            inTableCol = true;
            inTableBold = true;
        }

        protected override void StartHeaderColumnRight()
        {
            if (inFirstTableRow)
            {
                tableDeclaration.Append(endJustification);
                tableDeclaration.Append("|");
            }
            if (needAmpersand)
                WriteHtml(" & ");
            WriteHtml(@"\BDB ");
            inTableBold = true;
            inTableCol = true;
        }

        protected override void StartTableRow()
        {
            needAmpersand = false;
            inTableRow = true;
            WriteHtml(@"\BDE ");
        }

        protected override void StartCenteredColumn()
        {
            if (inFirstTableRow)
            {
                tableDeclaration.Append(normalJustification);
                tableDeclaration.Append("|");
            }
            if (needAmpersand)
                WriteHtml(@" & \BDE ");
            inTableCol = true;
        }

        protected override void StartRightJustifiedColumn()
        {
            if (inFirstTableRow)
            {
                tableDeclaration.Append(endJustification);
                tableDeclaration.Append("|");
            }
            if (needAmpersand)
                WriteHtml(@" & \SetFont ");
            inTableCol = true;
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
            string destFigFile = Path.Combine(texDir, figFileName);
            if (figFileName.Length > 4)
            {
                if (!File.Exists(destFigFile))
                    File.Copy(Path.Combine(htmlextrasDir, figFileName), destFigFile);
                WriteHtml(String.Format("\\par\\vskip 1ex{0}\\goodbreak\\CAPT\\resizebox{0}\\columnwidth{2}{0}!{2}{0}\\includegraphics{0}{1}{2}{2}{0}\\par",
                    LEFTBRACE, figFileName, RIGHTBRACE));
                if (!String.IsNullOrEmpty(figCopyright))
                    WriteHtml(String.Format("\\nobreak\\CAPTCOPR {0}\\par", figCopyright));
                if (!String.IsNullOrEmpty(figCaption))
                    WriteHtml(String.Format("\\nobreak{2}{0}\\CAPT {4}{2}{0} \\CAPTREF {5}\\par\\goodbreak{2}{2}",
                    LEFTBRACE, figFileName, RIGHTBRACE, figCopyright, figCaption, figReference));
            }
        }


        /// <summary>
        /// End an HTML table column
        /// </summary>
        protected override void EndHtmlTableCol()
        {
            if (inTableBold)
            {
                WriteHtml(@"\BDE ");
                inTableBold = false;
            }
            if (inTableCol)
            {
                needAmpersand = true;
                inTableCol = false;
            }
        }

        /// <summary>
        /// End and HTML table row
        /// </summary>
        protected override void EndHtmlTableRow()
        {
            EndHtmlTableCol();
            if (inTableRow)
            {
                if (inFirstTableRow)
                {
                    inFirstTableRow = false;
                    WriteHtml(tableDeclaration.ToString() + RIGHTBRACE + Environment.NewLine);
                    WriteHtml("\\hline\n");
                    WriteHtml(tableFirstRow.ToString());
                    tableFirstRow.Length = 0;
                    tableDeclaration.Length = 0;
                }
                WriteHtml("\\\\\n\\hline\n");
                inTableRow = false;
            }
        }

        /// <summary>
        /// End and HTML table
        /// </summary>
        protected override void EndHtmlTable()
        {
            EndHtmlTableRow();
            if (inTable)
            {
                WriteHtml("\\end{0}tabular{1}\n", LEFTBRACE, RIGHTBRACE);
                WriteHtml("\\end{0}center{1}\n", LEFTBRACE, RIGHTBRACE);
                inTable = false;
            }
        }


        /// <summary>
        /// Find an input file in the input project directory, input directory, or program directory
        /// </summary>
        /// <param name="fileName">full path to the found file</param>
        protected string FindInputFile(string fileName)
        {
            string result = Path.Combine(projectInputDir, fileName);
            if (File.Exists(result))
                return result;
            result = Path.Combine(inputDir, fileName);
            if (File.Exists(result))
                return result;
            return SFConverter.FindAuxFile(fileName);
        }


        protected void WritePDFTemplate()
        {
            StreamWriter webIndex;
            bool hasNT = false;
            bool hasPsalms = false;
            try
            {
                foreach (BibleBookRecord br in bookInfo.publishArray)
                {
                    if ((br != null) && br.IsPresent)
                    {
                        if (br.testament == "n")
                            hasNT = true;
                        else if (br.tla == "PSA")
                            hasPsalms = true;
                    }
                }
                webIndex = new StreamWriter(Path.Combine(texDir, "indextemplate.txt"));
                webIndex.WriteLine("letter size 12 point color");
                webIndex.WriteLine(projectOptions.translationId + "_all");
                webIndex.WriteLine("A4 size 12 point color");
                webIndex.WriteLine(projectOptions.translationId + "_a4");
                webIndex.WriteLine("202 x 135 mm 9 point color");
                webIndex.WriteLine(projectOptions.translationId + "_prt");
                webIndex.WriteLine("6 in x 9 in 8 point monochrome");
                webIndex.WriteLine(projectOptions.translationId + "_book");
                if (hasNT)
                {
                    webIndex.WriteLine("New Testament 197 x 118 mm 10 point monochrome");
                    webIndex.WriteLine(projectOptions.translationId + "_nt");
                    if (hasPsalms)
                    {
                        webIndex.WriteLine("New Testament and Psalms 6 x 9 in 10 point monochrome");
                        webIndex.WriteLine(projectOptions.translationId + "_ntp");
                    }
                }
                foreach (BibleBookRecord br in bookInfo.publishArray)
                {
                    if ((br != null) && br.IsPresent)
                    {
                        webIndex.WriteLine(br.vernacularShortName);
                        webIndex.WriteLine(projectOptions.translationId + "_" + br.tla);
                    }
                }
                webIndex.Close();
            }
            catch (Exception ex)
            {
                Logit.WriteError("Error writing template for HTML index of PDF files:");
                Logit.WriteError(ex.Message);
            }
        }

        /// <summary>
        /// Write an (X)HTML line break element
        /// </summary>
        protected override void WriteHtmlLineBreak()
        {
            WriteHtml("\\par ");
        }




        /// <summary>
        /// This override version calls XeTeX to create PDF files
        /// </summary>
        protected override void ZipResults()
        {
            // string bookFileName;
            fileHelper.CopyFile(FindInputFile("haiola.tex"), Path.Combine(texDir, "haiola.tex"));
            //TODO: move the following hard-coded dimensions and options to the user interface
            if (projectOptions.textDir == "rtl")
            {
                WriteXeTeXHeader(Path.Combine(texDir, "12ptrtl.tex"), "11in", "8.5in", "1in", "0.75in", "0.75in", "0.75in", "11.0pt", "12pt", 12.0, 1, true, true);
                WriteXeTeXHeader(Path.Combine(texDir, "12pta4rtl.tex"), "297mm", "210mm", "30mm", "30mm", "25mm", "25mm", "11.0pt", "12pt", 12.0, 1, true, true);
                WriteXeTeXHeader(Path.Combine(texDir, "12pta5rtl.tex"), "210mm", "148mm", "25mm", "25mm", "25mm", "25mm", "11.0pt", "12pt", 12.0, 1, true, true);
                WriteXeTeXHeader(Path.Combine(texDir, "printrtl.tex"), "202mm", "135mm", "12mm", "11mm", "10mm", "10mm", "11.0pt", "6pt", 9.0, 1, true, true);
                WriteXeTeXHeader(Path.Combine(texDir, "bookrtl.tex"), "9in", "6in", "0.85in", "0.5in", "0.5in", "0.5in", "11.0pt", "6pt", 8.0, 1, true, false);
                WriteXeTeXHeader(Path.Combine(texDir, "ntrtl.tex"), "197mm", "118mm", "9mm", "9mm", "6.5mm", "6.5mm", "11.0pt", "6pt", 10.0, 1, false, false);
                WriteXeTeXHeader(Path.Combine(texDir, "ntprtl.tex"), "9in", "6in", "0.85in", "0.5in", "0.5in", "0.5in", "11.0pt", "6pt", 8.0, 1, false, false);
            }
            else
            {
                WriteXeTeXHeader(Path.Combine(texDir, "12pt.tex"), "11in", "8.5in", "1in", "0.75in", "0.75in", "0.75in", "11.0pt", "12pt", 12.0, 2, false, true);
                WriteXeTeXHeader(Path.Combine(texDir, "12pta4.tex"), "297mm", "210mm", "30mm", "30mm", "25mm", "25mm", "11.0pt", "12pt", 12.0, 2, false, true);
                WriteXeTeXHeader(Path.Combine(texDir, "12pta5.tex"), "210mm", "148mm", "25mm", "25mm", "25mm", "25mm", "11.0pt", "12pt", 12.0, 1, false, true);
                WriteXeTeXHeader(Path.Combine(texDir, "print.tex"), "202mm", "135mm", "12mm", "11mm", "10mm", "10mm", "11.0pt", "8pt", 9.0, 1, false, true);
                WriteXeTeXHeader(Path.Combine(texDir, "book.tex"), "9in", "6in", "0.85in", "0.5in", "0.5in", "0.5in", "11.0pt", "6pt", 8.0, 2, false, false);
                WriteXeTeXHeader(Path.Combine(texDir, "nt.tex"), "197mm", "118mm", "9mm", "9mm", "6.5mm", "6.5mm", "11.0pt", "6pt", 10.0, 2, false, false);
                WriteXeTeXHeader(Path.Combine(texDir, "ntp.tex"), "9in", "6in", "0.85in", "0.5in", "0.5in", "0.5in", "11.0pt", "6pt", 10.0, 2, false, false);
            }
            fileHelper.CopyFile(FindInputFile("footkmpj.sty"), Path.Combine(texDir, "footkmpj.sty"));

            /*
            if (callXetex)
            {
                foreach (BibleBookRecord br in bookInfo.publishArray)
                {
                    if ((br != null) && br.IsPresent)
                    {
                        bookFileName = Path.Combine(texDir, projectOptions.translationId + "_" + br.tla + ".tex");
                        RunXeTeX(bookFileName);
                    }
                }
                WritePDFIndex();
            }
            else
            {
            */
                WritePDFTemplate();
            /*
            }
            */
        }

    
        /// <summary>
        /// Write navigational links to get to another book or another chapter from here. Also include links to site home, previous chapter, and next chapter.
        /// </summary>
        protected override void WriteNavButtons()
        {
        }

        /// <summary>
        /// Writes out a XeTeX header file for the given page geometry, font size, and text direction.
        /// </summary>
        /// <param name="texHeaderName">Full path and name of the file to write</param>
        /// <param name="paperHeight">Paper height in TeX dimensions, like 11in or 297mm</param>
        /// <param name="paperWidth">Paper width in Tex dimensions, like 8.5in or 210mm</param>
        /// <param name="innerMargin">Inner page margin (next to the binding) in TeX dimensions, 25mm</param>
        /// <param name="outerMargin">Outer page margin in TeX dimensions, like 20mm</param>
        /// <param name="topMargin">Top page margin in TeX dimensions</param>
        /// <param name="bottomMargin">Bottom page margin in TeX dimensions</param>
        /// <param name="columnSep">Column separation in TeX dimensions</param>
        /// <param name="headSep">Head separation in TeX dimensions</param>
        /// <param name="pointSize">Base point size for the main text, normally between 8 and 12, inclusive</param>
        /// <param name="columns">Number of columns of text (1 or 2)</param>
        /// <param name="bidi">True iff the text contains right-to-left writing of more than one word at a time</param>
        /// <param name="useColor">True iff peripheral words should be blue and words of Jesus Christ red</param>
        protected void WriteXeTeXHeader(string texHeaderName, string paperHeight, string paperWidth, string innerMargin, string outerMargin,
            string topMargin, string bottomMargin, string columnSep, string headSep, double pointSize, int columns, bool bidi, bool useColor)
        {
            double headerPointSize = Math.Max(pointSize * 0.6, 8.0);
            string scriptName = projectOptions.script;
            string languageName = scriptName.ToLowerInvariant(); //projectOptions.languageNameInEnglish.ToLowerInvariant().Replace(" ", "");
            switch (languageName)
            {
                case "arabic":
                case "farsi":
                case "hebrew":
                case "syriac":
                    // Let it be.
                    break;
                default:
                    languageName = "arabic";
                    break;
            }
            
            StreamWriter texFile = new StreamWriter(texHeaderName);
            texFile.WriteLine("\\documentclass[paper={0}:{1},{2}pt]{3}book{4}", paperWidth, paperHeight, pointSize.ToString(), LEFTBRACE, RIGHTBRACE);
            if (columns > 1)
                texFile.WriteLine(@"\usepackage{multicol}");
            texFile.WriteLine(@"\usepackage{graphics}");
            texFile.WriteLine(@"\usepackage{calc}");
            //texFile.WriteLine(@"\usepackage{metalogo}");
            if (useColor)
                texFile.WriteLine(@"\usepackage[colorlinks=true,linkcolor=blue]{hyperref}");
            else
                texFile.WriteLine(@"\usepackage[colorlinks=true,linkcolor=black]{hyperref}");
            texFile.WriteLine("\\usepackage[paperheight={0},paperwidth={1},inner={2},outer={3},top={4},bottom={5},includehead=true,columnsep={6},headsep={7}]{8}geometry{9}",
                paperHeight, paperWidth, innerMargin, outerMargin, topMargin, bottomMargin, columnSep, headSep,
                LEFTBRACE, RIGHTBRACE);
            texFile.WriteLine(@"\usepackage[para]{footkmpj}");
            texFile.WriteLine(@"\usepackage{tocloft}");
            texFile.WriteLine(@"\usepackage{ucharclasses}");
            if (bidi)
            {
                texFile.WriteLine(@"\usepackage{bidi}");
            }
            texFile.WriteLine("\\newcommand{0}\\TocFont{1}{0}\\font\\Y=\"\\OtherFontFace:color=000000\" at {2}pt\\Y {1}", LEFTBRACE, RIGHTBRACE, pointSize.ToString());
            texFile.WriteLine(@"\newcommand{\TextColor}{:color=000000}");
            texFile.WriteLine("\\newcommand{0}\\TinyFontSize{1}{0}{2}pt{1}", LEFTBRACE, RIGHTBRACE, Math.Max(6.8, pointSize * 2.0 / 3.0));
            if (useColor)
            {
                texFile.WriteLine("\\newcommand{0}\\FnMarkFont{1}{0}\\font\\Z=\"FreeSerif:color=000080\" at {2}pt \\Z {1}", LEFTBRACE, RIGHTBRACE, pointSize * 0.75);
                texFile.WriteLine(@"\newcommand{\IntroColor}{:color=000080}");
                texFile.WriteLine(@"\newcommand{\WJColor}{:color=ff0000}");
                texFile.WriteLine(@"\newcommand{\BlueColor}{:color=0000ff}");
                texFile.WriteLine(@"\newcommand{\GreenColor}{:color=00ff00}");
                texFile.WriteLine(@"\newcommand{\YellowColor}{:color=808000}");
                texFile.WriteLine(@"\newcommand{\WJ}{\Red}");
            }
            else
            {
                texFile.WriteLine("\\newcommand{0}\\FnMarkFont{1}{0}\\font\\Z=\"FreeSerif:color=000000\" at {2}pt \\Z {1}", LEFTBRACE, RIGHTBRACE, pointSize * 0.75);
                texFile.WriteLine(@"\newcommand{\IntroColor}{:color=000000}");
                texFile.WriteLine(@"\newcommand{\WJColor}{:color=000000}");
                texFile.WriteLine(@"\newcommand{\BlueColor}{:color=000000}");
                texFile.WriteLine(@"\newcommand{\GreenColor}{:color=000000}");
                texFile.WriteLine(@"\newcommand{\YellowColor}{:color=000000}");
                texFile.WriteLine(@"\newcommand{\WJ}{\BDB}");
            }
            texFile.WriteLine("\\newcommand{0}\\RaiseVerse{1}{0}{2}pt{1}", LEFTBRACE, RIGHTBRACE, pointSize/4.0);
            texFile.WriteLine("\\newcommand{0}\\FnSize{1}{0}{2}pt{1}", LEFTBRACE, RIGHTBRACE, pointSize*0.75);
            texFile.WriteLine("\\newcommand{0}\\TextSize{1}{0}{2}pt{1}", LEFTBRACE, RIGHTBRACE, pointSize);
            texFile.WriteLine("\\newcommand{0}\\MTSize{1}{0}{2}pt{1}", LEFTBRACE, RIGHTBRACE, pointSize*1.5);
            texFile.WriteLine("\\newcommand{0}\\MTBSize{1}{0}{2}pt{1}", LEFTBRACE, RIGHTBRACE, pointSize*1.333);
            texFile.WriteLine("\\newcommand{0}\\MTCSize{1}{0}{2}pt{1}", LEFTBRACE, RIGHTBRACE, pointSize*1.167);
            texFile.WriteLine("\\newcommand{0}\\MTDSize{1}{0}{2}pt{1}", LEFTBRACE, RIGHTBRACE, pointSize*1.083);
            texFile.WriteLine("\\newcommand{0}\\VerseFontSize{1}{0}{2}pt{1}", LEFTBRACE, RIGHTBRACE, pointSize*0.75);
            texFile.Close();
            /* if (callXetex)
            {
                if (!RunXeTeX(texHeaderName))
                    Logit.WriteError("Unable to run XeLaTeX on " + texHeaderName);
            } */
        }



        protected void WriteMasterTexFile(string texFileName, string formattingFileName, int numColumns, string bookSet)
        {
            StreamWriter texFile;
            shortLangId = langCodes.ShortCode(langId);
            bool isRtl = projectOptions.textDir == "rtl";
            if (isRtl)
            {
                formattingFileName = formattingFileName + "rtl";
                numColumns = 1;

            }
            if (projectOptions.longestWordLength > 20)
            {
                numColumns = 1;
            }
            try
            {
                Utils.EnsureDirectory(texDir);
                texFile = new StreamWriter(texFileName);
                texFile.WriteLine("\\XeTeXlinebreaklocale \"{0}\"", shortLangId);
                texFile.WriteLine(@"\XeTeXlinebreakskip = 0pt plus 1pt minus 0.1pt");
                texFile.WriteLine(@"\XeTeXlinebreakpenalty = 10");
                texFile.WriteLine("\\newcommand\\ScriptureFontFace{0}{1}{2}%", LEFTBRACE, preferredFont, RIGHTBRACE);
                texFile.WriteLine("\\newcommand\\OtherFontFace{0}{1}{2}%", LEFTBRACE, projectOptions.headerFooterFont, RIGHTBRACE);
                texFile.WriteLine("\\newcommand\\HeaderFontFace{0}{1}{2}%", LEFTBRACE, projectOptions.headerFooterFont, RIGHTBRACE);
                texFile.Write(@"\input{");
                texFile.Write(formattingFileName);
                texFile.WriteLine(@"}%");
                texFile.WriteLine(@"\input{haiola}%");
                texFile.WriteLine(@"\begin{document}%");
                if (isRtl)
                    texFile.WriteLine(@"\makeatletter\def\@evenhead{{\HDRF\HeaderFont\leftmark\hfil\thepage\hfil\rightmark}}\def\@oddhead{{\HDRF\HeaderFont\leftmark\hfil\thepage\hfil\rightmark}}\makeatother\frontmatter\pagenumbering{roman}%");
                else
                    texFile.WriteLine(@"\makeatletter\def\@evenhead{{\HDRF\HeaderFont\rightmark\hfil\thepage\hfil\leftmark}}\def\@oddhead{{\HDRF\HeaderFont\rightmark\hfil\thepage\hfil\leftmark}}\makeatother\frontmatter\pagenumbering{roman}%");

                string coverFile = Path.Combine(projectInputDir, "insidecover.png");
                string coverDestination = Path.Combine(texDir, "cover.png");
                if (!File.Exists(coverDestination))
                {
                    if (!File.Exists(coverFile))
                        coverFile = Path.Combine(projectInputDir, "cover.png");
                    if (!File.Exists(coverFile))
                        coverFile = Path.Combine(Path.Combine(projectOutputDir, "cover"), "cover.png");
                    if (File.Exists(coverFile))
                    {
                        File.Copy(coverFile, Path.Combine(texDir, "cover.png"));
                    }
                }
                if (File.Exists(coverDestination))
                {
                    texFile.WriteLine(@"\thispagestyle{empty}\markboth{\HeaderFont }{\HeaderFont }%");
                    texFile.WriteLine("\\resizebox{0}!{2}{0}\\textwidth{2}{0}\\includegraphics{0}{1}{2}{2}", LEFTBRACE, "cover.png", RIGHTBRACE);
                    texFile.WriteLine(@"\vfill\eject");
                }

                texFile.WriteLine("\\input{0}CPR{1}", LEFTBRACE, RIGHTBRACE);
                if (numColumns > 1)
                    texFile.WriteLine(@"\begin{multicols}{2}%");
                texFile.WriteLine(@"\tableofcontents\clearpage");
                texFile.WriteLine(@"\setcounter{page}{1}\pagenumbering{arabic}\mainmatter%");
                foreach (BibleBookRecord br in bookInfo.publishArray)
                {
                    if ((br != null) && br.IsPresent)
                    {
                        if ((bookSet == "*") && (br.tla == "TOB") && (numColumns > 1))
                        {
                            texFile.WriteLine(@"\addtocontents{toc}{\protect\thispagestyle{empty}\protect{\vfill\columnbreak DC\par}}");
                        }
                        if ((bookSet == "*") && (br.tla == "MAT") && (projectOptions.otBookCount > 20) && (numColumns > 1))
                        {
                            texFile.WriteLine(@"\addtocontents{toc}{\protect\thispagestyle{empty}\protect{\par NT\par}}");
                        }
                        if (((bookSet == "n") || (bookSet == "p")) && (br.testament == "n"))
                        {
                            texFile.WriteLine("\\input{0}{1}_src{2}", LEFTBRACE, br.tla, RIGHTBRACE);
                        }
                        else if (bookSet == "*")
                        {
                            texFile.WriteLine("\\input{0}{1}_src{2}", LEFTBRACE, br.tla, RIGHTBRACE);
                        }
                    }
                }
                if (bookSet == "p")
                {
                    foreach (BibleBookRecord br in bookInfo.publishArray)
                    {
                        if ((br != null) && (br.tla == "PSA") && br.IsPresent)
                        {
                            texFile.WriteLine("\\input{0}{1}_src{2}", LEFTBRACE, br.tla, RIGHTBRACE);
                        }
                    }

                }
                if (numColumns > 1)
                    texFile.WriteLine(@"\end{multicols}");
                texFile.WriteLine(@"\clearpage");
                texFile.WriteLine(@"\end{document}");
                texFile.Close();
            }
            catch (Exception ex)
            {
                Logit.WriteError("Error writing XeTeX master file "+texFileName);
                Logit.WriteError(ex.Message);
            }

        }


        /// <summary>
        /// Write table of contents file/master document XeTeX files
        /// </summary>
        /// <param name="translationId">translation ID code</param>
        /// <param name="indexHtml">Unused</param>
        /// <param name="goText">Text for link to starting point</param>
        protected override void GenerateIndexFile(string translationId, string indexHtml, string goText)
        {
            string bookFileName;
            StreamWriter texFile;
            bool isRtl = projectOptions.textDir == "rtl";
            shortLangId = langCodes.ShortCode(langId);

            WriteMasterTexFile(Path.Combine(texDir, projectOptions.translationId + "_all.tex"), "12pt", 2, "*");
            WriteMasterTexFile(Path.Combine(texDir, projectOptions.translationId + "_prt.tex"), "print", 1, "*");
            WriteMasterTexFile(Path.Combine(texDir, projectOptions.translationId + "_book.tex"), "book", 2, "*");
            WriteMasterTexFile(Path.Combine(texDir, projectOptions.translationId + "_a4.tex"), "12pta4", 2, "*");
            WriteMasterTexFile(Path.Combine(texDir, projectOptions.translationId + "_nt.tex"), "nt", 2, "n");
            WriteMasterTexFile(Path.Combine(texDir, projectOptions.translationId + "_ntp.tex"), "ntp", 2, "p");
            try
            {
                // One PDF file per book
                foreach (BibleBookRecord br in bookInfo.publishArray)
                {
                    if ((br != null) && br.IsPresent)
                    {
                        bookFileName = Path.Combine(texDir, projectOptions.translationId + "_" + br.tla + ".tex");
                        texFile = new StreamWriter(bookFileName);
                        texFile.WriteLine("\\XeTeXlinebreaklocale \"{0}\"", shortLangId);
                        texFile.WriteLine(@"\XeTeXlinebreakskip = 0pt plus 1pt minus 0.1pt");
                        texFile.WriteLine(@"\XeTeXlinebreakpenalty = 10");
                        /*
                        if (projectOptions.script == "Latin")
                        {
                        */
                            texFile.WriteLine("\\newcommand\\ScriptureFontFace{0}{1}{2}%", LEFTBRACE, preferredFont, RIGHTBRACE);
                            texFile.WriteLine("\\newcommand\\OtherFontFace{0}{1}{2}%", LEFTBRACE, projectOptions.headerFooterFont, RIGHTBRACE);
                            texFile.WriteLine("\\newcommand\\HeaderFontFace{0}{1}{2}%", LEFTBRACE, projectOptions.headerFooterFont, RIGHTBRACE);
                        /*
                        }
                        else
                        {
                            texFile.WriteLine("\\newcommand\\HeaderFontFace{0}{1}{2}%", LEFTBRACE, "Noto Sans", RIGHTBRACE);
                            texFile.WriteLine("\\newcommand\\ScriptureFontFace{0}{1}{2}%", LEFTBRACE, "Noto Serif", RIGHTBRACE);
                            texFile.WriteLine("\\newcommand\\OtherFontFace{0}{1}{2}%", LEFTBRACE, "Noto Sans", RIGHTBRACE);
                        }
                        */
                        if (isRtl)
                        {
                            texFile.WriteLine(@"\input{12pta5rtl}");
                        }
                        else
                        {
                            texFile.WriteLine(@"\input{12pta5}");
                        }
                        texFile.WriteLine(@"\input{haiola}");
                        //texFile.WriteLine("\\newfontfamily\\{0}font[Script={1}]{2}{3}{4}", projectOptions.languageNameInEnglish.ToLowerInvariant(), projectOptions.script, LEFTBRACE, projectOptions.fontFamily, RIGHTBRACE);
                        texFile.WriteLine(@"\begin{document}");
                        if (isRtl)
                            texFile.WriteLine(@"\makeatletter\def\@evenhead{{\HDRF\HeaderFont\leftmark\hfil\thepage\hfil\rightmark}}\def\@oddhead{{\HDRF\HeaderFont\leftmark\hfil\thepage\hfil\rightmark}}\makeatother\frontmatter\pagenumbering{roman}%");
                        else
                            texFile.WriteLine(@"\makeatletter\def\@evenhead{{\HDRF\HeaderFont\rightmark\hfil\thepage\hfil\leftmark}}\def\@oddhead{{\HDRF\HeaderFont\rightmark\hfil\thepage\hfil\leftmark}}\makeatother\frontmatter\pagenumbering{roman}%");
                        texFile.WriteLine("\\input{0}{1}_src{2}", LEFTBRACE, br.tla, RIGHTBRACE);
                        texFile.WriteLine(@"\vfill\eject");
                        texFile.WriteLine("\\input{0}CPR{1}", LEFTBRACE, RIGHTBRACE);
                        texFile.WriteLine(@"\clearpage");
                        texFile.WriteLine(@"\end{document}");
                        texFile.Close();
                    }
                }

            }
            catch (Exception ex)
            {
                Logit.WriteError("Error writing XeTeX master files:");
                Logit.WriteError(ex.Message);
            }

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
        /// Escape TeX special characters
        /// </summary>
        /// <param name="s">Plain text</param>
        /// <returns>Text with backslant before ^$%&#_</returns>
        public string EscapeTex(string s)
        {
            return s.Replace("^", "\\^").Replace("$", "\\$").Replace("%", "\\%").Replace("&", "\\&").Replace("#", "\\#").Replace("_", "\\_");
        }

        /// <summary>
        /// Write text to HTML file if open, or queue it up for when an HTML file gets opened.
        /// </summary>
        /// <param name="s"></param>
        protected override void WriteHtml(string s)
        {
            if (String.IsNullOrEmpty(s))
                return;
            // Check for latin text within RTL script and change direction of writing if necessary
            // NOT NEEDED: s = DoBiDi(s);
            
            if (!ignore)
            {
                if (inFirstTableRow)
                    tableFirstRow.Append(s);
                else if (texFile == null)
                    preVerse.Append(s);
                else
                    texFile.Write(s);
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
                if (texFile == null)
                    OpenHtmlFile();
                if (preVerse.Length > 0)
                {
                    texFile.WriteLine(preVerse.ToString());
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
            /*
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
             */
        }

        /// <summary>
        /// End a link started by StartLink.
        /// </summary>
        protected override void EndLink()
        {
            /*
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
             */
        }


       
        /// <summary>
        /// Stub.
        /// </summary>
        protected override void LeaveHeader()
        {
            /*
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
            */
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

        protected override void DisplayPublishedVerse()
        {
            WriteHtml(String.Format("\\VP{0}{1}{2}", LEFTBRACE, currentVersePublished, RIGHTBRACE));
        }

        protected override void DisplayAlternateVerse()
        {
            WriteHtml(String.Format("\\VA{0}{1}{2}", LEFTBRACE, currentVerseAlternate, RIGHTBRACE));
        }


        protected override void StartStrongs(string StrongsNumber, string plural, string morphology = "", string lemma = "")
        {
            inStrongs = true;
        }

        protected override void EndStrongs()
        {
            inStrongs = false;
        }

        /// <summary>
        /// Given a book ID and a chapter identifier string, generate the corresponding HTML file name.
        /// </summary>
        /// <param name="bookId"></param>
        /// <param name="chap"></param>
        /// <returns></returns>
        public override string HtmName(string bookId, string chap)
        {
            return bookId + ".tex";
        }

        protected override void WriteHtmlOptionalLineBreak()
        {
            WriteHtml(@"\newline ");
        }

        protected override void WriteHorizontalRule()
        {
            WriteHtml(@"\hrulefill ");
        }

        protected override void PageBreak()
        {
            WriteHtml(@"\newpage ");
        }


        /// <summary>
        /// Start an HTML text style, which is really a span with the style name as an attribute.
        /// </summary>
        /// <param name="style">name of text style/span class</param>
        protected override void StartHtmlTextStyle(string style)
        {
            if (!ignore)
            {
                inTextStyle++;
                WriteHtml(String.Format("{0}{1}{0}", LEFTBRACE, Usfm2Tex(style)));
            }
        }

        /// <summary>
        /// End a TeX character style group
        /// </summary>
        protected override void EndHtmlTextStyle()
        {
            if (inTextStyle > 0)
            {
                WriteHtml("}}");
                inTextStyle--;
            }
        }






















        /// <summary>
        /// End a run of text with a given style
        /// </summary>
        protected void EndTextStyle()
        {
            if (inTextStyle > 0)
            {
                WriteHtml(RIGHTBRACE);
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
            WriteHtml("{\\tsfm{0} {1}", styleName, text);
            inTextStyle++;
        }

        /// <summary>
        /// End any currently active paragaph
        /// </summary>
        protected void EndParagraph()
        {
            if (inParagraph)
            {
                WriteHtml("\\par }");
                inParagraph = false;
            }
        }

        protected void EndFootnoteStyle()
        {
            if (inFootnoteStyle)
            {
                WriteHtml("}}");
                inFootnoteStyle = false;
            }
        }

        protected void EndFootnote()
        {
            EndFootnoteStyle();
            if (inFootnote)
            {
                WriteHtml("}}");
                inFootnote = false;
            }
        }

    }
}
