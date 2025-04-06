using System.Collections;
using System.Collections.Generic;
using Omni.Collections;
using Omni.Core;
using UnityEngine;

[RequireComponent(typeof(NetworkIdentity))] // Ensure this script is a NetworkBehaviour
public class GroupManager : NetworkBehaviour
{
    public Dictionary<(int, int), Botao> botoes = new();
    public ObservableDictionary<string, string> botoesStrings = new();
    public NetworkGroup group;
    public List<PlayerManager> entityList = new();
    int count = 0;
    int vira = 0;
    public void StartGame()
    {
        vira++;
        foreach (var botao in botoes.Values)
        {
            botao.Identity.Despawn();
        }
        botoes.Clear();
        botoesStrings.Clear();
        bool lado = true;
        if (vira > 1) entityList.Reverse();
        foreach (var player in entityList)
        {
            player.Lado = lado;
            player.PossoJogar = lado;
            player.QtdGanhas = 0;
            player.group = this;
            player.Jogue(lado);
            InstantiateBotoes(lado, player.Identity.Owner, player);
            lado = !lado; // Toggle the side for the next player
        }
    }
    protected override void OnServerClientSpawned(NetworkPeer peer)
    {
        count++;
        if (count == 2)
        {
            foreach (var p in group.Peers.Values)
            {
                var player = NetworkManager.GetPrefab(1).Spawn(p, groupId: group.Id).Get<PlayerManager>();
                entityList.Add(player);
            }
            StartGame();
            count = 0;
        }
    }

    public void Jogado(PlayerManager meuPlayer, bool possoComerMais = false)
    {
        entityList.ForEach(p =>
        {
            if (p != meuPlayer)
            {
                p.Jogue(!possoComerMais);
            }
            else
            {
                p.Jogue(possoComerMais);
            }
        });
    }
    public void InstantiateBotoes(bool lado, NetworkPeer peer, PlayerManager playerManager)
    {
        if (lado)
        {
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 8; j += 2)
                {
                    SetBotoes(i, j);
                }
            }
        }
        else
        {
            for (int i = 5; i < 8; i++) // Start from row 5 for the second player
            {
                for (int j = 0; j < 8; j += 2)
                {
                    SetBotoes(i, j);
                }
            }
        }
        void SetBotoes(int i, int j)
        {
            int newJ = j;
            if (i % 2 != 0) newJ = j + 1;

            var botao = NetworkManager.GetPrefab(2).Spawn(peer, groupId: group.Id).Get<Botao>();
            botao.MyPosition = new byte[] { (byte)i, (byte)newJ };
            botao.ChangeSprite(lado); // Set the sprite based on the player's side
            botao.playerManager = playerManager; // Assign the player manager to the button
            botao.group = this;
            botao.transform.SetParent(transform, false);
            AddBotao(new int[] { i, newJ }, botao);
        }
    }

    public void AddBotao(int[] pos, Botao botao)
    {
        botoes.TryAdd((pos[0], pos[1]), botao);
        botoesStrings.TryAdd($"{pos[0]}-{pos[1]}", botao.name);
    }
    public void AddBotao(byte[] pos, Botao botao)
    {
        botoes.TryAdd((pos[0], pos[1]), botao);
        botoesStrings.TryAdd($"{pos[0]}-{pos[1]}", botao.name);
    }

    public void RemoveBotao(int[] pos)
    {
        botoes.Remove((pos[0], pos[1]));
        botoesStrings.Remove($"{pos[0]}-{pos[1]}");
    }
    public void RemoveBotao(byte[] pos)
    {
        botoes.Remove((pos[0], pos[1]));
        botoesStrings.Remove($"{pos[0]}-{pos[1]}");
    }
    public bool TryGetBotao(int[] pos, out Botao botao)
    {
        if (botoes.TryGetValue((pos[0], pos[1]), out botao))
        {
            return true;
        }
        return false;
    }
}
