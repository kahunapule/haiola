using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;

namespace sepp
{
	public class HTML_To_BibleReadingGuide
	{
		private string m_htmlDirName;
		private string m_finalOutputDirName;
		private Options m_options;

		private List<SectionInfo> m_sections = new List<SectionInfo>();

		private int kReadingsPerMonth = 25;

		private int m_totalReadings;

		private Reading[] m_readings;

		public HTML_To_BibleReadingGuide(string htmlDirName, string finalOutputDirName, Options options)
		{
			m_htmlDirName = htmlDirName;
			m_finalOutputDirName = finalOutputDirName;
			m_options = options;
		}

		public void Run()
		{
			string[] inputFileNames = Directory.GetFiles(m_htmlDirName, "*.htm");
			foreach (string path in inputFileNames)
			{
				GetSectionInfo(path);
			}
			MakeReadings();
			// Todo: generate files with readings.
		}

		private void MakeReadings()
		{
			m_totalReadings = 0;
			for (int i = 0; i < 12; i++)
				m_totalReadings += ReadingsForMonth(i);
			m_readings = new Reading[m_totalReadings];

			int totalLength = m_sections.Sum(info => info.Length);

			int remainingLength = totalLength;
			int iSection = 0;
			int spareSections = m_readings.Length - m_sections.Count;

			for (int iReading = 0; iReading < m_totalReadings; iReading++)
			{
				int remainingReadings = m_totalReadings - iReading;
				int desiredLength = remainingLength/remainingReadings;
				int cSections = 1;
				int readingLength = m_sections[iSection].Length;
				while(spareSections > 0 && readingLength < desiredLength)
				{
					// decide whether to include another section.
					int currentError = desiredLength - readingLength;
					int addLength = m_sections[iSection + cSections].Length;
					int newError = currentError - addLength;
					if (Math.Abs(newError) > currentError)
						break;
					cSections++;
					readingLength += addLength;
					spareSections--;
				}
				// Todo: finish making reading starting at iSection;
				Reading reading = new Reading();

				reading.Href = "<a href=\"" + m_sections[iSection].HRef + "\"</a>";

				iSection += cSections;
				// If we don't have at least one section per day loop around.
				if (iSection >= m_sections.Count)
					iSection = 0;
			}
		}

		int ReadingsForMonth(int month)
		{
			return kReadingsPerMonth; // currently does not depend on days in month
		}

		// Matches a section heading div and its associated anchor, captures the name of the anchor.
		Regex reSection = new Regex("<div class=\"sectionheading\"><a name=\"([^\"]*)");
		// Matches something like var curBook="1 Korintus", captures the book name.
		Regex reBook = new Regex("var curBook=\"([^\"]+)\"");

		private string m_book;

		private void GetSectionInfo(string path)
		{
			string input = new StreamReader(path, Encoding.UTF8).ReadToEnd();
			Match matchBook = reBook.Match(input);
			if (matchBook.Success)
				m_book = matchBook.Captures[1].Value;
			else
				m_book = "???";

			MatchCollection matches = reSection.Matches(input);
			int start = m_sections.Count;
			int prevIndex = -1;
			foreach (Match match in matches)
			{
				SectionInfo info = new SectionInfo();
				info.Start = match.Index;
				if (prevIndex != -1)
					CompleteSectionInfo(m_sections[m_sections.Count - 1], match.Index - prevIndex, input);
				prevIndex = match.Index;
				info.Filename = Path.GetFileName(path);
				info.HRef = match.Captures[1].Value;
				m_sections.Add(info);
			}

			if (prevIndex == -1)
				return;

			int lastIndex = input.IndexOf("<div class=\"navButtons\">");
			if (lastIndex == -1)
				lastIndex = input.IndexOf("<div class=\"footnotes\">");
			if (lastIndex == -1)
				lastIndex = input.Length;

			CompleteSectionInfo(m_sections[m_sections.Count - 1],lastIndex - prevIndex, input);
		}

		Regex reCvAnchor = new Regex("<a name=\"([0-9]+)#([0-9]+)");
		private void CompleteSectionInfo(SectionInfo info, int length, string input)
		{
			info.Length = length;
			MatchCollection matches = reCvAnchor.Matches(input.Substring(info.Start, info.Length));
			if (matches.Count > 0)
			{
				info.FirstReference = MakeSectionRef(matches[0]);
				info.LastReference = MakeSectionRef(matches[matches.Count - 1]);
			}
			else
			{
				info.FirstReference = info.LastReference = m_book; // Enhance JohnT: some localizeable string??
			}
		}

		private string MakeSectionRef(Match matche)
		{
			return m_book + " " + matche.Captures[1].Value + ":" + matche.Captures[2].Value;
		}
	}

	class SectionInfo
	{
		internal int Start { get; set; }
		internal int Length { get; set;}
		internal string Filename{ get; set;}
		// The actual body of the target, e.g., "C1V2" or more likely simply a number from the anchor in the section heading.
		internal string HRef { get; set; }
		// The first reference in the section.
		internal string FirstReference { get; set; }
		// The last Scripture reference in the section.
		internal string LastReference { get; set; }
	}

	class Reading
	{
		internal string Text { get; set; }
		internal string Href { get; set; }
	}
}
