﻿using System;
using System.Collections.Generic;
using System.Linq;
using Pronunciation;

namespace Puns.Strategies
{
    public abstract class PunStrategy
    {
        protected PunStrategy(IEnumerable<PhoneticsWord> themeWords)
        {
            ThemeWordLookup = themeWords.SelectMany(word =>
                    GetThemeWordSubwords(word).Select(cluster=> (word, cluster)))
                .OrderBy(x=>x.word.Text.Length)
                .ToLookup(x => x.cluster, x => x.word, WordPronunciationComparer.Instance);
        }

        public ILookup<PhoneticsWord, PhoneticsWord> ThemeWordLookup { get; }

        public abstract IEnumerable<PhoneticsWord> GetThemeWordSubwords(PhoneticsWord word);

        public abstract IEnumerable<PunReplacement> GetPossibleReplacements(PhoneticsWord originalWord);

        protected string CreateSpelling(IEnumerable<Syllable> syllables)
        {
            //TODO do better
            return new string(syllables.SelectMany(x => x.ToString()).Where(char.IsLetter).ToArray());

        }
    }
}