using UnityEngine;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;

public class enemytestLightingArtifact : MonoBehaviour
{
    public float health = 100f;
    public GameObject lightingBoltPrefab;   

    void Update()
    {
        if (health <= 0) gameObject.SetActive(false);
    }

    public void TakeDamage(float damage)
    {
        health -= damage;
    }

    public void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("PlayerSword"))
        {
            var sd = other.transform.root.GetComponent<swordDamageDeterminer>();
            int damage = sd.damage;
            if (sd.isLighting)
            {
                TakeDamage(damage);
                float radius = 10f;
                float damageMultiplier = 0.5f;
                Transform lastDamaged = this.transform;
                Collider[] closeEnemies = Physics.OverlapSphere(transform.position, radius, ~0, QueryTriggerInteraction.Ignore);
                foreach (Collider col in closeEnemies)
                {
                    if (col.CompareTag("Enemy") && col.transform != this.transform)
                    {
                        var enemyTestScript = col.GetComponent<enemytestLightingArtifact>();
                        Debug.Log("Lighting damage transferred");
                        enemyTestScript.TakeDamage(Mathf.RoundToInt(damage * damageMultiplier));
                        SpawnLightningArc(lastDamaged, enemyTestScript.transform);
                        lastDamaged = enemyTestScript.transform;
                    }
                    
                }
            }
            else
            {
                TakeDamage(damage);
            }
        }
    }

    private void SpawnLightningArc(Transform start, Transform end)
    {
        var lightning = Instantiate(lightingBoltPrefab);
        MonoBehaviour bolt = null;
        foreach (var scriptType in lightning.GetComponents<MonoBehaviour>())
        {
            if (scriptType.GetType().Name == "LightningBoltPrefabScript")
            {
                bolt = scriptType;
                break;
            }
        }
        var t = bolt.GetType();
        var fSource = t.GetField("Source"); if (fSource != null) fSource.SetValue(bolt, start.gameObject);
        var fDest = t.GetField("Destination"); if (fDest != null) fDest.SetValue(bolt, end.gameObject);
    }
}
