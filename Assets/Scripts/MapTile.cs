using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapTile : MonoBehaviour {
    public SpriteRenderer[] spriteRenderers;
    public SpriteRenderer spriteRenderer;
    public Sprite defaultSprite;
    public void SetSprite(int index, Sprite value){
        spriteRenderers[index].sprite = value;
    }

	public void Reset()
	{
        for (int i = 0; i < 4; i++){
            spriteRenderers[i].sprite = defaultSprite;
        }
	}

	public void SetSprite(Sprite value){
        spriteRenderer.sprite = value;
    }

    public void SetColor(Color value)
    {
        for (int i = 0; i < 4; i++){
            spriteRenderers[i].color = value;
        }
    }
}
