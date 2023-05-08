namespace Tacoly;

public class Either<A,B>
{
	public A? Left;
	public B? Right;

	public void Set(A left){
		Left = left;
		Right = null;
	}

	public void Set(B right){
		Right = right;
		Left = null;
	}
}