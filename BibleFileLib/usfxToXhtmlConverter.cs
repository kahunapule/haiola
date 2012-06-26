using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace WordSend
{
	/// <summary>
	/// modifies usfxToHtmlConverter to generate XHTML instead of traditional HTML.
	/// I have not yet (June 2 2012) verified that the end result is valid XHTML.
	/// Testing has been limited to verifying that the files produced in one project can be parsed using code like this:
	///			//XmlReaderSettings settings = new XmlReaderSettings();
				//settings.ConformanceLevel = ConformanceLevel.Fragment;
				//settings.IgnoreComments = true;
				//settings.ProhibitDtd = false;
				//settings.ValidationType = ValidationType.None;
				//settings.ConformanceLevel = ConformanceLevel.Fragment;
				//foreach (var inputFile in Directory.GetFiles(xhtmlPath))
				//{
				//    TextReader input = new StreamReader(inputFile, Encoding.UTF8);
				//    input.ReadLine(); // Skip the HTML DOCTYPE, which the XmlReader can't cope with.
				//    var content = input.ReadToEnd().Replace("&nbsp;", "&#160;");
				//    XmlReader reader = XmlReader.Create(new MemoryStream(Encoding.UTF8.GetBytes(content)), settings);
				//    while (reader.Read())
				//    {
				//    }
				//}
	/// </summary>
	public class usfxToXhtmlConverter : usfxToHtmlConverter
	{
		/// <summary>
		/// The way to close an element that is not going to have any content. In HTML, we can just put a closing bracket. In XHTML, we need a slash before it.
		/// </summary>
		protected override string CloseOfContentlessElement
		{
			get { return "/>"; }
		}

		/// <summary>
		/// In html, option selected can start out with just the attribute nam, but xhtml requires the attribute to have a specific value.
		/// </summary>
		protected override string OptionSelectedOpeningElement
		{
			get { return "<option selected=\"selected\">"; }
		}
	}
}
