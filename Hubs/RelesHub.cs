using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace RanchoMqttApi;

[Authorize]
public class RelesHub : Hub
{

}
