using System;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Collections;
using System.Reflection;
using System.Xml;


namespace XmlManipulator
{
	public class Initialiser
	{
		public static string CONFIG_FILE_PARAM = "configFile";

		private string[] _args;
		
		private ArrayList _additionalConfigNames;
		private ParameterConfig _parameterConfig;
		private bool _useConfigFile;
		private Hashtable _additionalConfigs;
		private Hashtable _arguments;
		private ConfigHandler _configHandler;
		private XmlDocument _configDoc;
		private string _configFile;

		public Initialiser(string[] args)
		{
			_args = args;
			_configFile = ConfigFile.SetConfigFile(_args);
		}

		public bool Run()
		{
			try
			{
				Initialise();

				if (! GetArguments() || ! GetAdditionalConfigs())
				{
					return false;
				}

				return true;
			}
			catch (Exception e)
			{
				new BrokerException(string.Format(ResStrings.INIT_EXCEPTION, e.Message));

				return false;
			}
		}

		public Hashtable Arguments
		{
			get { return _arguments; }
		}

		public object GetArgumentValue(string argumentName)
		{
			return _arguments[argumentName];
		}

		public ParameterConfig ParameterConfiguration
		{
			get { return _parameterConfig; }
		}

		public Hashtable AdditionalConfigs
		{
			get { return _additionalConfigs; }
		}

		public XmlDocument ConfigDoc
		{
			get { return _configDoc; }  
		}

		private bool GetArguments()
		{
			_arguments = new Hashtable();

			if (_useConfigFile)
			{
				_parameterConfig = (ParameterConfig)_configHandler.GetConfig(ResStrings.PARAMETER_INFO_CONFIG);

				ParameterHandler parameterHandler = new ParameterHandler(_args, _parameterConfig);

				if (! parameterHandler.ValidateParameters())
				{
					return false;
				}

				foreach (ParameterDescriptor parameterDescriptor in _parameterConfig.Parameters.Values)
				{
					if (parameterDescriptor.Name != CONFIG_FILE_PARAM)
					{
						object value = parameterHandler.GetArgumentValue(parameterDescriptor.Name);

						if (value != null)
						{
							_arguments[parameterDescriptor.Name] = value;
						}
					}
				}
			}
			else
			{
				for (int i = 0; i < _args.Length; ++i)
				{
					_arguments[i] = _args[i];
				}
			}

			return true;
		}

		private bool GetAdditionalConfigs()
		{
			_additionalConfigs = new Hashtable();

			foreach (string additionalConfig in _additionalConfigNames)
			{
				_additionalConfigs[additionalConfig] = _configHandler.GetConfig(additionalConfig);
			}

			return true;
		}

		private void Initialise()
		{
			_configHandler = new ConfigHandler();

			_additionalConfigNames = new ArrayList();

			//string configFile = string.Format("{0}.config", System.Reflection.Assembly.GetExecutingAssembly().Location);

			_useConfigFile = File.Exists(_configFile);
			
			if (_useConfigFile)
			{
				_configDoc = new XmlDocument();

				try
				{
					_configDoc.Load(_configFile);

					XmlNodeList configNodes = _configDoc.SelectNodes("configuration/configSections/section");

					for (int i = 0; i < configNodes.Count; ++i)
					{
						XmlElement configElement = (XmlElement)configNodes[i];

						string configName = configElement.GetAttribute("name");

						if (configName != ResStrings.PARAMETER_INFO_CONFIG)
						{
							_additionalConfigNames.Add(configName);
						}
					}
				}
				catch
				{	
					throw;
				}
			}
		}

	}
}
