using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using Tacoly.Tokenizer;

namespace Tacoly;

public struct Variable
{
    public VarType Type;
    public string Identifier;
    public string Label;
}

public class Scope
{
    private static HashSet<string> allLabels { get; set; } = new();
    private Dictionary<string, Variable> scopedVars { get; init; } = new();
    private List<Variable> scopedMethods { get; init; } = new();
    public Scope? Parent;

    public Variable? Get(string identifier)
    {
        return scopedVars.ContainsKey(identifier) ? scopedVars[identifier] : Parent?.Get(identifier);
    }

    public Variable? Get(string identifier, VarType funcType)
    {
        return (scopedMethods.Where(c => c.Identifier == identifier)
                             .Where(c => c.Type.Like(funcType))
                             .FirstOrDefault() as Variable?) ?? Parent?.Get(identifier, funcType);
    }

    private static readonly Regex BadChars = new(@"\W");
    public string Make(string identifier, VarType type)
    {
        string baselabel = BadChars.Replace($"{type.Name}_{identifier}", "");
        string label = baselabel;
        while (allLabels.Contains(label))
            label += $"{new System.Random().NextInt64() % 16:x}";
        allLabels.Add(label);
        this.scopedVars[identifier] = new()
        {
            Type = type,
            Identifier = identifier,
            Label = label
        };
        return label;
    }
}