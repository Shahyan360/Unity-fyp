using TMPro;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

/// <summary>Builds an explorable, on-device museum gallery with artifact labels and selectable evidence.</summary>
public static class MuseumGalleryExperience
{
    private static GameObject gallery;
    private static Material navy;
    private static Material limestone;
    private static Material brass;
    private static Material ivory;
    private static Material glass;
    private static readonly Vector3 WorldOrigin = new Vector3(60f, 0f, 60f);

    public static Vector3 EntryPosition => WorldOrigin + new Vector3(0f, 0f, -19.25f);
    // Keep Lumen just ahead and to the side of the entry sightline so the guide
    // accompanies the visitor without obscuring the first row of exhibits.
    public static Vector3 GuidePosition => WorldOrigin + new Vector3(-1.4f, 0f, -15.8f);

    public static void Ensure()
    {
        GameObject existing = GameObject.Find("MuseumOfHumanIdeas_Gallery");
        if (existing != null) { gallery = existing; return; }
        MakeMaterials();
        gallery = new GameObject("MuseumOfHumanIdeas_Gallery");
        gallery.transform.position = WorldOrigin;

        CreateBox("Gallery marble floor", new Vector3(0f, -0.15f, 0f), new Vector3(21f, 0.3f, 18f), limestone, true);
        foreach(int side in new[]{-1,1})
        {
            foreach(int end in new[]{-1,1})CreateBox("Gallery side doorway wing",new Vector3(side*10.5f,3.2f,end*5.5f),new Vector3(.35f,6.4f,7),navy,true);
            CreateBox("Gallery side doorway lintel",new Vector3(side*10.5f,5,0),new Vector3(.35f,2.8f,4),navy,true);
            CreateBox("History entrance wall",new Vector3(side*6.25f,3.2f,9),new Vector3(8.5f,6.4f,.35f),navy,true);
        }
        CreateBox("History entrance lintel",new Vector3(0,5,9),new Vector3(4,2.8f,.35f),navy,true);
        BuildCourtyard();
        CreateBox("Gallery ceiling", new Vector3(0f, 6.35f, 0f), new Vector3(21f, 0.25f, 18f), navy, false);

        AddWorldLabel("HUMAN IDEAS / NATURAL HISTORY", new Vector3(0f, 4.8f, -8.45f), .8f, ivory.color, new Vector2(7.4f, 0.8f));
        AddInteriorDetails();
        AddGalleryLighting();
        MuseumArchitecture.Extend(gallery.transform);

        CreateArtifact("Rosetta Stone", "Egyptian decree, 196 BCE", "A decree from 196 BCE written in Egyptian hieroglyphic, Demotic, and Ancient Greek. Because scholars could read Greek, it helped decipher hieroglyphs; it is not a bilingual dictionary.", "egypt, writing, language, ptolemaic, 196 bce", new Vector3(-7.1f, 0f, 2.3f), BuildRosettaStone);
        CreateArtifact("Antikythera Mechanism", "Greek astronomical instrument", "A fragmented ancient Greek geared device, probably from the second or first century BCE, that modelled astronomical cycles. Its exact original purpose is still debated.", "greece, astronomy, technology, gears, antiquity", new Vector3(-2.5f, 0f, 2.3f), BuildAntikythera);
        CreateArtifact("Roman Coin", "Roman denarius · political image", "Roman coins circulated portraits, titles, and political messages through everyday exchange. Their imagery is evidence of public messaging, not a neutral picture of Roman life.", "rome, coin, denarius, trade, politics", new Vector3(2.5f, 0f, 2.3f), BuildRomanCoin);
        CreateArtifact("Terracotta Warrior", "Qin dynasty · third century BCE", "A life-sized terracotta figure made for the mausoleum of Qin Shi Huang, China's first emperor. Figures have individually varied features and originally carried real weapons.", "china, qin, terracotta army, archaeology, emperor", new Vector3(7.1f, 0f, 2.3f), BuildWarrior);
        CreateArtifact("Greek Amphora", "Storage vessel · ancient Greece", "A two-handled vessel used to store goods such as wine or oil. Painted scenes can illuminate myth and social ideals, but must be interpreted alongside archaeological context.", "greece, pottery, amphora, daily life, trade", new Vector3(-7.1f, 0f, 6.7f), BuildAmphora);
        CreateArtifact("Venus de Milo", "Hellenistic Greek sculpture", "A marble sculpture found on Milos in 1820 and commonly identified as Aphrodite. Its missing arms leave its original pose and attributes uncertain.", "greece, sculpture, aphrodite, hellenistic, marble", new Vector3(-2.5f, 0f, 6.7f), BuildVenus);
        CreateArtifact("Sutton Hoo Helmet", "Early seventh-century England", "A helmet buried in an early seventh-century ship burial. Iron, bronze, and garnet details show elite craftsmanship and long-distance connections; the occupant's identity remains uncertain.", "sutton hoo, england, helmet, anglo-saxon, archaeology", new Vector3(2.5f, 0f, 6.7f), BuildHelmet);
        CreateArtifact("Gutenberg Press", "Movable type · fifteenth-century Europe", "Gutenberg's mid-fifteenth-century European movable-type system helped make books faster and cheaper to reproduce. Printing technologies had earlier histories in East Asia.", "printing, books, europe, east asia, technology", new Vector3(7.1f, 0f, 6.7f), BuildPrintingPress);
        CreateArtifact("Triceratops", "Smithsonian Open Access · Late Cretaceous", "Triceratops horridus was a horned herbivore from the Late Cretaceous. Its frill and three horns are distinctive anatomical features; this downloadable Smithsonian model is a digital specimen for close inspection.", "dinosaur, fossil, paleontology, triceratops, smithsonian, open access", new Vector3(-7.1f, 0f, -2.1f), BuildTriceratops);
        CreateArtifact("Cyathea Fern", "Smithsonian Open Access · plant specimen", "This three-dimensional Cyathea furfuracea fern specimen makes plant form easy to explore. Ferns reproduce by spores rather than flowers and seeds, and their fronds unfurl as they grow.", "fern, botany, plant, spores, smithsonian, open access", new Vector3(-2.5f, 0f, -2.1f), BuildFern);
        CreateArtifact("Apollo 11 Command Module", "Smithsonian Open Access · 1969", "The Apollo 11 Command Module carried Neil Armstrong, Buzz Aldrin, and Michael Collins and returned the crew to Earth in July 1969. Inspect the spacecraft's compact exterior and heat-shielded return capsule.", "apollo 11, spaceflight, engineering, nasa, smithsonian, open access", new Vector3(2.5f, 0f, -2.1f), BuildApolloCommandModule);
        CreateArtifact("George Washington Bust", "Smithsonian Open Access · portrait history", "A digital Smithsonian model of a George Washington bust. Portraits shape public memory: ask what they show, what they leave out, and why a society chooses to represent a leader in this way.", "george washington, portrait, sculpture, united states, smithsonian, open access", new Vector3(7.1f, 0f, -2.1f), BuildWashingtonBust);

        AddCollectionDisplay("FossilCabinet", "Fossil studies", new Vector3(6.5f, 0f, -6f),
            "Original interpretive ammonite spiral and fern impression in shale. These are schematic reconstructions, not scanned specimens. Ammonites were extinct marine molluscs; ferns reproduce through spores.");
        AddCollectionDisplay("ForumTable", "Roman architecture miniature", new Vector3(0f, 0f, -5f),
            "An original tabletop study of Roman architecture: colonnaded temples, terracotta roofs, courtyard houses and an arcaded amphitheatre. It is an interpretive composition, not an accurate map of ancient Rome.");

        NavMeshSurface surface = gallery.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.Children;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.layerMask = ~0;
        surface.BuildNavMesh();
        MuseumArchitecture.Populate(gallery.transform);
    }

