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
        public string osisChapter; // OSIS ID of this book and chapter, i.e. Gen.3
        public string alternate;    // Alternate chapter number
    }
}
