using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace CsvConverter
{
    public class ClassGenerator
    {
        const string FIELD_FORMAT = "    public {0} {1};\n";
        public static readonly string ROWS = "rows";

        static public void GenerateClass(string destination, string name, Field[] fields)
        {
            string classData = "";
            classData = "using UnityEngine;\n";
            classData += "using System.Collections.Generic;\n";
            classData += "\n";

            classData += "public class " + name + " : ScriptableObject\n";
            classData += "{\n";

            for (int col = 0; col < fields.Length; col++)
            {
                Field f = fields[col];

                if (f.fieldName == "" || f.typeName == "")
                {
                    continue;
                }

                classData += string.Format(FIELD_FORMAT, f.typeName, f.fieldName);
            }

            classData += "}\n";

            string filePath = Path.Combine(Path.Combine(Application.dataPath, destination), name + ".cs");
            using (StreamWriter writer = File.CreateText(filePath))
            {
                writer.WriteLine(classData);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static void GenerateTableClass(CsvConverterSettings.Setting setting, string tableClassName, Field key)
        {
            string classData = "";
            classData = "using UnityEngine;\n";
            classData += "using System.Collections.Generic;\n";
            classData += "\n";

            classData += "public class " + tableClassName + " : ScriptableObject\n";
            classData += "{\n";
            classData += string.Format("    public List<{0}> {1} = new List<{0}>();\n", setting.className, ROWS);

            if (key != null && key.isValid)
            {
                classData += "\n";
                classData += string.Format("    public {0} Find({1} key)\n", setting.className, key.typeName);
                classData += "    {\n";
                classData += string.Format("        foreach ({0} o in {1})\n", setting.className, ROWS);
                classData += "        {\n";
                classData += string.Format("            if (o.{0} == key)\n", key.fieldName);
                classData += "            {\n";
                classData += "                return o;\n";
                classData += "            }\n";
                classData += "        }\n";
                classData += "        return null;\n";
                classData += "    }\n";
            }

            classData += "}\n";

            string filePath = Path.Combine(Path.Combine(Application.dataPath, setting.destination), tableClassName + ".cs");
            using (StreamWriter writer = File.CreateText(filePath))
            {
                writer.WriteLine(classData);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static int FindKeyIndex(CsvConverterSettings.Setting setting, Field[] fields)
        {
            if (setting.key != "")
            {
                for (int i = 0; i < fields.Length; i++)
                {
                    if (fields[i].fieldName == setting.key)
                    {
                        return i;
                    }
                }
            }
            return -1;
        }

        public static bool CreateCsvAssets(CsvConverterSettings.Setting setting, string tableClassName, Field[] fields, CsvData content)
        {
            Type assetType = GetTypeByName(setting.className);
            if (assetType == null)
            {
                EditorUtility.DisplayDialog("Error", "Cannot find the class \"" + setting.className + "\", please execute \"Tools/CsvConverter/Generate Code\".", "ok");
                return false;
            }

            // class のフィールド名と一致しないものは除外する.
            for (int j = 0; j < fields.Length; j++)
            {
                if (!fields[j].isValid)
                {
                    continue;
                }

                FieldInfo info = assetType.GetField(fields[j].fieldName);

                if (info == null)
                {
                    Debug.LogWarningFormat("{0} に存在しないフィールド \"{1}\" を無視", setting.className, fields[j].fieldName);
                    fields[j].isValid = false;
                }
            }

            string folder = Path.Combine("Assets", setting.destination);

            // table の設定
            ScriptableObject table = null;
            Type tableType = null;

            FieldInfo dataListField = null;
            object dataList = null;

            if (setting.tableGenerate)
            {
                tableType = GetTypeByName(tableClassName);
                if (tableType == null)
                {
                    EditorUtility.DisplayDialog("Error", "Cannot find the class \"" + tableClassName + "\", please execute \"Tools/CsvConverter/Generate Code\".", "ok");
                    return false;
                }

                dataListField = tableType.GetField(ROWS);

                table = ScriptableObject.CreateInstance(tableType);
                string filePath = Path.Combine(folder, tableClassName + ".asset");
                AssetDatabase.CreateAsset(table, filePath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                dataList = dataListField.GetValue(table);
                //dataList.GetType().GetMethod("Clear").Invoke(dataList, null);
            }


            // Asset の名前をつけるときに利用する key.
            int keyIndex = FindKeyIndex(setting, fields);

            for (int i = 0; i < content.row; i++)
            {
                int line = i + 2 + 1;

#if VERBOSE
                Debug.Log("line:" + line);
#endif

                string fileName = setting.className + i + ".asset";
                if (keyIndex != -1)
                {
                    string key = content.Get(i, keyIndex).Trim();

                    if (key == "")
                    {
                        Debug.LogWarningFormat("{0} line {1}: key が存在しない行をスキップしました", setting.className, line);
                        continue;
                    }
                    fileName = setting.className + "_" + content.Get(i, keyIndex) + ".asset";
                }

                string filePath = Path.Combine(folder, fileName);
                var data = AssetDatabase.LoadAssetAtPath(filePath, assetType);
                if (data == null)
                {
                    data = ScriptableObject.CreateInstance(assetType);
                    AssetDatabase.CreateAsset(data, filePath);
                    Debug.LogFormat("Create {0}", filePath);
                }

                for (int j = 0; j < content.col; j++)
                {
                    if (!fields[j].isValid) continue;

                    FieldInfo info = assetType.GetField(fields[j].fieldName);

                    string sValue = content.Get(i, j);
                    object value = null;

                    // 文字列型のときは " でラップする.
                    if (info.FieldType ==  typeof (string)) {
                        sValue = "\"" + sValue + "\"";
                    }

                    if (sValue == "")
                    {
                        Debug.LogWarningFormat("{0} {1}行{2}列目: 空の値があります: {3}=\"{4}\"", setting.className, line, j + 1, info.Name, sValue);
                    }
                    else
                    {
                        value = Str2TypeConverter.Convert(info.FieldType, sValue);

                        if (value == null)
                        {
                            Debug.LogWarningFormat("{0} {1}行{2}列目: 変換に失敗しました: {3}=\"{4}\"", setting.className, line, j + 1, info.Name, sValue);
                        }
                    }

                    info.SetValue(data, value);
                }

                EditorUtility.SetDirty(data);

                if (setting.tableGenerate)
                {
                    dataList.GetType().GetMethod("Add").Invoke(dataList, new object[] { data });
                }

                show_progress(setting.className, (float)i / content.row, i, content.row);
            }


            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return true;
        }

        public static Type GetTypeByName(string name)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type type in assembly.GetTypes())
                {
                    if (type.Name == name)
                        return type;
                }
            }

            return null;
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