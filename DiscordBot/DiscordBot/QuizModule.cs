namespace DiscordBot
{
    using Discord;
    using Discord.Interactions;
    using System.Collections.Concurrent;

    public class QuizModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly QuestionService _questions;
        private static readonly ConcurrentDictionary<ulong, ActiveQuiz> _activeQuizzes = new();

        public QuizModule(QuestionService questions)
        {
            _questions = questions;
        }

        [SlashCommand("kvíz", "Zobrazí náhodnou otázku s tlačítky pro všechny hráče")]
        public async Task Quiz()
        {
            if (_activeQuizzes.ContainsKey(Context.Channel.Id))
            {
                await RespondAsync("❗ V tomto kanálu už běží otázka.", ephemeral: true);
                return;
            }

            var question = _questions.GetRandomQuestion();

            var embed = new EmbedBuilder()
                .WithTitle("🎯 Kvízová otázka")
                .WithDescription($"{question.Question}\n\n" +
                                 $"A) {question.Options[0]}\n" +
                                 $"B) {question.Options[1]}\n" +
                                 $"C) {question.Options[2]}\n\nKlikni na odpověď. Máš 30 sekund!")
                .WithColor(Color.Blue)
                .Build();

            var components = new ComponentBuilder()
                .WithButton("A", customId: "odpoved:A", ButtonStyle.Primary)
                .WithButton("B", customId: "odpoved:B", ButtonStyle.Primary)
                .WithButton("C", customId: "odpoved:C", ButtonStyle.Primary);

            await RespondAsync(embed: embed, components: components.Build());
            var original = await GetOriginalResponseAsync();

            var quiz = new ActiveQuiz(question.CorrectLetter);
            _activeQuizzes[Context.Channel.Id] = quiz;

            _ = Task.Run(async () =>
            {
                await Task.Delay(30000); // 30 sekund

                var correct = quiz.CorrectAnswer;
                var correctUsers = quiz.Answers.Where(x => x.Value == correct).Select(x => $"<@{x.Key}>").ToList();
                var wrongUsers = quiz.Answers.Where(x => x.Value != correct).Select(x => $"<@{x.Key}>").ToList();

                string summary = $"✅ Správná odpověď: **{correct}**\n";
                if (correctUsers.Any())
                    summary += $"🎉 Správně: {string.Join(", ", correctUsers)}\n";
                if (wrongUsers.Any())
                    summary += $"❌ Špatně: {string.Join(", ", wrongUsers)}\n";
                if (!quiz.Answers.Any())
                    summary += "😢 Nikdo neodpověděl.";

                await Context.Channel.SendMessageAsync(summary);
                _activeQuizzes.TryRemove(Context.Channel.Id, out _);
            });
        }

        [ComponentInteraction("odpoved:*")]
        public async Task HandleAnswer(string odpoved)
        {
            if (!_activeQuizzes.TryGetValue(Context.Channel.Id, out var quiz))
            {
                await RespondAsync("❌ Žádná aktivní otázka.", ephemeral: true);
                return;
            }

            if (quiz.Answers.ContainsKey(Context.User.Id))
            {
                await RespondAsync("🕓 Už jsi odpověděl/a.", ephemeral: true);
                return;
            }

            quiz.Answers[Context.User.Id] = odpoved;
            await RespondAsync($"Zaznamenáno: {odpoved}", ephemeral: true);
        }

        [SlashCommand("přidej-kvíz", "Přidej vlastní otázku do kvízu")]
        public async Task AddQuiz(
            [Summary("otázka", "Text otázky")] string otazka,
            [Summary("a", "Možnost A")] string a,
            [Summary("b", "Možnost B")] string b,
            [Summary("c", "Možnost C")] string c,
            [Summary("spravna", "Správná možnost (A/B/C)")] string spravna)
        {
            if (!new[] { "A", "B", "C" }.Contains(spravna.ToUpper()))
            {
                await RespondAsync("❌ Správná odpověď musí být A, B nebo C.");
                return;
            }

            int index = spravna.ToUpper() switch
            {
                "A" => 0,
                "B" => 1,
                "C" => 2,
                _ => -1
            };

            _questions.AddQuestion(new TriviaQuestion
            {
                Question = otazka,
                Options = new[] { a, b, c },
                CorrectIndex = index
            });

            await RespondAsync("✅ Otázka úspěšně přidána!");
        }
    }

}
