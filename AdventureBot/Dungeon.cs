using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LiteDB;

namespace AdventureBot
{
    public class Dungeon
    {
        private Room[,,] _rooms;
        private Language _language;
        private Player _player;
        private Random _rnd;
        private LiteDatabase _db;
        public int XSize { get; set; }
        public int YSize { get; set; }
        public int ZSize { get; set; }

        public Dungeon(LiteDatabase db)
        {
            _rooms = new Room[Program.XSize, Program.YSize, Program.ZSize];
            _language = new Language();
            _player = new Player();
            _rnd = new Random();
            _db = db;
        }

        // --- PROCESSING LOGIC ---
        #region ProcessingLogic

        // Process this player's input, note upsert is done in main program after all processing
        public TootToSend Process(Player p, long statusId, string content)
        {
            TootToSend toReturn = new TootToSend();
            toReturn.AccountId = 0;    // We have this on calling func 
            toReturn.Content = "";
            toReturn.Username = p.Username;
            toReturn.Privacy = "direct";    // Not visible to anyone else

            // Update with last status update id to prevent re-doing same command
            if (!Program.Debug)
            {
                p.LastStatusId = statusId;  // Only update this if we're not debugging
            }

            // parse string
            content = Program.TagRegex.Replace(content.Replace("<br />", "\n"), "").Trim();
            //var words = content.Trim().Split(' ');
            var contentScan = "|"+content.Replace(" ", "| |")+"|";
            contentScan = contentScan.Replace("?", "");
            contentScan = contentScan.Replace("!", "").ToLower();
            var punctuation = contentScan.Where(Char.IsPunctuation).Distinct().ToArray();
            var words = contentScan.Split().Select(x => x.Trim(punctuation));

            var showRoomDetails = true;

            // Directions
            if (words.Any("|north|".Contains))
            {
                if (_rooms[p.X, p.Y, p.Z].ExitNorth == (int) ExitStates.Open)
                {
                    p.Y--;
                    toReturn.Content += "You exit north. \r\n";
                }
                else
                {
                    toReturn.Content += "You can't go that way.\r\n";
                }
            }
            if (words.Any("|east|".Contains))
            {
                if (_rooms[p.X, p.Y, p.Z].ExitEast == (int)ExitStates.Open)
                {
                    p.X++;
                    toReturn.Content += "You exit east. \r\n";
                }
                else
                {
                    toReturn.Content += "You can't go that way.\r\n";
                }
            }
            if (words.Any("|south|".Contains))
            {
                if (_rooms[p.X, p.Y, p.Z].ExitSouth == (int)ExitStates.Open)
                {
                    p.Y++;
                    toReturn.Content += "You exit south. \r\n";
                }
                else
                {
                    toReturn.Content += "You can't go that way.\r\n";
                }
            }
            if (words.Any("|west|".Contains))
            {
                if (_rooms[p.X, p.Y, p.Z].ExitWest == (int)ExitStates.Open)
                {
                    p.X--;
                    toReturn.Content += "You exit west. \r\n";
                }
                else
                {
                    toReturn.Content += "You can't go that way.\r\n";
                }
            }
            if (words.Any("|up|".Contains))
            {
                if (_rooms[p.X, p.Y, p.Z].ExitUp == (int)ExitStates.Open)
                {
                    p.Z--;
                    toReturn.Content += "You ascend to safer places in the dungeon - you go up to level " + p.Z + "... \r\n";
                }
                else
                {
                    toReturn.Content += "You can't go that way.\r\n";
                }
            }
            if (words.Any("|down|".Contains))
            {
                if (_rooms[p.X, p.Y, p.Z].ExitDown == (int)ExitStates.Open)
                {
                    p.Z++;
                    toReturn.Content += "You descend deeper into the dungeon - you go down to level "+p.Z+"... \r\n";
                }
                else
                {
                    toReturn.Content += "You can't go that way.\r\n";
                }
            }

            // Look
            if (words.Any("|look|".Contains))
            {
                toReturn.Content += "You look around...\r\n";
            }

            // Help
            if (words.Any("|score|".Contains) || words.Any("|status|".Contains))
            {
                toReturn.Content += "==Status==\r\n";
                toReturn.Content += "Name: "+p.Username+"\r\n";
                toReturn.Content += "Location co-ords (xyz): " + p.X + p.Y + p.Z + "\r\n";
                toReturn.Content += "Level: " + p.Level + "\r\n";
                toReturn.Content += "Health: " + p.Health + "\r\n";
                toReturn.Content += "Strengh: " + p.Strength + "\r\n";
                toReturn.Content += "Magic: " + p.Magic + "\r\n";
                toReturn.Content += "Luck: " + p.Luck + "\r\n";
                toReturn.Content += "Current weapons:(to do)\r\n";
                toReturn.Content += "Wearing:(to do)\r\n";
                return toReturn;
            }

            // Help
            if (words.Any("|help|".Contains))
            {
                toReturn.Content += "Commands so far are north, east, south, west, up, down, look, status and help.\r\n";
                return toReturn;
            }

            if (toReturn.Content == "")
            {
                // Don't understand
                toReturn.Content =
                    "I'm sorry, I didn't understand that. Try using simple words, type 'help' for a full list of commands I understand.\r\n";
            }
            else
            {
                // Get info about room to append to this at end of every toot unless we say not to above
                if (showRoomDetails)
                {
                    toReturn.Content += ExamineRoomExtras(p);
                }

            }

            return toReturn;
        }
        #endregion

