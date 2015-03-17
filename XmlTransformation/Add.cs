using System;
using System.Xml;
using System.Collections;


namespace XmlTransformation
{
	public class Add  : TransformBase
	{
		XmlNodeType _addedNodeType;
		string _addedNodeName;

		public Add(XmlDocument doc, Transform transform, RunMode runMode, Hashtable htmlEntities, NamespaceManagerHelper nsmh, XmlDocument configDoc, Hashtable values) : base(doc, transform, runMode, htmlEntities, nsmh, configDoc, values)
		{
			_addedNodeType = Transform.Action.With.NodeType;
			_addedNodeName = Transform.Action.With.Argument;
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
			if (this.Mode == RunMode.forward || this.Mode == RunMode.single)
			{
				ProcessNodeForward(node);
			}
			else
			{
				ProcessNodeBackward(node);
			}
		}

		private void ProcessNodeForward(XmlNode node)
		{
			object contextObject = Transform.Action.Target.Get(node, null, NameSpaceHelper);

			if (contextObject is XmlNode)
			{
				switch (_addedNodeType)
				{
					case XmlNodeType.Attribute:
						AddAttribute((XmlNode)contextObject);
						break;

					case XmlNodeType.Text:
						InsertText((XmlNode)contextObject);
						break;

					case XmlNodeType.Element:
						AddElement((XmlNode)contextObject);
						break;
	
					default:
						break;
				}
			}
			else
			{
				throw new Exception(string.Format("Can not add an object of type '{0}' to another of type '{1}'.", _addedNodeType.GetType().Name, contextObject.GetType().Name));
			}
		}
		
		private void InsertText(XmlNode node)
		{
			switch (node.NodeType)
			{
				case XmlNodeType.Element:
					node.InnerText = Transform.Action.With.Argument;
					break;

				case XmlNodeType.CDATA:
				case XmlNodeType.Comment:
				case XmlNodeType.Attribute:
				case XmlNodeType.Text:
					node.Value = Transform.Action.With.Argument;
					break;

				default:
					break;

			}
		}

		private void AddAttribute(XmlNode node)
		{
			object valueObject = GetValue(node, Transform.Action.Extra);

			string attributeValue = "";

			switch (valueObject.GetType().FullName)
			{
				case "System.String":
					attributeValue = (string)valueObject;
					break;
				
				case "System.Xml.XmlNodeList":
				case "System.Xml.XPath.XPathNodeList":
                case "System.Xml.XPathNodeList":
					XmlNodeList valueNodeList = (XmlNodeList)valueObject;
					attributeValue = valueNodeList[0].NodeType == XmlNodeType.Element ? valueNodeList[0].InnerText : valueNodeList[0].Value;
					break;
				
				case "System.Xml.XmlElement":
				case "System.Xml.XmlText":
					XmlElement valueElement = (XmlElement)valueObject;
					attributeValue = valueElement.InnerText;
					break;

				case "System.Xml.XmlComment":
				case "System.Xml.XmlAttribute":
				case "System.Xml.XmlCDataSection":
					attributeValue = ((XmlNode)valueObject).Value;
					break;
					
				default:
					break;
			}

			switch (node.NodeType)
			{
				case XmlNodeType.Element:
					string attName = Transform.Action.With.Argument;
					int prefixColon = attName.IndexOf(':');	
					
					XmlAttribute newAttribute = null;

					if (prefixColon == -1 || attName.Substring(0, prefixColon).ToLower() == "xml")
					{
						newAttribute = Document.CreateAttribute(attName);
					}
					else
					{
						string prefix = attName.Substring(0, prefixColon);
						string localName = attName.Substring(prefixColon +1);
						newAttribute = Document.CreateAttribute(prefix, localName, Transform.Action.With.Reference);
					}

					newAttribute.Value = Concatenate(attributeValue, Transform.Action.Extra);
					XmlHelper.AddSingleAttribute((XmlElement)node, newAttribute);
					break;

				default:
					break;
			}
		}

