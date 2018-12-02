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

    }
}
