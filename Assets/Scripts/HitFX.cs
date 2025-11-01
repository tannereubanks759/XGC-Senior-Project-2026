using UnityEngine;

public class HitFX : MonoBehaviour
{
    public AudioSource source;
    public AudioClip[] hitSkeleton;
    public GameObject boneChips;
    

    public void PlaySound(AudioClip clip)
    {
        source.PlayOneShot(clip);
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.tag == "Enemy" || other.tag == "Boss")
        {
            Destroy(Instantiate(boneChips, other.ClosestPoint(this.transform.position), Quaternion.identity), 3);
            PlaySound(hitSkeleton[Random.Range(0,hitSkeleton.Length)]);
        }
    }
}
