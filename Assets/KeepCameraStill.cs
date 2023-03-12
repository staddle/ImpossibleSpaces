using UnityEngine;

[RequireComponent(typeof(Camera))]
public class KeepCameraStill : MonoBehaviour
{
    public Transform xRRig;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if(xRRig != null)
        {
            if(transform.localPosition.magnitude > 0)
            {
                xRRig.Translate(transform.localPosition);
                transform.localPosition = Vector3.zero;
            }
            if(transform.localRotation.eulerAngles.magnitude > 0)
            {
                xRRig.Rotate(transform.localRotation.eulerAngles);
                transform.localRotation = Quaternion.identity;
            }
        }
    }
}
