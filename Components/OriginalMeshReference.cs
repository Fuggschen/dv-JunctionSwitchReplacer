using UnityEngine;

namespace JunctionSwitchReplacer.Components
{
    // Component to store reference to original mesh and materials for restoration
    public class OriginalMeshReference : MonoBehaviour
    {
        public Mesh originalMesh;
        public Material[] originalMaterials;
    }
}
