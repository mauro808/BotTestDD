// Generated with Bot Builder V4 SDK Template for Visual Studio CoreBot v4.22.0

using Antlr4.Runtime.Dfa;
using Azure;
using CoreBotTestDD.CognitiveModels;
using CoreBotTestDD.Models;
using CoreBotTestDD.Services;
using CoreBotTestDD.Utilities;
using DonBot.Dialogs;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CoreBotTestDD.Dialogs
{
    public class MainDialog : ComponentDialog
    {
        private readonly UserState _userState;
        private readonly ConversationState _conversationState;
        private readonly AgendarDialog _agendarDialog;
        private readonly CancelarCitaDialog _cancelarCitaDialog;
        private readonly CLUService _cluService;
        private readonly CQAService _cqaService;
        private readonly ClientMessages _clientMessages;
        private readonly ILogger _logger;
        private readonly IStatePropertyAccessor<UserProfileModel> _userStateAccessor;

        public MainDialog(CLUService cluService, CQAService cqaService, ClientMessages clientMessages, ILogger<MainDialog> logger, UserState userState, ConversationState conversationState, AgendarDialog agendarDialog, CancelarCitaDialog cancelarCitaDialog,IBotTelemetryClient telemetryClient)
            : base(nameof(MainDialog))
        {
            _userState = userState ?? throw new ArgumentNullException(nameof(userState));
            _conversationState = conversationState ?? throw new ArgumentNullException(nameof(conversationState));
            TelemetryClient = telemetryClient;
            _cluService = cluService;
            _cqaService = cqaService;
            _clientMessages = clientMessages;
            _logger = logger;
            _userStateAccessor = _userState.CreateProperty<UserProfileModel>("UserProfile");
            _agendarDialog = agendarDialog;
            _cancelarCitaDialog = cancelarCitaDialog;

            AddDialog(agendarDialog);
            AddDialog(cancelarCitaDialog);
            AddDialog(new TextPrompt(nameof(TextPrompt)));

            var waterfallSteps = new WaterfallStep[]
            {
                IntroStepAsync,
                ActStepAsync,
                RouterStepAsync,
                FinalStepAsync,
            };

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));
            InitialDialogId = nameof(WaterfallDialog);
        }
        private async Task<DialogTurnResult> IntroStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userProfile = await _userStateAccessor.GetAsync(stepContext.Context, () => new UserProfileModel { IsNewUser = true }, cancellationToken);
            if (userProfile.IsNewUser)
            {
                userProfile.CodeCompany = "330ab1418a035af17b0762b7920f84e78998aea19f5d94948d1ae7ed481514a7";
                //userProfile.CodeCompany = await _clientMessages.GetClientSha("whatsapp:+573247496430");
                //await stepContext.Context.SendActivityAsync(MessageFactory.Text(await _clientMessages.GetClientMessages("Bienvenida", userProfile.CodeCompany)));
                //await stepContext.Context.SendActivityAsync(MessageFactory.Text(await _clientMessages.GetClientMessages("Tratamiento datos personales", userProfile.CodeCompany)));
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Hola! Soy el asistente virtual de Mederi. Al continuar, aceptas nuestros términos y condiciones [URL](http://fundacionhomi.org/.co/). Te puedo ayudar a agendar y/o gestionar tus citas y pedir información. ¿Que deseas hacer? \n\n1-Agendar\n\n2- Obtener información"));
                userProfile.IsNewUser = false;
            }
            await _userStateAccessor.SetAsync(stepContext.Context, userProfile, cancellationToken);
            await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
            return await stepContext.NextAsync(null, cancellationToken);
        }
        private async Task<DialogTurnResult> ActStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userProfile = await _userStateAccessor.GetAsync(stepContext.Context, () => new UserProfileModel(), cancellationToken);
            return new DialogTurnResult(DialogTurnStatus.Waiting);
        }

        private async Task<DialogTurnResult> RouterStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userProfile = await _userStateAccessor.GetAsync(stepContext.Context, () => new UserProfileModel { IsNewUser = true }, cancellationToken);
            var userStateAccessors = stepContext.Context.TurnState.Get<UserState>();
            if(stepContext.Context.Activity.Text == "1")
            {
                await stepContext.Context.SendActivityAsync("Muy bien, vamos a agendar una cita.", cancellationToken: cancellationToken);
                return await stepContext.BeginDialogAsync(nameof(AgendarDialog), null, cancellationToken);
            }else if(stepContext.Context.Activity.Text == "2" || stepContext.Context.Activity.Text.Equals("obtener informacion", StringComparison.OrdinalIgnoreCase))
            {
                await stepContext.Context.SendActivityAsync("Perfecto. ¿Que informacion necesitas? Si tengo respuesta para ello te dare la informacion.");
                return await stepContext.NextAsync(null, cancellationToken);
            }
            var responseQna = await _cqaService.AnalyzeQuestionAsync(stepContext.Context.Activity.Text.ToString());
            if (responseQna != null)
            {
                await stepContext.Context.SendActivityAsync(responseQna.text);
                if (responseQna.metadata != "static")
                {
                    await stepContext.Context.SendActivityAsync("¿Puedo ayudarte en algo mas?");
                }
            }
            else
            {
                var generalScore = await _cluService.AnalyzeTextAsync(stepContext.Context.Activity.Text.ToString());
                var IntentScore = await _cluService.AnalyzeTextEntitiesAsync(stepContext.Context.Activity.Text.ToString());
                if (generalScore == "Agendar")
                {
                    await stepContext.Context.SendActivityAsync("Muy bien, vamos a agendar una cita.", cancellationToken: cancellationToken);
                    return await stepContext.BeginDialogAsync(nameof(AgendarDialog), null, cancellationToken);
                }
                else if (IntentScore == "Negacion")
                {
                    await stepContext.Context.SendActivityAsync("Fue un placer atenderte. Estare aqui para ayudarte en nuestra proxima interaccion.", cancellationToken: cancellationToken);
                    userProfile.IsNewUser = true;
                    await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
                    return await stepContext.EndDialogAsync();
                }else if (IntentScore == "Afirmacion")
                {

                }else if(IntentScore == "Cancelacion de cita" || generalScore == "Cancelar cita")
                {
                    await stepContext.Context.SendActivityAsync("Muy bien, vamos a realizar la cancelacion de una cita.", cancellationToken: cancellationToken);
                    return await stepContext.BeginDialogAsync(nameof(CancelarCitaDialog), null, cancellationToken);
                }
                else
                {
                    await stepContext.Context.SendActivityAsync("Lo siento no entiendo lo que escribiste, te recomendamos seguir el paso a paso");
                }
            }
            return await stepContext.NextAsync(null, cancellationToken);
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.ReplaceDialogAsync(InitialDialogId, default);
        }

        protected override Task<DialogTurnResult> OnContinueDialogAsync(DialogContext innerDc, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Añade lógica adicional que necesites antes de llamar al método base
            _logger.LogInformation("OnContinueDialogAsync was called.");

            // Llama al método base para asegurarte de que se ejecuten otras implementaciones de OnContinueDialogAsync en la pila de diálogos
            return base.OnContinueDialogAsync(innerDc, cancellationToken);
        }

        static string RemoveCommasAndAccents(string text)
        {
            if (text == null)
                throw new ArgumentNullException(nameof(text));

            // Eliminar comas
            string withoutCommas = text.Replace(",", "");

            // Normalizar el texto a forma de descomposición canónica
            string normalized = withoutCommas.Normalize(NormalizationForm.FormD);

            // Crear un StringBuilder para construir el texto resultante sin acentos
            StringBuilder builder = new StringBuilder();

            // Recorrer cada carácter en el texto normalizado
            foreach (char c in normalized)
            {
                // Agregar solo caracteres que no sean marcas de acento
                UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(c);
                if (category != UnicodeCategory.NonSpacingMark)
                {
                    builder.Append(c);
                }
            }

            // Devolver el texto final sin comas y sin acentos
            return builder.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
