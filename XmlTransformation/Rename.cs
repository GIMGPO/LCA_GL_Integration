using System;
using System.Collections;
using System.Xml;

namespace XmlTransformation
{
	public class Rename : TransformBase
	{
		private static string BACUP_ATTRIBUTE				= "backupAttribute";
		private static string USE_CDATA_ATTRIBUTE			= "useCDATA";
		private static string RENAME_CONTENT_ATTRIBUTE		= "renameContent";
		private static string NESTING_ATTRIBUTE				= "nesting";

		private string _newElementName;
		private string _backupAttributeName;
		private bool _useCDATA;
		private bool _renameContent;
		private bool _nesting;

		private ArrayList _deepNodes; 

		public Rename(XmlDocument doc, Transform transform, RunMode runMode, Hashtable htmlEntities, NamespaceManagerHelper nsmh, XmlDocument configDoc, Hashtable values) : base(doc, transform, runMode, htmlEntities, nsmh, configDoc, values)
		{
		}

		public override void Initialise()
		{
			_newElementName = Transform.Action.With.Argument;
			_backupAttributeName = (string)Transform.Action.Extra.Attributes[BACUP_ATTRIBUTE];
			_useCDATA = Transform.Action.Extra.Attributes[USE_CDATA_ATTRIBUTE] == null ? false : Convert.ToBoolean(Transform.Action.Extra.Attributes[USE_CDATA_ATTRIBUTE]);
			_renameContent = Transform.Action.Extra.Attributes[RENAME_CONTENT_ATTRIBUTE] == null ? false : Convert.ToBoolean(Transform.Action.Extra.Attributes[RENAME_CONTENT_ATTRIBUTE]);
			_nesting = Transform.Action.Extra.Attributes[NESTING_ATTRIBUTE] == null ? false : Convert.ToBoolean(Transform.Action.Extra.Attributes[NESTING_ATTRIBUTE]);
		}

		public override void CollectNodes()
		{
			if (Mode == RunMode.backward)
			{
				FoundNodes = NameSpaceHelper.GetNodes(GetXPath());
			}
			else
			{
				ResolveCollectNodes();
			}
		}

		private void ResolveCollectNodes()
		{
			base.CollectNodes();

			if (_renameContent)
			{
				_deepNodes = _nesting ? FindRootmostNodes(FoundNodes) : ArrayFromIEnumerable(FoundNodes, typeof(XmlNode));

				_deepNodes = GetDescendants(_deepNodes);
			}
		}

		private ArrayList FindRootmostNodes(XmlNodeList nodeList)
		{
			ArrayList sortedListLevelNodes = XmlHelper.SortNodeListArrays(nodeList, typeof(XmlHelper.ListLevelNode));

			ArrayList sortedNodes = ArrayFromIEnumerable(sortedListLevelNodes, typeof(XmlHelper.ListLevelNode));

			ArrayList nodesToRemove = new ArrayList();

			for (int i = 0; i < sortedNodes.Count; ++i)
			{
				XmlNode sortedNode = (XmlNode)sortedNodes[i];

				XmlNodeList childNodes = sortedNode.SelectNodes(".//*");

				foreach(XmlNode childNode in childNodes)
				{
					if (childNode.NodeType == XmlNodeType.Element)
					{
						if (sortedNodes.Contains(childNode))
						{
							int nodeToRemoveIndex = GetContainedChildIndex(sortedNodes, childNode);

							nodesToRemove.Add(nodeToRemoveIndex);
						}
					}
				}
			}

			for (int j = sortedNodes.Count - 1; j >= 0; --j)
			{
				if (nodesToRemove.Contains(j))
				{
					sortedNodes.RemoveAt(j);
				}
			}

			return sortedNodes;
		}

//		private ArrayList FindRootmostNodes(XmlNodeList nodeList)
//		{
//			ArrayList sortedListLevelNodes = XmlHelper.SortNodeListArrays(nodeList, typeof(XmlHelper.ListLevelNode));
//
//			ArrayList sortedNodes = ArrayFromIEnumerable(sortedListLevelNodes, typeof(XmlHelper.ListLevelNode));
//	
//			int currentCount = sortedNodes.Count;
//	
//			int newCount = 0;
//			
//			int nodeIndex = -1; 
//
//			while (currentCount != newCount)
//			{
//				currentCount = sortedNodes.Count;
//
//				XmlNode node = (XmlNode)sortedNodes[++nodeIndex];
//
//				XmlNodeList childNodes = node.SelectNodes(".//*");
//
//				foreach(XmlNode childNode in childNodes)
//				{
//					if (childNode.NodeType == XmlNodeType.Element)
//					{
//						if (sortedNodes.Contains(childNode))
//						{
//							sortedNodes.Remove(childNode);
//						}
//					}
//				}
//
//				newCount = sortedNodes.Count;
//			}
//
//			return sortedNodes;
//		}

