# F-List Integration

This page explains how FCDiceBot integrates with F-List chat, including authentication, WebSocket communication, and message handling.

## F-List Chat Protocol

FCDiceBot connects to F-List's WebSocket-based chat server to send and receive messages.

### Connection Details

- **WebSocket URL:** `wss://chat.f-list.net/chat2`
- **Protocol:** WebSocket (WSS - secure)
- **Message Format:** JSON
- **Authentication:** Ticket-based

## Authentication Flow

### 1. Get API Ticket

**Location:** `BotMain.cs` → `GetNewApiTicket()`

```csharp
POST https://www.f-list.net/json/getApiTicket.php
Body: {
    "account": "your_account",
    "password": "your_password"
}
```

**Response:**
```json
{
    "ticket": "unique_authentication_ticket",
    "characters": ["Character1", "Character2", ...]
}
```

The ticket is valid for approximately 30 minutes and is used to identify the connection.

### 2. WebSocket Connection

**Location:** `BotMain.cs` → `Run()`

1. Connect to `wss://chat.f-list.net/chat2`
2. Wait for WebSocket to be ready
3. Send IDN (identification) message

### 3. IDN (Identification) Message

**Location:** `BotMain.cs` → `GetNewIDNRequest()`

```json
{
    "method": "ticket",
    "account": "your_account",
    "ticket": "ticket_from_step_1",
    "character": "BotCharacterName",
    "cname": "FCDiceBot",
    "cversion": "1.0"
}
```

**Note:** `cname` and `cversion` identify your client to F-List.

### 4. Server Response

On successful authentication, the server sends:
- `IDN` confirmation
- `HLO` hello message with connection info
- Channel lists
- User lists

## Message Types

### Incoming Messages

The bot handles the following F-List server message types:

| Type | Description | Handler |
|------|-------------|---------|
| `PIN` | Server ping (keepalive) | Respond with `PIN` |
| `MSG` | Channel message | Parse for commands |
| `PRI` | Private message | Parse for commands |
| `JCH` | User joined channel | Greet if enabled |
| `LCH` | User left channel | Update tracking |
| `NLN` | User connected | Track online status |
| `FLN` | User disconnected | Track offline status |
| `COL` | Channel ops list | Process pending admin commands |
| `CDS` | Channel description | Update channel info |
| `ICH` | Initial channel data | Update channel info |
| `CHA` | Channel announcement | Display to console |
| `STA` | Status change | Track user status |
| `KID` | Kicked from channel | Log and rejoin logic |
| `ERR` | Error from server | Log and handle |
| `VAR` | Server variables | Update chat variables |
| `HLO` | Hello (connection info) | Log connection success |

**Implementation:** `BotMain.cs` → `OnMessage(string data)`

```csharp
switch(messageType) {
    case "PIN":
        SendPing();
        break;
    case "MSG":
        var msgData = JsonConvert.DeserializeObject<MSGserver>(data);
        InterpretChatCommand(msgData.message, msgData.character, msgData.channel);
        break;
    case "PRI":
        var priData = JsonConvert.DeserializeObject<PRIserver>(data);
        InterpretChatCommand(priData.message, priData.character, null);
        break;
    // ... more cases
}
```

### Outgoing Messages

#### MSG (Channel Message)

**Location:** `BotMain.cs` → `SendMessageInChannel()`

```json
{
    "type": "MSG",
    "channel": "adh-channelid",
    "message": "Message text here"
}
```

#### PRI (Private Message)

**Location:** `BotMain.cs` → `SendPrivateMessage()`

```json
{
    "type": "PRI",
    "recipient": "CharacterName",
    "message": "Private message text"
}
```

#### JCH (Join Channel)

**Location:** `BotMain.cs` → `JoinChannel()`

```json
{
    "type": "JCH",
    "channel": "adh-channelid"
}
```

#### LCH (Leave Channel)

**Location:** `BotMain.cs` → `LeaveChannel()`

```json
{
    "type": "LCH",
    "channel": "adh-channelid"
}
```

#### STA (Set Status)

**Location:** `BotMain.cs` → `SetStatus()`

```json
{
    "type": "STA",
    "status": "online",
    "statusmsg": "FCDiceBot ready!"
}
```

**Status Values:**
- `online` - Available
- `busy` - Do not disturb
- `dnd` - Do not disturb
- `away` - Away
- `looking` - Looking for roleplay

#### PIN (Keepalive)

**Location:** `BotMain.cs` → `SendPing()`

```json
{
    "type": "PIN"
}
```

Sent in response to server `PIN` to maintain connection.

## Rate Limiting

### F-List Requirements

F-List enforces strict rate limits to prevent spam:

- **Message Rate:** Maximum 1 message per 1.5 seconds
- **Flood Protection:** Sending too fast can result in kicks or bans
- **Burst Allowance:** Some burst is tolerated, but sustained rate matters

