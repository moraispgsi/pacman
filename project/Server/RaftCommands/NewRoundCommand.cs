﻿using System;
using System.Collections.Generic;
using System.Linq;
using Shared;

namespace Server.RaftCommands
{
    /// <summary>
    /// The comman to compute a new round
    /// </summary>
    [Serializable]
    public class NewRoundCommand : RaftCommand
    {
        public override void Execute(RaftServer server, bool AsLeader)
        {
          //  Console.WriteLine("number players:" + server.StateMachine.Stage.GetPlayers().Count);

            foreach (Uri address in server.plays.Keys)
            {
                IPlayer player = server.StateMachine
                    .Stage.GetPlayers().First(p => p.Address.ToString() == address.ToString());
                server.StateMachine.SetPlay(player, server.plays[address]);
            }

            server.plays.Clear();
            if (AsLeader)
            {
             //   Console.WriteLine("EXECUTE ROUND AS LEADER");
                List<Shared.Action> actionList = server.StateMachine.NextRound();

                IClient client;
                for (int i = server.SessionClients.Count - 1; i >= 0; i--)
                {
                    try
                    {
                        client = server.SessionClients.ElementAt(i);
                        client.SendRound(actionList, server.PlayerList, server.StateMachine.Round);
                     //   Console.WriteLine(String.Format("Sending stage to client: {0}, at: {1}", client.Username, client.Address));
                     //   Console.WriteLine(String.Format("Round Nº{0}", server.StateMachine.Round));
                    }
                    catch (Exception ex)
                    {
                      //  Console.WriteLine("CANT CONTACT CLIENT" + ex);
                        //server.SessionClients.RemoveAt(i);


                        // todo: try to reach the client again. Uma thread à parte. Verificar se faz sentido.

                        /*todo:
                         * qual a estrategia a adoptar aqui para tentar reconectar com o cliente?
                         * 
                         * Dectar falhas de clientes, lidar com falsos positivos.
                         * 
                         * Caso não seja pssível contactar o cliente, na próxima ronda deve de ir uma acção em que o player 
                         * está morto, e deve ser removido do jogo.
                         * E deve ser apresentado no chat UMA MENSAGEM no chat a indicar que o jogador saiu do jogo
                         * 
                         * garantimos a possibilidade de um cliente voltar a entrar no jogo?
                         * 
                         */
                    }
                }

                server.RoundTimer.Start(); //Start the timer

            };
        }
    }
}