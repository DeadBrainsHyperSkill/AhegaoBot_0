using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using MongoDB.Driver;
using Discord;
using Discord.WebSocket;

namespace DiscordBot.Services;

class CommandsService
{

    private static readonly MongoDB mongoDB = new("mongodb://localhost:27017");

    public static async Task AddDefaultRoles(SocketGuildUser socketGuildUser)
    {
        await socketGuildUser.AddRolesAsync(new IRole[] { socketGuildUser.Guild.GetRole(836975520151371826), socketGuildUser.Guild.GetRole(839975478655975474) });
    }

    public static async Task CheckUserDeafenedOrMuted(SocketGuildUser user, SocketVoiceState voiceState, SocketVoiceState voiceStatePrevious)
    {
        try
        {
            if (voiceStatePrevious.VoiceChannel != null && voiceStatePrevious.VoiceChannel.Id != voiceState.VoiceChannel?.Id)
            {
                var filter2 = Builders<MongoDB.DiscordVoiceUser>.Filter.ElemMatch(x => x.DeafenedMutedUsers, x => x.UserId == user.Id.ToString()) & Builders<MongoDB.DiscordVoiceUser>.Filter.Eq("VoiceChannelId", voiceStatePrevious.VoiceChannel.Id.ToString());
                var update2 = Builders<MongoDB.DiscordVoiceUser>.Update.Set(x => x.DeafenedMutedUsers[-1].EventTriggeredDeafen, false);
                mongoDB.voiceChannelsCollection.UpdateOne(filter2, update2);
                var update3 = Builders<MongoDB.DiscordVoiceUser>.Update.Set(x => x.DeafenedMutedUsers[-1].EventTriggeredMute, false);
                mongoDB.voiceChannelsCollection.UpdateOne(filter2, update3);
            }
            if (voiceState.VoiceChannel == null || voiceState.VoiceChannel.Id == 834432875406426144)
            {
                return;
            }
            var filter = Builders<MongoDB.DiscordVoiceUser>.Filter.Eq("VoiceChannelId", voiceState.VoiceChannel.Id.ToString()) & Builders<MongoDB.DiscordVoiceUser>.Filter.ElemMatch(x => x.DeafenedMutedUsers, x => x.UserId == user.Id.ToString());
            var document = mongoDB.voiceChannelsCollection.Find(filter).FirstOrDefault();
            var update = Builders<MongoDB.DiscordVoiceUser>.Update.Set(x => x.DeafenedMutedUsers[-1].EventTriggeredDeafen, true);
            if (document == null)
            {
                var filter3 = Builders<MongoDB.DiscordVoiceUser>.Filter.Eq("VoiceChannelId", voiceState.VoiceChannel.Id.ToString());
                var filterCheck = filter3 & Builders<MongoDB.DiscordVoiceUser>.Filter.AnyEq(x => x.DeafenedMutedUsers, null);
                if (mongoDB.voiceChannelsCollection.Find(filterCheck).FirstOrDefault() != null)
                {
                    update = Builders<MongoDB.DiscordVoiceUser>.Update.Set("DeafenedMutedUsers", new List<MongoDB.DiscordVoiceUser>());
                    mongoDB.voiceChannelsCollection.UpdateOne(filterCheck, update);
                }
                update = Builders<MongoDB.DiscordVoiceUser>.Update.Push(x => x.DeafenedMutedUsers, new MongoDB.DiscordVoiceUser.DeafenedMutedUser() { UserId = user.Id.ToString() });
                document = mongoDB.voiceChannelsCollection.FindOneAndUpdate(filter3, update, new FindOneAndUpdateOptions<MongoDB.DiscordVoiceUser, MongoDB.DiscordVoiceUser> { ReturnDocument = ReturnDocument.After });
            }
            var deafenedMutedUser = document.DeafenedMutedUsers.First(x => x.UserId == user.Id.ToString());
            if (deafenedMutedUser.EventTriggeredDeafen == false && deafenedMutedUser.IsDeafened)
            {
                update = Builders<MongoDB.DiscordVoiceUser>.Update.Set(x => x.DeafenedMutedUsers[-1].EventTriggeredDeafen, true);
                mongoDB.voiceChannelsCollection.UpdateOne(filter, update);
                await user.ModifyAsync(func => func.Deaf = true);
                return;
            }
            else if (deafenedMutedUser.EventTriggeredDeafen == false && !deafenedMutedUser.IsDeafened)
            {
                update = Builders<MongoDB.DiscordVoiceUser>.Update.Set(x => x.DeafenedMutedUsers[-1].EventTriggeredDeafen, true);
                mongoDB.voiceChannelsCollection.UpdateOne(filter, update);
                await user.ModifyAsync(func => func.Deaf = false);
                return;
            }
            if (deafenedMutedUser.EventTriggeredMute == false && deafenedMutedUser.IsMuted)
            {
                update = Builders<MongoDB.DiscordVoiceUser>.Update.Set(x => x.DeafenedMutedUsers[-1].EventTriggeredMute, true);
                mongoDB.voiceChannelsCollection.UpdateOne(filter, update);
                await user.ModifyAsync(func => func.Mute = true);
                return;
            }
            else if (deafenedMutedUser.EventTriggeredMute == false && !deafenedMutedUser.IsMuted)
            {
                update = Builders<MongoDB.DiscordVoiceUser>.Update.Set(x => x.DeafenedMutedUsers[-1].EventTriggeredMute, true);
                mongoDB.voiceChannelsCollection.UpdateOne(filter, update);
                await user.ModifyAsync(func => func.Mute = false);
                return;
            }
            if (voiceState.IsDeafened)
            {
                update = Builders<MongoDB.DiscordVoiceUser>.Update.Set(x => x.DeafenedMutedUsers[-1].IsDeafened, true);
                mongoDB.voiceChannelsCollection.UpdateOne(filter, update);
            }
            else
            {
                update = Builders<MongoDB.DiscordVoiceUser>.Update.Set(x => x.DeafenedMutedUsers[-1].IsDeafened, false);
                mongoDB.voiceChannelsCollection.UpdateOne(filter, update);
            }
            if (voiceState.IsMuted)
            {
                update = Builders<MongoDB.DiscordVoiceUser>.Update.Set(x => x.DeafenedMutedUsers[-1].IsMuted, true);
                mongoDB.voiceChannelsCollection.UpdateOne(filter, update);
            }
            else
            {
                update = Builders<MongoDB.DiscordVoiceUser>.Update.Set(x => x.DeafenedMutedUsers[-1].IsMuted, false);
                mongoDB.voiceChannelsCollection.UpdateOne(filter, update);
            }
            if (voiceState.IsDeafened && voiceState.IsMuted)
            {
                var overwritePermissions = OverwritePermissions.DenyAll(voiceState.VoiceChannel.Category).Modify(viewChannel: PermValue.Allow, readMessageHistory: PermValue.Allow, useVoiceActivation: PermValue.Allow);
                await voiceState.VoiceChannel.AddPermissionOverwriteAsync(user, overwritePermissions);
                await user.Guild.GetTextChannel(Convert.ToUInt64(document.TextChannelId)).AddPermissionOverwriteAsync(user, overwritePermissions);
                update = Builders<MongoDB.DiscordVoiceUser>.Update.PullFilter(x => x.DeafenedMutedUsers, p => p.UserId == user.Id.ToString());
                mongoDB.voiceChannelsCollection.UpdateOne(filter, update);
            }
        }
        catch (Exception)
        {
            return;
        }
    }

