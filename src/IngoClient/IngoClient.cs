using IngoClient.Interfaces;
using IngoClient.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace IngoClient
{
    public class IngoClientClass : IIngoClient
    {
        public Guid ClientId { get; set; } = Guid.Parse("f890879a-3f2f-11e9-9293-b3f7a52580cb");
        private string Scope = "USER INGO_LOYALTY";

        private IHttpWrapper client;
        private IConsole console;

        public IngoClientClass(IConsole console, IHttpWrapper client)
        {
            this.console = console;
            this.client = client;
            client.AddDefaultHeader("User-Agent", "okhttp/4.4.1");

            client.AddDefaultHeader("X-App-Platform", "Android");
            client.AddDefaultHeader("X-App-Version", "4.9.1 (2933)");
            client.AddDefaultHeader("X-App-Name", "Ingo");

            //var session = GetGameSession();
            var game = GenerateRoundabout(20.5371823);
            var str = JsonConvert.SerializeObject(game);
            Console.WriteLine();
        }

        private User lastUser;
        private string mfaSessionId;
        public void Login(User user)
        {
            lastUser = user;
            var content = new StringContent(LoginBodyBuilder(user));
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/x-www-form-urlencoded");
            var result = client.Post("https://id.circlekeurope.com/api/v3/oauth/authorize/password", content);
            console.Write(result);
            if (result.StatusCode==200)
            {
                var obj = JObject.Parse(result.ResponseBody);
                if(obj.TryGetValue("mfaRequired", out JToken mfaRequired) && mfaRequired.ToObject<bool>()==true)
                {
                    mfaSessionId = obj.GetValue("mfaSessionId").ToString();
                    SetupMFA(mfaSessionId);
                }
            }
        }

        private void SetupMFA(string mfaSessionId)
        {
            var content = new StringContent("{\"languageCode\":\"da\",\"mfaSessionId\":\""+mfaSessionId+"\"}");
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            var result = client.Post("https://id.circlekeurope.com/api/v3/oauth/authorize/password/mfa/send", content);
            console.Write(result);
        }

        public string LoginBodyBuilder(User user)
        {
            var body = "";
            body = AddParameter(body,"client_id", ClientId.ToString());
            body = AddParameter(body, "scope", Scope);
            body = AddParameter(body, "grant_type", "password");

            body = AddParameter(body, "username", user.Username);
            body = AddParameter(body, "password", user.Password);
            body = AddParameter(body, "deviceName", user.DeviceName);
            if(user.DeviceToken!=null)
                body = AddParameter(body, "deviceToken", user.DeviceToken);

            return body;
        }

        private string AddParameter(string body, string key, string value)
        {
            var parameter = key + "=" + Uri.EscapeDataString(value);
            if (string.IsNullOrEmpty(body))
                return parameter;
            return body + "&" + parameter;
        }

        public void Confirm(string code)
        {
            var content = new StringContent("{\"client_id\":\"" + ClientId.ToString() + "\",\"scope\":\"" + Scope + "\",\"otp\":\"" + code + "\",\"deviceName\":\"" + lastUser.DeviceName + "\",\"mfaSessionId\":\"" + mfaSessionId + "\",\"markDeviceAsTrusted\":true,\"deviceToken\":null}");
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            var result = client.Post("https://id.circlekeurope.com/api/v3/oauth/authorize/password/mfa/verify", content);
            console.Write(result);

            if(result.StatusCode ==200)
            {
                var obj = JObject.Parse(result.ResponseBody);
                lastUser.DeviceToken = obj["deviceToken"].ToString();
                lastUser.AccessToken = obj["access_token"].ToString();
                lastUser.LastTimeTokenGenerated = DateTime.UtcNow;
                lastUser.ExpiresIn = obj["expires_in"].ToObject<int>();
                lastUser.RefreshToken = obj["refresh_token"].ToString();
            }
        }

        public void RefreshToken(User user)
        {
            var content = new StringContent(RefreshTokenBodyBuilder(user));
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/x-www-form-urlencoded");
            var result = client.Post("https://id.circlekeurope.com/api/v2/oauth/token/refresh", content);
            console.Write(result);
            if (result.StatusCode == 200)
            {
                var obj = JObject.Parse(result.ResponseBody);
                user.AccessToken = obj["access_token"].ToString();
                user.LastTimeTokenGenerated = DateTime.UtcNow;
                user.ExpiresIn = obj["expires_in"].ToObject<int>();
            }
        }

        public string RefreshTokenBodyBuilder(User user)
        {
            var body = "";
            body = AddParameter(body, "refresh_token", user.RefreshToken);
            body = AddParameter(body, "client_id", ClientId.ToString());
            body = AddParameter(body, "grant_type", "refresh_token");

            return body;
        }

        //https://games.app.ingoapp.com/games/?token=KwhJfjft+UcwnU/EtGyPIRGB3gSJn2iZ+acNye2SqqjGCZMBdF2Xukl6Jo2F2I6tHfu0A319ki+RTGIW5cWpbayDu9jTHj6wF7zwKgmN7I4=&region=DK&language=da
        public string GenerateGameToken(User user)
        {
            var shouldRefreshToken = DateTime.UtcNow >= user.LastTimeTokenGenerated.AddSeconds(user.ExpiresIn - 10);
            
            if (shouldRefreshToken)
                RefreshToken(user);
            else if (user.GameToken != null)
                return user.GameToken;

            client.AddDefaultHeader("Authorization", "Bearer "+user.AccessToken);
            var result = client.Get("https://backend.ingoapp.com/api/secured/competition/token");
            console.Write(result);
            client.RemoveDefaultHeader("Authorization");
            if (result.StatusCode == 200)
            {
                var token = JObject.Parse(result.ResponseBody)["token"].ToString();
                user.GameToken = token;

                return token;
            }
            return null;
        }

        public GameInfo GetGameSession()
        {
            var info = new GameInfo();
            var result = client.Get("https://games.app.ingoapp.com/api/games/current?region=DK");
            console.Write(result);
            //shortname
            var b = JObject.Parse(result.ResponseBody);
            if (b["shortname"].ToString() == "roundabout")
            {
                info.Type = GameType.ROUNDABOUT;
                info.PeriodId = b["periodId"].ToObject<int>();
                info.Cookie = result.Headers["Set-Cookie"].First().Split(';').First();
            }
            return info;
        }

        public void Play(User user, GameInfo info)
        {
            if (info.Type ==GameType.ROUNDABOUT)
            {
                GenerateGameToken(user);
                SetToken(user.GameToken, info.Cookie);
                BeginGame(info);
                var gameData = GenerateRoundabout(20.483192);
                FinishGame(gameData, info.Cookie);
            }
        }

        private void SetToken(string token, string cookie)
        {
            client.AddDefaultHeader("Cookie", cookie);
            var result = client.Get("https://games.app.ingoapp.com/api/users?token="+token);
            client.RemoveDefaultHeader("Cookie");
        }

        private void BeginGame(GameInfo info)
        {
            client.AddDefaultHeader("Cookie", info.Cookie);
            client.AddDefaultHeader("Origin", "https://games.app.ingoapp.com");
            client.AddDefaultHeader("Referer", "https://games.app.ingoapp.com/games/?token=D4yI2A7f4ExNvNeDr/euQDidsYWAqutg8sONgfW8ybDT4TAtBUJHLHv7UFyqBVuYHfu0A319ki+RTGIW5cWpbfQuiJHlNsT3LiR+kmOYMrM=&region=DK&language=da");
            var d = "{\"game_id\":" + (int)info.Type + ",\"period_id\":" + info.PeriodId + "}";
            var content = new StringContent(d);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            var result = client.Patch("https://games.app.ingoapp.com/api/users/update", null);
            var result2 = client.Post("https://games.app.ingoapp.com/api/games/" + (int)info.Type + "/attempts/add", content);
            client.RemoveDefaultHeader("Cookie");
        }

        private void FinishGame(RoundaboutGame game, string cookie)
        {
            client.AddDefaultHeader("Cookie", cookie);
            var data = JsonConvert.SerializeObject(game);
            var content = new StringContent(data);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            var result = client.Patch("https://games.app.ingoapp.com/api/games/5/attempts/update", content);
            client.RemoveDefaultHeader("Cookie");
        }

        public RoundaboutGame GenerateRoundabout(double finalScore)
        {
            var game = new RoundaboutGame
            {
                Score = finalScore.ToString(CultureInfo.InvariantCulture),
                GameId = GameType.ROUNDABOUT,
                Attempts = new List<Attempt>()
            };

            var timeLeft = finalScore;
            var maxLaps = 8;
            for (int i=0;i<maxLaps;i++)
            {
                var timeSpent = timeLeft/(maxLaps-i)+(i%2==1 ? GetRandomNumber(-1, -0.5) : GetRandomNumber(1,0.5));
                timeLeft -= timeSpent;
                if (timeLeft < 0 || i==maxLaps-1)
                    timeLeft = 0;

                var time = finalScore - timeLeft;
                if (i % 2 == 1)
                {
                    game.AddAttempt(EventType.CHANGE_LANE, GetRandomNumber(time - 1, time - 0.5));
                    game.AddAttempt(EventType.BOOST_SPEED, GetRandomNumber(time - 1, time - 0.5));
                }
                game.AddAttempt(EventType.LAP_ENDED, time);
            }

            //add game data
            game.AddAttempt(EventType.FINISH, finalScore);
            game.AddAttempt(EventType.FPS, 50);
            return game;
        }

        Random random = new Random();
        public double GetRandomNumber(double minimum, double maximum)
        {
            return random.NextDouble() * (maximum - minimum) + minimum;
        }
    }

    public enum GameType
    {
        UNKNOWN,
        ROUNDABOUT=5
    }

    public class GameInfo
    {
        public GameType Type { get; set; }
        public int PeriodId { get; set; }
        public string Cookie { get; set; }
    }

    public class RoundaboutGame
    {
        [JsonProperty("game_id")]
        public GameType GameId { get; set; }
        [JsonProperty("score")]
        public string Score { get; set; }
        [JsonProperty("hash")]
        public string Hash
        {
            get => CreateMD5((int)GameId + Score+ "ingoChallenge2018");
        }
        [JsonProperty("attempt_data")]
        public List<Attempt> Attempts { get; set; } = new List<Attempt>();

        public void AddAttempt(EventType evnt, double time)
        {
            Attempts.Add(new Attempt { GameId=GameType.ROUNDABOUT,Time=time,EventId=evnt});
        }

        public static string CreateMD5(string input)
        {
            MD5 md5 = MD5.Create();

            byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
            StringBuilder sb = new StringBuilder();
            for (int j = 0; j < hash.Length; j++)
            {
                sb.Append(hash[j].ToString("X2"));
            }

            return sb.ToString().ToLower();
        }
    }

    public class Attempt
    {
        [JsonProperty("eventId")]
        public EventType EventId { get; set; }
        [JsonProperty("time")]
        public double Time { get; set; }
        [JsonProperty("game_id")]
        public GameType GameId { get; set; }
    }

    public enum EventType
    {
        //COLLISION=1,
        LAP_ENDED=2,
        FINISH=3,
        CHANGE_LANE=4,
        FPS=8,
        BOOST_SPEED=9,
    }
}