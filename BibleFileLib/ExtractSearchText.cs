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
        protected string currentBook;
        protected string currentChapter;
        protected string currentVerse;
        protected string currentPlace;
        protected string osisBook;
        protected string osisVerse;
        protected XmlTextReader usfx;
        protected XmlTextWriter verseFile;
        bool inVerse;
        bool inPsalmTitle;
        bool verseEndedWithSpace;
        BibleBookInfo bookInfo;
        BibleBookRecord bookRecord;
        StringBuilder verseText;

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
                if (verseEndedWithSpace)
                    s = s.TrimStart(null);
                verseFile.WriteString(s);
                verseEndedWithSpace = s.EndsWith(" ");
                verseFile.WriteEndElement();    // v
                inVerse = false;
                verseText.Length = 0;
            }
        }

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
            verseText = new StringBuilder();
            bool result = false;

            try
            {
                usfx = new XmlTextReader(usfxFileName);
                usfx.WhitespaceHandling = WhitespaceHandling.All;
                verseFile = new XmlTextWriter(verseFileName, Encoding.UTF8);
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
                                currentPlace = currentBook;
                                break;
                            case "id":
                                if (id != currentBook)
                                {
                                    Logit.WriteError("Book ID in <id> and <book> do not match; " + currentBook + " is not " + id);
                                }
                                SkipElement();  // Strip out comment portion.
                                break;
                            case "c":
                                EndVerse(); // In case file lacks <ve /> elements.
                                currentChapter = id;
                                currentVerse = String.Empty;
                                currentPlace = currentBook + "." + currentChapter;
                                SkipElement(); // Doesn't skip chapter, just the published chapter number, if present.
                                break;
                            case "v":
                                EndVerse(); // In case file lacks <ve /> elements.
                                inVerse = true;
                                currentVerse = id;
                                currentPlace = currentBook + "." + currentChapter + "." + currentVerse;
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
                                inPsalmTitle = true;
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
                            case "toc":
                            case "h":
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
                                inPsalmTitle = false;
                                break;
                        }
                    }
                    else if ((usfx.NodeType == XmlNodeType.Text) || (usfx.NodeType == XmlNodeType.SignificantWhitespace))
                    {
                        if (inVerse || inPsalmTitle)
                            verseText.Append(usfx.Value);
                    }
                }
                Logit.ShowStatus("writing " + verseFileName);
                verseFile.WriteEndElement();    // verseFile
                verseFile.Close();
                usfx.Close();
                result = true;
            }
            catch (Exception ex)
            {
                Logit.WriteError(ex.Message);
            }
            return result;
        } 
    }
}
