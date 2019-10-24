using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace CsvConverter
{
    [SerializeField]
    public class Field
    {
        public string fieldName = "";
        public string typeName = "";
        public bool isValid = true;

        public string fieldNameWithoutIndexing
        {
            get
            {
                if (!isArrayField)
                {
                    return fieldName;
                }

                return fieldName.Remove(fieldName.LastIndexOf("["));
            }
        }

        public override string ToString()
        {
            return string.Format("{0}({1}, {2})", fieldName, typeName, isValid);
        }

        public bool isArrayField
        {
            get { return fieldName.Contains("["); }
        }

        public bool hasArrayIndex
        {
            get
            {
                if (isArrayField)
                {
                    return false;
                }

                return fieldName[fieldName.IndexOf("[") + 1] != ']';
            }
        }

        public int GetArrayIndex()
        {
            int firstIndex = fieldName.IndexOf("[");
            int endIndex = fieldName.IndexOf("]");
            return int.Parse(fieldName.Substring(firstIndex + 1, endIndex - firstIndex - 1));
        }

        public Type GetTypeAs(FieldInfo info)
        {
            if (isArrayField)
            {
                return info.FieldType.GetElementType();
            }

            return info.FieldType;
        }
    }
}