using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Util;
using VNTextPatch.Shared.Util;

namespace VNTextPatch.Shared.Scripts
{
    public class GoogleDocsScript : IScript, ILineStatistics
    {
        private const string EmptyTextMarker = "(empty)";

        private IList<IList<object>> _cells;

        public string Extension
        {
            get { return null; }
        }

        public void Load(ScriptLocation location)
        {
            GoogleDocsScriptCollection collection = (GoogleDocsScriptCollection)location.Collection;
            string sheetName = location.ScriptName;

            string range = GetScriptRange(collection.GetSheet(sheetName));
            var request = GoogleDocsScriptCollection.GetService().Spreadsheets.Values.BatchGet(collection.SpreadsheetId);
            request.Ranges = new Repeatable<string>(new[] { range });
            _cells = request.ExecuteRateLimited().ValueRanges[0].Values;
        }

        public IEnumerable<ScriptString> GetStrings()
        {
            for (int rowIdx = 1; rowIdx < _cells.Count; rowIdx++)
            {
                string characterNames = GetCellContent(rowIdx, ExcelColumn.TranslatedCharacter) ??
                                        GetCellContent(rowIdx, ExcelColumn.OriginalCharacter);

                if (characterNames != null)
                {
                    foreach (string characterName in characterNames.Split('/'))
                    {
                        yield return new ScriptString(characterName, ScriptStringType.CharacterName);
                    }
                }

                string text = GetText(rowIdx);
                if (text != null)
                {
                    text = Regex.Replace(text, @"(?<!\r)\n", "\r\n");
                    yield return new ScriptString(text, ScriptStringType.Message);
                }
            }
        }

        private string GetText(int rowIdx)
        {
            string originalText = GetCellContent(rowIdx, ExcelColumn.OriginalLine);
            if (originalText != null)
                Total++;

            string translatedText = GetCellContent(rowIdx, ExcelColumn.TranslatedLine);
            if (translatedText != null)
                Translated++;

            string checkedText = GetCellContent(rowIdx, ExcelColumn.CheckedLine);
            if (checkedText != null)
                Checked++;

            string editedText = GetCellContent(rowIdx, ExcelColumn.EditedLine);
            if (editedText != null)
                Edited++;

            string text = StringUtil.NullIf(editedText, ".") ??
                          StringUtil.NullIf(checkedText, ".") ??
                          translatedText ??
                          originalText;
            return text != EmptyTextMarker ? text : string.Empty;
        }

        private string GetCellContent(int rowIdx, ExcelColumn column)
        {
            if (rowIdx >= _cells.Count)
                return null;

            IList<object> row = _cells[rowIdx];
            int colIdx = (int)column - 1;
            if (colIdx >= row.Count)
                return null;

            string value = row[colIdx] as string;
            return StringUtil.NullIf(value, string.Empty);
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

        public void WritePatched(IEnumerable<ScriptString> strings, ScriptLocation location)
        {
            throw new NotImplementedException();
        }

        private static string GetScriptRange(Sheet sheet)
        {
            GridProperties properties = sheet.Properties.GridProperties;
            int maxColumnIdx = Enum.GetValues(typeof(ExcelColumn)).Cast<ExcelColumn>().Max(c => (int)c);
            char maxColumnName = (char)('A' + maxColumnIdx - 1);
            return $"{sheet.Properties.Title}!A1:{maxColumnName}{properties.RowCount}";
        }
    }
}
