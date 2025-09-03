using System;
using UnityEngine;

public class ColorSelection : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject characterRender;

    private Color selectedColor;

    enum PlayerColor
    {
        white, red, blue,
        yellow, orange, green,
        purple
    }

    public void SetSelectedColor(int index)
    {
        if (!Enum.IsDefined(typeof(PlayerColor), index))
        {
            Debug.LogWarning("Índice fuera de rango para PlayerColor.");
            return;
        }

        PlayerColor playerColor = (PlayerColor)index;
        selectedColor = GetColor(playerColor);
        SetPlayerColor();
    }

    private Color GetColor(PlayerColor color)
    {
        switch (color)
        {
            case PlayerColor.white: return Color.white;
            case PlayerColor.red: return Color.red;
            case PlayerColor.blue: return Color.blue;
            case PlayerColor.yellow: return Color.yellow;
            case PlayerColor.orange: return Color.Lerp(Color.red, Color.yellow, 0.5f);
            case PlayerColor.green: return Color.green;
            case PlayerColor.purple: return Color.Lerp(Color.red, Color.blue, 0.5f);
            default: return Color.white;
        }
    }

    private void SetPlayerColor()
    {
        PlayerObject.Local.Rpc_SetColor(selectedColor);
        characterRender.GetComponent<MeshRenderer>().material.color = selectedColor;
    }

    public void ToggleCheckMark()
    {
        bool checkMarkActive = PlayerObject.Local.checkMark;
        PlayerObject.Local.Rpc_SetCheckMark(!checkMarkActive);
    }
}
