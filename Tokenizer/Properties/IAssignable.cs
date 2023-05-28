using System.Collections.Generic;
namespace Tacoly.Tokenizer.Properties;

public interface IAssignable
{
    public bool CanAssign(Scope scope, IEnumerable<VarType> types);
    public string AssignValue(Scope scope, IEnumerable<VarType> types);

    public IEnumerable<VarType> ConsumeTypes(Scope scope, IEnumerable<VarType> types);
}