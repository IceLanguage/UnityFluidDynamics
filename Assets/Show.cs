
using UnityEngine;


/// <summary>
/// Author : 林豪
/// Creation Date : Jan 20 2019
/// Description:
/// This code is translated from the code provided in Jos Stam's GDC2003 paper 
/// Reference: Jos Stam, "Real-Time Fluid Dynamics for Games". Proceedings of the Game Developer Conference, March 2003.
/// </summary>
public class Show : MonoBehaviour {

    private readonly uint N = 64;
    private readonly uint size;
    private readonly float cellSize;
    private float[] velocityU, velocityV, velocityUpre, velocityVpre;
    private float[] densities, densitiesPre;
    private float preMousePosX, mousePosX, preMousePosY, mousePosY;

    public Material Mat;
    public float DiffusionVelocity = 0f;
    public float force = 5f;
    public float source = 100f;
    public DrawType drawType = DrawType.Density; 

    public enum DrawType
    {
        Density,
        Velocity
    }

    enum BoundaryCollisionType
    {
        Null,
        XDirection,
        YDirection
    }

    Show()
    {
        size = (N + 2) * (N + 2);
        cellSize = 1.0f / N;
    }

    private void Awake()
    {
        velocityU = new float[size];
        velocityV = new float[size];
        velocityUpre = new float[size];
        velocityVpre = new float[size];
        densities = new float[size];
        densitiesPre = new float[size];

        if(Mat == null)
        {
            var shader = Shader.Find("Hidden/Internal-Colored");
            Mat = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            // Turn on alpha blending
            Mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            Mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            // Turn backface culling off
            Mat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            // Turn off depth writes
            Mat.SetInt("_ZWrite", 0);
        }
    }

    private void Update()
    {
        for(uint i = 0;i < size; ++i)
        {
            velocityUpre[i] = 0f;
        }

        for (uint i = 0; i < size; ++i)
        {
            velocityVpre[i] = 0f;
        }

        for (uint i = 0; i < size; ++i)
        {
            densitiesPre[i] = 0f;
        }

        if (Input.GetMouseButtonDown(0))
        {
            uint index = GetIndexFromMousePos();
            velocityUpre[index] = force * (preMousePosX - mousePosX);
            velocityVpre[index] = force * (preMousePosY - mousePosY);
            preMousePosX = mousePosX;
            preMousePosY = mousePosY;
        }

        if (Input.GetMouseButtonDown(1))
        {
            uint index = GetIndexFromMousePos();
            densitiesPre[index] = source;
            preMousePosX = mousePosX;
            preMousePosY = mousePosY;
        }

        if(Input.GetKeyDown(KeyCode.C))
        {
            for (uint i = 0; i < size; i++)
            {
                velocityU[i] = velocityV[i] = velocityUpre[i] = velocityVpre[i] = densities[i] = densitiesPre[i] = 0.0f;
            }
        }

        float dt = 0.1f;// Time.deltaTime;
        UpdateVelocity(cellSize, N, size, velocityU, velocityV, velocityUpre, velocityVpre, DiffusionVelocity, dt);
        UpdateDensity(N, size, densities, densitiesPre, velocityU, velocityV, DiffusionVelocity, dt);
    }

    private void OnPostRender()
    {
       
        Mat.SetPass(0);
        GL.PushMatrix();
        GL.LoadOrtho();
        GL.Clear(false, true, Color.black);
        if (drawType == DrawType.Density)
        {
            GL.Begin(GL.QUADS);
            

            for (uint i = 0; i <= N; i++)
            {
                float x = (i - 0.5f) * cellSize;
                for (uint j = 0; j <= N; j++)
                {
                    float y = (j - 0.5f) * cellSize;

                    float d00 = densities[GetIndex(N, i, j)];
                    float d01 = densities[GetIndex(N, i, j + 1)];
                    float d10 = densities[GetIndex(N, i + 1, j)];
                    float d11 = densities[GetIndex(N, i + 1, j + 1)];

                    GL.Color(new Color(d00, d00, d00)); DrawOnePoint(x, y);
                    GL.Color(new Color(d10, d10, d10)); DrawOnePoint(x + cellSize, y);
                    GL.Color(new Color(d11, d11, d11)); DrawOnePoint(x + cellSize, y + cellSize);
                    GL.Color(new Color(d01, d01, d01)); DrawOnePoint(x, y + cellSize);
                }
            }

            GL.End();
        }
        else if (drawType == DrawType.Velocity)
        {

            GL.Begin(GL.LINES);
            GL.Color(Color.cyan);
            for (uint i = 1; i <= N; i++)
            {
                float x = (i - 0.5f) * cellSize;
                for (uint j = 1; j <= N; j++)
                {
                    float y = (j - 0.5f) * cellSize;
                    Vector2 v = new Vector2(velocityU[GetIndex(N, i, j)], velocityV[GetIndex(N, i, j)]);
                   
                    if (v == Vector2.zero)
                    {
                        DrawOnePoint(x, y);
                        DrawOnePoint(x + cellSize/size, y + cellSize / size);
                    }
                    else
                    {
                        
                        DrawOnePoint(x, y);
                        v.Normalize();
                        v /= N;
                        DrawOnePoint(x + v.x, y + v.y);
                       
                    }
                    
                }
            }
            GL.End();
        }

        GL.PopMatrix();
    }