    private static void BuildCourtyard()
    {
        GameObject source = Resources.Load<GameObject>("LumenCollection/MuseumCourtyard");
        if (source != null)
        {
            GameObject courtyard = Object.Instantiate(source, gallery.transform, false);
            courtyard.transform.localPosition = new Vector3(0f, 0f, -9f);
            courtyard.name = "Compact classical entrance courtyard";
        }
        // Separate invisible physics preserves the 5.5 m doorway and level route.
        AddStructureCollider("Courtyard walkable foundation", new Vector3(0f,-.14f,-14.5f), new Vector3(15f,.28f,11f));
        foreach (int side in new[]{-1,1})
        {
            AddStructureCollider("Facade wing", new Vector3(side*6.8f,2.9f,-9f),new Vector3(7.4f,5.8f,.65f));
            AddStructureCollider("Reflecting basin",new Vector3(side*4.65f,.35f,-14f),new Vector3(3.65f,.7f,4.8f));
            foreach(float x in new[]{3.15f,4.30f})
                AddStructureCollider("Portico column",new Vector3(side*x,2.65f,-9.68f),new Vector3(.78f,5.3f,.9f));
            foreach(float z in new[]{-11.25f,-16.75f})
                AddStructureCollider("Hedge planter",new Vector3(side*4.65f,.40f,z),new Vector3(3.7f,.8f,.7f));
            CreateBox("Low courtyard boundary",new Vector3(side*7.45f,.4f,-14.5f),new Vector3(.22f,.8f,11f),limestone,true);
            foreach(float z in new[]{-11f,-17.7f})
            {
                GameObject lamp = new GameObject("Courtyard lantern glow"); lamp.transform.SetParent(gallery.transform,false);
                lamp.transform.localPosition = new Vector3(side*1.92f,1.53f,z);
                Light light = lamp.AddComponent<Light>(); light.type=LightType.Point;
                light.color=new Color(1f,.73f,.40f); light.intensity=2f; light.range=3.2f;
            }
        }
        AddStructureCollider("Courtyard end boundary",new Vector3(0f,.25f,-20f),new Vector3(15f,.5f,.2f));
    }

