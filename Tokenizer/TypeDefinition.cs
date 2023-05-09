using System;
namespace Tacoly.Tokenizer;

public struct Member
{
    public uint Offset;
    public string Name;
    public VarType Type;
}

public class TypeDefinition
{
    public required string Name;
    public string[] GenericNames = Array.Empty<string>();
    public Member[] Members = Array.Empty<Member>();

}