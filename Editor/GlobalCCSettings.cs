using UnityEngine;

namespace CsvConverter
{
    /// <summary>
    /// CsvConverter のグローバルな設定.
    /// </summary>
    [CreateAssetMenuAttribute(menuName = "CsvConverter/GlobalSettings")]
    public class GlobalCCSettings : ScriptableObject
    {
        /// <summary>
        /// フィールド名の行の index.
        /// 1行目が index 0 に対応する.
        /// </summary>
        public int rowIndexOfName = 0;

        /// <summary>
        /// 型指定の行の index.
        /// </summary>
        public int rowIndexOfType = 1;

        /// <summary>
        /// 列が有効かどうかを指定する行の index.
        /// </summary>
        public int rowIndexOfEnabledColumn = -1;

        /// <summary>
        /// 実際のコンテンツ定義が始まる index.
        /// </summary>
        public int rowIndexOfContentStart = 2;
        
        /// <summary>
        /// enum 定義の実際のコンテンツ定義が始まる index.
        /// </summary>
        public int rowIndexOfEnumContentStart = 1;
    }
}