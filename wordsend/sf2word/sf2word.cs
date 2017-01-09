// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2004, SIL International. All Rights Reserved.   
// <copyright from='2004' to='2004' company='SIL International'>
//		Copyright © 2004, SIL International. All Rights Reserved.   
//    
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
// File: sf2word.cs
// Responsibility: (Kahunapule) Michael P. Johnson
// Last reviewed: 
// 
// <remarks>
// This file is contains the Main function for the command line version
// of the USFM to WordML converter. Most of the inner workings of the
// WordSend Bible format conversion project are in the external DLL
// BibleFileLib.dll.
// </remarks>
// --------------------------------------------------------------------------------------------
#define TRACE

using System;
using System.IO;
using System.Collections;
using System.Text;
using System.Xml;
using System.Windows.Forms;
using System.Diagnostics;

namespace WordSend
{
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
			string outName = "";
			string logName = "WordSendLog.txt";
			string jobOptionsName = Environment.GetEnvironmentVariable("APPDATA")+
				"\\SIL\\WordSend\\joboptions.xml";
			string templateName = "";
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
							case 'j':	// Job options file name
								jobOptionsName = SFConverter.GetOption(ref i, args);
								break;
							case 'n':	// No banner display
								showBanner = false;
								break;
							case 'o':	// Set output file name
								outName = SFConverter.GetOption(ref i, args);
								break;
							case 't':	// Set template file name
								templateName = SFConverter.GetOption(ref i, args);
								break;
							case 'l':	// Set log name
								logName = SFConverter.GetOption(ref i, args);
								break;
							case '?':
							case 'h':
							case '-':
								showBanner = true;
								break;
							default:
								Logit.WriteLine("Unrecognized command line switch: " + args[i]);
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
			Logit.WriteLine("\nWordSend project sf2word compiled " + Version.date);
			if (showBanner)
			{
				Logit.WriteLine("");
				Logit.WriteLine(Version.copyright);
				Logit.WriteLine("");
				Logit.WriteLine(Version.contact);
				Logit.WriteLine(@"
Syntax:
sf2word [-o Output] [-j Job] [-t Template] [-n] [-?] [-l logname] filespec(s)
 Output = output WordML file name
 Job = name of XML job options file created with usfm2word.exe.
 Template = example WordML document with the required style definitions
 -n = don't display copyright and banner information.
 -? = cancel previous /n and show this information.
 -l = set log file name (default is WordSendLog.txt in the current directory)
 filespec = SFM file specification to read
If a JobOptions file is specified, and that file contains the input
file specifications, then filespec(s) need not be specified on the
command line. If filespec(s) or output files are specified on the
command line, then the command line overrides those specifications
in the job options file.
You may use / instead of - to introduce switches. Do not use / or
- as the first character of a file name.

");
			}

			// Read XML job options file. Note that this file is only read
			// from in the command line program, but may be
			// written to from the Windows UI version. It is reasonable to
			// set up the options the way you want them in the Windows UI
			// version of the program, then read them with this command line
			// version. We convert some command line arguments to entries
			// in this class for consistency in the handling of options
			// between the command line and Windows UI versions.
			SFConverter.jobIni = new XMLini(jobOptionsName);
			SFConverter.jobIni.WriteString("TemplateName", templateName);

			if (outName == "")
				outName = SFConverter.jobIni.ReadString("outputFileName", "Output.xml");
			if (templateName != "")
				SFConverter.jobIni.WriteString("templateName", templateName);
			if (fileSpecs.Count < 1)
			{
				int numSfmFiles = SFConverter.jobIni.ReadInt("numSfmFiles", 0);
				for (i = 0; i < numSfmFiles; i++)
				{
					fileSpecs.Add(
						(object)SFConverter.jobIni.ReadString("sfmFile"+i.ToString(), "*.sfm"));
				}
			}
			if (fileSpecs.Count < 1)
			{
				Logit.WriteLine("Nothing to do. No input files specified.");
			}
			else
			{

				// We don't really have a need for application options in the
				// command line program, but if we did, they would go in the
				// following place:
				// SFConverter.appIni = new XMLini(Environment.GetEnvironmentVariable("APPDATA")+"\\SIL\\WordSend\\sf2word.xml");

				// Here we instantiate the object that does most of the work.
				SFConverter.scripture = new Scriptures();

				Logit.WriteLine("Job options: " + jobOptionsName);
				Logit.WriteLine("Output file: " + outName);

				// Read the input USFM files into internal data structures.
				for (i = 0; i < fileSpecs.Count; i++)
					SFConverter.ProcessFilespec((string) fileSpecs[i]);

				// Write out the WordML file.
				SFConverter.scripture.WriteToWordML(outName);
			}
			Logit.CloseFile();
		}
	}
}