        public string ExamineRoomExtras(Player p)
        {
            string toReturn = "";

            toReturn += "\r\nYou are in " + GetRoomName(p.X, p.Y, p.Z) +
                        "\r\n" + GetRoomExits(p.X, p.Y, p.Z);

            // Get players in room with you
            var playersInRoom = GetPlayersInRoom(p.X, p.Y, p.Z);
            if (playersInRoom != null)
            {
                string playersAdd = "";
                foreach (var player in playersInRoom)
                {
                    if (player.Username != p.Username)
                    {
                        playersAdd += "@" + player.Username + ", ";
                    }
                }
                if (playersAdd != "")
                {
                    toReturn += "\r\nAlso here is " + playersAdd;
                }
                toReturn = toReturn.TrimEnd(' '); toReturn = toReturn.TrimEnd(',');
            }

            // Monsters in room with you
            var monstersEnumerable = GetMonstersInRoom(p.X, p.Y, p.Z);
            if (monstersEnumerable != null)
            {
                string monstersAdd = "";
                foreach (var monster in monstersEnumerable)
                {
                    monstersAdd += monster.Username + ", ";
                }
                if (monstersAdd != "")
                {
                    toReturn += "\r\nThere are monsters here: " + monstersAdd;
                }
                toReturn = toReturn.TrimEnd(' '); toReturn = toReturn.TrimEnd(',');
            }

            // Items in room with you
            var itemsEnumerable = GetItemsInRoom(p.X, p.Y, p.Z);
            if (itemsEnumerable != null)
            {
                string itemsAdd = "";
                foreach (var item in itemsEnumerable)
                {
                    itemsAdd += item.Name + ", ";
                }
                if (itemsAdd != "")
                {
                    toReturn += "\r\nThere are items here: " + itemsAdd;
                }
                toReturn = toReturn.TrimEnd(' '); toReturn = toReturn.TrimEnd(',');
            }

            return toReturn;
        }


        // Load dungeon into memory
        public void LoadDungeon()
        {
            // Get collection
            var dbRooms = _db.GetCollection<Room>("rooms");

            Console.WriteLine("Loading rooms from db...");
            for (var z = 0; z < Program.ZSize; z++)
            {
                for (var x = 0; x < Program.XSize; x++)
                {
                    for (var y = 0; y < Program.YSize; y++)
                    {
                            _rooms[x,y,z] = dbRooms.FindOne(r => r.X == x && r.Y==y && r.Z==z);
                    }
                }
            }
            Console.WriteLine("Complete...");
            //var rm = dbRooms.FindOne(x => x.X == 0 );
            //Console.WriteLine("Room: "+rm.Title+" ["+rm.Id+"]");
        }

        // --- PLAYERS ---
        #region PlayersLogic

        // Load player into memory
        public Player LoadPlayer(string username)
        {
            // Get collection
            var dbPlayers = _db.GetCollection<Player>("players");
            dbPlayers.EnsureIndex(x => x.Username);
            Player player = dbPlayers.FindOne(r => r.Username == username);
            return player;
        }
        public Player LoadPlayer(int x, int y, int z)
        {
            // Get collection
            var dbPlayers = _db.GetCollection<Player>("players");
            dbPlayers.EnsureIndex(s => s.X);
            dbPlayers.EnsureIndex(s => s.Y);
            dbPlayers.EnsureIndex(s => s.Z);
            Player player = dbPlayers.FindOne(s => s.X==x && s.Y == y && s.Z == z);
            return player;
        }

