using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using System.Configuration;

namespace PerfectAndMergeData
{
    public class TraceLog
    {
        public static void Write(string message, Exception ex = null)
        {
                string sYear = DateTime.Now.Year.ToString();
                string sMonth = DateTime.Now.Month.ToString();
                string sDay = DateTime.Now.Day.ToString();
                string sErrorTime = sYear + sMonth + sDay;
                string path = "";
                try
                {
                if (!string.IsNullOrWhiteSpace(ConfigurationManager.AppSettings.Get("EnableTracing")) && (ConfigurationManager.AppSettings["EnableTracing"]).ToString().ToLower().Equals("true"))
                {
                    if (System.Web.HttpContext.Current != null && System.Web.HttpContext.Current.Server != null)
                    {
                        path = System.Web.HttpContext.Current.Server.MapPath(".");
                    }
                    else
                    {
                        path = AppDomain.CurrentDomain.BaseDirectory;
                    }
                    //string path = System.Web.HttpContext.Current.Server.MapPath(".");
                    path += string.Format(@"\Log{0}.txt", sErrorTime);
                    FileStream fstr = File.Open(path, FileMode.Append);
                    StreamWriter sw = new StreamWriter(fstr);
                    sw.WriteLine(DateTime.Now.ToString("dd-MMM-yyyy HH:mm:ss:ffff") + " : " + message);
                    if (ex != null)
                    {
                        sw.WriteLine("Exception : " + ex.Message);
                    }
                    sw.Dispose();
                }
            }
            catch (Exception exc)
            {
            }
        }
    }
}