    private static void AddStructureCollider(string name, Vector3 position, Vector3 size)
    {
        GameObject structure = new GameObject(name,typeof(BoxCollider));
        structure.transform.SetParent(gallery.transform,false); structure.transform.localPosition=position;
        structure.GetComponent<BoxCollider>().size=size;
    }

    private static void AddCollectionDisplay(string resource, string label, Vector3 position, string description)
    {
        GameObject source=Resources.Load<GameObject>("LumenCollection/"+resource);
        if(source==null) { Debug.LogWarning("Missing compact museum display: "+resource); return; }
        GameObject display=Object.Instantiate(source,gallery.transform,false); display.name=label;
        display.transform.localPosition=position;
        ObjectMetadata metadata=display.AddComponent<ObjectMetadata>();
        metadata.objectName=label; metadata.displayName=label; metadata.objectId="museum_"+resource;
        metadata.description=description; metadata.descriptionSummary=description;
        metadata.scenario="museum"; metadata.category="Interpretive museum exhibit"; metadata.isInteractable=true;
        metadata.interactionHint="Press E to ask about "+label;
        LearningModelInspection.MakeSelectable(display);
        AddWorldLabel(label.ToUpperInvariant(),position+new Vector3(0f,1.72f,-.8f),.38f,ivory.color,new Vector2(3.7f,.4f));
    }

