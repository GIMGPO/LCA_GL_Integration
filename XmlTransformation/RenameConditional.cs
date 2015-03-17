using System;
using System.Text;
using System.Collections;
using System.Xml;
using XmlTransformation;

namespace XmlTransformation
{
	public class RenameConditional : Rename
	{
		private ConfigUtils _configUtils;
		private XmlDocument _configDoc;
		private Hashtable _values;
		private XmlHelper.MarkupListNode _currentMarkupListNode;
		
		private static string BACUP_ATTRIBUTE				= "backupAttribute";
		private static string USE_CDATA_ATTRIBUTE			= "useCDATA";
		private static string EXPRESSION_INFO_ATTRIBUTE		= "expressionInfoXPath";
		private static string CONDITION_ELEMENT				= "condition";
		private static string NODESPECS_ELEMENT				= "nodeSpecs";
		
		private static string XPATH_BUILDER_ELEMENT			= "xpathBuilder";
		private static string XPATH_QUERY_TYPE_ATT			= "type";
		private static string XPATH_QUERY_NAME_ATT			= "name";

		private string _backupAttributeName;
		private bool _useCDATA;

		private Condition _condition;
		private NodeSpecs _nodeSpecs;
		private XPathQueryBuilder _xpathBuilder;

		public RenameConditional(XmlDocument doc, Transform transform, RunMode runMode, Hashtable htmlEntities, NamespaceManagerHelper nsmh, XmlDocument configDoc, Hashtable values) : base(doc, transform, runMode, htmlEntities, nsmh, configDoc, values)
		{
			_configDoc = configDoc;

			_configUtils = new ConfigUtils(_configDoc);

			_values = values;
		}

		public override void Initialise()
		{
			_backupAttributeName = (string)Transform.Action.Extra.Attributes[BACUP_ATTRIBUTE];
			
			_useCDATA = Transform.Action.Extra.Attributes[USE_CDATA_ATTRIBUTE] == null ? false : Convert.ToBoolean(Transform.Action.Extra.Attributes[USE_CDATA_ATTRIBUTE]);

			InitialiseExpressionInfo();
		}

		public override void CollectNodes()
		{
			string searchXPath;

			if (Mode != RunMode.backward)
			{
				searchXPath = ResolveSearch(Transform.SearchXPath); //_configUtils.ResolveXPathMulti(Transform.SearchXPath, _values, nodeSpec.ValuePattern, nodeSpec.ValueSeparator);
			}
			else
			{
				searchXPath = ResolveSearch(Transform.Action.With.Argument); //_configUtils.ResolveXPathMulti(Transform.Action.With.Argument, _values, _nodeSpecs.ValuePattern, _nodeSpecs.ValueSeparator);
			}

			FoundNodes = NameSpaceHelper.GetNodes(searchXPath);
		}

		private string ResolveSearch(string searchSpec)
		{
			string trimSearchSpec = searchSpec.Trim();

			if (trimSearchSpec.StartsWith("{") && trimSearchSpec.EndsWith("}"))
			{
				trimSearchSpec = trimSearchSpec.Substring(1, trimSearchSpec.Length - 2);
				
				XmlNode node = _configUtils.GetConfigNode(trimSearchSpec);
				
				if (node is System.Xml.XmlElement)
				{
					string typeAttribute = ((XmlElement)node).GetAttribute(XPATH_QUERY_TYPE_ATT);
					string nameAttribute = ((XmlElement)node).GetAttribute(XPATH_QUERY_NAME_ATT);
					
					if (typeAttribute == typeof(XmlTransformation.RenameConditional.XPathQueryItem).Name)
					{
						XPathQueryItem query = (XPathQueryItem)_xpathBuilder.Queries[nameAttribute];

						return query.Resolve(_configUtils, _values);
					}

					return trimSearchSpec;
				}

				return trimSearchSpec;
			}
			else
			{
				return searchSpec;
			}
		}

