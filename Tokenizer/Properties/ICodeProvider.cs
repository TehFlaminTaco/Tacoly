using System.Collections.Generic;

namespace Tacoly.Tokenizer.Properties;

public interface ICodeProvider
{
    public string ProvidedCode(Scope scope);
    public IEnumerable<VarType> ResultStack(Scope scope);
}