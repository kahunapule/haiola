using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace WordSend
{
    public class readssf
    {

        public readssf()
        {
            LangCodes = new LanguageCodeInfo();
        }

        public static LanguageCodeInfo LangCodes;

        /// <summary>
        /// Reads a Paratext .ssf file and imports selected configuration items into our own Options object.
        /// </summary>
        /// <param name="projectOptions">Options object to update with data in the Paratext .ssf file.</param>
        /// <param name="ssfFileName">Full path to the Paratext .ssf file to update.</param>
        public void ReadParatextSsf(Options projectOptions, string ssfFileName)
        {
            string elementName, setting;
            try
            {
                if (!File.Exists(ssfFileName))
                    return;
                XmlTextReader ssf = new XmlTextReader(ssfFileName);
                ssf.WhitespaceHandling = WhitespaceHandling.Significant;
                ssf.MoveToContent();
                while (ssf.Read())
                {
                    if ((ssf.NodeType == XmlNodeType.Element) && (ssf.Name != "ScriptureText"))
                    {
                        if (!ssf.IsEmptyElement)
                        {
                            elementName = ssf.Name;
                            ssf.Read(); // Get content of element
                            if ((ssf.NodeType == XmlNodeType.Text) && (!String.IsNullOrEmpty(ssf.Value)))
                            {
                                setting = ssf.Value;
                                switch (elementName)
                                {
                                    case "Encoding":
                                        if (setting != "65001")
                                            Logit.WriteLine("Warning: Paratext encoding is not Unicode UTF-8 (" + setting + ") in " + ssfFileName);
                                        break;
                                    case "EthnologueCode":
                                        if (projectOptions.languageId.Length < 3)
                                        {
                                            projectOptions.languageId = setting;
                                        }
                                        break;
                                    case "RangeIndicator":  // verse range separator
                                        projectOptions.rangeSeparator = setting;
                                        break;
                                    case "SequenceIndicator":
                                        projectOptions.multiRefSameChapterSeparator = setting;
                                        break;
                                    case "ChapterVerseSeparator":
                                        projectOptions.CVSeparator = setting;
                                        break;
                                    case "ChapterRangeSeparator":
                                        projectOptions.multiRefDifferentChapterSeparator = setting;
                                        break;
                                    case "BookSequenceSeparator":
                                        projectOptions.BookSequenceSeparator = setting;
                                        break;
                                    case "ChapterNumberSeparator":
                                        projectOptions.ChapterNumberSeparator = setting;
                                        break;
                                    case "BookSourceForMarkerXt":
                                        projectOptions.BookSourceForMarkerXt = setting;
                                        break;
                                    case "BookSourceForMarkerR":
                                        projectOptions.BookSourceForMarkerR = setting;
                                        break;
                                    case "Guid":
                                        projectOptions.paratextGuid = setting;
                                        break;
                                }
                            }
                        }
                    }
                }
                ssf.Close();
                projectOptions.Write();
            }
            catch (Exception ex)
            {
                Logit.WriteError("Error reading Paratext options file " + ssfFileName + ": " + ex.Message);
                Logit.WriteError(ex.StackTrace);
            }
        }
    }
}
