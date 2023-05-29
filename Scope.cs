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

public partial class Scope
{
    private static HashSet<string> AllLabels { get; set; } = new();
    private Dictionary<string, Variable> ScopedVars { get; init; } = new();
    private List<Variable> ScopedMethods { get; init; } = new();
    public Scope? Parent;

    public Variable? Get(string identifier)
    {
        return ScopedVars.TryGetValue(identifier, out Variable value) ? value : Parent?.Get(identifier);
    }

    public Variable? Get(string identifier, VarType funcType)
    {
        return ScopedMethods.Where(c => c.Identifier == identifier)
                             .Where(c => c.Type.Like(funcType))
                             .FirstOrDefault() as Variable? ?? Parent?.Get(identifier, funcType);
    }

    public static string RandomLabel(string baselabel)
    {
        string label = baselabel;
        while (AllLabels.Contains(label))
            label += $"{new System.Random().NextInt64() % 16:x}";
        AllLabels.Add(label);
        return label;
    }

    private static readonly Regex BadChars = GenerateBadChars();
    public string Make(string identifier, VarType type)
    {
        string baselabel = BadChars.Replace($"{type.Name}_{identifier}", "");
        string label = RandomLabel(baselabel);
        ScopedVars[identifier] = new()
        {
            Type = type,
            Identifier = identifier,
            Label = label
        };
        return label;
    }

    [GeneratedRegex("\\W")]
    private static partial Regex GenerateBadChars();

	public Scope Sub(){
		return new Scope(){
			Parent = this
		};
	}
}