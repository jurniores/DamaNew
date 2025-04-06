using System;
using System.Collections;
using System.Collections.Generic;
using Omni.Core;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(NetworkIdentity))]
public partial class Botao : NetworkBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
{
    [NetworkVariable(CheckEquality = false)]
    private byte[] m_MyPosition = new byte[2] { 15, 15 };
    [NetworkVariable]
    private bool m_IsDama = false;
    [SerializeField]
    private CanvasGroup canvasGroup;
    [SerializeField]
    private Image image;
    [SerializeField]
    private Canvas canvas;
    private bool moved = false;
    private Vector2 initialPosition;
    private bool lado;
    public PlayerManager playerManager; // Reference to the PlayerManager component for local and enemy players
    [SerializeField]
    private Sprite[] sprites;
    public GroupManager group;
    public UnityAction mark;
    bool podeComerMais = false;

    protected override void OnStart()
    {
        if (IsServer)
        {
            canvas.enabled = false;
        }

        if (IsLocalPlayer)
        {
            playerManager = NetworkService.Get<ManagerClient>().pManagerLocal;
        }
        else
        {
            playerManager = NetworkService.Get<ManagerClient>().pManagerEnemy;
        }

        if (!IsServer) ChangeSprite(playerManager.Lado);
    }

    public void ChangeSprite(bool lado)
    {
        this.lado = lado;
        if (IsServer) return;
        SetSprite(image);
    }

