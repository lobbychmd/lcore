using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace l.core
{
    public class MetaTypeInfo {
        public Type MetaType { get; set; }
        public string[] Keys { get; set; }
    }
    public class MetaHelper
    {
        static public Dictionary<string, MetaTypeInfo> MetaTypeInfos = new Dictionary<string, MetaTypeInfo> {
            {"MetaQuery", new MetaTypeInfo{MetaType = typeof(l.core.Query), Keys= new []{"QueryName"}}},
            {"MetaModule", new MetaTypeInfo{MetaType = typeof(l.core.Module), Keys= new []{"ModuleID"}}},
            {"MetaBiz", new MetaTypeInfo{MetaType = typeof(l.core.Biz), Keys= new []{"BizID"}}},
            {"MetaField", new MetaTypeInfo{MetaType = typeof(l.core.MetaField), Keys= new []{"FieldName, Context"}}},
        };
    }
}
