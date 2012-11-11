using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using WordSend;

namespace BibleFileLib
{
	/// <summary>
	/// This class reads a Usfx file and extracts a list of the books it contains, each with vernacular name and an
	/// abbreviation suitable to use in concordance references.
	/// </summary>
	public class UsfxToBookAndAbbr
	{
		/// <summary>
		/// Key is standard book ID, value is vernacular name (primarily as used in cross-references)
		/// </summary>
		public Dictionary<string, string> VernacularNames { get; private set; }
		/// <summary>
		/// Key is standard book ID, value is abbreviation suitable to use in displaying reference in a concordance
		/// </summary>
		public Dictionary<string, string> ReferenceAbbreviations { get; private set; }

		/// <summary>
		/// Same values as VernacularNames.Keys and ReferenceAbbreviations.Keys, but in the order they appear in the file.
		/// </summary>
		public List<string> BookIds { get; private set; } 

		private XmlTextReader usfx;

		private string vernacularName = ""; // from toc level 1 if found
		private string mtName = ""; // from p sfm = mt level = 1
		private string bookId = ""; // ID of current book
		private string vernacularAbbreviation = "";
		protected string sfm; // sfm attribute of current element
		protected string id; // id attribute of current element
		protected string level; // level attribute of current element

		public UsfxToBookAndAbbr()
		{
			VernacularNames = new Dictionary<string, string>();
			ReferenceAbbreviations = new Dictionary<string, string>();
			BookIds = new List<string>();
		}

		public void Parse(string usfxPath)
		{
			usfx = new XmlTextReader(usfxPath);
			usfx.WhitespaceHandling = WhitespaceHandling.Significant;
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
							vernacularName = "";
							bookId = "";
							vernacularAbbreviation = "";
							mtName = "";
							if (id.Length > 2)
								bookId = id;
							break;
						case "id":
							if (id.Length > 2)
								bookId = id;
							break;
						case "p":
							// Michael (JohnT): should we use other levels? Always or only if there is no level 1 mt?
                            // If you want the whole main title, you must use all levels of \mt#. If you want a shorter
                            // title, then \h is the thing to look for.
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
										case "3":
											vernacularAbbreviation = usfxToHtmlConverter.EscapeHtml(usfx.Value.Trim());
											break;
									}
								}
							}
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
							if (vernacularAbbreviation.Length == 0)
								vernacularAbbreviation = bookId;
							BookIds.Add(bookId);
							VernacularNames[bookId] = vernacularName;
							ReferenceAbbreviations[bookId] = vernacularAbbreviation;
							break;
					}
				}
			}
		}
		protected string GetNamedAttribute(string attributeName)
		{
			string result = usfx.GetAttribute(attributeName);
			if (result == null)
				result = String.Empty;
			return result;
		}

	}
}
