namespace DiscordBot
{
    using Discord;
    using Discord.Interactions;
    using System.Collections.Concurrent;

    public class QuizModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly QuestionService _questions;
        private static readonly ConcurrentDictionary<ulong, ActiveQuiz> _activeQuizzes = new();

        private readonly ScoreService _scoreService;

        public QuizModule(QuestionService questions, ScoreService scoreService)
        {
            _questions = questions;
            _scoreService = scoreService;
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

            await RespondAsync(embed: embed, components: new ComponentBuilder()
                .WithButton("A", customId: "odpoved:A", ButtonStyle.Primary)
                .WithButton("B", customId: "odpoved:B", ButtonStyle.Primary)
                .WithButton("C", customId: "odpoved:C", ButtonStyle.Primary)
                .Build());

            var quiz = new ActiveQuiz(question.CorrectLetter);
            _activeQuizzes[Context.Channel.Id] = quiz;

            _ = Task.Run(async () =>
            {
                await Task.Delay(30000); // 30 sekund

                var correct = quiz.CorrectAnswer;
                var correctUsers = quiz.Answers.Where(x => x.Value == correct).Select(x => x.Key).ToList();
                var wrongUsers = quiz.Answers.Where(x => x.Value != correct).Select(x => x.Key).ToList();

                foreach (var userId in correctUsers)
                {
                    _scoreService.AddPoint(userId);
                }

                string summary = $"✅ Správná odpověď: **{correct}**\n";
                if (correctUsers.Any())
                    summary += $"🎉 Správně: {string.Join(", ", correctUsers.Select(id => $"<@{id}>").ToList())}\n";
                if (wrongUsers.Any())
                    summary += $"❌ Špatně: {string.Join(", ", wrongUsers.Select(id => $"<@{id}>").ToList())}\n";
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


        [SlashCommand("skóre", "Ukáže tvoje aktuální body")]
        public async Task Skore()
        {
            int points = _scoreService.GetScore(Context.User.Id);
            if (points == 1)
            {
                await RespondAsync($"🎯 {Context.User.Mention}, máš {points} bod!");

            }
            else if (points >= 2 && points <= 4)
            {
                await RespondAsync($"🎯 {Context.User.Mention}, máš {points} body!");
            }
            else
            {
                await RespondAsync($"🎯 {Context.User.Mention}, máš {points} bodů!");
            }
        }

        [SlashCommand("top3", "Zobrazí 3 nejlepší hráče podle skóre")]
        public async Task Top3()
        {
            var topScores = _scoreService.GetTopScores(3);

            if (topScores.Count == 0)
            {
                await RespondAsync("❌ Zatím nikdo nezískal žádné body.");
                return;
            }

            var description = string.Join("\n", topScores.Select((entry, index) => $"#{index + 1}: <@{entry.Key}> - {entry.Value} bodů"));

            var embed = new EmbedBuilder()
                .WithTitle("🏆 Top 3 hráči")
                .WithDescription(description)
                .WithColor(Color.Gold)
                .Build();

            await RespondAsync(embed: embed);
        }
    }


}