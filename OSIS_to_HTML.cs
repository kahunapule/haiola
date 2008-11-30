using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Diagnostics;
using System.Collections;
using System.Xml;  // For Process class.
using System.Text.RegularExpressions;

namespace sepp
{
	/// <summary>
	/// This class uses an XSLT script and some custom programming to convert OSIS XML files to HTML
	/// </summary>
	public class OSIS_to_HTML : ExternalProgramConverter
	{
		string m_finalOutputDir;
		string m_copyright = "(�2007 UBB-GMIT)"; // default
		// Keys are book names used in references; values are HTM file names.
		Dictionary<string, string> m_files = new Dictionary<string, string>();
		// Key is file name, value is next file in sequence.
		Dictionary<string, string> m_nextFiles = new Dictionary<string, string>();
		private string m_prevFile = null;
		private Options m_options;

		/// <summary>
		/// initialize one.
		/// </summary>
		/// <param name="inputDirName"></param>
		/// <param name="outputDirName"></param>
		public OSIS_to_HTML(string inputDirName, string outputDirName, string finalOutputDirName, string optionsPath, Options options)
			: base(inputDirName, outputDirName)
		{
			m_finalOutputDir = finalOutputDirName;
			m_options = options;

			XmlDocument optionsDoc = new XmlDocument();
			optionsDoc.Load(optionsPath);
			XmlNode root = optionsDoc.DocumentElement;
			foreach (XmlNode node in root.ChildNodes)
			{
				switch (node.Name)
				{
					case "files":
						BuildFileList(node);
						break;
					case "copyright":
						m_copyright = node.InnerText;
						break;
				}
			}
		}

		internal override void Init()
		{
			base.Init();
			Utils.EnsureDirectory(m_finalOutputDir);
		}

		private const string IntroFileSuffix = "-Intro";

		protected override void ImmediatePostProcess(string inputFilePath, string outputFilePath)
		{
			base.ImmediatePostProcess(inputFilePath, outputFilePath);
			string baseFileName = Path.GetFileNameWithoutExtension(outputFilePath);
			string introName = MakeIntroFileName(baseFileName);
			string introPath = Path.Combine(Path.GetDirectoryName(outputFilePath), introName);
			RunProcess(inputFilePath, introPath, ToolPath, CreateIntroArguments(inputFilePath, introPath));
			if (!File.Exists(introPath))
				return;
			string introContents = ReadFileToString(introPath);
			if (introContents.Contains("<html"))
			{
				// We found some introductory material, adjust links and copy it to the output directory.
				MakeIntroHotLinks(introContents, introPath, baseFileName);
				if (m_finalOutputDir != null)
				{
					File.Copy(outputFilePath, Path.Combine(m_finalOutputDir, introName), true);
				}
			}
			else
			{
				// Empty file, get rid of it.
				File.Delete(introPath);
			}
		}

		public static string MakeIntroFileName(string baseFileName)
		{
			return Path.ChangeExtension(Path.GetFileNameWithoutExtension(baseFileName) + IntroFileSuffix, "htm");
		}


		private void BuildFileList(XmlNode node)
		{
			string prevFile = "none";// will become a dummy key
			foreach (XmlNode item in node.ChildNodes)
			{
				string fileName = item.Attributes["name"].Value;
				string htmlFileName = Path.ChangeExtension(fileName, "htm");
				XmlAttribute attr = item.Attributes["parallel"];
				if (attr != null)
				{
					string parallel = attr.Value;
					m_files[parallel] = htmlFileName;
				}
				m_nextFiles[prevFile] = htmlFileName;
				prevFile = htmlFileName;
			}
			m_nextFiles[prevFile] = null; // last file has no next.
		}



		internal override void DoPostProcessing(string outputFileName, string outputFilePath)
		{
			string input = ReadFileToString(outputFilePath);
			if (input == null)
			{
				ReportError("Could not do postprocessing because xslt produced no output");
				return; // couldn't complete initial stages?
			}
			input = CreateSymbolCrossRefs(input);
			string stage2 = FixDuplicateAnchors(input);

			MakeCrossRefLinkInfo(stage2, outputFilePath);

			if (m_options.ChapterPerFile)
			{
				string message = new ChapterSplitter(outputFilePath, m_options).Run(ref m_prevFile, m_nextFiles[outputFileName]);
				if (message == null)
				{
					File.Delete(outputFilePath);
					foreach (string itemPath in Utils.ChapFiles(outputFilePath))
						File.Copy(itemPath, Path.Combine(m_finalOutputDir, Path.GetFileName(itemPath)), true);
					return;
				}
				else
				{
					MessageBox.Show("Could not split " + outputFilePath + " - " + message, "Error"); // Enhance: provide more info.
				}
			}

			// And copy to the main output directory
			if (m_finalOutputDir != null)
			{
				File.Copy(outputFilePath, Path.Combine(m_finalOutputDir, outputFileName), true);
			}
		}

