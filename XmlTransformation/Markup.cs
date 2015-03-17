using System;
using System.Collections;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace XmlTransformation
{
    public class Markup : TransformBase
    {
        private static string METHOD = "method";
        private static string REGEX = "regex";
        private static string PROTECT = "protect";
        private static string VALUE = "value";//private static string VALUE			= "verbatim";
        private static string MARKUP_GROUP = "markup";


        private bool _useRegex;
        private string _searchText;
        private int _searchTextLength;
        private Regex _searchRegex;
        private string _tagName;

        private bool _protect;

        public Markup(XmlDocument doc, Transform transform, RunMode runMode, Hashtable htmlEntities, NamespaceManagerHelper nsmh, XmlDocument configDoc, Hashtable values)
            : base(doc, transform, runMode, htmlEntities, nsmh, configDoc, values)
        {
        }

        public override void Initialise()
        {
            _useRegex = false;

            if (Transform.Action.Extra != null && Transform.Action.Extra.Attributes[METHOD] != null)
            {
                _searchText = Transform.Action.Extra.Argument;

                _searchTextLength = _searchText.Length;

                if ((string)Transform.Action.Extra.Attributes[METHOD] == REGEX)
                {
                    _useRegex = true;

                    _searchRegex = new Regex(_searchText);
                }
            }

            _protect = false;

            if (Transform.Action.Extra != null && Transform.Action.Extra.Attributes[PROTECT] != null)
            {
                string protectString = (string)Transform.Action.Extra.Attributes[PROTECT];

                if (protectString != "" && protectString.ToLower() == "true")
                {
                    _protect = true;
                }
            }

            if (Transform.Action.With.NodeType == XmlNodeType.Element)
            {
                _tagName = Transform.Action.With.Argument;
            }
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
                case "System.String":	// if Get() returns a string, node is implicitly assumed to be the string's parent, i.e. the enclosing element.
                    if (enclosedObject != null && (string)enclosedObject != "")
                    {
                        XmlText textNode = ReplaceContentWithTextNode(Transform.Action.Target.ParentElement, (string)enclosedObject);
                        ProcessLeafNode(Transform.Action.Target.ParentElement, textNode);
                    }
                    break;

                case "System.Xml.XmlText":
                    string val = ((System.Xml.XmlText)enclosedObject).Value;
                    if (val != null && val != "")
                    {
                        XmlText textNode = ReplaceContentWithTextNode((XmlElement)node, val);
                        ProcessLeafNode((XmlElement)node, textNode);
                    }
                    break;

                case "System.Xml.XmlElement":
                case "System.Xml.XmlNode":
                    ProcessNodeContent((XmlElement)node, (XmlNode)enclosedObject);
                    break;

                case "System.Xml.XmlNodeList":
                case "System.Xml.XPath.XPathNodeList":
                case "System.Xml.XPathNodeList":
                    // works only with "flat" lists: PCDATA|<closed />
                    // need to enhance for depth :	PCDATA|<open>PCDATA<other>PCDATA</other></open>
                    foreach (XmlNode inNode in ((XmlNodeList)enclosedObject))
                    {
                        ProcessNodeContent((XmlElement)node, inNode);
                    }
                    break;

                default:
                    break;
            }
        }

        private XmlText ReplaceContentWithTextNode(XmlElement parentElement, string text)
        {
            XmlHelper.RemoveChildNodes(parentElement, false);

            XmlText textNode = (XmlText)XmlHelper.CreateNode(Document, XmlNodeType.Text, null, text);

            XmlHelper.AddSingleNode(parentElement, textNode, true);

            return textNode;
        }

        private void ProcessNodeContent(XmlElement outElement, XmlNode inNode)
        {
            if (inNode.HasChildNodes)
            {
                foreach (XmlNode node in inNode.ChildNodes)
                {
                    ProcessNodeContent((XmlElement)inNode, node);
                }
            }
            else
            {
                if (inNode.NodeType == XmlNodeType.Text)
                {
                    ProcessLeafNode(outElement, (XmlText)inNode);
                }
            }
        }

        private void ProcessLeafNode(XmlElement element, XmlText textNode)
        {
            if (_useRegex)
            {
                ProcessAsRegex(element, textNode);
            }
            else
            {
                ProcessAsText(element, textNode);
            }
        }

        private void ProcessAsRegex(XmlElement element, XmlText oldTextNode)
        {
            string text = oldTextNode.Value;

            MatchCollection matches = _searchRegex.Matches(text);

            if (matches.Count > 0)
            {
                element.RemoveChild(oldTextNode);

                int index = 0;

                foreach (Match match in matches)
                {
                    if (match.Groups[MARKUP_GROUP] != null)
                    {
                        Group group = match.Groups[MARKUP_GROUP];

                        if (index < group.Index)
                        {
                            XmlText textNode = Document.CreateTextNode(text.Substring(index, group.Index - index));

                            element.AppendChild(textNode);
                        }

                        AddContent(element, group.Value);

                        index = group.Index + group.Length;
                    }
                    else
                    {
                        if (index < match.Index)
                        {
                            XmlText textNode = Document.CreateTextNode(text.Substring(index, match.Index - index));

                            element.AppendChild(textNode);
                        }

                        AddContent(element, match.Value);

                        index = match.Index + match.Length;
                    }
                }

                if (index < text.Length)
                {
                    XmlText textNode = Document.CreateTextNode(text.Substring(index));

                    element.AppendChild(textNode);
                }
            }
        }

        private void AddContent(XmlElement parentElement, string matchValue)
        {
            XmlElement tagElement = Document.CreateElement(_tagName);

            if (!_protect)
            {
                XmlText tagTextNode = Document.CreateTextNode(matchValue);

                tagElement.AppendChild(tagTextNode);
            }
            else
            {
                XmlAttribute valueAttribute = Document.CreateAttribute(VALUE);

                valueAttribute.Value = matchValue;

                tagElement.Attributes.Append(valueAttribute);
            }

            parentElement.AppendChild(tagElement);
        }

        private void ProcessAsText(XmlElement element, XmlText oldTextNode)
        {
            string text = oldTextNode.Value;

            int foundIndex = text.IndexOf(_searchText);

            if (foundIndex > -1)
            {
                element.RemoveChild(oldTextNode);

                int startIndex = 0;

                while (foundIndex > -1)
                {
                    if (startIndex < foundIndex)
                    {
                        XmlText textNode = Document.CreateTextNode(text.Substring(startIndex, foundIndex - startIndex));

                        element.AppendChild(textNode);
                    }

                    AddContent(element, _searchText);

                    startIndex = foundIndex + _searchTextLength;

                    foundIndex = text.IndexOf(_searchText, startIndex);
                }

                if (startIndex < text.Length)
                {
                    XmlText textNode = Document.CreateTextNode(text.Substring(startIndex));

                    element.AppendChild(textNode);
                }
            }
        }

        // [2012-Jan-27] modified to support both scenarios:
        // - if the Method='Xml' (use reflection) and Member='InnerText' (returned type is System.String), query the type of the incoming node
        // - otherwise, Method='Xpath' (use xpath) and Member='node()' (returned type is XPathNodeList), query the type of the enclosed object 

        private void ProcessNodeBackward(XmlNode node)
        {
            object enclosedObject = Transform.Action.Target.Get(node, null, NameSpaceHelper);

            switch (enclosedObject.GetType().FullName)
            {
                case "System.Xml.XmlElement":
                    ProcessBackwardNodeContent(node);
                    break;

                case "System.Xml.XmlNodeList":
                case "System.Xml.XPath.XPathNodeList":
                case "System.Xml.XPathNodeList":
                    ProcessBackwardNodes((XmlNodeList)enclosedObject);
                    break;

                default:
                    if (node.NodeType == XmlNodeType.Element)
                    {
                        if (!node.HasChildNodes)
                        {
                            ProcessBackwardNodeContent(node);
                        }
                        else
                        {
                            ProcessBackwardNodes(node.ChildNodes);
                        }
                    }
                    break;
            }
        }

        // when removing a node (replace it with a text node), proceeed backward
        private void ProcessBackwardNodes(XmlNodeList nodes)
        {
            for (int childIndex = nodes.Count - 1; childIndex >= 0; --childIndex)
            {
                ProcessBackwardNodeContent(nodes[childIndex]);
            }
        }

        private void ProcessBackwardNodeContent(XmlNode node)
        {
            if (node.NodeType == XmlNodeType.Element)
            {
                XmlElement element = (XmlElement)node;

                if (node.Name == _tagName)
                {
                    //							<tag value="my taggged value" /> : <tag>my taggged value</tag>
                    string tagValue = _protect ? element.Attributes[VALUE].Value : element.InnerText;

                    XmlText tagTextNode = Document.CreateTextNode(tagValue);

                    XmlElement parentElement = (XmlElement)element.ParentNode;

                    parentElement.InsertAfter(tagTextNode, element);

                    parentElement.RemoveChild(element);
                }
            }
        }

        private void OldProcessNodeBackward(XmlNode node)
        {
            string nondevalue;

            try
            {
                if (node.NodeType == XmlNodeType.Element)
                {
                    nondevalue = ((XmlElement)node).InnerText;

                    StringBuilder sb = new StringBuilder();

                    foreach (XmlNode childNode in node.ChildNodes)
                    {
                        switch (childNode.NodeType)
                        {
                            case XmlNodeType.Text:
                                sb.Append(childNode.Value);
                                break;

                            case XmlNodeType.Element:

                                if (!_protect)
                                {
                                    sb.Append(childNode.InnerXml);
                                }
                                else
                                {
                                    if (childNode.Name == _tagName && childNode.Attributes[VALUE] != null)
                                    {
                                        sb.Append(childNode.Attributes[VALUE].Value);
                                    }
                                    else
                                    {
                                        sb.Append(childNode.InnerXml);
                                    }
                                }
                                break;

                            case XmlNodeType.EntityReference:
                                sb.Append(childNode.InnerXml);
                                break;

                            case XmlNodeType.Whitespace:
                                sb.Append(childNode.InnerText);
                                break;

                            default:
                                sb.Append(childNode.Value);
                                break;
                        }
                    }
                    /*
                        if (childNode.NodeType == XmlNodeType.Text)
                        {
                            sb.Append(childNode.Value);
                        }
                        else if (childNode.NodeType == XmlNodeType.Element)
                        {
                            if (! _protect)
                            {
                                sb.Append(childNode.InnerXml);
                            }
                            else
                            {
                                if (childNode.Name == _tagName && childNode.Attributes[VALUE] != null)
                                {
                                    sb.Append(childNode.Attributes[VALUE].Value);
                                }
                                else
                                {
                                    sb.Append(childNode.InnerXml);
                                }
                            }
                        }
                        else if (childNode.NodeType == XmlNodeType.EntityReference)
                        {
                            sb.Append(childNode.InnerXml);
                        }
                        else
                        {
                            sb.Append(childNode.Value);
                        }
					
                    */

                    node.InnerText = sb.ToString();
                }
            }
            catch (Exception e)
            {
                string a = e.ToString();

                string b = a;
            }
        }
    }
}
