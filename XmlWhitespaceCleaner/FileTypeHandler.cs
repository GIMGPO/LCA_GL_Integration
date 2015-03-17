using System;
using System.Xml;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;

namespace XmlNormalizer
{
    public enum LbStyle { unix, mac, windows }
    public enum LbLocation { leading, trailing, both }
    public enum Tag { sCI, sOI, sCE, sOE, sM, unknown } 
    public enum Usage { forward, backward, single }

	public class FileTypeConfig : IConfig
	{
		private const string FILE_TYPE_ELT = "fileType";
        private string _name;
		private Hashtable _fileTypes;
		private XmlElement _configXml;

		public FileTypeConfig(object parent, object configContext, XmlNode node)
		{
            _name = ((XmlElement)node).Name;

			_configXml =  (XmlElement)node;

			_fileTypes = new Hashtable();

			XmlNodeList fileTypeElements = node.SelectNodes(FILE_TYPE_ELT);

			foreach (XmlNode fileTypeElement in fileTypeElements)
			{
				FileType fileType = new FileType((XmlElement)fileTypeElement);

                _fileTypes[fileType.Index] = fileType;
			}
		}

        public string Name
        {
            get { return _name; } 
        }

		public XmlElement ConfigXml
		{
			get { return _configXml; }
		}

		public Hashtable FileTypes
		{
			get { return _fileTypes; }
		}

		internal static object CreateConfig(object parent, object configContext, XmlNode section)
		{
			return new FileTypeConfig(parent, configContext, section);
		}
	}

    public class FileType
    {
        private const string INDEX_ATT = "index";
        private const string FILTER_ATT = "filter";
        private const string TEXT_REGEX_ELT = "textNodeWhitespace";
        private const string REGEX_ATT = "regex";
		private const string ESCAPE_ATT = "escape";
        private const string REPLACE_ATT = "replace";
        private const string PRESERVE_ELEMENTS_ELT = "xmlSpacePreserveScope";

        private const string COSMETIC_LB_ELT = "cosmeticLinebreaks";
        private const string LB_TYPE = "type";
        private const string LB_LOCATION = "location";

        private const string INLINE_TAGGING_ELT = "inlineTagging";

        public int Index;
        public ArrayList Filter;
        public ArrayList DocTypes;
        public ArrayList XmlSpacePreserveScope;
        public bool OmitUtf8BOM;
        public Regex WhiteSpaceRegex;
        public string WhiteSpaceReplace;
        
        public bool HasCosmeticLinebreaks;
        public ArrayList CosmeticLinebreakElements;
        public string CosmeticLinebreak;
        public LbLocation Location;

        public bool HasInlineTagging;
        public Regex InlineTaggingRegex;
		public bool HasEscapes;
		public string Escapes;
 

        public FileType(XmlElement fileTypeElement)
        {
            Index = Convert.ToInt32(fileTypeElement.GetAttribute(INDEX_ATT));

            Filter = new ArrayList();

            string filterValue = Convert.ToString(fileTypeElement.GetAttribute(FILTER_ATT)).ToLower();

            Filter.AddRange(filterValue.Split('|'));

            if (String.IsNullOrEmpty(fileTypeElement.GetAttribute(Strings.OMIT_UTF8_BOM)))
            {
                OmitUtf8BOM = false;
            }
            else 
            {
                try
                {
                    OmitUtf8BOM = Convert.ToBoolean(fileTypeElement.GetAttribute(Strings.OMIT_UTF8_BOM));
                }
                catch 
                {
                    OmitUtf8BOM = false;
                }
            }

            DocTypes = new ArrayList();

            try
            {
                DocTypes.AddRange(fileTypeElement.GetAttribute(Strings.DOC_TYPES).Split('|'));
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("The list of supported document types is not mentioned correctly on <fileType> #{0}. Reason:\n{1}", Index, e.Message));
            }

            XmlNode xmlSpacePreserveScopeNode = fileTypeElement.SelectSingleNode(PRESERVE_ELEMENTS_ELT);

            if (xmlSpacePreserveScopeNode == null)
            {
                throw new Exception(string.Format("The Xml Space Preserve Scope is not mentioned correctly on <fileType> #{0}.", Index));
            }

            XmlSpacePreserveScope = new ArrayList();

