using System;
using System.Text.RegularExpressions;
using System.Collections;
using System.Xml;
using System.Xml.XPath;

namespace XmlTransformation
{
	public class Update : TransformBase
	{
		internal enum ExtraMethod { text, xpath, regex, unknown }
        internal enum Location { replace, append, prepend, insert, unknown }
        internal enum ConditionType { none, xpath, regex }

		private const string NODETYPE_ATT_TEXT_VAL = "text";
		private const string EXTRA_METHOD_ATT = "method";
		private const string EXTRA_VALUE_ATT = "value";
		private const string METHOD_ATT_REGEX_VAL = "regex";
		private const string METHOD_ATT_XPATH_VAL = "xpath";
		private const string USECONFIG_ATT = "useConfig";
		private const string LOCATION_ATT = "location";
        private const string WHERE_ATT = "where";
        private const string PREFIX_ATT = "prefix";
		private const string POSTFIX_ATT = "postfix";
        private const string CONDITION_TYPE_ATT = "conditionType";
        private const string CONDITION_ATT = "condition";

		
		private ExtraMethod _extraMethod;
		private XmlDocument _configDoc;
		private Hashtable _values;
		private ConfigUtils _configUtils;
		private bool _useConfigFile;
		private string _extraValue;
		private Location _location;
		private string _prefix;
		private string _postfix;

        private ConditionType _conditionType;
        private string _condition;

        private Regex _insertWhereRegex;
        private MatchEvaluator _insertWhereHandler;
        private string _insertWhereReplaceValue;

        public Update(XmlDocument doc, Transform transform, RunMode runMode, Hashtable htmlEntities, NamespaceManagerHelper nsmh, XmlDocument configDoc, Hashtable values)
            : base(doc, transform, runMode, htmlEntities, nsmh, configDoc, values)
		{
			_useConfigFile = false;

			_extraMethod = GetExtraMethod();

			_configUtils = new ConfigUtils(configDoc);

			_extraValue = Transform.Action.Extra.Attributes[EXTRA_VALUE_ATT] == null ? String.Empty : (string)Transform.Action.Extra.Attributes[EXTRA_VALUE_ATT];

			_location = Transform.Action.Extra.Attributes[LOCATION_ATT] == null ? Location.replace : (Location)Enum.Parse(typeof(Location), (string)Transform.Action.Extra.Attributes[LOCATION_ATT], true);

            try
            {
                if (_location == Location.insert)
                {
                    _insertWhereRegex = new Regex((string)Transform.Action.Extra.Attributes[WHERE_ATT], RegexOptions.Compiled);
                    _insertWhereHandler = new MatchEvaluator(InsertWhereReplace);
                }
            }
            catch 
            {
            }

            _prefix = Transform.Action.Extra.Attributes[PREFIX_ATT] == null ? String.Empty : (string)Transform.Action.Extra.Attributes[PREFIX_ATT];

            _postfix = Transform.Action.Extra.Attributes[POSTFIX_ATT] == null ? String.Empty : (string)Transform.Action.Extra.Attributes[POSTFIX_ATT];

            _conditionType = Transform.Action.Extra.Attributes[CONDITION_TYPE_ATT] == null ? ConditionType.none : (ConditionType)Enum.Parse(typeof(ConditionType), (string)Transform.Action.Extra.Attributes[CONDITION_TYPE_ATT], true);
            
            _condition = _conditionType != ConditionType.none && Transform.Action.Extra.Attributes[CONDITION_ATT] != null ? (string)Transform.Action.Extra.Attributes[CONDITION_ATT] : String.Empty;

			_configDoc = configDoc;

			_values	= values;
		}

		public override void ProcessNodes()
		{
			ArrayList sortedFoundNodes = XmlHelper.SortNodeListArrays(FoundNodes);

			for (int i = sortedFoundNodes.Count - 1; i >= 0; --i)
			{
				XmlHelper.ListNode listNode = (XmlHelper.ListNode)sortedFoundNodes[i];
			
				this.ProcessNode(listNode.Node);
			}
		}

		public override void ProcessNode(XmlNode node)
		{
            if (node.HasChildNodes)
            {
                foreach (XmlNode subNode in node.ChildNodes)
                {
                    ProcessNode(subNode);
                }
            }
            else
            {
                if (VerifyCondition(node))
                {
                    string updateValue = GetUpdateValue(node);

                    object enclosedObject = Transform.Action.Target.Get(node, null, NameSpaceHelper);

                    string existingValue = GetExistingValue(enclosedObject, node);

                    updateValue = GetCompleteUpdateValue(existingValue, updateValue);

                    switch (enclosedObject.GetType().FullName)
                    {
                        case "System.Xml.XmlNode":
                        case "System.String":
                            UpdateNode(node, updateValue);
                            break;

                        case "System.Text.RegularExpressions.MatchCollection":
                            break;

                        default:
                            throw new Exception(string.Format("Unable to update object of type: '{0}'.", enclosedObject.GetType().FullName));
                    }
                }
            }
		}

