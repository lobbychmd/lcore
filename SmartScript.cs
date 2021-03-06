﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RazorEngine;

namespace l.core
{
    public class SysParamsItem {
        public string ProjectCode { get; set; }
        public Dictionary<string, string[]> Params { get; set; }
        public DateTime Expired { get; set; }
    }
    public class SmartScript
    {
        private static List<SysParamsItem> SysParams = new List<SysParamsItem>();

        private static Dictionary<string, string[]> getSysParams()
        {
            if (l.core.Project.Current == null)
                return null;

            var projectCode = l.core.Project.Current.ProjectCode;
            var f = SysParams.Find(p => p.ProjectCode == projectCode);
            if (f != null && f.Expired < DateTime.Now) {
                SysParams.Remove(f);
                f = null;
            }
            if (f == null) {
                try
                {
                    var __SysParams = new l.core.Query("__SysParams").Load();
                    var ds = __SysParams.ExecuteQuery();
                    f =new SysParamsItem{ ProjectCode =  projectCode, Expired = DateTime.Now.AddMinutes(60), Params = (from System.Data.DataRow dr in ds.Tables[0].Rows select dr)
                        .ToDictionary(
                            p => p["name"].ToString(),
                            q => new[] { q["value1"].ToString(), q["value2"].ToString(), q["value3"].ToString() })
                    };
                    SysParams.Add(f);
                }
                catch {
                    SysParams.Add(new SysParamsItem { ProjectCode = projectCode, Expired = DateTime.Now.AddMinutes(60) });
                }

            }
            return (f ?? SysParams.Find(p => p.ProjectCode == projectCode)).Params;
        }

        static public string Eval(string script, Dictionary<string, string> @params) {
            //try {
                string template = script.Replace("@", "^^^^").Replace("$", "@");
                string result = Razor.Parse(template, new { Params = @params, Sys = getSysParams() });
                result = result.Replace("^^^^", "@");
                return result;
            //} catch { return script; }
        }
    }
}
