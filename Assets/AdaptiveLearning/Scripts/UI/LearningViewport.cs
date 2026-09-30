using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace AdaptiveOhmsLaw
{
    // Render the 3D view into the UI so modal panels reliably appear above it in URP.
    [RequireComponent(typeof(RawImage))]
    public class LearningViewport : MonoBehaviour, IPointerClickHandler
    {
        Camera view;
        RenderTexture texture;
        RawImage image;
        public void Configure(Camera camera)
        {
            view=camera;image=GetComponent<RawImage>();image.raycastTarget=true;
            // This camera renders into a UI texture. Match a single-sample
            // attachment explicitly instead of inheriting the URP backbuffer MSAA.
            view.allowMSAA=false;
            var raycaster=view.GetComponent<PhysicsRaycaster>();if(raycaster!=null)raycaster.enabled=false;
            Refresh();
        }
        public void Refresh()
        {
            Canvas.ForceUpdateCanvases();var rt=(RectTransform)transform;var corners=new Vector3[4];rt.GetWorldCorners(corners);
            float pixelWidth=Mathf.Max(1,corners[2].x-corners[0].x),pixelHeight=Mathf.Max(1,corners[2].y-corners[0].y);
            // Keep tablets economical while retaining crisp apparatus edges on desktop.
            float maxHeight=Application.isMobilePlatform?720:1080;
            float scale=Mathf.Min(1,(maxHeight*16/9)/pixelWidth,maxHeight/pixelHeight);
            int width=Mathf.Max(1,Mathf.RoundToInt(pixelWidth*scale)),height=Mathf.Max(1,Mathf.RoundToInt(pixelHeight*scale));
            if(texture==null||texture.width!=width||texture.height!=height)
            {
                if(texture!=null){view.targetTexture=null;texture.Release();Destroy(texture);}
                texture=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32){name="Learning apparatus viewport",antiAliasing=1};texture.Create();image.texture=texture;view.targetTexture=texture;
            }
            view.rect=new Rect(0,0,1,1);view.aspect=pixelWidth/pixelHeight;
        }
        public void OnPointerClick(PointerEventData data)
        {
            if(view==null||data.button!=PointerEventData.InputButton.Left)return;
            var rt=(RectTransform)transform;
            if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(rt,data.position,data.pressEventCamera,out var point))return;
            var uv=(point-rt.rect.min)/rt.rect.size;
            if(Physics.Raycast(view.ViewportPointToRay(uv),out var hit,100,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))
            {
                var target=hit.collider.GetComponentInParent<CircuitSwitchInteraction>();if(target!=null&&target.isActiveAndEnabled)target.OnPointerClick(data);
            }
        }
        void OnDestroy(){if(view!=null)view.targetTexture=null;if(texture!=null){texture.Release();Destroy(texture);}}
    }
}
