using System;
using System.Xml;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XmlNormalizer
{
    class Processor
    {
        private Initialiser _init;

        private string _inputFile;
        private Encoding _encoding;
        private Usage _usage;
        private FileTypeConfig _fileTypeConfig;

        public Processor(Initialiser init)
        {
            _init = init;
            _inputFile = (string)_init.GetArgumentValue(Strings.INPUT_FILE);
            _encoding = Encoding.GetEncoding((string)_init.GetArgumentValue(Strings.ENCODING));
            _usage = (Usage)Enum.Parse(typeof(Usage), (string)_init.GetArgumentValue(Strings.USAGE), true);
            _fileTypeConfig = (FileTypeConfig)_init.AdditionalConfigs[Strings.FILE_TYPES_CONFIG];
        }

        public bool Run()
        { 
            FileType thisFileType = GetFileType();

            if (thisFileType != null)
            {
                ReaderWriter readerWriter = new ReaderWriter(_inputFile, _encoding, _usage, thisFileType);

                if (_usage == Usage.forward || _usage == Usage.single)
                {
                    return readerWriter.Resolve() && readerWriter.SaveForward();
                }
                else 
                {
                    return readerWriter.Cleanup() && readerWriter.SaveBackward();
                }

            }

            return true;    // file type out of supported scope 
        }

        private FileType GetFileType()
        {
            string ext = Path.GetExtension(_inputFile).ToLower();

            string docType = GetDocType();

            if (String.IsNullOrEmpty(docType))
            {
                return null;
            }

            foreach (FileType thisFileType in _fileTypeConfig.FileTypes.Values)
            {
                if (thisFileType.Filter.Contains(ext) && thisFileType.DocTypes.Contains(docType))
                {
                    return thisFileType;
                }
            }

            return null;
        }

        private string GetDocType()
        {
            bool readFirstElement = false;

            try
            {
                using (XmlTextReader xr = new XmlTextReader(_inputFile))
                {
                    xr.XmlResolver = null;

                    xr.WhitespaceHandling = WhitespaceHandling.All;

                    while (xr.Read())
                    {
                        switch (xr.NodeType)
                        {
                            case XmlNodeType.DocumentType:
                                return xr.Name;

                            case XmlNodeType.Document:
                                return xr.Name;

                            case XmlNodeType.Element:
                                if (!readFirstElement)
                                {
                                    return xr.Name;
                                }
                                readFirstElement = true;
                                break;
                       
                            default:
                                break;
                        }
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }

        }
    }
}