		string[] m_symbols; // shared with delegate

		private string CreateSymbolCrossRefs(string input)
		{
			string[] basicSymbols = { "*", "�", "�", "�" }; // enhance JohnT: could come from options
			// Total set of symbols is formed of original group, originals doubled, then all other combinations. Total # is n-squared plus n
			int totalSymbols = basicSymbols.Length * basicSymbols.Length + basicSymbols.Length;
			m_symbols = new string[totalSymbols];
			int next = basicSymbols.Length * 2; // start of subsequent symbols
			for (int i = 0; i < basicSymbols.Length; i++)
			{
				m_symbols[i] = basicSymbols[i];
				m_symbols[i + basicSymbols.Length] = basicSymbols[i] + basicSymbols[i];
				for (int j = 0; j < basicSymbols.Length; j++)
				{
					if (i != j) // those already used
						m_symbols[next++] = basicSymbols[i] + basicSymbols[j];
				}
			}
			Regex re = new Regex("<span class=\"notemark\">(.[^<]*)</span>");
			string output = re.Replace(input, new MatchEvaluator(ReplaceFootnote));
			return output;
		}

		/// <summary>
		/// Replace each footnote with the appropriate special marker
		/// </summary>
		/// <param name="m"></param>
		/// <returns></returns>
		public string ReplaceFootnote(Match m)
		{
			string notemark = m.Groups[1].Value;
			int index = Convert.ToInt32(notemark[0]) - Convert.ToInt32('a');
			if (notemark.Length == 1 && index >= 0 && index < m_symbols.Length)
				notemark = m_symbols[index];
			return "<span class=\"notemark\">" + notemark + "</span>";
		}

		/// <summary>
		/// If there are duplicate anchors in the file, disambguate by adding _1, etc.
		/// </summary>
		/// <param name="input"></param>
		/// <returns></returns>
		private string FixDuplicateAnchors(string input)
		{
			StringBuilder bldr = new StringBuilder(input.Length * 10 / 9 + 100);
			Hashtable anchors = new Hashtable();
			int prev = 0;
			string prefix = "<a name=\"";
			int start = input.IndexOf(prefix);
			while (start != -1)
			{
				start += prefix.Length; // character after prefix (after opening quote of name attr)
				bldr.Append(input.Substring(prev, start - prev)); // copy everything up to (and including) the opening quote
				int next = input.IndexOf("\"", start);
				if (next == -1)
				{
					// ouch! no end quote!
					next = input.Length;
				}
				string anchor = input.Substring(start, next - start); // actual text of anchor
				if (anchors.ContainsKey(anchor))
				{
					// bother! duplicate.
					string root = anchor;
					for (int i = 1; ;i++ )
					{
						anchor = root + "_" + i;
						if (!anchors.ContainsKey(anchor))
							break; // we must eventually find a non-duplicate!
					}
				}
				anchors[anchor] = true;
				bldr.Append(anchor);
				prev = next;
				start = input.IndexOf(prefix, next + 1);
			}
			bldr.Append(input.Substring(prev));
			return bldr.ToString();
		}

		private delegate string DoReplacements(string input);
		/// <summary>
		/// This looks for scripture references in the file and inserts info to allow the correct
		/// hot links to be made.
		/// </summary>
		/// <param name="input">input text to process</param>
		/// <param name="filePath">file to write to (often source of input also)</param>
		private void MakeCrossRefLinkInfo(string input, string filePath)
		{
			// Strings identifying elements that should be converted
			string[] starts = new string[] {
				"<p class=\"crossRefNote\"",
				"<div class=\"parallel\">",
				"<div class=\"parallelSub\">"
				};
			string[] ends = new string[] {
				"</p>",
				"</div>",
				"</div>"
			};
			ReplaceInSelectedElements(input, filePath, starts, ends, ConvertRefs);
		}

