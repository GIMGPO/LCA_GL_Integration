using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using XmlTransformation;


namespace XmlManipulator
{
    public class Processor
    {
        private static string UDPATE_CONFIG_NAME = "updateInfo";
        private static string ENTITY_CONFIG_NAME = "entityInfo";
        //private static string TASK_ID = "taskId";
        private static string INPUT_FILE = "inputFile";
        private static string TARGET_LOCALE = "tmsTargetLocale";
        private static string USAGE = "usage";
        private static string RUN_MODE = "runMode";
        private static char SEPARATOR = '|';
        private static string LANG_INFO_SECTION = "languageInfo";
        private static string SCOPE_INFO_SECTION = "scopeInfo";
		private static string OTHER_MODE_IGNORE_BACKWARD = "ignoreBackward";
		private static string OTHER_MODE_IGNORE_FORWARD = "ignoreForward";

        private Initialiser _initialiser;

        private UpdateInfoConfig _updateConfig;

        //private int _taskId;
        private string _inputFile;
        private XmlDocument _doc;
        private string[] _usage;
        private string _usageList = null;
        private RunMode _runMode;
        private string _targetLocale;
        private Hashtable _htmlEntities;
        private NamespaceManagerHelper _nsmh;
        private LanguageInfoConfig _langConfig;
        private ScopeInfoConfig _scopeConfig;
        private Hashtable _values;
        private FileTypeUpdate _fileType;
        private string _entityConversionMode; //added by Kiet Jun 01, 09
        private bool _otherEntitiesEscaped;
        private Encoding _inputFileEncoding;
		private string _otherEntityConversionMode;

        private Transform _transform;

        private ITransform _transformImplementation;

        public Processor(Initialiser initialiser)
        {
            try
            {
                Initialise(initialiser);
            }
            catch (Exception e)
            {
                new BrokerException(e.Message);
                throw;
            }
        }

        public bool Run()
        {
            try
            {
                if (Xml() && CanRun() && HasEligibleExtension())
                {
                    bool hasRun = false;

                    // new: use entity manager to resolve character and predefined entities.
                    ResolveHtmlAndCharacterEntities();

                    EscapeOtherEntities();

                    LoadDocument();

                    foreach (string usage in _usage)
                    {
                        if (Eligible(usage) && InScope(usage))
                        {
                            GetImplementation();

                            bool result = Do();

                            hasRun = true;
                        }
                    }

                    if (hasRun)
                    {
                        SaveDocument();

                        //ConcludeRun();
                    }

                    RestoreOtherEntities();

                    return true;
                }

                return true;
            }
            catch (Exception e)
            {
                new BrokerException(e.Message);

                return false;
            }
        }

        private void GetImplementation()
        {
            object[] args = new object[] { _doc, _transform, _runMode, _htmlEntities, _nsmh, _initialiser.ConfigDoc, _values };

            Type implementationType = GetImplementationType();

            _transformImplementation = null;

            try
            {
                _transformImplementation = (ITransform)Activator.CreateInstance(implementationType, args);
            }
            catch (Exception)
            {
                _transformImplementation = (ITransform)Activator.CreateInstance(typeof(XmlTransformation.TransformBase), args);
            }

            foreach (IConfig config in _initialiser.AdditionalConfigs.Values)
            {
                if (config.Name != ResStrings.UPDATE_INFO_CONFIG)
                {
                    _transformImplementation.AddConfigXml(config.Name, config.ConfigXml);
                }
            }
        }

        private bool Do()
        {
            return _transformImplementation.Do();
        }

        private void SaveDocument()
        {
            _doc.PreserveWhitespace = _fileType.Whitespaces == Whitespaces.Preserve ? true : false;

            //XmlTextWriter w = new XmlTextWriter(_inputFile);

            _doc.Save(_inputFile);
        }

