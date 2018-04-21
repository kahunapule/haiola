using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;
using System.IO;
using System.Collections;
using System.Xml;

namespace WordSend
{
    public class CreateIndex
    {
        BibleBookInfo bookInfo;
        protected XmlTextReader searchTextXml;
        protected string currentBook;
        protected string currentChapter;
        protected string currentVerse;
        protected string startVerse;
        protected string verseID;
        protected StringBuilder word;
        protected Hashtable wordCollection;
        protected HashSet<string> includedVerses;

        const int LEMMASIZE = 15;

        /// <summary>
        /// Initialize variables
        /// </summary>
        public CreateIndex()
        {
            currentBook = String.Empty;
            currentChapter = String.Empty;
            currentVerse = String.Empty;
            verseID = String.Empty;
            word = new StringBuilder();
            bookInfo = new BibleBookInfo();
        }

        /// <summary>
        /// Add a verse to the string for a given Strong's number
        /// </summary>
        protected void AddWordToLemma()
        {
            string verseList;
            string w;
            if (word.Length > 0)
            {
                w = word.ToString().ToUpper(CultureInfo.InvariantCulture);
                verseList = (string)wordCollection[w];
                if (verseList == null)
                {
                    verseList = verseID;
                    wordCollection[w] = verseList;
                }
                else
                {
                    if (!verseList.EndsWith(verseID))
                    {
                        verseList += "," + verseID;
                        wordCollection[w] = verseList;
                    }
                }
                word.Length = 0;
            }
        }

        /// <summary>
        /// Figure out which lemma index file to write to.
        /// </summary>
        /// <param name="theWord">Strong's number (starting with capital H or G)</param>
        /// <returns>index into array of files</returns>
        protected int HashLemma(string theWord)
        {
            int hash = 0;

            while ((!Char.IsDigit(theWord[theWord.Length - 1])) && (theWord.Length > 2))
            {
                theWord = theWord.Substring(0, theWord.Length - 1);
            }
            if (!Int32.TryParse(theWord.Substring(1), out hash))
                return 0;
            hash = hash / 1000;
            if (theWord[0] == 'H')
                hash += 6;
            if (hash >= HASHSIZE)
            {
                Logit.WriteError("Bad Strong's number: " + theWord);
                hash = 0;
            }
            return hash;
        }


        /// <summary>
        /// Create an index of Strong's numbers (corresponding to the lemma or root word lexicon entry number).
        /// NOTE: Call MakeJsonIndex immediately before calling MakeLemmaIndex.
        /// </summary>
        /// <param name="lemmaTextFile"></param>
        /// <param name="lemmaDir"></param>
        public void MakeLemmaIndex(string lemmaTextFile, string lemmaDir)
        {
            string oneWord;
            string bookCode;
            searchTextXml = new XmlTextReader(lemmaTextFile);
            wordCollection = new Hashtable(19999);
            StreamWriter[] lemmaFiles;
            int i, j, lineLength;
            char ch;
            char defaultSourceLanguage = 'H';
            Utils.EnsureDirectory(lemmaDir);
            BibleBookRecord br;

            try
            {
                // Read all references to Strong's numbers into wordCollection hash table.
                while (searchTextXml.Read())
                {
                    if ((searchTextXml.NodeType == XmlNodeType.Element) && (searchTextXml.Name == "v"))
                    {
                        bookCode = fileHelper.GetNamedAttribute(searchTextXml, "b");
                        currentBook = bookInfo.getShortCode(bookCode);
                        br = (BibleBookRecord)bookInfo.books[bookCode];
                        if (br.testament == "o")
                            defaultSourceLanguage = 'H';
                        else
                            defaultSourceLanguage = 'G';
                        currentChapter = fileHelper.GetNamedAttribute(searchTextXml, "c");
                        startVerse = currentVerse = fileHelper.GetNamedAttribute(searchTextXml, "v");
                        // Verse numbers might be verse bridges, like "20-22" or simple numbers, like "20".
                        i = currentVerse.IndexOf('-');
                        if (i > 0)
                        {
                            startVerse = startVerse.Substring(0, i);
                        }
                        verseID = currentBook + currentChapter + "_" + startVerse;
                        if (!Logit.ShowStatus("Creating lemma index " + verseID))
                        {
                            searchTextXml.Close();
                            return;
                        }
                        searchTextXml.Read();
                        if (includedVerses.Contains(verseID) && (searchTextXml.NodeType == XmlNodeType.Text))
                        {
                            string s = searchTextXml.Value;
                            for (i = 0; i < s.Length; i++)
                            {
                                if (!Char.IsWhiteSpace(s[i]))
                                {
                                    if (word.Length == 0)
                                    {
                                        if (Char.IsDigit(s[i]))
                                            word.Append(defaultSourceLanguage);
                                    }
                                    word.Append(s[i]);
                                }
                                else
                                {
                                    AddWordToLemma();
                                }
                            }
                            AddWordToLemma();
                        }
                    }
                }
                searchTextXml.Close();

                // Write search index with fewer files.
                bool[] commaNeeded = new bool[LEMMASIZE];   // Boolean variables are created with value "false"

                lemmaFiles = new StreamWriter[LEMMASIZE];
                char srcLang = 'G';
                for (i = 0, j=0; i < LEMMASIZE; i++, j++)
                {
                    if (i == 6)
                    {
                        srcLang = 'H';
                        j = 0;
                    }
                    lemmaFiles[i] = new StreamWriter(Path.Combine(lemmaDir, "_" + srcLang + j.ToString() + "000.json"), false, Encoding.UTF8);
                    lemmaFiles[i].Write("{\n");
                }

                // Also write combined search index for web server use
                //wordLocationFile = new StreamWriter(Path.Combine(searchDir, "search.json"));
                //wordLocationFile.WriteLine("{");

                foreach (DictionaryEntry de in wordCollection)
                {
                    oneWord = (string)de.Key;
                    int hash = HashLemma(oneWord);
                    string longString = (string)de.Value;
                    sqlConcordance.WriteLine("INSERT INTO {0} VALUES (\"{1}\",\"{2}\");", concTableName, oneWord, longString);
                    StringBuilder sb = new StringBuilder();
                    lineLength = 26 + oneWord.Length;
                    for (i = 0; i < longString.Length; i++)
                    {
                        ch = longString[i];
                        if (ch == ',')
                        {
                            sb.Append("\",");
                            lineLength += 2;
                            if (lineLength > 100)
                            {
                                sb.Append("\n");
                                lineLength = 0;
                            }
                            sb.Append("\"");
                            lineLength++;
                        }
                        else
                        {
                            sb.Append(ch);
                            lineLength++;
                        }
                    }

                    if (commaNeeded[hash])
                    {
                        lemmaFiles[hash].Write(",\n");
                    }
                    lemmaFiles[hash].Write("\"{0}\":[\"{1}\"]", oneWord, sb.ToString());
                    commaNeeded[hash] = true;

                    if (!Logit.ShowStatus("Writing lemma index " + oneWord))
                    {
                        return;
                    }
                }
                for (i = 0; i < LEMMASIZE; i++)
                {
                    lemmaFiles[i].Write("}\n");
                    lemmaFiles[i].Close();
                }
                sqlConcordance.WriteLine("UNLOCK TABLES;");
                sqlConcordance.Close();
            }
            catch (Exception ex)
            {
                Logit.WriteError(ex.Message);
            }
        }


