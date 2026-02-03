using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraManager : MonoBehaviour
{
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
        if (Input.GetKey(KeyCode.LeftShift))
        {
            localMoveSpeed *= 2f;
        }

        Vector3 move = new Vector3(h, 0, v) * localMoveSpeed * Time.deltaTime;
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
