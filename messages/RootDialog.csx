#load ".\Shared\HeroCardExtensions.csx"

using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;

// For more information about this template visit http://aka.ms/azurebots-csharp-basic
[Serializable]
public class RootDialog : IDialog<object>
{
    private const string RootDialog_Welcome_Ingredients = "Introduce ingredients";
    private const string RootDialog_Welcome_Support = "Call DNN Support";
    private const string RootDialog_Welcome_Error = "That is not a valid option. Please try again.";
    private const string RootDialog_Support_Message = "Support will contact you shortly.Have a nice day :)";

    private static IEnumerable<string> cancelTerms = new[] { "Cancel", "Back", "B", "Abort" };


    private ResumptionCookie resumptionCookie;

    public Task StartAsync(IDialogContext context)
    {
        try
        {
            context.Wait(MessageReceivedAsync);
        }
        catch (OperationCanceledException error)
        {
            return Task.FromCanceled(error.CancellationToken);
        }
        catch (Exception error)
        {
            return Task.FromException(error);
        }

        return Task.CompletedTask;
    }

    public virtual async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> argument)
    {
        var message = await argument;

        if (this.resumptionCookie == null)
        {
            this.resumptionCookie = new ResumptionCookie(message);
        }

        await this.WelcomeMessageAsync(context);
    }

    private async Task WelcomeMessageAsync(IDialogContext context)
    {
        var reply = context.MakeMessage();

        var options = new[]
        {
                RootDialog_Welcome_Ingredients,
                RootDialog_Welcome_Support
            };

        HeroCardExtensions.AddHeroCard(ref reply,
            "Recipes Bot",
            "Your agent that allows you to cook Liquid Content recipes2",
            options, 
            new[] { "https://cdn.pixabay.com/photo/2017/03/17/10/29/breakfast-2151201_960_720.jpg" });

        await context.PostAsync(reply);

        context.Wait(this.OnOptionSelected);
    }

    private async Task OnOptionSelected(IDialogContext context, IAwaitable<IMessageActivity> result)
    {
        var message = await result;

        if (message.Text == RootDialog_Welcome_Ingredients)
        {
            //this.order = new Models.Order();

            //context.Call(this.dialogFactory.Create<TourCategoriesDialog>(), this.AfterTourCategorySelected);
            await this.StartOverAsync(context, RootDialog_Support_Message);
        }
        else if (message.Text == RootDialog_Welcome_Support)
        {
            await this.StartOverAsync(context, RootDialog_Support_Message);
        }
        else
        {
            await this.StartOverAsync(context, RootDialog_Welcome_Error);
        }
    }

    private async Task StartOverAsync(IDialogContext context, string text)
    {
        var message = context.MakeMessage();
        message.Text = text;
        await this.StartOverAsync(context, message);
    }

    private async Task StartOverAsync(IDialogContext context, IMessageActivity message)
    {
        await context.PostAsync(message);
        //this.order = new Models.Order();
        await this.WelcomeMessageAsync(context);
    }

}