﻿using System;
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

        public IReadOnlyCollection<IGrouping<string, Pun>>? PunList { get; set; }

        public HashSet<string> RevealedWords { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase); //TODO replace with choice

        public bool IsGenerating { get; set; } = false;

        public WordNetEngine WordNetEngine { get; }

        public PronunciationEngine PronunciationEngine { get; }

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
            PunList = null;
            RevealedWords.Clear();
            var synSets = SynSets.Where(x=>x.Chosen).Select(x=>x.Entity).ToList();

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
                .SelectMany(pun=> pun.PunWords.Select(punWord=> (pun, punWord)))


                .GroupBy(x => x.punWord, x=>x.pun, StringComparer.OrdinalIgnoreCase)
                .Where(x=> x.Count() > 1)
                .OrderByDescending(x => x.Count())
                .ToList();

            Console.WriteLine($@"{puns.Count} Puns Got ({sw.Elapsed})");

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
