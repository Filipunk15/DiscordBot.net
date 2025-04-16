namespace DiscordBot
{
    using Discord;
    using Discord.Interactions;

    public class QuizModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly QuestionService _questions;

        public QuizModule(QuestionService questions)
        {
            _questions = questions;
        }

        [SlashCommand("kvíz", "Zobrazí náhodnou otázku s tlačítky")]
        public async Task Quiz()
        {
            var question = _questions.GetRandomQuestion();

            var embed = new EmbedBuilder()
                .WithTitle("🎯 Kvízová otázka")
                .WithDescription($"{question.Question}\n\n" +
                                 $"A) {question.Options[0]}\n" +
                                 $"B) {question.Options[1]}\n" +
                                 $"C) {question.Options[2]}")
                .WithColor(Color.Orange)
                .Build();

            var components = new ComponentBuilder()
                .WithButton("A", customId: $"odpoved:A|{question.CorrectLetter}", ButtonStyle.Primary)
                .WithButton("B", customId: $"odpoved:B|{question.CorrectLetter}", ButtonStyle.Primary)
                .WithButton("C", customId: $"odpoved:C|{question.CorrectLetter}", ButtonStyle.Primary);

            await RespondAsync(embed: embed, components: components.Build());
        }

        [ComponentInteraction("odpoved:*")]
        public async Task HandleAnswer(string data)
        {
            var parts = data.Split('|');
            var userAnswer = parts[0];
            var correctAnswer = parts[1];

            string response = userAnswer == correctAnswer
                ? $"✅ Správně, {Context.User.Mention}!"
                : $"❌ Špatně, {Context.User.Mention}! Správná odpověď byla **{correctAnswer}**.";

            await RespondAsync(response, ephemeral: true); // jen pro toho, kdo kliknul
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
