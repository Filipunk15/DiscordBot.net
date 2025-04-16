namespace DiscordBot
{
    using System.Text.Json;

    public class QuestionService
    {
        private const string FilePath = "questions.json";
        private List<TriviaQuestion> _questions;

        public QuestionService()
        {
            if (File.Exists(FilePath))
            {
                var json = File.ReadAllText(FilePath);
                _questions = JsonSerializer.Deserialize<List<TriviaQuestion>>(json) ?? new();
            }
            else
            {
                _questions = new List<TriviaQuestion>();
            }
        }

        public TriviaQuestion GetRandomQuestion()
        {
            if (_questions.Count == 0)
            {
                return new TriviaQuestion
                {
                    Question = "Zatím žádné otázky!",
                    Options = new[] { "-", "-", "-" },
                    CorrectIndex = 0
                };
            }

            var rnd = new Random();
            return _questions[rnd.Next(_questions.Count)];
        }

        public void AddQuestion(TriviaQuestion question)
        {
            _questions.Add(question);
            Save();
        }

        private void Save()
        {
            var json = JsonSerializer.Serialize(_questions, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FilePath, json);
        }
    }

}