        private void RestoreOtherEntities()
        {
            if (_otherEntitiesEscaped)
            {
                string workFile = _inputFile + ".wrk";

                try
                {
                    File.Copy(_inputFile, workFile);

                    EntityManager entityManager = new EntityManager(workFile, _inputFileEncoding, _htmlEntities);

                    entityManager.RestoreEntities();

                    if (entityManager.HasOtherEntityChanged)
                    {
                        File.Delete(_inputFile);
                        File.Move(workFile, _inputFile);
                    }
                    else
                    {
                        File.Delete(workFile);
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        private void ResolveHtmlAndCharacterEntities()
        {
            if (_entityConversionMode.IndexOf(_runMode.ToString()) >= 0)
            {
                string workFile = _inputFile + ".wrk";

                try
                {
                    File.Copy(_inputFile, workFile);

                    EntityManager entityManager = new EntityManager(workFile, _inputFileEncoding, _htmlEntities);

                    bool htmlCharAndEntitiesEscaped = entityManager.ConvertHtmlCharacterEntities(ConvertTo.NCR);

                    if (htmlCharAndEntitiesEscaped)
                    {
                        File.Delete(_inputFile);

                        File.Move(workFile, _inputFile);
                    }
                    else
                    {
                        File.Delete(workFile);
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        private void EscapeOtherEntities()
        {
			if (ConvertOtherEntities())
			{
				string workFile = _inputFile + ".wrk";

				try
				{
					File.Copy(_inputFile, workFile);

					Hashtable entitiesToPass = (_entityConversionMode.IndexOf(_runMode.ToString()) >= 0) ? _htmlEntities : null;

					EntityManager entityManager = new EntityManager(workFile, _inputFileEncoding, entitiesToPass);

					_otherEntitiesEscaped = entityManager.EscapeEntities();

					if (_otherEntitiesEscaped)
					{
						File.Delete(_inputFile);

						File.Move(workFile, _inputFile);
					}
					else
					{
						File.Delete(workFile);
					}
				}
				catch (Exception)
				{
				}
			}
        }

		private bool ConvertOtherEntities()
		{
			// if entityInfo element is missing @otherEntityMode, or @otherEntityMode="", perform the change
			if (String.IsNullOrEmpty(_otherEntityConversionMode))
			{
				return true;
			}

			// if this is a forward convert, perform the change unless @otherEntityMode contains "ignoreForward"
			if (_runMode == RunMode.forward || _runMode == RunMode.single || _runMode == RunMode.repeat)
			{
				return !_otherEntityConversionMode.ToLower().Contains(OTHER_MODE_IGNORE_FORWARD.ToLower());
			}
			else // otherwise, perform the change unless @otherEntityMode contains "ignoreBackward"
			{
				return !_otherEntityConversionMode.ToLower().Contains(OTHER_MODE_IGNORE_BACKWARD.ToLower());
			}
		}

        private Type GetImplementationType()
        {
            Assembly assembly = GetExecutingAssembly();

            if (assembly != null)
            {
                Type[] derivedTypes = assembly.GetTypes();

                foreach (Type derivedType in derivedTypes)
                {
                    if (derivedType.FullName == _transform.Type)
                    {
                        return derivedType;
                    }
                }
            }

            return null;
        }

        private Assembly GetExecutingAssembly()
        {
            try
            {
                Assembly thisAssembly = Assembly.GetExecutingAssembly();

                string thisAssemblyPath = thisAssembly.Location;

                string executionPath = Path.GetDirectoryName(thisAssemblyPath);

                string assemblyName = _transform.Assembly;

                if (!assemblyName.ToLower().EndsWith(".dll"))
                {
                    assemblyName = string.Format("{0}.dll", assemblyName);
                }

                string requiredAssemblyPath = Path.Combine(executionPath, assemblyName);

                return (string.Compare(requiredAssemblyPath, thisAssemblyPath, true) == 0) ? thisAssembly : Assembly.LoadFile(requiredAssemblyPath);
            }
            catch
            {
                return null;
            }
        }

        private bool Eligible(string thisUsage)
        {
            string filename = Path.GetFileName(_inputFile);

            string extension = Path.GetExtension(filename);

            extension = extension.ToLower();

            extension = extension != "" && extension.LastIndexOf('.') > -1 ? extension.Substring(extension.LastIndexOf('.') + 1) : "";

            foreach (FileTypeUpdate fileType in _updateConfig.FileTypes.Values)
            {
                if (fileType.Filter.Contains(extension) && _nsmh.GetNodes(fileType.Type).Count > 0)
                {
                    foreach (Transform transform in fileType.Transforms.Values)
                    {
                        if (string.Compare(transform.Usage, thisUsage, true) == 0)
                        {
                            _fileType = fileType;

                            _transform = transform;

                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private bool InScope(string thisUsage)
        {
            if (_scopeConfig != null)
            {
                if (_scopeConfig.ScopeCollection[_targetLocale] != null)
                {
                    ScopeInfo scopeInfo = (ScopeInfo)_scopeConfig.ScopeCollection[_targetLocale];

                    if (scopeInfo.Usages != null)
                    {
                        return scopeInfo.Usages.Contains(thisUsage);
                    }
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        private string GetClientTargetLocale(string sdlLocale)
        {
            if (_langConfig != null)
            {
                Hashtable languages = _langConfig.Languages;

                if (languages[sdlLocale] != null)
                {
                    LanguageInfo languageInfo = (LanguageInfo)languages[sdlLocale];

                    return languageInfo.ClientLocale != null ? languageInfo.ClientLocale : sdlLocale.ToLower();
                }
            }

            return sdlLocale.ToLower();
        }

        private void Initialise(Initialiser initialiser)
        {
            _initialiser = initialiser;

            _updateConfig = (UpdateInfoConfig)_initialiser.AdditionalConfigs[UDPATE_CONFIG_NAME];

            //_taskId = (int)_initialiser.GetArgumentValue(TASK_ID);
            _inputFile = Convert.ToString(_initialiser.GetArgumentValue(INPUT_FILE));
            _targetLocale = Convert.ToString(_initialiser.GetArgumentValue(TARGET_LOCALE));
            _usageList = (string)_initialiser.GetArgumentValue(USAGE); //(Usage)Enum.Parse(typeof(Usage), Convert.ToString(_initialiser.GetArgumentValue(USAGE)));
            _usage = _usageList.Split(SEPARATOR);
            _runMode = (RunMode)Enum.Parse(typeof(RunMode), Convert.ToString(_initialiser.GetArgumentValue(RUN_MODE)));

            _htmlEntities = ((EntityInfoConfig)_initialiser.AdditionalConfigs[ENTITY_CONFIG_NAME]).Entities;
            //added by Kiet Jun 01, 09
            _entityConversionMode = ((EntityInfoConfig)_initialiser.AdditionalConfigs[ENTITY_CONFIG_NAME]).RunMode;

			//added by Eric, Oct 15, 2013
			_otherEntityConversionMode = ((EntityInfoConfig)_initialiser.AdditionalConfigs[ENTITY_CONFIG_NAME]).OtherEntityMode;

            _langConfig = ((LanguageInfoConfig)_initialiser.AdditionalConfigs[LANG_INFO_SECTION]);

            _scopeConfig = ((ScopeInfoConfig)_initialiser.AdditionalConfigs[SCOPE_INFO_SECTION]);

            _values = new Hashtable();

            //_values[TASK_ID] = _taskId;
           _values[INPUT_FILE] = _inputFile;
            _values[TARGET_LOCALE] = _targetLocale;
            _values[USAGE] = _usageList;
            _values[RUN_MODE] = _runMode;

           // _values[INPUT_FILE] = "C:\\Users\\vijayr3\\Documents\\XMLTransformation\\BEFORE_GUID-E2F778B5-F756-42BE-A415-7AC4DFE7B4D6.xml";
           // _values[TARGET_LOCALE] = "JA";
           //_values[USAGE] = "add|add_text|copy_term|rename_term|update_ph";
           //_values[RUN_MODE] = "forward"; 


            //Kiet -- April 19, 2011
            //Added for XSLT Transformation
            for (int i = 6; i < _initialiser.ParameterConfiguration.Parameters.Count; i++)
            {
                string paramName = ((ParameterDescriptor)_initialiser.ParameterConfiguration.Parameters[i]).Name;
                string paramValue = (string)initialiser.Arguments[paramName];
                _values[paramName] = paramValue;
            }

            _otherEntitiesEscaped = false;
        }

        private void LoadDocument()
        {
            _doc = new XmlDocument();

            _doc.XmlResolver = null;

            _doc.PreserveWhitespace = true;

            //XmlSpace[None|Default|Preserve]
            //WhitespaceHandling[All|None|Significant]
            //Formatting[Indented|None]

            _doc.Load(_inputFile);

            _nsmh = new NamespaceManagerHelper(_doc);
        }

        private bool Xml()
        {
            try
            {
                using (XmlTextReader xr = new XmlTextReader(_inputFile))
                {
                    xr.XmlResolver = null;

                    xr.WhitespaceHandling = WhitespaceHandling.All;

                    xr.Read();

                    _inputFileEncoding = xr.Encoding;

                    return true;
                }
            }
            catch
            {
                _inputFileEncoding = Encoding.UTF8;
            }

            using (StreamReader sr = new StreamReader(_inputFile, _inputFileEncoding))
            {
                string line = sr.ReadLine();

                if (line == null)
                {
                    return false;
                }

                Regex r = new Regex(@"\<\?xml[^\?\>]+\?\>", RegexOptions.IgnoreCase);

                return r.IsMatch(line);
            }
        }

        private bool CanRun()
        {
            //Added by Kiet - Dec 06, 09
            // | _runMode == RunMode.repeat
            if (_runMode == RunMode.backward || _runMode == RunMode.repeat)
            {
                return true;
            }

            return !ExistsFlagFile();
        }

        private void ConcludeRun()
        {
            if (_runMode != RunMode.backward)
            {
                if (!ExistsFlagFile())
                {
                    using (StreamWriter sw = new StreamWriter(GetFlagFileName(), false, Encoding.UTF8))
                    {
                        sw.WriteLine("");
                    }
                }
            }
        }

        private bool ExistsFlagFile()
        {
            return File.Exists(GetFlagFileName());
        }

        private string GetFlagFileName()
        {
            //return string.Format("{0}{1}_{2}", _inputFile, FLAG_EXTENSION, _usageList.Replace(SEPARATOR, '-'));
            string folder = Path.GetDirectoryName(_inputFile);
            string ext = Path.GetExtension(_inputFile);
            string name = string.Format("{0}", _usageList.Replace(SEPARATOR, '-'));
            return Path.Combine(folder, name);
        }


        private void Write(string line)
        {
            //			System.Text.RegularExpressions.Match m = Regex.Match(_inputFile, @"\\\d+\\");
            //
            //			string jobFolder = m.Success ? _inputFile.Substring(0, m.Index + m.Length) : @"C:\temp\DITA tests\";
            //
            //			if (! Directory.Exists(jobFolder))
            //			{
            //				Directory.CreateDirectory(jobFolder);
            //			}
            //
            //			string fileName = _taskId + ".log";
            //
            //			string file = Path.Combine(jobFolder, fileName);
            //
            //			using (StreamWriter sw = new StreamWriter(file, true, Encoding.UTF8))
            //			{
            //				sw.WriteLine(line);
            //			}
        }

        private bool HasEligibleExtension()
        {
            string filename = Path.GetFileName(_inputFile);

            string extension = Path.GetExtension(filename);

            extension = extension.ToLower();

            extension = extension != "" && extension.LastIndexOf('.') > -1 ? extension.Substring(extension.LastIndexOf('.') + 1) : "";

            foreach (FileTypeUpdate fileType in _updateConfig.FileTypes.Values)
            {
                if (fileType.Filter.Contains(extension))
                {
                    return true;
                }
            }

            return false;
        }

    }
}
