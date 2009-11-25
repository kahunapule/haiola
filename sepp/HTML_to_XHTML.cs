using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Diagnostics;
using System.Collections;  // For Process class.

namespace sepp
{
	/// <summary>
	/// This class uses a program called 'tidy' to convert from HTML to XHTML which XmlReader can more easily process.
	/// </summary>
	public class HTML_TO_XHTML : ExternalProgramConverter
	{
		private Options m_options;
		/// <summary>
		/// initialize one.
		/// </summary>
		/// <param name="inputDirName"></param>
		/// <param name="outputDirName"></param>
		public HTML_TO_XHTML(string inputDirName, string outputDirName, Options options)
			: base(inputDirName, outputDirName)
		{
			m_options = options;
		}

		internal override bool LogErrors
		{
			get
			{
				return true;
			}
		}

        //// We can't readily check for file existence here because if chapterPerFile is on, we only have individual
        //// file chapters, not the book as a whole.
        //internal override bool CheckFileExists(string filepath)
        //{
        //    return true;
        //}
		internal override string ToolPath
		{
			get { return Utils.GetUtilityFile("tidy.exe"); }
		}

		internal override string OutputExtension
		{
			get { return "xml"; }
		}

		protected override bool WantToConvert(IList files, string filename)
		{
			if (m_options.ChapterPerFile)
			{
				return base.WantToConvert(files, Utils.MainFileName(filename));
			}
			else
			{
				return base.WantToConvert(files, filename); 
			}
		}

        /// <summary>
        /// Given a main (book) file name, generate the related files we should actually operate on.
        /// </summary>
        /// <param name="mainFilename"></param>
        /// <returns></returns>
        protected override IEnumerable<string> GetActualFileNames(string mainFilename)
        {
            if (m_options.ChapterPerFile)
            {
                return Utils.ChapFiles(mainFilename);
            }
            return new string[] { mainFilename };
        }

		internal override string[] Extensions
		{
			get { return new string[] { "*.htm" }; }
		}

		internal override string CreateArguments(string inputFilePath, string outputFilePath)
		{
			return "-asxhtml -utf8 -o \"" + outputFilePath + "\" \"" + inputFilePath + "\"";
		}

		/// <summary>
		/// Override (see e.g. HTML_to_XHTML.cs) when critical error messages may appear as ordinary output.
		/// </summary>
		/// <param name="message"></param>
		/// <returns></returns>
		protected override bool IndicatesError(string message)
		{
			return message.IndexOf("Error:" )>= 0;
		}


	}

}