    private static void AddInteriorDetails()
    {
        Material timber=MakeMaterial("Museum dark walnut",new Color(.15f,.085f,.045f));
        Material runner=MakeMaterial("Museum oxblood textile",new Color(.25f,.04f,.04f));
        CreateBox("Arrival gallery runner",new Vector3(0f,.012f,-2f),new Vector3(2.5f,.025f,13f),runner,false);
        for(int side=-1;side<=1;side+=2)
        {
            CreateBox("Gallery stone skirting",new Vector3(side*10.22f,.35f,0f),new Vector3(.17f,.7f,17.8f),limestone,false);
            CreateBox("Gallery bronze picture rail",new Vector3(side*10.19f,3.5f,0f),new Vector3(.08f,.05f,17.8f),brass,false);
            foreach(float z in new[]{-6f,-1f,4f,8f})
            {
                CreateBox("Gallery wall pilaster",new Vector3(side*10.13f,3f,z),new Vector3(.28f,6f,.42f),limestone,false);
                CreateBox("Pilaster capital",new Vector3(side*10.07f,5.5f,z),new Vector3(.4f,.20f,.65f),ivory,false);
            }
        }
        foreach(float z in new[]{-6f,-1f,4f,8f})
        {
            CreateBox("Timber ceiling coffer beam",new Vector3(0f,6.02f,z),new Vector3(20.8f,.35f,.28f),timber,false);
            CreateBox("Ceiling bronze inlay",new Vector3(0f,5.83f,z),new Vector3(20.8f,.025f,.045f),brass,false);
        }
    }

    public static bool ShowArtifact(string requested)
    {
        if (string.IsNullOrWhiteSpace(requested)) return false;
        foreach (ObjectMetadata metadata in Object.FindObjectsByType<ObjectMetadata>(FindObjectsInactive.Exclude))
        {
            if (metadata.scenario != "museum") continue;
            if (metadata.GetDisplayName().IndexOf(requested, System.StringComparison.OrdinalIgnoreCase) < 0
                && requested.IndexOf(metadata.GetDisplayName(), System.StringComparison.OrdinalIgnoreCase) < 0) continue;
            LearningModelPulse pulse = metadata.GetComponent<LearningModelPulse>();
            if (pulse == null) pulse = metadata.gameObject.AddComponent<LearningModelPulse>();
            pulse.Play();
            CafeWalkController explorer = Object.FindAnyObjectByType<CafeWalkController>();
            if (explorer != null) explorer.FaceTarget(metadata.transform);
            return true;
        }
        return false;
    }

    private delegate void ArtifactBuilder(Transform artifact);

    private static void CreateArtifact(string name, string subtitle, string description, string tags, Vector3 position, ArtifactBuilder build)
    {
        position=MuseumArchitecture.ExhibitPosition(name,position);
        GameObject display = new GameObject("Museum Exhibit · " + name);
        display.transform.SetParent(gallery.transform, false);
        display.transform.localPosition = position;
        ObjectMetadata metadata = display.AddComponent<ObjectMetadata>();
        metadata.objectId = "museum_" + name.ToLowerInvariant().Replace(' ', '_');
        metadata.displayName = name;
        metadata.objectName = name;
        metadata.category = "Museum artifact replica";
        metadata.descriptionSummary = description;
        metadata.description = description;
        metadata.contextualTags = "museum, artifact, replica, " + tags;
        metadata.scenario = "museum";
        metadata.isInteractable = true;
        metadata.interactionHint = "Press E to hear the curator's explanation";
        SphereCollider selection = display.AddComponent<SphereCollider>();
        selection.center = new Vector3(0f, 1.5f, 0f);
        selection.radius = 1.2f;
        selection.isTrigger = true;

        GameObject pedestal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pedestal.name = name + " · exhibit plinth"; pedestal.transform.SetParent(display.transform, false); pedestal.transform.localPosition = new Vector3(0f, 0.43f, 0f); pedestal.transform.localScale = new Vector3(1.7f, 0.42f, 1.7f); pedestal.GetComponent<Renderer>().sharedMaterial = navy; Object.Destroy(pedestal.GetComponent<Collider>());
        GameObject trim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trim.name = name + " · brass inlay"; trim.transform.SetParent(display.transform, false); trim.transform.localPosition = new Vector3(0f, 0.66f, 0f); trim.transform.localScale = new Vector3(1.55f, 0.025f, 1.55f); trim.GetComponent<Renderer>().sharedMaterial = brass; Object.Destroy(trim.GetComponent<Collider>());
        build(display.transform);
        LearningModelInspection.MakeSelectable(display);
        AddWorldLabel(name.ToUpperInvariant(), new Vector3(position.x, position.y + 3.15f, position.z - 0.7f), 0.52f, ivory.color, new Vector2(3.5f, 0.42f));
        AddWorldLabel(subtitle, new Vector3(position.x, position.y + 2.83f, position.z - 0.7f), 0.28f, brass.color, new Vector2(3.6f, 0.3f));
    }

