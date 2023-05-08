using System.Collections.Generic;
using System.Linq;

namespace Tacoly;

public class VarType
{
    public string Name;
    public int PointerDepth;
    public IEnumerable<VarType> GenericArguments;

    public static VarType VOID = new()
    {
        Name = "void",
        PointerDepth = 0,
        GenericArguments = Enumerable.Empty<VarType>()
    };
    public static VarType INT = new()
    {
        Name = "int",
        PointerDepth = 0,
        GenericArguments = Enumerable.Empty<VarType>()
    };
    public static VarType FLOAT = new()
    {
        Name = "float",
        PointerDepth = 0,
        GenericArguments = Enumerable.Empty<VarType>()
    };
}

public class VarTypeToken : Token, ITypeProvider
{
    string typeName;
    int pointerDepth;
    Token? genericArgs;

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
            typeName = ident.Match!.Value,
            pointerDepth = ptrDepth,
            genericArgs = genericArgs
        };
    }

    private VarTypeToken(string raw, string file) : base(raw, file) { }

    public VarType ProvidedType(Scope scope)
    {
        IEnumerable<VarType> args = Enumerable.Empty<VarType>();
        if (genericArgs is ITypesProvider tps)
        {
            args = tps.ProvidedTypes(scope);
        }
        else if (genericArgs is ITypeProvider tp)
        {
            args = new VarType[] { tp.ProvidedType(scope) };

        }
        return new VarType()
        {
            Name = typeName,
            PointerDepth = pointerDepth,
            GenericArguments = args
        };
    }
}