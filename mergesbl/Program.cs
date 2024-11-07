// See https://aka.ms/new-console-template for more information
// One-time use code for merging the SBL Greek NT apparatus into the SBL Greek NT USFX file
Console.WriteLine("Syntax: mergesbl usfxin.xml usfxout.xml notefiles.xml");
Console.WriteLine("args.Length = "+(args.Length.ToString()));
if (args.Length > 2)
{
    NoteMerge merge = new NoteMerge();
    for (int i = 2; i < args.Length; i++)
    {
        Console.WriteLine("Reading " + args[i]);
        merge.ReadNotes(args[i]);
    }
    Console.WriteLine("Reading " + args[0], args[1]);
    merge.WriteNotes(args[0], args[1]);
}