﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AlexaController.Utils;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Session;
using MediaBrowser.Controller.TV;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using User = MediaBrowser.Controller.Entities.User;


namespace AlexaController
{
    
    // ReSharper disable once ClassTooBig
    public class ServerQuery : SearchUtility, IServerEntryPoint 
    {
        private IServerApplicationHost Host            { get; }
        private IUserManager UserManager               { get; }
        private ILibraryManager LibraryManager         { get; }
        private ITVSeriesManager TvSeriesManager       { get; }
        private ISessionManager SessionManager         { get; }
       
        public static ServerQuery Instance             { get; private set; }

        // ReSharper disable once TooManyDependencies
        public ServerQuery(ILibraryManager libMan, ITVSeriesManager tvMan, ISessionManager sesMan, IServerApplicationHost host, IUserManager userManager) : base(libMan, userManager)
        {
            Host            = host;
            LibraryManager  = libMan;
            TvSeriesManager = tvMan;
            SessionManager  = sesMan;
            UserManager     = userManager;
            Instance        = this;
        }
        
        public IEnumerable<SessionInfo> GetCurrentSessions()
        {
            return SessionManager.Sessions;
        }

        public QueryResult<BaseItem> GetItemsResult(BaseItem parent, string[] types, User user)
        {
            var result = LibraryManager.GetItemsResult(new InternalItemsQuery(user)
            {
                Parent = parent,
                IncludeItemTypes = types,
                Recursive = true
            });
            return result;
        }

        public async Task<string> GetLocalApiUrlAsync()
        {
            return await Host.GetLocalApiUrl(CancellationToken.None);
        }

        public List<BaseItem> GetEpisodes(int seasonNumber, BaseItem parent, User user)
        {
            var result = LibraryManager.GetItemsResult(new InternalItemsQuery(user)
            {
                Parent = parent,
                IncludeItemTypes = new[] { "Episode" },
                ParentIndexNumber = seasonNumber,
                Recursive = true
            });
            return result.Items.ToList();
        }
        
        public BaseItem GetItemById<T>(T id)
        {
            return LibraryManager.GetItemById(id.ToString());
        }

        public BaseItem GetNextUpEpisode(string seriesName, User user)
        {
            try
            {
                var id     = QuerySpeechResultItem(seriesName, new[] { "Series" }).InternalId;
                var nextUp = TvSeriesManager.GetNextUp(new NextUpQuery()
                {
                    SeriesId = id,
                    UserId   = user.InternalId
                }, user, new DtoOptions());
                
                return nextUp.Items.FirstOrDefault();
            }
            catch
            {
                return null; //Return null and handle no next up episodes in Alexa Response
            }
        }

        public string GetLibraryId(string name)
        {
            return LibraryManager.GetVirtualFolders().FirstOrDefault(r => r.Name == name)?.ItemId;
        }
        
        public Dictionary<BaseItem, List<BaseItem>> GetCollectionItems(User user, string collectionName)
        {
            var result = QuerySpeechResultItem(collectionName, new[] { "BoxSet" });

            ServerController.Instance.Log.Info("Search found collection item: " + result.Name);

            var collection = LibraryManager.QueryItems(new InternalItemsQuery(UserManager.Users.FirstOrDefault(u => u.Policy.IsAdministrator))
            {
                ListIds        = new[] { result.InternalId },
                EnableAutoSort = true,
                OrderBy        = new[] { ItemSortBy.PremiereDate }.Select(i => new ValueTuple<string, SortOrder>(i, SortOrder.Ascending)).ToArray(),
                
            });
            
            return new Dictionary<BaseItem, List<BaseItem>>() { { result, collection.Items.ToList() } };

        }

        public IEnumerable<BaseItem> GetLatestMovies(User user, DateTime duration)
        {
            // Duration can be request from the user, but will default to anything add in the last 25 days.
            var results = LibraryManager.GetItemIds(new InternalItemsQuery
            {
                IncludeItemTypes = new[] { "Movie" },
                User             = user,
                MinDateCreated   = duration,
                Limit            = 20,
                EnableAutoSort   = true,
                OrderBy          = new[] { ItemSortBy.DateCreated }.Select(i => new ValueTuple<string, SortOrder>(i, SortOrder.Descending)).ToArray()
            });
            
            return results.Select(id => LibraryManager.GetItemById(id)).ToList();
        }

        public IEnumerable<BaseItem> GetLatestTv(User user, DateTime duration)
        {
            // Duration can be request from the user, but will default to anything add in the last 25 days.
            var results = LibraryManager.GetItemIds(new InternalItemsQuery
            {
                IncludeItemTypes = new[] { "Episode" },
                User             = user,
                MinDateCreated   = duration,
                IsPlayed         = false,
                EnableAutoSort   = true,
                OrderBy          = new[] { ItemSortBy.DateCreated }.Select(i => new ValueTuple<string, SortOrder>(i, SortOrder.Descending)).ToArray()
            });

            return results.Select(id => LibraryManager.GetItemById(id).Parent.Parent).Distinct().ToList();
        }

