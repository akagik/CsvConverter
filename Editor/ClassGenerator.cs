using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace CsvConverter {
    public class ClassGenerator {
        const string FIELD_FORMAT = "    public {0} {1};\n";
        public static readonly string ROWS = "rows";

        static public void GenerateClass(string destination, string name, Field[] fields, bool onlyTable) {
            string classData = "";
            classData = "using UnityEngine;\n";
            classData += "using System.Collections.Generic;\n";
            classData += "\n";

            if (onlyTable) {
                classData += "[System.Serializable]\n";
                classData += "public class " + name + "\n";
            }
            else {
                classData += "public class " + name + " : ScriptableObject\n";
            }
            classData += "{\n";

            HashSet<string> addedFields = new HashSet<string>();
            for (int col = 0; col < fields.Length; col++) {
                Field f = fields[col];
                if (addedFields.Contains(f.fieldNameWithoutIndexing)) break;

                string fieldName = f.fieldName;
                string typeName = f.typeName;

                if (fieldName == "" || typeName == "") {
                    continue;
                }

                if (f.isArrayField) {
                    fieldName = f.fieldNameWithoutIndexing;
                    typeName = typeName + "[]";
                }

                classData += string.Format(FIELD_FORMAT, typeName, fieldName);
                addedFields.Add(f.fieldNameWithoutIndexing);
            }

            classData += "}\n";

            string filePath = Path.Combine(Path.Combine(Application.dataPath, destination), name + ".cs");
            using (StreamWriter writer = File.CreateText(filePath)) {
                writer.WriteLine(classData);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static void GenerateTableClass(CsvConverterSettings.Setting setting, string tableClassName, Field[] keys) {
            string classData = "";
            classData = "using UnityEngine;\n";
            classData += "using System.Collections.Generic;\n";
            classData += "\n";

            classData += "public class " + tableClassName + " : ScriptableObject\n";
            classData += "{\n";
            classData += string.Format("    public List<{0}> {1} = new List<{0}>();\n", setting.className, ROWS);

            if (keys != null && keys.All((arg) => arg.isValid)) {
                classData += "\n";

                string argStr = "";
                string condStr = "";

                for (int i = 0; i < keys.Length; i++) {
                    argStr += keys[i].typeName + " key" + i + ", ";
                    condStr += string.Format("o.{0} == key{1} && ", keys[i].fieldName, i);
                }
                argStr = argStr.Substring(0, argStr.Length - 2);
                condStr = condStr.Substring(0, condStr.Length - 4);

                classData += string.Format("    public {0} Find({1})\n", setting.className, argStr);
                classData += "    {\n";
                classData += string.Format("        foreach ({0} o in {1})\n", setting.className, ROWS);
                classData += "        {\n";
                classData += string.Format("            if ({0})\n", condStr);
                classData += "            {\n";
                classData += "                return o;\n";
                classData += "            }\n";
                classData += "        }\n";
                classData += "        return null;\n";
                classData += "    }\n";
            }

            classData += "}\n";

            string filePath = Path.Combine(Path.Combine(Application.dataPath, setting.destination), tableClassName + ".cs");
            using (StreamWriter writer = File.CreateText(filePath)) {
                writer.WriteLine(classData);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static int[] FindKeyIndexes(CsvConverterSettings.Setting setting, Field[] fields) {
            List<int> indexes = new List<int>();

            string[] keys = setting.keys;
            Debug.Log(keys.ToString<string>());

            for (int j = 0; j < keys.Length; j++) {
                for (int i = 0; i < fields.Length; i++) {
                    if (fields[i].fieldName == keys[j]) {
                        indexes.Add(i);
                    }
                } 
            }
            return indexes.ToArray();
        }

        public static bool CreateCsvAssets(CsvConverterSettings.Setting setting, string tableClassName, Field[] fields, CsvData content) {
            Type assetType = GetTypeByName(setting.className);
            if (assetType == null) {
                EditorUtility.DisplayDialog("Error", "Cannot find the class \"" + setting.className + "\", please execute \"Tools/CsvConverter/Generate Code\".", "ok");
                return false;
            }

            // class のフィールド名と一致しないものは除外する.
            for (int j = 0; j < fields.Length; j++) {
                if (!fields[j].isValid) {
                    continue;
                }
                
                string fieldName = fields[j].fieldName;
                if (fieldName.Contains("[")) {
                    fieldName = fieldName.Remove(fieldName.LastIndexOf("["));
                }

                FieldInfo info = assetType.GetField(fieldName);

                if (info == null) {
                    Debug.LogWarningFormat("{0} に存在しないフィールド \"{1}\" を無視", setting.className, fieldName);
                    fields[j].isValid = false;
                }
            }

            string folder = Path.Combine("Assets", setting.destination);

            // table の設定
            ScriptableObject table = null;
            Type tableType = null;

            FieldInfo dataListField = null;
            object dataList = null;

            if (setting.tableGenerate) {
                tableType = GetTypeByName(tableClassName);
                if (tableType == null) {
                    EditorUtility.DisplayDialog("Error", "Cannot find the class \"" + tableClassName + "\", please execute \"Tools/CsvConverter/Generate Code\".", "ok");
                    return false;
                }

                dataListField = tableType.GetField(ROWS);

                string filePath = Path.Combine(folder, tableClassName + ".asset");

                table = AssetDatabase.LoadAssetAtPath<ScriptableObject>(filePath);
                if (table == null) {
                    table = ScriptableObject.CreateInstance(tableType);
                    AssetDatabase.CreateAsset(table, filePath);
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                dataList = dataListField.GetValue(table);
                dataList.GetType().GetMethod("Clear").Invoke(dataList, null);
            }


            // Asset の名前をつけるときに利用する key.
            int[] keyIndexes = FindKeyIndexes(setting, fields);

            for (int i = 0; i < content.row; i++) {
                int line = i + 2 + 1;

#if VERBOSE
                Debug.Log("line:" + line);
#endif

                string fileName = setting.className + i + ".asset";
                if (keyIndexes.Length > 0) {
                    fileName = setting.className;
                    for (int j = 0; j < keyIndexes.Length; j++) {
                        int keyIndex = keyIndexes[j];

                        string key = content.Get(i, keyIndex).Trim();

                        if (key == "") {
                            Debug.LogWarningFormat("{0} line {1}: key が存在しない行をスキップしました", setting.className, line);
                            continue;
                        }
                        fileName += "_" + key;
                    }
                    fileName += ".asset";
                }

                string filePath = Path.Combine(folder, fileName);

                object data = null;

                // テーブルのみ作成する場合は ScriptableObject としてではなく
                // 通常のインスタンスとして作成する.
                if (setting.onlyTableCreate) {
                    data = Activator.CreateInstance(assetType);
                }
                else {
                    data = AssetDatabase.LoadAssetAtPath(filePath, assetType);
                    if (data == null) {
                        data = ScriptableObject.CreateInstance(assetType);
                        AssetDatabase.CreateAsset(data as UnityEngine.Object, filePath);
                        Debug.LogFormat("Create {0}", filePath);
                    }
                }

                // フィールド名に[]が付いているカラムを先に検索し、配列インスタンスを生成しておく.
                for (int j = 0; j < content.col; j++) {
                    if (!fields[j].isValid) continue;
                    
                    FieldInfo info = assetType.GetField(fields[j].fieldNameWithoutIndexing);

                    // フィールド名が配列要素の場合は配列のデータをセットしておく.
                    if (fields[j].isArrayField) {
                        int length = 0;
                        var v = Array.CreateInstance(info.FieldType.GetElementType(), length);
                        info.SetValue(data, v);
                    }
                }

                for (int j = 0; j < content.col; j++) {
                    if (!fields[j].isValid) continue;
                    
                    string fieldName = fields[j].fieldName;
                    if (fieldName.Contains("[")) {
                        fieldName = fieldName.Remove(fieldName.LastIndexOf("["));
                    }

                    FieldInfo info = assetType.GetField(fieldName);
                    Type fieldType;
                    if (fields[j].fieldName.Contains("[")) {
                        fieldType = info.FieldType.GetElementType();
                    } else {
                        fieldType = info.FieldType;
                    }

                    string sValue = content.Get(i, j);
                    object value = null;

                    // 文字列型のときは " でラップする.
                    if (fieldType == typeof(string)) {
                        sValue = "\"" + sValue + "\"";
                    }

                    if (sValue == "") {
                        Debug.LogWarningFormat("{0} {1}行{2}列目: 空の値があります: {3}=\"{4}\"", setting.className, line, j + 1, info.Name, sValue);
                    }
                    else {
                        value = Str2TypeConverter.Convert(fieldType, sValue);

                        if (value == null) {
                            Debug.LogWarningFormat("{0} {1}行{2}列目: 変換に失敗しました: {3}=\"{4}\"", setting.className, line, j + 1, info.Name, sValue);
                        }
                    }

                    // フィールド名が配列要素の場合
                    // もともとの配列データを読み込んで、そこに value を追加した配列を value とする.
                    // TODO 添字を反映させる.
                    if (fields[j].fieldName.Contains("[")) {
                        var t = ((IEnumerable) info.GetValue(data));

                        Type listType = typeof(List<>);
                        var constructedListType = listType.MakeGenericType(info.FieldType.GetElementType());
                        var objects = Activator.CreateInstance(constructedListType);

                        IEnumerable<object> infoValue = ((IEnumerable) info.GetValue(data)).Cast<object>();

                        if (infoValue != null) {
                            for (int k=0; k<infoValue.Count(); k++) {
                                object obj = infoValue.ElementAt(k);
                                objects.GetType().GetMethod("Add").Invoke(objects, new object[] { obj });
                            }
                        }
                        objects.GetType().GetMethod("Add").Invoke(objects, new object[] { value });
                        value = objects.GetType().GetMethod("ToArray").Invoke(objects, new object[] { });
                    }
                    
                    info.SetValue(data, value);
                }

                if (!setting.onlyTableCreate) {
                    EditorUtility.SetDirty(data as UnityEngine.Object);
                }

                if (setting.tableGenerate) {
                    dataList.GetType().GetMethod("Add").Invoke(dataList, new object[] { data });
                }

                show_progress(setting.className, (float)i / content.row, i, content.row);
            }

            if (setting.tableGenerate) {
                EditorUtility.SetDirty(table);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return true;
        }

        public static Type GetTypeByName(string name) {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                foreach (Type type in assembly.GetTypes()) {
                    if (type.Name == name)
                        return type;
                }
            }

            return null;
        }

        private static void show_progress(string name, float progress, int i, int total) {
            EditorUtility.DisplayProgressBar("Progress", progress_msg(name, i, total), progress);
        }

        private static string progress_msg(string name, int i, int total) {
            return string.Format("Creating {0} ({1}/{2})", name, i, total);
        }

    }
}
