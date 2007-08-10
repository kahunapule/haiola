using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.IO;
using System.Windows.Forms;
using System.Collections;

namespace sepp
{
	/// <summary>
	/// This class builds a concordance of a set of HTML files.
	/// Currently it is generally necessary to first 'tidy' the files, typically using
	/// tidy -asxhtml -raw -o x.xml x.htm
	/// Then manually delete the initial DOCTYPE element from the resulting XML files.
	/// Then run the program and click the Build Concordance button
	/// Then copy to the output directory
	///		- the input htm files
	///		- display.css (or whatever stylesheet they need)
	///		- concMain.htm, dummyConc.htm, dummyInner.htm, dummyText.htm, indexconc.htm
	/// The master file to open in the browser is indexconc.htm
	/// 
	/// For the tree-based index, copy to the output directory
	///		- mktree.js, mktree.css, plus.gif, minus.gif, bullet.gif
	///		- treeMaster.htm, treeMain.htm, treeConc.htm, treeText.htm
	/// The master file to open in the browser for this version is treeconc.htm
	/// </summary>
	public class ConcGenerator
	{
		string m_inputDirName;
		string m_outputDirName;

		// The information we accumulate during parsing is basically a list of wordforms, each with a list of occurrences,
		// each storing file, chapter, and verse.
		Dictionary<string, WordformInfo> m_occurrences = new Dictionary<string, WordformInfo>();

		// tracks current state
		string m_inputFile;
		int m_chapter;
		string m_verse;
		string m_htmlFile; // input file without path and with htm extension.

		int m_wordListFileCount = 1;

		StringBuilder m_context = new StringBuilder();
		List<WordOccurrence> m_pendingOccurrences = new List<WordOccurrence>(); // don't yet have context.

		// Options
		bool m_mergeCase; // if wordforms differing only by case occur merge them
		string m_wordformingChars; // overrides, list of characters that should be considered word-forming in defiance of Unicode.
		int m_maxFrequency = Int32.MaxValue; // exclude words occurring more often than this.
		Dictionary<string, bool> m_excludeWords = new Dictionary<string,bool>(); // key is words to exclude, ignore value
		Dictionary<string, bool> m_excludeClasses = new Dictionary<string, bool>(); // exclude elements with these names
		List<string> m_files = new List<string>(); // Files to process in desired order.
		Dictionary<string, string> m_abbreviations = new Dictionary<string,string>(); // Key is file name, value is abbreviation to use in refs.

		List<string> m_pendingExclusions = new List<string>(); // names of elemenents opened that must close before we restart.
		// Entries represent wordforms in lower case (wordform.ToLower()) where we have not yet encountered a lowercase version
		// of the word in the text, but have encountered an other-case version. Key is the lowercase version, value is the
		// uppercase version.
		// If we encounter two uppercase versions which map to the same LC wordform we arbitrarily use the one that occurs first.
		Dictionary<string, string> m_uppercaseForms = new Dictionary<string, string>();
		string m_bookChapText; // text for the 'Books and Chapters' hot link, from options file.

		private string AttVal(XmlNode node, string name, string defVal)
		{
			XmlAttribute att = node.Attributes[name];
			if (att != null)
				return att.Value;
			return defVal;
		}

		/// <summary>
		/// Add to dictionary each of the space-separated words in the list (with value true).
		/// </summary>
		/// <param name="dict"></param>
		/// <param name="list"></param>
		private void BuildDictionary(Dictionary<string, bool> dict, string list)
		{
			foreach (string key in list.Split(' '))
				dict[key] = true;
		}

		public ConcGenerator(string inputDirName, string outputDirName, string optionsPath)
		{
			m_inputDirName = inputDirName;
			m_outputDirName = outputDirName;
			XmlDocument optionsDoc = new XmlDocument();
			optionsDoc.Load(optionsPath);
			XmlNode root = optionsDoc.DocumentElement;
			foreach (XmlNode node in root.ChildNodes)
			{
				switch (node.Name)
				{
					case "options":
						m_mergeCase = AttVal(node, "mergeCase", "false") == "true";
						m_wordformingChars = AttVal(node, "wordFormingCharacters", "");
						break;
					case "excludeWords":
						string maxFreq = AttVal(node, "moreFrequentThan", "unlimited");
						m_maxFrequency = maxFreq == "unlimited" ? Int32.MaxValue : Int32.Parse(maxFreq);
						BuildDictionary(m_excludeWords,node.InnerText);
						break;
					case "excludeClasses":
						BuildDictionary(m_excludeClasses,node.InnerText);
						break;
					case "files":
						BuildFileList(node);
						break;
					case "bookChap":
						m_bookChapText = node.InnerText;
						break;
				}
			}
		}

