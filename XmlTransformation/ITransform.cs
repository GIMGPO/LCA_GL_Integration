using System;
using System.Xml;

namespace XmlTransformation
{
	public interface ITransform
	{
		bool Do();

		void AddConfigXml(string name, XmlElement xml);
	}
}