		private void AddElement(XmlNode node)
		{
			object valueObject = GetValue(node, Transform.Action.Extra);

			bool removeWhiteSpace = GetRemoveWhitespace(Transform.Action.Extra);

			switch (node.NodeType)
			{
				case XmlNodeType.Element:	// The only supported case

					XmlElement newElement = Document.CreateElement(Transform.Action.With.Argument);

				switch (valueObject.GetType().FullName)
				{
					case "System.Xml.XmlText":
					case "System.String":
						newElement.InnerText = (string)valueObject;
						break;
					
					case "System.Xml.XmlNodeList":
					case "System.Xml.XPath.XPathNodeList":
                    case "System.Xml.XPathNodeList":
						XmlNodeList valueNodeList = (XmlNodeList)valueObject;
						foreach(XmlNode valueNode in valueNodeList)
						{
							if (valueNode.NodeType == XmlNodeType.Text)
							{
								XmlNode newNode = valueNode.Clone();
								newNode.Value = Concatenate(newNode.Value, Transform.Action.Extra);
								newElement.AppendChild(newNode);
							}
							else if (valueNode.NodeType == XmlNodeType.Whitespace && removeWhiteSpace)
							{
							}
							else
							{
								newElement.AppendChild(valueNode.Clone());
							}
						}
						//node.AppendChild(newElement);
						break;
					
					case "System.Xml.XmlElement":
						XmlElement valueElement = (XmlElement)valueObject;
						newElement.AppendChild(valueElement.Clone());
						break;

					case "System.Xml.XmlAttribute":
						XmlHelper.AddSingleAttribute(newElement, (XmlAttribute)((XmlNode)valueObject).Clone());
						break;

					case "System.Xml.XmlComment":
					case "System.Xml.XmlCDataSection":
						newElement.AppendChild(((XmlNode)valueObject).Clone());
						break;
						
					default:
						break;
				}

                bool insertAfter = GetInsertAfter(Transform.Action.Extra);

                XmlHelper.AddSingleNodeAt(node, newElement, node.ChildNodes[0], insertAfter);
					break;

				default:
					break;
			}
		}

		private void ProcessNodeBackward(XmlNode node)
		{
		}

		private object GetValue(XmlNode node, Extra extra)
		{
			const string METHOD = "method";
			const string VALUE = "value";

			if (extra != null)
			{
				string methodText = GetText(extra, METHOD);
				string valueText = GetText(extra, VALUE);

				if (methodText != null && methodText != String.Empty)
				{
					Method method = (Method)Enum.Parse(typeof(Method), (string)extra.Attributes[METHOD], true);

					switch (method)
					{
						case Method.regex:
							return GetValueRegex(node, valueText, extra);
	
						case Method.xml:
							return GetValueXML(node, valueText, extra);

						case Method.xpath:
							return GetValueXPath(node, valueText, extra);

						case Method.literal:
							return valueText;
							
						case Method.text:
						default:
							return node.InnerText;
					}
				}
				else
				{
					return node.InnerText;
				}
			}

			return null;
		}

		private bool GetRemoveWhitespace(Extra extra)
		{
			const string WHITESPACE = "whitespace";
			const string DELETE = "delete";

			string whiteSpace = GetText(extra, WHITESPACE);

			return whiteSpace == null ? false : whiteSpace.ToLower() == DELETE;
		}

        private bool GetInsertAfter(Extra extra)
        {
            const string AFTER = "addAfter";
            const string TRUE = "true";

            string before = GetText(extra, AFTER);

            return before == null ? false : before.ToLower() == TRUE;
        }

		private string GetValueRegex(XmlNode node, string valueText, Extra extra)
		{
			if (valueText != null && valueText != String.Empty)
			{
				string result = System.Text.RegularExpressions.Regex.Match(node.InnerText, valueText).Value;

				return Concatenate(result, extra);
			}
			else
			{
				return Concatenate(node.InnerText , extra);
			}
		}

		private XmlNodeList GetValueXPath(XmlNode node, string valueText, Extra extra)
		{
			if (valueText != null && valueText != String.Empty)
			{
				XmlNodeList resultNodes = NameSpaceHelper.GetNodes(node, valueText);

				return resultNodes;
			}

			return null;
		}

		private object GetValueXML(XmlNode node, string valueText, Extra extra)
		{
			object result;

			if (valueText.EndsWith(")"))
			{
				int openParenthesis = valueText.IndexOf("(");

				string methodName = valueText.Substring(0, openParenthesis - 1);

				string argString = valueText.Substring(openParenthesis + 1);

				argString = argString.Replace(" ", "");

				object[] args = argString.Split(',');

				result = node.GetType().InvokeMember(methodName, System.Reflection.BindingFlags.InvokeMethod, null, node, args);
			}
			else
			{
				result = node.GetType().InvokeMember(valueText, System.Reflection.BindingFlags.GetProperty, null, node, null);
			}

			return result;
		}

		private string Concatenate(string text, Extra extra)
		{
			const string PREFIX = "prefix";
			const string POSTFIX = "postfix";

			string prefix = "";
			string postfix = "";

			if (extra.Attributes[PREFIX] != null)
			{
				prefix = Convert.ToString(extra.Attributes[PREFIX]);
			}

			if (extra.Attributes[POSTFIX] != null)
			{
				postfix = Convert.ToString(extra.Attributes[POSTFIX]);
			}

			bool removeWhitespace = GetRemoveWhitespace(extra);

			return new System.Text.StringBuilder().Append(prefix).Append(removeWhitespace ? text.Trim() : text).Append(postfix).ToString();
		}

		private string GetText(Extra extra, string attributeName)
		{
			if (extra.Attributes[attributeName] != null)
			{
				string attributeValue = Convert.ToString(extra.Attributes[attributeName]);
				
				if (attributeValue.Length == 0)
				{
					return String.Empty;
				}
				else
				{
					return attributeValue;
				}
			}
			else
			{
				return null;
			}
		}
	}
}
