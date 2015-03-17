using System;
using System.Collections;
using System.Xml;
using System.Text;
using System.Text.RegularExpressions;

namespace XmlTransformation
{
	public class Extract : TransformBase
	{
		private static string USE_CDATA_ATTRIBUTE			= "useCDATA";
		private static string INCLUDE_ATTRIBUTES_ATTRIBUTE	= "includeAttributes";
		private static string INDEX_VALUE_NAME				= "indexValue";
		
		private static string EXTRACTED_LIST_NAME			= "sdlExtractedList";
		private static string EXTRACTED_ELEMENT_NAME		= "sdlExtractedItem";
		private static string INDEX_ATTIBUTE_NAME			= "index";

		private bool _useCDATA;
		private bool _includeAttributes;

		private int _index;
		private string _inLineIndexString;
		private XmlElement _extractedListElement;

		private string _inlineIndexXPath;

		private Regex _inlineIndexRegex;
		private string _regexReserved;



		public Extract(XmlDocument doc, Transform transform, RunMode runMode, Hashtable htmlEntities, NamespaceManagerHelper nsmh, XmlDocument configDoc, Hashtable values) : base(doc, transform, runMode, htmlEntities, nsmh, configDoc, values)
		{
		}

		public override void Initialise()
		{
			_index = 0;

			_inLineIndexString = Transform.Action.With.Argument;
			_useCDATA = Transform.Action.Extra.Attributes[USE_CDATA_ATTRIBUTE] == null ? false : Convert.ToBoolean(Transform.Action.Extra.Attributes[USE_CDATA_ATTRIBUTE]);
			_includeAttributes = Transform.Action.Extra.Attributes[INCLUDE_ATTRIBUTES_ATTRIBUTE] == null ? false : Convert.ToBoolean(Transform.Action.Extra.Attributes[INCLUDE_ATTRIBUTES_ATTRIBUTE]);

			if (Mode == RunMode.backward)
			{
				_extractedListElement = (XmlElement)Document.DocumentElement.SelectSingleNode(EXTRACTED_LIST_NAME);

				int valueMarker = _inLineIndexString.IndexOf("{0}");

				string before = _inLineIndexString.Substring(0, valueMarker);
				
				string after = _inLineIndexString.Substring(valueMarker + 3);

				//_inlineIndexXPath = string.Format(@"//*[starts-with(., '{0}')]|//@*[starts-with(., '{0}')]", before);
				_inlineIndexXPath = string.Format(@"*[starts-with(., '{0}')]|@*[starts-with(., '{0}')]", before);

				_regexReserved = @".$^{[(|)]*+?\";

				string inlineIndexPattern = string.Format("{0}(?'{1}'[0-9]+){2}", EscapeRegexReserved(before), INDEX_VALUE_NAME, EscapeRegexReserved(after));
				
				_inlineIndexRegex = new Regex(inlineIndexPattern);
			}
			else
			{
				_extractedListElement = (XmlElement)XmlHelper.CreateNode(Document, XmlNodeType.Element, EXTRACTED_LIST_NAME, null);
 
				XmlHelper.AddSingleNode(Document.DocumentElement, _extractedListElement, true);
			}
		}

		public override void CollectNodes()
		{
			if (Mode == RunMode.backward)
			{
				//FoundNodes = NameSpaceHelper.GetNodes(_inlineIndexXPath);
				base.CollectNodes();
			}
			else
			{
				base.CollectNodes();
			}
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
			if (Mode == RunMode.forward)
			{
				ProcessNodeForward(node);
			}
			else if (Mode == RunMode.backward)
			{
				ProcessNodeBackward(node);
			}		
		}

		public override void PostProcess()
		{
			if (Mode == RunMode.backward)
			{
				XmlHelper.RemoveChildNode(Document.DocumentElement, _extractedListElement);
			}
		}

		private void ProcessNodeForward(XmlNode node)
		{
			if (node.NodeType == XmlNodeType.Element)
			{
				ProcessNodeForwardElement(node);
			}
			else if (node.NodeType == XmlNodeType.Attribute)
			{
				ProcessNodeForwardAttribute(node);
			}
		}

		private void ProcessNodeForwardElement(XmlNode node)
		{
			XmlElement element = (XmlElement)node;

			if (! element.IsEmpty && element.HasChildNodes)
			{
				// create element
				XmlElement newElement = (XmlElement)XmlHelper.CreateNode(Document, XmlNodeType.Element, EXTRACTED_ELEMENT_NAME, null);

				// create and set backup attribute
				XmlAttribute indexAttribute = (XmlAttribute)XmlHelper.CreateNode(Document, XmlNodeType.Attribute, INDEX_ATTIBUTE_NAME, null);

				indexAttribute.Value = _index.ToString();

				// insert backup attribute on new element
				XmlHelper.AddSingleAttribute(newElement, indexAttribute);

				XmlHelper.CopyNodes(element, newElement);
				
				//XmlHelper.AddNodes(newElement, element.ChildNodes, true);

				XmlHelper.RemoveChildNodes(element, false);

				element.InnerText = string.Format(_inLineIndexString, _index);

				ResolveInsertCDATA(newElement);

				XmlHelper.AddSingleNodeAt(_extractedListElement, newElement, null, true);

				IncrementIndex();
			}
		}

