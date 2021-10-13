
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Builder.Dialogs.Prompts;

namespace Microsoft.BotBuilderSamples.Dialogs
{
    public class ResourceDialog : ComponentDialog
    {
        public ResourceDialog()
            : base("resource")
        {         
            var waterfallSteps = new WaterfallStep[]
            {
                ResourceTypeStepAsync,
                ResourceStepAsync,
            };            

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
            AddDialog(new ResourceGroupDialog(nameof(ResourceGroupDialog)));

            InitialDialogId = nameof(WaterfallDialog);
        }

        private static async Task<DialogTurnResult> ResourceTypeStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.PromptAsync(nameof(ChoicePrompt),
            new PromptOptions
            {
                Prompt = MessageFactory.Text("Please enter the resource type you wish to create."),
                Choices = ChoiceFactory.ToChoices(new List<string>{"resource group"}),
            }, cancellationToken);            
        }

        private static async Task<DialogTurnResult> ResourceStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {            
            // the previous step set the resource type from the choices, set it here
            var resourceType = ((FoundChoice)stepContext.Result).Value;
            stepContext.Values["resource_type"] = resourceType;

            switch(resourceType)
            {
                case "resource group":
                    return await stepContext.BeginDialogAsync(nameof(ResourceGroupDialog), null, cancellationToken);
                case "storage account":
                    return await stepContext.BeginDialogAsync(nameof(StorageAccountDialog), null, cancellationToken);
            }

            await stepContext.Context.SendActivityAsync(
                MessageFactory.Text($"Unable to find resource type {resourceType}"), 
                cancellationToken);            

            return await stepContext.EndDialogAsync(null, cancellationToken);
        }
    }
}