		public override void ProcessNodes()
		{
			ArrayList sortedFoundNodes = XmlHelper.SortNodeListArrays(FoundNodes);

			if (Mode == RunMode.forward)
			{
				Hashtable markupNodes = CollectMarkupNodes(sortedFoundNodes);

				ArrayList sortedMarkupNodes = XmlHelper.SortNodeListArrays(markupNodes);

				for (int i = sortedMarkupNodes.Count -1; i >= 0; --i)
				{
					_currentMarkupListNode = (XmlHelper.MarkupListNode)sortedMarkupNodes[i];

					ProcessNode(_currentMarkupListNode.Node);
				}
			}
			else
			{
				for (int i = sortedFoundNodes.Count - 1; i >= 0; --i)
				{
					XmlHelper.ListNode listNode = (XmlHelper.ListNode)sortedFoundNodes[i];
			
					this.ProcessNode(listNode.Node);
				}
			}
		}

		private string GetElementType(XmlHelper.MarkupListNode markupListnode)
		{
			bool inline = false;
			bool translatable = markupListnode.State == MarkupState.Translatable ? true : false;

			foreach (NodeSpec nodeSpec in _nodeSpecs.NodeSpecCollection.Values)
			{
				if (nodeSpec.List.Contains(markupListnode.Node.Name))
				{
					inline = nodeSpec.InLine;
					break;
				}
			}

			foreach (NodeSpec nodeSpec in _nodeSpecs.NodeSpecCollection.Values)
			{
				if (nodeSpec.InLine == inline && nodeSpec.Translatable == translatable)
				{
					return nodeSpec.Name;
				}
			}

			return null;
		}

		public override void ProcessNode(XmlNode node)
		{
			XmlElement element = (XmlElement)node;

			if (Mode == RunMode.forward)
			{
				if (! element.IsEmpty && _currentMarkupListNode.State != MarkupState.No)
				{
					// create element
					XmlElement newElement = (XmlElement)XmlHelper.CreateNode(Document, XmlNodeType.Element, GetElementType(_currentMarkupListNode), null);

					// insert attributes from existing element
					XmlHelper.AddAttributes(newElement, element.Attributes);

					// create and set backup attribute
					XmlAttribute backupAttribute = (XmlAttribute)XmlHelper.CreateNode(Document, XmlNodeType.Attribute, _backupAttributeName, null);

					backupAttribute.Value = element.LocalName;

					// insert backup attribute on new element
					XmlHelper.AddSingleAttribute(newElement, backupAttribute);

					// insert child nodes from existing element
					if (element.HasChildNodes)
					{
						XmlHelper.AddNodes(newElement, element.ChildNodes, true);
					}

					XmlElement parentElement = (XmlElement)element.ParentNode;

					// add new element to parent
					XmlHelper.AddSingleNodeAt(parentElement, newElement, element, true);

					// remove existing element from parent
					XmlHelper.RemoveChildNode(parentElement, element);

					ResolveInsertCDATA(newElement);
				}
			}
			else if (Mode == RunMode.backward)
			{
				XmlAttribute backupAttribute = element.Attributes[_backupAttributeName];

				string origElementName = backupAttribute.Value;

				bool isEmpty = element.IsEmpty;

				XmlHelper.RemoveAttribute(element, backupAttribute);

				// create element
				XmlElement originalElement = (XmlElement)XmlHelper.CreateNode(Document, XmlNodeType.Element, origElementName, null);

				// insert attributes from existing element...
				XmlHelper.AddAttributes(originalElement, element.Attributes);

				// ... but remove the backup one.
				//XmlHelper.RemoveAttribute(originalElement, backupAttribute);

				if (element.HasChildNodes)
				{
					// insert child nodes from existing element
					XmlHelper.AddNodes(originalElement, element.ChildNodes, true);
				}

				XmlElement parentElement = (XmlElement)element.ParentNode;

				// add original element to parent
				XmlHelper.AddSingleNodeAt(parentElement, originalElement, element, true);

				// remove existing element from parent
				XmlHelper.RemoveChildNode(parentElement, element);

				originalElement.IsEmpty = isEmpty;

				ResolveRemoveCDATA(originalElement);
			}
		}

		private Hashtable CollectMarkupNodes(ArrayList sortedNodes)
		{
			Hashtable markupElements = new Hashtable();

			for (int i = 0; i < sortedNodes.Count ; ++i)
			{
				XmlHelper.ListNode sortedNode = (XmlHelper.ListNode)sortedNodes[i];

				XmlElement element = (XmlElement)sortedNode.Node;

				XmlElement parentElement = (XmlElement)element.ParentNode;

				bool isInherentlyTranslatable;

				bool isOverridden;

				bool isParentTranslatable = IsTranslatable(parentElement, out isInherentlyTranslatable, out isOverridden);

				AnalyseElement(element, isParentTranslatable, markupElements);
			}

			return markupElements;
		}

