using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PersistenData : MonoBehaviour
{
    
    public static PersistenData instance;
    public string Skin;
    void Awake()
    {
        if (instance == null)
        { 
            instance = this;
            DontDestroyOnLoad(this);
        }
        else
        {
            Destroy(gameObject); 
        }
    }

    public void SetSkin(string newSkin)
    {
        Skin = newSkin;
    }
    
}
