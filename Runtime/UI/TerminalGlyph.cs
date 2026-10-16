using UnityEngine;
using UnityEngine.UI;

namespace Achieve.CheatTerminal.UI
{
    /// <summary>Resolution-independent dock artwork, with no texture or font dependency.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    internal sealed class TerminalGlyph : MaskableGraphic
    {
        internal enum Shape { Surface, Terminal, Cheats, Grip }
        internal Shape Kind;

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect r = GetPixelAdjustedRect();
            Vector2 center = r.center;
            float unit = Mathf.Min(r.width, r.height);
            if (Kind == Shape.Surface)
            {
                float radius = Mathf.Min(18f, unit * 0.22f);
                mesh.AddVert(center, color, Vector2.zero);
                for (int corner = 0; corner < 4; corner++)
                {
                    Vector2 origin = new Vector2(corner == 0 || corner == 3 ? r.xMax - radius : r.xMin + radius,
                        corner < 2 ? r.yMax - radius : r.yMin + radius);
                    for (int step = 0; step <= 8; step++)
                    {
                        float angle = (corner * 90f + step * 90f / 8f) * Mathf.Deg2Rad;
                        mesh.AddVert(origin + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius, color, Vector2.zero);
                    }
                }
                for (int i = 1; i <= 36; i++) mesh.AddTriangle(0, i == 36 ? 1 : i + 1, i);
                return;
            }

            float stroke = unit * 0.065f;
            if (Kind == Shape.Terminal)
            {
                Line(mesh, center + new Vector2(-.28f, .22f) * unit, center + new Vector2(-.05f, 0f) * unit, stroke);
                Line(mesh, center + new Vector2(-.05f, 0f) * unit, center + new Vector2(-.28f, -.22f) * unit, stroke);
                Line(mesh, center + new Vector2(.06f, -.22f) * unit, center + new Vector2(.32f, -.22f) * unit, stroke);
            }
            else if (Kind == Shape.Cheats)
            {
                for (int row = -1; row <= 1; row++)
                {
                    float y = row * .23f * unit;
                    Line(mesh, center + new Vector2(-.34f * unit, y), center + new Vector2(.34f * unit, y), stroke);
                    float x = row == 0 ? .16f : -.14f;
                    Line(mesh, center + new Vector2(x, row * .23f - .08f) * unit,
                        center + new Vector2(x, row * .23f + .08f) * unit, stroke * 2f);
                }
            }
            else
            {
                for (int row = -1; row <= 1; row++)
                    for (int col = -1; col <= 1; col += 2)
                        Line(mesh, center + new Vector2(col * .13f, row * .28f - .035f) * unit,
                            center + new Vector2(col * .13f, row * .28f + .035f) * unit, stroke);
            }
        }

        private void Line(VertexHelper mesh, Vector2 from, Vector2 to, float width)
        {
            Vector2 normal = new Vector2(-(to - from).y, (to - from).x).normalized * width * .5f;
            int index = mesh.currentVertCount;
            mesh.AddVert(from - normal, color, Vector2.zero);
            mesh.AddVert(from + normal, color, Vector2.zero);
            mesh.AddVert(to + normal, color, Vector2.zero);
            mesh.AddVert(to - normal, color, Vector2.zero);
            mesh.AddTriangle(index, index + 1, index + 2);
            mesh.AddTriangle(index, index + 2, index + 3);
        }
    }
}
