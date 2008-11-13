using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace sepp
{
	/// <summary>
	/// Information about one input file (corresponds to one "file" child of the "files"
	/// element in the options file.
	/// </summary>
	public class InputFileInfo
	{
		public InputFileInfo(string fileName, string stdAbbr, string vernAbbr, string xrefName, string introFile)
		{
			FileName = Path.ChangeExtension(fileName, "xml");
			StandardAbbr = stdAbbr;
			VernAbbr = vernAbbr;
			if (xrefName != "")
				CrossRefName = xrefName;
			if (introFile != "")
				IntroFileName = introFile;
		}
		public string FileName;
		public string StandardAbbr;
		public string VernAbbr;
		public string CrossRefName;
		public string IntroFileName;
	}
}