        // Add player
        public Player AddPlayer(string username, long accountid)
        {
            // Get collection
            var dbPlayers = _db.GetCollection<Player>("players");
            dbPlayers.EnsureIndex(x => x.Username);
            var p = new Player
            {
                Username = username,
                X = 0, Y=0, Z=0,
                IsActive = true,
                Health = 100,
                IsMonster = false,
                IsNPC = false,
                Luck = 5,
                Magic = 5,
                Strength = 5,
                LastStatusId = 0,
                Level = 1,
                AccountId = accountid
            };
            MovePlayerRnd(p, 0);
            dbPlayers.Upsert(p);
            return p;
        }

        // Remove player
        public void DeletePlayer(string username)
        {
            // Get collection
            var dbPlayers = _db.GetCollection<Player>("players");
            dbPlayers.EnsureIndex(x => x.Username);
            dbPlayers.Delete(x => x.Username == username);
        }

        public void UpsertPlayer(Player p)
        {
            var dbPlayers = _db.GetCollection<Player>("players");
            dbPlayers.EnsureIndex(x => x.Username);
            dbPlayers.Upsert(p);
        }

        #endregion


        // --- THINGS ABOUT ROOMS Moving, finding things in etc ---
        #region RoomStuff

        // Move player to random room
        public void MovePlayerRnd(Player p, int level)
        {
            p.X = _rnd.Next(Program.XSize);
            p.Y = _rnd.Next(Program.YSize);
            p.Z = level;
        }

        public IEnumerable<Player> GetPlayersInRoom(int x, int y, int z)
        {
            var players = _db.GetCollection<Player>("players");
            IEnumerable<Player> toReturn =  players.Find(r => r.X == x && r.Y == y && r.Z == z);
            return toReturn;
        }

        public IEnumerable<Player> GetMonstersInRoom(int x, int y, int z)
        {
            var players = _db.GetCollection<Player>("monsters");
            IEnumerable<Player> toReturn = players.Find(r => r.X == x && r.Y == y && r.Z == z);
            return toReturn;
        }
        public IEnumerable<Item> GetItemsInRoom(int x, int y, int z)
        {
            var items = _db.GetCollection<Item>("items");
            IEnumerable<Item> toReturn = items.Find(r => r.X == x && r.Y == y && r.Z == z);
            return toReturn;
        }

        public string GetRoomName(int x, int y, int z)
        {
            return _rooms[x, y, z].Title;
            //+ " {" + x + "," + y + "," + z + "}";
        }

        public string GetRoomExits(int x, int y, int z)
        {
            var toReturn = "";
            if (_rooms[x, y, z].ExitNorth < 2)
            {
                toReturn += "north, ";
            }
            if (_rooms[x, y, z].ExitEast < 2)
            {
                toReturn += "east, ";
            }
            if (_rooms[x, y, z].ExitSouth < 2)
            {
                toReturn += "south, ";
            }
            if (_rooms[x, y, z].ExitWest < 2)
            {
                toReturn += "west, ";
            }
            if (_rooms[x, y, z].ExitUp < 2)
            {
                toReturn += "up, ";
            }
            if (_rooms[x, y, z].ExitDown < 2)
            {
                toReturn += "down, ";
            }
            if (toReturn != "")
            {
                toReturn = "There are exits to the " + toReturn;
            }
            toReturn = toReturn.TrimEnd(' '); toReturn = toReturn.TrimEnd(',');
            return toReturn;
        }

        #endregion

        // Process all monsters, so they 
        // attack if there is a player here, in random order
        // move if random, in random direction, not up or down except very rarely!
        // todo: handle combat, deaths, etc
        #region MonsterStuff

