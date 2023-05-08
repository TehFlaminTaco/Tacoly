using System;
using System.Linq;
using Tacoly.Tokenizer;
using Tacoly.Tokenizer.Tokens;

namespace Tacoly;

public class CLI
{
    public static void Main(string[] _)
    {
        Token.RegisterDefaultClaimers();

        Console.WriteLine(Token.Claimers.Count);
        Console.WriteLine(Token.LeftClaimers.Count);
        StringClaimer claimer = new("13", "test.taco");
        Program p = Program.Claim(claimer);
        Console.WriteLine(p.GetCode());

    }
}