using UnityEngine;
namespace LumaReef.Path
{
    public enum PathKind { Straight, Curve, SCurve, Wave, Zigzag, Circle, Spiral }
    [CreateAssetMenu(menuName="Luma Reef/Bezier path")]
    public sealed class BezierPath : ScriptableObject
    {
        public PathKind kind;
        // Cubic spline: anchor, control, control, anchor, control, control, anchor...
        public Vector2[] points;
        public float length=24;
        public Vector2 Evaluate(float t)
        {
            int count=(points.Length-1)/3;
            float scaled=Mathf.Clamp01(t)*count;
            int segment=Mathf.Min((int)scaled,count-1), i=segment*3;
            float u=scaled-segment, v=1-u;
            return v*v*v*points[i]+3*v*v*u*points[i+1]+3*v*u*u*points[i+2]+u*u*u*points[i+3];
        }
        public Vector2 Tangent(float t) => (Evaluate(Mathf.Min(t+.001f,1))-Evaluate(Mathf.Max(t-.001f,0))).normalized;
    }
}
