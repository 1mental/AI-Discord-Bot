using Discord;
using Discord.WebSocket;
using Discord.Commands;
using Discord.Net;
using Newtonsoft.Json;
using static Initilizer;
using ChatboxTest.ChatGPT;
using ChatboxTest.TextToSpeech;
using System.Net;

namespace ChatboxTest.Discord
{
    public class DiscordBot
    {
        public string Token { get; set; }
        public string GuildID { get; set; }
        public List<string> AllowedUsers { get; set; }
        private DiscordSocketClient _client;
        private IGuild? guild = null;
        private readonly string cacheFoler = Environment.CurrentDirectory + "/cache/";
        private Task Log(LogMessage message)
        {
            if (message.Exception is CommandException cmdException)
            {
                Console.WriteLine($"[Command/{message.Severity}] {cmdException.Command.Aliases.First()}"
                    + $" failed to execute in {cmdException.Context.Channel}.");
                Console.WriteLine(cmdException);
            }
            else
                Console.WriteLine($"[General/{message.Severity}] {message}");
            return Task.CompletedTask;
        }


        private Task SlashCommandHandler(SocketSlashCommand command)
        {
            switch (command.Data.Name)
            {
                case "tell":
                    Thread tellThread = new Thread(new ThreadStart(async () =>
                    {
                        await HandleTellCommand(command);
                    }));
                    tellThread.Start();
                    break;

                case "generate":
                    Thread generateThread = new Thread(new ThreadStart(async () =>
                    {
                        await HandleGenerateCommand(command);
                    }));
                    generateThread.Start();
                    break;
                case "say":
                    Thread sayThread = new Thread(new ThreadStart(async () =>
                    {
                        await HandleSayCommand(command);
                    }));
                    sayThread.Start();
                    break;

            }

            return Task.CompletedTask;
        }

