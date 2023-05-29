using System.Diagnostics;
using System.Text;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Tacoly.Tokenizer.Properties;
using Tacoly.Util;
using System;

namespace Tacoly.Tokenizer.Tokens;


public struct Operator
{
    public string Symbol;
    public string Name;
    public uint Precedence;
    public int Priority;
    public Func<long, long, Either<double, long>>? LongMath;
    public Func<double, double, Either<double, long>>? DoubleMath;
}

public class Math : Token, ICodeProvider, IConstantProvider, IRootCodeProvider
{
    public required Token Left;
    public required Token Right;
    public required Operator Operator;

    public static readonly Operator[] Operators = new[] {
        new Operator { Symbol = "||", Name = "or", Priority = 0, Precedence = 3, LongMath = (a,b)=> a!=0||b!=0 ? 1 : 0},
        new Operator { Symbol = "&&", Name = "and", Priority = 0, Precedence = 4, LongMath = (a,b)=> a!=0&&b!=0 ? 1 : 0},
        new Operator { Symbol = "|", Name = "bor", Priority = 0, Precedence = 5, LongMath = (a,b)=> a|b},
        new Operator { Symbol = "^", Name = "bxor", Priority = 0, Precedence = 6, LongMath = (a,b)=> a^b},
        new Operator { Symbol = "&", Name = "band", Priority = 0, Precedence = 7, LongMath = (a,b)=> a&b},
        new Operator { Symbol = "==", Name = "eq", Priority = 0, Precedence = 8, LongMath = (a,b)=> a==b ? 1 : 0, DoubleMath = (a,b)=>a==b ? 1 : 0},
        new Operator { Symbol = "!=", Name = "ne", Priority = 0, Precedence = 8, LongMath = (a,b)=> a!=b ? 1 : 0, DoubleMath = (a,b)=>a!=b ? 1 : 0},
        new Operator { Symbol = "<", Name = "lt", Priority = 0, Precedence = 9, LongMath = (a,b)=> a<b ? 1 : 0, DoubleMath = (a,b)=>a<b ? 1 : 0},
        new Operator { Symbol = ">", Name = "gt", Priority = 0, Precedence = 9, LongMath = (a,b)=> a>b ? 1 : 0, DoubleMath = (a,b)=>a>b ? 1 : 0},
        new Operator { Symbol = "<=", Name = "le", Priority = 1, Precedence = 9, LongMath = (a,b)=> a<=b ? 1 : 0, DoubleMath = (a,b)=>a<=b ? 1 : 0},
        new Operator { Symbol = ">=", Name = "ge", Priority = 1, Precedence = 9, LongMath = (a,b)=> a>=b ? 1 : 0, DoubleMath = (a,b)=>a>=b ? 1 : 0},
        new Operator { Symbol = "+", Name = "add", Priority = 0, Precedence = 11, LongMath = (a,b)=> a+b, DoubleMath = (a,b)=>a+b},
        new Operator { Symbol = "-", Name = "sub", Priority = 0, Precedence = 11, LongMath = (a,b)=> a-b, DoubleMath = (a,b)=>a-b},
        new Operator { Symbol = "*", Name = "mul", Priority = 0, Precedence = 12, LongMath = (a,b)=> a*b, DoubleMath = (a,b)=>a*b},
        new Operator { Symbol = "/", Name = "div", Priority = 0, Precedence = 12, LongMath = (a,b)=> a/b, DoubleMath = (a,b)=>a/b},
        new Operator { Symbol = "%", Name = "mod", Priority = 0, Precedence = 12, LongMath = (a,b)=> a%b, DoubleMath = (a,b)=>a%b},
        new Operator { Symbol = "/%", Name = "divmod", Priority = 1, Precedence = 12},
        new Operator { Symbol = "**", Name = "pow", Priority = 1, Precedence = 13, LongMath = (a,b)=>System.Math.Pow(a,b), DoubleMath = (a,b)=>System.Math.Pow(a,b)},
    };

    public static void RegisterMathOps()
    {
        foreach (var op in Operators)
        {
            Register((claimer, left) => Claim(claimer, left, op), op.Precedence, op.Priority, new[] { typeof(ICodeProvider), typeof(IConstantProvider) });
        }
    }

    public Math(string raw, string file) : base(raw, file) { }

