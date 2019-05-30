using Unity.Mathematics;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;

[ExecuteInEditMode]
public class WindowManager: MonoBehaviour
{
    public static Transform transform;

    public static WindowManager instance;
    public float2 sizeCreate;
    public float3 originCreate;

    public float2 sizeBase;
    public float3 originBase;

    public float2 sizeVisualize;
    public float3 originVisualize;

    private EntityManager entityManager;
    public static EntityArchetype WindowArchetype;
    public static Entity Window;
    private bool WindowCreated;

    [SerializeField]
    private float2 testSize;
    [SerializeField]
    private float3 testPos;

    public bool FollowMouse;
    public Camera cam;

    public GameObject[] quads;

    BioCrowdsPivotCorrectonatorSystemDeluxe PivotSystem;

    public delegate void MovedWindowEvent(float3 newPosition, float2 newSize);
    public static event MovedWindowEvent MovedWindow;

    public void ChangeVisualizationPivot(Vector3 newPivot)
    {
        PivotSystem.PivotChange(newPivot);
    }

    private float3 SnapPos2Grid(float3 pos)
    {
        pos = math.floor(pos);
        pos.x = pos.x - pos.x % 2;
        pos.y = pos.y - pos.y % 2;
        return pos;
    }

    private float2 SnapSize2Grid(float3 size)
    {
        size = math.floor(size);
        size.x = size.x - size.x % 2 + 1;
        size.y = size.y - size.y % 2 + 1;
        return new float2(size.x, size.y);
    }

    public void LateUpdate()
    {

        testSize = SnapSize2Grid(gameObject.transform.localScale);
        testPos = SnapPos2Grid(gameObject.transform.position);

        if (!SnapPos2Grid(gameObject.transform.position).Equals(originBase) || !SnapSize2Grid(gameObject.transform.localScale).Equals(sizeBase))
        {
            if (!WindowCreated)
            {
                entityManager = World.Active.GetOrCreateManager<EntityManager>();

                WindowArchetype = entityManager.CreateArchetype(
                    ComponentType.Create<Position>(),
                    ComponentType.Create<Rotation>());

                Window = entityManager.CreateEntity(WindowArchetype);
                Transform t = GameObject.Find("WindowManager").transform;
                entityManager.SetComponentData(Window, new Position { Value = new float3(t.position.x, t.position.y, t.position.z) });
                entityManager.SetComponentData(Window, new Rotation { Value = t.transform.rotation });
                WindowCreated = true;
            }
            SetWindow(SnapPos2Grid(gameObject.transform.position), SnapSize2Grid(gameObject.transform.localScale));

            quads[0].transform.localScale = new Vector3(sizeVisualize.x , sizeVisualize.y , 1f);
            quads[1].transform.localScale = new Vector3(sizeCreate.x, sizeCreate.y , 1f);
            quads[2].transform.localScale = new Vector3(sizeBase.x , sizeBase.y , 1f);

            quads[0].transform.localPosition = new Vector3(originVisualize.x + quads[0].transform.localScale.x/ 2f + 1f, originVisualize.y + quads[0].transform.localScale.y / 2f + 1f,  quads[0].transform.localPosition.z);
            quads[1].transform.localPosition = new Vector3(originCreate.x + quads[1].transform.localScale.x / 2f + 1f, originCreate.y + quads[1].transform.localScale.y / 2f + 1f, quads[1].transform.localPosition.z);
            quads[2].transform.localPosition = new Vector3(originBase.x + quads[2].transform.localScale.x / 2f + 1f, originBase.y + quads[2].transform.localScale.y / 2f + 1f, quads[2].transform.localPosition.z);



        }
    }

