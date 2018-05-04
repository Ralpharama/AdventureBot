using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using LiteDB;
using TootNet;

namespace AdventureBot
{
    class Program
    {
        internal static bool Debug = false;

        internal static string Username = "EMAIL_HERE";
        internal static string Password = "PASSWORD_HERE";
        internal static string InstanceName = "botsin.space";
        internal static int XSize = 5;
        internal static int YSize = 5;
        internal static int ZSize = 5;

        public static readonly Regex TagRegex =
            new Regex(@"<(""[^""]*""|'[^']*'|[^'"">])*>", RegexOptions.Compiled);

        static void Main(string[] args)
        {
            try
            {
                Task t = MainAsync(args);
                t.Wait();
            }
            catch (System.Exception ex)
            {
                Console.WriteLine("Error. Process will die.");
                Console.WriteLine(ex.Message);
                Console.ReadLine();
            }
        }

        static async Task MainAsync(string[] args)
        {
            using (var db =
                new LiteDatabase(
                    @"C:\AdventureBot\MyData.db"))
            {

                var language = new Language();
                var dungeon = new Dungeon(db);
                dungeon.LoadDungeon();

                var authorize = new Authorize();
                await authorize.CreateApp(Program.InstanceName, "Tootnet", Scope.Read | Scope.Write | Scope.Follow);

                Console.WriteLine("Logging in " + Program.Username);
                var tokens = await authorize.AuthorizeWithEmail(Program.Username, Program.Password);

                Console.WriteLine("toot [message] - toot message as AdventureBot");
                Console.WriteLine("notification - list notifications");
                Console.WriteLine("add - add/upset players from notif list into db... ALSO reomoves unfollow");
                Console.WriteLine("process - process mentions as commands");
                Console.WriteLine("loop - run add and process with pause on endless loop");
                Console.WriteLine("players - list current players");
                Console.WriteLine("createnew - !!!! wipe and create new dungeon");
                Console.WriteLine("createmonsters - !!!! wipe and create monsters");
                Console.WriteLine("createitems - !!!! wipe and create items");
                Console.WriteLine("quit");

                if (Program.Debug)
                {
                    Console.WriteLine("*** DEBUGGING IS ON ***");
                }

                var command = args;
                var commandWord = (args == null) ? "" : args.FirstOrDefault()?.ToLower();
                Console.WriteLine("...");
                while (commandWord != "exit")
                {
                    switch (commandWord)
                    {
                        case "quit":
                        case "exit":
                            return;

                        case "toot":
                            if (command.Length <= 1)
                                break;
                            var text = command[1].Trim();
                            var post = await tokens.Statuses.PostAsync(status => text);
                            Console.WriteLine(post.Account.DisplayName + "<br>" + post.Account.Acct);
                            Console.WriteLine(TagRegex.Replace(post.Content.Replace("<br />", "\n"), "").Trim());
                            Console.WriteLine(post.CreatedAt);
                            break;

                        case "notification":
                            var notifications = await tokens.Notifications.GetAsync();
                            foreach (var notification in notifications)
                            {
                                Console.WriteLine("-----[start]-------");
                                Console.WriteLine(notification.Account.DisplayName + "<br>" + notification.Account.Acct);
                                Console.WriteLine(notification.Type);
                                if (notification.Type == "mention" || notification.Type == "reblog" ||
                                    notification.Type == "favourite")
                                {
                                    Console.WriteLine(notification.Status.Id+": "+
                                        notification.Status.Account.DisplayName + "<br>" +
                                        notification.Status.Account.Acct);
                                    Console.WriteLine(TagRegex
                                        .Replace(notification.Status.Content.Replace("<br />", "\n"), "").Trim());
                                }
                                Console.WriteLine(notification.CreatedAt);
                                Console.WriteLine("------[end]------");
                            }
                            break;

                        // Add new players
                        case "add":
                            await DoAdds(tokens, db, dungeon);
                            break;

                        // Each player that has mentioned us, process their input
                        case "process":
                            await DoProcess(tokens, db, dungeon);
                            break;

                        // Add and process in andless loop - for task running from scheduler run with dotnet adventurebot.dll loop
                        case "loop":
                            while (true)
                            {
                                await DoAdds(tokens, db, dungeon);
                                await DoProcess(tokens, db, dungeon);
                                Thread.Sleep(900000);  // 15 mins
                            };
                            //await Task.Delay(20);
                            return;

                        case "createnew":
                            dungeon.CreateDungeon();
                            break;

                        case "createmonsters":
                            dungeon.CreateMonsters(5);
                            break;

                        case "createitems":
                            dungeon.CreateItems(5);
                            break;

                        case "load":
                            dungeon.LoadDungeon();
                            break;

                        case "players":
                            dungeon.LoadPlayers();
                            break;
                    }
                    Console.Write("command: ");
                    command = Console.ReadLine().Trim().Split(' ', 2);
                    commandWord = command.First().ToLower();
                }
            } // end of db using
        }

