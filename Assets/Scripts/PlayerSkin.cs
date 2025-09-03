using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class PlayerSkin
{
    public string Name;
    public int CoinCost;
    public GameObject ModelBones;
    public Mesh Mesh;
    public Material Material;
    public Sprite Icon;
    public Vector3 Position;
    public Vector3 DisplayPosition;
    public Vector3 Direction;
    public Vector3 Scale;
    public Vector3 DisplayScale;
}