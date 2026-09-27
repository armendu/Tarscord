# Tarscord 🤖

Named after TARS from Interstellar, Tarscord is a sarcastic Discord bot designed to help you manage your server with a touch of humor.

✨ Features
	•	Moderation Tools: Mute and unmute members seamlessly.
	•	Event Management: Add events and save them to a local database for easy tracking.
	•	Random Number Generation: Generate random numbers for games or decision-making.
	•	Reminders: Set reminders to keep your community engaged and informed.
	•	More to Come: Continuous updates with new and exciting features.

📦 Installation

1️⃣ Prerequisites
	•	.NET SDK: Ensure you have the latest version installed. You can download it from the .NET official website.

2️⃣ Clone the Repository

git clone https://github.com/armendu/Tarscord.git
cd Tarscord

3️⃣ Configure the Bot
	1.	Create a Discord Application:
	•	Navigate to the Discord Developer Portal.
	•	Click on “New Application” and provide a name.
	•	In the “Bot” section, add a new bot to your application.
	•	Copy the Token provided for the bot.
	2.	Set Up Configuration:
	•	In the src directory, locate the config.yml file.
	•	Replace the placeholder TokenId with the token you copied:

TokenId: "YOUR_DISCORD_BOT_TOKEN"



4️⃣ Build and Run

Navigate to the src directory and execute:

dotnet build
dotnet run

🔧 Configuration
	•	config.yml: Located in the src directory, this file contains essential configurations:
	•	TokenId: Your Discord bot token.
	•	Other settings can be adjusted as needed to customize Tarscord’s behavior.

💡 Usage

Once Tarscord is up and running:
	1.	Invite the Bot to Your Server:
	•	Generate an OAuth2 URL in the Discord Developer Portal with the necessary permissions.
	•	Use the URL to invite Tarscord to your server.
	2.	Interact with Tarscord:
	•	Use commands such as /mute, /unmute, /addevent, /random, and /remind to utilize its features.
	•	Tarscord responds with a unique, sarcastic flair to keep interactions entertaining.

🤝 Contributing

We welcome contributions! To get involved:
	1.	Fork the repository.
	2.	Create a new branch (feature/your-feature).
	3.	Commit your changes.
	4.	Push the branch and create a Pull Request.

Please ensure your code adheres to the project’s coding standards and includes appropriate tests.

📜 License

Tarscord is licensed under the MIT License. For more details, refer to the LICENSE file.

⭐ Support & Community

If you find Tarscord useful or entertaining:
	•	Star the Repository: Show your support by starring the project on GitHub!
	•	Report Issues: Encountered a bug or have a feature request? Open an issue.
	•	Stay Updated: Watch the repository for updates and new features.

Elevate your Discord server management with Tarscord’s unique blend of functionality and humor.