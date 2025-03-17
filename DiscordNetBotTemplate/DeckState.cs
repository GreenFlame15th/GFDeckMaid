using Discord.WebSocket;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace GFDeckMaid
{
    [Serializable]
    public class DeckState
    {
        //config
        [BsonId]
        public ObjectId Id;
        public string game;
        public int columns, rows, cardCount;
        public string imageLink;
        public List<int> dominanceMark;
        [BsonIgnoreIfNull]
        public Dictionary<string, Player> players;
        [BsonIgnoreIfNull]
        public Dictionary<string, string> plots;
        public List<int> deck, discard, dominance, trim, slotSouls;
        public bool lostSoulsOn;

        public DeckState(string gameName)
        {
            game = gameName;
            deck = new();
            discard = new();
            dominance = new();
            trim = new();
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            players ??= new Dictionary<string, Player>();
            plots ??= new Dictionary<string, string>();
        }

        public Player GetPlayer(SocketMessage message)
        => GetPlayer(message.Author.Id);
        public Player GetPlayer(ulong id)
        => GetPlayer(id.ToString());

        public Player GetPlayer(string id)
        {
            players ??= new Dictionary<string, Player>();
            if (!players.TryGetValue(id, out var player))
            {
                player = new Player();
                players[id] = player;
            }

            return player;
        }
    }

    [Serializable]
    public class Player
    {
        public List<int> hand, crafted, facedown, faceup;

        public Player()
        {
            hand = new();
            crafted = new();
            facedown = new();
            faceup = new();
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            hand ??= new List<int>();
        }
    }
}
