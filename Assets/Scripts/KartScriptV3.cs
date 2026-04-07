using UnityEngine;

void CheckIfOffTrack()
{
    Debug.Log("je suis appeler");
    if (respawnCooldown > 0)
    {
        respawnCooldown -= Time.fixedDeltaTime;
        return;
    }

    if (Physics.Raycast(groundRayOrigin.position, Vector3.down, 5f))
    {
        Debug.Log("Je touche quelque chose");
    }
    else
    {
        Debug.Log("Je touche RIEN");
    }
    if (!Physics.Raycast(groundRayOrigin.position, Vector3.down, 5f, trackLayer))
    {
        Debug.Log("cacacacacacaca");
        checkPointManager.Respawn();
        transform.position = startPosition;
        respawnCooldown = 1.5f;
    }
}

void GhostDrive()
{
    if (currentWaypoint == null)
    {
        currentWaypoint = firstWaypoint;
        return;
    }

    Vector3 dir = currentWaypoint.position - transform.position;

    float angle = Vector3.SignedAngle(transform.forward, dir, Vector3.up);

    turnDirection = Mathf.Clamp(angle / 30f, -1f, 1f);
    forwardDirection = 1f;

    if (dir.magnitude < 4f)
    {
        currentWaypoint = currentWaypoint.GetComponent<Waypoints>().nextWaypoint;
    }
}