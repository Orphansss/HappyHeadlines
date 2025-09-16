using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using ProfanityService.Data;

namespace ProfanityService.Services
{
    public interface IProfanityFilter
    {
        Task<(bool isClean, string cleaned, IEnumerable<string> hits)> FilterAsync(string text);
    }

    public class ProfanityFilter : IProfanityFilter
    {
        private readonly ProfanityDbContext _db;
        public ProfanityFilter(ProfanityDbContext db) => _db = db;

        public async Task<(bool, string, IEnumerable<string>)> FilterAsync(string text)
        {
            var words = await _db.Words.Select(w => w.Value).ToListAsync();
            if (!words.Any()) return (true, text, Array.Empty<string>());

            var hits = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string cleaned = text;

            foreach (var w in words)
            {
                var pattern = $@"\b{Regex.Escape(w)}\b";
                if (Regex.IsMatch(cleaned, pattern, RegexOptions.IgnoreCase))
                {
                    hits.Add(w);
                    cleaned = Regex.Replace(cleaned, pattern, new string('*', w.Length), RegexOptions.IgnoreCase);
                }
            }
            return (hits.Count == 0, cleaned, hits);
        }
    }
}
