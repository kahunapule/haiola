using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Diagnostics;
using System.Collections;
using System.Xml;  // For Process class.

namespace sepp
{
	/// <summary>
	/// This class uses Nathan's CPython script to convert USFM to OSIS
	/// </summary>
	public class USFM_to_OSIS : ExternalProgramConverter
	{
		string m_codec = "utf-8"; // string specifying a Python codec.
		string m_pythonPath = @"c:\python25\python.exe";

		/// <summary>
		/// initialize one.
		/// </summary>
		/// <param name="inputDirName"></param>
		/// <param name="outputDirName"></param>
		public USFM_to_OSIS(string inputDirName, string outputDirName, string optionsPath)
			: base(inputDirName, outputDirName)
		{
			XmlDocument optionsDoc = new XmlDocument();
			optionsDoc.Load(optionsPath);
			XmlNode root = optionsDoc.DocumentElement;
			foreach (XmlNode node in root.ChildNodes)
			{
				switch (node.Name)
				{
					case "options":
						m_codec = AttVal(node, "usfmEncoding", "utf-8");
						break;
				}
			}
		}

		internal override string ToolPath
		{
			get { return m_pythonPath; }
		}

		internal override string[] Extensions
		{
			get { return new string[] { "*.ptx", "*.sfm" }; }
		}

		internal override string OutputExtension
		{
			get { return "xml"; }
		}

		internal override string CreateArguments(string inputFilePath, string outputFilePath)
		{
			string scriptPath = Path.GetFullPath(@"..\..\JohnOsisBP.py");
			return "\"" + scriptPath + "\" " + base.CreateArguments(inputFilePath, outputFilePath) + " " + m_codec;
		}
		

		private string AttVal(XmlNode node, string name, string defVal)
		{
			XmlAttribute att = node.Attributes[name];
			if (att != null)
				return att.Value;
			return defVal;
		}
	}

}