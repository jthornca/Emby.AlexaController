﻿using AlexaController.Alexa.Presentation.DataSources;
using AlexaController.Alexa.RequestModel;
using AlexaController.Alexa.ResponseModel;
using AlexaController.Alexa.Viewport;
using AlexaController.Api;
using AlexaController.Session;
using AlexaController.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AlexaController.AlexaDataSourceManagers;
using AlexaController.AlexaDataSourceManagers.DataSourceProperties;
using AlexaController.AlexaPresentationManagers;

namespace AlexaController.Alexa.IntentRequest
{
    [Intent]
    // ReSharper disable once UnusedType.Global
    public class NewItemsIntent : IntentResponseBase<IAlexaRequest, IAlexaSession>, IIntentResponse
    {
        public IAlexaRequest AlexaRequest { get; }
        public IAlexaSession Session { get; }


        public NewItemsIntent(IAlexaRequest alexaRequest, IAlexaSession session) : base(alexaRequest, session)
        {
            AlexaRequest = alexaRequest;
            Session = session;
        }

        public async Task<string> Response()
        {
            var request = AlexaRequest.request;
            var slots = request.intent.slots;
            var duration = slots.Duration.value;
            var type = slots.MovieAlternatives.value is null ? "Series" : "Movie";

            IDataSource aplDataSource;
            IDataSource aplaDataSource;

            // Default will be 25 days ago unless given a time duration
            var d = duration is null ? DateTime.Now.AddDays(-25) : DateTimeDurationSerializer.GetMinDateCreation(duration);

            var query = type == "Series"
                ? ServerQuery.Instance.GetLatestTv(Session.User, d)
                : ServerQuery.Instance.GetLatestMovies(Session.User, d);

            var results = query.Where(item => item.IsParentalAllowed(Session.User)).ToList();

            if (!results.Any())
            {
                return await AlexaResponseClient.Instance.BuildAlexaResponseAsync(new Response()
                {
                    outputSpeech = new OutputSpeech()
                    {
                        phrase = $"No new { type } have been added."
                    },
                    shouldEndSession = true,
                    SpeakUserName = true,
                }, Session);
            }



            switch (Session.viewport)
            {
                case ViewportProfile.HUB_ROUND_SMALL:
                case ViewportProfile.HUB_LANDSCAPE_SMALL:
                case ViewportProfile.HUB_LANDSCAPE_MEDIUM:
                case ViewportProfile.HUB_LANDSCAPE_LARGE:
                    {

                        aplDataSource = await APL_DataSourceManager.Instance.GetSequenceItemsDataSourceAsync(results);
                        aplaDataSource = await APLA_DataSourceManager.Instance.GetNewItemsApl(results, d);

                        AlexaSessionManager.Instance.UpdateSession(Session, aplDataSource);

                        var renderDocumentDirective = await APL_RenderDocumentManager.Instance.GetRenderDocumentDirectiveAsync<MediaItem>(aplDataSource, Session);
                        var renderAudioDirective = await APLA_RenderDocumentManager.Instance.GetAudioDirectiveAsync(aplaDataSource);

                        return await AlexaResponseClient.Instance.BuildAlexaResponseAsync(new Response()
                        {
                            shouldEndSession = null,
                            SpeakUserName = true,
                            directives = new List<IDirective>()
                            {
                                renderDocumentDirective,
                                renderAudioDirective
                            }

                        }, Session);
                    }
                default: //Voice only
                    {
                        aplaDataSource = await APLA_DataSourceManager.Instance.NewItemsAplaOnly(results, d);

                        var renderAudioDirective = await APLA_RenderDocumentManager.Instance.GetAudioDirectiveAsync(aplaDataSource);
                        return await AlexaResponseClient.Instance.BuildAlexaResponseAsync(new Response()
                        {
                            shouldEndSession = true,
                            SpeakUserName = true,
                            directives = new List<IDirective>()
                            {
                                renderAudioDirective
                            }

                        }, Session);
                    }
            }
        }
    }
}
