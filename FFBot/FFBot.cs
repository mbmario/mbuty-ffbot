using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Bot.Builder.AI.QnA;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Bot.Configuration;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;

namespace FFBot
{
    /// <summary>
    /// Represents a bot that processes incoming activities.
    /// For each user interaction, an instance of this class is created and the OnTurnAsync method is called.
    /// This is a Transient lifetime service.  Transient lifetime services are created
    /// each time they're requested. For each Activity received, a new instance of this
    /// class is created. Objects that are expensive to construct, or have a lifetime
    /// beyond the single turn, should be carefully managed.E
    /// For example, the <see cref="MemoryStorage"/> object and associated
    /// <see cref="IStatePropertyAccessor{T}"/> object are created with a singleton lifetime.
    /// </summary>
    /// <seealso cref="https://docs.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-2.1"/>
    public class FFBot : IBot
    {

        private const string MainDialogId = "mainDialog";
        private const string FFHelloDialogId = "FFHelloDialog";
        private const string FFComplaintDialogId = "FFComplaintDialog";
        private const string FFQuestionDialogId = "FFQuestionDialog";
        private const string FFWDISDialogId = "FFWDISDialog";

        public static readonly string QnAMakerKey = "FFQnA";

        private DialogSet _dialogs;

        private readonly FFBotAccessors _accessors;
        private readonly ILogger _logger;
        private readonly BotServices _services;

        public FFBot(FFBotAccessors accessors, ILoggerFactory loggerFactory, BotServices services)
        {
            if (loggerFactory == null)
            {
                throw new System.ArgumentNullException(nameof(loggerFactory));
            }

            _logger = loggerFactory.CreateLogger<FFBot>();
            _logger.LogTrace("ffbot turn start.");
            _accessors = accessors ?? throw new System.ArgumentNullException(nameof(accessors));
            _services = services ?? throw new System.ArgumentNullException(nameof(services));

            if (!_services.QnAServices.ContainsKey(QnAMakerKey))
            {
                throw new System.ArgumentException($"Invalid configuration. Please check your '.bot' file for a QnA service named '{QnAMakerKey}'.");
            }

            // Define the steps of the main dialog.
            WaterfallStep[] steps = new WaterfallStep[]
            {
                MenuStepAsync,
                HandleChoiceAsync,
                LoopBackAsync,
            };

            // Create our bot's dialog set, adding a main dialog and the three component dialogs.
            _dialogs = new DialogSet(_accessors.DialogStateAccessor)
                .Add(new WaterfallDialog(MainDialogId, steps))
                .Add(new FFComplaintDialog(FFComplaintDialogId))
                .Add(new FFHelloDialog(FFHelloDialogId))
                .Add(new FFQuestionDialog(FFQuestionDialogId))
                .Add(new FFWDISDialog(FFWDISDialogId));
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
                // Establish dialog state from the conversation state.
                DialogContext dc = await _dialogs.CreateContextAsync(turnContext, cancellationToken);

                // Get the user's info.
                UserProfile userInfo = await _accessors.UserProfile.GetAsync(turnContext, () => new UserProfile(), cancellationToken);

                // QNA CASE: must be intercepted here because it doesn't seem to go in the Dialog
                // Check QnA Maker model
                var response = await _services.QnAServices[QnAMakerKey].GetAnswersAsync(turnContext);
                if (response != null && response.Length > 0)
                {
                    await turnContext.SendActivityAsync(response[0].Answer, cancellationToken: cancellationToken);
                }
                else
                {
                    var msg = @"Sorry, No QnA Maker answers were found.";
                    await turnContext.SendActivityAsync(msg, cancellationToken: cancellationToken);
                }

                // ONGOING DIALOG CASE: Continue any current dialog.
                DialogTurnResult dialogTurnResult = await dc.ContinueDialogAsync();

                // COMPLETED DIALOG CASE: last result was EndDialogAsync, process the result of any complete dialog
                if (dialogTurnResult.Status is DialogTurnStatus.Complete)
                {
                    switch (dialogTurnResult.Result)
                    {
                        case UserProfile upResult:
                            // Store the results of FFHelloDialog
                            await _accessors.UserProfile.SetAsync(turnContext, upResult, cancellationToken);
                            //await _accessors.UserProfile.SetAsync(turnContext, upResult);

                            // now start our bot's main dialog.
                            await dc.BeginDialogAsync(MainDialogId, null, cancellationToken);
                            break;
                        default:
                            // We shouldn't get here, since the main dialog is designed to loop.
                            break;
                    }
                }

                // INACTIVE DIALOG CASE: Every dialog step sends a response, so if no response was sent,
                //      then no dialog is currently active.
                else if (!turnContext.Responded)
                {
                    if (string.IsNullOrEmpty(userInfo.UserName)) //string.IsNullOrEmpty(userInfo.Guest?.Name))
                    {
                        // If we don't yet have the guest's info, start the check-in dialog.
                        await dc.BeginDialogAsync(FFHelloDialogId, null, cancellationToken);
                    }
                    else
                    {
                        // Otherwise, start our bot's main dialog.
                        await dc.BeginDialogAsync(MainDialogId, null, cancellationToken);
                    }
                }

                // Save the new turn count into the conversation state.
                await _accessors.ConversationState.SaveChangesAsync(turnContext, false, cancellationToken);
                await _accessors.UserState.SaveChangesAsync(turnContext, false, cancellationToken);
            }
            else
            {
                //await turnContext.SendActivityAsync($"{turnContext.Activity.Type} event detected");
            }
        }



