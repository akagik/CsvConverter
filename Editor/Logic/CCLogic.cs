using System;
using UnityEditor;
using UnityEngine;

namespace CsvConverter
{
    /// <summary>
    /// CsvConverter に関する一般のロジックをまとめたクラス.
    /// </summary>
    public static class CCLogic
    {
        /// <summary>
        /// グローバルな設定ファイルを検索して、それを返す.
        ///
        /// グローバル設定ファイルはプロジェクト内にただ１つだけ配置し、その場所は問わない。
        /// 設定ファイルは存在しない場合はデフォルトの設定を利用する。
        /// </summary>
        public static GlobalCCSettings GetGlobalSettings()
        {
            string[] settingGUIDArray = AssetDatabase.FindAssets("t:GlobalCsvConverterSettings");

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
    }
}