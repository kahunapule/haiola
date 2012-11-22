using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections;
using System.Diagnostics;

namespace CountChars
{
    class Program
    {
        static SortedList charList;

        static void CountChars(string name)
        {
            char ch;
            int c;
            int n;

            if (Directory.Exists(name))
            {
                Console.WriteLine("Directory: {0}", name);
                string[] fileEntries = Directory.GetFiles(name);
                foreach (string fileName in fileEntries)
                {
                    CountChars(fileName);
                }
            }
            else if (File.Exists(name) && (Path.GetExtension(name).ToLowerInvariant() == ".htm"))
            {
                Console.WriteLine("File: {0}", name);
                StreamReader sr = new StreamReader(name);
                while (!sr.EndOfStream)
                {
                    c = sr.Read();
                    if (c >= 0)
                    {
                        ch = (char)c;
                        if (charList[ch] == null)
                        {
                            charList.Add(ch, 1);
                        }
                        else
                        {
                            n = (int)charList[ch];
                            charList[ch] = n + 1;
                        }
                    }
                }
                sr.Close();
            }
        }

        static void Main(string[] args)
        {
            charList = new SortedList();
            foreach (string fileName in args)
            {
                CountChars(fileName);
            }
            StreamWriter htm = new StreamWriter("CharList.htm", false, Encoding.UTF8);
            int i, u, n;
            char ch;
            htm.WriteLine(
    "<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Transitional//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd\">");
            htm.WriteLine("<html xmlns:msxsl=\"urn:schemas-microsoft-com:xslt\" xmlns:user=\"urn:nowhere\">");
            htm.WriteLine("<head>");
            htm.WriteLine("<meta http-equiv=\"content-type\" content=\"text/html; charset=utf-8\">");
            htm.WriteLine("<meta name=\"viewport\" content=\"width=device-width\" />");
            htm.WriteLine("<link rel=\"stylesheet\" href=\"prophero.css\" type=\"text/css\">");
            htm.WriteLine("<title>Character List</title>");
            htm.WriteLine("</head>");
            htm.WriteLine("<body class=\"mainDoc\"{0}>");
            htm.WriteLine("<div class=\"main\">");
            htm.WriteLine("<h1>Character List</h1>");
            htm.WriteLine("<table border=\"2\" cellpadding=\"2\" cellspacing=\"2\"><tbody>");
            htm.WriteLine("<tr><td>Unicode</td><td>Char</td><td>Count</td></tr>");
            for (i = 0; i < charList.Count; i++)
            {
                ch = (char)charList.GetKey(i);
                u = (int)ch;
                n = (int)charList.GetByIndex(i);
                htm.WriteLine("<tr><td>U+{0}</td><td> {1}</td><td>{2}</td></tr>", u.ToString("X4"), ch, n);
            }
            htm.WriteLine("</tbody></table>");
            htm.WriteLine("</div>");
            htm.Close();
            System.Diagnostics.Process.Start("CharList.htm");
        }
    }
}
