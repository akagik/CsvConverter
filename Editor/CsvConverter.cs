using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

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

                Debug.Log("Create " + Path.Combine(s.destination, s.className + ".cs"));
            }
            else {
                CsvData headers = csv.Slice(0, 2);
                CsvData contents = csv.Slice(2);

                Field[] fields = GetFieldsFromHeader(headers);

                if (s.classGenerate) {
                    ClassGenerator.GenerateClass(s.destination, s.className, fields, s.tableGenerate && s.onlyTableCreate);
                    Debug.Log("Create " + Path.Combine(s.destination, s.className + ".cs"));
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
                    ClassGenerator.GenerateTableClass(s, s.className + "Table", key);
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

            var cell = CsvParser.ReadAsList(textAsset.text);

            CsvData csv = new CsvData();
            csv.SetFromList(cell);

            CsvData headers = csv.Slice(0, 2);
            CsvData contents = csv.Slice(2);

            Field[] fields = GetFieldsFromHeader(headers);

            bool success = AssetsGenerator.CreateCsvAssets(s, s.className + "Table", fields, contents);

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
