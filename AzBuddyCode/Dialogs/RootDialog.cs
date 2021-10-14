
using Azure.ResourceManager;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Adaptive;
using Microsoft.Bot.Builder.Dialogs.Prompts;

namespace Microsoft.BotBuilderSamples.Dialogs
{
    public class RootDialog : ComponentDialog
    {
        public RootDialog(ArmClient armclient)
        {            
            AddDialog(new ResourceDialog(armclient));
        }        
    }
}