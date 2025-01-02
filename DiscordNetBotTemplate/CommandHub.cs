using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Numerics;
using System.Runtime.CompilerServices;
using MongoDB.Bson.Serialization.Serializers;
using System.Diagnostics;
using static MongoDB.Bson.Serialization.Serializers.SerializerHelper;


namespace GFDeckMaid;

public class CommandHub
{
    public static CommandHub commandHub;
    private readonly string preFix;
    private readonly static string
        tinyGreen = "<:TinyGreen:918536115194056725>",
        gmuwu = "<:GMuwu:918536115051454555>",
        pantiesowo = "<:owo:918536115558965309>",
        owoblush = "<:owoblush:922814485868195850>",
        gmowo = "<:GMowo:918536114611056642>",
        megublush = "<:megublush:922814214379298876>",
        mommyplease = "<:please_mommy_gf:1091312984929865748>";
    public CommandHub(string preFix)
    {
        this.preFix = preFix;
        commandHub = this;
    }
    public async Task OnMessage(SocketMessage message)
    {
        if (message.Author.IsBot || !message.Content.StartsWith(preFix, StringComparison.InvariantCultureIgnoreCase))
        {
            return;
        }

        var args = new List<string>(message.Content.Split(' '));

        args[0] = args[0][preFix.Length..];
        args = args.Select(arg => arg.Trim()).ToList();
        args = args.Where(arg => !string.IsNullOrWhiteSpace(arg)).ToList();

        try
        {
            switch (args[0].ToLower())
            {
                case "draw":
                    await Draw(args.GetRange(1, args.Count - 1), message);
                    break;
                case "setdeck":
                    await SetDeck(args.GetRange(1, args.Count - 1), message);
                    break;
                case "viewcard":
                    await ViewCard(args.GetRange(1, args.Count - 1), message);
                    break;
                case "reset":
                    await Reset(message);
                    break;
                case "refill":
                    await Refill(message);
                    break;
                case "shuffle":
                    await Shuffle(message);
                    break;
                case "myhand":
                    await MyHand(message);
                    break;
                case "discard":
                    await Discard(args.GetRange(1, args.Count - 1), message);
                    break;
                case "dominance":
                    await Dominance(args.GetRange(1, args.Count - 1), message);
                    break;
                case "hand":
                    await Hand(message);
                    break;
                case "viewdiscard":
                    await ViewDiscard(message);
                    break;
                case "viewdominance":
                    await ViewDominance(message);
                    break;
                case "takerand":
                    await TakeRand(message);
                    break;
                case "giverand":
                    await GiveRand(message);
                    break;
                case "give":
                    await GiveCard(args.GetRange(1, args.Count - 1), message);
                    break;
                case "showhand":
                    await ShowHand(message);
                    break;
                case "viewhand":
                    await ViewHand(message);
                    break;
                case "showcards":
                case "showcard":
                    await ShowCards(args.GetRange(1, args.Count - 1), message);
                    break;
                case "take":
                    await TakeCard(args.GetRange(1, args.Count - 1), message);
                    break;
                case "craft":
                    await Craft(args.GetRange(1, args.Count - 1), message);
                    break;
                case "uncraft":
                    await Uncraft(args.GetRange(1, args.Count - 1), message);
                    break;
                case "crafted":
                    await Crafted(message);
                    break;
                case "trimdeck":
                    await TrimDeck(args.GetRange(1, args.Count - 1), message);
                    break;
                case "grab":
                    await Grab(args.GetRange(1, args.Count - 1), message);
                    break;

                default:
                    await message.Channel.SendMessageAsync($"whoopsy not a command I know");
                    break;

            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            Console.WriteLine(ex.StackTrace);
            await message.Channel.SendMessageAsync($":boom:error:boom:: {ex.Message}");
        }
    }
    private async Task Grab(List<string> args, SocketMessage message)
    {
        var deck = DBConnection.dBConnection.GetDeck(message);
        if (deck is null)
        {
            return;
        }

        if(!Enum.TryParse<Piles>(args.ElementAtOrDefault(0), true, out Piles pileType))
        {
            await message.Channel.SendMessageAsync($"Faild to prase pile{megublush}");
            return;
        }

        List<string> cardPositions;
        var target = message.MentionedUsers.FirstOrDefault();
        if (target is null)
        {
            cardPositions = args.GetRange(1, args.Count - 1);
            target = message.Author;
        }
        else
        {
            cardPositions = args.GetRange(1, args.Count - 2);
        }

        List<int> pile = null;
        switch (pileType)
        {
            case Piles.Discard:
                pile = deck.discard;
                break;
            default:
                await message.Channel.SendMessageAsync($"Pile type not supported{megublush}");
                break;
        }

        var cardPositionsInts = cardPositions.Any() ?
            await ArgaToIntArgs(message, cardPositions, pile.Count) :
            new() { pile.Count };

        var player = deck.GetPlayer(message);
        List<int> cards = new();
        for (int i = 0; i < cardPositionsInts.Count; i++)
        {
            var card = pile[cardPositionsInts[i]-1];
            cards.Add(card);
            player.cards.Add(card);
            pile.RemoveAt(cardPositionsInts[i]-1);
        }

        var cardImage = await GetCardImage(cards, message, deck, 10);
        await message.Channel.SendFileAsync(cardImage, "cards.png", text: $"Cards grabbed {gmowo}");
        DBConnection.dBConnection.SaveDeck(deck, message);
        await YourHandBoss(message.Author, deck, player, message);
    }
    public async Task TrimDeck(SocketMessage message)
    {
        var deck = DBConnection.dBConnection.GetDeck(message);
        if (deck is null)
        {
            return;
        }

        var discardImage = await GetCardImage(deck.discard, message, deck, 10);
        if (discardImage != null)
        {
            await message.Channel.SendFileAsync(discardImage, "DiscardCards.png", text: $"{megublush} discard pile:");
        }
        else
        {
            await message.Channel.SendMessageAsync($"{megublush} discard pile is empty");
        }
    }
    public async Task ViewDiscard(SocketMessage message)
    {
        var deck = DBConnection.dBConnection.GetDeck(message);
        if (deck is null)
        {
            return;
        }

        var discardImage = await GetCardImage(deck.discard, message, deck, 10);
        if (discardImage != null)
        {
            await message.Channel.SendFileAsync(discardImage, "DiscardCards.png", text: $"{megublush} discard pile:");
        }
        else
        {
            await message.Channel.SendMessageAsync($"{megublush} discard pile is empty");
        }
    }
    public async Task ViewDominance(SocketMessage message)
    {
        var deck = DBConnection.dBConnection.GetDeck(message);
        if (deck is null)
        {
            return;
        }

        var dominanceImage = await GetCardImage(deck.dominance, message, deck, 10);
        if (dominanceImage != null)
        {
            await message.Channel.SendFileAsync(dominanceImage, "DiscardCards.png", text: $"{mommyplease} dominance pile:");
        }
        else
        {
            await message.Channel.SendMessageAsync($"{mommyplease} dominance pile is empty");
        }
    }
    private async Task Draw(List<string> args, SocketMessage message)
    {

        if (!int.TryParse(args.ElementAtOrDefault(0), out int count))
        {
            if (args.ElementAtOrDefault(0) is null)
            {
                count = 1;
            }
            else
            {
                await message.Channel.SendMessageAsync($"Cannot parse {args[0]} {owoblush}");
                return;
            }
        }

        if (count > 10 || count <= 0)
        {
            await message.Channel.SendMessageAsync($"Boss, I didn't mean to be disrespectful, are you sure you want to draw {count} goddaim cards?");
            return;
        }

        var deck = DBConnection.dBConnection.GetDeck(message);
        if (deck is null)
        {
            return;
        }

        if (deck.columns is 0 || deck.rows is 0 || deck.cardCount is 0 || string.IsNullOrWhiteSpace(deck.imageLink))
        {
            await message.Channel.SendMessageAsync($"Deck not set");
            return;
        }

        var player = deck.GetPlayer(message.Author.Id);

        int i = 0;
        while (i < count && await CanDraw(message, deck, i))
        {
            i++;
            player.cards.Add(deck.deck[0]);
            deck.deck.RemoveAt(0);
        }
        DBConnection.dBConnection.SaveDeck(deck, message);
        await MyHand(message, deck);
    }
    public async Task Discard(List<string> args, SocketMessage message)
    {
        var deck = DBConnection.dBConnection.GetDeck(message);
        if (deck is null)
        {
            return;
        }

        var player = deck.GetPlayer(message);
        var playerHandLimit = player.cards.Count;

        var intArgs = await ArgaToIntArgs(message, args, playerHandLimit);
        if(intArgs is null)
        {
            return;
        }

        var sortedArgs = intArgs.OrderBy(n => -n);
        var cards = new List<int>();

        foreach (var arg in sortedArgs)
        {
            var card = player.cards[arg - 1];
            cards.Add(player.cards[arg - 1]);
            if (deck.dominanceMark.Contains(card))
            {
                deck.dominance.Add(card);
            }
            else
            {
                deck.discard.Add(card);
            }
            player.cards.RemoveAt(arg - 1);
        }

        DBConnection.dBConnection.SaveDeck(deck, message);
        await MyHand(message, deck);
        var handImage = await GetCardImage(cards, message, deck, 7);
        await message.Channel.SendFileAsync(handImage, "cards.png", text: $"Tossed those to the discard pile {gmowo}");
    }
    private async Task<List<int>> ArgaToIntArgs(SocketMessage message, List<string> args, int limit)
    {
        List<int> intArgs = new List<int>();

        foreach (string arg in args)
        {
            if (int.TryParse(arg, out int num))
            {
                intArgs.Add(num);
            }
            else
            {
                await message.Channel.SendMessageAsync($"Cannot parse {args} {owoblush}");
                return null;
            }
        }

        if (!intArgs.Any())
        {
            await message.Channel.SendMessageAsync($"No cards to use {owoblush}");
            return null;
        }
        if (intArgs.Any(num => num < 1 || num > limit))
        {
            await message.Channel.SendMessageAsync($"Card count in target are limited to {limit} cards {owoblush}");
            return null;
        }
        if (intArgs.GroupBy(num => num).Any(g => g.Count() > 1))
        {
            await message.Channel.SendMessageAsync($"Cannot discard same card twice {owoblush}");
            return null;
        }
        return intArgs.OrderBy(i => -i).ToList();
    }
    public async Task Dominance(List<string> args, SocketMessage message)
    {
        var deck = DBConnection.dBConnection.GetDeck(message);
        if (deck is null)
        {
            return;
        }

        List<int> intArgs = new List<int>();

        foreach (string arg in args)
        {
            if (int.TryParse(arg, out int num))
            {
                intArgs.Add(num);
            }
            else
            {
                await message.Channel.SendMessageAsync($"Cannot parse {arg} {owoblush}");
                return;
            }
        }

        if (!intArgs.Any())
        {
            await message.Channel.SendMessageAsync($"Removed dominance mark from cards {gmowo}");
            return;
        }

        deck.dominanceMark = intArgs;
        DBConnection.dBConnection.SaveDeck(deck, message);
        var dominanceCards = await GetCardImage(intArgs, message, deck, 7);
        if(dominanceCards != null)
        {
            await message.Channel.SendFileAsync(dominanceCards, "dominanceCards.png", text: $"Marked those as dominance {gmowo}");
        }
        else
        {
            await message.Channel.SendMessageAsync($"dommom nu nun nulll {gmowo}");
        }
    }
    public async Task TrimDeck(List<string> args, SocketMessage message)
    {
        var deck = DBConnection.dBConnection.GetDeck(message);
        if (deck is null)
        {
            return;
        }

        List<int> intArgs = new List<int>();

        foreach (string arg in args)
        {
            if (int.TryParse(arg, out int num))
            {
                intArgs.Add(num);
            }
            else
            {
                await message.Channel.SendMessageAsync($"Cannot parse {arg} {owoblush}");
                return;
            }
        }

        deck.trim = intArgs;
        DBConnection.dBConnection.SaveDeck(deck, message);
        var trimedCards = await GetCardImage(intArgs, message, deck, 7);
        if (trimedCards != null)
        {
            await message.Channel.SendFileAsync(trimedCards, "dominanceCards.png", text: $"Marked those as dominance {gmowo}");
        }
        else
        {
            await message.Channel.SendMessageAsync($"dommom nu nun nulll {gmowo}");
        }
    }
    public async Task MyHand(SocketMessage message)
    {
        var deck = DBConnection.dBConnection.GetDeck(message);
        if (deck is null)
        {
            return;
        }
        await MyHand(message, deck);
    }
    private async Task MyHand(SocketMessage message, DeckState deck)
    {
        var cards = deck.GetPlayer(message).cards;
        await message.Channel.SendMessageAsync($"You have {cards.Count} cards {tinyGreen}");
        var handImage = await GetCardImage(deck.GetPlayer(message).cards, message, deck, 7);
        if (handImage != null)
        {
            await message.Author.SendFileAsync(handImage, "cards.png", text: $"Here is your hand boss {pantiesowo}");
        }
    }
    private async Task YourHandBoss(SocketUser user, DeckState deck, Player player, SocketMessage message)
    {

        var channel = (message.Channel as IGuildChannel);
        if (channel == null)
        { 
            await message.Channel.SendMessageAsync($"This not be working in DMs sowwy {owoblush}.");
            return;
        }

        var socketGuild = channel.Guild as SocketGuild;
        var guildUser = socketGuild.GetUser(user.Id);

        var handImage = await GetCardImage(player.cards, message, deck, 7);
        if (handImage != null)
        {
            await guildUser.SendFileAsync(handImage, "cards.png", text: $"Here is your hand boss {pantiesowo}");
        }
    }
    public async Task ShowCards(List<string> args, SocketMessage message)
    {
        var deck = DBConnection.dBConnection.GetDeck(message);
        if (deck is null)
        {
            return;
        }

        var player = deck.GetPlayer(message);

        var intArgs = await ArgaToIntArgs(message, args, player.cards.Count);
        if (intArgs is null)
        {
            return;
        }

        var cards = intArgs.Select(arg => player.cards[arg]);

        var cardsImage = await GetCardImage(cards.ToList(), message, deck, 10);
        if (cardsImage != null)
        {
            await message.Channel.SendFileAsync(cardsImage, "cards.png", text: $"Some cards {tinyGreen}");
        }
        else
        {
            await message.Channel.SendMessageAsync($"Your hand is empty {gmuwu}");
        }
    }
    public async Task ShowHand(SocketMessage message)
    {
        var deck = DBConnection.dBConnection.GetDeck(message);
        if (deck is null)
        {
            return;
        }

        var handImage = await GetCardImage(deck.GetPlayer(message).cards, message, deck, 10);
        if (handImage != null)
        {
            await message.Channel.SendFileAsync(handImage, "HandCards.png", text: $"Your hand 💚");
        }
        else
        {
            await message.Channel.SendMessageAsync($"Your hand is empty {gmuwu}");
        }
    }
    public async Task ViewHand(SocketMessage message)
    {
        var deck = DBConnection.dBConnection.GetDeck(message);
        if (deck is null)
        {
            return;
        }
        var target = message.MentionedUsers?.FirstOrDefault();
        if (target is null)
        {
            await message.Channel.SendMessageAsync($"You need to @ somebody {tinyGreen}");
        }

        var handImage = await GetCardImage(deck.GetPlayer(target.Id).cards, message, deck, 10);
        await message.Channel.SendMessageAsync($"{message.Author.Mention} is peeping on {target.Mention} {pantiesowo}");
        if (handImage != null)
        {
            await message.Author.SendFileAsync(handImage, "HandCards.png", text: $"Hand of {target.Mention} {pantiesowo}");
        }
        else
        {
            await message.Channel.SendMessageAsync($"{target.Mention} hand is empty {gmuwu}");
        }
    }
    public async Task TakeRand(SocketMessage message)
    {
        var deck = DBConnection.dBConnection.GetDeck(message);
        if (deck is null)
        {
            return;
        }

        var target = message.MentionedUsers?.FirstOrDefault();
        if (target is null)
        {
            await message.Channel.SendMessageAsync($"You need to @ somebody {tinyGreen}");
        }

        var targetPlayer = deck.GetPlayer(target.Id);
        if (!targetPlayer.cards.Any())
        {
            await message.Channel.SendMessageAsync($"{target.Mention} has no cards to take {owoblush}");
        }

        var user = deck.GetPlayer(message);

        int rand = targetPlayer.cards.GetRandPosition();
        user.cards.Add(targetPlayer.cards[rand]);
        targetPlayer.cards.RemoveAt(rand);

        DBConnection.dBConnection.SaveDeck(deck, message);
        await message.Channel.SendMessageAsync($"You have {user.cards.Count} cards {tinyGreen}\n{target.Mention} has {targetPlayer.cards.Count} cards1 {tinyGreen}");
        await Task.WhenAll(
            YourHandBoss(message.Author, deck, user, message),
            YourHandBoss(target, deck, targetPlayer, message)
        );
    }
    public async Task GiveRand(SocketMessage message)
    {
        var deck = DBConnection.dBConnection.GetDeck(message);
        if (deck is null)
        {
            return;
        }

        var target = message.MentionedUsers?.FirstOrDefault();
        if (target is null)
        {
            await message.Channel.SendMessageAsync($"You need to @ somebody {tinyGreen}");
        }

        var targetPlayer = deck.GetPlayer(target.Id);
        if (!targetPlayer.cards.Any())
        {
            await message.Channel.SendMessageAsync($"{target.Mention} has no cards to take {owoblush}");
        }

        var user = deck.GetPlayer(message);

        int rand = user.cards.GetRandPosition();
        targetPlayer.cards.Add(user.cards[rand]);
        user.cards.RemoveAt(rand);

        DBConnection.dBConnection.SaveDeck(deck, message);
        await message.Channel.SendMessageAsync($"You have {user.cards.Count} cards {tinyGreen}\n{target.Mention} has {targetPlayer.cards.Count} cards1 {tinyGreen}");
        await Task.WhenAll(
            YourHandBoss(message.Author, deck, user, message),
            YourHandBoss(target, deck, targetPlayer, message)
        );
    }
    public async Task GiveCard(List<string> args, SocketMessage message)
    {
        var deck = DBConnection.dBConnection.GetDeck(message);
        if (deck is null)
        {
            return;
        }

        var target = message.MentionedUsers?.FirstOrDefault();
        if (target is null)
        {
            await message.Channel.SendMessageAsync($"You need to @ somebody {tinyGreen}");
        }

        var user = deck.GetPlayer(message);
        if (!user.cards.Any())
        {
            await message.Channel.SendMessageAsync($"You has no cards to give {owoblush}");
        }

        var targetPlayer = deck.GetPlayer(target.Id);

        var intArgs = await ArgaToIntArgs(message, args.Take(args.Count - 1).ToList(), user.cards.Count);
        if(intArgs is null)
        {
            return;
        }

        foreach(var intArg in intArgs)
        {
            targetPlayer.cards.Add(user.cards[intArg-1]);
            user.cards.RemoveAt(intArg-1);
        }

        DBConnection.dBConnection.SaveDeck(deck, message);
        await message.Channel.SendMessageAsync($"You have {user.cards.Count} cards {tinyGreen}\n{target.Mention} has {targetPlayer.cards.Count} cards1 {tinyGreen}");
        await Task.WhenAll(
            YourHandBoss(message.Author, deck, user, message),
            YourHandBoss(target, deck, targetPlayer, message)
        );
    }
    public async Task TakeCard(List<string> args, SocketMessage message)
    {
        var deck = DBConnection.dBConnection.GetDeck(message);
        if (deck is null)
        {
            return;
        }

        var target = message.MentionedUsers?.FirstOrDefault();
        if (target is null)
        {
            await message.Channel.SendMessageAsync($"You need to @ somebody {tinyGreen}");
        }

        var targetPlayer = deck.GetPlayer(target.Id);
        if (!targetPlayer.cards.Any())
        {
            await message.Channel.SendMessageAsync($"{target.Mention} has no cards to take {owoblush}");
        }

        var user = deck.GetPlayer(message);
        var intArgs = await ArgaToIntArgs(message, args.Take(args.Count - 1).ToList(), targetPlayer.cards.Count);
        if (intArgs is null)
        {
            return;
        }

        foreach (var intArg in intArgs)
        {
            user.cards.Add(targetPlayer.cards[intArg - 1]);
            targetPlayer.cards.RemoveAt(intArg - 1);
        }

        DBConnection.dBConnection.SaveDeck(deck, message);
        await message.Channel.SendMessageAsync($"You have {user.cards.Count} cards {tinyGreen}\n{target.Mention} has {targetPlayer.cards.Count} cards1 {tinyGreen}");
        await Task.WhenAll(
            YourHandBoss(message.Author, deck, user, message),
            YourHandBoss(target, deck, targetPlayer, message)
        );
    }
    public async Task Hand(SocketMessage message)
    {
        var deck = DBConnection.dBConnection.GetDeck(message);
        if (deck is null)
        {
            return;
        }

        var target = message.MentionedUsers?.FirstOrDefault();
        if (target is null)
        {
            await message.Channel.SendMessageAsync($"You need to @ somebody {tinyGreen}");
        }

        var cards = deck.GetPlayer(target.Id).cards;
        await message.Channel.SendMessageAsync($"{target.Mention} have {cards.Count} cards {tinyGreen}");
    }
    public async Task<MemoryStream> GetCardImage(List<int> cards, SocketMessage message, DeckState deck, int columns)
    {
        if (deck.columns == 0 || deck.rows == 0 || deck.cardCount == 0 || string.IsNullOrWhiteSpace(deck.imageLink))
        {
            await message.Channel.SendMessageAsync("Deck not set");
            return null;
        }

        if (cards.Count == 0)
        {
            return null;
        }

        try
        {
            using (HttpClient client = new HttpClient())
            {
                byte[] imageBytes = await client.GetByteArrayAsync(deck.imageLink);

                using (MemoryStream ms = new MemoryStream(imageBytes))
                {
                    using (Image<Rgba32> image = SixLabors.ImageSharp.Image.Load<Rgba32>(ms))
                    {
                        // Calculate card dimensions
                        int cardWidth = image.Width / deck.columns;
                        int cardHeight = image.Height / deck.rows;

                        // Create a new image with the required dimensions
                        int totalWidth = cardWidth * Math.Min(columns, cards.Count);
                        int totalRows = (int)Math.Ceiling((double)cards.Count / columns);
                        int totalHeight = cardHeight * totalRows;

                        using (Image<Rgba32> combinedImage = new Image<Rgba32>(totalWidth, totalHeight))
                        {
                            int currentRow = 0;
                            int currentColumn = 0;

                            foreach (int cardId in cards)
                            {
                                // Calculate the card's position in the deck
                                int row = (cardId - 1) / deck.columns;
                                int column = (cardId - 1) % deck.columns;

                                // Crop the specific card
                                Rectangle cardRectangle = new Rectangle(column * cardWidth, row * cardHeight, cardWidth, cardHeight);
                                Image<Rgba32> cardImage = image.Clone(ctx => ctx.Crop(cardRectangle));

                                // Position the card in the combined image
                                int xPos = currentColumn * cardWidth;
                                int yPos = currentRow * cardHeight;

                                combinedImage.Mutate(ctx => ctx.DrawImage(cardImage, new Point(xPos, yPos), 1f));

                                currentColumn++;
                                if (currentColumn >= columns)
                                {
                                    currentColumn = 0;
                                    currentRow++;
                                }
                            }

                            // Save the combined image to a memory stream
                            MemoryStream combinedMs = new MemoryStream();

                            combinedImage.SaveAsPng(combinedMs);
                            combinedMs.Seek(0, SeekOrigin.Begin);

                            return (combinedMs);
                        }
                    }
                }
            }
        }
        catch (HttpRequestException)
        {
            await message.Channel.SendMessageAsync("Error accessing the image URL.");
        }
        catch (Exception)
        {
            await message.Channel.SendMessageAsync("An error occurred while processing the image.");
        }
        return null;
    }
    private async Task<bool> CanDraw(SocketMessage message, DeckState deck, int drownCards)
    {
        if (deck.deck.Any() || Refill(deck))
        {
            return true;
        }
        await message.Channel.SendMessageAsync($"Cannot draw more then {drownCards} cards! Deck and discard empty");
        return false;
    }
    public async Task Refill(SocketMessage message)
    {
        var deck = DBConnection.dBConnection.GetDeck(message);
        if (deck is null)
        {
            return;
        }

        if (Refill(deck))
        {
            DBConnection.dBConnection.SaveDeck(deck, message);
            await message.Channel.SendMessageAsync($"Deck refiled! Cards in deck: {deck.deck.Count}");
        }

        await message.Channel.SendMessageAsync($"Discardpile empty! Cards in deck: {deck.deck.Count}");

    }
    private bool Refill(DeckState deck)
    {
        if (!deck.deck.Any() && !deck.discard.Any())
        {
            return false;
        }

        deck.deck.AddRange(deck.discard);
        deck.discard = new();
        deck.deck.Shuffle();

        return true;
    }
    public async Task SetDeck(List<string> args, SocketMessage message)
    {
        if (!int.TryParse(args.ElementAtOrDefault(0), out int columns))
        {
            await message.Channel.SendMessageAsync($"{args[0]} is not a number");
            return;
        }

        if (!int.TryParse(args.ElementAtOrDefault(1), out int rows))
        {
            await message.Channel.SendMessageAsync($"{args[0]} is not a number");
            return;
        }

        if (!int.TryParse(args.ElementAtOrDefault(2), out int cardCount))
        {
            await message.Channel.SendMessageAsync($"{args[0]} is not a number");
            return;
        }

        if (string.IsNullOrEmpty(args.ElementAtOrDefault(3)))
        {
            await message.Channel.SendMessageAsync("No URL provided.");
            return;
        }

        var deck = DBConnection.dBConnection.GetDeck(message);
        if (deck is null)
        {
            return;
        }

        deck.columns = columns;
        deck.rows = rows;
        deck.cardCount = cardCount;
        deck.imageLink = args.ElementAtOrDefault(3);
        Reset(deck, message);

        DBConnection.dBConnection.SaveDeck(deck, message);
        await message.Channel.SendMessageAsync($"All set and ready to play!");
    }
    public async Task Reset(SocketMessage message)
    {
        var deck = DBConnection.dBConnection.GetDeck(message);
        if (deck is null)
        {
            return;
        }
        Reset(deck, message);
        await message.Channel.SendMessageAsync($"Deck reset {gmuwu}");
    }
    private async Task Shuffle(SocketMessage message)
    {
        var deck = DBConnection.dBConnection.GetDeck(message);
        if (deck is null)
        {
            return;
        }
        deck.deck.Shuffle();
        DBConnection.dBConnection.SaveDeck(deck, message);
        await message.Channel.SendMessageAsync($"Deck shuffled {gmuwu}");
    }
    private void Reset(DeckState deckState, SocketMessage message)
    {
        deckState.players = new();
        deckState.discard = new();
        deckState.dominance = new();
        deckState.deck = new();
        for (int i = 1; i <= deckState.cardCount; i++)
        {
            deckState.deck.Add(i);
        }
        for (int i = 0; i < deckState.trim.Count; i++)
        {
            deckState.deck.Remove(deckState.trim[i]);
        }
        deckState.deck.Shuffle();
        DBConnection.dBConnection.SaveDeck(deckState, message);
    }
    public async Task ViewCard(List<string> args, SocketMessage message)
    {
        if (!int.TryParse(args.ElementAtOrDefault(0), out int cardId))
        {
            await message.Channel.SendMessageAsync($"{args[0]} is not a number");
            return;
        }

        var deck = DBConnection.dBConnection.GetDeck(message);
        if (deck is null)
        {
            return;
        }

        if (deck.columns is 0 || deck.rows is 0 || deck.cardCount is 0 || string.IsNullOrWhiteSpace(deck.imageLink))
        {
            await message.Channel.SendMessageAsync($"Deck not set");
            return;
        }

        try
        {
            using (HttpClient client = new HttpClient())
            {
                byte[] imageBytes = await client.GetByteArrayAsync(deck.imageLink);

                using (MemoryStream ms = new MemoryStream(imageBytes))
                {
                    using (Image<Rgba32> image = SixLabors.ImageSharp.Image.Load<Rgba32>(ms))
                    {
                        // Calculate card dimensions
                        int cardWidth = image.Width / deck.columns;
                        int cardHeight = image.Height / deck.rows;

                        // Calculate the card's position in the deck
                        int row = (cardId - 1) / deck.columns;
                        int column = (cardId - 1) % deck.columns;

                        // Crop the specific card
                        Rectangle cardRectangle = new Rectangle(column * cardWidth, row * cardHeight, cardWidth, cardHeight);
                        Image<Rgba32> cardImage = image.Clone(ctx => ctx.Crop(cardRectangle));

                        // Save the cropped card image to a memory stream
                        using (MemoryStream cardMs = new MemoryStream())
                        {
                            cardImage.SaveAsPng(cardMs);
                            cardMs.Seek(0, SeekOrigin.Begin);

                            // Send the cropped card image to the channel
                            await message.Channel.SendFileAsync(cardMs, $"card{cardId}.png", text: $"Showing card {cardId}");
                        }
                    }
                }
            }
        }
        catch (HttpRequestException)
        {
            await message.Channel.SendMessageAsync("Error accessing the image URL.");
        }
        catch (Exception)
        {
            await message.Channel.SendMessageAsync("An error occurred while processing the image.");
        }
    }
    private async Task ShowCrafted(DeckState deck, Player user, SocketMessage message)
    {
        var craftedImage = await GetCardImage(user.crafted, message, deck, 7);
        if (craftedImage != null)
        {
            await message.Channel.SendFileAsync(craftedImage, "CraftedCards.png", text: $"Crafted cards {gmuwu}");
        }
        else
        {
            await message.Channel.SendMessageAsync($"No crafted cards {owoblush}");
        }
    }
    public async Task Crafted(SocketMessage message)
    {
        var deck = DBConnection.dBConnection.GetDeck(message);
        if (deck is null)
        {
            return;
        }

        var target = message.MentionedUsers?.FirstOrDefault() ?? message.Author;
        await ShowCrafted(deck, deck.GetPlayer(target.Id), message);
    }
    public async Task Uncraft(List<string> args, SocketMessage message)
    {

        var deck = DBConnection.dBConnection.GetDeck(message);
        if (deck is null)
        {
            return;
        }

        var user = deck.GetPlayer(message);

        var intArgs = await ArgaToIntArgs(message, args.ToList(), user.cards.Count);
        if (intArgs is null)
        {
            return;
        }

        foreach (var intArg in intArgs)
        {
            deck.discard.Add(user.crafted[intArg-1]);
            user.crafted.RemoveAt(intArg-1);
            ;
        }

        await message.Channel.SendMessageAsync($"Discarded {intArgs.Count} crafted cards {gmowo}");
        DBConnection.dBConnection.SaveDeck(deck, message);
        await ShowCrafted(deck, deck.GetPlayer(message), message);
    }
    public async Task Craft(List<string> args, SocketMessage message)
    {
        var deck = DBConnection.dBConnection.GetDeck(message);
        if (deck is null)
        {
            return;
        }

        var user = deck.GetPlayer(message);

        var intArgs = await ArgaToIntArgs(message, args.ToList(), user.cards.Count);
        if (intArgs is null)
        {
            return;
        }

        foreach(var intArg in intArgs)
        {
            user.crafted.Add(user.cards[intArg-1]);
            user.cards.RemoveAt(intArg-1);
;       }

        await message.Channel.SendMessageAsync($"Craft {intArgs.Count} cards {gmowo}");
        DBConnection.dBConnection.SaveDeck(deck, message);
        await ShowCrafted(deck, deck.GetPlayer(message), message);
        await YourHandBoss(message.Author, deck, user, message);
    }

}

