using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Diagnostics;
using System.Collections;
using System.Xml;  // For Process class.

namespace sepp
{
	/// <summary>
	/// This abstract class uses an external program specified in the subclass to convert a set of
	/// files into another set.
	/// </summary>
	public abstract class ExternalProgramConverter
	{
		string m_inputDirName;
		string m_outputDirName;
		string m_reportPath; // where we write standard output
		StringBuilder m_errorInfo;  // where we write standard error
		string m_errorHeader; // Header line to write to error stream if we get any errors.
		StreamWriter m_outputWriter;
		string m_toolPath;

		/// <summary>
		/// initialize one.
		/// </summary>
		/// <param name="inputDirName"></param>
		/// <param name="outputDirName"></param>
		public ExternalProgramConverter(string inputDirName, string outputDirName)
		{
			m_inputDirName = inputDirName;
			m_outputDirName = outputDirName;
		}

		internal abstract string ToolPath
		{ get; }

		internal abstract string[] Extensions { get; }

		internal abstract string OutputExtension { get; }

		// True for programs like Tidy that dump masses of non-serious warnings to standard error.
		internal virtual bool LogErrors
		{
			get { return false; }
		}

		internal virtual void Init()
		{
		}

		internal virtual bool CheckToolPresent
		{
			get { return true; }
		}
		/// <summary>
		/// Run the algorithm (on all files).
		/// </summary>
		public void Run(IList files)
		{
			Init();
			m_toolPath = ToolPath;
			if (CheckToolPresent && !File.Exists(m_toolPath))
			{
				MessageBox.Show("The program needed for this conversion was not found: " + m_toolPath, "Error");
				return;
			}
			ConcGenerator.EnsureDirectory(m_outputDirName);
			string[] inputFileNames = new string[0];
			foreach (string pattern in Extensions)
			{
				inputFileNames = Directory.GetFiles(m_inputDirName, pattern);
				if (inputFileNames.Length != 0)
					break;
			}
			Progress status = new Progress(files.Count);
			status.Show();
			int count = 0;
			m_reportPath = Path.Combine(m_inputDirName, "ConversionReports.txt");
			File.Delete(m_reportPath); // get rid of any old reports
			m_errorInfo = null;
			m_outputWriter = new StreamWriter(m_reportPath, true, Encoding.UTF8);
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
			m_outputWriter.Close();
			if (m_errorInfo != null)
			{
				ProblemReport reportDlg = new ProblemReport();
				reportDlg.ReportContents = m_errorInfo.ToString();
				reportDlg.Show();
			}
		}

		/// <summary>
		/// Convert one file.
		/// </summary>
		/// <param name="inputFilePath">full path name to the file to convert.</param>
		private void Convert(string inputFilePath)
		{
			// Name of output file (without path)
			string outputFileName = Path.ChangeExtension(Path.GetFileName(inputFilePath), OutputExtension);
			string outputFilePath = Path.Combine(m_outputDirName, outputFileName);
			File.Delete(outputFilePath); // Make sure we don't somehow preserve an old version.
			m_outputWriter.WriteLine("------Report on converting " + inputFilePath + " to " + outputFilePath + " ----------");
			m_errorHeader = "------Errors converting " + inputFilePath + " to " + outputFilePath + " ----------";
			Process proc = new Process();
			ProcessStartInfo info = proc.StartInfo;
			info.Arguments = CreateArguments(inputFilePath, outputFilePath); ;
			info.FileName = m_toolPath;
			info.CreateNoWindow = true;
			info.WindowStyle = ProcessWindowStyle.Hidden;
			info.UseShellExecute = false;
			info.RedirectStandardError = true;
			info.RedirectStandardOutput = true;
			proc.OutputDataReceived += new DataReceivedEventHandler(proc_OutputDataReceived);
			if (LogErrors)
				proc.ErrorDataReceived += new DataReceivedEventHandler(proc_OutputDataReceived);
			else
				proc.ErrorDataReceived += new DataReceivedEventHandler(proc_ErrorDataReceived);

			proc.Start();
			proc.BeginErrorReadLine();
			proc.BeginOutputReadLine();
			proc.WaitForExit();
			DoPostProcessing(outputFileName, outputFilePath);
		}

		internal virtual void DoPostProcessing(string outputFileName, string outputFilePath)
		{
			// by default nothing to do (see e.g. OSIS_to_HTML.cs).
		}

		internal virtual string CreateArguments(string inputFilePath, string outputFilePath)
		{
			return "\"" + inputFilePath + "\" \"" + outputFilePath + "\"";
		}

		void proc_ErrorDataReceived(object sender, DataReceivedEventArgs e)
		{
			if (!String.IsNullOrEmpty(e.Data))
			{
				string message = e.Data;
				ReportError(message);
			}
		}

		internal void ReportError(string message)
		{
			if (m_errorInfo == null)
				m_errorInfo = new StringBuilder();
			if (m_errorHeader != null)
			{
				// Append header once per problem file
				m_errorInfo.AppendLine(m_errorHeader);
				m_errorHeader = null;
			}
			m_errorInfo.AppendLine(message);
		}

		void proc_OutputDataReceived(object sender, DataReceivedEventArgs e)
		{
			// Collect the error output.
			if (!String.IsNullOrEmpty(e.Data))
			{
				// Add the text to the collected output.
				m_outputWriter.WriteLine(e.Data);
			}
		}

	}

}