        /// <summary>
        /// Add an lower case version of the current word to the collection of words.
        /// </summary>
        protected void AddWordToIndex()
        {
            string verseList;
            string w;
            if (word.Length > 0)
            {
                w = word.ToString().ToLower(CultureInfo.InvariantCulture);
                verseList = (string)wordCollection[w];
                if (verseList == null)
                {
                    verseList = verseID;
                    wordCollection[w] = verseList;
                }
                else
                {
                    if (!verseList.EndsWith(verseID))
                    {
                        verseList += "," + verseID;
                        wordCollection[w] = verseList;
                    }
                }
                word.Length = 0;
            }
        }

        /// <summary>
        /// Index all of the words in a verse.
        /// </summary>
        protected void IndexWords(string s)
        {
            int i;
            for (i = 0; i < s.Length; i++)
            {
                if (!(Char.IsSeparator(s[i]) || Char.IsSymbol(s[i]) || Char.IsWhiteSpace(s[i]) || Char.IsPunctuation(s[i])))
                {
                    word.Append(s[i]);
                    uint c = (uint)s[i];
                    if (((c >= 0x4E00) && (c <= 0x9FFF)) || ((c >= 0x3400) && (c <= 0x4DFF)) || ((c >= 0x20000) && (c <= 0x2A6DF)))
                    {   // If this is a Chinese/Japanese/Korean ideograph, it is a word by itself. No separator is needed.
                        AddWordToIndex();   // Technically, some ideographs combine to make a compound word, but concordance/search will work without that refinement, possibly with extra hits.
                    }
                }
                else if ((i < s.Length - 1) && (i > 0) && Char.IsLetter(s[i - 1]) && Char.IsLetter(s[i + 1]) && ((s[i] == '-') || (s[i] == '\'') || (s[i] == '’')))
                {   // Count apostrophe, single right quote, or dash as word-forming character if surrounded by a letter on each side
                    word.Append(s[i]);
                }
                else
                {
                    AddWordToIndex();
                }
            }
            AddWordToIndex();
        }
 
        const int HASHSIZE = 20;

        /// <summary>
        /// Spread the words over HASHSIZE files, more or less evenly. This function must match that in the
        /// Browser Bible JavaScript.
        /// </summary>
        /// <param name="theWord">word to hash</param>
        /// <returns>integer that is at least 0 and less than HASHSIZE</returns>
        public int HashWord(string theWord)
        {
            int i;
            int hash = 0;
            for (i = 0; i < theWord.Length; i++)
            {
                hash += (int)(theWord[i]);
                hash %= HASHSIZE;
            }
            return hash;
        }

