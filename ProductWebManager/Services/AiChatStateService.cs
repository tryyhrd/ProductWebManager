using System;
using ProductWebManager.Models;

namespace ProductWebManager.Services;

public class AiChatStateService
{
    public event Action? OnHistoryCleared;
    public event Action? OnChatUpdated;

    public void NotifyHistoryCleared() => OnHistoryCleared?.Invoke();
    
    public void NotifyChatUpdated() => OnChatUpdated?.Invoke();
}