		/// <summary>
		/// Another user of ReplaceInSelectedElements, does a slightly different conversion on links in introduction list items.
		/// </summary>
		/// <param name="input">input text to process</param>
		/// <param name="destPath">file to write to (often source of input also)</param>
		private void MakeIntroHotLinks(string input, string destPath, string baseFileName)
		{
			// Strings identifying elements that should be converted
			string[] starts = new string[] {
				"<div class=\"introListItem\">"
				};
			string[] ends = new string[] {
				"</div>"
			};
			ReplaceInSelectedElements(input, destPath, starts, ends, new IntroRefConverter(this, baseFileName).ConvertIt);
		}

		class IntroRefConverter
		{
			private OSIS_to_HTML m_parent;
			string m_baseFileName;

			public IntroRefConverter (OSIS_to_HTML parent, string baseFileName)
			{
				m_parent = parent;
				m_baseFileName = baseFileName;
			}
			public string ConvertIt(string chunk)
			{
				return m_parent.ConvertOutlineRefs(chunk, m_baseFileName);
			}
		}

		/// <summary>
		/// In the input string, search for chunks that begin with one of the 'starts' strings and end with one of the 'ends' strings.
		/// (non-greedy matching).
		/// The text between each pair is replaced with whatever convert() produces.
		/// </summary>
		/// <param name="input"></param>
		/// <param name="filePath"></param>
		/// <param name="starts"></param>
		/// <param name="ends"></param>
		/// <param name="convert"></param>
		private void ReplaceInSelectedElements(string input, string filePath, string[] starts, string[] ends, DoReplacements convert)
		{
			Debug.Assert(starts.Length == ends.Length);
			TextWriter output = new StreamWriter(filePath, false, Encoding.UTF8);
			int start = 0;
			// offsets where we first found each kind of target element
			int[] offsets = new int[starts.Length];
			for (int i = 0; i < starts.Length; i++)
			{
				offsets[i] = input.IndexOf(starts[i]);
			}
			while (start < input.Length)
			{
				// pick the next thing to process by finding the smallest positive index in offsets
				int next = input.Length;
				int which = -1; // which offset was least
				for (int i = 0; i < starts.Length; i++)
				{
					if (offsets[i] > 0 && offsets[i] < next)
					{
						next = offsets[i];
						which = i;
					}
				}
				if (which == -1) // no more special chunks.
					break;
				int endChunk = input.IndexOf(ends[which], next + starts[which].Length);
				if (endChunk == -1)
				{
					// Huh?? No match?? give up.
					break;
				}
				// This is crude, but good enough for what we're doing. In all current cases, the bit we want to
				// convert is the body of the element, and comes after anything else. So the preceding > is the
				// start of what we want to work on.
				int startChunk = input.LastIndexOf(">", endChunk) + 1;
				if (startChunk >= 0 && startChunk > start)
				{
					// Copy everything we haven't processed up to the start of our chunk.
					output.Write(input.Substring(start, startChunk - start));
					start = endChunk; // Continue from here.
					string chunk = input.Substring(startChunk, endChunk - startChunk);
					output.Write(convert(chunk));
				} // otherwise it's a bizarre match to an unterminated element, don't mess with it.
				offsets[which] = input.IndexOf(starts[which], start + ends[which].Length);
			}
			output.Write(input.Substring(start)); // anything left over
			output.Close();
		}

		private static string ReadFileToString(string filePath)
		{
			TextReader reader = null;
			for (int attempt = 0; reader == null && attempt < 5; attempt++)
			{
				try
				{
					reader = new StreamReader(filePath, Encoding.UTF8);
				}
				catch (FileNotFoundException)
				{
					// Since another process just created this file, it seems we sometimes don't see it at once.
					reader = null;
					System.Threading.Thread.Sleep(500);
				}
			}
			if (reader == null)
				return null;
			string input = reader.ReadToEnd();
			reader.Close();
			return input;
		}

