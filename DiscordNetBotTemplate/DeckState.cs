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
        //game state
        public Dictionary<string, Player> players;
        public List<int> deck, discard, dominance, trim;

        public DeckState(string gameName)
        {
            game = gameName;
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            players ??= new Dictionary<string, Player>();
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
        public List<int> cards, crafted;

        public Player()
        {
            cards = new List<int>();
            crafted = new List<int>();
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            cards ??= new List<int>();
        }
    }
}
