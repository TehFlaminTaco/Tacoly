using System.Collections.Generic;
using Tacoly.Tokenizer.Properties;
using Tacoly.Tokenizer;

namespace Tacoly.Util;

public static class ConstantUtil
{
    public static string ConstantCode(this Token t, Scope scope)
    {
        if (t is IConstantProvider cons && cons.GetConstant(scope) is Either<double, long> res)
            return IConstantProvider.ProvidedCode(res);
        return (t as ICodeProvider)!.ProvidedCode(scope);
    }

    public static IEnumerable<VarType> ConstantStack(this Token t, Scope scope)
    {
        if (t is IConstantProvider cons && cons.GetConstant(scope) is Either<double, long> res)
            return IConstantProvider.ResultStack(res);
        return (t as ICodeProvider)!.ResultStack(scope);
    }

    public static string ConstantRoot(this Token t, Scope scope)
    {
        if (t is IConstantProvider cons && cons.GetConstant(scope) is Either<double, long> res)
            return "";
        return (t as IRootCodeProvider)?.ProvidedRootCode(scope) ?? "";
    }

    public static bool IsConstant(this Token t, Scope scope)
    {
        return (t is IConstantProvider cons && cons.GetConstant(scope) is Either<double, long>);
    }
    public static bool IsConstant(this Token t, Scope scope, out Either<double, long> res)
    {
        if (t is IConstantProvider cons && cons.GetConstant(scope) is Either<double, long> r)
        {
            res = r;
            return true;
        }
        res = null!;
        return false;
    }
}