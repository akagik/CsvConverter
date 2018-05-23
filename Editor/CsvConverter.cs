using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace CsvConverter
{
    public class CsvConverter
    {
        [MenuItem("Tools/CsvConvert/Generate Code")]
        public static void GenerateCode()
        {
            var settings = GetSettings();

            if (settings.Length == 0)
            {
                Debug.Log("No settings for csv converter");
                return;
            }

            int i = 0;
            foreach (CsvConverterSettings.Setting s in settings[0].list)
            {
                show_progress(s.className, (float)i / settings[0].list.Length, i, settings[0].list.Length);

                TextAsset textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(Path.Combine("Assets", s.filePath));

                if (textAsset == null)
                {
                    Debug.LogError("Not found : " + s.filePath);
                    continue;
                }

                var cell = CsvParser.ReadAsList(textAsset.text);

                CsvData csv = new CsvData();
                csv.SetFromList(cell);


                if (s.isEnum)
                {
                    CsvData headers = csv.Slice(0, 1);
                    CsvData contents = csv.Slice(1);
                    EnumGenerator.Generate(s.destination, s.className, headers, contents);
                }
                else
                {
                    CsvData headers = csv.Slice(0, 2);
                    CsvData contents = csv.Slice(2);

                    Field[] fields = GetFieldsFromHeader(headers);

                    if (s.classGenerate)
                    {
                        ClassGenerator.GenerateClass(s.destination, s.className, fields);
                    }

                    if (s.tableGenerate)
                    {
                        int keyIndex = ClassGenerator.FindKeyIndex(s, fields);

                        Field key = null;
                        if (keyIndex != -1)
                        {
                            key = fields[keyIndex];
                        }
                        ClassGenerator.GenerateTableClass(s, s.className + "Table", key);
                    }
                }

                i++;
                show_progress(s.className, (float)i / settings[0].list.Length, i, settings[0].list.Length);
            }
            EditorUtility.ClearProgressBar();
        }

        [MenuItem("Tools/CsvConvert/Create Assets")]
        public static void CreateAssets()
        {
            var settings = GetSettings();

            if (settings.Length == 0)
            {
                Debug.Log("No settings for csv converter");
                return;
            }

            foreach (CsvConverterSettings.Setting s in settings[0].list)
            {
                TextAsset textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(Path.Combine("Assets", s.filePath));

                if (textAsset == null)
                {
                    Debug.LogError("Not found : " + s.filePath);
                    continue;
                }

                if (s.isEnum)
                {
                    continue;
                }

                var cell = CsvParser.ReadAsList(textAsset.text);

                CsvData csv = new CsvData();
                csv.SetFromList(cell);

                CsvData headers = csv.Slice(0, 2);
                CsvData contents = csv.Slice(2);

                Field[] fields = GetFieldsFromHeader(headers);

                bool success = ClassGenerator.CreateCsvAssets(s, s.className + "Table", fields, contents);

                if (!success)
                {
                    return;
                }
            }
            EditorUtility.ClearProgressBar();
        }

        public static Field[] GetFieldsFromHeader(CsvData grid)
        {
            var fields = new Field[grid.col];
            for (int i = 0; i < fields.Length; i++)
            {
                fields[i] = new Field();
            }

            // get field names;
            for (int col = 0; col < grid.col; col++)
            {
                string fieldName = grid.Get(0, col);
                fieldName = fieldName.Trim();

                if (fieldName == string.Empty)
                {
                    fields[col].isValid = false;
                    continue;
                }

                fields[col].fieldName = fieldName;
            }

            // set field types;
            for (int col = 0; col < grid.col; col++)
            {
                if (!fields[col].isValid) continue;

                string typeName = grid.Get(1, col).Trim();

                if (typeName == string.Empty)
                {
                    fields[col].isValid = false;
                    continue;
                }

                fields[col].typeName = typeName;
            }

            return fields;
        }

        public static CsvConverterSettings[] GetSettings()
        {
            string[] settingGUIDArray = AssetDatabase.FindAssets("t:CsvConverterSettings");

            CsvConverterSettings[] settings = new CsvConverterSettings[settingGUIDArray.Length];

            for (int i = 0; i < settingGUIDArray.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(settingGUIDArray[i]);
                settings[i] = AssetDatabase.LoadAssetAtPath<CsvConverterSettings>(path);
            }

            return settings;
        }

        private static void show_progress(string name, float progress, int i, int total)
        {
            EditorUtility.DisplayProgressBar("Progress", progress_msg(name, i, total), progress);
        }

        private static string progress_msg(string name, int i, int total)
        {
            return string.Format("Creating {0} ({1}/{2})", name, i, total);
        }
    }
}
