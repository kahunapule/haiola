using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Collections;
using System.Text.RegularExpressions;
using WordSend;

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

		StringBuilder m_problemReports = new StringBuilder();
		Dictionary<string, int> m_ProblemMarkers = new Dictionary<string, int>();
		int m_seriousErrorCount = 0;
		string[] m_tablePaths;
		private Options m_options;

		/// <summary>
		/// initialize one.
		/// </summary>
		/// <param name="inputDirName"></param>
		/// <param name="outputDirName"></param>
		public OW_To_USFM(string inputDirName, string outputDirName, Options options)
		{
			m_inputDirName = inputDirName;
			m_outputDirName = outputDirName;
			m_options = options;
		}

		public string[] TablePaths
		{
			get { return m_tablePaths; }
			set { m_tablePaths = value; }
		}


        public static WordSend.BibleBookInfo bkInfo = null;

        /// <summary>
        /// Reads the \id line to get the standard abbreviation of this file to figure out what
        /// a good name for its standardized file name might be.
        /// </summary>
        /// <param name="pathName">Full path to the file to read the \id line from.</param>
        /// <returns>Sort order plus 3-letter abbreviation of the Bible book (or front/back matter), upper case,
        /// unless the file lacks an \id line, in which case in returns and empty string.</returns>
        public string MakeUpUsfmFileName(string pathName)
        {
            if (bkInfo == null)
                bkInfo = new WordSend.BibleBookInfo();
            // Use the ID line.
            string result = "";
            string line;
            StreamReader sr = new StreamReader(pathName);
            while ((result.Length < 1) && (!sr.EndOfStream))
            {
                line = sr.ReadLine();
                if (line.StartsWith(@"\id ") && (line.Length > 6))
                {
                    result = line.Substring(4, 3).ToUpper(System.Globalization.CultureInfo.InvariantCulture);
                }
            }
            sr.Close();
            if (result.Length > 0)
            {
                result = bkInfo.Order(result).ToString("D2") + "-" + result;
            }
            return result;
        }


		/// <summary>
		/// Run the algorithm on all non-backup files in the source directory.
		/// </summary>
		public void Run()
		{
			if (m_tablePaths == null)
			{
				// Use the default
				m_tablePaths = new string[] {
				// remove spaces at end of line. They can end up between end of sentence and note marker.
				Utils.GetUtilityFile("cleanup_EOL_spaces.cct"), 
				// removes btX fields, which (when converted to \note: btX....\note* by current OW_to_PT cause
				// Nathan's USFM->OSIS to drop the rest of the verse after a footnote.
				//Utils.GetUtilityFile("remove_bt_from_OW.cct"), 
				"bt",
				// removes \ov fields, which otherwise tend to result in a newline before various notes,
				// which becomes an unwanted space after following stages.
				//Utils.GetUtilityFile("remove_ov_from_OW.cct"), 
				"ov",
				// Strip all the \ntX fields. JohnD's file makes of all these markers that don't translate into \note.
				// The OSIS converter discards them, but the resulting blank lines mess up spacing of note markers.
				//Utils.GetUtilityFile("remove_nt_from_OW.cct"),
				"nt",
				// Several more fields that are not wanted and not handled by the OW_to_PT.
				"al ",
				"e",
				"chk2",
				"bnvt",
				"nq",

				// Consider using this...if so, must be BEFORE we convert angle brackets to quotes!
				//Utils.GetUtilityFile("fix_glottal.cct"),

				// The main tables don't do well with multiple foonotes in a block. Now we've got rid of the \bts, we can break those up.
				"footnotes.process",

				// OW_to_PT.cct does not seem to get the quotes quite right. Kupang makes use of <<< and >>> which
				// are ambiguous; OW_to_PT converts << and >> and < and >, but >>> is therefore interpreted as >> >
				// and closes the double first, which is (usually) wrong.
				// This version removes any space in >> > etc, and interprets >>> as > >>.
				// This change may be redundant with the latest version of JohnD's OW_to_PT.cct
				//Utils.GetUtilityFile("fix_quotes.cct"), 
				// Main conversion by John Duerkson implemented in these two files.
				Utils.GetUtilityFile("OW_to_PT.cct"),
				Utils.GetUtilityFile("move_footnote_to_fn.cct"),
				// Strip all the \note fields that JohnD's file makes of all the markers that don't translate.
				// The OSIS converter discards them, but the resulting blank lines mess up spacing of note markers.
				// Didn't work...strips the whole file content after the first \note. Also not needed, now have
				// \nt being deleted properly.
				//Utils.GetUtilityFile("remove_note_from_USFM.cct"),
				// Final cleanup strips remnants of s2 markers at end of field, and puts cross-ref notes inline so
				// we don't get a spurious space before the <note> in the OSIS and beyond.
				Utils.GetUtilityFile("cleanup_OW_to_USFM.cct")
				};
			}
			// Reset problem records, in case ever used repeatedly.
			m_ProblemMarkers.Clear();
			m_problemReports.Remove(0, m_problemReports.Length);
			m_seriousErrorCount = 0;

			// Input is typically db files from OurWord, sfm from TE, or ptx from ParaText.
            // It may also be from Bibledit or another USFM tool. Many archived input files
            // do not have consistent file extensions. Some are named for the language as a
            // suffix. Some use a book abbreviation as an extension.
            // Bibledit uses .usfm on its exports.
			string[] inputFileNames = Directory.GetFiles(m_inputDirName);
            if (inputFileNames.Length == 0)
            {
                MessageBox.Show("No files found in input directory " + m_inputDirName);
                return;
            }
//			Progress status = new Progress(inputFileNames.Length);
//			status.Show();
            Utils.DeleteDirectory(m_outputDirName);
			Utils.EnsureDirectory(m_outputDirName);
			int count = 0;
			foreach (string inputFile in inputFileNames)
			{
				string filename = Path.GetFileName(inputFile);
                string fileType = Path.GetExtension(filename).ToUpper();
                if ((fileType != ".BAK") && (fileType != ".LDS") && (fileType != ".SSF") && (fileType != ".DBG") && (fileType != ".STY") && (!inputFile.EndsWith("~")))
				{
//					status.File = filename;
					if (!Convert(inputFile))
					{
						break;
					}
				}
                count++;
//                status.Value = count;
            }

//			status.Close();

			if (m_ProblemMarkers.Count > 0 || m_seriousErrorCount > 0)
			{
				StringBuilder report = new StringBuilder();
				if (m_seriousErrorCount > 0)
				{
					report.Append("Serious problems occurred while processing at least one file, indicated by lines beginning **** ERROR in the output\n");
					report.Append("These errors result in the output being an incomplete conversion of the input\n\n");
				}
				if (m_ProblemMarkers.Count > 0)
				{
					List<string> keys = new List<string>(m_ProblemMarkers.Keys);
					keys.Sort();
					report.Append("The conversion process encountered some markers it could not handle.\n");
					report.Append("These are indicated in the output files by lines starting ***\\.\n");
					report.Append("The problem markers are these: ");
					bool fFirst = true;
					foreach (string key in keys)
					{
						if (fFirst)
							fFirst = false;
						else
							report.Append("; ");
						report.Append(String.Format("{0} ({1})", key, m_ProblemMarkers[key].ToString()));
					}
					report.Append(".\n\n");
				}
				report.Append("Here are the lines where the problems were found:\n");
				report.Append(m_problemReports.ToString());
				ProblemReport reportDlg = new ProblemReport();
				reportDlg.ReportContents = report.ToString();
				reportDlg.ShowDialog();
			}
		}

		/// <summary>
		/// Convert one file.
		/// </summary>
		/// <param name="inputFilePath">full path name to the file to convert.</param>
		private bool Convert(string inputFilePath)
		{
			// Name of output file (without path)
            string outputFileName = MakeUpUsfmFileName(inputFilePath) + ".usfm";
            if (outputFileName.Length < 8)
                return false;
			string outputFilePath = Path.Combine(m_outputDirName, outputFileName);

			return ConvertFileCC(inputFilePath, m_tablePaths, outputFilePath);
		}

		/// <summary>
		/// Enhance JohnT: this could be cleaned up so we don't need a member variable.
		/// Or, it could be optimised so it reuses file handles if the same file is wanted and hasn't
		/// been modified.
		/// </summary>
		/// <param name="inputPath"></param>
		/// <param name="tablePaths">A succession of tables to apply one after the other</param>
		/// <param name="outputPath"></param>
		private unsafe bool ConvertFileCC(string inputPath, string[] tablePaths, string outputPath)
		{
			string input;
			// Read file into input
            // Instead of asking the user what the character encoding is, we guess that it is either
            // Windows 1252 or UTF-8, and choose which one of those based on the assumed presence of
            // surrogates in UTF-8, unless there is a byte-order mark.
			StreamReader reader = new StreamReader(inputPath, fileHelper.IdentifyFileCharset(inputPath) /* Encoding.GetEncoding(m_options.InputEncoding) */);
			input = reader.ReadToEnd() + "\0";
			reader.Close();

			int status = 0;
			// Copy input into buffer
			byte[] inputBytes = Encoding.UTF8.GetBytes(input); // replaced by previous output for subsequent iterations.
			int cbyteInput = inputBytes.Length; // replaced by output length for subsequent iterations
			byte[] outBuffer = inputBytes; // in case 0 iterations
			int nOutLen = inputBytes.Length;
			foreach (string tp in tablePaths)
			{
                string tablePath = Utils.GetUtilityFile(tp);
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
								if (Load(tablePath) != 0)
									return false;
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
							return false;
						}
						// if we iterate, starting point is current output
						cbyteInput = nOutLen;
						inputBytes = outBuffer;
					}
				}
				else if (tablePath.EndsWith(".process"))
				{
					// This will become a switch if we get more processes
					if (tablePath == "footnotes.process")
					{
						FixMultipleFootnotes(ref inputBytes, ref cbyteInput);
						outBuffer = inputBytes;// in case last pass
						nOutLen = cbyteInput; // in case last pass.
					}
				}
				else if (tablePath.EndsWith(".re"))
				{
					// Apply a regular expression substitution
					string temp = Encoding.UTF8.GetString(inputBytes, 0, cbyteInput - 1); // leave out final null
					StreamReader tableReader = new StreamReader(tablePath, Encoding.UTF8);
					while (!tableReader.EndOfStream)
					{
						string source = tableReader.ReadLine();
						if (source.Trim().Length == 0)
							continue;

						char delim = source[0];
						string[] parts = source.Split(new char[] {delim});
						string pattern = parts[1]; // parts[0] is the empty string before the first delimiter
						string replacement = parts[2];
						temp = Regex.Replace(temp, pattern, replacement);
					}
					tableReader.Close();
					temp = temp + "\0";
					outBuffer = Encoding.UTF8.GetBytes(temp);
					inputBytes = outBuffer;
					cbyteInput = nOutLen = inputBytes.Length;
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
								target = matchEnd; // while looking for this we don't copy to output.
								i++;
								continue;
							}
							else
							{
								target = marker;
                                // Usually, we are deleting a whole line, the quoted part in 13, 10"\xx whatever 13 10"\
                                // i is pointing at the newline (10) which is the last character we want to delete, and we
                                // can achieve that (and prepare to check the following \ as the start of another marker)
                                // by just incrementing i.
                                // However, we could also be in the case 
                                // \yy previous text not deleted "\xx whatever 13 10"\
                                // In this case if we delete the newline the following marker will no longer follow a newline.
                                // It could then get deleted as part of the \\yy line, or we may just mess up the next stage
                                // because it expects that marker to start a line. Therefore, we delete the newline only if
                                // it is preceded by another newline in the output.
							    if (cbyteOut >= 0 && outBuffer[cbyteOut - 1] != 10)
                                {
                                    // need to keep newline. Put a CR before it if the input has one. Since there is
                                    // already output, i can't be 0.
                                    if (inputBytes[i-1] == 13)
                                        outBuffer[cbyteOut++] = 13;
                                    outBuffer[cbyteOut++] = 10;
                                }
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
					// copy the last few bytes.
					while (i < cbyteInput)
					{
						outBuffer[cbyteOut++] = inputBytes[i++];
					}
					inputBytes = outBuffer;
					cbyteInput = cbyteOut;
					nOutLen = cbyteOut; // in case last pass.
				}
			}
			// Convert the output back to a file
			StreamWriter output = new StreamWriter(outputPath);
			// Make sure no trailing nulls get written to file.
			while (nOutLen > 0 && outBuffer[nOutLen - 1] == 0)
				nOutLen--;
			string outputString = Encoding.UTF8.GetString(outBuffer, 0, nOutLen);
			//outputString = FixEmphasis(outputString);
			output.Write(outputString);
			output.Close();
			// Check for problems indicated by lines starting ***\
			StringReader checker = new StringReader(outputString);
			int lineCount = 0;
			for ( ; ; )
			{
				string line = checker.ReadLine();
				if (line == null)
					break;
				lineCount++;
				if (line.StartsWith("***\\"))
				{
					m_problemReports.AppendFormat("{0}({1}): {2}\n", outputPath, lineCount, line);
					Regex re = new Regex(@"\\\w*"); // TODO: finish.
					Match result = re.Match(line);
					string marker = result.Value;
					int count;
					if (!m_ProblemMarkers.TryGetValue(marker, out count))
						count = 0;
					m_ProblemMarkers[marker] = ++count;
				}
				if (line.StartsWith("**** ERROR"))
				{
					m_problemReports.AppendFormat("{0}({1}): {2}\n", outputPath, lineCount, line);
					m_seriousErrorCount++;
				}
			}
			return true;
		}

		enum FnStates
		{
			fnsAwaitingVt,
			fnsProcessingVt,
			fnsCollectingFt
		}

		private void FixMultipleFootnotes(ref byte[] inputBytes, ref int cbyteInput)
		{
			byte[] outBuffer = new byte[cbyteInput * 2 +100]; // super-generous
			int cbyteOut = 0;
			byte[] marker = Encoding.UTF8.GetBytes(@"\vt ");
			int i = 0;
			byte[] anchor = Encoding.UTF8.GetBytes("|fn");
			byte[] footnote = Encoding.UTF8.GetBytes(@"\ft ");
			List<int> anchors = new List<int>();
			List<int> footnotes = new List<int>();
			FnStates state = FnStates.fnsAwaitingVt;
			int ichStartVt = -1; // will cause crash if accidentally used before otherwise initialized.
			while (i < cbyteInput - 2)// 2 is length of shortest target
			{
				switch (state)
				{
					case FnStates.fnsAwaitingVt:
						if (FindMarker(inputBytes, i, marker))
						{
							state = FnStates.fnsProcessingVt;
							ichStartVt = i;
						}
						else
						{
							outBuffer[cbyteOut++] = inputBytes[i];
						}
						break;
					case FnStates.fnsProcessingVt:
						if (FindMarker(inputBytes, i, footnote))
						{
							footnotes.Add(i); // note it
							state = FnStates.fnsCollectingFt;
						}
						else if (FindMarker(inputBytes, i, anchor))
						{
							anchors.Add(i);
						}
						else if (i > 0 && inputBytes[i-1] == 10 && inputBytes[i] == 92) // backslash at start of line
						{
							// We found some marker that terminates things. Note that it MIGHT be another \vt.
							cbyteOut = HandleEndOfVtBlock(inputBytes, outBuffer, cbyteOut, marker, i, anchor, anchors, footnotes, ichStartVt);
							state = FnStates.fnsAwaitingVt;
							continue; // SKIP i++ so we can check to see whether this marker is \vt and starts a new block.
						}
						break;
					case FnStates.fnsCollectingFt:
						if (FindMarker(inputBytes, i, footnote))
						{
							footnotes.Add(i); // note it
						}
						else if (i > 0 && inputBytes[i - 1] == 10 && inputBytes[i] == 92) // backslash at start of line
						{
							cbyteOut = HandleEndOfVtBlock(inputBytes, outBuffer, cbyteOut, marker, i, anchor, anchors, footnotes, ichStartVt);
							state = FnStates.fnsAwaitingVt;
							continue; // SKIP i++ so we can check to see whether this marker is \vt and starts a new block.
						}
						break;
				}
				i++;
			}
			if (state != FnStates.fnsAwaitingVt)
			{
				// input terminated within a \vt block. Include the remaining text
				cbyteOut = HandleEndOfVtBlock(inputBytes, outBuffer, cbyteOut, marker, cbyteInput, anchor, anchors, footnotes, ichStartVt);
			}
			else
			{
				// Copy the last few bytes
				while (i < cbyteInput)
					outBuffer[cbyteOut++] = inputBytes[i++];
			}
			// switch buffers and counts.
			inputBytes = outBuffer;
			cbyteInput = cbyteOut;
		}

		private static int HandleEndOfVtBlock(byte[] inputBytes, byte[] outBuffer, int cbyteOut, byte[] marker, int i, byte[] anchor, List<int> anchors, List<int> footnotes, int ichStartVt)
		{
			// If all is not consistent, give up and let it fail at next stage.
			// If only one anchor, no need to fix.
			if (anchors.Count != footnotes.Count || anchors.Count < 2)
			{
				// no need to re-arrange, just copy everything since \vt
				cbyteOut = CopySegToOutput(inputBytes, outBuffer, cbyteOut, ichStartVt, i);
			}
			else
			{
				// Re-arrange so only one anchor per \vt.
				cbyteOut = CopySegToOutput(inputBytes, outBuffer, cbyteOut, ichStartVt, anchors[0] + anchor.Length);
				// the -1 should put a newline before the first \ft; copying up to footnotes[1] ends with a newline, too.
				cbyteOut = CopySegToOutput(inputBytes, outBuffer, cbyteOut, footnotes[0] - 1, footnotes[1]);
				footnotes.Add(i); // last footnote terminates at current character position.
				for (int ifn = 1; ifn < anchors.Count; ifn++)
				{
					// Copy an extra \vt.
					cbyteOut = CopySegToOutput(inputBytes, outBuffer, cbyteOut, ichStartVt, ichStartVt + marker.Length);
					// Copy the text between the previous footnote and this one, including the |f
					int ichStart = anchors[ifn - 1] + anchor.Length;
					// Drop one leading space or newline; newline between \vt blocks is equivalent.
					if (inputBytes[ichStart] == 32 || inputBytes[ichStart] == 10)
						ichStart++;
					cbyteOut = CopySegToOutput(inputBytes, outBuffer, cbyteOut, ichStart, anchors[ifn] + anchor.Length);
					// Copy the next footnote, including the preceding and following newlines
					cbyteOut = CopySegToOutput(inputBytes, outBuffer, cbyteOut, footnotes[ifn] - 1, footnotes[ifn + 1]);
				}
				if (anchors[anchors.Count - 1] + anchor.Length + 1 < footnotes[0])
				{
					int ichStart = anchors[anchors.Count - 1] + anchor.Length;
					// Drop one leading space or newline; newline between \vt blocks is equivalent.
					if (inputBytes[ichStart] == 32 || inputBytes[ichStart] == 10)
						ichStart++;
					if (ichStart + 1 < footnotes[0]) // further check in case ONLY one space
					{
						// We have text following the last anchor. Make yet another \vt
						cbyteOut = CopySegToOutput(inputBytes, outBuffer, cbyteOut, ichStartVt, ichStartVt + marker.Length);
						// And copy the trailing text. It runs from after the last anchor to before the first footnote body.
						cbyteOut = CopySegToOutput(inputBytes, outBuffer, cbyteOut, ichStart, footnotes[0]);
					}
				}
			}
			anchors.Clear();
			footnotes.Clear();
			return cbyteOut;
		}

		private static int CopySegToOutput(byte[] inputBytes, byte[] outBuffer, int cbyteOut, int ichStart, int ichLim)
		{
			for (int k = ichStart; k < ichLim; k++)
				outBuffer[cbyteOut++] = inputBytes[k];
			return cbyteOut;
		}

		private static bool FindMarker(byte[] inputBytes, int i, byte[] target)
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
			return gotIt;
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
		protected unsafe int Load(string strTablePath)
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
				return status;
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