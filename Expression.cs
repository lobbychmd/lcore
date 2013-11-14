using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CSharp;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Text.RegularExpressions;

namespace l.core
{
    public class Expression
    { 

        private string expression;

        public Expression(string expression) {
            this.expression = expression;
        }

        public object Eval(Dictionary<string, object> paramsValue) {
            Regex reg = new Regex(@"\{([\w_]+)\}");
            var ms = reg.Matches(expression);
            string exp1 = expression;


            foreach (Match i in ms) {
                var varName = i.Groups[1].Value;
                exp1 = exp1.Replace("{" + varName + "}", paramsValue[varName].ToString());
            }


            // 1.CSharpCodePrivoder
            CSharpCodeProvider objCSharpCodePrivoder = new CSharpCodeProvider();
 
            // 2.ICodeComplier
            ICodeCompiler objICodeCompiler = objCSharpCodePrivoder.CreateCompiler();
 
            // 3.CompilerParameters
            CompilerParameters objCompilerParameters = new CompilerParameters();
            objCompilerParameters.ReferencedAssemblies.Add("System.dll");
            objCompilerParameters.GenerateExecutable = false;
            objCompilerParameters.GenerateInMemory = true;
 
            // 4.CompilerResults
            CompilerResults cr = objICodeCompiler.CompileAssemblyFromSource(objCompilerParameters, GenerateCode( exp1));
 
            if (cr.Errors.HasErrors)  {
                throw new Exception(string.Join("\n", ( from CompilerError e in cr.Errors select e).Select(p=>p.ErrorText)));
                return null;
            }
            else  {
                // 通过反射，调用HelloWorld的实例
                Assembly objAssembly = cr.CompiledAssembly;
                object objHelloWorld = objAssembly.CreateInstance("l.core.lookupExpression");
                MethodInfo objMI = objHelloWorld.GetType().GetMethod("OutPut");
 
                return objMI.Invoke(objHelloWorld, null);
            }
 
        }

        private string GenerateCode(string _expression)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("using System;");
            sb.Append(Environment.NewLine);
            sb.Append("namespace l.core");
            sb.Append(Environment.NewLine);
            sb.Append("{");
            sb.Append(Environment.NewLine);
            sb.Append("    public class lookupExpression");
            sb.Append(Environment.NewLine);
            sb.Append("    {");
            sb.Append(Environment.NewLine);
            sb.Append("        public object OutPut()");
            sb.Append(Environment.NewLine);
            sb.Append("        {");
            sb.Append(Environment.NewLine);
            sb.Append("             return " + _expression + ";");
            sb.Append(Environment.NewLine);
            sb.Append("        }");
            sb.Append(Environment.NewLine);
            sb.Append("    }");
            sb.Append(Environment.NewLine);
            sb.Append("}");

            string code = sb.ToString();
 

            return code;
        }
    }
}
