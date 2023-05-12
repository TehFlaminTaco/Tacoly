using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System;
using Tacoly.Tokenizer.Properties;

namespace Tacoly.Tokenizer;

public struct Member
{
    public uint Offset;
    public string Name;
    public VarType Type;

    public string? MetamethodBody;

    public string DeOffset()
    {
        Debug.Assert(MetamethodBody is not null, "Cannot De-Offset non-metamethod!");
        return MetamethodBody.Replace(";Offset;", "" + Offset);
    }
}

public class TypeDefinition
{
    public static Dictionary<string, TypeDefinition> UserTypes { get; private set; } = new();

    public required string Name;
    public string[] GenericNames = Array.Empty<string>();
    public Member[] Members = Array.Empty<Member>();
    public string InternalType = "i32";

    public TypeDefinition? ClassType = null;
    public TypeDefinition? ParentType = null;

    public bool IsChildOf(TypeDefinition other)
    {
        return Name == other.Name || (ParentType != null && ParentType.IsChildOf(other));
    }

    public Member? GetMember(string name)
    {
        return Members.FirstOrDefault(c => c.Name == name);
    }

    public Member? GetMeta(string name, IEnumerable<VarType> parameters, IEnumerable<VarType>? returnTypes = null)
    {
        var allMetaMethods = Members
            .Where(c => c.Name == name)
            .Where(c => c.Type is FuncType)
            .Where(c => (c.Type as FuncType)!.ParameterTypes.Count() == parameters.Count())
            .Where(c => (c.Type as FuncType)!.ParameterTypes.Zip(parameters).All(a => a.Item2.CanCoax(a.Item1)))
            .Where(c => returnTypes is null
                || ((c.Type as FuncType)!.ReturnTypes.Count() == parameters.Count()
                && (c.Type as FuncType)!.ReturnTypes.Zip(returnTypes).All(a => a.Item2.CanCoax(a.Item1))))
            .Select(
                c => (c, (c.Type as FuncType)!.ParameterTypes.Zip(parameters).Sum(a => a.Item2.CoaxCost(a.Item1))
                    + (returnTypes is not null ? (c.Type as FuncType)!.ReturnTypes.Zip(returnTypes).Sum(a => a.Item2.CoaxCost(a.Item1)) : 0))
            )
            .OrderBy(c => c.Item2)
            .ToList();
        if (allMetaMethods.Count == 0) return null;
        if (allMetaMethods.Count > 1) Debug.Assert(allMetaMethods[0].Item2 != allMetaMethods[1].Item2,
                                     $"Metamethod ambigious between {name}({String.Join(", ", (allMetaMethods[0].Item1.Type as FuncType)!.ParameterTypes)}): {String.Join(", ", (allMetaMethods[0].Item1.Type as FuncType)!.ReturnTypes)} and {name}({String.Join(", ", (allMetaMethods[1].Item1.Type as FuncType)!.ParameterTypes)}): {String.Join(", ", (allMetaMethods[1].Item1.Type as FuncType)!.ReturnTypes)}");
        return allMetaMethods[0].Item1;
    }

    public static TypeDefinition INT = new()
    {
        Name = "int",
        InternalType = "i64",
        Members = new[]{
            new Member(){
                Name = "add",
                Type = VarType.Of("func(int,int):int"),
                MetamethodBody = "(i64.add)"
            }
        }
    };

    public static TypeDefinition FLOAT = new()
    {
        Name = "float",
        InternalType = "f64"
    };

    public static TypeDefinition VOID = new()
    {
        Name = "void",
        InternalType = "i64"
    };

    public static TypeDefinition POINTER = new()
    {
        Name = "ptr"
    };
}