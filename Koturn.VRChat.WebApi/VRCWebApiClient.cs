using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Json;
using System.Net;
using System.Net.Http.Headers;
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
using System.Net.Http;
using Koturn.VRChat.WebApi.Enums;
using Koturn.VRChat.WebApi.Events;


namespace Koturn.VRChat.WebApi
{
    /// <summary>
    /// VRChat Web API client.
    /// </summary>
    public class VRCWebApiClient : IDisposable
    {
        /// <summary>
        /// Base URL of VRChat Web API.
        /// </summary>
        private const string BaseUrl = "https://api.vrchat.cloud/api/1";
        /// <summary>
        /// Default user agent name.
        /// </summary>
        private const string DefaultUserAgentName = "application/1.00 VRChatTool";
        /// <summary>
        /// Header string of User-Agent.
        /// </summary>
        private const string HeaderUserAgent = "User-Agent";
        /// <summary>
        /// Header string of Cookie.
        /// </summary>
        private const string HeaderCookie = "Cookie";


        public bool IsDisposed { get; private set; }
        public event EventHandler<HttpRequestEventArgs>? HttpRequestSending;
        public event EventHandler<HttpResponseEventArgs>? HttpResponseRecieved;

        /// <summary>
        /// HTTP client instance.
        /// </summary>
        private readonly HttpClient _client;


        /// <summary>
        /// Get or Set (Replace) Cookie string.
        /// </summary>
        public string Cookie
        {
            get
            {
                return _client.DefaultRequestHeaders.GetValues(HeaderCookie).First();
            }
            set
            {
                var headers = _client.DefaultRequestHeaders;
                if (headers.Contains(HeaderCookie))
                {
                    headers.Remove(HeaderCookie);
                }
                headers.Add(HeaderCookie, value);
            }
        }

        /// <summary>
        /// Initialize HTTP client with default uger agent name (<see cref="DefaultUserAgentName"/>).
        /// </summary>
        public VRCWebApiClient()
            : this(DefaultUserAgentName)
        {
        }

        /// <summary>
        /// Initialize HTTP client with specified uger agent name.
        /// </summary>
        /// <param name="userAgentName">User agent name.</param>
        public VRCWebApiClient(string userAgentName)
        {
            var client = new HttpClient();
            if (!client.DefaultRequestHeaders.TryAddWithoutValidation(HeaderUserAgent, userAgentName))
            {
                throw new InvalidOperationException($"Failed to add ${HeaderUserAgent}: {userAgentName}");
            }
            _client = client;
        }

        /// <summary>
        /// Initialize HTTP client with specified uger agent name and cookie.
        /// </summary>
        /// <param name="userAgentName">User agent name.</param>
        /// <param name="authCookie">Cookie (assume including "auth=authcookie_xxxxxx").</param>
        public VRCWebApiClient(string userAgentName, string authCookie)
            : this(userAgentName)
        {
            _client.DefaultRequestHeaders.Add(HeaderCookie, authCookie);
        }


        /// <summary>
        /// Get API Key.
        /// </summary>
        /// <returns>Task of getting API Key.</returns>
        /// <exception cref="HttpRequestException">Thrown when status code is not <see cref="HttpStatusCode.OK"/>.</exception>
        /// <remarks>
        /// This method send GET request to <see href="https://api.vrchat.cloud/api/1/config"/>.
        /// </remarks>
        public async Task<string> GetApiKeyAsync()
        {
            const string EndPoint = "/config";
            const string Url = BaseUrl + EndPoint;

            var response = await GetAsync(Url);
            var body = await response.Content.ReadAsStringAsync();

            return LoadJsonFromString(body)["clientApiKey"];
        }


        /// <summary>
        /// Get Auth token cookie and update client cookie.
        /// </summary>
        /// <param name="userName">User name.</param>
        /// <param name="password">Password.</param>
        /// <param name="apiKey">API Key.</param>
        /// <returns>Task of getting auth token cookie.</returns>
        /// <exception cref="HttpRequestException">Thrown when status code is not <see cref="HttpStatusCode.OK"/>
        /// or "Set-Cookie" is not found on response header.</exception>
        public async Task<string> GetAndUpdateAuthTokenCookie(string userName, string password, string apiKey)
        {
            var cookie = await GetAuthTokenCookie(userName, password, apiKey);
            Cookie = cookie;
            return cookie;
        }


