using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;

namespace FFBot
{
    public class FFQuestionDialog : ComponentDialog
    {
        private const string UserKey = nameof(FFHelloDialog);
        private const string TextPrompt = "textPrompt";

        // You can start this from the parent using the dialog's ID.
        public FFQuestionDialog(string id)
            : base(id)
        {
            InitialDialogId = Id;

            // Define the prompts used in this conversation flow.
            AddDialog(new TextPrompt(TextPrompt));

            // Define the conversation flow using a waterfall model.
            WaterfallStep[] waterfallSteps = new WaterfallStep[]
            {
            FinalStepAsync,
            };
            AddDialog(new WaterfallDialog(Id, waterfallSteps));
        }

        private static async Task<DialogTurnResult> FinalStepAsync(
        WaterfallStepContext step,
        CancellationToken cancellationToken = default(CancellationToken))
        {
            // only have one step, because of the difficulty of passing service/key parameters
            await step.Context.SendActivityAsync(
                "What is your Question?",
                cancellationToken: cancellationToken);

            return await step.EndDialogAsync(cancellationToken);
        }
    }
}
