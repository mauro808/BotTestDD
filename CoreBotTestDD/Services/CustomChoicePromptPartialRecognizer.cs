using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.AI.TextAnalytics;
using CoreBotTestDD.Models;
using CoreBotTestDD.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Builder.Dialogs.Prompts;
using Microsoft.Bot.Schema;

public class CustomChoicePromptPartialRecognizer : ChoicePrompt
{
    private readonly TextAnalyticsClient _textAnalyticsClient;
    private readonly CLUService _cluService;
    private readonly IStatePropertyAccessor<UserProfileModel> _userStateAccessor;

    private readonly Dictionary<string, ChoiceFactoryOptions> _choiceDefaults;

    public CustomChoicePromptPartialRecognizer(string dialogId, TextAnalyticsClient textAnalyticsClient, CLUService cluService, IStatePropertyAccessor<UserProfileModel> userStateAccessor)
        : base(dialogId)
    {
        _cluService = cluService;
        _textAnalyticsClient = textAnalyticsClient;
        _userStateAccessor = userStateAccessor;
    }

    protected override async Task<PromptRecognizerResult<FoundChoice>> OnRecognizeAsync(ITurnContext turnContext, IDictionary<string, object> state, PromptOptions options, CancellationToken cancellationToken = default(CancellationToken))
    {
        var userProfile = await _userStateAccessor.GetAsync(turnContext, () => new UserProfileModel(), cancellationToken);
        if (turnContext == null)
        {
            throw new ArgumentNullException("turnContext");
        }
        IList<Choice> list = options.Choices ?? new List<Choice>();
        PromptRecognizerResult<FoundChoice> promptRecognizerResult = new PromptRecognizerResult<FoundChoice>();
        if (turnContext.Activity.Type == "message")
        {
            Activity activity = turnContext.Activity;
            string text = activity.Text;
            if (string.IsNullOrEmpty(text))
            {
                return promptRecognizerResult;
            }
            /**var response = await _cluService.AnalyzeTextAsync(text);
            if (response != null)
            {
                text = response;
            }**/
            FindChoicesOptions findChoicesOptions = RecognizerOptions ?? new FindChoicesOptions();
            findChoicesOptions.Locale = "es-es";
            //findChoicesOptions.AllowPartialMatches = true;
            List<ModelResult<FoundChoice>> list2 = ChoiceRecognizers.RecognizeChoices(text, list, findChoicesOptions);
            if (int.TryParse(text, out int choiceNumber))
            {
                if (list2 != null && list2.Count > 0)
                {
                    promptRecognizerResult.Succeeded = true;
                    promptRecognizerResult.Value = list2[0].Resolution;
                }
            }
            else
            {
                findChoicesOptions.RecognizeNumbers = false;
                list2 = ChoiceRecognizers.RecognizeChoices(text, list, findChoicesOptions);
                if (list2 != null && list2.Count > 0)
                {
                    promptRecognizerResult.Succeeded = true;
                    promptRecognizerResult.Value = list2[0].Resolution;
                }
                else
                {
                    var responseModel = await _cluService.AnalyzeTextEntitiesAsync(text);
                    if (responseModel != null)
                    {
                        if (responseModel.SubType != null && responseModel.SubType.Equals("Nombre"))
                        {
                            text = "Nombre del profesional";
                            userProfile.DoctorName = responseModel.Value;
                        }
                        else if (responseModel.SubType != null && responseModel.SubType.Equals("Fechas"))
                        {
                            text = responseModel.Value.ToString();
                        }
                        else
                        {
                            text = responseModel.intent;
                        }
                        list2 = ChoiceRecognizers.RecognizeChoices(text, list, findChoicesOptions);
                        if (list2 != null && list2.Count > 0)
                        {
                            promptRecognizerResult.Succeeded = true;
                            promptRecognizerResult.Value = list2[0].Resolution;
                        }
                    }
                }
            }
        }

        return promptRecognizerResult;
    }
    /**public async Task<string> FindMostSimilarChoiceAsync(IList<Choice> list, string text)
    {
        double maxSimilarity = double.MinValue;
        string mostSimilarChoice = string.Empty;
        foreach (var choice in list)
        {
            double similarity = await buscarSimilitud(choice.Value, text);
            if (similarity > maxSimilarity)
            {
                maxSimilarity = similarity;
                mostSimilarChoice = choice.Value;
            }
        }
        return mostSimilarChoice;
    }**/
    private string DetermineCulture(Activity activity, FindChoicesOptions opt = null)
    {
        string text = PromptCultureModels.MapToNearestLanguage(activity.Locale ?? opt?.Locale ?? DefaultLocale ?? PromptCultureModels.English.Locale);
        text = PromptCultureModels.Spanish.Locale;
        return text;
    }
}
