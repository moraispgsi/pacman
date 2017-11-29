﻿using Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
    public class LeaderStrategy : ServerStrategy, IServer
    {

        //Volatile state on leaders:
        //for each server, index of the next log entry to send to that server(initialized to leader last log index + 1)
        private List<int> nextIndex = new List<int>();
        //for each server, index of highest log known to be replicated on server(initialized to 0, increases monotonically)
        private List<int> matchIndex = new List<int>();

        private Timer Timer { get; set; }


        public LeaderStrategy(ServerContext context) : base(context, Role.LEADER)
        {
        }

        private void GameEnded()
        {
            Console.WriteLine("Game has ended!");
                this.context.CurrentGameSession.EndGame();
            IGameSession newGameSession = new GameSession(this.context.NumPlayers);
            this.context.GameSessionsTable[newGameSession.ID] = newGameSession;
            this.context.CurrentGameSession = newGameSession;
            // TODO: handle clients and server lists
            // TODO: Generate Log Entry and multicast AppendEntries
        }

        private void Tick(Object parameters)
        {
            if (this.context.CurrentGameSession.HasGameEnded())
            {
                GameEnded();
                return;
            }

            this.context.CurrentGameSession.PlayRound(); //Maybe send the log to the game session
            this.Timer = new Timer(new TimerCallback(Tick), null, this.context.RoundIntervalMsec, Timeout.Infinite);
        }

        private void addPlayersToCurrentGameSession()
        {
            int playersWaiting = this.context.CurrentGameSession.Clients.Count;
            int leftPlayers = this.context.NumPlayers - playersWaiting;

            List<IClient> inQuePlayers = this.context.WaitingQueue.Take(leftPlayers).ToList();

            //vamos obter os jogadores em lista de espera
            foreach (IClient player in inQuePlayers)
            {
                if (player != null)
                {
                    this.context.CurrentGameSession.Clients.Add(player);
                    this.context.WaitingQueue.RemoveAll(p => p.Address == player.Address);
                }
            }
        }

        public override Uri GetLeader()
        {
            return this.context.Address;
        }

        // TODO: What happens if a lot of players try to join at the same time? The method probably isn't thread safe.
        // Add the players to the GameSession until the session is full. Start the game when it is full 
        public override JoinResult Join(string username, Uri address)
        {
            Console.WriteLine($"Username {username} Address {address.ToString()}");
            if (this.context.Clients.Exists(c => c.Username == username) ||
                this.context.WaitingQueue.Exists(c => c.Username == username))
            {
                // TODO: Lauch exception to the client (username already exists)
                return JoinResult.REJECTED_USERNAME;
            }

            // ao enviar os dados dos clients para um cliente devem enviar o endereço e o username.
            IClient client = (IClient)Activator.GetObject(
                typeof(IClient),
                address.ToString() + "Client");
            //client.Username = username;

            Console.WriteLine(address.ToString());

            this.context.WaitingQueue.Add(client);

            Console.WriteLine(string.Format("Sending to client '{0}' that he has just been queued", username));

            this.context.Clients.Add(client);

            //vamos ver se podemos começar o jogo
            if (this.context.WaitingQueue.Count >= this.context.NumPlayers)
            {

                //mutex.WaitOne();
                Thread thread = new Thread(new ThreadStart(() =>
                {
                    // todo: this block has a problem
                    addPlayersToCurrentGameSession();
                    this.context.CurrentGameSession.StartGame();
                    Timer = new Timer(new TimerCallback(Tick), null, this.context.RoundIntervalMsec, Timeout.Infinite);
                }));
                thread.Start();
                //mutex.ReleaseMutex();
            }

            return JoinResult.QUEUED;
        }

        public override void SetPlay(Uri address, Play play, int round)
        {
            if (this.context.CurrentGameSession != null && this.context.CurrentGameSession.HasGameStarted)
            {
                this.context.CurrentGameSession.SetPlay(address, play, round);
            }
            else
            {
                // TODO: What?
            }
        }

        public override void Quit(Uri address)
        {
            Console.WriteLine(String.Format("Client at {0} is disconnecting.", address));
            this.context.Clients.RemoveAll(p => p.Address.ToString() == address.ToString());
            this.context.WaitingQueue.RemoveAll(p => p.ToString() == address.ToString());
        }


        public override void RegisterReplica(Uri replicaServerURL)
        { 
            this.context.ReplicaServersURIsList.Add(replicaServerURL);

            IServer replica = (IServer)Activator.GetObject(
                typeof(IServer),
                replicaServerURL.ToString() + "Server");

            this.context.Replicas.Add(replicaServerURL, replica);
            broadcastReplicaList();
        }

        private void broadcastReplicaList()
        {
            foreach (IClient client in this.context.Clients)
            {
                client.SetReplicaList(this.context.ReplicaServersURIsList);
            }

            foreach (Uri uri in this.context.ReplicaServersURIsList)
            {
                IServer server = this.context.Replicas[uri];
                //TODO: Create the log and send it to the replica
                //server.SetReplicaList(this.context.ReplicaServersURIsList);
            }
        }
    }
}