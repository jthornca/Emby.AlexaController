﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AlexaController.Alexa.IntentRequest.Rooms;
using AlexaController.Alexa.Presentation.APLA.Components;
using AlexaController.Alexa.Presentation.DirectiveBuilders;
using AlexaController.Alexa.RequestData.Model;
using AlexaController.Alexa.ResponseData.Model;
using AlexaController.Api;
using AlexaController.Session;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Logging;

// ReSharper disable TooManyChainedReferences
// ReSharper disable once ComplexConditionExpression

namespace AlexaController.Alexa.IntentRequest.Playback
{
    [Intent]
    public class PlayItemIntent : IIntentResponse
    {
        //If no room is requested in the PlayItemIntent intent, we follow up immediately to get a room value from 'RoomName' intent. 

        public IAlexaRequest AlexaRequest { get; }
        public IAlexaSession Session      { get; }
        
        public PlayItemIntent(IAlexaRequest alexaRequest, IAlexaSession session)
        {
            AlexaRequest = alexaRequest;
            Session      = session;
        }

        public async Task<string> Response()
        {
            try { Session.room = RoomManager.Instance.ValidateRoom(AlexaRequest, Session); } catch { }
            if (Session.room is null) return await RoomManager.Instance.RequestRoom(AlexaRequest, Session);
            
            var request        = AlexaRequest.request;
            var context        = AlexaRequest.context;
            var apiAccessToken = context.System.apiAccessToken;
            var requestId      = request.requestId;
            var intent         = request.intent;
            var slots          = intent.slots;

            //var progressiveSpeech = await SpeechStrings.GetPhrase(new RenderAudioTemplate()
            //{
            //    type = SpeechResponseType.PROGRESSIVE_RESPONSE, 
            //    session = Session
            //});

#pragma warning disable 4014
            ResponseClient.Instance.PostProgressiveResponse("One moment please...", apiAccessToken, requestId).ConfigureAwait(false);
#pragma warning restore 4014

            BaseItem result = null;
            if (Session.NowViewingBaseItem is null)
            {
                var type = slots.Movie.value is null ? "Series" : "Movie";
                result = ServerQuery.Instance.QuerySpeechResultItem(
                    type == "Movie" ? slots.Movie.value : slots.Series.value, new[] { type });
            }
            else
            {
                result = Session.NowViewingBaseItem;
            }
            
            //Item doesn't exist in the library
            if (result is null)
            {
                return await ResponseClient.Instance.BuildAlexaResponse(new Response()
                {
                    shouldEndSession = true,
                    directives = new List<IDirective>()
                    {
                        await RenderAudioBuilder.Instance.GetAudioDirectiveAsync(new RenderAudioTemplate()
                        {
                            speechContent = SpeechContent.GENERIC_ITEM_NOT_EXISTS_IN_LIBRARY,
                            audio = new Audio()
                            {
                                source ="soundbank://soundlibrary/computers/beeps_tones/beeps_tones_13",
                                
                            }
                        })
                    }
                    //outputSpeech = new OutputSpeech()
                    //{
                    //    phrase = await SpeechStrings.GetPhrase(new SpeechStringQuery()
                    //    {
                    //        type = SpeechResponseType.GENERIC_ITEM_NOT_EXISTS_IN_LIBRARY,
                    //        session = Session
                    //    }),
                    //}
                }, Session);
            }
            
            //Parental Control check for baseItem
            if (!result.IsParentalAllowed(Session.User))
            {
                if (Plugin.Instance.Configuration.EnableServerActivityLogNotifications)
                {
                    await ServerController.Instance.CreateActivityEntry(LogSeverity.Warn,
                        $"{Session.User} attempted to view a restricted item.", $"{Session.User} attempted to view {result.Name}.").ConfigureAwait(false);
                }

                return await ResponseClient.Instance.BuildAlexaResponse(new Response()
                {
                    shouldEndSession = true,
                    SpeakUserName = true,
                    directives = new List<IDirective>()
                    {
                        await RenderDocumentBuilder.Instance.GetRenderDocumentDirectiveAsync(
                            new RenderDocumentTemplate()
                            {
                                renderDocumentType = RenderDocumentType.GENERIC_HEADLINE_TEMPLATE,
                                HeadlinePrimaryText = $"Stop! Rated {result.OfficialRating}"

                            }, Session),
                        await RenderAudioBuilder.Instance.GetAudioDirectiveAsync(
                            new RenderAudioTemplate()
                            {
                                speechContent = SpeechContent.PARENTAL_CONTROL_NOT_ALLOWED,
                                audio = new Audio()
                                {
                                    source ="soundbank://soundlibrary/computers/beeps_tones/beeps_tones_13",
                                    
                                }
                            })
                    }
                    //outputSpeech = new OutputSpeech()
                    //{
                    //    phrase = await SpeechStrings.GetPhrase(new RenderAudioTemplate()
                    //    {
                    //        type    = SpeechResponseType.PARENTAL_CONTROL_NOT_ALLOWED, 
                    //        session = Session, 
                    //        items   = new List<BaseItem>() { result }
                    //    }),
                    //    sound = "<audio src=\"soundbank://soundlibrary/musical/amzn_sfx_electronic_beep_02\"/>"
                    //}
                }, Session);
            }

            try
            {
#pragma warning disable 4014
                await ServerController.Instance.PlayMediaItemAsync(Session, result);
#pragma warning restore 4014
            }
            catch (Exception exception)
            {
#pragma warning disable 4014
                Task.Run(() => ResponseClient.Instance.PostProgressiveResponse(exception.Message, apiAccessToken, requestId)).ConfigureAwait(false);
#pragma warning restore 4014
                await Task.Delay(1200);
            }

            Session.PlaybackStarted = true;
            AlexaSessionManager.Instance.UpdateSession(Session, null);

            var documentTemplateInfo = new RenderDocumentTemplate()
            {
                renderDocumentType = RenderDocumentType.ITEM_DETAILS_TEMPLATE,
                baseItems = new List<BaseItem>() {result}
            };

            var renderAudioTemplateInfo = new RenderAudioTemplate()
            {
                speechContent = SpeechContent.PLAY_MEDIA_ITEM,
                session = Session,
                items = new List<BaseItem>() { result },
                audio = new Audio()
                {
                    source ="soundbank://soundlibrary/computers/beeps_tones/beeps_tones_13",
                    
                }
            };

            var renderDocumentDirective = await RenderDocumentBuilder.Instance.GetRenderDocumentDirectiveAsync(documentTemplateInfo, Session);
            var renderAudioDirective    = await RenderAudioBuilder.Instance.GetAudioDirectiveAsync(renderAudioTemplateInfo);

            return await ResponseClient.Instance.BuildAlexaResponse(new Response()
            {
                //outputSpeech = new OutputSpeech()
                //{
                //    phrase = await SpeechStrings.GetPhrase(new RenderAudioTemplate()
                //    {
                //        type = SpeechResponseType.PLAY_MEDIA_ITEM, 
                //        session = Session, 
                //        items = new List<BaseItem>() { result }
                //    })
                //},
                SpeakUserName = true,
                shouldEndSession = null,
                directives = new List<IDirective>()
                {
                    renderDocumentDirective,
                    renderAudioDirective
                }

            }, Session);

        }
    }
}
