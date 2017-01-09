using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections;
using System.Windows.Forms;


namespace unbound2sfm
{
    class Program
    {
        static void Main(string[] args)
        {
            StreamReader sr;
            StreamWriter sw = null;
            string line, bookCode, chapter, verse, verseText;
            string bookID = String.Empty;
            string lastBook = String.Empty;
            string lastChapter = String.Empty;
            string inFileName = "afrikaans_1953_utf8.txt";
            string[] verseParts;
            char[] tabSeparator = new char[] {'\t'};
            Hashtable bkcodes = new Hashtable();
            try
            {
                // Read in unbound to SIL book designator list
                sr = new StreamReader(Path.Combine(Path.GetDirectoryName(Application.ExecutablePath),"unbound_book_names.txt"));
                line = sr.ReadLine();
                while (line != null)
                {
                    if ((line.Length > 6) && !line.StartsWith("#"))
                    {
                        bookCode = line.Substring(0, 3);
                        bookID = line.Substring(4, 3);
                        bkcodes.Add(bookCode, bookID);
                        // Console.WriteLine("Code = '{0}'  ID = '{1}'", bookCode, bookID);
                    }
                    line = sr.ReadLine();
                }
                sr.Close();

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
                        verseParts = line.Split(tabSeparator);
                        bookCode = verseParts[0];
                        chapter = verseParts[1];
                        verse = verseParts[2];
                        verseText = verseParts[3];
                        if (bookCode != lastBook)
                        {
                            if (sw != null)
                                sw.Close();
                            bookID = (string)bkcodes[bookCode];
                            sw = new StreamWriter(bookID + ".sfm", false, Encoding.UTF8);
                            sw.WriteLine("\\id {0}", bookID);
                            lastBook = bookCode;
                            lastChapter = String.Empty;
                            Console.Write("{0} ", bookID);
                        }
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
                Console.WriteLine("unbound2sfm done");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }
    }
}