### Bot Implementation

**Location:** `BotMain.cs` → `RunLoop()`

```csharp
public static int MessageIntervalMiliseconds = 1500; // 1.5 seconds

// In RunLoop():
if (DateTime.Now.Subtract(LastSentMessage).TotalMilliseconds > MessageIntervalMiliseconds) {
    BotMessage nextMessage = MessageQueue.GetNextMessage();
    if (nextMessage != null) {
        SendMessage(nextMessage);
        LastSentMessage = DateTime.Now;
    }
}
```

**Message Queue System:**
- All outgoing messages are queued
- Dequeued at maximum rate (1.5s intervals)
- Prevents accidental rate limit violations
- Priority queue for important messages

## Connection Management

### Keepalive System

**Ping-Pong:**
- Server sends `PIN` approximately every 45 seconds
- Bot must respond with `PIN` to maintain connection
- Failure to respond results in disconnection

**Implementation:** `BotMain.cs` → `OnMessage()`

```csharp
case "PIN":
    SendPing();
    break;
```

### Checkin System

**Location:** `BotMain.cs` → `RunLoop()`

Every 20 minutes, the bot refreshes its authentication ticket:

```csharp
if (DateTime.Now.Subtract(LastCheckin).TotalMinutes > CheckinInterval) {
    GetNewApiTicket();
    LastCheckin = DateTime.Now;
}
```

This prevents session expiration on long-running bots.

### Reconnection Logic

**Location:** `BotMain.cs` → `Run()`

If the WebSocket closes:

1. **Wait 5 seconds**
2. **Check if disconnection was intentional**
   - If `StayConnected == false`, don't reconnect
3. **Try reconnecting with HTTPS first**
4. **If HTTPS fails, try SSL fallback**
5. **Get new API ticket**
6. **Reconnect WebSocket**
7. **Send new IDN message**
8. **Rejoin channels**

**Error Handling:**

```csharp
ws.OnClose += (sender, e) => {
    if (StayConnected) {
        System.Threading.Thread.Sleep(5000);
        Run(); // Reconnect
    }
};

ws.OnError += (sender, e) => {
    Utils.AddToLog(e.Message, "ERROR");
};
```

### Connection Timeout

**5-Minute Timeout:**

If no messages received for 5 minutes, assume connection is dead and reconnect:

```csharp
if (DateTime.Now.Subtract(LastReceivedMessage).TotalMinutes > 5) {
    ws.Close();
    Run(); // Reconnect
}
```

## Channel Management

### Joining Channels

**Automatic on Startup:** `JoinStartingChannels()`

```csharp
private void JoinStartingChannels() {
    JoinChannel("adh-channel1");
    JoinChannel("adh-channel2");
}
```

**Manual Join:** `!joinchannel adh-channelid` (bot admin only)

### Tracking Joined Channels

**Location:** `BotMain.cs`

```csharp
public List<string> ChannelsJoined = new List<string>();
```

Updated on:
- `JCH` response (successful join)
- `LCH` (leave channel)
- `KID` (kicked from channel)

### Channel Ops Management

**Why:** Some commands require channel op verification

**Process:**
1. Command requires op check
2. Bot requests ops list: `COR` (Channel Op Request)
3. F-List responds with `COL` (Channel Op List)
4. Bot verifies user is in op list
5. Command executes or denies

**Implementation:** `BotMain.cs`

```csharp
// Store pending commands waiting for op verification
Dictionary<string, List<UserGeneratedCommand>> WaitingChannelOpRequests;

// When op check needed:
SendChannelOpRequest(channel);
WaitingChannelOpRequests[channel].Add(command);

// When COL received:
case "COL":
    var opList = JsonConvert.DeserializeObject<COLserver>(data);
    ProcessPendingCommands(opList.channel, opList.oplist);
    break;
```

## BBCode Formatting

F-List supports BBCode formatting in messages. The bot uses this for rich output.

### Common BBCode Tags

| Tag | Description | Example |
|-----|-------------|---------|
| `[b]text[/b]` | Bold | `[b]Important[/b]` |
| `[i]text[/i]` | Italic | `[i]Emphasis[/i]` |
| `[u]text[/u]` | Underline | `[u]Underlined[/u]` |
| `[user]Name[/user]` | User link | `[user]Character[/user]` |
| `[url=...]text[/url]` | Link | `[url=http://example.com]Link[/url]` |
| `[color=red]text[/color]` | Color | `[color=red]Red text[/color]` |
| `[session=Title]text[/session]` | Session link | `[session=RP Session]Join[/session]` |
| `[channel]ID[/channel]` | Channel link | `[channel]adh-example[/channel]` |
| `[icon]Name[/icon]` | Character icon | `[icon]Character[/icon]` |
| `[spoiler]text[/spoiler]` | Spoiler | `[spoiler]Hidden[/spoiler]` |
| `[sup]text[/sup]` | Superscript | `x[sup]2[/sup]` |
| `[sub]text[/sub]` | Subscript | `H[sub]2[/sub]O` |

