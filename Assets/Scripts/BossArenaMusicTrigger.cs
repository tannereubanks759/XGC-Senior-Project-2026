using UnityEngine;

public class BossArenaMusicTrigger : MonoBehaviour
{
    [Header("Player Detection")]
    public string playerTag = "Player";

    [Header("Music")]
    public bool instantTransition = false;

    [Header("Boss State")]
    public bool bossFightStarted = false;
    public bool bossDefeated = false;

    private void OnTriggerEnter(Collider other)
    {
        if (bossDefeated)
            return;

        if (bossFightStarted)
            return;

        if (!other.CompareTag(playerTag))
            return;

        bossFightStarted = true;

        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.EnterBossMusic(instantTransition);
        }
    }

    public void OnBossDied()
    {
        if (bossDefeated)
            return;

        bossDefeated = true;

        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.ExitBossMusic(false);
        }
    }
}