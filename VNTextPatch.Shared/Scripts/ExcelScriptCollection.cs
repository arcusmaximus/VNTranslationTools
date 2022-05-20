using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace VNTextPatch.Shared.Scripts
{
    public class ExcelScriptCollection : IScriptCollection, IDisposable
    {
        private XSSFWorkbook _workbook;
        private ExcelScript _script;
        private bool _isEmpty;

        public ExcelScriptCollection(string filePath)
        {
            Name = filePath;

            if (!File.Exists(filePath))
            {
                string folderPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string templateFilePath = Path.Combine(folderPath, "template.xlsx");
                File.Copy(templateFilePath, filePath);
                _isEmpty = true;
            }

            _workbook = new XSSFWorkbook(filePath);
            _script = new ExcelScript(this, _workbook);
        }

        public string Name
        {
            get;
        }

        public IScript GetTemporaryScript()
        {
            return _script;
        }

        public IEnumerable<string> Scripts
        {
            get { return Enumerable.Range(0, _workbook.NumberOfSheets).Select(_workbook.GetSheetName); }
        }

        public bool Exists(string scriptName)
        {
            return _workbook.GetSheet(scriptName) != null;
        }

        public void Add(string scriptName)
        {
            if (_isEmpty)
            {
                _workbook.SetSheetName(0, scriptName);
                _isEmpty = false;
            }
            else
            {
                ISheet sheet = _workbook.CloneSheet(0, scriptName);
                for (int i = sheet.LastRowNum; i > 0; i--)
                {
                    sheet.RemoveRow(sheet.GetRow(i));
                }
            }
        }

        public void Add(string scriptName, ScriptLocation copyFrom)
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return Name;
        }

        public void Dispose()
        {
            _script = null;

            if (_workbook != null)
            {
                using (Stream stream = File.Open(Name + ".temp", FileMode.Create))
                {
                    _workbook.Write(stream);
                    _workbook.Close();
                    _workbook = null;
                }
                File.Delete(Name);
                File.Move(Name + ".temp", Name);
            }
        }
    }
}
