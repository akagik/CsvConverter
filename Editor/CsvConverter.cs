using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace CsvConverter {
    public class CsvConverter {
        public static void GenerateCode(CsvConverterSettings.Setting s) {
            TextAsset textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(Path.Combine("Assets", s.csvFilePath));

            if (textAsset == null) {
                Debug.LogError("Not found : " + s.csvFilePath);
                return;
            }

            string directoryPath = Path.Combine(Application.dataPath, s.destination);

            if (!Directory.Exists(directoryPath)) {
                Debug.LogError("Not found directory: " + directoryPath);
                return;
            }

            var cell = CsvParser.ReadAsList(textAsset.text);

            CsvData csv = new CsvData();
            csv.SetFromList(cell);

            if (s.isEnum) {
                CsvData headers = csv.Slice(0, 1);
                CsvData contents = csv.Slice(1);
                EnumGenerator.Generate(s.destination, s.className, headers, contents);

                Debug.LogFormat("Create \"{0}\"", Path.Combine(s.destination, s.className + ".cs"));
            }
            else {
                CsvData headers = csv.Slice(0, 2);
                CsvData contents = csv.Slice(2);

                Field[] fields = GetFieldsFromHeader(headers);

                if (s.classGenerate) {
                    ClassGenerator.GenerateClass(s.destination, s.className, fields, s.tableGenerate && s.onlyTableCreate);
                    Debug.LogFormat("Create \"{0}\"", Path.Combine(s.destination, s.className + ".cs"));
                }

                if (s.tableClassGenerate) {
                    int[] keyIndexes = ClassGenerator.FindKeyIndexes(s, fields);

                    string[] keys = s.keys;
                    Field[] key = null;
                    if (keyIndexes.Length > 0) {
                        List<Field> keyFieldList = new List<Field>();

                        for (int i = 0; i < keyIndexes.Length; i++) {
                            keyFieldList.Add(fields[keyIndexes[i]]);
                        }

                        key = keyFieldList.ToArray();
                    }
                    ClassGenerator.GenerateTableClass(s, s.tableClassName, key);
                    Debug.LogFormat("Create \"{0}\"", Path.Combine(s.destination, s.tableClassName + ".cs"));
                }
            }
        }

        public static void CreateAssets(CsvConverterSettings.Setting s) {
            TextAsset textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(Path.Combine("Assets", s.csvFilePath));

            if (textAsset == null) {
                Debug.LogError("Not found : " + s.csvFilePath);
                return;
            }

            if (s.isEnum) {
                return;
            }

            // csv ファイルから読み込み
            var cell = CsvParser.ReadAsList(textAsset.text);

            CsvData csv = new CsvData();
            csv.SetFromList(cell);

            CsvData headers = csv.Slice(0, 2);
            CsvData contents = csv.Slice(2);

            Field[] fields = GetFieldsFromHeader(headers);


            // アセットを生成する.
            AssetsGenerator assetsGenerator = new AssetsGenerator(s, fields, contents);

            // 生成する各要素の class type を取得
            Type assetType = GetTypeByName(s.className);
            if(assetType == null)
            {
                EditorUtility.DisplayDialog("Error","Cannot find the class \"" + s.className + "\", please execute \"Tools/CsvConverter/Generate Code\".","ok");
                return;
            }

            // class のフィールド名と一致しないものは除外する.
            for(int j = 0; j < fields.Length; j++)
            {
                if(!fields[j].isValid)
                {
                    continue;
                }

                // フィールド名が配列表の場合は [] の部分を削除する
                // 例) names[2] => names
                string fieldName = fields[j].fieldName;
                if(fieldName.Contains("["))
                {
                    fieldName = fieldName.Remove(fieldName.LastIndexOf("["));
                }

                FieldInfo info = assetType.GetField(fieldName);

                if(info == null)
                {
                    Debug.LogWarningFormat("{0} に存在しないフィールド \"{1}\" を無視",s.className,fieldName);
                    fields[j].isValid = false;
                }
            }

            // テーブルを生成する場合は、生成するテーブル class type を取得
            Type tableType = null;
            if (s.tableGenerate) {
                tableType = GetTypeByName(s.tableClassName);
                if(tableType == null)
                {
                    EditorUtility.DisplayDialog("Error","Cannot find the class \"" + s.tableClassName + "\", please execute \"Tools/CsvConverter/Generate Code\".","ok");
                    return;
                }
            }

            if (s.tableGenerate) {
                assetsGenerator.Setup(assetType, tableType);
            }
            else {
                assetsGenerator.Setup(assetType);
            }

            bool success = assetsGenerator.CreateCsvAssets();

            if (!success) {
                return;
            }
        }

        public static Field[] GetFieldsFromHeader(CsvData grid) {
            var fields = new Field[grid.col];
            for (int i = 0; i < fields.Length; i++) {
                fields[i] = new Field();
            }

            // get field names;
            for (int col = 0; col < grid.col; col++) {
                string fieldName = grid.Get(0, col);
                fieldName = fieldName.Trim();

                if (fieldName == string.Empty) {
                    fields[col].isValid = false;
                    continue;
                }

                fields[col].fieldName = fieldName;
            }

            // set field types;
            for (int col = 0; col < grid.col; col++) {
                if (!fields[col].isValid) continue;

                string typeName = grid.Get(1, col).Trim();

                if (typeName == string.Empty) {
                    fields[col].isValid = false;
                    continue;
                }

                fields[col].typeName = typeName;
            }

            return fields;
        }

        public static Type GetTypeByName(string name)
        {
            foreach(Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach(Type type in assembly.GetTypes())
                {
                    if(type.Name == name)
                        return type;
                }
            }

            return null;
        }

        public static CsvConverterSettings[] GetSettings() {
            string[] settingGUIDArray = AssetDatabase.FindAssets("t:CsvConverterSettings");

            CsvConverterSettings[] settings = new CsvConverterSettings[settingGUIDArray.Length];

            for (int i = 0; i < settingGUIDArray.Length; i++) {
                string path = AssetDatabase.GUIDToAssetPath(settingGUIDArray[i]);
                settings[i] = AssetDatabase.LoadAssetAtPath<CsvConverterSettings>(path);
            }

            return settings;
        }
    }
}
