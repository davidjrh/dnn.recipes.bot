#r "Newtonsoft.Json"
#load "RootDialog.csx"

using System;
using System.Net;
using System.Threading;
using Newtonsoft.Json;

using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Internals;
using Microsoft.Bot.Connector;

public static async Task<object> Run(HttpRequestMessage req, TraceWriter log)
{
    log.Info($"Webhook was triggered!");

    // Initialize the azure bot
    using (BotService.Initialize())
    {
        // Deserialize the incoming activity
        string jsonContent = await req.Content.ReadAsStringAsync();
        var activity = JsonConvert.DeserializeObject<Activity>(jsonContent);
        
        // authenticate incoming request and add activity.ServiceUrl to MicrosoftAppCredentials.TrustedHostNames
        // if request is authenticated
        if (!await BotService.Authenticator.TryAuthenticateAsync(req, new [] {activity}, CancellationToken.None))
        {
            return BotAuthenticator.GenerateUnauthorizedResponse(req);
        }
        
        if (activity != null)
        {
            // one of these will have an interface and process it
            switch (activity.GetActivityType())
            {
                case ActivityTypes.Message:
                    using (var scope = DialogModule.BeginLifetimeScope(Conversation.Container, activity))
                    {
                        if (activity.Text != null)
                        {
                            var msg = activity.Text.ToLowerInvariant().Trim();
                            if (msg == "start over" || msg == "exit" || msg == "quit" || msg == "done" || msg == "start again" || msg == "restart" || msg == "leave" || msg == "reset")
                            {  
                                //var botData = scope.Resolve<IBotData>();
                                //await botData.LoadAsync(default(CancellationToken));
                                //await botData.FlushAsync(default(CancellationToken));
                                //var stack = scope.Resolve<IDialogStack>();
                                //stack.Reset();
                            }
                        }

                        await Conversation.SendAsync(activity, () => new RootDialog());
                    }

                    //await Conversation.SendAsync(activity, () => new RootDialog());
                    break;
                case ActivityTypes.ConversationUpdate:
                    var client = new ConnectorClient(new Uri(activity.ServiceUrl));
                    IConversationUpdateActivity update = activity;
                    if (update.MembersAdded.Any())
                    {
                        var reply = activity.CreateReply();
                        var newMembers = update.MembersAdded?.Where(t => t.Id != activity.Recipient.Id);
                        foreach (var newMember in newMembers)
                        {
                            reply.Text = "Welcome";
                            if (!string.IsNullOrEmpty(newMember.Name))
                            {
                                reply.Text += $" {newMember.Name}";
                            }
                            reply.Text += "!";
                            await client.Conversations.ReplyToActivityAsync(reply);
                        }
                    }
                    break;
                case ActivityTypes.ContactRelationUpdate:
                case ActivityTypes.Typing:
                case ActivityTypes.DeleteUserData:
                case ActivityTypes.Ping:
                default:
                    log.Error($"Unknown activity type ignored: {activity.GetActivityType()}");
                    break;
            }
        }
        return req.CreateResponse(HttpStatusCode.Accepted);
    }    
}