		private void BuildFileList(XmlNode node)
		{
			foreach (XmlNode item in node.ChildNodes)
			{
				string fileName = item.Attributes["name"].Value;
				string abbr = item.Attributes["abbr"].Value;
				m_files.Add(fileName);
				m_abbreviations[Path.ChangeExtension(fileName, "htm")] = abbr;
			}
		}

		public void Run(IList files)
		{
			Progress status = new Progress(files.Count);
			status.Text = "Parsing";
			status.Show();
			int count = 0;
			foreach (string inputFilePath in files)
			{
				status.File = inputFilePath;
				Parse(Path.Combine(m_inputDirName, inputFilePath));
				count++;
				status.Value = count;
			}
			status.Close();
			List<WordformInfo> sortedOccurrences = new List<WordformInfo>(m_occurrences.Count);
			foreach (WordformInfo info in m_occurrences.Values)
			{
				if (info.Occurrences.Count > m_maxFrequency)
					continue;
				if (m_excludeWords.ContainsKey(info.Form))
					continue;
				sortedOccurrences.Add(info);
			}
			sortedOccurrences.Sort();
			status = new Progress(sortedOccurrences.Count);
			status.Text = "Generating";
			status.Show();
			count = 0;
			// Must do this before making index files, it sets FileNumber property in each item.
			foreach (WordformInfo item in sortedOccurrences)
			{
				MakeOccurrenceFile(item);
				if (count % 10 == 0)
				{
					status.File = item.Form;
					status.Value = count;
				}
				count++;
			}

			MakeIndexFiles(sortedOccurrences);
			MakeTreeIndex(sortedOccurrences);

			status.Close();
		}

		private void MakeIndexFiles(List<WordformInfo> sortedOccurrences)
		{
			double count = sortedOccurrences.Count;
			int groupSize = Convert.ToInt32(Math.Sqrt(count));
			int iStartGroup = 0;
			string header = "<!doctype HTML PUBLIC \"-//W3C//DTD HTML 4.0 Transitional//EN\">\n<html>\n<body>\n";
			string trailer = "</body>\n</html>\n";
			string pathMain = Path.Combine(m_outputDirName, "concIndexBar.htm");
			TextWriter writerMain = new StreamWriter(pathMain, false, Encoding.UTF8);
			writerMain.Write(header);

			int groupIndex = 1;
			for (int cGroupsRemaining = groupSize; cGroupsRemaining > 0; cGroupsRemaining--)
			{
				// Enhance: adjust group size to give better breakdown, e.g., by first letter or first two letters.
				int cThisGroup = (sortedOccurrences.Count - iStartGroup) / cGroupsRemaining;
				if (cThisGroup > 0)
				{
					string groupFileName = "index" + groupIndex + ".htm";
					WordformInfo firstItemInGroup = sortedOccurrences[iStartGroup];
					WordformInfo lastItemInGroup = sortedOccurrences[iStartGroup + cThisGroup - 1];
					writerMain.Write("<a href=\"{0}\" target=\"inner\">{1} - {2}</a><br/>\n",
						new object[] { groupFileName, MakeSafeXml(firstItemInGroup.Form), MakeSafeXml(lastItemInGroup.Form) });
					WriteInnerIndexFile(groupFileName, sortedOccurrences, groupIndex, iStartGroup, cThisGroup);
				}
				iStartGroup += cThisGroup;
				groupIndex++;
			}
			writerMain.Write(trailer);
			writerMain.Close();
		}

