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
        public int chapterInteger; // Chapter number as an integer
        public string actual;  // Monotonically increasing integer as a string
        public string published;   // Published chapter number/name
        public string osisChapter; // OSIS ID of this book and chapter, i.e. John.3
        public string chapterId;    // Book and chapter ID using SIL/UBS TLA, i.e. JHN.3
        public string alternate;    // Alternate chapter number
        public int maxVerse;    // Highest verse number actually found in the chapter
        public int verseCount;  // Number of verse markers actually found
    }
}
