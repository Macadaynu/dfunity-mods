using UnityEngine;

[ExecuteAlways]
public class TerrainClipBox : MonoBehaviour
{
    void Update()
    {
        //Shader.SetGlobalMatrix("_WorldToTerrainBox", transform.worldToLocalMatrix);

        Shader.SetGlobalMatrix(transform.gameObject.name, transform.worldToLocalMatrix);
    }
}
