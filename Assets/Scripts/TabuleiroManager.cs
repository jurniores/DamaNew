using System.Collections;
using System.Collections.Generic;
using Omni.Core;
using UnityEngine;

public class TabuleiroManager : DualBehaviour
{
    private List<NetworkPeer> peersConnected = new();
    protected override void OnServerPeerConnected(NetworkPeer peer, Phase phase)
    {
        if (phase == Phase.End)
        {
            peersConnected.Add(peer);
            if (peersConnected.Count == 2)
            {
                string nameGroup = $"Game_{System.Guid.NewGuid()}";
                var group = ServerMatchmaking.AddGroup(nameGroup);

                peersConnected.ForEach(p =>
                {
                    ServerMatchmaking.JoinGroup(group, p);
                });

                var serverGroup = NetworkManager.GetPrefab(0).Spawn(NetworkManager.ServerSide.ServerPeer, groupId: group.Id).Get<GroupManager>();
                serverGroup.group = group;
                serverGroup.name = nameGroup;

                peersConnected = new();
            }

        }
    }
}
