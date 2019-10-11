using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AlignWithCamera : MonoBehaviour
{
	Transform camTransform;
    // Start is called before the first frame update
    void Start()
    {
		camTransform = Camera.main.transform;
    }

    // Update is called once per frame
    void Update()
    {
		Vector3 fwd = camTransform.position - transform.position;
		Vector3 upwd = camTransform.up;
		Quaternion rotation = Quaternion.LookRotation(fwd, upwd);
		Vector3 euler = rotation.eulerAngles;
		euler.x += 90;
		rotation = Quaternion.Euler(euler);
		transform.rotation = rotation;
    }
}
