using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using System.Collections.Generic;
using CortosoBank.Models;

namespace CortosoBank
{
    [Serializable]
    public class HelpDialog : IDialog<object>
    {

        public string[] options = new string[] { "create account", "delete account", "withdraw", "deposit", "my balance", "currency rate" };

        public async Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);
        }

        public virtual async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            var reply = context.MakeMessage();
            //reply.Text = string.Format("You said {0}", message.Text);
            reply.Attachments = new List<Attachment>();


            // CardButtons
            var actions = new List<CardAction>();
            for (int i = 0; i < options.Length; i++)
            {
                actions.Add(new CardAction
                {
                    Title = $"{options[i]}",
                    Value = $"{options[i]}",
                    Type = ActionTypes.ImBack
                });
            }

            // CardImage
            List<CardImage> cardImages = new List<CardImage>();
            cardImages.Add(new CardImage(url: "https://irp-cdn.multiscreensite.com/d0e68b97/dms3rep/multi/mobile/icon_002-300x300.png"));


            // Add ThumbnailCard to reply
            reply.Attachments.Add(
                 new ThumbnailCard
                 {
                     Title = $"Welcome to Cortoso Bank",
                     Subtitle = "Example of commands",
                     Images = cardImages,
                     Buttons = actions
                 }.ToAttachment()
            );

            await context.PostAsync(reply);
            context.Wait(MessageReceivedAsync);

        }
    }
}