        StreamWriter sqlConcordance;
        string concTableName;

        /// <summary>
        /// Create an index file to speed searches in Browser Bible
        /// </summary>
        /// <param name="verseTextFile">Name of XML file with normalized search text by verse.</param>
        /// <param name="searchDir">Name of directory to write search files into.</param>
        /// <parame name="sqlFile">Name of the SQL file to create.</parame>
        public void MakeJsonIndex(string verseTextFile, string searchDir, string sqlFile)
        {
            string oneWord;
            searchTextXml = new XmlTextReader(verseTextFile);
            wordCollection = new Hashtable(400009);
            //StreamWriter wordLocationFile;
            StreamWriter[] wordFiles;
            base32string b32 = new base32string();
            int i, lineLength;
            char ch;
            Utils.EnsureDirectory(searchDir);
            includedVerses = new HashSet<string>();
            sqlConcordance = new StreamWriter(sqlFile, false, Encoding.UTF8);
            concTableName = Path.GetFileNameWithoutExtension(sqlFile);

            // Write SQL file preamble
            sqlConcordance.WriteLine(@"USE sofia;
DROP TABLE IF EXISTS sofia.{0};
CREATE TABLE {0} (
    keyWord VARCHAR(128) COLLATE UTF8_GENERAL_CI NOT NULL,
    verseList TEXT NOT NULL) ENGINE=MyISAM;
LOCK TABLES {0} WRITE;", concTableName);


            // Read the verse list
            while (searchTextXml.Read())
            {
                if ((searchTextXml.NodeType == XmlNodeType.Element) && (searchTextXml.Name == "v"))
                {
                    currentBook = bookInfo.getShortCode(fileHelper.GetNamedAttribute(searchTextXml, "b"));
                    currentChapter = fileHelper.GetNamedAttribute(searchTextXml, "c");
                    startVerse = currentVerse = fileHelper.GetNamedAttribute(searchTextXml, "v");
                    // Verse numbers might be verse bridges, like "20-22" or simple numbers, like "20".
                    i = currentVerse.IndexOf('-');
                    if (i > 0)
                    {
                        startVerse = startVerse.Substring(0, i);
                    }
                    verseID = currentBook + currentChapter + "_" + startVerse;
                    if (!Logit.ShowStatus("Creating word index " + verseID))
                    {
                        searchTextXml.Close();
                        return;
                    }
                    searchTextXml.Read();
                    if (searchTextXml.NodeType == XmlNodeType.Text)
                    {
                        if (searchTextXml.Value.Trim().Length > 0)
                        {
                            includedVerses.Add(verseID);
                        }
                        IndexWords(searchTextXml.Value);
                    }
                }
            }
            searchTextXml.Close();

            // Write search index with fewer files.
            bool[] commaNeeded = new bool[HASHSIZE];
            //bool needComma = false;

            wordFiles = new StreamWriter[HASHSIZE];
            for (i = 0; i < HASHSIZE; i++)
            {
                wordFiles[i] = new StreamWriter(Path.Combine(searchDir, "_" + i.ToString() + ".json"));
                wordFiles[i].Write("{\n");
            }

            foreach (DictionaryEntry de in wordCollection)
            {
                oneWord = (string)de.Key;
                if (oneWord.Length > 0)
                {
                    int hash = HashWord(oneWord);
                    string longString = (string)de.Value;
                    sqlConcordance.WriteLine("INSERT INTO {0} VALUES (\"{1}\",\"{2}\");", concTableName, oneWord, longString);
                    StringBuilder sb = new StringBuilder();
                    lineLength = 26 + oneWord.Length;
                    for (i = 0; i < longString.Length; i++)
                    {
                        ch = longString[i];
                        if (ch == ',')
                        {
                            sb.Append("\",");
                            lineLength += 2;
                            if (lineLength > 100)
                            {
                                sb.Append("\n");
                                lineLength = 0;
                            }
                            sb.Append("\"");
                            lineLength++;
                        }
                        else
                        {
                            sb.Append(ch);
                            lineLength++;
                        }
                    }
                    if (Char.IsLetter(oneWord[0]))
                    {
                        if (commaNeeded[hash])
                        {
                            wordFiles[hash].Write(",\n");
                        }
                        wordFiles[hash].Write("\"{0}\":[\"{1}\"]", oneWord, sb.ToString());
                        commaNeeded[hash] = true;
                    }
                    if (!Logit.ShowStatus("Writing word index " + oneWord))
                    {
                        return;
                    }
                }
            }
            for (i = 0; i < HASHSIZE; i++)
            {
                wordFiles[i].Write("}\n");
                wordFiles[i].Close();
            }
        }

    }
}
