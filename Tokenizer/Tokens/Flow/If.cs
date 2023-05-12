using System.Collections.Generic;
using Tacoly.Tokenizer.Properties;
using Tacoly.Tokenizer;
using Tacoly.Util;

namespace Tacoly.Tokenizer.Tokens;

public class If : Token, ICodeProvider, IRootCodeProvider, IConstantProvider
{
    public required Token Condition;
    public required Token Body;
    public Token? Otherwise = null;

    public static If? Claim(StringClaimer claimer)
    {
        Claim keyword = claimer.Claim(@"if\b");
        if (!keyword.Success) return null;
        Token? condition = Claim<ICodeProvider, IConstantProvider>(claimer);
        if (condition is null)
        {
            keyword.Fail();
            return null;
        }
        Token? body = Claim<ICodeProvider, IConstantProvider>(claimer);
        if (body is null)
        {
            keyword.Fail();
            return null;
        }
        Token? otherwise = null;
        if (claimer.Claim("else\b").Success)
        {
            otherwise = Claim<ICodeProvider, IConstantProvider>(claimer);
            if (otherwise is null)
            {
                keyword.Fail();
                return null;
            }
        }
        return new If(claimer.Raw(keyword), claimer.File)
        {
            Condition = condition,
            Body = body,
            Otherwise = otherwise
        };
    }

    public If(string raw, string file) : base(raw, file) { }

    public Either<double, long>? GetConstant(Scope scope)
    {
        throw new System.NotImplementedException();
    }

    public string ProvidedCode(Scope scope)
    {
        throw new System.NotImplementedException();
    }

    public string ProvidedRootCode(Scope scope)
    {
        throw new System.NotImplementedException();
    }

    public IEnumerable<VarType> ResultStack(Scope scope)
    {
        throw new System.NotImplementedException();
    }
}