		/// <summary>
		/// Convert references. A simple input is something like "Lukas 12:3". Output would then be
		/// <a href="KUP-MAT-Final-Q.htm#C12V3>Lucas 12:3</a>.
		/// The name conversion is performed by finding "Lukas" in m_files.
		/// But it's more complicated than that; we get cases like
		/// (Mateos 26:26-29; Markus 14:22-25; Lukas 22:14-20) -- extra punctuation and multiple refs
		/// (Carita Ulang so'al Jalan Idop 4:35,39; 6:4) - name not found!
		/// (1 Korintus 5:1-13) - range!
		/// (Utusan dong pung Carita 22:6-16, 26:12-18) - list of refs in same book
		/// " Efesus 5:22, Kolose 3:18" - commas separate complete refs!
		/// "Hahuu (J�nesis , Kejadian ) 15:13-14; �zodu (Keluaran ) 3:12" - book name is complex and has comma!
		/// The algorithm is:
		/// 0. Wherever a recognized book name occurs, change it to something definitely not containing problem
		/// punctuation: #%#%bookN
		/// 1. Split the string at semi-colons or commas; handle each one independently,
		/// except if we get a book or chapter, remember for subsequent ones.
		/// 2. Each item from the above is split at commas. Consider all to come from same book.
		/// 3. In first of each comma group, search for a match for known book name. If found, use longest.
		/// 4. Convert occurrences of #%#%bookN back.
		/// </summary>
		/// <param name="chunk1"></param>
		/// <returns></returns>
		private string ConvertRefs(string chunk1)
		{
			string chunk = chunk1;
			int ibook = 0;
			Dictionary<string, string> subsBookToFile = new Dictionary<string, string>(m_files.Count);
			foreach (string bookName in m_files.Keys)
			{
				string subskey = "#%#%book" + ibook;
				subsBookToFile[subskey] = m_files[bookName];
				chunk = chunk.Replace(bookName, subskey);
				ibook++;
			}
			string[] mainRefs = chunk.Split(';');
			StringBuilder output = new StringBuilder(chunk.Length * 5 + 50);
			// Ref may be simple verse number, chapter:verse, verse-verse, chapter:verse-verse
			Regex reRef = new Regex("[0-9]+(:[0-9]+)?(-[0-9]+)?");
			Regex reNum = new Regex("[0-9]+");
			Regex reAlpha = new Regex(@"\w");
			string fileName = ""; // empty until we get a book name match.
			// This is both a default for books that don't have chapters, and also,
			// if we find a chapter in ANY reference, we keep it for subsequent ones.
			// This handles cases like Matt 26:3,4 (which gets split into two items by the comma).
			string chap = "1";
			foreach (string item in mainRefs)
			{
				// Put back the semi-colons we split on.
				if (output.Length > 0)
					output.Append(";");
				string[] refs = item.Split(',');
				bool fFirst = true;
				foreach (string target in refs)
				{
					if (!fFirst)
					{
						output.Append(","); // put these back too.
					}
					string match = "";
					int matchOffset = -1;
					foreach (string bookName in subsBookToFile.Keys)
					{
						if (bookName.Length > match.Length)
						{
							int matchOffsetT = target.IndexOf(bookName);
							if (matchOffsetT >= 0)
							{
								matchOffset = matchOffsetT;
								match = bookName;
								fileName = subsBookToFile[match];
							}
						}
					}
					if (fileName == "")
					{
						// haven't found a book name, here or in previous item; don't convert this item
						output.Append(target);
						fFirst = false;
						continue;
					}

					// Look for something like a reference. Also, check that we don't have
					// alphabetic text that did NOT match one of our books if we didn't match
					// a book; otherwise, something like Titus 4:2; Isaiah 12:3 makes both links
					// to Titus. Note that we take the last match for the reference, otherwise, 1 Timothy 2:4
					// finds the '1' as the reference. Grr.
					int startNumSearch = 0;
					if (matchOffset >= 0)
						startNumSearch = matchOffset + match.Length;
					MatchCollection matches = reRef.Matches(target, startNumSearch);
					Match m = null;
					if (matches.Count != 0)
						m = matches[matches.Count - 1];
					if (m == null || (matchOffset < 0 && reAlpha.Match(target, 0, m.Index) != Match.Empty))
					{
						// Nothing looks like a reference, just output what we have.
						// Also, stop carrying book and chapter forward.
						fileName = "";
						chap = "1";
						output.Append(target);
						fFirst = false;
						continue;
					}
					// Construct the anchor.
					string[] parts = m.Value.Split(':');
					string anchor;
					string verse = parts[0];
					// Do NOT reset chap unless two parts; see above.
					if (parts.Length == 2)
					{
						chap = parts[0];
						verse = parts[1];
					}
					verse = reNum.Match(verse).Value; // Take the first number in the verse part.
					anchor = fileName + "#C" + chap + "V" + verse;

					// The anchor starts at the beginning of the numeric reference, unless we
					// matched a book name, in which case, start at the beginning of if.
					int start = m.Index;
					if (matchOffset >= 0)
					{
						start = matchOffset;
					}
					InsertHotLink(output, target, m, start, anchor);
					fFirst = false;
				}
			}
			string result = output.ToString();
			ibook = 0;
			foreach (string bookName in m_files.Keys)
			{
				result = result.Replace("#%#%book" + ibook, bookName);
				ibook++;
			}

			return result;
		}

