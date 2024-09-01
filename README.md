Koturn.VRChat.WebApi
====================

Simple VRChat Web API client library.

## Usage

### 2FA

```cs
namespace TFASample
{
    internal class Program
    {
        private const string CoolkieFilePath = "cookie.txt";

        static void Main()
        {
            using (var client = GetAuthedClient().Result)
            {
                // Setup to write all HTTP Request/Response log to console.
                client.HttpRequestSending += (sender, e) =>
                {
                    var request = e.Request;
                    Console.WriteLine($"[{request.Method}]: {request.RequestUri}");
                };
                client.HttpResponseRecieved += (sender, e) =>
                {
                    var response = e.Response;
                    var request = response.RequestMessage;
                    var statusCode = response.StatusCode;
                    if (request is null)
                    {
                        Console.WriteLine($"[{(int)statusCode}][{statusCode}]");
                    }
                    else
                    {
                        Console.WriteLine($"[{request.Method}][{(int)statusCode}][{statusCode}]: {request.RequestUri}");
                    }
                };

                var userInfo = client.GetCurrentUserAsync().Result;
                Console.WriteLine(userInfo);
            }
        }

        private static async Task<VRCWebApiClient> GetAuthedClient()
        {
            var client = new VRCWebApiClient();

            if (File.Exists(CoolkieFilePath))
            {
                client.Cookie = File.ReadAllText(CoolkieFilePath);
            }

            // Try to login.
            var result = await client.TryGetCurrentUserAsync();
            if (result.StatusCode == HttpStatusCode.OK)
            {
                return client;
            }

            var apiKey = await client.GetApiKeyAsync();

            Console.Write("User Name> ");
            var userName = Console.ReadLine() ?? "";
            Console.Write("Password> ");
            var password = Console.ReadLine() ?? "";

            var cookie = await client.GetAndUpdateAuthTokenCookie(userName, password, apiKey);

            Console.Write("2FA Code> ");
            var tfaCode = Console.ReadLine();
            if (tfaCode != null)
            {
                await client.TwoFactorAuth(tfaCode);
            }

            // Retry.
            await client.GetCurrentUserAsync();

            File.WriteAllText(CoolkieFilePath, cookie);

            return client;
        }
    }
}
```

## LICENSE

This software is released under the MIT License, see [LICENSE](LICENSE "LICENSE").
