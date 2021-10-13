
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder;
using Newtonsoft.Json.Linq;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Resources.Models;
using System;

namespace Microsoft.BotBuilderSamples.Dialogs
{
    public class ResourceGroupDialog : ComponentDialog
    {
        public ResourceGroupDialog(string dialogID) 
            : base(dialogID)
        {   
            InitialDialogId = nameof(WaterfallDialog);
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                NameStepAsync,
                LocationStepAsync,
                CreateStepAsync,
            }));
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
        }

        private async Task<DialogTurnResult> NameStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.PromptAsync(
                nameof(TextPrompt), 
                new PromptOptions 
                { 
                    Prompt = MessageFactory.Text("Please enter the resource group name.") 
                }, cancellationToken);
        }

        private async Task<DialogTurnResult> LocationStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // save the result from the previous step
            stepContext.Values["name"] = stepContext.Result;

            // prompt for the location
            return await stepContext.PromptAsync(nameof(ChoicePrompt),
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("Please enter the azure location."),
                    Choices = ChoiceFactory.ToChoices(new List<string> { Location.EastUS.Name, "WestUS", "EastUS2" }),
                }, cancellationToken);
        }

        private async Task<DialogTurnResult> CreateStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var location = ((FoundChoice)stepContext.Result).Value;
            stepContext.Values["location"] = location;
            var name = (string)stepContext.Values["name"];

            await stepContext.Context.SendActivityAsync(
                MessageFactory.Text($"Creating Resource Group {name} in {location}"), 
                cancellationToken);
            try{
                var armClient = new ArmClient(new DefaultAzureCredential());
                var subscription = armClient.DefaultSubscription;
                var resourceGroupContainer = subscription.GetResourceGroups();
                var resourceGroupData = new ResourceGroupData(location);
                var resourceGroup = await resourceGroupContainer.CreateOrUpdateAsync(
                    name, 
                    resourceGroupData, 
                    true, 
                    cancellationToken);
                
                await stepContext.Context.SendActivityAsync(
                    MessageFactory.Text($"Resource Group {name} as {resourceGroup.Id}"), 
                    cancellationToken);
            }
            catch(Exception ex)
            {
                await stepContext.Context.SendActivityAsync(
                MessageFactory.Text($"An error occured createing resource group {name}. Message {ex.Message}"), 
                cancellationToken);
            }
            
            // Remember to call EndAsync to indicate to the runtime that this is the end of our waterfall.
            return await stepContext.EndDialogAsync(null, cancellationToken);
        }
    }
}