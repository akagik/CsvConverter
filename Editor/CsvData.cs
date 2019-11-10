using System;
using UnityEngine;
using System.Collections.Generic;

namespace CsvConverter
{
    [Serializable]
    public class CsvData
    {
        public Row[] content;

        public int row
        {
            get { return content.Length; }
        }

        public int col
        {
            get { return content[0].data.Length; }
        }

        [Serializable]
        public class Row
        {
            public string[] data;

            public Row(int col)
            {
                data = new string[col];
            }
            
            public Row Slice(int startIndex, int endIndex = int.MaxValue)
            {
                int n = data.Length;

                if (endIndex >= n)
                {
                    endIndex = n;
                }
                else if (endIndex <= -n)
                {
                    return new Row(0);
                }
                else
                {
                    endIndex = (endIndex % n + n) % n;
                }

                if (startIndex >= endIndex)
                {
                    return new Row(0);
                }

                Row row = new Row(endIndex - startIndex);
                Array.Copy(data, startIndex, row.data, 0, row.data.Length);
                return row;
            }
        }

        public CsvData()
        {
            this.content = new Row[0];
        }

        public CsvData(Row[] rows)
        {
            this.content = rows;
        }

        public CsvData Slice(int startIndex, int endIndex = int.MaxValue)
        {
            int n = content.Length;

            if (endIndex >= n)
            {
                endIndex = n;
            }
            else if (endIndex <= -n)
            {
                return new CsvData();
            }
            else
            {
                endIndex = (endIndex % n + n) % n;
            }

            if (startIndex >= endIndex)
            {
                return new CsvData();
            }

            Row[] newContent = new Row[endIndex - startIndex];
            Array.Copy(content, startIndex, newContent, 0, newContent.Length);
            return new CsvData(newContent);
        }
        
        public CsvData SliceColumn(int startIndex, int endIndex = int.MaxValue)
        {
            int n = col;

            if (endIndex >= n)
            {
                endIndex = n;
            }
            else if (endIndex <= -n)
            {
                return new CsvData();
            }
            else
            {
                endIndex = (endIndex % n + n) % n;
            }

            Row[] newContent = new Row[row];
            for (int i = 0; i < newContent.Length; i++)
            {
                newContent[i] = content[i].Slice(startIndex, endIndex);
            }
            return new CsvData(newContent);
        }

        public string Get(int i, int j)
        {
            return content[i].data[j];
        }

        public void Set(int i, int j, string v)
        {
            content[i].data[j] = v;
        }

        public static Row[] CreateTable(int row, int col)
        {
            Row[] rows = new Row[row];
            for (int i = 0; i < row; i++)
            {
                rows[i] = new Row(col);
            }

            return rows;
        }

        public void SetFromList(List<List<string>> list)
        {
            int maxCol = -1;

            foreach (List<string> row in list)
            {
                if (row.Count > maxCol)
                {
                    maxCol = row.Count;
                }
            }

            content = CreateTable(list.Count, maxCol);

            for (int i = 0; i < row; i++)
            {
                for (int j = 0; j < col; j++)
                {
                    if (j < list[i].Count)
                    {
                        Set(i, j, list[i][j]);
                    }
                    else
                    {
                        Set(i, j, "");
                    }
                }
            }
        }

        public override string ToString()
        {
            string s = "";

            for (int i = 0; i < row; i++)
            {
                for (int j = 0; j < col; j++)
                {
                    s += "\"" + Get(i, j) + "\", ";
                }

                s = s.Substring(0, s.Length - 2);
                s += "\n";
            }

            return s;
        }
    }
}