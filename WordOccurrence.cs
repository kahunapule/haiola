using System;
using System.Collections.Generic;
using System.Text;

namespace sepp
{
	/// <summary>
	/// WordOccurrence captures the information we want to record about one occurrence of a particular wordform.
	/// </summary>
	class WordOccurrence
	{
		string m_file;
		int m_chapter;
		string m_verse;
		int m_offset;
		string m_context;
		string m_form; // actual form, may be a case variant.

		/// <summary>
		/// Make one.
		/// </summary>
		/// <param name="file"></param>
		/// <param name="chapter"></param>
		/// <param name="verse"></param>
		public WordOccurrence(string file, int chapter, string verse, int offset, string form)
		{
			m_file = file;
			m_chapter = chapter;
			m_verse = verse;
			m_offset = offset;
			m_form = form;
		}

		/// <summary>
		/// The file it occurs in.
		/// </summary>
		public string FileName
		{
			get { return m_file; }
		}

		/// <summary>
		/// The chapter from the most recent reference marker.
		/// </summary>
		public int Chapter
		{
			get { return m_chapter; }
		}

		/// <summary>
		/// The verse from the most recent reference marker.
		/// </summary>
		public string Verse
		{
			get { return m_verse; }
		}

		/// <summary>
		/// The offset into the context string where the occurrence occurs.
		/// </summary>
		public int Offset
		{
			get { return m_offset; }
		}

		/// <summary>
		/// The context information we want to display for this word.
		/// </summary>
		public string Context
		{
			get { return m_context; }
			set { m_context = value; }
		}

		/// <summary>
		/// The actual form in this context (may be a case variant).
		/// </summary>
		public string Form
		{
			get { return m_form; }
		}
	}
}
