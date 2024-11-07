using System;
using System.Collections;
using System.Reflection.Emit;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml;
using WordSend;

public class RawNote
{
    public RawNote()
    {
        bcv = "";
        notes = new string[16];
        len = 0;
    }
    public int len;
	public string bcv;
	public string[] notes;
}


public class NoteMerge
{

	public NoteMerge()
	{
		rawNotes = new Hashtable(8009);
        inFile = null;
	}

	private Hashtable rawNotes;

	public void ReadNotes(string noteFileName)
	{
        int noteCount = 0;
        int verseCount = 0;
		string? line;
		RawNote note;
		note = new RawNote();
		if (File.Exists(noteFileName))
		{
			StreamReader? sr = new StreamReader(noteFileName);
            if (sr != null)
            {
                while ((line = sr.ReadLine()) != null)
                {
                    line = line.Trim();
                    if (String.IsNullOrEmpty(line) || (line.StartsWith("<book>")) || 
                        (line.StartsWith("<noteset>")) || (line.StartsWith("<?")) ||
                        (line.StartsWith("</noteset>")))
                    {
                        // Skip line
                        // Console.WriteLine("Skipping " + line);
                    }
                    else if (line.StartsWith("<verse>"))
                    {
                        if (!String.IsNullOrEmpty(note.bcv))
                        {
                            verseCount++;
                            rawNotes.Add(note.bcv, note);
                            /*
                            Console.WriteLine("verse end: " + note.bcv + " : ");
                            for (i = 0; i < note.len; i++)
                            {
                                Console.WriteLine("notes["+i.ToString()+"]="+note.notes[i]);
                            }
                            */
                            note = new RawNote();
                        }
                        note.bcv = line.Remove(0, 7).Replace("</verse>", "");
                        // Console.WriteLine(line + " -> " + note.bcv);
                    }
                    else if (line.StartsWith("</book>"))
                    {
                        if (!String.IsNullOrEmpty(note.bcv))
                        {
                            rawNotes.Add(note.bcv, note);
                            /*
                            Console.WriteLine("book end: " + note.bcv + " : ");
                            for (i = 0; i < note.len; i++)
                            {
                                Console.WriteLine("notes[" + i.ToString() + "]=" + note.notes[i]);
                            }
                            */
                            note = new RawNote();
                        }
                    }
                    else if (!(line.StartsWith("<p") || line.StartsWith("</p")))
                    {
                        note.notes[note.len++] = line;
                        noteCount++;
                        // Console.WriteLine("note line: " + line);
                    }

                }
                sr.Close();
                Console.WriteLine("Read " + noteCount.ToString() + " notes in " + verseCount + " verses.");
            }
            else
            {
                Console.WriteLine("ERROR: Couldn't open note file " + noteFileName);
            }
		}
	}

    private string name = String.Empty;
    private string attributeName = String.Empty;
    private string attributeValue = String.Empty;
    private string verseText = String.Empty;
    private string book = String.Empty;
    private string chapter = String.Empty;
    private string verse = String.Empty;
    private string currentBCV = String.Empty;
    private StreamReader? inFile;
    private StreamWriter? outFile;
    
    private bool ReadVerse()
    {
        if ((inFile == null) || (outFile == null))
            return false;
        bool inAttribute = false;
        bool inSeparator = false;
        bool inValue = false;
        bool inSpace = false;
        bool inName = false;
        verseText = String.Empty;
        StringBuilder sb = new StringBuilder();
        int c = inFile.Read();
        if (c < 0)
        {
            return false;
        }
        char ch = (char)c;
        if (ch == '<')
        {
            sb.Append(ch);
            name = String.Empty;
            inName = true;
            while ('>' != (ch = (char)inFile.Read()))
            {
                sb.Append(ch);
                if (inName)
                {
                    if (Char.IsLetterOrDigit(ch) || (ch == '/'))
                    {
                        name = name + ch;
                    }
                    else
                    {
                        inName = false;
                        inAttribute = true;
                        attributeName = attributeValue = String.Empty;
                    }

                }
                else if (inAttribute)
                {
                    if (Char.IsLetterOrDigit(ch))
                    {
                        attributeName = attributeName + ch;
                    }
                    else if ((ch == '=') || (ch == '"') || (ch == '\''))
                    {
                        // skip separator
                        inAttribute = false;
                        inSeparator = true;
                    }
                    else if (ch == '/')
                    {
                        inAttribute = false;
                        inSeparator = false;
                    }
                }
                else if (inSeparator)
                {
                    if ((ch != '"') && (ch != '\''))
                    {
                        inSeparator = false;
                        inValue = true;
                        attributeValue = String.Empty + ch;
                    }

                }
                else if (inValue)
                {
                    if (!((ch == '"') || (ch == '\'')))
                    {
                        attributeValue = attributeValue + ch;
                    }
                    else
                    {
                        inValue = false;
                        inSpace = true;
                        if (attributeName == "id")
                        {
                            if (name == "book")
                            {
                                book = attributeValue;
                                currentBCV = book;
                            }
                            else if (name == "c")
                            {
                                chapter = attributeValue;
                                currentBCV = book + "." + chapter;
                            }
                            else if (name == "v")
                            {
                                verse = attributeValue;
                                currentBCV = book + "." + chapter + "." + verse;
                            }
                        }
                        // Console.WriteLine(attributeName + "=" + attributeValue);
                        // Console.WriteLine(currentBCV);
                    }
                }
                else if (inSpace)
                {
                    if (!Char.IsWhiteSpace(ch))
                    {
                        inSpace = false;
                        inAttribute = true;
                        attributeName = attributeValue = String.Empty;
                        if (Char.IsLetterOrDigit(ch))
                        {
                            attributeName = attributeName + ch;
                        }
                    }
                }
            }
            sb.Append(ch);
            outFile.Write(sb.ToString());
            // Console.Write(sb.ToString());
            sb.Clear();
        }
        else
        {
            sb.Append(ch);
            while (((c = inFile.Peek()) > 0) && ('<' != (char)c))
            {
                ch = (char)inFile.Read();
                sb.Append(ch);
            }
            verseText += sb.ToString();
            sb.Clear();
        }
        return true;
    }


