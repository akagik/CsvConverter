using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CsvConverter
{
    /// <summary>
    /// CsvConverter に関する一般のロジックをまとめたクラス.
    /// </summary>
    public static class CCLogic
    {
        public static string SETTINGS_KEY = "CsvConverter/settings";

        /// <summary>
        /// グローバルな設定ファイルを検索して、それを返す.
        ///
        /// グローバル設定ファイルはプロジェクト内にただ１つだけ配置し、その場所は問わない。
        /// 設定ファイルは存在しない場合はデフォルトの設定を利用する。
        /// </summary>
        public static GlobalCCSettings GetGlobalSettings()
        {
            string[] settingGUIDArray = AssetDatabase.FindAssets("t:" + typeof(GlobalCCSettings));

            if (settingGUIDArray.Length >= 2)
            {
                throw new Exception("GlobalCsvConverterSettings がプロジェクト内に複数存在します");
            }
            // グローバルな設定ファイルが見つからない場合はその場で一時的に生成する.
            else if (settingGUIDArray.Length == 0)
            {
                return ScriptableObject.CreateInstance<GlobalCCSettings>();
            }

            string path = AssetDatabase.GUIDToAssetPath(settingGUIDArray[0]);
            return AssetDatabase.LoadAssetAtPath<GlobalCCSettings>(path);
        }

        /// <summary>
        /// 設定ファイルのディレクトリと対象パスから Assets からのパスを求める.
        /// 対象パスが "/" から始まるかどうかで絶対パスか相対パスかを判断する.
        /// </summary>
        public static string GetFilePathRelativesToAssets(string settingPath, string destination)
        {
            if (destination.StartsWith("/"))
            {
                return "Assets/" + destination.Substring(1);
            }

            // ".." などを解決するために一度 FullPath を取得したのち、Assets より前を削除する.
            int index = Path.GetFullPath(".").Length;
            return Path.GetFullPath(Path.Combine(settingPath, destination)).Substring(index + 1);
        }

        public static string GetFullPath(string settingPath, string destination)
        {
            string pathRelativeToAssets = GetFilePathRelativesToAssets(settingPath, destination);
            
            // "Assets/" までを除外して dataPath と結合
            return Path.Combine(Application.dataPath, pathRelativeToAssets.Substring(7));
        }

        /// <summary>
        /// メインの出力アセットへのパスを指す.
        /// </summary>
        public static string GetMainOutputPath(CsvConverterSettings.Setting s, string settingsPath)
        {
            var dst = CCLogic.GetFilePathRelativesToAssets(settingsPath, s.destination);

            if (s.isEnum)
            {
                return Path.Combine(dst, s.className + ".cs");
            }

            if (s.tableGenerate)
            {
                return Path.Combine(dst, s.tableAssetName + ".asset");
            }

            return dst;
        }
    }
}