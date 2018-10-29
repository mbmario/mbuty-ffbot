using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;

namespace FFBot
{
    public class FFComplaintDialog : ComponentDialog
    {
        private const string UserKey = nameof(FFHelloDialog);
        private const string TextPrompt = "textPrompt";
        private readonly FFBotAccessors _accessors;

        // You can start this from the parent using the dialog's ID.
        public FFComplaintDialog(string id)
            : base(id)
        {
            InitialDialogId = Id;

            // Define the prompts used in this conversation flow.
            AddDialog(new TextPrompt(TextPrompt));

            // Define the conversation flow using a waterfall model.
            WaterfallStep[] waterfallSteps = new WaterfallStep[]
            {
            AskStepAsync,
            FinalStepAsync,
            };
            AddDialog(new WaterfallDialog(Id, waterfallSteps));
        }

        private static async Task<DialogTurnResult> AskStepAsync(
        WaterfallStepContext step,
        CancellationToken cancellationToken = default(CancellationToken))
        {

        return await step.PromptAsync(
            TextPrompt,
            new PromptOptions
            {
                Prompt = MessageFactory.Text("Ok! What are your thoughts?"),
            },
            cancellationToken);
        }


        private static async Task<DialogTurnResult> FinalStepAsync(
        WaterfallStepContext step,
        CancellationToken cancellationToken = default(CancellationToken))
        {
            // TODO: call the accessor to get user name, comforting the user

            await step.Context.SendActivityAsync(
                "That's a valid point, we will take that into consideration.",
                cancellationToken: cancellationToken);

            return await step.EndDialogAsync(cancellationToken);
        }
    }
}
