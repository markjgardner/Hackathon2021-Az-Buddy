
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
        private ArmClient armClient { get; set; }
        public StorageAccountDialog(string dialogID, ArmClient armclient) 
            : base(dialogID)
        {   
            this.armClient = armclient;
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
            AddDialog(new ChoicePrompt("SkuChoicePrompt"));
        }

        private async Task<DialogTurnResult> ResourceGroupStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var groups = await GetResourceGroupsAsync(cancellationToken);            
            var choices = ChoiceFactory.ToChoices(groups.Select(x=>x.Data.Name).ToList());
            
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
            stepContext.Values["resourceGroup"] = ((FoundChoice)stepContext.Result).Value;

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
            stepContext.Values["location"] = ((FoundChoice)stepContext.Result).Value;;

            // prompt for the location
            return await stepContext.PromptAsync("SkuChoicePrompt",
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
            var storage = new StorageManagementClient(subscription.Id.SubscriptionId, creds).StorageAccounts;
            var storageParams = new StorageAccountCreateParameters(new Sku(sku), Kind.StorageV2, location);
            var rawResult = await storage.StartCreateAsync(resourceGroup, name, storageParams);
            var storageAccount = (await rawResult.WaitForCompletionAsync()).Value;
            
            await stepContext.Context.SendActivityAsync(
                MessageFactory.Text($"Storage account {name} created at {storageAccount.Id}."), 
                cancellationToken);

            // Remember to call EndAsync to indicate to the runtime that this is the end of our waterfall.
            return await stepContext.EndDialogAsync(null, cancellationToken);
        }


        private ValueTask<List<ResourceGroup>> GetResourceGroupsAsync(CancellationToken cancellationToken)
        {
            var subscription = armClient.DefaultSubscription;
            var resourceGroupContainer = subscription.GetResourceGroups();
            
            var pageable = resourceGroupContainer.GetAllAsync("tagName eq 'hackathon' and tagValue eq 'azbuddy'", null, cancellationToken);            
            return pageable.ToListAsync(cancellationToken);
        }
    }
}