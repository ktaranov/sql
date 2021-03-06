﻿using CsvHelper;
using Microsoft.SqlServer.Server;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bi_dev.sql.mssql.extensions.@string.csv
{
    public static class Constants
    {
        public const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss";
    }
    public static class Utils
    {
        private static List<string[]> parseCsv(string value, string delimiter)
        {
            List<string[]> result = new List<string[]>();
            using (TextReader reader = new StringReader(value))
            {
                CsvParser csv = new CsvParser(reader,CultureInfo.CurrentCulture);
                csv.Configuration.Delimiter = (string.IsNullOrEmpty(delimiter) ? ";" : delimiter);
                while (true)
                {
                    var row = csv.Read();
                    if (row == null)
                    {
                        break;
                    }
                    else
                    {
                        result.Add(row);
                    }
                }
            }
            return result;
        }
        public static void FillRow(Object obj, out SqlInt32 rowType, out SqlInt32 key, out SqlChars value)
        {
            TableType.FillRow(obj, out rowType, out key,out value);
        }
        [SqlFunction(FillRowMethodName = "FillRow")]
        public static IEnumerable ParseCsv(string value, string delimiter, bool nullWhenError)
        {
            try
            {
                List<TableType> l = new List<TableType>();
                var result = parseCsv(value, delimiter);
                for (int i = 0; i < result.Count; i++)
                {
                    for (int j = 0; j < result[i].Length; j++)
                    {
                        l.Add(new TableType(i, j, result[i][j]));
                    }
                }
                return l;
            }
            catch (Exception e)
            {
                return Common.ThrowIfNeeded<IEnumerable>(e, nullWhenError);
            }
        }
        public static string JsonToCsv(string jsonObject, string delimiter, string dateTimeFormat, bool nullWhenError)
        {
            try
            {
                List<JObject> values = JsonConvert.DeserializeObject<List<JObject>>(jsonObject);
                string result = "";
                using (TextWriter tw = new StringWriter())
                {
                    using (CsvWriter cw = new CsvWriter(tw, CultureInfo.CurrentCulture))
                    {
                        cw.Configuration.Delimiter = (string.IsNullOrEmpty(delimiter) ? ";" : delimiter);
                        dateTimeFormat = string.IsNullOrWhiteSpace(dateTimeFormat) ? Constants.DateTimeFormat : dateTimeFormat;
                        for (int i = 0; i < values.Count; i++)
                        {
                            List<JProperty> properties = values[i].Properties().ToList();
                            for (int j = 0; j < properties.Count; j++)
                            {
                                var property = properties[j];
                                string valueString;
                                if (property.Value.Type == JTokenType.Date)
                                {
                                    valueString = ((DateTime)property.Value).ToString(dateTimeFormat, CultureInfo.CurrentCulture);
                                }
                                else
                                {
                                    valueString = property.Value.ToString();
                                }
                                cw.WriteField(valueString);
                            }
                            cw.NextRecord();
                        }
                    }
                    result = tw.ToString();

                }
                return result;
            }
            catch (Exception e)
            {
                return Common.ThrowIfNeeded<string>(e, nullWhenError);
            }
        }
    }
}
