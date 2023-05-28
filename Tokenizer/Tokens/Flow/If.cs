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

    [RegisterClaimer()]
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

    [RegisterLeftClaimer<ICodeProvider, IConstantProvider>(2)]
    public static If? LeftClaim(StringClaimer claimer, Token left)
    {
        Claim keyword = claimer.Claim(@"\?");
        if (!keyword.Success) return null;
        Token? body = Claim<ICodeProvider, IConstantProvider>(claimer, 2);
        if (body is null)
        {
            keyword.Fail();
            return null;
        }
        Token? otherwise = null;
        if (claimer.Claim(":").Success)
        {
            otherwise = Claim<ICodeProvider, IConstantProvider>(claimer, 2);
            if (otherwise is null)
            {
                keyword.Fail();
                return null;
            }
        }
        return new If(claimer.Raw(keyword), claimer.File)
        {
            Condition = left,
            Body = body,
            Otherwise = otherwise
        };
    }

    public If(string raw, string file) : base(raw, file) { }

    public Either<double, long>? GetConstant(Scope scope)
    {
        if (Condition is not IConstantProvider cond
         || Body is not IConstantProvider body
         || Otherwise is not IConstantProvider otherwise)
            return null;

        if (cond.GetConstant(scope) is not Either<double, long> condVal)
            return null;

        return condVal.Match(c => c, c => c) != 0 ? body.GetConstant(scope) : otherwise.GetConstant(scope);
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