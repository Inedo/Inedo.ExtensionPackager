using System.Collections.Immutable;

namespace Inedo.ExtensionPackager
{
    internal sealed record InputArgs(ImmutableArray<string> Positional, ImmutableDictionary<string, string> Named)
    {
        public static InputArgs Parse(string[] args)
        {
            var positional = new List<string>();
            var options = ImmutableDictionary<string, string>.Empty.ToBuilder();

            foreach (var a in args)
            {
                if (!a.StartsWith('-'))
                {
                    positional.Add(a);
                    continue;
                }

                try
                {
                    var trimmed = a.AsSpan().TrimStart('-');
                    int equalsIndex = trimmed.IndexOf('=');
                    if (equalsIndex < 0)
                    {
                        options.Add(trimmed.ToString(), string.Empty);
                        continue;
                    }

                    var name = trimmed[..equalsIndex];
                    var value = equalsIndex < trimmed.Length - 1 ? trimmed[(equalsIndex + 1)..] : default;
                    options.Add(name.ToString(), value.ToString());
                }
                catch (ArgumentException)
                {
                    throw new ConsoleException($"Duplicate argument: {a}");
                }
            }

            return new InputArgs(positional.ToImmutableArray(), options.ToImmutable());
        }
    }
}