    public void Update()
    {
       if (_DrawRect)
      {
            //Debug.Log("???");
            DrawRect(originCreate + new float3(1.0f, 1.0f, 0.0f), sizeCreate, colorCreate);
            DrawRect(originBase + new float3(1.0f, 1.0f, 0.0f), sizeBase, colorDestroy);
            DrawRect(originVisualize + new float3(1.0f, 1.0f, 0.0f), sizeVisualize, colorVisualize);
       }

        if (Application.isPlaying)
        {
            
            if (Input.GetMouseButton(0))
            {
                RaycastHit hit;
                Ray r = cam.ScreenPointToRay(Input.mousePosition);
                Physics.Raycast(r, out hit);
                Vector3 newPos = new Vector3(hit.point.x, hit.point.y, 0f);
                transform.position = newPos;
            }
        }
    }

    public void Awake()
    {
        World activeWorld = World.Active;
        PivotSystem = activeWorld.GetExistingManager<BioCrowdsPivotCorrectonatorSystemDeluxe>();
        transform = gameObject.transform;
        if (!WindowCreated)
        {
            entityManager = World.Active.GetOrCreateManager<EntityManager>();

            WindowArchetype = entityManager.CreateArchetype(
                ComponentType.Create<Position>(),
                ComponentType.Create<Rotation>());

            Window = entityManager.CreateEntity(WindowArchetype);
            Transform t = GameObject.Find("WindowManager").transform;
            entityManager.SetComponentData(Window, new Position { Value = new float3(t.position.x, t.position.y, t.position.z) });
            entityManager.SetComponentData(Window, new Rotation { Value = t.transform.rotation });
            WindowCreated = true;
        }
        if (instance == null)
        {
            instance = this;
           // DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(instance);

        }
    }

    public static void SetWindow(float3 origin, float2 size)
    {
        float2 f2size = new float2(size.x, size.y);
        float3 aux = new float3(1f, 1f, 0f);

        WindowManager window = instance;

        window.originBase = origin;// - (4 * aux);
        window.sizeBase = f2size;// + new float2(8f, 8f);

        window.originVisualize = origin + (8 * aux);  ;
        window.sizeVisualize = f2size - new float2(16f, 16f);


        window.originCreate = origin + (4*aux);
        window.sizeCreate = f2size - new float2(8f, 8f);
        //Debug.Log("testeteste");

        if(transform == null)
            transform = GameObject.Find("WindowManager").transform;

        window.entityManager.SetComponentData(Window, new Position { Value = new float3(transform.position.x, transform.position.y, transform.position.z) });
        window.entityManager.SetComponentData(Window, new Rotation { Value = transform.transform.rotation });

        instance.ChangeVisualizationPivot(window.originBase);


        MovedWindow.Invoke(window.originBase, window.sizeBase);

    }

    public static float3 SubtractPivot(float3 pos, float3 pivot)
    {
        float3 auxPos = pos - pivot;
        return new float3(auxPos.x, auxPos.z, auxPos.y);
    }

    public static float3 AddPivot(float3 pos, float3 pivot)
    {
        float3 auxPos = new float3(pos.x, pos.z, pos.y);
        return auxPos + pivot;
    }

    public static float3 ChangePivot(float3 pos, float3 oldPivot, float3 newPivot)
    {
        float3 auxPos = AddPivot(pos, oldPivot);
        return SubtractPivot(auxPos, newPivot);
    }

    public static float3 Clouds2Crowds(float3 pos)
    {
        return (SubtractPivot(pos, instance.originBase));
        //float3 auxPos = pos - instance.originBase;
        //return new float3(auxPos.x, auxPos.z, auxPos.y);
    }

    public static float3 Crowds2Clouds(float3 pos)
    {
        return AddPivot(pos, instance.originBase);
        //float3 auxPos = new float3(pos.x, pos.z, pos.y);
        //return auxPos + instance.originBase;
    }

    private static bool CheckRectangle(float3 pos, float3 origin, float2 size)
    {

        return pos.x > origin.x             &&
               pos.x < origin.x + size.x    &&
               pos.y > origin.y             && 
               pos.y < origin.y + size.y;
    }

