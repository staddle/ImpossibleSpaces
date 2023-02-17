using UnityEngine;

public class SetShaderProperties : MonoBehaviour
{
    public Vector2 playArea = new Vector2(10, 10);

    // Start is called before the first frame update
    void Start()
    {
        Vector4[] room =
        {
            new Vector4(0,0,0,0),
            new Vector4(10,0,0,0),
            new Vector4(10,0,10,0),
            new Vector4(5,0,10,0),
            new Vector4(5,0,5,0),
            new Vector4(0,0,5,0),
        };
        
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        Material material = renderer.material;
        material.SetVectorArray("_RoomVertices", room);
        material.SetInt("_RoomVertexCount", room.Length);
        renderer.material = material;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
