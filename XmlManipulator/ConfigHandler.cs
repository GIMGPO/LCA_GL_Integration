using System;
using System.Collections;
using System.Configuration;
using System.Xml;
using XmlTransformation;

namespace XmlManipulator
{
    public interface IConfig
    {
        string Name { get; }
        XmlElement ConfigXml { get; }
    }


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
                new BrokerException(string.Format(ResStrings.CONFIGURATION_INVALID, e.Message));
                return null;
            }
        }

    }


    internal class EntityInfoSectionConfigHandler : System.Configuration.IConfigurationSectionHandler
    {
        public object Create(object parent, object configContext, XmlNode section)
        {
            return EntityInfoConfig.CreateConfig(parent, configContext, section);
        }
    }


    // Updated by Kiet -- Jun 01,09
    public class EntityInfoConfig : IConfig
    {
        private static string ENTITY_ELT = "entity";
        private static string HTML_ATT = "html";
        private static string CODEPOINT_ATT = "codePoint";
        private static string RUNMODE_ATT = "runMode";  //added by Kiet Jun 01, 09
        private static string FORWARD_MODE = "forward";  //added by Kiet Jun 01, 09
		private static string OTHER_ENTITY_MODE = "otherEntityMode"; //added by Eric, Oct 15, 2013

        private string _name;
        private string _runMode;
        private XmlElement _configXml;
		private string _otherEntityMode;

        public Hashtable Entities;

        public EntityInfoConfig(object parent, object configContext, XmlNode node)
        {
            _name = node.LocalName;

            _configXml = (XmlElement)node;

            //added by Kiet Jun 01, 09
            _runMode = FORWARD_MODE;

            if (_configXml.HasAttribute(RUNMODE_ATT))
            {
                if (!_configXml.Attributes[RUNMODE_ATT].Value.Equals(string.Empty))
                {
                    _runMode = _configXml.Attributes[RUNMODE_ATT].Value;
                }
            }
            //End added by Kiet Jun 01, 09


			//added by Eric, Oct 15, 2013
			_otherEntityMode = string.Empty;

			if (_configXml.HasAttribute(OTHER_ENTITY_MODE))
			{
				if (!_configXml.Attributes[OTHER_ENTITY_MODE].Value.Equals(string.Empty))
				{
					_otherEntityMode = _configXml.Attributes[OTHER_ENTITY_MODE].Value;
				}
			}
			//END added by Eric, Oct 15, 2013

            Entities = new Hashtable();

            XmlNodeList entityNodes = node.SelectNodes(ENTITY_ELT);

            foreach (XmlElement entityElement in entityNodes)
            {
                string html = entityElement.Attributes[HTML_ATT].Value;

                int codePoint = Convert.ToInt32(entityElement.Attributes[CODEPOINT_ATT].Value);

                Entities[html] = codePoint;
            }
        }

        internal static object CreateConfig(object parent, object configContext, XmlNode section)
        {
            return new EntityInfoConfig(parent, configContext, section);
        }

        public string Name
        {
            get { return _name; }
        }

        //added by Kiet Jun 01, 09
        public string RunMode
        {
            get { return _runMode; }
        }

		//added by Eric, Oct 15, 2013
		public string OtherEntityMode
		{
			get { return _otherEntityMode; }
		}

        public XmlElement ConfigXml
        {
            get { return _configXml; }
        }
    }


    internal class UpdateInfoSectionConfigHandler : System.Configuration.IConfigurationSectionHandler
    {
        public object Create(object parent, object configContext, XmlNode section)
        {
            return UpdateInfoConfig.CreateConfig(parent, configContext, section);
        }
    }


    public class UpdateInfoConfig : IConfig
    {
        private static string FILETYPE_ELT = "fileType";

        public Hashtable FileTypes;

        private string _name;
        private XmlElement _configXml;

        public UpdateInfoConfig(object parent, object configContext, XmlNode node)
        {
            _name = node.LocalName;

            _configXml = (XmlElement)node;

            FileTypes = new Hashtable();

            int fileTypeIndex = 0;

            XmlNodeList fileTypeNodes = node.SelectNodes(FILETYPE_ELT);

            foreach (XmlElement fileTypeElement in fileTypeNodes)
            {
                FileTypeUpdate fileTypeUpdate = new FileTypeUpdate(fileTypeElement);

                FileTypes[fileTypeIndex] = fileTypeUpdate;

                fileTypeIndex++;
            }
        }

        internal static object CreateConfig(object parent, object configContext, XmlNode section)
        {
            return new UpdateInfoConfig(parent, configContext, section);
        }

        public string Name
        {
            get { return _name; }
        }

        public XmlElement ConfigXml
        {
            get { return _configXml; }
        }
    }


    internal class ScopeInfoSectionConfigHandler : System.Configuration.IConfigurationSectionHandler
    {
        public object Create(object parent, object configContext, XmlNode section)
        {
            return ScopeInfoConfig.CreateConfig(parent, configContext, section);
        }
    }

    internal class ExpressionInfoSectionConfigHandler : System.Configuration.IConfigurationSectionHandler
    {
        public object Create(object parent, object configContext, XmlNode section)
        {
            return ExpressionInfoConfig.CreateConfig(parent, configContext, section);
        }
    }

    public class ExpressionInfoConfig : IConfig
    {
        private string _name;
        private XmlElement _configXml;

        public ExpressionInfoConfig(object parent, object configContext, XmlNode node)
        {
            _name = node.LocalName;

            _configXml = (XmlElement)node;
        }

        internal static object CreateConfig(object parent, object configContext, XmlNode section)
        {
            return new ExpressionInfoConfig(parent, configContext, section);
        }

        public string Name
        {
            get { return _name; }
        }

        public XmlElement ConfigXml
        {
            get { return _configXml; }
        }
    }



    public class ScopeInfoConfig : IConfig
    {
        private static string SCOPE_ELT = "language";

        public Hashtable ScopeCollection;

        private string _name;
        private XmlElement _configXml;

        public ScopeInfoConfig(object parent, object configContext, XmlNode node)
        {
            _name = node.LocalName;

            _configXml = (XmlElement)node;

            ScopeCollection = new Hashtable();

            int scopeIndex = 0;

            XmlNodeList scopeIndexNodes = node.SelectNodes(SCOPE_ELT);

            foreach (XmlElement scopeElement in scopeIndexNodes)
            {
                ScopeInfo scopeInfo = new ScopeInfo(scopeElement);

                ScopeCollection[scopeInfo.SdlLocale] = scopeInfo;

                scopeIndex++;
            }
        }

        internal static object CreateConfig(object parent, object configContext, XmlNode section)
        {
            return new ScopeInfoConfig(parent, configContext, section);
        }

        public string Name
        {
            get { return _name; }
        }

        public XmlElement ConfigXml
        {
            get { return _configXml; }
        }
    }


    public class ScopeInfo
    {
        private static string TMS_ATT = "tms";
        private static string SCOPE_ATT = "scope";

        public string SdlLocale;
        public ArrayList Usages;

        public ScopeInfo(XmlElement scopeElement)
        {
            SdlLocale = XmlHelper.GetAttValue(scopeElement, TMS_ATT);

            string scopeString = XmlHelper.GetAttValue(scopeElement, SCOPE_ATT);

            if (scopeString != null && scopeString != "")
            {
                Usages = new ArrayList();

                Usages.InsertRange(0, scopeString.Split('|'));
            }
        }
    }

    internal class LanguageInfoSectionConfigHandler : System.Configuration.IConfigurationSectionHandler
    {
        public object Create(object parent, object configContext, XmlNode section)
        {
            return LanguageInfoConfig.CreateConfig(parent, configContext, section);
        }
    }


    public class LanguageInfoConfig : IConfig
    {
        private static string LANGUAGE_ELT = "language";

        public Hashtable Languages;

        private string _name;
        private XmlElement _configXml;

        public LanguageInfoConfig(object parent, object configContext, XmlNode node)
        {
            _name = node.LocalName;

            _configXml = (XmlElement)node;

            Languages = new Hashtable();

            int languageIndex = 0;

            XmlNodeList languageIndexNodes = node.SelectNodes(LANGUAGE_ELT);

            foreach (XmlElement languageElement in languageIndexNodes)
            {
                LanguageInfo languageInfo = new LanguageInfo(languageElement);

                Languages[languageInfo.SdlLocale] = languageInfo;

                languageIndex++;
            }
        }

        internal static object CreateConfig(object parent, object configContext, XmlNode section)
        {
            return new LanguageInfoConfig(parent, configContext, section);
        }

        public string Name
        {
            get { return _name; }
        }

        public XmlElement ConfigXml
        {
            get { return _configXml; }
        }
    }


    public class LanguageInfo
    {
        private static string TMS_ATT = "tms";
        private static string CLIENT_ATT = "client";

        public string SdlLocale;
        public string ClientLocale;

        public LanguageInfo(XmlElement languageElement)
        {
            SdlLocale = XmlHelper.GetAttValue(languageElement, TMS_ATT);

            ClientLocale = XmlHelper.GetAttValue(languageElement, CLIENT_ATT);

        }
    }

    /*
        * private static string SCOPE_ATT = "scope";
        public ArrayList FileTypes;
            string scopeString = XmlHelper.GetAttValue(languageElement, SCOPE_ATT);
	
            if (scopeString != null && scopeString != "")
            {
                FileTypes = new ArrayList();
		
                FileTypes.InsertRange(0, scopeString.Split('|'));
            }
        * 
    */
}
