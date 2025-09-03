using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class NicknameInputUI : MonoBehaviour
{
	public TMP_InputField nicknameField;

    private void OnEnable()
    {
        if (SaveDataManager.Instance.HasNickname)
        {
            nicknameField.text = SaveDataManager.Instance.PlayerNickname;
        }
    }
    public void SaveNickname(string value)
    {
        SaveDataManager.Instance.PlayerNickname = value;
    }
}
