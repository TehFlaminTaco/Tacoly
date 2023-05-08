using System.Collections.Generic;
using System.Linq;
using Tacoly.Tokenizer.Properties;

namespace Tacoly.Tokenizer;

public class VarType
{
    public required string Name;
    public required int PointerDepth;
    public required IEnumerable<VarType> GenericArguments;

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
        int ptrDepth = claimer.Claim("@+").Match?.Length ?? 0;
        Claim ident = claimer.Claim(StringClaimer.Identifier);
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
        {
            args = tps.ProvidedTypes(scope);
        }
        else if (GenericArgs is ITypeProvider tp)
        {
            args = new VarType[] { tp.ProvidedType(scope) };

        }
        return new VarType()
        {
            Name = TypeName,
            PointerDepth = PointerDepth,
            GenericArguments = args
        };
    }
}