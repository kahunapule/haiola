using System;
using System.IO;

namespace UpdateVersion
{
	/// <summary>
	/// This class is a simple command line program that writes a C# source
	/// file that contains the current date and version information for the
	/// WordSend project.
	/// </summary>
	class Class1
	{
		public static string copyright =
@"Copyright (c) 2004-2010 SIL International, Evangel Bible Translators,
and Rainbow Missions, Inc.
This set of programs is released under the Gnu Lesser Public License or
the Common Public License, as explained in LICENSING.txt.";
		public static string contact = 
@"This software is not officially supported, but feedback to the author is              
welcome at https://cryptography.org/cgi-bin/contact.cgi, or
my Kahunapule Johnson at Wycliffe dot org email address. Please check to see
if your issue has already been addressed in the latest version at
http://eBible.org/wordsend/. If you would like email notification of updates,
please sign up at http://groups.google.com/group/wordsend.";
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] args)
		{
			StreamWriter sw = new StreamWriter(@"..\..\..\BibleFileLib\Version.cs", false, System.Text.Encoding.UTF8);
			sw.Write(@"using System;

namespace WordSend
{
	/// <summary>
	/// All this does is provide a place for version strings to be stored and 
	/// updated with UpdateVersion.exe. This is a generated file. You should
	/// not edit it directly, but edit UpdateVersion\Class1.cs instead.
	/// </summary>
	public class Version
	{
		public static string date = ");

			sw.WriteLine("\"{0}\";", DateTime.UtcNow.ToString("u"));
			sw.WriteLine("		public static string copyright =\r\n@\"{0}\";", copyright);
			sw.WriteLine("		public static string contact =\r\n@\"{0}\";", contact);
			sw.WriteLine(@"		public Version()
		{
		}
	}
}");

			sw.Close();

			string isoDateString = DateTime.UtcNow.Year.ToString("d2")+"-"+DateTime.UtcNow.Month.ToString("d2")+
				"-"+DateTime.UtcNow.Day.ToString("d2");

			sw = new StreamWriter(@"..\..\..\VersionStamp.bat", false, System.Text.Encoding.ASCII);
			sw.Write(@"copy Installer\InstallWordSend.exe dist\wordsend-{0}.exe
copy dist\WordSend-console.zip dist\wordsend-console-{0}.zip
", isoDateString);
			sw.Close();

			StreamReader sr = new StreamReader(@"..\..\..\doc\downloads.template", System.Text.Encoding.UTF8);
			string webPage = sr.ReadToEnd();
			sr.Close();
			webPage = webPage.Replace("@date", isoDateString);
			sw = new StreamWriter(@"..\..\..\doc\downloads.htm", false, System.Text.Encoding.UTF8);
			sw.Write(webPage);
			sw.Close();
		}
	}
}
