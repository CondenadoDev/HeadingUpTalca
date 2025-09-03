using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Resources;

public class PostGameUI : MonoBehaviour
{
    public UIScreen screen;
    public TMP_Text returningText;
    public TMP_Text winnerText;
    public void SetWinner(PlayerObject player)
    {
        winnerText.text = $"{player.Nickname} is the winner!";

        // Load the skin from the Resource Manager
        PlayerSkin skin = ResourcesManager.Instance.GetSkin(player.Skin);
        var renderGolf = InterfaceManager.Instance.sessionScreen.golfBallRend;
        if (skin != null)
        {
            // Apply the mesh and materials
            renderGolf.GetComponent<MeshFilter>().mesh = skin.Mesh;
            renderGolf.GetComponent<MeshRenderer>().material = skin.Material;
        }
        InterfaceManager.Instance.sessionScreen.golfBallRend.GetComponent<MeshRenderer>().material.color = player.Color;

        renderGolf.transform.localRotation = Quaternion.Euler(skin.Direction);

        if (skin.DisplayPosition != Vector3.zero) renderGolf.transform.localPosition = skin.DisplayPosition;
        else renderGolf.transform.localPosition = skin.DisplayPosition;

        if (skin.Name == "Golf") renderGolf.transform.localScale = skin.Scale;
        else if (skin.DisplayScale != Vector3.zero) renderGolf.transform.localScale = skin.DisplayScale * 0.5f;
        else renderGolf.transform.localScale = skin.Scale * 0.5f;
    }

    public void UpdateReturningText(int time)
    {
        returningText.text = $"Returning to Room in {time}...";
    }
}