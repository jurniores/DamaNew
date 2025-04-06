using System.Collections;
using System.Collections.Generic;
using Omni.Core;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(NetworkIdentity))]
public partial class PlayerManager : NetworkBehaviour
{
    [NetworkVariable]
    private bool m_Lado = false;
    [NetworkVariable]
    private int m_QtdGanhas = 0;
    [NetworkVariable]
    private bool m_PossoJogar = false;

    [SerializeField]
    private TextMeshProUGUI timeText, qtdGanhasText;
    public GroupManager group;

    protected override void OnStart()
    {
        if (IsLocalPlayer)
        {
            NetworkService.Get<ManagerClient>().pManagerLocal = this;
            var texts = GameObject.Find("player1").GetComponentsInChildren<TextMeshProUGUI>();
            timeText = texts[0]; // Assuming the first TextMeshProUGUI is for time
            qtdGanhasText = texts[1]; // Assuming the second TextMeshProUGUI is for qtdGanhas
        }
        else if (IsClient)
        {
            NetworkService.Get<ManagerClient>().pManagerEnemy = this;
            var texts = GameObject.Find("player2").GetComponentsInChildren<TextMeshProUGUI>();
            timeText = texts[0]; // Assuming the first TextMeshProUGUI is for time
            qtdGanhasText = texts[1]; // Assuming the second TextMeshProUGUI is for qtdGanhas
        }
    }

    public void StopTime()
    {

        StopAllCoroutines();
        timeText.text = ""; // Clear the time text when stopping the timer
    }
    public void Jogue(bool jogar)
    {
        StopAllCoroutines();
        if (jogar)
        {
            StartCoroutine(WaitJogarIE());
            Server.Rpc(ConstantsDama.SEND_TIME);
        }

        PossoJogar = jogar;
    }
    [Client(ConstantsDama.SEND_TIME)]
    void RecieveTimeClientRPC(DataBuffer buffer)
    {
        StartCoroutine(WaitJogarIE());

    }
    partial void OnQtdGanhasChanged(int prevQtdGanhas, int nextQtdGanhas, bool isWriting)
    {
        if (isWriting)
        {
            if (nextQtdGanhas == 12)
            {
                group.StartGame();
            }
        }
        if (IsClient)
        {
            qtdGanhasText.text = "Ganhas: " + nextQtdGanhas.ToString();
        }
    }
    IEnumerator WaitJogarIE()
    {
        int count = 10;
        while (count > 0)
        {
            count--;
            yield return new WaitForSeconds(1f);
            if (IsServer && count == 0)
            {
                group.Jogado(this);

            }
            if (IsClient)
            {
                if (count == 0)
                {
                    timeText.text = "Tempo esgotado!";
                }
                else if (count <= 2)
                {
                    timeText.text = "Ultimos segundos";
                }
                else
                {
                    timeText.text = "Tempo: " + count.ToString();
                }
            }
        }
    }
}
