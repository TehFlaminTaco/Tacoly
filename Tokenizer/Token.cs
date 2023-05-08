using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

namespace Tacoly.Tokenizer;

public struct Claimer
{
    public Func<StringClaimer, Token?> Method;
    public int Priority;
    public uint Precedence;

}

public struct LeftClaimer
{
    public Func<StringClaimer, Token, Token?> Method;
    public int Priority;
    public uint Precedence;
    public IEnumerable<Type> AcceptedTypes;
}

public abstract class Token
{
    public string Raw;
    public string File;
    public static List<Claimer> Claimers { get; private set; } = new();
    public static List<LeftClaimer> LeftClaimers { get; private set; } = new();

    public Token(string raw, string file)
    {
        Raw = raw;
        File = file;
    }

    public static void RegisterDefaultClaimers()
    {
        var TaggedClaimers = Assembly
            .GetExecutingAssembly()
            .GetTypes()
            .SelectMany(c => c.GetMethods())
            .SelectMany(c => c.GetCustomAttributes<RegisterClaimer>()
                            .Select(d => (c, d)));
        var TaggedLeftClaimers = Assembly
            .GetExecutingAssembly()
            .GetTypes()
            .SelectMany(c => c.GetMethods())
            .SelectMany(c => c.GetCustomAttributes<RegisterLeftClaimer>()
                            .Select(d => (c, d)));
        foreach (var (method, attr) in TaggedClaimers)
        {
            Register((Func<StringClaimer, Token?>)Delegate.CreateDelegate(typeof(Func<StringClaimer, Token?>), null, method), attr.Precedence, attr.Priority);
        }
        foreach (var (method, attr) in TaggedLeftClaimers)
        {
            Register((Func<StringClaimer, Token, Token?>)Delegate.CreateDelegate(typeof(Func<StringClaimer, Token, Token?>), null, method), attr.Precedence, attr.Priority, attr.AcceptedTypes);
        }
    }

    public static Token? Claim(StringClaimer claimer, uint minPrecedence = 0, params Type[] typeOptions)
    {
        foreach (var claimMethod in Claimers
            .Where(c => c.Precedence >= minPrecedence)
            .OrderByDescending(c => c.Priority)
            .Select(c => c.Method)
            )
        {
            Claim flag = claimer.Flag();
            Token? result = claimMethod(claimer);
            if (result is null) continue;
            result = LeftClaim(claimer, minPrecedence, typeOptions, result);
            if (OneOf(result, typeOptions))
                return result;
            flag.Fail();
        }
        return null;
    }

    public static Token? Claim<T1>(StringClaimer claimer, uint minPrecedence = 0)
    {
        return Claim(claimer, minPrecedence, typeof(T1));
    }

    public static Token? Claim<T1, T2>(StringClaimer claimer, uint minPrecedence = 0)
    {
        return Claim(claimer, minPrecedence, typeof(T1), typeof(T2));
    }

    public static Token? LeftClaim(StringClaimer claimer, uint minPrecedence, IEnumerable<Type> typeOptions, Token left)
    {
        var leftFlag = claimer.Flag();
        foreach (var claimMethod in LeftClaimers
                .Where(c => c.Precedence > minPrecedence)
                .Where(c => OneOf(left, c.AcceptedTypes))
                .OrderByDescending(c => c.Priority)
                .Select(c => c.Method)
            )
        {
            var result = claimMethod(claimer, left);
            var resultFlag = claimer.Flag();
            if (result is null) continue;
            var right = LeftClaim(claimer, minPrecedence, typeOptions, result);
            if (right is not null && OneOf(right, typeOptions))
                return right;
            if (OneOf(result, typeOptions))
            {
                resultFlag.Fail();
                return result;
            }
        }
        leftFlag.Fail();
        return left;
    }

    public static bool OneOf(Token? t, IEnumerable<Type> options)
    {
        return t is not null && options.Any(c => c.IsAssignableFrom(t.GetType()));
    }

    public static bool OneOf(Type t, IEnumerable<Type> options)
    {
        return options.Any(c => c.IsAssignableFrom(t));
    }

    public static void Register(Func<StringClaimer, Token?> method, uint precedence = uint.MaxValue, int priority = 0)
    {
        Claimers.Add(new Claimer()
        {
            Method = method,
            Precedence = precedence,
            Priority = priority,
        });
    }


    public static void Register(Func<StringClaimer, Token, Token?> method, uint precedence, int priority, IEnumerable<Type> acceptedTypes)
    {
        LeftClaimers.Add(new LeftClaimer()
        {
            Method = method,
            Precedence = precedence,
            Priority = priority,
            AcceptedTypes = acceptedTypes
        });
    }

    public override string ToString()
    {
        return $"{this.GetType().Name}({this.Raw})";
    }
}

[AttributeUsage(AttributeTargets.Method)]
public class RegisterClaimer : Attribute
{
    public int Priority;
    public uint Precedence;
    public RegisterClaimer(uint precedence = uint.MaxValue, int priority = 0)
    {
        Priority = priority;
        Precedence = precedence;
    }
}

[AttributeUsage(AttributeTargets.Method)]
public class RegisterLeftClaimer : Attribute
{
    public int Priority;
    public uint Precedence;
    public IEnumerable<Type> AcceptedTypes;
    public RegisterLeftClaimer(uint precedence, int priority, params Type[] acceptedTypes)
    {
        Priority = priority;
        Precedence = precedence;
        AcceptedTypes = acceptedTypes;
    }
}

[AttributeUsage(AttributeTargets.Method)]
public class RegisterLeftClaimer<T1> : RegisterLeftClaimer
{
    public RegisterLeftClaimer(uint precedence = uint.MaxValue, int priority = 0) : base(precedence, priority, typeof(T1)) { }
}

[AttributeUsage(AttributeTargets.Method)]
public class RegisterLeftClaimer<T1, T2> : RegisterLeftClaimer
{
    public RegisterLeftClaimer(uint precedence = uint.MaxValue, int priority = 0) : base(precedence, priority, typeof(T1), typeof(T2)) { }
}