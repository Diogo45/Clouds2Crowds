using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IKController : MonoBehaviour {

    public GameObject ikPrefab; 


	// Use this for initialization
	void Start () {
        GameObject.Instantiate(ikPrefab, Vector3.zero, Quaternion.identity);
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
