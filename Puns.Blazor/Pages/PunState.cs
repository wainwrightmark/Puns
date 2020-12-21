using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Pronunciation;
using WordNet;


namespace Puns.Blazor.Pages
{

    public class Choice<T>
    {
        public Choice(T entity, bool chosen)
        {
            Entity = entity;
            Chosen = chosen;
        }

        public T Entity { get; }

        public bool Chosen { get; set; }

        /// <inheritdoc />
        public override string ToString() => (Entity, Chosen).ToString();
    }

    public sealed class PunState : IDisposable
    {
        public PunState(string initialTheme)
        {
            WordNetEngine =  new WordNetEngine();
            PronunciationEngine =  new PronunciationEngine();
            SpellingEngine = new SpellingEngine();
            Theme = initialTheme;
        }


        public PunCategory PunCategory { get; set; } = PunCategory.Idiom;


        private string _theme = "";

        public string Theme
        {
            get => _theme;
            set
            {
                var changed = !_theme.Trim().Equals(value.Trim(), StringComparison.OrdinalIgnoreCase);

                _theme = value.Trim();

                if (changed)
                {
                    SynSets =
                    GetSynSets(Theme, WordNetEngine).Select((x,i)=> new Choice<SynSet>(x, i == 0)).ToList();
                }

            }
        }

        public IReadOnlyCollection<Choice<SynSet>> SynSets { get; private set; } = new List<Choice<SynSet>>();

        public IReadOnlyCollection<Choice<IGrouping<string, Pun>>> PunList { get; set; } = Array.Empty<Choice<IGrouping<string, Pun>>>();

        public HashSet<string> RevealedWords { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase); //TODO replace with choice

        public bool IsGenerating { get; set; }

        public WordNetEngine WordNetEngine { get; }

        public PronunciationEngine PronunciationEngine { get; }

        public SpellingEngine SpellingEngine { get; }

        private static IEnumerable<SynSet> GetSynSets(string? theme, WordNetEngine wordNetEngine)
        {
            if (string.IsNullOrWhiteSpace(theme))
                return Enumerable.Empty<SynSet>();

            var sets = wordNetEngine.GetSynSets(theme);
            return sets;
        }


        public async Task Find()
        {
            if (string.IsNullOrWhiteSpace(Theme))
            {
                IsGenerating = false;
                return;
            }

            IsGenerating = true;
            PunList = Array.Empty<Choice<IGrouping<string, Pun>>>();
            RevealedWords.Clear();
            var synSets = SynSets.Where(x=>x.Chosen).Select(x=>x.Entity).ToList();

            var task = new Task<IReadOnlyCollection<Choice<IGrouping<string, Pun>>>>(
                ()=>GetPuns(synSets, PunCategory, Theme, WordNetEngine, PronunciationEngine, SpellingEngine));
            task.Start();

            PunList = await task;

            IsGenerating = false;
        }

        private static IReadOnlyCollection<Choice<IGrouping<string, Pun>>> GetPuns(IReadOnlyCollection<SynSet> synSets,
            PunCategory punCategory,
            string theme,
            WordNetEngine wordNetEngine,
            PronunciationEngine pronunciationEngine,
            SpellingEngine spellingEngine)
        {

            //TODO use virtualize https://docs.microsoft.com/en-us/aspnet/core/blazor/webassembly-performance-best-practices?view=aspnetcore-5.0

            var sw = Stopwatch.StartNew();
            Console.WriteLine(@"Getting Puns");

            var puns = PunHelper.GetPuns(punCategory, theme, synSets, wordNetEngine, pronunciationEngine, spellingEngine);

            Console.WriteLine($@"{puns.Count} Puns Got ({sw.Elapsed})");

            var groupedPuns = puns
                .Distinct()
                .SelectMany(pun=> pun.PunWords.Select(punWord=> (pun, punWord)))
                .GroupBy(x => x.punWord, x=>x.pun, StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(x => x.Count());

            HashSet<Pun> usedPuns = new HashSet<Pun>();
            var pairs = from @group in groupedPuns from pun in @group where usedPuns.Add(pun) select (@group.Key, pun);

            var finalList = pairs.GroupBy(x => x.Key, x => x.pun)
                .OrderByDescending(x=>x.Count())
                .Select(x => new Choice<IGrouping<string, Pun>>(x, false)).ToList();

            return finalList;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            WordNetEngine.Dispose();
            PronunciationEngine.Dispose();
        }
    }
}
