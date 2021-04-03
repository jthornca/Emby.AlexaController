﻿using AlexaController.EmbyAplDataSource.DataSourceProperties;
using AlexaController.Session;
using MediaBrowser.Controller.Entities;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;

namespace AlexaController.EmbyAplDataSource
{
    public class DataSourcePropertiesManager : SpeechBuilderService
    {
        public static DataSourcePropertiesManager Instance { get; private set; }

        public DataSourcePropertiesManager()
        {
            Instance = this;
        }

        public async Task<Properties<MediaItem>> GetSequenceViewPropertiesAsync(List<BaseItem> collectionItems, BaseItem collection = null)
        {
            var textInfo = CultureInfo.CurrentCulture.TextInfo;
            var mediaItems = new List<MediaItem>();

            //All the items in the list are of the same type, grab the first one and get the type
            var type = collectionItems[0].GetType().Name;

            collectionItems.ForEach(i => mediaItems.Add(new MediaItem()
            {
                type = type,
                primaryImageSource = ServerQuery.Instance.GetPrimaryImageSource(i),
                backdropImageSource = ServerQuery.Instance.GetBackdropImageSource(i),
                id = i.InternalId,
                name = i.Name,
                index = type == "Episode" ? $"Episode {i.IndexNumber}" : string.Empty,
                premiereDate = i.PremiereDate?.ToString("D")

            }));

            MediaItem mediaItem = null;
            if (collection != null)
            {
                mediaItem = new MediaItem()
                {
                    name = textInfo.ToTitleCase(collection.Name.ToLower()),
                    logoImageSource = ServerQuery.Instance.GetLogoImageSource(collection),
                    backdropImageSource = ServerQuery.Instance.GetBackdropImageSource(collection),
                    id = collection.InternalId,
                    genres = ServerQuery.Instance.GetGenres(collection)
                };
            }

            return await Task.FromResult(new Properties<MediaItem>()
            {
                url = await ServerQuery.Instance.GetLocalApiUrlAsync(),
                documentType = RenderDocumentType.MEDIA_ITEM_LIST_SEQUENCE_TEMPLATE,
                items = mediaItems,
                item = mediaItem
            });
        }
        public async Task<Properties<MediaItem>> GetBaseItemDetailViewPropertiesAsync(BaseItem item, IAlexaSession session)
        {
            var mediaItem = new MediaItem()
            {
                type = item.GetType().Name,
                isPlayed = item.IsPlayed(session.User),
                primaryImageSource = ServerQuery.Instance.GetPrimaryImageSource(item),
                id = item.InternalId,
                name = item.Name,
                premiereDate = item.ProductionYear.ToString(),
                officialRating = item.OfficialRating,
                tagLine = item.Tagline,
                runtimeMinutes = ServerQuery.Instance.GetRunTime(item),
                endTime = ServerQuery.Instance.GetEndTime(item),
                genres = ServerQuery.Instance.GetGenres(item),
                logoImageSource = ServerQuery.Instance.GetLogoImageSource(item),
                overview = item.Overview,
                videoBackdropSource = ServerQuery.Instance.GetVideoBackdropImageSource(item),
                backdropImageSource = ServerQuery.Instance.GetBackdropImageSource(item),
                videoOverlaySource = "/EmptyPng?quality=90",
                themeAudioSource = ServerQuery.Instance.GetThemeSongSource(item),
                TotalRecordCount = item.GetType().Name == "Series" ? ServerQuery.Instance.GetItemsResult(item.InternalId, new[] { "Season" }, session.User).TotalRecordCount : 0,
                chapterData = item.GetType().Name == "Movie" || item.GetType().Name == "Episode" ? ServerQuery.Instance.GetChapterInfo(item) : null
            };

            var similarItems = ServerQuery.Instance.GetSimilarItems(item);

            var recommendedItems = new List<MediaItem>();

            similarItems.ForEach(r => recommendedItems.Add(new MediaItem()
            {
                id = r.InternalId,
                thumbImageSource = ServerQuery.Instance.GetThumbImageSource(r)
            }));

            return await Task.FromResult(new Properties<MediaItem>()
            {
                url = await ServerQuery.Instance.GetLocalApiUrlAsync(),
                wanAddress = await ServerQuery.Instance.GetWanAddressAsync(),
                documentType = RenderDocumentType.MEDIA_ITEM_DETAILS_TEMPLATE,
                similarItems = recommendedItems,
                item = mediaItem
            });

        }
        public async Task<Properties<List<Value>>> GetHelpViewPropertiesAsync()
        {
            var helpContent = new List<Value>()
            {
                 new Value() {value = "Accessing Emby accounts based on your voice."},
                 new Value() {value = "I have the ability to access specific emby user accounts, and library data based on the sound of your voice. If you have not yet turned on Personalization in your Alexa App, and enabled it for this skill, please do that now."},
                 new Value() {value = "You can enable Parental Controls, so media will be filtered based on who is speaking. This way, media items will be filtered using voice verification."},
                 new Value() {value = "To enable this feature, open the Emby plugin configuration page, and toggle the button for \"Enable parental control using voice recognition\". If this feature is turned off, I will not filter media items, and will show media based on the Emby Administrators account, at all times."},
                 new Value() {value = "Select the \"New Authorization Account Link\" button, and follow the instructions to add personalization."},
                 new Value() {value = "Accessing media Items in rooms"},
                 new Value() {value = "The Emby plugin will allow you to create \"Rooms\", based on the devices in your home."},
                 new Value() {value = "In the plugin configuration, map each Emby ready device to a specific room. You will create the room name that I will understand."},
                 new Value() {value = "For example: map your Amazon Fire Stick 4K to a room named: \"Family Room\"."},
                 new Value() {value = "Now you can access titles and request them to display per room."},
                 new Value() {value = "You can use the phrase:"},
                 new Value() {value = "Ask home theater to play the movie Iron Man in the family room."},
                 new Value() {value = "To display the movies library on the \"Family Room\" device, you can use the phrase:"},
                 new Value() {value = "Ask home theater to show \"Movies\" in the Family Room."},
                 new Value() {value = "The same can be said for \"Collections\", and \"TV Series\" libraries"},
                 new Value() {value = "The Emby client must already be running on the device in order to access the room commands."},
                 new Value() {value = "Accessing Collection Items"},
                 new Value() {value = "I have the ability to show collection data on echo show, echo spot, or other devices with screens."},
                 new Value() {value = "To access this ability, you can use the following phrases:"},
                 new Value() {value = "Ask home theater to show all the Iron Man movies..."},
                 new Value() {value = "Ask home theater to show the Spiderman collection..."},
                 new Value() {value = "Fire TV devices, with Alexa enabled, are exempt from displaying these items because the Emby client will take care of this for you."},
                 new Value() {value = "Accessing individual media Items"},
                 new Value() {value = "I am able to show individual media items as well."},
                 new Value() {value = "You can use the phrases:"},
                 new Value() {value = "Ask home theater to show the movie Spiderman Far From Home..."},
                 new Value() {value = "Ask home theater to show the movie Spiderman Far From Home, in the Family Room."},
                 new Value() {value = "You can also access TV Series the same way."},
                 new Value() {value = "I will understand the phrases: "},
                 new Value() {value = "Ask home theater to show the series Westworld..."},
                 new Value() {value = "Ask home theater to show the next up episode for Westworld..."},
                 new Value() {value = "Accessing new media Items"},
                 new Value() {value = "To access new titles in your library, you can use the phrases:"},
                 new Value() {value = "Ask home theater about new movies..."},
                 new Value() {value = "Ask home theater about new TV Series..."},
                 new Value() {value = "You can also request a duration for new items... For example:"},
                 new Value() {value = "Ask home theater for new movies added in the last three days..."},
                 new Value() {value = "Ask home theater for new tv shows added in the last month"},
                 new Value() {value = "Remember that as long as the echo show, or spot is displaying an image, you are in an open session. This means you don't have to use the \"Ask home theater\" phrases to access media"},
                 new Value() {value = "This concludes the help section. Good luck!"},
            };

            return await Task.FromResult(new Properties<List<Value>>
            {
                documentType = RenderDocumentType.HELP_TEMPLATE,
                values = helpContent
            });
        }
        public async Task<Properties<string>> GetGenericViewPropertiesAsync(string text, string videoUrl)
        {
            return await Task.FromResult(new Properties<string>()
            {
                documentType = RenderDocumentType.GENERIC_VIEW_TEMPLATE,
                text = text,
                url = await ServerQuery.Instance.GetLocalApiUrlAsync(),
                wanAddress = await ServerQuery.Instance.GetWanAddressAsync(),
                videoUrl = videoUrl
            });
        }
        public async Task<Properties<MediaItem>> GetRoomSelectionViewPropertiesAsync(BaseItem item, IAlexaSession session)
        {
            return await Task.FromResult(new Properties<MediaItem>()
            {
                url = await ServerQuery.Instance.GetLocalApiUrlAsync(),
                documentType = RenderDocumentType.ROOM_SELECTION_TEMPLATE,
                item = new MediaItem()
                {
                    type = item.GetType().Name,
                    isPlayed = item.IsPlayed(session.User),
                    primaryImageSource = ServerQuery.Instance.GetPrimaryImageSource(item),
                    id = item.InternalId,
                    name = item.Name,
                    premiereDate = item.ProductionYear.ToString(),
                    officialRating = item.OfficialRating,
                    tagLine = item.Tagline,
                    runtimeMinutes = ServerQuery.Instance.GetRunTime(item),
                    endTime = ServerQuery.Instance.GetEndTime(item),
                    genres = ServerQuery.Instance.GetGenres(item),
                    logoImageSource = ServerQuery.Instance.GetLogoImageSource(item),
                    overview = item.Overview,
                    videoBackdropSource = ServerQuery.Instance.GetVideoBackdropImageSource(item),
                    backdropImageSource = ServerQuery.Instance.GetBackdropImageSource(item)
                }
            });
        }