        /// <summary>
        /// Get Auth token cookie.
        /// </summary>
        /// <param name="userName">User name.</param>
        /// <param name="password">Password.</param>
        /// <param name="apiKey">API Key.</param>
        /// <returns>Task of getting auth token cookie.</returns>
        /// <exception cref="HttpRequestException">Thrown when status code is not <see cref="HttpStatusCode.OK"/>
        /// or "Set-Cookie" is not found on response header.</exception>
        public async Task<string> GetAuthTokenCookie(string userName, string password, string apiKey)
        {
            const string EndPoint = "/auth/user";
            const string Url = BaseUrl + EndPoint;

            var paramPart = await new FormUrlEncodedContent(new Dictionary<string, string>()
            {
                {"apiKey", apiKey}
            }).ReadAsStringAsync();
            var request = new HttpRequestMessage(HttpMethod.Get, $"{Url}?{paramPart}");
            request.Headers.Authorization = new AuthenticationHeaderValue(
                "Basic",
                Convert.ToBase64String(Encoding.ASCII.GetBytes($"{userName}:{password}")));

            var response = await SendAsync(request);

            if (!response.Headers.Contains("Set-Cookie"))
            {
                ThrowHttpRequestException($"Set-Cookie not found", response.StatusCode);
            }

            return response.Headers.GetValues("Set-Cookie").First();
        }


        /// <summary>
        /// <para>Try to get current user information.</para>
        /// <para>This method is useful <see cref="Cookie"/> is enable or not.</para>
        /// </summary>
        /// <returns>Task of getting <see cref="ValueTuple"/> of HTTP Status code and Current user information.</returns>
        public async Task<(HttpStatusCode StatusCode, UserInfo? UserInfo)> TryGetCurrentUserAsync()
        {
            try
            {
                return (HttpStatusCode.OK, await GetCurrentUserAsync());
            }
#if NET5_0_OR_GREATER
            catch (HttpRequestException ex)
            {
                if (ex.StatusCode.HasValue)
                {
                    return (ex.StatusCode.Value, null);
                }
                else
                {
                    throw;
                }
            }
#else
            catch (HttpRequestException)
            {
                return (HttpStatusCode.Forbidden, null);
            }
#endif
        }

        public async Task<UserInfo> GetCurrentUserAsync()
        {
            const string EndPoint = "/auth/user";
            const string Url = BaseUrl + EndPoint;

            var response = await GetAsync(Url);
            var body = await response.Content.ReadAsStringAsync();

            var json = LoadJsonFromString(body);

            // Console.WriteLine($"allowAvatarCopying: {json["allowAvatarCopying"]}");
            // Console.WriteLine($"isFrind: {json["isFriend"]}");
            // Console.WriteLine($"===");

            // var friends = json["friends"];
            // var cnt = friends.Count;
            // using (var fs = new FileStream("FriendList.txt", FileMode.Create, FileAccess.Write, FileShare.Read))
            // using (var sw = new StreamWriter(fs))
            // {
            //     for (var i = 0; i < cnt; i++)
            //     {
            //         // Console.WriteLine((string)friends[i]);
            //         sw.WriteLine((string)friends[i]);
            //     }
            // }

            return ToUserInfo(json);
        }

        public async Task<string> TwoFactorAuth(string tfaCode)
        {
            const string EndPoint = "/auth/twofactorauth/totp/verify";
            const string Url = BaseUrl + EndPoint;

            // curl -X POST "https://api.vrchat.cloud/api/1/auth/twofactorauth/totp/verify" \
            // -H "Content-Type: application/json" \
            // -b "auth={authCookie}" \
            // --data '{"code": "string"}'

            var response = await PostAsync(
                Url,
                new StringContent($"{{\"code\": \"{tfaCode}\"}}", Encoding.UTF8, "application/json"));
            var body = await response.Content.ReadAsStringAsync();

            return body;
        }


        /// <summary>
        /// Get specified user information.
        /// </summary>
        /// <param name="userId">User ID (usr_xxxx...)</param>
        /// <returns>Task of user information.</returns>
        public async Task<UserInfo> GetUserById(string userId)
        {
            const string EndPoint = "/users/";
            const string Url = BaseUrl + EndPoint;

            var userUrl = Url + userId;

            var response = await GetAsync(userUrl);
            var body = await response.Content.ReadAsStringAsync();

            var json = LoadJsonFromString(body);
            return ToUserInfo(json);
        }


