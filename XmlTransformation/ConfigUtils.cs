using System;
using System.Xml;
using System.Collections;
using System.Text;
using System.Text.RegularExpressions;

namespace XmlTransformation
{
	public class ConfigUtils
	{
		private const string NAME_GROUP = "name";

		private XmlDocument _configDoc;
		private Regex _valueMarker;


		public ConfigUtils(XmlDocument configDoc)
		{
			_configDoc = configDoc;

			string valueMarkerPattern = @"\{(?'" + NAME_GROUP + @"'[^\}]+)\}";

			_valueMarker = new Regex(valueMarkerPattern, RegexOptions.IgnoreCase);
		}

		public string GetXPathValue(string xpathSpec, Hashtable values)
		{
			return GetXPathValue(_configDoc, xpathSpec, values);
		}

		public string GetXPathValue(XmlDocument doc, string xpathSpec, Hashtable values)
		{
			string xpath = ResolveXPath(xpathSpec, values);

			XmlNode node = _configDoc.SelectSingleNode(xpath);
			
			if (node != null)
			{
				switch (node.NodeType)
				{
					case XmlNodeType.Attribute:
						return node.Value;

					case XmlNodeType.Element:
						return ((XmlElement)node).InnerText;
					
					default:
						return node.Value;
				}
			}

			return null;
		}

		public XmlNode GetConfigNode(string xpath)
		{
			return _configDoc.SelectSingleNode(xpath);
		}

		public XmlNodeList GetConfigNodes(string xpath)
		{
			return _configDoc.SelectNodes(xpath);
		}

		public string ResolveXPathMulti(string xpathSpec, Hashtable values, string valuePattern, string separator)
		{
			return Resolve(xpathSpec, values, valuePattern, separator);
		}

		public string ResolveXPath(string xpathSpec, Hashtable values)
		{
			return Resolve(xpathSpec, values, null, null);
		}
			
		private string Resolve(string xpathSpec, Hashtable values, string valuePattern, string separator)
		{
			MatchCollection matches = _valueMarker.Matches(xpathSpec);

			StringBuilder sb = new StringBuilder();

			int last = 0;
 
			for (int i = 0; i < matches.Count; ++i)
			{
				Match match = matches[i];

				sb.Append(xpathSpec.Substring(last, match.Index - last));

				if (match.Groups[NAME_GROUP].Success)
				{
					string valueName = match.Groups[NAME_GROUP].Value;

					string valueContent = GetValue(valueName, match, values, valuePattern, separator);

					sb.Append(valueContent);
				}
				else
				{
					sb.Append(match.Value);
				}

				last = match.Index + match.Length;
			}

			sb.Append(last < xpathSpec.Length ? xpathSpec.Substring(last) : "");

			return sb.ToString();
		}

		private string GetValue(string valueName, Match match, Hashtable values, string valuePattern, string separator)
		{
			if (values[valueName] != null)
			{
				return Convert.ToString(values[valueName]);
			}

			if (valuePattern == null && separator == null)
			{
				XmlNode node = _configDoc.SelectSingleNode(valueName);

				if (node != null)
				{
					switch (node.NodeType)
					{
						case XmlNodeType.Element:
							return ((XmlElement)node).InnerText;
			
						default:
							return node.Value;
					}
				}
			}

			XmlNodeList nodes = _configDoc.SelectNodes(valueName);
	
			StringBuilder sb = new StringBuilder();

			foreach (XmlNode node in nodes)
			{
				switch (node.NodeType)
				{
					case XmlNodeType.Element:
						sb.Append(String.Format(valuePattern, ((XmlElement)node).InnerText));
						break;
			
					default:
						sb.Append(String.Format(valuePattern, node.Value));
						break;
				}

				sb.Append(separator);
			}

			sb.Remove(sb.Length -1, 1);

			return sb.ToString();

			//return match.Value;
		}
	}
}
