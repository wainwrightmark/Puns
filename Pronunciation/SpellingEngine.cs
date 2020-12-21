using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FileDatabase;

namespace Pronunciation
{
    public sealed class SpellingEngine : IDisposable
    {
        public IEnumerable<Spelling> GetAllSpellings() => _database.GetAll();


        public SpellingEngine() =>
            _database = new Database<Spelling, (string, bool)>(
                PhoeneticsFiles.Spelling, Encoding.UTF8, x => (x.Syllable.ToString(), true), CreateFromLine);


        public Spelling? GetSpelling(Syllable syllable)
        {
            var key = (syllable.ToString(), true);
            var r = _database[key];
            return r;
        }

        public static Spelling CreateFromLine(string s)
        {
            var tabIndex = s.IndexOf('\t');
            var syllableText = s.Substring(0, tabIndex);
            var text = s.Substring(tabIndex + 1);

            var symbols = syllableText.Split(' ')
                .Select(x=> Enum.TryParse(x, out Symbol symbol)? symbol :
                    throw new Exception($"Could not parse Symbol '{symbol}'")).ToList();

            var syllable = new Syllable(symbols);
            var spelling = new Spelling(syllable, text);

            return spelling;
        }

        private readonly Database<Spelling, (string, bool)> _database;

        /// <inheritdoc />
        public void Dispose()
        {
            _database.Dispose();
        }
    }

    public record Spelling(Syllable Syllable, string Text);
}