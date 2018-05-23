using System;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace CsvConverter
{
	public class CsvConverterSettings : ScriptableObject
    {
        public Setting[] list;

        [MenuItem("Assets/Create/CsvConverterSettings")]
        public static void Create()
        {
            CsvConverterSettings o = ScriptableObject.CreateInstance<CsvConverterSettings>();
            create<CsvConverterSettings>(o);
        }

        private static void create<T>(T t) where T : UnityEngine.Object
        {
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);

            if (path == "")
            {
                path = "Assets";
            }
            else if (Path.GetExtension(path) != "")
            {
                path = path.Replace(Path.GetFileName(path), "");
            }

            string filePath = Path.Combine(path, "CsvConvertSettings.asset");
            AssetDatabase.CreateAsset(t, AssetDatabase.GenerateUniqueAssetPath(filePath));
            AssetDatabase.Refresh();
        }

        [Serializable]
        public class Setting
        {
            public string filePath;
            public string className;
            public string destination;
            public bool isEnum;
            public bool classGenerate;
            public bool tableGenerate;
            public string key; // ScriptableObject の名前に使用.
        }
    }
}