		private void MakeTreeIndex(List<WordformInfo> sortedOccurrences)
		{
			double count = sortedOccurrences.Count;
			int groupSize = Convert.ToInt32(Math.Sqrt(count));
			int iStartGroup = 0;
			string header = "<!doctype HTML PUBLIC \"-//W3C//DTD HTML 4.0 Transitional//EN\">\n<html>\n"
				+ "<head>\n\t<link rel=\"stylesheet\" type=\"text/css\" href=\"mktree.css\">\n\t"
				+ "<script type=\"text/javascript\" src=\"mktree.js\"></script>\n</head>\n<body>\n"
				+ "<p><a target=\"_top\" href=\"Root.htm\">" + m_bookChapText + "</a></p>\n"
				+"<ul class=\"mktree\">\n";

			string trailer = "</ul>\n</body>\n</html>\n";
			string pathMain = Path.Combine(m_outputDirName, "concTreeIndex.htm");
			TextWriter writerMain = new StreamWriter(pathMain, false, Encoding.UTF8);
			writerMain.Write(header);

			int groupIndex = 1;
			for (int cGroupsRemaining = groupSize; cGroupsRemaining > 0; cGroupsRemaining--)
			{
				// Enhance: adjust group size to give better breakdown, e.g., by first letter or first two letters.
				int cThisGroup = (sortedOccurrences.Count - iStartGroup) / cGroupsRemaining;
				if (cThisGroup > 0)
				{
					WordformInfo firstItemInGroup = sortedOccurrences[iStartGroup];
					WordformInfo lastItemInGroup = sortedOccurrences[iStartGroup + cThisGroup - 1];
					writerMain.Write("<li>{0} - {1}<ul>\n",
						new object[] { MakeSafeXml(firstItemInGroup.Form), MakeSafeXml(lastItemInGroup.Form) });
					WriteInnerIndexItems(writerMain, sortedOccurrences, groupIndex, iStartGroup, cThisGroup);
					writerMain.Write("</ul></li>\n");
				}
				iStartGroup += cThisGroup;
				groupIndex++;
			}
			writerMain.Write(trailer);
			writerMain.Close();
		}

		private void WriteInnerIndexItems(TextWriter writer, List<WordformInfo> sortedOccurrences, int groupIndex,
			int iStartGroup, int cThisGroup)
		{
			for (int i = iStartGroup; i < iStartGroup + cThisGroup; i++)
			{
				WordformInfo item = sortedOccurrences[i];
				writer.Write("<li><a href=\"wl{0}.htm\" target=\"conc\">{1} ({2})</a></li>\n",
					new object[] { item.FileNumber, MakeSafeXml(item.Form), item.Occurrences.Count });
			}
		}

		private void WriteInnerIndexFile(string groupFileName, List<WordformInfo> sortedOccurrences, int groupIndex,
			int iStartGroup, int cThisGroup)
		{
			string header = "<!doctype HTML PUBLIC \"-//W3C//DTD HTML 4.0 Transitional//EN\">\n<html>\n<body>\n";
			string trailer = "</body>\n</html>\n";
			string path = Path.Combine(m_outputDirName, "index" + groupIndex + ".htm");
			TextWriter writer = new StreamWriter(path, false, Encoding.UTF8);
			writer.Write(header);

			for (int i = iStartGroup; i < iStartGroup + cThisGroup; i++)
			{
				WordformInfo item = sortedOccurrences[i];
				writer.Write("<a href=\"wl{0}.htm\" target=\"conc\">{1}</a><br/>\n",
					new object[] { item.FileNumber, MakeSafeXml(item.Form) });
			}
			writer.Write(trailer);
			writer.Close();
		}

		private void Parse(string inputFile)
		{
			m_inputFile = inputFile;
			m_htmlFile = Path.ChangeExtension(Path.GetFileName(inputFile), "htm");
			m_chapter = -1;
			m_verse = "";
			m_context.Remove(0, m_context.Length);
			m_saveContext = "";
 			XmlReaderSettings settings = new XmlReaderSettings();
			settings.ConformanceLevel = ConformanceLevel.Fragment;
			settings.IgnoreComments = true;
			settings.ProhibitDtd = false;
			settings.ValidationType = ValidationType.None;
			settings.ConformanceLevel = ConformanceLevel.Fragment;
			TextReader input = new StreamReader(inputFile, Encoding.UTF8);
			input.ReadLine(); // Skip the HTML DOCTYPE, which the XmlReader can't cope with.
			input.ReadLine();
			XmlReader reader = XmlReader.Create(input, settings);
			while(reader.Read())
			{
				switch (reader.NodeType)
				{
					case XmlNodeType.Element:
						ProcessElement(reader);
						break;
					case XmlNodeType.EndElement:
						ProcessEndElement(reader);
						break;
					case XmlNodeType.Text:
						ProcessText(reader.Value);
						break;
						// Review JohnT: do we need to process white space elements?
				}
			}
			ProcessEndOfSentence(); // In case text does not end with sentence-final punctuation, don't want wordforms with no context.
		}

		/// <summary>
		/// Fix the string to be safe in a text region of XML.
		/// </summary>
		/// <param name="sInput"></param>
		/// <returns></returns>