            XmlElement xmlSpacePreserveScopeElement = (XmlElement)xmlSpacePreserveScopeNode;

            string xmlSpacePreserveScopeElementlist = xmlSpacePreserveScopeElement.InnerText;

            XmlSpacePreserveScope.AddRange(xmlSpacePreserveScopeElementlist.Split( '|' ));

            XmlNode regexNode = fileTypeElement.SelectSingleNode(TEXT_REGEX_ELT);

            if (regexNode == null)
            {
                WhiteSpaceRegex = new Regex(@"\s+", RegexOptions.Compiled);
                
                WhiteSpaceReplace = " ";
            }
            else
            {
                XmlElement regexElement = (XmlElement)regexNode;

                string regexAtt = regexElement.GetAttribute(REGEX_ATT);

                if (String.IsNullOrEmpty(regexAtt))
                {
                    throw new Exception(string.Format("Initialisation error: Attribute {0} on element {1} is null or empty.", REGEX_ATT, regexElement.Name));
                }

                string replaceAtt = regexElement.GetAttribute(REPLACE_ATT);

                if (String.IsNullOrEmpty(replaceAtt))
                {
                    throw new Exception(string.Format("Initialisation error: Attribute {0} on element {1} is null or empty.", REPLACE_ATT, regexElement.Name));
                }

                try
                {
                    WhiteSpaceRegex = new Regex(regexAtt, RegexOptions.Compiled);
                    
                    WhiteSpaceReplace = replaceAtt;
                }
                catch (Exception e)
                {
                    throw new Exception(string.Format("Initialisation error: string '{0}' is not a valid regex pattern. Error message\n{1}", regexAtt, e.Message));
                }
            }

            XmlNode cosmeticLinebreaksNode = fileTypeElement.SelectSingleNode(COSMETIC_LB_ELT);

            HasCosmeticLinebreaks = cosmeticLinebreaksNode != null;

            if (HasCosmeticLinebreaks)
            {
                try
                {
                    XmlElement cosmeticLinebreakElement = (XmlElement)cosmeticLinebreaksNode;

                    CosmeticLinebreakElements = new ArrayList();

                    CosmeticLinebreakElements.AddRange(cosmeticLinebreakElement.InnerText.Split('|'));

                    LbStyle lbStyle = (LbStyle)Enum.Parse(typeof(LbStyle), cosmeticLinebreakElement.GetAttribute(LB_TYPE));

                    switch (lbStyle)
                    {
                        case LbStyle.unix:
                            CosmeticLinebreak = "\n";
                            break;
                        
                        case LbStyle.mac:
                            CosmeticLinebreak = "\r";
                            break;

                        case LbStyle.windows:
                        default:
                            CosmeticLinebreak = "\r\n";
                            break;
                    }

                    Location = (LbLocation)Enum.Parse(typeof(LbLocation), cosmeticLinebreakElement.GetAttribute(LB_LOCATION));
                }
                catch 
                {
                    HasCosmeticLinebreaks = false;
                }
            }

            XmlNode inlineTaggingNode = fileTypeElement.SelectSingleNode(INLINE_TAGGING_ELT);

            HasInlineTagging = inlineTaggingNode != null;

			if (HasInlineTagging)
			{
				XmlElement inlineTaggingElement = (XmlElement)inlineTaggingNode;

				string regexAtt = inlineTaggingElement.GetAttribute(REGEX_ATT);

				try
				{
					InlineTaggingRegex = new Regex(regexAtt, RegexOptions.Compiled);
				}
				catch (Exception e)
				{
					throw new Exception(string.Format("Initialisation error: string '{0}' is not a valid regex pattern. Error message\n{1}", regexAtt, e.Message));
				}

				string escapeAtt = inlineTaggingElement.GetAttribute(ESCAPE_ATT);

				HasEscapes = !String.IsNullOrEmpty(escapeAtt);

				if (HasEscapes)
				{
					Escapes = escapeAtt;
				}
			}
			else 
			{
				Escapes = String.Empty;

				HasEscapes = false;
			}
        }
    }

	internal class FileTypeSectionConfigHandler : System.Configuration.IConfigurationSectionHandler
	{
		public object Create(object parent, object configContext, XmlNode section)
		{
			return FileTypeConfig.CreateConfig(parent, configContext, section);
		}
	}
}
