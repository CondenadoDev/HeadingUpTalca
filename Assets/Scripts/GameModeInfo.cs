using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Level;

[System.Serializable]
public class GameModeInfo
{
    public string modeName;
    public Sprite modeImage;
    [TextArea] public string modeDescription;
}