﻿using System.Collections.Generic;
using System.Linq;
using Pronunciation;

namespace Puns
{
    /// <summary>
    /// The theme word rhymes with original word
    /// </summary>
    public class PerfectRhymePunStrategy : PunStrategy
    {
        public PerfectRhymePunStrategy(IEnumerable<PhoneticsWord> themeWords) : base(themeWords) { }

        /// <inheritdoc />
        public override IEnumerable<PhoneticsWord> GetThemeWordSubwords(PhoneticsWord word)
        {
            var lastStressedVowelIndex = word.Syllables.LastIndexOf(x=>x.Nucleus.IsStressedVowel());

            if (lastStressedVowelIndex < 0) yield break; //No stressed vowel


            var syllables = word.Syllables.Skip(lastStressedVowelIndex).Select((x,i)=> i == 0? x.GetRhymeSyllable : x).ToList();
            var subWord = new PhoneticsWord(string.Join("", syllables), 0, true, syllables);

            yield return subWord;
        }


        public override IEnumerable<PunReplacement> GetPossibleReplacements(PhoneticsWord originalWord)
        {
            var lastStressedVowel = GetThemeWordSubwords(originalWord).FirstOrDefault();

            if(lastStressedVowel is null) yield break;


            foreach (var themeWord in ThemeWordLookup[lastStressedVowel])
            {
                var insert = false;

                if (themeWord.Text.Length < originalWord.Text.Length)
                {
                    var originalWordStressedVowels = originalWord.Syllables.Count(x => x.Nucleus.IsStressedVowel());
                    if (originalWordStressedVowels > 1)
                    {
                        var themeWordStressedVowels = themeWord.Syllables.Count(x => x.Nucleus.IsStressedVowel());
                        if (originalWordStressedVowels > themeWordStressedVowels)
                            insert = true;
                    }
                }

                string replacement = insert
                    ? originalWord.Text.Substring(0, originalWord.Text.Length - themeWord.Text.Length) + themeWord.Text//TODO improve this replacement
                    : themeWord.Text;

                yield return new PunReplacement(PunType.PerfectRhyme, replacement, insert, themeWord.Text);
            }
        }


    }
}