﻿using Microsoft.SqlServer.Server;
using Mono.Web;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace bi_dev.sql.mssql.extensions.web
{
    public static class Utils
    {
        [SqlFunction]
        public static string Get(string url, string headersInUrlFormat, bool nullWhenError)
        {
            try
            {
                WebClient wc = new WebClient();
                wc.Encoding = Encoding.UTF8;
                System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                if (!string.IsNullOrWhiteSpace(headersInUrlFormat)) wc.Headers.Add(HttpUtility.ParseQueryString(headersInUrlFormat));
                return wc.DownloadString(url);
            }
            catch (Exception e)
            {
                return Common.ThrowIfNeeded<string>(e, nullWhenError);
            }
        }
        [SqlFunction]
        public static string Post(string url, string body,  string headersInUrlFormat, bool nullWhenError)
        {
            try
            {
                WebClient wc = new WebClient();
                wc.Encoding = Encoding.UTF8;
                if (!string.IsNullOrWhiteSpace(headersInUrlFormat)) wc.Headers.Add(HttpUtility.ParseQueryString(headersInUrlFormat));
                return wc.UploadString(url, body);
            }
            catch (Exception e)
            {
                return Common.ThrowIfNeeded<string>(e, nullWhenError);
            }
        }
        public class WebRequestResult
        {

            public string Url { get; set; }
            public string Body { get; set; }
            public Dictionary<string, string> RequestHeaders { get; set; }
            public Dictionary<string, string> RequestCookies { get; set; }

            public string ResponseText { get; set; }
            public HttpStatusCode HttpStatusCode { get; set; } 
            public int StatusCode { get { return (int)this.HttpStatusCode; } }
            public WebHeaderCollection ResponseHeaders { get; set; }
            public CookieCollection ResponseCookies { get; set; }
            public int CodePage { get; set; }
            public Exception Exception { get; set; }

        }
        private static WebRequestResult processWebRequest(string url, string method, string body, string contentType, int? codePage, Dictionary<string, string> headers, Dictionary<string, string> cookies, bool allowAutoRedirect)
        {
            WebRequestResult result = new WebRequestResult();
            result.RequestCookies = cookies;
            result.RequestHeaders = headers;
            result.Url = url;
            result.Body = body;
            try
            {
                HttpWebRequest r = (HttpWebRequest)WebRequest.Create(url);
                r.Method = method;
                r.CookieContainer = new CookieContainer();
                if (cookies != null)
                {
                    Uri target = new Uri(url);
                    foreach (var cookie in cookies)
                    {
                        r.CookieContainer.Add(new Cookie(cookie.Key, cookie.Value, "/", target.Host));
                    }
                }
                if (headers != null)
                {
                    var defaultHeaders = Enum.GetValues(typeof(HttpRequestHeader)).Cast<HttpRequestHeader>();
                    foreach (var header in headers)
                    {
                        var defaultHeader = defaultHeaders.Where(x => x.ToString().ToLower() == header.Key.ToLower());

                        if (defaultHeader.Count() > 0)
                        {
                            r.Headers[defaultHeader.FirstOrDefault()] = header.Value;
                        }
                        else
                        {
                            r.Headers[header.Key] = header.Value;
                        }
                    }
                }
                int currentCodePage = (codePage.HasValue) ? codePage.Value : 65001; // Default Unicode;
                result.CodePage = currentCodePage;
                if (body != null)
                {
                    var encoding = Encoding.GetEncoding(currentCodePage);
                    byte[] bytes = encoding.GetBytes(body);
                    //r.ContentLength = bytes.Length;
                    using (var stream = r.GetRequestStream())
                    {
                        stream.Write(bytes, 0, bytes.Length);
                    }
                }
                if (!string.IsNullOrWhiteSpace(contentType))
                {
                    r.ContentType = contentType;
                }
                string responseText;
                r.AllowAutoRedirect = allowAutoRedirect;

                using (HttpWebResponse response = (HttpWebResponse)r.GetResponse())
                {
                    result.ResponseCookies = response.Cookies;
                    result.ResponseHeaders = response.Headers;
                    using (var s = response.GetResponseStream())
                    {
                        using (var reader = new StreamReader(s, Encoding.GetEncoding(currentCodePage)))
                        {
                            responseText = reader.ReadToEnd();
                            result.ResponseText = responseText;
                        }
                    }
                    return result;
                }
            }
            catch (WebException e)
            {
                var respone = (HttpWebResponse)e.Response;
                result.Exception = e;
                using (var responseStream = respone.GetResponseStream())
                {
                    result.ResponseCookies = respone.Cookies;
                    using (var reader = new StreamReader(responseStream))
                    {
                        result.ResponseText = reader.ReadToEnd();
                        result.HttpStatusCode = respone.StatusCode;
                    }
                }
                return result;
            }
        }
        // -----------------
        public class TableType
        {
            public string RowType { get; set; }
            public string RowKey { get; set; }
            public string RowValue { get; set; }
            public TableType()
            {

            }
            public TableType(string rowType, string rowValue)
            {
                this.RowType = rowType;
                this.RowValue = rowValue;
            }
            public TableType(string rowType, string rowKey, string rowValue)
            {
                this.RowType = rowType;
                this.RowKey = rowKey;
                this.RowValue = rowValue;
            }
        }
        [SqlFunction(FillRowMethodName = "FillRow")]
        public static IEnumerable ProcessWebRequest(string url, string method, string body, string contentType, int? codePage, string headersInUrlFormat, string cookiesInUrlFormat, bool allowAutoRedirect, bool nullWhenError)
        {
            try
            {
                Dictionary<string, string> headerDict = null;
                Dictionary<string, string> cookieDict = null;
                if (!string.IsNullOrWhiteSpace(headersInUrlFormat))
                {
                    var headers = HttpUtility.ParseQueryString(headersInUrlFormat);
                    headerDict = headers.AllKeys.ToDictionary(x => x, y => Uri.UnescapeDataString(headers[y]));
                }
                if (!string.IsNullOrWhiteSpace(cookiesInUrlFormat))
                {
                    var cookies = HttpUtility.ParseQueryString(cookiesInUrlFormat);
                    cookieDict = cookies.AllKeys.ToDictionary(x => x, y => Uri.UnescapeDataString(cookies[y]));
                }

                var res = processWebRequest(
                    url,
                    method,
                    body,
                    contentType,
                    codePage,
                    headerDict,
                    cookieDict,
                    allowAutoRedirect
                );
                if (res.Exception != null)
                {
                    throw res.Exception;
                }
                List<TableType> l = new List<TableType>();
                l.Add(new TableType("url", url));
                l.Add(new TableType("method", method));
                l.Add(new TableType("body", res.Body));
                l.Add(new TableType("content_type", contentType));
                l.Add(new TableType("code_page", res.CodePage.ToString()));
                if (res.RequestCookies != null)
                {
                    foreach (var cookie in res.RequestCookies)
                    {
                        l.Add(new TableType("request_cookie", cookie.Key, cookie.Value));
                    }
                }
                if (res.ResponseCookies != null)
                {
                    foreach (Cookie cookie in res.ResponseCookies)
                    {
                        l.Add(new TableType("response_cookie", cookie.Name, cookie.Value));
                    }
                }
                if (res.RequestHeaders != null)
                {
                    foreach (var header in res.RequestHeaders)
                    {
                        l.Add(new TableType("request_header", header.Key, header.Value));
                    }
                }
                if (res.ResponseHeaders != null)
                {
                    foreach (var header in res.ResponseHeaders.AllKeys)
                    {
                        l.Add(new TableType("response_header", header, res.ResponseHeaders[header]));
                    }
                }
                return l;
            }
            catch (Exception e)
            {
                return Common.ThrowIfNeeded<IEnumerable>(e, nullWhenError);
            }
        }
        public static void FillRow(Object obj, out SqlChars rowType, out SqlChars key, out SqlChars value)
        {
            TableType table = (TableType)obj;
            rowType = new SqlChars(table.RowType);
            key = new SqlChars(table.RowKey);
            value = new SqlChars(table.RowValue);
        }
        // -----------------
    }
}
