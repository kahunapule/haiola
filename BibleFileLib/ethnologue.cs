using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using System.Collections;
using System.Windows.Forms;
using WordSend;

namespace WordSend
{
    public class ethnorecord
    {
        public ethnorecord()
        {
            langId = langName = countryId = countryName = region = String.Empty;
        }
        public string langId;
        public string langName;
        public string countryId;
        public string countryName;
        public string region;
        public string countries;
    }

    public class Ethnologue
    {
        public class country
        {   // Country data from Ethnologue
            public string code;
            public string name;
            public string region;
            public string languages;
        }

        public class language
        {
            public string langid;
            public string countryid;
            public string status;
            public string name;
            public string countries;
        }

        public class langCountry
        {   // Association of one language to one country
            public string langCode;
            public string country;
        }

        public langCountry oneLangCountry;

        public ArrayList langCountries;

        public ArrayList countryList;

        public Hashtable countries;
        public Hashtable languages;

        public country oneCountry;
        public language oneLang;

        protected string ReadXmlText(XmlTextReader xml)
        {
            string result = String.Empty;
            if (!xml.IsEmptyElement)
            {
                xml.Read();
                if (xml.NodeType == XmlNodeType.Text)
                    result = xml.Value;
            }
            return result;
        }

        public class CompareCountries : IComparer
        {
            public int Compare(object x, object y)
            {
                country a = (country)x;
                country b = (country)y;
                return String.Compare(a.name, b.name, true, System.Globalization.CultureInfo.InvariantCulture);
            }
        }


        /// <summary>
        /// Read Ethnologue data from CountryCodes.xml and LanguageCodes.xml
        /// </summary>
        public Ethnologue()
        {
            try
            {
                countries = new Hashtable();
                languages = new Hashtable();
                langCountries = new ArrayList();
                countryList = new ArrayList();
                XmlTextReader ethnologue = new XmlTextReader(WordSend.SFConverter.FindAuxFile("CountryCodes.xml"));
                ethnologue.WhitespaceHandling = WhitespaceHandling.None;
			    ethnologue.MoveToContent();
                while (ethnologue.Read())
                {
                    if (ethnologue.NodeType == XmlNodeType.Element)
                    {
                        switch (ethnologue.Name)
                        {
                            case "country":
                                oneCountry = new country();
                                break;
                            case "code":
                                    oneCountry.code = ReadXmlText(ethnologue);
                                break;
                            case "name":
                                oneCountry.name = ReadXmlText(ethnologue);
                                break;
                            case "region":
                                oneCountry.region = ReadXmlText(ethnologue);
                                break;
                        }
                    }
                    else if ((ethnologue.NodeType == XmlNodeType.EndElement) && (ethnologue.Name == "country"))
                    {
                        if (!String.IsNullOrEmpty(oneCountry.code))
                        {
                            countries.Add(oneCountry.code, oneCountry);
                            countryList.Add(oneCountry);
                        }
                    }
                }
                ethnologue.Close();
                IComparer compCountries = (IComparer)new CompareCountries();
                countryList.Sort(compCountries);

                ethnologue = new XmlTextReader(WordSend.SFConverter.FindAuxFile("LanguageCodes.xml"));
                ethnologue.WhitespaceHandling = WhitespaceHandling.None;
                ethnologue.MoveToContent();
                while (ethnologue.Read())
                {
                    if (ethnologue.NodeType == XmlNodeType.Element)
                    {
                        switch (ethnologue.Name)
                        {
                            case "lang":
                                oneLang = new language();
                                break;
                            case "langid":
                                oneLang.langid = ReadXmlText(ethnologue);
                                break;
                            case "countryid":
                                oneLang.countryid = ReadXmlText(ethnologue);
                                break;
                            case "status":
                                oneLang.status = ReadXmlText(ethnologue);
                                break;
                            case "name":
                                oneLang.name = ReadXmlText(ethnologue);
                                break;
                        }
                    }
                    else if ((ethnologue.NodeType == XmlNodeType.EndElement) && (ethnologue.Name == "lang"))
                    {
                        if (!String.IsNullOrEmpty(oneLang.langid))
                            languages.Add(oneLang.langid, oneLang);
                    }
                }
                ethnologue.Close();
                ethnologue = new XmlTextReader(WordSend.SFConverter.FindAuxFile("languagecountry.xml"));
                ethnologue.MoveToContent();
                while (ethnologue.Read())
                {
                    if (ethnologue.NodeType == XmlNodeType.Element)
                    {
                        switch (ethnologue.Name)
                        {
                            case "langHome":
                                oneLangCountry = new langCountry();
                                break;
                            case "langCode":
                                oneLangCountry.langCode = ReadXmlText(ethnologue);
                                break;
                            case "country":
                                oneLangCountry.country = ReadXmlText(ethnologue);
                                break;
                        }
                    }
                    else if ((ethnologue.NodeType == XmlNodeType.EndElement) && (ethnologue.Name == "langHome"))
                    {
                        if (!String.IsNullOrEmpty(oneLangCountry.country) && !String.IsNullOrEmpty(oneLangCountry.langCode))
                            langCountries.Add(oneLangCountry);
                    }
                }
                ethnologue.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// Return Ethnologue data for a given language code
        /// </summary>
        /// <param name="languageId">3-letter ISO/Ethnologue language code</param>
        /// <returns>ethnorecord for that language</returns>
        public ethnorecord ReadEthnologue(string languageId)
        {
            ethnorecord result = new ethnorecord();
            result.langId = languageId;
            language lang = (language)languages[languageId];
            if (lang != null)
            {
                result.langName = lang.name;
                result.countryId = lang.countryid;
                if (String.IsNullOrEmpty(lang.countries))
                {
                    lang.countries = lang.countryid;
                    foreach (langCountry lc in langCountries)
                    {
                        if ((lc.langCode == languageId) && (!lang.countries.Contains(lc.country)))
                        {
                            lang.countries = lang.countries + " " + lc.country;
                        }
                    }

                }
                result.countries = lang.countries;
                if (String.IsNullOrEmpty(result.countries))
                    result.countries = String.Empty;
                country cntry = (country)countries[lang.countryid];
                if (cntry != null)
                {
                    result.countryName = cntry.name;
                    result.region = cntry.region;
                }
            }
            return result;
        }


        public country countryLanguages(string countryCode)
        {
            country result = (country)countries[countryCode];
            if (String.IsNullOrEmpty(result.languages))
            {
                foreach (langCountry lc in langCountries)
                {
                    if (lc.country == countryCode)
                    {
                        if (String.IsNullOrEmpty(result.languages))
                            result.languages = lc.langCode;
                        else
                            result.languages = result.languages + " " + lc.langCode;
                    }
                }
            }
            return result;
        }
    }
}
