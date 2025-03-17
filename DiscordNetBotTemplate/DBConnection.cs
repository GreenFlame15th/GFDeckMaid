using Discord.WebSocket;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Core.Servers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace GFDeckMaid;

public class DBConnection
{
    public static DBConnection dBConnection;
    private readonly MongoClient client;
    private readonly IMongoDatabase database;
    private readonly IMongoCollection<BsonDocument> decks;
    public const string DefaultGame = "default_game";

    public DBConnection(string dbConnectionString, string dbName)
    {
        client = new MongoClient(dbConnectionString);
        database = client.GetDatabase(dbName);
        decks = database.GetCollection<BsonDocument>("decks");
        var indexKeys = Builders<BsonDocument>.IndexKeys.Ascending(Constants.gameIndex);
        var indexOptions = new CreateIndexOptions { Unique = true };
        decks.Indexes.CreateOne(new CreateIndexModel<BsonDocument>(indexKeys, indexOptions));

        dBConnection = this;
    }

    public DeckState GetDeck(SocketMessage message)
    {
        var gameName = GetGameName(message);

        var filter = Builders<BsonDocument>.Filter.Eq(Constants.gameIndex, gameName);
        var document = decks.Find(filter).FirstOrDefault();

        if(document is null)
        {
            return new DeckState(gameName);
        }

        return BsonSerializer.Deserialize<DeckState>(document);
    }
    public bool TryGetDeck(SocketMessage message, out DeckState deck)
    {
        var gamename =  GetGameName(message);
        var result = TryGetDeck(gamename, out DeckState foundDeck);
        deck = foundDeck;
        return result;
    }

    public bool TryGetDeck(string gameName, out DeckState deck)
    {
        var filter = Builders<BsonDocument>.Filter.Eq(Constants.gameIndex, gameName);
        var document = decks.Find(filter).FirstOrDefault();
        if (document is null)
        {
            deck = null;
            return false;
        }
        deck = BsonSerializer.Deserialize<DeckState>(document);
        return true;
    }
    public void SaveDeck(DeckState deckState, SocketMessage message)
    {
        SaveDeck(deckState, GetGameName(message));
    }


    public void SaveDeck(DeckState deckState, String gameName)
    {
        // Create a filter to find a document by its ID
        var filter = Builders<BsonDocument>.Filter.Eq("_id", deckState.Id);
        var existingDocument = decks.Find(filter).FirstOrDefault();

        if (existingDocument is null)
        {
            // If the document doesn't exist, assign a new ObjectId
            deckState.Id = ObjectId.GenerateNewId();
            Console.WriteLine($"Inserting new document: {deckState.Id}");
            decks.InsertOne(deckState.ToBsonDocument());
        }
        else
        {
            // If the document exists, replace it
            Console.WriteLine($"Replacing existing document: {deckState.Id}");
            decks.ReplaceOne(filter, deckState.ToBsonDocument());
        }
    }

    public static string GetGameName(SocketMessage message)
    {
        if (message is not SocketUserMessage userMessage)
        {
            message.Channel.SendMessageAsync($"Bimg bong SocketUserMessage expected");
            throw new CommandEarlyExist();
        }

        ulong channelId = userMessage.Channel.Id;
        ulong? guildId = (userMessage.Channel as SocketTextChannel)?.Guild.Id;

        if(guildId is null)
        {
            message.Channel.SendMessageAsync($"No card peaking in DMs!");
            throw new CommandEarlyExist();
        }

        return $"{guildId}_{channelId}_deck";

    }

}


