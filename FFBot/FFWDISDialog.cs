using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace FFBot
{
    public class FFWDISDialog : ComponentDialog
    {
        private const string UserKey = nameof(FFHelloDialog);
        private const string TextPrompt = "textPrompt";
        private const string resxFlexFull = @".\flex_full_names.resx";
        private const string resxQBFull = @".\QB_full_names.resx";
        private const string resxMisc = @".\misc_d_st.resx";

        // You can start this from the parent using the dialog's ID.
        public FFWDISDialog(string id)
            : base(id)
        {
            InitialDialogId = Id;

            // Define the prompts used in this conversation flow.
            AddDialog(new TextPrompt(TextPrompt));

            // Define the conversation flow using a waterfall model.
            WaterfallStep[] waterfallSteps = new WaterfallStep[]
            {
            AskStepAsync,
            DivinationStepAsync,
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
                    Prompt = MessageFactory.Text($"Which two players are you choosing between?"),
                },
                cancellationToken);
        }

        private static async Task<DialogTurnResult> DivinationStepAsync(
        WaterfallStepContext step,
        CancellationToken cancellationToken = default(CancellationToken))
        {
            // TODO: restart from previous step in case of confusion

            // process the input
            string text = step.Result as string;
            string[] players = PlayersInput(text);

            // early termination: can't understand "or"
            if (players[0] == "")
            {
                await step.Context.SendActivityAsync(
                    "Sorry, I didn't understand the choice",
                    cancellationToken: cancellationToken);
                return await step.EndDialogAsync(cancellationToken);
            }

            string starter = ChoosePlayer(players);

            // early termination: can't find players
            if (starter == "")
            {
                await step.Context.SendActivityAsync(
                    "Sorry, I couldn't find one or both of those players",
                    cancellationToken: cancellationToken);
                return await step.EndDialogAsync(cancellationToken);
            }

            // were able to choose successfully
            await step.Context.SendActivityAsync(
                $"You should start {starter}.",
                cancellationToken: cancellationToken);
            return await step.EndDialogAsync(cancellationToken);

        }

        private static string[] PlayersInput(string text)
        {
            // TODO: account for leading or trailing words ("Either", "I was thinking" etc)
            //  or just grab the two closest to the conjunction
            string[] players = new string[] { "", "" };

            if (text.Contains(" or "))
            {
                players[0] = Regex.Split(text, " or ")[0].Trim();
                players[1] = Regex.Split(text, " or ")[1].Trim();
            }
            else if (text.Contains(" and "))
            {
                players[0] = Regex.Split(text, " and ")[0].Trim();
                players[1] = Regex.Split(text, " and ")[1].Trim();
            }
            return players;
        }

        private static string ChoosePlayer(string[] players)
        {
            string result = "";

            // TODO: figure out how to store/retrieve these as resx or json or csv

            // check flex resource
            Dictionary<string, int> flex_full_names = new Dictionary<string, int>();
            flex_full_names.Add("Todd Gurley", 1);
            flex_full_names.Add("Kareem Hunt", 2);
            flex_full_names.Add("Saquon Barkley", 3);
            flex_full_names.Add("Rashaad Penny", 132);

            // check qb resource
            Dictionary<string, int> flex_qb_names = new Dictionary<string, int>();
            flex_qb_names.Add("Patrick Mahomes", 1);
            flex_qb_names.Add("Aaron Rodgers", 2);
            flex_qb_names.Add("Kirk Cousins", 3);
            flex_qb_names.Add("Nathan Peterman", 32);

            if (flex_full_names.ContainsKey(players[0]) && flex_full_names.ContainsKey(players[1]))
            {
                if (flex_full_names[players[0]] > flex_full_names[players[1]])
                {
                    return players[1];
                }
                else
                {
                    return players[0];
                }
            }
            else if (flex_qb_names.ContainsKey(players[0]) && flex_qb_names.ContainsKey(players[1]))
            {
                if (flex_qb_names[players[0]] > flex_qb_names[players[1]])
                {
                    return players[1];
                }
                else
                {
                    return players[0];
                }
            }
            return result; // not found
        }
    }
}
