using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using Google.Apis.Auth.OAuth2;
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

        internal static SheetsService GetService()
        {
            BaseClientService.Initializer initializer = GetServiceAccountInitializer() ?? GetApiKeyInitializer();
            if (initializer == null)
            {
                throw new Exception("No Google credentials registered. Please put an API key in the \"GoogleApiKey\" entry of the .config file, " +
                                    "or store the private key of a service account in a file called \"google-service-account.json\".");
            }

            initializer.ApplicationName = "VNTextPatch";
            return new SheetsService(initializer);
        }

        private static BaseClientService.Initializer GetApiKeyInitializer()
        {
            string apiKey = ConfigurationManager.AppSettings["GoogleApiKey"];
            if (string.IsNullOrEmpty(apiKey))
                return null;

            return new BaseClientService.Initializer
                   {
                       ApiKey = apiKey
                   };
        }

        private static BaseClientService.Initializer GetServiceAccountInitializer()
        {
            string keyFilePath = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "google-service-account.json");
            if (!File.Exists(keyFilePath))
                return null;

            var credential = (ServiceAccountCredential)GoogleCredential.FromFile(keyFilePath).UnderlyingCredential;
            var credentialInitializer = new ServiceAccountCredential.Initializer(credential.Id)
                                        {
                                            Key = credential.Key,
                                            Scopes = new[] { SheetsService.Scope.SpreadsheetsReadonly }
                                        };
            credential = new ServiceAccountCredential(credentialInitializer);
            return new BaseClientService.Initializer
                   {
                       HttpClientInitializer = credential
                   };
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
