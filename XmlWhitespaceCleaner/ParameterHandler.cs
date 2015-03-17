using System;
using System.IO;
using System.Xml;
using System.Collections;

namespace XmlNormalizer
{
    public class ParameterHandler
    {
        private string[] _args;
        private ParameterConfig _parameterConfig;

        public ParameterHandler(string[] args, ParameterConfig parameterConfig)
        {
            _args = args;
            _parameterConfig = parameterConfig;
        }

        public bool ValidateParameters()
        {
            if (_args.Length != _parameterConfig.Min && _args.Length != _parameterConfig.Max)
            {
                string message = _parameterConfig.Min == _parameterConfig.Max ? Convert.ToString(_parameterConfig.Min) : string.Format("{0} or {1}", _parameterConfig.Min, _parameterConfig.Max);
                new BrokerException(string.Format(Strings.INVALID_ARG_COUNT, message, _args.Length));
                return false;
            }

            for (int i = 0; i < _args.Length; ++i)
            {
                if (_args[i] == null || _args[i] == "")
                {
                    new BrokerException(string.Format(Strings.INVALID_ARG, Convert.ToString(i + 1)));
                    return false;
                }

                ParameterDescriptor parameterDescriptor = (ParameterDescriptor)_parameterConfig.Parameters[i];
                try
                {
                    Convert.ChangeType(_args[i], parameterDescriptor.Type);
                }
                catch
                {
                    new BrokerException(string.Format(Strings.INVALID_ARG_TYPE, Convert.ToString(i + 1), parameterDescriptor.Type.Name));
                    return false;
                }

                if (parameterDescriptor.IsFile)
                {
                    if (!File.Exists(_args[i]))
                    {
                        new BrokerException(string.Format(Strings.INVALID_FILE, _args[i]));
                        return false;
                    }
                }

            }

            return true;
        }

        public object GetArgumentValue(string argName)
        {
            int i = 0;

            for (i = 0; i < _args.Length; ++i)
            {
                ParameterDescriptor parameterDescriptor = (ParameterDescriptor)_parameterConfig.Parameters[i];

                if (parameterDescriptor.Name == argName)
                {
                    return GetArgumentValue(i);
                }
            }

            throw new Exception(string.Format(Strings.ARGUMENT_MISSING, argName));
        }

        public object GetArgumentValue(int argIndex)
        {
            object argumentValue = null;

            ParameterDescriptor parameterDescriptor = (ParameterDescriptor)_parameterConfig.Parameters[argIndex];

            try
            {
                argumentValue = Convert.ChangeType(_args[argIndex], parameterDescriptor.Type);
            }
            catch
            {
                throw new Exception(string.Format(Strings.INVALID_ARG_TYPE, parameterDescriptor.Type.Name));
            }

            if (argumentValue != null)
            {
                return argumentValue;
            }

            return null;
        }
    }

    internal class ParameterSectionConfigHandler : System.Configuration.IConfigurationSectionHandler
    {
        public object Create(object parent, object configContext, XmlNode section)
        {
            return ParameterConfig.CreateConfig(parent, configContext, section);
        }
    }

    public class ParameterConfig
    {
        private const string PARAMETERS = "parameters";
        private const string MIN = "min";
        private const string MAX = "max";

        private const string PARAMETER = "parameterDescriptor";
        private const string INDEX = "index";
        private const string TYPE = "type";
        private const string NAME = "name";
        private const string ISINPUTFILE = "isInputFile";
        private const string ISFILE = "isFile";

        private int _min;
        private int _max;

        private Hashtable _parameters;

        public ParameterConfig(object parent, object configContext, XmlNode node)
        {
            XmlElement parametersElement = (XmlElement)node.SelectSingleNode(PARAMETERS);
            _min = Convert.ToInt32(parametersElement.GetAttribute(MIN));
            _max = Convert.ToInt32(parametersElement.GetAttribute(MAX));

            _parameters = new Hashtable();
            XmlNodeList paramNodes = node.SelectNodes(string.Format("//{0}", PARAMETER));

            for (int i = 0; i < paramNodes.Count; i++)
            {
                XmlElement paramElement = (XmlElement)paramNodes[i];

                int index = Convert.ToInt32(paramElement.GetAttribute(INDEX));
                Type type = Type.GetType(paramElement.GetAttribute(TYPE));
                string name = paramElement.GetAttribute(NAME);
                bool isInputFile = GetFileBool(paramElement, ISINPUTFILE);

                bool isFile = isInputFile ? true : GetFileBool(paramElement, ISFILE);

                _parameters[index] = new ParameterDescriptor(index, type, name, isInputFile, isFile);
            }
        }

        public int Min
        {
            get { return _min; }
        }

        public int Max
        {
            get { return _max; }
        }

        public Hashtable Parameters
        {
            get { return _parameters; }
        }

        internal static object CreateConfig(object parent, object configContext, XmlNode section)
        {
            return new ParameterConfig(parent, configContext, section);
        }

        private bool GetFileBool(XmlElement paramElement, string boolAttribute)
        {
            string boolValue = paramElement.GetAttribute(boolAttribute);

            bool isFile = false;

            if (boolValue != "")
            {
                try
                {
                    isFile = Convert.ToBoolean(boolValue);
                }
                catch
                {
                }
            }

            return isFile;
        }

    }

    internal class GeneralSectionConfigHandler : System.Configuration.IConfigurationSectionHandler
    {
        public object Create(object parent, object configContext, XmlNode section)
        {
            return ParameterConfig.CreateConfig(parent, configContext, section);
        }
    }

    public class ParameterDescriptor
    {
        public int Index;
        public Type Type;
        public string Name;
        public bool IsInputFile;
        public bool IsFile;

        public ParameterDescriptor(int index, Type type, string name, bool isInputFile, bool isFile)
        {
            Index = index;
            Type = type;
            Name = name;
            IsInputFile = isInputFile;
            IsFile = isFile;
        }
    }
}
