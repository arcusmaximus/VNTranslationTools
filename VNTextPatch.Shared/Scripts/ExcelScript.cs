using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NPOI.SS.UserModel;
using VNTextPatch.Shared.Util;

namespace VNTextPatch.Shared.Scripts
{
    public class ExcelScript : IScript, ILineStatistics
    {
        private const string EmptyTextMarker = "(empty)";

        private readonly ExcelScriptCollection _collection;
        private readonly IWorkbook _workbook;
        private ISheet _sheet;

        public ExcelScript(ExcelScriptCollection collection, IWorkbook workbook)
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

            _sheet = _workbook.GetSheet(location.ScriptName);
        }

        public IEnumerable<ScriptString> GetStrings()
        {
            foreach (IRow row in _sheet)
            {
                if (row.RowNum == 0)
                    continue;

                string characterNames = StringUtil.NullIfEmpty(row.GetCell((int)ExcelColumn.TranslatedCharacter)?.StringCellValue) ??
                                        StringUtil.NullIfEmpty(row.GetCell((int)ExcelColumn.OriginalCharacter)?.StringCellValue);
                if (characterNames != null)
                {
                    foreach (string characterName in SplitNames(characterNames))
                    {
                        yield return new ScriptString(characterName, ScriptStringType.CharacterName);
                    }
                }

                string text = GetText(row);
                if (text != null)
                {
                    text = Regex.Replace(text, @"(?<!\r)\n", "\r\n");
                    yield return new ScriptString(text, ScriptStringType.Message);
                }
            }
        }

        private string GetText(IRow row)
        {
            string originalText = StringUtil.NullIfEmpty(row.GetCell((int)ExcelColumn.OriginalLine)?.StringCellValue);
            if (originalText != null)
                Total++;

            string translatedText = StringUtil.NullIfEmpty(row.GetCell((int)ExcelColumn.TranslatedLine)?.StringCellValue);
            if (translatedText != null)
                Translated++;

            string checkedText = StringUtil.NullIfEmpty(row.GetCell((int)ExcelColumn.CheckedLine)?.StringCellValue);
            if (checkedText != null)
                Checked++;

            string editedText = StringUtil.NullIfEmpty(row.GetCell((int)ExcelColumn.EditedLine)?.StringCellValue);
            if (editedText != null)
                Edited++;

            string text = StringUtil.NullIf(editedText, ".") ??
                          StringUtil.NullIf(checkedText, ".") ??
                          translatedText ??
                          originalText;
            return text != EmptyTextMarker ? text : string.Empty;
        }

        public void WritePatched(IEnumerable<ScriptString> strings, ScriptLocation location)
        {
            _sheet = _workbook.GetSheet(location.ScriptName);

            int rowNum = 1;
            List<string> pendingCharacterNames = new List<string>();
            foreach (ScriptString str in strings)
            {
                if (str.Type == ScriptStringType.CharacterName)
                {
                    pendingCharacterNames.Add(str.Text);
                }
                else
                {
                    IRow row = _sheet.CreateRow(rowNum);
                    FillRow(row, pendingCharacterNames, str.Text);
                    pendingCharacterNames.Clear();
                    rowNum++;
                }
            }
        }

        private void FillRow(IRow row, List<string> characterNames, string message)
        {
            if (characterNames.Count > 0)
                FillCell(row, ExcelColumn.OriginalCharacter, JoinNames(characterNames));

            FillCell(row, ExcelColumn.OriginalLine, message.Length > 0 ? message : EmptyTextMarker);

            if (characterNames.Count > 0)
            {
                string translatedNames = JoinNames(characterNames.Select(CharacterNames.GetTranslation));
                FillCell(row, ExcelColumn.TranslatedCharacter, translatedNames);
            }
        }

        private void FillCell(IRow row, ExcelColumn column, string value)
        {
            ICell cell = row.CreateCell((int)column);
            cell.SetCellValue(value);
            cell.CellStyle = _sheet.GetColumnStyle((int)column);
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
