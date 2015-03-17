using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace XmlNormalizer
{
    class ReaderWriter
    {
        private const string SHORT_TAG_VALUE_ATT = "v";

		private const string ESCAPE_PATTERN = @"\u{0:x4}";
		private const string ESCAPE_NOTATION = "X4";
		private const string UNESCAPE_VALUE_GROUP = "hex";
		private const string UNESCAPE_PATTERN = @"\\u(?'hex'[0-9a-fA-F]{4})";

        private string _inputFile;
        private Encoding _encoding;
        private Usage _usage;

        private string _workFile;
        private ArrayList _preserveSpaceElements;
        private string _curPreserveSpaceElement;
        private Regex _whitespaceRegex;
        private string _whitespaceReplace;
        private bool _inPreserveSpace;
        private int _readLevel;
        private int _curPreserveLevel;

        private bool _addLeadingLinebreak;
        private bool _addTrailingLinebreak;
        private string _linebreak;
        private ArrayList _layoutLinebreakElements;

        private bool _hasInlineTagging;
        private Regex _inlineTaggingRegex;
		private bool _hasEscapes;
		private string _escapes;

		private Regex _unescapeRegex; 
		private MatchEvaluator _unescapeHandler;


        public ReaderWriter(string inputFile, Encoding encoding, Usage usage, FileType fileType)
        {
            _inputFile = inputFile;

            _workFile = Path.ChangeExtension(inputFile, ".wrk");

            if (encoding.WebName.ToLower() == "utf-8" && fileType.OmitUtf8BOM)
            {
                _encoding = new UTF8Encoding(false);
            }
            else
            {
                _encoding = encoding;
            }

            _usage = usage;

            _preserveSpaceElements = fileType.XmlSpacePreserveScope;

            _whitespaceRegex = fileType.WhiteSpaceRegex;

            _whitespaceReplace = fileType.WhiteSpaceReplace;

            _addLeadingLinebreak = fileType.HasCosmeticLinebreaks && (fileType.Location == LbLocation.leading || fileType.Location == LbLocation.both);

            _addTrailingLinebreak = fileType.HasCosmeticLinebreaks && (fileType.Location == LbLocation.trailing || fileType.Location == LbLocation.both);

            _linebreak = fileType.CosmeticLinebreak;

            _layoutLinebreakElements = fileType.CosmeticLinebreakElements;

            _hasInlineTagging = fileType.HasInlineTagging;

            _inlineTaggingRegex = _hasInlineTagging ? fileType.InlineTaggingRegex : null;

			_hasEscapes = fileType.HasEscapes;

			if (_hasEscapes)
			{
				_escapes = fileType.Escapes;

				_unescapeRegex = new Regex(UNESCAPE_PATTERN);

				_unescapeHandler = new MatchEvaluator(ReplaceEscape);
			}

            _inPreserveSpace = false;

            _readLevel = 0;
            
            _curPreserveLevel = 0;
        }

        public bool SaveForward()
        {
            return Save(_inputFile);
        }


        public bool SaveBackward()
        {
            if (_hasInlineTagging || _addLeadingLinebreak || _addTrailingLinebreak)
            {
                return Save(_inputFile);
            }
            else
            {
                return true;
            }
        }

        private bool Save(string targetFile)
        {
            try
            {
                string backupFile = (string.Format("{0}.bak", _inputFile));

                File.Move(_inputFile, backupFile);

                File.Delete(_inputFile);

                File.Move(_workFile, _inputFile);

                File.Delete(backupFile);

                return true;
            }
            catch (Exception e)
            {
                new BrokerException(string.Format("Work file could not be saved. Error:\n{0}", e.Message));

                return false;
            }
        }

        public bool Cleanup()
        {
            if (_hasInlineTagging || _addLeadingLinebreak || _addTrailingLinebreak)
            {
                using (XmlTextReader xr = new XmlTextReader(_inputFile))
                {
                    xr.XmlResolver = null;

                    xr.WhitespaceHandling = WhitespaceHandling.All;

                    using (XmlTextWriter xw = new XmlTextWriter(_workFile, _encoding))
                    {
                        try
                        {
                            xw.WriteStartDocument();

                            while (ReadNextNode(xr))
                            {
                                Evaluate(xr);

                                WriteBackward(xr, xw);
                            }

                            xw.WriteEndDocument();

                            return true;
                        }
                        catch (Exception e)
                        {
                            new BrokerException(string.Format("An error occurred during processing. Message:\n{0}", e.Message));

                            return false;
                        }
                    }
                } 
            }

            return true;
        }

        #region XmlTextReader & XmlTextWriter
        public bool Resolve()
        {
            using (XmlTextReader xr = new XmlTextReader(_inputFile))
            {
                xr.XmlResolver = null;

                xr.WhitespaceHandling = WhitespaceHandling.All;

                using (XmlTextWriter xw = new XmlTextWriter(_workFile, _encoding))
                {
                    try
                    {
                        xw.WriteStartDocument();

                        while (ReadNextNode(xr))
                        {
                            Evaluate(xr);

                            WriteForward(xr, xw);
                        }

                        xw.WriteEndDocument();

                        return true;
                    }
                    catch (Exception e)
                    {
                        new BrokerException(string.Format("An error occurred during processing. Message:\n{0}", e.Message));

                        return false;
                    }
                }
            }
        }

        private void Evaluate(XmlTextReader xr)
        {
            try
            {
                switch (xr.NodeType)
                {
                    case XmlNodeType.Element:
                        TestOpenPreserveElement(xr);
                        break;

                    case XmlNodeType.EndElement:
                        TestClosePreserveElement(xr);
                        break;

                    default:
                        break;
                }
            }
            catch (Exception e)
            { 
                throw new Exception(String.Format("An error ocurred while evaluating an Xml node. Line: {0}, Position: {1}, Cause:\n{2}", 
                    xr.LineNumber.ToString(),
                    xr.LinePosition.ToString(),
                    e.Message));
            }
        }

        private void WriteBackward(XmlTextReader xr, XmlTextWriter xw)
        {
            try
            {
                switch (xr.NodeType)
                {
                    case XmlNodeType.Text:
                        xw.WriteString(xr.Value);
                        break;

                    case XmlNodeType.Document:
                    case XmlNodeType.Element:
                        WriteStartOrEmptyElementBackward(xr, xw);
                        break;

                    case XmlNodeType.EndElement:
                        WriteFullEndElementBackward(xr, xw);
                        break;

                    default:
                        WriteOtherXmlnodes(xr, xw);
                        break;
                }
            }
            catch (XmlException xe)
            {
                throw new Exception(String.Format("An XML error ocurred while attempting to write a node to the output stream. Line: {0}, Position: {1}, Node type: {2}, Cause:\n{3}",
                   xr.LineNumber.ToString(),
                   xr.LinePosition.ToString(),
                   xr.NodeType.ToString(),
                   xe.Message));
            }
            catch (Exception e)
            {
                throw new Exception(String.Format("An error ocurred while attempting to write an Xml node to the output stream. Line: {0}, Position: {1}, Cause:\n{2}",
                   xr.LineNumber.ToString(),
                   xr.LinePosition.ToString(),
                   e.Message));
            }
        }

        private void WriteForward(XmlTextReader xr, XmlTextWriter xw)
        {
            try
            {
                switch (xr.NodeType)
                {
                    case XmlNodeType.Text:
                        if (InPreserveSpace())
                        {
                            WritePreserveSpaceText(xr, xw);
                        }
                        else 
                        {
                            WriteNonPreserveSpaceText(xr, xw);
                        }
                        break;
                   
                    case XmlNodeType.Document:
                    case XmlNodeType.Element:
                        WriteStartOrEmptyElementForward(xr, xw);
                        break;

                    case XmlNodeType.EndElement:
                        WriteFullEndElementForward(xr, xw);
                        break;

                    default:
                        WriteOtherXmlnodes(xr, xw);
                        break;
                }
            }
            catch (XmlException xe)
            {
                throw new Exception(String.Format("An XML error ocurred while attempting to write a node to the output stream. Line: {0}, Position: {1}, Node type: {2}, Cause:\n{3}",
                   xr.LineNumber.ToString(),
                   xr.LinePosition.ToString(),
                   xr.NodeType.ToString(),
                   xe.Message));
            }
            catch (Exception e)
            {
                throw new Exception(String.Format("An error ocurred while attempting to write an Xml node to the output stream. Line: {0}, Position: {1}, Cause:\n{2}",
                   xr.LineNumber.ToString(),
                   xr.LinePosition.ToString(),
                   e.Message));
            }
        }

        private void WriteOtherXmlnodes(XmlTextReader xr, XmlTextWriter xw)
        {
            try
            {
                switch (xr.NodeType)
                {
                    case XmlNodeType.SignificantWhitespace:
                    case XmlNodeType.Whitespace:
                        if (InPreserveSpace() || IsValueSingleSpace(xr))
                        {
                            xw.WriteWhitespace(xr.Value);
                        }
                        break;

                    case XmlNodeType.CDATA:
                        xw.WriteCData(xr.Value);
                        break;

                    case XmlNodeType.Comment:
                        xw.WriteComment(xr.Value);
                        break;

                    case XmlNodeType.DocumentType:
                        string pubid = xr.GetAttribute("PUBLIC");
                        string sysid = xr.GetAttribute("SYSTEM");
                        xw.WriteDocType(xr.Name, pubid, sysid, xr.Value);
                        break;

                    case XmlNodeType.Entity:
                        xw.WriteString(String.Empty);   // should not happen: entity declarations are in the DOCTYPE declaration
                        break;

                    case XmlNodeType.EndEntity:         // should not happen: we don't call ResolveEntity() to replace the entity ref with the value
                        xw.WriteString(String.Empty);
                        break;

                    case XmlNodeType.EntityReference:
                        xw.WriteEntityRef(xr.Name);
                        break;

                    case XmlNodeType.Notation:
                        xw.WriteString(xr.Value);
                        break;

                    case XmlNodeType.ProcessingInstruction:
                        xw.WriteProcessingInstruction(xr.Name, xr.Value);
                        break;

                    case XmlNodeType.XmlDeclaration:    // already written the XML Declaration
                    default:
                        break;
                }
            }
            catch (XmlException xe)
            {
                throw new Exception(String.Format("An XML error ocurred while attempting to write a node to the output stream. Line: {0}, Position: {1}, Node type: {2}, Cause:\n{3}",
                   xr.LineNumber.ToString(),
                   xr.LinePosition.ToString(),
                   xr.NodeType.ToString(),
                   xe.Message));
            }
            catch (Exception e)
            {
                throw new Exception(String.Format("An error ocurred while attempting to write an Xml node to the output stream. Line: {0}, Position: {1}, Cause:\n{2}",
                   xr.LineNumber.ToString(),
                   xr.LinePosition.ToString(),
                   e.Message));
            }
        }

        private void WriteNonPreserveSpaceText(XmlTextReader xr, XmlTextWriter xw)
        {
            xw.WriteString(_whitespaceRegex.Replace(xr.Value, _whitespaceReplace));
        }

        private void WritePreserveSpaceText(XmlTextReader xr, XmlTextWriter xw)
        {
            if (_hasInlineTagging)
            {
                string value = xr.Value;

                MatchCollection matches = _inlineTaggingRegex.Matches(value);

                if (matches.Count == 0)
                {
                    xw.WriteString(xr.Value);
                }
                else
                {
                    int stringStart = 0;
                    int matchIndex;
                    int matchLength;
                    string matchValue;
                    Tag tag;

                    foreach (Match match in matches)
                    {
                        GetCapture(match, out matchIndex, out matchLength, out matchValue, out tag);

						matchValue = Escape(matchValue);

                        xw.WriteString(value.Substring(stringStart, matchIndex - (stringStart)));

                        switch (tag)
                        {
                            case Tag.sM:
                                xw.WriteStartElement(tag.ToString());
                                xw.WriteEndElement();
                                break;

                            case Tag.sCI:
                            case Tag.sCE:
                                xw.WriteStartElement(tag.ToString());
                                xw.WriteAttributeString(SHORT_TAG_VALUE_ATT, matchValue);
                                xw.WriteEndElement();
                                break;

                            case Tag.sOI:
                            case Tag.sOE:
                                xw.WriteStartElement(tag.ToString());
                                xw.WriteString(matchValue);
                                xw.WriteEndElement();
                                break;

                            default:
                                xw.WriteString(matchValue); 
                                break;
                        }

                        stringStart = matchIndex + matchLength;
                    }

                    xw.WriteString(value.Length > stringStart ? value.Substring(stringStart) : String.Empty);
                }
            }
            else
            {
                xw.WriteString(xr.Value);
            }
        }

        private bool IsValueSingleSpace(XmlTextReader xr)
        {
            return xr.Value == " ";
        }

        private void GetCapture(Match match, out int matchIndex, out int matchLength, out string matchValue, out Tag tag)
        {
            if (match.Groups[Tag.sCI.ToString()] != null && match.Groups[Tag.sCI.ToString()].Value != String.Empty)
            {
                tag = Tag.sCI;
            }
            else if (match.Groups[Tag.sOI.ToString()] != null && match.Groups[Tag.sOI.ToString()].Value != String.Empty)
            {
                tag = Tag.sOI;
            }
            else if (match.Groups[Tag.sCE.ToString()] != null && match.Groups[Tag.sCE.ToString()].Value != String.Empty)
            {
                tag = Tag.sCE;
            }
            else if (match.Groups[Tag.sOE.ToString()] != null && match.Groups[Tag.sOE.ToString()].Value != String.Empty)
            {
                tag = Tag.sOE;
            }
            else if (match.Groups[Tag.sM.ToString()].Success)
            {
                tag = Tag.sM;
            }
            else
            {
                tag = Tag.unknown;
            }

            matchIndex = (tag == Tag.unknown) ? match.Index : match.Groups[tag.ToString()].Index;
            matchLength = (tag == Tag.unknown) ? match.Length : match.Groups[tag.ToString()].Length;
            matchValue = (tag == Tag.unknown) ? match.Value : match.Groups[tag.ToString()].Value;
        }
     
        private void EvaluateWriteCosmeticLinebreak(string elementName, bool start, XmlTextReader xr, XmlTextWriter xw)
        {
            if (! InPreserveSpace() && 
                (start ? _addLeadingLinebreak : _addTrailingLinebreak) && 
                _layoutLinebreakElements.Contains(elementName) && 
                xr.ReadState == ReadState.Interactive)
            {
                try
                {
                    xw.WriteWhitespace(_linebreak);
                }
                catch {}
            }
        }

		private string Escape(string matchValue)
		{
			string escapedMatchValue = matchValue;

			if (_hasEscapes)
			{
				foreach (char escapeChar in _escapes)
				{
					string escapeString = System.Char.ConvertFromUtf32(escapeChar);

					int escapeCodePoint = (int)escapeChar;
		
					string escapeSequence = string.Format(string.Format(ESCAPE_PATTERN, escapeCodePoint.ToString(ESCAPE_NOTATION)));
					
					escapedMatchValue = escapedMatchValue.Replace(escapeString, escapeSequence);
				}
			}

			return escapedMatchValue;
		}

		private string Unescape(string matchValue)
		{
			string unescapedMatchValue = matchValue;

			if (_hasEscapes)
			{
				unescapedMatchValue = _unescapeRegex.Replace(unescapedMatchValue, _unescapeHandler);
			}

			return unescapedMatchValue;
		}

		private string ReplaceEscape(Match match)
		{
			string valueGroupValue = match.Groups[UNESCAPE_VALUE_GROUP].Value;

			int codePoint = Convert.ToInt32(valueGroupValue, 16);
	
			return System.Char.ConvertFromUtf32(codePoint);
		}


        private bool NotInlineTag(XmlTextReader xr)
        {
            return (xr.Name != Tag.sOE.ToString() && 
                    xr.Name != Tag.sOI.ToString() && 
                    xr.Name != Tag.sCE.ToString() && 
                    xr.Name != Tag.sCI.ToString() && 
                    xr.Name != Tag.sM.ToString());
        }

        private void WriteStartOrEmptyElementBackward(XmlTextReader xr, XmlTextWriter xw)
        {
            if (NotInlineTag(xr))
            {
                EvaluateWriteCosmeticLinebreak(xr.Name, true, xr, xw);

                xw.WriteStartElement(xr.Name);

                if (xr.HasAttributes)
                {
                    xw.WriteAttributes(xr, true);
                }

                if (xr.IsEmptyElement)
                {
                    xw.WriteEndElement();

                    EvaluateWriteCosmeticLinebreak(xr.Name, false, xr, xw);
                }
            }
            else
            {
                if (xr.Name == Tag.sCI.ToString() || xr.Name == Tag.sCE.ToString())
                {
                    if (xr.MoveToAttribute(SHORT_TAG_VALUE_ATT))
                    {
						string unescapedValue = Unescape(xr.Value);

						xw.WriteString(unescapedValue);

                        xr.MoveToElement();
                    }
                }
            }
        }

        private void WriteFullEndElementForward(XmlTextReader xr, XmlTextWriter xw)
        {
            xw.WriteFullEndElement();
        }

        private void WriteStartOrEmptyElementForward(XmlTextReader xr, XmlTextWriter xw)
        {
            xw.WriteStartElement(xr.Name);

            if (xr.HasAttributes)
            {
                // TODO: fix linebreaks in attribute values here
                xw.WriteAttributes(xr, true);
            }

            if (xr.IsEmptyElement)
            {
                xw.WriteEndElement();
            }
        }

        private void WriteFullEndElementBackward(XmlTextReader xr, XmlTextWriter xw)
        {
            if (NotInlineTag(xr))
            {
                xw.WriteFullEndElement();

                EvaluateWriteCosmeticLinebreak(xr.Name, false, xr, xw);
            }
        }

        private void TestOpenPreserveElement(XmlTextReader xr)
        {
            if (! xr.IsEmptyElement)
            {
                ++_readLevel;

                if (!InPreserveSpace() && _preserveSpaceElements.Contains(xr.Name))
                {
                    _curPreserveSpaceElement = xr.Name;

                    _curPreserveLevel = _readLevel;

                    _inPreserveSpace = true;
                }
            }
        }

        private void TestClosePreserveElement(XmlTextReader xr)
        {
            if (InPreserveSpace() && xr.Name == _curPreserveSpaceElement && _readLevel == _curPreserveLevel)
            {
                _inPreserveSpace = false;
            }

            --_readLevel;
        }
        #endregion#

        
        private bool InPreserveSpace()
        {
            return _inPreserveSpace;
        }

        private bool ReadNextNode(XmlTextReader xr)
        {
            try
            {
                return xr.Read();
            }
            catch (XmlException xmlEx)
            {
                throw new Exception(String.Format("An XML error ocurred while attempting to read the next Xml node in the input stream. Line: {0}, Position: {1}, Cause:\n{2}",
                     xr.LineNumber.ToString(),
                     xr.LinePosition.ToString(),
                     xmlEx.Message));
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("An unspecified error ocurred while attempting to read the next Xml node in the input stream. Line: {0}, Position: {1}, Cause:\n{2}",
                     xr.LineNumber.ToString(),
                     xr.LinePosition.ToString(),
                     ex.Message));
            }
        }

        #region USELESS CRAP

        //private string PrepareRawValue(string readValue, ref bool changed)
        //{
        //    string newValue = EscapeRawMarkup(readValue, changed);

        //    // section for implementing internal markup


        //    return newValue;
        //}

        //private string EscapeRawMarkup(string value, bool changed)
        //{
        //    string newValue = _xmlLiteralAmpersandLessthan.Replace(value, _evaluator);

        //    if (String.Compare(newValue, value) != 0)
        //    {
        //        changed = true;

        //        return newValue;
        //    }

        //    return value;

            //string newValue = value;

            //// try to run the match & replace as 1 operation
            //MatchCollection matches = _xmlLiteralAmpersandLessthan.Matches(newValue);

            //if (matches.Count > 0)
            //{
            //    changed = true;

            //    newValue = _xmlLiteralAmpersandLessthan.Replace(newValue, _evaluator);
            //}

        //    //return newValue;
        //}

        //private string EscapeLiteralAmpersandLessthan(Match match)
        //{
        //    switch (match.Value)
        //    {
        //        case "<":
        //            return "&lt;";
        //        case "&":
        //            return "&amp;";
        //        default:
        //            return match.Value;
        //    }
        //}

        #endregion
    }
}
