using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tacoly.Tokenizer.Properties;
using Tacoly.Util;

namespace Tacoly.Tokenizer.Tokens;

public class VarIdentifier : Token, ICodeProvider, IAssignable
{
    public required string Identifier { get; set; }
    public VarIdentifier(string raw, string file) : base(raw, file) { }

    [RegisterClaimer()]
    public static VarIdentifier? Claim(StringClaimer claimer)
    {
        Claim flag = claimer.Flag();
        Claim ident = claimer.Identifier();
        if (!ident.Success)
        {
            flag.Fail();
            return null;
        }

        return new VarIdentifier(claimer.Raw(flag), claimer.File)
        {
            Identifier = ident.Match!.Value
        };
    }

    public string ProvidedCode(Scope scope)
    {
        if (scope.Get(Identifier) is not Variable var)
        {
            throw new Exception($"Variable {Identifier} not found in scope");
        }
        return $"(global.get ${var.Label})";

    }
    public IEnumerable<VarType> ResultStack(Scope scope)
    {
        if (scope.Get(Identifier) is not Variable var)
        {
            throw new Exception($"Variable {Identifier} not found in scope");
        }
        return new VarType[] { var.Type };
    }

    public bool CanAssign(Scope scope, IEnumerable<VarType> types)
    {
        if (scope.Get(Identifier) is not Variable var)
        {
            throw new Exception($"Variable {Identifier} not found in scope");
        }
        return types.Any(t => t.CanCoax(var.Type));
    }
    public string AssignValue(Scope scope, IEnumerable<VarType> types)
    {
        if (scope.Get(Identifier) is not Variable var)
        {
            throw new Exception($"Variable {Identifier} not found in scope");
        }
        var typ = var.Type;
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
        if (scope.Get(Identifier) is not Variable var)
        {
            throw new Exception($"Variable {Identifier} not found in scope");
        }
        VarType typ = var.Type;
        if (typ.Name == "var") return types.Skip(1);
        while (types.Any() && !types.First().CanCoax(typ)) types = types.Skip(1);
        types = types.Skip(1);
        return types;
    }
}