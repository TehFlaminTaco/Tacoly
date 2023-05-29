using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System;
using Tacoly.Tokenizer.Properties;
using Tacoly.Util;

namespace Tacoly.Tokenizer;

public class VarType
{
    public required string Name;
    public required int PointerDepth;
    public required IEnumerable<VarType> GenericArguments;

    public VarType Root => new()
    {
        Name = Name,
        PointerDepth = 0,
        GenericArguments = GenericArguments
    };

    public static VarType VOID { get; private set; } = new()
    {
        Name = "void",
        PointerDepth = 0,
        GenericArguments = Enumerable.Empty<VarType>()
    };
    public static VarType INT { get; private set; } = new()
    {
        Name = "int",
        PointerDepth = 0,
        GenericArguments = Enumerable.Empty<VarType>()
    };
    public static VarType FLOAT { get; private set; } = new()
    {
        Name = "float",
        PointerDepth = 0,
        GenericArguments = Enumerable.Empty<VarType>()
    };

    private static readonly Dictionary<string, VarType> _cachedTypes = new();
    public static VarType Of(string str)
    {
        if (_cachedTypes.TryGetValue(str, out VarType? value)) return value;
        StringClaimer claimer = new(str, "<internal>");
        ITypeProvider? typ = FuncTypeToken.Claim(claimer) as ITypeProvider ?? VarTypeToken.Claim(claimer) as ITypeProvider;
        Debug.Assert(typ is not null, "Could not parse static type: " + str);
        return _cachedTypes[str] = typ.ProvidedType(new Scope());
    }

    public virtual TypeDefinition GetDefinition()
    {
        if (PointerDepth > 0) return TypeDefinition.POINTER;
        if (Name == "int") return TypeDefinition.INT;
        if (Name == "float") return TypeDefinition.FLOAT;
        if (Name == "void") return TypeDefinition.VOID;
        throw new System.Exception($"Invalid type: {this}");
    }

    public virtual int CoaxCost(VarType to)
    {
        Debug.Assert(CanCoax(to), $"Can't coax {this} to {to}");
        if (!Like(to, true)) return 4; // Only do-able via Metamethod then.
        int totalCost = GenericArguments.Zip(to.GenericArguments).Select(a => a.First.CoaxCost(a.Second)).Sum(); ;
        if (this.PointerDepth > to.PointerDepth)
            totalCost += 2;
        if (this.Name != to.Name)
        {
            totalCost += 3;
        }
        return totalCost;

    }

    public virtual bool Like(VarType other, bool implicitDereference = false)
    {
        if (other is FuncType) return false;
        return (implicitDereference ? PointerDepth >= other.PointerDepth : PointerDepth == other.PointerDepth)
            && (
                ((this.Name == "void" || other.Name == "void") && this.PointerDepth > 0 && other.PointerDepth > 0) ||
                Root.GetDefinition().IsChildOf(other.Root.GetDefinition())
            )
            && GenericArguments.Count() == other.GenericArguments.Count()
            && GenericArguments.Zip(other.GenericArguments).All(a => a.First.Like(a.Second));
    }

    public virtual bool CanCoax(VarType to)
    {
        if (Like(to, true)) return true;
        if (to.Name == "void") return true;
        if (Name == "void") return true;
        var metaMethod = GetDefinition().GetMeta("cast", Enumerable.Empty<VarType>(), new VarType[] { to });
        if (metaMethod is not null) return true;
        metaMethod = to.GetDefinition().GetMeta("cast", new VarType[] { this }, new VarType[] { to });
        return metaMethod is not null;
    }