        private static async Task<DialogTurnResult> MenuStepAsync(
            WaterfallStepContext stepContext,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            // Present the user with a set of "suggested actions".
            List<string> menu = new List<string> { "Whom Do I Start?", "Ask a Question", "Suggestion or Complaint" };
            await stepContext.Context.SendActivityAsync(
                MessageFactory.SuggestedActions(menu, "How can I help you?"),
                cancellationToken: cancellationToken);

            //var optionsCard = CreateOptionsCardAttachment();
            //var response = CreateResponse(turnContext.Activity, optionsCard);   
            //await stepContext.Context.SendActivityAsync(response);

            return Dialog.EndOfTurn;
        }

        private async Task<DialogTurnResult> HandleChoiceAsync(
            WaterfallStepContext stepContext,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            // Get the user's info. (Since the type factory is null, this will throw if state does not yet have a value for user info.)
            UserProfile userInfo = await _accessors.UserProfile.GetAsync(stepContext.Context, null, cancellationToken);

            // Check the user's input and decide which dialog to start.
            // Pass in the guest info when starting either of the child dialogs.
            string choice = (stepContext.Result as string)?.Trim()?.ToLowerInvariant();
            switch (choice)
            {
                case "Whom Do I Start?":
                    return await stepContext.BeginDialogAsync(FFWDISDialogId, userInfo.UserName, cancellationToken);

                case "Ask a Question":
                    // exists now outside of dialog space
                    return await stepContext.BeginDialogAsync(FFQuestionDialogId, userInfo.UserName, cancellationToken);

                case "Suggestion or Complaint":
                    return await stepContext.BeginDialogAsync(FFComplaintDialogId, userInfo.UserName, cancellationToken);
                default:
                    // If user does something weird, start again from the beginning.
                    await stepContext.Context.SendActivityAsync(
                        "Sorry, I don't understand that command. Please choose an option from the list.");
                    return await stepContext.ReplaceDialogAsync(MainDialogId, null, cancellationToken);
            }
        }

        private async Task<DialogTurnResult> LoopBackAsync(
            WaterfallStepContext stepContext,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            // Get the user's info. (Because the type factory is null, this will throw if state does not yet have a value for user info.)
            UserProfile userInfo = await _accessors.UserProfile.GetAsync(stepContext.Context, null, cancellationToken);

            // Process the return value from the child dialog.
            switch (stepContext.Result)
            {
                // if we were to store something, it would be here
                default:
                    break;



            }

            // Restart the main menu dialog.
            return await stepContext.ReplaceDialogAsync(MainDialogId, null, cancellationToken);
        }

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