		private void ProcessNodeForwardAttribute(XmlNode node)
		{
			XmlAttribute attribute = (XmlAttribute)node;

			if (attribute.Value != "")
			{
				// create new extracted element
				XmlElement newElement = (XmlElement)XmlHelper.CreateNode(Document, XmlNodeType.Element, EXTRACTED_ELEMENT_NAME, null);

				// create and set index attribute
				XmlAttribute indexAttribute = (XmlAttribute)XmlHelper.CreateNode(Document, XmlNodeType.Attribute, INDEX_ATTIBUTE_NAME, null);

				indexAttribute.Value = _index.ToString();

				// insert index attribute on new element
				XmlHelper.AddSingleAttribute(newElement, indexAttribute);

				newElement.InnerText = attribute.Value;

				attribute.Value = string.Format(_inLineIndexString, _index);

				ResolveInsertCDATA(newElement);

				// add new element
				XmlHelper.AddSingleNodeAt(_extractedListElement, newElement, null, true);

				IncrementIndex();
			}
		}


		private void ProcessNodeBackward(XmlNode node)
		{
			if (node.NodeType == XmlNodeType.Attribute)
			{
				ProcessNodeBackwardAttribute(node);
			}
			else if (node.NodeType == XmlNodeType.Element)
			{
				ProcessNodeBackwardElement(node);
			}
		}
	
		private void ProcessNodeBackwardAttribute(XmlNode node)
		{
			XmlAttribute attribute = (XmlAttribute)node;

			XmlElement indexedElement = GetIndexedElement(attribute.Value);

			if (indexedElement != null)
			{
				attribute.Value = indexedElement.InnerText;
			}
			
		}

		private void ProcessNodeBackwardElement(XmlNode node)
		{
			XmlElement element = (XmlElement)node;
			
			XmlElement indexedElement = GetIndexedElement(element.InnerText);

			if (indexedElement != null)
			{
				XmlHelper.RemoveChildNodes(element, false);

				XmlHelper.CopyNodes(indexedElement, element);

				//XmlHelper.AddNodes(element, indexedElement.ChildNodes, true);

				ResolveRemoveCDATA(element);
			}
		}

		private XmlElement GetIndexedElement(string nodeValue)
		{
			Match m = _inlineIndexRegex.Match(nodeValue);

			if (m.Success && m.Groups[INDEX_VALUE_NAME].Success)
			{
				int index = Convert.ToInt32(m.Groups[INDEX_VALUE_NAME].Value);

				string elementQuery = string.Format(@"{0}[@{1}={2}]", EXTRACTED_ELEMENT_NAME, INDEX_ATTIBUTE_NAME, index);

				XmlNodeList indexedElements = NameSpaceHelper.GetNodes(_extractedListElement, elementQuery);

				if (indexedElements.Count == 1)
				{
					return (XmlElement)indexedElements[0];
				}
			}

			return null;
		}

		private void ResolveInsertCDATA(XmlElement newElement)
		{
			if (_useCDATA)
			{
				if (newElement.ChildNodes.Count == 1 && newElement.ChildNodes[0].NodeType == XmlNodeType.Text)
				{
					XmlCDataSection newCDATASection = (XmlCDataSection)XmlHelper.CreateNode(Document, XmlNodeType.CDATA, null, newElement.InnerText);

					newCDATASection = (XmlCDataSection)Normalizer.EscapeIllegalChars(newCDATASection);
				
					XmlHelper.RemoveChildNodes(newElement, false);
				
					XmlHelper.AddSingleNode(newElement, newCDATASection, true);
				}
			}
		}

		private void ResolveRemoveCDATA(XmlElement originalElement)
		{
			if (_useCDATA && originalElement.HasChildNodes)
			{
				if (originalElement.ChildNodes.Count == 1 && originalElement.ChildNodes[0].NodeType == XmlNodeType.CDATA)
				{
					XmlNode contentNode = originalElement.ChildNodes[0];

					XmlText textNode = (XmlText)XmlHelper.CreateNode(Document, XmlNodeType.Text, null, contentNode.Value);
			
					textNode.Value = Normalizer.RestoreIllegalChars(textNode.Value);

					//textNode.Value = Normalizer.ConvertHtmlEntities(textNode.Value, true);
			
					originalElement.ReplaceChild(textNode, contentNode);
				}
			}
		}

		private void IncrementIndex()
		{
			_index++;
		}

		public string EscapeRegexReserved(string regexDef)
		{
			StringBuilder sb = new StringBuilder();

			foreach (char c in regexDef)
			{
				if (_regexReserved.IndexOf(c) != -1)
				{
					sb.Append('\\');
				}

				sb.Append(c);
			}
			
			return sb.ToString();
		}
	}
}
