﻿/***
 *              HuaHua
 *              2020-09-25
 *              富文本，一个drawcall 支持文字和图片混排
 **/

using HuaHua;

namespace UnityEngine.UI
{
    //继承Image
    [ExecuteInEditMode]
    public class RichImage : Image
    {
        //辅助生成网格
        //可以存储顶点的位置、颜色、UV、法线等信息，并提供方法来生成UI元素的Mesh网格
        //辅助自定义UI元素的渲染：通过重写 OnPopulateMesh(VertexHelper vh) 方法
        [System.NonSerialized]
        private static readonly VertexHelper s_VertexHelper = new VertexHelper();

        internal static readonly HideFlags MeshHideflags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor | HideFlags.HideInInspector;

        [SerializeField]
        public ERichTextMode m_UiMode = ERichTextMode.ERTM_MergeText;

        [System.NonSerialized]
        private Mesh m_mesh;

        [System.NonSerialized]
        private Vector3 m_lastPosition;

        [System.NonSerialized]
        private Quaternion m_lastRotation;

        [System.NonSerialized]
        private Vector3 m_lastScale;

        [SerializeField]
        public bool m_isMajor = false;
        public Mesh Mesh()
        {
            return m_mesh;
        }

        void ChangeZTest()
        {
            string propertyName = "_Ztest";
            int ztestID = Shader.PropertyToID(propertyName);
            var prop = new MaterialPropertyBlock();
            var render = gameObject.GetOrAddComponent<MeshRenderer>();
            render.GetPropertyBlock(prop);

            //只能设置深度,设置渲染队列会导致不合批
            if (m_isMajor == true)
            {
                prop.SetInt(ztestID, (int)UnityEngine.Rendering.CompareFunction.Always);
            }
            else
            {
                prop.SetInt(ztestID, (int)UnityEngine.Rendering.CompareFunction.LessEqual);
            }
            render.SetPropertyBlock(prop);
        }

        protected override void UpdateGeometry()
        {
            if (m_UiMode == ERichTextMode.ERTM_UI)
            {
                base.UpdateGeometry();
            }
            else if (m_UiMode == ERichTextMode.ERTM_MergeText)
            {
                if (m_mesh == null)
                {
                    m_mesh = new Mesh();
                    m_mesh.MarkDynamic();
                    m_mesh.hideFlags = MeshHideflags;
                }

                DoMeshGeneration3D();

                var render = transform.GetComponentInParent<RichTextRender>();
                if (render)
                {
                    render.MarkDirty();
                }
            }
        }

        private void DoMeshGeneration3D()
        {
            if (rectTransform != null && rectTransform.rect.width >= 0 && rectTransform.rect.height >= 0)
            {
                OnPopulateMesh3D(s_VertexHelper);
            }
            else
            {
                s_VertexHelper.Clear(); // clear the vertex helper so invalid graphics dont draw.
            }

            var components = ListPool<Component>.Get();
            GetComponents(typeof(IMeshModifier), components);

            for (var i = 0; i < components.Count; i++)
            {
                ((IMeshModifier)components[i]).ModifyMesh(s_VertexHelper);
            }

            ListPool<Component>.Release(components);

            s_VertexHelper.FillMesh(m_mesh);
        }

        private void setSpriteVertex(ref UIVertex v, Vector3 position, Vector2 uv0)
        {
            v.position = position;
            v.uv0 = uv0;
            v.uv1 = new Vector2(0, 1.0f);
            v.color = this.color.linear;
        }

        protected override void OnEnable()
        {
            //ChangeZTest();

            base.OnEnable();
        }

        private void GenerateSimpleSprite(VertexHelper toFill)
        {
            var tagSize = rectTransform.sizeDelta;

            var texture = sprite.texture;
            var textureWidthInv = 1.0f / texture.width;
            var textureHeightInv = 1.0f / texture.height;
            var uvRect = sprite.textureRect;
            uvRect = new Rect(uvRect.x * textureWidthInv, uvRect.y * textureHeightInv, uvRect.width * textureWidthInv, uvRect.height * textureHeightInv);

            UIVertex[] v = new UIVertex[4];
            for (int i = 0; i < 4; ++i)
            {
                v[i] = new UIVertex();
            }

            Vector3 orgPos = Vector3.zero;
            orgPos.x -= rectTransform.pivot.x * tagSize.x;
            orgPos.y -= rectTransform.pivot.y * tagSize.y;

            // pos = (0, 0)
            var position = orgPos;
            var uv0 = new Vector2(uvRect.x, uvRect.y);
            setSpriteVertex(ref v[0], position, uv0);

            // pos = (1, 0)
            position = new Vector3(tagSize.x * fillAmount, 0, 0) + orgPos;
            uv0 = new Vector2(uvRect.x + uvRect.width * fillAmount, uvRect.y);
            setSpriteVertex(ref v[1], position, uv0);

            // pos = (1, 1)
            position = new Vector3(tagSize.x * fillAmount, tagSize.y, 0) + orgPos;
            uv0 = new Vector2(uvRect.x + uvRect.width * fillAmount, uvRect.y + uvRect.height);
            setSpriteVertex(ref v[2], position, uv0);

            // pos = (0, 1)
            position = new Vector3(0, tagSize.y, 0) + orgPos;
            uv0 = new Vector2(uvRect.x, uvRect.y + uvRect.height);
            setSpriteVertex(ref v[3], position, uv0);

            toFill.AddUIVertexQuad(v);
        }

