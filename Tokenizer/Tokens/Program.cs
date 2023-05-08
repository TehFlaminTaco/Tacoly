using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Tacoly.Tokenizer.Tokens;

public class Program : Token
{
    public Program(string raw, string file) : base(raw, file) { }

    public List<Token> Statements { get; private set; } = new();
    public static Program Claim(StringClaimer claimer)
    {
        Program prog = new(claimer.Code, claimer.File);
        Token? last;
        while ((last = Token.Claim<IRootCodeProvider, ICodeProvider>(claimer, 0)) is not null)
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

        Scope scope = new();
        foreach (var statement in Statements)
        {
            if (statement is IRootCodeProvider root)
            {
                sb.Append(root.ProvidedRootCode(scope));
            }
            if (statement is ICodeProvider code)
            {
                mainMethod.Append(code.ProvidedCode(scope));
                mainMethod.Append(' ');
                int resultStackSize = code.ResultStack(scope).Count();
                for (int i = 0; i < resultStackSize; i++)
                {
                    mainMethod.Append("discard ");
                }
            }
        }

        sb.Append("(module ");

        sb.Append(@"(func (export ""main"") ");
        sb.Append(mainMethod);
        sb.Append("))");

        return sb.ToString();
    }
}