    private uint GetIndexFromMousePos()
    {
        mousePosX = Input.mousePosition.x;
        mousePosY = Input.mousePosition.y;
        uint i = (uint)((mousePosX / Screen.width) * N + 1);
        uint j = (uint)((mousePosY / Screen.height) * N + 1);

        return GetIndex(N, i, j);
    }

    private void DrawOnePoint(float x, float y)
    {
        GL.Vertex(new Vector3(x, y, 0)); 
    }

    private static uint GetIndex(uint N, uint i, uint j)
    {
        return i + j * (N + 2);
    }
    
    private static void Swap(uint size, float[] a, float[] b)
    {
        for(uint i = 0; i < size; ++i)
        {
            float tmp = a[i];
            a[i] = b[i];
            b[i] = tmp;
        }
    }

    private static void UpdateVelocity(float cellSize, uint N, uint size, float[] u, float[] v, float[] upre, float[] vpre, float diffusionVelocity, float dt)
    {
        AddSource(size, u, upre, dt); AddSource(size, v, vpre, dt);
        Swap(size, upre, u); Diffuse(N, u, upre, diffusionVelocity, dt, BoundaryCollisionType.XDirection);
        Swap(size, vpre, v); Diffuse(N, v, vpre, diffusionVelocity, dt, BoundaryCollisionType.YDirection);
        Project(cellSize, N, u, v, upre, vpre);
        Swap(size, upre, u); Swap(size, vpre, v);
        Advect(N, dt, u, upre, upre, vpre, BoundaryCollisionType.XDirection); Advect(N, dt, v, vpre, upre, vpre, BoundaryCollisionType.YDirection);
        Project(cellSize, N, u, v, upre, vpre);
    }

    private static void UpdateDensity(uint N, uint size, float[] densities, float[] densitiesPre, float[] u, float[] v, float diffusionVelocity, float dt)
    {
        AddSource(size, densities, densitiesPre, dt);
        Swap(size, densitiesPre, densities);
        Diffuse(N, densities, densitiesPre, diffusionVelocity, dt, BoundaryCollisionType.Null);   
        Swap(size, densitiesPre, densities);       
        Advect(N, dt, densities, densitiesPre, u, v, BoundaryCollisionType.Null);
    }
    

    private static void Diffuse(uint N, float[] cur, float[] pre, float dt, float diffusionVelocity, BoundaryCollisionType b)
    {
        float a = diffusionVelocity * dt * N * N;
        for (uint k = 0; k < 20; ++k)
        {
            for(uint i = 1; i <= N; ++i)
            {
                for(uint j = 1; j <= N; ++j)
                {
                    uint centerIndex = GetIndex(N, i, j);
                    cur[centerIndex] = 
                    (
                        pre[centerIndex] + a *
                        (
                            cur[GetIndex(N, i + 1,j)] + cur[GetIndex(N, i, j + 1)] + cur[GetIndex(N, i - 1, j)] + cur[GetIndex(N, i, j - 1)]
                        )
                    )
                    /(1 + 4 * a);
                }
            }
            SetBoundary(N, b, cur);
        }
    }

    private static void AddSource(uint size, float[] dest, float[] source, float dt)
    {
        for (uint i = 0; i < size; i++)
        {
            dest[i] += dt * source[i];
        }
    }

