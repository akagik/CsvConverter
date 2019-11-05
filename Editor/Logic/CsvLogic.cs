using Generic;

namespace CsvConverter
{
    public static class CsvLogic
    {
        public static Field[] GetFieldsFromHeader(CsvData csv, GlobalCsvConverterSettings gSettings)
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