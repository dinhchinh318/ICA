using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace LumaReef.Core
{
    public static class ReefCapture
    {
        // Explicit offscreen render also works when the automated player window is hidden.
        public static void Save(Camera camera,Canvas canvas,string filename)
        {
            var target=new RenderTexture(1080,1920,24,RenderTextureFormat.ARGB32);target.Create();
            var mode=canvas.renderMode;var previousCamera=canvas.worldCamera;float plane=canvas.planeDistance;
            var oldActive=RenderTexture.active;var oldRect=camera.rect;
            Texture2D image=null;
            try
            {
                canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
                camera.rect=new Rect(0,0,1,1);Canvas.ForceUpdateCanvases();
                RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});
                RenderTexture.active=target;image=new Texture2D(1080,1920,TextureFormat.RGB24,false);
                image.ReadPixels(new Rect(0,0,1080,1920),0,0);image.Apply();
                // A successful logic test must not mask an empty rendered scene.
                var pixels=image.GetPixels32();int lit=0;for(int i=0;i<pixels.Length;i+=61)if(pixels[i].r+pixels[i].g+pixels[i].b>40)lit++;
                Debug.Assert(lit>1000,"Rendered capture must contain visible game content");
                File.WriteAllBytes(System.IO.Path.Combine(Application.dataPath,"../"+filename),image.EncodeToPNG());
            }
            finally
            {
                canvas.renderMode=mode;canvas.worldCamera=previousCamera;canvas.planeDistance=plane;camera.rect=oldRect;RenderTexture.active=oldActive;
                if(image!=null)Object.Destroy(image);target.Release();Object.Destroy(target);
            }
        }
    }
}
