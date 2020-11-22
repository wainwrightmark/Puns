using System;
using System.Collections.Generic;
using System.Linq;
using Pronunciation;
using WordNet;


namespace Puns.Blazor.Pages
{
    public class PunState
    {
        public PunState()
        {
            WordNetEngine = new Lazy<WordNetEngine>(()=> new WordNetEngine());

            PronunciationEngine = new Lazy<PronunciationEngine>(()=> new PronunciationEngine());
        }


        public PunCategory PunCategory { get; set; } = PunCategory.Idiom;

        public string? Theme { get; set; } = "Fish";

        public IEnumerable<SynSet> SynSets => GetSynSets(Theme, WordNetEngine.Value);


        public HashSet<SynSet> CrossedOffSynsets { get; } = new HashSet<SynSet>();


        public IReadOnlyCollection<IGrouping<string, Pun>>? PunList { get; set; }

        public HashSet<string> RevealedWords { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public bool IsGenerating { get; set; } = false;

        public Lazy<WordNetEngine> WordNetEngine { get; }

        public Lazy<PronunciationEngine> PronunciationEngine { get; }

        private static IEnumerable<SynSet> GetSynSets(string? theme, WordNetEngine wordNetEngine)
        {
            if (string.IsNullOrWhiteSpace(theme))
                return Enumerable.Empty<SynSet>();

            var sets = wordNetEngine.GetSynSets(theme).ToList();
            return sets;
        }


        public void Find()
        {
            if (string.IsNullOrWhiteSpace(Theme))
            {
                IsGenerating = false;
                return;
            }

            IsGenerating = true;

            PunList = null;
            RevealedWords.Clear();

            Console.WriteLine(@"Getting Puns");

            var puns = SynSets.Except(CrossedOffSynsets)
                .SelectMany(synSet => PunHelper.GetPuns(PunCategory, Theme, synSet, WordNetEngine.Value, PronunciationEngine.Value));

            PunList = puns
                .Distinct()
                .GroupBy(x => x.Word, StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(x => x.Count())
                .ToList();

            Console.WriteLine($@"{PunList.Count} Puns Got");

            IsGenerating = false;
        }

    }
}
