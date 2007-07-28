using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Diagnostics;  // For Process class.

namespace sepp
{
	/// <summary>
	/// This class uses some CC files to convert from the OurWord SF model to USFM.
	/// CC tables provided initially by John Duerksen.
	/// Convertes all .db files in input directory.
	/// </summary>
	public class HTML_TO_XHTML
	{
		string m_inputDirName;
		string m_outputDirName;

		/// <summary>
		/// initialize one.
		/// </summary>
		/// <param name="inputDirName"></param>
		/// <param name="outputDirName"></param>
		public HTML_TO_XHTML(string inputDirName, string outputDirName)
		{
			m_inputDirName = inputDirName;
			m_outputDirName = outputDirName;
		}

		/// <summary>
		/// Run the algorithm (on all files).
		/// </summary>
		public void Run()
		{
			string[] inputFileNames = Directory.GetFiles(m_inputDirName, "*.htm");
			foreach (string inputFile in inputFileNames)
			{
				Convert(inputFile);
			}

			MessageBox.Show("Done");
		}

		/// <summary>
		/// Convert one file.
		/// </summary>
		/// <param name="inputFilePath">full path name to the file to convert.</param>
		private void Convert(string inputFilePath)
		{
			// Name of output file (without path)
			string outputFileName = Path.ChangeExtension(Path.GetFileName(inputFilePath), "xml");
			string outputFilePath = Path.Combine(m_outputDirName, outputFileName);
			string tidyPath = Path.GetFullPath(@"..\..\tidy.exe");
			string args = "-asxhtml -raw -o \"" + outputFilePath + "\" \"" + inputFilePath + "\"";
			Process proc = Process.Start(tidyPath, args);
			proc.WaitForExit();
		}

	}

}