        /// <summary>
        /// Get all friend user information.
        /// </summary>
        /// <returns>Task of getting user information of friend.</returns>
        public async Task<List<UserInfo>> GetAllFriends()
        {
            var friendList = await GetAllFriends(true);
            friendList.AddRange(await GetAllFriends(false));
            return friendList;
        }

        /// <summary>
        /// Get specified user information.
        /// </summary>
        /// <param name="userId">User ID (usr_xxxx...)</param>
        /// <returns>Task of user information.</returns>
        public async Task<List<UserInfo>> GetAllFriends(bool isOfflineOnly)
        {
            const int GetCount = 100;

            var friendList = new List<UserInfo>();
            for (int offset = 0; ; offset += GetCount)
            {
                var subFriendList = await GetFriends(GetCount, offset, isOfflineOnly);
                if (subFriendList.Count == 0)
                {
                    break;
                }
                friendList.AddRange(subFriendList);
            }

            return friendList;
        }


        /// <summary>
        /// Get specified user information.
        /// </summary>
        /// <param name="userId">User ID (usr_xxxx...)</param>
        /// <returns>Task of user information.</returns>
        public async Task<List<UserInfo>> GetFriends(int n, int offset, bool isOfflineOnly)
        {
            const string EndPoint = "/auth/user/friends";
            const string Url = BaseUrl + EndPoint;

            var paramPart = await new FormUrlEncodedContent(new Dictionary<string, string>()
            {
                {"n", n.ToString()},
                {"offset", offset.ToString()},
                {"offline", isOfflineOnly ? "true" : "false"}
            }).ReadAsStringAsync();

            var paramUrl = Url + "?" + paramPart;

            var response = await GetAsync(paramUrl);
            var body = await response.Content.ReadAsStringAsync();

            var friendList = new List<UserInfo>();
            foreach (var json in (JsonArray)LoadJsonFromString(body))
            {
                var userInfo = ToUserInfo(json);
                friendList.Add(userInfo);
            }

            return friendList;
        }


        /// <summary>
        /// Get specified world information.
        /// </summary>
        /// <param name="worldId">World ID (wrld_xxxx...)</param>
        /// <returns>Task of user information.</returns>
        public async Task<WorldInfo> GetWorldById(string worldId)
        {
            const string EndPoint = "/worlds/";
            const string Url = BaseUrl + EndPoint;

            var worldUrl = Url + worldId;

            var response = await GetAsync(worldUrl);
            var body = await response.Content.ReadAsStringAsync();

            var json = LoadJsonFromString(body);
            return ToWorldInfo(json);
        }

        public async Task<List<WorldInfo>> GetAllFavoriteWorlds()
        {
            const int GetCount = 100;

            var worldList = new List<WorldInfo>();
            for (int offset = 0; ; offset += GetCount)
            {
                var subWorldList = await GetFavoriteWorlds(GetCount, offset);
                if (subWorldList.Count == 0)
                {
                    break;
                }
                worldList.AddRange(subWorldList);
            }

            return worldList;
        }


