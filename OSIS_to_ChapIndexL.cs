using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Diagnostics;  // For Process class.
using System.Xml;
using System.Xml.Xsl;
using System.Collections;

namespace sepp
{
	/// <summary>
	/// This class uses XSLTs and some post-processing to generate a book/chapter index file.
	/// </summary>
	public class OSIS_to_ChapIndex
	{
		string m_inputDirName;
		string m_outputDirName;
		string m_introDirName; // directory contains corresponding introduction files.
		string m_extraDirName; // directory contains extra files to make links to at end.
		List<string> m_files = new List<string>(); // Files to process in desired order.
		Dictionary<string, string> m_abbreviations = new Dictionary<string, string>(); // Key is HTM file name, value is abbreviation to use in refs.
		Dictionary<string, string> m_introFiles = new Dictionary<string, string>(); // Key is XML file name, value is corresponding intro file.
		XslCompiledTransform m_xslt = new XslCompiledTransform();
		string m_introText; // text for the 'Introduction' hot link, from options file.
		string m_concLinkText; // text for the 'Concordance' hot link, from options file.
		string m_loading;
		XmlNode m_extraFiles; // from  options file, easier to process as needed.

		/// <summary>
		/// initialize one.
		/// </summary>
		/// <param name="inputDirName"></param>
		/// <param name="outputDirName"></param>
		public OSIS_to_ChapIndex(string inputDirName, string outputDirName, string introDirName, string extraDirName, string optionsPath)
		{
			m_inputDirName = inputDirName;
			m_outputDirName = outputDirName;
			m_introDirName = introDirName;
			m_extraDirName = extraDirName;
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
					case "introduction":
						m_introText = node.InnerText;
						break;
					case "concordance":
						m_concLinkText = node.InnerText;
						break;
					case "extraFiles":
						m_extraFiles = node;
						break;
					case "loading":
						m_loading = node.InnerText;
						break;
				}
			}
		}

		/// <summary>
		/// Run the algorithm (on all files).
		/// </summary>
		public void Run(IList files)
		{
			ConcGenerator.EnsureDirectory(m_outputDirName);
			m_xslt.Load(@"..\..\osis2ChapIndexFrag.xsl");
			string header = "<!doctype HTML PUBLIC \"-//W3C//DTD HTML 4.0 Transitional//EN\">\n<html>\n"
				+ "<link rel=\"stylesheet\" href=\"display.css\" type=\"text/css\">"
				+ "<head>"
				+ "</head>\n<body class=\"BookChapIndex\">\n"
				+ "<p><a target=\"body\" href=\"treemaster.htm\" onclick=\"if(parent.parent.navigation) parent.parent.navigation.SetBookName('" + m_loading
				+"')\">" + m_concLinkText + "</a></p>\n";
			string trailer = "</body>\n</html>\n";
			string path = Path.Combine(m_outputDirName, "ChapterIndex.htm");
			TextWriter writer = new StreamWriter(path, false, Encoding.UTF8);
			writer.Write(header);

			Progress status = new Progress(files.Count);
			status.Show();
			int count = 0;

			foreach (string inputFile in m_files)
			{
				string filename = Path.GetFileName(inputFile);
				if (files.Contains(Path.ChangeExtension(filename, "xml")))
				{
					status.File = filename;
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

					// Handle introduction if any
					string introCrossRef = "";
					string introFile;
					if (m_introFiles.TryGetValue(inputFile, out introFile))
					{
						string introPath = Path.Combine(m_introDirName, introFile);
						if (File.Exists(introPath))
						{
							introCrossRef = "<p class=\"IndexIntroduction\"><a target=\"main\" href=\"" + introFile + "\">" + m_introText + "</a></p>";
							File.Copy(introPath, Path.Combine(m_outputDirName, introFile), true);
						}
						else
							MessageBox.Show("Introduction file not found: " + introPath, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
					}

					fragment = fragment.Replace("$$intro$$", introCrossRef);
					writer.WriteLine(fragment);
					count++;
					status.Value = count;
				}
			}
			if (m_extraFiles != null)
			{
				foreach (XmlNode fileNode in m_extraFiles)
				{
					if (fileNode.Attributes["name"] == null)
						continue;
					if (fileNode.Attributes["linkText"] == null)
						continue;
					string fileName = fileNode.Attributes["name"].Value;
					string linkText = fileNode.Attributes["linkText"].Value;
					string filePath = Path.Combine(m_extraDirName, fileName);
					if (!File.Exists(filePath))
					{
						MessageBox.Show(String.Format("File {0} requested as link but not found.", filePath), "Warning");
						continue;
					}

					writer.Write("<p class=\"extraLink\"><a target=\"main\" href=\""
					+ fileName + "\">" + linkText + "</a></p>\n");
					File.Copy(filePath, Path.Combine(m_outputDirName, fileName), true);
				}
			}
			writer.Write(trailer);
			writer.Close();

			status.Close();
		}

		private void BuildFileList(XmlNode node)
		{
			foreach (XmlNode item in node.ChildNodes)
			{
				string fileName = item.Attributes["name"].Value;
				string abbr = item.Attributes["abbr"].Value;
				m_files.Add(fileName);
				m_abbreviations[Path.ChangeExtension(fileName, "htm")] = abbr;
				XmlAttribute attr = item.Attributes["intro"];
				if (attr != null)
					m_introFiles[fileName] = attr.Value;
			}
		}


	}

}