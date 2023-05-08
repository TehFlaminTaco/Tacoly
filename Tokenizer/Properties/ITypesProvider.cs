using System.Collections.Generic;

namespace Tacoly.Tokenizer.Properties;

interface ITypesProvider
{
    public IEnumerable<VarType> ProvidedTypes(Scope scope);
}