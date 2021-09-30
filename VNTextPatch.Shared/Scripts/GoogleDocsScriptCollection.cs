using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;

namespace VNTextPatch.Shared.Scripts
{
    public class GoogleDocsScriptCollection : IScriptCollection
    {
        private readonly Dictionary<string, Sheet> _sheets;

        public GoogleDocsScriptCollection(string spreadsheetId)
        {
            SpreadsheetId = spreadsheetId;

            Spreadsheet spreadsheet = GetService().Spreadsheets.Get(spreadsheetId).Execute();
            _sheets = spreadsheet.Sheets.ToDictionary(s => s.Properties.Title);
        }

        public string Name
        {
            get { return SpreadsheetId; }
        }

        public string SpreadsheetId
        {
            get;
        }

        public IScript GetTemporaryScript()
        {
            return new GoogleDocsScript();
        }

        public IEnumerable<string> Scripts
        {
            get { return _sheets.Keys; }
        }

        public bool Exists(string scriptName)
        {
            return _sheets.ContainsKey(scriptName);
        }

        public void Add(string scriptName)
        {
            throw new NotImplementedException();
        }

        public void Add(string scriptName, ScriptLocation copyFrom)
        {
            throw new NotImplementedException();
        }

        internal SheetsService GetService()
        {
            return new SheetsService(
                new BaseClientService.Initializer
                {
                    ApiKey = ConfigurationManager.AppSettings["GoogleApiKey"],
                    ApplicationName = "TextPatch"
                }
            );
        }

        internal Sheet GetSheet(string sheetName)
        {
            return _sheets[sheetName];
        }

        public override string ToString()
        {
            return SpreadsheetId;
        }
    }
}
