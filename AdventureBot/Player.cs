namespace AdventureBot
{
    public class Player
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public bool IsMonster { get; set; }
        public bool IsPlayer { get; set; }
        public bool IsNPC { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }
        public int Health { get; set; }
        public int Strength { get; set; }
        public int Magic { get; set; }
        public int Luck { get; set; }
        public int Weapon { get; set; }
        public int[] Wearing { get; set; }
        public int[] Objects { get; set; }
        public bool IsActive { get; set; }
        public long LastStatusId { get; set; }
    }
}