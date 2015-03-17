using System;
using System.Xml;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;

namespace XmlTransformation
{
	/// <summary>
	/// Summary description for TransformBase.
	/// </summary>
	public class TransformBase : ITransform 
	{
		private XmlDocument _doc;
		private RunMode _runMode;
		private Transform _transform;
		private XmlNodeList _nodes;
		private CharNormalizer _charNormalizer;
		private NamespaceManagerHelper _nameSpaceManagerHelper;

		private Hashtable _additionalConfig;

		public TransformBase(XmlDocument doc, Transform transform, RunMode runMode, Hashtable htmlEntities, NamespaceManagerHelper nameSpaceManagerHelper, XmlDocument configDoc, Hashtable values)
		{
			_doc = doc;

			_runMode = runMode;

			_transform = transform;

			_charNormalizer = new CharNormalizer(htmlEntities);

			_nameSpaceManagerHelper = nameSpaceManagerHelper;
		}
		
		public virtual bool Do()
		{
			Initialise();

			CollectNodes();

			ProcessNodes();

			PostProcess();

			return true;
		}

		public virtual void Initialise()
		{
		}

		public virtual void CollectNodes()
		{
			_nodes = NameSpaceHelper.GetNodes(Transform.SearchXPath);

			//TODO: implement reverse sorting here
		}

		public virtual void ProcessNodes()
		{
			foreach (XmlNode node in _nodes)
			{
				ProcessNode(node);
			}
		}

		public virtual void ProcessNode(XmlNode node)
		{
		}

		public virtual void PostProcess()
		{
		}

		public void AddConfigXml(string configName, XmlElement xml)
		{
			AdditionalConfig[configName] = xml;
		}
		
		public XmlDocument Document
		{
			get { return _doc; }
			set { _doc = value; }
		}

		public RunMode Mode
		{
			get { return _runMode; }
		}

		public Transform Transform
		{
			get { return _transform; }
		}

		public XmlNodeList FoundNodes
		{
			get { return _nodes; }
			set { _nodes = value; }
		}

		public NamespaceManagerHelper NameSpaceHelper
		{
			get { return _nameSpaceManagerHelper == null ? _nameSpaceManagerHelper = new NamespaceManagerHelper(Document) : _nameSpaceManagerHelper; }
		}

		public CharNormalizer Normalizer
		{
			get { return _charNormalizer; }
		}

		public Hashtable AdditionalConfig
		{
			get { return _additionalConfig == null ? _additionalConfig = new Hashtable() : _additionalConfig; }	
			set { _additionalConfig = value; }
		}
	}
}
