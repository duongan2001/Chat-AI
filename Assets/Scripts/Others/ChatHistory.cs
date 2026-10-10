using System;
using System.Collections.Generic;

[Serializable]
public class ChatHistory
{
    public long lastMessageTimestamp;
    public List<Message> messages;
}
