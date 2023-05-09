using System;

namespace Tacoly.Util;

public class Either<A, B>
{
    public Either(A left)
    {
        Set(left);
    }

    public Either(B right)
    {
        Set(right);
    }

    public Either(object obj)
    {
        if (obj is A a)
            Set(a);
        else if (obj is B b)
            Set(b);
        else
            throw new Exception("Either is not " + typeof(A).Name + " or " + typeof(B).Name);
    }

    private bool Lefty = false;
    public A? Left;
    public B? Right;

    public void Set(A left)
    {
        Lefty = true;
        Left = left;
        Right = default;
    }

    public void Set(B right)
    {
        Lefty = false;
        Right = right;
        Left = default;
    }

    public A GetLeft()
    {
        if (Lefty)
            return Left!;
        throw new Exception("Either is not " + typeof(A).Name);
    }

    public B GetRight()
    {
        if (!Lefty)
            return Right!;
        throw new Exception("Either is not " + typeof(B).Name);
    }

    public bool IsLeft()
    {
        return Lefty;
    }

    public bool IsRight()
    {
        return !Lefty;
    }

    public bool IsLeft(out A left)
    {
        left = Left!;
        return Lefty;
    }

    public bool IsRight(out B right)
    {
        right = Right!;
        return !Lefty;
    }

    public T Match<T>(Func<A, T> left, Func<B, T> right)
    {
        return Lefty
            ? left(Left!)
            : right(Right!);
    }

    public static implicit operator A(Either<A, B> either)
    {
        return either.GetLeft();
    }

    public static implicit operator B(Either<A, B> either)
    {
        return either.GetRight();
    }

    public static implicit operator Either<A, B>(A left)
    {
        return new Either<A, B>(left);
    }

    public static implicit operator Either<A, B>(B right)
    {
        return new Either<A, B>(right);
    }
}