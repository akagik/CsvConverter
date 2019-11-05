using UnityEngine;
using UnityEditor;
using System.IO;

namespace CsvConverter
{
    public class EnumGenerator
    {
        const string FIELD_FORMAT = "    {0} = {1},\n";
        public static readonly string ID_NAME = "ID";
        public static readonly string VALUE_NAME = "VALUE";


        public static string Generate(string name, CsvData header, CsvData contents)
        {
            Field[] fields = GetFieldsFromHeader(header);

            string classData = "";
            classData += "public enum " + name + "\n";
            classData += "{\n";

            for (int i = 0; i < contents.row; i++)
            {
                int line = i + 2;

#if VERBOSE
                Debug.Log("line:" + line);
#endif

                string eid = "";
                int value = -1;
                bool isOkEid = false;
                bool isOkValue = false;

                for (int j = 0; j < contents.col; j++)
                {
                    Field f = fields[j];
                    if (!f.isValid) continue;

                    if (f.fieldName == ID_NAME)
                    {
                        isOkEid = true;
                        eid = contents.Get(i, j).Trim();
                    }
                    else if (f.fieldName == VALUE_NAME)
                    {
                        isOkValue = true;
                        string vs = contents.Get(i, j).Trim();

                        if (!int.TryParse(vs, out value))
                        {
                            Debug.LogWarningFormat("{0} line {1}: int に変換出来ない値です: \"{2}\"", name, line, vs);
                            isOkValue = false;
                            continue;
                        }
                    }
                }

                if (!isOkEid || !isOkValue)
                {
                    continue;
                }

                classData += string.Format(FIELD_FORMAT, eid, value);
            }

            classData += "}";
            
            return classData;
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
                fields[col].typeName = fields[col].fieldName;
            }

            return fields;
        }
    }
}