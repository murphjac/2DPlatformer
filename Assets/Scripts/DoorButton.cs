using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class DoorButton : MonoBehaviour, UsableObject {
    const int OPEN = 0, OPENING = 1, CLOSING = 2, CLOSED = 3;

    public Transform door;
    public bool doorOpen = false;
    public float doorSpeed;
    public Vector3 doorOpenPointLocal = new Vector3(0, 1, 0);
    public Vector3 doorClosedPointLocal = new Vector3(0, 0, 0);

    private int status;
    private float percentOpen;
    private float doorOpeningDistance;
    private Vector3 doorOpenPointGlobal;
    private Vector3 doorClosedPointGlobal;
    private Renderer rend;

    void Start () {
        rend = GetComponent<Renderer>();
        doorOpenPointGlobal = doorOpenPointLocal + door.position;
        doorClosedPointGlobal = doorClosedPointLocal + door.position;
        doorOpeningDistance = Vector3.Distance(doorOpenPointGlobal, doorClosedPointGlobal);

        if (doorOpen)
        {
            status = OPEN;
            percentOpen = 1.0f;
            rend.sharedMaterial = Resources.Load("Materials/Player_Wind") as Material;
            door.Translate(doorOpenPointLocal);
        }
        else
        {
            status = CLOSED;
            percentOpen = 0.0f;
            rend.sharedMaterial = Resources.Load("Materials/Player_Fire") as Material;
            door.Translate(doorClosedPointLocal);
        }
    }

    void Update()
    {
        if(status == OPENING || status == CLOSING)
        {
            float displacement = Time.deltaTime * doorSpeed / doorOpeningDistance;

            if (status == OPENING)
            {
                percentOpen = Mathf.Clamp01(percentOpen + displacement);
                if(percentOpen >= 1.0f) { status = OPEN; }
            }
            else if(status == CLOSING)
            {
                percentOpen = Mathf.Clamp01(percentOpen - displacement);
                if(percentOpen <= 0.0f) { status = CLOSED; }
            }

            Vector3 newPos = Vector3.Lerp(doorClosedPointGlobal, doorOpenPointGlobal, percentOpen);
            door.Translate(newPos - door.position);
        }
    }

    public void Use()
    {
        if (status == OPEN || status == OPENING)
        {
            rend.sharedMaterial = Resources.Load("Materials/Player_Fire") as Material;
            status = CLOSING;
        }
        else if (status == CLOSING || status == CLOSED)
        {
            rend.sharedMaterial = Resources.Load("Materials/Player_Wind") as Material;
            status = OPENING;
        }
    }

    void OnDrawGizmos()
    {
        float size = 0.3f;
        Vector3 drawPos = (Application.isPlaying) ? doorClosedPointGlobal : doorClosedPointLocal + door.position;

        Gizmos.color = Color.red;
        Gizmos.DrawLine(drawPos - Vector3.up * size, drawPos + Vector3.up * size);
        Gizmos.DrawLine(drawPos - Vector3.left * size, drawPos + Vector3.left * size);

        drawPos = (Application.isPlaying) ? doorOpenPointGlobal : doorOpenPointLocal + door.position;

        Gizmos.color = Color.green;
        Gizmos.DrawLine(drawPos - Vector3.up * size, drawPos + Vector3.up * size);
        Gizmos.DrawLine(drawPos - Vector3.left * size, drawPos + Vector3.left * size);
    }
}
