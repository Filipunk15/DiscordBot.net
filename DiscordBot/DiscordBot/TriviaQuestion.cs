namespace DiscordBot
{
    public class TriviaQuestion
    {
        public string Question { get; set; }
        public string[] Options { get; set; }
        public int CorrectIndex { get; set; }

        public string CorrectLetter => CorrectIndex switch
        {
            0 => "A",
            1 => "B",
            2 => "C",
            _ => "?"
        };
    }

}
