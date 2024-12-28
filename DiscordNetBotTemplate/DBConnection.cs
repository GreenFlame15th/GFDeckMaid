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
        if(gameName == null)
        {
            return null;
        }

        var filter = Builders<BsonDocument>.Filter.Eq(Constants.gameIndex, gameName);
        var document = decks.Find(filter).FirstOrDefault();

        if(document is null)
        {
            return new DeckState(gameName);
        }

        return BsonSerializer.Deserialize<DeckState>(document);
    }

    public void SaveDeck(DeckState deckState, SocketMessage message)
    {
        var gameName = GetGameName(message);
        if (gameName == null)
        {
            return;
        }

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

    private string GetGameName(SocketMessage message)
    {
        if (message is not SocketUserMessage userMessage)
        {
            message.Channel.SendMessageAsync($"Bimg bong SocketUserMessage expected");
            return null;   
        }

        ulong channelId = userMessage.Channel.Id;
        ulong? guildId = (userMessage.Channel as SocketTextChannel)?.Guild.Id;

        if(guildId is null)
        {
            message.Channel.SendMessageAsync($"No card peaking in DMs!");
            return null;
        }

        return $"{guildId}_{channelId}_deck";

    }

}