    private static void BuildRosettaStone(Transform parent)
    {
        GameObject slab = CreatePrimitive(parent, PrimitiveType.Cube, "Replica stone tablet", new Vector3(0f, 1.55f, 0f), new Vector3(0.98f, 1.55f, 0.24f), ivory);
        slab.transform.localRotation = Quaternion.Euler(0f, -8f, 0f);
        Material writing = MakeMaterial("Carved inscription", new Color(0.18f, 0.20f, 0.22f));
        for (int i = 0; i < 7; i++) CreateBox("Stone inscription line", new Vector3(-0.25f, 2.12f - i * 0.16f, -0.14f), new Vector3(0.48f + (i % 3) * 0.08f, 0.024f, 0.018f), writing, false, parent);
    }

    private static void BuildAntikythera(Transform parent)
    {
        CreatePrimitive(parent, PrimitiveType.Cylinder, "Bronze mechanism plate", new Vector3(0f, 1.55f, 0f), new Vector3(0.98f, 0.12f, 0.98f), brass).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        AddRing(parent, "Outer gear ring", new Vector3(0f, 1.55f, -0.12f), 0.75f, 0.045f, ivory);
        AddRing(parent, "Inner gear ring", new Vector3(0f, 1.55f, -0.14f), 0.42f, 0.035f, brass);
        CreateBox("Mechanism indicator hand", new Vector3(0.16f, 1.55f, -0.18f), new Vector3(0.55f, 0.035f, 0.03f), ivory, false, parent).transform.localRotation = Quaternion.Euler(0f, 0f, -28f);
        CreateBox("Mechanism indicator hand", new Vector3(-0.08f, 1.55f, -0.20f), new Vector3(0.04f, 0.47f, 0.03f), ivory, false, parent).transform.localRotation = Quaternion.Euler(0f, 0f, 24f);
    }

    private static void BuildRomanCoin(Transform parent)
    {
        GameObject coin = CreatePrimitive(parent, PrimitiveType.Cylinder, "Replica denarius", new Vector3(0f, 1.42f, 0f), new Vector3(0.90f, 0.12f, 0.90f), brass);
        coin.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        AddRing(parent, "Coin relief rim", new Vector3(0f, 1.42f, -0.13f), 0.70f, 0.04f, ivory);
        CreatePrimitive(parent, PrimitiveType.Sphere, "Coin portrait relief", new Vector3(0f, 1.42f, -0.17f), new Vector3(0.36f, 0.50f, 0.06f), ivory);
    }

    private static void BuildWarrior(Transform parent)
    {
        Material clay = MakeMaterial("Terracotta", new Color(0.67f, 0.32f, 0.20f));
        CreatePrimitive(parent, PrimitiveType.Capsule, "Terracotta armor", new Vector3(0f, 1.40f, 0f), new Vector3(0.60f, 0.70f, 0.44f), clay);
        CreatePrimitive(parent, PrimitiveType.Sphere, "Terracotta head", new Vector3(0f, 2.22f, 0f), new Vector3(0.42f, 0.50f, 0.40f), clay);
        CreateBox("Terracotta left arm", new Vector3(-0.51f, 1.67f, 0f), new Vector3(0.48f, 0.16f, 0.18f), clay, false, parent);
        CreateBox("Terracotta right arm", new Vector3(0.51f, 1.67f, 0f), new Vector3(0.48f, 0.16f, 0.18f), clay, false, parent);
        CreateBox("Terracotta belt", new Vector3(0f, 1.34f, -0.23f), new Vector3(0.48f, 0.09f, 0.035f), brass, false, parent);
    }

