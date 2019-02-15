using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Mathematics;
public class CameraController : MonoBehaviour {


    public Camera cam;
    public Vector3 center;
    public float zoomSpeed = 15f;
    public float arcSpeed = 8f;

    GUIStyle style;
    Rect rect;
    float deltaTime;
    public Text textComponent;


    private void Start()
    {
        RaycastHit hit;
        Physics.Raycast(transform.position, transform.forward, out hit);
        center = hit.point;

        style = new GUIStyle();
        int w = Screen.width, h = Screen.height;
        rect = new Rect(0, 0, w, h * 2 / 100);
        style.alignment = TextAnchor.UpperLeft;
        style.fontSize = h * 2 / 100;
        style.normal.textColor = new Color(0.0f, 0.0f, 0.5f, 1.0f);


    }
    // Update is called once per frame
    void Update () {

        deltaTime = Time.unscaledDeltaTime;

        float msec = deltaTime * 1000.0f;
        float fps = 1.0f / deltaTime;
        textComponent.text = string.Format("{0:0.0} ms ({1:0.} fps)", msec, fps);

        if (Application.isPlaying)
        { 

            if (Input.GetAxisRaw("Vertical") != 0f)
            {
                RaycastHit hit;
                Physics.Raycast(transform.position, transform.forward, out hit);
                center = hit.point;

                float dir = Input.GetAxisRaw("Vertical");
                float dist = math.length(center - transform.position);

                if (!cam.orthographic)
                    cam.transform.position += (transform.forward * dir * (dist * zoomSpeed));
            }
            else if(Input.GetAxisRaw("Horizontal") != 0f)
            {
                //transform.position = transform.position + ((arcSpeed * wheelInput) * transform.up);
                
                transform.RotateAround(center, transform.right, arcSpeed * Input.GetAxisRaw("Horizontal"));
                transform.LookAt(center, transform.up);
            }
            else if (Input.GetButton("Up1") || Input.GetButton("Up2"))
            {
                int dir = 1;
                if (Input.GetButton("Up2"))
                    dir = -1;

                if (!cam.orthographic)
                    cam.transform.position += (Vector3.forward * dir * zoomSpeed);
                else
                    cam.orthographicSize += dir * zoomSpeed;
            }
        }

    }
}
