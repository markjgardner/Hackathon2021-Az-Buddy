
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure.ResourceManager;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Builder.Dialogs.Prompts;

namespace Microsoft.BotBuilderSamples.Dialogs
{
    public class ResourceDialog : ComponentDialog
    {
        public ResourceDialog(ArmClient armclient)
            : base("resource")
        {         
            var waterfallSteps = new WaterfallStep[]
            {
                ResourceTypeStepAsync,
                ResourceStepAsync,
                AskToContinueAsync,
                ContinueOrNotAsync
            };            

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
            AddDialog(new ChoicePrompt("AskToContinue"));
            AddDialog(new ResourceGroupDialog("ResourceGroupDialog", armclient));
            AddDialog(new StorageAccountDialog("StorageAccountDialog", armclient));

            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> ResourceTypeStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.PromptAsync(nameof(ChoicePrompt),
            new PromptOptions
            {
                Prompt = MessageFactory.Text("Please enter the resource type you wish to create."),
                Choices = ChoiceFactory.ToChoices(new List<string>{"resource group", "storage account"}),
            }, cancellationToken);            
        }

        private async Task<DialogTurnResult> ResourceStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {            
            // the previous step set the resource type from the choices, set it here
            var resourceType = ((FoundChoice)stepContext.Result).Value;
            stepContext.Values["resource_type"] = resourceType;

            switch(resourceType)
            {
                case "resource group":
                    return await stepContext.BeginDialogAsync("ResourceGroupDialog", null, cancellationToken);
                case "storage account":
                    return await stepContext.BeginDialogAsync("StorageAccountDialog", null, cancellationToken);
            }

            await stepContext.Context.SendActivityAsync(
                MessageFactory.Text($"Unable to find resource type {resourceType}"), 
                cancellationToken);

            return await stepContext.EndDialogAsync(null, cancellationToken);
        }

        private async Task<DialogTurnResult> AskToContinueAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.PromptAsync("AskToContinue",
            new PromptOptions
            {
                Prompt = MessageFactory.Text("Would you like to create another resource?"),
                Choices = ChoiceFactory.ToChoices(new List<string> {"Yes", "No"})
            }, cancellationToken);
        }

        private async Task<DialogTurnResult> ContinueOrNotAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var answer = ((FoundChoice)stepContext.Result).Value;
            if (string.Equals(answer,"yes", System.StringComparison.InvariantCultureIgnoreCase))
                return await stepContext.ReplaceDialogAsync(InitialDialogId, null, cancellationToken);
            
            await stepContext.Context.SendActivityAsync(MessageFactory.Text("Goodbye!"), cancellationToken);
            return await stepContext.EndDialogAsync(null, cancellationToken);
        }
    }
}