using System;
using System.Collections;
using System.Xml;

namespace XmlTransformation
{
	public class RenameAttribute : TransformBase
	{
		private static string SEARCH_FOR_ATTRIBUTE			= "searchFor";
		private static string REPLACE_WITH_ATTRIBUTE		= "replaceWith";
		private static string SEARCH_ATTRIBUTE_NAME			= "attributeName";
		private static string REPLACE_NAME_OBJECT			= "name";
		private static string REPLACE_VALUE_OBJECT			= "value";
		private static string REPLACE_ACTION				= "replace";

		private string _objectType;
		private string _searchFor;
		private string _replaceWith;
		private string _searchAttributeName;
		private string _action;
		
		public RenameAttribute(XmlDocument doc, Transform transform, RunMode runMode, Hashtable htmlEntities, NamespaceManagerHelper nsmh, XmlDocument configDoc, Hashtable values) : base(doc, transform, runMode, htmlEntities, nsmh, configDoc, values)
		{
		}

		public override void Initialise()
		{
			_objectType = Transform.Action.Target.Implementation;
			_action = Transform.Action.With.Argument;
			_searchFor = (string)Transform.Action.Extra.Attributes[SEARCH_FOR_ATTRIBUTE];
			_replaceWith = (string)Transform.Action.Extra.Attributes[REPLACE_WITH_ATTRIBUTE];
			_searchAttributeName = (string)Transform.Action.Extra.Attributes[SEARCH_ATTRIBUTE_NAME];
		}

		public override void CollectNodes()
		{
			base.CollectNodes();
		}

		public override void ProcessNodes()
		{
			ArrayList sortedFoundNodes = new ArrayList();
			
			sortedFoundNodes = XmlHelper.SortNodeListArrays(FoundNodes, typeof(XmlHelper.ListNode));

			for (int i = sortedFoundNodes.Count - 1; i >= 0; --i)
			{
				XmlHelper.ListNode listNode = (XmlHelper.ListNode)sortedFoundNodes[i];
			
				this.ProcessNode(listNode.Node);
			}
		}

		public override void ProcessNode(XmlNode node)
		{
			XmlElement element = (XmlElement)node;

			if (Mode == RunMode.forward)
			{
				if (!element.IsEmpty 
					    && _objectType.ToLower().Equals(REPLACE_NAME_OBJECT) 
					    && _action.ToLower().Equals(REPLACE_ACTION))
				{
					ReplaceAttributeName(element, _searchFor, _replaceWith);
				}
				else if(!element.IsEmpty 
					&& _objectType.ToLower().Equals(REPLACE_VALUE_OBJECT) 
					&& _action.ToLower().Equals(REPLACE_ACTION))
				{
					if(_searchAttributeName != null && !_searchAttributeName.Equals(string.Empty))
					{
						string[] attributeNames = _searchAttributeName.Split(new char[]{'|'});

						if(attributeNames != null && attributeNames.Length > 0)
						{
							ReplaceAttributeValues(element, attributeNames, _searchFor, _replaceWith);
						}
					}
				}
			}
			else if (Mode == RunMode.backward)
			{
				if (!element.IsEmpty 
					&& _objectType.ToLower().Equals(REPLACE_NAME_OBJECT) 
					&& _action.ToLower().Equals(REPLACE_ACTION))
				{
					ReplaceAttributeName(element, _replaceWith, _searchFor);
				}
				else if(!element.IsEmpty 
					&& _objectType.ToLower().Equals(REPLACE_VALUE_OBJECT) 
					&& _action.ToLower().Equals(REPLACE_ACTION))
				{
					if(_searchAttributeName != null && !_searchAttributeName.Equals(string.Empty))
					{
						string[] attributeNames = _searchAttributeName.Split(new char[]{'|'});

						if(attributeNames != null && attributeNames.Length > 0)
						{
							ReplaceAttributeValues(element, attributeNames, _replaceWith, _searchFor);
						}
					}
				}

			}
		}
		
		private XmlElement ReplaceAttributeName(XmlElement element, string oldAttributeName, string newAttributeName)
		{
			string attValue = XmlHelper.GetAttValue(element, oldAttributeName);
			XmlAttribute oldAttribute = element.Attributes[oldAttributeName];
			
			if(!attValue.Equals(string.Empty) && oldAttribute != null)
			{
				XmlAttribute attribute = (XmlAttribute)XmlHelper.CreateNode(Document, System.Xml.XmlNodeType.Attribute, newAttributeName, attValue);
				attribute.Value = attValue;
				XmlHelper.AddSingleAttribute(element, attribute);
				XmlHelper.RemoveAttribute(element, oldAttribute);	
			}

			return element;
		}//end ReplaceAttributeName

		private XmlElement ReplaceAttributeValue(XmlElement element, string attributeName, string oldAttributeValue, string newAttributeValue)
		{
			if(attributeName != null && !attributeName.Equals(string.Empty))
			{
				string attValue = XmlHelper.GetAttValue(element, attributeName);
				XmlAttribute attribute = element.Attributes[attributeName];
			
				if(attValue != null && !attValue.Equals(string.Empty) && attValue.Equals(oldAttributeValue))
				{
					attribute.Value = newAttributeValue;
				}
			}

			return element;
		}//end ReplaceAttributeValue

		private XmlElement ReplaceAttributeValues(XmlElement element, string[] attributeNames, string oldAttributeValue, string newAttributeValue)
		{
			foreach(string attributeName in attributeNames)
			{
				ReplaceAttributeValue(element, attributeName, oldAttributeValue, newAttributeValue);
			}
			return element;
		}//end ReplaceAttributeValues
	}
}
