using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace sepp
{
	/// <summary>
	/// Takes an HTML file representing one book of Scripture and splits it into one file per chapter plus one TOC file.
	/// The original file is deleted.
	/// </summary>
	class ChapterSplitter
	{
		private string m_inputPath;
		private string m_outputDir;
		private string m_inputFileName;
		string m_input;
		private string m_header;
		private string m_toc;
		private string m_body;
		private string m_footnoteText = "";
		private List<int> m_chapAnchorIndexes = new List<int>(); // index of opening angle bracket of <a name="Cn"> elements
		private List<string> m_chapNames = new List<string>();
		private Dictionary<string, string> m_footnotes = new Dictionary<string, string>();
		private Dictionary<string, string> m_crossRefs = new Dictionary<string, string>();
		private string m_prevFile; // Exact name (with chapter number) of target file for previous link in first chapter; or null if none
		private string m_prevMainFile; // Name of chapter before current book.
		private string m_nextMainFile; // Exact name of file for next link in last chapter.
		// Dictionary built while writing chapter files mapping anchors (the value of the name attribute of <a> elements)
		// to the file containing them. This is used in patching hrefs in the TOC.
		Dictionary<string, string>m_anchorToFile = new Dictionary<string, string>();
		private OSIS_to_HTML m_parent;
		private Options m_options;
	    private string m_pageFooter;
		/// <summary>
		/// Make one.
		/// </summary>
		/// <param name="inputPath"></param>
		public ChapterSplitter(string inputPath, Options options, string pageFooter)
		{
			m_inputPath = inputPath;
			m_options = options;
			m_inputFileName = Path.GetFileNameWithoutExtension(inputPath);
			m_outputDir = Path.GetDirectoryName(m_inputPath);
		    m_pageFooter = pageFooter;
		}

		internal const string tocTag = "TOC";
		
		/// <summary>
		/// Run the breakdown...return null if successful, error message otherwise.
		/// </summary>
		/// <returns></returns>
		public string Run(ref string prevFile, string nextFile, OSIS_to_HTML parent)
		{
			m_parent = parent;
			m_prevFile = m_prevMainFile = prevFile;
			if (nextFile != null)
			{
				m_nextMainFile = BuildNextFileLinkTargetName(Path.Combine(m_outputDir, nextFile));
			}
			// Read file into input
			StreamReader reader = new StreamReader(m_inputPath, Encoding.UTF8);
			m_input = reader.ReadToEnd();
			reader.Close();

			if (!GetMainElements())
				return "problems parsing HTML--could not find <body> element";
			BuildFootnoteMap();
			GetChapterAnchors();
			if (m_chapAnchorIndexes.Count == 0)
				return "no chapters found";
			if (m_parent.KeepAsSingleFile(m_inputFileName))
			{
				MakeSingleFile();
				prevFile = m_prevFile;
			}
			else
			{
				// break into chapters
				MakeChapterFiles();
				prevFile = m_prevFile; // Return for next book the last chaper of this (not the TOC!)
				if (m_toc != null)
					MakeTocFile(); // Depends on info set up by MakeChapterFiles!
			}
			return null;
		}

		/// <summary>
		/// Make a single file, as there is only one chapter.
		/// For consistency with BuildNextFileLinkTargetName, it will be called TOC if there is a TOC, otherwise, _1.
		/// </summary>
		private void MakeSingleFile()
		{
			// Determine the name that the previous file will use to link to this.
			// This is a bit redundant, but more robust than other approaches.
			string outputFile = BuildNextFileLinkTargetName(Path.Combine(m_outputDir, m_inputFileName));
			MakeChapterBodyPath(m_toc + m_body, Path.Combine(m_outputDir, outputFile), GetPrevFileName(-1), m_nextMainFile);
		}

		/// <summary>
		/// Given the path to the next (unsplit) file, generate the file name we should link to
		/// after it is split. We search for a table, which would be the TOC, and if we find it,
		/// return a link to the TOC file. Otherwise, we find the first chapter, and link to its file.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		internal static string BuildNextFileLinkTargetName(string path)
		{
			// If the next file does not exist, probably because we're doing a single file out of a group, we can
			// only guess. Hope this is a normal NT with section titles.
			if (!File.Exists(path))
				return Path.GetFileNameWithoutExtension(path) + "-" + tocTag + ".htm";
			Regex re = new Regex("<table|<a name=\"C(\\d*)\"");
			StreamReader reader = new StreamReader(path, Encoding.UTF8);
			try
			{
				while (!reader.EndOfStream)
				{
					Match m = re.Match(reader.ReadLine());
					if (m.Success)
					{
						if (m.ToString() == "<table")
							return MakeNameForSegment(path, tocTag);
						else
							return MakeNameForSegment(path, m.Groups[1].Value);
					}
				}

			}
			finally
			{
				reader.Close();
			}
			return null;
		}

		/// <summary>
		/// Make a name for the file that is the specified part of the specified base file.
		/// </summary>
		public static string MakeNameForSegment(string baseName, string segmentId)
		{
			return Path.ChangeExtension(Path.GetFileNameWithoutExtension(baseName) + "-" + segmentId, "htm");
		}

		private void MakeTocFile()
		{
			string nextFile = m_nextMainFile;
			if (m_chapNames.Count > 1)
				nextFile = Path.GetFileName(BuildOutputPathname(m_chapNames[0]));
			// Scan m_toc for HREFs which need to have the appropriate filename inserted, and do it.
			// This is any href that is to an anchor in 'the current file' (starts with #) where the anchor is now in one of the chapter files.
			StringBuilder toc = new StringBuilder(m_toc.Length * 4/3 + 100);
			Regex reHref = new Regex("<a href=\"#([^\"]+)\"");
			int start = 0;
			for (Match m = reHref.Match(m_toc); m.Success; m = m.NextMatch() )
			{
				toc.Append(m_toc.Substring(start, m.Index - start)); // copy to start of match
				start = m.Index + m.Length; // next copy starts after pattern
				toc.Append("<a href=\"");
				string fileName = "";
				string anchor = m.Groups[1].ToString();
				m_anchorToFile.TryGetValue(anchor, out fileName);
				toc.Append(fileName);
				toc.Append("#");
				toc.Append(anchor);
				toc.Append("\"");
				toc.Append(m.Groups);
			}
			toc.Append(m_toc.Substring(start, m_toc.Length - start));
			MakeChapterBody(toc.ToString(), tocTag, GetPrevFileName(-1), nextFile);
		}

		// Each <p> element in the foonotes list is captured using its id as a key.
		private void BuildFootnoteMap()
		{
			Regex reNotePara = new Regex("<p class=\"([^\"]+)\" id=\"([^\"]+)\".*?</p>");
			for (Match m = reNotePara.Match(m_footnoteText); m.Success; m = m.NextMatch())
			{
				if (m.Groups[1].ToString() == "footnote")
					m_footnotes[m.Groups[2].ToString()] = m.ToString();
				else
					m_crossRefs[m.Groups[2].ToString()] = m.ToString();
			}
		}

		/// <summary>
		/// Break the input file down into m_header (up to end of opening tag of body element), m_toc (to the end of the first table),
		/// and m_body (the balance up to the end of body)
		/// </summary>
		/// <returns></returns>
		bool GetMainElements()
		{
			try
			{
				int position = m_input.IndexOf("<body");
				position = m_input.IndexOf(">", position) + 1;
				m_header = m_input.Substring(0, position);

				string endTableTag = "</table>";
				int lastPosition = position;
				position = m_input.IndexOf(endTableTag);
				if (position > 0)
				{
					position += endTableTag.Length;
					m_toc = m_input.Substring(lastPosition, position - lastPosition);
					lastPosition = position;
				}

				string footnoteElt = "<div class=\"footnotes\">";
				position = m_input.LastIndexOf(footnoteElt);
				if (position == -1)
				{
					position = m_input.LastIndexOf("</body>");
					m_body = m_input.Substring(lastPosition, position - lastPosition);
				}
				else
				{
					m_body = m_input.Substring(lastPosition, position - lastPosition);  // body extends up to footnotes
					lastPosition = position + footnoteElt.Length;
					position = m_input.IndexOf("</div>", lastPosition);
					m_footnoteText = m_input.Substring(lastPosition, position - lastPosition);
				}
			}
			catch (Exception)
			{
				// If we don't find the required pieces, fail.
				return false;
			}
			return true;
		}

		// Get the positions of the anchors that look like <a name="Cnn"> (1-3 digits, actually, but accept any number 1 or more)
		// along with the value of the number.
		private void GetChapterAnchors()
		{
			Regex matcher = new Regex("<a name=\"C(\\d+)\"/?>");
			foreach (Match m in matcher.Matches(m_body))
			{
				m_chapAnchorIndexes.Add(m.Index);
				m_chapNames.Add(m.Groups[1].ToString());
			}
		}


		// The first chapter begins at 0 in m_body; the last ends at the end of m_body.
		// Other chapters typically have their anchor as the first non-white thing in a <div class="text">.
		// If so, that chapter includes any preceding <div>s with any other class.
		// Otherwise, the chapter begins right at the anchor.
		// We also have to keep track of the stack of open elements, since each chapter's string
		// has to close off whatever was open when it started.
		private bool MakeChapterFiles()
		{
			// Start first chapter at first div...this skips any space, hr, etc normally between TOC and chapter 1.
			int startOfChapter = Math.Max(0, m_body.IndexOf("<div"));
			bool needExtraDivAtStartOfChap = false;
			string nestedDivHeader = "";
            // match a complete (opening or closing) <div> element, incluing attributes and the angle brackets.
			Regex reDiv = new Regex(@"<(/?)div[^>]*>",RegexOptions.RightToLeft);
			// Tests for a string ending in </div> with optional white space
			Regex reDivTextAtEnd = new Regex("</div>\\s*\\G", RegexOptions.RightToLeft);
			// Finds the last text division header.
			Regex reLastDivText = new Regex("<div [^>]*class=\"text\"[^>]*>", RegexOptions.RightToLeft);
			// loop deliberately omits last chapter.
			// Each iteration determines the break at the end of chapter i, and in the process, the start of chapter i+1.
			// needExtraDivAtStartOfChap is set true when the break is not at a text div boundary, in order
			// that the extra open div might be put in the following chapter.
			for (int ichapter = 0; ichapter < m_chapNames.Count - 1; ichapter++)
			{
				int endOfChapter = m_chapAnchorIndexes[ichapter + 1];
				Match lastDiv = reDivTextAtEnd.Match(m_body, endOfChapter);
				string chapContents;
				if (lastDiv.Success)
				{
					// Chapter break is between divisions. Break exactly at the anchor.
					// This is by far the common case.
					chapContents = GetBaseChapContents(endOfChapter, startOfChapter, needExtraDivAtStartOfChap, nestedDivHeader);
					// The next chapter
					needExtraDivAtStartOfChap = false;
				}
				else
				{
					// chapter does not start between text divisions. It includes everything up to the chapter anchor,
					chapContents = GetBaseChapContents(endOfChapter, startOfChapter, needExtraDivAtStartOfChap, nestedDivHeader);
				    // and also something to close off any open divisions...
					Match lastDivTextMatch = reLastDivText.Match(m_body, endOfChapter);
					int startLastDivBody = lastDivTextMatch.Index + lastDivTextMatch.Length;
					string lastDivBody = m_body.Substring(startLastDivBody, endOfChapter - startLastDivBody);
					string nestedDivTrailer;
				    bool chapSplitsDiv; // true if we want to append the rest of the split division as overlap
					nestedDivHeader = GetNestedDivInfo(lastDivBody, out nestedDivTrailer, out chapSplitsDiv);

                    // Include the rest of the current div, but marked up for exclusion from concording (and possibly different style)
                    if (chapSplitsDiv)
                    {
                        int endpara = m_body.IndexOf("</div>", endOfChapter);
                        if (endpara > 0)
                        {
                            string tailOfChapter = m_body.Substring(endOfChapter, endpara - endOfChapter);
                            chapContents += "<span class=\"overlap\">" + tailOfChapter + "</span>";
                        }
                    }
				    chapContents += nestedDivTrailer;
					needExtraDivAtStartOfChap = true;
				}
				MakeChapterBody(chapContents, m_chapNames[ichapter], GetPrevFileName(ichapter),
					Path.GetFileName(BuildOutputPathname(m_chapNames[ichapter + 1])));
				startOfChapter = endOfChapter;
			}
			// The final chapter uses the start information as usual, but extends to the end of the whole body.
			MakeChapterBody(GetBaseChapContents(m_body.Length, startOfChapter, needExtraDivAtStartOfChap, nestedDivHeader),
			                m_chapNames[m_chapNames.Count - 1], GetPrevFileName(m_chapNames.Count - 1), m_nextMainFile);
			return true;
		}

		/// <summary>
		/// Get the appropriate previous file name. Depends on chapter index (-1 for toc). Assumes chapters except toc
		/// are written in order, so m_prevFile contains appropriate value for most. We want toc AND first chapter to
		/// point back to the previous book.
		/// </summary>
		/// <param name="ichapter"></param>
		/// <returns></returns>
		string GetPrevFileName(int ichapter)
		{
			if (ichapter < 0)
				return m_prevMainFile;
			if (ichapter > 0)
				return m_prevFile;
			// ichapter is 0: first chapter
			if (String.IsNullOrEmpty(m_toc))
				return m_prevMainFile;
			return Path.GetFileName(BuildOutputPathname(tocTag));
		}

		// Given a piece of the input that corresponds to one chapter, make a file out of it.
		private void MakeChapterBody(string chapContents, string chapId, string prevFile, string nextFile)
		{
			MakeChapterBodyPath(chapContents, BuildOutputPathname(chapId), prevFile, nextFile);
		}
		private void MakeChapterBodyPath(string chapContents, string outputPath, string prevFile, string nextFile)
		{
			chapContents = FixFileCrossRefs(chapContents);
			AddAnchorsToMap(chapContents, outputPath);
			StreamWriter output = new StreamWriter(outputPath, false, Encoding.UTF8);
			output.Write(m_header);
			MakeNavigationDiv(output, prevFile, nextFile);
			output.Write(chapContents);
			MakeNavigationDiv(output, prevFile, nextFile);
			string footnotes = FootnotesFor(chapContents);
			output.Write(FixFileCrossRefs(footnotes));
			if (footnotes.Length > 0) // Enhance: maybe check for some minimum number of lines?
				MakeNavigationDiv(output, prevFile, nextFile);
            if (m_pageFooter != null)
                output.Write(m_pageFooter);
			output.Write("</body></html>\n");
			output.Close();
			m_prevFile = Path.GetFileName(outputPath);
		}

		private void AddAnchorsToMap(string chapContents, string path)
		{
			string value = Path.GetFileName(path);
			Regex reAnchor = new Regex("<a name=\"([^\"]+)\"");
			foreach (Match m in reAnchor.Matches(chapContents))
				m_anchorToFile[m.Groups[1].ToString()] = value;
		}

		private string BuildOutputPathname(string chapId)
		{
			return Path.Combine(m_outputDir, m_inputFileName + "-" + chapId + ".htm");
		}

		private void MakeNavigationDiv(StreamWriter output, string prevFile, string nextFile)
		{
			output.Write("\n<div class=\"navButtons\">\n");
			if (prevFile != null)
				output.Write(MakeButton(m_options.PreviousChapterText, prevFile));
			if (nextFile != null)
				output.Write(MakeButton(m_options.NextChapterText, nextFile));
			output.Write("</div >\n");
		}

		private string MakeButton(string title, string destFile)
		{
		   return "<input type=\"button\" value=\"" + title + "\" title=\"" + title + "\""
			+ " onclick=\"location='" + destFile + "'\"/>\n";
		}

		private string FixFileCrossRefs(string input)
		{
			StringBuilder output = new StringBuilder();
			Regex reCrossRef = new Regex("(href=\"([^\"]+))(\\.htm#C(\\d+)V\\d+)");
			int start = 0;
			for (Match m = reCrossRef.Match(input); m.Success; m = m.NextMatch())
			{
				output.Append(input.Substring(start, m.Index - start));
				start = m.Index + m.Length;
				output.Append(m.Groups[1].ToString()); // pattern up to end of filename w/o extension
				output.Append("-");
				if (m_parent.KeepAsSingleFile(m.Groups[2].ToString())) // the actual file name
					output.Append(tocTag);
				else
					output.Append(m.Groups[4].ToString()); // chapter number
				output.Append(m.Groups[3].ToString()); // rest of pattern
			}
			output.Append(input.Substring(start, input.Length - start));// balance of input
			return output.ToString();
		}

		// Generate a foonotes section for the specified chapter.
		// For each caller copy the appropriate footnote to the result.
		private string FootnotesFor(string chapContents)
		{
			Regex reCaller = new Regex("<a href=\"#(FN[^\"]+)\"");
			StringBuilder footnotes = new StringBuilder();
			StringBuilder crossRefs = new StringBuilder();
			for (Match m = reCaller.Match(chapContents); m.Success; m = m.NextMatch())
			{
				string footnote;
				if (m_footnotes.TryGetValue(m.Groups[1].ToString(), out footnote))
					footnotes.AppendLine(footnote);
				if (m_crossRefs.TryGetValue(m.Groups[1].ToString(), out footnote))
					crossRefs.AppendLine(footnote);
			}
			if (footnotes.Length + crossRefs.Length != 0)
				return "<div class=\"footnotes\">\n" + footnotes.ToString() + crossRefs.ToString() + "</div>\n";
			return "";
		}

        // Return the extra information we want to add at the start of the next chapter, and also the trailer
        // we need to put at the end of this chapter, to close any open divs properly in this chapter,
        // and re-open them at the start of the next. In addition, the body of the last div is included
        // in the return value, marked with a special class.
        // Exception: if the chapter anchor occurs right at the start of a <div>, we don't duplicate it, and return chapSplitsDiv false.
		private string GetNestedDivInfo(string lastDivBody, out string nestedDivTrailer, out bool chapSplitsDiv)
		{
			List<string> openDivElts = new List<string>();
			Regex reDiv = new Regex(@"<(/?)div[^>]*>");
		    int ichLastDivStart = -1;
			for (Match m = reDiv.Match(lastDivBody); m.Success; m = m.NextMatch())
			{
				if (m.Groups[1].Length > 0)
				{
					// a close-of-div
					openDivElts.RemoveAt(openDivElts.Count - 1);
				}
				else
				{
					openDivElts.Add(m.ToString());
				    ichLastDivStart = m.Index + m.Length;
				}
			}
			string result = "";
			nestedDivTrailer = "";
			foreach (string divHeader in openDivElts)
			{
				result += divHeader;
				nestedDivTrailer += "</div>";
			}
		    chapSplitsDiv = false;
            if (ichLastDivStart >= 0)
            {
                string overlap = lastDivBody.Substring(ichLastDivStart);
                chapSplitsDiv = !Regex.IsMatch(overlap, "^\\s$");
                if (chapSplitsDiv)
                    result += "<span class=\"overlap\">" + overlap + "</span>";
            }
		    return result;
		}

		private string GetBaseChapContents(int endOfChapter, int startOfChapter, bool needExtraDivAtStartOfChap, string nestedDivHeader)
		{
			string chapContents = m_body.Substring(startOfChapter, endOfChapter - startOfChapter);
			if (needExtraDivAtStartOfChap)
			{
				chapContents = "<div class=\"text\">" + nestedDivHeader + chapContents;
			}
			return chapContents;
		}

	}
}
