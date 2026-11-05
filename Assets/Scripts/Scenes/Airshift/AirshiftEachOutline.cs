using UnityEngine;

#nullable enable
namespace MajdataPlay.Scenes.Airshift
{
    /// <summary>
    /// Reusable, visual-only AIRSHIFT Each cue for arbitrary STAR/TOUCH skin
    /// sprites. A slightly enlarged alpha silhouette is drawn behind the
    /// original sprite; the imported texture, its material and RGB tint are
    /// never modified. The overlay is created lazily for AIRSHIFT Each only.
    /// </summary>
    internal static class AirshiftEachOutline
    {
        private const float SilhouetteScale = 1.09f;
        // More positive Z means behind its source sprite in the note layers.
        private const float BehindSourceDepth = 0.0004f;
        private const string MaterialResourcePath = "Airshift/EachSilhouette";

        /// <summary>
        /// Synchronizes a reusable child renderer with the skin currently in
        /// use. No allocating/destroying renderers on pooled-note reuse.
        /// </summary>
        internal static void Sync(
            ref SpriteRenderer? outline,
            SpriteRenderer source,
            bool show,
            float alpha = 1f)
        {
            if (show && outline is null)
            {
                var material = Resources.Load<Material>(MaterialResourcePath);
                if (material is null)
                {
                    Debug.LogError("[AIRSHIFT] Missing Each outline material: " + MaterialResourcePath);
                    return;
                }

                var go = new GameObject("AIRSHIFT_EachYellowOutline");
                var t = go.transform;
                t.SetParent(source.transform, false);
                t.localPosition = new Vector3(0f, 0f, BehindSourceDepth);
                t.localRotation = Quaternion.identity;
                t.localScale = new Vector3(SilhouetteScale, SilhouetteScale, 1f);

                outline = go.AddComponent<SpriteRenderer>();
                outline.sharedMaterial = material;
                outline.enabled = false;
            }

            if (outline is null)
            {
                return;
            }

            outline.sprite = source.sprite;
            outline.flipX = source.flipX;
            outline.flipY = source.flipY;
            outline.sortingLayerID = source.sortingLayerID;
            outline.sortingOrder = source.sortingOrder;
            outline.spriteSortPoint = source.spriteSortPoint;
            outline.maskInteraction = source.maskInteraction;
            // The shader samples texture alpha and emits flat yellow. Preserve
            // animated transparency independently of body RGB multiplication.
            outline.color = new Color(1f, 1f, 1f, Mathf.Clamp01(alpha));
            outline.enabled = show && source.enabled;
        }
    }
}
