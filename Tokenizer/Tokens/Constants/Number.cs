using System.Collections.Generic;
using System.Linq;
using Tacoly.Util;
using Tacoly.Tokenizer.Properties;

namespace Tacoly.Tokenizer.Tokens.Constants;

public class Number : Token, ICodeProvider, IConstantProvider
{
    private Number(string raw, string file) : base(raw, file) { }

    public required Either<double, long> Underlying { private get; init; }

    [RegisterClaimer()]
    public static Number? Claim(StringClaimer claimer)
    {
        Claim numberText = claimer.Claim(@"(?<negative>-?)(?:(?<integer>0(?:x(?<hex_val>[0-9A-Fa-f]+)|b(?<bin_val>[01]+)))|(?:(?<float>(?<int_comp>\d*)\.(?<float_comp>\d+))|(?<int>\d+))(?:e(?<expon>-?\d+))?)");
        if (!numberText.Success) return null;
        bool forcedFloaty = claimer.Claim(@"f", true).Success || numberText.Match!.Value.Contains('.');

        Number numb = new(claimer.Raw(numberText), claimer.File)
        {
            Underlying = forcedFloaty ? double.Parse(numberText.Match!.Value) : long.Parse(numberText.Match!.Value)
        };
        return numb;
    }

    public string ProvidedCode(Scope scope)
    {
        if (Underlying.IsLeft(out double floatValue))
        {
            return $"(f64.const {floatValue})";
        }
        return $"(i64.const {Underlying.GetRight()})";
    }

    public IEnumerable<VarType> ResultStack(Scope scope)
    {
        return new VarType[] { Underlying.IsLeft() ? VarType.FLOAT : VarType.INT };
    }

    public Either<double, long>? GetConstant(Scope scope)
    {
        return Underlying;
    }
}