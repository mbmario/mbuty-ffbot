// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Bot_Builder_Echo_Bot_V4
{
    /// <summary>
    /// Represents a bot that processes incoming activities.
    /// For each user interaction, an instance of this class is created and the OnTurnAsync method is called.
    /// This is a Transient lifetime service.  Transient lifetime services are created
    /// each time they're requested. For each Activity received, a new instance of this
    /// class is created. Objects that are expensive to construct, or have a lifetime
    /// beyond the single turn, should be carefully managed.
    /// For example, the <see cref="MemoryStorage"/> object and associated
    /// <see cref="IStatePropertyAccessor{T}"/> object are created with a singleton lifetime.
    /// </summary>
    /// <seealso cref="https://docs.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-2.1"/>
    public class FFBot : IBot
    {
        private readonly EchoBotAccessors _accessors;
        private readonly ILogger _logger;
        private DialogSet _dialogs;

        public FFBot(EchoBotAccessors accessors, ILoggerFactory loggerFactory)
        {
            if (loggerFactory == null)
            {
                throw new System.ArgumentNullException(nameof(loggerFactory));
            }

            _logger = loggerFactory.CreateLogger<FFBot>();
            _logger.LogTrace("ffbot turn start.");
            _accessors = accessors ?? throw new System.ArgumentNullException(nameof(accessors));
        }

        // Every conversation turn for our Echo Bot will call this method.
        public async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (turnContext == null) 
            {
                throw new ArgumentNullException(nameof(turnContext));
            }

            // Handle Message activity type, which is the main activity type for shown within a conversational interface
            if (turnContext.Activity.Type == ActivityTypes.Message)
            {
                // get states
                var convo = await _accessors.TopicState.GetAsync(turnContext, () => new TopicState());
                var user = await _accessors.UserProfile.GetAsync(turnContext, () => new UserProfile());

                // get prev response
                var text = turnContext.Activity.Text;

                //// TODO: shortcuts in case of card clicks
                //if (text == "WDIS" || text == "Complaint")
                //{
                //    convo.Stage = text;
                //    // save state
                //    await _accessors.TopicState.SetAsync(turnContext, convo);
                //    await _accessors.ConversationState.SaveChangesAsync(turnContext);
                //}

                // beginning round of questions to gather info
                if (convo.Stage == "intro")
                { 
                    if (convo.Prompt == "hi")
                    {
                        await turnContext.SendActivityAsync("Hello! I'm the fot. Who are you?");

                        // Set the Prompt to ask the next question for this conversation
                        convo.Prompt = "askTeam";

                        // save states
                        await _accessors.TopicState.SetAsync(turnContext, convo);
                        await _accessors.ConversationState.SaveChangesAsync(turnContext);
                    }
                    else if (convo.Prompt == "askTeam")
                    {
                        // store intro's prompt's user
                        user.UserName=text;

                        // Use the user name to prompt the user for team name
                        await turnContext.SendActivityAsync($"Hello, {user.UserName}. What's your team name?");

                        // Set the Prompt to ask the next question for this conversation
                        convo.Stage = "menu";
                        convo.Prompt = "ask";

                        // update states
                        await _accessors.TopicState.SetAsync(turnContext, convo);
                        await _accessors.ConversationState.SaveChangesAsync(turnContext);

                        await _accessors.UserProfile.SetAsync(turnContext, user);
                        await _accessors.UserState.SaveChangesAsync(turnContext);

                    }

                }

                // show menu in ask, process it in switch
                else if (convo.Stage == "menu")
                {
                    if (convo.Prompt == "ask")
                    {
                        // grab team name from previous
                        user.TeamName = text;

                        string teamStatement = $"{text} is a very clever name. ";
                        await turnContext.SendActivityAsync(teamStatement);

                        var optionsCard = CreateOptionsCardAttachment();
                        var response = CreateResponse(turnContext.Activity, optionsCard);

                        await turnContext.SendActivityAsync(response).ConfigureAwait(false);

                        convo.Prompt = "fork";

                        // update states
                        await _accessors.TopicState.SetAsync(turnContext, convo);
                        await _accessors.ConversationState.SaveChangesAsync(turnContext);

                        await _accessors.UserProfile.SetAsync(turnContext, user);
                        await _accessors.UserState.SaveChangesAsync(turnContext);

                    }
                    else if (convo.Prompt == "fork")
                    {
                        // grab suggested actions from previous. Set that as the stage
                        var choice = text;
                        convo.Stage = choice;
                        convo.Prompt = "1";

                        // save state
                        await _accessors.TopicState.SetAsync(turnContext, convo);
                        await _accessors.ConversationState.SaveChangesAsync(turnContext);

                        if (choice == "WDIS")
                        {
                            await turnContext.SendActivityAsync($"Which two players are you choosing between?");
                        }
                        else if (choice == "Complaint")
                        {
                            await turnContext.SendActivityAsync($"Ok! What do you have to say?");
                        }
                        else if (choice == "Question")
                        {
                            await turnContext.SendActivityAsync($"");
                        }
                        else
                        {
                            await turnContext.SendActivityAsync($"Tell me a little bit about yourself.");
                            convo.Stage = "Lonely";

                            // save default state
                            await _accessors.TopicState.SetAsync(turnContext, convo);
                            await _accessors.ConversationState.SaveChangesAsync(turnContext);

                        }

                    }
                }
                else if (convo.Stage == "WDIS")
                {

                    string player1 = "";
                    string player2 = "";

                    if (text.Contains("or"))
                    {
                        player1 = Regex.Split(text, " or ")[0];
                        player2 = Regex.Split(text, " or ")[1];
                    }
                    else if (text.Contains(" and "))
                    {
                        player1 = Regex.Split(text, " and ")[0];
                        player2 = Regex.Split(text, " and ")[1];
                    }

                    if (text.Contains(" or ") || text.Contains(" and "))
                    {
                        string[] players = { player1, player2 };
                        Random ran = new Random();
                        string choice = players[ran.Next(0, players.Length)];
                        await turnContext.SendActivityAsync($"Start {choice}.");

                        convo.Stage = "";
                        await _accessors.TopicState.SetAsync(turnContext, convo);
                        await _accessors.ConversationState.SaveChangesAsync(turnContext);
                    }
                    else
                    {
                        await turnContext.SendActivityAsync($"Sorry, I didn't get that.");

                    }
                }
                else if (convo.Stage == "Complaint")
                {
                    await turnContext.SendActivityAsync($"Thank you for your input.");
                    convo.Stage = "";
                    await _accessors.TopicState.SetAsync(turnContext, convo);
                    await _accessors.ConversationState.SaveChangesAsync(turnContext);
                }
                else if (convo.Stage == "Question")
                {
                    await turnContext.SendActivityAsync($"This feature coming soon.");
                    convo.Stage = "";
                    await _accessors.TopicState.SetAsync(turnContext, convo);
                    await _accessors.ConversationState.SaveChangesAsync(turnContext);
                }
                else
                {
                    string[] responses = { "Interesting, tell me more", "Why is that?", "How cool", "When did that start?" };
                    Random ran = new Random();
                    string reply = responses[ran.Next(0,responses.Length)];
                    await turnContext.SendActivityAsync(reply);
                }
            }
        }

        //// Creates and sends an activity with suggested actions to the user. 
        //private static async Task SendSuggestedActionsAsync(ITurnContext turnContext, CancellationToken cancellationToken, String teamStatement)
        //{
        //    var reply = turnContext.Activity.CreateReply($"{teamStatement} What can I help you with?");

        //    reply.SuggestedActions = new SuggestedActions()
        //    {
        //        Actions = new List<CardAction>()
        //        {
        //            new CardAction() { Title = "Which player should I start?", Type = ActionTypes.ImBack, Value = "WDIS" },
        //            new CardAction() { Title = "I'd like to file a complaint or make a suggestion.", Type = ActionTypes.ImBack, Value = "Complaint" },
        //            new CardAction() { Title = "I have a specific question.", Type = ActionTypes.ImBack, Value = "Question" },
        //            new CardAction() { Title = "I just want someone to talk to", Type = ActionTypes.ImBack, Value = "Lonely" },
        //        },
        //    };
        //    await turnContext.SendActivityAsync(reply, cancellationToken);
        //}

        // load attachment from file

        private Attachment CreateOptionsCardAttachment()
        {
            var adaptiveCard = File.ReadAllText(@".\Resources\optionsCard.json");
            return new Attachment()
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = JsonConvert.DeserializeObject(adaptiveCard),
            };
        }

        // Create an attachment message response.
        private Activity CreateResponse(Activity activity, Attachment attachment)
        {
            var response = activity.CreateReply();
            response.Attachments = new List<Attachment>() { attachment };
            return response;
        }
    }
}