		private void AnalyseElement(XmlElement element, bool isParentTranslatable, Hashtable markupElements)
		{
			bool isInherentlyTranslatable;
			
			bool isOverridden;
 
			bool isTranslatable = IsTranslatable(element, out isInherentlyTranslatable, out isOverridden);

			foreach (XmlNode childNode in element.ChildNodes)
			{
				if (childNode.NodeType == XmlNodeType.Element)
				{
					AnalyseElement((XmlElement)childNode, isTranslatable, markupElements);
				}
			}
 
			ResolveAddToMarkup(markupElements, element, isParentTranslatable, isInherentlyTranslatable, isOverridden);
		}

		private void ResolveAddToMarkup(Hashtable markupElements, XmlElement element, bool isParentTranslatable, bool isInherentlyTranslatable, bool isOverridden)
		{
			//Write(element, isParentTranslatable,  isInherentlyTranslatable, isOverridden);

			if (! isParentTranslatable)									/*	N or -T	*/ 
			{
				if (isInherentlyTranslatable && isOverridden)			/*	-T */
				{
					AddToMarkup(markupElements, element, false);
				}
				else if (isInherentlyTranslatable && ! isOverridden)	/*	T */
				{
					AddToMarkup(markupElements, element, false);
				}
				else if (! isInherentlyTranslatable && ! isOverridden)	/*	N */
				{
					//AddToMarkup(markupElements, element, false);
				}					
				else if (! isInherentlyTranslatable && isOverridden)	/*	-N */
				{
					AddToMarkup(markupElements, element, true);
				}
			}
			else														/*	T or -N	*/ 
			{
				if (isInherentlyTranslatable && isOverridden)			/*	-T */
				{
					AddToMarkup(markupElements, element, false);
				}
				else if (isInherentlyTranslatable && ! isOverridden)	/*	T */
				{
					//AddToMarkup(markupElements, false);
				}
				else if (! isInherentlyTranslatable && ! isOverridden)	/*	N */
				{
					//AddToMarkup(markupElements, element, false);
				}					
				else if (! isInherentlyTranslatable && isOverridden)	/*	-N */
				{
					AddToMarkup(markupElements, element, true);
				}
			}
		}

		private void AddToMarkup(Hashtable markupElements, XmlElement element, bool asTranslatable)
		{
			if (markupElements[element] == null)
			{
				markupElements[element] = asTranslatable ? MarkupState.Translatable : MarkupState.NonTranslatable;
			}
		}

		private bool IsTranslatable(XmlElement element, out bool isInherentlyTranslatable, out bool isOverridden)
		{
			isInherentlyTranslatable = IsInherentlyTranslatable(element);

			isOverridden = IsOverridden(element, isInherentlyTranslatable);

			return	isInherentlyTranslatable && ! isOverridden ? true :		/* T */
				! isInherentlyTranslatable && isOverridden ? true :		/* -N */ 
				isInherentlyTranslatable && isOverridden ? false :		/* -T */
				! isInherentlyTranslatable && ! isOverridden ? false :	/* N */
				false;
		}

		private bool IsInherentlyTranslatable(XmlElement element)
		{
			int translatables = 0;
			int nonTranslatables = 0;

			foreach (NodeSpec nodeSpec in _nodeSpecs.NodeSpecCollection.Values)
			{
				if (nodeSpec.List.Contains(element.Name))
				{
					if (nodeSpec.Translatable)
					{
						++translatables;
					}
					else
					{
						++nonTranslatables;
					}
				}
			}

			return translatables > 0;
		}

		private bool IsOverridden(XmlElement element, bool isInherentlyTranslatable)
		{
			if (_condition.HasConditionNode(element) && _condition.HasUsableValue(element))
			{
				bool conditionResult = _condition.Evaluate(element);

				return isInherentlyTranslatable ? conditionResult == false : conditionResult == true;
			}
			else
			{
				return _condition.DefaultValue;
			}
		}

		private void ResolveInsertCDATA(XmlElement newElement)
		{
			if (_useCDATA)
			{
				XmlCDataSection newCDATASection = (XmlCDataSection)XmlHelper.CreateNode(Document, XmlNodeType.CDATA, null, newElement.InnerText);

				newCDATASection = (XmlCDataSection)Normalizer.EscapeIllegalChars(newCDATASection);
				
				XmlHelper.RemoveChildNodes(newElement, false);
				
				XmlHelper.AddSingleNode(newElement, newCDATASection, true);
			}
		}

