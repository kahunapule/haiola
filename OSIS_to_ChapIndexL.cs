using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Diagnostics;  // For Process class.
using System.Xml;
using System.Xml.Xsl;

namespace sepp
{
	/// <summary>
	/// This class uses XSLTs and some post-processing to generate a book/chapter index file.
	/// </summary>
	public class OSIS_to_ChapIndex
	{
		string m_inputDirName;
		string m_outputDirName;
		List<string> m_files = new List<string>(); // Files to process in desired order.
		Dictionary<string, string> m_abbreviations = new Dictionary<string, string>(); // Key is file name, value is abbreviation to use in refs.
		XslCompiledTransform m_xslt = new XslCompiledTransform();

		/// <summary>
		/// initialize one.
		/// </summary>
		/// <param name="inputDirName"></param>
		/// <param name="outputDirName"></param>
		public OSIS_to_ChapIndex(string inputDirName, string outputDirName, string optionsPath)
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
					case "files":
						BuildFileList(node);
						break;
				}
			}
		}

		/// <summary>
		/// Run the algorithm (on all files).
		/// </summary>
		public void Run()
		{
			m_xslt.Load(@"..\..\osis2ChapIndexFrag.xsl");
			string header = "<!doctype HTML PUBLIC \"-//W3C//DTD HTML 4.0 Transitional//EN\">\n<html>\n"
				+ "<link rel=\"stylesheet\" href=\"display.css\" type=\"text/css\">"
				+ "<head>"
				+ "</head>\n<body>\n"
				+ "<p><a target=\"_top\" href=\"treemaster.htm\">Concordance</a></p>\n";
			string trailer = "</body>\n</html>\n";
			string path = Path.Combine(m_outputDirName, "ChapterIndex.htm");
			TextWriter writer = new StreamWriter(path, false, Encoding.UTF8);
			writer.Write(header);

			foreach (string inputFile in m_files)
			{
				string inputFilePath = Path.Combine(m_inputDirName, inputFile);
				MemoryStream output = new MemoryStream();

				TextReader inputReader = new StreamReader(inputFilePath, Encoding.UTF8);
				XmlReader input = XmlReader.Create(inputReader);
				m_xslt.Transform(input, new XsltArgumentList(), output);
				output.Seek(0, SeekOrigin.Begin);
				StreamReader reader = new StreamReader(output, Encoding.UTF8);
				string fragment = reader.ReadToEnd();
				string htmlFile = Path.ChangeExtension(inputFile, "htm");
				fragment = fragment.Replace("$$filename$$", htmlFile);
				fragment = fragment.Replace(" xmlns:osis=\"http://www.bibletechnologies.net/2003/OSIS/namespace\"", "");
				writer.WriteLine(fragment);
			}
			writer.Write(trailer);
			writer.Close();

			MessageBox.Show("Done");
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


	}

}