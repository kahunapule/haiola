using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections;
using System.Text.RegularExpressions;

namespace massregex
{
    class Program
    {
        static string regexFile, inDir, outDir;
 
        static void Main(string[] args)
        {
            if (args.Length != 3)
            {
                Console.WriteLine("Copyright 2012 Michael Paul Johnson.");
                Console.WriteLine("Released under the Gnu Lesser Public License version 3 or later.");
                Console.WriteLine("Syntax:");
                Console.WriteLine("massregex regexFile inDir outDir");
                Console.WriteLine("  regexFile is a text file of the format /match/replace/comment on each line.");
                Console.WriteLine("  inDir is a directory containing all text files to process.");
                Console.WriteLine("  outDir is the directory to write the results to. Existing files will be erased.");
                Console.WriteLine("All text files are assumed to be UTF-8 Unicode.");
            }
            else
            {
                regexFile = args[0];
                inDir = args[1];
                outDir = args[2];
                ProcessFiles();
            }
        }

        static void ProcessFiles()
        {
            string line;
            int i = 0;
            char sep;
            StreamReader inputFile;
            StreamWriter outputFile;
            ArrayList FindThis;
            ArrayList ReplaceWith;

            try
            {
                if (!Directory.Exists(outDir))
                    Directory.CreateDirectory(outDir);
                string[] inputFileNames = Directory.GetFiles(inDir);
                if (inputFileNames.Length == 0)
                {
                    Console.WriteLine("No files found in " + inDir);
                    return;
                }

                FindThis = new ArrayList();
                ReplaceWith = new ArrayList();
                StreamReader sr = new StreamReader(regexFile);
                while (!sr.EndOfStream)
                {
                    line = sr.ReadLine().Trim();
                    if (line.Length > 3)
                    {
                        sep = line[0];
                        string[] parts = line.Split(new char[] { sep });
                        FindThis.Add(parts[1]); // parts[0] is the empty string before the first delimiter
                        ReplaceWith.Add(parts[2]);
                    }
                }
                sr.Close();
                foreach (string inName in inputFileNames)
                {
                    if (File.Exists(inName) && !Directory.Exists(inName))
                    {
                        inputFile = new StreamReader(inName);
                        string text = inputFile.ReadToEnd();
                        inputFile.Close();
                        for (i = 0; i < FindThis.Count; i++)
                        {
                            text = Regex.Replace(text, (string)FindThis[i], (string)ReplaceWith[i]);
                        }
                        outputFile = new StreamWriter(Path.Combine(outDir, Path.GetFileName(inName)));
                        outputFile.Write(text);
                        outputFile.Close();
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error processing " + inDir + " to " + outDir + " with regular expressions in " + regexFile + ":");
                Console.WriteLine(ex.Message);
            }
        }
    }
}