		private void ResolveRemoveCDATA(XmlElement originalElement)
		{
			if (_useCDATA && originalElement.HasChildNodes)
			{
				XmlNode contentNode = originalElement.ChildNodes[0];
			
				XmlText textNode = (XmlText)XmlHelper.CreateNode(Document, XmlNodeType.Text, null, contentNode.Value);
			
				textNode.Value = Normalizer.RestoreIllegalChars(textNode.Value);

				//textNode.Value = Normalizer.ConvertHtmlEntities(textNode.Value, true);
			
				originalElement.ReplaceChild(textNode, contentNode);
			}
		}

		private void InitialiseExpressionInfo()
		{
			object expressionInfoAttribute = Transform.Action.Extra.Attributes[EXPRESSION_INFO_ATTRIBUTE];

			if (expressionInfoAttribute == null)
			{
				return;
			}

			string expressionInfoAttributeValue = Convert.ToString(Transform.Action.Extra.Attributes[EXPRESSION_INFO_ATTRIBUTE]);

			string expressionInfoXPath = _configUtils.ResolveXPath(expressionInfoAttributeValue, _values);

			XmlElement expressionInfoElement = (XmlElement)_configUtils.GetConfigNode(expressionInfoXPath);

			_condition = new Condition((XmlElement)expressionInfoElement.SelectSingleNode(CONDITION_ELEMENT));

			_nodeSpecs = new NodeSpecs((XmlElement)expressionInfoElement.SelectSingleNode(NODESPECS_ELEMENT));

			_xpathBuilder = new XPathQueryBuilder((XmlElement)expressionInfoElement.SelectSingleNode(XPATH_BUILDER_ELEMENT));
		}

		private class Condition
		{
			private const string NODE_TYPE_ATT		= "nodeType";
			private const string NAME_ATT			= "name";
			private const string VALUE_TRUE_ATT		= "valueTrue";
			private const string VALUE_FALSE_ATT	= "valueFalse";
			private const string DEFAULT_VALUE_ATT	= "defaultValue";

			public XmlNodeType NodeType;
			public string Name;
			public string ValueFalse;
			public string ValueTrue;
			public bool DefaultValue;

			public Condition(XmlElement conditionElement)
			{
				NodeType = 	(XmlNodeType)Enum.Parse(typeof(XmlNodeType), conditionElement.Attributes[NODE_TYPE_ATT].Value, true);

				Name = conditionElement.Attributes[NAME_ATT].Value;

				ValueFalse = conditionElement.Attributes[VALUE_FALSE_ATT].Value;

				ValueTrue = conditionElement.Attributes[VALUE_TRUE_ATT].Value;

				DefaultValue = Convert.ToBoolean(conditionElement.Attributes[DEFAULT_VALUE_ATT].Value);
			}

			public bool HasConditionNode(XmlElement element)
			{
				switch (NodeType)
				{
					case XmlNodeType.Attribute:
						return element.Attributes[Name] != null;

					case XmlNodeType.Element:
						return element.GetElementsByTagName(Name).Count > 0;

					default:
						return false;
				}
			}

			public bool HasUsableValue(XmlElement element)
			{
				string value = GetConditionValue(element);

				if (value == null)
				{
					return false;
				}

				if (value.ToLower() == ValueTrue.ToLower() || value.ToLower() == ValueFalse.ToLower())
				{
					return true;
				}

				return false;
			}

			public bool Evaluate(XmlElement element)
			{
				string value = GetConditionValue(element);

				if (value == null)
				{
					return DefaultValue;
				}

				if (value.ToLower() == ValueTrue.ToLower())
				{
					return true;
				}

				if (value.ToLower() == ValueFalse.ToLower())
				{
					return false;
				}

				return DefaultValue;
			}

			private string GetConditionValue(XmlElement element)
			{
				switch (NodeType)
				{
					case XmlNodeType.Attribute:
						return element.Attributes[Name].Value;

					case XmlNodeType.Element:
						XmlNodeList childElements = element.GetElementsByTagName(Name);
						XmlElement childElement = (XmlElement)childElements[0];
						return childElement.InnerText;

					default:
						return null;
				}
			}
		}

