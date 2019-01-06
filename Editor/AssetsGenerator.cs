using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace CsvConverter
{
    public class AssetsGenerator
    {
        private CsvConverterSettings.Setting setting;
        private Type assetType;
        private string dstFolder;

        // テーブル情報
        // これらの情報は setting.tableGenerate が true のときのみ利用される.
        private Type tableType;
        private ScriptableObject tableInstance;
        private object dataList = null;

        public AssetsGenerator(CsvConverterSettings.Setting _setting) {
            setting = _setting;
        }

        public void Setup(Type _assetType) {
            assetType = _assetType;
            dstFolder = Path.Combine("Assets",setting.destination);
        }

        // テーブルありの設定
        public void Setup(Type _assetType, Type _tableType) {
            tableType = _tableType;
            Setup(_assetType);

            FieldInfo dataListField = tableType.GetField(ClassGenerator.ROWS);

            // 既存のテーブルインスタンスがストレージにあればロードし、なければ新規に作成する.
            string filePath = Path.Combine(dstFolder,setting.tableAssetName + ".asset");
            tableInstance = AssetDatabase.LoadAssetAtPath<ScriptableObject>(filePath);
            if(tableInstance == null)
            {
                tableInstance = ScriptableObject.CreateInstance(tableType);
                AssetDatabase.CreateAsset(tableInstance,filePath);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            dataList = dataListField.GetValue(tableInstance);
            dataList.GetType().GetMethod("Clear").Invoke(dataList,null);
        }

        public bool CreateCsvAssets(Field[] fields,CsvData content)
        {
            // Asset の名前をつけるときに利用する key.
            int[] keyIndexes = ClassGenerator.FindKeyIndexes(setting,fields);

            // 各行に対してアセットを生成する.
            for(int i = 0; i < content.row; i++)
            {
                int line = i + 2 + 1;

#if VERBOSE
                Debug.Log("line:" + line);
#endif

                string fileName = setting.className + i + ".asset";
                if(keyIndexes.Length > 0)
                {
                    fileName = setting.className;
                    for(int j = 0; j < keyIndexes.Length; j++)
                    {
                        int keyIndex = keyIndexes[j];

                        string key = content.Get(i,keyIndex).Trim();

                        if(key == "")
                        {
                            Debug.LogWarningFormat("{0} line {1}: key が存在しない行をスキップしました",setting.className,line);
                            continue;
                        }
                        fileName += "_" + key;
                    }
                    fileName += ".asset";
                }

                string filePath = Path.Combine(dstFolder,fileName);

                object data = null;

                // テーブルのみ作成する場合は ScriptableObject としてではなく
                // 通常のインスタンスとして作成する.
                if(setting.tableGenerate && setting.onlyTableCreate)
                {
                    data = Activator.CreateInstance(assetType);
                }
                else
                {
                    data = AssetDatabase.LoadAssetAtPath(filePath,assetType);
                    if(data == null)
                    {
                        data = ScriptableObject.CreateInstance(assetType);
                        AssetDatabase.CreateAsset(data as UnityEngine.Object,filePath);
                        Debug.LogFormat("Create \"{0}\"",filePath);
                    }
                    else {
                        Debug.LogFormat("Update \"{0}\"",filePath);
                    }
                }

                // フィールド名に[]が付いているカラムを先に検索し、配列インスタンスを生成しておく.
                for(int j = 0; j < content.col; j++)
                {
                    if(!fields[j].isValid) continue;

                    FieldInfo info = assetType.GetField(fields[j].fieldNameWithoutIndexing);

                    // フィールド名が配列要素の場合は配列のデータをセットしておく.
                    if(fields[j].isArrayField)
                    {
                        int length = 0;
                        var v = Array.CreateInstance(info.FieldType.GetElementType(),length);
                        info.SetValue(data,v);
                    }
                }

                for(int j = 0; j < content.col; j++)
                {
                    if(!fields[j].isValid) continue;

                    string fieldName = fields[j].fieldName;
                    if(fieldName.Contains("["))
                    {
                        fieldName = fieldName.Remove(fieldName.LastIndexOf("["));
                    }

                    FieldInfo info = assetType.GetField(fieldName);
                    Type fieldType;
                    if(fields[j].fieldName.Contains("["))
                    {
                        fieldType = info.FieldType.GetElementType();
                    }
                    else
                    {
                        fieldType = info.FieldType;
                    }

                    string sValue = content.Get(i,j);
                    object value = null;

                    // 文字列型のときは " でラップする.
                    if(fieldType == typeof(string))
                    {
                        sValue = "\"" + sValue + "\"";
                    }

                    if(sValue == "")
                    {
                        Debug.LogWarningFormat("{0} {1}行{2}列目: 空の値があります: {3}=\"{4}\"",setting.className,line,j + 1,info.Name,sValue);
                    }
                    else
                    {
                        value = Str2TypeConverter.Convert(fieldType,sValue);

                        if(value == null)
                        {
                            Debug.LogWarningFormat("{0} {1}行{2}列目: 変換に失敗しました: {3}=\"{4}\"",setting.className,line,j + 1,info.Name,sValue);
                        }
                    }

                    // フィールド名が配列要素の場合
                    // もともとの配列データを読み込んで、そこに value を追加した配列を value とする.
                    // TODO 添字を反映させる.
                    if(fields[j].fieldName.Contains("["))
                    {
                        var t = ((IEnumerable)info.GetValue(data));

                        Type listType = typeof(List<>);
                        var constructedListType = listType.MakeGenericType(info.FieldType.GetElementType());
                        var objects = Activator.CreateInstance(constructedListType);

                        IEnumerable<object> infoValue = ((IEnumerable)info.GetValue(data)).Cast<object>();

                        if(infoValue != null)
                        {
                            for(int k = 0; k < infoValue.Count(); k++)
                            {
                                object obj = infoValue.ElementAt(k);
                                objects.GetType().GetMethod("Add").Invoke(objects,new object[] { obj });
                            }
                        }
                        objects.GetType().GetMethod("Add").Invoke(objects,new object[] { value });
                        value = objects.GetType().GetMethod("ToArray").Invoke(objects,new object[] { });
                    }

                    info.SetValue(data,value);
                }

                if(!setting.tableGenerate || !setting.onlyTableCreate)
                {
                    EditorUtility.SetDirty(data as UnityEngine.Object);
                }

                if(setting.tableGenerate)
                {
                    dataList.GetType().GetMethod("Add").Invoke(dataList,new object[] { data });
                }

                show_progress(setting.className,(float)i / content.row,i,content.row);
            }

            if(setting.tableGenerate)
            {
                EditorUtility.SetDirty(tableInstance);
                Debug.LogFormat("Create \"{0}\"", Path.Combine(dstFolder,setting.tableAssetName + ".asset"));
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return true;
        }

        private static void show_progress(string name,float progress,int i,int total)
        {
            EditorUtility.DisplayProgressBar("Progress",progress_msg(name,i,total),progress);
        }

        private static string progress_msg(string name,int i,int total)
        {
            return string.Format("Creating {0} ({1}/{2})",name,i,total);
        }
    }
}