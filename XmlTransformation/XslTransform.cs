using System;
using System.Collections;
using System.IO;
using System.Xml;
using System.Xml.Xsl;

/* Kiet - April 19, 2011
 * Added to support XSLT Transformation for DITA 1.2
 * 
 */

namespace XmlTransformation
{
    public class XslTransform:TransformBase
    {
        private const string XSLT_ARGUMENT_LIST_XPATH = @"//xsltArguments/xsltArgument";
        private const string XSLT_ARGUMENT_NAME_XPATH = "name";
        private const string XSLT_ARGUMENT_TYPE_XPATH = "type";
        private const string XSLT_ARGUMENT_FROM_XPATH = "from";
        private const string BACKUP_SOURCE_XPATH = @"//xsltprocessing/backupsource/@value";
        private const string BACKUP_SOURCE_EXTENSION_XPATH = @"//xsltprocessing/backupsource/@extension";
        private const string INPUT_FILE_PARAM_NAME = @"inputFile";
        private const string XSLT_STYLESHEET_PARAM_NAME = @"xslStylesheet";
        private const string FROM_XML_SOURCE = @"xmlSource";
        private const string DOCTYPE_ELEMENT = @"DOCTYPE";
        private const string ELEMENT_NODE = @"ELEMENT";
        private const string ELEMENT_TYPE = @"elementType";
        private const string STRING_TYPE_NAME = @"System.String";
        private const string TEMP_EXTENSION = @".tmp";
        private const string DOT = @".";

        private bool _backupSource = false;
        private string _backupExtension = @".bak";
        private XmlDocument _configDoc = null;
        private string _inputFilePath = string.Empty;
        private string _xsltStylesheetFilePath = string.Empty;
        private Hashtable _values;

        public XslTransform(XmlDocument doc, Transform transform, RunMode runMode, Hashtable htmlEntities, NamespaceManagerHelper nsmh, XmlDocument configDoc, Hashtable values)
            : base(doc, transform, runMode, htmlEntities, nsmh, configDoc, values)
		{
            _configDoc = configDoc;
            _values = values;
		}

        public override void Initialise()
        {
            string withArgument = Transform.Action.With.Argument;

            if (!string.IsNullOrEmpty(withArgument))
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(withArgument);

                string nodeValue = GetNodeValue(doc, BACKUP_SOURCE_XPATH);

                if (!string.IsNullOrEmpty(nodeValue))
                {
                    _backupSource = Convert.ToBoolean(nodeValue);
                }

                nodeValue = GetNodeValue(doc, BACKUP_SOURCE_EXTENSION_XPATH);

                if (!string.IsNullOrEmpty(nodeValue))
                {
                    _backupExtension = nodeValue;
                }

            }
            
            _inputFilePath = (string)_values[INPUT_FILE_PARAM_NAME];
            _xsltStylesheetFilePath = (string)_values[XSLT_STYLESHEET_PARAM_NAME];
        }

        private string GetNodeValue(XmlDocument doc, string xpath)
        {
            XmlNode node = doc.SelectSingleNode(xpath);
            string nodeValue = GetNodeValue(node);

            if (nodeValue == null)
            {
                nodeValue = string.Empty;
            }

            return nodeValue;
        }

        private string GetNodeValue(XmlNode node)
        {
            if (node != null && node.Value != null)
            {
                return node.Value;
            } 
            
            return null;
        }

        public override void CollectNodes()
        {
        }

        public override void ProcessNodes()
        {
            TransformXsl();
        }

        public override void ProcessNode(XmlNode node)
        {
        }

        public override void PostProcess()
        {
        }

        private void TransformXsl()
        {
            if (_backupSource)
            {
                BackUpSource();
            }

            XslCompiledTransform xslTransform = new XslCompiledTransform(true);
            XsltSettings settings = new XsltSettings(true, true);
            xslTransform.Load(_xsltStylesheetFilePath, settings, null);

            XsltArgumentList xslArgs = GetXsltArgumentList();

            string tempOutputFilePath = GetNewFilename(_inputFilePath, TEMP_EXTENSION);
            FileInfo tempOutput = new FileInfo(tempOutputFilePath);
            using (FileStream fileStream = tempOutput.Open(FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                xslTransform.Transform(Document, xslArgs, fileStream);
            }

            //XmlDocument tempDoc = new XmlDocument();
            //tempDoc.XmlResolver = null;
            //tempDoc.Load(tempOutputFilePath);
            //this.Document = tempDoc;
            this.Document.XmlResolver = null;
            this.Document.Load(tempOutputFilePath);

            File.Delete(tempOutputFilePath);

        }

        private string GetNewFilename(string oldFilename, string extension)
        {
            string filename = Path.Combine(Path.GetDirectoryName(oldFilename), Path.GetFileNameWithoutExtension(oldFilename));

            if (!extension.StartsWith(DOT))
            {
                extension = DOT + extension;
            }

            return filename + extension;
        }

        private void BackUpSource()
        {
            //to be implemented
            string backupFilename = GetNewFilename(_inputFilePath, _backupExtension);

            if (File.Exists(backupFilename))
            {
                File.Delete(backupFilename);
            }

            File.Copy(_inputFilePath, backupFilename);
        }

        private XsltArgumentList GetXsltArgumentList()
        {
            XsltArgumentList argumentList = null;

            string withArgument = Transform.Action.With.Argument;

            if (!string.IsNullOrEmpty(withArgument))
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(withArgument);

                XmlNodeList nodes = doc.SelectNodes(XSLT_ARGUMENT_LIST_XPATH);
                argumentList = new XsltArgumentList();

                foreach (XmlNode node in nodes)
                {
                    string name = node.Attributes[XSLT_ARGUMENT_NAME_XPATH].Value;
                    string type = node.Attributes[XSLT_ARGUMENT_TYPE_XPATH].Value;
                    string from = node.Attributes[XSLT_ARGUMENT_FROM_XPATH].Value;
                    string elementType = node.Attributes[ELEMENT_TYPE].Value;
                    string value = node.InnerText;
                    //Namespace to be implemented
                    argumentList.AddParam(name, string.Empty, CreateParameter(type, from, elementType, value));
                }
            }

            return argumentList;
        }//End GetXsltArgumentList

        private object CreateParameter(string type, string from, string elementType, string value)
        {
            object parameter = null;
            string parameterString = null;

            if (string.IsNullOrEmpty(from))
            {
                parameterString = value;
            }
            else if(from.Equals(FROM_XML_SOURCE, StringComparison.CurrentCultureIgnoreCase))
            {
                if (elementType.Equals(DOCTYPE_ELEMENT))
                {
                    parameter = this.Document.DocumentType.OuterXml;
                }
                else if (elementType.Equals(ELEMENT_NODE))
                {
                    //to be implemented
                    //try to get the node OuterXml based on the XPATH from value
                }
            }

            if (!type.Equals(STRING_TYPE_NAME) && !string.IsNullOrEmpty(parameterString))
            {
                parameter = Convert.ChangeType(parameterString, Type.GetType(type));
            }

            return parameter;
        }
    }
}
