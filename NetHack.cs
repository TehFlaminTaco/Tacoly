using System.Text.RegularExpressions;

namespace System.Runtime.CompilerServices
{
    /// <summary>
	/// Indicates that compiler support for a particular feature is required for the location where this attribute is applied.
	/// </summary>
	[AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = false)]
    public sealed class CompilerFeatureRequiredAttribute : Attribute
    {
        public CompilerFeatureRequiredAttribute(string featureName)
        {
            FeatureName = featureName;
        }

        /// <summary>
        /// The name of the compiler feature.
        /// </summary>
        public string FeatureName { get; }

        /// <summary>
        /// If true, the compiler can choose to allow access to the location where this attribute is applied if it does not understand <see cref="FeatureName"/>.
        /// </summary>
        public bool IsOptional { get; init; }

        /// <summary>
        /// The <see cref="FeatureName"/> used for the ref structs C# feature.
        /// </summary>
        public const string RefStructs = nameof(RefStructs);

        /// <summary>
        /// The <see cref="FeatureName"/> used for the required members C# feature.
        /// </summary>
        public const string RequiredMembers = nameof(RequiredMembers);
    }

    /// <summary>Specifies that a type has required members or that a member is required.</summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
#if SYSTEM_PRIVATE_CORELIB
	    public
#else
    internal
#endif
        sealed class RequiredMemberAttribute : Attribute
    {

    }
}

namespace System.Diagnostics.CodeAnalysis
{

    /// <summary>
    /// Specifies that this constructor sets all required members for the current type, and callers
    /// do not need to set any required members themselves.
    /// </summary>
    [AttributeUsage(AttributeTargets.Constructor, AllowMultiple = false, Inherited = false)]
#if SYSTEM_PRIVATE_CORELIB
	    public
#else
    internal
#endif
        sealed class SetsRequiredMembersAttribute : Attribute
    { }

}

namespace System.Text.RegularExpressions
{
    public class GeneratedRegex : Attribute
    {
        public GeneratedRegex(string source, RegexOptions options = RegexOptions.None) { }
    }
}

namespace Tacoly
{
    partial class Scope
    {
        private static Regex? _badChars = null;
        private static partial Regex GenerateBadChars()
        {
            return _badChars ??= new Regex("\\W", RegexOptions.Compiled);
        }
    }
}

namespace Tacoly.Tokenizer
{
    partial class StringClaimer
    {
        private static Regex? _whitespace = null;
        private static partial Regex GenerateWhitespaceRegex()
        {
            return _whitespace ??= new Regex("\\G(?://.*|/\\*(?:.|\\s)*(?:\\*|$)/|\\s+)+", RegexOptions.Compiled);
        }
        private static Regex? _identifier = null;
        private static partial Regex GenerateIdentifierRegex()
        {
            return _identifier ??= new Regex("\\G[a-zA-Z_]\\w+", RegexOptions.Compiled);
        }
    }
}