    public void SetSprite(Image image)
    {
        if (lado)
            image.sprite = sprites[0];
        else
            image.sprite = sprites[1];
    }
    public void OnDrag(PointerEventData eventData)
    {
        if (!IsLocalPlayer) return;
        if (!moved) return;
        Vector2 mousePos = Input.mousePosition;
        transform.position = (Vector2)Camera.main.ScreenToWorldPoint(mousePos);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!IsLocalPlayer) return;
        initialPosition = transform.position;
        moved = true;
        canvasGroup.blocksRaycasts = false;
        image.color = Color.red;
        canvas.sortingOrder = 3;
    }

    public void StateInitial()
    {
        moved = false;
        canvasGroup.blocksRaycasts = true;
        image.color = Color.white;
        canvas.sortingOrder = 2;
        StopAllCoroutines();
    }
    public void OnPointerUp(PointerEventData eventData)
    {
        if (!IsLocalPlayer) return;
        StartCoroutine(ReturnInitial());
    }

    IEnumerator ReturnInitial()
    {
        yield return new WaitForSeconds(0.5f);
        transform.position = initialPosition;
        StateInitial();
    }


    partial void OnMyPositionChanged(byte[] prevMyPosition, byte[] nextMyPosition, bool isWriting)
    {
        if (IsClient)
        {
            if (prevMyPosition != nextMyPosition && nextMyPosition != new byte[2] { 15, 15 }) playerManager.StopTime();
            NetworkService.Get<Tabuleiro>().SetInCampo(nextMyPosition, this);
        }
    }
    partial void OnIsDamaChanged(bool prevIsDama, bool nextIsDama, bool isWriting)
    {
        if (IsClient)
        {
            if (nextIsDama)
            {
                var imgs = GetComponentsInChildren<Image>(true);
                SetSprite(imgs[1]);
                imgs[1].gameObject.SetActive(true);
            }
        }
    }
    internal void SendMyPosition(int[] myPosition, Botao botaoAtual)
    {
        if (!IsLocalPlayer) return;
        if (botaoAtual != null || !playerManager.PossoJogar)
        {
            // If the current button is not the one being dragged, do not proceed
            StartCoroutine(ReturnInitial());
            return;
        }

        if (MinusArray(new byte[] { (byte)myPosition[0], (byte)myPosition[1] }, MyPosition, lado, IsDama))
        {
            SendMyPosition();
            return;
        }
        else
        {
            StartCoroutine(ReturnInitial());
            return;
        }

        void SendMyPosition()
        {
            using var buffer = Rent();
            buffer.Write(new byte[] { (byte)myPosition[0], (byte)myPosition[1] });
            Client.Rpc(ConstantsDama.SEND_POSITION, buffer);
        }
    }

    void MeDestroy()
    {
        Identity.Despawn();
    }
    bool MinusArray(byte[] newPos, byte[] oldPos, bool ePreto, bool ehDama = false)
    {
        int newPosR = newPos[0];
        int newPosC = newPos[1];

        int oldPosR = oldPos[0];
        int oldPosC = oldPos[1];

        int r = newPosR - oldPosR;
        int c = newPosC - oldPosC;

        if (ehDama)
        {
            if (IsServer)
            {
                // Verificar se o movimento é diagonal
                if (Mathf.Abs(r) != Mathf.Abs(c))
                    return false;

                int dirR = r > 0 ? 1 : -1;
                int dirC = c > 0 ? 1 : -1;

                int countR = newPosR;
                int countC = newPosC;

                List<Botao> bolachasCapturadas = new();
                while (countR != oldPosR && countC != oldPosC)
                {
                    if (group.TryGetBotao(new[] { countR, countC }, out var bolachaCapturada))
                    {
                        bolachasCapturadas.Add(bolachaCapturada);
                    }
                    else bolachasCapturadas.Add(null);

                    countR -= dirR;
                    countC -= dirC;
                }
                int seguidas = 0;
                foreach (var bolachaCapturada in bolachasCapturadas)
                {
                    if (bolachaCapturada != null)
                    {
                        seguidas++;
                        if (seguidas > 1 || bolachaCapturada.playerManager == playerManager)
                        {
                            return false;
                        }
                    }
                    else
                    {
                        seguidas = 0;
                    }
                }
                int comidas = 0;
                foreach (var bolachaCapturada in bolachasCapturadas)
                {
                    if (bolachaCapturada != null)
                    {
                        group.RemoveBotao(bolachaCapturada.MyPosition);
                        bolachaCapturada.MeDestroy();
                        playerManager.QtdGanhas++;
                        comidas++;
                    }
                }
                if (comidas > 0) podeComerMais = PossoComerMaisDama();

                return true;
            }
            else
            {
                return Mathf.Abs(r) == Mathf.Abs(c);
            }
        }
        int rNew = Mathf.Abs(r);
        int cNew = Mathf.Abs(c);

        if (rNew == 2 && cNew == 2)
        {
            if (IsServer)
            {

                int metadeR = oldPosR + r / 2;
                int metadeC = oldPosC + c / 2;
                if (group.TryGetBotao(new[] { newPosR, newPosC }, out var _))
                {
                    return false;
                }
                if (ePreto && r == 2 || !ePreto && r == -2)
                {
                    if (!group.TryGetBotao(new[] { metadeR, metadeC }, out var bolachaCapturada)) return false;
                    if (bolachaCapturada.playerManager == playerManager) return false;
                    group.RemoveBotao(bolachaCapturada.MyPosition);
                    bolachaCapturada.MeDestroy();
                    playerManager.QtdGanhas++;

                    podeComerMais = PossoComerMaisPecaNormal(newPosR, newPosC, ePreto);
                    if (podeComerMais)
                    {
                        Debug.Log($"Peça normal em [{newPosR},{newPosC}] pode capturar mais!");
                    }

                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return true;
            }

        }
        if (rNew == 1 && cNew == 1)
        {
            if (ePreto && r == 1 || !ePreto && r == -1) return true;

        }

        return false;


        bool PossoComerMaisDama()
        {
            int[][] direcoes = new int[][]
            {
                new int[] { -1, 1 },
                new int[] { -1, -1 },
                new int[] { 1, 1 },
                new int[] { 1, -1 }
            };

            foreach (var direcao in direcoes)
            {
                int dirR = direcao[0];
                int dirC = direcao[1];

                int countR = newPosR + dirR;
                int countC = newPosC + dirC;

                bool encontrouAdversario = false;

                while (countR >= 0 && countR < 8 && countC >= 0 && countC < 8)
                {
                    if (group.TryGetBotao(new[] { countR, countC }, out var bolachaNoPath))
                    {
                        if (encontrouAdversario)
                            break;

                        if (bolachaNoPath.playerManager != playerManager)
                        {
                            encontrouAdversario = true;


                            int nextR = countR + dirR;
                            int nextC = countC + dirC;

                            if (nextR < 0 || nextR >= 8 || nextC < 0 || nextC >= 8)
                            {
                                encontrouAdversario = false;
                                break;
                            }

                            if (group.TryGetBotao(new[] { nextR, nextC }, out var _))
                            {
                                encontrouAdversario = false;
                                break;
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                    else if (encontrouAdversario)
                    {
                        return true;
                    }

                    countR += dirR;
                    countC += dirC;
                }
            }

            return false;
        }

        bool PossoComerMaisPecaNormal(int posR, int posC, bool ePreto)
        {
            int[][] direcoes = ePreto
                ? new int[][] { new int[] { 1, 1 }, new int[] { 1, -1 } }
                : new int[][] { new int[] { -1, 1 }, new int[] { -1, -1 } };

            foreach (var direcao in direcoes)
            {
                int dirR = direcao[0];
                int dirC = direcao[1];

                int countR = posR + dirR;
                int countC = posC + dirC;

                if (countR < 0 || countR >= 8 || countC < 0 || countC >= 8)
                {
                    continue;
                }

                if (group.TryGetBotao(new[] { countR, countC }, out var bolachaNoPath))
                {
                    if (bolachaNoPath.playerManager != playerManager)
                    {
                        int nextR = countR + dirR;
                        int nextC = countC + dirC;

                        if (nextR < 0 || nextR >= 8 || nextC < 0 || nextC >= 8)
                        {
                            continue;
                        }

                        if (!group.TryGetBotao(new[] { nextR, nextC }, out var _))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }
    [Server(ConstantsDama.SEND_POSITION)]
    void RecievePositionServerRpc(DataBuffer buffer)
    {
        byte[] newPos = buffer.ReadArray<byte>();
        if (playerManager.PossoJogar && MinusArray(newPos, MyPosition, lado, IsDama))
        {
            if (lado)
            {
                if (newPos[0] == 7) IsDama = true;
            }
            else
            {
                if (newPos[0] == 0) IsDama = true;
            }

            group.RemoveBotao(MyPosition);
            group.AddBotao(newPos, this);
            MyPosition = newPos;
            group.Jogado(playerManager, podeComerMais);
            podeComerMais = false;

        }
        else
        {
            MyPosition = m_MyPosition;
        }
    }
}
