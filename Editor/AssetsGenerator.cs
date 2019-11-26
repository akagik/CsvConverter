using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Object = UnityEngine.Object;

namespace CsvConverter
{
    public class AssetsGenerator
    {
        private CsvConverterSettings.Setting setting;
        private Type[] customAssetTypes;

        private Field[] fields;
        private CsvData content;

        // setup 情報
        private Type assetType;
        public string dstFolder { get; private set; }
        private int[] keyIndexes;

        // テーブル情報
        // これらの情報は setting.tableGenerate が true のときのみ利用される.
        private Type tableType;
        private ScriptableObject tableInstance;
        private object dataList = null;

        // ログ情報
        public Result result;

        public class Result
        {
            public int createdRowCount;
            public Object tableInstance;
        }

        public int contentRowCount => content.row;

        public AssetsGenerator(CsvConverterSettings.Setting _setting, Field[] _fields, CsvData _content)
        {
            setting = _setting;
            fields = _fields;
            content = _content;
        }

        public void SetCustomAssetTypes(Type[] _customAssetTypes)
        {
            customAssetTypes = _customAssetTypes;
        }

        public void Setup(Type _assetType, string settingPath)
        {
            result = new Result();

            assetType = _assetType;
            dstFolder = CCLogic.GetFilePathRelativesToAssets(settingPath, setting.destination);

            // Asset の名前をつけるときに利用する key.
            keyIndexes = ClassGenerator.FindKeyIndexes(setting, fields);
        }

        // テーブルありの設定
        public void Setup(Type _assetType, Type _tableType, string settingPath)
        {
            tableType = _tableType;
            Setup(_assetType, settingPath);

            FieldInfo dataListField = tableType.GetField(ClassGenerator.ROWS,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);

            // 既存のテーブルインスタンスがストレージにあればロードし、なければ新規に作成する.
            string filePath = Path.Combine(dstFolder, setting.tableAssetName + ".asset");
            tableInstance = AssetDatabase.LoadAssetAtPath<ScriptableObject>(filePath);
            if (tableInstance == null)
            {
                tableInstance = ScriptableObject.CreateInstance(tableType);
                AssetDatabase.CreateAsset(tableInstance, filePath);
            }
            result.tableInstance = tableInstance;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            dataList = dataListField.GetValue(tableInstance);

            // 初めてテーブルを作成する場合は null になっているので、
            // インスタンスを作成して、tableInstance に代入する。
            if (dataList == null)
            {
                Type listType = typeof(List<>);
                var constructedListType = listType.MakeGenericType(dataListField.FieldType.GenericTypeArguments[0]);
                dataList = Activator.CreateInstance(constructedListType);
                dataListField.SetValue(tableInstance, dataList);
            }

            dataList.GetType().GetMethod("Clear").Invoke(dataList, null);
        }

        private string createAssetName(int rowIndex)
        {
            string fileName = "";

            // キーがある場合は、キーの値をアセット名に含める.
            if (keyIndexes.Length > 0)
            {
                fileName = setting.className;
                for (int j = 0; j < keyIndexes.Length; j++)
                {
                    int keyIndex = keyIndexes[j];
                    fileName += "_" + content.Get(rowIndex, keyIndex).Trim();
                }

                fileName += ".asset";
            }
            else
            {
                fileName = setting.className + rowIndex + ".asset";
            }

            return fileName;
        }

        private bool checkKeyIsValid(int rowIndex)
        {
            for (int j = 0; j < keyIndexes.Length; j++)
            {
                int keyIndex = keyIndexes[j];
                string key = content.Get(rowIndex, keyIndex).Trim();

                if (key == "")
                {
                    return false;
                }
            }

            return true;
        }

        [Flags]
        public enum ResultType
        {
            None = 0,
            SkipNoKey = 1 << 0,
            EmptyCell = 1 << 1,
            ConvertFails = 1 << 2,
            
        }

