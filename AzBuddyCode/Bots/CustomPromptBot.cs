// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;

namespace Microsoft.BotBuilderSamples
{
    // This IBot implementation can run any type of Dialog. The use of type parameterization is to allows multiple different bots
    // to be run at different endpoints within the same project. This can be achieved by defining distinct Controller types
    // each with dependency on distinct IBot types, this way ASP Dependency Injection can glue everything together without ambiguity.
    public class CustomPromptBot<T> : ActivityHandler 
        where T : Dialog
    {
        private readonly BotState _userState;
        private readonly BotState _conversationState;
        protected readonly ILogger _logger;
        private readonly Dialog _dialog;
        
        public CustomPromptBot(ConversationState conversationState, UserState userState, T dialog, ILogger<CustomPromptBot<T>> logger)
        {
            _conversationState = conversationState;
            _userState = userState;
            _dialog = dialog;
            _logger = logger;
        }

         public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default)
        {
            await base.OnTurnAsync(turnContext, cancellationToken);

            // Save any state changes that might have occurred during the turn.
            await _conversationState.SaveChangesAsync(turnContext, false, cancellationToken);
            await _userState.SaveChangesAsync(turnContext, false, cancellationToken);
        }

        // Messages sent to the user.
        private const string WelcomeMessage = 
            "This is a simple Welcome Bot sample. This bot will introduce you " +
            "to welcoming and greeting users. You can say 'intro' to see the " +
            "introduction card. If you are running this bot in the Bot Framework " +
            "Emulator, press the 'Start Over' button to simulate user joining " +
            "a bot or a channel";

        private const string InfoMessage = 
            "You are seeing this message because the bot received at least one " +
            "'ConversationUpdate' event, indicating you (and possibly others) " +
            "joined the conversation. If you are using the emulator, pressing " +
            "the 'Start Over' button to trigger this event again. The specifics " +
            "of the 'ConversationUpdate' event depends on the channel. You can " +
            "read more information at: " +
            "https://aka.ms/about-botframework-welcome-user";

        private const string LocaleMessage = 
            "You can use the activity's 'GetLocale()' method to welcome the user " +
            "using the locale received from the channel. " + 
            "If you are using the Emulator, you can set this value in Settings.";

        private const string PatternMessage = 
            "Welcome to the Az Buddy Bot. This bot will help you create azure resources.";

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            foreach(var member in membersAdded)
            {
                if(member.Id != turnContext.Activity.Recipient.Id)
                {
                    await turnContext.SendActivityAsync($"Hi there - {member.Name}. {WelcomeMessage}", cancellationToken: cancellationToken);
                    await turnContext.SendActivityAsync(InfoMessage, cancellationToken: cancellationToken);
                    await turnContext.SendActivityAsync($"{LocaleMessage} Current locale is '{turnContext.Activity.GetLocale()}'.", cancellationToken: cancellationToken);
                    await turnContext.SendActivityAsync(PatternMessage, cancellationToken: cancellationToken);

                    
                    // Run the Dialog with the new message Activity.
                    await _dialog.RunAsync(turnContext, _conversationState.CreateProperty<DialogState>(nameof(DialogState)), cancellationToken);
                }
            }
        }
    }
}
