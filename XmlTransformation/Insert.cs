using System;
using System.Xml;
using System.Text;
using System.Collections;

namespace XmlTransformation
{
	public class Insert : TransformBase
	{
		private static string VALUE_ATT = "value";

		public Insert(XmlDocument doc, Transform transform, RunMode runMode, Hashtable htmlEntities, NamespaceManagerHelper nsmh, XmlDocument configDoc, Hashtable values) : base(doc, transform, runMode, htmlEntities, nsmh, configDoc, values)
		{
		}

		public override void ProcessNode(XmlNode node)
		{
			if (base.Mode == RunMode.forward || base.Mode == RunMode.single)	// ADD
			{	
				Add(node);
			}
			else	// REMOVE
			{
				Remove(node);
			}
		}

		private void Add(XmlNode workNode)
		{
			string xpathQuery = XmlHelper.PrepareXPathQuery(Transform.Action.With.NodeType, "", Transform.Action.With.Argument, GetArgs());
			
			if (workNode.SelectNodes(xpathQuery).Count == 0)
			{
				XmlNode newNode = XmlHelper.CreateNode(Document, Transform.Action.With.NodeType, Transform.Action.With.Argument, GetArgs());

				XmlHelper.AddSingleNodeAt(workNode, newNode, GetRefNode(workNode), GetLocation());
			}
		}

		private void Remove(XmlNode workNode)
		{
			string xpathQuery = XmlHelper.PrepareXPathQuery(Transform.Action.With.NodeType, "", Transform.Action.With.Argument, GetArgs());

			XmlNodeList nodeList = workNode.SelectNodes(xpathQuery);

			if (nodeList.Count == 1)
			{
				workNode.RemoveChild(nodeList[0]);
			}
		}

		private string GetArgs()
		{
			StringBuilder sb = new StringBuilder();
			
			switch (Transform.Action.With.NodeType)
			{
				case XmlNodeType.ProcessingInstruction:
					foreach (string key in Transform.Action.Extra.Attributes.Keys)
					{
						sb.Append(string.Format("{0}='{1}' ", key, (string)Transform.Action.Extra.Attributes[key]));
					}
					sb.Remove(sb.Length -1, 1);
					break;

				case XmlNodeType.Attribute:
					if (Transform.Action.Extra.Attributes[VALUE_ATT] != null)
					{	
						sb.Append((string)Transform.Action.Extra.Attributes[VALUE_ATT]);
					}

					break;
	
				default:
					sb.Append(Transform.Action.Extra.Argument);
					break;
			}

			return sb.ToString();
		}

		private XmlNode GetRefNode(XmlNode workNode)
		{
			switch (Transform.Action.With.NodeType)
			{
				case XmlNodeType.ProcessingInstruction:
				case XmlNodeType.Attribute:
					return workNode.FirstChild;

				default:
					return workNode.LastChild;
			}
		}

		private bool GetLocation()
		{
			switch (Transform.Action.With.NodeType)
			{
				case XmlNodeType.ProcessingInstruction:
					return true;

				case XmlNodeType.Attribute:
					return false;

				default:
					return true;
			}
		}
	}
}
