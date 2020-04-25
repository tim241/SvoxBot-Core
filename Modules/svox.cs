using Discord.Commands;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace SvoxBot.Modules
{
    public class svox : ModuleBase<SocketCommandContext>
    {
        /// <summary>
        /// SoundPack search directory
        /// </summary>
        private readonly string _soundPackSearchDirectory = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "soundpacks"));

        /// <summary>
        /// Maximum upload file size in bytes
        /// </summary>
        private readonly Int64 _maxUploadFileSize = 8388119;

        /// <summary>
        /// Checks whether directory is a valid soundpack
        /// </summary>
        /// <param name="directory">directory that needs to be checked</param>
        /// <returns>whether directory is a valid soundpack</returns>
        private bool _isValidSoundPack(string directory)
        {
            // make sure the directory exists & isn't empty
            if (String.IsNullOrEmpty(directory) || !Directory.Exists(directory))
                return false;

            // make sure we're not outside the search path
            if (!Path.GetFullPath(directory).Contains(_soundPackSearchDirectory))
                return false;

            foreach (string file in Directory.GetFiles(directory))
            {
                if (file.ToLower().EndsWith(".wav"))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Builds an array of all soundpacks
        /// </summary>
        /// <returns>array of soundpacks</returns>
        private string[] _getSoundPacks()
        {
            List<string> packs = new List<string>();

            foreach (string dir in Directory.GetDirectories(_soundPackSearchDirectory))
            {
                // make sure it's a valid soundpack
                if (this._isValidSoundPack(Path.Combine(_soundPackSearchDirectory, dir)))
                    packs.Add(Path.GetFileName(dir));
            }

            packs.Sort();
            return packs.ToArray();
        }

        /// <summary>
        /// Gets sounds for pack
        /// </summary>
        /// <param name="pack">SoundPack</param>
        /// <returns>sounds from pack</returns>
        private string[] _getSounds(string pack)
        {
            if (!this._isValidSoundPack(pack))
                return null;

            List<string> sounds = new List<string>();
            foreach (string sound in Directory.GetFiles(pack))
            {
                if (sound.ToLower().EndsWith(".wav"))
                    sounds.Add(Path.GetFileNameWithoutExtension(sound));
            }

            sounds.Sort();
            return sounds.ToArray();
        }

        /// <summary>
        /// Combines multiple WAV files
        /// </summary>
        /// <param name="sourceFiles">List of files that need to be combined</param>
        /// <param name="context">SocketCommandContext for sending messages to discord channel</param>
        /// <returns>MemoryStream of combined WAV File</returns>
        private MemoryStream _concatenate(IEnumerable<string> sourceFiles, SocketCommandContext context)
        {
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

                    if (outStream.Length > this._maxUploadFileSize)
                    {
                        context.Channel.SendMessageAsync($"Combined WAV file too big for Discord!");
                        return null;
                    }
                }
            }

            // reset position
            outStream.Position = 0;

            return outStream;
        }

        /// <summary>
        /// Processes text, makes sure the WAV files exist and combines the WAV files
        /// </summary>
        /// <param name="collection">soundpack</param>
        /// <param name="inputText">sentence that needs to be said</param>
        /// <param name="context">SocketCommandContext for sending messages to discord channel</param>
        /// <returns>MemoryStream of combined WAV files, null when error</returns>
        private MemoryStream _processText(string collection, string inputText, SocketCommandContext context)
        {
            string phrase = inputText;
            string[] words = phrase.Split(' ');
            List<string> missingWords = new List<string>();
            bool missing = false;
            string soundPackDir = Path.Combine(_soundPackSearchDirectory, collection);

            if (!this._isValidSoundPack(soundPackDir))
            {
                context.Channel.SendMessageAsync($"Invalid soundpack: `{collection}`");
                return null;
            }

            for (int i = 0; i < words.Length; i++)
            {
                words[i] = Path.Combine(soundPackDir, $"{words[i]}.wav");

                if (!File.Exists(words[i]))
                {
                    missingWords.Add(words[i]);
                    missing = true;
                }
            }

            if (missing)
            {
                context.Channel.SendMessageAsync($"Missing File(s): `{String.Join(", ", missingWords)}`");
                return null;
            }
            else
            {
                return this._concatenate(words, context);
            }
        }

        /// <summary>
        /// Sends array in chunks if needed
        /// </summary>
        /// <param name="seperator">String.Join seperator</param>
        /// <param name="array">array of contents</param>
        /// <param name="context">SocketCommandContext to send message</param>
        private void _sendArrayInChunks(string seperator, string[] array, SocketCommandContext context)
        {
            int maxLength = 2000 - 2;
            int charCount = 0;
            string message = String.Join(seperator, array);

            // when it's possible to send in one go, do it
            if (message.Length < maxLength)
            {
                context.Channel.SendMessageAsync($"`{message}`");
                return;
            }

            // else just send in chunks as needed
            message = null;
            for (int i = 0; i < array.Length; i++)
            {
                charCount += array[i].Length + seperator.Length;
                message += array[i] + seperator;

                if ((charCount + (array[i + 1].Length + seperator.Length)) > maxLength)
                {
                    context.Channel.SendMessageAsync($"`{message}`").RunSynchronously();
                    message = null;
                    charCount = 0;
                }
            }

            // send remainder
            if (!String.IsNullOrEmpty(message))
                context.Channel.SendMessageAsync($"`{message}`").RunSynchronously();
        }

        [Command("help")]
        public async Task help()
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
                             "Enhanced by Tim Wanders\n" +
                             "https://github.com/tim241/SvoxBot-Core \n");
        }

        [Command("sounds")]
        public async Task sounds([Remainder] string text)
        {
            string[] words = text.Split(' ');
            List<string> sounds = new List<string>();
            List<string> invalidPacks = new List<string>();
            bool invalidPacksFound = false;
            string pack = null;

            for (int i = 0; i < words.Length; i++)
            {
                pack = Path.Combine(_soundPackSearchDirectory, words[i]);
                string[] packSounds = this._getSounds(pack);

                if (packSounds == null)
                {
                    invalidPacks.Add(words[i]);
                    invalidPacksFound = true;
                }

                if (!invalidPacksFound)
                    sounds.AddRange(packSounds);
            }

            if (invalidPacksFound)
                await ReplyAsync($"Invalid pack(s) `{String.Join(", ", invalidPacks)}`");
            else
            {
                sounds.Sort();
                this._sendArrayInChunks(", ", sounds.ToArray(), Context);
            }
        }

        [Command("packs")]
        public async Task packs()
        {
            string[] soundPacks = this._getSoundPacks();

            if (soundPacks.Length == 0)
                await ReplyAsync("no soundpacks found!");
            else
                this._sendArrayInChunks(", ", soundPacks, Context);
        }

        [Command("say")]
        public async Task say([Remainder] string text)
        {
            string[] words = text.Split(' ');
            string rest = text.Replace(words[0] + " ", "");

            using (MemoryStream stream = this._processText(words[0], rest, Context))
            {
                // check if failed
                if (stream == null)
                    return;

                await Context.Channel.SendFileAsync(stream, $"{rest}.wav"); // Send the file
            }
        }
    }
}