    public static async Task CreateDeleteChannels(SocketGuildUser user, SocketVoiceState voiceState, SocketVoiceState voiceStatePrevious)
    {
        if (voiceState.VoiceChannel == null)
        {
            var document0 = mongoDB.purchasedVoiceChannelsCollection.Find(
                Builders<MongoDB.DiscordPurchasedVoiceChannel>.Filter.Eq("VoiceChannelId", voiceStatePrevious.VoiceChannel.Id.ToString())).FirstOrDefault();

            if (voiceStatePrevious.VoiceChannel.Users.Count == 0 && voiceStatePrevious.VoiceChannel.Id != 834432875406426144 && document0 == null)
            {
                var filter = Builders<MongoDB.DiscordVoiceUser>.Filter.Eq("VoiceChannelId", voiceStatePrevious.VoiceChannel.Id.ToString());
                var document = mongoDB.voiceChannelsCollection.Find(filter).First();
                await user.Guild.GetTextChannel(Convert.ToUInt64(document.TextChannelId)).DeleteAsync();
                await user.Guild.GetVoiceChannel(Convert.ToUInt64(document.VoiceChannelId)).DeleteAsync();
                await user.Guild.GetCategoryChannel(Convert.ToUInt64(document.CategoryId)).DeleteAsync();
                mongoDB.voiceChannelsCollection.DeleteOne(filter);
            }
            return;
        }
        if (voiceState.VoiceChannel.Id == 834432875406426144)
        {
            var category = await user.Guild.CreateCategoryChannelAsync(user.Username);
            var voice = await user.Guild.CreateVoiceChannelAsync("『🎤』голос", func =>
            {
                func.CategoryId = category.Id;
            });
            await user.ModifyAsync(x => x.Channel = voice);
            var text = await user.Guild.CreateTextChannelAsync($"『💬』сообщения", func =>
            {
                func.CategoryId = category.Id;
                func.Topic = $"**Личный текстовый канал <@!{user.Id}>**";
            });
            mongoDB.voiceChannelsCollection.InsertOne(new MongoDB.DiscordVoiceUser() { UserId = user.Id.ToString(), CategoryId = category.Id.ToString(), TextChannelId = text.Id.ToString(), VoiceChannelId = voice.Id.ToString() });
            await voice.AddPermissionOverwriteAsync(user, OverwritePermissions.AllowAll(voice));
            await text.AddPermissionOverwriteAsync(user, OverwritePermissions.AllowAll(voice));
        }
        if (voiceStatePrevious.VoiceChannel != null)
        {
            var filter0 = Builders<MongoDB.DiscordPurchasedVoiceChannel>.Filter.Eq("VoiceChannelId", voiceStatePrevious.VoiceChannel.Id.ToString());
            var document0 = mongoDB.purchasedVoiceChannelsCollection.Find(filter0).FirstOrDefault();
            if (voiceStatePrevious.VoiceChannel.Users.Count == 0 && voiceStatePrevious.VoiceChannel.Id != 834432875406426144 && document0 == null)
            {
                var filter = Builders<MongoDB.DiscordVoiceUser>.Filter.Eq("VoiceChannelId", voiceStatePrevious.VoiceChannel.Id.ToString());
                var document = mongoDB.voiceChannelsCollection.Find(filter).First();
                await user.Guild.GetTextChannel(Convert.ToUInt64(document.TextChannelId)).DeleteAsync();
                await user.Guild.GetVoiceChannel(Convert.ToUInt64(document.VoiceChannelId)).DeleteAsync();
                await user.Guild.GetCategoryChannel(Convert.ToUInt64(document.CategoryId)).DeleteAsync();
                mongoDB.voiceChannelsCollection.DeleteOne(filter);
            }
        }
    }

    public static void InteractionWithBot(string userId, string voiceChannelId)
    {
        mongoDB.purchasedVoiceChannelsCollection.InsertOne(new MongoDB.DiscordPurchasedVoiceChannel() { VoiceChannelId = voiceChannelId });
        mongoDB.voiceChannelsCollection.InsertOne(new MongoDB.DiscordVoiceUser() { UserId = userId, VoiceChannelId = voiceChannelId });
    }
}
