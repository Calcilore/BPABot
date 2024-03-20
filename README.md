# Social Credit Bot
A bot that listens to you speak in voice chats, and gives you a score based on how positive you are.
It transcribes what you say using either openai's whisper or vosk, and then uses a sentiment analysis model to give you a score.

It also reads sent messages and gives a score based on the sentiment of the message.

## Prerequisites
- .NET 8.0
- `sudo apt-get install libopus0 libopus-dev libsodium23 libsodium-dev` or equivalent for your system
- Models which are downloaded at runtime (No action required)
- A discord bot account with a token

