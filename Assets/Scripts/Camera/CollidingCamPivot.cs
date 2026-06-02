using UnityEngine;

public class CollidingCamPivot : MonoBehaviour
{
    Transform parentPivot;
    Transform cam;
    private void FixedUpdate()
    {
        Vector3 camDir = cam.position - parentPivot.position;
        if (Physics.Raycast(parentPivot.position, camDir.normalized, out RaycastHit hit, camDir.magnitude))
        {

           //  groundNormal = hit.normal;
           // transform.position = new Vector3(transform.position.x, hit.point.y + 0.54f, transform.position.z);
           //Debug.Log("grounded" + (0.5f - hit.distance));      
        }

    }
    /*private void OnCollisionStay(Collision collision)
    {
        transform.localEulerAngles += new Vector3(0, 0, 1f) * Time.fixedDeltaTime;
    }*/
}
