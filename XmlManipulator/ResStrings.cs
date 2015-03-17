using System;

namespace XmlManipulator
{
	public class BrokerException
	{
		public BrokerException(string message)
		{
			Console.WriteLine(message);
		}
	}

	public class ResStrings
	{
		public static string PARAMETER_INFO_CONFIG = "parameterInfo";

		public static string UPDATE_INFO_CONFIG		= "updateInfo";

		public static string PRE_PROCESSING_TOOL = "preProcessingTool";
		
		public static string SUCCESS_RETURN = "Return value = 0";

		// exception and errors
		public static string INVALID_FILE = "The input file reference '{0}' is not valid.";
		public static string INVALID_ARG = "Argument '{0}' is null or empty. A real value is expected.";
		public static string INVALID_ARG_TYPE = "Argument '{0}' is of incorrect type. Expected '{1}.'";
		public static string INVALID_ARG_COUNT = "Invalid number of arguments passed; expected {0} or {1}, got {2}.";
		public static string INVALID_ARG_VALUE = "Invalid argument value passed; expected {0}, got {1}.";
		public static string PROCESSING_EXCEPTION = "An exception occurred while processing file '{0}'. Message: {1}";
		public static string INIT_EXCEPTION = "An exception occurred during stage initialisation. Message: {0}";
		public static string INVALID_TASK_ID = "The Task ID argument supplied '{0}' is not a valid numeric value.";
		public static string ARGUMENT_MISSING = "The required '{0}' argument could not be found.";
		public static string CONFIGURATION_MISSING = "Configuration file '{0}' missing.";
		public static string CONFIGURATION_INVALID = "Configuration invalid. Message: {0}";
		public static string UNKNOWN_FILE_EXTENSION = "Files with extension '{0}' are not supported.";
		public static string UNSUPPORTED_TYPE_RETURN = "Unsupported file type: '{0}'.\nReturn value = 0";
		public static string FILE_WITHOUT_EXTENSION = "File '{0}' has no extension.\nReturn value = 0";
		public static string UNSUPPORTED_LANGUAGE = "Language code '{0}' is not present in the configuration.\nReturn value = 0";
		public static string UNSUPPORTED_EXTENSION = "Extension '{0}' is not supported for language code '{1}'.\nReturn value = 0";
		public static string USAGE_INVALID = "The use of this broker could not be determined from the supplied parameter '{0}'.";

		public static string BROKER_METHOD_EXCEPTION = "Error using function '{0}' on '{1}'.";

		public static string LOCALE_MAPPINGS_CONFIG = "localeMappings";
		public static string FILE_INFO_CONFIG = "filenameInfo";
		public static string EXPECTED_CONTENT_TYPE = "expectedContentType";
		public static string SDLTMS_REG_KEY = "sdltmsRegKey";
		public static string SDLTMS_REG_VALUE = "sdltmsRegValue";
	}
}