        public string GetDeviceIdFromRoomName(string roomName)
        {
            var config = Plugin.Instance.Configuration;
            if (!config.Rooms.Any())
            {
                throw new Exception("There are no rooms setup in configuration.");
            }

            var room = config.Rooms.FirstOrDefault(r => string.Equals(r.Name, roomName, StringComparison.CurrentCultureIgnoreCase));
            if (room is null)
            {
                throw new Exception("That room doesn't exist in the plugin configuration.");
            }

            var device = room.DeviceName;
            if (!ReferenceEquals(null, SessionManager.Sessions.FirstOrDefault(d => d.DeviceName == device)))
            {
                return SessionManager.Sessions.FirstOrDefault(d => d.DeviceName == device)?.DeviceId;
            }
            if (!ReferenceEquals(null, SessionManager.Sessions.FirstOrDefault(d => d.Client == device)))
            {
                return SessionManager.Sessions.FirstOrDefault(d => d.Client == device)?.DeviceId;
            }

            throw new Exception($"{room}'s device is currently unavailable.");
        }

        public SessionInfo GetSession(string deviceId)
        {
            return SessionManager.Sessions.FirstOrDefault(i => i.DeviceId == deviceId);
        }

        public QueryResult<BaseItem> GetBaseItemsByGenre(string[] type, string[] genres)
        {
            return LibraryManager.GetItemsResult(new InternalItemsQuery()
            {
                Genres = genres,
                IncludeItemTypes = type,
                Recursive = true,
                Limit = 15
            });
        }

        public IDictionary<List<BaseItem>, List<BaseItem>> GetItemsByActor(User user, List<string> actorNames)
        {
            var actors = new List<BaseItem>();

            foreach (var actor in actorNames)
            {
                var actorName = StringNormalization.ValidateSpeechQueryString(actor);
                var actorQuery = LibraryManager.GetItemsResult(new InternalItemsQuery()
                {
                    IncludeItemTypes = new []{ "Person" },
                    SearchTerm = actorName,
                    Recursive = true
                });
          
                if (actorQuery.TotalRecordCount <= 0) continue;

                actors.Add(actorQuery.Items[0]);
            }

           
            var query = LibraryManager.GetItemsResult(new InternalItemsQuery(user)
            {
                IncludeItemTypes = new []{"Series", "Movie"},
                Recursive = true,
                PersonIds = actors.Select(a => a.InternalId).ToArray()
            });

            return new Dictionary<List<BaseItem>, List<BaseItem>>() {{ actors, query.Items.ToList() }};

        }

        public async Task<QueryResult<BaseItem>> GetUpComingTvAsync(DateTime duration)
        {
            return await Task.FromResult(LibraryManager.GetItemsResult(new InternalItemsQuery()
            {
                MinPremiereDate  = DateTime.Now.AddDays(-1),
                IncludeItemTypes = new[] {"Episode"},
                MaxPremiereDate  = duration,
                OrderBy          = new[] { ItemSortBy.PremiereDate }.Select(i => new ValueTuple<string, SortOrder>(i, SortOrder.Ascending)).ToArray()
            }));

            //return await Task.FromResult(upComing.Items.ToList());
        }

        public string GetRunTime(BaseItem baseItem)
        {
            if (!string.Equals(baseItem.GetType().Name, "Movie")) return string.Empty;
            var runTimeTicks = baseItem.RunTimeTicks;
            return !(runTimeTicks is null) ? $"{TimeSpan.FromTicks(runTimeTicks.Value).TotalMinutes.ToString(CultureInfo.InvariantCulture).Split('.')[0]} minutes" : string.Empty;
        }

        public string GetEndTime(BaseItem baseItem)
        {
            return
                $"Ends at: {DateTime.Now.AddTicks(baseItem.GetRunTimeTicksForPlayState()).ToString("h:mm tt", CultureInfo.InvariantCulture)}";
        }

        public string GetPrimaryImageUrl(BaseItem item)
        {
            
            if (item.GetType().Name != "Episode")
            {
                return $"/Items/{item.InternalId}/Images/primary?quality=90&amp;maxHeight=1008&amp;maxWidth=700&amp;";
            }

            return item.HasImage(ImageType.Primary) 
                ? $"/Items/{ item.InternalId }/Images/primary?quality=90&amp;maxHeight=508&amp;maxWidth=600&amp;" 
                : $"/Items/{ item.InternalId }/Images/backdrop/0?quality=90&amp;maxHeight=508&amp;maxWidth=600&amp;";
        }

        public string GetVideoOverlay()
        {
            return "/EmptyPng?quality=90";
        }

        public string GetBackdropImageUrl(BaseItem item)
        {
            var internalId = item.InternalId;
            return $"/Items/{internalId}/Images/backdrop?maxWidth=1200&amp;maxHeight=800&amp;quality=90";
        }

        public string GetLogoUrl(BaseItem item)
        {
            return item.HasImage(ImageType.Logo)
                ? $"/Items/{item.InternalId}/Images/logo?quality=90&maxHeight=508&maxWidth=200" : "";
        }

        public string GetVideoBackdropUrl(BaseItem item)
        {
            var videoBackdropIds = item.ThemeVideoIds;
            return videoBackdropIds.Length > 0 ? $"/videos/{GetItemById(videoBackdropIds[0]).InternalId}/stream.mp4" : string.Empty;
        }

        public void Dispose()
        {
            
        }

        // ReSharper disable once MethodNameNotMeaningful
        public void Run()
        {
            
        }

        
    }
}