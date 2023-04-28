using System;
using static System.Net.Mime.MediaTypeNames;

public class clRun
{
    global globe;
    Ethnologue eth;


    public clRun(global g)
	{
        globe = g;
        eth = new Ethnologue();
        globe.orderFile = "bookorder.txt";
    }

    public bool UpdateConsole(string status)
    {
        Console.Write("\r                                                                                            \r");
        Console.Write(status+"        ");
        return true;
    }


	public bool Run(string projName)
	{
        bool result = false;
        globe.UpdateStatus = UpdateConsole;
        Logit.UpdateStatus = UpdateConsole;
        fileHelper.fAllRunning = true;

        fileHelper.DebugWrite("Processing " + projName);
        if (globe.projectOptions == null)
        {
            globe.projectOptions = new Options(globe.projectXiniPath, globe);
        }
        else
        {
            globe.projectOptions.Reload(globe.projectXiniPath);
        }
        if (globe.rebuild)
            globe.projectOptions.done = false;
        if (globe.projectOptions.languageId.Length == 3)
        {
            globe.er = eth.ReadEthnologue(globe.projectOptions.languageId);
            if (globe.projectOptions.country.Length == 0)
                globe.projectOptions.country = globe.er.countryName;
            if (globe.projectOptions.countryCode.Length == 0)
                globe.projectOptions.countryCode = globe.er.countryId;
            if (globe.projectOptions.languageNameInEnglish.Length == 0)
                globe.projectOptions.languageNameInEnglish = globe.er.langName;

            if (globe.projectOptions.done || !fileHelper.lockProject(globe.inputProjectDirectory))
            {
                Console.WriteLine("Project locked or done. Skipping.");
                return result;
            }


            // Find out what kind of input we have (USFX, USFM, or USX)
            // and produce USFX, USFM, (and in the future) USX outputs.

            globe.orderFile = Path.Combine(globe.inputProjectDirectory, "bookorder.txt");
            if (!File.Exists(globe.orderFile))
                globe.orderFile = SFConverter.FindAuxFile("bookorder.txt");
            StreamReader sr = new StreamReader(globe.orderFile);
            globe.projectOptions.allowedBookList = sr.ReadToEnd();
            sr.Close();

            if (!globe.GetSource())
            {
                Logit.WriteError("No source directory found for " + projName + "!");
                fileHelper.unlockProject();
                return false;
            }
            if ((globe.projectOptions.currentFingerprint == globe.projectOptions.builtFingerprint) && !globe.rebuild)
            {
                Logit.WriteLine("Skipping up-to-date project " + projName + " built: " + globe.projectOptions.lastRunDate.ToString());
                globe.projectOptions.done = true;
                globe.projectOptions.Write();
                fileHelper.unlockProject();
                return true;
            }
            Utils.DeleteDirectory(Path.Combine(globe.outputProjectDirectory, "search"));
            Utils.DeleteDirectory(Path.Combine(globe.outputProjectDirectory, "readaloud"));
            Utils.DeleteDirectory(Path.Combine(globe.outputProjectDirectory, "WordML"));
            Utils.DeleteDirectory(Path.Combine(globe.outputProjectDirectory, "sql"));

            globe.preferredCover = globe.CreateCover();
            if (File.Exists(globe.preferredCover))
            {
                fileHelper.DebugWrite(globe.preferredCover + " created.");
            }
            else
            {
                fileHelper.DebugWrite(globe.preferredCover + " creation FAILED.");
            }
            // Create verseText.xml with unformatted canonical text only in verse containers.
            if (fileHelper.fAllRunning)
            {
                fileHelper.DebugWrite("Preparing search text.");
                globe.PrepareSearchText();
            }
                // Create epub file
                string epubDir = Path.Combine(globe.outputProjectDirectory, "epub");
            if (fileHelper.fAllRunning && globe.projectOptions.makeEub)
            {
                fileHelper.DebugWrite("Creating epub.");
                Utils.DeleteDirectory(epubDir);
                globe.ConvertUsfxToEPub();
            }
            // Create HTML output for posting on web sites.
            string htmlDir = Path.Combine(globe.outputProjectDirectory, "html");
            if (fileHelper.fAllRunning && globe.projectOptions.makeHtml)
            {
                fileHelper.DebugWrite("Creating simple HTML.");
                Utils.DeleteDirectory(htmlDir);
                globe.ConvertUsfxToPortableHtml();
            }
            string WordMLDir = Path.Combine(globe.outputProjectDirectory, "WordML");
            if (fileHelper.fAllRunning && globe.projectOptions.makeWordML)
            {   // Write out WordML document
                // Note: this conversion departs from the standard architecture of making the USFX file the hub, because the WordML writer code was already done in WordSend,
                // and expected USFM input. Therefore, we read the normalized USFM files, which should be present even if the project input is USFX or USX.
                // If this code needs much maintenance in the future, it may be better to refactor the WordML output to go from USFX to WordML directly.
                // Then again, USFX to Open Document Text would be better.
                try
                {
                    fileHelper.DebugWrite("Making WordML.");
                    Utils.DeleteDirectory(WordMLDir);
                    globe.currentConversion = "Reading normalized USFM";
                    string logFile = Path.Combine(globe.outputProjectDirectory, "WordMLConversionReport.txt");
                    Logit.OpenFile(logFile);
                    SFConverter.scripture = new Scriptures(globe);
                    string seedFile = Path.Combine(globe.inputProjectDirectory, "Scripture.xml");
                    if (!File.Exists(seedFile))
                    {
                        seedFile = Path.Combine(globe.inputDirectory, "Scripture.xml");
                    }
                    if (!File.Exists(seedFile))
                    {
                        seedFile = SFConverter.FindAuxFile("Scripture.xml");
                    }
                    SFConverter.scripture.templateName = seedFile;
                    SFConverter.ProcessFilespec(Path.Combine(Path.Combine(globe.outputProjectDirectory, "usfm"), "*.usfm"));
                    globe.currentConversion = "Writing WordML";
                    Utils.EnsureDirectory(WordMLDir);
                    SFConverter.scripture.WriteToWordML(Path.Combine(WordMLDir, globe.projectOptions.translationId + "_word.xml"));
                }
                catch (Exception ex)
                {

                    Logit.WriteError("Error writing WordML file: " + ex.Message);
                    Logit.WriteError(ex.StackTrace);
                    globe.projectOptions.makeWordML = false;
                }
                Logit.CloseFile();
            }
            // Create sile files for conversion to PDF.
            string sileDir = Path.Combine(globe.outputProjectDirectory, "sile");
            if (fileHelper.fAllRunning && globe.projectOptions.makeSile)
            {
                fileHelper.DebugWrite("Creating sile files.");
                Utils.DeleteDirectory(sileDir);
                globe.ConvertUsfxToSile();
            }
            // Create Modified OSIS output for conversion to Sword format.
            string mosisDir = Path.Combine(globe.outputProjectDirectory, "mosis");
            if (fileHelper.fAllRunning && globe.projectOptions.makeSword)
            {
                fileHelper.DebugWrite("Creating MOSIS.");
                Utils.DeleteDirectory(mosisDir);
                globe.clKludge = true;
                globe.ConvertUsfxToMosis();
            }
            string xetexDir = Path.Combine(globe.outputProjectDirectory, "xetex");
            if (fileHelper.fAllRunning && globe.projectOptions.makePDF)
            {
                fileHelper.DebugWrite("Creating XeTeX.");
                globe.ConvertUsfxToPDF(xetexDir);
            }
            string browserBibleDir = Path.Combine(globe.outputProjectDirectory, "browserBible");
            DateTime browserBibleCreated = Directory.GetCreationTime(browserBibleDir);
            if (fileHelper.fAllRunning && globe.projectOptions.makeBrowserBible)
            {
                fileHelper.DebugWrite("Creating Browser Bible module.");
                Utils.DeleteDirectory(browserBibleDir);
                globe.currentConversion = "Writing browser Bible module";
                globe.EnsureTemplateFile("haiola.css");
                globe.EnsureTemplateFile("prophero.css");
                string logFile = Path.Combine(globe.outputProjectDirectory, "browserBibleModuleConversionReport.txt");
                Logit.OpenFile(logFile);

                WordSend.WriteBrowserBibleModule wism = new WriteBrowserBibleModule();
                wism.globe = globe;
                wism.certified = globe.certified;
                wism.WriteTheModule();
            }
            // Run custom per project scripts.
            if (fileHelper.fAllRunning)
            {
                fileHelper.DebugWrite("Postprocessing.");
                globe.DoPostprocess();
                globe.projectOptions.done = true;
                globe.projectOptions.selected = !globe.projectOptions.lastRunResult;
                if (globe.projectOptions.lastRunResult)
                {
                    globe.projectOptions.lastRunDate = DateTime.UtcNow;
                    globe.projectOptions.builtFingerprint = globe.projectOptions.currentFingerprint;
                }
                else
                    globe.projectOptions.lastRunDate = DateTime.MinValue;
                globe.projectOptions.Write();
            }
            if (Logit.loggedError)
                Logit.WriteLine("Error logged processing " + projName);
            else
                Logit.WriteLine("Success processing " + projName);
            fileHelper.unlockProject();
        }

        return result;
	}
}
