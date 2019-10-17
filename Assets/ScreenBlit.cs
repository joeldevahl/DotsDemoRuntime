using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Mathematics;
using Unity.Jobs;

[ExecuteInEditMode]
public class ScreenBlit : MonoBehaviour
{
    private CommandBuffer cmd;
    private int width;
    private int height;
    private ComputeBuffer computeBuffer;
    private NativeArray<float4> buffer;
    public Material blitMaterial;
    
    void OnEnable()
    {
        cmd = new CommandBuffer();
        Camera.main.AddCommandBuffer(CameraEvent.AfterEverything, cmd);
        height = Camera.main.pixelHeight;
        width = Camera.main.pixelWidth;
        buffer = new NativeArray<float4>(width * height, Allocator.Persistent);
        computeBuffer = new ComputeBuffer(buffer.Length * 16, 16, ComputeBufferType.Structured);
        
        // fill initial data
        for (int i = 0; i < buffer.Length; ++i)
        {
            buffer[i] = new float4(1.0f, 0.0f, 1.0f, 1.0f);
        }
        computeBuffer.SetData(buffer);
        
        // set up blitting command buffer
        blitMaterial.SetBuffer("_buffer", computeBuffer);
        cmd.DrawProcedural(Matrix4x4.identity, blitMaterial, 0, MeshTopology.Triangles, 3, 1);
    }

    private void OnDisable()
    {
        Camera.main.RemoveCommandBuffer(CameraEvent.AfterEverything, cmd);
        cmd.Dispose();
        computeBuffer.Dispose();
        buffer.Dispose();
    }

    [BurstCompile(CompileSynchronously = true)]
    struct RTJob : IJobParallelFor
    {
        public float aspect;
        public float near;
        public int width;
        public int height;
        public NativeArray<float4> buffer;
        public void Execute(int i)
        {
            var x = (float)(i % width) / (float)width;
            var y = (float)(i / width) / (float)height;

            x = x * 2.0f - 1.0f;
            y = 1.0f - y * 2.0f;

            var rayOrigin = new float3(0.0f, 0.0f, 0.0f);
            var forward = new float3(0.0f, 0.0f, -1.0f);
            var up = new float3(0.0f, 1.0f, 1.0f);
            var right = new float3(1.0f, 0.0f, 0.0f);

            var rayDir = math.normalize(forward * near + up * y + right * x);

            var color = new float3(rayDir.xyz);
            buffer[i] += new float4(color, 1.0f);
        }
    }

    private void Update()
    {
        // fill per frame data
        var job = new RTJob();
        job.aspect = (float)width / (float)height;
        job.near = 1.0f;
        job.width = width;
        job.height = height;
        job.buffer = buffer;
        
        job.Schedule(width * height, 256).Complete();
        
        computeBuffer.SetData(buffer);
    }
}