    private string getElementString(string s, ref int start, ref string name)
    {
        // Assumptions: XML fragment; no attributes; no empty elements; no nested elements
        StringBuilder elementName = new StringBuilder();
        StringBuilder result = new StringBuilder();
        int state = 0; //0< 1n> 2s <3/n>4
        int i = start;
        while ((i < s.Length) && (state < 4))
        {
            switch (state)
            {
                case 0: // Opening bracket
                    if (s[i] ==  '<')
                    {
                        state = 1; // Element name
                    }
                    break;
                case 1: // Element name
                    if (Char.IsLetterOrDigit((char)s[i]))
                    {
                        elementName.Append(s[i]);
                    }
                    else if (s[i] == '>')
                    {
                        state = 2; // Ended opening element
                        name = elementName.ToString();
                    }
                    break;
                case 2: // Element string
                    if (s[i] == '<')
                    {
                        state = 3; // end of element string; expecting end element
                    }
                    else
                    {
                        result.Append((char)s[i]);
                    }
                    break;
                case 3: // End tag
                    if (s[i] ==  '>')
                    {
                        state = 4;
                    }
                    break;
            }
            i++;
        }
        start = i;
        return result.ToString();
    }


    public void WriteNotes(string inFileName, string outFileName)
	{
        RawNote? currentNote;
        int i;
        int notePos;
        string elementName = String.Empty;
        string s;
        int noteNumber;
        int versePos;
        StringBuilder newVerse = new StringBuilder();
 

        if (String.IsNullOrEmpty(inFileName) || String.IsNullOrEmpty(outFileName))
            return;
        inFile = new StreamReader(inFileName);
        outFile = new StreamWriter(outFileName, false, Encoding.UTF8);
        while (ReadVerse())
        {
            if ((rawNotes.ContainsKey(currentBCV)) && (rawNotes[currentBCV] != null))
            {
                versePos = 0;
                noteNumber = 0;
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
                currentNote = (RawNote)rawNotes[currentBCV];
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                if ((currentNote.len > 0) && !String.IsNullOrEmpty(verseText.Trim()))
                {
                    Console.WriteLine(currentBCV);
                    for (noteNumber = 0; noteNumber < currentNote.len; noteNumber++)
                    {
                        notePos = 0;
                        elementName = String.Empty;
                        while ((elementName != "note") && (notePos < currentNote.notes[noteNumber].Length))
                        {
                            s = getElementString(currentNote.notes[noteNumber], ref notePos, ref elementName);
                            if (versePos < 0)
                                versePos = 0;
                            if ((elementName == "s") || (elementName == "m"))
                            {
                                if (versePos < verseText.Length)
                                {
                                    i = verseText.IndexOf(s, versePos);
                                    if (i == -1)
                                    {
                                        versePos = verseText.Length - 1;
                                    }
                                    else
                                    {
                                        versePos = i + s.Length;
                                    }
                                    if (versePos >= verseText.Length)
                                        versePos = verseText.Length - 1;
                                    Console.WriteLine("skipping " + s);
                                }
                            }
                            else if (elementName == "note")
                            {
                                if ((versePos > 0) && (versePos < verseText.Length))
                                    newVerse.Append(verseText.Substring(0, versePos));
                                Console.WriteLine("Inserting note after "+newVerse.ToString());
                                if (verseText.Length > versePos + 1)
                                    verseText = verseText.Substring(versePos + 1);
                                else
                                    verseText = String.Empty;
                                versePos = 0;
                                newVerse.Append("<f caller=\"+\"><fr>");
                                newVerse.Append(currentBCV.Substring(4).Replace('.',':'));
                                newVerse.Append(" </fr><ft>");
                                newVerse.Append(s);
                                newVerse.Append("</ft></f>");
                            }
                        }
                    }
                    newVerse.Append(verseText);
                    outFile.Write(newVerse.ToString());
                    // Console.WriteLine(currentBCV + " : " + newVerse.ToString());
                    currentNote.len = 0;
                    newVerse.Clear();
                }
                else
                {
                    if (!String.IsNullOrEmpty(verseText))
                        outFile.Write(verseText);
                }
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            }
            else if (!String.IsNullOrEmpty(verseText))
            {
                outFile.Write(verseText);
            }
        }
        outFile.Close();
        inFile.Close();
    }
}