		public static string MakeSafeXml(string sInput)
		{
			string sOutput = sInput;

			if (sOutput != null && sOutput.Length != 0)
			{
				sOutput = sOutput.Replace("&", "&amp;");
				sOutput = sOutput.Replace("<", "&lt;");
				sOutput = sOutput.Replace(">", "&gt;");
			}
			return sOutput;
		}

		/// <summary>
		/// Make an HTML file containing the occurrences of a particular wordform, with links to the text and click actions
		/// to highlight the key word in the destination file.
		/// 
		/// Enhance: now we have the global variables for curWord and curFlags, we needn't pass that as arguments
		/// to the individual sel() functions, which will make files smaller.
		/// We could make a global variable indicating the current word in the main text pane, and only run
		/// the algorithm when it changes. That would save compute time on the client.
		/// </summary>
		/// <param name="info"></param>
		private void MakeOccurrenceFile(WordformInfo info)
		{
			List<WordOccurrence> items = info.Occurrences;
			string flags = info.MixedCase ? "i" : "";
			string header = "<!doctype HTML PUBLIC \"-//W3C//DTD HTML 4.0 Transitional//EN\">\n<html>\n"
				+ "<head><script src=\"ConcFuncs.js\" type=\"text/javascript\"></script>"
				+ string.Format("<script type=\"text/javascript\">var curWord = \"{0}\"; var curFlags = \"{1}\"</script>", MakeSafeXml(info.Form), flags)
				+ "</head>\n<body>\n";
			string trailer = "</body>\n</html>\n";
			string path = Path.Combine(m_outputDirName, "wl" + m_wordListFileCount.ToString() + ".htm");
			info.FileNumber = m_wordListFileCount;
			m_wordListFileCount++;
			TextWriter writer = new StreamWriter(path, false, Encoding.UTF8);
			writer.Write(header);
			foreach (WordOccurrence item in items)
			{
				writer.Write(m_abbreviations[item.FileName]);
				writer.Write(" ");
				writer.Write(item.Chapter);
				writer.Write(".");
				writer.Write(MakeSafeXml(item.Verse));
				writer.Write(": ");
				WritePrecedingContext(writer, item.Context.Substring(0, item.Offset));
				writer.Write("<a href=\"{0}#C{1}V{2}\" target=\"main\" onclick='sel(\"{3}\",\"{4}\")'>{3}</a>",
					new object[] { item.FileName, item.Chapter, item.FirstVerse, MakeSafeXml(item.Form), flags });
				//writer.Write(item.Context.Substring(item.Offset + key.Length, item.Context.Length - item.Offset - key.Length));
				WriteFollowingContext(writer, item.Context.Substring(item.Offset + info.Form.Length, item.Context.Length - item.Offset - info.Form.Length));
				writer.Write("<br/>\n");
			}
			writer.Write(trailer);
			writer.Close();
		}

		const int kMaxContextLength = 40;
		const int kMinContextLength = 35;
		private static void WritePrecedingContext(TextWriter writer, string context)
		{
			if (context.Length < kMaxContextLength)
			{
				writer.Write(MakeSafeXml(context));
				return;
			}
			int iWhiteSpace = -1;
			for (int i = context.Length - kMaxContextLength; i < context.Length - kMinContextLength; i++)
			{
				if (Char.IsWhiteSpace(context[i]))
				{
					iWhiteSpace = i + 1;
					break;
				}
			}
			if (iWhiteSpace > 0)
			{
				while (iWhiteSpace < context.Length && Char.IsWhiteSpace(context[iWhiteSpace]))
					iWhiteSpace++;
				writer.Write(MakeSafeXml(context.Substring(iWhiteSpace, context.Length - iWhiteSpace)));
			}
			else
			{
				writer.Write("...");
				writer.Write(MakeSafeXml(context.Substring(context.Length - kMaxContextLength + 3, kMaxContextLength - 3)));
			}
		}

		private static void WriteFollowingContext(TextWriter writer, string context)
		{
			if (context.Length < kMaxContextLength)
			{
				writer.Write(MakeSafeXml(context));
				return;
			}
			int iWhiteSpace = -1;
			for (int i = kMaxContextLength - 1; i >= kMinContextLength; i--)
			{
				if (Char.IsWhiteSpace(context[i]))
				{
					iWhiteSpace = i;
					break;
				}
			}
			if (iWhiteSpace >= 0)
			{
				while (iWhiteSpace > 0 && Char.IsWhiteSpace(context[iWhiteSpace - 1]))
					iWhiteSpace--;
				writer.Write(MakeSafeXml(context.Substring(0, iWhiteSpace)));
			}
			else
			{
				writer.Write(MakeSafeXml(context.Substring(0, kMaxContextLength - 3)));
				writer.Write("...");
			}
		}

