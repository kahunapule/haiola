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

    public class ChapterInfo
    {
        public const int MAXNUMVERSES = 301;    // Maximum number of verses in a chapter + 1; larger number allows for non-Scripture texts in "extra" books
        public int chapterInteger; // Chapter number as an integer
        public string actual;  // Monotonically increasing integer as a string
        public string published;   // Published chapter number/name
        public string osisChapter; // OSIS ID of this book and chapter, i.e. John.3
        public string chapterId;    // Book and chapter ID using 2-character book code and chapter i.e. PS119 or JN3.
        public string alternate;    // Alternate chapter number
        public int maxVerse;    // Highest verse number actually found in the chapter
        public int verseCount;  // Number of verse markers actually found
        public VerseInfo[] verses = new VerseInfo[MAXNUMVERSES];
        public BibleBookRecord bookRecord; // Pointer to information about the book this chapter resides in.
    }

    public class VerseInfo
    {
        public int startVerse;  // First or only verse of verse range
        public int endVerse;    // Last or only verse of verse range
        public string verseMarker;  // This is the "name" of the verse, which normally is the string corresponding to startVerse, but may be something like "6a", "1-2", or "3b-4a".
        public string verse;    // String verse as given in verse marker
    }

    public class BCVInfo
    {
        public bool exists;
        public BibleBookRecord bkInfo;
        public ChapterInfo chapInfo;
        public VerseInfo vsInfo;
    }
}
