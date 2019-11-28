using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IKHandling : MonoBehaviour {

    Animator anim;

    //public float IKWeight = 1;
    //public Transform leftIKTarget, rightIKTarget;
    //public Transform leftIKHint, rightIKHint;

    public Vector3 lFpos, rFpos;
    Quaternion lFrot, rFrot;
    float lFweight, rFweight;
    Transform leftFoot, rightFoot;

	
	void Start ()
    {
        anim = GetComponent<Animator>();

        leftFoot = anim.GetBoneTransform(HumanBodyBones.LeftFoot);
        rightFoot = anim.GetBoneTransform(HumanBodyBones.RightFoot);

        lFrot = leftFoot.rotation;
        rFrot = rightFoot.rotation;
    }
	
	// Update is called once per frame
	void Update ()
    {
        RaycastHit lHit, rHit;

        Vector3 lpos = leftFoot.TransformPoint(Vector3.zero);
        Vector3 rpos = rightFoot.TransformPoint(Vector3.zero);

        //if ( Physics.Raycast(lpos, -Vector3.up, out lHit, 1) )
        //{
        //    lFpos = lHit.point;
        //    lFrot = Quaternion.FromToRotation(transform.up, lHit.normal) * transform.rotation;
        //}

        //if ( Physics.Raycast(rpos, -Vector3.up, out rHit, 1) )
        //{
        //    rFpos = rHit.point;
        //    rFrot = Quaternion.FromToRotation(transform.up, rHit.normal) * transform.rotation;
        //}

    }

    void OnAnimatorIK (int layerIndex)
    {
        lFweight = anim.GetFloat("LeftFoot");
        rFweight = anim.GetFloat("RightFoot");

        anim.SetIKPositionWeight(AvatarIKGoal.LeftFoot, lFweight);
        anim.SetIKPositionWeight(AvatarIKGoal.RightFoot, rFweight);

        anim.SetIKPosition(AvatarIKGoal.LeftFoot, lFpos);
        anim.SetIKPosition(AvatarIKGoal.RightFoot, rFpos);

        anim.SetIKRotationWeight(AvatarIKGoal.LeftFoot, lFweight);
        anim.SetIKRotationWeight(AvatarIKGoal.RightFoot, rFweight);
        
        anim.SetIKRotation(AvatarIKGoal.LeftFoot, lFrot);
        anim.SetIKRotation(AvatarIKGoal.RightFoot, rFrot);

        //anim.SetIKHintPositionWeight(AvatarIKHint.LeftKnee, IKWeight);
        //anim.SetIKHintPositionWeight(AvatarIKHint.RightKnee, IKWeight);
        
        //anim.SetIKHintPosition(AvatarIKHint.LeftKnee, leftIKHint.position);
        //anim.SetIKHintPosition(AvatarIKHint.RightKnee, rightIKHint.position);
        

    }
}
