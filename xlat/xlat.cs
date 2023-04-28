using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WordSend
{
    class xlatProgram
    {
        static void Main(string[] args)
        {
            bool showBanner = true;
            Logit.useConsole = true;
            if (args.Length >= 3)
            {
                try
                {
                    usfxToHtmlConverter conv = new usfxToHtmlConverter();
                    Console.WriteLine("Calling conv.FilterUsfx({0},{1},{2})", args[0], args[1], args[2]);
                    conv.FilterUsfx(args[0], args[1], args[2], (args.Length >= 4) && (args[3] == "-a"));
                    showBanner = false;
                    Console.WriteLine("{0} written.", args[1]);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("ERROR IN "+ex.Source);
                    Console.WriteLine(ex.ToString());
                }
            }
            if (showBanner)
            {
                Console.WriteLine(@"
Syntax:
xlat.exe infile.xml outfile.xml globalsubst.txt localsubst.txt [-a]
infile.xml is the USFX file to read
outfile.xml is the USFX file to write
localsubst.txt is the text file with substitutions to make at specific book,
  chapter, verse locations
localsubst.txt contains one line per substitution, with fields separated by
whatever character is first on the line. Local substitutions have 7 fields:
book abbreviation, chapter number, verse number, N if in footnote,
find text, replace text, and optional comment.
If the Apocrypha is to be included, -a must be the 4th command line parameter.
");
            }
        }
    }
}
