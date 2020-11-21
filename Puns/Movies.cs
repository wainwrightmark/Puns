using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp;

namespace Puns
{
    public class Movies
    {

        public static async Task<IReadOnlyCollection<string>> GetTopMovies(int num)
        {
            const int pageSize = 50;

            var movies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (var i = 1; i <= num; i+= pageSize)
            {
                var ms = await GetMovies(i);
                movies.UnionWith(ms);
            }


            return movies;
        }

        private static  async Task<IReadOnlyCollection<string>> GetMovies(int start)
        {
            var config = Configuration.Default.WithDefaultLoader();
            var address = $"https://www.imdb.com/search/title/?groups=top_1000&sort=user_rating,desc&view=simple&start={start}";
            var context = BrowsingContext.New(config);
            var document = await context.OpenAsync(address);
            var cellSelector = "img.loadlate";
            var cells = document.QuerySelectorAll(cellSelector);
            var titles = cells.SelectMany(m => m.Attributes.Where(x => x.Name == "alt").Select(x => x.Value));

            return titles.Distinct().ToList();
        }
    }
}