        private async Task HandleSayCommand(SocketSlashCommand command)
        {
            // Checking if allowed users send the command

            if (!AllowedUsers.Contains(command.User.Id.ToString()))
            {
                await command.RespondAsync("You're not allowed to use this command");
                return;
            }

            await command.RespondAsync("انتظر شوي....");

            // taking the sentence
            var sentence = command.Data.Options.First().Value;
            if (string.IsNullOrEmpty(sentence.ToString()))
                return;

            try
            {
                string msg = await _gptClient.sendMessage(sentence.ToString());
                string response = await GenerateSpeech(msg.Replace("\\n",""));
                await EditMessage(command, response);
            }
            catch (ChatGPTMessageException)
            {
                await command.RespondAsync("Message cannot be null!");
            }
            catch (InvaildLengthException)
            {
                await EditMessage(command, "ممنوع تكتب اكثر من 150 حرف!");
            }
            catch (HttpException ex)
            {
                await Console.Out.WriteLineAsync(ex.Reason);
            }
            catch (TimeoutException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }


        private Task<string> DownloadFileAsync(string url)
        {
            WebClient client = new WebClient();
            string path = string.Format("{0}{1}{2}", cacheFoler, Guid.NewGuid().ToString(), ".wav");
            client.DownloadFile(new Uri(url), path);
            return Task.FromResult(path);
        }
        public async Task EditMessage(SocketSlashCommand command, string response)
        {
            if (!string.IsNullOrEmpty(response))
            {
                await command.ModifyOriginalResponseAsync(async (x) =>
                {
                    if (response.Contains("tts"))
                    {
                        x.Content = command.Data.Options.First().Value.ToString();
                        string filepath = await DownloadFileAsync(response);
                        FileAttachment attachment = new FileAttachment(filepath);
                        List<FileAttachment> files = new List<FileAttachment>
                        {
                            attachment
                        };
                        x.Attachments = files;
                        return;
                    }
                    x.Content = response;
                });
                return;
            }else
            {
      
                await command.ModifyOriginalResponseAsync((x) =>
                {
                    x.Content = "حصلت مشكلة!";
                });
                return;
            }
        }
        


        public async Task HandleGenerateCommand(SocketSlashCommand command)
        {
            // Checking if allowed users send the command
            if (!AllowedUsers.Contains(command.User.Id.ToString()))
            {
                await command.RespondAsync("You're not allowed to use this command");
                return;
            }
            

            // taking the prompt and checking if it null
            var prompt = command.Data.Options.First().Value;
            if (string.IsNullOrEmpty(prompt.ToString()))
                return;

            try
            {
                // responding the command to avoid the 3 seconds gap
                await command.RespondAsync("انتظر شوي....");

                // Getting the image..
                string response = await _gptClient.GenerateImage(prompt.ToString());

                // Editing the message
                await EditMessage(command, response);

            }catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public async Task HandleTellCommand(SocketSlashCommand command)
        {

            string temp = "";

            if (!AllowedUsers.Contains(command.User.Id.ToString()))
            {
                await command.RespondAsync("You're not allowed to use this command");
                return;
            }
            try
            {
                var sentence = command.Data.Options.First().Value;
                if (string.IsNullOrEmpty(sentence.ToString()))
                    return;
                await command.RespondAsync("Please wait...");
                string response = await _gptClient.sendMessage(sentence.ToString());
                if (response.Contains("\\n"))
                    response = response.Replace("\\n", "\n");
                temp = response;

                await EditMessage(command, response);
            }
            catch (ChatGPTMessageException)
            {
                await command.RespondAsync("Message cannot be null!");
            }
            catch (InvaildLengthException)
            {
                await EditMessage(command, "ممنوع تكتب اكثر من 150 حرف!");
            }
            catch (HttpException ex)
            {
                await Console.Out.WriteLineAsync(ex.Reason);
            }
            catch (TimeoutException)
            {
                IChannel ch = command.Channel;
                await ch.GetUserAsync(command.User.Id).GetAwaiter().GetResult().SendMessageAsync(temp);
            }
       
        }

        public SocketGuildUser GetGuildOwner(SocketChannel channel)
        {
            var guild = (channel as SocketGuildChannel)?.Guild;
            return guild?.Owner;
        }

        public async void CreateSlashCommand(string name, string disc, CommadProperties properties)
        {
            if (guild == null)
                throw new NullReferenceException("Guild is null");
            var command = new SlashCommandBuilder()
            {
                Name = name,
                Description = disc,
                IsDMEnabled = false,
                IsNsfw = false
            };

            

            // O(1)
            command.AddOption(properties.Name, properties.type, properties.Description, properties.isRequired);
            try
            {
                await guild.CreateApplicationCommandAsync(command.Build());
            }catch(ApplicationCommandException ex)
            {
                Console.WriteLine(JsonConvert.SerializeObject(ex.Errors,Formatting.Indented));
            }catch(Exception ex)
            {
                await Console.Out.WriteLineAsync(ex.Message);
            }
        }


        public async void CreateSlashCommand(string name, string disc, CommadProperties[] properties)
        {
            if (guild == null)
                throw new NullReferenceException("Guild is null");
            var command = new SlashCommandBuilder()
            {
                Name = name,
                Description = disc,
                IsDMEnabled = false,
                IsNsfw = false
            };

            // O(n)
            foreach (var property in properties)
            command.AddOption(property.Name, property.type, property.Description, property.isRequired);

            try
            {
                await guild.CreateApplicationCommandAsync(command.Build());
            }
            catch (ApplicationCommandException ex)
            {
                Console.WriteLine(JsonConvert.SerializeObject(ex.Errors, Formatting.Indented));
            }
            catch (Exception ex)
            {
                await Console.Out.WriteLineAsync(ex.Message);
            }
        }

        public void Start()
        {
            Task.Run(async () =>
            {
                var _config = new DiscordSocketConfig { MessageCacheSize = 100,
                    UseInteractionSnowflakeDate = false,
                    UseSystemClock = false };
                _client = new DiscordSocketClient(_config);
                _client.Log += Log;
                _client.SlashCommandExecuted += SlashCommandHandler;
                _client.MessageReceived += _client_MessageReceived;
                _client.Ready += () =>
                {
                    // Getting the guild
                    guild = _client.GetGuild(ulong.Parse(GuildID));

                    // Creating guildy SlashComamnds
                    CreateSlashCommand("tell", "Telling the bot something", new CommadProperties("sentence", "The sentence you want to tell", true, ApplicationCommandOptionType.String));
                    CreateSlashCommand("generate",
                        "Generate an image for you!",
                        new CommadProperties("prompt","describe the image",true, ApplicationCommandOptionType.String));
                    CreateSlashCommand("say","Command Eun to say something for you!",
                        new CommadProperties("sentence","What you want to make Eun say for you.",true, ApplicationCommandOptionType.String));
                    // Writing bot stats.
                    Console.WriteLine($"[+] Client is connected as {_client.CurrentUser.Username}!");
                    return Task.CompletedTask;
                };
                await _client.LoginAsync(TokenType.Bot, Token);
                await _client.StartAsync();
                
                await Task.Delay(-1);
            }).Wait();
        }

        private async Task _client_MessageReceived(SocketMessage arg)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            await Console.Out.WriteLineAsync(string.Format("[{0}] Message have been received by : {1}", DateTime.UtcNow.ToString(),arg.Author.Username));
            Console.ForegroundColor = ConsoleColor.White;
        }


        private async Task<string> GenerateSpeech(string message)
        {
            try
            {
               string response = await Talker.Speech(message);
                await Console.Out.WriteLineAsync(response);
                if (!response.Contains("//"))
                    return "فيه مشكلة حصلت!";
                return response;
            }
            catch (TalkerInvaildLengthException)
            {
                return "حدك 5000 حرف!";
            }catch (ArgumentNullException) 
            {
                return "اكتب رسالة عالاقل!.";
            }
        }

        public class CommadProperties
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public bool isRequired { get; set; }
            public ApplicationCommandOptionType type { get; set; }

            public CommadProperties(string name, string disc, bool required, ApplicationCommandOptionType type)
            {
                this.Name = name;
                this.Description = disc;
                this.isRequired = required;
                this.type = type;
            }
        }

    }
}
