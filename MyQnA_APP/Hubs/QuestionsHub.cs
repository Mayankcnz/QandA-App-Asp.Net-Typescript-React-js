using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;


namespace MyQnA_APP.Hubs
{
    /**
     * The base Hub class gives us the features we need to interact with clients.
     * A hub is a class on the server where we can interact with clients. we can
     * choose to interact with a single client, all connected clients, or just a subset of them. 
     */
    public class QuestionsHub : Hub
    {


        // When a client connects to  this hub, this method wol;l be called, which calls
        // the base implementation of this method in the first statement
        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
            await Clients.Caller.SendAsync("Message", "Successfully Connected");
        }


        // a handler called Message with a paramter value of succesffuly disconnected
        // will be called in our react client when it disconnects from the signalR API 
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            await Clients.Caller.SendAsync("Message", "Successfully disconnected");
            await base.OnDisconnectedAsync(exception);

        }


        /**
         * We use the AddToGroupAsync method in the Groups property to add the client to the group
         * while passing in the client connection ID, which we can get from the Context property on the Hub base class.
         * The second parameter that's passed to the AddToGroupAsync method is the name of the group,
         * which we set to the word "Question", followed by a hyphen and then the question ID. 
         * If the group doesn't exist, SignalR will automatically create the group,
         * which will be the case for the first client that subscribes to a question
         */
        public async Task SubscribeQuestion(int questionId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Question-{questionId}");
            await Clients.Caller.SendAsync("Message", "Successfully subscribed");
        }


        /**
         *  When all the clients have been removed from the group, SignalR will automatically remove the group.
         */
        public async Task UnsubscribeQuestion(int questionId) 
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Question-{questionId}");
            await Clients.Caller.SendAsync("Message", "Successfully unsubscribed"); }
    }
}

