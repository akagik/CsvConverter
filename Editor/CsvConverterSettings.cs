using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using System.Linq;


#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace CsvConverter {
    public class CsvConverterSettings : ScriptableObject {
        public Setting[] list;

        [MenuItem("Assets/Create/CsvConverterSettings")]
        public static void Create() {
            CsvConverterSettings o = ScriptableObject.CreateInstance<CsvConverterSettings>();
            create<CsvConverterSettings>(o);
        }

        private static void create<T>(T t) where T : UnityEngine.Object {
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);

            if (path == "") {
                path = "Assets";
            }
            else if (Path.GetExtension(path) != "") {
                path = path.Replace(Path.GetFileName(path), "");
            }

            string filePath = Path.Combine(path, "CsvConvertSettings.asset");
            AssetDatabase.CreateAsset(t, AssetDatabase.GenerateUniqueAssetPath(filePath));
            AssetDatabase.Refresh();
        }

        public void OnValidate() {
            for (int i = 0; i < list.Length; i++) {
                list[i].CalculateCanCreateAsset();
            }
        }

        [Serializable]
        public class Setting {
#if ODIN_INSPECTOR
            [FilePath(ParentFolder = "Assets", RequireExistingPath = true)]
#endif
            public string csvFilePath;
            public string className;

#if ODIN_INSPECTOR
            [FolderPath(ParentFolder = "Assets", RequireExistingPath = true)]
#endif
            public string destination;
            public bool isEnum;

#if ODIN_INSPECTOR
            [HideIf("isEnum")]
#endif
            public bool classGenerate;

#if ODIN_INSPECTOR
            [HideIf("isEnum")]
            [ToggleGroup("tableGenerate")]
#endif
            public bool tableGenerate;

#if ODIN_INSPECTOR
            [ShowIf("tableGenerate")]
            [HideIf("isEnum")]
            [ToggleGroup("tableGenerate")]
#endif
            public bool tableClassGenerate;

#if ODIN_INSPECTOR
            [ShowIf("tableGenerate")]
            [HideIf("isEnum")]
            [ToggleGroup("tableGenerate")]
#endif
            public bool onlyTableCreate;

#if ODIN_INSPECTOR
            [HideIf("isEnum")]
#endif
            public string key; // ScriptableObject の名前に使用.

            public string[] keys {
                get {
                    return key.Split(',').Select((arg) => arg.Trim()).Where((arg) => arg.Length > 0).ToArray();
                }
            }

            // code を生成できるか？
            public bool canGenerateCode {
                get {
                    return isEnum || classGenerate || tableGenerate;
                }
            }

            // asset を生成できるかどうか?
            public bool canCreateAsset { get; private set; }

            public void CalculateCanCreateAsset() {
                // enum の場合は Asset を生成できない
                if (isEnum) {
                    canCreateAsset = false;
                    return;
                }

                // クラスが生成されていない場合も Asset を生成できない
                Type assetType = AssetsGenerator.GetTypeByName(className);
                if (assetType == null) {
                    canCreateAsset = false;
                }
                else {
                    canCreateAsset = true;
                }
            }
        }
    }
}
