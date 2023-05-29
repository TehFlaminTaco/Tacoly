using System.Collections.Generic;
using System.Text;
using System.Linq;
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
        if (claimer.Claim(@"else\b").Success)
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
        var cnst = this.GetConstant(scope);
        if (cnst is not null) return IConstantProvider.ProvidedCode(cnst);
        if (Condition is IConstantProvider icp && icp.GetConstant(scope) is Either<double, long> con)
        {
            if (con.Match(c => c, c => c) != 0)
            {
                return Body.ConstantCode(scope);
            }
            if (Otherwise is null)
                return "";
            return Otherwise.ConstantCode(scope);
        }

        StringBuilder sb = new();
        sb.MaybeAppendLine((Condition as ICodeProvider)!.ProvidedCode(scope));
        var conditionTypes = Condition.ConstantStack(scope);
        for (int i = 1; i < conditionTypes.Count(); i++)
            sb.AppendLine("drop");
        var truthyMeta = conditionTypes.First().GetDefinition().GetMeta("truthy", new VarType[] { conditionTypes.First() });
        if (truthyMeta is not null)
            sb.MaybeAppendLine(truthyMeta.Value.DeOffset());
        var leftTypes = Body.ConstantStack(scope);
        var rightTypes = Otherwise?.ConstantStack(scope) ?? Enumerable.Empty<VarType>();

        var outTypes = VarType.MostCommonStack(leftTypes, rightTypes);
        sb.MaybeAppendLine($"(if (result {VarType.InternalTypes(outTypes)})");
        sb.MaybeAppendLine("(then".Tabbed());
        sb.MaybeAppendLine(Body.ConstantCode(scope).Tabbed().Tabbed());
        sb.MaybeAppendLine(VarType.CoaxStack(leftTypes, outTypes).Tabbed().Tabbed());
        if (Otherwise is Token right)
        {
            sb.MaybeAppendLine(") (else".Tabbed());
            sb.MaybeAppendLine(right.ConstantCode(scope).Tabbed().Tabbed());
            sb.MaybeAppendLine(VarType.CoaxStack(rightTypes, outTypes).Tabbed().Tabbed());
        }
        sb.MaybeAppendLine(")".Tabbed());

        sb.MaybeAppendLine(")");
        return sb.ToString();
    }

    public string ProvidedRootCode(Scope scope)
    {
        var cnst = this.GetConstant(scope);
        if (cnst is not null) return "";
        if (Condition.IsConstant(scope, out var c))
        {
            if (c.Match(v => v, v => v) != 0)
                return Body.ConstantRoot(scope);
            if (Otherwise is not null)
                return Otherwise.ConstantRoot(scope);
            return "";
        }
        StringBuilder sb = new();
        var leftTypes = Body.ConstantStack(scope);
        var rightTypes = Otherwise?.ConstantStack(scope) ?? Enumerable.Empty<VarType>();

        var outTypes = VarType.MostCommonStack(leftTypes, rightTypes);
        sb.MaybeAppendLine(VarType.WithGenerateCoaxStack(leftTypes, outTypes));
        sb.MaybeAppendLine(VarType.WithGenerateCoaxStack(rightTypes, outTypes));
        sb.MaybeAppendLine(Condition.ConstantRoot(scope));
        sb.MaybeAppendLine(Body.ConstantRoot(scope));
        if (Otherwise is not null)
            sb.MaybeAppendLine(Otherwise.ConstantRoot(scope));
        return sb.ToString();
    }

    public IEnumerable<VarType> ResultStack(Scope scope)
    {
        var cnst = this.GetConstant(scope);
        if (cnst is not null) return IConstantProvider.ResultStack(cnst);
        if (Condition.IsConstant(scope, out var c))
        {
            if (c.Match(v => v, v => v) != 0)
                return Body.ConstantStack(scope);

            if (Otherwise is not null)
                return Otherwise.ConstantStack(scope);

            return Enumerable.Empty<VarType>();
        };
        var leftTypes = Body.ConstantStack(scope);
        var rightTypes = Otherwise?.ConstantStack(scope) ?? Enumerable.Empty<VarType>();

        return VarType.MostCommonStack(leftTypes, rightTypes);
    }
}