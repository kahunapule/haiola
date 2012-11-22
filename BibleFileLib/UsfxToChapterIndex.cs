using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using WordSend;

namespace BibleFileLib
{
	/// <summary>
	/// Class to generate the ChapterIndex file from the USFX file
	/// </summary>
	public class UsfxToChapterIndex
	{
		private XmlTextReader usfx;

		private string vernacularName = ""; // from toc level 1 if found
		private string mtName = ""; // from p sfm = mt level = 1
		private string bookId = ""; // ID of current book
		protected string sfm; // sfm attribute of current element
		protected string id; // id attribute of current element
		protected string level; // level attribute of current element
		private int chapterNumber; // last read
		private string chapterLinks = "";

		StreamWriter htm;

		public UsfxToChapterIndex()
		{
			IntroductionLinkText = "Introduction";
		}

		/// <summary>
		/// The text to use for the link to the concordance; leave null for no concordance (link).
		/// </summary>
		public string ConcordanceLinkText { get; set; }

		/// <summary>
		/// The (localized) text to use for links to book introductions.
		/// </summary>
		public string IntroductionLinkText { get; set; }

		/// <summary>
		/// If this is set, it is a folder in which we look for files XXX_Introduction.htm. If one is found for a particular book, it is copied to the output folder,
		/// and a link is made to it. 
		/// </summary>
		public string IntroductionDirectory { get; set; }

		/// <summary>
		/// The string we append to a book ID to get the introduction file name for that book, if any.
		/// </summary>
		public const string IntroductionSuffix = "_Introduction.htm";

		public void Generate(string usfxPath, string chapterIndexPath)
		{
			usfx = new XmlTextReader(usfxPath);
			usfx.WhitespaceHandling = WhitespaceHandling.Significant;
			OpenHtmlFile(chapterIndexPath);
			if (!string.IsNullOrEmpty(ConcordanceLinkText))
			{
				htm.WriteLine("<div class=\"BookChapIndex\">");
				htm.WriteLine("<p class=\"IndexBookName\"><a target=\"_top\" href=\"conc\\treeMaster.htm\">" + ConcordanceLinkText + "</a></p>");
				htm.WriteLine("</div>");
			}

		    string htmDirectory = Path.GetDirectoryName(chapterIndexPath);

			if (chapterNumber > 1)
			{
				htm.WriteLine("<p class=\"IndexChapterList\">" + chapterLinks + "</p>");
			}
			htm.WriteLine("</div>");
			while (usfx.Read())
			{
				if (usfx.NodeType == XmlNodeType.Element)
				{
					level = GetNamedAttribute("level");
					sfm = GetNamedAttribute("sfm");
					id = GetNamedAttribute("id");
					switch (usfx.Name)
					{
						case "book":
							chapterNumber = 0;
							chapterLinks = "";
							vernacularName = "";
							bookId = "";
							mtName = "";
							if (id.Length > 2)
								bookId = id;
							break;
						case "id":
							if (id.Length > 2)
								bookId = id;
							break;
						case "p":
							// Review Michael (JohnT): should we use other levels? Always or only if there is no level 1 mt?
                            // We should either use all levels of \mt or just use \h. In other cases, I use \mt as a fallback
                            // when \h is absent in the source file. \mt is required, and \h is sometimes absent. -- Michael
							if (sfm == "mt") // && (level == "" || level == "1"))
							{
								usfx.Read();
								if (usfx.NodeType == XmlNodeType.Text)
								{
									if (mtName.Length > 0)
										mtName = mtName + " " + usfxToHtmlConverter.EscapeHtml(usfx.Value.Trim());
									else
										mtName = usfxToHtmlConverter.EscapeHtml(usfx.Value.Trim());
								}
							}
							break;
						case "toc":
							if (!usfx.IsEmptyElement)
							{
								usfx.Read();
								if (usfx.NodeType == XmlNodeType.Text)
								{
									switch (level)
									{
										case "1":
											vernacularName = usfxToHtmlConverter.EscapeHtml(usfx.Value.Trim());
											break;
									}
								}
							}
							break;
						case "c":
							var currentChapter = id;
							var currentChapterPublished = fileHelper.LocalizeDigits(currentChapter);

							if (!usfx.IsEmptyElement)
							{
								usfx.Read();
								if (usfx.NodeType == XmlNodeType.Text)
									currentChapterPublished = fileHelper.LocalizeDigits(usfx.Value.Trim());
							}
							int chNum;
							if (Int32.TryParse(id, out chNum))
								chapterNumber = chNum;
							else
								chapterNumber++;

							// e.g. <a target="_top" href="frame_MAT02.htm">2</a>
							chapterLinks += "<a target=\"_top\" href=\"" 
								+ UsfxToFramedHtmlConverter.TopFrameName(bookId, chapterNumber)
								+ "\">" + currentChapterPublished + "</a> ";
							break;
					}
				}
				else if (usfx.NodeType == XmlNodeType.EndElement)
				{
					switch (usfx.Name)
					{
						case "book":
							if (bookId.Length < 2)
								break; // ignore
							if (vernacularName.Length == 0)
								vernacularName = mtName;
							// We want to produce something like
							//<div id = "MRK" class="BookChapIndex">
							//  <p class="IndexBookName"><a target="top" href="frame_MRKTOC.htm">Markus</a></p>
							//        <p class="IndexIntroduction"><a target="top" href="frame_MRK-Intro-Q.htm">Pengantar Buku</a></p>

							//    <p class="IndexChapterList"><a target="top" href="frame_MRK01.htm">1</a> <a target="top" href="frame_MRK02.htm">2</a>...  </p>
							//</div>
							htm.WriteLine("<div id=\"" + bookId + "\" class=\"BookChapIndex\">");
                            // A TOC page is ONLY generated if there are headings in the book to generate it with. If there isn't one, the book name
                            // link goes straight to chapter 1. (bookid00{0} is the id generated for the TOC page by TopFrameName(bookId, 0))
					        var firstFrameName = UsfxToFramedHtmlConverter.TopFrameName(bookId, 0);
                            if (!File.Exists(Path.Combine(htmDirectory, firstFrameName)))
                                firstFrameName = UsfxToFramedHtmlConverter.TopFrameName(bookId, 1);
                            htm.WriteLine("<p class=\"IndexBookName\"><a target=\"_top\" href=\""
								+ firstFrameName + "\">"
								+ usfxToHtmlConverter.EscapeHtml(vernacularName) + "</a></p>");
							// If we have an introduction file for this book generate a link to it.
							string introductionFileName = bookId + IntroductionSuffix;
							if (File.Exists(Path.Combine(IntroductionDirectory, introductionFileName)))
							{
								htm.WriteLine("<p class=\"IndexIntroduction\"><a target=\"_top\" href=\""
								              + UsfxToFramedHtmlConverter.TopFrameName(introductionFileName) + "\">"
								              + usfxToHtmlConverter.EscapeHtml(IntroductionLinkText) + "</a></p>");
							}
							if (chapterNumber > 1)
							{
								htm.WriteLine("<p class=\"IndexChapterList\">" + chapterLinks + "</p>");
							}
							htm.WriteLine("</div>");
							break;
					}
				}
			}
			CloseHtmlFile();
		}

