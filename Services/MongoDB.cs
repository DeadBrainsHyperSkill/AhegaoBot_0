using System;
using MongoDB.Driver;
using MongoDB.Bson.Serialization.Attributes;

namespace DiscordBot.Services;

class MongoDB
{
    private readonly IMongoClient client;
    public readonly IMongoDatabase database;
    public readonly IMongoCollection<DiscordVoiceUser> voiceChannelsCollection;
    public readonly IMongoCollection<DiscordPurchasedVoiceChannel> purchasedVoiceChannelsCollection;

    public MongoDB(string connectionString)
    {
        client = new MongoClient(connectionString);
        database = client.GetDatabase("Ahegao");
        voiceChannelsCollection = database.GetCollection<DiscordVoiceUser>("VoiceChannels");
        purchasedVoiceChannelsCollection = database.GetCollection<DiscordPurchasedVoiceChannel>("PurchasedVoiceChannels");
    }

    public class DiscordVoiceUser
    {
        [BsonId]
        public Guid Id { get; set; }
        public string UserId { get; set; }
        public string CategoryId { get; set; }
        public string TextChannelId { get; set; }
        public string VoiceChannelId { get; set; }
        public DeafenedMutedUser[] DeafenedMutedUsers { get; set; }

        public class DeafenedMutedUser
        {
            public string UserId { get; set; }
            public bool EventTriggeredDeafen { get; set; }
            public bool EventTriggeredMute { get; set; }
            public bool IsDeafened { get; set; }
            public bool IsMuted { get; set; }
        }
    }

    public class DiscordPurchasedVoiceChannel
    {
        [BsonId]
        public int Id { get; set; }
        public string VoiceChannelId { get; set; }
    }
}


