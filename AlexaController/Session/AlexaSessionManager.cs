﻿using System;
using System.Collections.Generic;
using System.Linq;
using AlexaController.Alexa.RequestData.Model;
using AlexaController.Alexa.Viewport;
using AlexaController.Api;
using AlexaController.Configuration;
using AlexaController.Utils;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Logging;
using User = MediaBrowser.Controller.Entities.User;


namespace AlexaController.Session
{
    public interface IAlexaSessionManager
    {
        void EndSession(AlexaRequest alexaRequest);
        AlexaSession GetSession(AlexaRequest alexaRequest, User user = null);
        void UpdateSession(AlexaSession session, RenderDocumentTemplateInfo templateInfo = null, bool? isBack = null);
        //bool ValidateRoomConfiguration(string room, PluginConfiguration config);

        //Room ValidateRoom(AlexaRequest alexaRequest, AlexaSession session);
        //void UpdateSessionRenderDocumentPages(AlexaSession session, RenderDocumentTemplateInfo templateInfo = null, bool? goBack = null);
    }

    public class AlexaSessionManager : IAlexaSessionManager, IServerEntryPoint
    {
        public static IAlexaSessionManager Instance { get; private set; }

        private static readonly List<AlexaSession> OpenSessions = new List<AlexaSession>();

        private ISessionManager SessionManager { get; }
        private ILogger log { get; }
        public AlexaSessionManager(ISessionManager sessionManager, ILogManager logMan)
        {
            Instance = this;
            SessionManager = sessionManager;
            log = logMan.GetLogger(Plugin.Instance.Name);
        }

        private static AlexaSessionDisplayType GetCurrentViewport(AlexaRequest alexaRequest)
        {
            var viewportUtility = new ViewportUtility();
            var viewportProfile = viewportUtility.GetViewportProfile(alexaRequest.context.Viewport);

            if (viewportProfile == ViewportProfile.UNKNOWN_VIEWPORT_PROFILE) return AlexaSessionDisplayType.NONE;

            return !(alexaRequest.context.Viewports is null)
                ? viewportUtility.ViewportSizeIsLessThen(viewportProfile, ViewportProfile.TV_LANDSCAPE_MEDIUM)
                    ? alexaRequest.context.Viewports[0].type == "APL"
                        ? AlexaSessionDisplayType.ALEXA_PRESENTATION_LANGUAGE : AlexaSessionDisplayType.NONE : AlexaSessionDisplayType.NONE : AlexaSessionDisplayType.NONE;
        }


        public void EndSession(AlexaRequest alexaRequest)
        {
            OpenSessions.RemoveAll(s => s.SessionId.Equals(alexaRequest.session.sessionId));
        }

        public AlexaSession GetSession(AlexaRequest alexaRequest, User user = null)
        {
            // A UserEvent can only happen in an open session because sessions will always start with voice.
            if (string.Equals(alexaRequest.request.type, "Alexa.Presentation.APL.UserEvent"))
            {
                return OpenSessions.FirstOrDefault(s => s.SessionId == alexaRequest.session.sessionId);
            }

            var context = alexaRequest.context;
            var system = context.System;
            var person = system.person;
            var amazonSession = alexaRequest.session;

            AlexaRequest persistedRequestData = null;
            AlexaSession sessionInfo = null;
            Room room = null;
            if (OpenSessions.Exists(s => s.SessionId.Equals(amazonSession.sessionId)))
            {
                // Not a new session open with AMAZON, we should have a corresponding session
                // already registered in the "OpenSessions" list
                sessionInfo = OpenSessions.FirstOrDefault(s => s.SessionId == amazonSession.sessionId);

                /*
                    We need this persist data so we can follow up if Alexa needs more information, 
                    and have context for the conversation.
                    for example, requesting a "Room Name/Device" to access media on, without request the media name again
                    from the first request.
                */
                persistedRequestData = sessionInfo?.PersistedRequestData;
                room = sessionInfo?.room;
                /*
                    Check to see if the person object has changed in the Alexa Session.
                    Someone else may have taken control of the open session by speaking with Alexa.
                    We may need to update the person object if they are new, so we don't display media outside
                    the scope of parental controls.
                */
                // ReSharper disable once ComplexConditionExpression
                if (!(person is null) && !(sessionInfo?.person is null))
                {
                    if (string.Equals(sessionInfo.person.personId, person.personId))
                    {
                        return sessionInfo; // It is the same person speaking - return the sessionInfo.
                    }
                }

                // Someone else must have taken control of the Alexa session.
                // Remove the session from the "OpenSessions" List, and rebuild the session with the new data
                OpenSessions.RemoveAll(s => s.SessionId.Equals(alexaRequest.session.sessionId));

            }

            // New session data.
            // We sync the AMAZON session Id with our own.
            sessionInfo = new AlexaSession()
            {
                SessionId = amazonSession.sessionId,
                DeviceId = system.device.deviceId,
                person = person,
                room = room,
                User = user,
                alexaSessionDisplayType = GetCurrentViewport(alexaRequest),
                PersistedRequestData = persistedRequestData,
                paging = new Paging { pages = new Dictionary<int, RenderDocumentTemplateInfo>() }
            };

            OpenSessions.Add(sessionInfo);

            return sessionInfo;
        }

