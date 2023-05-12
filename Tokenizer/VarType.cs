using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System;
using Tacoly.Tokenizer.Properties;

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
        if (_cachedTypes.ContainsKey(str)) return _cachedTypes[str];
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
        if (!Like(to)) return 4; // Only do-able via Metamethod then.
        int totalCost = GenericArguments.Zip(to.GenericArguments).Select(a => a.Item1.CoaxCost(a.Item2)).Sum(); ;
        if (this.PointerDepth > to.PointerDepth)
            totalCost += 1;
        if (this.Name != to.Name)
            totalCost += 2;
        return totalCost;

    }

    public virtual bool Like(VarType other, bool implicitDereference = false)
    {
        if (other is FuncType) return false;
        return (implicitDereference ? PointerDepth >= other.PointerDepth : PointerDepth == other.PointerDepth)
            && Root.GetDefinition().IsChildOf(other.Root.GetDefinition())
            && GenericArguments.Count() == other.GenericArguments.Count()
            && GenericArguments.Zip(other.GenericArguments).All(a => a.Item1.Like(a.Item2));
    }

    public virtual bool CanCoax(VarType to)
    {
        if (Like(to, true)) return true;
        var metaMethod = GetDefinition().GetMeta("cast", Enumerable.Empty<VarType>(), new VarType[] { to });
        if (metaMethod is not null) return true;
        metaMethod = to.GetDefinition().GetMeta("cast", new VarType[] { this }, new VarType[] { to });
        return metaMethod is not null;
    }

    public virtual string Coax(VarType to)
    {
        Debug.Assert(CanCoax(to), $"Can't coax {this} to {to}");
        if (Like(to, true))
        {
            if (this.PointerDepth - to.PointerDepth >= 1)
                return String.Join('\n', Enumerable.Repeat("(i32.load)", (this.PointerDepth - to.PointerDepth) - 1).Append($"({to.GetDefinition().InternalType}.load)"));
            return "";
        }
        var metaMethod = GetDefinition().GetMeta("cast", Enumerable.Empty<VarType>(), new VarType[] { to })
                   ?? to.GetDefinition().GetMeta("cast", new VarType[] { this }, new VarType[] { to });
        Debug.Assert(metaMethod is not null, $"Couldn't coax {this} to {to} (Impossible?)");
        return metaMethod.Value.DeOffset();
    }

    public override string ToString()
    {
        if (this.GenericArguments.Count() > 0)
            return new string('@', this.PointerDepth) + this.Name + '<' + String.Join(", ", this.GenericArguments) + ">";
        return new string('@', this.PointerDepth) + this.Name;
    }

    public static string InternalTypes(IEnumerable<VarType> types)
    {
        return String.Join(' ', types.Select(c => c.GetDefinition().InternalType));
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
            && ParameterTypes.Zip(otherF.ParameterTypes).All(a => a.Item1.Like(a.Item2))
            && ReturnTypes.Zip(otherF.ReturnTypes).All(a => a.Item1.Like(a.Item2));
    }

    public override string ToString()
    {
        if (this.ReturnTypes.Count() > 0)
            return new string('@', this.PointerDepth) + "func(" + String.Join(", ", this.ParameterTypes) + "): " + String.Join(", ", this.ReturnTypes);
        return new string('@', this.PointerDepth) + "func(" + String.Join(", ", this.ParameterTypes) + ")";
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