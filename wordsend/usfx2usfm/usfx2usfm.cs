using System;
using System.IO;
using System.Text;
using System.Xml;


namespace WordSend
{
	/// <summary>
	/// Summary description for Class1.
	/// </summary>
	class StartHere
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] args)
		{
			int i;
			bool showBanner = false;
			string logName = "WordSendLog.txt";
			string inName = "";
			string outName = ".sfm";
			// string jobOptionsName = Environment.GetEnvironmentVariable("APPDATA")+
			// 	"\\SIL\\WordSend\\joboptions.xml";
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
							case '-':
							case 'h':
							case '?':
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
			Logit.WriteLine("\nWordSend project usfx2usfm compiled " + Version.date);

			if (inName == "")
			{
				showBanner = true;
			}
			else
			{
				Logit.WriteLine("Input file name is " + inName + "; output suffx is " + outName);
				// Something to refactor: make this line not required.
				// SFConverter.jobIni = new XMLini(jobOptionsName);

				// Here we instantiate the object that does most of the work.
				SFConverter.scripture = new Scriptures();

				// Write out the USFM file
				SFConverter.scripture.USFXtoUSFM(inName, Path.GetDirectoryName(outName), Path.GetFileName(outName));
			}
			if (showBanner)
			{
				Logit.WriteLine("");
				Logit.WriteLine(Version.copyright);
				Logit.WriteLine("");
				Logit.WriteLine(Version.contact);
				Logit.WriteLine(@"
Syntax:
usfx2usfm [-o Output] [-?] [-l logname] [-i] inputfile
 Output = output USFM path and file name suffix
      default is .sfm
      book code will be added, i. e. pdg.sfm -> MATpdg.sfm
      and subdir\eng.sfm -> subdir\MATeng.sfm
 logname is name of log file to write, default is WordSendLog.txt
 -h or -? = show this information.
 inputfile = name of USFX file to convert to USFM
File names with embedded spaces must be surrounded by quotes.
Do not use - as the first character of a path or file name.

");
				Logit.CloseFile();
			}
		}
	}
}
