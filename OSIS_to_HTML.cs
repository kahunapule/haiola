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
	/// This class uses some CC files to convert from the OurWord SF model to USFM.
	/// CC tables provided initially by John Duerksen.
	/// Convertes all .db files in input directory.
	/// </summary>
	public class OSIS_to_HTML
	{
		string m_inputDirName;
		string m_outputDirName;
		string m_finalOutputDir;
		// Keys are book names used in references; values are HTM file names.
		Dictionary<string, string> m_files = new Dictionary<string, string>();

		/// <summary>
		/// initialize one.
		/// </summary>
		/// <param name="inputDirName"></param>
		/// <param name="outputDirName"></param>
		public OSIS_to_HTML(string inputDirName, string outputDirName, string finalOutputDirName, string optionsPath)
		{
			m_inputDirName = inputDirName;
			m_outputDirName = outputDirName;
			m_finalOutputDir = finalOutputDirName;

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
				}
			}
		}

		private void BuildFileList(XmlNode node)
		{
			foreach (XmlNode item in node.ChildNodes)
			{
				string fileName = item.Attributes["name"].Value;
				XmlAttribute attr = item.Attributes["parallel"];
				if (attr == null)
					continue;
				string parallel = attr.Value;
				m_files[parallel] = Path.ChangeExtension(fileName, "htm");
			}
		}

		/// <summary>
		/// Run the algorithm (on the intersection of the files in the list and those in the directory).
		/// </summary>
		public void Run(IList files)
		{
			string[] inputFileNames = Directory.GetFiles(m_inputDirName, "*.xml");
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
			string outputFileName = Path.ChangeExtension(Path.GetFileName(inputFilePath), "htm");
			File.Delete(outputFileName); // Make sure we don't somehow preserve an old version.
			string outputFilePath = Path.Combine(m_outputDirName, outputFileName);
			string scriptPath = Path.GetFullPath(@"..\..\osis2Html.xsl");
			string args = "\"" + inputFilePath + "\" \"" + scriptPath + "\" -o \"" + outputFilePath + "\"";
			ProcessStartInfo info = new ProcessStartInfo("msxsl", args);
			info.WindowStyle = ProcessWindowStyle.Minimized;
			//info.RedirectStandardError = 
			Process proc = Process.Start(info);
			proc.WaitForExit();

			MakeCrossRefLinkInfo(outputFilePath);

			// And copy to the main output directory
			if (m_finalOutputDir != null)
			{
				File.Copy(outputFilePath, Path.Combine(m_finalOutputDir, outputFileName), true);
			}
		}

		/// <summary>
		/// This looks for scripture references in the file and inserts info to allow the correct
		/// hot links to be made.
		/// </summary>
		/// <param name="outputFilePath"></param>
		private void MakeCrossRefLinkInfo(string filePath)
		{
			TextReader reader = null;
			for (int attempt = 0; reader == null && attempt < 5; attempt++)
			{
				try
				{
					reader = new StreamReader(filePath, Encoding.UTF8);
				}
				catch (FileNotFoundException f)
				{
					// Since another process just created this file, it seems we sometimes don't see it at once.
					reader = null;
					System.Threading.Thread.Sleep(500);
				}
			}
			string input = reader.ReadToEnd();
			reader.Close();
			TextWriter output = new StreamWriter(filePath, false, Encoding.UTF8);
			int start = 0;
			// Strings identifying elements that should be converted
			string[] starts = new string[] {
				"<p class=\"crossRefNote\">",
				"<div class=\"parallel\">",
				"<div class=\"parallelSub\">"
				};
			string[] ends = new string[] {
				"</p>",
				"</div>",
				"</div>"
			};
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
				// start of what we want to work on. This must be findable, because all our 'start' strings have
				// a closing angle bracket. (Maintain this property!)
				int startChunk = input.LastIndexOf(">", endChunk) + 1;
				// Copy everything we haven't processed up to the start of our chunk.
				output.Write(input.Substring(start, startChunk - start));
				start = endChunk; // Continue from here.
				string chunk = input.Substring(startChunk, endChunk - startChunk);
				output.Write(ConvertRefs(chunk));
				offsets[which] = input.IndexOf(starts[which], start + ends[which].Length);
			}
			output.Write(input.Substring(start)); // anything left over
			output.Close();



			//XmlDocument doc = new XmlDocument();
			//doc.Load(outputFilePath);
			//bool fChanges = false;
			//foreach (XmlNode parallel in doc.SelectNodes("title[@type='parallel']"))
			//{
			//    XmlNode reference = null;
			//    foreach (XmlNode child in parallel.ChildNodes)
			//    {
			//        if (child.Name == "reference")
			//        {
			//            reference = child;
			//            break;
			//        }
			//    }
			//    if (reference == null)
			//        continue;
			//    string source = reference.Value;
			//}
			//if (fChanges)
			//{
			//    XmlWriter writer = new XmlTextWriter(outputFilePath, Encoding.UTF8);
			//    doc.WriteTo(writer);
			//}
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
		/// The algorithm is:
		/// 1. Split the string at semi-colons or commas; handle each one independently,
		/// except if we get a book or chapter, remember for subsequent ones.
		/// 2. Each item from the above is split at commas. Consider all to come from same book.
		/// 3. In first of each comma group, search for a match for known book name. If found, use longest.
		/// </summary>
		/// <param name="chunk"></param>
		/// <returns></returns>
		private string ConvertRefs(string chunk)
		{
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
					foreach (string bookName in m_files.Keys)
					{
						if (bookName.Length > match.Length)
						{
							int matchOffsetT = target.IndexOf(bookName);
							if (matchOffsetT >= 0)
							{
								matchOffset = matchOffsetT;
								match = bookName;
								fileName = m_files[match];
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
					// a book; otherwise, something like Titus 4:2; Isaih 12:3 makes both links
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
					// Put anything in the input before the reference
					output.Append(item.Substring(0, start));
					// The next bit will be part of the anchor, so start it.
					output.Append("<a href=\"");
					output.Append(anchor);
					output.Append("\">");
					// The bit that should be the text of the anchor: input from start to end of reference.
					output.Append(target.Substring(start, m.Index + m.Length - start));
					// terminate the anchor
					output.Append("</a>");
					// And add anything else, possibly final punctuation
					output.Append(target.Substring(m.Index + m.Length));
					fFirst = false;
				}
			}
			string result = output.ToString();
			return result;
		}

	}

}