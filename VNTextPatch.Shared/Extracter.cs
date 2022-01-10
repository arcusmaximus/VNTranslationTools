using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace VNTextPatch.Shared
{
    public class Extracter
    {
        private readonly IScriptCollection _inputCollection;
        private readonly IScript _inputScript;

        private readonly IScriptCollection _textCollection;
        private readonly IScript _textScript;

        public Extracter(IScriptCollection inputCollection, IScriptCollection textCollection)
        {
            _inputCollection = inputCollection;
            _inputScript = inputCollection.GetTemporaryScript();

            _textCollection = textCollection;
            _textScript = _textCollection.GetTemporaryScript();
        }

        public int TotalLines
        {
            get;
            private set;
        }

        public int TotalCharacters
        {
            get;
            private set;
        }

        public void ExtractOne(string inputScriptName, string textScriptName)
        {
            if (!_inputCollection.Exists(inputScriptName))
                throw new FileNotFoundException($"{inputScriptName} does not exist in {_inputCollection.Name}");

            _inputScript.Load(new ScriptLocation(_inputCollection, inputScriptName));
            List<ScriptString> strings = _inputScript.GetStrings().ToList();
            if (strings.Count == 0)
                return;

            if (!_textCollection.Exists(textScriptName))
                _textCollection.Add(textScriptName);

            _textScript.WritePatched(strings, new ScriptLocation(_textCollection, textScriptName));

            foreach (ScriptString str in strings.Where(s => s.Type == ScriptStringType.Message))
            {
                TotalLines++;
                TotalCharacters += str.Text.Count(c => "「」『』【】（）“”、。？！".IndexOf(c) < 0);
            }
        }

        public void ExtractAll()
        {
            foreach (string inputScriptName in _inputCollection.Scripts)
            {
                Console.WriteLine(inputScriptName);

                string textScriptName;
                if (!string.IsNullOrEmpty(_inputScript.Extension))
                    textScriptName = Path.ChangeExtension(inputScriptName, _textScript.Extension);
                else
                    textScriptName = inputScriptName + _textScript.Extension;

                ExtractOne(inputScriptName, textScriptName);
            }
        }
    }
}
