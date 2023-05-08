using System.Collections.Generic;
using System.Linq;
using Tacoly.Tokenizer.Properties;

namespace Tacoly.Tokenizer;

public class TypeList : Token, ITypesProvider
{
    public required Token Left;
    public required Token Right;

    [RegisterLeftClaimer<ITypeProvider, ITypesProvider>(1)]
    public static TypeList? Claim(StringClaimer claimer, Token left)
    {
        Claim flag = claimer.Flag();

        if (!claimer.Claim(",").Success)
        {
            flag.Fail();
            return null;
        }

        Token? right = Claim<ITypeProvider, ITypesProvider>(claimer, 1);
        if (right is null)
        {
            flag.Fail();
            return null;
        }

        return new TypeList(left.Raw + claimer.Raw(flag), claimer.File)
        {
            Left = left,
            Right = right
        };
    }

    public TypeList(string raw, string file) : base(raw, file) { }

    public IEnumerable<VarType> ProvidedTypes(Scope scope)
    {
        IEnumerable<VarType> types = Enumerable.Empty<VarType>();
        if (Left is ITypeProvider l)
        {
            types = types.Append(l.ProvidedType(scope));
        }
        else
        {
            types = types.Concat((Left as ITypesProvider)!.ProvidedTypes(scope));
        }
        if (Right is ITypeProvider r)
        {
            types = types.Append(r.ProvidedType(scope));
        }
        else
        {
            types = types.Concat((Right as ITypesProvider)!.ProvidedTypes(scope));
        }
        return types;
    }
}