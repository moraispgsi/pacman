﻿using Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pacman
{
    [Serializable]
    public class Message : EventArgs,IChatMessage
    {

        public string Username { get; set; }
        public string Content { get; set; }

        public Message(string username, string message)
        {
            Username = username;
            Content = message;
        }

    }

    public class ChatRoom : IChat
    {
        public List<IClient> Peers { get; private set; }
        private List<Message> messagesReceived;
        public event MessageReceivedHandler OnMessageReceived;
        public delegate void RemovedPeerHandler(IClient e);
        public event RemovedPeerHandler OnPeerRemoved;
        private Session session;
        private VetorClock<IChatMessage> vetorClock;

        public ChatRoom(Session session)
        {
            if (session == null)
            {
                throw new Exception("The session must not be null");
            }
            this.session = session;
            Peers = new List<IClient>();
            messagesReceived = new List<Message>();
        }

   

        //Remoting
        public void ReceiveMessage(string username, IVetorMessage<IChatMessage> message)
        {
            
            this.vetorClock.ReceiveMessage(message);//quando recebe manda para o vetorclock para ele ver
            //se existe dependencias
            //pede a lista e mostra no jogo
            OnMessageReceived?.Invoke(this.vetorClock.GetMessages());
        }

        public void SetPeers(Dictionary<string, Uri> peers)
        {

            int index = 0;
            foreach (string key in peers.Keys)
            {
                
                Uri address = peers[key];
                IClient client = (IClient)Activator.GetObject(
                    typeof(IClient),
                    address.ToString() + "Client");

                Peers.Add(client);

                if (client.Username == this.session.Username)
                {
                    //cria o vetor clock com a lista de clientes recebida
                   this.vetorClock = new VetorClock<IChatMessage>(peers.Keys.Count,index);
                }
                index++;
            }


        }


        public void PublishMessage(string username, string message)
        {
            
            
            //cria a mensagem, dá um tick o clock
            Message m = new Message(username,message);
            IVetorMessage<IChatMessage> vetorMessage = this.vetorClock.Tick(m);

            //todo upadate
            OnMessageReceived?.Invoke(this.vetorClock.GetMessages());

            for (int i = 0; i < this.Peers.Count; i++)
            {
                /* try
                 {*/
                IClient client = Peers[i];
                if (client.Username == username) continue;
                //envia para cada cliente
                client.ReceiveMessage(username, vetorMessage);
                /*}
                catch (Exception)
                {
                   /* 
                    * IClient removedClient = Peers[i];
                    Peers.RemoveAt(i);
                    OnPeerRemoved?.Invoke(removedClient);
                    
                }*/
            }
        }

    }

    public delegate void MessageReceivedHandler(List<IChatMessage> messages);
}