    public static Math? Claim(StringClaimer claimer, Token left, Operator op)
    {
        Claim flag = claimer.Flag();

        if (!claimer.Claim(Regex.Escape(op.Symbol)).Success)
        {
            flag.Fail();
            return null;
        }

        Token? right = Claim<ICodeProvider, IConstantProvider>(claimer, op.Precedence);
        if (right is null)
        {
            flag.Fail();
            return null;
        }

        return new Math(left.Raw + claimer.Raw(flag), claimer.File)
        {
            Left = left,
            Right = right,
            Operator = op
        };
    }

    public Either<double, long>? GetConstant(Scope scope)
    {
        if (Left is not IConstantProvider l || Right is not IConstantProvider r)
            return null;
        Either<double, long>? left = null;
        if (Operator.Name is "and" or "or")
        { // Use short circuit logic
            left = l.GetConstant(scope);
            if (left is not null)
            {
                if (Operator.Name is "and" && left.Match(c => c, c => c) == 0)
                    return left;
                if (Operator.Name is "or" && left.Match(c => c, c => c) != 0)
                    return left;
            }
            return r.GetConstant(scope);
        }
        var lConstant = left ?? l.GetConstant(scope);
        var rConstant = r.GetConstant(scope);
        if (lConstant is null || rConstant is null)
            return null;

        if ((lConstant.IsLeft(out double lFloat) || rConstant.IsLeft(out double rFloat)) && Operator.DoubleMath is not null)
            return Operator.DoubleMath(lConstant.Match(c => c, c => c), rConstant.Match(c => c, c => c));

        if (Operator.LongMath is not null)
            return Operator.LongMath(lConstant.Match(c => (long)c, c => c), rConstant.Match(c => (long)c, c => c));

        return null;
    }

