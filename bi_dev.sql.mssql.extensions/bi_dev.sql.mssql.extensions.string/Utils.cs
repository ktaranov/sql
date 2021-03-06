﻿using Microsoft.SqlServer.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using bi_dev.sql.mssql.extensions;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using System.Security.Cryptography;
using System.IO;
using System.Data.SqlTypes;
using System.Collections;

namespace bi_dev.sql.mssql.extensions.@string
{
    public static class Utils
    {
        [SqlFunction]
        public static long? RemoveNonDigits(string value, bool nullWhenError)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(value)) return null;
                else return long.Parse(new string(value.Where(c => char.IsDigit(c)).ToArray()));
            }
            catch (Exception e)
            {
                return Common.ThrowIfNeeded<long?>(e, nullWhenError);
            }
        }
        [SqlFunction]
        public static string UrlDecode(string value, bool nullWhenError)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(value)) return null;
                else return Uri.UnescapeDataString(value);
            }
            catch (Exception e)
            {
                return Common.ThrowIfNeeded<string>(e, nullWhenError);
            }
        }
        [SqlFunction]
        public static string UrlEncode(string value, bool nullWhenError)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(value)) return null;
                else return Uri.EscapeDataString(value);
            }
            catch (Exception e)
            {
                return Common.ThrowIfNeeded<string>(e, nullWhenError);
            }
        }
        [SqlFunction]
        public static string SplitString(string value, string separator, int index, bool nullWhenError)
        {
            try
            {
                if (string.IsNullOrEmpty(value)) return null;
                else return value.Split(new string[] { separator }, StringSplitOptions.None)[index];
            }
            catch (Exception e)
            {
                return Common.ThrowIfNeeded<string>(e, nullWhenError);
            }
        }
        [SqlFunction]
        public static string UnicodeDecode(string value, bool nullWhenError)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(value)) return null;
                else return System.Text.RegularExpressions.Regex.Unescape(value);
            }
            catch (Exception e)
            {
                return Common.ThrowIfNeeded<string>(e, nullWhenError);
            }
        }
        [SqlFunction]
        public static string RegexMatch(string value, string regexPattern, int groupNumber, bool nullWhenError)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(value)) return null;
                else
                {
                    return new Regex(regexPattern).Match(value).Groups[groupNumber].Value;
                }
            }
            catch (Exception e)
            {
                return Common.ThrowIfNeeded<string>(e, nullWhenError);
            }
        }
        
        public static string GetSha256Hash(string value, bool nullWhenError)
        {
            try
            {
                SHA256Managed sha256 = new SHA256Managed();
                Encoding encoding = Encoding.UTF8;
                byte[] inputByteArray = encoding.GetBytes(value);
                byte[] hashByteArray = sha256.ComputeHash(inputByteArray);
                string hashString = string.Empty;
                foreach (byte x in hashByteArray)
                {
                    hashString += String.Format("{0:x2}", x);
                }
                return hashString;
            }
            catch (Exception e)
            {
                return Common.ThrowIfNeeded<string>(e, nullWhenError);
            }
        }
        [SqlFunction]
        public static string Base64Decode(string value, bool nullWhenError)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(value)) return null;
                else return Encoding.UTF8.GetString(Convert.FromBase64String(value));
            }
            catch (Exception e)
            {
                return Common.ThrowIfNeeded<string>(e, nullWhenError);
            }
        }
        [SqlFunction]
        public static string Base64Encode(string value, bool nullWhenError)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(value)) return null;
                else return System.Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
            }
            catch (Exception e)
            {
                return Common.ThrowIfNeeded<string>(e, nullWhenError);
            }
        }


    }
}
