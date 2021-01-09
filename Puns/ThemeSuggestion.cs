using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Puns
{

public static class ThemeSuggestion
{
    public static readonly Lazy<IReadOnlyCollection<string>> All =
        new(
            () =>
                Suggestions.ThemeSuggestions.Split(
                        new[] { '\r', '\n' },
                        StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
                    )
                    .ToList()
        );
}

}
