using System;
using System.Xml;
using System.Configuration;

namespace XmlNormalizer
{
    public class ConfigHandler
    {
        public ConfigHandler()
        {
        }

        public object GetConfig(string configName)
        {
            try
            {
                return ConfigurationManager.GetSection(configName);
            }
            catch (Exception e)
            {
                new BrokerException(string.Format(Strings.CONFIGURATION_INVALID, e.Message));
                return null;
            }
        }
    }

    public interface IConfig
    {
        string Name { get; }
        XmlElement ConfigXml { get; }
    }

    public class BrokerException
    {
        public BrokerException(string message)
        {
            Console.WriteLine(message);

            throw new Exception(message);
        }
    }

    /// <summary>
    /// Centralize UI strings in this area.
    /// </summary>
    public class Strings
    {
        public static string INPUT_FILE = "inputFile";
        public static string ENCODING = "encoding";
        public static string USAGE = "usage";
        public static string OMIT_UTF8_BOM = "omitUtf8BOM";
        public static string DOC_TYPES = "docTypes";

        // config sections
        public static string PARAMETER_CONFIG = "parameterInfo";
        public static string FILE_TYPES_CONFIG = "fileTypes";
        public static string LOCALE_MAPPING_CONFIG = "localeMappings";

        // normal return code
        public static string SUCCESS_RETURN = "Return value = 0";

        // exception and errors
        public static string INVALID_FILE = "The input file reference '{0}' is not valid.";
        public static string INVALID_ARG = "Argument '{0}' is null or empty. A real value is expected.";
        public static string INVALID_ARG_TYPE = "Argument '{0}' is of incorrect type. Expected '{1}.'";
        public static string INVALID_ARG_COUNT = "Invalid number of arguments passed; expected {0}, got {1}.";
        public static string INVALID_ARG_VALUE = "Invalid argument value passed; expected {0}, got {1}.";
        public static string PROCESSING_EXCEPTION = "An exception occurred while processing file '{0}'. Message: {1}";
        public static string INVALID_TASK_ID = "The Task ID argument supplied '{0}' is not a valid numeric value.";
        public static string ARGUMENT_MISSING = "The required '{0}' argument could not be found.";
        public static string CONFIGURATION_MISSING = "Configuration file '{0}' missing.";
        public static string CONFIGURATION_INVALID = "Configuration invalid. Message: {0}";
        public static string UNKNOWN_FILE_EXTENSION = "Files with extension '{0}' are not supported.\nReturn value = 0";
        public static string UNKNOWN_FILE_TYPE = "The type of file '{0}' could not be determined.\nReturn value = 0";
        public static string UNSUPPORTED_TYPE_RETURN = "Unsupported file type: '{0}'.\nReturn value = 0";
        public static string XML_EXCEPTION = "Unable to find expected 'fileType' attribute in the XML root element of file '{0}'. Please correct.";
        public static string INIT_EXCEPTION = "An exception occurred during stage initialisation. Message: {0}";

    }
}
