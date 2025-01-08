using UnityEngine;

public class SetGroundTextureTiling : MonoBehaviour
{
    [SerializeField] private Material groundMaterial; // Assign your material here
    [SerializeField] private string textureProperty = "_MainTex"; // Default texture property for the texture
    [SerializeField] private float textureSize = 10f; // Size the texture is designed for (e.g., 10 units)

    void Start()
    {
        if (groundMaterial != null && groundMaterial.HasProperty(textureProperty))
        {
            // Get the size of the ground object
            Vector3 groundSize = transform.localScale;

            // Calculate tiling based on the size of the ground and texture size
            Vector2 tiling = new Vector2(groundSize.x / textureSize, groundSize.z / textureSize);

            // Apply the tiling to the material
            groundMaterial.SetTextureScale(textureProperty, tiling);
        }
    }
}