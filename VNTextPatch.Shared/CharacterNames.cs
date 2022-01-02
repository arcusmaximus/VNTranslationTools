using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;
using VNTextPatch.Shared.Util;

namespace VNTextPatch.Shared
{
    public class CharacterNames
    {
        private static readonly Lazy<CharacterNames> _instance = new Lazy<CharacterNames>(() => new CharacterNames());

        private readonly Dictionary<string, string> _translations;

        private CharacterNames()
        {
            Document doc;
            using (Stream stream = File.OpenRead(FilePath))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(Document));
                doc = (Document)serializer.Deserialize(stream);
            }
            _translations = (doc.Characters ?? new Character[0]).ToDictionary(c => c.JapaneseName, c => c.EnglishName);
        }

        private static CharacterNames Instance
        {
            get { return _instance.Value; }
        }

        public static string GetTranslation(string japaneseName)
        {
            string englishName = Instance._translations.GetOrDefault(japaneseName);
            if (englishName == null)
            {
                Instance._translations.Add(japaneseName, japaneseName);
                return japaneseName;
            }
            return englishName;
        }

        public static void Save()
        {
            Instance.Write();
        }

        private void Write()
        {
            using Stream stream = File.Open(FilePath, FileMode.Create, FileAccess.Write);
            XmlSerializer serializer = new XmlSerializer(typeof(Document));
            serializer.Serialize(stream, new Document { Characters = _translations.Select(n => new Character { JapaneseName = n.Key, EnglishName = n.Value }).ToArray() });
        }

        private static string FilePath
        {
            get { return Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "names.xml"); }
        }

        [XmlRoot("names")]
        public class Document
        {
            [XmlElement("n")]
            public Character[] Characters
            {
                get;
                set;
            }
        }

        public class Character
        {
            [XmlElement("o")]
            public string JapaneseName
            {
                get;
                set;
            }

            [XmlElement("tl")]
            public string EnglishName
            {
                get;
                set;
            }
        }
    }
}
