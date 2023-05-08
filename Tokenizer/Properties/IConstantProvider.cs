namespace Tacoly;

public interface IConstantProvider
{
    public Either<double, long>? GetConstant(Scope scope);
}