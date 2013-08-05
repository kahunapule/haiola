using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BibleFileLib
{
	/// <summary>
	/// Generates the frame file treeMaster.htm. 
	/// </summary>
	public class ConcFrameGenerator
	{
		public string ConcDirectory { get; set; }
		public string LangName { get; set; }
		/// <summary>
		/// String like "{0} Concordance" into which LangName can be inserted to make a title for the frame.
		/// </summary>
		public string ConcordanceString { get; set; }

		private string currentFileName;
		protected StreamWriter htm;

		public ConcFrameGenerator()
		{
			ConcordanceString = "{0} Concordance";
		}
		public void Run()
		{
			OpenHtmlFile("treeMaster.htm");
			htm.WriteLine("<frameset cols=\"20%,80%\">");
			htm.WriteLine("<frame name=\"outer\" src=\"concTreeIndex.htm\"/>");
			htm.WriteLine("<frame name=\"conc\" src=\"treeconc.htm\"/>");
			htm.WriteLine("<noframes>");
			htm.WriteLine("<body>");

			htm.WriteLine("<p>If you can read this, you need a browser that handles frames to use the concordance. <a href=\"../../index.htm\" target=\"_top\">click here for the original index</a>.</p>"); // todo: localization?

			htm.WriteLine("</body>");
			htm.WriteLine("</noframes>");
			htm.WriteLine("</frameset>");
			CloseHtmlFile();

			OpenHtmlFile("treeconc.htm");
			htm.WriteLine("<body>");

			htm.WriteLine("<p>Click a plus sign in the left column to expand the range of words and show individual words. Click on a particular word to see a list of occurrences in context.</p>"); // todo: localization?

			htm.WriteLine("</body>");
			CloseHtmlFile();
		}

		protected void OpenHtmlFile(string fileName)
		{
			currentFileName = Path.Combine(ConcDirectory, fileName);

			htm = new StreamWriter(currentFileName, false, Encoding.UTF8);
			htm.WriteLine(
				"<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Transitional//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd\">");
			htm.WriteLine("<html xmlns:msxsl=\"urn:schemas-microsoft-com:xslt\" xmlns:user=\"urn:nowhere\">");
			htm.WriteLine("<head>");
			htm.WriteLine("<META http-equiv=\"Content-Type\" content=\"text/html; charset=UTF-8\">");
			htm.WriteLine("<meta name=\"viewport\" content=\"width=device-width\" />");
			htm.WriteLine("<link rel=\"stylesheet\" href=\"prophero.css\" type=\"text/css\">");
			htm.WriteLine("<title>{0}</title></head>", string.Format(ConcordanceString, LangName));
			// May want something like this; but goal is to prevent concordance being indexed, so keywords are not relevant.
			//htm.WriteLine(string.Format("<meta name=\"keywords\" content=\"{0}, {1}, Holy Bible, Scripture, Bible, Scriptures, New Testament, Old Testament, Gospel\">",
			//    langName, langId));
		}

		protected void CloseHtmlFile()
		{
			if (htm != null)
			{
				htm.WriteLine("</html>");
				htm.Close();
				htm = null;
			}
		}
	}
}