        /// <summary>
        /// Get specified user information.
        /// </summary>
        /// <param name="worldId">World ID (wrld_xxxx...)</param>
        /// <returns>Task of user information.</returns>
        public async Task<List<WorldInfo>> GetFavoriteWorlds(int n = 60, int offset = 0)
        {
            const string EndPoint = "/worlds/favorites";
            const string Url = BaseUrl + EndPoint;

            var paramUrl = Url;
            if (n != 60 && offset != 0)
            {
                var paramDict = new Dictionary<string, string>();
                if (n != 60)
                {
                    paramDict.Add("n", n.ToString());
                }
                if (offset != 0)
                {
                    paramDict.Add("offset", offset.ToString());
                }
                paramUrl += "?" + await new FormUrlEncodedContent(paramDict).ReadAsStringAsync();
            }

            var response = await GetAsync(paramUrl);
            var body = await response.Content.ReadAsStringAsync();
            // if (!response.IsSuccessStatusCode)
            // {
            //     ThrowHttpRequestException($"Failed to get world information from {worldUrl}", response.StatusCode, body);
            // }

            // Normal
            // {
            //   "authorId": "usr_502842d5-73df-4ca5-af53-1b27c654f923",
            //   "authorName": "はるる早苗",
            //   "capacity": 8,
            //   "created_at": "2022-11-17T07:02:30.912Z",
            //   "favoriteGroup": "worlds4",
            //   "favoriteId": "fvrt_0ca97ec0-7a8f-47cc-b1bd-216d3c441b6a",
            //   "favorites": 116,
            //   "heat": 3,
            //   "id": "wrld_765cfcb2-45b3-4829-9d5a-2b7d5b851f8c",
            //   "imageUrl": "https://api.vrchat.cloud/api/1/file/file_13d56660-be50-4878-9236-48fb77c50079/1/file",
            //   "labsPublicationDate": "2022-11-17T07:17:04.561Z",
            //   "name": "RBS Bedroom 03",
            //   "occupants": 0,
            //   "organization": "vrchat",
            //   "popularity": 5,
            //   "previewYoutubeId": null,
            //   "publicationDate": "2022-11-30T08:28:39.005Z",
            //   "releaseStatus": "public",
            //   "tags": [
            //     "system_approved"
            //   ],
            //   "thumbnailImageUrl": "https://api.vrchat.cloud/api/1/image/file_13d56660-be50-4878-9236-48fb77c50079/1/256",
            //   "unityPackages": [
            //     {
            //       "platform": "standalonewindows",
            //       "unityVersion": "2019.4.29f1"
            //     }
            //   ],
            //   "updated_at": "2022-11-17T07:02:30.912Z",
            //   "visits": 2880
            // }
            // ---
            // Unavailable world.
            // {
            //   "authorName": "???",
            //   "capacity": 0,
            //   "favoriteGroup": "worlds2",
            //   "favoriteId": "fvrt_3f023e5e-25a4-4d94-9a14-fa60f53e562b",
            //   "id": "???",
            //   "imageUrl": "",
            //   "isSecure": false,
            //   "name": "???",
            //   "occupants": 0,
            //   "releaseStatus": "hidden",
            //   "thumbnailImageUrl": "https://assets.vrchat.com/default/unavailable-world.png"
            // }
            var worldList = new List<WorldInfo>(n);

            foreach (var json in (JsonArray)LoadJsonFromString(body))
            {
                if (!json.ContainsKey("authorId") || json["id"] == "???")
                {
                    continue;
                }
                worldList.Add(ToWorldInfo(json));
            }

            return worldList;
        }


        /// <summary>
        /// Get specified user information.
        /// </summary>
        /// <returns>Task of user information.</returns>
        public async Task<List<WorldInfo>> GetRecentWorlds()
        {
            const string EndPoint = "/worlds/recent";
            const string Url = BaseUrl + EndPoint;


            var response = await GetAsync(Url);
            var body = await response.Content.ReadAsStringAsync();

            var worldList = new List<WorldInfo>();
            foreach (var json in (JsonArray)LoadJsonFromString(body))
            {
                worldList.Add(ToWorldInfo(json));
            }
            return worldList;
        }

        private async Task<HttpResponseMessage> GetAsync(string url)
        {
            return await SendAsync(new HttpRequestMessage(HttpMethod.Get, url));
        }

        private async Task<HttpResponseMessage> PostAsync(string url, HttpContent content)
        {
            return await SendAsync(new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = content
            });
        }

