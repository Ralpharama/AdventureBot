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
        public int Level { get; set; }
        public long AccountId { get; set; }
    }

    // todo: implement objects
    public class Item
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string PlayerUsername { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }
        public int Health { get; set; }
        public int Strength { get; set; }
        public int Magic { get; set; }
        public int Luck { get; set; }
        public bool Weapon { get; set; }
        public bool Wearable { get; set; }
        public int Level { get; set; }
    }
}