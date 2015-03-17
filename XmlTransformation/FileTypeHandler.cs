using System;
using System.Xml;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;

namespace XmlTransformation
{
	public enum RunMode				{ single, forward, backward, repeat } //repeat is forward without checking on flag file
	public enum Method				{ unknown, xpath, xml, regex, text, literal }
	public enum Whitespaces			{ Remove, Preserve, Indent }
	public enum Trim				{ leading, trailing, all, none }

	public class FileTypeUpdate
	{
		private static string FILTER_ATT	= "filter";
		private static string TYPE_ATT		= "type";
		private static string TRANSFORM_ELT	= "transform";
		private static string WHITESPACE_ATT = "whitespace";

		// Visible members
		public ArrayList Filter;
		public string Type;
		public Hashtable Transforms;
		public Whitespaces Whitespaces;

		public FileTypeUpdate(XmlElement fileTypeElement)
		{
			string whitespaceString = XmlHelper.GetAttValue(fileTypeElement, WHITESPACE_ATT);

			if (whitespaceString != "")
			{
				try
				{
					Whitespaces = (Whitespaces)Enum.Parse(typeof(Whitespaces), whitespaceString, true);
				}
				catch
				{
					Whitespaces = Whitespaces.Preserve;
				}
			}
			else
			{
				Whitespaces = Whitespaces.Preserve;
			}

			string filterSpec = XmlHelper.GetAttValue(fileTypeElement, FILTER_ATT);
			
			filterSpec = filterSpec.ToLower();

			string[] filterStrings = filterSpec.Split('|');

			Filter = new ArrayList();

			Filter.AddRange(filterStrings);

			Type = XmlHelper.GetAttValue(fileTypeElement, TYPE_ATT);

			Transforms = new Hashtable();
			
			XmlNodeList transformNodes = fileTypeElement.SelectNodes(TRANSFORM_ELT);

			foreach (XmlElement transformElement in transformNodes)
			{	
				Transform transform = new Transform(transformElement);

				Transforms[transform.Index] = transform;
			}
		}
	}

	public class Transform
	{
		private static string ASSEMBLY_ELT	= "assembly";
		private static string TYPE_ELT		= "type";
		private static string INDEX_ATT		= "index";
		private static string USAGE_ELT		= "usage";
		private static string SEARCH_ELT	= "search";
		private static string ACTION_ELT	= "action";

		public string Assembly;
		public string Type;
		public string Index;
		public string Usage;
		public string SearchXPath;
		public Action Action;
		
		public Transform(XmlElement transformElement)
		{
			Assembly = XmlHelper.GetChildElementValue(transformElement, ASSEMBLY_ELT);

			Type = XmlHelper.GetChildElementValue(transformElement, TYPE_ELT);

			Index = XmlHelper.GetAttValue(transformElement, INDEX_ATT);

			string usageString = XmlHelper.GetChildElementValue(transformElement, USAGE_ELT);
			
			Usage = usageString;// != "" ? (Usage)Enum.Parse(typeof(Usage), usageString) : Usage.none;

			SearchXPath = XmlHelper.GetChildElementValue(transformElement, SEARCH_ELT);

			Action = new Action((XmlElement)XmlHelper.GetChildNode(transformElement, ACTION_ELT));
		}
	}

	public class Action
	{
		private static string OBJECT		= "object";
		private static string WITH			= "with";
		private static string EXTRA			= "extra";

		public Object Target;
		public With With;
		public Extra Extra;

		public Action(XmlElement actionElement)
		{
			Target = new Object((XmlElement)XmlHelper.GetChildNode(actionElement, OBJECT));

			With = new With((XmlElement)XmlHelper.GetChildNode(actionElement, WITH));

			XmlNode extraNode = XmlHelper.GetChildNode(actionElement, EXTRA);

			if (extraNode != null)
			{
				Extra = new Extra((XmlElement)extraNode);
			}
		}
	}

	public class Extra
	{
		public Hashtable Attributes;
		
		public string Argument;

		public Extra(XmlElement extraElement)
		{
			Argument = extraElement.InnerText.Trim();

			Attributes = new Hashtable();

			foreach (XmlNode attributeNode in extraElement.Attributes)
			{
				Attributes[attributeNode.Name] = attributeNode.Value;
			}
		}
	}

	public class With
	{
		private static string NODE_TYPE = "nodeType";
		private static string TRIM = "trim";
		private static string REFERENCE = "reference";

		public string Argument;

		public string Reference;

		public Trim Trim;
		
		public XmlNodeType NodeType;