        public async Task<Properties<string>> GetAudioResponsePropertiesAsync(SpeechResponsePropertiesQuery speechResponsePropertiesQuery)
        {
            var speech = new StringBuilder();
            switch (speechResponsePropertiesQuery.SpeechResponseType)
            {
                case SpeechResponseType.PersonNotRecognized:
                    PersonNotRecognized(speech);
                    break;
                case SpeechResponseType.OnLaunch:
                    OnLaunch(speech);
                    break;
                case SpeechResponseType.NotUnderstood:
                    NotUnderstood(speech);
                    break;
                case SpeechResponseType.NoItemExists:
                    NoItemExists(speech);
                    break;
                case SpeechResponseType.ItemBrowse:
                    ItemBrowse(speech, speechResponsePropertiesQuery.item, speechResponsePropertiesQuery.session, speechResponsePropertiesQuery.deviceAvailable);
                    break;
                case SpeechResponseType.BrowseNextUpEpisode:
                    BrowseNextUpEpisode(speech, speechResponsePropertiesQuery.item, speechResponsePropertiesQuery.session);
                    break;
                case SpeechResponseType.NoNextUpEpisodeAvailable:
                    NoNextUpEpisodeAvailable(speech);
                    break;
                case SpeechResponseType.PlayNextUpEpisode:
                    PlayNextUpEpisode(speech, speechResponsePropertiesQuery.item, speechResponsePropertiesQuery.session);
                    break;
                case SpeechResponseType.ParentalControlNotAllowed:
                    ParentalControlNotAllowed(speech, speechResponsePropertiesQuery.item, speechResponsePropertiesQuery.session);
                    break;
                case SpeechResponseType.PlayItem:
                    PlayItem(speech, speechResponsePropertiesQuery.item);
                    break;
                case SpeechResponseType.RoomContext:
                    RoomContext(speech);
                    break;
                case SpeechResponseType.VoiceAuthenticationExists:
                    VoiceAuthenticationExists(speech, speechResponsePropertiesQuery.session);
                    break;
                case SpeechResponseType.VoiceAuthenticationAccountLinkError:
                    VoiceAuthenticationAccountLinkError(speech);
                    break;
                case SpeechResponseType.VoiceAuthenticationAccountLinkSuccess:
                    VoiceAuthenticationAccountLinkSuccess(speech, speechResponsePropertiesQuery.session);
                    break;
                case SpeechResponseType.UpComingEpisodes:
                    UpComingEpisodes(speech, speechResponsePropertiesQuery.items, speechResponsePropertiesQuery.date);
                    break;
                case SpeechResponseType.NewLibraryItems:
                    NewLibraryItems(speech, speechResponsePropertiesQuery.items, speechResponsePropertiesQuery.date, speechResponsePropertiesQuery.session);
                    break;
                case SpeechResponseType.BrowseItemByActor:
                    BrowseItemByActor(speech, speechResponsePropertiesQuery.items);
                    break;
                default: return null;
            }
            return await Task.FromResult(new Properties<string>()
            {
                value = speech.ToString(),
                audioUrl = "soundbank://soundlibrary/computers/beeps_tones/beeps_tones_13"
            });
        }
    }
}