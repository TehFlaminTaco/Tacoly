using Tacoly.Util;

namespace Tacoly.Tokenizer.Properties;

public interface IConstantProvider
{
    public Either<double, long>? GetConstant(Scope scope);
}