    /// <summary>
    /// Checks if a Cloud-coordinate position is in a destruction zone.
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    public static bool CheckDestructZone(float3 pos)
    {
        WindowManager window = instance;
        return !CheckCreateZone(pos) && !CheckVisualZone(pos);
        //return CheckRectangle(pos, instance.originBase, instance.sizeBase) &&
        //       (!CheckRectangle(pos, instance.originCreate, instance.sizeCreate));
        //return !CheckRectangle(pos, instance.originCreate, instance.sizeCreate);
    }

    /// <summary>
    /// Checks if a Cloud-coordinate position is in a creation zone.
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    public static bool CheckCreateZone(float3 pos)
    {
        WindowManager window = instance;
        return CheckRectangle(pos, instance.originCreate, instance.sizeCreate) &&
               (!CheckRectangle(pos, instance.originVisualize, instance.sizeVisualize));
    }
    
    /// <summary>
    /// Checks if a Cloud-coordinate position is in a Visualization zone.
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>fa
    public static bool CheckVisualZone(float3 pos)
    {
        WindowManager window = instance;
        return CheckRectangle(pos, instance.originVisualize, instance.sizeVisualize);
    }



    #region DebugVisualization
    public bool _DrawRect = true;
    public Color colorCreate;
    public Color colorDestroy;
    public Color colorVisualize;
    public Material LineMaterial;

    private Mesh cachedMesh;
    private static bool dirtyMesh = true;

    public void DrawLine(float3 origin, float3 dest, float width, Color color)
    {
        float3 widthVec = Vector3.Cross(Vector3.forward, dest - origin).normalized;
        float3 parallel = math.normalize(dest - origin);

        if (dirtyMesh)
        {
            Mesh m = new Mesh();
            m.vertices = new Vector3[]
            {
            (origin - parallel * width *0.5f) - 0.5f * widthVec * width,
            (origin - parallel * width *0.5f) + 0.5f * width * widthVec,
            (dest + parallel * width *0.5f) + 0.5f * width * widthVec,
            (dest + parallel * width *0.5f) - 0.5f * widthVec * width
            };

            m.colors = new Color[m.vertices.Length];
            for (int i = 0; i < m.vertices.Length; i++)
            {
                m.colors[i] = color;
            }

            m.triangles = new int[6] { 0, 1, 2, 2, 3, 0 };
            cachedMesh = m;
            //Debug.Log(m);
        }

        Graphics.DrawMesh(cachedMesh, Vector3.zero, quaternion.identity, LineMaterial, 0);
    }

    public void DrawRect(float3 origin, float2 size, Color color)
    {
        //Debug.Log("LeroLero" + origin + size);

        //if (Application.isPlaying)
        //{
        //    DrawLine(origin, new float3(origin.x + size.x, origin.y, origin.z), 0.5f, color);
        //    DrawLine(new float3(origin.x + size.x, origin.y, origin.z), new float3(origin.x + size.x, origin.y + size.y, origin.z), 0.5f, color);
        //    DrawLine(new float3(origin.x + size.x, origin.y + size.y, origin.z), new float3(origin.x, origin.y + size.y, origin.z), 0.5f, color);
        //    DrawLine(origin, new float3(origin.x, origin.y + size.y, origin.z), 0.5f, color);
        //}
        //else
        //{
            Debug.DrawLine(origin, new float3(origin.x + size.x, origin.y, origin.z), color);
            Debug.DrawLine(new float3(origin.x + size.x, origin.y, origin.z), new float3(origin.x + size.x, origin.y + size.y, origin.z), color);
            Debug.DrawLine(new float3(origin.x + size.x, origin.y + size.y, origin.z), new float3(origin.x, origin.y + size.y, origin.z), color);
            Debug.DrawLine(origin, new float3(origin.x, origin.y + size.y, origin.z), color);
        //}


    }
    #endregion


}