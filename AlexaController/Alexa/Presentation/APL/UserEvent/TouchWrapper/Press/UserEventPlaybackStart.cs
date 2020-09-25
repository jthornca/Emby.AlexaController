﻿using System.Collections.Generic;
using System.Threading.Tasks;
using AlexaController.Alexa.ResponseData.Model;
using AlexaController.Api;
using AlexaController.Session;
using AlexaController.Utils;
using AlexaController.Utils.SemanticSpeech;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;


namespace AlexaController.Alexa.Presentation.APL.UserEvent.TouchWrapper.Press
{
    public class UserEventPlaybackStart : IUserEventResponse
    {
        public IAlexaRequest AlexaRequest { get; }
        

        public UserEventPlaybackStart(IAlexaRequest alexaRequest)
        {
            AlexaRequest = alexaRequest;
        }
        public async Task<string> Response()
        { 
            EmbyServerEntryPoint.Instance.Log.Info("Playback endpoint hit!");
            var source = AlexaRequest.request.source;
            var session = AlexaSessionManager.Instance.GetSession(AlexaRequest);
            var baseItem = EmbyServerEntryPoint.Instance.GetItemById(source.id);
            var room =  session.room;
            
            var responseData = new Response();
            
            if (room is null)
            {
                session.NowViewingBaseItem = baseItem;
                AlexaSessionManager.Instance.UpdateSession(session);

                EmbyServerEntryPoint.Instance.Log.Info("Playback endpoint needs a room! for " + baseItem.Name);

                responseData.shouldEndSession = null;
                responseData.directives = new List<IDirective>()
                {
                    RenderDocumentBuilder.Instance.GetRenderDocumentTemplate(new RenderDocumentTemplate()
                    {
                        renderDocumentType = RenderDocumentType.ROOM_SELECTION_TEMPLATE,
                        baseItems          = new List<BaseItem>() { baseItem },
                        
                    }, session)
                };

                var t = await ResponseClient.Instance.BuildAlexaResponse(responseData, AlexaSessionDisplayType.ALEXA_PRESENTATION_LANGUAGE);
                return t;
            }

            session.PlaybackStarted = true;
            AlexaSessionManager.Instance.UpdateSession(session);

            Task.Run(() => EmbyServerEntryPoint.Instance.PlayMediaItemAsync(session, baseItem));

            return await ResponseClient.Instance.BuildAlexaResponse(new Response()
            {
                person = session.person,
                outputSpeech = new OutputSpeech()
                {
                    phrase         = SpeechStrings.GetPhrase(SpeechResponseType.PLAY_MEDIA_ITEM, session, new List<BaseItem>() {baseItem}),
                    speechType = SpeechType.COMPLIANCE
                },
                shouldEndSession = null,
                directives = new List<IDirective>()
                {
                    RenderDocumentBuilder.Instance.GetRenderDocumentTemplate(new RenderDocumentTemplate()
                    {
                        baseItems          = new List<BaseItem>() {baseItem},
                        renderDocumentType = RenderDocumentType.ITEM_DETAILS_TEMPLATE

                    }, session)
                }
            }, AlexaSessionDisplayType.ALEXA_PRESENTATION_LANGUAGE);   
            
        }
    }
}