		public With(XmlElement withElement)
		{
			Argument = withElement.IsEmpty || ! withElement.HasChildNodes ? "" : withElement.InnerXml;

			string trimVal = XmlHelper.GetAttValue(withElement, TRIM);

			Trim = String.IsNullOrEmpty(trimVal) ? Trim.all : (Trim)Enum.Parse(typeof(Trim), trimVal, true);

			if (Trim != Trim.none)
			{
				Argument = Trim == Trim.all ?
					Argument.Trim() :
					Trim == Trim.leading ?
						Argument.TrimStart() :
						Argument.TrimEnd();
			}

			string nodeTypeName = XmlHelper.GetAttValue(withElement, NODE_TYPE);

			NodeType = (nodeTypeName != null && nodeTypeName != "") ? (XmlNodeType)Enum.Parse(typeof(XmlNodeType), nodeTypeName) : XmlNodeType.None;

			Reference =  XmlHelper.GetAttValue(withElement, REFERENCE);
		}
	}


	public class Object
	{
		private static string METHOD = "method";

		public Method Method;
		public string Implementation;
		private Regex _implementationRegex;
		private XmlElement _parentElement;

		public Object(XmlElement objectElement)
		{
			string methodString = XmlHelper.GetAttValue(objectElement, METHOD);
		
			Method = methodString != "" ? (Method)Enum.Parse(typeof(Method), methodString) : Method.unknown;
		
			Implementation = objectElement.InnerXml;

			_implementationRegex = Method == Method.regex ? new Regex(Implementation) : null;
		}

		public object Get(XmlNode workNode, object[] args, NamespaceManagerHelper nameSpaceHelper)
		{
			object result;

			switch (Method)
			{
				case Method.xml:
					result = UseReflection(workNode, args);
					DetermineParent(workNode, result);
					return result;
			
				case Method.xpath:
					result = UseXPath(workNode, nameSpaceHelper);

                    if (result != null)
                    {
                        DetermineParent(workNode, result);
                    }

					return result;

				case Method.regex:
					_parentElement = (XmlElement)workNode;
					return _implementationRegex.Matches(workNode.InnerXml);

				default:
					_parentElement = (XmlElement)workNode;
					return Implementation;
			}
		}

		public XmlElement ParentElement
		{
			get { return _parentElement; }
		}

		private object UseXPath(XmlNode node, NamespaceManagerHelper nameSpaceHelper)
		{
			//XmlNodeList nodes = node.SelectNodes(Implementation);
			
			XmlNodeList nodes = nameSpaceHelper.GetNodes(node, Implementation);
		
			if (nodes.Count == 0)
			{
				_parentElement = null;

				return null;
			}
			else
			{
				DetermineParent(node, nodes);

				if (nodes.Count == 1)
				{
					return nodes[0];
				}
				else
				{
					return nodes;
				}
			}		
		}

		private object UseReflection(XmlNode workNode, object[] args)
		{
			object result = null;

			if (Implementation.EndsWith(")"))
			{
				result = workNode.GetType().InvokeMember(Implementation, System.Reflection.BindingFlags.InvokeMethod, null, workNode, args);
			}
			else
			{
				result = workNode.GetType().InvokeMember(Implementation, System.Reflection.BindingFlags.GetProperty, null, workNode, null);
			}

			return result;
		}

		private void DetermineParent(XmlNode workNode, object result)
		{
			switch (result.GetType().FullName)
			{
				case "System.Xml.XmlNode":
					SetParentElement((XmlNode)result);
					break;
				
				case "System.Xml.XmlNodeList":
					//case "System.Xml.XmlNodeList":
					XmlNodeList nodes = (XmlNodeList)result;
					if (nodes.Count > 0)
					{
						SetParentElement(nodes[0]);
					}
					break;

				case "System.String":
					switch (workNode.NodeType)
					{
						case XmlNodeType.Element:
							_parentElement = (XmlElement)workNode;
							break;
						case XmlNodeType.Attribute:
							_parentElement = null;
						break;
						default:
							break;
					}
					break;
			
				default:
					break;
			}
		}

		private void SetParentElement(XmlNode node)
		{
			switch (node.NodeType)
			{
				case XmlNodeType.Element:
				case XmlNodeType.Text:
				case XmlNodeType.Comment:
				case XmlNodeType.EntityReference:
				case XmlNodeType.ProcessingInstruction:
					_parentElement = (XmlElement)node.ParentNode;
					break;
				default:
					_parentElement = null;
					break;
			}
		}
	}
}
