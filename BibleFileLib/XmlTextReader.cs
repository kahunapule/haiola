// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003-2013, SIL International, EBT, and Youth With A Mission
// <copyright from='2003' to='2013' company='SIL International, EBT, and Youth With A Mission'>
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
// File: XmlFileReader.cs
// Responsibility: (Kahunapule) Michael P. Johnson
// Last reviewed: 
// 
// <remarks>
// An extended XmlTextReader that keeps track of the current node path
// </remarks>
// --------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Data;
using System.IO;
using System.Collections;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Schema;
using System.Diagnostics;


namespace WordSend
{

    /// <summary>
    /// This class extends XmlTextReader to keep track of some the current node
    /// path as the file is read. It also adds a copynode method to make XML
    /// copy with modification operations easier.
    /// 
    /// Only the constructor that takes a file name as a parameter is supported.
    /// (But you could easily extend it if you need to read from something other
    /// than a named text file.)
    /// </summary>
    public class XmlFileReader : XmlTextReader
    {
        protected ArrayList nodePathList;
        protected string nodePathCache;
        protected bool atEmptyElement;
        public string currentElement;

        /// <summary>
        /// Instantiate a new XmlFileReader object to read the file with the given name
        /// </summary>
        /// <param name="fileName">Name of XML file to read</param>
        public XmlFileReader(string fileName)
            : base(fileName)
        {
            nodePathList = new ArrayList(64);
            currentElement = "";
        }

        /// <summary>
        /// Read the next XML element
        /// </summary>
        /// <returns>true iff there was a node to read and it was read OK</returns>
        public override bool Read()
        {
            bool result = base.Read();
            nodePathCache = null;
            if (result)
            {
                if (NodeType == XmlNodeType.Element)
                {
                    currentElement = Name;
                    atEmptyElement = IsEmptyElement;
                    if (!IsEmptyElement)
                        nodePathList.Add(Name);
                }
                else if (NodeType == XmlNodeType.EndElement)
                {
                    if (nodePathList.Count > 0)
                        nodePathList.RemoveAt(nodePathList.Count - 1);
                    atEmptyElement = false;
                }

            }
            return result;
        }

        /// <summary>
        /// Access the current node path
        /// </summary>
        /// <returns>string representation of the current XML node</returns>
        public string NodePath()
        {
            if (nodePathCache != null)
                return nodePathCache;
            int i;
            StringBuilder sb = new StringBuilder(128);

            for (i = 0; i < nodePathList.Count; i++)
            {
                sb.Append("/");
                sb.Append((string)nodePathList[i]);
            }
            if (atEmptyElement)
            {
                sb.Append("/");
                sb.Append(currentElement);
            }
            sb.Append("/");
            nodePathCache = sb.ToString();
            return nodePathCache;
        }

        /// <summary>
        /// Check to see if the current node path contains a given string
        /// </summary>
        /// <param name="s">string to check for in the node path</param>
        /// <returns>true iff the string is present in the current node path</returns>
        public bool NodePathContains(string s)
        {
            string np = NodePath();
            return np.IndexOf(s) != -1;
        }

        /// <summary>
        /// Copy a node from the current XmlTextReader object to the given XmlTextWriter object.
        /// </summary>
        /// <param name="xw">the XmlTextWriter object to write to</param>
        public void CopyNode(XmlTextWriter xw)
        {
            switch (NodeType)
            {
                case XmlNodeType.Element:
                    xw.WriteStartElement(Name);
                    xw.WriteAttributes(this, true);
                    if (IsEmptyElement)
                        xw.WriteEndElement();
                    break;
                case XmlNodeType.EndElement:
                    xw.WriteEndElement();
                    break;
                case XmlNodeType.Text:
                    xw.WriteString(Value);
                    break;
                case XmlNodeType.SignificantWhitespace:
                    xw.WriteWhitespace(Value);
                    break;
                case XmlNodeType.Whitespace:
                    // You could insert xw.WriteWhitespace(Value); to preserve
                    // insignificant white space, but it either adds bloat or
                    // messes up formatting.
                    break;
                case XmlNodeType.Attribute:
                    xw.WriteAttributeString(Name, Value);
                    break;
                case XmlNodeType.ProcessingInstruction:
                    xw.WriteProcessingInstruction(Name, Value);
                    break;
                case XmlNodeType.XmlDeclaration:
                    xw.WriteStartDocument(true);
                    break;
                default:
                    Logit.WriteLine("Doing NOTHING with type=" + NodeType.ToString() + " Name=" + Name + " Value=" + Value); // DEBUG
                    break;
            }
        }
    }
}