using Microsoft.AspNetCore.SignalR;

namespace CVManager.Web.Hubs;

public class DiscussionHub : Hub
{
    // Clients join a group for a specific position
    public async Task JoinPosition(int positionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"position_{positionId}");
    }

    public async Task LeavePosition(int positionId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"position_{positionId}");
    }
}