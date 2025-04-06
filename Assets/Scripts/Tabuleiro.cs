using System;
using System.Collections;
using System.Collections.Generic;
using Omni.Core;
using UnityEngine;

public class Tabuleiro : ServiceBehaviour
{
    private Botao botao;
    public Dictionary<(int, int), Campo> campos = new();
    [SerializeField]
    private bool otherLado = false;

    protected override void OnAwake()
    {
        var panels = GetComponentsInChildren<Panel>();
        if (otherLado)
        {
            for (int i = panels.Length - 1; i >= 0; i--)
            {
                panels[panels.Length - i - 1].column = i;
            }
        }
        else
        {
            for (int i = 0; i < panels.Length; i++)
            {
                panels[i].column = i;
            }
        }

    }

    public void SetInCampo(byte[] pos, Botao botao)
    {
        this.botao = botao;
        if(campos.ContainsKey((pos[0], pos[1])))
        {
            campos[(pos[0], pos[1])].SetInCampo(botao);
        }
       
    }
}
