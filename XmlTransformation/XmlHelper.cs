using System;
using System.Collections;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace XmlTransformation
{
    public enum MarkupState { No, Translatable, NonTranslatable }

    public class XmlHelper
    {
        public XmlHelper()
        {
        }

        public static XmlNode GetChildNode(XmlNode node, string childElementName)
        {
            return node.SelectSingleNode(childElementName);
        }

        public static string GetAttValue(XmlNode node, string attName)
        {
            XmlAttribute attribute = node.Attributes[attName];

            if (attribute != null && attribute.Value != "")
            {
                return attribute.Value;
            }

            return "";
        }

        public static string GetChildElementValue(XmlNode node, string childElementName)
        {
            XmlNode childNode = GetChildNode(node, childElementName);

            if (childNode != null)
            {
                return ((XmlElement)childNode).InnerText;
            }

            return "";
        }

        public static void AddAttributes(XmlElement parentElement, XmlAttributeCollection attributes)
        {
            foreach (XmlAttribute attribute in attributes)
            {
                AddSingleAttribute(parentElement, (XmlAttribute)attribute.Clone());
            }
        }

        public static void AddSingleAttribute(XmlElement parentElement, XmlAttribute attribute)
        {
            parentElement.Attributes.Append(attribute);
        }

        public static void AddSingleNode(XmlNode parentNode, XmlNode newChildNode, bool append)
        {
            AddSingleNodeAt(parentNode, newChildNode, append ? parentNode.LastChild : parentNode.FirstChild, append);
        }

        public static XmlNode AddSingleNodeAt(XmlNode parentNode, XmlNode newChildNode, XmlNode refChildNode, bool after)
        {
			if (newChildNode is XmlAttribute)
			{
				return parentNode.Attributes.Append((XmlAttribute)newChildNode);
			}
			else
			{
				if (refChildNode == null)
				{
					return parentNode.AppendChild(newChildNode);
				}
				else
				{
					if (after)
					{
						return parentNode.InsertAfter(newChildNode, refChildNode);
					}
					else
					{
						return parentNode.InsertBefore(newChildNode, refChildNode);
					}
				}
			}
        }

        public static void CopyNodes(XmlNode donorNode, XmlNode recipientNode)
        {
            XmlNode refChildNode = AddSingleNodeAt(recipientNode, donorNode.ChildNodes[0].Clone(), null, true);

            for (int nodeIndex = 1; nodeIndex < donorNode.ChildNodes.Count; ++nodeIndex)
            {
                refChildNode = AddSingleNodeAt(recipientNode, donorNode.ChildNodes[nodeIndex].Clone(), refChildNode, true);
            }
        }

        public static void AddNodes(XmlNode parentNode, XmlNodeList newChildNodes, bool append)
        {
            XmlNode refChildNode = append ? parentNode.LastChild : parentNode.FirstChild;

            refChildNode = AddSingleNodeAt(parentNode, newChildNodes[0].Clone(), refChildNode, append);

            for (int nodeIndex = 1; nodeIndex < newChildNodes.Count; ++nodeIndex)
            {
                refChildNode = AddSingleNodeAt(parentNode, newChildNodes[nodeIndex].Clone(), refChildNode, true);
            }
        }

        public static void RemoveChildNodes(XmlNode parentNode, bool includeAttributes)
        {
            if (includeAttributes)
            {
                parentNode.RemoveAll();
            }
            else
            {
                for (int i = parentNode.ChildNodes.Count - 1; i >= 0; --i)
                {
                    XmlNode childNode = parentNode.ChildNodes[i];

                    if (childNode.NodeType != XmlNodeType.Attribute)
                    {
                        parentNode.RemoveChild(childNode);
                    }

                }
                //				foreach (XmlNode childNode in parentNode.ChildNodes)
                //				{
                //					if (childNode.NodeType != XmlNodeType.Attribute)
                //					{
                //						parentNode.RemoveChild(childNode);
                //					}	
                //				}
            }
        }

        public static void RemoveChildNode(XmlNode parentNode, XmlNodeType nodeType, string nodeName)
        {
            XmlNode removeNode = null;

            switch (nodeType)
            {
                case XmlNodeType.Element:
                    removeNode = parentNode.SelectSingleNode(nodeName);
                    break;

                case XmlNodeType.Attribute:
                    removeNode = parentNode.Attributes[nodeName];
                    break;

                default:
                    break;
            }

            if (removeNode != null)
            {
                RemoveChildNode(parentNode, removeNode);
            }
        }

        public static ArrayList GetQualifiedName(XmlNode node)
        {
            ArrayList al = new ArrayList();

            if (node.NodeType == XmlNodeType.Attribute || node.NodeType == XmlNodeType.Element)
            {
                if (node.Prefix != "")
                {
                    al.Add(node.Prefix);
                    al.Add(node.LocalName);
                    al.Add(node.NamespaceURI);
                }
                else
                {
                    al.Add(node.NamespaceURI);
                }
            }

            return al;
        }

        public static void RemoveChildNode(XmlNode parentNode, XmlNode removeChildNode)
        {
            parentNode.RemoveChild(removeChildNode);
        }

        public static void RemoveAttribute(XmlNode parentNode, XmlAttribute removeAttribute)
        {
            parentNode.Attributes.Remove(removeAttribute);
        }

        public static XmlNode CreateQualifiedNode(XmlDocument doc, XmlNodeType nodeType, string nodeName, string nodeValue, XmlNode referenceNode)
        {
            switch (nodeType)
            {
                case XmlNodeType.Element:
                    if (referenceNode.Prefix != "")
                    {
                        return doc.CreateElement(referenceNode.Prefix, nodeName, referenceNode.NamespaceURI);
                    }

                    return doc.CreateElement(nodeName);

                case XmlNodeType.Attribute:
                    if (referenceNode.Prefix != "")
                    {
                        return doc.CreateAttribute(referenceNode.Prefix, nodeName, referenceNode.NamespaceURI);
                    }

                    return doc.CreateAttribute(nodeName);

                default:
                    return CreateNode(doc, nodeType, nodeName, nodeValue);
            }
        }

        public static string GetText(Extra extra, string attributeName)
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

        public static string PrepareXPathQuery(XmlNodeType nodeType, string prefix, string nodeName, string nodeValue)
        {
            switch (nodeType)
            {
                case XmlNodeType.Attribute:
                    if (nodeValue == String.Empty)
                    {
                        return string.Format("/@{0}", nodeName);
                    }
                    else
					{	//					  @translate[.='no']
						return string.Format("/@{0}[.='{1}']", nodeName, nodeValue);
                    }

                case XmlNodeType.Element:
                    if (nodeValue == String.Empty)
                    {
                        return string.Format("./{0}", nodeName);
                    }
                    else
                    {
                        return string.Format(".[{0}='{1)']/*", nodeName, nodeValue);
                    }

                case XmlNodeType.Comment:
                    return "./comment()";

                case XmlNodeType.Text:
                case XmlNodeType.CDATA:
                    return "./text()";

                case XmlNodeType.ProcessingInstruction:
                    return string.Format("./processing-instruction({0})", nodeName == String.Empty ? "" : string.Format("'{0}'", nodeName));

                case XmlNodeType.EntityReference:
                    return ".[contains(text(), '&')]/.";

                default:
                    return "./nodes()";
            }

            /*
a node name 
"prefix:*" to select nodes with a given namespace prefix 
"text()" (to select text nodes) 
"node()" (to select any node) 
"processing-instruction()" (to select any processing instruction) 
"processing-instruction('literal')" to select processing instructions with the given name (target) 
comment() to select comment nodes
            */
        }

        public static XmlNode CreateNode(XmlDocument doc, XmlNodeType nodeType, string nodeName, string nodeValue, string namespaceURI, string prefix)
        {
            switch (nodeType)
            {
                case XmlNodeType.Element:
                    if (namespaceURI != null && namespaceURI != String.Empty)
                    {
                        if (prefix != null && prefix != String.Empty)
                        {
                            return doc.CreateElement(prefix, nodeName, namespaceURI);
                        }

                        return doc.CreateElement(nodeName, namespaceURI);
                    }

                    return CreateNode(doc, nodeType, nodeName, nodeValue);


                case XmlNodeType.Attribute:
                    if (namespaceURI != null && namespaceURI != String.Empty)
                    {
                        if (prefix != null && prefix != String.Empty)
                        {
                            return doc.CreateAttribute(prefix, nodeName, namespaceURI);
                        }

                        return doc.CreateAttribute(nodeName, namespaceURI);
                    }

                    return doc.CreateAttribute(nodeName);

                default:
                    return CreateNode(doc, nodeType, nodeName, nodeValue);
            }
        }

        public static XmlNode CreateNode(XmlDocument doc, XmlNodeType nodeType, string nodeName, string nodeValue)
        {
            switch (nodeType)
            {
                case XmlNodeType.CDATA:
                    return doc.CreateCDataSection(nodeValue);

                case XmlNodeType.Comment:
                    return doc.CreateComment(nodeValue);

                case XmlNodeType.Element:
                    string[] nameParts = nodeName.Split(':');

                    if (nameParts.Length > 1)
                    {
                        if (nameParts[0].ToLower().Trim() == "default")
                        {
                            return doc.CreateElement(nameParts[1], doc.DocumentElement.NamespaceURI);
                        }
                        else
                        {
                            return doc.CreateElement(nameParts[1], nameParts[0]);
                        }
                    }

                    return doc.CreateElement(nodeName, doc.DocumentElement.NamespaceURI);

                case XmlNodeType.EntityReference:
                    return doc.CreateEntityReference(nodeName);

                case XmlNodeType.Text:
                    return doc.CreateTextNode(nodeValue);

                case XmlNodeType.Attribute:
                    XmlAttribute newAtt = doc.CreateAttribute(nodeName);
                    newAtt.Value = nodeValue;
                    return newAtt;

                case XmlNodeType.ProcessingInstruction:
                    return doc.CreateProcessingInstruction(nodeName, nodeValue);

                default:
                    return null;
            }
        }

        public class ListNode : IComparable
        {
            public string CompleteXPath;
            public XmlNode Node;

            public ListNode(string completeXPath, XmlNode node)
            {
                CompleteXPath = completeXPath; ;
                Node = node;
            }

            public int CompareTo(object obj)
            {
                if (obj is ListNode)
                {
                    ListNode bListNode = (ListNode)obj;

                    return CompleteXPath.CompareTo(bListNode.CompleteXPath);
                }

                throw new Exception("Object is not of type 'ListNode'.");
            }
        }

        public class MarkupListNode : ListNode
        {
            private MarkupState _state;

            public MarkupListNode(string completeXPath, XmlNode node)
                : base(completeXPath, node)
            {
                _state = MarkupState.No;
            }

            public MarkupState State
            {
                get { return _state; }
                set { _state = value; }
            }
        }

        public class ListLevelNode : IComparable
        {
            public XmlNode Node;
            public string CompleteXPath;
            public int Depth;

            public ListLevelNode(string completeXPath, XmlNode node)
            {
                CompleteXPath = completeXPath; ;
                Node = node;
                Depth = completeXPath.Split('/').Length;
            }

            public int CompareTo(object obj)
            {
                if (obj is ListLevelNode)
                {
                    ListLevelNode bListLevelNode = (ListLevelNode)obj;

                    if (bListLevelNode.Depth != Depth)
                    {
                        return Depth.CompareTo(bListLevelNode.Depth);
                    }
                    else
                    {
                        return CompleteXPath.CompareTo(bListLevelNode.CompleteXPath);

                    }
                }

                throw new Exception("Object is not of type 'ListLevelNode'.");
            }
        }

        public static ArrayList SortNodeListArrays(Hashtable nodes)
        {
            ArrayList arrayList = new ArrayList();

            foreach (XmlNode node in nodes.Keys)
            {
                MarkupListNode markupListNode = new MarkupListNode(GetListNodeKey(node), node);

                markupListNode.State = (MarkupState)nodes[node];

                arrayList.Add(markupListNode);
            }

            arrayList.Sort(0, arrayList.Count, null);

            return arrayList;
        }

        public static ArrayList SortNodeListArrays(XmlNodeList nodes)
        {
            return SortNodeListArrays(nodes, typeof(XmlHelper.ListNode));
        }

        public static ArrayList SortNodeListArrays(IEnumerable nodes, Type listNodeType)
        {
            ArrayList arrayList = new ArrayList();

            SortedList sortedList = new SortedList();

            foreach (XmlNode node in nodes)
            {
                StringBuilder sb = new StringBuilder();

                XmlNode parentNode;

                switch (node.NodeType)
                {
                    case XmlNodeType.Element:
                        sb.Append("/" + node.Name);
                        parentNode = node.ParentNode;
                        break;

                    case XmlNodeType.Attribute:
                        sb.Append("/@" + node.Name);
                        parentNode = ((XmlAttribute)node).OwnerElement;
                        break;

                    default:
                        parentNode = node.ParentNode;
                        break;
                }

                while (parentNode.NodeType != XmlNodeType.Document)
                {
                    sb.Insert(0, "/" + parentNode.Name);

                    parentNode = parentNode.ParentNode;
                }

                if (listNodeType == typeof(XmlHelper.ListNode))
                {
                    ListNode listNode = new ListNode(sb.ToString(), node);
                    arrayList.Add(listNode);
                }
                else if (listNodeType == typeof(XmlHelper.ListLevelNode))
                {
                    ListLevelNode listLevelNode = new ListLevelNode(sb.ToString(), node);
                    arrayList.Add(listLevelNode);
                }

            }

            arrayList.Sort(0, arrayList.Count, null);

            return arrayList;
        }

        public static string GetListNodeKey(XmlNode node)
        {
            StringBuilder sb = new StringBuilder();

            XmlNode parentNode;

            switch (node.NodeType)
            {
                case XmlNodeType.Element:
                    sb.Append("/" + node.Name);
                    parentNode = node.ParentNode;
                    break;

                case XmlNodeType.Attribute:
                    sb.Append("/@" + node.Name);
                    parentNode = ((XmlAttribute)node).OwnerElement;
                    break;

                default:
                    parentNode = node.ParentNode;
                    break;
            }

            while (parentNode.NodeType != XmlNodeType.Document)
            {
                sb.Insert(0, "/" + parentNode.Name);

                parentNode = parentNode.ParentNode;
            }

            return sb.ToString();

            /*ListNode listNode = new ListNode(sb.ToString(), node);
            return listNode;*/

        }
    }

    public class CharNormalizer
    {
        private Hashtable _illegalChars;
        private Regex _wrappedIllegalsPattern;
        private static string VALUE_GROUP = "value";

        private Hashtable _htmlEntities;
        private Regex _entityRegex;
        private static string ENTITY_GROUP = "entity";

        public CharNormalizer(Hashtable htmlEntities)
        {
            _illegalChars = BuildIllegalCharTable();
            _wrappedIllegalsPattern = new Regex(@"<char value='&#x(?<value>[0-9A-F]{1,2});'[ ]{0,1}/>", RegexOptions.IgnoreCase);

            _htmlEntities = htmlEntities; //BuildHtmlEntityList();
            _entityRegex = new Regex(string.Format(@"&(?<{0}>[A-AZa-z0-9]+);", ENTITY_GROUP));
        }

        public XmlNode EscapeIllegalChars(XmlNode contentNode)
        {
            contentNode.Value = EscapeIllegalChars(contentNode.Value);

            return contentNode;
        }

        public string EscapeIllegalChars(string nodeValue)
        {
            StringBuilder sb = new StringBuilder();

            foreach (char c in nodeValue)
            {
                if (_illegalChars[c] != null)
                {
                    string asHex = Convert.ToString((int)_illegalChars[c], 16);

                    sb.Append(string.Format("<char value='&#x{0};' />", asHex));
                }
                else
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }

        public XmlNode RestoreIllegalChars(XmlNode textNode)
        {
            textNode.Value = RestoreIllegalChars(textNode.Value);

            return textNode;
        }

        public string RestoreIllegalChars(string nodeValue)
        {
            StringBuilder sb = new StringBuilder();

            int endLastTag = 0;

            MatchCollection matches = _wrappedIllegalsPattern.Matches(nodeValue);

            for (int i = 0; i < matches.Count; ++i)
            {
                sb.Append(nodeValue.Substring(endLastTag, matches[i].Index - endLastTag));

                char c = (char)(Convert.ToInt32(matches[i].Groups[VALUE_GROUP].Value, 16));

                sb.Append(c);

                endLastTag = matches[i].Index + matches[i].Length;
            }

            sb.Append(endLastTag < nodeValue.Length ? nodeValue.Substring(endLastTag) : "");

            return sb.ToString();
        }

        public string ConvertHtmlEntities(string htmlString, bool toNCR)
        {
            StringBuilder entSB = new StringBuilder();

            int endLastEntity = 0;

            MatchCollection entityInstances = _entityRegex.Matches(htmlString);

            for (int ii = 0; ii < entityInstances.Count; ++ii)
            {
                Match entityInstance = entityInstances[ii];

                entSB.Append(htmlString.Substring(endLastEntity, entityInstance.Index - endLastEntity));

                try
                {
                    if (entityInstance.Groups[ENTITY_GROUP] != null && _htmlEntities[entityInstance.Groups[ENTITY_GROUP].Value] != null)
                    {
                        if (toNCR)
                        {
                            entSB.Append(string.Format("&#{0};", Convert.ToString(_htmlEntities[entityInstance.Groups[ENTITY_GROUP].Value])));
                        }
                        else
                        {
                            int charPoint = Convert.ToInt32(_htmlEntities[entityInstance.Groups[ENTITY_GROUP].Value]);

                            if (charPoint <= 255)
                            {
                                byte[] myByte = new byte[] { (byte)charPoint };
                                entSB.Append(Encoding.Default.GetString(myByte));
                            }
                            else
                            {
                                char newChar = (char)charPoint;
                                entSB.Append(newChar);
                            }
                        }
                    }
                    else
                    {
                        entSB.Append(entityInstance.Value);
                    }
                }
                catch
                {
                    entSB.Append(entityInstance.Value);
                }

                endLastEntity = entityInstance.Index + entityInstance.Length;
            }

            entSB.Append(endLastEntity < htmlString.Length ? htmlString.Substring(endLastEntity) : "");

            return entSB.ToString();
        }

        private Hashtable BuildIllegalCharTable()
        {
            Hashtable chars = new Hashtable();

            for (int i = 1; i < 32; i++)
            {
                if ((i > 0 && i < 9) || (i > 10 && i < 13) || (i > 13 && i < 32))
                {
                    chars[(char)i] = i;
                }
            }

            return chars;

        }

        private Hashtable BuildHtmlEntityList()
        {
            _htmlEntities = new Hashtable();

            _htmlEntities["nbsp"] = 160;
            _htmlEntities["iexcl"] = 161;
            _htmlEntities["curren"] = 164;
            _htmlEntities["cent"] = 162;
            _htmlEntities["pound"] = 163;
            _htmlEntities["yen"] = 165;
            _htmlEntities["brvbar"] = 166;
            _htmlEntities["sect"] = 167;
            _htmlEntities["uml"] = 168;
            _htmlEntities["copy"] = 169;
            _htmlEntities["ordf"] = 170;
            _htmlEntities["laquo"] = 171;
            _htmlEntities["not"] = 172;
            _htmlEntities["shy"] = 173;
            _htmlEntities["reg"] = 174;
            _htmlEntities["trade"] = 8482;
            _htmlEntities["macr"] = 175;
            _htmlEntities["deg"] = 176;
            _htmlEntities["plusmn"] = 177;
            _htmlEntities["sup2"] = 178;
            _htmlEntities["sup3"] = 179;
            _htmlEntities["acute"] = 180;
            _htmlEntities["micro"] = 181;
            _htmlEntities["para"] = 182;
            _htmlEntities["middot"] = 183;
            _htmlEntities["cedil"] = 184;
            _htmlEntities["sup1"] = 185;
            _htmlEntities["ordm"] = 186;
            _htmlEntities["raquo"] = 187;
            _htmlEntities["frac14"] = 188;
            _htmlEntities["frac12"] = 189;
            _htmlEntities["frac34"] = 190;
            _htmlEntities["iquest"] = 191;
            _htmlEntities["times"] = 215;
            _htmlEntities["divide"] = 247;

            return _htmlEntities;
        }
    }

    public class NamespaceManagerHelper
    {
        private const string NAMESPACE = "NS";
        private const string DEFAULT = "default";
        private const string XML = "xml";

        private Regex _nameSpaceRegex;
        private XmlDocument _xmlDoc;
        private XmlNamespaceManager _nameSpaceManager;
        private Hashtable _namespaces;

        public NamespaceManagerHelper(XmlDocument xmlDoc)
        {
            _xmlDoc = xmlDoc;

            _namespaces = new Hashtable();

            _nameSpaceManager = new XmlNamespaceManager(_xmlDoc.NameTable);

            string uri = xmlDoc.DocumentElement.GetNamespaceOfPrefix(String.Empty);

            uri = uri == null || uri == "" ? GetDefaultNamespace() : uri;

            if (uri != null && uri != "")
            {
                _nameSpaceManager.AddNamespace(DEFAULT, uri);

                _namespaces[DEFAULT] = uri;
            }

            //_nameSpaceRegex = new Regex(string.Format(@"(^|/)(?<{0}>\w+):", NAMESPACE));
            _nameSpaceRegex = new Regex(string.Format(@"(?<{0}>\w+):", NAMESPACE));

        }

        public XmlNodeList GetNodes(XmlNode baseNode, string xpathQuery)
        {
            MatchCollection matches = _nameSpaceRegex.Matches(xpathQuery);

            foreach (Match match in matches)
            {
                string prefix = match.Groups[NAMESPACE].Value;

                if (prefix != DEFAULT && !_nameSpaceManager.HasNamespace(prefix) && _namespaces[prefix] == null)
                {
                    if (prefix.ToLower() == XML)
                    {
                        _nameSpaceManager.AddNamespace(XML, "http://www.w3.org/XML/1998/namespace");

                        _namespaces[XML] = "http://www.w3.org/XML/1998/namespace";

                    }
                    else
                    {
                        string uri = _xmlDoc.DocumentElement.GetNamespaceOfPrefix(prefix);

                        //if (uri != null && uri != "")
                        //{
                        _nameSpaceManager.AddNamespace(prefix, uri);

                        _namespaces[prefix] = uri;
                        //}
                    }
                }
            }


            string query = xpathQuery;

            bool useNamespaceManager = HasDefaultOrXmlNamespace();

            try
            {
                return useNamespaceManager ? baseNode.SelectNodes(query, _nameSpaceManager) : baseNode.SelectNodes(query);
            }
            catch (Exception)
            {
                return baseNode.SelectNodes("aaaaaaaaaaaaaaaaaaaaaaa/zzzzzzzzzzzzzz/qqqqqqqqqqqqqqqqqqq");
            }
        }

        public XmlNodeList GetNodes(string xpathQuery)
        {
            return GetNodes(_xmlDoc, xpathQuery);
        }

        public XmlNamespaceManager NamespaceManager
        {
            get { return _nameSpaceManager; }
        }

        public string DefaultNamespaceName
        {
            get { return DEFAULT; }
        }

        public string XmlNamespaceName
        {
            get { return XML; }
        }

        public bool HasDefaultOrXmlNamespace()
        {
            return (_nameSpaceManager.HasNamespace(DEFAULT) || _nameSpaceManager.HasNamespace(XML));
        }

        private string GetDefaultNamespace()
        {
            //Regex defaultNameSpaceRegex = new Regex(@"xmlns\w*=\w*[""']{1}(?'default'[^""']+)[""']{1}", RegexOptions.IgnoreCase);

            foreach (XmlAttribute attribute in _xmlDoc.DocumentElement.Attributes)
            {
                if (attribute.Name.ToLower() == "xmlns")
                {
                    return attribute.Value;
                }
                else if (attribute.Name.ToLower().StartsWith("xmlns"))
                {
                    return attribute.Value;
                }
            }

            return null;
        }
    }
}
