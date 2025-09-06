using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUD : MonoBehaviour
{
	[SerializeField] TMP_Text timeText;
	[SerializeField] TMP_Text levelNameText, introLevelNameText;
	[SerializeField] Animator introAnimator;
	[SerializeField] TMP_Text strokesText;
	[SerializeField] TMP_Text levelText;
	[Space]
	[SerializeField] Image puttChargeDot;
	[SerializeField] Image puttCharge;
	[SerializeField] Image puttCooldown;
	[SerializeField] CanvasGroup puttCooldownGroup;
	[field: SerializeField] public Gradient PuttChargeColor { get; private set; }
	[field: SerializeField, Space] public GameObject SpectatingObj { get; private set; }

	[Space]
	[Header("Radial Emoji Menu")]
	[SerializeField] private GameObject radialEmojiMenu;

	static IEnumerator puttChargeRoutine = null;
	static IEnumerator puttCooldownRoutine = null;

	// OPTIMIZATION: Cache para string formatting
	private static readonly Dictionary<int, string> minuteStringCache = new Dictionary<int, string>();
	private static readonly Dictionary<int, string> strokeStringCache = new Dictionary<int, string>();
	private static readonly Dictionary<int, string> livesStringCache = new Dictionary<int, string>();

	public static HUD Instance { get; private set; }
	private void Awake()
	{
		Instance = this;
		// Pre-populate caches con valores comunes
		InitializeStringCaches();
	}

	private void InitializeStringCaches()
	{
		// Pre-cache valores comunes para evitar string allocations
		for (int i = 0; i <= 30; i++)
		{
			strokeStringCache[i] = $"Strokes: {i}";
		}

		for (int i = 0; i <= 10; i++)
		{
			livesStringCache[i] = $"Lives: {i}";
		}
	}

	private void OnEnable()
	{
		if (puttChargeRoutine != null)
			StartCoroutine(puttChargeRoutine);

		if (puttCooldownRoutine != null)
			StartCoroutine(puttCooldownRoutine);
	}

	public static void SetLives(int lives)
	{
		// OPTIMIZATION: Use cached string si existe
		if (livesStringCache.TryGetValue(lives, out string cachedString))
		{
			Instance.levelText.text = cachedString;
		}
		else
		{
			Instance.levelText.text = $"Lives: {lives}";
		}

		if (lives <= 1) Instance.levelText.color = Color.red;
        else if (lives == 2) Instance.levelText.color = Color.yellow;
        else if (lives >= 3) Instance.levelText.color = Color.white;
    }
	
	public static void SetLevelName(int holeIndex)
	{
		Instance.introLevelNameText.text = Instance.levelNameText.text = $"Hole {holeIndex + 1}";
		Instance.introAnimator.SetTrigger("Intro");
	}

    public static void SetTimerText(float time)
    {
        var maxTime = GameManager.MaxTime;
        if (GameManager.ModifiedMaxTime > 0) maxTime = GameManager.ModifiedMaxTime;
        
        float remainingTime = Mathf.Max(0, maxTime - time);

		// OPTIMIZATION: Minimize string allocations
        int minutes = (int)(remainingTime / 60f);
        float seconds = remainingTime % 60;
        
        Instance.timeText.text = $"{minutes:00}:{seconds:00.00}";
    }

    public static void SetStrokeCount(int count)
	{
		// OPTIMIZATION: Use cached string si existe
		if (strokeStringCache.TryGetValue(count, out string cachedString))
		{
			Instance.strokesText.text = cachedString;
		}
		else
		{
			Instance.strokesText.text = $"Strokes: {count}";
		}
	}

	public static void SetPuttCharge(float fill, bool canPutt)
	{
		Instance.puttCharge.fillAmount = fill;
		Instance.puttCharge.color = canPutt ? Instance.PuttChargeColor.Evaluate(fill) : Color.gray;
	}

	public static void SetPuttCooldown(float fill)
	{
		Instance.puttCooldown.fillAmount = fill;
	}

	public static void ShowPuttCharge()
	{
		if (puttChargeRoutine != null)
		{
			Instance.StopCoroutine(puttChargeRoutine);
		}
		puttChargeRoutine = Instance.ShowPuttChargeRoutine();

		if (Instance.gameObject.activeInHierarchy)
		{
			Instance.StartCoroutine(puttChargeRoutine);
		}
	}

	IEnumerator ShowPuttChargeRoutine()
	{
		puttChargeDot.color = new Color(1, 1, 1, 0);
		puttCharge.rectTransform.localScale = Vector3.zero;

		float t = 0;
		while (t < 1)
		{
			puttCharge.rectTransform.localScale = Vector3.Lerp(puttCharge.rectTransform.localScale, Vector3.one, t);
			puttChargeDot.color = Color.Lerp(puttChargeDot.color, Color.white, t);
			t += Time.deltaTime;
			yield return null; // OPTIMIZATION: Use null instead of WaitForEndOfFrame
		}
		puttCharge.rectTransform.localScale = Vector3.one;
		puttChargeDot.color = Color.white;
		puttChargeRoutine = null;
	}

	public static void HidePuttCharge()
	{
		if (puttChargeRoutine != null)
		{
			Instance.StopCoroutine(puttChargeRoutine);
		}
		puttChargeRoutine = Instance.HidePuttChargeRoutine();

		if (Instance.gameObject.activeInHierarchy)
		{
			Instance.StartCoroutine(puttChargeRoutine);
		}
	}

	IEnumerator HidePuttChargeRoutine()
	{
		puttChargeDot.color = Color.white;
		puttCharge.rectTransform.localScale = Vector3.one;

		Color clear = new Color(1, 1, 1, 0);
		float t = 0;
		while (t < 1)
		{
			puttCharge.fillAmount = Mathf.Lerp(puttCharge.fillAmount, 0, t);
			
			puttChargeDot.color = Color.Lerp(puttChargeDot.color, clear, t);
			t += Time.deltaTime;
			yield return null; // OPTIMIZATION: Use null instead of WaitForEndOfFrame
		}
		puttCharge.fillAmount = 0;
		puttChargeRoutine = null;
	}

	public static void ShowPuttCooldown()
	{
		if (puttCooldownRoutine != null)
		{
			Instance.StopCoroutine(puttCooldownRoutine);
		}
		puttCooldownRoutine = Instance.ShowPuttCooldownRoutine();

		if (Instance.gameObject.activeInHierarchy)
		{
			Instance.StartCoroutine(puttCooldownRoutine);
		}
	}

	IEnumerator ShowPuttCooldownRoutine()
	{
		puttCooldownGroup.alpha = 0;
		puttCooldownGroup.transform.localScale = new Vector3(0.5f, 1, 1);
		float t = 0;
		while (t < 1)
		{
			puttCooldownGroup.alpha = Mathf.Lerp(puttCooldownGroup.alpha, 1, t);
			puttCooldownGroup.transform.localScale = Vector3.Lerp(puttCooldownGroup.transform.localScale, Vector3.one, t);
			t += Time.deltaTime;
			yield return null; // OPTIMIZATION: Use null instead of WaitForEndOfFrame
		}
		puttCooldownGroup.alpha = 1;
		puttCooldownGroup.transform.localScale = Vector3.one;
		puttCooldownRoutine = null;
	}

	public static void HidePuttCooldown()
	{
		if (puttCooldownRoutine != null)
		{
			Instance.StopCoroutine(puttCooldownRoutine);
		}
		puttCooldownRoutine = Instance.HidePuttCooldownRoutine();

		if (Instance.gameObject.activeInHierarchy)
		{
			Instance.StartCoroutine(puttCooldownRoutine);
		}
	}

	IEnumerator HidePuttCooldownRoutine()
	{
		puttCooldownGroup.alpha = 1;
		puttCooldownGroup.transform.localScale = Vector3.one;
		float t = 0;
		while (t < 1)
		{
			puttCooldownGroup.alpha = Mathf.Lerp(puttCooldownGroup.alpha, 0, t);
			t += Time.deltaTime;
			yield return null; // OPTIMIZATION: Use null instead of WaitForEndOfFrame
		}
		puttCooldownGroup.alpha = 0;
		puttCooldownRoutine = null;
	}

	public static void ForceHideAll()
	{
		puttChargeRoutine = null;
		puttCooldownRoutine = null;
		Instance.StopAllCoroutines();
		Instance.puttCharge.fillAmount = 0;
		Instance.puttCharge.rectTransform.localScale = Vector3.one;
		Instance.puttCooldownGroup.alpha = 0;
		Instance.puttCooldownGroup.transform.localScale = Vector3.one;
	}

	public static void ToggleEmojiRadialMenu()
	{
		bool active = Instance.radialEmojiMenu.activeSelf;
		Cursor.lockState = active ? CursorLockMode.Locked : CursorLockMode.None;
		Instance.radialEmojiMenu.SetActive(!active);
	}
}