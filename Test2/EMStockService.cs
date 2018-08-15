﻿using LJC.Com.StockService.Contract;
using LJC.FrameWork.Comm;
using LJC.FrameWork.Data.EntityDataBase;
using LJC.FrameWork.EntityBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test2
{
    public class EMStockService
    {
        const string TBName = "EMStockDayQuote";
        const string QuoteIndexName = "Code_Time";
        static EMStockService()
        {
            BigEntityTableEngine.LocalEngine.CreateTable(TBName, "Key", typeof(EMStockDayQuote), new IndexInfo[]{
                new IndexInfo{
                     IndexName=QuoteIndexName,
                     Indexs=new IndexItem[]{
                         new IndexItem{
                             Field="InnerCode",
                             FieldType=EntityType.STRING
                         },
                         new IndexItem{
                             Field="Time",
                             FieldType=EntityType.DATETIME
                         }
                     }
                }
            });
        }

        public class EMDayQuoteResponse
        {
            public string name { get; set; }
            public string code { get; set; }
            public EMQuoteInfo info { get; set; }
            public string[] data { get; set; }
        }

        public class EMQuoteInfo
        {
            public string c { get; set; }
            public string h { get; set; }
            public string l { get; set; }
            public string o { get; set; }
            public string a { get; set; }
            public string v { get; set; }
            public string yc { get; set; }
            public string time { get; set; }
            public string ticks { get; set; }
            public string total { get; set; }
            public string pricedigit { get; set; }
            public string jys { get; set; }
            public string Settlement { get; set; }
            public int mk { get; set; }
            public string sp { get; set; }
            public bool isrzrq { get; set; }
        }

        public class EMStockDayQuote : StockQuote
        {
            public string Key
            {
                get;
                set;
            }
        }

        private static double ConvertPrice(string price)
        {
            if (string.IsNullOrWhiteSpace(price) || price == "-")
            {
                return 0.00;
            }
            price = price.Trim();
            if (price.EndsWith("%"))
            {
                return double.Parse(price.TrimEnd('%'));
            }
            return double.Parse(price);
        }

        public static IEnumerable<StockQuote> GetStockDayQuote(string code,DateTime bein,DateTime end)
        {
            var quotetblist = BigEntityTableEngine.LocalEngine.Scan<EMStockDayQuote>(TBName, QuoteIndexName, new object[] { code, bein }, new object[] { code, end }).ToList().OrderBy(p => p.Time);
            DateTime last = DateTime.MinValue;
            if (quotetblist.Count() > 0)
            {
                last = quotetblist.Last().Time;
            }
            if (last >= end)
            {
                foreach (var item in quotetblist)
                {
                    yield return item;
                }
            }
            else
            {
                if (code.StartsWith("6"))
                {
                    code += 1;
                }
                else
                {
                    code += 2;
                }
                var token = Guid.NewGuid().ToString("N");
                string url = string.Format("http://pdfm.eastmoney.com/EM_UBG_PDTI_Fast/api/js?token={0}&rtntype=6&id={1}&type=k&authorityType=fa&cb=jsonp{2}", token, code, DateTimeHelper.GetTimeStamp());
                byte[] data = null;
                var respjson = new HttpRequestEx().DoRequest(url, data);
                respjson.ResponseContent = Encoding.UTF8.GetString(respjson.ResponseBytes);
                var resp = JsonUtil<EMDayQuoteResponse>.Deserialize(respjson.ResponseContent.Substring(respjson.ResponseContent.IndexOf('(') + 1).TrimEnd(')'));
                foreach (var s in resp.data)
                {
                    var arr = s.Split(',');
                    var quote = new EMStockDayQuote
                    {
                        Time = DateTime.Parse(arr[0]),
                        Open = ConvertPrice(arr[1]),
                        Close = ConvertPrice(arr[2]),
                        High = ConvertPrice(arr[3]),
                        Low = ConvertPrice(arr[4]),
                        Volumne = ConvertPrice(arr[5]),
                        Amount = ConvertPrice(arr[6]),
                        ChangeRate = ConvertPrice(arr[7]),
                        InnerCode=resp.code,
                        
                    };
                    quote.Key = resp.code + "_" + quote.Time.ToString("yyyyMMdd");

                    if (quote.Time > last)
                    {
                        BigEntityTableEngine.LocalEngine.Insert(TBName, quote);
                    }

                    if (quote.Time >= bein && quote.Time <= end)
                    {
                        yield return quote;
                    }
                }
            }
        }
    }
}
