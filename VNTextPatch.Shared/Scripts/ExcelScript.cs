using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using VNTextPatch.Shared.Util;
using MsExcel = Microsoft.Office.Interop.Excel;

namespace VNTextPatch.Shared.Scripts
{
    public class ExcelScript : IScript, ILineStatistics
    {
        private readonly ExcelScriptCollection _collection;
        private readonly MsExcel.Workbook _workbook;
        private object[,] _values;

        public ExcelScript(ExcelScriptCollection collection, MsExcel.Workbook workbook)
        {
            _collection = collection;
            _workbook = workbook;
        }

        public string Extension
        {
            get { return null; }
        }

        public void Load(ScriptLocation location)
        {
            if (location.Collection != _collection)
                throw new InvalidOperationException();

            MsExcel.Worksheet sheet = (MsExcel.Worksheet)_workbook.Worksheets[location.ScriptName];
            _values = (object[,])sheet.UsedRange.Value;
        }

        public IEnumerable<ScriptString> GetStrings()
        {
            int lastRow = _values.GetUpperBound(0);
            for (int rowNumber = 2; rowNumber <= lastRow; rowNumber++)
            {
                string characterNames = StringUtil.NullIfEmpty(_values[rowNumber, (int)ExcelColumn.TranslatedCharacter] as string) ??
                                        StringUtil.NullIfEmpty(_values[rowNumber, (int)ExcelColumn.OriginalCharacter] as string);
                if (characterNames != null)
                {
                    foreach (string characterName in SplitNames(characterNames))
                    {
                        yield return new ScriptString(characterName, ScriptStringType.CharacterName);
                    }
                }

                string text = GetText(rowNumber);
                if (text != null)
                {
                    text = Regex.Replace(text, @"(?<!\r)\n", "\r\n");
                    yield return new ScriptString(text, ScriptStringType.Message);
                }
            }
        }

        private string GetText(int rowNumber)
        {
            string originalText = StringUtil.NullIfEmpty(_values[rowNumber, (int)ExcelColumn.OriginalLine] as string);
            if (originalText != null)
                Total++;

            string translatedText = StringUtil.NullIfEmpty(_values[rowNumber, (int)ExcelColumn.TranslatedLine] as string);
            if (translatedText != null)
                Translated++;

            string checkedText = StringUtil.NullIfEmpty(_values[rowNumber, (int)ExcelColumn.CheckedLine] as string);
            if (checkedText != null)
                Checked++;

            string editedText = StringUtil.NullIfEmpty(_values[rowNumber, (int)ExcelColumn.EditedLine] as string);
            if (editedText != null)
                Edited++;

            return StringUtil.NullIf(editedText, ".") ??
                   StringUtil.NullIf(checkedText, ".") ??
                   translatedText ??
                   originalText;
        }

        public void WritePatched(IEnumerable<ScriptString> strings, ScriptLocation location)
        {
            FillTable(strings);
            WriteTable(location);
        }

        private void FillTable(IEnumerable<ScriptString> strings)
        {
            IList<ScriptString> stringList = strings.AsList();
            int numRows = stringList.Count(s => s.Type == ScriptStringType.Message);
            int numColumns = Enum.GetValues(typeof(ExcelColumn)).Cast<int>().Max();
            _values = (object[,])Array.CreateInstance(typeof(object), new[] { numRows, numColumns }, new[] { 2, 1 });

            int row = 2;
            List<string> pendingCharacterNames = new List<string>();
            foreach (ScriptString str in stringList)
            {
                if (str.Type == ScriptStringType.CharacterName)
                {
                    pendingCharacterNames.Add(str.Text);
                }
                else
                {
                    FillRow(row, pendingCharacterNames, str.Text);
                    pendingCharacterNames.Clear();
                    row++;
                }
            }
        }

        private void FillRow(int row, List<string> characterNames, string message)
        {
            if (characterNames.Count > 0)
                _values[row, (int)ExcelColumn.OriginalCharacter] = JoinNames(characterNames);

            _values[row, (int)ExcelColumn.OriginalLine] = message;

            if (characterNames.Count > 0)
            {
                string translatedNames = JoinNames(characterNames.Select(CharacterNames.GetTranslation));
                _values[row, (int)ExcelColumn.TranslatedCharacter] = translatedNames;
            }
        }

        private void WriteTable(ScriptLocation location)
        {
            if (location.Collection != _collection)
                throw new InvalidOperationException();

            MsExcel.Worksheet sheet = (MsExcel.Worksheet)_workbook.Worksheets[location.ScriptName];
            int bottomRow = _values.GetUpperBound(0);
            char rightColumn = (char)(0x40 + _values.GetUpperBound(1));
            string address = $"A2:{rightColumn}{bottomRow}";
            sheet.Range[address].Value = _values;
        }

        public int Translated
        {
            get;
            private set;
        }

        public int Checked
        {
            get;
            private set;
        }

        public int Edited
        {
            get;
            private set;
        }

        public int Total
        {
            get;
            set;
        }

        public void Reset()
        {
            Translated = 0;
            Checked = 0;
            Edited = 0;
            Total = 0;
        }

        private static string JoinNames(IEnumerable<string> names)
        {
            return string.Join("/", names.Select(QuoteName));
        }

        private IEnumerable<string> SplitNames(string names)
        {
            return Regex.Matches(names, @"(?:""(?:\\.|[^""])+""|[^/]+)")
                        .Cast<Match>()
                        .Select(m => UnquoteName(m.Value));
        }

        private static string QuoteName(string name)
        {
            if (!name.Contains("/") && !name.Contains("\""))
                return name;

            name = name.Replace("\\", "\\\\");
            name = name.Replace("\"", "\\\"");
            return "\"" + name + "\"";
        }

        private static string UnquoteName(string name)
        {
            if (!name.StartsWith("\"") || !name.EndsWith("\""))
                return name;

            name = name.Substring(1, name.Length - 2);
            name = Regex.Replace(name, @"\\(.)", "$1");
            return name;
        }
    }
}
