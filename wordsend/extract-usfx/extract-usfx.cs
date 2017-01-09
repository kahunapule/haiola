using System;
using System.IO;
using System.Collections;
using System.Text;
using System.Xml;
using System.Diagnostics;

namespace WordSend
{
	/// <summary>
	/// Summary description for Class1.
	/// </summary>
	class Class1
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] args)
		{
			bool showBanner = true;
			bool showHelp = false;
			string logName = "WordSendLog.txt";
			string inName = "";
			string outName = "";
			int i;
			string jobOptionsName = Environment.GetEnvironmentVariable("APPDATA")+
				"\\SIL\\WordSend\\joboptions.xml";
			SFConverter.jobIni = new XMLini(jobOptionsName);
			Logit.useConsole = true;
			for (i = 0; i < args.Length; i++)
			{	// Scan the command line
				string s = args[i];
				if ((s != null) && (s.Length > 0))
				{
					if (((s[0] == '-') || (s[0] == '/')) && (s.Length > 1))
					{	// command line switch: take action
						switch (Char.ToLower(s[1]))
						{
							case 'n':	// No banner display
								showBanner = false;
								break;
							case 'o':	// Set output file name
								outName = SFConverter.GetOption(ref i, args);
								break;
							case 'i':	// Set input file name
								inName = SFConverter.GetOption(ref i, args);
								break;
							case 'l':	// Set log name
								logName = SFConverter.GetOption(ref i, args);
								break;
							case '?':
							case 'h':
							case '-':
								showBanner = true;
								showHelp = true;
								break;
							default:
								Logit.WriteLine("Unrecognized command line switch: " + args[i]);
								break;
						}
					}
					else
					{
						if (inName == "")
						{
							inName = s;
						}
						else
						{
							if (outName == "")
							{
								outName = s;
							}
							else
							{
								showBanner = true;
								showHelp = true;
							}
						}
					}
				}
			}
			SFConverter.scripture = new Scriptures();
			Logit.OpenFile(logName);
			if (showBanner)
			{
				Logit.WriteLine("\nWordSend project extract_usfx compiled " + Version.date);
				Logit.WriteLine("");
				Logit.WriteLine(Version.copyright);
				Logit.WriteLine("");
				Logit.WriteLine(Version.contact);
			}
			if ((outName.Length < 1) || (inName.Length < 1))
				showHelp = true;
			if (showHelp)
			{
				Logit.WriteLine(@"
Syntax:
sf2word [-l logname] [-h] [-n] [-i] inputfile [-o] outputfile
 logname = log file name (default is WordSendLog.txt)
 -h = show this information then exit
 -n = supress banner
 inputfile = Microsoft Word XML file that has embedded USFX
 outputfile = name of USFX file to write

");
				Logit.CloseFile();
				return;
			}
			Logit.WriteLine("Auxilliary files read.");
			SFConverter.scripture.ExtractUSFX(inName, outName);
			Logit.WriteLine("Done.");
			Logit.CloseFile();

		}
	}
}
