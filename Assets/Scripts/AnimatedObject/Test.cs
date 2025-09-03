using UnityEngine;

public class Test : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.X))
        {
            HUD.ToggleEmojiRadialMenu();
        }
        if (Input.GetKeyDown(KeyCode.K))
        {
            Cursor.SetCursor(null, new Vector2(Screen.width / 2, Screen.height / 2), CursorMode.Auto);
        }
    }
}