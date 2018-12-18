using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using System.Linq;
using Unity.Transforms;
using Unity.Jobs;
using UnityEditor;

namespace BioCrowds
{
    public class NormalLifeSettings : MonoBehaviour
    {

        public static NormalLifeSettings instance;
        //maior número de auxinas que um agente pode ter para o cálculo relacionado ao conforto
        public int MAX_C_AUXINS = 80;
        //multiplicador do efeito de baixo conforto
        public float CONFORT_MULT = 1f;
        //parametro para força de contagio
        public float CONST_CONTAGIO = 1f;
        //parametro para diminuição do calmo
        public float CALMO_DEC = 0.07f;
        //parametro para decaimento
        public float CONST_DECAIMENTO = 1f;
        public float STRESS_BETA = 0.02f;
        public float STRESS_RHO = 0.4f;
        public float STRESS_K1 = 10f;



        private void Awake()
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
}