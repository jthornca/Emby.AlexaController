﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AlexaController.Alexa.IntentRequest.Rooms;
using AlexaController.Alexa.RequestModel;
using AlexaController.Alexa.ResponseModel;
using AlexaController.AlexaDataSourceManagers;
using AlexaController.AlexaPresentationManagers;
using AlexaController.Api;
using AlexaController.Session;

namespace AlexaController.Alexa.IntentRequest.Browse
{
    [Intent]
    // ReSharper disable once UnusedType.Global
    public class DisplayItemFromEchoShowToDevice : IntentResponseBase<IAlexaRequest, IAlexaSession>, IIntentResponse
    {
        public IAlexaRequest AlexaRequest { get; }
        public IAlexaSession Session { get; }

        public DisplayItemFromEchoShowToDevice(IAlexaRequest alexaRequest, IAlexaSession session) : base(alexaRequest, session)
        {
            AlexaRequest = alexaRequest;
            Session = session;
        }
       
        public async Task<string> Response()
        {
            Session.room = RoomManager.Instance.ValidateRoom(AlexaRequest, Session);
            Session.hasRoom = !(Session.room is null);
            if (!Session.hasRoom && !Session.supportsApl)
            {
                Session.PersistedRequestContextData = AlexaRequest;
                AlexaSessionManager.Instance.UpdateSession(Session, null);
                return await RoomManager.Instance.RequestRoom(AlexaRequest, Session);
            }

            AlexaSessionManager.Instance.UpdateSession(Session, null);

            try
            {
                await ServerController.Instance.BrowseItemAsync(Session, Session.NowViewingBaseItem);
            }
            catch (Exception exception)
            {
                ServerController.Instance.Log.Error(exception.Message);
            }

            var aplaDataSource       = await APLA_DataSourceManager.Instance.ItemBrowse(Session.NowViewingBaseItem, Session);
            var renderAudioDirective = await APLA_RenderDocumentManager.Instance.GetAudioDirectiveAsync(aplaDataSource);

            return await AlexaResponseClient.Instance.BuildAlexaResponseAsync(new Response()
            {
                shouldEndSession = null,
                SpeakUserName = true,
                directives = new List<IDirective>()
                {
                    renderAudioDirective
                }

            }, Session);

        }
    }
}
