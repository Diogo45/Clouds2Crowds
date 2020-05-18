

using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class VisualAgent : MonoBehaviour {

    
    private Animator anim;
    private int moveMemLength = 30;
    public Queue<float> moveMem;        //fila com deslocamentos/magnitudes dos últimos moveMemLength movimentos
    public float[] qview;
    private Vector3 currPosition;
    private bool updated;
    private bool initialized;

    public bool Ragdoll = false;
    public Vector3 ParticleMeanPos = Vector3.up;
    public Vector3 ParticleDeltaVel { get; internal set; }

    public GameObject hips;
    private Rigidbody hips_rigdbody;

    public Vector3 force;
    //public Vector3[] forceByLimb;

    void Start()
    {
        if (!initialized) {
            Initialize(0);
        }

        
        hips_rigdbody = hips.GetComponent<Rigidbody>();

        DontDestroyOnLoad(gameObject);
    }
	// Update is called once per frame
	void Update () {

        //if (ControlVariables.instance.isLockBioCrowds())
        //{

        //    for (int i = 0; i < anim.layerCount; i++)
        //    {
        //        anim.
        //    } 

            

        //    return;
            
        //}

        if (Ragdoll)
        {
            //Debug.Log("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAA");
            //this.enabled = false;
            anim.enabled = false;
            //hips_rigdbody.isKinematic = true;
        }

        /*
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        
        transform.Translate(speed * Time.deltaTime, Space.World);
        
        
        */
        //if (!updated)
        //{
        //    Destroy(this.gameObject);
        //}

        
        //atualiza histórico de movimento
        Vector3 currMoveVect = currPosition - transform.position;
        moveMem.Dequeue();
        moveMem.Enqueue(currMoveVect.magnitude);


        float angleDifSum = 0;
        var prevV = moveMem.Peek();       //não parece é usado em nenhum lugar
        float speedSum = 0;
        foreach (float v in moveMem){
            speedSum += v;
            //angleDifSum += Vector3.SignedAngle(prevV, v,Vector3.back);
            prevV = v;
        }
        float presentAvgSpeed = (speedSum  / moveMem.Count) ;
        float estFutureSpeed = currMoveVect.magnitude;
        float AvgSpeed = (presentAvgSpeed + estFutureSpeed) / 2;

        //float presentAvgAngleDif = angleDifSum / moveMem.Count;
        //float estFutureAngDif = Vector3.SignedAngle(prevV, currMoveVect, Vector3.back);
        //float avgAngleDif = (presentAvgAngleDif + estFutureAngDif) / 2;
        float totalAngleDiff = Vector3.SignedAngle(transform.forward, currMoveVect, Vector3.up);

        float angFact = totalAngleDiff / 90f;
        //anim.SetFloat("AngSpeed", angFact * 0.5f);// Mathf.Clamp(angDif/6f,-1f,1f));


        if (!Ragdoll)
        {
            transform.Rotate(new Vector3(0, totalAngleDiff * 0.05f, 0), Space.World);
            //transform.rotation = Quaternion.Euler(0, Mathf.Atan2(speed.x,speed.z)*180f,0);
            //anim.SetFloat("AngSpeed", presentAvgAngleDif/3f);
            anim.SetFloat("Speed", (AvgSpeed*5) / SimulationConstants.instance.BioCrowdsTimeStep);

            transform.position = currPosition;
        }
        else
        {
            //Debug.Log(ParticleMeanPos);
            hips_rigdbody.AddForce( currPosition -  hips.transform.position, ForceMode.VelocityChange);

            //hips.transform.position = currPosition + Vector3.up * ParticleMeanPos.y;

            //Debug.DrawRay(ParticleMeanPos, ParticleDeltaVel, Color.red, 5f);

            // hips_rigdbody.


        }

        qview = moveMem.ToArray();
        updated = false;

    }

    //podia ser um setter né
    public float3 CurrPosition
    {
        get { return currPosition; }
        set { updated = true;
            currPosition = new Vector3(value.x, value.y, value.z); }
    }


    public void Initialize(float3 pos)
    {
        //transform.Rotate(Vector3.right, -90);
        anim = GetComponent<Animator>();
        moveMem = new Queue<float>();
        currPosition = new Vector3(pos.x, pos.y, pos.z);
        transform.position = currPosition;
        updated = false;
        for (int i = 0; i < moveMemLength; i++)
        {
            moveMem.Enqueue(0);
        }

        initialized = true;
    }
}