        static readonly Vector2[] s_VertScratch = new Vector2[4];
        static readonly Vector2[] s_UVScratch = new Vector2[4];

        private void addSpriteQuad(VertexHelper toFill, int startIndex, Vector2 posMin, Vector2 posMax, Vector2 uvMin, Vector2 uvMax)
        {
            UIVertex[] v = new UIVertex[4];
            for (int i = 0; i < 4; ++i)
            {
                v[i] = new UIVertex();
            }

            setSpriteVertex(ref v[0], new Vector3(posMin.x, posMin.y, 0), new Vector2(uvMin.x, uvMin.y));
            setSpriteVertex(ref v[1], new Vector3(posMin.x, posMax.y, 0), new Vector2(uvMin.x, uvMax.y));
            setSpriteVertex(ref v[2], new Vector3(posMax.x, posMax.y, 0), new Vector2(uvMax.x, uvMax.y));
            setSpriteVertex(ref v[3], new Vector3(posMax.x, posMin.y, 0), new Vector2(uvMax.x, uvMin.y));

            toFill.AddUIVertexQuad(v);
        }

        private void GenerateSlicedSprite(VertexHelper toFill)
        {
            var tagSize = rectTransform.sizeDelta;

            Vector4 border = sprite.border;
            Vector4 adjustedBorders = border;

            Vector4 outer, inner;

            outer = Sprites.DataUtility.GetOuterUV(sprite);
            inner = Sprites.DataUtility.GetInnerUV(sprite);

            Vector2 orgPos = Vector2.zero;
            orgPos.x -= rectTransform.pivot.x * tagSize.x;
            orgPos.y -= rectTransform.pivot.y * tagSize.y;

            var length = Mathf.Max(tagSize.x * fillAmount - adjustedBorders.z, 0);
            s_VertScratch[0] = orgPos;
            s_VertScratch[3] = new Vector2(tagSize.x * fillAmount, tagSize.y) + orgPos;

            s_VertScratch[1].x = Mathf.Min(adjustedBorders.x, length);
            s_VertScratch[1].y = adjustedBorders.y;
            s_VertScratch[1] += orgPos;

            s_VertScratch[2].x = length;
            s_VertScratch[2].y = tagSize.y - adjustedBorders.w;
            s_VertScratch[2] += orgPos;

            s_UVScratch[0] = new Vector2(outer.x, outer.y);
            s_UVScratch[1] = new Vector2(inner.x, inner.y);
            s_UVScratch[2] = new Vector2(inner.z, inner.w);
            s_UVScratch[3] = new Vector2(outer.z, outer.w);

            int vertexCount = 0;
            for (int x = 0; x < 3; ++x)
            {
                int x2 = x + 1;

                for (int y = 0; y < 3; ++y)
                {
                    int y2 = y + 1;

                    addSpriteQuad(toFill,
                        vertexCount,
                        new Vector2(s_VertScratch[x].x, s_VertScratch[y].y),
                        new Vector2(s_VertScratch[x2].x, s_VertScratch[y2].y),
                        new Vector2(s_UVScratch[x].x, s_UVScratch[y].y),
                        new Vector2(s_UVScratch[x2].x, s_UVScratch[y2].y));

                    vertexCount += 4;
                }
            }
        }

        protected void OnPopulateMesh3D(VertexHelper toFill)
        {
            toFill.Clear();

            if (overrideSprite == null)
            {
                return;
            }

            switch (type)
            {
                case Type.Simple:
                    GenerateSimpleSprite(toFill);
                    break;
                case Type.Sliced:
                    GenerateSlicedSprite(toFill);
                    break;
            }
        }

        private void LateUpdate()
        {
            if (rectTransform.hasChanged)
            {
                rectTransform.hasChanged = false;

                if (m_UiMode == ERichTextMode.ERTM_MergeText)
                {
                    var lastPosition = transform.localPosition;
                    var lastRotation = transform.localRotation;
                    var lastScale = transform.localScale;
                    if (m_lastPosition != lastPosition || m_lastRotation != lastRotation || m_lastScale != lastScale)
                    {
                        m_lastPosition = lastPosition;
                        m_lastRotation = lastRotation;
                        m_lastScale = lastScale;
                        SetVerticesDirty();
                    }
                }
                else
                {
                    SetVerticesDirty();
                }
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (m_UiMode == ERichTextMode.ERTM_MergeText)
            {
                var render = transform.GetComponentInParent<RichTextRender>();
                if (render)
                {
                    render.MarkDirty();
                }
            }
        }
    }
}