using System;
using System.IO;

namespace XmlNormalizer
{
    /// <summary>
    /// Summary description for ConfigFile.
    /// </summary>
    public class ConfigFile
    {
        private static string CONFIG_FILE_EXT = "config";

        public static string SetConfigFile(string[] args)
        {
            string configFile = string.Empty;

            if (args != null && args.Length > 0)
            {
                foreach (string arg in args)
                {
                    if (arg.EndsWith(CONFIG_FILE_EXT))
                    {
                        string executableName = Path.GetFileName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);

                        if (Path.GetFileName(arg).StartsWith(executableName))
                        {
                            configFile = string.Format("{0}.{1}", System.Reflection.Assembly.GetExecutingAssembly().Location, CONFIG_FILE_EXT);
                        }
                        else
                        {
                            configFile = Path.GetFullPath(arg);
                            AppDomain.CurrentDomain.SetData("APP_CONFIG_FILE", configFile);
                        }
                    }
                }
            }

            return configFile;
        }



    }
}