		private bool IsLetter(char c)
		{
			if (m_wordformingChars.IndexOf(c) >= 0)
				return true;
			return Char.IsLetter(c);
		}

		private void ProcessText(string text)
		{
			if (m_chapter < 0 || m_pendingExclusions.Count > 0)
				return; // not currently Scripture text we want to concord (or include in context).
			EnsureWhiteSpaceEndsContext();
			char prev = ' ';
			int startWord = -1;
			int added = 0; // index of first character not yet added to context.
			for (int i = 0; i < text.Length; i++)
			{
				char current = text[i];
				if (IsLetter(current))
				{
					if (!IsLetter(prev))
					{
						// start of word
						startWord = i;
						added = UpdateContext(text, added, i);
					}
				}
				else
				{
					if (IsLetter(prev))
					{
						// end of word. Note that this check must be BEFORE we do any special handling related to
						// the exact nature of the following character.
						string wordform = text.Substring(startWord, i - startWord);
						ProcessWordform(wordform);
					}
					if (current == '.' || current == '!' || current == '?') // enhance JohnT: more generaly sentence separator set.
					{
						added = UpdateContext(text, added, i + 1); // include the punctuation
						ProcessEndOfSentence();
					}
					else if (current == '\n')
					{
						// add any non-wordforming text we already processed
						added = UpdateContext(text, added, i);
						added++; // so we won't subsequently copy the newline
						// If there's something there and it isn't already white space, put a space in the context
						// to stand for the linebreak.
						EnsureWhiteSpaceEndsContext();
					}
				}
				prev = current;
			}
			if (IsLetter(prev))
			{
				// process final wordform
				string wordform = text.Substring(startWord, text.Length - startWord);
				ProcessWordform(wordform);
			}
			UpdateContext(text, added, text.Length);
		}

		/// <summary>
		/// About to add text following markup or newline to the context; insert a space if it doesn't end
		/// with something white.
		/// </summary>
		private void EnsureWhiteSpaceEndsContext()
		{
			if (m_context.Length != 0 && !Char.IsWhiteSpace(m_context[m_context.Length - 1]))
			{
				m_context.Append(' ');
			}
		}

		private int UpdateContext(string text, int added, int i)
		{
			m_context.Append(text.Substring(added, i - added));
			added = i;
			return added;
		}

		string m_saveContext = "";

		/// <summary>
		/// We've just added end-of-sentence punctuation to the context.
		/// Set it as the context of any pending wordforms, and reset it.
		/// </summary>
		private void ProcessEndOfSentence()
		{
			string context = m_context.ToString();
			m_context.Remove(0, m_context.Length); // reset for next time
			int cInitialPunct = 0;
			// This loop strips from the start of context trailing punctuation from the previous sentence.
			// The loop terminates when it finds a non-punctuation non-white character.
			// The correction is made only if we find a white space character following some punctuation
			// before the first non-white.
			// The bit to strip from this context is everything up to the last white space character before
			// the first non-white non-punct.
			for (int i = 0; i < context.Length; i++)
			{
				if (Char.IsWhiteSpace(context[i]))
				{
					cInitialPunct = i + 1;
					continue;
				}
				if (!Char.IsPunctuation(context[i]))
					break;
			}
			string nextSave = "";
			if (cInitialPunct > 0)
			{
				// Save for next context stuff up to and including character cSave
				int cSave = cInitialPunct - 1;
				while (cSave > 0 && Char.IsWhiteSpace(context[cSave + 1]))
					cSave--;
				nextSave = context.Substring(0, cSave);
			}
			context = context.Substring(cInitialPunct) + m_saveContext;
			m_saveContext = nextSave;
			foreach (WordOccurrence w in m_pendingOccurrences)
			{
				w.Context = context;
				w.Offset = w.Offset - cInitialPunct;
			}
			m_pendingOccurrences.Clear();
		}

