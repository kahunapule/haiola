// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003-2013, SIL International, EBT, and Youth With A Mission
// <copyright from='2003' to='2013' company='SIL International, EBT, and Youth With A Mission'>
//    
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
// File: BibleBookInfo.cs
// Responsibility: (Kahunapule) Michael P. Johnson
// Last reviewed: 
// 
// <remarks>
// Bible book metadata: names, versification, etc.
// </remarks>
// --------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace WordSend
{
    /// <summary>
    /// Structure holding information about one particular book of the Bible.
    /// Some of the information is more or less global, and some specific to
    /// one particular translation.
    /// </summary>
    public class BibleBookRecord
    {
        public int sortOrder;
        public int numChapters;
        public int[] verseCount;
        public string tla;  // Standard three letter abbreviation of book
        public string osisName;
        public string name; // Constant English long name
        public string shortName;    // Constant English short name
        public string shortCode;    // Constant English-like 2-char abbreviation
        public string bibleworksCode;   // 3-letter codes used by BibleWorks software for import
        private string swordName;   // Hard coded Sword Project English short name
        public int actualChapters;
        public string vernacularHeader; // From \h
        public string vernacularName;   // From \mt
        public string vernacularLongName; // from \toc1
        public string vernacularShortName;  // from \toc2
        public string vernacularAbbreviation;   // From \toc3
        public string vernacularAltName;    // From \ztoc4 or BookNames.xml alt attribute
        public string testament;
        public StringBuilder toc;
        public int publicationOrder;
        public ArrayList chapterFiles;  // Chapter file names only, i.e. PSA119.htm
        public ArrayList chaptersFound; // Contains ChapterInfo records of chapters in this book
        public bool isOnDisk;
        public bool includeThisBook;

        public bool IsPresent
        {
            get { return isOnDisk && includeThisBook; }
            set { isOnDisk = value; }
        }


        /// <summary>
        /// Constructor initalizes a (sort of) empty BibleBookRecord
        /// </summary>
        public BibleBookRecord()
        {
            sortOrder = publicationOrder = 0;
            numChapters = 151;
            actualChapters = 0;
            IsPresent = false;
            tla = osisName = name = shortName = testament = vernacularAbbreviation = vernacularHeader = String.Empty;
            vernacularName = vernacularShortName = vernacularLongName = bibleworksCode = String.Empty;
            toc = new StringBuilder();
        }

        public string swordShortName
        {
            get {
                if (String.IsNullOrEmpty(swordName))
                {
                    if (tla == "REV")
                        swordName = "Revelation of John";
                    else
                    {
                        if (shortName != null)
                            swordName = shortName.Replace("1", "I").Replace("2", "II").Replace("3", "III");
                    }
                }
                return (swordName);
            }
            set { swordName = value; }
        }

        /// <summary>
        /// Read only property returns true iff this book is present and nonempty
        /// </summary>
        public bool HasContent
        {
            get { return IsPresent && ((chapterFiles != null && chapterFiles.Count > 0) || (testament == "x")); }
        }

        /// <summary>
        /// Returns true iff the listed chapter and verse are included in this translation.
        /// (This currently does not take into account missing verses within a chapter.)
        /// </summary>
        /// <param name="ch">Chapter number to check</param>
        /// <param name="vs">Verse number to check</param>
        /// <returns></returns>
        public bool isValidTarget(int ch, int vs)
        {
            bool result = false;
            int i;
            ChapterInfo ci;
            if (IsPresent)
            {
                for (i = 0; (i < chaptersFound.Count) && (!result); i++)
                {
                    ci = (ChapterInfo)chaptersFound[i];
                    if (ci != null)
                    {
                        if (ci.chapterInteger == ch)
                        {
                            if (vs <= ci.maxVerse)
                                result = true;
                        }
                    }
                }
            }
            return result;
        }
    }

    /// <summary>
    /// Information about books of the Bible in general and the current translation in particular, including
    /// names, versification, etc. It tracks the vernacular names and abbreviations of books, the versification
    /// and structure of the current project based on what is actually there for navigational purposes, and
    /// gathers a few statistics. Book order is assumed to be that which is given in BibleBookInfo.xml unless
    /// overridden by a bookorder.txt file in the project directory.
    /// </summary>
    public class BibleBookInfo
    {
        public const int MAXNUMBOOKS = 125;	// Includes Apocrypha + extrabiblical helps, front & back matter, etc.
        public const int MAXNUMCHAPTERS = 2000;  // Because someone used XXA for a hymnal, one chapter per hymn.
        public Hashtable books;
        public BibleBookRecord[] bookArray = new BibleBookRecord[MAXNUMBOOKS];
        public BibleBookRecord[] publishArray = new BibleBookRecord[MAXNUMBOOKS];
        protected int publishArrayActualBookCount = -1;
        // public Hashtable altNames;
        protected bool apocryphaFound;

        public int publishArrayCount
        {
            get
            {
                int i;
                if (publishArrayActualBookCount < 0)
                {
                    i = 0;
                    while ((publishArray[i] != null) && (i < publishArray.Length))
                    {
                        i++;
                    }
                    publishArrayActualBookCount = i;
                }
                return publishArrayActualBookCount;
            }
        }

        /// <summary>
        /// Returns the standard three-character abbreviation for a book given the 2-character short code for the book.
        /// </summary>
        /// <param name="shortAbbr">2-chararter abbreviation for a book</param>
        /// <returns>3-character abbreviation for a book</returns>
        public string tlaFrom2la(string shortAbbr)
        {
            string tla = String.Empty;
            int i = 0;
            while ((tla == String.Empty) && (i < bookArray.Length))
            {   // Ugly, time-consuming linear search... but simple to code. Refactor later if this is too slow.
                if (bookArray[i].shortCode == shortAbbr)
                    tla = bookArray[i].tla;
                i++;
            }
            return tla;
        }

        /// <summary>
        /// Returns standard three-character abbreviation for a book given the BibleWorks code.
        /// </summary>
        /// <param name="bwCode">BibleWorks book abbreviation</param>
        /// <returns>Standard SIL/UBS 3-character abbreviation for a book.</returns>
        public string tlaFromBW(string bwCode)
        {
            string tla = String.Empty;
            int i = 0;
            while ((tla == String.Empty) && (i < bookArray.Length))
            {   // Ugly, time-consuming linear search... but simple to code. Refactor later if this is too slow.
                if ((bookArray[i] != null) && !String.IsNullOrEmpty(bookArray[i].bibleworksCode))
                {
                    if (bookArray[i].bibleworksCode.ToUpper() == bwCode.ToUpper())
                        tla = bookArray[i].tla;
                }
                i++;
            }
            return tla;
        }

        // protected HashSet<string> presentVerses;

        /// <summary>
        /// Normalize strings for comparison by removing spaces and periods and converting
        /// to all upper case.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        protected string PrepareToCompare(string s)
        {
            string result = s.Trim().ToUpperInvariant();
            result = s.Replace(" ", "");
            result = s.Replace(".", "");
            return result;
        }

        /// <summary>
        /// Find the standard three-letter abbreviation for a Bible book given any
        /// of its vernacular names (long, short, or abbreviation).
        /// </summary>
        /// <param name="book">vernacular name of Bible book</param>
        /// <returns>standard 3-letter abbreviation of Bible book</returns>
        /*
        public string getTla(string book)
        {
            string tla = (string)altNames[PrepareToCompare(book)];
            if (tla == null)
                tla = String.Empty;
            return tla;
        }
        */

        protected Hashtable osis2Tla = null;

        /// <summary>
        /// Finds the UBS/SIL three-letter abbreviation for a book, given the OSIS book abbreviation.
        /// </summary>
        /// <param name="book"></param>
        /// <returns>3-letter book abbreviation</returns>
        public string TlaFromOsisBook(string book)
        {
            string tla;
            if (osis2Tla == null)
            {
                osis2Tla = new Hashtable(109);
                foreach (BibleBookRecord br in bookArray)
                {
                    if (br != null)
                        osis2Tla[br.osisName] = br.tla;
                }
            }
            tla = (string)osis2Tla[book];
            if (tla == null)
                tla = String.Empty;
            return tla;
        }

        protected Hashtable sword2Tla = null;

        /// <summary>
        /// Finds the UBS/SIL three-letter abbreviation for a book, given the Sword short book name.
        /// </summary>
        /// <param name="book">Standard Sword short book name.</param>
        /// <returns>3-letter book abbreviation</returns>
        public string TlaFromSwordBook(string book)
        {
            string tla;
            if (sword2Tla == null)
            {
                sword2Tla = new Hashtable(109);
                foreach (BibleBookRecord br in bookArray)
                {
                    if (br != null)
                        sword2Tla[br.swordShortName.ToUpperInvariant()] = br.tla;
                }
            }
            tla = (string)sword2Tla[book.Trim().ToUpperInvariant()];
            if (tla == null)
                tla = String.Empty;
            return tla;
        }

        /// <summary>
        /// Is this book an Apocrypha/Deuterocanon book?
        /// </summary>
        /// <param name="abbrev">standard 3-letter book abbreviation</param>
        /// <returns>true iff the book is part of the Apocrypha/Deuterocanon</returns>
        public bool isApocrypha(string abbrev)
        {
            BibleBookRecord br = (BibleBookRecord)books[abbrev];
            if (br == null)
                return false;
            return br.testament == "a";
        }

        public bool isCanon(string abbrev)
        {
            BibleBookRecord br = (BibleBookRecord)books[abbrev];
            if (br == null)
                return false;
            return (br.testament == "o") || (br.testament == "n");
        }

        /// <summary>
        /// Is this book a peripheral book (front matter, back matter, glossary, helps)?
        /// </summary>
        /// <param name="abbrev">standard 3-letter book abbreviation</param>
        /// <returns>true iff this is a peripheral book</returns>
        public bool isPeripheral(string abbrev)
        {
            BibleBookRecord br = (BibleBookRecord)books[abbrev];
            if (br == null)
                return false;
            return br.testament == "x";
        }

        /// <summary>
        /// What order should this book be sorted into for publication?
        /// The default order is like the NRSV with Apocrypha (not the RC edition),
        /// as presented in the BibleBookInfo.xml file in the program data,
        /// but can be overridden with the ReadPublicationOrder method.
        /// </summary>
        /// <param name="abbrev">standard 3-letter book abbreviation</param>
        /// <returns>integer indicating where this book belongs from front to back of the complete Bible + helps</returns>
        public int Order(string abbrev)
        {
            BibleBookRecord br = (BibleBookRecord)books[abbrev];
            if (br == null)
                return 0;
            return br.sortOrder;
        }

        /// <summary>
        /// Make up the first part of a file name that will sort in canonical order in a directory
        /// listing and which contains the standard 3-letter abbreviation of each book.
        /// </summary>
        /// <param name="abbrev">standard 3-letter book abbreviation</param>
        /// <returns>string like 01-GEN, 02-EXO, etc.</returns>
        public string FilePrefix(string abbrev)
        {
            BibleBookRecord br = (BibleBookRecord)books[abbrev];
            if (br == null)
                return "00-" + abbrev;
            int num = br.sortOrder;
            if (num < 40)
            {
                apocryphaFound = false;
                return num.ToString("d2") + "-" + abbrev;
            }
            if (num < 64)
            {
                apocryphaFound = true;
            }
            else
            {
                if (!apocryphaFound)
                    num -= 24;
            }
            return num.ToString("d2") + "-" + abbrev;
        }

        /// <summary>
        /// What is the variable-length OSIS ID for this book?
        /// </summary>
        /// <param name="abbrev">standard 3-letter book abbreviation</param>
        /// <returns>OSIS ID for this book or an empty string if not found</returns>
        public string OsisID(string abbrev)
        {
            BibleBookRecord br = (BibleBookRecord)books[abbrev];
            if (br == null)
                return "";
            return br.osisName;
        }

        /// <summary>
        /// Get the 2-character short code for a book given its 3-letter abbreviation
        /// </summary>
        /// <param name="bookAbbrev">standard UBS/SIL 3-character abbreviation</param>
        /// <returns>2-character short book code or "" if not found</returns>
        public string getShortCode(string bookAbbrev)
        {
            BibleBookRecord br = (BibleBookRecord)books[bookAbbrev];
            if (br == null)
                return "";
            return br.shortCode;
        }



        /// <summary>
        /// Returns true iff we are pretty sure this book, chapter, and verse are present.
        /// </summary>
        /// <param name="bk">Three-character book abbreviation</param>
        /// <param name="ch">chapter number</param>
        /// <param name="vs">verse number</param>
        /// <returns>true iff we think that verse is in this translation</returns>
        public bool isValidTarget(string bk, int ch, int vs)
        {
            BibleBookRecord br = (BibleBookRecord)books[bk];
            if (br == null)
                return false;
            if (!br.IsPresent)
                return false;
            if (!br.includeThisBook)
                return false;
            return br.isValidTarget(ch, vs);
        }

        /// <summary>
        /// Gets the full BibleBookRecord structure for the book with the given
        /// standard 3-letter abbreviation.
        /// </summary>
        /// <param name="abbrev">standard 3-letter book abbreviation</param>
        /// <returns>BibleBookRecord with information about this book</returns>
        public BibleBookRecord BkRec(string abbrev)
        {
            return (BibleBookRecord)books[abbrev];
        }

        /// <summary>
        /// Reads Bible book names from BookNames.xml, as exported by Paratext
        /// </summary>
        /// <param name="bookNamesFile">Full path and file name of BookNames.xml</param>
        /// <returns>true iff the file was found and read</returns>
        public bool ReadDefaultBookNames(string bookNamesFile)
        {
            XmlTextReader xr;
            bool inBookNames = false;
            string tla;
            BibleBookRecord br;

            if (!File.Exists(bookNamesFile))
                return false;
            try
            {
                xr = new XmlTextReader(bookNamesFile);
                xr.WhitespaceHandling = WhitespaceHandling.Significant;
                while (xr.Read())
                {
                    if (xr.Name == "BookNames")
                    {
                        inBookNames = xr.IsStartElement();
                    }
                    else if (inBookNames && xr.IsStartElement("book"))
                    {
                        tla = xr.GetAttribute("code");
                        if (!String.IsNullOrEmpty(tla))
                        {
                            br = BkRec(tla);
                            if (br != null)
                            {
                                br.vernacularAbbreviation = fileHelper.NoNull(xr.GetAttribute("abbr"));
                                br.vernacularShortName = fileHelper.NoNull(xr.GetAttribute("short"));
                                br.vernacularLongName = fileHelper.NoNull(xr.GetAttribute("long"));
                                br.vernacularAltName = fileHelper.NoNull(xr.GetAttribute("alt"));
                            }
                        }
                    }
                }
                xr.Close();
            }
            catch (Exception ex)
            {
                Logit.WriteError("Error reading "+ bookNamesFile +Environment.NewLine+ ex.Message);
                return false;
            }
            return true;
        }

        public List<bookMatch> bookMatchList;

        /// <summary>
        /// Writes a BookNames.xml file in the same format as used by Paratext.
        /// </summary>
        /// <param name="bookNamesFile">Full path and name of XML file to write</param>
        public void WriteDefaultBookNames(string bookNamesFile)
        {
            XmlTextWriter xw;
            bookMatchList = new List<bookMatch>();
            try
            {
                Utils.EnsureDirectory(Path.GetDirectoryName(bookNamesFile));
                xw = new XmlTextWriter(bookNamesFile, Encoding.UTF8);
                xw.Formatting = Formatting.Indented;
                xw.WriteStartDocument();
                xw.WriteStartElement("BookNames");
                foreach (BibleBookRecord br in bookArray)
                {
                    if (br != null)
                    {
                        if ((br.testament != "x") && (!(String.IsNullOrEmpty(br.tla) || (String.IsNullOrEmpty(br.vernacularShortName)))))
                        {
                            xw.WriteStartElement("book");
                            xw.WriteAttributeString("code", br.tla);
                            xw.WriteAttributeString("abbr", fileHelper.NoNull(br.vernacularAbbreviation));
                            if (!string.IsNullOrEmpty(br.vernacularAbbreviation))
                                bookMatchList.Add(new bookMatch() { bookName = br.vernacularAbbreviation, bookTLA = br.tla });
                            xw.WriteAttributeString("short", fileHelper.NoNull(br.vernacularShortName));
                            if ((br.vernacularAbbreviation != br.vernacularShortName) && !string.IsNullOrEmpty(br.vernacularShortName))
                            {
                                bookMatchList.Add(new bookMatch() { bookName = br.vernacularShortName, bookTLA = br.tla });
                            }
                            xw.WriteAttributeString("long", fileHelper.NoNull(br.vernacularLongName));
                            xw.WriteAttributeString("alt", fileHelper.NoNull(br.vernacularAltName));
                            if ((!string.IsNullOrEmpty(br.vernacularAltName)) && (br.vernacularAltName != br.vernacularShortName))
                            {
                                bookMatchList.Add(new bookMatch() { bookName = br.vernacularShortName, bookTLA = br.tla });
                            }
                            xw.WriteEndElement();   // book
                        }
                    }
                }
                xw.WriteEndElement();   // BookNames
                xw.Close();
                bookMatchList.Sort();
            }
            catch (Exception ex)
            {
                Logit.WriteError("Error writing "+bookNamesFile+Environment.NewLine+ex.Message);
            }
        }



        /// <summary>
        /// Read Bible Book information from the indicated file assuming that it
        /// contains information about all supported books that might be bound in
        /// a Bible (both canonical books and certain extra stuff), assuming that
        /// it was written to the BibleBookInfo.xsd schema.
        /// </summary>
        /// <param name="fileName">Name of XML data file with Bible book information</param>
        public void ReadBookInfoFile(string fileName)
        {
            int i;
            books = new Hashtable(197);
            BibleBookRecord bkRecord = null;
            XmlTextReader xr = new XmlTextReader(fileName);
            xr.WhitespaceHandling = WhitespaceHandling.Significant;
            while (xr.Read())
            {
                if ((xr.NodeType == XmlNodeType.Element) && (!xr.IsEmptyElement))
                {
                    switch (xr.Name)
                    {
                        case "Book":
                            bkRecord = new BibleBookRecord();
                            for (i = 0; i < xr.AttributeCount; i++)
                            {
                                xr.MoveToAttribute(i);
                                if (xr.Name == "testament")
                                {
                                    bkRecord.testament = xr.Value;
                                }
                            }
                            xr.MoveToElement();
                            break;
                        case "sfmTla":
                            xr.Read();
                            bkRecord.tla = xr.Value;
                            break;
                        case "osisName":
                            xr.Read();
                            bkRecord.osisName = xr.Value;
                            break;
                        case "name":
                            xr.Read();
                            bkRecord.name = xr.Value;
                            break;
                        case "shortName":
                            xr.Read();
                            bkRecord.shortName = xr.Value;
                            break;
                        case "sortOrder":
                            xr.Read();
                            bkRecord.sortOrder = Convert.ToInt32(xr.Value);
                            break;
                        case "numChapters":
                            xr.Read();
                            bkRecord.numChapters = Convert.ToInt32(xr.Value);
                            bkRecord.verseCount = new int[MAXNUMCHAPTERS];
                            if ((bkRecord.sortOrder < 0) || (bkRecord.sortOrder >= BibleBookInfo.MAXNUMBOOKS))
                            {
                                Logit.WriteError("ERROR: bad sort order number:" + bkRecord.sortOrder.ToString());
                                bkRecord.sortOrder = 0;
                            }
                            break;
                        case "shortCode":
                            xr.Read();
                            bkRecord.shortCode = xr.Value;
                            break;
                        case "bibleworksCode":
                            xr.Read();
                            bkRecord.bibleworksCode = xr.Value;
                            break;
                    }
                }
                else if (xr.NodeType == XmlNodeType.EndElement)
                {
                    if (xr.Name == "Book")
                    {
                        books[bkRecord.tla] = bkRecord;
                        bookArray[bkRecord.sortOrder] = bkRecord;
                        //publishArray[bkRecord.sortOrder] = bkRecord;    // Default book publication order- set elsewhere
                    }
                }
            }
            xr.Close();
        }

        /// <summary>
        /// Instantiates a new instance of BibleBookInfo using data in the named file.
        /// </summary>
        /// <param name="fileName">BibleBookInfo.xml</param>
        public BibleBookInfo(string fileName)
        {
            ReadBookInfoFile(fileName);
        }

        /// <summary>
        /// Reads a file indicating the proper publication order for this translation instance.
        /// The file should be a plain text file, one line per book, with the standard 3-letter
        /// book abbreviation being the first 3 characters on the line, in the order that this
        /// Bible translation should be presented to the reader. All-blank lines or lines starting
        /// with anything other than a letter or digit are comments and are ignored. Anything
        /// after the first 3 nonblank characters of a line are ignored.
        /// </summary>
        /// <param name="fileName">text file to read</param>
        public void ReadPublicationOrder(string fileName)
        {
            int i = 0;
            BibleBookRecord br;

            string line;
            try
            {
                foreach (BibleBookRecord bkrec in bookArray)
                {
                    if (bkrec != null)
                        bkrec.includeThisBook = false;
                }
                StreamReader sr = new StreamReader(fileName);
                while (sr.Peek() >= 0)
                {
                    line = sr.ReadLine().Trim().ToUpperInvariant();
                    if (line.Length > 3)
                        line = line.Substring(0, 3);
                    if ((line.Length == 3) && Char.IsLetterOrDigit(line[0]) && (i < MAXNUMBOOKS))
                    {
                        br = (BibleBookRecord)books[line];
                        if (br == null)
                        {
                            Logit.WriteError("Bad abbreviation " + line + " in " + fileName);
                        }
                        else
                        {
                            br.publicationOrder = i;
                            br.IsPresent = false;
                            br.includeThisBook = true;
                            publishArray[i] = br;
                            i++;
                        }
                    }
                }
                if (i < MAXNUMBOOKS)
                    publishArray[i] = null;
                publishArrayActualBookCount = i;
                sr.Close();
            }
            catch (Exception ex)
            {
                Logit.WriteError("Error reading " + fileName + ex.Message);
            }
        }

        /// <summary>
        /// Instantiate a new BibleBookInfo object using data in BibleBookInfo.xml
        /// </summary>
        public BibleBookInfo()
        {
            ReadBookInfoFile(SFConverter.FindAuxFile("BibleBookInfo.xml"));
        }

        private string languageCode = string.Empty;

        /// <summary>
        /// Read-only property getting the current translation's 3-letter Ethnologue language code.
        /// </summary>
        public string ethnologueCode { get { return languageCode; } }

        public ArrayList allChapters;

        /// <summary>
        /// Reads vernacular book name and versification information from USFX file.
        /// Also tracks which chapters and verses are present and populates ChapterInfo and VerseInfo arrays
        /// for each book.
        /// </summary>
        /// <param name="usfxName">Name of the USFX file to parse</param>
        public void ReadUsfxVernacularNames(string usfxName)
        {
            string level = String.Empty;
            string style = String.Empty;
            string sfm = String.Empty;
            string caller = String.Empty;
            string id = String.Empty;
            string currentBookAbbrev = String.Empty;
            string currentBookCode = String.Empty;  // Current book short code
            string bookOsisId = String.Empty;
            BibleBookRecord bookRecord = (BibleBookRecord)bookArray[0];
            string chapterString = String.Empty;
            int chapterNumber = 0;
            string verseString = String.Empty;
            string verseRangeEnd = String.Empty;
            string bookNamesFile = Path.Combine(Path.GetDirectoryName(usfxName), "BookNames.xml");
            int verseNumber = 0;
            int verseRangeEndNumber = 0;
            //int i;
            bool inParagraph = false;
            allChapters = new ArrayList(1195);  // Big enough for OT + NT + some peripherals. Reallocation will happen with Apocrypha/Deuterocanon.
            ChapterInfo ci = new ChapterInfo();

            // presentVerses = new HashSet<string>;

            try
            {
                // Look for default names for books not in the USFX file.
                ReadDefaultBookNames(bookNamesFile);

                XmlTextReader usfx = new XmlTextReader(usfxName);
                usfx.WhitespaceHandling = WhitespaceHandling.Significant;
                // altNames = new Hashtable(997);
                while (usfx.Read())
                {
                    if (usfx.NodeType == XmlNodeType.Element)
                    {
                        level = fileHelper.GetNamedAttribute(usfx, "level");
                        style = fileHelper.GetNamedAttribute(usfx, "style");
                        sfm = fileHelper.GetNamedAttribute(usfx, "sfm");
                        caller = fileHelper.GetNamedAttribute(usfx, "caller");
                        id = fileHelper.GetNamedAttribute(usfx, "id");

                        switch (usfx.Name)
                        {
                            case "languageCode":
                                usfx.Read();
                                if (usfx.NodeType == XmlNodeType.Text)
                                    languageCode = usfx.Value;
                                break;
                            case "book":
                                if (id.Length > 2)
                                {
                                    currentBookAbbrev = PrepareToCompare(id);
                                    bookRecord = (BibleBookRecord)books[currentBookAbbrev];
                                    bookRecord.chaptersFound = new ArrayList(151);
                                    bookOsisId = bookRecord.osisName;
                                    currentBookCode = bookRecord.shortCode;
                                    if (bookRecord == null)
                                    {
                                        Logit.WriteError("Cannot process unknown book \"" + currentBookAbbrev + "\" in " + usfxName);
                                        return;
                                    }
                                    // altNames[currentBookAbbrev] = currentBookAbbrev;
                                }
                                chapterString = verseString = String.Empty;
                                chapterNumber = 0;
                                verseNumber = 0;
                                break;
                            case "id":
                                if (PrepareToCompare(id) != currentBookAbbrev)
                                {
                                    Logit.WriteError("ERROR: id element " + id + " <> " + " book id " + currentBookAbbrev + " in " + usfxName);
                                }
                                break;
                            case "p":
                                if (sfm.CompareTo("mt") == 0)
                                {
                                    usfx.Read();
                                    if (usfx.NodeType == XmlNodeType.Text)
                                    {
                                        if (bookRecord.vernacularName.Length > 0)
                                            bookRecord.vernacularName = bookRecord.vernacularName + " " + usfx.Value.Trim();
                                        else
                                            bookRecord.vernacularName = usfx.Value.Trim();
                                    }
                                    // altNames[PrepareToCompare(bookRecord.vernacularName)] = currentBookAbbrev;
                                }
                                else
                                {
                                    inParagraph = true;
                                }
                                break;
                            case "h":
                                if (level != String.Empty)
                                    Logit.WriteLine("Warning: level not supported on h element.");
                                usfx.Read();
                                if (usfx.NodeType == XmlNodeType.Text)
                                {
                                    bookRecord.vernacularHeader = usfx.Value.Trim();
                                    // altNames[PrepareToCompare(bookRecord.vernacularHeader)] = currentBookAbbrev;
                                }

                                break;
                            case "c":
                                chapterString = id.Trim();
                                verseString = verseRangeEnd = String.Empty;
                                verseNumber = verseRangeEndNumber = 0;
                                int chNum;
                                if (Int32.TryParse(chapterString, out chNum))
                                {
                                    chapterNumber = chNum;
                                    if (chapterNumber >= MAXNUMCHAPTERS)
                                    {
                                        chapterNumber = MAXNUMCHAPTERS - 1;
                                        Logit.WriteError("Bad chapter number at " + currentBookAbbrev + " " + chapterString + " in " + usfxName);
                                    }
                                }
                                else
                                {
                                    chapterNumber++;
                                    Logit.WriteError("Bad chapter number at " + currentBookAbbrev + " " + chapterString + " in " + usfxName);
                                }
                                bookRecord.actualChapters = Math.Max(bookRecord.numChapters, chapterNumber);
                                ci = new ChapterInfo();
                                ci.chapterInteger = chapterNumber;
                                ci.alternate = ci.actual = chapterString;
                                ci.published = chapterString;
                                ci.osisChapter = bookOsisId + "." + chapterNumber.ToString();
                                ci.chapterId = currentBookCode + chapterNumber.ToString();
                                ci.maxVerse = ci.verseCount = 0;
                                ci.bookRecord = bookRecord;
                                ci.verses[0] = new VerseInfo();
                                ci.verses[0].startVerse = ci.verses[0].endVerse = 0;
                                ci.verses[0].verse = string.Empty;
                                bookRecord.chaptersFound.Add(ci);
                                allChapters.Add(ci);
                                break;
                            case "cp":
                                if (!usfx.IsEmptyElement)
                                {
                                    usfx.Read();
                                    if (usfx.NodeType == XmlNodeType.Text)
                                    {
                                        ci.published = fileHelper.LocalizeDigits(usfx.Value.Trim());
                                    }
                                }
                                break;
                            case "ca":
                                if (!usfx.IsEmptyElement)
                                {
                                    usfx.Read();
                                    if (usfx.NodeType == XmlNodeType.Text)
                                    {
                                        ci.alternate = usfx.Value.Trim();
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
                                                if (String.IsNullOrEmpty(bookRecord.vernacularLongName))
                                                {
                                                    bookRecord.vernacularLongName = usfx.Value.Trim();
                                                }
                                                // altNames[PrepareToCompare(bookRecord.vernacularLongName)] = currentBookAbbrev;
                                                break;
                                            case "2":
                                                if (String.IsNullOrEmpty(bookRecord.vernacularShortName))
                                                {
                                                    bookRecord.vernacularShortName = usfx.Value.Trim();
                                                }
                                                // altNames[PrepareToCompare(bookRecord.vernacularShortName)] = currentBookAbbrev;
                                                break;
                                            case "3":
                                                if (String.IsNullOrEmpty(bookRecord.vernacularAbbreviation))
                                                {
                                                    bookRecord.vernacularAbbreviation = usfx.Value.Trim();
                                                }
                                                // altNames[PrepareToCompare(bookRecord.vernacularAbbreviation)] = currentBookAbbrev;
                                                break;
                                            case "4":
                                                if (String.IsNullOrEmpty(bookRecord.vernacularAltName))
                                                {
                                                    bookRecord.vernacularAltName = usfx.Value.Trim();
                                                }
                                                // altNames[PrepareToCompare(bookRecord.vernacularAltName)] = currentBookAbbrev;
                                                break;
                                        }
                                    }
                                }
                                break;
                            case "v":
                                verseString = id.Trim();
                                int dashPlace = verseString.IndexOf('-');
                                if (dashPlace > 0)
                                {
                                    verseRangeEnd = verseString.Substring(dashPlace + 1);
                                    verseString = verseString.Substring(0, dashPlace);
                                }
                                else
                                {
                                    verseRangeEnd = verseString;
                                }
                                int vnum;
                                if (Int32.TryParse(verseString, out vnum))
                                {
                                    verseNumber = vnum;
                                }
                                else
                                {
                                    verseNumber++;
                                }
                                if (Int32.TryParse(verseRangeEnd, out vnum))
                                {
                                    if (vnum >= verseNumber)
                                    {
                                        verseRangeEndNumber = vnum;
                                    }
                                    else
                                    {
                                        verseRangeEndNumber = verseNumber;
                                    }
                                }
                                else
                                {
                                    verseRangeEndNumber = verseNumber;
                                }
                                bookRecord.verseCount[chapterNumber] = verseRangeEndNumber;
                                ci.maxVerse = Math.Max(ci.maxVerse, verseRangeEndNumber);
                                if (ci.verseCount < ChapterInfo.MAXNUMVERSES)
                                {
                                    ci.verses[ci.verseCount] = new VerseInfo();
                                    ci.verses[ci.verseCount].verseMarker = verseString;
                                    ci.verses[ci.verseCount].startVerse = verseNumber;
                                    ci.verses[ci.verseCount].endVerse = verseRangeEndNumber;
                                    ci.verses[ci.verseCount].verse = verseString;
                                    ci.verseCount++;
                                }
                                else
                                {
                                    Logit.WriteError("Bad verse number: " + verseString + " in " + currentBookAbbrev + " " + chapterString);
                                }
                                break;
                            case "x":

                                break;
                        }
                    }
                    else if (usfx.NodeType == XmlNodeType.Text)
                    {
                        if (inParagraph && (usfx.Value.Trim().Length > 0))
                        {
                            bookRecord.IsPresent = true;
                        }
                    }
                    else if (usfx.NodeType == XmlNodeType.EndElement)
                    {
                        if (usfx.Name == "book")
                        {
                            if (bookRecord.vernacularName == String.Empty)
                            {
                                if ((bookRecord.testament == "o") || (bookRecord.testament == "n") || (bookRecord.testament == "a"))
                                {
                                    Logit.WriteError("Missing main title in " + currentBookAbbrev + " in " + usfxName);
                                }
                                bookRecord.vernacularName = bookRecord.vernacularLongName;
                                if (bookRecord.vernacularName == String.Empty)
                                {
                                    bookRecord.vernacularName = bookRecord.vernacularShortName;
                                }
                            }
                            if (bookRecord.vernacularLongName == String.Empty)
                            {
                                bookRecord.vernacularLongName = bookRecord.vernacularName;
                                // Logit.WriteLine("Missing toc1 (long title) element in " + currentBookAbbrev + " in " + usfxName);
                            }
                            if (bookRecord.vernacularShortName == String.Empty)
                            {
                                bookRecord.vernacularShortName = bookRecord.vernacularHeader;
                            }
                            if (bookRecord.vernacularShortName == String.Empty)
                            {
                                if (bookRecord.testament == "x")
                                {
                                    bookRecord.vernacularShortName = bookRecord.tla;
                                }
                                else
                                {
                                    bookRecord.vernacularShortName = bookRecord.vernacularLongName;
                                    Logit.WriteError("Missing vernacular short name toc2 or h in " + currentBookAbbrev + " in " + usfxName);
                                }
                            }
                            if (bookRecord.vernacularAbbreviation == String.Empty)
                            {
                                bookRecord.vernacularAbbreviation = bookRecord.vernacularShortName;
                            }
                            if ((bookRecord.IsPresent) && (chapterNumber == 0))
                            {
                                ci = new ChapterInfo();
                                ci.chapterInteger = chapterNumber;
                                ci.alternate = ci.actual = chapterString;
                                ci.published = chapterString;
                                ci.osisChapter = bookOsisId + "." + chapterNumber.ToString();
                                ci.chapterId = currentBookCode + chapterNumber.ToString();
                                ci.maxVerse = ci.verseCount = 0;
                                ci.bookRecord = bookRecord;
                                bookRecord.chaptersFound.Add(ci);
                                allChapters.Add(ci);
                            }
                        }
                        else if (usfx.Name == "p")
                        {
                            inParagraph = false;
                        }
                    }
                    else if ((usfx.NodeType == XmlNodeType.Text) && (bookRecord != null) && (bookRecord.tla == currentBookAbbrev) && (usfx.Value.Trim().Length > 2))
                    {   // We don't count a book as present unless there is some text in it.
                        bookRecord.IsPresent = true;
                    }
                    //conversionProgress = "navigation " + currentBookAbbrev + " " + currentChapter + ":" + currentVerse;
                    //System.Windows.Forms.Application.DoEvents();
                }
                usfx.Close();
                WriteDefaultBookNames(bookNamesFile);   // Write book names with possible additions from toc tags
            }
            catch (Exception ex)
            {
                Logit.WriteError("Error reading vernacular file names from USFX: " + ex.Message + "  " + ex.StackTrace);
            }
        }

        /// <summary>
        /// Splits Book.chapter.verse string up and validates to see if it exists in this translation.
        /// </summary>
        /// <param name="BCV">Input string, like JHN.3.16</param>
        /// <returns>Information about the normalized Book Chapter and Verse information</returns>
        public BCVInfo ValidateInternalReference(string BCV)
        {
            BCVInfo result = new BCVInfo();
            BCV = BCV.Trim();
            if (BCV.Length < 7)
                return result;
            string[] sa = BCV.Split(new char[] { '.' });
            if (sa.Length < 3)
                return result;
            return ValidateInternalReference(sa[0], sa[1], sa[2]);
        }

        /// <summary>
        /// Check to see if the book, chapter, and verse given exist in this translation, and if so,
        /// normalize the reference to the first verse of the target verse bridge.
        /// </summary>
        /// <param name="bookTla">Book TLA, like JHN</param>
        /// <param name="chap">Chapter string, like 3</param>
        /// <param name="vs">Verse string, 16</param>
        /// <returns></returns>
        public BCVInfo ValidateInternalReference(string bookTla, string chap, string vs)
        {
            BCVInfo result = new BCVInfo();
            result.exists = false;
            int i, j, k;
            int vnum;
            int chapNum;
            if (bookTla.Length != 3)
            {
                return result;
            }
            if (chap.Length < 1)
            {
                return result;
            }
            else if (!Int32.TryParse(chap.Trim(), out chapNum))
            {
                return result;
            }
            vs = vs.Trim();
            int dashPlace = vs.IndexOf('-');
            if (dashPlace > 0)
            {
                vs = vs.Substring(0, dashPlace);
            }
            if (vs.Length < 1)
            {
                vnum = 0;
            }
            else if (!Int32.TryParse(vs, out vnum))
            {
                return result;
            }
            if ((vnum < 0) || (vnum >= ChapterInfo.MAXNUMVERSES))
                return result;
            BibleBookRecord foundBr;
            ChapterInfo foundCi;
            VerseInfo foundVi;
            // Is the book in publishArray?
            bool found = false;
            for (i = 0; (i < publishArray.Length) && !found; i++)
            {
                if (publishArray[i] != null)
                {
                    foundBr = publishArray[i];
                    if (foundBr.tla == bookTla)
                    {
                        result.bkInfo = foundBr;
                        if (foundBr.chapterFiles == null)
                            return result;
                        // We found the book and chapter list.
                        for (j = 0; (j < foundBr.chaptersFound.Count) && !found; j++)
                        {
                            foundCi = (ChapterInfo)foundBr.chaptersFound[j];
                            if (foundCi != null)
                            {
                                if ((foundCi.chapterInteger == chapNum) || (chapNum == 0))
                                {   // We found the chapter.
                                    result.chapInfo = foundCi;
                                    k = 0;
                                    foundVi = foundCi.verses[k];
                                    while ((foundVi != null) && (foundVi.verseMarker != vs) && (k <= foundCi.maxVerse))
                                    {
                                        k++;
                                    }
                                    if (foundVi == null)
                                    {
                                        return result;  // No such verse in this translation.
                                    }
                                    result.vsInfo = foundVi;
                                    result.exists = true;
                                    found = true;
                                    // We found it! Normalize to first verse of verse bridge.
                                }
                            }
                        }
                        if (!found)
                            return result;
                    }
                }
            }
            return result;
        }

        public void RecordStats(Options m_options)
        {
            int i, j;
            int otBookCount = 0;
            int ntBookCount = 0;
            int adBookCount = 0;
            int pBookCount = 0;
            int otChapCount = 0;
            int ntChapCount = 0;
            int adChapCount = 0;
            int otVerseCount = 0;
            int ntVerseCount = 0;
            int adVerseCount = 0;
            int otVerseMax = 0;
            int ntVerseMax = 0;
            int adVerseMax = 0;
            BibleBookRecord br;
            ChapterInfo ci;
            for (i = 0; (i < publishArray.Length) && (publishArray[i] != null); i++)
            {
                br = (BibleBookRecord)publishArray[i];
                if (br.IsPresent)
                {
                    switch (br.testament)
                    {
                        case "o":
                            otBookCount++;
                            if (br.chaptersFound != null)
                            {
                                otChapCount += br.chaptersFound.Count;
                                for (j = 0; j < br.chaptersFound.Count; j++)
                                {
                                    ci = (ChapterInfo)br.chaptersFound[j];
                                    if (ci != null)
                                    {
                                        otVerseCount += ci.verseCount;
                                        otVerseMax += ci.maxVerse;
                                    }
                                }
                            }
                            break;
                        case "n":
                            ntBookCount++;
                            ntChapCount += br.chaptersFound.Count;
                            for (j = 0; j < br.chaptersFound.Count; j++)
                            {
                                ci = (ChapterInfo)br.chaptersFound[j];
                                if (ci != null)
                                {
                                    ntVerseCount += ci.verseCount;
                                    ntVerseMax += ci.maxVerse;
                                }
                            }
                            break;
                        case "x":
                            pBookCount++;
                            break;
                        default:    // Testament = "a" or one of the other Apocrypha designations, like "Septuagint", "Vulgate", "Orthodox Canon"
                            adBookCount++;
                            adChapCount += br.chaptersFound.Count;
                            for (j = 0; j < br.chaptersFound.Count; j++)
                            {
                                ci = (ChapterInfo)br.chaptersFound[j];
                                if (ci != null)
                                {
                                    adVerseCount += ci.verseCount;
                                    adVerseMax += ci.maxVerse;
                                }
                            }
                            break;
                    }
                }
            }
            m_options.otBookCount = otBookCount;
            m_options.ntBookCount = ntBookCount;
            m_options.adBookCount = adBookCount;
            m_options.pBookCount = pBookCount;
            m_options.otChapCount = otChapCount;
            m_options.ntChapCount = ntChapCount;
            m_options.adChapCount = adChapCount;
            m_options.otVerseCount = otVerseCount;
            m_options.ntVerseCount = ntVerseCount;
            m_options.adVerseCount = adVerseCount;
            m_options.otVerseMax = otVerseMax;
            m_options.ntVerseMax = ntVerseMax;
            m_options.adVerseMax = adVerseMax;
        }

    }

}
