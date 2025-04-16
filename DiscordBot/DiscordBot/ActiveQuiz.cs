using System.Collections.Concurrent;

namespace DiscordBot
{
    public class ActiveQuiz
    {
        public string CorrectAnswer { get; }
        public ConcurrentDictionary<ulong, string> Answers { get; } = new();

        public ActiveQuiz(string correct)
        {
            CorrectAnswer = correct;
        }
    }
}