        public ResultType CreateCsvAssetAt(int i)
        {
            ResultType resultType = ResultType.None;
            
            int line = i + 2 + 1;

            if (!checkKeyIsValid(i))
            {
                return ResultType.SkipNoKey;
            }

            string fileName = createAssetName(i);
            string filePath = Path.Combine(dstFolder, fileName);

            object data = null;

            // テーブルのみ作成する場合は ScriptableObject としてではなく
            // 通常のインスタンスとして作成する.
            if (setting.tableGenerate && setting.onlyTableCreate)
            {
                data = Activator.CreateInstance(assetType);
            }
            else
            {
                data = AssetDatabase.LoadAssetAtPath(filePath, assetType);
                if (data == null)
                {
                    data = ScriptableObject.CreateInstance(assetType);
                    AssetDatabase.CreateAsset(data as UnityEngine.Object, filePath);
                    Debug.LogFormat("Create \"{0}\"", filePath);
                }
                else
                {
                    Debug.LogFormat("Update \"{0}\"", filePath);
                }
            }

            // フィールド名に[]が付いているカラムを先に検索し、配列インスタンスを生成しておく.
            for (int j = 0; j < content.col; j++)
            {
                if (!fields[j].isValid) continue;

                FieldInfo info = assetType.GetField(fields[j].fieldNameWithoutIndexing);

                // フィールド名が配列要素の場合は配列のデータをセットしておく.
                if (fields[j].isArrayField)
                {
                    int length = 0;
                    var v = Array.CreateInstance(info.FieldType.GetElementType(), length);
                    info.SetValue(data, v);
                }
            }

            // 各列に対して、有効なフィールドのみ値を読み込んで実際のデータに変換し、この行のインスタンス data に代入する.
            for (int j = 0; j < content.col; j++)
            {
                if (!fields[j].isValid) continue;

                FieldInfo info = assetType.GetField(fields[j].fieldNameWithoutIndexing);
                Type fieldType = fields[j].GetTypeAs(info);

                // (i, j) セルに格納されている生のテキストデータを fieldType 型に変換する.
                object value = null;
                {
                    string sValue = content.Get(i, j);

                    // 文字列型のときは " でラップする.
                    if (fieldType == typeof(string))
                    {
                        sValue = "\"" + sValue + "\"";
                    }

                    if (sValue == "")
                    {
                        Debug.LogWarningFormat("{0} {1}行{2}列目: 空の値があります: {3}=\"{4}\"", setting.className, line,
                            j + 1, info.Name, sValue);
                    }
                    else
                    {
                        value = Str2TypeConverter.Convert(fieldType, sValue);

                        // 基本型で変換できないときは GlobalSettings の customAssetTypes で変換を試みる.
                        if (value == null)
                        {
                            foreach (var type in customAssetTypes)
                            {
                                if (fieldType == type)
                                {
                                    value = Str2TypeConverter.LoadAsset(sValue, type);

                                    if (value != null)
                                    {
                                        break;
                                    }
                                }
                            }
                        }

                        if (value == null)
                        {
                            Debug.LogWarningFormat("{0} {1}行{2}列目: 変換に失敗しました: {3}=\"{4}\"", setting.className, line,
                                j + 1, info.Name, sValue);
                        }
                    }
                }

                // フィールド名が配列要素の場合
                // もともとの配列データを読み込んで、そこに value を追加した配列を value とする.
                // TODO 添字を反映させる.
                if (fields[j].isArrayField)
                {
                    var t = ((IEnumerable) info.GetValue(data));

                    Type listType = typeof(List<>);
                    var constructedListType = listType.MakeGenericType(info.FieldType.GetElementType());
                    var objects = Activator.CreateInstance(constructedListType);

                    IEnumerable<object> infoValue = ((IEnumerable) info.GetValue(data)).Cast<object>();

                    if (infoValue != null)
                    {
                        for (int k = 0; k < infoValue.Count(); k++)
                        {
                            object obj = infoValue.ElementAt(k);
                            objects.GetType().GetMethod("Add").Invoke(objects, new object[] {obj});
                        }
                    }

                    objects.GetType().GetMethod("Add").Invoke(objects, new object[] {value});
                    value = objects.GetType().GetMethod("ToArray").Invoke(objects, new object[] { });
                }

                info.SetValue(data, value);
            }

            if (!setting.tableGenerate || !setting.onlyTableCreate)
            {
                EditorUtility.SetDirty(data as UnityEngine.Object);
            }

            if (setting.tableGenerate)
            {
                dataList.GetType().GetMethod("Add").Invoke(dataList, new object[] {data});
            }

            result.createdRowCount++;
            return resultType;
        }
    }
}