namespace Tacoly.Tokenizer.Properties;

public class TypeProvider : ITypeProvider
{
    public VarType StoredType { get; set; }
    public TypeProvider(VarType type)
    {
        StoredType = type;
    }
    public VarType ProvidedType(Scope scope)
    {
        return StoredType;
    }
}