    // Valid types are i32, i64, or f64
    public static string BitCast(string from, string to)
    {
        if (from == to) return "";
        switch (from)
        {
            case "i32":
                {
                    // Firstly, upsize to i64
                    string s = "(i64.extend_i32_s)";
                    switch (to)
                    {
                        case "i64": return s;
                        case "f64": return $"{s}\n(f64.reinterpret_i64)";
                    }
                    break;
                }
            case "i64":
                {
                    switch (to)
                    {
                        case "i32": return "(i32.wrap_i64)";
                        case "f64": return "(f64.reinterpret_i64)";
                    }
                    break;
                }
            case "f64":
                {
                    // Uninterpret to i64
                    string s = "(i64.reinterpret_f64)";
                    switch (to)
                    {
                        case "i32": return $"{s}\n(i32.wrap_i64)";
                        case "i64": return s;
                    }
                    break;
                }
        }
        throw new System.Exception($"Invalid bitcast: {from} -> {to}");
    }

    public static VarType MostCommonRoot(params VarType[] options)
    {
        if (options.Length == 0) return VOID;
        if (options.Length == 1) return options[0];
        foreach (var option in options)
        {
            if (option == VOID) return VOID;
            if (options.All(o => o.CanCoax(option))) return option;
        }
        return VOID;
    }
    public static IEnumerable<VarType> MostCommonStack(IEnumerable<VarType> left, IEnumerable<VarType> right)
    {
        return left.Zip(right).Select(c => MostCommonRoot(c.First, c.Second));
    }

    public virtual string Coax(VarType to)
    {
        Debug.Assert(CanCoax(to), $"Can't coax {this} to {to}");
        if (Like(to, true))
        {
            if (this.PointerDepth - to.PointerDepth >= 1)
                return string.Join('\n', Enumerable.Repeat("(i32.load)", PointerDepth - to.PointerDepth - 1).Append($"({to.GetDefinition().InternalType}.load)"));
            return "";
        }
        if (to.Name == "void" || Name == "void")
        {
            return BitCast(GetDefinition().InternalType, to.GetDefinition().InternalType);
        }

        var metaMethod = GetDefinition().GetMeta("cast", Enumerable.Empty<VarType>(), new VarType[] { to })
                   ?? to.GetDefinition().GetMeta("cast", new VarType[] { this }, new VarType[] { to });
        Debug.Assert(metaMethod is not null, $"Couldn't coax {this} to {to} (Impossible?)");
        return metaMethod.Value.DeOffset();
    }

    public override string ToString()
    {
        if (GenericArguments.Any())
            return new string('@', this.PointerDepth) + this.Name + '<' + string.Join(", ", this.GenericArguments) + ">";
        return new string('@', this.PointerDepth) + this.Name;
    }

    public static string InternalTypes(IEnumerable<VarType> types)
    {
        return string.Join(' ', types.Select(c => c.GetDefinition().InternalType));
    }

    public static string TypePair(IEnumerable<VarType> inTypes, IEnumerable<VarType> outTypes)
    {
        return string.Join(' ', inTypes) + ": " + string.Join(' ', outTypes);
    }

    public static Dictionary<string, string> CoaxLastBodies = new();
    public static Dictionary<string, string> CoaxLastNames = new();
    public static Dictionary<string, bool> CoaxIsGenerated = new();

    public static string CoaxStack(IEnumerable<VarType> inTypes, IEnumerable<VarType> outTypes)
    {
        string ID = TypePair(inTypes, outTypes);
        Debug.Assert(inTypes.Count() >= outTypes.Count(), $"Cannot coax unmatched sides! {ID}");
        string Body = CoaxStackBody(inTypes, outTypes);
        while (outTypes.Any() && inTypes.First().Like(outTypes.First()))
        {
            // Whilst we don't actually need to cast them...
            inTypes = inTypes.Skip(1);
            outTypes = outTypes.Skip(1);
        }
        int overflow = inTypes.Count() - outTypes.Count();
		StringBuilder sb = new();
        for (int i = 0; i < overflow; i++)
            sb.AppendLine("drop");
        if (!outTypes.Any()) return sb.ToString();
        if (outTypes.Count() == 1 && inTypes.Count() == 1){
			sb.MaybeAppendLine(inTypes.First().Coax(outTypes.First()));
            return sb.ToString();
		}
        Console.WriteLine($"{ID} == {TypePair(inTypes, outTypes)} {overflow}");
        if (CoaxLastBodies.ContainsKey(ID) && CoaxLastBodies[ID] == Body)
        {
            sb.AppendLine($"(call ${CoaxLastNames[ID]})");
            return sb.ToString();
        }
        CoaxLastBodies[ID] = Body;
        CoaxLastNames[ID] = Scope.RandomLabel("coax");
        CoaxIsGenerated[ID] = false;
        sb.AppendLine($"(call ${CoaxLastNames[ID]})");
        return sb.ToString();
    }