		private NodeSpec GetNodeSpec(object key)
		{
			if (_nodeSpecs.NodeSpecCollection[key] != null)
			{
				return (NodeSpec)_nodeSpecs.NodeSpecCollection[key];
			}

			if (key is System.Int32)
			{
				int index = 0;

				NodeSpec thisNodeSpec=  null;

				foreach (NodeSpec nodeSpec in _nodeSpecs.NodeSpecCollection.Values)
				{
					thisNodeSpec = nodeSpec;

					if (index == (int)key)
					{
						return nodeSpec;
					}

					++index;
				}

				return thisNodeSpec;
			}
			return null;
		}

		private class NodeSpecs
		{
			private static string NAME_ATTRIBUTE		= "name";
			private static string NODESPEC_ELEMENT		= "nodeSpec";

			public string Name;

			public Hashtable NodeSpecCollection;

			public NodeSpecs(XmlElement nodeSpecsElement)
			{
				Name = nodeSpecsElement.Attributes[NAME_ATTRIBUTE].Value;
				
				NodeSpecCollection = new Hashtable();

				foreach (XmlElement nodeSpecElement in nodeSpecsElement.SelectNodes(NODESPEC_ELEMENT))
				{
					NodeSpec nodeSpec = new NodeSpec(nodeSpecElement);

					NodeSpecCollection[nodeSpec.Name] = nodeSpec;
				}
			}
		}

		private class NodeSpec
		{
			private static string NAME_ATTRIBUTE			= "name";
			private static string INLINE_ATTRIBUTE			= "inline";
			private static string TRANSLATABLE_ATTRIBUTE	= "translatable";

			private static char	  SEPARATOR	= '|';
			
			public string Name;
			public bool InLine;
			public bool Translatable;
			public ArrayList List;

			public NodeSpec(XmlElement nodeSpecElement)
			{
				Name = nodeSpecElement.Attributes[NAME_ATTRIBUTE].Value;
				
				InLine = Convert.ToBoolean(nodeSpecElement.Attributes[INLINE_ATTRIBUTE].Value);

				Translatable = Convert.ToBoolean(nodeSpecElement.Attributes[TRANSLATABLE_ATTRIBUTE].Value);

				List = new ArrayList();
				
				string[] items = nodeSpecElement.InnerText.Split(SEPARATOR);

				List.AddRange(items);
			}
		}

		private class XPathQueryBuilder
		{
			private static string XPATH_QUERY_ELEMENT		= "xpathQuery";

			public Hashtable Queries;

			public XPathQueryBuilder(XmlElement xpathBuilderElement)
			{
				Queries = new Hashtable();

				foreach (XmlElement queryElement in xpathBuilderElement.SelectNodes(XPATH_QUERY_ELEMENT))
				{
					XPathQueryItem query = new XPathQueryItem(queryElement);

					Queries[query.Name] = query;
				}

			}
		}
		
		private class XPathQueryItem
		{
			private static string NAME_ATTRIBUTE			= "name";
			private static string OUTER_PATTERN_ATTRIBUTE	= "outerPattern";
			private static string PATTERN_ATTRIBUTE			= "pattern";
			private static string SEPARATOR_ATTRIBUTE		= "separator";

			public string Name;
			public string OuterPattern; 
			public string ValuePattern;
			public string ValueSeparator;
			public string QuerySpec;
			
			public XPathQueryItem(XmlElement element)
			{
				Name = XmlHelper.GetAttValue(element, NAME_ATTRIBUTE);

				OuterPattern = XmlHelper.GetAttValue(element, OUTER_PATTERN_ATTRIBUTE);

				OuterPattern = OuterPattern == "" ? "{0}" : OuterPattern;

				ValuePattern = XmlHelper.GetAttValue(element, PATTERN_ATTRIBUTE);
				
				ValuePattern = ValuePattern == "" ? "{0}" : ValuePattern;
				
				ValueSeparator = element.HasAttribute(SEPARATOR_ATTRIBUTE) ? XmlHelper.GetAttValue(element, SEPARATOR_ATTRIBUTE) : "|";

				QuerySpec = element.InnerText;
			}

			public string Resolve(ConfigUtils configUtils, Hashtable values)
			{
				return string.Format(OuterPattern, configUtils.ResolveXPathMulti(QuerySpec, values, ValuePattern, ValueSeparator));
			}
		}
	}
}