    private static void BuildAmphora(Transform parent)
    {
        Material ceramic = MakeMaterial("Greek painted ceramic", new Color(0.82f, 0.72f, 0.57f));
        CreatePrimitive(parent, PrimitiveType.Sphere, "Amphora vessel", new Vector3(0f, 1.42f, 0f), new Vector3(0.82f, 1.08f, 0.62f), ceramic);
        CreatePrimitive(parent, PrimitiveType.Cylinder, "Amphora neck", new Vector3(0f, 2.15f, 0f), new Vector3(0.30f, 0.28f, 0.30f), ceramic);
        CreatePrimitive(parent, PrimitiveType.Cylinder, "Amphora foot", new Vector3(0f, 0.93f, 0f), new Vector3(0.28f, 0.12f, 0.28f), ceramic);
        AddRing(parent, "Amphora handle", new Vector3(-0.49f, 1.88f, 0f), 0.30f, 0.055f, brass);
        AddRing(parent, "Amphora handle", new Vector3(0.49f, 1.88f, 0f), 0.30f, 0.055f, brass);
        CreateBox("Painted band", new Vector3(0f, 1.52f, -0.31f), new Vector3(0.46f, 0.07f, 0.035f), navy, false, parent);
    }

    private static void BuildVenus(Transform parent)
    {
        CreatePrimitive(parent, PrimitiveType.Capsule, "Marble sculpture torso", new Vector3(0f, 1.57f, 0f), new Vector3(0.52f, 0.77f, 0.38f), ivory);
        CreatePrimitive(parent, PrimitiveType.Sphere, "Marble sculpture head", new Vector3(0f, 2.50f, 0f), new Vector3(0.38f, 0.46f, 0.34f), ivory);
        CreatePrimitive(parent, PrimitiveType.Cylinder, "Sculpted base", new Vector3(0f, 0.88f, 0f), new Vector3(0.70f, 0.18f, 0.70f), limestone);
        CreateBox("Folded drapery", new Vector3(0f, 1.10f, 0f), new Vector3(0.72f, 0.32f, 0.42f), limestone, false, parent);
    }

    private static void BuildHelmet(Transform parent)
    {
        CreatePrimitive(parent, PrimitiveType.Sphere, "Gilded helmet dome", new Vector3(0f, 1.55f, 0f), new Vector3(0.85f, 0.95f, 0.72f), brass);
        CreateBox("Helmet face guard", new Vector3(0f, 1.17f, -0.32f), new Vector3(0.78f, 0.40f, 0.18f), brass, false, parent);
        AddRing(parent, "Helmet brow band", new Vector3(0f, 1.30f, -0.30f), 0.58f, 0.07f, ivory);
        CreatePrimitive(parent, PrimitiveType.Cylinder, "Helmet crest support", new Vector3(0f, 2.27f, 0f), new Vector3(0.10f, 0.25f, 0.10f), ivory);
    }

    private static void BuildPrintingPress(Transform parent)
    {
        Material wood = MakeMaterial("Press timber", new Color(0.34f, 0.21f, 0.14f));
        CreateBox("Press base", new Vector3(0f, 1.02f, 0f), new Vector3(1.28f, 0.18f, 0.72f), wood, false, parent);
        CreateBox("Press left upright", new Vector3(-0.48f, 1.72f, 0f), new Vector3(0.16f, 1.42f, 0.18f), wood, false, parent);
        CreateBox("Press right upright", new Vector3(0.48f, 1.72f, 0f), new Vector3(0.16f, 1.42f, 0.18f), wood, false, parent);
        CreateBox("Press top beam", new Vector3(0f, 2.40f, 0f), new Vector3(1.15f, 0.22f, 0.25f), brass, false, parent);
        CreatePrimitive(parent, PrimitiveType.Cylinder, "Printing screw", new Vector3(0f, 1.95f, 0f), new Vector3(0.12f, 0.48f, 0.12f), brass);
        CreateBox("Movable type tray", new Vector3(0f, 1.28f, -0.36f), new Vector3(0.62f, 0.10f, 0.18f), ivory, false, parent);
    }

