using System.Text;
using System.Collections.Generic;
using System.Linq;
using Tacoly.Tokenizer;
using Tacoly.Tokenizer.Properties;
using Tacoly.Util;

namespace Tacoly.Tokenizer.Tokens;

public class VariableDeclaration : Token, IRootCodeProvider, ICodeProvider, IAssignable
{
    public VariableDeclaration(string raw, string file) : base(raw, file) { }

    public required ITypeProvider Type;
    public required string Identifier;

    [RegisterLeftClaimer<ITypeProvider>]
    public static VariableDeclaration? Claim(StringClaimer claimer, Token left)
    {
        var flag = claimer.Flag();
        var ident = claimer.Identifier();
        if (!ident.Success)
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
        return $"(global.get ${label})";
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

    public bool CanAssign(Scope scope, IEnumerable<VarType> types)
    {
        if (!types.Any()) return false;
        VarType typ = Type.ProvidedType(scope);
        if (typ.Name == "var") return true;
        return types.Any(t => typ.CanCoax(t));
    }
    public string AssignValue(Scope scope, IEnumerable<VarType> types)
    {
        VarType typ = Type.ProvidedType(scope);
        if (typ.Name == "var")
        {
            Type = new TypeProvider(types.First());
        }
        StringBuilder finalDrops = new();
        while (!types.First().CanCoax(typ))
        {
            finalDrops.AppendLine($"(drop)");
            types = types.Skip(1);
        }
        string coax = types.First().Coax(typ);
        StringBuilder code = new();
        for (int i = 1; i < types.Count(); i++)
        {
            code.MaybeAppendLine($"(drop)");
        }
        code.MaybeAppendLine(coax);
        code.MaybeAppendLine($"(global.set ${scope.Get(Identifier)!.Value.Label})");
        code.MaybeAppendLine(finalDrops);
        return code.ToString();
    }

    public IEnumerable<VarType> ConsumeTypes(Scope scope, IEnumerable<VarType> types)
    {
        VarType typ = Type.ProvidedType(scope);
        if (typ.Name == "var") return types.Skip(1);
        while (types.Any() && !types.First().CanCoax(typ)) types = types.Skip(1);
        types = types.Skip(1);
        return types;
    }
}