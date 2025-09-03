using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MapInfoUI : MonoBehaviour
{
    public GameObject infoPanel; // Panel containing UI
    public TMP_Text modeNameText;
    public Image modeImage;
    public TMP_Text modeDescriptionText;
    public CanvasGroup panelCanvasGroup;
    public UIScreen screen;

    public void ShowGameModeInfo(string modeName, float duration)
    {
        GameModeInfo selectedMode = ResourcesManager.Instance.gameModes.Find(mode => mode.modeName == modeName);

        if (selectedMode != null)
        {
            Debug.Log($"Showing {modeName} info.");

            // Set UI elements
            modeNameText.text = selectedMode.modeName;
            modeImage.sprite = selectedMode.modeImage;
            modeDescriptionText.text = selectedMode.modeDescription;

            // Show UI
            infoPanel.SetActive(true);
            panelCanvasGroup.alpha = 1f;

            // Start fade-out sequence
            StartCoroutine(ShowAndHideInfo(duration));
        }
        else
        {
            Debug.LogWarning($"Game mode {modeName} not found!");
        }
    }

    private IEnumerator ShowAndHideInfo(float duration)
    {
        float fadeDuration = 0.25f; // Duration for fade in/out

        // Fade in
        panelCanvasGroup.alpha = 0f;
        infoPanel.SetActive(true);
        panelCanvasGroup.blocksRaycasts = true;

        float elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            panelCanvasGroup.alpha = elapsedTime / fadeDuration;
            yield return null;
        }
        panelCanvasGroup.alpha = 1f;

        // Wait for the remaining duration (excluding fade times)
        yield return new WaitForSeconds(duration - (2 * fadeDuration));

        // Fade out
        elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            panelCanvasGroup.alpha = 1f - (elapsedTime / fadeDuration);
            yield return null;
        }

        panelCanvasGroup.alpha = 0f;
        panelCanvasGroup.blocksRaycasts = false;
        infoPanel.SetActive(false);
    }
}
