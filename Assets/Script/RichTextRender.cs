/***
 *              HuaHua
 *              2020-09-25
 *              富文本，一个drawcall 支持文字和图片混排
 **/

using HuaHua;

namespace UnityEngine.UI
{
    [ExecuteInEditMode]
    public class RichTextRender : MonoBehaviour
    {
        [System.NonSerialized]
        private MeshRenderer m_meshRender;
        [System.NonSerialized]
        private MeshFilter m_meshFilter;
        [System.NonSerialized]
        private Mesh m_mesh;

        public bool m_isMajor = false; //是否是主角，主角最后渲染
        private bool m_dirty;

        //生成出来的mesh不显示
        internal static readonly HideFlags MeshHideflags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor | HideFlags.HideInInspector;


        //网格顺序
        struct MeshOrder
        {
            public Mesh mesh;
            public float z;
            public Matrix4x4 matrix;
        }

        public void MarkDirty()
        {
            m_dirty = true;
#if UNITY_EDITOR
            OnCombineMesh();
#endif
        }




        private void OnCombineMesh()
        {
            //包含text，image控件
            //名字板会运行时动态加新图，新字吗？伤害飘字动态出现。是否伤害飘字独立，且加入对象池复用
            //每次只把名字板中子物体active = true 的合并网格
            RichText[] texts = GetComponentsInChildren<RichText>(false);
            RichImage[] images = GetComponentsInChildren<RichImage>(false);
            var meshCount = texts.Length + images.Length;
            if (meshCount == 0)
            {
				if (m_mesh)
                {
                    m_mesh.Clear();
                }
                if (m_meshRender)
                {
                    m_meshRender.enabled = false;
                }
                return;
            }

            //创建mesh
            if (m_meshRender == null)
            {
                m_meshRender = gameObject.GetOrAddComponent<MeshRenderer>();
                m_meshRender.hideFlags = MeshHideflags;

                m_meshFilter = gameObject.GetOrAddComponent<MeshFilter>();
                m_meshFilter.hideFlags = MeshHideflags;

                m_mesh = new Mesh();
                m_mesh.MarkDynamic();
                m_mesh.hideFlags = MeshHideflags;
            }            

            Material material = null;

            var meshes = ListPool<MeshOrder>.Get();

            //父物体，世界控件到局部空间的转换矩阵
            var worldToLocalMatrix = this.transform.worldToLocalMatrix;
            for (int i = 0; i < images.Length; ++i)
            {
                var image = images[i];

                if (!image.IsActive())
                {
                    continue;
                }

                var mesh = image.Mesh();
                if (mesh == null)
                {
                    continue;
                }

                var meshOrder = new MeshOrder();
                meshOrder.mesh = mesh;
                //b的局部空间转世界，再转为a的局部空间。因为b可能不是a的直接子物体，例如a/c/b
                meshOrder.matrix = worldToLocalMatrix * image.transform.localToWorldMatrix;
                meshOrder.z = image.transform.localPosition.z;

                meshes.Add(meshOrder);
                //image与text 是共用一个材质球
                if (material == null)
                {
                    material = image.material;
                }
            }
            for (int j = 0; j < texts.Length; ++j)
            {
                var text = texts[j];

                if (!text.IsActive())
                {
                    continue;
                }

                var mesh = text.Mesh();
                if (mesh == null)
                {
                    continue;
                }

                var meshOrder = new MeshOrder();
                meshOrder.mesh = mesh;
                meshOrder.matrix = worldToLocalMatrix * text.transform.localToWorldMatrix;
                meshOrder.z = text.transform.localPosition.z;

                meshes.Add(meshOrder);

                if (material == null)
                {
                    material = text.material;
                }
            }

            if (meshes.Count == 0)
            {
                if (m_meshRender)
                {
                    m_meshRender.enabled = false;
                }
                return;
            }
            m_meshRender.enabled = true;

            meshes.Sort((lhs, rhs) => rhs.z.CompareTo(lhs.z));
            //合并mesh
            CombineInstance[] combine = new CombineInstance[meshes.Count];
            for (int i = 0; i < meshes.Count; ++i)
            {
                combine[i].mesh = meshes[i].mesh;
                combine[i].transform = meshes[i].matrix;
            }

            ListPool<MeshOrder>.Release(meshes);

            m_mesh.CombineMeshes(combine, true);
            m_meshFilter.sharedMesh = m_mesh;
            m_meshRender.sharedMaterial = material;

            ChangeZTest();
        }

        protected void OnEnable()
        {
            m_dirty = true;
        }

        private void LateUpdate()
        {
            if(m_dirty)
            {
                OnCombineMesh();

                m_dirty = false;
            }
        }


        public void ChangeZTest()
        {
            bool isMajor = m_isMajor;
            string propertyName = "_Ztest";
            int ztestID = Shader.PropertyToID(propertyName);
            var prop = new MaterialPropertyBlock();
            var render = m_meshRender;
            render.GetPropertyBlock(prop);

            //只能设置深度
            if (isMajor == true)
            {
                prop.SetInt(ztestID, (int)UnityEngine.Rendering.CompareFunction.Always);
            }
            else
            {
                prop.SetInt(ztestID, (int)UnityEngine.Rendering.CompareFunction.LessEqual);
            }
            render.SetPropertyBlock(prop);
        }
    }

}