		private void UpdateNode(XmlNode node, string updateValue)
		{
			switch (node.NodeType)
			{
				case XmlNodeType.Text:
				case XmlNodeType.Attribute:
                case XmlNodeType.CDATA:
                case XmlNodeType.ProcessingInstruction:
                    node.Value = updateValue;
					break;
				case XmlNodeType.Element:
					((XmlElement)node).InnerText = updateValue;
					break;
				default:
					break;
			}
		}

		private string GetUpdateValue(XmlNode node)
		{
			switch (_extraMethod)
			{
				case ExtraMethod.text:
					return GetStringUpdateValue();

				case ExtraMethod.regex:
					return GetRegexUpdateValue(node);

				case ExtraMethod.xpath:
					return GetXPathUpdateValue(node);

				case ExtraMethod.unknown:
					return "";
				default:
					return "";
			}
		}

		private string GetExistingValue(object enclosedObject, XmlNode node)
		{
			switch (enclosedObject.GetType().FullName)
			{
				case "System.Xml.XmlNodeList":
					return "";

				case "System.Xml.XmlNode":
				case "System.String":
                    return (node.NodeType == XmlNodeType.Text || 
                            node.NodeType == XmlNodeType.Attribute || 
                            node.NodeType == XmlNodeType.ProcessingInstruction ||
							node.NodeType == XmlNodeType.CDATA) ? 
                        node.Value : 
                        node.NodeType == XmlNodeType.Element ? 
                            ((XmlElement)node).InnerText : 
                            "";
					
				case "System.Text.RegularExpressions.MatchCollection":
					return "";

				default:
					throw new Exception(string.Format("Unable to update object of type: '{0}'.", enclosedObject.GetType().FullName));
			}
		}


		private string GetCompleteUpdateValue(string existingValue, string updateValue)
		{
			updateValue = string.Format("{0}{1}{2}", _prefix, updateValue, _postfix);

			switch (_location)
			{
				case Location.replace:
				case Location.unknown:
					return updateValue;

				case Location.append:
					return string.Format("{0}{1}", existingValue, updateValue);

				case Location.prepend:
					return string.Format("{0}{1}", updateValue, existingValue);

                case Location.insert:
                    _insertWhereReplaceValue = updateValue;
                    return _insertWhereRegex.Replace(existingValue, _insertWhereHandler);

				default:
					return "";
			}
		}

		private string GetStringUpdateValue()
		{
			return Transform.Action.With.Argument;
		}

		private string GetRegexUpdateValue(XmlNode node)
		{
            string returnValue = string.Empty;

            switch (node.NodeType)
            {
                case XmlNodeType.Text:
                case XmlNodeType.Attribute:
                    returnValue = GetRegexUpdateValuePart(node.Value);
                    break;

                case XmlNodeType.Element:
                    foreach (XmlNode childNode in ((XmlElement)node).ChildNodes)
                    {
                        if (childNode.NodeType == XmlNodeType.Text)
                        {
                            returnValue = GetRegexUpdateValuePart(childNode.Value);
                        }

                    }
                    break;
                
                default:
                    break;
            }

            return returnValue;
		}

        private string GetRegexUpdateValuePart(string text)
        {
            string pattern = Transform.Action.Extra.Argument;
            string replacement = _extraValue;
            string result = Regex.Replace(text, pattern, replacement);
            return result;
        }

		private string GetXPathUpdateValue(XmlNode node)
		{
			if (_useConfigFile)
			{
				return _configUtils.GetXPathValue(_extraValue, _values);
			}
			else
			{
				return _configUtils.GetXPathValue(node.OwnerDocument, _extraValue, _values);
			}	
		}

        private bool VerifyCondition(XmlNode node)
        {
            switch (_conditionType)
            {
                case ConditionType.none:
                    return true;

                case ConditionType.xpath:
                    return (bool)node.CreateNavigator().Evaluate(_condition);

                case ConditionType.regex:
                    return Regex.IsMatch(node.Value, _condition);
            }

            return true;
        }

		private ExtraMethod GetExtraMethod()
		{
			if (Transform.Action.With.NodeType == XmlNodeType.Text && Transform.Action.With.Argument != "")
			{
				return ExtraMethod.text;
			}

			if (Transform.Action.Extra != null)
			{
				Extra extra = Transform.Action.Extra;
				
				if (extra.Attributes[EXTRA_METHOD_ATT] != null)
				{
					string method = (string)extra.Attributes[EXTRA_METHOD_ATT];

					if (method.ToLower() == METHOD_ATT_XPATH_VAL)
					{	
						if (extra.Attributes[USECONFIG_ATT] != null && ((string)extra.Attributes[USECONFIG_ATT]) != "")
						{
							try
							{		
								string useConfigString = ((string)extra.Attributes[USECONFIG_ATT]).ToLower();
						
								_useConfigFile = Convert.ToBoolean(useConfigString);	
							}
							catch
							{
							}
						}

						return ExtraMethod.xpath;
					}

					if (method.ToLower() == METHOD_ATT_REGEX_VAL)
					{
						return ExtraMethod.regex;
					}
				}
			}

			return ExtraMethod.unknown;
			
		}

        private string InsertWhereReplace(Match match)
        {
            return _insertWhereReplaceValue;
        }
	}
}
