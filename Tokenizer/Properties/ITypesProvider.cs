using System.Collections.Generic;

namespace Tacoly.Tokenizer.Properties;

public interface ITypesProvider
{
    public IEnumerable<VarType> ProvidedTypes(Scope scope);
}