        public IEnumerable<TootToSend> ProcessMonsters()
        {
            List<TootToSend> toReturn = new List<TootToSend>();

            var dbMonsters = _db.GetCollection<Player>("monsters");
            dbMonsters.EnsureIndex(x => x.Username);

            var monsters = dbMonsters.Find(r => r.IsActive && r.IsMonster);
            foreach (var monster in monsters)
            {
                TootToSend thisToot = new TootToSend();

                // Is there a player in this room?
                var p = LoadPlayer(monster.X, monster.Y, monster.Z);
                if (p != null)
                {
                    thisToot.Username = p.Username;
                    thisToot.AccountId = p.AccountId;
                    thisToot.Privacy = "direct";
                    thisToot.Content = monster.Username + " looks at you nastily.";
                    toReturn.Add(thisToot);
                }
                // If not, let's maybe move
                else
                {
                    if (_rnd.Next(10) < 5)
                    {
                        var ch = _rnd.Next(6);
                        switch (ch)
                        {
                            case 0:
                                if (_rooms[monster.X, monster.Y, monster.Z].ExitNorth < 3)
                                {
                                    monster.Y--;
                                    UpsertMonster(monster);
                                    Console.WriteLine("--Monster " + monster.Username + " moved to " + monster.X + "," + monster.Y + "," + monster.Z);
                                }
                                break;
                            case 1:
                                if (_rooms[monster.X, monster.Y, monster.Z].ExitEast < 3)
                                {
                                    monster.X++;
                                    UpsertMonster(monster);
                                    Console.WriteLine("--Monster " + monster.Username + " moved to " + monster.X + "," + monster.Y + "," + monster.Z);
                                }
                                break;
                            case 2:
                                if (_rooms[monster.X, monster.Y, monster.Z].ExitSouth < 3)
                                {
                                    monster.Y++;
                                    UpsertMonster(monster);
                                    Console.WriteLine("--Monster " + monster.Username + " moved to " + monster.X + "," + monster.Y + "," + monster.Z);
                                }
                                break;
                            case 3:
                                if (_rooms[monster.X, monster.Y, monster.Z].ExitWest < 3)
                                {
                                    monster.X--;
                                    UpsertMonster(monster);
                                    Console.WriteLine("--Monster " + monster.Username + " moved to " + monster.X + "," + monster.Y + "," + monster.Z);
                                }
                                break;
                            case 4:
                                if (_rooms[monster.X, monster.Y, monster.Z].ExitDown < 3 && _rnd.Next(20)==1)
                                {
                                    monster.Z++;
                                    UpsertMonster(monster);
                                    Console.WriteLine("--Monster " + monster.Username + " moved to " + monster.X + "," + monster.Y + "," + monster.Z);
                                }
                                break;
                            case 5:
                                if (_rooms[monster.X, monster.Y, monster.Z].ExitUp < 3 && _rnd.Next(20) == 1)
                                {
                                    monster.Z--;
                                    UpsertMonster(monster);
                                    Console.WriteLine("--Monster "+monster.Username+" moved to "+monster.X+"," + monster.Y + "," + monster.Z);
                                }
                                break;
                        }
                    }

                    // After move, is there a player here? If so, toot him the good news!
                    var q = LoadPlayer(monster.X, monster.Y, monster.Z);
                    if (q != null)
                    {
                        thisToot.Username = q.Username;
                        thisToot.AccountId = q.AccountId;
                        thisToot.Privacy = "direct";
                        thisToot.Content = monster.Username + " has entered the room.";
                        toReturn.Add(thisToot);
                    }

                }
            }

            return toReturn;
        }

        public Player CreateNewMonster(int x, int y, int z)
        {
            return new Player
            {
                IsMonster = true,
                IsPlayer = false,
                IsNPC = false,
                X = x,
                Y = y,
                Z = z,
                Username = _language.GetARandomMonsterName(),
                Health = (_rnd.Next(10) * z) + 1,
                Magic = (_rnd.Next(10) * z) + 1,
                Luck = (_rnd.Next(10) * z) + 1,
                Strength = (_rnd.Next(10) * z) + 1,
                Level = (_rnd.Next(3) * z) + 1,
                Weapon = 0,
                IsActive = true,
                LastStatusId = 0,
            };
        }

        public void UpsertMonster(Player p)
        {
            var dbMonsters = _db.GetCollection<Player>("monsters");
            dbMonsters.EnsureIndex(x => x.Username);
            dbMonsters.Upsert(p);
        }

        #endregion


        // --- ITEMS ---

        #region Items

