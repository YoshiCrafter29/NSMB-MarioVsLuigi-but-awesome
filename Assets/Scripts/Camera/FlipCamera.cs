using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Camera))]
public class FlipCamera : MonoBehaviour
{
    private Material material;
    // Creates a private material used to the effect
    void Awake()
    {
        material = new Material(Shader.Find("CameraShit/ReverseShader"));
    }

    private void Start()
    {
        Matrix4x4 mat = GetComponent<Camera>().projectionMatrix;
        mat *= Matrix4x4.Scale(new Vector3(1, -1, 1));
        GetComponent<Camera>().projectionMatrix = mat;
    }

    // Postprocess the image
    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Debug.Log("render!!!");
        /*
        if (intensity == 0)
        {
            Graphics.Blit(source, destination);
            return;
        }
        material.SetFloat("_bwBlend", intensity);
        */
        Graphics.Blit(source, destination, material);
    }
}