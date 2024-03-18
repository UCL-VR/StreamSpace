using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Ubiq.Messaging;
using UnityEngine;
using System.Collections.Specialized;

using Ubiq.Samples.Social;
using Ubiq.Rooms; 

public class ScreenManager : MonoBehaviour
{
    private NetworkContext context;
    public GameObject screenPrefab;
    [HideInInspector]
    public  OrderedDictionary screens = new OrderedDictionary();
    public  Dictionary<string,string> machine_tracks = new Dictionary<string, string>();
    public Dictionary<string, Color> machine_color = new Dictionary<string, Color>();
    
    public Layout layout;
    public Dictionary<string, string> machine_name = new Dictionary<string, string>();
    
    private ColorGenerator colorGenerator;
    public RoomClient roomClient;
    public string localMachineId;
    public bool somethingIsChanged = false;
    private bool updateCoroutineIsRunning = false;
    

    public NetworkId Id => new NetworkId("e12e9c92-30a45567");

    private struct NewScreenMessage
    {
        public string action;
        public string id; // screen ID
        public string mid; // machine ID
        public string name; // machine name
        public bool isprivate;
    }

    // Start is called before the first frame update
    void Start()
    {
        context = NetworkScene.Register(this, Id);
        colorGenerator = GetComponent<ColorGenerator>();
    }
    private void Awake()
    {
        roomClient = GetComponentInParent<RoomClient>();
    }

    public IEnumerator UpdateAllScreens()
    {
        bool allReady;
        updateCoroutineIsRunning = true;
        do
        {
            allReady = true;

            for (int i = 0; i < screens.Count; i++)
            {
                string key = screens.Cast<DictionaryEntry>().ElementAt(i).Key as string;
                Screen s = screens[key] as Screen;

                // Check if the screenQuad GameObject is ready
                if (s.screenQuad == null)
                {
                    allReady = false;
                    break; // Exit the loop if any GameObject is not ready
                }

                // Check if the texture on the screenQuad is ready
                Renderer renderer = s.screenQuad.GetComponent<Renderer>();
                if (renderer == null || renderer.material == null || renderer.material.mainTexture == null)
                {
                    allReady = false;
                    break; // Exit the loop if any texture is not ready
                }
            }

            yield return null; // Wait for the next frame
        }
        while (!allReady);

        for (int i = 0; i < screens.Count; i++)
        {
            string key = screens.Cast<DictionaryEntry>().ElementAt(i).Key as string;
            Screen s = screens[key] as Screen;
            s.UpdateScreen(machine_color[s.MachineId]);
            /*if (s.GetComponent<Outline>() == null)
            {
                s.gameObject.AddComponent<Outline>();
            }
            Outline outline = s.gameObject.GetComponent<Outline>();
            outline.OutlineMode = Outline.Mode.OutlineAll;
            outline.OutlineColor = machine_color[s.MachineId];
            outline.OutlineWidth = 10f;*/
        }

        updateCoroutineIsRunning = false;
    }

    void CreateScreen(string networkId, string trackId, string machineId, bool isPrivate, string machineName)
    {
        // Check if we already have a screen for this track
        if (screens.Contains(trackId))
        {
            Debug.Log("Screen already exists for track " + trackId);
            return;
        }

        // Instantiate a new screen with prefab
        GameObject screen = Instantiate(screenPrefab);

        // Cast to Screen class
        Screen screenComponent = screen.GetComponent<Screen>();

        // Set the network id
        screenComponent.NetworkId = new NetworkId(networkId);
        screenComponent.TrackId = trackId;
        screenComponent.MachineId = machineId;
        screenComponent.screenManager = this;
        screenComponent.isPrivate = isPrivate;
        screenComponent.MachineName = machineName;

        // Add to a dictionary of screens
        screens.Add(screenComponent.TrackId, screenComponent);
        
        machine_tracks.Add(trackId, machineId);
        if (!machine_color.ContainsKey(machineId))
        {
            Color color = colorGenerator.GenerateColor();
            machine_color.Add(machineId, color);
        }

        StartCoroutine(UpdateAllScreens());

    }

    

    public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
    {
        var m = message.FromJson<NewScreenMessage>();
        string trackId = m.id;
        string machineId = m.mid;
        bool isPrivate = m.isprivate;
        string machineName = m.name;

        Debug.Log("Received message with action " + m.action + " for screen " + trackId + " from machine " + machineId + " with name " + machineName + " and isPrivate " + isPrivate);

        if (m.action == "add")
        {
            Debug.Log("Adding screen " + trackId);
            string part1 = trackId.Substring(0, 8);
            string part2 = trackId.Substring(8, 8);
            string networkId = $"{part1}-{part2}";
            CreateScreen(networkId, trackId, machineId, isPrivate, machineName);
        }
        else if (m.action == "remove")
        {
            Debug.Log("Removing screen " + trackId);
            var s = screens[trackId] as Screen;
            Destroy(s.gameObject);
            screens.Remove(trackId);
        }
        else if (m.action == "privacy")
        {
            var s = screens[trackId] as Screen;
            s.isPrivate = isPrivate;
            s.UpdateVisibility();
        }
    }


    private void Update()
    {
        if (somethingIsChanged && !updateCoroutineIsRunning)
        {
            somethingIsChanged = false;
            StartCoroutine(UpdateAllScreens());
        }
    }
}