		private ArrayList GetDescendants(ArrayList nodes)
		{
			ArrayList deepNodes = new ArrayList();

			foreach (XmlNode node in nodes)
			{
				deepNodes.Add(node);

				XmlNodeList childNodes = node.SelectNodes(".//*");

				foreach (XmlNode childNode in childNodes)
				{
					if (childNode.NodeType == XmlNodeType.Element)
					{
						deepNodes.Add(childNode);
					}
				}
			}

			return deepNodes;
		}

		private ArrayList ArrayFromIEnumerable(IEnumerable list, Type objectType)
		{
			ArrayList arrayList = new ArrayList();

			XmlNode node;

			foreach (object item in list)
			{
				if (objectType == typeof(XmlNode))
				{
					node = (XmlNode)item;
				}
				else if (objectType == typeof(XmlHelper.ListNode))
				{
					XmlHelper.ListNode listNode = (XmlHelper.ListNode)item;
					node = listNode.Node;
				}
				else if (objectType == typeof(XmlHelper.ListLevelNode))
				{
					XmlHelper.ListLevelNode listLeveNode = (XmlHelper.ListLevelNode)item;
					node = listLeveNode.Node;
				}
				else
				{
					throw new Exception(string.Format("Unsupported list item object type: '{0}'.",  objectType.Name));
				}

				arrayList.Add(node);
			}
		
			return arrayList;
		}

		public override void ProcessNodes()
		{
			ArrayList sortedFoundNodes = new ArrayList();
			
			if (_renameContent && Mode == RunMode.forward)
			{
				sortedFoundNodes = XmlHelper.SortNodeListArrays(_deepNodes, typeof(XmlHelper.ListNode));
			}
			else
			{
				sortedFoundNodes = XmlHelper.SortNodeListArrays(FoundNodes, typeof(XmlHelper.ListNode));
			}

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
				if (! element.IsEmpty)
				{
					// create element
					XmlElement newElement = (XmlElement)XmlHelper.CreateNode(Document, XmlNodeType.Element, _newElementName, null);

					// insert attributes from existing element
					XmlHelper.AddAttributes(newElement, element.Attributes);

                    if (!String.IsNullOrEmpty(_backupAttributeName))
                    {
                        // create and set backup attribute
                        XmlAttribute backupAttribute = (XmlAttribute)XmlHelper.CreateNode(Document, XmlNodeType.Attribute, _backupAttributeName, null);

                        backupAttribute.Value = element.LocalName;

                        // insert backup attribute on new element
                        XmlHelper.AddSingleAttribute(newElement, backupAttribute);
                    }

					// insert child nodes from existing element
					if (element.HasChildNodes)
					{
						XmlHelper.AddNodes(newElement, element.ChildNodes, true);
					}

					if (element.ParentNode.NodeType != XmlNodeType.Document)
					{
						XmlElement parentElement = (XmlElement)element.ParentNode;

						// add new element to parent
						XmlHelper.AddSingleNodeAt(parentElement, newElement, element, true);

						// remove existing element from parent
						XmlHelper.RemoveChildNode(parentElement, element);
					}
					else
					{
						Document.RemoveChild(element);

						Document.AppendChild(newElement);
					}

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

				if (element.ParentNode.NodeType != XmlNodeType.Document)
				{
					XmlElement parentElement = (XmlElement)element.ParentNode;

					// add original element to parent
					XmlHelper.AddSingleNodeAt(parentElement, originalElement, element, true);

					// remove existing element from parent
					XmlHelper.RemoveChildNode(parentElement, element);

					originalElement.IsEmpty = isEmpty;
				}
				else
				{
					Document.RemoveChild(element);

					Document.AppendChild(originalElement);
				}

				ResolveRemoveCDATA(originalElement);
			}
		}

		private string GetXPath()
		{
//			if (NameSpaceHelper.HasDefaultOrXmlNamespace())
//			{
//				return string.Format("//{0}:{1}", NameSpaceHelper.DefaultNamespaceName, _newElementName.TrimStart(new char [2] {' ', '/'}));
//				
//			}
//			else
//			{
				return string.Format("//{0}", _newElementName.TrimStart(new char [2] {' ', '/'}));
			}

//		}

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

		private int GetContainedChildIndex(ArrayList sortedNodes, XmlNode childNode)
		{
			for (int i = 0; i < sortedNodes.Count; ++i)
			{
				XmlNode sortedNode = (XmlNode)sortedNodes[i];
				
				if (sortedNode == childNode)
				{
					return i;
				}
			}

			return -1;
		}
	}
}
