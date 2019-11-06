using System;
using Generic;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace CsvConverter
{
    public static class CsvLogic
    {
        public static CsvData GetValidCsvData(string csvText, GlobalCCSettings gSettings)
        {
            var csvAsList = CsvParser.ReadAsList(csvText);

            CsvData csvData = new CsvData();
            csvData.SetFromList(csvAsList);

            // END マーカーが有効な場合は END 以下を無視するようにする.
            if (gSettings.isEndMarkerEnabled)
            {
                int endMarkerIndex = gSettings.columnIndexOfEndMarker;
                string marker = gSettings.endMarker;

                if (endMarkerIndex < 0 || csvData.col <= endMarkerIndex)
                {
                    throw new Exception("無効な columnIndexOfEndMarker です: " + endMarkerIndex);
                }

                for (int i = 0; i < csvData.row; i++)
                {
                    if (csvData.Get(i, endMarkerIndex).Trim() == marker)
                    {
                        csvData = csvData.Slice(0, i);
                        break;
                    }
                }
            }
            
            // 無効な列を除外する
            csvData = csvData.SliceColumn(gSettings.columnIndexOfTableStart);
            
            return csvData;
        }
        
        public static Field[] GetFieldsFromHeader(CsvData csv, GlobalCCSettings gSettings)
        {
            CsvData nameHeaders = csv.Slice(gSettings.rowIndexOfName, gSettings.rowIndexOfName + 1);
            CsvData typeHeaders = csv.Slice(gSettings.rowIndexOfType, gSettings.rowIndexOfType + 1);

            var fields = new Field[csv.col];
            for (int i = 0; i < fields.Length; i++)
            {
                fields[i] = new Field();
            }

            // enabled check
            if (gSettings.rowIndexOfEnabledColumn != -1)
            {
                CsvData enabledHeaders =
                    csv.Slice(gSettings.rowIndexOfEnabledColumn, gSettings.rowIndexOfEnabledColumn + 1);

                for (int col = 0; col < csv.col; col++)
                {
                    string enabledCell = enabledHeaders.Get(0, col);

                    if (!GetEnabledColumn(enabledCell))
                    {
                        fields[col].isValid = false;
                    }
                }
            }

            // get field names;
            for (int col = 0; col < csv.col; col++)
            {
                string fieldName = nameHeaders.Get(0, col);

                fieldName = fieldName.Trim();

                if (fieldName == string.Empty)
                {
                    fields[col].isValid = false;
                    continue;
                }

                fields[col].fieldName = fieldName;
            }

            // set field types;
            for (int col = 0; col < csv.col; col++)
            {
                if (!fields[col].isValid) continue;

                string typeName = typeHeaders.Get(0, col).Trim();

                if (typeName == string.Empty)
                {
                    fields[col].isValid = false;
                    continue;
                }

                fields[col].typeName = typeName;
            }

            return fields;
        }

        /// <summary>
        /// EnabledColumn のセルに入っているテキストから、
        /// この列が有効かどうかを判定する.
        /// </summary>
        public static bool GetEnabledColumn(string cell)
        {
            // 空欄の場合は false とする
            if (string.IsNullOrWhiteSpace(cell))
            {
                return false;
            }
            // bool で解釈できる場合はそのパースされた bool 値を返す.
            else if (bool.TryParse(cell, out var boolValue))
            {
                return boolValue;
            }

            // それ以外のケースの場合は true と扱う.
            return true;
        }
    }
}