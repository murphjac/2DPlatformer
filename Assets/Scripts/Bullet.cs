using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour {

    public float velocity = 10;
    public float bulletLifetime = 1.0f;

    void Start () {
        Destroy(gameObject, bulletLifetime);
    }

    void Update () {
        transform.Translate(Vector2.down * velocity);
    }
}
