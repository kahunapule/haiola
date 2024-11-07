// Top level of imua: build one or more Bible publication projects, given their project configuration .xini file(s). -f to force rebuild.
global using WordSend;
global using BibleFileLib;
global using System.Xml;
global using System.Xml.Schema;

Console.WriteLine("Convert Bible files to useful outputs, as specified in .xini configuration file(s).");
Console.WriteLine("Syntax:");
Console.WriteLine(System.Diagnostics.Process.GetCurrentProcess().ProcessName+" RootDataDirectory [-f] projects");
global globe = new global();
bool rebuildit = false;
Logit.useConsole = true;
Logit.GUIWriteString = null;
clRun processIt = new clRun(globe);

Console.WriteLine(String.Format("Imua Copyright © 2003-2023 SIL, EBT, and eBible.org. Released under Gnu LGPL 3 or later."));
Console.WriteLine("Syntax:");
Console.WriteLine("imua [-f] DataRootDirectory Project(s)");
Console.WriteLine("-f means force rebuild even if the project is already up to date.");

foreach (string s in args)
{
    if (s == "-f")
    {
        rebuildit = true;
        Console.WriteLine("Rebuilding the following projects:");
    }
    else if (String.IsNullOrEmpty(globe.dataRootDir))
    {
        globe.xini = new XMLini(Path.Combine(s, "imua.xini"));
        globe.dataRootDir = s;
        globe.xini.WriteString("globe.dataRootDir", globe.dataRootDir);
        globe.inputDirectory = Path.Combine(globe.dataRootDir, "input");
        globe.outputDirectory = Path.Combine(globe.dataRootDir, "output");
        globe.rebuild = rebuildit;
        Console.WriteLine("Root data directory = " + globe.dataRootDir);
    }
    else
    {
        Console.WriteLine("Project: "+s);
        globe.inputProjectDirectory = Path.Combine(globe.inputDirectory, s);
        globe.projectXiniPath = Path.Combine(globe.inputProjectDirectory, "options.xini");
        globe.outputProjectDirectory = Path.Combine(globe.outputDirectory, s);
        processIt.Run(s);

    }
}