    public static string WithGenerateCoaxStack(IEnumerable<VarType> inTypes, IEnumerable<VarType> outTypes)
    {
        string ID = TypePair(inTypes, outTypes);
        string Body = CoaxStackBody(inTypes, outTypes);
        while (outTypes.Any() && inTypes.First().Like(outTypes.First()))
        {
            // Whilst we don't actually need to cast them...
            inTypes = inTypes.Skip(1);
            outTypes = outTypes.Skip(1);
        }
        if (!outTypes.Any()) return "";
        if (outTypes.Count() == 1 && inTypes.Count() == 1)
            return "";
        if (CoaxLastBodies.ContainsKey(ID) && CoaxLastBodies[ID] == Body)
        {
            if (CoaxIsGenerated[ID])
                return "";
            CoaxIsGenerated[ID] = true;
            return $"(func ${CoaxLastNames[ID]}\n{Body.Tabbed()}\n)";
        }
        CoaxLastBodies[ID] = Body;
        CoaxLastNames[ID] = Scope.RandomLabel("coax");
        CoaxIsGenerated[ID] = true;
        return $"(func ${CoaxLastNames[ID]}\n{Body.Tabbed()}\n)";


    }

    public static string CoaxStackBody(IEnumerable<VarType> inTypes, IEnumerable<VarType> outTypes)
    {
        string ID = TypePair(inTypes, outTypes);
        Debug.Assert(inTypes.Count() >= outTypes.Count(), $"Cannot coax unmatched sides! {ID}");
        while (outTypes.Any() && inTypes.First().Like(outTypes.First()))
        {
            // Whilst we don't actually need to cast them...
            inTypes = inTypes.Skip(1);
            outTypes = outTypes.Skip(1);
        }
        if (!outTypes.Any()) return "";
        if (inTypes.Count() == 1 && outTypes.Count() == 1)
            return "";
        StringBuilder sb = new();
        sb.Append("(params ");
        sb.Append(InternalTypes(inTypes));
        sb.AppendLine(")");
        sb.Append("(result ");
        sb.Append(InternalTypes(outTypes));
        sb.AppendLine(")");
        int index = 0;
        foreach ((var i, var o) in inTypes.Zip(outTypes))
        {
            sb.Append("local.get ");
            sb.AppendLine(index.ToString());
            sb.MaybeAppendLine(i.Coax(o));
            index++;
        }
        return sb.ToString();
    }
}

public class FuncType : VarType
{
    public required IEnumerable<VarType> ParameterTypes;
    public required IEnumerable<VarType> ReturnTypes;

    public override bool Like(VarType other, bool implicitDereference = false)
    {
        if (other is not FuncType otherF) return false;
        return (implicitDereference ? PointerDepth >= other.PointerDepth : PointerDepth == other.PointerDepth)
            && ParameterTypes.Count() == otherF.ParameterTypes.Count()
            && ReturnTypes.Count() == otherF.ReturnTypes.Count()
            && ParameterTypes.Zip(otherF.ParameterTypes).All(a => a.First.Like(a.Second))
            && ReturnTypes.Zip(otherF.ReturnTypes).All(a => a.First.Like(a.Second));
    }

    public override string ToString()
    {
        if (ReturnTypes.Any())
            return new string('@', PointerDepth) + "func(" + string.Join(", ", ParameterTypes) + "): " + string.Join(", ", ReturnTypes);
        return new string('@', PointerDepth) + "func(" + string.Join(", ", ParameterTypes) + ")";
    }
}

