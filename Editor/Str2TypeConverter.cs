using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;


/// <summary>
/// 文字列を指定された型のオブジェクトに変換する.
/// Primitive 型、Sprite, Vector2, Vector3, 配列型
/// Material, GameObjectをサポート.
/// </summary>
public static class Str2TypeConverter
{
    public static object Convert(Type t, string sValue)
    {
        sValue = sValue.Trim();
#if VERBOSE
        Debug.Log("Convert: #" + sValue + "#");
#endif
        object value = null;
        // 型に応じて string を変換する.
        if (t == typeof(int))
        {
            int intValue;
            if (int.TryParse(sValue, out intValue))
            {
                value = intValue;
            }
            else
            {
                // enum チェックする
                // 例えば int フィールドとして `KeyCode.Tab` のような値も許容するようにする.
                string[] splits = sValue.Split('.');

                if (splits.Length == 2)
                {
                    string typeName = splits[0];
                    string enumId = splits[1];

                    List<Type> candidates = CsvConverter.CsvConverter.GetTypeByName(typeName);

                    if (candidates.Count > 2)
                    {
                        Debug.LogWarningFormat("指定の enum が複数見つかりました", typeName);
                    }
                    else if (candidates.Count == 1)
                    {
                        value = Enum.Parse(candidates[0], enumId);
                    }
                }
            }
        }
        else if (t == typeof(float))
        {
            float floatValue;
            if (float.TryParse(sValue, out floatValue))
            {
                value = floatValue;
            }
        }
        else if (t == typeof(double))
        {
            double doubleValue;
            if (double.TryParse(sValue, out doubleValue))
            {
                value = doubleValue;
            }
        }
        else if (t == typeof(long))
        {
            long longValue;
            if (long.TryParse(sValue, out longValue))
            {
                value = longValue;
            }
        }
        else if (t == typeof(bool))
        {
            bool cValue;
            if (bool.TryParse(sValue, out cValue))
            {
                value = cValue;
            }
        }
        else if (t == typeof(GameObject))
        {
            value = LoadAsset<GameObject>(sValue);
        }
        else if (t == typeof(Sprite))
        {
            value = LoadAsset<Sprite>(sValue);
        }
        else if (t == typeof(Material))
        {
            value = LoadAsset<Material>(sValue);
        }
        else if (t == typeof(string))
        {
            value = sValue.Substring(1, sValue.Length - 2);
        }
        else if (t == typeof(Vector2))
        {
            // 両端の "(" をとる
            sValue = sValue.Substring(1, sValue.Length - 2);
            string[] splits = sValue.Split(',');

            Vector2 vector = Vector2.zero;

            for (int i = 0; i < splits.Length; i++)
            {
                string elemValue = splits[i].Trim();

                if (elemValue == "")
                {
                    return null;
                }

                object elem = Convert(typeof(float), elemValue);

                if (elem == null)
                {
                    return null;
                }

                vector[i] = (float) elem;
            }

            value = vector;
        }
        else if (t == typeof(Vector3))
        {
            // 両端の "(" をとる
            sValue = sValue.Substring(1, sValue.Length - 2);
            string[] splits = sValue.Split(',');

            Vector3 vector = Vector3.zero;

            for (int i = 0; i < splits.Length; i++)
            {
                string elemValue = splits[i].Trim();

                if (elemValue == "")
                {
                    return null;
                }

                object elem = Convert(typeof(float), elemValue);

                if (elem == null)
                {
                    return null;
                }

                vector[i] = (float) elem;
            }

            value = vector;
        }
        else if (t.IsArray)
        {
            // TODO 文字列中にカンマがあったときに対応できない.
            sValue = sValue.Substring(1, sValue.Length - 2);
            string[] splits = sValue.Split(',');

            // TODO List<object> だと Cast Error が発生する...
            //List<int> objects = new List<int>();

            //Debug.Log("t.GetElementType() = " + t.GetElementType());
            Type listType = typeof(List<>);
            var constructedListType = listType.MakeGenericType(t.GetElementType());

            var objects = Activator.CreateInstance(constructedListType);

            for (int i = 0; i < splits.Length; i++)
            {
                Type elemType = t.GetElementType();
                string elemValue = splits[i].Trim();

                if (elemValue == "")
                {
                    continue;
                }

                object elem = Convert(elemType, elemValue);

                if (elem == null)
                {
                    return null;
                }

                //objects.GetType().
                //objects.Add((int)elem);
                objects.GetType().GetMethod("Add").Invoke(objects, new object[] {elem});
            }

            //value = System.Convert.ChangeType(objects.ToArray(), t);
            //value = objects.ToArray();
            value = objects.GetType().GetMethod("ToArray").Invoke(objects, new object[] { });
        }
        // ユーザー定義型の enum
        else if (t.IsEnum)
        {
            Type fieldType = t;
            value = Enum.Parse(fieldType, sValue);
        }
        else
        {
            return null;
        }

        return value;
    }

    /// <summary>
    /// 指定されたパス文字列から拡張子を削除して返します
    /// </summary>
    public static string GetPathWithoutExtension(string path)
    {
        var extension = Path.GetExtension(path);
        if (string.IsNullOrEmpty(extension))
        {
            return path;
        }

        return path.Replace(extension, string.Empty);
    }

    public static object LoadAsset<T>(string sValue) where T : UnityEngine.Object
    {
        return LoadAsset(sValue, typeof(T));
    }

    public static object LoadAsset(string sValue, Type type)
    {
        if (sValue == "")
        {
            return null;
        }

        string typeName = type.Name;
        string path = Path.Combine("Assets", sValue);
        var asset = AssetDatabase.LoadAssetAtPath(path, type);

        // フルパスで見つからない場合はファイル名＋指定フィルターで最初に見つかったものを返す.
        if (asset == null)
        {
            string filter = $"\"{GetPathWithoutExtension(sValue)}\" t:{typeName}";
            string[] guids = AssetDatabase.FindAssets(filter);

            if (guids.Length == 0)
            {
                Debug.LogErrorFormat("Not found {0}: \"{1}\"", typeName, sValue);
            }

            if (guids.Length > 1)
            {
                Debug.LogWarningFormat("{0} \"{1}\" に対して複数のアセットが見つかりました:\n{2}", typeName, sValue,
                    string.Join("\n", guids));
            }

            if (guids.Length > 0)
            {
                path = AssetDatabase.GUIDToAssetPath(guids[0]);
                asset = AssetDatabase.LoadAssetAtPath(path, type);
            }
            else
            {
                asset = null;
            }
        }

        return asset;
    }
}