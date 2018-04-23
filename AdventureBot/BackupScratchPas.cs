namespace AdventureBot
{
    public class BackupScratchPas
    {
        // Get a collection (or create, if doesn't exist)
        //var col = db.GetCollection<Player>("players");
        //var player = new Player
        //{
        //    X = 1, Y = 1, Z = 1,
        //    Username = "@ralpharama@social.tchncs.de",
        //    IsActive =true,
        //    Health = 100, Strength = 10, Magic = 5, Luck = 5
        //};
        //col.Insert(player);

        //// Do we have a valid token?
        //string code = System.IO.File.ReadAllText(@"C:\AdventureBot\current_token.txt");

        //// Display the file contents to the console. Variable text is a string.
        //Console.WriteLine("Starting. Saved token = "+code);
        //Console.WriteLine("Trying with this token... ");
        //Tokens tokens;
        //try
        //{
        //    tokens = await authorize.AuthorizeWithCode(code);            
        //}
        //// need new token
        //catch
        //{
        //    var authorizeUrl = authorize.GetAuthorizeUri();
        //    Console.WriteLine(authorizeUrl);
        //    Console.Write("code: ");
        //    code = Console.ReadLine().Trim();
        //    tokens = await authorize.AuthorizeWithCode(code);
        //    if (tokens != null)
        //        System.IO.File.WriteAllText($@"C:\AdventureBot\current_token.txt", code);
        //}
        //Console.WriteLine("Response is: "+tokens);

        //case "home":
        //    var home = await tokens.Timelines.HomeAsync();
        //    Console.WriteLine("--------------------");
        //    foreach (var status in home)
        //    {
        //        Console.WriteLine(status.Account.DisplayName + "\t\t" + status.Account.Acct);
        //        Console.WriteLine(TagRegex.Replace(status.Content.Replace("<br />", "\n"), "").Trim());
        //        Console.WriteLine(status.CreatedAt);
        //        Console.WriteLine("--------------------");
        //    }
        //    break;

        //case "ftl":
        //    var ftl = await tokens.Timelines.PublicAsync();
        //    Console.WriteLine("--------------------");
        //    foreach (var status in ftl)
        //    {
        //        Console.WriteLine(status.Account.DisplayName + "\t\t" + status.Account.Acct);
        //        Console.WriteLine(TagRegex.Replace(status.Content.Replace("<br />", "\n"), "").Trim());
        //        Console.WriteLine(status.CreatedAt);
        //        Console.WriteLine("--------------------");
        //    }
        //    break;

        //case "ltl":
        //    var ltl = await tokens.Timelines.PublicAsync(local => true);
        //    Console.WriteLine("--------------------");
        //    foreach (var status in ltl)
        //    {
        //        Console.WriteLine(status.Account.DisplayName + "\t\t" + status.Account.Acct);
        //        Console.WriteLine(TagRegex.Replace(status.Content.Replace("<br />", "\n"), "").Trim());
        //        Console.WriteLine(status.CreatedAt);
        //        Console.WriteLine("--------------------");
        //    }
        //    break;


    }
}