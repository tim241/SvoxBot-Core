# SvoxBot
A C# Discord bot for Half-Life fans (ported to .Net Core)

# How to Run

1. `git clone https://github.com/tim241/SvoxBot-Core`
2. `cd SvoxBot-Core`
3. `export SVOX_TOKEN={YOUR DISCORD TOKEN}`
4. `dotnet run`

Remember to run !help if you forget anything!

# What does it do?

it's a Discord bot that is made to run on the .Net Core that
combines .wav files in the order that you type them in discord.

It's made to be able to recreate the VOX lines from Half-Life

![alt](https://i.imgur.com/EoRCIrp.png)

syntax: { !play } { desired wav subfolder } {words, words, words}

Right now all you need to do is to find your desired wav pack (Google is your friend) and extract it into a subfolder
with something you won't mind typing to trigger the command, for example, I have a HEV folder for the hev lines, so when I want to trigger that I type 

<!play hev hiss beep getmedkit>

Each generated sound will be saved in the working folder seperately. I'll add the option to overwrite itself later. Just makes it easier to keep hold of good lines, but it is wasteful.

Some notes:
- You need to make sure you only use .WAV files that have the same bitrate and channels
- I plan on allowing much more customization, but I thought I would get a more basic version out now rather than later

Some goals:
- Allow zipped soundpacks
- Ability to show you all the words you messed up instead of just the first one
