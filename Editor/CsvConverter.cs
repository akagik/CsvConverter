using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using ResultType = CsvConverter.AssetsGenerator.ResultType;

namespace CsvConverter
{
    public class CsvConverter
    {
        public static void GenerateCode(CsvConverterSettings.Setting s, GlobalCCSettings gSettings, string settingPath)
        {
            string csvPath = CCLogic.GetFilePathRelativesToAssets(settingPath, s.csvFilePath);
            TextAsset textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(csvPath);

            if (textAsset == null)
            {
                Debug.LogError("Not found : " + csvPath);
                return;
            }

            string directoryPath = CCLogic.GetFullPath(settingPath, s.destination);

            if (!Directory.Exists(directoryPath))
            {
                Debug.LogError("Not found directory: " + directoryPath);
                return;
            }

            CsvData csv = CsvLogic.GetValidCsvData(textAsset.text, gSettings);

            if (s.isEnum)
            {
                CsvData headers = csv.Slice(gSettings.rowIndexOfName, gSettings.rowIndexOfName + 1);
                CsvData contents = csv.Slice(gSettings.rowIndexOfEnumContentStart);
                string code = EnumGenerator.Generate(s.className, headers, contents);

                string filePath = Path.Combine(directoryPath, s.className + ".cs");
                using (StreamWriter writer = File.CreateText(filePath))
                {
                    writer.WriteLine(code);
                }

                Debug.LogFormat("Create \"{0}\"", filePath);
            }
            else
            {
                Field[] fields = CsvLogic.GetFieldsFromHeader(csv, gSettings);

                if (s.classGenerate)
                {
                    string code =
                        ClassGenerator.GenerateClass(s.className, fields, s.tableGenerate && s.onlyTableCreate);

                    string filePath = Path.Combine(directoryPath, s.className + ".cs");
                    using (StreamWriter writer = File.CreateText(filePath))
                    {
                        writer.WriteLine(code);
                    }

                    Debug.LogFormat("Create \"{0}\"", filePath);
                }

                if (s.tableClassGenerate)
                {
                    int[] keyIndexes = ClassGenerator.FindKeyIndexes(s, fields);

                    string[] keys = s.keys;
                    Field[] key = null;
                    if (keyIndexes.Length > 0)
                    {
                        List<Field> keyFieldList = new List<Field>();

                        for (int i = 0; i < keyIndexes.Length; i++)
                        {
                            keyFieldList.Add(fields[keyIndexes[i]]);
                        }

                        key = keyFieldList.ToArray();
                    }

                    string code = ClassGenerator.GenerateTableClass(s, s.tableClassName, key);

                    string filePath = Path.Combine(directoryPath, s.tableClassName + ".cs");
                    using (StreamWriter writer = File.CreateText(filePath))
                    {
                        writer.WriteLine(code);
                    }

                    Debug.LogFormat("Create \"{0}\"", filePath);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static void CreateAssets(CsvConverterSettings.Setting s, GlobalCCSettings gSettings, string settingPath)
        {
            string csvPath = CCLogic.GetFilePathRelativesToAssets(settingPath, s.csvFilePath);
            TextAsset textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(csvPath);

            if (textAsset == null)
            {
                Debug.LogError("Not found : " + csvPath);
                return;
            }

            if (s.isEnum)
            {
                return;
            }

            // csv ファイルから読み込み
            CsvData csv = CsvLogic.GetValidCsvData(textAsset.text, gSettings);
            CsvData contents = csv.Slice(gSettings.rowIndexOfContentStart);

            Field[] fields = CsvLogic.GetFieldsFromHeader(csv, gSettings);

            // アセットを生成する.
            AssetsGenerator assetsGenerator = new AssetsGenerator(s, fields, contents);

            // カスタムアセットタイプを設定する
            // これはプロジェクト固有のアセットをテーブルでセット出来るようにする.
            {
                Type[] customAssetTypes = new Type[gSettings.customAssetTypes.Length];
                for (int i = 0; i < customAssetTypes.Length; i++)
                {
                    if (TryGetTypeWithError(gSettings.customAssetTypes[i], out var type))
                    {
                        customAssetTypes[i] = type;
                    }
                    else
                    {
                        return;
                    }
                }

                assetsGenerator.SetCustomAssetTypes(customAssetTypes);
            }

            // 生成する各要素の class type を取得
            Type assetType;
            if (!TryGetTypeWithError(s.className, out assetType))
            {
                return;
            }

            // class のフィールド名と一致しないものは除外する.
            for (int j = 0; j < fields.Length; j++)
            {
                if (!fields[j].isValid)
                {
                    continue;
                }

                // フィールド名が配列表の場合は [] の部分を削除する
                // 例) names[2] => names
                string fieldName = fields[j].fieldNameWithoutIndexing;
                FieldInfo info = assetType.GetField(fieldName);

                if (info == null)
                {
                    Debug.LogWarningFormat("{0} に存在しないフィールド \"{1}\" を無視", s.className, fieldName);
                    fields[j].isValid = false;
                }
            }

            // テーブルを生成する場合は、生成するテーブル class type を取得
            Type tableType = null;
            if (s.tableGenerate)
            {
                if (!TryGetTypeWithError(s.tableClassName, out tableType))
                {
                    return;
                }
            }

            if (s.tableGenerate)
            {
                assetsGenerator.Setup(assetType, tableType, settingPath);
            }
            else
            {
                assetsGenerator.Setup(assetType, settingPath);
            }

            // アセットを作成する.
            for (int i = 0; i < assetsGenerator.contentRowCount; i++)
            {
                int line = i + 2 + 1;
                ResultType resultType = assetsGenerator.CreateCsvAssetAt(i);

                if ((resultType & ResultType.SkipNoKey) != 0)
                {
                    Debug.LogWarningFormat("{0} line {1}: key が存在しない行をスキップしました", s.className, line);
                }

                int total = assetsGenerator.contentRowCount;
                if (total <= 10 || i == total - 1 || i % (total / 10) == 0)
                {
                    float progress = (float) i / total;
                    EditorUtility.DisplayProgressBar("Progress", $"Creating {s.className} ({i + 1}/{total})", progress);
                }
            }

            // 結果を出力して保存する.
            var result = assetsGenerator.result;
            if (s.tableGenerate)
            {
                EditorUtility.SetDirty(result.tableInstance);
                Debug.Log($"Create \"{Path.Combine(assetsGenerator.dstFolder, s.tableAssetName)}.asset\"");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"生成された総行数: {result.createdRowCount}");

            if (result.tableInstance != null)
            {
                EditorGUIUtility.PingObject(result.tableInstance.GetInstanceID());
            }

            EditorUtility.ClearProgressBar();
        }

        public static bool TryGetTypeWithError(string name, out Type type)
        {
            List<Type> candidates = GetTypeByName(name);
            type = null;

            if (candidates.Count == 0)
            {
                EditorUtility.DisplayDialog(
                    "Error",
                    "Cannot find the class \"" + name + "\", please execute \"Tools/CsvConverter/Generate Code\".",
                    "ok"
                );
                return false;
            }

            if (candidates.Count > 1)
            {
                EditorUtility.DisplayDialog(
                    "Error",
                    "複数候補の class が発見されました: \"" + name + "\".",
                    "ok"
                );
                return false;
            }

            type = candidates[0];
            return true;
        }

        public static List<Type> GetTypeByName(string name)
        {
            List<Type> candidates = new List<Type>();

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type type in assembly.GetTypes())
                {
                    if (type.Name == name)
                    {
                        candidates.Add(type);
                    }
                }
            }

            return candidates;
        }
    }
}