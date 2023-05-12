using System.Collections.Generic;
using Tacoly.Tokenizer;
using Tacoly.Tokenizer.Properties;

namespace Tacoly.Tokenizer.Tokens;

public class VariableDeclaration : Token, IRootCodeProvider, ICodeProvider
{
    public VariableDeclaration(string raw, string file) : base(raw, file) { }

    public required ITypeProvider Type;
    public required string Identifier;

    [RegisterLeftClaimer<ITypeProvider>]
    public static VariableDeclaration? Claim(StringClaimer claimer, Token left)
    {
        var flag = claimer.Flag();
        var ident = claimer.Identifier();
        if (ident is null)
        {
            flag.Fail();
            return null;
        }

        return new VariableDeclaration(left.Raw + " " + claimer.Raw(flag), claimer.File)
        {
            Type = (ITypeProvider)left,
            Identifier = ident.Match!.Value
        };
    }

    public string ProvidedCode(Scope scope)
    {
        var label = scope.Get(Identifier)!.Value.Label;
        return $"(global ${label})";
    }

    public string ProvidedRootCode(Scope scope)
    {
        var varType = Type.ProvidedType(scope);
        var typeDef = varType.GetDefinition();
        var label = scope.Make(Identifier, varType);
        return $"(global ${label} (mut {typeDef.InternalType}) ({typeDef.InternalType}.const 0))";
    }

    public IEnumerable<VarType> ResultStack(Scope scope)
    {
        return new[] { Type.ProvidedType(scope) };
    }
}