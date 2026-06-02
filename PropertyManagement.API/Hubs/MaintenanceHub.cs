using Microsoft.AspNetCore.SignalR;

namespace PropertyManagement.API.Hubs
{
    // SignalR hub that powers the real time maintenance board
    // clients join the StaffBoard group on page load and receive live push events

    public class MaintenanceHub : Hub
    {
        // called by the board page when it loads to subscribe this connection to staff broadcasts
        public async Task JoinStaffBoard()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "StaffBoard");
        }

        // called when the user navigates away from the board to unsubscribe
        public async Task LeaveStaffBoard()
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "StaffBoard");
        }
    }
}