		private void ProcessWordform(string wordform)
		{
			WordformInfo info;
			if (!m_occurrences.TryGetValue(wordform, out info))
			{
				if (m_mergeCase)
				{
					string wordformLC = wordform.ToLower(System.Globalization.CultureInfo.InvariantCulture); // review JohnT: should we select a culture?
					if (wordformLC != wordform)
					{
						// It's an upper case form of some sort.
						if (m_occurrences.TryGetValue(wordformLC, out info))
						{
							// We already have an entry for the LC version of this word; the new occurrence just gets added to it.
							// This accomplishes the merging of cases. Note that we do have a mixture, if we don't already know it.
							info.MixedCase = true;
						}
						else
						{
							// We don't have an entry for the LC version of the word. Do we have one for some other UC version?
							string existingUCform;
							if (m_uppercaseForms.TryGetValue(wordformLC, out existingUCform))
							{
								// bizarre...we have more than one UC version of the wordform, since we don't have an entry for the current
								// one but do for SOME UC version. Use the existing one. This makes it a bit arbitraray whether we will get an
								// entry for 'James' or 'JAMES' if both occur, but hopefully that is rare. If necessary enhance to allow a list
								// of strings as the value in m_uppercaseForms, but then we may need to merge them all if we see a corresponding
								// LC form at last.
								info = m_occurrences[existingUCform];
								info.MixedCase = true;
							}
							else
							{
								// a truly new UC form, we've never seen this before. Make a new one and note the correspondence.
								info = new WordformInfo(wordform);
								m_occurrences[wordform] = info;
								m_uppercaseForms[wordformLC] = wordform;
							}
						}
					}
					else
					{
						// a new wordform, equal to its own ToLower(). Have we already seen a UC version?
						string existingUCform;
						if (m_uppercaseForms.TryGetValue(wordform, out existingUCform))
						{
							// We're seeing for the first time a lower case form of a word we've already made a list of occurrences
							// for in its upper case form.
							// Here we actually do the merging of cases. We keep using the existing WordformInfo, but change its form.
							info = m_occurrences[existingUCform];
							m_occurrences.Remove(existingUCform);
							m_occurrences[wordform] = info; // save again with different key
							info.Form = wordform; // replaces the UC form.
							info.MixedCase = true;
						}
						else
						{
							// a new LC form not matching anything; just make an entry.
							info = new WordformInfo(wordform);
							m_occurrences[wordform] = info;
						}
					}
				}
				else
				{
					// Not merging case, just make a new WordformInfo for the new form
					info = new WordformInfo(wordform);
					m_occurrences[wordform] = info;
				}
			}
			WordOccurrence item = new WordOccurrence(m_htmlFile, m_chapter, m_verse, m_context.Length, wordform);
			info.Occurrences.Add(item);
			m_pendingOccurrences.Add(item);
		}

		// This version is suitable for the World English Bible format, where the CV anchors are generated by JavaScript.
		// Hence we are looking for something like <script ...>cb(2,5)/>
		//private void ProcessElement(XmlReader reader)
		//{
		//    if (reader.Name == "script")
		//    {
		//        string script = reader.ReadString().Trim().Trim(new char[] { '\n', '/', ';' });
		//        if ((script.StartsWith("cb(") || script.StartsWith("cj("))
		//            && script.EndsWith(")"))
		//        {
		//            string refSource = script.Substring(3, script.Length - 4);
		//            string[] parts = refSource.Split(',');
		//            if (parts.Length == 2)
		//            {
		//                m_chapter = Int32.Parse(parts[0]);
		//                m_verse = Int32.Parse(parts[1]);
		//            }
		//        }
		//    }
		//}

		// This version is for the OSIStoHTML converter, which inserts literal anchors <a name="C2V5"/>
		private void ProcessElement(XmlReader reader)
		{
			if (reader.Name == "a")
			{
				string name = reader.GetAttribute("name");
				if (name != null && name.StartsWith("C"))
				{
					string refSource = name.Substring(1, name.Length - 1); // strip of 'C'
					string[] parts = refSource.Split('V');
					if (parts.Length == 2)
					{
						m_chapter = Int32.Parse(parts[0]);
						m_verse = parts[1]; // don't try to parse this, may be complex, eg. 11-12
					}
				}
			}
			else
			{
				string className = reader.GetAttribute("class");
				if (className != null && m_excludeClasses.ContainsKey(className))
				{
					// Prevents processing wordforms until we find the corresponding end marker.
					m_pendingExclusions.Add(reader.Name);
				}
			}
		}

		private void ProcessEndElement(XmlReader reader)
		{
			if (m_pendingExclusions.Count > 0 && reader.Name == m_pendingExclusions[m_pendingExclusions.Count - 1])
				m_pendingExclusions.RemoveAt(m_pendingExclusions.Count - 1);
		}
	}
}