    public string ProvidedCode(Scope scope)
    {
        if (Left is IConstantProvider l && Operator.Name is "and" or "or")
        {
            var left = l.GetConstant(scope);
            if (left is not null)
            {
                if (Operator.Name is "and" && left.Match(c => c, c => c) == 0)
                    return IConstantProvider.ProvidedCode(left);
                if (Operator.Name is "or" && left.Match(c => c, c => c) != 0)
                    return IConstantProvider.ProvidedCode(left);

                string? rightCode = null;
                if (Right is ICodeProvider rCode)
                {
                    rightCode = rCode.ProvidedCode(scope);
                }
                if (Right is IConstantProvider r)
                {
                    var right = r.GetConstant(scope);
                    if (right != null)
                    {
                        rightCode = IConstantProvider.ProvidedCode(right);
                    }
                }
                Debug.Assert(rightCode is not null, "Right is not ICodeProvider or IConstantProvider (Impossible?)");
                return rightCode;
            }
        }
        var constant = GetConstant(scope);
        if (constant is not null) return IConstantProvider.ProvidedCode(constant);
        {
            string? leftCode = null;
            IEnumerable<VarType> leftTypes = Enumerable.Empty<VarType>();
            Either<double, long>? leftConst = null;
            if (Left is IConstantProvider l2 && (leftConst = l2.GetConstant(scope)) is not null)
            {
                leftCode = IConstantProvider.ProvidedCode(leftConst);
                leftTypes = IConstantProvider.ResultStack(leftConst);
            }
            else
            {
                if (Left is not ICodeProvider lcp) throw new Exception("Impossible");
                leftCode = lcp.ProvidedCode(scope);
                leftTypes = lcp.ResultStack(scope);
            }
            string? rightCode = null;
            Either<double, long>? rightConst = null;
            IEnumerable<VarType> rightTypes = Enumerable.Empty<VarType>();
            if (Right is IConstantProvider r2 && (rightConst = r2.GetConstant(scope)) is not null)
            {
                rightCode = IConstantProvider.ProvidedCode(rightConst);
                rightTypes = IConstantProvider.ResultStack(rightConst);
            }
            else
            {
                if (Right is not ICodeProvider rcp) throw new Exception("Impossible");
                rightCode = rcp.ProvidedCode(scope);
                rightTypes = rcp.ResultStack(scope);
            }
            if (Operator.Name is "and" or "or" && leftConst is not null)
            {
                if (Operator.Name is "and" && leftConst.Match(c => c, c => c) == 0)
                    return leftCode;
                if (Operator.Name is "or" && leftConst.Match(c => c, c => c) != 0)
                    return leftCode;
                return rightCode;
            }
            StringBuilder code = new();
            var lTypes = leftTypes.ToList();
            var rTypes = rightTypes.ToList();
            if (lTypes.Count == 0 || rTypes.Count == 0)
                throw new Exception("Partial expression has no type.");
            code.MaybeAppendLine(leftCode);
            for (int i = 1; i < lTypes.Count; i++)
                code.MaybeAppendLine("drop");
            if (Operator.Name is "and" or "or")
            {
                code.AppendLine($"(call ${lTypes[0].GetDefinition().InternalType}dup)");
                // Get the truthy metamethod, if any
                var truthyMeta = lTypes[0].GetDefinition().GetMeta("truthy", new VarType[] { lTypes[0] });
                if (truthyMeta is not null)
                {
                    code.MaybeAppendLine(truthyMeta.Value.DeOffset());
                }
                VarType rootType = VarType.MostCommonRoot(lTypes[0], rTypes[0]);
                StringBuilder codeA = new();
                codeA.MaybeAppendLine(lTypes[0].Coax(rootType).Tabbed().Tabbed());
                StringBuilder codeB = new();
                codeB.MaybeAppendLine("drop".Tabbed().Tabbed());
                codeB.MaybeAppendLine(rightCode.Tabbed().Tabbed());
                for (int i = 1; i < rTypes.Count; i++)
                    codeB.MaybeAppendLine("drop".Tabbed().Tabbed());
                codeB.MaybeAppendLine(rTypes[0].Coax(rootType).Tabbed().Tabbed());

                code.MaybeAppendLine($"(if (param {lTypes[0].GetDefinition().InternalType}) (result {rootType.GetDefinition().InternalType})");
                code.MaybeAppendLine($"\t(then");
                code.MaybeAppendLine(Operator.Name == "and" ? codeB.ToString() : codeA.ToString());
                code.MaybeAppendLine($"\t) (else");
                code.MaybeAppendLine(Operator.Name == "and" ? codeA.ToString() : codeB.ToString());
                code.MaybeAppendLine("\t)");
                code.MaybeAppendLine(")");
                return code.ToString();
            }
            var meta = (lTypes[0].GetDefinition().GetMeta(Operator.Name, new VarType[] { lTypes[0], rTypes[0] }) ?? rTypes[0].GetDefinition().GetMeta(Operator.Name, new VarType[] { lTypes[0], rTypes[0] })) ?? throw new Exception($"No metamethod {Operator.Name}({lTypes[0]}, {rTypes[0]})");
            FuncType metaType = (meta.Type as FuncType)!;
            code.MaybeAppendLine(rightCode);
            for (int i = 1; i < rTypes.Count; i++)
                code.MaybeAppendLine("drop");
            code.MaybeAppendLine(VarType.CoaxStack(new VarType[] { lTypes[0], rTypes[0] }, metaType.ParameterTypes));
            code.MaybeAppendLine(meta.DeOffset());
            return code.ToString();
        }
    }

