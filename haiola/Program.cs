using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace haiola
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
            if ((args.Length > 0) && (args[0].CompareTo("-a") == 0))
            {   // if -a is the first command line parameter, just run selected tasks on all projects then exit.
                autorun = true;
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new haiolaForm());
        }
    }
}
