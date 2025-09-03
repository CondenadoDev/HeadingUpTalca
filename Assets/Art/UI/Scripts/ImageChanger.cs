using UnityEngine;
using UnityEngine.UI;

public class ImageChanger : MonoBehaviour
{
    public Image displayImage;  // Imagen principal que cambia
    public Image lobbyImage;    // Imagen en el lobby que se actualiza

    public Button[] personajesButtons; 
    public Button[] skinsButtons;
    public Button[] accesoriosButtons;

    public Sprite[] personajesSprites;
    public Sprite[] skinsSprites;
    public Sprite[] accesoriosSprites;

    private Sprite currentSprite; // Guarda la imagen seleccionada

    void Start()
    {
        // Asignar eventos a los botones de personajes
        AssignButtonListeners(personajesButtons, personajesSprites);

        // Asignar eventos a los botones de skins
        AssignButtonListeners(skinsButtons, skinsSprites);

        // Asignar eventos a los botones de accesorios
        AssignButtonListeners(accesoriosButtons, accesoriosSprites);
    }

    void AssignButtonListeners(Button[] buttons, Sprite[] sprites)
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            int index = i; 
            buttons[i].onClick.AddListener(() => ChangeImage(sprites[index]));
        }
    }
    void ChangeImage(Sprite newSprite)
    {
        currentSprite = newSprite; 
        displayImage.sprite = currentSprite;
        lobbyImage.sprite = currentSprite;
    }
}
