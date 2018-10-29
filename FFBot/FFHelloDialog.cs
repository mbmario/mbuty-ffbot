using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;

namespace FFBot
{
    public class FFHelloDialog : ComponentDialog
    {
        private const string UserKey = nameof(FFHelloDialog);
        private const string TextPrompt = "textPrompt";

        // You can start this from the parent using the dialog's ID.
        public FFHelloDialog(string id)
            : base(id)
        {
            InitialDialogId = Id;

            // Define the prompts used in this conversation flow.
            AddDialog(new TextPrompt(TextPrompt));

            // Define the conversation flow using a waterfall model.
            WaterfallStep[] waterfallSteps = new WaterfallStep[]
            {
            NameStepAsync,
            TeamStepAsync,
            FinalStepAsync,
            };
            AddDialog(new WaterfallDialog(Id, waterfallSteps));
        }

        private static async Task<DialogTurnResult> NameStepAsync(
        WaterfallStepContext step,
        CancellationToken cancellationToken = default(CancellationToken))
        {
        // Clear the guest information and prompt for the user's name.
        step.Values[UserKey] = new UserProfile();
        return await step.PromptAsync(
            TextPrompt,
            new PromptOptions
            {
                Prompt = MessageFactory.Text("What is your name?"),
            },
            cancellationToken);
        }

        private static async Task<DialogTurnResult> TeamStepAsync(
        WaterfallStepContext step,
        CancellationToken cancellationToken = default(CancellationToken))
        {
            // Save the name and prompt for the team name.
            string name = step.Result as string;
            ((UserProfile)step.Values[UserKey]).UserName = name;
            return await step.PromptAsync(
                TextPrompt,
                new PromptOptions
                {
                    Prompt = MessageFactory.Text($"Hi {name}. What is your team name?"),
                },
                cancellationToken);
        }

        private static async Task<DialogTurnResult> FinalStepAsync(
        WaterfallStepContext step,
        CancellationToken cancellationToken = default(CancellationToken))
        {
            // Save the room number and "sign off".
            string team = step.Result as string;
            ((UserProfile)step.Values[UserKey]).TeamName = team;

            await step.Context.SendActivityAsync(
                "That's a very clever name!",
                cancellationToken: cancellationToken);

            // End the dialog, returning the guest info.
            return await step.EndDialogAsync(
                (UserProfile)step.Values[UserKey],
                cancellationToken);
        }
    }
}