        // Add new users to game
        public static async Task DoAdds(Tokens tokens, LiteDatabase db, Dungeon dungeon)
        {
            Console.WriteLine("Checking for followers...");
            var addNotif = await tokens.Notifications.GetAsync();
            foreach (var notification in addNotif)
            {
                if (notification.Type == "follow")
                {
                    Player p = dungeon.LoadPlayer(notification.Account.Acct);
                    if (p != null)
                    {
                        Console.WriteLine("Player " + p.Username + " already exists in db...");
                    }
                    else
                    {
                        //Console.WriteLine("Adding new player " + notification.Account.Acct + "...");
                        p = dungeon.AddPlayer(notification.Account.Acct, notification.Account.Id);
                        Console.WriteLine("New player (" + p.Id + ") #" + p.AccountId + " - " + p.Username + " in rm " + p.X + "," + p.Y + "," + p.Z);
                        var addMessage = "Welcome @" + notification.Account.Acct +
                                         ", you have joined the game. Type 'help' for commands. " +
                                         "You are in " + dungeon.GetRoomName(p.X, p.Y, p.Z) + "\r\n" +
                                         dungeon.GetRoomExits(p.X, p.Y, p.Z);
                        if (!Program.Debug) // Only send toots if not debugging
                        {
                            await tokens.Statuses.PostAsync(status => addMessage, in_reply_to_account_id => notification.Account.Id, visibility => "private");
                        }
                    }
                }
                //if (notification.Type == "unfollow")
                //{
                //    dungeon.DeletePlayer(notification.Account.Acct);
                //    var delMessage = "Farewell @" + notification.Account.Acct +
                //                     ", you have left the game. Follow to to play again. ";
                //    if (!Program.Debug) // Only send toots if not debugging
                //    {
                //        await tokens.Statuses.PostAsync(status => delMessage, in_reply_to_account_id => notification.Account.Id, visibility => "direct");
                //    }
                //}
            }
            // Unfollow, there is no unfollow notification, so we have to check each player!
            // Get collection 
            // https://github.com/tootsuite/documentation/blob/master/Using-the-API/API.md#accounts 
            var dbPlayers = db.GetCollection<Player>("players");
            dbPlayers.EnsureIndex(x => x.Username);
            Console.WriteLine("Checking for unfollows...");
            var allPlayers = dbPlayers.Find(r => r.Username!=null);
            foreach (var p in allPlayers)
            {
                var u = await tokens.Accounts.RelationshipsAsync(id => p.AccountId);
                if (u != null && !u[0].FollowedBy)
                {
                    Console.WriteLine("Player unfollowed (" + p.Id + ") #" + p.AccountId + " - " + p.Username + " in rm " + p.X + "," + p.Y + "," + p.Z);
                    Console.WriteLine("...deleteing");
                    dungeon.DeletePlayer(p.Username);
                }
            }
            Console.WriteLine("Complete...");
        }

        // Process each mention as command
        public static async Task DoProcess(Tokens tokens, LiteDatabase db, Dungeon dungeon)
        {            
            Console.WriteLine("Processing players...");
            var pn = await tokens.Notifications.GetAsync();
            foreach (var notification in pn)
            {
                if (notification.Type == "mention")
                {
                    var mentionToReturn = TagRegex.Replace(notification.Status.Content.Replace("<br />", "\n"), "").Trim();
                    mentionToReturn = (mentionToReturn.Length > 55) ? mentionToReturn.Substring(0,54) : mentionToReturn;
                    Console.WriteLine("M#" + notification.Status.Id + " " + notification.Account.Acct + ": "+ mentionToReturn);
                    var currentStatusId = notification.Status.Id;
                    Player pl = dungeon.LoadPlayer(notification.Account.Acct);
                    if (pl != null && (pl.LastStatusId<currentStatusId || Program.Debug))  // Only last command! unless debugging
                    {
                        var tootToSend = dungeon.Process(pl, notification.Status.Id, notification.Status.Content);
                        dungeon.UpsertPlayer(pl);
                        if (tootToSend.Content != "")
                        {
                            if (!Program.Debug) // Only send toots if not debugging
                            {
                                tootToSend.AccountId = notification.Account.Id;
                                await tokens.Statuses.PostAsync(status => "@" + tootToSend.Username + " " + tootToSend.Content, inReplyToAccountId => tootToSend.AccountId, visibility => tootToSend.Privacy);
                            }
                            Console.Write(" >> toot to " + tootToSend.Username);
                            Console.WriteLine(" : " + dungeon.FormatForConsole(tootToSend.Content));
                        }
                    }
                }
            }

            // Process/Move monsters
            var tootsToSend = dungeon.ProcessMonsters();
            if (tootsToSend != null)
            {
                foreach (var ts in tootsToSend)
                {
                    if (!Program.Debug) // Only send toots if not debugging
                    {
                        await tokens.Statuses.PostAsync(status => "@" + ts.Username + " " + ts.Content, in_reply_to_account_id => ts.AccountId, visibility => ts.Privacy);
                    }
                    Console.Write(" >> toot to " + ts.Username);
                    Console.WriteLine(" : " + dungeon.FormatForConsole(ts.Content));
                }

            }




        }



    }
}