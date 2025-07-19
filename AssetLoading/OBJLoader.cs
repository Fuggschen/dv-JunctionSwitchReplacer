using System;
using System.Collections.Generic;
using UnityEngine;
using UnityModManagerNet;

namespace JunctionSwitchReplacer.AssetLoading
{
    public static class OBJLoader
    {
        // Load OBJ mesh from text content (for AssetBundle TextAssets only)
        public static Mesh LoadMeshFromText(string objContent, UnityModManager.ModEntry mod)
        {
            try
            {
                string[] lines = objContent.Split('\n');
                var vertices = new List<Vector3>();
                var triangles = new List<int>();
                var uvs = new List<Vector2>();
                
                foreach (string line in lines)
                {
                    string[] parts = line.Trim().Split(' ');
                    if (parts.Length < 2) continue;
                    
                    switch (parts[0])
                    {
                        case "v": // Vertex
                            if (parts.Length >= 4)
                            {
                                float x = float.Parse(parts[1]);
                                float y = float.Parse(parts[2]);
                                float z = float.Parse(parts[3]);
                                vertices.Add(new Vector3(x, y, z));
                            }
                            break;
                            
                        case "vt": // UV
                            if (parts.Length >= 3)
                            {
                                float u = float.Parse(parts[1]);
                                float v = float.Parse(parts[2]);
                                uvs.Add(new Vector2(u, v));
                            }
                            break;
                            
                        case "f": // Face
                            if (parts.Length >= 4)
                            {
                                // Simple triangulation (assumes triangular faces)
                                for (int i = 1; i <= 3; i++)
                                {
                                    string[] indices = parts[i].Split('/');
                                    if (indices.Length > 0 && int.TryParse(indices[0], out int vertIndex))
                                    {
                                        triangles.Add(vertIndex - 1); // OBJ indices are 1-based
                                    }
                                }
                            }
                            break;
                    }
                }
                
                if (vertices.Count > 0 && triangles.Count > 0)
                {
                    var mesh = new Mesh();
                    mesh.vertices = vertices.ToArray();
                    mesh.triangles = triangles.ToArray();
                    
                    // Ensure we have UV coordinates
                    if (uvs.Count == vertices.Count)
                    {
                        mesh.uv = uvs.ToArray();
                        mod.Logger.Log($"OBJ loaded with {uvs.Count} UV coordinates");
                    }
                    else
                    {
                        // Generate cylindrical UV mapping for switch poles
                        var generatedUVs = new Vector2[vertices.Count];
                        var bounds = GetMeshBounds(vertices);
                        
                        for (int i = 0; i < vertices.Count; i++)
                        {
                            var v = vertices[i];
                            // Cylindrical mapping - good for pole-like objects
                            float u = (Mathf.Atan2(v.z - bounds.center.z, v.x - bounds.center.x) / (2 * Mathf.PI)) + 0.5f;
                            float vCoord = (v.y - bounds.min.y) / bounds.size.y;
                            generatedUVs[i] = new Vector2(u, vCoord);
                        }
                        mesh.uv = generatedUVs;
                        mod.Logger.Log($"Generated {generatedUVs.Length} UV coordinates (cylindrical mapping for pole)");
                    }
                    
                    mesh.RecalculateNormals();
                    mesh.RecalculateBounds();
                    mesh.name = "Custom OBJ Switch Setter";
                    
                    mod.Logger.Log($"OBJ mesh created successfully: {vertices.Count} vertices, {triangles.Count/3} triangles");
                    return mesh;
                }
            }
            catch (Exception ex)
            {
                mod.Logger.Error($"Failed to parse OBJ content: {ex.Message}");
            }
            
            return null;
        }

        private static Bounds GetMeshBounds(List<Vector3> vertices)
        {
            if (vertices.Count == 0) return new Bounds();
            
            Vector3 min = vertices[0];
            Vector3 max = vertices[0];
            
            foreach (var v in vertices)
            {
                min = Vector3.Min(min, v);
                max = Vector3.Max(max, v);
            }
            
            return new Bounds((min + max) * 0.5f, max - min);
        }
    }
}