    public IEnumerable<VarType> ResultStack(Scope scope)
    {
        if (Left is IConstantProvider l && Operator.Name is "and" or "or")
        {
            var left = l.GetConstant(scope);
            if (left is not null)
            {
                if (Operator.Name is "and" && left.Match(c => c, c => c) == 0)
                    return IConstantProvider.ResultStack(left);
                if (Operator.Name is "or" && left.Match(c => c, c => c) != 0)
                    return IConstantProvider.ResultStack(left);
                IEnumerable<VarType>? rightStack = null;
                if (Right is ICodeProvider rCode)
                {
                    rightStack = rCode.ResultStack(scope);
                }
                if (Right is IConstantProvider r)
                {
                    var right = r.GetConstant(scope);
                    if (right != null)
                    {
                        rightStack = IConstantProvider.ResultStack(right);
                    }
                }
                Debug.Assert(rightStack is not null, "Right is not ICodeProvider or IConstantProvider (Impossible?)");
                return rightStack;
            }
        }
        var constant = GetConstant(scope);
        if (constant is not null) return IConstantProvider.ResultStack(constant);
        {
            IEnumerable<VarType> leftTypes = Enumerable.Empty<VarType>();
            Either<double, long>? leftConst = null;
            if (Left is IConstantProvider l2 && (leftConst = l2.GetConstant(scope)) is not null)
            {
                leftTypes = IConstantProvider.ResultStack(leftConst);
            }
            else
            {
                if (Left is not ICodeProvider lcp) throw new Exception("Impossible");
                leftTypes = lcp.ResultStack(scope);
            }
            Either<double, long>? rightConst = null;
            IEnumerable<VarType> rightTypes = Enumerable.Empty<VarType>();
            if (Right is IConstantProvider r2 && (rightConst = r2.GetConstant(scope)) is not null)
            {
                rightTypes = IConstantProvider.ResultStack(rightConst);
            }
            else
            {
                if (Right is not ICodeProvider rcp) throw new Exception("Impossible");
                rightTypes = rcp.ResultStack(scope);
            }
            if (Operator.Name is "and" or "or" && leftConst is not null)
            {
                if (Operator.Name is "and" && leftConst.Match(c => c, c => c) == 0)
                    return leftTypes;
                if (Operator.Name is "or" && leftConst.Match(c => c, c => c) != 0)
                    return leftTypes;
                return rightTypes;
            }
            var lTypes = leftTypes.ToList();
            var rTypes = rightTypes.ToList();
            if (lTypes.Count == 0 || rTypes.Count == 0)
                throw new Exception("Partial expression has no type.");
            if (Operator.Name is "and" or "or")
            {
                return new VarType[] { VarType.MostCommonRoot(lTypes[0], rTypes[0]) };
            }
            var meta = (lTypes[0].GetDefinition().GetMeta(Operator.Name, new VarType[] { lTypes[0], rTypes[0] }) ?? rTypes[0].GetDefinition().GetMeta(Operator.Name, new VarType[] { lTypes[0], rTypes[0] })) ?? throw new Exception($"No metamethod {Operator.Name}({lTypes[0]}, {rTypes[0]})");
            return (meta.Type as FuncType)!.ReturnTypes;
        }
    }

    public string ProvidedRootCode(Scope scope)
    {
        if (GetConstant(scope) is not null) return "";
        StringBuilder sb = new();
        {
            if (Left is IRootCodeProvider lrcp)
                if (Left is not IConstantProvider lcst || lcst.GetConstant(scope) == null)
                    sb.MaybeAppendLine(lrcp.ProvidedRootCode(scope));
            if (Right is IRootCodeProvider rrcp)
                if (Right is not IConstantProvider rcst || rcst.GetConstant(scope) == null)
                    sb.MaybeAppendLine(rrcp.ProvidedRootCode(scope));
        }
        if (!(Operator.Name is "and" or "or"))
        {
            if (Left is not IConstantProvider lcst || lcst.GetConstant(scope) == null)
            {
                var lcp = (ICodeProvider)Left;
                IEnumerable<VarType> leftTypes = lcp.ResultStack(scope);
                Either<double, long>? rightConst = null;
                IEnumerable<VarType> rightTypes = Enumerable.Empty<VarType>();
                if (Right is IConstantProvider r2 && (rightConst = r2.GetConstant(scope)) is not null)
                {
                    rightTypes = IConstantProvider.ResultStack(rightConst);
                }
                else
                {
                    if (Right is not ICodeProvider rcp) throw new Exception("Impossible");
                    rightTypes = rcp.ResultStack(scope);
                }
                var lTypes = leftTypes.ToList();
                var rTypes = rightTypes.ToList();
                if (lTypes.Count == 0 || rTypes.Count == 0)
                    throw new Exception("Partial expression has no type.");
                var meta = (lTypes[0].GetDefinition().GetMeta(Operator.Name, new VarType[] { lTypes[0], rTypes[0] }) ?? rTypes[0].GetDefinition().GetMeta(Operator.Name, new VarType[] { lTypes[0], rTypes[0] })) ?? throw new Exception($"No metamethod {Operator.Name}({lTypes[0]}, {rTypes[0]})");
                var metaType = (FuncType)meta.Type;
                sb.MaybeAppendLine(VarType.WithGenerateCoaxStack(new VarType[] { lTypes[0], rTypes[0] }, metaType.ParameterTypes));
            }
        }
        return sb.ToString();
    }
}