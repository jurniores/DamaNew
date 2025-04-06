using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.EventSystems;

public class Campo : MonoBehaviour, IDropHandler
{
    public int[] myPosition = new int[2];
    [SerializeField]
    private int y, x;
    public Botao botaoAtual;
    public void SetPosition(int a, int b)
    {
        myPosition = new int[] { a, b };
        y = a;
        x = b;
    }
    public void OnDrop(PointerEventData eventData)
    {
        Botao botao = eventData.pointerDrag.GetComponent<Botao>();
        botao.SendMyPosition(myPosition, botaoAtual);
    }

    public void SetInCampo(Botao botao)
    {
        botao.mark?.Invoke();
        botao.mark = Discart;
        botaoAtual = botao;
        
        botao.transform.SetParent(transform, false);
        botao.transform.position = transform.position;
        botao.StateInitial();
    }

    void Discart()
    {
        botaoAtual = null;
    }
}
