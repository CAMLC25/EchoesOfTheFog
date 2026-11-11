using UnityEngine;
using UnityEngine.UI;

namespace EchoesOfTheFog.UI
{
    // Native UI vertex shading: keeps the supplied background image untouched.
    [RequireComponent(typeof(Graphic))]
    public sealed class MenuGradient : BaseMeshEffect
    {
        public Color left = new Color(0.015f, 0.035f, 0.04f, 0.9f);
        public Color right = new Color(0.015f, 0.035f, 0.04f, 0);

        public override void ModifyMesh(VertexHelper vertices)
        {
            if (!IsActive()) return;
            var rect = graphic.rectTransform.rect;
            for (int i = 0; i < vertices.currentVertCount; i++)
            {
                var vertex = new UIVertex(); vertices.PopulateUIVertex(ref vertex, i);
                vertex.color = (Color)vertex.color * Color.Lerp(left, right, Mathf.InverseLerp(rect.xMin, rect.xMax, vertex.position.x));
                vertices.SetUIVertex(vertex, i);
            }
        }
    }
}