        // Create item in either x,y,z or in posession of user
        // typ = 0 = weapon, 1 = wearable
        public Item CreateNewItem(int typ, int x, int y, int z, string playerUsername)
        {
            return new Item
            {
                Name = _language.GetARandomItemName(typ),
                PlayerUsername = playerUsername,
                X = (playerUsername=="") ? x : 0,
                Y = (playerUsername == "") ? y : 0,
                Z = (playerUsername == "") ? z : 0,
                Health = (_rnd.Next(3) * z) + 1,
                Strength = (_rnd.Next(3) * z) + 1,
                Magic = (_rnd.Next(3) * z) + 1,
                Luck = (_rnd.Next(3) * z) + 1,
                Weapon = (typ == 0),
                Wearable = (typ == 1),
                Level = (_rnd.Next(3) * z) + 1
            };
        }

        #endregion




        // --- DESTRICTIVE AND MAKE NEW/WIPE stuff ---
        #region MakeDestroyEtc

        // Clear a room of its info etc, used in making new rooms
        public void ClearRoom(int x, int y, int z, string title)
        {
            _rooms[x, y, z].X = x;
            _rooms[x, y, z].Y = y;
            _rooms[x, y, z].Z = z;
            _rooms[x, y, z].Title = title;
            _rooms[x, y, z].ExitNorth = (int)ExitStates.NoExit;
            _rooms[x, y, z].ExitEast = (int)ExitStates.NoExit;
            _rooms[x, y, z].ExitSouth = (int)ExitStates.NoExit;
            _rooms[x, y, z].ExitWest = (int)ExitStates.NoExit;
            _rooms[x, y, z].ExitUp = (int)ExitStates.NoExit;
            _rooms[x, y, z].ExitDown = (int)ExitStates.NoExit;
        }

        // Wipe and recreate monsters
        public void CreateMonsters(int numPerLevel)
        {
            // Delete all currently in db
            var dbMonsters = _db.GetCollection<Player>("monsters");
            dbMonsters.EnsureIndex(x => x.Username);
            dbMonsters.Delete(x => x.Username != null);
            for (int z = 0; z < Program.ZSize; z++)
            {
                for (int x = 0; x < numPerLevel; x++)
                {
                    Player m = CreateNewMonster(_rnd.Next(Program.XSize), _rnd.Next(Program.YSize), z);
                    Console.WriteLine("New Monster:");
                    Console.WriteLine("Name: " + m.Username + "");
                    Console.WriteLine("Location co-ords: " + m.X + m.Y + m.Z + "");
                    Console.WriteLine("Level: " + m.Level + "");
                    Console.WriteLine("Health: " + m.Health + "");
                    Console.WriteLine("Strengh: " + m.Strength + "");
                    Console.WriteLine("Magic: " + m.Magic + "");
                    Console.WriteLine("Luck: " + m.Luck + "");
                    Console.WriteLine("Current weapon: " + m.Weapon + "");
                    dbMonsters.Upsert(m);
                }
            }
        }

        // Wipe and recreate items
        public void CreateItems(int numPerLevel)
        {
            // Delete all currently in db
            var dbItems = _db.GetCollection<Item>("items");
            dbItems.EnsureIndex(x => x.Name);
            dbItems.Delete(x => x.Name != null);
            for (int z = 0; z < Program.ZSize; z++)
            {
                for (int x = 0; x < numPerLevel; x++)
                {
                    Item m = CreateNewItem(_rnd.Next(2), _rnd.Next(Program.XSize), _rnd.Next(Program.YSize), z, "");
                    Console.WriteLine("New Item:");
                    Console.WriteLine("Name: " + m.Name + "");
                    Console.WriteLine("Location co-ords: " + m.X + m.Y + m.Z + "");
                    Console.WriteLine("Level: " + m.Level + "");
                    Console.WriteLine("Health: " + m.Health + "");
                    Console.WriteLine("Defence(str): " + m.Strength + "");
                    Console.WriteLine("Magic: " + m.Magic + "");
                    Console.WriteLine("Luck: " + m.Luck + "");
                    Console.WriteLine("Wearable?: " + m.Wearable + "");
                    Console.WriteLine("Weapon?: " + m.Weapon + "");
                    dbItems.Upsert(m);
                }
            }
        }



