using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.Reflection;
using haiola;

namespace WordSend
{
    /// <summary>
    /// Interface for a generic file format converter. For now, we only support either
    /// zero or one plugin. If present, the purpose of the plugin is to provide a place
    /// for conversions to proprietary formats which cannot be put in the main open
    /// source code. If present, "proprietary.dll" will be loaded. If not, it won't,
    /// but all of the open, built-in conversions of Haiola will still be available.
    /// This could be extended to iterate through several plugins, but we really don't
    /// need that capability, right now.
    /// </summary>
    public interface IConverterPlugin
    {
        void RunConversions();  // Write new output formats.
        string ShowStatus();    // Give the user feedback about what is going on.
    }

    /*
    public partial class Extension : IConverterPlugin
    {
        public extern string ShowStatus();    // Give the user feedback about what is going on.
        public extern Extension();
        public extern void RunConversions();
    }
     */


    /// <summary>
    /// Load the plugin if present.
    /// </summary>
    class PluginManager
    {
        private bool pluginPresent = false;

        public IConverterPlugin ThePlugin;

        public bool PluginLoaded()
        {
            return pluginPresent;
        }

        private bool pluginIsRunning = false;

        public bool PluginRunning()
        {
            return pluginIsRunning;
        }

        public string PluginMessage()
        {
            if (pluginPresent)
                return ThePlugin.ShowStatus();
            else
                return "";
        }

        public PluginManager()
        {
            try
            {
                string proprietaryModule = WordSend.SFConverter.FindAuxFile("proprietary.dll");
                if (File.Exists(proprietaryModule))
                {
                    Assembly a = System.Reflection.Assembly.LoadFile(proprietaryModule);
                    ThePlugin = (IConverterPlugin)a.CreateInstance("WordSend.Extension");
                    pluginPresent = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error loading plugin");
            }
        }

        public void DoProprietaryConversions()
        {
            if (pluginPresent)
            {
                pluginIsRunning = true;
                try
                {
                    ThePlugin.RunConversions();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error running extension");
                }
                pluginIsRunning = false;
            }
        }
    }
}
