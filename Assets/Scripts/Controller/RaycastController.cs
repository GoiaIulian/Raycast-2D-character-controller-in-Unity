using UnityEngine;
using UnityEngine.Serialization;

namespace Controller
{
    public class RaycastController : MonoBehaviour
    {
        public LayerMask collisionMask;
        protected const float SkinWidth = .015f;
        private const float DistanceBetweenRays = .25f;
        protected int horizontalRayCount;
        protected int verticalRayCount;
        
        [HideInInspector]
        public float horizontalRaySpacing;
        [HideInInspector]
        public float verticalRaySpacing;
        
        [HideInInspector]
        public BoxCollider2D boxCollider;
        protected RaycastOrigins raycastOrigins;
        
        //========================================================================================//
        public virtual void Start()
        {
            boxCollider = GetComponent<BoxCollider2D>();
            CalculateRaySpacing();
        }
        
        //========================================================================================//
        protected void UpdateRaycastOrigins()
        {
            var bounds = boxCollider.bounds;
            bounds.Expand(SkinWidth * -2);
            
            raycastOrigins.bottomLeft = new Vector2(bounds.min.x, bounds.min.y);
            raycastOrigins.bottomRight = new Vector2(bounds.max.x, bounds.min.y);
            raycastOrigins.topLeft = new Vector2(bounds.min.x, bounds.max.y);
            raycastOrigins.topRight = new Vector2(bounds.max.x, bounds.max.y);
        }
        
        //========================================================================================//
        private void CalculateRaySpacing()
        {
            var bounds = boxCollider.bounds;
            bounds.Expand(SkinWidth * -2);

            var boundsWidth = bounds.size.x;
            var boundsHeight = bounds.size.y;
            
            horizontalRayCount = Mathf.RoundToInt(boundsHeight / DistanceBetweenRays);
            verticalRayCount = Mathf.RoundToInt(boundsWidth / DistanceBetweenRays);

            horizontalRaySpacing = bounds.size.y / (horizontalRayCount - 1);
            verticalRaySpacing = bounds.size.x / (verticalRayCount - 1);
        }
        
        //========================================================================================//
        protected struct RaycastOrigins
        {
            public Vector2 topLeft, topRight;
            public Vector2 bottomLeft, bottomRight;
        }
    }
}