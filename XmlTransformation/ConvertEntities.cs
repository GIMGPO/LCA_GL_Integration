using System;
using System.Collections;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Web;


namespace XmlTransformation
{
	public class ConvertEntities : TransformBase
	{
		private XmlDocument _configDoc;
		private Hashtable _values;

        private const string EXTRA_DIRECTION_ATT = "direction";
        private const string INPUT_FILE = "inputFile";
        private const string DIRECTION_TO_LITERALS = "toliterals";
        private const string DIRECTION_TO_ENTITIES = "toentities";

        private string _direction = string.Empty;

        public ConvertEntities(XmlDocument doc, Transform transform, RunMode runMode, Hashtable htmlEntities, NamespaceManagerHelper nsmh, XmlDocument configDoc, Hashtable values)
            : base(doc, transform, runMode, htmlEntities, nsmh, configDoc, values)
		{
			_configDoc = configDoc;

			_values	= values;

            _direction = Transform.Action.Extra.Attributes[EXTRA_DIRECTION_ATT] == null ? string.Empty : (string)Transform.Action.Extra.Attributes[EXTRA_DIRECTION_ATT];
		}

		public override void ProcessNodes()
		{
            if (IsValidEncoding())
            {
                ArrayList sortedFoundNodes = XmlHelper.SortNodeListArrays(FoundNodes);

                for (int i = sortedFoundNodes.Count - 1; i >= 0; --i)
                {
                    XmlHelper.ListNode listNode = (XmlHelper.ListNode)sortedFoundNodes[i];

                    this.ProcessNode(listNode.Node);
                }
            }
		}//end ProcessNodes()

        public override void ProcessNode(XmlNode node)
        {
            UpdateNode(node);
        }

		private void UpdateNode(XmlNode node)
		{
            if (node.HasChildNodes)
            {
                foreach (XmlNode child in node.ChildNodes)
                {
                    switch (child.NodeType)
                    {
                        case XmlNodeType.CDATA:
                            if (_direction.Equals(DIRECTION_TO_LITERALS, StringComparison.CurrentCultureIgnoreCase))
                            {
                                child.Value = HttpUtility.HtmlDecode(child.Value);
                            }
                            else if (_direction.Equals(DIRECTION_TO_ENTITIES, StringComparison.CurrentCultureIgnoreCase))
                            {
                                child.Value = HttpUtility.HtmlEncode(child.Value);
                            }
                            break;
                        default:
                            break;
                    }
                }//end foreach
            }
		}//end UpdateNode

        private bool IsValidEncoding()
        {
            bool valid = false;

            string input = Convert.ToString(_values[INPUT_FILE]);

            Encoding encoding = GetXmlEncoding(input);

			if (encoding.GetType().FullName == Encoding.UTF8.GetType().FullName ||
			   encoding.GetType().FullName == Encoding.Unicode.GetType().FullName)
			{
                valid = true;
            }

            return valid;
        }

        private Encoding GetXmlEncoding(string xmlFile)
        {
            try
            {
                using (XmlTextReader xr = new XmlTextReader(xmlFile))
                {
                    xr.XmlResolver = null;

                    xr.WhitespaceHandling = WhitespaceHandling.All;
                    
                    xr.Read();
                    
                    return xr.Encoding;
                }
            }
            catch
            {
            }
            
            string line = null;

            using (StreamReader sr = new StreamReader(xmlFile, Encoding.UTF8))
            {
                line = sr.ReadLine();
            }

            if (line == null)
            {
                return Encoding.UTF8;
            }

            Match match = Regex.Match(line, @"encoding ?= ?['""]{1}(?'encoding'[^'""]+)['""]{1}", RegexOptions.IgnoreCase);

            if (!match.Success)
            {
                return Encoding.UTF8;
            }

            if (match.Groups["encoding"].Captures.Count == 0)
            {
                return Encoding.UTF8;
            }

            try
            {
                return Encoding.GetEncoding(match.Groups["encoding"].Value);
            }
            catch
            {
                return Encoding.UTF8;
            }

        }//end GetXmlEncoding

	}
}
