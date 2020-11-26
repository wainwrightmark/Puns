using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Pronunciation;
using WordNet;


namespace Puns.Blazor.Pages
{
    public class PunState : IDisposable
    {
        public PunState()
        {
            WordNetEngine =  new WordNetEngine();
            PronunciationEngine =  new PronunciationEngine();
        }


        public PunCategory PunCategory { get; set; } = PunCategory.Idiom;

        public string? Theme { get; set; } = "Fish";

        public IEnumerable<SynSet> SynSets => GetSynSets(Theme, WordNetEngine);


        public HashSet<SynSet> CrossedOffSynsets { get; } = new HashSet<SynSet>();

        public IReadOnlyCollection<IGrouping<string, Pun>>? PunList { get; set; }

        public HashSet<string> RevealedWords { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public bool IsGenerating { get; set; } = false;

        public WordNetEngine WordNetEngine { get; }

        public PronunciationEngine PronunciationEngine { get; }

        private static IEnumerable<SynSet> GetSynSets(string? theme, WordNetEngine wordNetEngine)
        {
            if (string.IsNullOrWhiteSpace(theme))
                return Enumerable.Empty<SynSet>();

            var sets = wordNetEngine.GetSynSets(theme).ToList();
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
            PunList = null;
            RevealedWords.Clear();
            var synSets = SynSets.Except(CrossedOffSynsets).ToList();

            var task = new Task<IReadOnlyCollection<IGrouping<string, Pun>>?>(()=>GetPuns(synSets, PunCategory, Theme, WordNetEngine, PronunciationEngine));
            task.Start();

            PunList = await task;

            IsGenerating = false;
        }

        private static IReadOnlyCollection<IGrouping<string, Pun>> GetPuns(IReadOnlyCollection<SynSet> synSets,
            PunCategory punCategory,
            string theme,
            WordNetEngine wordNetEngine,
            PronunciationEngine pronunciationEngine)
        {
            var sw = Stopwatch.StartNew();
            Console.WriteLine(@"Getting Puns");

            var puns = PunHelper.GetPuns(punCategory, theme, synSets, wordNetEngine, pronunciationEngine);

            var punList = puns
                .Distinct()
                .GroupBy(x => x.Word, StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(x => x.Count())
                .ToList();

            Console.WriteLine($@"{puns.Count} Puns Got ({sw.Elapsed.ToString()})");

            return punList;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            WordNetEngine.Dispose();
            PronunciationEngine.Dispose();
        }
    }
}
