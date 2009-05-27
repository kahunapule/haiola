using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.IO;
using System.Windows.Forms;
using System.Collections;
using System.Globalization;

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
		public enum IndexTypes
		{
			rangeTree, // a tree where the roots are ranges of words, equal in length.
			alphaTree, // a tree where the roots are initial letters of the alphabet.
			alphaTreeMf, // Looks like alphaTree, but done with multiple files for faster loading.
			twoLevelRange // a top-level index using equal word ranges, with multiple second-level files for individual words
		}
		string m_inputDirName;
		string m_outputDirName;

		// The information we accumulate during parsing is basically a list of wordforms, each with a list of occurrences,
		// each storing file, chapter, and verse.
		Dictionary<string, WordformInfo> m_occurrences = new Dictionary<string, WordformInfo>();

		// tracks current state
		string m_inputFile;
		int m_chapter;
		string m_verse;
		string m_anchor; // full text of last anchor seen
		string m_htmlFile; // input file without path and with htm extension.

		int m_wordListFileCount = 1;

		StringBuilder m_context = new StringBuilder();
		List<WordOccurrence> m_pendingOccurrences = new List<WordOccurrence>(); // don't yet have context.

		// Options
		bool m_mergeCase; // if wordforms differing only by case occur merge them
		string m_wordformingChars; // overrides, list of characters that should be considered word-forming in defiance of Unicode.
		int m_maxFrequency = Int32.MaxValue; // exclude words occurring more often than this.
		Dictionary<string, bool> m_excludeWords = new Dictionary<string,bool>(); // key is words to exclude, ignore value
		Dictionary<string, bool> m_excludeClasses = new Dictionary<string, bool>(); // exclude elements with these classes
		Dictionary<string, bool> m_nonCanonicalClasses = new Dictionary<string, bool>(); // elements with these classes are not canonical
		List<string> m_files = new List<string>(); // Files to process in desired order.
		Dictionary<string, string> m_abbreviations = new Dictionary<string,string>(); // Key is file name, value is abbreviation to use in refs.
		int m_maxContextLength = 50;
		int m_minContextLength = 35;
		IndexTypes m_indexType = IndexTypes.alphaTree;
		string m_notesClass; // element with this class contains footnotes; references should not be output.
		string m_notesRef; // use this string as the 'reference' for words within notesClass.
		string m_headingRef; // use this string as the 'reference' for other non-Canonical words.
		IComparer<string> m_comparer = StringComparer.InvariantCultureIgnoreCase; // a default

		List<string> m_pendingExclusions = new List<string>(); // names of elemenents opened that must close before we restart.
		List<string> m_pendingNonCanonical = new List<string>(); // names of open elemenents that indicate non-canonical text.
		// Entries represent wordforms in lower case (wordform.ToLower()) where we have not yet encountered a lowercase version
		// of the word in the text, but have encountered an other-case version. Key is the lowercase version, value is the
		// uppercase version.
		// If we encounter two uppercase versions which map to the same LC wordform we arbitrarily use the one that occurs first.
		Dictionary<string, string> m_uppercaseForms = new Dictionary<string, string>();
		string m_bookChapText; // text for the 'Books and Chapters' hot link, from options file.

		public ConcGenerator(string inputDirName, string outputDirName, string optionsPath, Options options)
		{
			m_options = options;
			m_inputDirName = inputDirName;
			m_outputDirName = outputDirName;
			// Enhance JohnT: move options reading into Options class.
			XmlDocument optionsDoc = new XmlDocument();
			optionsDoc.Load(optionsPath);
			XmlNode root = optionsDoc.DocumentElement;
			foreach (XmlNode node in root.ChildNodes)
			{
				switch (node.Name)
				{
					case "options":
						m_mergeCase = Utils.AttVal(node, "mergeCase", "false") == "true";
						m_wordformingChars = Utils.AttVal(node, "wordFormingCharacters", "");
						m_maxContextLength = Utils.IntAttVal(node, "maxContext", m_maxContextLength);
						m_minContextLength = Utils.IntAttVal(node, "minContext", m_minContextLength);
						string indexType = Utils.AttVal(node, "indexType", "alphaTree");
						switch (indexType)
						{
							case "alphaTree":
								m_indexType = IndexTypes.alphaTree;
								break;
							case "rangeTree":
								m_indexType = IndexTypes.rangeTree;
								break;
							case "twoLevelRange":
								m_indexType = IndexTypes.twoLevelRange;
								break;
							case "alphaTreeMf":
								m_indexType = IndexTypes.alphaTreeMf;
								break;
						}
						break;
					case "excludeWords":
						string maxFreq = Utils.AttVal(node, "moreFrequentThan", "unlimited");
						m_maxFrequency = maxFreq == "unlimited" ? Int32.MaxValue : Int32.Parse(maxFreq);
						Utils.BuildDictionary(m_excludeWords,node.InnerText);
						break;
					case "excludeClasses":
						Utils.BuildDictionary(m_excludeClasses,node.InnerText);
						break;
					case "specialClasses":
						m_notesClass = Utils.AttVal(node, "notesClass", "");
						m_notesRef = Utils.MakeSafeXml(Utils.AttVal(node, "notesRef", "-----") + ": ");
						m_headingRef = Utils.MakeSafeXml(Utils.AttVal(node, "headingRef", "-----") + ": ");
						Utils.BuildDictionary(m_nonCanonicalClasses, node.InnerText);
						break;
					case "files":
						BuildFileList(node);
						break;
					case "bookChap":
						m_bookChapText = node.InnerText;
						break;
					case "collation":
						GetComparer(node);
						break;
				}
			}
		}

		// Enhance: move functionality to Options.
		private void GetComparer(XmlNode node)
		{
			string compareId = Utils.AttVal(node, "comparer", null);
			if (compareId == null)
				return;
			if (compareId == "CustomSimple")
			{
				m_comparer = new Palaso.WritingSystems.Collation.SimpleRulesCollator(node.InnerText);
			}
			else if (compareId == "CustomICU")
			{
				m_comparer = new Palaso.WritingSystems.Collation.IcuRulesCollator(node.InnerText);
			}
			else
			{
				try
				{
					CultureInfo info = new CultureInfo(compareId);
					m_comparer = StringComparer.Create(info, true);
				}
				catch (ArgumentException)
				{
					MessageBox.Show("Cannot interpret " + compareId + " as a collation ID. Using default collation.", "Error", MessageBoxButtons.OK,
						MessageBoxIcon.Warning);
				}
			}
		}

		private void BuildFileList(XmlNode node)
		{
			foreach (XmlNode item in node.ChildNodes)
			{
				string fileName = item.Attributes["name"].Value;
				string abbr = item.Attributes["abbr"].Value;
				if(abbr =="" || abbr == "???")
					continue; // non-canonical
				m_files.Add(fileName);
				m_abbreviations[Path.ChangeExtension(fileName, "htm")] = abbr;
			}
		}

		public void Run(IList files)
		{
			Progress status = new Progress(files.Count);
			Utils.EnsureDirectory(m_outputDirName);
			status.Text = "Parsing";
			status.Show();
			int count = 0;
			foreach (string inputFile in files)
			{
				status.File = inputFile;
				string inputFilePath = Path.Combine(m_inputDirName, inputFile);
				if (m_options.ChapterPerFile)
				{
					foreach (string inputPath in Utils.ChapFiles(inputFilePath))
						Parse(inputPath);
				}
				else
				{
					Parse(inputFilePath); 
				}
				count++;
				status.Value = count;
			}
			status.Close();

			// Use the individual word occurrences to find phrase occurrences (and add them before sorting etc.)
			AddPhraseOccurrences();
			
			List<WordformInfo> sortedOccurrences = new List<WordformInfo>(m_occurrences.Count);
			foreach (WordformInfo info in m_occurrences.Values)
			{
				if (info.Occurrences.Count > m_maxFrequency)
					continue;
				if (m_excludeWords.ContainsKey(info.Form))
					continue;
				sortedOccurrences.Add(info);
			}
			sortedOccurrences.Sort(new WordformInfoComparer(m_comparer));
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

			switch (m_indexType)
			{
				case IndexTypes.twoLevelRange:
					MakeIndexFiles(sortedOccurrences);
					break;
				case IndexTypes.rangeTree:
					MakeTreeRangeIndex(sortedOccurrences);
					break;
				case IndexTypes.alphaTree:
					MakeAlphaIndex(sortedOccurrences);
					break;
				case IndexTypes.alphaTreeMf:
					MakeAlphaMfIndex(sortedOccurrences);
					break;
			}

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
						new object[] { groupFileName, Utils.MakeSafeXml(firstItemInGroup.Form), Utils.MakeSafeXml(lastItemInGroup.Form) });
					WriteInnerIndexFile(groupFileName, sortedOccurrences, groupIndex, iStartGroup, cThisGroup);
				}
				iStartGroup += cThisGroup;
				groupIndex++;
			}
			writerMain.Write(trailer);
			writerMain.Close();
		}

		const string indexHeader = "<!doctype HTML PUBLIC \"-//W3C//DTD HTML 4.0 Transitional//EN\">\n<html>\n"
				+ "<head>\n\t<link rel=\"stylesheet\" type=\"text/css\" href=\"mktree.css\">\n\t"
				+ "<link rel=\"stylesheet\" href=\"display.css\" type=\"text/css\">\n\t"
				+ "<script type=\"text/javascript\" src=\"mktree.js\"></script>\n</head>\n"
				+ "<body class=\"ConcIndex\">\n"
				+ "<p><a target=\"body\" href=\"root.htm\">";
		const string indexHeader2 = "</a></p>\n"
				+ "<ul class=\"mktree\">\n";
		const string indexTrailer = "</ul>\n</body>\n</html>\n";

		/// <summary>
		/// Make tree-organization index with equal ranges of words as the roots.
		/// </summary>
		/// <param name="sortedOccurrences"></param>
		private void MakeTreeRangeIndex(List<WordformInfo> sortedOccurrences)
		{
			double count = sortedOccurrences.Count;
			int groupSize = Convert.ToInt32(Math.Sqrt(count));
			int iStartGroup = 0;

			string pathMain = Path.Combine(m_outputDirName, "concTreeIndex.htm");
			TextWriter writerMain = new StreamWriter(pathMain, false, Encoding.UTF8);

            string ciHeaderPath = Path.Combine(Path.GetDirectoryName(m_inputDirName), "concIndexHeader.txt");
            if (File.Exists(ciHeaderPath))
            {
                string headerFmt = new StreamReader(ciHeaderPath, Encoding.UTF8).ReadToEnd();
                writerMain.Write(string.Format(headerFmt, m_options.ConcordanceLinkText));
            }
            else
            {
                writerMain.Write(indexHeader);
                writerMain.Write(m_bookChapText);
                writerMain.Write(indexHeader2);               
            }

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
						new object[] { Utils.MakeSafeXml(firstItemInGroup.Form), Utils.MakeSafeXml(lastItemInGroup.Form) });
					WriteInnerIndexItems(writerMain, sortedOccurrences, iStartGroup, cThisGroup);
					writerMain.Write("</ul></li>\n");
				}
				iStartGroup += cThisGroup;
				groupIndex++;
			}
            string ciFooterPath = Path.Combine(Path.GetDirectoryName(m_inputDirName), "concIndexFooter.txt");
            if (File.Exists(ciFooterPath))
            {
                string fmt = new StreamReader(ciFooterPath, Encoding.UTF8).ReadToEnd();
                writerMain.Write(string.Format(fmt, m_options.ConcordanceLinkText));
            }
            else
            {
                writerMain.Write(indexTrailer);
            }
			writerMain.Close();
		}
		/// <summary>
		/// Make tree-organization index with letters of alphabet as roots.
		/// </summary>
		/// <param name="sortedOccurrences"></param>
		private void MakeAlphaIndex(List<WordformInfo> sortedOccurrences)
		{
			int iStartGroup = 0;

			string pathMain = Path.Combine(m_outputDirName, "concTreeIndex.htm");
			TextWriter writerMain = new StreamWriter(pathMain, false, Encoding.UTF8);
			writerMain.Write(indexHeader);
			writerMain.Write(m_bookChapText);
			writerMain.Write(indexHeader2);

			while (iStartGroup < sortedOccurrences.Count)
			{
				string keyLetter = sortedOccurrences[iStartGroup].Form.Substring(0, 1).ToUpper();
				// Enhance JohnT: handle surrogate pair or multigraph.
				int iLimGroup = iStartGroup + 1;
				while (iLimGroup < sortedOccurrences.Count &&
					sortedOccurrences[iLimGroup].Form.Substring(0, keyLetter.Length).ToUpper() == keyLetter)
				{
					iLimGroup++;
				}
				writerMain.Write("<li><span class=\"indexKeyLetter\">{0}</span><ul>\n", Utils.MakeSafeXml(keyLetter));
				WriteInnerIndexItems(writerMain, sortedOccurrences, iStartGroup, iLimGroup - iStartGroup);
				writerMain.Write("</ul></li>\n");
				iStartGroup = iLimGroup;
			}
			writerMain.Write(indexTrailer);
			writerMain.Close();
		}
		const string indexMfHeader = "<!doctype HTML PUBLIC \"-//W3C//DTD HTML 4.0 Transitional//EN\">\n<html>\n"
				+ "<head>\n\t<link rel=\"stylesheet\" type=\"text/css\" href=\"mktree.css\">\n\t"
				+ "<link rel=\"stylesheet\" href=\"display.css\" type=\"text/css\">\n"
				+ "</head>\n"
				+ "<body class=\"ConcIndex\">\n"
				+ "<p><a target=\"body\" href=\"root.htm\">";
		const string indexMfHeader2 = "</a></p>\n"
				+ "<ul class=\"mktree\">\n";
		/// <summary>
		/// Make tree-organization index with letters of alphabet as roots.
		/// </summary>
		/// <param name="sortedOccurrences"></param>
		private void MakeAlphaMfIndex(List<WordformInfo> sortedOccurrences)
		{
			string pathMain = Path.Combine(m_outputDirName, "concTreeIndex.htm");
			MakeAlphaMfIndexItem(sortedOccurrences, pathMain, pathMain, null);
		}

		/// <summary>
		/// Write one of the indexes that together make up the multi-file alphabetic index.
		/// If expandLetter is null, also write the subfiles. Otherwise, expand that one letter.
		/// </summary>
		/// <param name="sortedOccurrences"></param>
		/// <param name="pathMain"></param>
		/// <param name="expandLetter"></param>
		private void MakeAlphaMfIndexItem(List<WordformInfo> sortedOccurrences, string pathMain, string pathRoot, string expandLetter)
		{
			int iStartGroup = 0;
			TextWriter writerMain = new StreamWriter(pathMain, false, Encoding.UTF8);
            string ciHeaderPath = Path.Combine(Path.GetDirectoryName(m_inputDirName), "concIndexHeader.txt");
            if (File.Exists(ciHeaderPath))
            {
                string headerFmt = new StreamReader(ciHeaderPath, Encoding.UTF8).ReadToEnd();
                writerMain.Write(string.Format(headerFmt, m_options.ConcordanceLinkText));
            }
            else
            {
                writerMain.Write(indexMfHeader);
                writerMain.Write(m_bookChapText);
                writerMain.Write(indexHeader2);
            }

		    while (iStartGroup < sortedOccurrences.Count)
			{
				string keyLetter = GetSortLetter(sortedOccurrences[iStartGroup].Form);
				// Enhance JohnT: handle surrogate pair or multigraph. (See keyLetterFileSuffix below, too.)
				int iLimGroup = iStartGroup + 1;
				while (iLimGroup < sortedOccurrences.Count &&
					GetSortLetter(sortedOccurrences[iLimGroup].Form) == keyLetter)
				{
					iLimGroup++;
				}
				// We want a predictable file suffix but not one that might be some non-Roman character.
				// This needs enhancing for surrogate pairs, too.
				string keyLetterFileSuffix = Convert.ToInt32(keyLetter[0]).ToString();
				string keyLetterPath = Path.Combine(m_outputDirName, "Index" + keyLetterFileSuffix + ".htm");
				if (expandLetter == keyLetter)
				{
					writerMain.Write("<li class=\"liOpen\"><span id=\"open\" class=\"indexKeyLetter\"><span class=\"bullet\" onclick=\"location='{1}#open'\">&nbsp;</span><a href=\"{1}\">{0}</a></span><ul>\n",
						Utils.MakeSafeXml(keyLetter), Path.GetFileName(pathRoot));
					WriteInnerIndexItems(writerMain, sortedOccurrences, iStartGroup, iLimGroup - iStartGroup);
					writerMain.Write("</ul></li>\n");
				}
				else
				{
					// write an element that looks like a closed node, but is actually a hotlink to another index file.
					// (And do NOT write the subitems!)
					writerMain.Write("<li class=\"liClosed\"><span class=\"indexKeyLetter\"><span class=\"bullet\"onclick=\"location='{1}'\">&nbsp;</span><a href=\"{1}#open\">{0}</a></span></li>\n",
						Utils.MakeSafeXml(keyLetter), Path.GetFileName(keyLetterPath));
				}
				if (expandLetter == null)
				{
					MakeAlphaMfIndexItem(sortedOccurrences, keyLetterPath, pathRoot, keyLetter);
				}
				iStartGroup = iLimGroup;
			}
            string ciFooterPath = Path.Combine(Path.GetDirectoryName(m_inputDirName), "concIndexFooter.txt");
            if (File.Exists(ciFooterPath))
            {
                string fmt = new StreamReader(ciFooterPath, Encoding.UTF8).ReadToEnd();
                writerMain.Write(string.Format(fmt, m_options.ConcordanceLinkText));
            }
            else
            {
                writerMain.Write(indexTrailer);
            }
		    writerMain.Close();
		}

		// Get a string representing the leading letter for a sort group.
		// Todo: handle surrogate pairs.
		private string GetSortLetter(string form1)
		{
			// Decomposing the string will leave the base character as the one we break on, if any words start with
			// characters that are composed with diacritics. Otherwise, for example, since the A and A-acute words may have primary
			// sort differences further along the word, we get an alternation between A and A-acute, and hence several groups for each.
			string form = form1.Normalize(NormalizationForm.FormD);
			//return form.Substring(0, 1).ToUpper()
			for (int i = 0; i < form.Length; i++)
			{
				System.Globalization.UnicodeCategory[] goodCategories = new System.Globalization.UnicodeCategory[]
				{
					System.Globalization.UnicodeCategory.LowercaseLetter,
					System.Globalization.UnicodeCategory.OtherLetter,
					// probably wordforming, but probably mess up sorting. Better to leave out than break groups,
					// until we have a better sort algorithm.
					//System.Globalization.UnicodeCategory.PrivateUse, 
					System.Globalization.UnicodeCategory.TitlecaseLetter,
					System.Globalization.UnicodeCategory.UppercaseLetter
				};
				char c = form[i];
				for (int j = 0; j < goodCategories.Length; j++)
				{
					if (Char.GetUnicodeCategory(c) == goodCategories[j])
						return c.ToString().ToUpper();
				}
			}
			return form[0].ToString().ToUpper(); // desperate resort!
		}

		private void WriteInnerIndexItems(TextWriter writer, List<WordformInfo> sortedOccurrences,
			int iStartGroup, int cThisGroup)
		{
			for (int i = iStartGroup; i < iStartGroup + cThisGroup; i++)
			{
				WordformInfo item = sortedOccurrences[i];
				writer.Write("<li><a href=\"wl{0}.htm\" target=\"conc\">{1} ({2})</a></li>\n",
					new object[] { item.FileNumber, Utils.MakeSafeXml(item.Form), item.Occurrences.Count });
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
					new object[] { item.FileNumber, Utils.MakeSafeXml(item.Form) });
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
			m_anchor = "";
			m_pendingNonCanonical.Clear();
			m_pendingExclusions.Clear();
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
			string infoForm = Utils.MakeSafeXml(info.Form);
			string fixQuoteInfoForm = infoForm.Replace("'", "&#39"); // apostrophe in word can close onclick quote.
			string header = "<!doctype HTML PUBLIC \"-//W3C//DTD HTML 4.0 Transitional//EN\">\n<html>\n"
				+ "<head><script src=\"ConcFuncs.js\" type=\"text/javascript\"></script>\n"
				+ "<link rel=\"stylesheet\" type=\"text/css\" href=\"display.css\">\n"
				+ string.Format("<script type=\"text/javascript\">var curWord = \"{0}\"; var curFlags = \"{1}\"</script>", infoForm, flags)
				+ string.Format("</head>\n<body onload='sel(curWord,\"{1}\")'>\n", fixQuoteInfoForm, flags);
			string trailer = "</body>\n</html>\n";
			string path = Path.Combine(m_outputDirName, "wl" + m_wordListFileCount.ToString() + ".htm");
			info.FileNumber = m_wordListFileCount;
			m_wordListFileCount++;
			TextWriter writer = new StreamWriter(path, false, Encoding.UTF8);
			writer.Write(header);
			foreach (WordOccurrence item in items)
			{
				writer.Write("<span class=\"OccRef\">");
				if (item.Canonical)
				{
					writer.Write(GetAbbreviation(item.FileName));
					writer.Write(" ");
					writer.Write(item.Chapter);
					writer.Write(".");
					writer.Write(Utils.MakeSafeXml(item.Verse));
					writer.Write(": ");
				}
				else if (item.Verse == "")
					writer.Write(m_notesRef); // in Notes area.
				else
					writer.Write(m_headingRef); // other option for now is some sort of heading.
				writer.Write("</span>");
				if (!item.Canonical)
				{
					writer.Write("<span class=\"special\">");
				}
				WritePrecedingContext(writer, item.Context.Substring(0, item.Offset));
				string form = Utils.MakeSafeXml(item.Form);
				string fixQuoteForm = form.Replace("'", "&#39"); // apostrophe in word can close onclick quote.
				writer.Write("<a href=\"{0}#{1}\" target=\"main\">{4}</a>",
					new object[] { item.FileName, item.Anchor, fixQuoteForm, flags, form});
				//writer.Write(item.Context.Substring(item.Offset + key.Length, item.Context.Length - item.Offset - key.Length));
				WriteFollowingContext(writer, item.Context.Substring(item.Offset + info.Form.Length, item.Context.Length - item.Offset - info.Form.Length));
				if (!item.Canonical)
				{
					writer.Write("</span>");
				}
				writer.Write("<br/>\n");
			}
			writer.Write(trailer);
			writer.Close();
		}

		private string GetAbbreviation(string fileName)
		{
			string abbr;
			if (m_abbreviations.TryGetValue(fileName, out abbr))
				return abbr;
			if (m_options.ChapterPerFile)
			{
				int lastHyphen = fileName.LastIndexOf("-");
				int lastDot = fileName.LastIndexOf(".");
				if (lastHyphen > 0 && lastDot > 0)
				{
					string originalFileName = fileName.Remove(lastHyphen, lastDot - lastHyphen);
					if (m_abbreviations.TryGetValue(originalFileName, out abbr))
					{
						m_abbreviations[fileName] = abbr; // find faster next time
						return abbr;
					}
				}
			}
			return "???"; // last resort.
		}

		private void WritePrecedingContext(TextWriter writer, string context)
		{
			if (context.Length < m_maxContextLength)
			{
				writer.Write(Utils.MakeSafeXml(context));
				return;
			}
			int iWhiteSpace = -1;
			for (int i = context.Length - m_maxContextLength; i < context.Length - m_minContextLength; i++)
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
				writer.Write(Utils.MakeSafeXml(context.Substring(iWhiteSpace, context.Length - iWhiteSpace)));
			}
			else
			{
				writer.Write("...");
				writer.Write(Utils.MakeSafeXml(context.Substring(context.Length - m_maxContextLength + 3, m_maxContextLength - 3)));
			}
		}

		private void WriteFollowingContext(TextWriter writer, string context)
		{
			if (context.Length < m_maxContextLength)
			{
				writer.Write(Utils.MakeSafeXml(context));
				return;
			}
			int iWhiteSpace = -1;
			for (int i = m_maxContextLength - 1; i >= m_minContextLength; i--)
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
				writer.Write(Utils.MakeSafeXml(context.Substring(0, iWhiteSpace)));
			}
			else
			{
				writer.Write(Utils.MakeSafeXml(context.Substring(0, m_maxContextLength - 3)));
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
		private Options m_options;

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
				while (cSave > 0 && (cSave >= context.Length - 1 || Char.IsWhiteSpace(context[cSave + 1])))
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
			WordOccurrence item = new WordOccurrence(m_htmlFile, m_chapter, m_verse, m_anchor, m_context.Length, wordform, m_pendingNonCanonical.Count == 0);
			info.Occurrences.Add(item);
			m_pendingOccurrences.Add(item);
		}

		void AddPhraseOccurrences()
		{
			foreach (string phrase in m_options.Phrases)
			{
				int firstNonLetter;
				for (firstNonLetter = 0; firstNonLetter < phrase.Length && IsLetter(phrase[firstNonLetter]); firstNonLetter++)
					;
				if (firstNonLetter == 0 || firstNonLetter == phrase.Length)
					return; // nothing word-like, or only one word; nothing useful we can add
				string firstWord = phrase.Substring(0, firstNonLetter);
				WordformInfo info;
				m_occurrences.TryGetValue(firstWord, out info);
				if (info == null)
				{
					string firstWordLc = firstWord.ToLowerInvariant();
					m_occurrences.TryGetValue(firstWordLc, out info);
					if (info == null)
					{
						string existingUCForm;
						m_uppercaseForms.TryGetValue(firstWordLc, out existingUCForm);
						if (existingUCForm != null)
							m_occurrences.TryGetValue(existingUCForm, out info);
					}
				}
				if (info == null)
					continue;
				WordformInfo phraseInfo = new WordformInfo(phrase);
				string target = phrase;
				if (m_options.MergeCase)
					target = phrase.ToLowerInvariant();

				foreach (WordOccurrence wi in info.Occurrences)
				{
					string match = wi.Context.Substring(wi.Offset);
					if (m_options.MergeCase)
						match = match.ToLowerInvariant();
					if (match.StartsWith(target))
					{
						WordOccurrence phraseOccurrence = new WordOccurrence(wi.FileName, wi.Chapter, wi.Verse, wi.Anchor, wi.Offset,
						                                                     phrase, wi.Canonical);
						phraseInfo.Occurrences.Add(phraseOccurrence);
						phraseOccurrence.Context = wi.Context;
					}
				}

				if (phraseInfo.Occurrences.Count > 0)
					m_occurrences[phrase] = phraseInfo;
			}
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
			// Get into 'name' the thing if any that can be an href target.
			string name = reader.GetAttribute("id");

			if (reader.Name == "a")
			{
				name = reader.GetAttribute("name");
			}

			// If we got a target remember various stuff from it.
			if (name != null && name != "") // don't clear anchor when we hit a link
			{
				m_anchor = name;
				if (name != null && name.StartsWith("C"))
				{
					string refSource = name.Substring(1, name.Length - 1); // strip off 'C'
					string[] parts = refSource.Split('V');
					if (parts.Length == 2)
					{
						m_chapter = Int32.Parse(parts[0]);
						m_verse = parts[1]; // don't try to parse this, may be complex, eg. 11-12
					}
				}
			}
			string className = reader.GetAttribute("class");
			if (className != null)
			{
				if (m_excludeClasses.ContainsKey(className))
				{
					// Prevents processing wordforms until we find the corresponding end marker.
					m_pendingExclusions.Add(reader.Name);
				}
				else if (m_nonCanonicalClasses.ContainsKey(className))
				{
					if (m_pendingNonCanonical.Count == 0)
						ProcessEndOfSentence();
					m_pendingNonCanonical.Add(reader.Name);
				}
				if (className == m_notesClass)
				{
					// Output no refs till we see another CV anchor
					m_verse = "";
					m_chapter = 0;
				}
			}
		}

		private void ProcessEndElement(XmlReader reader)
		{
			if (m_pendingExclusions.Count > 0 && reader.Name == m_pendingExclusions[m_pendingExclusions.Count - 1])
				m_pendingExclusions.RemoveAt(m_pendingExclusions.Count - 1);
			if (m_pendingNonCanonical.Count > 0 && reader.Name == m_pendingNonCanonical[m_pendingNonCanonical.Count - 1])
			{
				m_pendingNonCanonical.RemoveAt(m_pendingNonCanonical.Count - 1);
				if (m_pendingNonCanonical.Count == 0)
					ProcessEndOfSentence();
			}
		}
	}

	// Compare wordforms using the specified string comparer.
	class WordformInfoComparer : IComparer<WordformInfo>
	{
		IComparer<string> m_comparer;

		public WordformInfoComparer(IComparer<string> comparer)
		{
			m_comparer = comparer;
		}
		#region IComparer<WordformInfo> Members

		public int Compare(WordformInfo x, WordformInfo y)
		{
			return m_comparer.Compare(x.Form, y.Form);
		}

		#endregion
	}
}
