using System.Collections.Generic;
using System.Text;
using System.Linq;
using Tacoly.Tokenizer.Properties;
using Tacoly.Util;

namespace Tacoly.Tokenizer.Tokens;

public class Program : Token
{
    public Program(string raw, string file) : base(raw, file) { }

    public List<Token> Statements { get; private set; } = new();
    public static Program Claim(StringClaimer claimer)
    {
        Program prog = new(claimer.Code, claimer.File);
        Token? last;
        while ((last = Claim<IRootCodeProvider, ICodeProvider>(claimer, 0)) is not null)
        {
            prog.Statements.Add(last);
            claimer.Claim(@";");
        }
        return prog;
    }

    public string GetCode()
    {
        StringBuilder sb = new();
        StringBuilder mainMethod = new();
        sb.AppendLine("(module");
        sb.AppendLine("(memory 1)".Tabbed());
        sb.AppendLine("(func $i32dup (param i32) (result i32 i32) local.get 0 local.get 0)".Tabbed());
        sb.AppendLine("(func $i64dup (param i64) (result i64 i64) local.get 0 local.get 0)".Tabbed());
        sb.AppendLine("(func $f64dup (param f64) (result f64 f64) local.get 0 local.get 0)".Tabbed());
        Scope scope = new();
        foreach (var statement in Statements)
        {
            if (statement is IRootCodeProvider root)
            {
                sb.MaybeAppendLine(root.ProvidedRootCode(scope).Tabbed());
            }
            if (statement is ICodeProvider code)
            {
                mainMethod.MaybeAppendLine(code.ProvidedCode(scope));
                int resultStackSize = code.ResultStack(scope).Count();
                for (int i = 0; i < resultStackSize; i++)
                    mainMethod.AppendLine("drop");
            }
        }

        sb.AppendLine("\t(func (export \"main\")");
        sb.MaybeAppendLine(mainMethod.Tabbed(2));
        sb.Append("\t)\n)");

        return sb.ToString();
    }
}