using System.Collections.Generic;

namespace Tacoly;

public interface ICodeProvider
{
    public string ProvidedCode(Scope scope);
    public IEnumerable<VarType> ResultStack(Scope scope);
}