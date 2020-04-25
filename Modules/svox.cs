using Discord.Commands;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace SvoxBot.Modules
{
    // Just a ping to see if the bot is working
    public class svox : ModuleBase<SocketCommandContext>
    {
        [Command("help")]
        public async Task Test()
        {
            await ReplyAsync("**SVOXBOT**: A Bot for Half-Life fans (ported to .Net Core)\n" +
                             "Combines .wav files in the order that you specify, and uploads the file to Discord  \n\n" +
                             "`!say [soundpack] [words words words]`: Generate a sound file \n" +
                             "`!packs`: Show installed soundpacks \n" +
                             "`!sounds [soundpack]`: Show sounds in a soundpack");
        }

        [Command("about")]
        public async Task about()
        {
            await ReplyAsync("**SVOXBOT**: A Bot for Half-Life fans (ported to .Net Core)\n" +
                             "Combines .wav files in the order that you specify, and uploads the file to Discord  \n\n" +
                             "Created by Robin Universe \n" +
                             "https://github.com/tim241/SvoxBot-Core \n");
        }


        // Combine the .WAV files
        private MemoryStream Concatenate(IEnumerable<string> sourceFiles, SocketCommandContext context)
        {
            byte[] buffer = new byte[1024];
            MemoryStream outStream = new MemoryStream();
            WaveFileWriter writer = null;
            WaveFormat format = null;

            foreach (string sourceFile in sourceFiles)
            {
                using (WaveFileReader reader = new WaveFileReader(sourceFile))
                {
                    if (writer == null)
                    {
                        format = reader.WaveFormat;
                        writer = new WaveFileWriter(outStream, format);
                    }

                    if (!reader.WaveFormat.Equals(format))
                    {
                        context.Channel.SendMessageAsync($"File `{sourceFile}` has a different WaveFormat!");
                        return null;
                    }

                    reader.CopyTo(outStream);
                }
            }

            // reset position
            outStream.Position = 0;

            return outStream;
        }

        // Split up the text and format it for the Concatinator to process
        private MemoryStream processText(string collection, string inputText, SocketCommandContext context)
        {
            string phrase = inputText;
            string[] words = phrase.Split(' ');
            List<string> missingWords = new List<string>();
            bool missing = false;

            for (int i = 0; i < words.Length; i++)
            {
                words[i] = $"{words[i]}.wav";
                words[i] = Path.Combine(collection, words[i]);
                
                if (!File.Exists(words[i]))
                {
                    missingWords.Add(words[i]);
                    missing = true;
                }
            }

            if (missing)
            {
                context.Channel.SendMessageAsync($"Missing File(s): `{String.Join(",", missingWords)}`");
                return null;
            }
            else
            {
                return this.Concatenate(words, context);
            }
        }

        [Command("sounds")]
        public async Task soundsCommand([Remainder] string text)
        {
            string[] words = text.Split(' ');
            string folder = text.Replace(words[0] + " ", "");

            if (String.IsNullOrEmpty(folder) || !Directory.Exists(folder))
            {
                await ReplyAsync("Folder not found!");
                return;
            }

            List<string> files = new List<string>();
            foreach (string file in Directory.GetFiles(folder))
            {
                files.Add(Path.GetFileName(file));
            }

            string fileText = String.Join(",", files);
            int maxLength = 2000 - 6;
            if (fileText.Length > maxLength)
            {
                // send it in chunks
                int charCount = 0;
                fileText = null;
                for (int i = 0; i < files.Count; i++)
                {
                    charCount += files[i].Length + 1;
                    fileText += files[i] + ",";

                    if ((charCount + (files[i + 1].Length + 1)) > maxLength)
                    {
                        await ReplyAsync($"```\n{fileText}```");
                        fileText = null;
                        charCount = 0;
                    }
                }

                // send remainder
                if (!String.IsNullOrEmpty(fileText))
                    await ReplyAsync($"```\n{fileText}```");
            }
            else
                await ReplyAsync($"```\n{String.Join(",", files)}```");
        }

        [Command("packs")]
        public async Task packsCommand()
        {
            var directories = Directory.GetDirectories(Environment.CurrentDirectory);
            List<string> packs = new List<string>();

            for (int i = 0; i < directories.Length; i++)
            {
                foreach (string file in Directory.GetFiles(directories[i]))
                {
                    if (file.EndsWith(".wav"))
                    {
                        packs.Add(Path.GetFileName(directories[i]));
                        break;
                    }
                }
            }

            await ReplyAsync($"`{String.Join(", ", packs)}`");
        }

        [Command("say")] // The actual command
        public async Task svoxCommand([Remainder] string text)
        {
            string[] words = text.Split(' ');
            string rest = text.Replace(words[0] + " ", "");

            using (MemoryStream stream = processText(words[0], rest, Context))
            {
                // check if failed
                if (stream == null)
                    return;

                await Context.Channel.SendFileAsync(stream, $"{rest}.wav"); // Send the file
            }
        }
    }
}
