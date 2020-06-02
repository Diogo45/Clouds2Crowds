

using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class VisualAgent : MonoBehaviour {

    
    private Animator anim;
    private int queueLength = 30;
    private int queueAltLength = 120;
    public Queue<float> moveMem;        //fila com deslocamentos/magnitudes dos últimos moveMemLength movimentos
    public Queue<float> ragMem;        
    public Queue<float> heightMem;        
    public float[] qview;
    public float[] ragdoll;
    private Vector3 currPosition;
    private bool updated;
    private bool initialized;

    public int Ragdoll = 0;
    public Vector3 ParticleMeanPos = Vector3.up;
    public Vector3 ParticleDeltaVel { get; internal set; }

    public GameObject hips;
    private Rigidbody hips_rigdbody;

    public GameObject handL;
    private Rigidbody handL_rigdbody;

    public GameObject handR;
    private Rigidbody handR_rigdbody;

    public GameObject kneeL;
    private Rigidbody kneeL_rigdbody;

    public GameObject kneeR;
    private Rigidbody kneeR_rigdbody;

    public Vector3 force;
    //public Vector3[] forceByLimb;


    public float ragMean;

    private float side_rotate;
    void Start()
    {
        if (!initialized) {
            Initialize(0);
        }

        
        hips_rigdbody = hips.GetComponent<Rigidbody>();
        handL_rigdbody = handL.GetComponent<Rigidbody>();
        handR_rigdbody = handR.GetComponent<Rigidbody>();
        kneeL_rigdbody = kneeL.GetComponent<Rigidbody>();
        kneeR_rigdbody = kneeR.GetComponent<Rigidbody>();

        DontDestroyOnLoad(gameObject);
    }
	// Update is called once per frame
	void Update () {

        //TODO: Pause anim when biocrowds locked
        //if (ControlVariables.instance.isLockBioCrowds())
        //{

        //    for (int i = 0; i < anim.layerCount; i++)
        //    {
        //        anim.
        //    } 

            

        //    return;
            
        //}


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
        

        ragMem.Dequeue();
        ragMem.Enqueue(Ragdoll);

        heightMem.Dequeue();
        heightMem.Enqueue(ParticleMeanPos.y > 0 ? ParticleMeanPos.y : 1f);
        


        ragMean = 0f;
        foreach (float v in ragMem)
        {
            ragMean += v;
        }

        var heighMean = 0f;
        foreach (float v in heightMem)
        {
            heighMean += v;
        }


        heighMean /= queueAltLength;
        ragMean /= queueAltLength;


        

        if (ragMean > 0)
        {
            //Debug.Log("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAA");
            //this.enabled = false;
            anim.enabled = false;
            hips_rigdbody.isKinematic = true;

            //Debug.Log(ParticleMeanPos);
            

            var partVel = math.abs(ParticleDeltaVel.x)> 0f && math.abs(ParticleDeltaVel.y) > 0f && math.abs(ParticleDeltaVel.z) > 0f ? ParticleDeltaVel : Vector3.zero;
            
            hips.transform.position = currPosition + Vector3.up * heighMean;
            hips.transform.Rotate(partVel.x + (UnityEngine.Random.Range(0.1f, 0.5f) * side_rotate), partVel.y + (UnityEngine.Random.Range(0.1f, 0.5f) * side_rotate), partVel.z + (UnityEngine.Random.Range(0.1f, 0.5f) * side_rotate));

            //var rotate = partVel * UnityEngine.Random.Range(1.5f, 3.5f);
            //Debug.Log(rotate + " " + ParticleDeltaVel);
            //hips.transform.Rotate(rotate);
            //Debug.DrawRay(ParticleMeanPos, ParticleDeltaVel, Color.red, 5f);

            ////var diff = handL.transform.position;
            //var diff = Vector3.zero;
            //diff.y = (heighMean - handL.transform.position.y) * 0.5f;

            //handL_rigdbody.AddForce(diff, ForceMode.VelocityChange);


            ////diff = handR.transform.position;
            //diff = Vector3.zero;
            //diff.y = (heighMean - handR.transform.position.y) * 0.5f;

            //handR_rigdbody.AddForce(diff, ForceMode.VelocityChange);


            //// diff = kneeL.transform.position;
            //diff = Vector3.zero;
            //diff.y = (heighMean - kneeL.transform.position.y) * 0.8f;

            //kneeL_rigdbody.AddForce(diff, ForceMode.VelocityChange);

            ////diff = kneeR.transform.position;
            //diff = Vector3.zero;
            //diff.y = (heighMean - kneeR.transform.position.y) * 0.8f;

            //kneeR_rigdbody.AddForce(diff, ForceMode.VelocityChange);


        }
        else
        {
                side_rotate = UnityEngine.Random.Range(-1f, 1f) > 0f ? 1f : -1f;

                Vector3 currMoveVect = currPosition - transform.position;


                anim.enabled = true;

                


                float angleDifSum = 0;
                var prevV = moveMem.Peek();       //não parece é usado em nenhum lugar
                float speedSum = 0;
                foreach (float v in moveMem)
                {
                    speedSum += v;
                    //angleDifSum += Vector3.SignedAngle(prevV, v,Vector3.back);
                    prevV = v;
                }

                float presentAvgSpeed = (speedSum / moveMem.Count);
                float estFutureSpeed = currMoveVect.magnitude;
                float AvgSpeed = (presentAvgSpeed + estFutureSpeed) / 2;

                //float presentAvgAngleDif = angleDifSum / moveMem.Count;
                //float estFutureAngDif = Vector3.SignedAngle(prevV, currMoveVect, Vector3.back);
                //float avgAngleDif = (presentAvgAngleDif + estFutureAngDif) / 2;
                float totalAngleDiff = Vector3.SignedAngle(transform.forward, currMoveVect, Vector3.up);

                float angFact = totalAngleDiff / 90f;
                //anim.SetFloat("AngSpeed", angFact * 0.5f);// Mathf.Clamp(angDif/6f,-1f,1f));


                transform.Rotate(new Vector3(0, totalAngleDiff * 0.05f, 0), Space.World);
                //transform.rotation = Quaternion.Euler(0, Mathf.Atan2(speed.x,speed.z)*180f,0);
                //anim.SetFloat("AngSpeed", presentAvgAngleDif/3f);
                anim.SetFloat("Speed", (AvgSpeed * 3) / SimulationConstants.instance.BioCrowdsTimeStep);

                transform.position = currPosition;

        }





        qview = moveMem.ToArray();
        ragdoll = ragMem.ToArray();
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
        ragMem = new Queue<float>();
        heightMem = new Queue<float>();
        currPosition = new Vector3(pos.x, pos.y, pos.z);
        transform.position = currPosition;
        updated = false;
        for (int i = 0; i < queueLength; i++)
        {
            moveMem.Enqueue(0);
            
        }

        for (int i = 0; i < queueAltLength; i++)
        {
            ragMem.Enqueue(0);
            heightMem.Enqueue(0);

        }

        
        side_rotate = UnityEngine.Random.Range(-1f, 1f) > 0f ? 1f : -1f;
        initialized = true;
    }
}