        // Wipe and overwrite db with new dungeon (careful!)
        public void CreateDungeon()
        {
            // Get collection
            var dbRooms = _db.GetCollection<Room>("rooms");

            // Create, if not exists, new indexes
            dbRooms.EnsureIndex(x => x.X);
            dbRooms.EnsureIndex(x => x.Y);
            dbRooms.EnsureIndex(x => x.Z);

            for (int z = 0; z < Program.ZSize; z++)
            {
                for (int x = 0; x < Program.XSize; x++)
                {
                    for (int y = 0; y < Program.YSize; y++)
                    {
                        // Make room
                        Room currentRoom = dbRooms.FindOne(r => r.X == x && r.Y == y && r.Z == z);
                        if (currentRoom != null)
                        {
                            _rooms[x, y, z] = currentRoom;
                        }
                        else
                        {
                            _rooms[x, y, z] = new Room();
                        }
                        ClearRoom(x, y, z, _language.GetARandomLocationName());

                        // Exits
                        if (y != 0) // Can't go north from top edge
                        {
                            if (_rnd.Next(10) < 6 && y > 0)
                            {
                                _rooms[x, y, z].ExitNorth = (int)ExitStates.Open;
                                _rooms[x, y - 1, z].ExitSouth = (int)ExitStates.Open;
                                dbRooms.Upsert(_rooms[x, y - 1, z]);
                            }
                        }
                        if (x != Program.XSize) // Can't go east from right edge
                        {
                            if (_rnd.Next(10) < 6 && x < XSize)
                            {
                                _rooms[x, y, z].ExitEast = (int)ExitStates.Open;
                                _rooms[x + 1, y, z].ExitWest = (int)ExitStates.Open;
                                dbRooms.Upsert(_rooms[x + 1, y, z]);
                            }
                        }
                        if (y != Program.YSize) // Can't go south from bottom edge
                        {
                            if (_rnd.Next(10) < 6 && y < YSize)
                            {
                                _rooms[x, y, z].ExitSouth = (int)ExitStates.Open;
                                _rooms[x, y + 1, z].ExitNorth = (int)ExitStates.Open;
                                dbRooms.Upsert(_rooms[x, y + 1, z]);
                            }
                        }
                        if (x != 0) // Can't go west from left edge
                        {
                            if (_rnd.Next(10) < 6 && x > 0)
                            {
                                _rooms[x, y, z].ExitWest = (int)ExitStates.Open;
                                _rooms[x - 1, y, z].ExitEast = (int)ExitStates.Open;
                                dbRooms.Upsert(_rooms[x - 1, y, z]);
                            }
                        }
                        if (z != 0) // Can't go up from top level
                        {
                            if (_rnd.Next(10) < 6 && z > 0)
                            {
                                _rooms[x, y, z].ExitUp = (int)ExitStates.Open;
                                _rooms[x, y, z - 1].ExitDown = (int)ExitStates.Open;
                                dbRooms.Upsert(_rooms[x, y, z - 1]);
                            }
                        }
                        if (z != Program.ZSize) // Can't go down from bottom level
                        {
                            if (_rnd.Next(10) < 6 && z < ZSize)
                            {
                                _rooms[x, y, z].ExitDown = (int)ExitStates.Open;
                                _rooms[x, y, z + 1].ExitUp = (int)ExitStates.Open;
                                dbRooms.Upsert(_rooms[x, y, z + 1]);
                            }
                        }

                        dbRooms.Upsert(_rooms[x, y, z]);
                        Console.WriteLine(GetRoomName(x, y, z));
                        Console.WriteLine(GetRoomExits(x, y, z));
                    }

                }
            }

        }

        #endregion


        // --- Misc debug etc ---

        #region DebuggingStuff

        // List all players *** DEBUG
        public void LoadPlayers()
        {
            // Get collection
            var dbPlayers = _db.GetCollection<Player>("players");
            dbPlayers.EnsureIndex(x => x.Username);

            Console.WriteLine("Loading players from db...");
            var allPlayers = dbPlayers.Find(r => r.IsActive);
            foreach (var p in allPlayers)
            {
                Console.WriteLine("Player " + p.Id + "." + p.Username + " in rm " + p.X + "," + p.Y + "," + p.Z);
            }
            Console.WriteLine("Complete...");
            //var rm = dbRooms.FindOne(x => x.X == 0 );
            //Console.WriteLine("Room: "+rm.Title+" ["+rm.Id+"]");
        }

        public string FormatForConsole(string toot)
        {
            toot = toot.Replace("\r", " ");
            toot = toot.Replace("\n", " ");
            toot = toot.Trim();
            toot = toot.Substring(0,(toot.Length < 50) ? toot.Length : 50);
            return toot;
        }

        #endregion


    }
}