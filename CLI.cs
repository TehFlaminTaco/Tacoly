﻿using System;
using System.Linq;

namespace Tacoly;

public class CLI
{
    public static void Main(string[] args)
    {
        Token.RegisterDefaultClaimers();

        Console.WriteLine(Token.Claimers.Count);
        Console.WriteLine(Token.LeftClaimers.Count);
        StringClaimer claimer = new("13", "test.taco");
        Program p = Program.Claim(claimer);
        Console.WriteLine(p.GetCode());

    }
}