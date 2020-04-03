using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;

#endif

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
#if ODIN_INSPECTOR
            [Title("Basic Settings")]
#endif
            public string csvFilePath;

            public string className;

            public string destination = "";

#if ODIN_INSPECTOR
            [Title("Advanced Settings")]
#endif
            public bool isEnum;

#if ODIN_INSPECTOR
            [HideIf("isEnum")]
#endif
            public bool classGenerate;

#if ODIN_INSPECTOR
            [ToggleGroup("tableGenerate", "Table Generate")]
#endif
            public bool tableGenerate;

#if ODIN_INSPECTOR
            [ToggleGroup("tableGenerate")]
            [ValidateInput("IsValidClassName")]
#endif
            public string tableClassName;

#if ODIN_INSPECTOR
            [ToggleGroup("tableGenerate")]
            [InfoBox("If empty string, its value is tableClassName.")]
#endif
            public string _tableAssetName;

#if ODIN_INSPECTOR
            [ToggleGroup("tableGenerate")]
#endif
            public bool tableClassGenerate;

#if ODIN_INSPECTOR
            [ToggleGroup("tableGenerate")]
#endif
            public bool isDictionary;


#if ODIN_INSPECTOR
            [ToggleGroup("tableGenerate")]
#endif
            public bool onlyTableCreate;

#if ODIN_INSPECTOR
            [HideIf("isEnum")]
#endif
            public string key; // ScriptableObject の名前に使用.

            public string[] keys
            {
                get
                {
                    if (key == null || key.Length == 0)
                    {
                        return new string[0];
                    }

                    return key.Split(',').Select((arg) => arg.Trim()).Where((arg) => arg.Length > 0).ToArray();
                }
            }


            public bool useGSPlugin;

#if ODIN_INSPECTOR
            [Sirenix.OdinInspector.ShowIf("useGSPlugin")]
#endif
            public string sheetID;

#if ODIN_INSPECTOR
            [Sirenix.OdinInspector.ShowIf("useGSPlugin")]
#endif
            public string gid;

            // code を生成できるか？
            public bool canGenerateCode
            {
                get { return isEnum || classGenerate || tableGenerate; }
            }

            // asset を生成できるかどうか?
            public bool canCreateAsset
            {
                get { return !isEnum; }
            }
            
            public string tableAssetName
            {
                get
                {
                    if (_tableAssetName.Length > 0)
                    {
                        return _tableAssetName;
                    }

                    return tableClassName;
                }
            }

#if ODIN_INSPECTOR && UNITY_EDITOR
            private bool IsValidClassName(string name)
            {
                if (name == null)
                {
                    return false;
                }

                return name.Length > 0 && char.IsUpper(name[0]);
            }
#endif
        }
    }
}