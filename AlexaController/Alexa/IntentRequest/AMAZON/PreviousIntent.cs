﻿// ReSharper disable TooManyChainedReferences
// ReSharper disable TooManyDependencies
// ReSharper disable once UnusedAutoPropertyAccessor.Local
// ReSharper disable once ExcessiveIndentation
// ReSharper disable twice ComplexConditionExpression
// ReSharper disable PossibleNullReferenceException
// ReSharper disable TooManyArguments

using System.Collections.Generic;
using AlexaController.Alexa.RequestData.Model;
using AlexaController.Alexa.ResponseData.Model;
using AlexaController.Api;
using AlexaController.Session;
using AlexaController.Utils;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;

namespace AlexaController.Alexa.IntentRequest.AMAZON
{
    [Intent]
    public class PreviousIntent : IIntentResponseModel
    {
        public string Response
        (AlexaRequest alexaRequest, AlexaSession session, IResponseClient responseClient, ILibraryManager libraryManager, ISessionManager sessionManager, IUserManager userManager)
        {
            
            var previousPage = session.paging.pages[session.paging.currentPage - 1];
            var currentPage  = session.paging.pages[session.paging.currentPage];

            AlexaSessionManager.Instance.UpdateSession(session, currentPage, true);

            //if the user has requested an Emby client/room display during the session - go back on both if possible
            if (session.room != null)
                try { EmbyControllerUtility.Instance.BrowseItemAsync(session.room.Name, session.User, libraryManager.GetItemById(session.NowViewingBaseItem.Parent.InternalId)); } catch { }

            return responseClient.BuildAlexaResponse(new Response()
            {
                shouldEndSession = null,
                directives = new List<Directive>()
                {
                    RenderDocumentBuilder.Instance.GetRenderDocumentTemplate(previousPage, session)
                }

            }, session.alexaSessionDisplayType);
        }
    }
}