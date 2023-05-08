namespace Tacoly.Tokenizer.Properties;

public interface ITypeProvider
{
    public VarType ProvidedType(Scope scope);
}