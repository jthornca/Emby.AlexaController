﻿using System;
using System.Collections.Generic;
using System.Linq;
using AlexaController.Alexa.IntentRequest.Rooms;
using AlexaController.Alexa.ResponseData.Model;
using AlexaController.Api;
using AlexaController.Configuration;
using AlexaController.Session;
using AlexaController.Utils;
using AlexaController.Utils.SemanticSpeech;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;

// ReSharper disable TooManyChainedReferences
// ReSharper disable TooManyDependencies
// ReSharper disable once UnusedAutoPropertyAccessor.Local
// ReSharper disable once ExcessiveIndentation
// ReSharper disable twice ComplexConditionExpression
// ReSharper disable PossibleNullReferenceException
// ReSharper disable TooManyArguments

namespace AlexaController.Alexa.IntentRequest.Browse
{
    public class BrowseEpisodesIntent : IIntentResponseModel
    {
        public string Response
        (AlexaRequest alexaRequest, IAlexaSession session, IResponseClient responseClient, ILibraryManager libraryManager, ISessionManager sessionManager, IUserManager userManager)
        {
            var roomManager = new RoomContextManager();
            Room room = null;
            try { room = roomManager.ValidateRoom(alexaRequest, session); } catch { }
            var displayNone = Equals(session.alexaSessionDisplayType, AlexaSessionDisplayType.NONE);
            if (room is null && displayNone) return roomManager.RequestRoom(alexaRequest, session, responseClient);
            
            var request        = alexaRequest.request;
            var intent         = request.intent;
            var slots          = intent.slots;
            var seasonNumber   = slots.SeasonNumber.value;
            var context        = alexaRequest.context;
            var apiAccessToken = context.System.apiAccessToken;
            var requestId      = request.requestId;

            responseClient.PostProgressiveResponse($"{SemanticSpeechUtility.GetSemanticSpeechResponse(SemanticSpeechType.COMPLIANCE)} {SemanticSpeechUtility.GetSemanticSpeechResponse(SemanticSpeechType.REPOSE)}", apiAccessToken, requestId);
            
            // This is a recursive request, so if the user is viewing media at "Series" or "Season" level
            // it will return the episode list for the season. ASK: "Show Season 1" / "Season 1" .
            var result = libraryManager.GetItemsResult(new InternalItemsQuery(session.User)
            {
                Parent            = session.NowViewingBaseItem,
                IncludeItemTypes  = new[] { "Episode" },
                ParentIndexNumber = Convert.ToInt32(seasonNumber),
                Recursive         = true
            });

            // User requested season/episode data that doesn't exist
            if (!result.Items.Any())
            {
                return responseClient.BuildAlexaResponse(new Response()
                {
                    outputSpeech = new OutputSpeech()
                    {
                        phrase = SemanticSpeechStrings.GetPhrase(SpeechResponseType.NO_SEASON_ITEM_EXIST, session, null, new[] {seasonNumber}),
                        semanticSpeechType = SemanticSpeechType.APOLOGETIC,
                    },
                    shouldEndSession = null,
                    person           = null,
                }, session.alexaSessionDisplayType);
            }

            var season = libraryManager.GetItemById(result.Items[0].Parent.InternalId);

            if (!(room is null))
                try
                {
                    EmbyControllerUtility.Instance.BrowseItemAsync(room.Name, session.User, season);
                }
                catch (Exception exception)
                {
                    responseClient.PostProgressiveResponse(exception.Message, apiAccessToken, requestId);
                    room = null;
                }

            var documentTemplateInfo = new RenderDocumentTemplateInfo()
            {
                baseItems          = result.Items.ToList(),
                renderDocumentType = RenderDocumentType.ITEM_LIST_SEQUENCE_TEMPLATE,
                HeaderTitle        = $"Season {seasonNumber}"
            };

            session.NowViewingBaseItem = season;
            session.room = room; 
            AlexaSessionManager.Instance.UpdateSession(session, documentTemplateInfo);

            return responseClient.BuildAlexaResponse(new Response()
            {
                outputSpeech = new OutputSpeech()
                {
                    phrase = $"Season { seasonNumber}"
                },
                shouldEndSession = null,
                directives       = new List<Directive>()
                {
                    RenderDocumentBuilder.Instance.GetRenderDocumentTemplate(documentTemplateInfo, session)
                }
            }, session.alexaSessionDisplayType);

        }
    }
}
