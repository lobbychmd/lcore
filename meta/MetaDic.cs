using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace l.core
{
    //通用模型加上ORM 设定
    public class Dic : MetaDic {
        public string HashCode { get; set; }
        public Dic(string dicNO) {
            this.DicNO = dicNO;
            Dtl = new List<MetaDicDtl>();
        }

        private void checkHashCode() {
            if (!string.IsNullOrEmpty(HashCode))
                System.Diagnostics.Debug.Assert(
                    l.core.ScriptHelper.GetHashCode(this) == HashCode,
                    "校验码错误.");
                    
        }

        private OrmHelper getOrm() {
            return OrmHelper.From("tDic", false).F("DicNO", "DicDesc").PK("DicNO").Obj(this).End
                .SubFrom("tDicDtl").Obj(Dtl).End;
        }

        public Dic Load() {
            getOrm().Setup();
            //checkHashCode();
            return this;
        }
        public void Remove() { getOrm().Dels(); }
        public void Save()
        {
            //int i = 0;
            //foreach (QueryParam p in Params) {
            //    p.ParamIdx = i;
            //    i++;
            //}
            getOrm().Save();
        }
    }

    //下面是通用模型
    public class MetaDic
    {
        public string DicNO { get; set; }
        public string DicDesc { get; set; }

        public List<MetaDicDtl> Dtl { get; set; }
    }

    public class MetaDicDtl {
        public string DicNO { get; set; }
        public string DicDtlNO { get; set; }
        public string DicDtlValue { get; set; }
    }
}
