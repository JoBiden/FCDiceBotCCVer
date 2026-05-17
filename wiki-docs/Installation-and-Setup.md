# Installation and Setup

This guide will walk you through setting up FCDiceBot from scratch.

## Prerequisites

### Software Requirements

1. **Visual Studio 2013 or later** (for C# development)
   - Community Edition is sufficient
   - Ensure .NET Framework support is installed

2. **MongoDB** (for database)
   - Download from [mongodb.com](https://www.mongodb.com/try/download/community)
   - Install and run MongoDB service
   - Default connection: `mongodb://localhost:27017`

3. **F-List Account**
   - Create account at [f-list.net](https://www.f-list.net)
   - Create a character for the bot to use

### Libraries (Included via NuGet)

- Newtonsoft.Json (13.0.3)
- websocket-sharp
- MongoDB.Driver (2.20.0)
- MongoDB.Bson (2.20.0)
- DnsClient (1.7.0)

## Installation Steps

### 1. Clone the Repository

```bash
git clone https://github.com/JoBiden/FCDiceBotCCVer.git
cd FCDiceBotCCVer
```

### 2. Open in Visual Studio

1. Open `FChatDicebot/FCDicebot.sln` in Visual Studio
2. Restore NuGet packages (right-click solution → Restore NuGet Packages)
3. Build the solution to ensure all dependencies are resolved

### 3. Set Up Directory Structure

The bot expects data files in a specific location:

```
C:\BotData\
└── DiceBot\
    ├── account_settings.txt (required)
    ├── channel_settings.txt (optional)
    ├── ImmediateBackup\ (created automatically)
    └── [other data files created automatically]
```

Create this directory structure:

```cmd
mkdir C:\BotData\DiceBot
mkdir C:\BotData\DiceBot\ImmediateBackup
```

### 4. Configure Account Settings

Create `C:\BotData\DiceBot\account_settings.txt` with your F-List credentials:

```json
{
  "AccountName": "your_flist_account",
  "CharacterName": "BotCharacterName",
  "Password": "your_password",
  "CName": "FCDiceBot",
  "AdminCharacters": ["YourMainCharacter", "AnotherAdminCharacter"]
}
```

**Fields:**
- `AccountName` - Your F-List account name (for login)
- `CharacterName` - The character the bot will use
- `Password` - Your F-List account password
- `CName` - Client name (can be anything, "FCDiceBot" is standard)
- `AdminCharacters` - Array of character names with bot admin privileges

**Security Note:** Keep this file secure. It contains your F-List password.

### 5. Configure MongoDB

#### Option A: Local MongoDB (Recommended for Development)

1. Install MongoDB Community Edition
2. Start MongoDB service:
   ```cmd
   net start MongoDB
   ```
3. Verify MongoDB is running:
   ```cmd
   mongo --eval "db.version()"
   ```

The bot will automatically create the `ChateauDb` database and collections on first run.

#### Option B: Remote MongoDB

If using a remote MongoDB instance, update the connection string in:

`FChatDicebot/FChatDicebot/Database/Chateaudatabase.cs`

```csharp
const string connectionString = "mongodb://your-remote-host:27017";
```

### 6. Configure Channel Settings (Optional)

Create `C:\BotData\DiceBot\channel_settings.txt` to configure per-channel settings:

```json
{
  "adh-channelid": {
    "commandChar": "!",
    "enableChips": true,
    "enableGames": true,
    "enableTables": true,
    "enableSlots": true,
    "startingChips": 1000,
    "chipsClearanceLevel": 0,
    "greetNewUsers": false,
    "slotsMaxMultiplier": 100
  }
}
```

**Fields:**
- `commandChar` - Prefix for commands (default: `!`)
- `enableChips` - Allow chip economy
- `enableGames` - Allow game sessions
- `enableTables` - Allow custom roll tables
- `enableSlots` - Allow slot machines
- `startingChips` - Chips given on registration
- `chipsClearanceLevel` - Admin level required to clear chips
- `greetNewUsers` - Auto-greet users joining channel
- `slotsMaxMultiplier` - Maximum slot payout multiplier

**Note:** Channel IDs are in the format `adh-channelcode` where `channelcode` is from the F-List channel URL.

## Running the Bot

### Development Mode (Visual Studio)

1. Open the solution in Visual Studio
2. Set `FChatDicebot` as the startup project
3. Press F5 to run in debug mode

The bot will:
1. Load account settings
2. Backup existing data files
3. Connect to MongoDB
4. Get F-List authentication ticket
5. Connect to WebSocket
6. Join starting channels (defined in code)
7. Set status to online

### Production Mode (Compiled)

1. Build in Release mode
2. Copy the compiled executable and dependencies
3. Ensure the bot can access:
   - `C:\BotData\DiceBot\`
   - MongoDB instance
   - Internet connection for F-List

Run the executable:

```cmd
FChatDicebot.exe
```

## Initial Configuration

### 1. Test the Connection

Once running, the bot should:
- Connect to F-List
- Join configured channels
- Respond to commands

Test with a simple command:
```
!roll 1d20
```

### 2. Register for Chateau Features

In a channel with the bot:
```
!register
```

This creates your profile in the Chateau database and grants starting chips (if enabled).

### 3. Add Admin Characters

Admin characters are defined in `account_settings.txt`. They can:
- Access bot admin commands
- Manage channel settings
- Clear data
- Create coupons

### 4. Configure Channels

Join the bot to channels:

In F-List, invite the bot character to your channel, or modify the starting channels in `BotMain.cs`:

```csharp
private void JoinStartingChannels()
{
    JoinChannel("adh-channelid1");
    JoinChannel("adh-channelid2");
    // Add more as needed
}
```

## Troubleshooting

### Bot Won't Connect to F-List

**Error:** "Unable to get API ticket"
- **Solution:** Check account credentials in `account_settings.txt`
- **Solution:** Ensure character exists and is owned by the account
- **Solution:** Check internet connection

**Error:** WebSocket connection failed
- **Solution:** Check F-List server status
- **Solution:** Try SSL fallback (automatic)
- **Solution:** Check firewall settings

### Database Connection Issues

**Error:** "MongoDB connection failed"
- **Solution:** Ensure MongoDB service is running
- **Solution:** Check connection string
- **Solution:** Verify MongoDB port (default: 27017)

**Error:** "Unable to access ChateauDb"
- **Solution:** Check MongoDB user permissions
- **Solution:** Ensure database can be created

### File Access Issues

**Error:** "Could not find account_settings.txt"
- **Solution:** Create the file at `C:\BotData\DiceBot\account_settings.txt`
- **Solution:** Check file path matches expected location

**Error:** "Access denied to BotData directory"
- **Solution:** Run as administrator (for initial setup)
- **Solution:** Grant write permissions to the directory

### Bot Not Responding to Commands

**Issue:** Bot joins but ignores commands
- **Check:** Command prefix (default: `!`)
- **Check:** Bot is registered in channel (for Chateau commands)
- **Check:** User is registered (use `!register`)
- **Check:** Console for error messages

### Rate Limiting Issues

**Issue:** Commands timeout or are slow
- **Cause:** F-List enforces 1.5 second rate limit
- **Expected:** Bot will queue messages and send them gradually
- **Solution:** This is normal behavior, not an error

## Advanced Configuration

### Customizing Starting Channels

Edit `BotMain.cs`, method `JoinStartingChannels()`:

```csharp
private void JoinStartingChannels()
{
    JoinChannel("adh-yourchannel1");
    JoinChannel("adh-yourchannel2");
    JoinChannel("adh-yourchannel3");
}
```

### Customizing Bot Behavior

**Change message rate limit** (not recommended, F-List may ban):

In `BotMain.cs`:
```csharp
public static int MessageIntervalMiliseconds = 1500; // Default: 1.5 seconds
```

**Change tick rate:**

In `BotMain.cs`:
```csharp
public static int TickTimeMiliseconds = 200; // Default: 200ms
```

**Change checkin interval** (authentication refresh):

In `BotMain.cs`:
```csharp
private static int CheckinInterval = 20; // Default: 20 minutes
```

### VelvetCuff Integration (Optional)

For real currency chip purchases:

1. Get VelvetCuff API credentials
2. Add to account settings:
   ```json
   {
     "AccountName": "...",
     "VelvetCuffClientId": "your_client_id",
     "VelvetCuffClientSecret": "your_client_secret"
   }
   ```

3. Configure in `VelvetcuffConnection.cs`

## Maintenance

### Backups

The bot automatically backs up data files on startup to:
```
C:\BotData\DiceBot\ImmediateBackup\
```

For MongoDB backups:

```bash
mongodump --db ChateauDb --out C:\Backups\MongoDB\
```

### Updating the Bot

1. Pull latest changes:
   ```bash
   git pull origin main
   ```

2. Rebuild in Visual Studio

3. Test in development mode before deploying

4. Backup data before updating production

### Monitoring

Check logs for errors:
- Console output (if running in terminal)
- Log files (if implemented)
- MongoDB connection status
- F-List connection status

### Common Maintenance Tasks

**Clear chip balances:**
```
!clearchips
```
(Requires bot admin)

**Clear pending interactions:**
- Automatically expire after 10 minutes
- Or restart the bot

**Reset channel deck:**
```
!resetdeck
```

## Security Considerations

1. **Protect account_settings.txt** - Contains F-List password
2. **Secure MongoDB** - Use authentication in production
3. **Admin access** - Only trusted characters in AdminCharacters list
4. **Input validation** - Bot sanitizes user input, but review custom commands
5. **Rate limiting** - Prevents spam, protects against abuse

## Next Steps

- Read the [Command Reference](Command-Reference) to learn available commands
- Explore the [Interaction System](Interaction-System) for Chateau features
- Check the [Development Guide](Development-Guide) to add custom features
- Review the [Architecture](Architecture) to understand the system

## Getting Help

- Check console output for error messages
- Review MongoDB logs
- Verify F-List server status
- Check the wiki for your specific issue
