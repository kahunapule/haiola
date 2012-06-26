using System;
using System.Collections.Generic;
using System.Text;

namespace sepp
{
	/// <summary>
	/// Records the information we care about concerning a single wordform.
	/// </summary>
	class WordformInfo : IComparable<WordformInfo>
	{
		string m_wordform;
		List<WordOccurrence> m_occurrences;
		int m_fileNo;
		bool m_fMixedCase;

		public WordformInfo(string form)
		{
			m_occurrences = new List<WordOccurrence>();
			m_wordform = form;
		}

		/// <summary>
		/// The actual form of the word.
		/// </summary>
		public string Form
		{
			get { return m_wordform; }
			set { m_wordform = value; }
		}

		/// <summary>
		/// List of occurrences in the documents.
		/// </summary>
		public List<WordOccurrence> Occurrences
		{
			get { return m_occurrences; }
		}

		/// <summary>
		/// The file generated for the form.
		/// </summary>
		public int FileNumber
		{
			get { return m_fileNo; }
			set { m_fileNo = value; }
		}

		public bool MixedCase
		{
			get { return m_fMixedCase; }
			set { m_fMixedCase = value; }
		}


		#region IComparable<WordformInfo> Members

		public int CompareTo(WordformInfo other)
		{
			return m_wordform.CompareTo(other.Form);
		}

		#endregion
	}
}
