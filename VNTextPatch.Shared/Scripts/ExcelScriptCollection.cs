using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using MsExcel = Microsoft.Office.Interop.Excel;

namespace VNTextPatch.Shared.Scripts
{
    public class ExcelScriptCollection : IScriptCollection, IDisposable
    {
        private MsExcel.Application _application;
        private MsExcel.Workbook _workbook;
        private ExcelScript _script;
        private bool _isEmpty;

        public ExcelScriptCollection(string filePath)
        {
            _application = new MsExcel.Application { DisplayAlerts = false };

            if (File.Exists(filePath))
            {
                _workbook = _application.Workbooks.Open(filePath);
            }
            else
            {
                string folderPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string templateFilePath = Path.Combine(folderPath, "template.xlsx");
                _workbook = _application.Workbooks.Open(templateFilePath);
                _workbook.SaveAs(filePath);
                _isEmpty = true;
            }

            Name = filePath;
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
            get { return _workbook.Worksheets.Cast<MsExcel.Worksheet>().Select(s => s.Name); }
        }

        public bool Exists(string scriptName)
        {
            try
            {
                _ = _workbook.Worksheets[scriptName];
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void Add(string scriptName)
        {
            if (!_isEmpty)
            {
                MsExcel.Worksheet lastSheet = (MsExcel.Worksheet)_workbook.Worksheets[_workbook.Worksheets.Count];
                lastSheet.Copy(After: lastSheet);
            }
            else
            {
                _isEmpty = false;
            }

            MsExcel.Worksheet sheet = (MsExcel.Worksheet)_workbook.Worksheets[_workbook.Worksheets.Count];
            sheet.Name = scriptName;

            int bottomRow = sheet.UsedRange.Rows.Count;
            if (bottomRow >= 2)
                sheet.Range[$"A2:A{bottomRow}"].EntireRow.Delete(MsExcel.XlDeleteShiftDirection.xlShiftUp);
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
            if (_application == null)
                return;

            _script = null;

            ((MsExcel.Worksheet)_workbook.Worksheets[1]).Activate();
            _workbook.Save();
            _workbook.Close();
            _workbook = null;

            _application.Quit();
            Marshal.FinalReleaseComObject(_application);
            _application = null;
        }
    }
}