### Bot Usage

**User Tags:** Used extensively for targeting users

```csharp
// Parse user from command:
string user = Utils.GetCharacterUserTags(terms[0]);
// Produces: [user]CharacterName[/user]
```

**Formatting Output:**

```csharp
string message = $"[b]{initiator}[/b] kisses [b]{recipient}[/b]!";
SendMessageInChannel(channel, message);
```

**Spoiler for Long Messages:**

```csharp
if (message.Length > 500) {
    message = $"[spoiler]{message}[/spoiler]";
}
```

## Error Handling

### Server Errors

**ERR Message:**

```json
{
    "type": "ERR",
    "number": 4,
    "message": "Error description"
}
```

**Common Error Codes:**
- `4` - Too many connections
- `9` - Authentication failed
- `28` - Flood protection triggered
- `30` - Invalid command
- `39` - Character doesn't have permission

**Bot Response:**

```csharp
case "ERR":
    var error = JsonConvert.DeserializeObject<ERRserver>(data);
    Utils.AddToLog($"F-List Error {error.number}: {error.message}", "ERROR");

    // Handle specific errors:
    if (error.number == 28) {
        // Slow down message rate
    }
    break;
```

### Connection Errors

**WebSocket Closure:**
- Automatic reconnection (see Reconnection Logic above)
- Exponential backoff (5 seconds initial)

**Authentication Failure:**
- Log error
- Check credentials
- Retry with new ticket

## Performance Considerations

### Message Queue Size

Large queue = slow response time:
- Average 1.5s per message
- 100 messages = 2.5 minutes to clear

**Solution:** Prioritize important messages, batch similar messages

### Channel Count

More channels = more message traffic:
- Each channel can trigger commands
- Rate limit is global (all channels share the 1.5s limit)

**Recommendation:** Limit to 5-10 active channels for responsive bot

### Pending Commands

Check and clean pending commands regularly:
- 10-minute expiration
- Clear on consent/rejection
- Prevent database bloat

## Security Considerations

### Authentication

- Never log passwords
- Store tickets securely (memory only)
- Refresh tickets regularly (20 min interval)

### Input Validation

All user input is sanitized:

```csharp
// In BotCommandController:
for (int i = 0; i < terms.Length; i++) {
    terms[i] = terms[i].ToLower();
    terms[i] = Utils.SanitizeInput(terms[i]);
}
```

### Permission Checks

Before executing commands:
- Check if user is registered
- Check if user is bot admin
- Check if user is channel op (if required)

### Rate Limit Protection

Built-in rate limiting prevents:
- Accidental spam
- Malicious flood attacks
- Server bans

## Advanced Features

### Future Messages

**Schedule messages for later:**

```csharp
BotMain.FutureMessages.Add(new FutureMessage {
    channel = channel,
    message = message,
    sendTime = DateTime.Now.AddMinutes(5)
});
```

**Processed in RunLoop():**

```csharp
foreach (var fm in FutureMessages.Where(f => f.sendTime <= DateTime.Now)) {
    SendMessageInChannel(fm.channel, fm.message);
    FutureMessages.Remove(fm);
}
```

### VelvetCuff Polling

**Background task checking payment status:**

Every 4 seconds, check for completed VelvetCuff transactions:

```csharp
if (DateTime.Now.Subtract(LastVcOrderCheck).TotalSeconds > 4) {
    CheckVelvetcuffOrders();
    LastVcOrderCheck = DateTime.Now;
}
```

When payment confirmed:
1. Add chips to user's balance
2. Send confirmation message
3. Delete transaction record

### Status Updates

Update bot status based on activity:

```csharp
// On idle:
SetStatus("online", "Waiting for commands...");

// During heavy load:
SetStatus("busy", "Processing...");

// During maintenance:
SetStatus("dnd", "Updating...");
```

## Debugging F-List Connection

### Enable Verbose Logging

Add logging to OnMessage:

```csharp
ws.OnMessage += (sender, e) => {
    Utils.AddToLog($"Received: {e.Data}", "DEBUG");
    OnMessage(e.Data);
};
```

### Monitor Connection State

```csharp
Utils.AddToLog($"WebSocket State: {ws.ReadyState}", "DEBUG");
```

**States:**
- `Connecting` - Initial connection
- `Open` - Connected and ready
- `Closing` - Graceful shutdown
- `Closed` - Disconnected

### Test Commands

Simple commands to verify connection:

```
!roll 1d6
!echo test
!ping
```

## See Also

- [Installation and Setup](Installation-and-Setup) - Initial configuration
- [Architecture](Architecture) - System design
- [Command Reference](Command-Reference) - Available commands
