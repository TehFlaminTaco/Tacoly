namespace Tacoly;

public interface ITypeProvider
{
    public VarType ProvidedType(Scope scope);
}