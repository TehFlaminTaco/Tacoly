using System.Collections.Generic;
using System.Text;
using System.Linq;
using Tacoly.Tokenizer.Properties;
using Tacoly.Util;

namespace Tacoly.Tokenizer.Tokens;

public class Parenthetical : Token, ICodeProvider, IConstantProvider, IRootCodeProvider
{
    public required Token Body;
    public Token? CastTypes = null;

    public Parenthetical(string raw, string file) : base(raw, file) { }

    [RegisterClaimer()]
    public static Parenthetical? Claim(StringClaimer claimer)
    {
        Claim flag = claimer.Flag();
        if (!claimer.Claim(@"\(").Success)
        {
            flag.Fail();
            return null;
        }

        var body = Token.Claim<ICodeProvider, IConstantProvider>(claimer);
        if (body is null)
        {
            flag.Fail();
            return null;
        }
        Token? types = null;

        if (claimer.Claim(":").Success)
        {
            types = Token.Claim<ITypesProvider, ITypeProvider>(claimer);
            if (types is null)
            {
                flag.Fail();
                return null;
            }
        }

        if (!claimer.Claim(@"\)").Success)
        {
            return null;
        }
        return new Parenthetical(claimer.Raw(flag), claimer.File)
        {
            Body = body,
            CastTypes = types,
        };
    }

    public Either<double, long>? GetConstant(Scope scope)
    {
        if (Body.IsConstant(scope, out var cst))
            return cst;
        return null;
    }

    public string ProvidedCode(Scope scope)
    {
        if (Body.IsConstant(scope, out var cst))
            return IConstantProvider.ProvidedCode(cst);

        StringBuilder sb = new();
        sb.MaybeAppendLine(Body.ConstantCode(scope));
        IEnumerable<VarType> forceTypes;
        var bodyTypes = Body.ConstantStack(scope);
        if (CastTypes is Token ct)
        {
            if (ct is ITypesProvider itpsp)
                forceTypes = itpsp.ProvidedTypes(scope);
            else
                forceTypes = new VarType[] { (ct as ITypeProvider)!.ProvidedType(scope) };
        }
        else
            forceTypes = new VarType[] { bodyTypes.First() };

        sb.MaybeAppendLine(VarType.CoaxStack(bodyTypes, forceTypes));
        return sb.ToString();
    }

    public string ProvidedRootCode(Scope scope)
    {
        if (Body.IsConstant(scope, out var cst))
            return "";
        StringBuilder sb = new();
        sb.MaybeAppendLine(Body.ConstantRoot(scope));
        IEnumerable<VarType> forceTypes;
        var bodyTypes = Body.ConstantStack(scope);
        if (CastTypes is Token ct)
        {
            if (ct is ITypesProvider itpsp)
                forceTypes = itpsp.ProvidedTypes(scope);
            else
                forceTypes = new VarType[] { (ct as ITypeProvider)!.ProvidedType(scope) };
        }
        else
            forceTypes = new VarType[] { bodyTypes.First() };
        sb.MaybeAppendLine(VarType.WithGenerateCoaxStack(bodyTypes, forceTypes));
        return sb.ToString();
    }

    public IEnumerable<VarType> ResultStack(Scope scope)
    {
        if (Body.IsConstant(scope, out var cst))
            return IConstantProvider.ResultStack(cst);
        var bodyTypes = Body.ConstantStack(scope);
        if (CastTypes is Token ct)
        {
            if (ct is ITypesProvider itpsp)
                return itpsp.ProvidedTypes(scope);
            return new VarType[] { (ct as ITypeProvider)!.ProvidedType(scope) };
        }
        return new VarType[] { bodyTypes.First() };
    }
}