﻿using System;
using System.Linq;
using Tacoly.Tokenizer;
using Tacoly.Tokenizer.Tokens;

namespace Tacoly;

public class CLI
{
    public static void Main(string[] _)
    {
        Token.RegisterDefaultClaimers();

        StringClaimer claimer = new("if(int john){john}else{4}", "test.taco");
        Program p = Program.Claim(claimer);
        Console.WriteLine(p.GetCode());

    }
}