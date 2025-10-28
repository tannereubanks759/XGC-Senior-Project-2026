using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ImageSelector : MonoBehaviour
{
    [SerializeField]
    Sprite[] sprites;
    [SerializeField]
    int currentSprite;

    [SerializeField]
    Image image;

    void Start()
    {
        if (image == null)
        {
            image = GetComponent<Image>();
        }
        UpdateImage();
    }

    void UpdateImage()
    {
        image.sprite = sprites[currentSprite];
    }

    public void Next()
    {
        currentSprite += 1;
        currentSprite= currentSprite < sprites.Length? currentSprite : 0;
        UpdateImage();
    }
    
    public void Previous()
    {
        currentSprite -= 1;
        currentSprite = currentSprite < 0 ? sprites.Length-1 : currentSprite;
        UpdateImage();
    }
}
