using System.Collections.Generic;
using System.Linq;
using Tacoly.Util;

namespace Tacoly.Tokenizer.Properties;

public interface IConstantProvider
{
    public Either<double, long>? GetConstant(Scope scope);

    public static string ProvidedCode(Either<double, long> constant) => constant.Match(
        (double d) => "(f64.const " + d.ToString() + ")",
        (long l) => "(i64.const " + l.ToString() + ")"
    );

    public static IEnumerable<VarType> ResultStack(Either<double, long> constant) => constant.Match(
        (double d) => new VarType[] { VarType.FLOAT },
        (long l) => new VarType[] { VarType.INT }
    );
}