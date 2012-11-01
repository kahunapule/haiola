using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace sepp
{
	static class Program
	{
        public static bool autorun = false;
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
        static void Main(string[] args)
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
            Master masterWindow = new Master();
            if ((args.Length > 0) && (args[0].CompareTo("-a") == 0))
            {   // if -a is the first command line parameter, just run selected tasks on all projects then exit.
                autorun = true;
            }
			Application.Run(masterWindow);
		}
	}
}