using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShellCollision : MonoBehaviour
{
    public GameObject explosionParticlesPrefab;

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            return;
        }

        else if (explosionParticlesPrefab)
        {
            GameObject explosion = (GameObject) Instantiate(explosionParticlesPrefab, transform.position, explosionParticlesPrefab.transform.rotation);
            Destroy(explosion, explosion.GetComponent<ParticleSystem>().main.startLifetimeMultiplier);
            Destroy(gameObject);

            if (collision.gameObject.tag == "Enemy")
            {
                Destroy(collision.gameObject);
            }
        }
    }
}