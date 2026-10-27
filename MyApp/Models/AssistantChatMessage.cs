namespace MyApp.Models;

public sealed record AssistantChatMessage(string Sender, string Text, bool IsUser);
