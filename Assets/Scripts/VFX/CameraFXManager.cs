using UnityEngine;
namespace LumaReef.VFX
{
    public sealed class CameraFXManager
    {
        readonly Transform camera;
        readonly Vector3 origin;
        float shake,trauma,slow,hitPause;
        public float SimulationScale=>hitPause>0?0:slow>0?.4f:1;
        public CameraFXManager(Camera cam) { camera=cam.transform; origin=camera.position; }
        public void Pulse(float intensity,bool big=false) { trauma=Mathf.Max(trauma,intensity); shake=.35f; if(big) { slow=.75f; hitPause=.065f; } }
        public void Tick(float dt)
        {
            hitPause=Mathf.Max(0,hitPause-dt); slow=Mathf.Max(0,slow-dt); shake=Mathf.Max(0,shake-dt);
            camera.position=origin+(shake>0?(Vector3)Random.insideUnitCircle*trauma*(shake/.35f):Vector3.zero);
        }
        public void Reset() { shake=slow=hitPause=0; camera.position=origin; }
    }
}
