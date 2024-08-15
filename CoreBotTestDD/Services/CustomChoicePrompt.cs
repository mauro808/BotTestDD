using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.AI.TextAnalytics;
using CoreBotTestDD.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Builder.Dialogs.Prompts;
using Microsoft.Bot.Schema;

public class CustomChoicePrompt : ChoicePrompt
{
    private readonly TextAnalyticsClient _textAnalyticsClient;
    private readonly CLUService _cluService;
    private readonly Dictionary<string, ChoiceFactoryOptions> _choiceDefaults;

    public CustomChoicePrompt(string dialogId, TextAnalyticsClient textAnalyticsClient, CLUService cluService)
        : base(dialogId)
    {
        _cluService = cluService;
        _textAnalyticsClient = textAnalyticsClient;
    }

    protected override async Task<PromptRecognizerResult<FoundChoice>> OnRecognizeAsync(ITurnContext turnContext, IDictionary<string, object> state, PromptOptions options, CancellationToken cancellationToken = default(CancellationToken))
    {
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
            findChoicesOptions.Locale = DetermineCulture(activity, findChoicesOptions);
            List<ModelResult<FoundChoice>> list2 = ChoiceRecognizers.RecognizeChoices(text, list, findChoicesOptions);
            if (list2 != null && list2.Count > 0)
            {
                promptRecognizerResult.Succeeded = true;
                promptRecognizerResult.Value = list2[0].Resolution;
            }
            else
            {
                var response2 = await _cluService.AnalyzeTextEntitiesAsync(text);
                if (response2 != null)
                {
                    text = response2;
                    list2 = ChoiceRecognizers.RecognizeChoices(text, list, findChoicesOptions);
                    if (list2 != null && list2.Count > 0)
                    {
                        promptRecognizerResult.Succeeded = true;
                        promptRecognizerResult.Value = list2[0].Resolution;
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
