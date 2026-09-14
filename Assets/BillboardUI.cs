using UnityEngine;

public class BillboardUI : MonoBehaviour
{
    private Camera mainCam;

    private void LateUpdate()
    {
        if (mainCam == null) mainCam = Camera.main;
        if (mainCam != null)
        {
            transform.LookAt(transform.position + mainCam.transform.rotation * Vector3.forward, mainCam.transform.rotation * Vector3.up);
        }
    }
}