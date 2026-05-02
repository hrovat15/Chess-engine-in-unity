using UnityEngine;

public class ui_scriipt : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] GameObject cam;

    public int angle = 180;

    public void Rotate()
    {
        Vector3 targetRotation = new Vector3(0, 0, angle);

        cam.transform.rotation = Quaternion.Euler(targetRotation);
    }
}
