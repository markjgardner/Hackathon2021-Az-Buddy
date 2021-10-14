
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
using Microsoft.BotBuilderSamples.Extensions;
using System.Linq;
using Azure.ResourceManager.Resources.Models;
using Azure.ResourceManager.Storage;
using Azure.ResourceManager.Storage.Models;

namespace Microsoft.BotBuilderSamples.Dialogs
{
    public class StorageAccountDialog : ComponentDialog
    {
        public StorageAccountDialog(string dialogID) 
            : base(dialogID)
        {   
            var waterfallSteps =  new WaterfallStep[]
            {
                ResourceGroupStepAsync,
                NameStepAsync,
                LocationStepAsync,
                SkuStepAsync,
                CreateStepAsync
            };

            InitialDialogId = nameof(WaterfallDialog);
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ChoicePrompt("LocationChoicePrompt"));
            AddDialog(new ChoicePrompt("RGChoicePrompt"));
        }

        private async Task<DialogTurnResult> ResourceGroupStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var groups = GetResourceGroups().Take(3).Select((rg) => {return rg.Data.Name;}).ToList();
            var choices = ChoiceFactory.ToChoices(groups);
            return await stepContext.PromptAsync("RGChoicePrompt", 
                new PromptOptions 
                { 
                    Prompt = MessageFactory.Text("Which resource group would you like to deploy the storage account into?"),
                    Choices = choices
                }, cancellationToken);
        }

        private async Task<DialogTurnResult> NameStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // save the result from the previous step
            stepContext.Values["resourceGroup"] = stepContext.Result;

            return await stepContext.PromptAsync(
                nameof(TextPrompt), 
                new PromptOptions 
                { 
                    Prompt = MessageFactory.Text("Enter the storage account name.") 
                }, cancellationToken);
        }

        private async Task<DialogTurnResult> LocationStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // save the result from the previous step
            stepContext.Values["name"] = stepContext.Result;

            // prompt for the location
            return await stepContext.PromptAsync("LocationChoicePrompt",
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("Enter the azure location."),
                    Choices = ChoiceFactory.ToChoices(new List<string> { Location.EastUS.Name, Location.WestUS.Name, Location.EastUS2.Name }),
                }, cancellationToken);
        }

        private async Task<DialogTurnResult> SkuStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken) 
        {
            // save the result from the previous step
            stepContext.Values["location"] = stepContext.Result;

            // prompt for the location
            return await stepContext.PromptAsync(nameof(ChoicePrompt),
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("Pick a sku for the storage account."),
                    Choices = ChoiceFactory.ToChoices(new List<string> { "Standard_LRS", "Standard_ZRS", "Standard_GRS", "Standard_RAGRS" }),
                }, cancellationToken);

        }

        private async Task<DialogTurnResult> CreateStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var sku = ((FoundChoice)stepContext.Result).Value;
            stepContext.Values["sku"] = sku;

            var name = (string)stepContext.Values["name"];
            var resourceGroup = (string)stepContext.Values["resourceGroup"];
            var location = (string)stepContext.Values["location"];

            await stepContext.Context.SendActivityAsync(
                MessageFactory.Text($"Creating Storage Account {name} in {resourceGroup}"), 
                cancellationToken);

            var creds = new DefaultAzureCredential();
            var armClient = new ArmClient(creds);
            var subscription = armClient.DefaultSubscription;
            var storage = new StorageManagementClient(subscription.Id, creds).StorageAccounts;
            var storageParams = new StorageAccountCreateParameters(new Sku(sku), Kind.StorageV2, location);
            var rawResult = await storage.StartCreateAsync(resourceGroup, name, storageParams);
            var storageAccount = (await rawResult.WaitForCompletionAsync()).Value;
            
            await stepContext.Context.SendActivityAsync(
                MessageFactory.Text($"Storage account {name} created at {storageAccount.Id}."), 
                cancellationToken);

            // Remember to call EndAsync to indicate to the runtime that this is the end of our waterfall.
            return await stepContext.EndDialogAsync(null, cancellationToken);
        }

        private IEnumerable<ResourceGroup> GetResourceGroups()
        {
            var armClient = new ArmClient(new DefaultAzureCredential());
            var subscription = armClient.DefaultSubscription;
            var resourceGroupContainer = subscription.GetResourceGroups();
            
            return resourceGroupContainer.GetAll(null, 3).GetEnumerator().ToIEnumerable();
        }
    }
}