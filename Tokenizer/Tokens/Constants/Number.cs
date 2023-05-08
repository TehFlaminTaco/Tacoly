using System.Collections.Generic;
using System.Linq;

namespace Tacoly;

public class Number : Token, ICodeProvider, IConstantProvider
{
    public Number(string raw, string file) : base(raw, file) { }

    Either<double, long> underlying;

    [RegisterClaimer()]
    public static Number? Claim(StringClaimer claimer)
    {
        Claim numberText = claimer.Claim(@"(?<negative>-?)(?:(?<integer>0(?:x(?<hex_val>[0-9A-Fa-f]+)|b(?<bin_val>[01]+)))|(?:(?<float>(?<int_comp>\d*)\.(?<float_comp>\d+))|(?<int>\d+))(?:e(?<expon>-?\d+))?)");
        if (!numberText.Success) return null;
        bool forcedFloaty = claimer.Claim(@"f", true).Success || numberText.Match!.Value.Contains('.');

        Number numb = new Number(claimer.Raw(numberText), claimer.File);
        if (forcedFloaty)
            numb.floatValue = double.Parse(numberText.Match!.Value);
        else
            numb.intValue = long.Parse(numberText.Match!.Value);
        return numb;
    }

    public string ProvidedCode(Scope scope)
    {
        if (floatValue is not null)
        {
            return $"(f64.const {floatValue})";
        }
        return $"(i64.const {intValue!})";
    }

    public IEnumerable<VarType> ResultStack(Scope scope)
    {
        return new VarType[] { floatValue is not null ? VarType.FLOAT : VarType.INT };
    }
}