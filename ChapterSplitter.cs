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
		private string m_nextFile; // Exact name of file for next link in last chapter.
		// Dictionary built while writing chapter files mapping anchors (the value of the name attribute of <a> elements)
		// to the file containing them. This is used in patching hrefs in the TOC.
		Dictionary<string, string>m_anchorToFile = new Dictionary<string, string>();
		private Options m_options;
		/// <summary>
		/// Make one.
		/// </summary>
		/// <param name="inputPath"></param>
		public ChapterSplitter(string inputPath, Options options)
		{
			m_inputPath = inputPath;
			m_options = options;
			m_inputFileName = Path.GetFileNameWithoutExtension(inputPath);
			m_outputDir = Path.GetDirectoryName(m_inputPath);
		}

		internal const string tocTag = "TOC";
		
		/// <summary>
		/// Run the breakdown...return true if successful.
		/// </summary>
		/// <returns></returns>
		public bool Run(ref string prevFile, string nextFile)
		{
			m_prevFile = prevFile;
			if (nextFile != null)
			{
				m_nextFile = BuildNextFileLinkTargetName(Path.Combine(m_outputDir, nextFile));
			}
			// Read file into input
			StreamReader reader = new StreamReader(m_inputPath, Encoding.UTF8);
			m_input = reader.ReadToEnd();
			reader.Close();

			if (!GetMainElements())
				return false;
			BuildFootnoteMap();
			GetChapterAnchors();
			if (m_chapAnchorIndexes.Count == 0)
				return false;
			MakeChapterFiles();
			if (m_toc != null)
				MakeTocFile(); // Depends on info set up by MakeChapterFiles!
			prevFile = m_prevFile;
			return true;
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
							return Path.GetFileNameWithoutExtension(path) + "-" + tocTag + ".htm";
						else
							return Path.GetFileNameWithoutExtension(path) + "-" + m.Groups[1] + ".htm";
					}
				}

			}
			finally
			{
				reader.Close();
			}
			return null;
		}

		private void MakeTocFile()
		{
			string nextFile = m_nextFile;
			if (m_chapNames.Count > 0)
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
			MakeChapterBody(toc.ToString(), tocTag, nextFile);
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
			Regex reDiv = new Regex(@"<(/?)div[^>]*>",RegexOptions.RightToLeft);
			// Tests for a string ending in <div class="text">, optionally followed by another <div> element, with optional white space
			Regex reDivTextAtEnd = new Regex("<div [^>]*class=\"text\"[^>]*>\\s*(<div[^>]*>\\s*)?\\G", RegexOptions.RightToLeft);
			// Finds the last text division header.
			Regex reLastDivText = new Regex("<div [^>]*class=\"text\"[^>]*>", RegexOptions.RightToLeft);
			// loop deliberately omits last chapter
			for (int ichapter = 0; ichapter < m_chapNames.Count - 1; ichapter++)
			{
				int endOfChapter = m_chapAnchorIndexes[ichapter + 1];
				Match lastDiv = reDivTextAtEnd.Match(m_body, endOfChapter);
				string chapContents;
				if (lastDiv.Success)
				{
					// Chapter break is at start of text div. Include in this chapter any previous non-text divs.
					// This is by far the common case.
					endOfChapter = lastDiv.Index;
					int depth = 0; // incremented for </div>
					for (Match m = reDiv.Match(m_body, startOfChapter, endOfChapter - startOfChapter);
					     m.Success;
					     m = m.NextMatch())
					{
						if (m.Groups[1].Length > 0)
							depth++; // working backwards, end of div increases depth
						else
							depth--; // forwards, start of div decreases depth.
						// Don't consider breaking the chapter at a div that is nested inside a previous main div.
						if (depth != 0)
							continue;
						string divHeader = m.ToString();
						if (divHeader.IndexOf("class=\"text\"") > 0)
						{
							break; // found a preceding div we do NOT want to include in following chapter
						}
						endOfChapter = m.Index; // unless we find we can include more in the following chapter.
					}
					chapContents = GetBaseChapContents(endOfChapter, startOfChapter, needExtraDivAtStartOfChap, nestedDivHeader);
				}
				else
				{
					// chapter does not start at beginning of text division. It includes everything up to the chapter anchor,
					chapContents = GetBaseChapContents(endOfChapter, startOfChapter, needExtraDivAtStartOfChap, nestedDivHeader);
					// and also something to close off any open divisions...
					Match lastDivMatch = reLastDivText.Match(m_body, endOfChapter);
					int startLastDivBody = lastDivMatch.Index + lastDivMatch.Length;
					string lastDivBody = m_body.Substring(startLastDivBody, endOfChapter - startLastDivBody);
					string nestedDivTrailer;
					nestedDivHeader = GetNestedDivInfo(lastDivBody, out nestedDivTrailer);
					chapContents += nestedDivTrailer;
				}
				MakeChapterBody(chapContents, m_chapNames[ichapter], 
					Path.GetFileName(BuildOutputPathname(m_chapNames[ichapter + 1])));
				startOfChapter = endOfChapter;
			}
			// The final chapter uses the start information as usual, but extends to the end of the whole body.
			MakeChapterBody(GetBaseChapContents(m_body.Length, startOfChapter, needExtraDivAtStartOfChap, nestedDivHeader),
			                m_chapNames[m_chapNames.Count - 1], m_nextFile);
			return true;
		}

		// Given a piece of the input that corresponds to one chapter, make a file out of it.
		private void MakeChapterBody(string chapContents, string chapId, string nextFile)
		{
			chapContents = FixFileCrossRefs(chapContents);
			string outputPath = BuildOutputPathname(chapId);
			AddAnchorsToMap(chapContents, outputPath);
			StreamWriter output = new StreamWriter(outputPath, false, Encoding.UTF8);
			output.Write(m_header);
			MakeNavigationDiv(output, nextFile);
			output.Write(chapContents);
			MakeNavigationDiv(output, nextFile);
			string footnotes = FootnotesFor(chapContents);
			output.Write(footnotes);
			if (footnotes.Length > 0) // Enhance: maybe check for some minimum number of lines?
				MakeNavigationDiv(output, nextFile);
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

		private void MakeNavigationDiv(StreamWriter output, string nextFile)
		{
			output.Write("\n<div class=\"navButtons\">\n");
			if (m_prevFile != null)
				output.Write(MakeButton(m_options.PreviousChapterText, m_prevFile));
			if (m_nextFile != null)
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
			Regex reCrossRef = new Regex("(href=\"[^\"]+)(\\.htm#C(\\d+)V\\d+)");
			int start = 0;
			for (Match m = reCrossRef.Match(input); m.Success; m = m.NextMatch())
			{
				output.Append(input.Substring(start, m.Index - start));
				start = m.Index + m.Length;
				output.Append(m.Groups[1].ToString()); // pattern up to end of filename w/o extension
				output.Append("-");
				output.Append(m.Groups[3].ToString()); // chapter number
				output.Append(m.Groups[2].ToString()); // rest of pattern
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

		private string GetNestedDivInfo(string lastDivBody, out string nestedDivTrailer)
		{
			List<string> openDivElts = new List<string>();
			Regex reDiv = new Regex(@"<(/?)div[^>]*>");
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
				}
			}
			string result = "";
			nestedDivTrailer = "";
			foreach (string divHeader in openDivElts)
			{
				result += divHeader;
				nestedDivTrailer += "</div>";
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
