using System;
using System.Collections.Generic;
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
            var tweets = new List<object>();
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
                

                var stream = client.Streams.CreateTweetStream();
                stream.AddLanguageFilter(LanguageFilter.English);

                var count = 1;                
                Console.WriteLine($"Time now {DateTime.Now}");
                var timer = new Stopwatch();
                timer.Start();
                stream.EventReceived += (sender, eventReceived) =>
                {
                    //Console.WriteLine(eventReceived.Json);
                    tweets.Add(eventReceived.Json);
                    count++;
                    if (count > 1000)
                    {
                        stream.Stop();timer.Stop();
                        TimeSpan timeTaken = timer.Elapsed;
                        Console.WriteLine($"Time taken: {timeTaken.ToString(@"m\:ss\.fff")}");
                        Console.WriteLine($"tweets {tweets.Count}");
                        Console.WriteLine($"Time now {DateTime.Now}");
                    }   
                };                
                await stream.StartAsync("https://stream.twitter.com/1.1/statuses/sample.json"); 
                _logger.LogInformation($"2 of the tweets in Tweets \n\r {tweets[1]},\n\r\n\r {tweets[2]} ");  
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
