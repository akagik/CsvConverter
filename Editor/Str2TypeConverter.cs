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
/// をサポート.
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
        }
        else if (t == typeof(float))
        {
            float floatValue;
            if (float.TryParse(sValue, out floatValue))
            {
                value = floatValue;
            }
        } else if (t == typeof(double)) {
            double doubleValue;
            if (double.TryParse(sValue, out doubleValue)) {
                value = doubleValue;
            }
        } else if (t == typeof(long)) {
            long longValue;
            if (long.TryParse(sValue, out longValue)) {
                value = longValue;
            }
        } else if (t == typeof(bool))
        {
            bool cValue;
            if (bool.TryParse(sValue, out cValue))
            {
                value = cValue;
            }
        }
        else if (t == typeof(UnityEngine.Sprite))
        {
            if (sValue == "")
            {
                return value;
            }
            string path = Path.Combine("Assets", sValue);
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);

            // フルパスで見つからない場合はファイル名＋Spriteフィルターで最初に見つかったものを返す.
            if (sprite == null)
            {
                string[] guids = AssetDatabase.FindAssets("\"" + GetPathWithoutExtension(sValue) + "\" t:Sprite");

                if (guids.Length == 0)
                {
                    Debug.LogErrorFormat("Not found sprite: \"{0}\"", sValue);
                }
                if (guids.Length > 1)
                {
                    Debug.LogWarningFormat("Sprite \"{0}\" に対して複数のアセットが見つかりました:\n{1}", sValue, string.Join("\n", guids));
                }

                path = AssetDatabase.GUIDToAssetPath(guids[0]);
                sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
            value = sprite;
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
                vector[i] = (float)elem;
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
                vector[i] = (float)elem;
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

            Debug.Log("t.GetElementType() = " + t.GetElementType());
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
                objects.GetType().GetMethod("Add").Invoke(objects, new object[] { elem });
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
            Debug.LogError("サポートされていない型です: " + t);
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
}
