using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class CloudManager : MonoBehaviour
{
    [Header("Target / Infini")]
    [Tooltip("La cible autour de laquelle les nuages restent infinis. Si vide, le script prend Camera.main.")]
    public Transform followTarget;

    [Tooltip("Taille de la zone de nuages autour de la cible, en X/Z.")]
    public Vector2 areaSize = new Vector2(500f, 500f);

    [Header("Nuages")]
    [Range(1, 500)]
    public int cloudCount = 90;

    [Tooltip("Nombre de textures proc�durales diff�rentes g�n�r�es au lancement.")]
    [Range(1, 32)]
    public int textureVariants = 10;

    [Tooltip("Hauteur des nuages. Si altitudeRelativeToTarget est false, c'est une hauteur monde.")]
    public Vector2 altitudeRange = new Vector2(55f, 95f);

    [Tooltip("Active �a si tu veux que les nuages restent au-dessus du joueur/cam�ra en Y.")]
    public bool altitudeRelativeToTarget = false;

    [Tooltip("Taille g�n�rale des planes.")]
    public Vector2 sizeRange = new Vector2(25f, 75f);

    [Tooltip("�tirement al�atoire des nuages.")]
    public Vector2 stretchRange = new Vector2(0.8f, 1.8f);

    [Header("Mouvement")]
    [Tooltip("Direction du vent en X/Z.")]
    public Vector2 windDirectionXZ = new Vector2(1f, 0.25f);

    [Tooltip("Vitesse des nuages.")]
    public Vector2 speedRange = new Vector2(2f, 7f);

    [Tooltip("Rotation lente al�atoire des nuages.")]
    public Vector2 spinSpeedRange = new Vector2(-3f, 3f);

    [Tooltip("Petit mouvement vertical pour �viter que ce soit trop statique.")]
    public float verticalWobble = 1.2f;

    public Vector2 wobbleFrequencyRange = new Vector2(0.08f, 0.18f);

    [Header("Texture proc�durale")]
    [Tooltip("0 = seed al�atoire � chaque play. Sinon, m�me seed = m�mes nuages.")]
    public int seed = 12345;

    [Range(32, 1024)]
    public int textureSize = 256;

    [Range(0.5f, 8f)]
    public float noiseScale = 2.7f;

    [Range(1, 6)]
    public int noiseOctaves = 4;

    [Tooltip("Plus bas = nuages plus pleins. Plus haut = nuages plus trou�s.")]
    [Range(0.25f, 0.75f)]
    public float cloudDensity = 0.46f;

    [Tooltip("Douceur du contour des nuages.")]
    [Range(0.02f, 0.5f)]
    public float edgeSoftness = 0.2f;

    [Header("Couleur")]
    public Color cloudTint = new Color(1f, 1f, 1f, 0.82f);

    private readonly List<CloudRuntime> clouds = new List<CloudRuntime>();
    private readonly List<Material> generatedMaterials = new List<Material>();
    private readonly List<Texture2D> generatedTextures = new List<Texture2D>();

    private Mesh cloudMesh;
    private Pcg32 rng;

    private class CloudRuntime
    {
        public Transform transform;
        public float height;
        public float speed;
        public float spinSpeed;
        public float wobblePhase;
        public float wobbleFrequency;
    }

    private void Start()
    {
        GenerateClouds();
    }

    private void Update()
    {
        if (clouds.Count == 0)
            return;

        Vector3 center = GetCenter();
        Vector3 wind = GetWindDirection();
        float dt = Time.deltaTime;
        float time = Time.time;

        for (int i = 0; i < clouds.Count; i++)
        {
            CloudRuntime cloud = clouds[i];

            if (cloud.transform == null)
                continue;

            Vector3 pos = cloud.transform.position;

            pos += wind * cloud.speed * dt;

            pos.x = WrapAxis(pos.x, center.x, areaSize.x);
            pos.z = WrapAxis(pos.z, center.z, areaSize.y);

            float baseY = altitudeRelativeToTarget ? center.y + cloud.height : cloud.height;
            pos.y = baseY + Mathf.Sin(time * cloud.wobbleFrequency + cloud.wobblePhase) * verticalWobble;

            cloud.transform.position = pos;

            cloud.transform.Rotate(0f, cloud.spinSpeed * dt, 0f, Space.Self);
        }
    }

    [ContextMenu("Regenerate Clouds")]
    public void GenerateClouds()
    {
        ClearClouds();

        if (followTarget == null && Camera.main != null)
            followTarget = Camera.main.transform;

        rng = new Pcg32();

        int textureSeedBase;

        if (seed == 0)
        {
            textureSeedBase = (int)(rng.NextUInt() & 0x7FFFFFFF);
        }
        else
        {
            rng.Seed((ulong)seed, (ulong)seed * 747796405UL);
            textureSeedBase = seed;
        }

        cloudMesh = CreateCloudQuadMesh();

        for (int i = 0; i < textureVariants; i++)
        {
            Texture2D tex =
                GenerateProceduralCloudTexture(textureSeedBase + i * 9973);

            generatedTextures.Add(tex);

            Material mat = CreateCloudMaterial(tex);
            generatedMaterials.Add(mat);
        }

        for (int i = 0; i < cloudCount; i++)
        {
            SpawnCloud(i);
        }
    }

    private void SpawnCloud(int index)
    {
        GameObject go = new GameObject("Cloud_" + index.ToString("000"));
        go.transform.SetParent(transform, true);

        MeshFilter meshFilter = go.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = cloudMesh;

        MeshRenderer meshRenderer = go.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = generatedMaterials[RandomInt(0, generatedMaterials.Count)];
        meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;

        CloudRuntime cloud = new CloudRuntime();
        cloud.transform = go.transform;
        cloud.height = RandomRange(altitudeRange.x, altitudeRange.y);
        cloud.speed = RandomRange(speedRange.x, speedRange.y);
        cloud.spinSpeed = RandomRange(spinSpeedRange.x, spinSpeedRange.y);
        cloud.wobblePhase = RandomRange(0f, Mathf.PI * 2f);
        cloud.wobbleFrequency = RandomRange(wobbleFrequencyRange.x, wobbleFrequencyRange.y);

        float size = RandomRange(sizeRange.x, sizeRange.y);
        float stretch = RandomRange(stretchRange.x, stretchRange.y);

        go.transform.localScale = new Vector3(size * stretch, 1f, size);
        go.transform.rotation = Quaternion.Euler(0f, RandomRange(0f, 360f), 0f);
        go.transform.position = GetRandomCloudPosition(cloud.height);

        clouds.Add(cloud);
    }

    private Vector3 GetRandomCloudPosition(float height)
    {
        Vector3 center = GetCenter();

        float x = center.x + RandomRange(-areaSize.x * 0.5f, areaSize.x * 0.5f);
        float z = center.z + RandomRange(-areaSize.y * 0.5f, areaSize.y * 0.5f);
        float y = altitudeRelativeToTarget ? center.y + height : height;

        return new Vector3(x, y, z);
    }

    private Vector3 GetCenter()
    {
        if (followTarget != null)
            return followTarget.position;

        return transform.position;
    }

    private Vector3 GetWindDirection()
    {
        Vector3 dir = new Vector3(windDirectionXZ.x, 0f, windDirectionXZ.y);

        if (dir.sqrMagnitude < 0.0001f)
            return Vector3.zero;

        return dir.normalized;
    }

    private static float WrapAxis(float value, float center, float size)
    {
        if (size <= 0.01f)
            return value;

        float min = center - size * 0.5f;
        return Mathf.Repeat(value - min, size) + min;
    }

    private Mesh CreateCloudQuadMesh()
    {
        Mesh mesh = new Mesh();
        mesh.name = "Generated_Cloud_Quad";

        Vector3[] vertices =
        {
            new Vector3(-0.5f, 0f, -0.5f),
            new Vector3( 0.5f, 0f, -0.5f),
            new Vector3(-0.5f, 0f,  0.5f),
            new Vector3( 0.5f, 0f,  0.5f)
        };

        Vector2[] uvs =
        {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f)
        };

        int[] triangles =
        {
            0, 2, 1,
            2, 3, 1,

            0, 1, 2,
            2, 1, 3
        };

        Vector3[] normals =
        {
            Vector3.up,
            Vector3.up,
            Vector3.up,
            Vector3.up
        };

        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.normals = normals;
        mesh.RecalculateBounds();

        return mesh;
    }

    private Texture2D GenerateProceduralCloudTexture(int textureSeed)
    {
        int size = Mathf.Clamp(textureSize, 32, 1024);

        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.name = "Generated_Cloud_Texture_" + textureSeed;
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        Color[] pixels = new Color[size * size];

        Pcg32 localRng = new Pcg32();
        localRng.Seed((ulong)textureSeed, (ulong)textureSeed * 747796405UL);
        float offsetX = RandomRange(localRng, -10000f, 10000f);
        float offsetY = RandomRange(localRng, -10000f, 10000f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = (x + 0.5f) / size;
                float v = (y + 0.5f) / size;

                float centeredX = u * 2f - 1f;
                float centeredY = v * 2f - 1f;

                float dist = Mathf.Sqrt(centeredX * centeredX + centeredY * centeredY);

                float radialFade = 1f - Mathf.SmoothStep(0.35f, 1f, dist);

                float noise = FractalPerlin(
                    u * noiseScale + offsetX,
                    v * noiseScale + offsetY,
                    noiseOctaves
                );

                float threshold = Mathf.Lerp(0.78f, cloudDensity, radialFade);

                float alpha = Mathf.SmoothStep(threshold, threshold + edgeSoftness, noise);
                alpha *= radialFade;

                alpha = Mathf.Pow(alpha, 1.15f);

                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply(false, true);

        return tex;
    }

    private float FractalPerlin(float x, float y, int octaves)
    {
        float value = 0f;
        float amplitude = 1f;
        float frequency = 1f;
        float amplitudeSum = 0f;

        int safeOctaves = Mathf.Clamp(octaves, 1, 6);

        for (int i = 0; i < safeOctaves; i++)
        {
            value += Mathf.PerlinNoise(x * frequency, y * frequency) * amplitude;
            amplitudeSum += amplitude;

            amplitude *= 0.5f;
            frequency *= 2f;
        }

        if (amplitudeSum <= 0f)
            return 0f;

        return value / amplitudeSum;
    }

    private Material CreateCloudMaterial(Texture2D texture)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");

        if (shader == null)
            shader = Shader.Find("Unlit/Transparent");

        if (shader == null)
            shader = Shader.Find("Sprites/Default");

        Material mat = new Material(shader);
        mat.name = "Generated_Cloud_Material";

        if (mat.HasProperty("_BaseMap"))
            mat.SetTexture("_BaseMap", texture);

        if (mat.HasProperty("_MainTex"))
            mat.SetTexture("_MainTex", texture);

        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", cloudTint);

        if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", cloudTint);

        if (mat.HasProperty("_Surface"))
            mat.SetFloat("_Surface", 1f);

        if (mat.HasProperty("_Blend"))
            mat.SetFloat("_Blend", 0f);

        if (mat.HasProperty("_AlphaClip"))
            mat.SetFloat("_AlphaClip", 0f);

        if (mat.HasProperty("_SrcBlend"))
            mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);

        if (mat.HasProperty("_DstBlend"))
            mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);

        if (mat.HasProperty("_ZWrite"))
            mat.SetFloat("_ZWrite", 0f);

        if (mat.HasProperty("_Cull"))
            mat.SetFloat("_Cull", (float)CullMode.Off);

        mat.SetOverrideTag("RenderType", "Transparent");
        mat.renderQueue = (int)RenderQueue.Transparent;

        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");

        return mat;
    }

    private float RandomRange(float min, float max)
    {
        if (max < min)
        {
            (min, max) = (max, min);
        }

        return min + rng.NextFloat() * (max - min);
    }

    private static float RandomRange(Pcg32 random, float min, float max)
    {
        if (max < min)
        {
            (min, max) = (max, min);
        }

        return min + random.NextFloat() * (max - min);
    }

    private int RandomInt(int minInclusive, int maxExclusive)
    {
        return rng.Range(minInclusive, maxExclusive);
    }

    private void ClearClouds()
    {
        for (int i = clouds.Count - 1; i >= 0; i--)
        {
            if (clouds[i] != null && clouds[i].transform != null)
                SafeDestroy(clouds[i].transform.gameObject);
        }

        clouds.Clear();

        for (int i = 0; i < generatedMaterials.Count; i++)
            SafeDestroy(generatedMaterials[i]);

        generatedMaterials.Clear();

        for (int i = 0; i < generatedTextures.Count; i++)
            SafeDestroy(generatedTextures[i]);

        generatedTextures.Clear();

        if (cloudMesh != null)
            SafeDestroy(cloudMesh);

        cloudMesh = null;
    }

    private void SafeDestroy(Object obj)
    {
        if (obj == null)
            return;

        if (Application.isPlaying)
            Destroy(obj);
        else
            DestroyImmediate(obj);
    }

    private void OnDestroy()
    {
        ClearClouds();
    }

    private void OnValidate()
    {
        cloudCount = Mathf.Max(1, cloudCount);
        textureVariants = Mathf.Clamp(textureVariants, 1, 32);
        textureSize = Mathf.Clamp(textureSize, 32, 1024);

        areaSize.x = Mathf.Max(20f, areaSize.x);
        areaSize.y = Mathf.Max(20f, areaSize.y);

        if (altitudeRange.y < altitudeRange.x)
            altitudeRange.y = altitudeRange.x;

        if (sizeRange.y < sizeRange.x)
            sizeRange.y = sizeRange.x;

        if (stretchRange.y < stretchRange.x)
            stretchRange.y = stretchRange.x;

        if (speedRange.y < speedRange.x)
            speedRange.y = speedRange.x;

        if (wobbleFrequencyRange.y < wobbleFrequencyRange.x)
            wobbleFrequencyRange.y = wobbleFrequencyRange.x;
    }
}
