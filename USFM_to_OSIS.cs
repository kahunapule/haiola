using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Diagnostics;
using System.Collections;  // For Process class.

namespace sepp
{
	/// <summary>
	/// This class uses some CC files to convert from the OurWord SF model to USFM.
	/// CC tables provided initially by John Duerksen.
	/// Convertes all .db files in input directory.
	/// </summary>
	public class USFM_to_OSIS
	{
		string m_inputDirName;
		string m_outputDirName;

		/// <summary>
		/// initialize one.
		/// </summary>
		/// <param name="inputDirName"></param>
		/// <param name="outputDirName"></param>
		public USFM_to_OSIS(string inputDirName, string outputDirName)
		{
			m_inputDirName = inputDirName;
			m_outputDirName = outputDirName;
		}

		/// <summary>
		/// Run the algorithm (on all files).
		/// </summary>
		public void Run(IList files)
		{
			string[] inputFileNames = Directory.GetFiles(m_inputDirName, "*.ptx");
			Progress status = new Progress(files.Count);
			status.Show();
			int count = 0;
			foreach (string inputFile in inputFileNames)
			{
				string filename = Path.GetFileName(inputFile);
				if (files.Contains(Path.ChangeExtension(filename, "xml")))
				{
					status.File = filename;
					Convert(inputFile);
					count++;
					status.Value = count;
				}
			}

			status.Close();
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
			string scriptPath = Path.GetFullPath(@"..\..\JohnOsisBP.py");
			string args = "\"" + scriptPath + "\" \"" + inputFilePath + "\" \"" + outputFilePath + "\"";
			ProcessStartInfo info = new ProcessStartInfo("python", args);
			info.WindowStyle = ProcessWindowStyle.Minimized;
			//info.RedirectStandardError = 
			Process proc = Process.Start(info);
			proc.WaitForExit();
		}

	}

}