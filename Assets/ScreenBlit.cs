using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Mathematics;

//[ExecuteInEditMode]
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

    private void Update()
    {
        // fill per frame data
        for (int i = 0; i < buffer.Length; ++i)
        {
            buffer[i] = new float4(1.0f, 0.0f, 1.0f, 1.0f);
        }
        computeBuffer.SetData(buffer);
    }
}
