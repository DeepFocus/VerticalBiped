namespace JumpFocus.Models
{
    class LeaderScoreItem
    {
        public int Id { get; set; }
        public int Rank { get; set; }
        public string RankSuperscript { get; set; }
        public string Name { get; set; }
        public int Score { get; set; }
        public string BackgroundColor { get; set; }

        public LeaderScoreItem()
        {
            BackgroundColor = "#FF343E4E";
        }
    }
}