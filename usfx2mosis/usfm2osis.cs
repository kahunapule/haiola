using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace WordSend
{
    class usfm2osis
    {
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] args)
		{
			int i;
			bool showBanner = false;
			string logName = "usfx2mosislog.txt";
			string inName = "usfx.xml";
			string outName = "mosis.xml";
            string ethnologueCode = String.Empty;
            string translationId = String.Empty;
            Logit.useConsole = true;
            for (i = 0; i < args.Length; i++)
			{	// Scan the command line
				string s = args[i];
				if ((s != null) && (s.Length > 0))
				{
					if ((s[0] == '-') && (s.Length > 1))
					{	// command line switch: take action
						switch (Char.ToLower(s[1]))
						{
							case 'o':	// Set output file name
								outName = SFConverter.GetOption(ref i, args);
								break;
							case 'i':	// Set input file name
								inName = SFConverter.GetOption(ref i, args);
								break;
							case 'l':	// Set log name
								logName = SFConverter.GetOption(ref i, args);
								break;
                            case 't':
                                translationId = SFConverter.GetOption(ref i, args);
                                break;
							case '-':
							case 'h':
							case '?':
                            case '/':
								showBanner = true;
								break;
							default:
								Logit.WriteLine("Unrecognized command line switch: " + args[i]);
								showBanner = true;
								break;
						}
					}
					else if (inName == "")
					{
						inName = args[i];
					}
				}
			}
			Logit.OpenFile(logName);

            if (showBanner)
            {
                Logit.WriteLine("");
                Logit.WriteLine("This is part of Haiola open source software.");
                Logit.WriteLine("Please see http://haiola.org for copyright information.");
                Logit.WriteLine(@"
Syntax:
usfx2usfm [-t translationId] [-o Output] [-?] [-l logname] [-i] [inputfile]
 translationId = unique translation identifier
      default is Ethnologue language code specified in usfx file
 Output = output USFM path and file name suffix
      default is mosis.xml
 logname is name of log file to write, default is usfx2mosislog.txt
 -h or -? = show this information.
 inputfile = name of USFX file to convert to USFM
      default is usfx.xml
File names with embedded spaces must be surrounded by quotes.
Do not use - as the first character of a path or file name.

");
            }
            else
			{
                Logit.WriteLine(DateTime.Now.ToString());
				Logit.WriteLine("Input USFX: " + inName + "; output MOSIS: " + outName);

                usfxToMosisConverter toMosis = new usfxToMosisConverter();

                toMosis.translationId = translationId;
                toMosis.revisionDateTime = DateTime.Now;

                toMosis.languageCode = String.Empty;
                toMosis.vernacularTitle = toMosis.contentCreator = toMosis.contentContributor = String.Empty;
                toMosis.englishDescription = toMosis.lwcDescription = toMosis.printPublisher = String.Empty;
                toMosis.ePublisher = toMosis.languageName = toMosis.dialect = String.Empty;
                toMosis.vernacularLanguageName = toMosis.copyrightNotice = String.Empty;
                toMosis.rightsNotice = String.Empty;
                toMosis.langCodes = new LanguageCodeInfo();
                toMosis.ConvertUsfxToMosis(inName, outName);
            }
		Logit.CloseFile();
        }
    }
}


