﻿using System.Collections.Generic;
using System.Linq;
using AlexaController.Alexa.ResponseData.Model;
using AlexaController.Api;
using AlexaController.Configuration;
using AlexaController.Session;
using AlexaController.Utils;
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

namespace AlexaController.Alexa.Presentation.APL.UserEvent.TouchWrapper.Press
{
    public class UserEventShowItemListSequenceTemplate : IUserEventResponse
    {
        public IAlexaRequest AlexaRequest { get; }
        public IAlexaEntryPoint Alexa { get; }

        public UserEventShowItemListSequenceTemplate(IAlexaRequest alexaRequest, IAlexaEntryPoint alexa)
        {
            AlexaRequest = alexaRequest;
            Alexa = alexa;
        }
        public string Response()
        {
            var request  = AlexaRequest.request;
            var source   = request.source;
            var baseItem = Alexa.LibraryManager.GetItemById(source.id);
            var session  = AlexaSessionManager.Instance.GetSession(AlexaRequest);
            var room     = session.room;
            var type     = baseItem.GetType().Name;

           
            var phrase = "";
            
            var result   = Alexa.LibraryManager.GetItemsResult(new InternalItemsQuery(session.User)
            {
                Parent           =  baseItem,
                IncludeItemTypes = new [] { type == "Series" ? "Season" : "Episode" },
                Recursive        = true
            });


            var documentTemplateInfo = new RenderDocumentTemplate()
            {
                baseItems          = result.Items.ToList(),
                renderDocumentType = RenderDocumentType.ITEM_LIST_SEQUENCE_TEMPLATE,
                HeaderTitle        = type == "Season" ? $"{baseItem.Parent.Name} > {baseItem.Name}" : baseItem.Name
            };
            
            // Update session data
            session.NowViewingBaseItem = baseItem;
            session.room = room;
            AlexaSessionManager.Instance.UpdateSession(session, documentTemplateInfo);


            //if the user has requested an Emby client/room display during the session - display both if possible
            if (room != null)
                try { EmbyControllerUtility.Instance.BrowseItemAsync(room.Name, session.User, baseItem); } catch { }
            

            return Alexa.ResponseClient.BuildAlexaResponse(new Response()
            {
                outputSpeech = new OutputSpeech()
                {
                    phrase = phrase
                },
                shouldEndSession = null,
                directives = new List<IDirective>()
                {
                    RenderDocumentBuilder.Instance.GetRenderDocumentTemplate(documentTemplateInfo, session)
                }
            }, AlexaSessionDisplayType.ALEXA_PRESENTATION_LANGUAGE);

        }
    }
}
