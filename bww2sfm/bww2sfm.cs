using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections;
using System.Windows.Forms;
using System.Text.RegularExpressions;


namespace WordSend
{
    class Program
    {
        static void Main(string[] args)
        {
            BibleBookInfo bkInfo = new BibleBookInfo();
            StreamReader sr;
            StreamWriter sw = null;
            string line, bookCode, chapter, verse, verseText, s;
            string bookID = String.Empty;
            string lastBook = String.Empty;
            string lastChapter = String.Empty;
            string testament = "o";
            string inFileName = "luo.txt";
            string findStrongMorph = @"(\w*) <(\d*)> \((\d*)\)";
            string replaceStrongMorphOT = @"\zw \+zws H$2\+zws*\+zwm strongMorph:TH$3\+zwm*\zw*$1\zx \zx*";
            string replaceStrongMorphNT = @"\zw \+zws G$2\+zws*\+zwm strongMorph:TG$3\+zwm*\zw*$1\zx \zx*";
            string findStrongOnly = @"(\w*) <(\d*)>";
            string replaceStrongOnlyOT = @"\zw \+zws H$2\+zws*\zw*$1\zx \zx*";
            string replaceStrongOnlyNT = @"\zw \+zws G$2\+zws*\zw*$1\zx \zx*";
            string findMorphOnly = @"(\w*) \((\d*)\)";
            string replaceMorphOnlyOT = @"\zw \+zwm strongMorph:TH$2\+zwm*\zw*$1\zx \zx*";
            string replaceMorphOnlyNT = @"\zw \+zwm strongMorph:TG$2\+zwm*\zw*$1\zx \zx*";
            string findBackwardsStrong = @"<(\d*)> (\w*)";
            string replaceBackwardsStrongOT = @"\zw \+zws H$1\+zws*\zw*$2\zx \zx*";
            string replaceBackwardsStrongNT = @"\zw \+zws G$1\+zws*\zw*$2\zx \zx*";
            string findStrayStrongs = @"<\d*>";
            string replaceStrayStrongs = String.Empty;
            string findFootNoteKeyWords = @"\{(.*)<i>(.*)</i>(.*)\}";
            string replaceFootNoteKeyWords = @"{$1\fk $2\ft $3}";
            string findFootNote = @"\ { \w\w\w (\d*:\d*)(.*) \}";
            string replaceFootNote = @"\f + \fr $1 \ft $2\f*";
            BibleBookRecord br;

            char[] tabSeparator = new char[] { '\t' };
            Hashtable bkcodes = new Hashtable();
            int i, j;
            try
            {
                // Get the name of our input file
                if (args.Length > 0)
                    inFileName = args[0];

                // Read in book text and write simple USFM
                sr = new StreamReader(inFileName);
                line = sr.ReadLine();
                while (line != null)
                {
                    if ((line.Length > 8) && !line.StartsWith("#"))
                    {
                        bookCode = line.Substring(0, 3);
                        i = line.IndexOf(':');
                        chapter = line.Substring(4, i-4);
                        j = line.IndexOf(' ', i + 1);
                        verse = line.Substring(i + 1, j - i - 1);
                        verseText = line.Substring(j + 1);
                        if (bookCode != lastBook)
                        {
                            if (sw != null)
                                sw.Close();
                            bookID = bkInfo.tlaFromBW(bookCode);
                            br = bkInfo.BkRec(bookID);
                            if (br != null)
                            {
                                testament = bkInfo.BkRec(bookID).testament;
                            }
                            sw = new StreamWriter(bookID + ".sfm", false, Encoding.UTF8);
                            sw.WriteLine("\\id {0}", bookID);
                            lastBook = bookCode;
                            lastChapter = String.Empty;
                            Console.Write("{0} ", bookID);
                        }
                        if (testament == "o")
                        {
                            verseText = Regex.Replace(verseText, findStrongMorph, replaceStrongMorphOT);
                            verseText = Regex.Replace(verseText, findStrongOnly, replaceStrongOnlyOT);
                            verseText = Regex.Replace(verseText, findMorphOnly, replaceMorphOnlyOT);
                            verseText = Regex.Replace(verseText, findBackwardsStrong, replaceBackwardsStrongOT);
                        }
                        else if (testament == "n")
                        {
                            verseText = Regex.Replace(verseText, findStrongMorph, replaceStrongMorphNT);
                            verseText = Regex.Replace(verseText, findStrongOnly, replaceStrongOnlyNT);
                            verseText = Regex.Replace(verseText, findMorphOnly, replaceMorphOnlyNT);
                            verseText = Regex.Replace(verseText, findBackwardsStrong, replaceBackwardsStrongNT);
                        }
                        verseText = Regex.Replace(verseText, findStrayStrongs, replaceStrayStrongs);
                        do
                        {
                            s = verseText;
                            verseText = Regex.Replace(verseText, findFootNoteKeyWords, replaceFootNoteKeyWords);
                        }
                        while (s != verseText);
                        verseText = Regex.Replace(verseText, findFootNote, replaceFootNote);
                        verseText = Regex.Replace(verseText, @"<i>", @"\it ");
                        verseText = Regex.Replace(verseText, @"</i>", @" \it*");
                        verseText = Regex.Replace(verseText, @"\(\d*:\d*\)", String.Empty); // Alternate versification markers-- ignore for now

                        if (chapter != lastChapter)
                        {
                            sw.WriteLine("\\c {0}", chapter);
                            lastChapter = chapter;
                            if (bookID != "PSA")
                                sw.WriteLine("\\p");
                        }
                        if (bookID == "PSA")
                            sw.WriteLine("\\q1");
                        sw.WriteLine("\\v {0} {1}", verse, verseText);
                    }
                    line = sr.ReadLine();
                }
                Console.WriteLine();
                sw.Close();
                sr.Close();
                Console.WriteLine("bww2sfm done");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }

        }
    }
}
