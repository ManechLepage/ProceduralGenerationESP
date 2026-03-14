using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraManager : MonoBehaviour
{
    /*
    Gérer le déplacement et la rotation de la caméra dans la scène.
    Contrôles :
     - WASD pour se déplacer horizontalement (par rapport à la direction de la caméra)
     - Espace et Shift pour monter et descendre
     - Scroll pour augmenter/diminuer la vitesse de déplacement
     - Click droit pour effectuer une rotation
     - Click du milieu pour effectuer une translation (désactivé pour le moment)
    */

    public float moveSpeed = 10f;
    public float rotateSpeed = 10f;
    public float panSpeed = 10f;

    [Space]
    public float speedModifier = 2f;

    [Space]
    public int initialFoV = 60;

    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
        cam.fieldOfView = initialFoV;
    }

    void Update()
    {
        if (GameManager.Instance.openedUI)
            return;

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        // the scroll changes the movespeed (* or / by speedModifier)
        if (scroll != 0f)
        {
            moveSpeed += speedModifier * scroll * 100f;
            moveSpeed = Mathf.Max(10f, moveSpeed);
        }

        float localMoveSpeed = moveSpeed;
        if (Input.GetKey(KeyCode.LeftControl))
        {
            localMoveSpeed *= 2f;
        }

        float up = 0f;

        if (Input.GetKey(KeyCode.Space))
            up = 1f;
        else if (Input.GetKey(KeyCode.LeftShift))
            up = -1f;

        Vector3 move = new Vector3(h, up, v) * localMoveSpeed * Time.deltaTime;
        transform.Translate(move, Space.Self);

        // Right click: rotate camera
        if (Input.GetMouseButton(1))
        {
            float mouseX = Input.GetAxis("Mouse X") * rotateSpeed;
            float mouseY = -Input.GetAxis("Mouse Y") * rotateSpeed;

            transform.Rotate(Vector3.up, mouseX, Space.World);
            transform.Rotate(Vector3.right, mouseY, Space.Self);
        }

        // Middle click: pan camera
        if (Input.GetMouseButton(2))
        {
            float mouseX = -Input.GetAxis("Mouse X") * panSpeed * Time.deltaTime;
            float mouseY = -Input.GetAxis("Mouse Y") * panSpeed * Time.deltaTime;

            transform.Translate(new Vector3(mouseX, mouseY, 0), Space.Self);
        }
    }
}
