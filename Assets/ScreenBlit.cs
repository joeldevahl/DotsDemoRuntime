using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Mathematics;
using Unity.Jobs;
using UnityEditor;

[ExecuteInEditMode]
public class ScreenBlit : MonoBehaviour
{
    private CommandBuffer cmd;
    private int width;
    private int height;
    private ComputeBuffer computeBuffer;
    private NativeArray<float4> backBuffer;

    private double startTime = 0.0;
    private double sampleRate = 0.0;
    private bool running = false;
    
    public Material blitMaterial;
    
    void OnEnable()
    {
        cmd = new CommandBuffer();
        Camera.main.AddCommandBuffer(CameraEvent.AfterEverything, cmd);
        height = Camera.main.pixelHeight;
        width = Camera.main.pixelWidth;
        backBuffer = new NativeArray<float4>(width * height, Allocator.Persistent);
        computeBuffer = new ComputeBuffer(backBuffer.Length * 16, 16, ComputeBufferType.Structured);
        
        // fill initial data
        for (int i = 0; i < backBuffer.Length; ++i)
        {
            backBuffer[i] = new float4(1.0f, 0.0f, 1.0f, 1.0f);
        }
        computeBuffer.SetData(backBuffer);
        
        // set up blitting command buffer
        blitMaterial.SetBuffer("_buffer", computeBuffer);
        cmd.DrawProcedural(Matrix4x4.identity, blitMaterial, 0, MeshTopology.Triangles, 3, 1);
    }

    void Start()
    {
        if (!EditorApplication.isPlaying)
            return;
        
        startTime = AudioSettings.dspTime;
        sampleRate = AudioSettings.outputSampleRate;
        running = true;
    }
    
    void OnDisable()
    {
        Camera.main.RemoveCommandBuffer(CameraEvent.AfterEverything, cmd);
        cmd.Dispose();
        computeBuffer.Dispose();
        backBuffer.Dispose();
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

    void Update()
    {
        // fill per frame data
        var job = new RTJob();
        job.aspect = (float)width / (float)height;
        job.near = 1.0f;
        job.width = width;
        job.height = height;
        job.buffer = backBuffer;
        
        job.Schedule(width * height, 256).Complete();
        
        computeBuffer.SetData(backBuffer);
    }

    void OnAudioFilterRead(float[] data, int channels)
    {
        if (!running)
            return;
        
        double currTime = (AudioSettings.dspTime - startTime);
        double currTick = currTime * sampleRate;
        
        var numSamples = data.Length / channels;
        for (int i = 0; i < numSamples; i++)
        {
            float sample = math.sin((float)currTick * 1.0f);
            for (int c = 0; c < channels; ++c)
            {
                data[i + c] = sample;
            }
        }
    }
}
