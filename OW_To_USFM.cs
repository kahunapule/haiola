using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace sepp
{
	/// <summary>
	/// This class uses some CC files to convert from the OurWord SF model to USFM.
	/// CC tables provided initially by John Duerksen.
	/// Convertes all .db files in input directory.
	/// </summary>
	public class OW_To_USFM
	{
		string m_inputDirName;
		string m_outputDirName;
		private Int32 m_hTable = 0;

		/// <summary>
		/// initialize one.
		/// </summary>
		/// <param name="inputDirName"></param>
		/// <param name="outputDirName"></param>
		public OW_To_USFM(string inputDirName, string outputDirName)
		{
			m_inputDirName = inputDirName;
			m_outputDirName = outputDirName;
		}

		/// <summary>
		/// Run the algorithm (on all files).
		/// </summary>
		public void Run()
		{
			string[] inputFileNames = Directory.GetFiles(m_inputDirName, "*.db");
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
			string outputFileName = Path.ChangeExtension(Path.GetFileName(inputFilePath), "ptx");
			string outputFilePath = Path.Combine(m_outputDirName, outputFileName);
			string[] tablePaths = new string[] {
				// removes btX fields, which (when converted to \note: btX....\note* by current OW_to_PT cause
				// Nathan's USFM->OSIS to drop the rest of the verse after a footnote.
				@"..\..\remove_bt_from_OW.cct", 
				// OW_to_PT.cct does not seem to get the quotes quite right. Kupang makes use of <<< and >>> which
				// are ambiguous; OW_to_PT converts << and >> and < and >, but >>> is therefore interpreted as >> >
				// and closes the double first, which is (usually) wrong.
				// This version removes any space in >> > etc, and interprets >>> as > >>.
				@"..\..\fix_quotes.cct", 
				@"..\..\OW_to_PT.cct",
				@"..\..\move_footnote_to_fn.cct"
			};
			ConvertFileCC(inputFilePath, tablePaths, outputFilePath);
		}

		/// <summary>
		/// Enhance JohnT: this could be cleaned up so we don't need a member variable.
		/// Or, it could be optimised so it reuses file handles if the same file is wanted and hasn't
		/// been modified.
		/// </summary>
		/// <param name="inputPath"></param>
		/// <param name="tablePaths">A succession of tables to apply one after the other</param>
		/// <param name="outputPath"></param>
		private unsafe void ConvertFileCC(string inputPath, string[] tablePaths, string outputPath)
		{
			string input;
			// Read file into input
			StreamReader reader = new StreamReader(inputPath);
			input = reader.ReadToEnd() + "\0";

			int status = 0;
			// Copy input into buffer
			byte[] inputBytes = Encoding.UTF8.GetBytes(input); // replaced by previous output for subsequent iterations.
			int cbyteInput = inputBytes.Length; // replaced by output length for subsequent iterations
			byte[] outBuffer = inputBytes; // in case 0 iterations
			int nOutLen = inputBytes.Length;
			foreach (string tablePath in tablePaths)
			{
				// allocate a new buffer
				nOutLen = Math.Max(10000, cbyteInput * 6);
				outBuffer = new byte[nOutLen];
				fixed (byte* lpOutBuffer = outBuffer)
				{
					lpOutBuffer[0] = lpOutBuffer[1] = lpOutBuffer[2] = lpOutBuffer[3] = 0;
					fixed (byte* lpInBuffer = inputBytes)
					{
						// Do the conversion

						try
						{
							Load(tablePath);
							// call the wrapper sub-classes' DoConvert to let them do it.
							int* pnOut = &nOutLen;
							{
								status = CCProcessBuffer(m_hTable, lpInBuffer, cbyteInput, lpOutBuffer, pnOut);
							}
						}
						finally
						{
							Unload();
						}
					}
					if (status != 0)
					{
						TranslateErrStatus(status);
					}
					// if we iterate, starting point is current output
					cbyteInput = nOutLen;
					inputBytes = outBuffer;
				}
			}
			// Convert the output back to a file
			StreamWriter output = new StreamWriter(outputPath);
			string outputString = Encoding.UTF8.GetString(outBuffer, 0, nOutLen);
			output.Write(outputString);
			output.Close();
		}
		protected bool IsFileLoaded()
		{
			return (m_hTable != 0);
		}

		protected void Unload()
		{
			if (IsFileLoaded())
			{
				CCUnloadTable(m_hTable);
				m_hTable = 0;
			}
		}
		protected unsafe void Load(string strTablePath)
		{
			// first make sure it's there
			if (!File.Exists(strTablePath))
				MessageBox.Show("Table " + strTablePath + "does not exist", "Error");
			Unload();
			Int32 hInstanceHandle = 0;  // don't know what else to use here...
			byte[] baTablePath = Encoding.ASCII.GetBytes(strTablePath);
			fixed (byte* pszTablePath = baTablePath)
			fixed (Int32* phTable = &m_hTable)
			{
				int status = CCLoadTable(pszTablePath, phTable, hInstanceHandle);
				if (status != 0)
				{
					TranslateErrStatus(status);
				}
			}
		}

		protected void TranslateErrStatus(int status)
		{
			switch (status)
			{
				case 0: return; // all well
				case -1:    // what CC returns when the buffer we provided wasn't big enough.
					MessageBox.Show("Not enough buffer", "Error");
					break;
				case -2:    // CC_SYNTAX_ERROR from ccdll.h
					MessageBox.Show("Syntax error in table", "Error");
					break;

				default:
					MessageBox.Show("Unknown/unexpected error " + status, "Error");
					break;
			}
			throw new Exception("Conversion failed");
		}
		#region DLLImport Statements
		[DllImport("Cc32", SetLastError = true)]
		static extern unsafe int CCLoadTable(byte* lpszCCTableFile,
			Int32* hpLoadHandle,
			Int32 hinstCurrent);

		[DllImport("Cc32", SetLastError = true)]
		static extern unsafe int CCUnloadTable(Int32 hUnlHandle);

		[DllImport("Cc32", SetLastError = true)]
		static extern unsafe int CCProcessBuffer(Int32 hProHandle,
			byte* lpInputBuffer, int nInBufLen,
			byte* lpOutputBuffer, int* npOutBufLen);
		#endregion DLLImport Statements
	}

}