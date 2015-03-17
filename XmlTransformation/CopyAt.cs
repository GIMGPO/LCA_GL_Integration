using System;
using System.Collections;
using System.Xml;

namespace XmlTransformation
{
    public class CopyAt : TransformBase
    {
        private static string ADD_ATTRIBUTE = "add";
        private static string TYPE_ATTRIBUTE = "type";
        private static string NAME_ATTRIBUTE = "name";
        private static string VALUE_ATTRIBUTE = "value";
        private static string LOCATION_ATTRIBUTE = "location";


        private string _copySource;
        private Method _copySourceMethod;
        private XmlNodeType _copyTargetNodeType;
        private string _copyXPath;
        private string _copyName;

        private bool _addExtra;
        private XmlNodeType _extraNodeType;
        private string _extraNodeName;
        private string _extraNodeValue;
        private string _extraLocation;

        public CopyAt(XmlDocument doc, Transform transform, RunMode runMode, Hashtable htmlEntities, NamespaceManagerHelper nsmh, XmlDocument configDoc, Hashtable values)
            : base(doc, transform, runMode, htmlEntities, nsmh, configDoc, values)
        {
        }

        public override void Initialise()
        {
            if (Transform.Action.Extra != null)
            {
                _addExtra = Transform.Action.Extra.Attributes[ADD_ATTRIBUTE] == null ? false : Convert.ToBoolean(Transform.Action.Extra.Attributes[ADD_ATTRIBUTE]);

                if (_addExtra)
                {
                    string extraNodeName = (string)Transform.Action.Extra.Attributes[TYPE_ATTRIBUTE];

                    _extraNodeType = (XmlNodeType)Enum.Parse(typeof(XmlNodeType), extraNodeName);

                    _extraNodeName = (string)Transform.Action.Extra.Attributes[NAME_ATTRIBUTE];

                    if (Transform.Action.Extra.Attributes[VALUE_ATTRIBUTE] != null)
                    {
                        _extraNodeValue = (string)Transform.Action.Extra.Attributes[VALUE_ATTRIBUTE];
                    }

                    _extraLocation = (string)Transform.Action.Extra.Attributes[LOCATION_ATTRIBUTE];
                }
            }
            else
            {
                _addExtra = false;
            }

            _copySource = Transform.Action.Target.Implementation;

            _copySourceMethod = Transform.Action.Target.Method;

            _copyTargetNodeType = Transform.Action.With.NodeType;

            int pathMarker = Transform.Action.With.Argument.LastIndexOf('/');

            _copyXPath = pathMarker != -1 ? Transform.Action.With.Argument.Substring(0, pathMarker) : null;

            _copyName = Transform.Action.With.Argument.Substring(Transform.Action.With.Argument.LastIndexOfAny(new char[2] { '/', ':' }) + 1);
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

            if (enclosedObject != null && IsTargetContainer(_copyTargetNodeType) && ObjectsTypesCompatible(enclosedObject, _copyTargetNodeType))
            {
                bool targetExists = false;

                // create new node (the target of the copy)
                XmlNode newNode = ResolveCreateNode(node, Transform.Action.With.Argument, _copyTargetNodeType, _copyName, ref targetExists);

                // set content and/or value
                PopulateNode(node, newNode, enclosedObject);

                // add to document, as sibling of context node
                if (!targetExists)
                {
                    //Kiet Aug 4, 2011 -- Add addAfter
                    XmlHelper.AddSingleNodeAt(GetCopyParentNode(node, _copyXPath), newNode, GetReferenceNode(enclosedObject), GetCopyAfter(Transform.Action.Extra));
                }

                // add extra attribute
                ResolveExtraInformation(node, newNode);
            }
        }

        private XmlNode GetReferenceNode(object enclosedObject)
        {
            if (enclosedObject is XmlNode)
            {
                return ((XmlNode)enclosedObject).ParentNode;
            }
            else if (enclosedObject is XmlNodeList)
            {
                return (((XmlNodeList)enclosedObject)[0]).ParentNode;
            }
            else
            {
                return (XmlNode)enclosedObject;
            }
        }

        private bool GetCopyAfter(Extra extra)
        {
            const string AFTER = "copyAfter";
            const string TRUE = "true";

            string before = XmlHelper.GetText(extra, AFTER);

            return before == null ? true : before.ToLower() == TRUE;
        }

        private void ProcessNodeBackward(XmlNode workNode)
        {
            // simply remove extra node

            if (_addExtra)
            {
                XmlNodeList extraNodeLocations = NameSpaceHelper.GetNodes(workNode, _extraLocation);

                if (extraNodeLocations.Count > 0)
                {
                    XmlNode extraNodeParent = extraNodeLocations[0];

                    XmlNodeList extraNodes = NameSpaceHelper.GetNodes(extraNodeParent, _extraNodeName);

                    if (extraNodes.Count > 0)
                    {
                        XmlNode extraNode = extraNodes[0];

                        if (extraNode.NodeType == XmlNodeType.Attribute)
                        {
                            XmlHelper.RemoveAttribute(extraNodeParent, (XmlAttribute)extraNode);
                        }
                        else
                        {
                            XmlHelper.RemoveChildNode(extraNodeParent, extraNode);
                        }
                    }

                }
            }
        }

