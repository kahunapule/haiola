using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections;
using System.Diagnostics;

namespace binfilter
{
    class Program
    {
        static void Main(string[] args)
        {
            string inDir, outDir;
            if (args.Length != 2)
            {
                Console.WriteLine("Copyright 2012 Michael Paul Johnson.");
                Console.WriteLine("Released under the Gnu Lesser Public License version 3 or later.");
                Console.WriteLine("Syntax:");
                Console.WriteLine("binfilter inDir outDir");
                Console.WriteLine("  inDir is a folder containing all text files to process.");
                Console.WriteLine("  outDir is the folder to write the results to. Existing files will be erased.");
                Console.WriteLine("Output text files are UTF-8 Unicode.");
            }
            else
            {
                inDir = args[0];
                outDir = args[1];
                FilterFiles(inDir, outDir);
            }
        }

        /// <summary>
        /// Reads a byte stream, applies programmed conversions, and writes a UTF-8 text file out.
        /// </summary>
        /// <param name="inDir">Directory containing input files.</param>
        /// <param name="outDir">Directory containing output files.</param>
        static void FilterFiles(string inDir, string outDir)
        {
            StreamWriter outputFile;
            byte[] sourceBytes;
            byte b;
            int i;
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

                foreach (string inName in inputFileNames)
                {
                    if (File.Exists(inName) && !Directory.Exists(inName))
                    {
                        Console.WriteLine(inName);
                        sourceBytes = File.ReadAllBytes(inName);
                        string outName = Path.Combine(outDir, Path.GetFileName(inName));
                        outputFile = new StreamWriter(outName);

                        for (i = 0; i < sourceBytes.Length; i++)
                        {
                            b = (byte)(sourceBytes[i] & 0x7F);
                            if (b != 0x1A)
                                outputFile.Write((char)b);
                        }
                        outputFile.Close();
                        Console.WriteLine(" -> " + outName);
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error processing " + inDir + " to " + outDir);
                Console.WriteLine(ex.Message);
            }
        }

    }
}
