using UnityEngine;

public class SkeletonGoldBoneScript : MonoBehaviour
{
    private Renderer rend;
    private Material mat;
    public int goldBoneCount = 0;
    public Texture2D[] masks;
    public Color[] colors;
    public bool isElite = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rend = GetComponent<Renderer>();
        mat = rend.material;

        goldBoneCount = Random.Range(0, 4);
        Debug.Log(goldBoneCount);
        for(int i = 0; i < goldBoneCount+1; i++)
        {
            if(i != 0)
            {
                mat.SetTexture("mask" + i.ToString(), masks[Random.Range(0, masks.Length - 1)]);
            }
        }

        GetComponentInParent<BaseEnemyAI>().GoldInit(goldBoneCount);

        if (isElite)
        {
            UpdateColor();
        }
    }

    void UpdateColor()
    {
        int rand = Random.Range(0, colors.Length);
        mat.SetColor("emmisionColor", colors[rand]);
    }
}
