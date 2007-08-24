using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Collections;

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
		public void Run(IList files)
		{
			string[] inputFileNames = Directory.GetFiles(m_inputDirName, "*.db");
			Progress status = new Progress(files.Count);
			status.Show();
			ConcGenerator.EnsureDirectory(m_outputDirName);
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
			string outputFileName = Path.ChangeExtension(Path.GetFileName(inputFilePath), "ptx");
			string outputFilePath = Path.Combine(m_outputDirName, outputFileName);
			string[] tablePaths = new string[] {
				// remove spaces at end of line. They can end up between end of sentence and note marker.
				@"..\..\cleanup_EOL_spaces.cct", 
				// removes btX fields, which (when converted to \note: btX....\note* by current OW_to_PT cause
				// Nathan's USFM->OSIS to drop the rest of the verse after a footnote.
				//@"..\..\remove_bt_from_OW.cct", 
				"bt",
				// removes \ov fields, which otherwise tend to result in a newline before various notes,
				// which becomes an unwanted space after following stages.
				//@"..\..\remove_ov_from_OW.cct", 
				"ov",
				// Strip all the \ntX fields. JohnD's file makes of all these markers that don't translate into \note.
				// The OSIS converter discards them, but the resulting blank lines mess up spacing of note markers.
				//@"..\..\remove_nt_from_OW.cct",
				"nt",
				// Several more fields that are not wanted and not handled by the OW_to_PT.
				"al ",
				"e",
				"chk2",

				// OW_to_PT.cct does not seem to get the quotes quite right. Kupang makes use of <<< and >>> which
				// are ambiguous; OW_to_PT converts << and >> and < and >, but >>> is therefore interpreted as >> >
				// and closes the double first, which is (usually) wrong.
				// This version removes any space in >> > etc, and interprets >>> as > >>.
				// This change may be redundant with the latest version of JohnD's OW_to_PT.cct
				//@"..\..\fix_quotes.cct", 
				// Main conversion by John Duerkson implemented in these two files.
				@"..\..\OW_to_PT.cct",
				@"..\..\move_footnote_to_fn.cct",
				// Strip all the \note fields that JohnD's file makes of all the markers that don't translate.
				// The OSIS converter discards them, but the resulting blank lines mess up spacing of note markers.
				// Didn't work...strips the whole file content after the first \note. Also not needed, now have
				// \nt being deleted properly.
				//@"..\..\remove_note_from_USFM.cct",
				// Final cleanup strips remnants of s2 markers at end of field, and puts cross-ref notes inline so
				// we don't get a spurious space before the <note> in the OSIS and beyond.
				@"..\..\cleanup_OW_to_USFM.cct"
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
				if (tablePath.EndsWith(".cct"))
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
				else
				{
					// Simple strings are interpreted as markers to be deleted.
					outBuffer = new byte[cbyteInput]; // deletes only
					int cbyteOut = 0;
					string markerS = @"\" + tablePath;
					byte[] marker = Encoding.UTF8.GetBytes(markerS);
					int i = 0;
					byte[] matchEnd = new byte[] {10, 92}; // cr, lf, backslash
					byte[] target = marker; // initially searching for marker
					while (i < cbyteInput - target.Length)
					{
						bool gotIt = true;
						for (int j = 0; j < target.Length; j++)
						{
							if (inputBytes[i + j] != target[j])
							{
								gotIt = false;
								break;
							}
						}
						if (gotIt)
						{
							if (target == marker)
							{
								target = matchEnd;
								i++;
								continue;
							}
							else
							{
								target = marker;
								// i is pointing at the newline before the end marker. We don't want to copy that.
								// but, we do want to consider the terminating backslash as a possible start of another
								// match.
								i += 1; // the backslash is the current character (skip the nl)
								continue;
							}
						}
						if (target != matchEnd)
						{
							outBuffer[cbyteOut] = inputBytes[i];
							cbyteOut++;
						}
						i++;
					}
					inputBytes = outBuffer;
				}
			}
			// Convert the output back to a file
			StreamWriter output = new StreamWriter(outputPath);
			string outputString = Encoding.UTF8.GetString(outBuffer, 0, nOutLen);
			//outputString = FixEmphasis(outputString);
			output.Write(outputString);
			output.Close();
		}

		int startEmphasis;
		int endEmphasis;
		int missingEnd;
		int missingStart;
		int broken;

		private string FixEmphasis(string outputString)
		{
			int state = 0; // 0 = normal, 1 = seen |i
			char prev = '\0';
			StringBuilder bldr = new StringBuilder(outputString.Length);
			for (int i = 0; i < outputString.Length; i++)
			{
				char c = outputString[i];
				bldr.Append(c);
				if (c == '\\' && state == 1)
				{
					broken++;
					missingEnd++;
					state = 0;
				}
				if (prev == '|')
				{
					if (c == 'i')
					{
						if (state == 0)
							state = 1;
						else
							missingEnd++;
						startEmphasis++;
						bldr.Remove(bldr.Length - 2, 2); // remove the |i
						bldr.Append("\\em ");
					}
					else if (c == 'r')
					{
						if (state == 1)
							state = 0;
						else
							missingStart++;
						endEmphasis++;
						bldr.Remove(bldr.Length - 2, 2); // remove the |r
						bldr.Append("\\em*");
					}
				}
				prev = c;
			}
			return bldr.ToString();
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