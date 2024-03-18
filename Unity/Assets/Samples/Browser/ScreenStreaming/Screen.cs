using System;
using System.Collections.Generic;
using System.Linq;
using Ubiq.Messaging;
using Ubiq.XR;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Screen : MonoBehaviour, IGraspable
{
    public NetworkId NetworkId { get; set; }
    public string TrackId { get; set; }
    public string MachineId { get; set; }
    public string MachineName { get; set; }
    private NetworkContext context;
    private Hand follow;
    public bool isPrivate;

    public GameObject screenQuad;
    public GameObject screenFrame;
    public bool owner;
    Hand attached;
    GameObject screensContainer;

    public Vector2 texturePos;
    float widthInPixels;
    float heightInPixels;
    //cylindrical screen properties
    public ScreenManager screenManager;
    public float divisionsPerDegree = 0.1f;  // Divisions per degree (e.g., 0.1 for one division every 10 degrees)


    private float lastRadius;
    private float lastArcHeight;
    private float lastArcAngle;
    private float lastWidthTexture;
    private float lastHeightTexture;
    private float lastDivisionsPerDegree;

    public string id;
    public float width;
    public float height;
    public Texture2D Tex;
    public string imageData;
    public float X;
    public float Y;

    public Screen(float w, float h, string sid)
    {
        width = w;
        height = h;
        id = sid;
    }

    public Layout layout;
    private int _resolution;

    public Screen(NetworkId networkId)
    {
        NetworkId = networkId;
    }

    private void Awake()
    {
    }

    // Start is called before the first frame update
    void Start()
    {
        Register();

        screensContainer = GameObject.Find("Screens");
        this.transform.parent = screensContainer.transform;

        layout = screenManager.gameObject.GetComponent<Layout>();

    }

    public void Register()
    {
        context = NetworkScene.Register(this);
        Debug.Log("Registered screen with id " + NetworkId);
    }

    public struct Message
    {
        public TransformMessage transform;

        public Message(Transform transform)
        {
            this.transform = new TransformMessage(transform);
        }
    }

    public void UpdateScreen(Color color)
    {

        Texture tex = screenQuad.GetComponent<Renderer>().material.mainTexture;


        widthInPixels = tex.width;
        heightInPixels = tex.height;
        Debug.Log(this.TrackId + " widthInPixels: " + widthInPixels + " heightInPixels: " + heightInPixels);

        //place
        if (layout.RefreshPlacement() == false) return; //wait until all the screen are ready

        //generate Mesh
        Mesh mesh = GenerateArcMesh(xmax(), xmin(), ymax(), ymin());

        MeshFilter meshFilter = screenQuad.GetComponent<MeshFilter>();
        if (meshFilter)
        {
            meshFilter.mesh = mesh;
        }

        //set local scale to 1
        screenQuad.transform.localScale = Vector3.one;
        screenQuad.transform.parent.localPosition = Vector3.zero;

        //frame mesh
        Mesh frameMesh = GenerateFrameMesh(xmax(), xmin(), ymax(), ymin(), 0.1f);
        MeshFilter meshFrame = screenFrame.GetComponent<MeshFilter>();
        if (meshFrame)
        {
            meshFrame.mesh = frameMesh;
        }
        //set local scale to 1
        screenFrame.transform.localScale = Vector3.one;
        screenFrame.transform.parent.localPosition = Vector3.zero;

        if (screenFrame.GetComponent<Renderer>().material == null)
        {
            Material newMaterial = new Material(Shader.Find("Standard"));
            newMaterial.color = color;
            screenFrame.GetComponent<Renderer>().material = newMaterial;
        }
        screenFrame.GetComponent<Renderer>().material.color = color;
    }


    // Update is called once per frame
    private void Update()
    {
        // Search for GameObject with name "Screen-{TrackId}" and assign to screenQuad
        if (screenQuad == null)
        {
            screenQuad = GameObject.Find($"Screen-{TrackId}");

            if (screenQuad != null)
            {
                // Move screenQuad to be a child of this GameObject
                screenQuad.transform.parent = this.transform;

                // Set the position and rotation of the screenQuad to be the same as this GameObject
                screenQuad.transform.localPosition = Vector3.zero;
                screenQuad.transform.localRotation = Quaternion.identity;

                screenFrame = GameObject.CreatePrimitive(PrimitiveType.Quad);
                screenFrame.transform.parent = this.transform;
                screenFrame.transform.localPosition = Vector3.zero;
                screenFrame.transform.localRotation = Quaternion.identity;
            }
        }
        else
        {
            // Update the screenQuad if it is available
            screenQuad.transform.localScale = Vector3.one;
            screenFrame.transform.localScale = Vector3.one;
        }

        if (follow != null)
        {
            transform.position = follow.transform.position;
            transform.rotation = follow.transform.rotation;
            context.SendJson(new Message(transform));
        }

        try
        {
            Texture tex = screenQuad.GetComponent<Renderer>().material.mainTexture;
            if (layout.HasChanged() || tex.width != lastWidthTexture || tex.height != lastHeightTexture || lastDivisionsPerDegree != divisionsPerDegree)
            {
                Debug.Log("Changing Texture " + this.TrackId);
                screenManager.somethingIsChanged = true;
                // Update last known values
                layout.UpdateValues();

                lastWidthTexture = widthInPixels;
                lastHeightTexture = heightInPixels;
                lastDivisionsPerDegree = divisionsPerDegree;
            }

        }
        catch
        {

        }

    }

    public void Grasp(Hand controller)
    {
        follow = controller;
    }

    public void Release(Hand controller)
    {
        follow = null;
    }

    public void Dispose()
    {
        if (screenQuad != null)
            Destroy(screenQuad);

        if (screenFrame != null)
            Destroy(screenFrame);
    }

    #region Boundaries
    public float Width()
    {
        return 0.003f * widthInPixels; //global scale
    }

    public float Height()
    {
        return 0.003f * heightInPixels;
    }

    public float NormalizeX(float x)
    {
        return 2 * (x / Layout.spaceWidth) - 1;
    }

    public float NormalizeY(float y)
    {
        return 2 * (y / Layout.spaceHeight) - 1;
    }
    
    float NormXToLayout(float x)
    {
        // Normalize and scale the x coordinate within the layout's angle range
        return Mathf.Lerp(-layout.Angle / 2, layout.Angle / 2, (x + 1) / 2);
    }

    float NormYToLayout(float y)
    {
        // Normalize and scale the y coordinate within the layout's height range
        return Mathf.Lerp(0, layout.Height, (y + 1) / 2);
    }

    public float xmax()
    {
        return NormalizeX(texturePos[0] + Width());
    }

    public float xmin()
    {
        return NormalizeX(texturePos[0]);
    }

    public float ymax()
    {
        return NormalizeY(texturePos[1] + Height());
    }

    public float ymin()
    {
        return NormalizeY(texturePos[1]);
    }

    #endregion

    #region MeshGeneration
    Mesh GenerateFrameMesh(float xmax, float xmin, float ymax, float ymin, float frameThickness)
    {
        // Generate each frame side mesh using the scalar values directly
        Mesh topFrame = GenerateTopFrameMesh(xmin, xmax, ymax, frameThickness);
        Mesh bottomFrame = GenerateBottomFrameMesh(xmin, xmax, ymin, frameThickness);
        Mesh leftFrame = GenerateLeftFrameMesh(xmin, ymax, ymin, frameThickness);
        Mesh rightFrame = GenerateRightFrameMesh(xmax, ymax, ymin, frameThickness);

        // Combine the four frame meshes into one frame mesh
        Mesh frameMesh = new Mesh();
        CombineInstance[] combine = new CombineInstance[4];

        combine[0].mesh = topFrame;
        combine[0].transform = Matrix4x4.identity;

        combine[1].mesh = bottomFrame;
        combine[1].transform = Matrix4x4.identity;

        combine[2].mesh = leftFrame;
        combine[2].transform = Matrix4x4.identity;

        combine[3].mesh = rightFrame;
        combine[3].transform = Matrix4x4.identity;

        frameMesh.CombineMeshes(combine, true, false);
        frameMesh.RecalculateNormals();

        return frameMesh;
    }

    Mesh GenerateTopFrameMesh(float xmin, float xmax, float ymax, float frameThickness)
    {
        // Normalize the scalar values directly
        float normalizedXmin = NormXToLayout(xmin);
        float normalizedXmax = NormXToLayout(xmax);
        float normalizedYmax = NormYToLayout(ymax);

        int resolution = Mathf.CeilToInt(Mathf.Abs(normalizedXmax - normalizedXmin) * divisionsPerDegree);

        Mesh mesh = new Mesh();
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        // Generate the top edge vertices
        foreach (float u in Interpolator(normalizedXmin, normalizedXmax, resolution))
        {
            Vector3 vertex = layout.CoordsUVToWorld(u, normalizedYmax);
            vertices.Add(vertex);

            Vector3 extrudedVertex = new Vector3(vertex.x, vertex.y + frameThickness, vertex.z);
            vertices.Add(extrudedVertex);
        }

        var triangleIndices = GenerateTriangleIndexes(resolution);
        triangles.AddRange(triangleIndices);

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();

        return mesh;
    }

    Mesh GenerateBottomFrameMesh(float xmin, float xmax, float ymin, float frameThickness)
    {
        // Normalize the scalar values directly
        float normalizedXmin = NormXToLayout(xmin);
        float normalizedXmax = NormXToLayout(xmax);
        float normalizedYmin = NormYToLayout(ymin);

        int resolution = Mathf.CeilToInt(Mathf.Abs(normalizedXmax - normalizedXmin) * divisionsPerDegree);

        Mesh mesh = new Mesh();
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        // Generate the bottom edge vertices
        foreach (float u in Interpolator(normalizedXmin, normalizedXmax, resolution))
        {
            Vector3 vertex = layout.CoordsUVToWorld(u, normalizedYmin);
            vertices.Add(vertex);

            Vector3 extrudedVertex = new Vector3(vertex.x, vertex.y - frameThickness, vertex.z);
            vertices.Add(extrudedVertex);
        }

        var triangleIndices = GenerateTriangleIndexes(resolution);
        triangles.AddRange(triangleIndices);

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();

        return mesh;
    }

    Mesh GenerateLeftFrameMesh(float xmin, float ymax, float ymin, float frameThickness)
    {
        // Normalize the scalar values directly
        float normalizedXmin = NormXToLayout(xmin);
        float normalizedYmax = NormYToLayout(ymax);
        float normalizedYmin = NormYToLayout(ymin);

        int resolution = 3; // Define the resolution for the vertical line

        Mesh mesh = new Mesh();
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        // Iterate to create the thickness of the frame
        for (int i = 0; i < resolution; i++)
        {
            float offset = (float)i / (resolution - 1) * frameThickness; // Calculate the offset for the thickness
            float u = normalizedXmin - offset; // Apply the offset to the xmin value

            // Create the top and bottom vertices for the current segment
            Vector3 vertexTop = layout.CoordsUVToWorld(u, normalizedYmax);
            Vector3 vertexBottom = layout.CoordsUVToWorld(u, normalizedYmin);
            vertices.Add(vertexTop);
            vertices.Add(vertexBottom);
        }

        // Generate the triangle indices for the frame
        var triangleIndices = GenerateTriangleIndexes(resolution);

        triangles.AddRange(triangleIndices);

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();

        return mesh;
    }

    Mesh GenerateRightFrameMesh(float xmax, float ymax, float ymin, float frameThickness)
    {
        // Normalize the scalar values directly
        float normalizedXmax = NormXToLayout(xmax);
        float normalizedYmax = NormYToLayout(ymax);
        float normalizedYmin = NormYToLayout(ymin);

        int resolution = 3; // Define the resolution for the vertical line

        Mesh mesh = new Mesh();
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        // Since this is the right frame, we'll use xmax for all x-values
        // We iterate over a small range to create the thickness effect
        for (int i = 0; i < resolution; i++)
        {
            float offset = (float)i / (resolution - 1) * frameThickness; // Calculate the offset for the thickness
            float u = normalizedXmax + offset; // Apply the offset to the xmax value

            // Create the top and bottom vertices for the current segment
            Vector3 vertexTop = layout.CoordsUVToWorld(u, normalizedYmax);
            Vector3 vertexBottom = layout.CoordsUVToWorld(u, normalizedYmin);
            vertices.Add(vertexTop);
            vertices.Add(vertexBottom);
        }

        // Generate the triangle indices for the frame
        var triangleIndices = GenerateTriangleIndexes(resolution);
        triangles.AddRange(triangleIndices);

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();

        return mesh;
    }

    Mesh GenerateArcMesh(float xmax, float xmin, float ymax, float ymin)
    {
        // Normalize the scalar values directly
        float normalizedXmax = NormXToLayout(xmax);
        float normalizedXmin = NormXToLayout(xmin);
        float normalizedYmax = NormYToLayout(ymax);
        float normalizedYmin = NormYToLayout(ymin);

        // Calculate resolution based on angular span and divisionsPerDegree
        float angularSpan = Mathf.Abs(normalizedXmax - normalizedXmin); // U values are in degrees
        _resolution = Mathf.CeilToInt(angularSpan * divisionsPerDegree);

        Mesh mesh = new Mesh();
        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> triangles = new List<int>();

        // Use the normalized scalar values directly in the loop
        foreach (float u in Interpolator(normalizedXmin, normalizedXmax, _resolution))
        {
            // Only two points for v (top and bottom)
            for (int j = 0; j < 2; j++)
            {
                float v = (j == 0) ? normalizedYmax : normalizedYmin;
                Vector3 vertex = layout.CoordsUVToWorld(u, v);
                vertices.Add(vertex);

                float normalizedU = (u - normalizedXmin) / (normalizedXmax - normalizedXmin);
                float normalizedV = (j == 0) ? 1.0f : 0.0f; // Top or bottom edge
                uvs.Add(new Vector2(normalizedU, normalizedV));
            }
        }

        var triangleIndices = Enumerable.Range(0, _resolution - 1)
            .SelectMany(i => new List<int>
            {
                i * 2, i * 2 + 1, i * 2 + 2,
                i * 2 + 2, i * 2 + 1, i * 2 + 3
            }).ToList();

        triangles.AddRange(triangleIndices);

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.RecalculateNormals();

        return mesh;
    }

    List<int> GenerateTriangleIndexes(int resolution)
    {

        return Enumerable.Range(0, resolution - 1)
            .SelectMany(i =>
            {
                int topLeft = i * 2;
                int topRight = topLeft + 2;
                int bottomLeft = topLeft + 1;
                int bottomRight = topLeft + 3;

                return new List<int>
                {
                    topLeft, bottomRight, bottomLeft, // First triangle
                    topLeft, topRight, bottomRight    // Second triangle
                };
            }).ToList();

    }

    public IEnumerable<float> Interpolator(float start, float end, int numPoints)
    {
        if (numPoints <= 0)
        {
            throw new ArgumentException("Number of points must be positive.", nameof(numPoints));
        }

        for (int i = 0; i < numPoints; i++)
        {
            float t = i / (float)(numPoints - 1);
            yield return Mathf.Lerp(start, end, t);
        }
    }

    public void UpdateVisibility()
    {
        Renderer renderer = screenQuad.GetComponent<Renderer>();
        Debug.Log("1" + this.TrackId);
        Debug.Log("2" + isPrivate);
        Debug.Log(MachineName);
        Debug.Log("4" + screenManager.roomClient.Me["ubiq.samples.social.name"].ToString().ToLower());

        Debug.Log("UpdateVisibility " + this.TrackId 
                   + " with privacy" + isPrivate 
                   + ". Checking if " 
                   + MachineName.ToLower() 
                   + " == " 
                   + screenManager.roomClient.Me["ubiq.samples.social.name"].ToString().ToLower());
        // If the screen is private and this is not the machine that owns it, turn off visibility
        if (isPrivate && MachineName.ToLower() != screenManager.roomClient.Me["ubiq.samples.social.name"].ToString().ToLower())
        {
            renderer.material.shader = Shader.Find("Custom/BlurShader");
        }
        else
        {
            renderer.material.shader = Shader.Find("Standard");
        }
    }

    public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
    {
        var msg = message.FromJson<Message>();
        transform.localPosition = msg.transform.position; // The Message constructor will take the *local* properties of the passed transform.
        transform.localRotation = msg.transform.rotation;
    }
    #endregion
}