        public void UpdateSession(AlexaSession session, RenderDocumentTemplateInfo templateInfo = null, bool? isBack = null)
        {
            if (!(templateInfo is null))
                session = UpdateSessionPaging(session, templateInfo, isBack);

            OpenSessions.RemoveAll(s => s.SessionId.Equals(session.SessionId));
            OpenSessions.Add(session);
        }

        private static AlexaSession UpdateSessionPaging(AlexaSession session, RenderDocumentTemplateInfo templateInfo, bool? isBack = null)
        {
            if (isBack == true)
            {
                session.paging.pages.Remove(session.paging.currentPage);
                session.paging.currentPage -= 1;

                if (session.paging.pages.Count <= 1) session.paging.canGoBack = false;
                
                return session;
            }


            if (session.paging.pages.Count == 0)
            {
                //set the pages dictionary with page 1
                session.paging.currentPage = 1;
                session.paging.pages.Add(session.paging.currentPage, templateInfo);

                return session;
            }

            if (!session.paging.pages.ContainsValue(templateInfo))
            {
                session.paging.currentPage += 1;
                session.paging.canGoBack = true;
                session.paging.pages.Add(session.paging.currentPage, templateInfo);

                return session;
            }

            return session;

        }

        public void Dispose()
        {

        }

        // ReSharper disable once MethodNameNotMeaningful
        public void Run()
        {
            SessionManager.PlaybackStart += SessionManager_PlaybackStart;
            SessionManager.PlaybackStopped += SessionManager_PlaybackStopped;

        }

        private void SessionManager_PlaybackStopped(object sender, MediaBrowser.Controller.Library.PlaybackStopEventArgs e)
        {
            var deviceName = e.DeviceName;
            var config = Plugin.Instance.Configuration;
            var configRooms = config.Rooms;

            if (!configRooms.Exists(r => r.Device == deviceName)) return;

            var room = configRooms.FirstOrDefault(r => r.Device == deviceName);

            if (!OpenSessions.Exists(session => session.room.Name == room?.Name)) return;

            var sessionToUpdate = OpenSessions.FirstOrDefault(session => session.room.Name == room?.Name);

            sessionToUpdate.PlaybackStarted = false;
            UpdateSession(sessionToUpdate);
        }

        private void SessionManager_PlaybackStart(object sender, MediaBrowser.Controller.Library.PlaybackProgressEventArgs e)
        {
            var deviceName = e.DeviceName;
            var config = Plugin.Instance.Configuration;
            var configRooms = config.Rooms;

            if (!configRooms.Exists(r => r.Device == deviceName)) return;

            var room = configRooms.FirstOrDefault(r => r.Device == deviceName);

            if (!OpenSessions.Exists(session => session.room.Name == room?.Name)) return;

            var sessionToUpdate = OpenSessions.FirstOrDefault(session => session.room.Name == room?.Name);
            ServerEntryPoint.Instance.Log.Info("ALEXA Playback started args session:" + sessionToUpdate?.room);

        }

        
    }
}