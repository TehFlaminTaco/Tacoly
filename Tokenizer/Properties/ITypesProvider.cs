using System.Collections.Generic;

namespace Tacoly;

interface ITypesProvider
{
    public IEnumerable<VarType> ProvidedTypes(Scope scope);
}