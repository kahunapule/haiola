using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;
using System.Windows.Forms;
using System.Collections;

namespace sepp
{
	public class BookNamePageGenerator
	{
		string m_workDir; // working directory for whole project, contains BookNames.xml
		string m_langDir; // working directory for language, contains Sepp Options.xml, place to put /Extras/BookNames.htm
		string m_outputDirName; // m_langDir/Extras
		Dictionary<string, string> m_fileToKey = new Dictionary<string, string>();
		Dictionary<string, string> m_KeyToVern = new Dictionary<string, string>();
		List<string> m_languages = new List<string>();
		List<string> m_headings = new List<string>();

		public BookNamePageGenerator(string workDir, string langDir, string optionsPath)
		{
			m_workDir = workDir;
			m_langDir = langDir;
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
					case "bookNameColumns":
						BuildLangList(node);
						break;
				}
			}
		}

		private void BuildLangList(XmlNode node)
		{
			foreach (XmlNode item in node.ChildNodes)
			{
				XmlAttribute langAttr = item.Attributes["name"];
				if (langAttr == null || langAttr.Value == "*")
					m_languages.Add("*");
				else
					m_languages.Add(langAttr.Value);
				m_headings.Add(Utils.MakeSafeXml(item.InnerText));
			}
		}

		private void BuildFileList(XmlNode node)
		{
			foreach (XmlNode item in node.ChildNodes)
			{
				string fileName = item.Attributes["name"].Value;
				if (item.Attributes["eng"] != null && item.Attributes["parallel"] != null)
				{
					string key = item.Attributes["eng"].Value;
					string vern = item.Attributes["parallel"].Value;
					m_fileToKey[fileName] = key;
					m_KeyToVern[key] = vern;
				}
				// else error?
			}
		}

		public void Run(IList files)
		{
			m_outputDirName = Path.Combine(m_langDir, "Extras");
			Utils.EnsureDirectory(m_outputDirName);
			List<XmlDocument> bookLists = new List<XmlDocument>();
			foreach (string langName in m_languages)
			{
				if (langName == "*")
				{
					bookLists.Add(null);
					continue; // the column for this language
				}
				string langBookNamePath = Path.Combine(m_workDir, "BookNames_" + langName + ".xml");
				if (!File.Exists(langBookNamePath))
					MessageBox.Show("File " + langBookNamePath + "not found...can't generate requested column", "Error");
				XmlDocument bookNameDoc = new XmlDocument();
				try
				{
					bookNameDoc.Load(langBookNamePath);
				}
				catch (Exception e)
				{
					MessageBox.Show("Error loading file " + langBookNamePath + ": " + e.Message, "Error");
				}
				bookLists.Add(bookNameDoc);
			}
			// Generate an HTML file containing a table.
			// one column for each name in BookNames languages element
			// corresponding headings taken from elements under <headings>
			// One further column which is taken from parallel field in Sepp Options and the language name
			// Rows, one for each file in files, contain corresponding attribute from books elements in BookNames
			// and parallel attr in Sepp Options/files.
			string header = "<!doctype HTML PUBLIC \"-//W3C//DTD HTML 4.0 Transitional//EN\">\n<html>\n"
				+ "<head>\n"
				+ "<link rel=\"stylesheet\" href=\"display.css\" type=\"text/css\">\n"
				+ "</head>\n<body>\n"
				+ "<table class=\"BookNameTable\">\n<thead>\n<tr>";
			string trailer = "</tbody>\n</table>\n</body>\n</html>\n";
			string path = Path.Combine(m_outputDirName, "BookNames.htm");
			TextWriter writer = new StreamWriter(path, false, Encoding.UTF8);
			writer.Write(header);
			int icol = 0;
			foreach (string heading in m_headings)
			{
				string colClass = "bookNameHeader";
				if (m_languages[icol++] == "*")
					colClass = "bookNameVernHeader";
				writer.Write("<th class=\"" + colClass + "\">" + heading + "</th>");
			}
			writer.WriteLine("</tr>");
			writer.WriteLine("</thead>");
			writer.WriteLine("</tbody>");

			Progress status = new Progress(files.Count);
			status.Show();
			int count = 0;
			foreach (string inputFile in files)
			{
				string filename = Path.GetFileName(inputFile);
				string keyFileName = Path.ChangeExtension(filename, "xml");
				if (files.Contains(keyFileName))
				{
					status.File = filename;
					string key = m_fileToKey[keyFileName];
					if (key == null)
					{
						MessageBox.Show("File " + keyFileName + " is missing the 'eng' or 'parallel' attribute and will be omitted", "Warning");
						continue;
					}
					string vern = m_KeyToVern[key];
					writer.Write("<tr>");
					int ilang = 0;
					foreach (string langName in m_languages)
					{
						XmlDocument doc = bookLists[ilang++];
						if (doc == null)
						{
							// The special column for the language itself.
							writer.Write("<td class=\"bookNameVern\">" + vern + "</td>");
						}
						else
						{
							XmlNode book = doc.GetElementById(key);
							if (book == null || book.Attributes["name"] == null)
							{
								writer.Write("<td class=\"bookNameMissing\">Missing name<td>");
							}
							else
							{
								writer.Write("<td class=\"bookNameItem\">" + book.Attributes["name"].Value + "</td>");
							}
						}
					}
					writer.WriteLine("</tr>");

					count++;
					status.Value = count;
				}
			}
			writer.Write(trailer);
			writer.Close();

			status.Close();
		}
	}
}
