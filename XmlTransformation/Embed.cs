using System;
using System.Collections;
using System.Xml;
using System.Text.RegularExpressions;

namespace XmlTransformation
{
	public class Embed : TransformBase
	{
		XmlNodeType _newEnclosingNodeType;

		public Embed(XmlDocument doc, Transform transform, RunMode runMode, Hashtable htmlEntities, NamespaceManagerHelper nsmh, XmlDocument configDoc, Hashtable values) 
			: base(doc, transform, runMode, htmlEntities, nsmh, configDoc, values)
		{
		}

		public override void Initialise()
		{
			_newEnclosingNodeType = Transform.Action.With.NodeType;
		}

		
		public override void ProcessNode(XmlNode node)
		{
			switch (Mode)
			{
				case RunMode.forward:
					ProcessNodeForward(node);
					break;

				case RunMode.backward:
					ProcessNodeBackward(node);
					break;

				default:
					break;
			}
		}

		private void ProcessNodeForward(XmlNode node)
		{
			object enclosedObject = Transform.Action.Target.Get(node, null, NameSpaceHelper);

			switch (enclosedObject.GetType().FullName)
			{
				case "System.Xml.XmlNodeList":
					EmbedNodeList(node, (XmlNodeList)enclosedObject);
					break;

				case "System.Xml.XmlNode":
					EmbedNode(node, (XmlNode)enclosedObject);
					break;

				case "System.Text.RegularExpressions.MatchCollection":
					EmbedMatches(node, (MatchCollection)enclosedObject);
					break;

				case "System.String":
					EmbedString(node, (string)enclosedObject);
					break;
					
				default:
					throw new Exception(string.Format("Unable to run embed object of type: '{0}'.", enclosedObject.GetType().FullName));
			}
		}

		private void ProcessNodeBackward(XmlNode node)
		{
			switch (_newEnclosingNodeType)
			{
				case XmlNodeType.CDATA:
					XmlNode contentNode = node.ChildNodes[0];
					if (contentNode != null && contentNode.NodeType == XmlNodeType.CDATA)
					{
						XmlText textNode = (XmlText)XmlHelper.CreateNode(Document, XmlNodeType.Text, null, contentNode.Value);
						Normalizer.RestoreIllegalChars(textNode);
						//textNode.Value = Normalizer.ConvertHtmlEntities(textNode.Value, true);
						node.ReplaceChild(textNode, contentNode);
					}
					break;

				case XmlNodeType.Element:
					XmlNode removeNode = XmlHelper.GetChildNode(node, Transform.Action.With.Argument);
					if (removeNode != null)
					{
						XmlHelper.AddNodes(node, removeNode.ChildNodes, true);
						XmlHelper.RemoveChildNode(node, removeNode);
					}
					break;

				default:
					throw new Exception(string.Format("Unable to promote object of type: '{0}'", _newEnclosingNodeType.ToString()));
			}
		}

		private void EmbedString(XmlNode node, string enclosedText)
		{
			if (enclosedText != null && enclosedText != "")
			{
				switch (_newEnclosingNodeType)
				{
					case XmlNodeType.CDATA:
						XmlCDataSection newCDATASection = (XmlCDataSection)XmlHelper.CreateNode(Document, _newEnclosingNodeType, null, enclosedText);
						newCDATASection = (XmlCDataSection)Normalizer.EscapeIllegalChars(newCDATASection);
						InsertNode(node, newCDATASection);
						break;

					case XmlNodeType.Element:
						XmlElement newElement = (XmlElement)XmlHelper.CreateNode(Document, _newEnclosingNodeType, Transform.Action.With.Argument, null);
						XmlText newText = (XmlText)XmlHelper.CreateNode(Document, XmlNodeType.Text, null, enclosedText);
						newElement.AppendChild((XmlText)Normalizer.EscapeIllegalChars(newText));
						InsertNode(node, newElement);
						break;

					default:
						throw new Exception(string.Format("Can not embed an XmlText node in a node of type '{0}'.", _newEnclosingNodeType.ToString()));
				}
			}
		}

		private void EmbedNodeList(XmlNode node, XmlNodeList enclosedNodes)
		{
			if (_newEnclosingNodeType == XmlNodeType.Element)
			{
				XmlElement newElement = (XmlElement)XmlHelper.CreateNode(Document, _newEnclosingNodeType, Transform.Action.With.Argument, null);
				XmlHelper.AddNodes(newElement, enclosedNodes, true);
				InsertNode(node, newElement);
			}
			else
			{
				throw new Exception(string.Format("Can not embed an XmlNodeList in a node of type '{0}'.", _newEnclosingNodeType.ToString()));
			}

		}

		private void EmbedNode(XmlNode node, XmlNode enclosedNode)
		{
			if (_newEnclosingNodeType == XmlNodeType.Element)
			{
				XmlElement newElement = (XmlElement)enclosedNode;
				InsertNode(node, newElement);
			}
			else
			{
				throw new Exception(string.Format("Can not embed an XmlNode in a node of type '{0}'.", _newEnclosingNodeType.ToString()));
			}
		}

		private void EmbedMatches(XmlNode node, MatchCollection matches)
		{
			throw new Exception(string.Format("Unsupported at present: enclosing Regex.Match values."));
		}

		private void InsertNode(XmlNode node, XmlNode newNode)
		{
			XmlHelper.RemoveChildNodes(node, false);
			XmlHelper.AddSingleNode(node, newNode, true);
		}
	}
}
