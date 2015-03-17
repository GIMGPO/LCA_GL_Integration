using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Text.RegularExpressions;

namespace XmlManipulator
{
    public enum ConvertTo { NCR, Literal, AsIs }

	class EntityManager
	{
		private const string PREFIX = "~@!";
        private const string POSTFIX = "!@~";
		private const string ENT_START = "&";
		private const string ENT_END = ";";
		private const string NAME_GROUP = "entName";

		private string _workFile;
		private Encoding _encoding;

		private Regex _entEscapeRegex;
		private Regex _entRestoreRegex;
        private Regex _predefEntRegex;
        //private Regex _genericCharacterEntityRegex;

		private MatchEvaluator _escapeMatchDelegate;
		private MatchEvaluator _restoreMatchDelegate;
        private MatchEvaluator _convertCharacterEntityDelegate;

		private bool _hasOtherEntityChanged;
        private bool _hasCharacterEntityChanged;
        private ConvertTo _convertTo;

		private Hashtable _specifiedEntities;

		public EntityManager(string workFile, Encoding encoding, Hashtable specifiedEntities)
		{
			_encoding = encoding;
			_workFile = workFile;

            string entPattern = string.Format(@"(?'{0}'(?<!\#[xX]?)[a-zA-Z_0-9\-\.\:]+)", NAME_GROUP);

			_entEscapeRegex = new Regex(string.Format(@"{0}{1}{2}", ENT_START, entPattern, ENT_END), RegexOptions.Compiled);
			_entRestoreRegex = new Regex(string.Format(@"{0}{1}{2}", PREFIX, entPattern, POSTFIX), RegexOptions.Compiled);
			_predefEntRegex = new Regex(@"&(amp|apos|quot|lt|gt);");
			//_predefEntRegex = new Regex(@"&(amp(?!;[a-zA-Z_0-9\-\.\:]+;)|apos|quot|lt|gt);");

			_escapeMatchDelegate = new MatchEvaluator(EscapeMatchHandler);
			_restoreMatchDelegate = new MatchEvaluator(RestoreMatchHandler);
            _convertCharacterEntityDelegate = new MatchEvaluator(ConvertCharacterEntityHandler);

			_hasOtherEntityChanged = false;
            _hasCharacterEntityChanged = false;

			_specifiedEntities = specifiedEntities;
        }

        #region 1. HTML Character Entities + predefined Entities: Resolve to NCR (&#130;), literal (é), or leave as is (&eacute;)
        public bool ConvertHtmlCharacterEntities(ConvertTo convertTo)
        {
            _convertTo = convertTo;

            string convertedEntitiesFileString = String.Empty;

            using (StreamReader sr = new StreamReader(_workFile, _encoding))
            {
                string fileString = sr.ReadToEnd();

                convertedEntitiesFileString = _entEscapeRegex.Replace(fileString, _convertCharacterEntityDelegate);
            }

            if (convertedEntitiesFileString != string.Empty)
            {
                using (StreamWriter sw = new StreamWriter(_workFile, false, _encoding))
                {
                    sw.Write(convertedEntitiesFileString);
                }
            }

            return _hasCharacterEntityChanged;
        }

        public string ConvertCharacterEntityHandler(Match match)
        {
            try
            {
                if (_predefEntRegex.IsMatch(match.Value))
                {
                    return match.Value;
                }

                if (match.Groups[NAME_GROUP] != null && 
                    _specifiedEntities != null && 
                    _specifiedEntities[match.Groups[NAME_GROUP].Value] != null)
                {
                    switch (_convertTo)
                    {
                        case ConvertTo.NCR:
                            _hasCharacterEntityChanged = true;
                            return string.Format("&#{0};", Convert.ToString(_specifiedEntities[match.Groups[NAME_GROUP].Value]));

                        case ConvertTo.Literal:
                            _hasCharacterEntityChanged = true;
                            int charPoint = Convert.ToInt32(_specifiedEntities[match.Groups[NAME_GROUP].Value]);
                            if (charPoint <= 255)
                            {
                                byte[] myByte = new byte[] { (byte)charPoint };
                                return Encoding.Default.GetString(myByte);
                            }
                            else
                            {
                                char newChar = (char)charPoint;
                                return Convert.ToString(newChar);
                            }

                        case ConvertTo.AsIs:
                        default:
                            return match.Value;
                    }
                }
                else
                {
                    return match.Value;
                }
            }
            catch
            {
                return match.Value;
            }
        }

        public bool HasCharacterEntityChanged
        {
            get { return _hasCharacterEntityChanged; }
        }
        #endregion


        #region 2. Other entities: escape &xxx; to ~@!xxx!@~, run XML transform, revert ~@!xxx!@~ to &xxx;
        // 1. Escape &xxx; to ~@!xxx!@~
        public bool EscapeEntities()
		{
			string escapedFileString = String.Empty;

			using (StreamReader sr = new StreamReader(_workFile, _encoding))
			{
				string fileString = sr.ReadToEnd();

				escapedFileString = _entEscapeRegex.Replace(fileString, _escapeMatchDelegate);
			}

            if (escapedFileString != string.Empty)
            {
                using (StreamWriter sw = new StreamWriter(_workFile, false, _encoding))
			    {
					sw.Write(escapedFileString);
			    }
            }

			return _hasOtherEntityChanged;
		}

		private string EscapeMatchHandler(Match match)
		{
			if (_predefEntRegex.IsMatch(match.Value))
			{
				return match.Value;
			}
			else if (_specifiedEntities != null && _specifiedEntities[match.Groups[NAME_GROUP].Value] != null)
			{
				_hasOtherEntityChanged = true;

				return string.Format("{0}{1}{2}", PREFIX, Convert.ToString(_specifiedEntities[match.Groups[NAME_GROUP].Value]), POSTFIX);
			}
			else
			{
				_hasOtherEntityChanged = true;

				return string.Format("{0}{1}{2}", PREFIX, match.Groups[NAME_GROUP].Value, POSTFIX);
			}
		}

        // 2. Revert ~@!xxx!@~ to &xxx;
		public void RestoreEntities()
		{
			string restoredFileString = String.Empty;

			using (StreamReader sr = new StreamReader(_workFile, _encoding))
			{
				string fileString = sr.ReadToEnd();

				restoredFileString = _entRestoreRegex.Replace(fileString, _restoreMatchDelegate);
			}

            if (restoredFileString != string.Empty)
            {
			    using (StreamWriter sw = new StreamWriter(_workFile, false, _encoding))
			    {
					    sw.Write(restoredFileString);
			    }
            }
        }

		private string RestoreMatchHandler(Match match)
		{
            _hasOtherEntityChanged = true;

            return string.Format("{0}{1}{2}", ENT_START, match.Groups[NAME_GROUP].Value, ENT_END);
		}

		public bool HasOtherEntityChanged
		{
			get { return _hasOtherEntityChanged; }
        }
        #endregion
	}
}