public class VarTypeToken : Token, ITypeProvider
{
    public required string TypeName;
    public required int PointerDepth;
    public required Token? GenericArgs;

    [RegisterClaimer()]
    public static VarTypeToken? Claim(StringClaimer claimer)
    {
        Claim flag = claimer.Flag();
        Claim pointerDepthClaim = claimer.Claim("@+");
        int ptrDepth = pointerDepthClaim.Match?.Value.Length ?? 0;
        Claim ident = claimer.Identifier();
        if (!ident.Success)
        {
            flag.Fail();
            return null;
        }
        Token? genericArgs = null;
        if (claimer.Claim("<").Success)
        {
            genericArgs = Claim<ITypeProvider, ITypesProvider>(claimer);
            if (!claimer.Claim(">").Success)
            {
                flag.Fail();
                return null;
            }
        }
        return new(claimer.Raw(flag), claimer.File)
        {
            TypeName = ident.Match!.Value,
            PointerDepth = ptrDepth,
            GenericArgs = genericArgs
        };
    }

    private VarTypeToken(string raw, string file) : base(raw, file) { }

    public VarType ProvidedType(Scope scope)
    {
        IEnumerable<VarType> args = Enumerable.Empty<VarType>();
        if (GenericArgs is ITypesProvider tps)
            args = tps.ProvidedTypes(scope);
        else if (GenericArgs is ITypeProvider tp)
            args = new VarType[] { tp.ProvidedType(scope) };
        return new VarType()
        {
            Name = TypeName,
            PointerDepth = PointerDepth,
            GenericArguments = args
        };
    }
}

public class FuncTypeToken : Token, ITypeProvider
{
    public required string TypeName;
    public required int PointerDepth;
    public Token? ParameterTypes = null;
    public Token? ReturnTypes = null;

    [RegisterClaimer(uint.MaxValue, 1)]
    public static FuncTypeToken? Claim(StringClaimer claimer)
    {
        Claim flag = claimer.Flag();
        int ptrDepth = claimer.Claim("@+").Match?.Length ?? 0;
        Claim func = claimer.Claim(@"func\b");
        if (!func.Success)
        {
            flag.Fail();
            return null;
        }
        if (!claimer.Claim(@"\(").Success)
        {
            flag.Fail();
            return null;
        }
        Token? paramters = Claim<ITypeProvider, ITypesProvider>(claimer);
        if (!claimer.Claim(@"\)").Success)
        {
            flag.Fail();
            return null;
        }
        Token? returnTypes = null;
        if (claimer.Claim(":").Success)
        {
            returnTypes = Claim<ITypeProvider, ITypesProvider>(claimer);
        }
        return new(claimer.Raw(flag), claimer.File)
        {
            TypeName = "func",
            PointerDepth = ptrDepth,
            ParameterTypes = paramters,
            ReturnTypes = returnTypes
        };
    }

    private FuncTypeToken(string raw, string file) : base(raw, file) { }

    public VarType ProvidedType(Scope scope)
    {
        IEnumerable<VarType> parameters = Enumerable.Empty<VarType>();
        IEnumerable<VarType> returns = Enumerable.Empty<VarType>();
        if (ParameterTypes is ITypesProvider tps)
            parameters = tps.ProvidedTypes(scope);
        else if (ParameterTypes is ITypeProvider tp)
            parameters = new VarType[] { tp.ProvidedType(scope) };

        if (ReturnTypes is ITypesProvider rps)
            returns = rps.ProvidedTypes(scope);
        else if (ReturnTypes is ITypeProvider rp)
            returns = new VarType[] { rp.ProvidedType(scope) };

        return new FuncType()
        {
            Name = TypeName,
            PointerDepth = PointerDepth,
            GenericArguments = Enumerable.Empty<VarType>(),
            ParameterTypes = parameters,
            ReturnTypes = returns
        };
    }
}