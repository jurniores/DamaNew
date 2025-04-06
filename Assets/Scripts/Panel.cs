using System.Collections;
using System.Collections.Generic;
using Omni.Core;
using UnityEngine;

public class Panel : MonoBehaviour
{
    public int column = 0;
    void Start()
    {
        var tabuleiro = NetworkService.Get<Tabuleiro>();
        var campos = GetComponentsInChildren<Campo>();

        for (int i = 0; i < campos.Length; i++)
        {
            campos[i].gameObject.name = $"{column}-{i}";
            campos[i].SetPosition(column, i);
            tabuleiro.campos.TryAdd((column, i), campos[i]);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
