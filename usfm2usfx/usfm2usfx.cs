using System;
using System.IO;
using System.Collections;
using System.Text;
using System.Xml;
using System.Diagnostics;

namespace WordSend
{
	/// <summary>
	/// Command line conversion of USFM to USFX files.
	/// </summary>
	/// <summary>
	/// The main reason for the exsistence of MainEntry class is to give the Main
	/// function a nice place to live.
	/// </summary>
	class MainEntry
	{

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[STAThread]
		static void Main(string[] args)
		{
			int i;
			bool showBanner = true;
			string logName = "usfm2usfxlog.txt";
			string outName = "";

			ArrayList fileSpecs = new ArrayList(127);
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
					else
					{
						fileSpecs.Add(args[i]);
					}
				}
			}
			Logit.OpenFile(logName);
			if (showBanner)
			{
                Logit.WriteLine("This is part of Haiola open source software.");
                Logit.WriteLine("Please see http://haiola.org for copyright information.");
                Logit.WriteLine(@"
Syntax:
usfm2usfx [-o Output] [-n] [-l logname] [-?] filespec(s)
 Output = output USFX file name
 -n = don't display copyright and banner information.
 logname = log file name (default is usfm2usfxlog.txt)
 -? = cancel previous /n and show this information.
 filespec = SFM file specification(s) to read. Wild cards are OK.
You may use / instead of - to introduce switches. Do not use
either of those two characters as the first character of a file name.

");
			}

			if (outName == "")
				outName = "output.usfx.xml";
			if (fileSpecs.Count < 1)
			{
				Logit.WriteLine("Nothing to do. No input files specified.");
			}
			else
			{
				// Instantiate the object that does most of the work.
				SFConverter.scripture = new Scriptures();

				// Read the input USFM files into internal data structures.
				for (i = 0; i < fileSpecs.Count; i++)
					SFConverter.ProcessFilespec((string) fileSpecs[i]);

				// Write out the USFX file.
				SFConverter.scripture.WriteUSFX(outName);
			}
		}
	}
}