    private static void BuildTriceratops(Transform parent)
    {
        if (ImportedModelUtility.AddFittedModel("SmithsonianTriceratops", parent, 2.30f, 0.73f, 180f) != null) return;
        Material fossil = MakeMaterial("Triceratops fallback", new Color(0.66f, 0.45f, 0.32f));
        CreatePrimitive(parent, PrimitiveType.Sphere, "Triceratops body", new Vector3(0f, 1.45f, 0f), new Vector3(1.6f, 0.78f, 0.74f), fossil);
        CreatePrimitive(parent, PrimitiveType.Sphere, "Triceratops head and frill", new Vector3(0f, 1.72f, -0.73f), new Vector3(1.02f, 1.02f, 0.46f), fossil);
    }

    private static void BuildFern(Transform parent)
    {
        if (ImportedModelUtility.AddFittedModel("SmithsonianFern", parent, 2.15f, 0.72f) != null) return;
        Material fern = MakeMaterial("Fern fallback", new Color(0.30f, 0.61f, 0.36f));
        for (int i = 0; i < 7; i++)
        {
            float angle = i * Mathf.PI * 2f / 7f;
            CreatePrimitive(parent, PrimitiveType.Sphere, "Fern frond", new Vector3(Mathf.Cos(angle) * 0.34f, 1.44f, Mathf.Sin(angle) * 0.24f), new Vector3(0.20f, 1.55f, 0.12f), fern).transform.localRotation = Quaternion.Euler(0f, 0f, (i - 3) * 9f);
        }
    }

    private static void BuildApolloCommandModule(Transform parent)
    {
        if (ImportedModelUtility.AddFittedModel("SmithsonianApollo11CommandModule", parent, 2.15f, 0.72f, 180f) != null) return;
        Material capsule = MakeMaterial("Apollo module fallback", new Color(0.72f, 0.73f, 0.70f));
        CreatePrimitive(parent, PrimitiveType.Cylinder, "Command module cabin", new Vector3(0f, 1.54f, 0f), new Vector3(1.02f, 0.82f, 1.02f), capsule).transform.localRotation = Quaternion.Euler(180f, 0f, 0f);
        CreatePrimitive(parent, PrimitiveType.Cylinder, "Command module base", new Vector3(0f, 0.94f, 0f), new Vector3(1.15f, 0.18f, 1.15f), brass);
    }

    private static void BuildWashingtonBust(Transform parent)
    {
        if (ImportedModelUtility.AddFittedModel("SmithsonianGeorgeWashingtonBust", parent, 1.90f, 0.72f,45f,45f,10f) != null) return;
        CreatePrimitive(parent, PrimitiveType.Capsule, "Portrait bust replica", new Vector3(0f, 1.48f, 0f), new Vector3(0.76f, 1.15f, 0.60f), ivory);
        CreatePrimitive(parent, PrimitiveType.Sphere, "Portrait head replica", new Vector3(0f, 2.22f, 0f), new Vector3(0.60f, 0.72f, 0.52f), ivory);
    }

    private static GameObject CreatePrimitive(Transform parent, PrimitiveType type, string name, Vector3 position, Vector3 scale, Material material)
    {
        GameObject item = GameObject.CreatePrimitive(type);
        item.name = name; item.transform.SetParent(parent, false); item.transform.localPosition = position; item.transform.localScale = scale; item.GetComponent<Renderer>().sharedMaterial = material; Object.Destroy(item.GetComponent<Collider>());
        return item;
    }

    private static GameObject CreateBox(string name, Vector3 localPosition, Vector3 scale, Material material, bool keepCollider, Transform parent = null)
    {
        GameObject item = GameObject.CreatePrimitive(PrimitiveType.Cube);
        item.name = name; item.transform.SetParent(parent != null ? parent : gallery.transform, false); item.transform.localPosition = localPosition; item.transform.localScale = scale; item.GetComponent<Renderer>().sharedMaterial = material;
        if (!keepCollider) Object.Destroy(item.GetComponent<Collider>());
        return item;
    }

