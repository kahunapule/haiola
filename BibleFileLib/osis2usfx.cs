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
    // Converts USFX to Modified OSIS. The modification is subtle in that the results still validate
    // against the OSIS Schema, but do not strictly comply with some of the more problematical
    // assertions of the OSIS manual. The output is designed for import to Sword Project modules,
    // and not recommended for archival use, because some of the non-Scripture features of the
    // work are stripped out due to incompatibilities between the formats.
    public class RecoverOsisData
    {
        // The following need to be set by the caller.
        public string languageCode;
        // Internal variables:

        protected string osisWorkId;
        protected XmlTextReader xr;

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
        protected string osisBook;
        protected string osisVerseId;   // First or only verse in verse bridge
        protected string osisVersesId;  // Space-separated list of verses in verse bridge
        protected string verseeID;
        protected string vpeID;
        protected string chaptereID;
        protected string epeID;
        protected string altChapterID;
        protected string qeID;
        protected int serialNumber = 0;
        protected int noteNumber = 0;
        protected string lastNoteVerse = "";
        protected string currentChapter;
        protected string currentChapterAlternate;
        protected string wordForChapter;
        protected string currentChapterPublished;
        protected string currentBCV;
        protected int chapterNumber; // of the file we are currently generating
        protected string currentVerse;
        protected string currentVersePublished;
        protected string currentVerseAlternate;
        protected string currentFileName = "";
        protected string previousFileName = "";
        protected string chapterLabel = "";
        protected string psalmLabel = "";
        protected string langName = "";
        protected string langId = "";
        protected StringBuilder preVerse = new StringBuilder(String.Empty);
        protected string previousChapterText;
        protected string nextChapterText;
        protected ArrayList chapterFileList = new ArrayList();
        protected int chapterFileIndex = 0;
        protected bool inParagraph = false;
        protected bool inBook = false;
        protected bool ignore = false;
        protected bool inLineGroup = false;
        protected bool inPoetryLine = false;
        protected bool inNote = false;
        protected bool eatPoetryLineEnd = false;
        protected bool mtStarted = false;
        protected bool inReference = false;
        protected bool wHasContent = false;
        protected int listLevel = 0;
        protected int itemLevel = 0;
        protected int indentLevel = 0;
        protected string osisFileName;
        protected string currentElement;
        protected string strongs;
        protected string lastElementWritten = String.Empty;
        protected int mosisNestLevel = 0;
        protected ArrayList elementContext = new ArrayList();

        public static ArrayList bookList = new ArrayList();

        public Scriptures holyBooks;
        public LanguageCodeInfo langCodes;
        public BibleBookInfo bkInfo;

        public RecoverOsisData()
        {
            holyBooks = new Scriptures();
            inWJ = false;
            suspendedWJ = false;
            delayedVerse = 0;
        }

        /// <summary>
        /// Extracts the book, chapter, and verse (or starting verse of a verse range) from an OSIS ID.
        /// Chapter ranges are not supported.
        /// </summary>
        /// <param name="osisId"></param>
        /// <param name="bkTla"></param>
        /// <param name="chNum"></param>
        /// <param name="vNum"></param>
        /// <returns></returns>
        public string ParseOsisId(string osisId, out string bkTla, out int chNum, out int vNum)
        {
            string bcv = String.Empty;
            string ch = "0";
            string v = "0";
            int i;
            bkTla = String.Empty;
            chNum = 0;
            vNum = 0;
            string [] parts = osisId.Trim().Split(new Char[]{'.','-'});
            if (parts.Length > 0)
            {
                bcv = bkTla = holyBooks.bkInfo.TlaFromOsisBook(parts[0]);
                if (parts.Length > 1)
                {
                    ch = parts[1];
                    if (int.TryParse(ch, out i))
                    {
                        chNum = i;
                        bcv += "." + chNum.ToString();
                    }
                    if (parts.Length > 2)
                    {
                        v = parts[2];
                        if (int.TryParse(v, out i))
                        {
                            vNum = i;
                            bcv += "." + vNum.ToString();
                        }
                    }
                }
            }
            return bcv;
        }

        /// <summary>
        /// Finds whatever is in between osisID=" and " and returns it.
        /// </summary>
        /// <param name="s">String with osisID in it.</param>
        /// <returns>Just the osisID contents.</returns>
        protected string ExtractOsisId(string s)
        {
            int i = s.IndexOf("\"");
            if (i > 0)
            {
                s = s.Substring(i + 1);
                i = s.IndexOf("\"");
                s = s.Substring(0, i);
            }
            else
            {
                s = String.Empty;
            }
            return s;
        }

        /// <summary>
        /// Turns a Crosswire mod2imp.exe "OSIS" output into an XML file.
        /// </summary>
        /// <param name="infile">File generated with mod2imp.exe</param>
        /// <param name="outfile">Intermediate XML file</param>
        public void ImpOsis2Xml(string infile, string outfile)
        {
            StreamReader sr;
            StreamWriter sw;
            string line;
            string trimmedLine;
            int c, v;
            string ch = "0";
            //string lastCh = "0";
            string vs = "0";
            //string lastVs = "0";
            string bcv;
            string lastBookAbbrev = String.Empty;
            currentBookAbbrev = String.Empty;
            bool inBook = false;
            bool inIntro = false;
            try
            {
                sr = new StreamReader(infile, Encoding.UTF8);
                sw = new StreamWriter(outfile, false, Encoding.UTF8);
                sw.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
                sw.WriteLine("<impxml>");
                line = sr.ReadLine().Replace("&c.", "&amp;c.");
                while (line != null)
                {
                    trimmedLine = line.Trim();
                    if (trimmedLine.Length > 0)
                    {
                        if (trimmedLine.StartsWith("$$$"))
                        {
                            if (!trimmedLine.Contains("["))
                            {
                                currentBookAbbrev = ParseImpLocation(trimmedLine, out ch, out vs);
                                if (lastBookAbbrev != currentBookAbbrev)
                                {
                                    if (inBook)
                                    {
                                        sw.WriteLine("</book>");
                                    }
                                    sw.WriteLine("<book id=\"{0}\">", currentBookAbbrev);
                                    lastBookAbbrev = currentBookAbbrev;
                                    inBook = true;
                                }
                                if ((!String.IsNullOrEmpty(vs)) && (vs == "0") && !inIntro)
                                {
                                    sw.WriteLine("<p sfm=\"ip\">");
                                    inIntro = true;
                                }
                                if ((!String.IsNullOrEmpty(vs)) && (vs != "0") && inIntro)
                                {
                                    sw.WriteLine("</p>");
                                    inIntro = false;
                                }
                                /*
                                if ((!String.IsNullOrEmpty(vs)) && (vs != "0"))
                                    sw.WriteLine("<v id=\"{0}\"/>", vs);
                                 */
                            }
                        }
                        else if (trimmedLine.StartsWith("<verse"))
                        {
                            bcv = ExtractOsisId(line);
                            bcv = ParseOsisId(bcv, out currentBookAbbrev, out c, out v);
                            if (lastBookAbbrev != currentBookAbbrev)
                            {
                                if (inBook)
                                {
                                    sw.WriteLine("</book>");
                                }
                                sw.WriteLine("<book id=\"{0}\">", currentBookAbbrev);
                                lastBookAbbrev = currentBookAbbrev;
                                inBook = true;
                            }
                            sw.WriteLine(line);
                        }
                        else /*if (trimmedLine.StartsWith("<"))*/
                        {
                            string temp = System.Text.RegularExpressions.Regex.Replace(line, "<div [^>]*>", "");
                            temp = System.Text.RegularExpressions.Regex.Replace(temp, "<chapter [^>]*eID=[^>]*>", "</p>");
                            temp = System.Text.RegularExpressions.Regex.Replace(temp, "lemma=\"Strong:", "lemma=\"");
                            temp = System.Text.RegularExpressions.Regex.Replace(temp, "(<chapter [^>]*>)", "$1<p>");
                            sw.WriteLine(temp);
                        }
                        /*
                        else
                        {
                            sw.WriteLine("<ip>{0}</ip>", line);
                        }
                         */
                    }
                    line = sr.ReadLine();
                    if (line != null)
                        line = line.Replace("&c.", "&amp;c.");
                }
                if (inBook)
                    sw.WriteLine("</book>");
                sw.WriteLine("</impxml>");
                sw.Close();
                sr.Close();
            }
            catch (Exception ex)
            {
                Logit.WriteError(ex.Message);
            }
        }

        /// <summary>
        /// Get the attribute with the given name from the current element
        /// </summary>
        /// <param name="attributeName">name of attribute</param>
        /// <returns>contents of attribute if found, otherwise String.Empty</returns>
        protected string GetNamedAttribute(string attributeName)
        {
            string result = xr.GetAttribute(attributeName);
            if (result == null)
                result = String.Empty;
            return result;
        }

        /// <summary>
        /// Write an empty element with the given optional attributes.
        /// </summary>
        /// <param name="elementName">Element name</param>
        /// <param name="attributeName">First attribute name</param>
        /// <param name="attribute">First attribute string</param>
        /// <param name="attribute2Name">Second attribute name</param>
        /// <param name="attribute2">Second attribute string</param>
        protected void WriteEmptyElementWithAttributes(string elementName, string attributeName = null, string attribute = null, string attribute2Name = null, string attribute2 = null)
        {
            holyBooks.xw.WriteStartElement(elementName);
            if (!String.IsNullOrEmpty(attributeName) && (attribute != null))
            {
                holyBooks.xw.WriteAttributeString(attributeName, attribute);
            }
            if (!String.IsNullOrEmpty(attribute2Name) && (attribute2 != null))
            {
                holyBooks.xw.WriteAttributeString(attribute2Name, attribute2);
            }
            holyBooks.xw.WriteEndElement();


        }

        protected int delayedVerse;
        protected bool inWJ;
        protected bool suspendedWJ;
        /// <summary>
        /// End a run of direct quoted words of Jesus (optional red letters)
        /// </summary>
        protected void EndWJ()
        {
            if (inWJ)
            {
                holyBooks.xw.WriteEndElement(); // wj
                inWJ = false;
            }
        }

        /// <summary>
        /// Start a run of direct quoted words of Jesus (optional red letters)
        /// </summary>
        protected void StartWJ()
        {
            EndWJ();
            holyBooks.xw.WriteStartElement("wj");
            inWJ = true;
        }

        /// <summary>
        /// Temporarily suspend wj markup to pass over a verse marker or paragraph boundary
        /// </summary>
        protected void SuspendWJ()
        {
            if (inWJ)
            {
                suspendedWJ = true;
                EndWJ();
            }
        }

        /// <summary>
        /// Restart temporarily suspended wj markup after new verse or paragraph has started
        /// </summary>
        protected void RestartWJ()
        {
            if (suspendedWJ)
            {
                suspendedWJ = false;
                StartWJ();
            }
        }

        /// <summary>
        /// End the currently active USFX paragraph
        /// </summary>
        protected void EndParagraph()
        {
            if (inParagraph)
            {
                SuspendWJ();
                holyBooks.xw.WriteEndElement(); // p
                inParagraph = false;
            }
        }

        /// <summary>
        /// Start a new USFX paragraph
        /// </summary>
        /// <param name="marker"></param>
        /// <param name="sfm"></param>
        protected void StartNewParagraph(string marker, string sfm = null)
        {
            EndParagraph();
            holyBooks.xw.WriteStartElement(marker);
            if (!String.IsNullOrEmpty(sfm))
            {
                holyBooks.xw.WriteAttributeString("sfm", sfm);
            }
            inParagraph = true;
            if (delayedVerse > 0)
            {
                WriteEmptyElementWithAttributes("v", "id", delayedVerse.ToString());
                delayedVerse = 0;
            }
            RestartWJ();
        }

        /// <summary>
        /// End a USFX book
        /// </summary>
        protected void EndBook()
        {
            EndParagraph();
            if (inBook)
            {
                inBook = false;
                holyBooks.xw.WriteEndElement();
            }
        }

        /// <summary>
        /// Start USFX book
        /// </summary>
        /// <param name="tla"></param>
        protected void StartBook(string tla)
        {
            EndBook();
            holyBooks.xw.WriteStartElement("book");
            holyBooks.xw.WriteAttributeString("id", tla);
            holyBooks.xw.WriteStartElement("id");
            holyBooks.xw.WriteAttributeString("id", tla);
            BibleBookRecord bkrecord = holyBooks.bkInfo.BkRec(tla);
            if (bkrecord != null)
            {
                holyBooks.xw.WriteString(" " + bkrecord.name);
            }
            holyBooks.xw.WriteEndElement(); // id
            inBook = true;
        }


        protected void SkipElement()
        {
            string skip = xr.Name;
            if (!xr.IsEmptyElement)
            {
                while (xr.Read() && !((xr.NodeType == XmlNodeType.EndElement) && (xr.Name == skip)))
                {
                    // Keep looking for the end of this element.
                }
            }
        }


        protected string ParseImpLocation(string impLine, out string chapString, out string verseString)
        {
            string tla = "FRT";
            chapString = "0";
            verseString = "0";
            impLine = impLine.Replace("$$$", "").Trim();
            switch (impLine)
            {
                case "[ Module Heading ]":
                    break;  // Keep "FRT"
                case "[ Testament 1 Heading ]":
                    tla = "GEN";
                    break;
                case "[ Testament 2 Heading ]":
                    tla = "MAT";
                    break;
                default:
                    StringBuilder sb = new StringBuilder();
                    int i = 0;
                    int field = 0;  // 0 = book, 1 = ch, 2 = verse
                    while ((i < impLine.Length) && (field < 3))
                    {
                        switch (field)
                        {
                            case 0:
                                if (Char.IsLetter(impLine[i]) || Char.IsWhiteSpace(impLine[i]))
                                {
                                    sb.Append(impLine[i]);
                                }
                                else
                                {
                                    tla = holyBooks.bkInfo.TlaFromSwordBook(sb.ToString().Trim());
                                    sb.Length = 0;
                                    field++;
                                    if (Char.IsDigit(impLine[i]))
                                        sb.Append(impLine[i]);
                                }
                                break;
                            case 1:
                                if (Char.IsDigit(impLine[i]))
                                {
                                    sb.Append(impLine[i]);
                                }
                                else
                                {
                                    chapString = sb.ToString();
                                    sb.Length = 0;
                                    field++;    // Skip over separator (normally ':')
                                }
                                break;
                            case 2:
                                if (Char.IsDigit(impLine[i]) || (impLine[i] == '-') || (impLine[i] == ',') || Char.IsWhiteSpace(impLine[i]))
                                {
                                    sb.Append(impLine[i]);
                                }
                                else
                                {
                                    field++;    // Skip over unexpected stuff
                                }
                                break;
                        }
                        i++;
                    }
                    if (sb.Length > 0)
                    {
                        verseString = sb.ToString().Trim();
                    }
                break;
            }
            return tla;
        }

        /// <summary>
        /// Read an imp or OSIS file and convert at least the main canonical text to USFX.
        /// </summary>
        /// <param name="infile">input imp or OSIS file name</param>
        /// <param name="outfile">output USFX file name</param>
        public void readImpOsis(string infile, string outfile)
        {
            string line;
            string inname = infile;
            string bookAbbr = String.Empty;
            string bcv = String.Empty;
            string bk;
            int currentChapter = 0;
            int currentVerse = 0;
            int c = 0;
            int v = 0;
            string id = String.Empty;
            string type = String.Empty;
            string osisID = String.Empty;
            string sID = String.Empty;
            string eID = String.Empty;
            string lemma = String.Empty;
            string morph = String.Empty;
            string added = String.Empty;
            string marker = String.Empty;
            string subType = String.Empty;
            string src = String.Empty;
            string savlm = String.Empty;
            string n = String.Empty;
            string who = String.Empty;
            StreamReader sr;
            try
            {
                sr = new StreamReader(infile, Encoding.UTF8);
                line = sr.ReadLine().TrimStart();
                sr.Close();
                if (line.StartsWith("$$$"))
                {
                    inname = outfile + ".tMpXmL";
                    Logit.WriteLine("Converting imp file " + infile + " to imp xml file " + inname);
                    ImpOsis2Xml(infile, inname);
                }
                else if (!line.StartsWith("<?xml version=\"1.0\""))
                {
                    Logit.WriteError("I don't know what to do with this file: " + infile);
                    return;
                }
                xr = new XmlTextReader(inname);
                holyBooks.OpenUsfx(outfile);
                Logit.WriteLine("Converting from " + inname + " to USFX file " + outfile);
                while (xr.Read())
                {
                    if ((delayedVerse > 0) && (xr.Name != "milestone"))
                    {
                        if (!inParagraph)
                            StartNewParagraph("p");
                        if (delayedVerse > 0)
                        {
                            WriteEmptyElementWithAttributes("v", "id", delayedVerse.ToString());
                            delayedVerse = 0;
                        }
                        RestartWJ();
                        delayedVerse = 0;
                    }
                    if (xr.NodeType == XmlNodeType.Element)
                    {
                        id = GetNamedAttribute("id");
                        type = GetNamedAttribute("type");
                        osisID = GetNamedAttribute("osisID");
                        sID = GetNamedAttribute("sID");
                        eID = GetNamedAttribute("eID");
                        lemma = GetNamedAttribute("lemma");
                        morph = GetNamedAttribute("morph");
                        added = GetNamedAttribute("added");
                        marker = GetNamedAttribute("marker");
                        who = GetNamedAttribute("who");
                        subType = GetNamedAttribute("subType");
                        src = GetNamedAttribute("src");
                        savlm = GetNamedAttribute("savlm");
                        n = GetNamedAttribute("n");
                        sfm = GetNamedAttribute("sfm");
                        switch (xr.Name)
                        {
                            case "header":
                                SkipElement();
                                break;
                            case "impxml":
                                Logit.WriteLine("Parsing " + inname + " as imp xml.");
                                break;
                            case "osis":
                                Logit.WriteLine("Parsing " + inname + " as OSIS");
                                break;
                            case "book":
                                StartBook(id);
                                break;
                            case "chapter":
                                bcv = ParseOsisId(osisID, out bk, out c, out v);
                                EndParagraph();
                                WriteEmptyElementWithAttributes("c", "id", c.ToString());
                                currentChapter = c;
                                break;
                            case "v":
                                WriteEmptyElementWithAttributes("v", "id", id);
                                break;
                            case "p":
                                StartNewParagraph("p", sfm);
                                break;
                            case "verse":
                                if (eID.Length > 0)
                                {
                                    SuspendWJ();
                                    WriteEmptyElementWithAttributes("ve");
                                }
                                else
                                {
                                    bcv = ParseOsisId(osisID, out bk, out c, out v);
                                    if (c != currentChapter)
                                    {
                                        EndParagraph();
                                        WriteEmptyElementWithAttributes("c", "id", c.ToString());
                                        currentChapter = c;
                                    }
                                    delayedVerse = v;
                                    currentVerse = v;
                                }
                                break;
                            case "transChange":
                                holyBooks.xw.WriteStartElement("add");
                                break;
                            case "div":
                                switch (type)
                                {
                                    case "book":
                                        bcv = ParseOsisId(osisID, out bk, out c, out v);
                                        StartBook(bk);
                                        break;
                                    case "colophon":
                                        StartNewParagraph("p", "ie");
                                        break;
                                }

                                break;
                            case "milestone":
                                if ((type == "x-extra-p") || (type == "x-p"))
                                {
                                    SuspendWJ();
                                    StartNewParagraph("p");
                                    if (marker.Length > 0)
                                        holyBooks.xw.WriteString(marker + " ");
                                }
                                break;
                            case "w":
                                lemma = (lemma + " " + savlm).Trim();
                                morph = (morph + " " + src).Trim();
                                wHasContent = (!xr.IsEmptyElement) && ((lemma.Length + morph.Length) > 0);
                                if (wHasContent)
                                {
                                    holyBooks.xw.WriteStartElement("w");
                                    if (lemma.Length > 0)
                                        holyBooks.xw.WriteAttributeString("s", lemma.Replace("strong:", ""));
                                    if (morph.Length > 0)
                                        holyBooks.xw.WriteAttributeString("m", morph.Trim());
                                    if (xr.IsEmptyElement)
                                        holyBooks.xw.WriteEndElement();
                                }
                                else
                                {   // Otherwise, don't bother with the tag, because we really don't know semantically what it means.
                                    Logit.WriteLine("Warning: empty <w> element ignored at " + bcv);
                                }
                                break;
                            case "title":
                                switch (type)
                                {
                                    case "main":
                                        StartNewParagraph("mt");
                                        break;
                                    case "psalm":
                                        StartNewParagraph("d");
                                        break;
                                    case "acrostic":
                                        StartNewParagraph("s");
                                        break;
                                    case "chapter":
                                        if (!inParagraph)
                                            StartNewParagraph("p");
                                        SkipElement();
                                        break;
                                }
                                break;
                            case "note":    // type="study"
                                if (type == "study")
                                {
                                    holyBooks.xw.WriteStartElement("f");
                                    holyBooks.xw.WriteAttributeString("caller", "+");
                                }
                                else
                                {
                                    SkipElement();
                                }
                                break;
                            case "divineName":
                                holyBooks.xw.WriteStartElement("nd");
                                break;
                            case "foreign":
                                holyBooks.xw.WriteStartElement("tl");
                                if (n.Length > 0)
                                    holyBooks.xw.WriteString(" " + n + " ");
                                break;
                            case "ip":
                                holyBooks.xw.WriteStartElement("p");
                                holyBooks.xw.WriteAttributeString("sfm", "ip");
                                break;
                            case "q":
                                if (marker != String.Empty)
                                {
                                    Console.WriteLine("Unsupported marker in <q> at {0}: \"{1}\"", bcv, marker);
                                }
                                if (who == "Jesus")
                                {
                                    StartWJ();
                                }
                                break;
                        }
                    }
                    else if (xr.NodeType == XmlNodeType.EndElement)
                    {
                        switch (xr.Name)
                        {
                            case "w":
                                if (wHasContent)
                                    holyBooks.xw.WriteEndElement();
                                break;
                            case "book":
                                EndBook();
                                currentChapter = 0;
                                break;
                            case "divineName":
                            case "transChange":
                            case "foreign":
                            case "note":
                            case "ip":
                                holyBooks.xw.WriteEndElement();
                                break;
                            case "verse":   // <ve />
                                SuspendWJ();
                                WriteEmptyElementWithAttributes("ve");
                                break;
                            case "q":
                                EndWJ();
                                break;
                            case "title":
                                EndParagraph();
                                break;
                            case "p":
                                EndParagraph();
                                break;
                        }
                    }
                    else if (xr.NodeType == XmlNodeType.Text)
                    {
                        holyBooks.xw.WriteString(xr.Value);
                    }
                    else if (xr.NodeType == XmlNodeType.Whitespace)
                    {
                        holyBooks.xw.WriteWhitespace(xr.Value);
                    }



                }
                holyBooks.CloseUsfx();
                xr.Close();
            }
            catch (Exception ex)
            {
                Logit.WriteError("Error at " + bcv);
                Logit.WriteError(ex.Message);
                Logit.WriteError(ex.StackTrace);
            }
        }

    }
}
