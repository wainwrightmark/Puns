using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Pronunciation;
using WordNet;


namespace Puns.Blazor.Pages
{
    public class PunState
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public PunState()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            WordNetEngine = new Lazy<WordNetEngine>(()=> new WordNetEngine());

            PronunciationEngine = new Lazy<PronunciationEngine>(()=> new PronunciationEngine());

            var tasks = new List<Task>()
            {
                Task.Factory.StartNew(()=>WordNetEngine.Value),
                Task.Factory.StartNew(() => PronunciationEngine.Value)
            };

            _ = Task.WhenAll(tasks).ContinueWith(x =>
              {
                  EnginesLoaded = true;
                  PageLoaded?.Invoke(null, EventArgs.Empty);
              });


        }


        public PunCategory PunCategory { get; set; } = PunCategory.Idiom;

        public string? Theme { get; set; } = "Fish";

        public IEnumerable<SynSet> SynSets => GetSynSets(Theme, WordNetEngine.Value);


        public HashSet<SynSet> CrossedOffSynsets { get; } = new HashSet<SynSet>();

        public event EventHandler? PageLoaded;

        public IReadOnlyCollection<IGrouping<string, Pun>>? PunList { get; set; }

        public HashSet<string> RevealedWords { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public bool IsGenerating { get; set; } = false;

        public bool EnginesLoaded { get; private set; } = false;

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
