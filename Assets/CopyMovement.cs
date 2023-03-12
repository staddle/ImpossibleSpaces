using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CopyMovement : MonoBehaviour
{
    public Transform toCopy;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (toCopy != null)
        {
            transform.position = toCopy.position;
            transform.rotation = toCopy.rotation;
        }
    }
}
