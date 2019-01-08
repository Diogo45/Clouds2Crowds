

using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class VisualAgent : MonoBehaviour {

    
    private Animator anim;
    private Queue<Vector3> moveMem;
    private Vector3 currPosition;
    private bool updated;
    private bool initialized;

    void Start()
    {
        if (!initialized) {
            Initialize(0);
        }
    }
	// Update is called once per frame
	void Update () {
        /*
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        
        transform.Translate(speed * Time.deltaTime, Space.World);

        
        */
        if (!updated)
        {
            Destroy(this.gameObject);
        }

        

        Vector3 currMoveVect = currPosition - transform.position;
        moveMem.Dequeue();
        moveMem.Enqueue(currMoveVect);

        float speedSum = 0;
        float angleDifSum = 0;
        Vector3 prevV = moveMem.Peek();
        foreach(Vector3 v in moveMem.ToArray()){
            speedSum += v.magnitude;
            angleDifSum += Vector3.SignedAngle(prevV, v,Vector3.right);
            prevV = v;
        }
        float presentAvgSpeed = (speedSum  / moveMem.Count) ;
        float estFutureSpeed = currMoveVect.magnitude;
        float AvgSpeed = (presentAvgSpeed + estFutureSpeed) / 2;

        float presentAvgAngleDif = angleDifSum / moveMem.Count;
        float estFutureAngDif = Vector3.SignedAngle(prevV, currMoveVect, Vector3.right);
        float avgAngleDif = (presentAvgAngleDif + estFutureAngDif) / 2;
        //transform.LookAt(point.transform);
        float totalAngleDiff = Vector3.SignedAngle(transform.forward, currMoveVect, Vector3.right);

        float angFact = totalAngleDiff / 90f;
        anim.SetFloat("AngSpeed", angFact * 0.5f);// Mathf.Clamp(angDif/6f,-1f,1f));


        transform.Rotate(new Vector3(0,0, totalAngleDiff * 0.05f), Space.World);
        //transform.rotation = Quaternion.Euler(0, Mathf.Atan2(speed.x,speed.z)*180f,0);
        anim.SetFloat("Speed", presentAvgSpeed);
        //anim.SetFloat("AngSpeed", presentAvgAngleDif/3f);

        transform.position = currPosition;
        updated = false;

    }

    public void SetCurrPosition(float3 pos)
    {
        currPosition = new Vector3(pos.x, pos.y, pos.z);
        updated = true;
    }
    public void Initialize(float3 pos)
    {
        transform.Rotate(Vector3.right,-90) ;
        anim = GetComponent<Animator>();
        moveMem = new Queue<Vector3>();
        currPosition = new Vector3(pos.x, pos.y, pos.z);
        updated = false;
        for (int i = 0; i <= 30; i++)
        {
            moveMem.Enqueue(new Vector3(0, 0, 0));
        }

        initialized = true;
    }



}