		//private bool convertDigitsToKhmer = false; // todo: configure

        /* Replaced by FileHelper.LocalizeDigits
		public string LocalizeNumerals(string s)
		{
			if (convertDigitsToKhmer)
				return fileHelper.KhmerDigits(s);
			else
				return s;
		}
         */
		protected string GetNamedAttribute(string attributeName)
		{
			string result = usfx.GetAttribute(attributeName);
			if (result == null)
				result = String.Empty;
			return result;
		}

		protected void OpenHtmlFile(string fileName)
		{

			htm = new StreamWriter(fileName, false, Encoding.UTF8);
			// It is important that the DOCTYPE declaration should be a single line, and that the <html> element starts the second line.
			// This is because the concordance parser uses a ReadLine to skip the DOCTYPE declaration in order to read the rest of the file as XML.
			htm.WriteLine(
				"<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Transitional//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd\">");
			htm.WriteLine("<html xmlns:msxsl=\"urn:schemas-microsoft-com:xslt\" xmlns:user=\"urn:nowhere\">");
			htm.WriteLine("<head>");
			htm.WriteLine("<META http-equiv=\"Content-Type\" content=\"text/html; charset=UTF-8\">");
			htm.WriteLine("<link rel=\"stylesheet\" href=\"chapIndex.css\" type=\"text/css\">");
			htm.WriteLine("<title>Contents</title></head>");
			htm.WriteLine("<body class=\"BookChapIndex\">");
		}

		protected void CloseHtmlFile()
		{
			if (htm != null)
			{
				htm.WriteLine("</body></html>");
				htm.Close();
				htm = null;
			}
		}

		public const string ChapIndexFileName = "ChapterIndex.htm";
	}
}
