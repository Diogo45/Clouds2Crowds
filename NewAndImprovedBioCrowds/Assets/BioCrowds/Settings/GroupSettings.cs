using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public class GroupSettings : ISettings
{

    public static GroupSettings instance;

    public float springForce = -500f;

    public float springDamping = 3f;


    public float SpringSystemTimeStep = 0.0005f;

    public float springRestLength = 0.1f;

    public void SetSpringK(float k) { springForce = k; }
    public void SetSpringKD(float kd) { }

    public override void LoadExperimentFromFile()
    {
        throw new NotImplementedException();
    }

    public override void SaveExperimentToFile()
    {
        throw new NotImplementedException();
    }

    public override void SetExperiment(ISettings exp)
    {
        throw new NotImplementedException();
    }

    private void Start()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
            Destroy(gameObject);
    }


} 
