using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Tweetinvi;
using Tweetinvi.Models;

namespace myTweetApp
{
    class Program
    {
        private 
        static async Task Main(string[] args)
        {
            var services = new ServiceCollection();
            ConfigureServices(services);
            ServiceProvider serviceProvider = services.BuildServiceProvider();
            myTweetApp app = serviceProvider.GetService<myTweetApp>();
            try
            {
                await app.Start();
            }
            catch (Exception ex)
            {
                app.HandleError(ex);
            }
            finally
            {
                app.Stop();
            }
        }

        private static void ConfigureServices(ServiceCollection services)
        {
            services.AddLogging(configure => configure.AddConsole())
            .AddTransient<myTweetApp>();
        }
    }
    class  myTweetApp
    {
        private readonly ILogger _logger;
        public myTweetApp(ILogger<myTweetApp> logger)
        {
            _logger = logger;
        }
        public async Task Start()
        {
            _logger.LogInformation($"MyTweetApp Started at {DateTime.Now}");
            await LoadDashboard();
        }

        private async Task LoadDashboard()
        {
            var watch = new Stopwatch();
            try
            {
                _logger.LogWarning("MyTweetApp->LoadDashboard() can throw Exception!");
                var credentials = new TwitterCredentials(MyCredentials.GenerateCredentials());
                var client = new TwitterClient(credentials);

                var appCred = new ConsumerOnlyCredentials(MyCredentials.GenerateAppCreds())
                {
                    BearerToken = Environment.GetEnvironmentVariable("BearerToken", EnvironmentVariableTarget.User)
                };
                var appClient = new TwitterClient(appCred);
                await appClient.Auth.InitializeClientBearerTokenAsync();
                Console.WriteLine(appClient.Users.GetAuthenticatedUserAsync());

                var stream = client.Streams.CreateTweetStream();

                var count = 0;
                stream.EventReceived += (sender, eventReceived) =>
                {                    
                    Console.WriteLine(eventReceived.Json);
                    if (watch.ElapsedMilliseconds == 2000)
                    {
                        stream.Stop();                        
                    }
                    Console.WriteLine($"count {count} {watch.ElapsedMilliseconds}");
                    count++;
                    if (count > 2)
                    {
                        stream.Stop();
                    }
                };
                Console.WriteLine("count " +count);
                await stream.StartAsync("https://stream.twitter.com/1.1/statuses/sample.json");                
                
                TweetinviEvents.SubscribeToClientEvents(client);
                var authenticatedUser = await client.Users.GetAuthenticatedUserAsync();
                Console.WriteLine(authenticatedUser);
                Console.ReadLine();
                
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                _logger.LogCritical($"MyTweetApp->LoadDashboard() Code need to be fixed");
            }
        }

        public void Stop()
        {
            _logger.LogInformation($"MyTweetApp Stopped at {DateTime.Now}");
        }

        public void HandleError(Exception ex)
        {
            _logger.LogError($"MyTweetApp Error Encountered at {DateTime.Now} & Error is : {ex.Message}");
        }
    }

    public static class MyCredentials
    {
        public static ITwitterCredentials GenerateCredentials()
        {
            return new TwitterCredentials(
                Environment.GetEnvironmentVariable("ConsumerKey", EnvironmentVariableTarget.User),
                Environment.GetEnvironmentVariable("ConsumerSecret", EnvironmentVariableTarget.User),
                Environment.GetEnvironmentVariable("TwitterApiKey", EnvironmentVariableTarget.User),
                Environment.GetEnvironmentVariable("TwitterApiSecret", EnvironmentVariableTarget.User)
            );
        }

        public static ITwitterCredentials GenerateAppCreds()
        {
            var userCreds = GenerateCredentials();
            return new TwitterCredentials(userCreds.ConsumerKey, userCreds.ConsumerSecret);

        }
    }
}