        private void PopulateNode(XmlNode workNode, XmlNode newNode, object enclosedObject)
        {
            switch (enclosedObject.GetType().FullName)
            {
                case "System.String":	// if Get() returns a string, node is implicitly assumed to be the string parent, i.e. the enclosing element.
                    newNode.InnerText = (string)enclosedObject;
                    break;

                case "System.Xml.XmlText":
                    newNode.InnerText = ((XmlText)enclosedObject).Value;
                    break;

                case "System.Xml.XmlSignificantWhitespace":
                    newNode.InnerText = ((XmlSignificantWhitespace)enclosedObject).Value;
                    break;

                case "System.Xml.XmlNode":
                case "System.Xml.XmlElement":
                    newNode.AppendChild((XmlElement)enclosedObject);
                    break;

                case "System.Xml.XmlNodeList":
                case "System.Xml.XPath.XPathNodeList":
                case "System.Xml.XPathNodeList":
                    XmlHelper.AddNodes(newNode, (XmlNodeList)enclosedObject, true);
                    break;

                //				case "System.Xml.XPath.XPathNodeList":
                //					XmlHelper.AddNodes(newNode, (System.Xml.XPath.XPathNodeList)enclosedObject, true);
                //					break;

                default:
                    break;
            }
        }

        private XmlNode GetCopyParentNode(XmlNode workNode, string copyXPath)
        {
            if (copyXPath == null)
            {
                return workNode;
            }
            else
            {
                try
                {
                    XmlNodeList nodes = NameSpaceHelper.GetNodes(workNode, copyXPath);

                    return nodes[0];
                }
                catch (Exception)
                {
                    return workNode;
                }
            }
        }

        private bool IsTargetContainer(XmlNodeType targetNodeType)
        {
            return targetNodeType == XmlNodeType.CDATA || targetNodeType == XmlNodeType.Element || targetNodeType == XmlNodeType.Attribute || targetNodeType == XmlNodeType.Entity;
        }

        private void ResolveExtraInformation(XmlNode workNode, XmlNode newNode)
        {
            if (_addExtra)
            {
                XmlNode extraNode = XmlHelper.CreateNode(Document, _extraNodeType, _extraNodeName, "");

                //				XmlNodeList newNodes = NameSpaceHelper.GetNodes(workNode, _extraLocation);

                //				XmlNode refNode = newNodes.Count > 0 ? newNodes[0] : null;

                if (_extraNodeType == XmlNodeType.Attribute)
                {
                    XmlAttribute extraAttribute = (XmlAttribute)extraNode;

                    extraAttribute.Value = _extraNodeValue;

                    XmlHelper.AddSingleAttribute((XmlElement)newNode, extraAttribute);
                }
                else if (_extraNodeType == XmlNodeType.Element)
                {
                    XmlElement extraElement = (XmlElement)extraNode;

                    extraElement.InnerText = _extraNodeValue;

                    //Kiet - Aug 4, 2011 - Add after
                    XmlHelper.AddSingleNodeAt(newNode, extraElement, null, GetCopyAfter(Transform.Action.Extra));
                }
            }
        }

        private bool ObjectsTypesCompatible(object copySourceObject, XmlNodeType targetNodeType)
        {
            string copiedType = copySourceObject.GetType().FullName;

            if (targetNodeType == XmlNodeType.Attribute || targetNodeType == XmlNodeType.CDATA)
            {
                return (copiedType == "System.Xml.XmlText" || copiedType == "System.String");
            }
            else if (targetNodeType == XmlNodeType.Element)
            {
                return (copiedType != "System.Xml.XmlEntity" &&
                    copiedType != "System.Xml.XmlDeclaration" &&
                    copiedType != "System.Xml.XmlDocument" &&
                    copiedType != "System.Xml.XmlDocumentType");
            }
            else if (targetNodeType == XmlNodeType.Entity)
            {
                return copiedType == "System.String";
            }
            else
            {
                return false;
            }
        }

        private XmlNode ResolveCreateNode(XmlNode workNode, string copyTargetXPath, XmlNodeType newNodeType, string copyName, ref bool targetExists)
        {
            XmlNodeList existingNodes = NameSpaceHelper.GetNodes(workNode, copyTargetXPath);

            targetExists = existingNodes.Count > 0;

            if (targetExists)
            {
                XmlNode existingNode = existingNodes[0];

                if (existingNode.HasChildNodes)
                {
                    XmlHelper.RemoveChildNodes(existingNode, false);
                }

                return existingNode;
            }
            else
            {
                //return XmlHelper.CreateNode(Document, newNodeType, copyName, "");

                //return XmlHelper.CreateNode(Document, newNodeType, copyName, "", workNode.Prefix, workNode.NamespaceURI);

                // 2011-Mar-30 - rectify order of last 2 arguments: first URI, then Prefix are expected (useful in the case of default namespace)
                return XmlHelper.CreateNode(Document, newNodeType, copyName, "", workNode.NamespaceURI, workNode.Prefix);
            }
        }

    }
}