    private static void Advect(uint N, float dt, float[] dest, float[] destPre, float[] u, float[] v, BoundaryCollisionType b)
    {
        float a = dt * N;
        for(uint i = 1; i <= N; ++i)
        {
            for (uint j = 1; j <= N; ++j)
            {
                uint centerIndex = GetIndex(N, i, j);
                float x = i - a * u[centerIndex];
                float y = j - a * v[centerIndex];

                if (x < 0.5f) x = 0.5f; if (x > N + 0.5f) x = N + 0.5f;
                uint i0 = (uint)x; uint i1 = i0 + 1;
                if (y < 0.5f) y = 0.5f; if (y > N + 0.5f) y = N + 0.5f;
                uint j0 = (uint)y; uint j1 = j0 + 1;

                float s1 = x - i0;float s0 = 1 - s1; float t1 = y - j0; float t0 = 1 - t1;

                dest[centerIndex] = 
                    s0 * (
                        t0 * destPre[GetIndex(N, i0, j0)] + 
                        t1 * destPre[GetIndex(N, i0, j1)]
                         )+
                    s1 * (
                        t0 * destPre[GetIndex(N, i1, j0)] + 
                        t1 * destPre[GetIndex(N, i1, j1)]
                        );
            }
        }
        SetBoundary(N, b, dest);
    }

    private static void Project(float h, uint N, float[] u, float[] v, float[] a, float[] b)
    {
        for (uint i = 1; i <= N; i++)
        {
            for (uint j = 1; j <= N; j++)
            {
                uint centerIndex = GetIndex(N, i, j);
                b[centerIndex] = - 0.5f * h * 
                    (   
                        u[GetIndex(N ,i + 1, j)] - u[GetIndex(N, i - 1, j)] +
                        v[GetIndex(N, i, j + 1)] - v[GetIndex(N, i, j - 1)]
                    );
                a[GetIndex(N, i, j)] = 0;
            }
        }

        for (uint k = 0; k < 20; ++k)
        {
            for (uint i = 1; i <= N; ++i)
            {
                for (uint j = 1; j <= N; ++j)
                {
                    a[GetIndex(N, i, j)] = 
                        (
                            b[GetIndex(N, i, j)] + 
                            a[GetIndex(N, i - 1, j)] + 
                            a[GetIndex(N, i + 1, j)] +
                            a[GetIndex(N, i, j - 1)] + 
                            a[GetIndex(N, i, j + 1)]
                        ) / 4f;
                }
            }
            SetBoundary(N, BoundaryCollisionType.Null, a);
        }

        for (uint i = 1; i <= N; i++)
        {
            for (uint j = 1; j <= N; j++)
            {
                uint centerIndex = GetIndex(N, i, j);
                u[centerIndex] -= 0.5f * (a[GetIndex(N, i + 1, j)] - a[GetIndex(N, i - 1, j)]) / h;
                v[centerIndex] -= 0.5f * (a[GetIndex(N, i, j + 1)] - a[GetIndex(N, i, j - 1)]) / h;
            }
        }

        SetBoundary(N, BoundaryCollisionType.XDirection, u);
        SetBoundary(N, BoundaryCollisionType.YDirection, v);
    }

    private static void SetBoundary(uint N ,BoundaryCollisionType b,float[] x)
    {
        for (uint i = 1; i <= N; i++)
        {
            x[GetIndex(N ,0, i)] = b == BoundaryCollisionType.XDirection ? -x[GetIndex(N, 1, i)] : x[GetIndex(N, 1, i)];
            x[GetIndex(N, N + 1, i)] = b == BoundaryCollisionType.XDirection ? -x[GetIndex(N, N, i)] : x[GetIndex(N, N, i)];
            x[GetIndex(N, i, 0)] = b == BoundaryCollisionType.YDirection ? -x[GetIndex(N, i, 1)] : x[GetIndex(N, i, 1)];
            x[GetIndex(N, i, N + 1)] = b == BoundaryCollisionType.YDirection ? -x[GetIndex(N, i, N)] : x[GetIndex(N, i, N)];
        }
        x[GetIndex(N, 0, 0)] = 0.5f * (x[GetIndex(N, 1, 0)] + x[GetIndex(N, 0, 1)]);
        x[GetIndex(N, 0, N + 1)] = 0.5f * (x[GetIndex(N, 1, N + 1)] + x[GetIndex(N, 0, N)]);
        x[GetIndex(N, N + 1, 0)] = 0.5f * (x[GetIndex(N, N, 0)] + x[GetIndex(N, N + 1, 1)]);
        x[GetIndex(N, N + 1, N + 1)] = 0.5f * (x[GetIndex(N, N, N + 1)] + x[GetIndex(N, N + 1, N)]);
    }
}
