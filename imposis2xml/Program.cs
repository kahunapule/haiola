using System;
using System.IO;
using System.Collections;
using System.Text;
using System.Xml;
using System.Diagnostics;

namespace WordSend
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Logit.useConsole = true;
            RecoverOsisData oz;
            if (args.Length < 2)
            {
                Console.WriteLine(@"Syntax:
imposis2xml.exe infile xmlfile [logname]
  infile = file written by mod2imp.exe or an OSIS file like the KJV2006 example
  xmlfile = xml file for further parsing
  logname = name of log text file");
                
                oz = new RecoverOsisData();
                oz.readImpOsis(@"C:\Users\Kahunapule\Documents\tmp\Wycliffe.imp", @"C:\Users\Kahunapule\Documents\tmp\Wycliffe.usfx");
                Logit.CloseFile();
            }
            else
            {
                if (args.Length >= 3)
                {
                    Logit.OpenFile(args[2]);
                }
                Console.WriteLine("imposis2xml " + args[0] + " " + args[1]);
                oz = new RecoverOsisData();
                oz.readImpOsis(args[0], args[1]);
                Logit.CloseFile();
            }
        }
    }
}