        private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url)
        {
            return await SendAsync(new HttpRequestMessage(method, url));
        }

        private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)
        {
            HttpRequestSending?.Invoke(this, new HttpRequestEventArgs(request));
            var response = await _client.SendAsync(request);
            HttpResponseRecieved?.Invoke(this, new HttpResponseEventArgs(response));
            response.EnsureSuccessStatusCode();
            return response;
        }


        private static UserInfo ToUserInfo(JsonValue json)
        {
            var userInfo = new UserInfo(
                Name: json["displayName"],
                Id: json["id"],
                AllowAvatarCopying: JsonNvlBool(json, "allowAvatarCopying"),
                Bio: JsonNvl(json, "bio"),
                CurrentAvatarImageUrl: json["currentAvatarImageUrl"],
                CurrentAvatarThumbnailImageUrl: json["currentAvatarThumbnailImageUrl"],
                DateJoined: JsonDateTimeNvl(json, "date_joined"),
                DeveloperType: ParseDeveloperType(json["developerType"]),
                FriendKey: json["friendKey"],
                FriendRequestStatus: JsonNvlParse(json, "friendRequestStatus", ParseFriendRequestStatus),
                InstancePart: JsonNvl(json, "instanceId"),
                IsFriend: (bool)json["isFriend"],
                LastActivity: JsonDateTimeNvl(json, "last_activity"),
                LastLogin: JsonNvlParse(json, "last_login", ParseDateTime),
                LastPlatform: ParsePlatform(json["last_platform"]),
                Location: JsonNvl(json, "location"),
                Note: JsonNvl(json, "note"),
                ProfilePicOverride: json["profilePicOverride"],
                State: JsonNvlParse(json, "state", ParseUserState),
                Status: ParseUserStatus(json["status"]),
                StatusDescription: json["statusDescription"],
                TravelingToInstance: JsonNvl(json, "travelingToInstance"),
                TravelingToLocation: JsonNvl(json, "travelingToLocation"),
                TravelingToWorld: JsonNvl(json, "travelingToWorld"),
                UserIcon: json["userIcon"],
                WorldId: JsonNvl(json, "worldId"));
            foreach (var tag in (JsonArray)json["tags"])
            {
                userInfo.Tags.Add(tag);
            }

            return userInfo;
        }

        private static WorldInfo ToWorldInfo(JsonValue json)
        {
            var worldInfo = new WorldInfo(
                Id: json["id"],
                Name: json["name"],
                Namespace: JsonNvl(json, "namespace"),
                AuthorId: json["authorId"],
                AuthorName: json["authorName"],
                Capacity: (int)json["capacity"],
                Description: JsonNvl(json, "description"),
                ReleaseStatus: ParseReleaseStatus(json["releaseStatus"]),
                CreatedAt: JsonDateTimeNvl(json, "created_at"),
                UpdatedAt: JsonDateTimeNvl(json, "updated_at"),
                LabsPublicationDate: JsonDateTimeNvl(json, "labsPublicationDate"),
                PublicationDate: JsonDateTimeNvl(json, "publicationDate"),
                Version: JsonNvlInt(json, "version"),
                Visits: (int)json["visits"],
                Favorites: (int)json["favorites"],
                Heat: (int)json["heat"],
                Featured: JsonNvlBool(json, "featured"),
                ImageUrl: json["imageUrl"],
                ThumbnailImageUrl: json["thumbnailImageUrl"],
                YoutubeUrl: JsonNvl(json, "previewYoutubeId"),
                Organization: json["organization"],
                Popularity: (int)json["popularity"],
                Occupants: (int)json["occupants"],
                PrivateOccupants: JsonNvlInt(json, "privateOccupants"),
                PublicOccupants: JsonNvlInt(json, "publicOccupants"),
                FavoriteGroup: JsonNvl(json, "favoriteGroup"),
                FavoriteId: JsonNvl(json, "favoriteId"));
            foreach (var tag in (JsonArray)json["tags"])
            {
                worldInfo.Tags.Add(tag);
            }

            return worldInfo;
        }


        private static DeveloperType ParseDeveloperType(string developerType)
        {
            return developerType switch
            {
                "none" => DeveloperType.None,
                "trusted" => DeveloperType.Trusted,
                "internal" => DeveloperType.Internal,
                "moderator" => DeveloperType.Moderator,
                _ => throw new InvalidDataException($"Unrecognized developer type: {developerType}")
            };
        }

        private static ReleaseStatus ParseReleaseStatus(string releaseStatus)
        {
            return releaseStatus switch
            {
                "public" => ReleaseStatus.Public,
                "private" => ReleaseStatus.Private,
                "hidden" => ReleaseStatus.Hidden,
                "all" => ReleaseStatus.All,
                _ => throw new InvalidDataException($"Unrecognized release status: {releaseStatus}")
            };
        }

        private static FriendRequestStatus ParseFriendRequestStatus(string friendRequestStatus)
        {
            return friendRequestStatus switch
            {
                "completed" => FriendRequestStatus.Completed,
                _ => FriendRequestStatus.OutGoing
            };
        }

        private static Platform ParsePlatform(string platform)
        {
            return platform switch
            {
                "standalonewindows" => Platform.StandaloneWindows,
                "android" => Platform.Android,
                _ => throw new InvalidDataException($"Unrecognized platform: {platform}")
            };
        }



        private static UserStatus ParseUserStatus(string userStatus)
        {
            return userStatus switch
            {
                "offline" => UserStatus.Offline,
                "join me" => UserStatus.JoinMe,
                "active" => UserStatus.Active,
                "ask me" => UserStatus.AskMe,
                "busy" => UserStatus.Busy,
                _ => throw new InvalidDataException($"Unrecognized user status: {userStatus}")
            };
        }

        private static UserState ParseUserState(string userState)
        {
            return userState switch
            {
                "offline" => UserState.Offline,
                "active" => UserState.Active,
                "online" => UserState.Online,
                _ => throw new InvalidDataException($"Unrecognized user state: {userState}")
            };
        }

        private static DateTime? JsonDateTimeNvl(JsonValue json, string key)
        {
            if (!json.ContainsKey(key))
            {
                return null;
            }
            var val = json[key];
            return val == string.Empty || val == "none" ? null : DateTime.Parse(val);
        }

        private static string? JsonNvl(JsonValue json, string key)
        {
            return json.ContainsKey(key) ? json[key] : null;
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        [return: NotNullIfNotNull(nameof(altVal))]
#endif
        private static dynamic JsonNvl(JsonValue json, string key, dynamic altVal)
        {
            return json.ContainsKey(key) ? json[key] : altVal;
        }


        private static T? JsonNvl<T>(JsonValue json, string key)
            where T : class
        {
            return json.ContainsKey(key) ? json[key] as T : null;
        }

        private static T? JsonNvlVal<T>(JsonValue json, string key)
            where T : struct
        {
            return json.ContainsKey(key) ? (T)Convert.ChangeType(json[key], typeof(T)) : null;
        }

        private static bool? JsonNvlBool(JsonValue json, string key)
        {
            return json.ContainsKey(key) ? (bool)json[key] : null;
        }

        private static int? JsonNvlInt(JsonValue json, string key)
        {
            return json.ContainsKey(key) ? (int)json[key] : null;
        }


        private static T? JsonNvlParse<T>(JsonValue json, string key, Func<string, T?> parse)
            where T : struct
        {
            return json.ContainsKey(key) ? parse(json[key]) : null;
        }



        private static T? JsonNvlParse<T>(JsonValue json, string key, Func<string, T> parse)
            where T : struct
        {
            return json.ContainsKey(key) ? parse(json[key]) : null;
        }

        private static DateTime? ParseDateTime(string dtString)
        {
            return dtString == string.Empty || dtString == "none" ? null : DateTime.Parse(dtString);
        }


        /// <summary>
        /// Load JSON from specified string.
        /// </summary>
        /// <param name="jsonString">JSON string.</param>
        /// <returns><see cref="JsonValue"/> of <paramref name="jsonString"/>.</returns>
        private static JsonValue LoadJsonFromString(string jsonString)
        {
            return LoadJsonFromString(jsonString, Encoding.UTF8);
        }

        /// <summary>
        /// Load JSON from specified string with specified encoding.
        /// </summary>
        /// <param name="jsonString">JSON string.</param>
        /// <param name="encoding">Encoding of <see cref="jsonString"/>.</param>
        /// <returns><see cref="JsonValue"/> of <paramref name="jsonString"/>.</returns>
        private static JsonValue LoadJsonFromString(string jsonString, Encoding encoding)
        {
            var ms = new MemoryStream(encoding.GetBytes(jsonString));
            return JsonValue.Load(ms);
        }

        private static void EnsureSuccessStatusCode(HttpResponseMessage response, string message, string url)
        {
            if (!response.IsSuccessStatusCode)
            {
                var body = response.Content.ReadAsStringAsync().Result;
                throw new HttpRequestException($"{message}: {url}: [{(int)response.StatusCode}][{response.StatusCode}]: {body}");
            }
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        [DoesNotReturn]
#endif
        private static void ThrowHttpRequestException(string message, HttpStatusCode status)
        {
#if NET5_0_OR_GREATER
            throw new HttpRequestException($"{message}: {(int)status}[{status}]", null, status);
#else
            throw new HttpRequestException($"{message}: {(int)status}[{status}]");
#endif
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        [DoesNotReturn]
#endif
        private static void ThrowHttpRequestException(string message, HttpStatusCode status, string body)
        {
#if NET5_0_OR_GREATER
            throw new HttpRequestException($"{message}: {(int)status}[{status}]: {body}", null, status);
#else
            throw new HttpRequestException($"{message}: {(int)status}[{status}]: {body}");
#endif
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    _client.Dispose();
                }
                IsDisposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
