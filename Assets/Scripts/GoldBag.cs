using UnityEngine;

public class GoldBag : MonoBehaviour
{
    [Header("Gold Settings")]
    [Tooltip("Specifies the amount of gold this bag will give the player")]
    public int AmountOfGold = 50;

    private void Start()
    {
        this.transform.rotation = Quaternion.Euler(new Vector3(0, Random.Range(0, 360), 0));
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            GoldBank bank = other.GetComponent<GoldBank>();
            bank.AddGold(AmountOfGold);
            AudioSource source = GetComponent<AudioSource>();
            if (source)
            {
                source.Play();
            }
            this.GetComponent<Collider>().enabled = false;
            this.GetComponentInChildren<MeshRenderer>().enabled = false;

            ParticleSystem particle = GetComponentInChildren<ParticleSystem>();
            if (particle)
            {
                particle.Play();
            }

            Destroy(this.gameObject, 3f);
        }
    }
}
