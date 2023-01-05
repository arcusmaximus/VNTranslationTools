using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using VNTextPatch.Shared.Scripts.AdvHd;
using VNTextPatch.Shared.Scripts.ArcGameEngine;
using VNTextPatch.Shared.Scripts.Artemis;
using VNTextPatch.Shared.Scripts.Ethornell;
using VNTextPatch.Shared.Scripts.Kirikiri;
using VNTextPatch.Shared.Scripts.Majiro;
using VNTextPatch.Shared.Scripts.Mware;
using VNTextPatch.Shared.Scripts.Propeller;
using VNTextPatch.Shared.Scripts.RealLive;
using VNTextPatch.Shared.Scripts.ShSystem;
using VNTextPatch.Shared.Scripts.Silkys;
using VNTextPatch.Shared.Scripts.Softpal;
using VNTextPatch.Shared.Scripts.SystemNnn;
using VNTextPatch.Shared.Scripts.TmrHiroAdvSystem;
using VNTextPatch.Shared.Scripts.Yuris;

namespace VNTextPatch.Shared.Scripts
{
    public class FolderScriptCollection : IScriptCollection
    {
        private static readonly IScript[] TemporaryScripts;

        static FolderScriptCollection()
        {
            TemporaryScripts =
                new IScript[]
                {
                    new AdvHdScript(),
                    new ArtemisAsbScript(),
                    new ArtemisAstScript(),
                    new ArtemisTxtScript(),
                    new AgeScript(),
                    new CatSystemScript(),
                    new CSystemScript(),
                    new EthornellScript(),
                    new JsonScript(),
                    new KaguyaScript(),
                    new KirikiriKsScript(),
                    new KirikiriScnScript(),
                    new KirikiriSocScript(),
                    new KirikiriTjsScript(),
                    new MajiroScript(),
                    new MusicaScript(),
                    new MwareScript(),
                    new PropellerScript(),
                    new QlieScript(),
                    new RealLiveScript(),
                    new RenpyScript(),
                    new ShSystemScript(),
                    new SilkysMapScript(),
                    new SilkysMesScript(),
                    new SoftpalScript(),
                    new SystemNnnDevScript(),
                    new SystemNnnReleaseScript(),
                    new TmrHiroAdvSystemCodeScript(),
                    new TmrHiroAdvSystemTextScript(),
                    new WhaleScript(),
                    new YurisScript()
                };
        }

        public FolderScriptCollection(string folderPath, string extension, string format = null)
        {
            if (!Directory.Exists(folderPath))
                throw new DirectoryNotFoundException($"{folderPath} does not exist");

            FolderPath = folderPath;
            Extension = extension ?? string.Empty;
            Format = format;
        }

        public string Name
        {
            get { return FolderPath; }
        }

        public string FolderPath
        {
            get;
        }

        public string Extension
        {
            get;
        }

        public string Format
        {
            get;
        }

        public IScript GetTemporaryScript()
        {
            IScript script;
            if (Format != null)
            {
                string typeName = Format + "Script";
                script = TemporaryScripts.FirstOrDefault(f => f.GetType().Name.Equals(typeName, StringComparison.InvariantCultureIgnoreCase));
                if (script == null)
                    throw new NotSupportedException($"Format {Format} is not supported");
            }
            else
            {
                script = TemporaryScripts.FirstOrDefault(f => !string.IsNullOrEmpty(f.Extension) && f.Extension.Equals(Extension, StringComparison.InvariantCultureIgnoreCase));
                if (script == null)
                    throw new NotSupportedException($"Extension {Extension} is not supported");
            }
            return script;
        }

        public IEnumerable<string> Scripts
        {
            get
            {
                int folderPathLength = FolderPath.Length;
                if (!Name.EndsWith("\\"))
                    folderPathLength++;

                return Directory.EnumerateFiles(Name, "*" + Extension, SearchOption.AllDirectories)
                                .Select(f => f.Substring(folderPathLength));
            }
        }

        public bool Exists(string scriptName)
        {
            return File.Exists(Path.Combine(FolderPath, scriptName));
        }

        public void Add(string scriptName)
        {
            string filePath = Path.Combine(FolderPath, scriptName);
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            File.Create(filePath).Close();
        }

        public void Add(string scriptName, ScriptLocation copyFrom)
        {
            string sourceFilePath = copyFrom.ToFilePath();
            string destFilePath = Path.Combine(FolderPath, copyFrom.ScriptName);
            Directory.CreateDirectory(Path.GetDirectoryName(destFilePath));
            File.Copy(sourceFilePath, destFilePath, true);
        }

        public override string ToString()
        {
            return FolderPath;
        }
    }
}
