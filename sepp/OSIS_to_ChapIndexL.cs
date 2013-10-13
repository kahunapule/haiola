using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
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
		string m_inputDirName; // contains OSIS files
		string m_outputDirName; // site\Conc, where we put generated chap index
		string m_introDirName; // directory contains corresponding introduction files.
		string m_extraDirName; // directory contains extra files to make links to at end.
		XslCompiledTransform m_xslt = new XslCompiledTransform();
		private Options m_options;
		/// <summary>
		/// initialize one.
		/// </summary>
		/// <param name="inputDirName"></param>
		/// <param name="outputDirName"></param>
		public OSIS_to_ChapIndex(string inputDirName, string outputDirName, string introDirName, string extraDirName, Options options)
		{
			m_inputDirName = inputDirName;
			m_outputDirName = outputDirName;
			m_introDirName = introDirName;
			m_extraDirName = extraDirName;
			m_options = options;
		}

		/// <summary>
		/// Run the algorithm (on all files).
		/// </summary>
		public void Run(IList files)
		{
			Utils.EnsureDirectory(m_outputDirName);
			m_xslt.Load(Utils.GetUtilityFile("osis2ChapIndexFrag.xsl"));

            string header = "<!doctype HTML PUBLIC \"-//W3C//DTD HTML 4.0 Transitional//EN\">\n<html>\n"
                + "<link rel=\"stylesheet\" href=\"display.css\" type=\"text/css\">"
                + "<head>"
                + "</head>\n<body class=\"BookChapIndex\">\n"
                + "<p><a target=\"body\" href=\"treeMaster.htm\">" + m_options.ConcordanceLinkText + "</a></p>\n";
            string bcHeaderPath = Path.Combine(Path.GetDirectoryName(m_inputDirName), "bookChapterHeader.txt");
            if (File.Exists(bcHeaderPath))
            {
                string headerFmt = new StreamReader(bcHeaderPath, Encoding.UTF8).ReadToEnd();
                header = string.Format(headerFmt, m_options.ConcordanceLinkText);
            }
			string trailer = "</body>\n</html>\n";

            string bcFooterPath = Path.Combine(Path.GetDirectoryName(m_inputDirName), "bookChapterFooter.txt");
            if (File.Exists(bcFooterPath))
            {
                string trailerFmt = new StreamReader(bcFooterPath, Encoding.UTF8).ReadToEnd();
                trailer = string.Format(trailerFmt, m_options.ConcordanceLinkText);
            }
            string path = Path.Combine(m_outputDirName, "ChapterIndex.htm");
			TextWriter writer = new StreamWriter(path, false, Encoding.UTF8);
			writer.Write(header);

			Progress status = new Progress(files.Count);
			status.Show();
			int count = 0;

			foreach (string inputFile in m_options.MainFiles)
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
					if (m_options.ChapterPerFile)
					{
						htmlFile = ChapterSplitter.BuildNextFileLinkTargetName(htmlFile);
					}
					fragment = fragment.Replace("$$filename$$", htmlFile);
					fragment = fragment.Replace(" xmlns:osis=\"http://www.bibletechnologies.net/2003/OSIS/namespace\"", "");

					// Handle introduction if any
					string introCrossRef = "";
					string introFile;
					string introPath = null;
					if (m_options.IntroFiles.TryGetValue(inputFile, out introFile))
					{
						introPath = Path.Combine(m_introDirName, introFile);
						if (File.Exists(introPath))
							File.Copy(introPath, Path.Combine(m_outputDirName, introFile), true);
						else
							MessageBox.Show("Introduction file not found: " + introPath, "Warning", MessageBoxButtons.OK,
							                MessageBoxIcon.Warning);
					}
					else
					{
						// See if we generated one
						introPath = Path.Combine(m_outputDirName, OSIS_to_HTML.MakeIntroFileName(filename));
						if (File.Exists(introPath))
							introFile = Path.GetFileName(introPath);
						else
							introPath = null;
					}

					if (introPath != null)
					{
						introCrossRef = "<p class=\"IndexIntroduction\"><a target=\"main\" href=\"" + introFile + "\">" +
						                m_options.IntroductionLinkText + "</a></p>";
					}

					fragment = fragment.Replace("$$intro$$", introCrossRef);
					if (m_options.ChapterPerFile)
						fragment = FixChapterHrefs(fragment);
					writer.WriteLine(fragment);
					count++;
					status.Value = count;
				}
			}
			if (m_options.ExtraFiles != null)
			{
				foreach (ExtraFileInfo efi in m_options.ExtraFiles)
				{
					string fileName = efi.FileName;
					string linkText = efi.HotLinkText;
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

		// Where we find an href like href="Kup-KEJ-Final-Pe.htm#C2" insert the chapter number into the reference
		private string FixChapterHrefs(string input)
		{
			Regex reHref = new Regex("(href=\"[^#\"]+-)" + ChapterSplitter.tocTag + "\\.htm#C(\\d+)\""); 
			StringBuilder output = new StringBuilder();
			int start = 0;
			for (Match m = reHref.Match(input); m.Success; m = m.NextMatch())
			{
				output.Append(input.Substring(start, m.Index - start));
				start = m.Index + m.Length;
				output.Append(m.Groups[1].ToString()); // pattern up to end of filename w/o extension
				output.Append(m.Groups[2].ToString()); // chapter number
				output.Append(".htm\""); // leave off #Cnn so we go to start of file.
			}
			output.Append(input.Substring(start, input.Length - start));// balance of input
			return output.ToString();

		}
	}
}