    private static void AddRing(Transform parent, string name, Vector3 position, float radius, float width, Material material)
    {
        GameObject lineObject = new GameObject(name);
        lineObject.transform.SetParent(parent, false);
        LineRenderer line = lineObject.AddComponent<LineRenderer>();
        line.useWorldSpace = false; line.loop = true; line.positionCount = 64; line.startWidth = line.endWidth = width; line.sharedMaterial = material; line.startColor = line.endColor = material.color;
        for (int i = 0; i < 64; i++) { float angle = i / 64f * Mathf.PI * 2f; line.SetPosition(i, position + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f)); }
    }

    private static void AddWorldLabel(string content, Vector3 position, float size, Color color, Vector2 dimensions)
    {
        GameObject go = new GameObject("Museum placard · " + content, typeof(TextMeshPro));
        go.transform.SetParent(gallery.transform, false); go.transform.localPosition = position;
        TextMeshPro text = go.GetComponent<TextMeshPro>(); text.text = content; text.fontSize = size*4f; text.color = color; text.alignment = TextAlignmentOptions.Center; text.fontStyle = FontStyles.Bold; text.outlineWidth = 0.08f; text.outlineColor = new Color(0.025f, 0.04f, 0.07f); text.rectTransform.sizeDelta = dimensions; go.AddComponent<WorldLabelBillboard>();
    }

    private static void AddGalleryLighting()
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.62f, 0.58f, 0.49f);
        RenderSettings.ambientIntensity = 1.1f;
        RenderSettings.reflectionIntensity = 0.35f;
        GameObject lightRoot = new GameObject("MuseumGalleryLighting"); lightRoot.transform.SetParent(gallery.transform, false);
        Light ambient = lightRoot.AddComponent<Light>(); ambient.type = LightType.Point; ambient.color = new Color(1f, .88f, .70f); ambient.intensity = 35f; ambient.range = 25f; ambient.transform.localPosition = new Vector3(0f, 5f, 0f);
        GameObject museumSun = new GameObject("Museum soft directional wash"); museumSun.transform.SetParent(gallery.transform, false);
        museumSun.transform.localRotation = Quaternion.Euler(46f, -24f, 0f);
        Light directional = museumSun.AddComponent<Light>(); directional.type = LightType.Directional; directional.color = new Color(1f, .92f, .78f); directional.intensity = 1.6f;
        directional.shadows=LightShadows.Soft; directional.shadowStrength=.45f;
        foreach (float x in new[] { -6.5f, -2.2f, 2.2f, 6.5f })
        {
            GameObject accent = new GameObject("Exhibit spotlight"); accent.transform.SetParent(gallery.transform, false); accent.transform.localPosition = new Vector3(x, 4.8f, 3.8f);
            Light light = accent.AddComponent<Light>(); light.type = LightType.Spot; light.color = new Color(1f, 0.86f, 0.68f); light.intensity = 34f; light.range = 16f; light.spotAngle = 74f; light.transform.localRotation = Quaternion.Euler(35f, 0f, 0f);
        }
    }

    private static void MakeMaterials()
    {
        navy = MakeMaterial("Museum warm charcoal", new Color(0.14f, 0.105f, 0.075f));
        limestone = MakeMaterial("Museum limestone", new Color(0.64f, 0.62f, 0.56f));
        brass = MakeMaterial("Museum brass", new Color(0.73f, 0.53f, 0.25f), true);
        ivory = MakeMaterial("Museum ivory", new Color(0.85f, 0.81f, 0.70f));
        glass = MakeMaterial("Museum display glass", new Color(0.4f, 0.7f, 0.75f, 0.22f));
    }

    private static Material MakeMaterial(string name, Color color, bool emissive = false)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Unlit/Color");
        Material material = LumenRuntimeResources.Own(new Material(shader) { name = name, color = color });
        if (emissive && material.HasProperty("_EmissionColor")) { material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor", color * 0.3f); }
        return material;
    }
}
