using UnityEngine;

public class PlayerSwordScript : MonoBehaviour
{
    public AudioSource source;
    public AudioClip[] hitSkeleton;
    public GameObject boneChips;
    public GameObject ClashEffect;

    public Collider col;
    public int damage = 10;
    public void PlaySound(AudioClip clip)
    {
        source.PlayOneShot(clip);
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.tag == "Enemy" || other.tag == "Boss")
        {
            SkeletonSwordEnemy skeleton = other.GetComponent<SkeletonSwordEnemy>();
            if(skeleton != null && ClashEffect && skeleton.isBlocking == true)
            {
                Collider enemySword = skeleton.GetComponentInChildren<SkeletonAnimEvents>().swordCol;
                Destroy(Instantiate(ClashEffect, enemySword.ClosestPoint(this.transform.position), Quaternion.identity), 3);
                col.enabled = false;
                other.GetComponent<DamageRef>().TakeDamage(damage);
            }
            else //Didnt hit basic skeleton
            {
                Destroy(Instantiate(boneChips, other.ClosestPoint(this.transform.position), Quaternion.identity), 3);
                PlaySound(hitSkeleton[Random.Range(0, hitSkeleton.Length)]);

                col.enabled = false;
                other.GetComponent<DamageRef>().TakeDamage(damage);
            }


            
        }
    }
}
