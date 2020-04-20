using System;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

namespace SvoxBot
{
    class SvoxBot
    {
        private DiscordSocketClient _client { get; set; }
        private CommandService _commands { get; set; }
        private IServiceProvider _services { get; set; }
        private string _token { get; set; }
        private string _prefix { get; set; }

        public SvoxBot(string token, string prefix)
        {
            this._token = token;
            this._prefix = prefix;
        }

        public async Task RunBotAsync()
        {
            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Info
            });
            _commands = new CommandService();
            _services = new ServiceCollection()
                .AddSingleton(_client)
                .AddSingleton(_commands)
                .BuildServiceProvider();

            await RegisterCommandsAsync();

            await _client.LoginAsync(TokenType.Bot, this._token);

            _client.Log += Log;

            await _client.StartAsync();

            await Task.Delay(-1);

        }

        private Task Log(LogMessage message)
        {
            Console.WriteLine(message.ToString() + System.Environment.NewLine);
            return Task.CompletedTask;
        }

        public async Task RegisterCommandsAsync()
        {
            _client.MessageReceived += HandleCommandAsync;

            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), null);
        }

        private async Task HandleCommandAsync(SocketMessage arg)
        {

            var message = arg as SocketUserMessage;

            if (message is null || message.Author.IsBot) return;

            int argPos = 0;

            if (message.HasStringPrefix(this._prefix, ref argPos) || message.HasMentionPrefix(_client.CurrentUser, ref argPos))
            {
                var context = new SocketCommandContext(_client, message);

                var result = await _commands.ExecuteAsync(context, argPos, _services);

                if (!result.IsSuccess)
                {
                    string error = "Invalid Command: " + message.ToString() + " by " + message.Author.ToString();
                    Console.WriteLine(error + System.Environment.NewLine);
                }
            }
        }
    }
}