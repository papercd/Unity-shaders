using UnityEngine;

public class GPUGraph : MonoBehaviour
{

    float duration;

    bool transitioning;

    ComputeBuffer positionsBuffer;
    FunctionLibrary.FunctionName transitionFunction;

    [SerializeField]
    ComputeShader computeShader;

    [SerializeField]
    Material material;

    [SerializeField]
    Mesh mesh;

    static readonly int
        positionsID = Shader.PropertyToID("_Positions"),
        resolutionID = Shader.PropertyToID("_Resolution"),
        stepID = Shader.PropertyToID("_Step"),
        timeID = Shader.PropertyToID("_Time");

    [SerializeField, Range(10, 1000)]
    int resolution = 10;

    [SerializeField]
    FunctionLibrary.FunctionName function;

    public enum TransitionMode { Cycle, Random }

    [SerializeField]
    TransitionMode transitionMode = TransitionMode.Cycle;

    [SerializeField, Min(0f)]
    float functionDuration = 1f, transitionDuration = 1f;

    void OnEnable()
    {
        positionsBuffer = new ComputeBuffer(resolution * resolution, 3 * 4);
    }

    void Osable()
    {
        positionsBuffer.Release();
        positionsBuffer = null;
    }

    void Update()
    {
        duration += Time.deltaTime;
        if (transitioning)
        {
            if (duration >= transitionDuration)
            {
                duration -= transitionDuration;
                transitioning = false;
            }
        }
        else if (duration >= functionDuration)
        {
            duration -= functionDuration;
            transitioning = true;
            transitionFunction = function;
            PickNextFunction();
        }

        UpdateFunctionOnGPU();

    }

    void PickNextFunction()
    {
        function = transitionMode == TransitionMode.Cycle ?
            FunctionLibrary.GetNextFunctionName(function) :
            FunctionLibrary.GetRandomFunctionNameOtherThan(function);
    }

    void UpdateFunctionOnGPU()
    {
        float step = 2f / resolution;
        computeShader.SetInt(resolutionID, resolution);
        computeShader.SetFloat(stepID, step);
        computeShader.SetFloat(timeID, Time.time);
        computeShader.SetBuffer(0, positionsID, positionsBuffer);
        int groups = Mathf.CeilToInt(resolution / 8f);
        computeShader.Dispatch(0, groups, groups, 1);
        material.SetBuffer(positionsID, positionsBuffer);
        material.SetFloat(stepID, step);
        var bounds = new Bounds(Vector3.zero, Vector3.one * (2f + 2f / resolution));
        Graphics.DrawMeshInstancedProcedural(mesh, 0, material,bounds,positionsBuffer.count);
    }
}
