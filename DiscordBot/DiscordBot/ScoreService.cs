using System.Text.Json;

namespace DiscordBot
{

    public class ScoreService
    {
        private const string FilePath = "score.json";
        private Dictionary<ulong, int> _scores;

        public ScoreService()
        {
            if (File.Exists(FilePath))
            {
                var json = File.ReadAllText(FilePath);
                _scores = JsonSerializer.Deserialize<Dictionary<ulong, int>>(json) ?? new();
            }
            else
            {
                _scores = new Dictionary<ulong, int>();
            }
        }

        public int GetScore(ulong userId)
        {
            return _scores.TryGetValue(userId, out var score) ? score : 0;
        }

        public List<KeyValuePair<ulong, int>> GetTopScores(int count)
        {
            return _scores
                .OrderByDescending(x => x.Value)
                .Take(count)
                .ToList();
        }

        public void AddPoint(ulong userId)
        {
            if (!_scores.ContainsKey(userId))
                _scores[userId] = 0;

            _scores[userId]++;
            Save();
        }

        private void Save()
        {
            var json = JsonSerializer.Serialize(_scores, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FilePath, json);
        }
    }

}