		/// <summary>
		/// Convert references as used in introductory outlines. A typical example would be "1. Wniak kalwieda. (1:1-2)"
		/// We want the output to be 1. Wniak kalwieda. <a href="#C1V1>(1:1-2)</a>.
		/// </summary>
		/// <param name="chunk"></param>
		/// <returns></returns>
		private string ConvertOutlineRefs(string chunk, string baseFileName)
		{
			// Looks for parenthesized expression containing (verse) number, or C:V, possibly followed by
			// a range indication. We don't care much about the range, so once we have C:V if that can be found,
			// we accept any combination of digits, colon, and hyphen
			Regex reRef = new Regex(@"\(([0-9]+)(:[0-9]+)?(-[0-9: ]+)?\)");
			Match match = reRef.Match(chunk);
			if (!match.Success)
				return chunk;
			StringBuilder output = new StringBuilder(chunk.Length + 40);
			string chap = "1";
			string verse = match.Groups[1].Value;
			if (match.Groups.Count > 2 && match.Groups[2].Length > 0)
			{
				chap = verse;
				verse = match.Groups[2].Value.Substring(1); // strip colon
			}
			string destFileName = baseFileName;
			if (m_options.ChapterPerFile)
				destFileName = ChapterSplitter.MakeNameForSegment(destFileName, chap);
			destFileName = Path.ChangeExtension(destFileName, "htm");

			InsertHotLink(output, chunk, match, match.Index, destFileName + "#C" + chap + "V" + verse);

			return output.ToString();
		}
		/// <summary>
		/// Insert into the string builder a string formed by replacing (in input) from start to the end of what m matched
		/// with a hot link.
		/// The anchor to which it links is supplied; the body of the link is what was replaced.
		/// </summary>
		/// <param name="output"></param>
		/// <param name="input"></param>
		/// <param name="input"></param>
		/// <param name="m"></param>
		/// <param name="anchor"></param>
		private void InsertHotLink(StringBuilder output, string input, Match m, int start, string anchor)
		{
			// Put anything in the input before the reference
			output.Append(input.Substring(0, start));
			// The next bit will be part of the anchor, so start it.
			output.Append("<a href=\"");
			output.Append(anchor);
			output.Append("\">");
			// The bit that should be the text of the anchor: input from start to end of reference.
			output.Append(input.Substring(start, m.Index + m.Length - start));
			// terminate the anchor
			output.Append("</a>");
			// And add anything else, possibly final punctuation
			output.Append(input.Substring(m.Index + m.Length));
		}


		internal override string ToolPath
		{
			get { return "msxsl.exe"; }
		}

		internal override string[] Extensions
		{
			get { return new string[] { "*.xml" }; }
		}

		internal override string OutputExtension
		{
			get { return "htm"; }
		}

		internal override string CreateArguments(string inputFilePath, string outputFilePath)
		{
			// Look for an override of osis2Html, if not found use standard one.
			string scriptPath = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(inputFilePath)), "osis2Html.xsl");
			if (!File.Exists(scriptPath))
				scriptPath = Path.GetFullPath(@"..\..\osis2Html.xsl");
			return "\"" + inputFilePath + "\" \"" + scriptPath + "\" -o \"" + outputFilePath + "\" copyright=\"" + m_copyright + "\"";

		}

		// Arguments for running the script that tries to extract an Introduction from the input.
		private string CreateIntroArguments(string inputFilePath, string outputFilePath)
		{
			string scriptPath = Path.GetFullPath(@"..\..\osis2Intro.xsl");
			return "\"" + inputFilePath + "\" \"" + scriptPath + "\" -o \"" + outputFilePath + "\" copyright=\"" + m_copyright + "\"";
		}

		internal override bool CheckToolPresent
		{
			get
			{
				return false; // can't readily check because we don't know where msxsl.exe should be
			}
		}
	}

}