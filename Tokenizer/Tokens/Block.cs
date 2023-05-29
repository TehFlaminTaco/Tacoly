using System.Collections.Generic;
using System.Text;
using System.Linq;
using Tacoly.Tokenizer.Properties;
using Tacoly.Util;

namespace Tacoly.Tokenizer.Tokens;

public class Block : Token, ICodeProvider, IConstantProvider, IRootCodeProvider
{
    public List<Token> Body = new();
	private Scope? subScope = null;

    public Block(string raw, string file) : base(raw, file) { }

    [RegisterClaimer()]
    public static Block? Claim(StringClaimer claimer)
    {
        Claim flag = claimer.Flag();
        if (!claimer.Claim(@"\{").Success)
        {
            flag.Fail();
            return null;
        }
        List<Token> body = new();
        while (Token.Claim<ICodeProvider, IConstantProvider>(claimer) is Token last)
        {
            body.Add(last);
            claimer.Claim(";");
        }
        if (!claimer.Claim(@"\}").Success)
        {
            flag.Fail();
            return null;
        }
        return new Block(claimer.Raw(flag), claimer.File)
        {
            Body = body
        };
    }

    public Either<double, long>? GetConstant(Scope scope)
    {
		subScope ??= scope.Sub();
        if (Body.All(c => c.IsConstant(subScope)))
            return (Body.Last() as IConstantProvider)!.GetConstant(subScope);
        return null;
    }

    public string ProvidedCode(Scope scope)
    {
		subScope ??= scope.Sub();
        if (this.IsConstant(subScope, out var cst))
            return IConstantProvider.ProvidedCode(cst);
        StringBuilder sb = new();
        int lastDrops = 0;
        foreach (var t in Body)
        {
            for (int i = 0; i < lastDrops; i++) sb.AppendLine("drop");

            sb.MaybeAppendLine(t.ConstantCode(subScope));
        }
        return sb.ToString();
    }

    public string ProvidedRootCode(Scope scope)
    {
		subScope ??= scope.Sub();
        if (this.IsConstant(subScope))
            return "";
        StringBuilder sb = new();
        foreach (var t in Body)
            sb.MaybeAppendLine(t.ConstantRoot(subScope));
        return sb.ToString();
    }

    public IEnumerable<VarType> ResultStack(Scope scope)
    {
		subScope ??= scope.Sub();
        if (this.IsConstant(subScope, out var cst))
            return IConstantProvider.ResultStack(cst);
        return Body.Last().ConstantStack(subScope);
    }
}