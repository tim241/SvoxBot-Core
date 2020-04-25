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
            _client = new DiscordSocketClient();
            _commands = new CommandService();
            _services = new ServiceCollection()
                .AddSingleton(_client)
                .AddSingleton(_commands)
                .BuildServiceProvider();

            await _registerCommandsAsync();
            await _client.LoginAsync(TokenType.Bot, this._token);

            _client.Log += _log;

            await _client.StartAsync();
            await Task.Delay(-1);
        }

        private Task _log(LogMessage message)
        {
            Console.WriteLine($"[{message.Severity}] {message.Message}");
            return Task.CompletedTask;
        }

        public async Task _registerCommandsAsync()
        {
            _client.MessageReceived += _handleCommandAsync;

            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), null);
        }

        private async Task _handleCommandAsync(SocketMessage message)
        {
            SocketUserMessage sMessage = (SocketUserMessage)message;
            int messagePos = 0;

            if (!sMessage.HasStringPrefix(this._prefix, ref messagePos))
                return;

            SocketCommandContext context = new SocketCommandContext(_client, sMessage);
            await _commands.ExecuteAsync(context, messagePos, _services);
        }
    }
}