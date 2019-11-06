using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NUnit.Framework;

namespace CsvConverter
{
    public class Test_CsvConverter
    {
        [Test]
        public void Test_CsvDataSlice()
        {
            CsvData csvData = new CsvData();
            csvData.SetFromList(new List<List<string>>
            {
                new List<string> {"00", "01", "02"},
                new List<string> {"10", "11", "12"},
                new List<string> {"20", "21", "22"},
            });
            
            var d0 = csvData.SliceColumn(0);
            Assert.AreEqual(3, d0.content.Length);
            Assert.AreEqual(3, d0.content[0].data.Length);
            
            var d1 = csvData.SliceColumn(1);
            Assert.AreEqual(3, d1.content.Length);
            Assert.AreEqual(2, d1.content[0].data.Length);
            Assert.AreEqual("01", d1.Get(0, 0));
            
            var d2 = csvData.SliceColumn(0, -1);
            Assert.AreEqual(3, d2.content.Length);
            Assert.AreEqual(2, d2.content[0].data.Length);
            Assert.AreEqual("00", d2.Get(0, 0));
            Assert.AreEqual("01", d2.Get(0, 1));
        }
    }
}